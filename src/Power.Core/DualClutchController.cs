// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed record DualClutchControllerDefinition
{
    public uint VehicleNode { get; init; }
    public uint OddClutch { get; init; }
    public uint EvenClutch { get; init; }
    private IReadOnlyList<uint> _selectors = Array.Empty<uint>();
    public IReadOnlyList<uint> Selectors
    {
        get => _selectors;
        init => _selectors = Array.AsReadOnly((value ?? throw new ArgumentNullException(nameof(value))).ToArray());
    }
    public ulong SamplePeriodNanoseconds { get; init; }
    public ulong ReleaseNanoseconds { get; init; }
    public ulong EngageNanoseconds { get; init; }
    public ulong SynchronizeTimeoutNanoseconds { get; init; }
    public Quantity SynchronizeTolerance { get; init; }
    public Quantity DirectionChangeSpeedLimit { get; init; }
}
public enum DctShiftPhase { Neutral, Preparing, Releasing, Synchronizing, Engaging, Driving, Fault }
public enum DctFault { None, SynchronizationTimeout, DirectionChangeBlocked, ConfirmedLockLost }

internal sealed record CompiledDctController(int Component, int Vehicle, int Odd, int Even, int[] Selectors,
    ulong Period, ulong Release, ulong Engage, ulong Timeout, double Tolerance, double DirectionLimit);

internal sealed class DctControllerState(int count)
{
    internal readonly int[] Active = new int[count], Target = new int[count], Odd = new int[count], Even = new int[count];
    internal readonly DctShiftPhase[] Phase = new DctShiftPhase[count];
    internal readonly DctFault[] Fault = new DctFault[count];
    internal readonly ulong[] Started = new ulong[count];
    internal readonly ulong[] LostAt = new ulong[count];
    internal readonly double[] Error = new double[count];
    internal void CopyFrom(DctControllerState other)
    {
        Array.Copy(other.Active, Active, count); Array.Copy(other.Target, Target, count); Array.Copy(other.Odd, Odd, count); Array.Copy(other.Even, Even, count);
        Array.Copy(other.Phase, Phase, count); Array.Copy(other.Fault, Fault, count); Array.Copy(other.Started, Started, count); Array.Copy(other.Error, Error, count);
        Array.Copy(other.LostAt, LostAt, count);
    }
    internal ulong Hash(ulong hash)
    {
        for (int i = 0; i < count; ++i)
        {
            hash = Numeric.Hash(hash, unchecked((ulong)Active[i])); hash = Numeric.Hash(hash, unchecked((ulong)Target[i]));
            hash = Numeric.Hash(hash, unchecked((ulong)Odd[i])); hash = Numeric.Hash(hash, unchecked((ulong)Even[i]));
            hash = Numeric.Hash(hash, (ulong)Phase[i]); hash = Numeric.Hash(hash, (ulong)Fault[i]);
            hash = Numeric.Hash(hash, Started[i]); hash = Numeric.Hash(hash, Error[i]);
            hash = Numeric.Hash(hash, LostAt[i]);
        }
        return hash;
    }
}

internal static class DctControl
{
    private static bool Odd(int gear) => gear > 0 && gear % 2 == 1;
    private static int Slot(int gear) => gear == -1 ? 7 : gear - 1;
    private static double Slip(CompiledModel model, int component, double[] x)
    {
        var c = model.Components[component];
        return x[model.Nodes[c.A].Index + 1] - c.P3 * x[model.Nodes[c.B].Index + 1];
    }
    private static void Select(CompiledDctController controller, DctControllerState state, int index, bool odd, int gear, double[] inputs)
    {
        for (int i = 0; i < 8; ++i)
            if ((i < 7 && i % 2 == 0) == odd)
            {
                int component = controller.Selectors[i];
                inputs[component] = gear != 0 && Slot(gear) == i ? Math.Min(1, inputs[component] + (double)controller.Period / controller.Engage) : 0;
            }
        if (odd) state.Odd[index] = gear; else state.Even[index] = gear;
    }
    private static void Neutral(CompiledDctController controller, DctControllerState state, int index, double[] inputs)
    {
        inputs[controller.Odd] = inputs[controller.Even] = 0;
        Select(controller, state, index, true, 0, inputs); Select(controller, state, index, false, 0, inputs);
        state.Active[index] = 0;
        state.LostAt[index] = 0;
    }
    private static void Transition(DctControllerState state, int index, DctShiftPhase phase, ulong now)
    { state.Phase[index] = phase; state.Started[index] = now; state.LostAt[index] = 0; }
    private static void Fail(CompiledDctController controller, DctControllerState state, int index, DctFault fault, double[] inputs)
    { Neutral(controller, state, index, inputs); state.Fault[index] = fault; state.Phase[index] = DctShiftPhase.Fault; }

    internal static bool Sample(CompiledModel model, CompiledDctController c, DctControllerState s, int k,
        ulong now, int requested, double[] x, double[] inputs, ClutchState clutches)
    {
        if (s.Phase[k] == DctShiftPhase.Fault)
        {
            Neutral(c, s, k, inputs);
            if (requested == s.Target[k]) return true;
            s.Fault[k] = DctFault.None; Transition(s, k, DctShiftPhase.Neutral, now);
        }
        if (requested == 0)
        {
            Neutral(c, s, k, inputs); s.Target[k] = 0; s.Fault[k] = DctFault.None; s.Error[k] = 0;
            Transition(s, k, DctShiftPhase.Neutral, now); return true;
        }
        if (s.Phase[k] is DctShiftPhase.Neutral or DctShiftPhase.Driving && requested != s.Active[k])
        {
            double speed = x[c.Vehicle + 1]; s.Target[k] = requested;
            if ((requested == -1 && speed > c.DirectionLimit) || (requested > 0 && speed < -c.DirectionLimit))
            { Fail(c, s, k, DctFault.DirectionChangeBlocked, inputs); return true; }
            bool samePath = s.Active[k] != 0 && Odd(s.Active[k]) == Odd(requested);
            Transition(s, k, samePath || s.Active[k] == 0 ? DctShiftPhase.Releasing : DctShiftPhase.Preparing, now);
        }
        int target = s.Target[k]; bool oddTarget = Odd(target); int drive = oddTarget ? c.Odd : c.Even, other = oddTarget ? c.Even : c.Odd;
        int selector = c.Selectors[Slot(target)];
        s.Error[k] = Slip(model, selector, x);
        if (!Numeric.Finite(s.Error[k])) return false;
        bool synced = inputs[selector] == 1 && Math.Abs(s.Error[k]) <= c.Tolerance && clutches.Mode[model.Components[selector].Index] == ClutchMode.Locked;
        ulong elapsed = now - s.Started[k];
        switch (s.Phase[k])
        {
            case DctShiftPhase.Preparing:
                inputs[drive] = 0; Select(c, s, k, oddTarget, target, inputs);
                if (synced) Transition(s, k, DctShiftPhase.Releasing, now);
                else if (elapsed >= c.Timeout) Fail(c, s, k, DctFault.SynchronizationTimeout, inputs);
                break;
            case DctShiftPhase.Releasing:
                inputs[drive] = 0;
                if (s.Active[k] != 0) inputs[Odd(s.Active[k]) ? c.Odd : c.Even] = Math.Max(0, 1 - (double)elapsed / c.Release);
                if (s.Active[k] == 0 || elapsed >= c.Release)
                {
                    inputs[c.Odd] = inputs[c.Even] = 0; s.Active[k] = 0;
                    Select(c, s, k, oddTarget, target, inputs); Transition(s, k, DctShiftPhase.Synchronizing, now);
                }
                break;
            case DctShiftPhase.Synchronizing:
                inputs[c.Odd] = inputs[c.Even] = 0; Select(c, s, k, oddTarget, target, inputs);
                if (synced) Transition(s, k, DctShiftPhase.Engaging, now);
                else if (elapsed >= c.Timeout) Fail(c, s, k, DctFault.SynchronizationTimeout, inputs);
                break;
            case DctShiftPhase.Engaging:
                inputs[other] = 0; inputs[drive] = Math.Min(1, (double)elapsed / c.Engage);
                if (elapsed >= c.Engage && Math.Abs(Slip(model, drive, x)) <= c.Tolerance && clutches.Mode[model.Components[drive].Index] == ClutchMode.Locked)
                { s.Active[k] = target; Transition(s, k, DctShiftPhase.Driving, now); }
                else if (elapsed >= c.Engage && elapsed - c.Engage >= c.Timeout) Fail(c, s, k, DctFault.SynchronizationTimeout, inputs);
                break;
            case DctShiftPhase.Driving:
                if (!synced || Math.Abs(Slip(model, drive, x)) > c.Tolerance || clutches.Mode[model.Components[drive].Index] != ClutchMode.Locked)
                {
                    if (s.LostAt[k] == 0) s.LostAt[k] = now;
                    if (now - s.LostAt[k] >= c.Timeout) { Fail(c, s, k, DctFault.ConfirmedLockLost, inputs); break; }
                }
                else s.LostAt[k] = 0;
                inputs[drive] = 1; inputs[other] = 0;
                if (target > 0 && target < 7) Select(c, s, k, !oddTarget, target + 1, inputs);
                break;
        }
        return true;
    }
}
