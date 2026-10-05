// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public readonly record struct AtActuatorRoute(uint Clutch, uint FillValve, uint DrainValve);
public sealed record HydraulicAtControllerDefinition
{
    public uint VehicleNode { get; init; }
    public uint RingNode { get; init; }
    public uint SupplyPressureNode { get; init; }
    private IReadOnlyList<AtActuatorRoute> _routes = Array.Empty<AtActuatorRoute>();
    public IReadOnlyList<AtActuatorRoute> Routes
    {
        get => _routes;
        init => _routes = Array.AsReadOnly((value ?? throw new ArgumentNullException(nameof(value))).ToArray());
    }
    public ulong SamplePeriodNanoseconds { get; init; }
    public ulong ReleaseTimeoutNanoseconds { get; init; }
    public ulong ApplyTimeoutNanoseconds { get; init; }
    public ulong LowSupplyTimeoutNanoseconds { get; init; }
    public ulong LockupDwellNanoseconds { get; init; }
    public Quantity ApplyPressure { get; init; }
    public Quantity PressureTolerance { get; init; }
    public Quantity MinimumSupplyPressure { get; init; }
    public Quantity ReleaseForce { get; init; }
    public Quantity MinimumApplyForce { get; init; }
    public Quantity ProportionalGain { get; init; }
    public Quantity IntegralGain { get; init; }
    public Quantity SynchronizeTolerance { get; init; }
    public Quantity DirectionChangeSpeedLimit { get; init; }
    public Quantity LockupSpeedLimit { get; init; }
    public Quantity UnlockSpeedLimit { get; init; }
    public Quantity MinimumLockupInputSpeed { get; init; }
    public int MinimumLockupForwardRange { get; init; }
}
public enum AtShiftPhase { Neutral, Releasing, Applying, Driving, Fault }
public enum AtControlFault { None, ReleaseTimeout, ApplyTimeout, LowSupplyPressure, DirectionChangeBlocked, ConfirmedLockLost }
public enum AtLockupPhase { Released, Applying, Locked, Releasing }
internal sealed record CompiledAtRoute(int Clutch, int Piston, int Pressure, int Position, int Fill, int Drain);
internal sealed record CompiledAtController(int Component, int Input, int Vehicle, int Supply, CompiledAtRoute[] Routes,
    ulong Period, ulong ReleaseTimeout, ulong ApplyTimeout, ulong SupplyTimeout, ulong LockupDwell,
    double ApplyPressure, double PressureTolerance, double MinimumSupply, double ReleaseForce, double ApplyForce,
    double Kp, double Ki, double SyncTolerance, double DirectionLimit, double LockupLimit, double UnlockLimit, double LockupMinimumSpeed, int LockupMinimumRange);
internal sealed class AtControllerState
{
    internal readonly int[] Active, Target;
    internal readonly AtShiftPhase[] Phase;
    internal readonly AtControlFault[] Fault;
    internal readonly ulong[] Started, LostAt, LowSupplyAt;
    internal readonly double[] Error, LinePressure;
    internal readonly bool[] LockupRequested;
    internal readonly double[][] Integral;
    internal AtControllerState(int count)
    {
        Active = new int[count]; Target = new int[count]; Phase = new AtShiftPhase[count]; Fault = new AtControlFault[count];
        Started = new ulong[count]; LostAt = new ulong[count]; LowSupplyAt = new ulong[count];
        Array.Fill(LostAt, ulong.MaxValue); Array.Fill(LowSupplyAt, ulong.MaxValue);
        Error = new double[count]; LinePressure = new double[count]; LockupRequested = new bool[count];
        Integral = Enumerable.Range(0, count).Select(_ => new double[6]).ToArray();
    }
    internal void CopyFrom(AtControllerState other)
    {
        int count = Active.Length;
        Array.Copy(other.Active, Active, count); Array.Copy(other.Target, Target, count); Array.Copy(other.Phase, Phase, count); Array.Copy(other.Fault, Fault, count);
        Array.Copy(other.Started, Started, count); Array.Copy(other.LostAt, LostAt, count); Array.Copy(other.LowSupplyAt, LowSupplyAt, count);
        Array.Copy(other.Error, Error, count); Array.Copy(other.LinePressure, LinePressure, count); Array.Copy(other.LockupRequested, LockupRequested, count);
        for (int i = 0; i < count; ++i) Array.Copy(other.Integral[i], Integral[i], 6);
    }
    internal ulong Hash(ulong hash)
    {
        for (int i = 0; i < Active.Length; ++i)
        {
            hash = Numeric.Hash(hash, unchecked((ulong)Active[i])); hash = Numeric.Hash(hash, unchecked((ulong)Target[i]));
            hash = Numeric.Hash(hash, (ulong)Phase[i]); hash = Numeric.Hash(hash, (ulong)Fault[i]);
            hash = Numeric.Hash(hash, Started[i]); hash = Numeric.Hash(hash, LostAt[i]); hash = Numeric.Hash(hash, LowSupplyAt[i]);
            hash = Numeric.Hash(hash, Error[i]); hash = Numeric.Hash(hash, LinePressure[i]); hash = Numeric.Hash(hash, LockupRequested[i] ? 1UL : 0UL);
            foreach (double integral in Integral[i]) hash = Numeric.Hash(hash, integral);
        }
        return hash;
    }
    internal bool Finite()
    {
        for (int i = 0; i < Active.Length; ++i)
        {
            if (!Numeric.Finite(Error[i]) || !Numeric.Finite(LinePressure[i])) return false;
            foreach (double value in Integral[i]) if (!Numeric.Finite(value) || Math.Abs(value) > 1) return false;
        }
        return true;
    }
}

internal static class AtControl
{
    internal static bool Selected(int route, int gear) => route switch
    { 0 => gear is 3 or 4, 1 => gear is 1 or 2 or 3, 2 => gear == -1, 3 => gear is 1 or -1, 4 => gear is 2 or 4, _ => false };
    private static double Force(CompiledModel model, CompiledAtRoute route, double[] x) => model.Pistons[route.Piston]!.ContactForce(x[route.Position]);
    private static double Slip(CompiledModel model, CompiledAtRoute route, double[] x)
    {
        var c = model.Components[route.Clutch]; return x[model.Nodes[c.A].Index + 1] - (c.B < 0 ? 0 : c.P3*x[model.Nodes[c.B].Index + 1]);
    }
    internal static bool Confirmed(CompiledModel model, CompiledAtController c, int gear, double[] x, ClutchState clutches)
    {
        if (gear == 0) return false;
        for (int i = 0; i < 5; ++i)
        {
            var route = c.Routes[i]; double force = Force(model, route, x);
            if (Selected(i, gear))
            {
                if (force < c.ApplyForce || Math.Abs(Slip(model, route, x)) > c.SyncTolerance || clutches.Mode[model.Components[route.Clutch].Index] != ClutchMode.Locked) return false;
            }
            else if (force > c.ReleaseForce) return false;
        }
        return true;
    }
    internal static AtLockupPhase Lockup(CompiledModel model, CompiledAtController c, AtControllerState s, int k, double[] x, ClutchState clutches)
    {
        if (c.Routes.Length != 6) return AtLockupPhase.Released;
        var route = c.Routes[5];
        if (!s.LockupRequested[k]) return Force(model, route, x) <= c.ReleaseForce ? AtLockupPhase.Released : AtLockupPhase.Releasing;
        return clutches.Mode[model.Components[route.Clutch].Index] == ClutchMode.Locked && Math.Abs(Slip(model, route, x)) <= c.SyncTolerance ? AtLockupPhase.Locked : AtLockupPhase.Applying;
    }
    private static void Vent(CompiledAtController c, AtControllerState s, int k, double[] inputs)
    {
        for (int i = 0; i < c.Routes.Length; ++i)
        { var r = c.Routes[i]; inputs[r.Fill] = 0; inputs[r.Drain] = 1; s.Integral[k][i] = 0; }
        s.Active[k] = 0; s.LockupRequested[k] = false;
    }
    private static bool Released(CompiledModel model, CompiledAtController c, double[] x)
    { foreach (var route in c.Routes) if (Force(model, route, x) > c.ReleaseForce) return false; return true; }
    private static void Transition(AtControllerState s, int k, AtShiftPhase phase, ulong now)
    { s.Phase[k] = phase; s.Started[k] = now; s.LostAt[k] = s.LowSupplyAt[k] = ulong.MaxValue; }
    private static void Fail(CompiledAtController c, AtControllerState s, int k, AtControlFault fault, double[] inputs)
    { Vent(c, s, k, inputs); s.Fault[k] = fault; s.Phase[k] = AtShiftPhase.Fault; }
    private static bool Pressure(CompiledAtController c, AtControllerState s, int k, int slot, double pressure, double[] inputs)
    {
        double error = c.ApplyPressure-pressure, increment = c.Ki*error*(c.Period*1e-9);
        double tentative = s.Integral[k][slot]+increment, raw = c.Kp*error+tentative;
        if (!Numeric.Finite(error) || !Numeric.Finite(raw)) return false;
        if (!(raw>1 && error>0 || raw< -1 && error<0)) s.Integral[k][slot] = Math.Max(-1,Math.Min(1,tentative));
        double command = Math.Max(-1,Math.Min(1,c.Kp*error+s.Integral[k][slot])); var route = c.Routes[slot];
        inputs[route.Fill] = Math.Max(0,command); inputs[route.Drain] = Math.Max(0,-command); return true;
    }
    internal static bool Sample(CompiledModel model, CompiledAtController c, AtControllerState s, int k, ulong now, int requested,
        double[] x, double[] pressure, double[] inputs, ClutchState clutches)
    {
        s.LinePressure[k] = pressure[c.Supply]; if (!Numeric.Finite(s.LinePressure[k])) return false;
        if (s.Phase[k] == AtShiftPhase.Fault)
        {
            Vent(c,s,k,inputs); if (requested == s.Target[k]) return true;
            s.Fault[k] = AtControlFault.None; Transition(s,k,AtShiftPhase.Neutral,now);
        }
        if (requested != s.Target[k] || s.Phase[k] == AtShiftPhase.Neutral && requested != 0)
        {
            s.Target[k] = requested; Vent(c,s,k,inputs);
            double speed = x[c.Vehicle+1];
            if (requested == -1 && speed > c.DirectionLimit || requested > 0 && speed < -c.DirectionLimit)
            { Fail(c,s,k,AtControlFault.DirectionChangeBlocked,inputs); return true; }
            Transition(s,k,AtShiftPhase.Releasing,now);
        }
        if (requested == 0 && s.Phase[k] != AtShiftPhase.Neutral && s.Phase[k] != AtShiftPhase.Releasing)
        { Vent(c,s,k,inputs); Transition(s,k,AtShiftPhase.Releasing,now); }
        if (s.Phase[k] == AtShiftPhase.Neutral) { Vent(c,s,k,inputs); s.Error[k] = 0; return true; }
        if (s.Phase[k] == AtShiftPhase.Releasing)
        {
            Vent(c,s,k,inputs);
            if (Released(model,c,x)) { Transition(s,k,requested == 0 ? AtShiftPhase.Neutral : AtShiftPhase.Applying,now); }
            else if (now-s.Started[k] >= c.ReleaseTimeout) Fail(c,s,k,AtControlFault.ReleaseTimeout,inputs);
            return true;
        }
        if (s.LinePressure[k] < c.MinimumSupply)
        {
            if (s.LowSupplyAt[k] == ulong.MaxValue) s.LowSupplyAt[k] = now;
            if (now-s.LowSupplyAt[k] >= c.SupplyTimeout) { Fail(c,s,k,AtControlFault.LowSupplyPressure,inputs); return true; }
        }
        else s.LowSupplyAt[k] = ulong.MaxValue;
        double error = 0; bool ready = true;
        for (int i = 0; i < 5; ++i)
        {
            var route = c.Routes[i];
            if (Selected(i,s.Target[k]))
            {
                if (!Pressure(c,s,k,i,pressure[route.Pressure],inputs)) return false;
                error = Math.Max(error,Math.Abs(Slip(model,route,x)));
                if (Math.Abs(pressure[route.Pressure]-c.ApplyPressure) > c.PressureTolerance) ready = false;
            }
            else { inputs[route.Fill] = 0; inputs[route.Drain] = 1; s.Integral[k][i] = 0; }
        }
        s.Error[k] = error; if (!Numeric.Finite(error)) return false;
        bool confirmed = Confirmed(model,c,s.Target[k],x,clutches);
        if (s.Phase[k] == AtShiftPhase.Applying)
        {
            if (ready && confirmed) { s.Active[k] = s.Target[k]; Transition(s,k,AtShiftPhase.Driving,now); }
            else if (now-s.Started[k] >= c.ApplyTimeout) { Fail(c,s,k,AtControlFault.ApplyTimeout,inputs); return true; }
        }
        else if (!confirmed)
        {
            if (s.LostAt[k] == ulong.MaxValue) s.LostAt[k] = now;
            if (now-s.LostAt[k] >= c.ApplyTimeout) { Fail(c,s,k,AtControlFault.ConfirmedLockLost,inputs); return true; }
        }
        else s.LostAt[k] = ulong.MaxValue;
        if (c.Routes.Length == 6)
        {
            var route = c.Routes[5]; double slip = Math.Abs(Slip(model,route,x)), inputSpeed = Math.Abs(x[c.Input+1]);
            bool eligible = s.Phase[k] == AtShiftPhase.Driving && confirmed && s.Target[k] >= c.LockupMinimumRange && s.LinePressure[k] >= c.MinimumSupply;
            if (!eligible || slip > c.UnlockLimit || inputSpeed < .8*c.LockupMinimumSpeed) s.LockupRequested[k] = false;
            else if (now-s.Started[k] >= c.LockupDwell && slip <= c.LockupLimit && inputSpeed >= c.LockupMinimumSpeed) s.LockupRequested[k] = true;
            if (s.LockupRequested[k]) { if (!Pressure(c,s,k,5,pressure[route.Pressure],inputs)) return false; }
            else { inputs[route.Fill] = 0; inputs[route.Drain] = 1; s.Integral[k][5] = 0; }
        }
        return true;
    }
}
