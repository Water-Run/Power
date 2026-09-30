// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed record GasFuelInjectorDefinition
{
    public uint CrankNode { get; init; }
    public Quantity CycleAngle { get; init; }
    public Quantity StartAngle { get; init; }
    public Quantity DurationAngle { get; init; }
    public Quantity MaximumDose { get; init; }
}

public readonly record struct FuelInjectionWindow(long CycleOrdinal, double Opening);

/// <summary>Forward-crank ideal metering window. Finite gas pressure determines the deliverable fuel flow.</summary>
public sealed class FuelDoseProfile
{
    public double CycleAngleRadians { get; }
    public double StartAngleRadians { get; }
    public double DurationAngleRadians { get; }
    public double MaximumDoseKilograms { get; }
    public double MaximumTravelRadians => Math.Min(.25, DurationAngleRadians / 8);
    public FuelDoseProfile(double cycleAngleRadians, double startAngleRadians, double durationAngleRadians, double maximumDoseKilograms)
    {
        if (cycleAngleRadians != 2 * Math.PI && cycleAngleRadians != 4 * Math.PI)
            throw new ArgumentException("Injection cycle must be 360 or 720 degrees.");
        if (!Numeric.Finite(startAngleRadians) || !Numeric.Finite(durationAngleRadians) || durationAngleRadians < 1e-6 || durationAngleRadians > cycleAngleRadians ||
            !Numeric.Finite(maximumDoseKilograms) || maximumDoseKilograms <= 0)
            throw new ArgumentException("Use finite start, duration in [1e-6 rad, cycle], and positive maximum fuel dose.");
        CycleAngleRadians = cycleAngleRadians; StartAngleRadians = startAngleRadians % cycleAngleRadians;
        if (StartAngleRadians < 0) StartAngleRadians += cycleAngleRadians;
        DurationAngleRadians = durationAngleRadians; MaximumDoseKilograms = maximumDoseKilograms;
    }
    public bool TryWindow(double angleRadians, double speedRadiansPerSecond, out FuelInjectionWindow window)
    {
        window = default;
        if (!Numeric.Finite(angleRadians) || !Numeric.Finite(speedRadiansPerSecond)) return false;
        double ordinal = Math.Floor((angleRadians - StartAngleRadians) / CycleAngleRadians);
        if (!Numeric.Finite(ordinal) || Math.Abs(ordinal) >= 4_503_599_627_370_496) return false;
        double phase = (angleRadians % CycleAngleRadians - StartAngleRadians) % CycleAngleRadians;
        if (phase < 0) phase += CycleAngleRadians;
        window = new((long)ordinal, speedRadiansPerSecond > 0 && phase < DurationAngleRadians ? 1 : 0); return true;
    }
}

internal sealed record CompiledInjector(int Component, int Crank, FuelDoseProfile Profile);

/// <summary>Sampled per-cycle quotas and accepted delivery history; every field belongs to speculative simulation state.</summary>
internal sealed class FuelInjectorState(int count)
{
    internal readonly bool[] Latched = new bool[count];
    internal readonly long[] Cycle = new long[count];
    internal readonly double[] Target = new double[count], Delivered = new double[count], DeliveredCorrection = new double[count];
    internal readonly double[] Total = new double[count], TotalCorrection = new double[count], TickFuel = new double[count];
    internal void CopyFrom(FuelInjectorState other)
    {
        Array.Copy(other.Latched, Latched, count); Array.Copy(other.Cycle, Cycle, count); Array.Copy(other.Target, Target, count);
        Array.Copy(other.Delivered, Delivered, count); Array.Copy(other.DeliveredCorrection, DeliveredCorrection, count);
        Array.Copy(other.Total, Total, count); Array.Copy(other.TotalCorrection, TotalCorrection, count); Array.Copy(other.TickFuel, TickFuel, count);
    }
    internal void BeginTick() => Array.Clear(TickFuel, 0, TickFuel.Length);
    internal bool Prepare(int index, CompiledInjector injector, double[] dynamics, double requested, double duration, out double opening, out double fuelRateLimit)
    {
        opening = fuelRateLimit = 0;
        if (!(duration > 0) || !Numeric.Finite(duration) || !injector.Profile.TryWindow(dynamics[injector.Crank], dynamics[injector.Crank + 1], out var window)) return false;
        if (window.Opening == 0 || Latched[index] && window.CycleOrdinal < Cycle[index]) return true;
        if (!Latched[index] || window.CycleOrdinal > Cycle[index])
        {
            Latched[index] = true; Cycle[index] = window.CycleOrdinal; Target[index] = requested;
            Delivered[index] = DeliveredCorrection[index] = 0;
        }
        double remaining = Math.Max(0, Target[index] - Delivered[index]);
        opening = remaining > 0 ? window.Opening : 0; fuelRateLimit = remaining / duration;
        return Numeric.Finite(fuelRateLimit);
    }
    internal bool Accept(int index, double fuelKilograms)
    {
        if (!Numeric.Finite(fuelKilograms) || fuelKilograms < 0) return false;
        Numeric.Accumulate(fuelKilograms, ref Delivered[index], ref DeliveredCorrection[index]);
        Numeric.Accumulate(fuelKilograms, ref Total[index], ref TotalCorrection[index]); TickFuel[index] += fuelKilograms;
        return Delivered[index] <= Target[index] + 16 * GearReference.Epsilon * Math.Max(Target[index], Delivered[index]);
    }
    internal double Opening(int index, CompiledInjector injector, double[] dynamics)
    {
        if (!Latched[index] || Delivered[index] >= Target[index] || !injector.Profile.TryWindow(dynamics[injector.Crank], dynamics[injector.Crank + 1], out var window) || window.CycleOrdinal != Cycle[index]) return 0;
        return window.Opening;
    }
    internal bool EndTick(double duration)
    {
        for (int i = 0; i < count; ++i) TickFuel[i] /= duration;
        return Finite();
    }
    internal bool Finite()
    {
        for (int i = 0; i < count; ++i)
            if (!GearReference.Finite(Target[i], Delivered[i], Total[i], TickFuel[i]) || !Numeric.Finite(DeliveredCorrection[i]) || !Numeric.Finite(TotalCorrection[i]) ||
                Target[i] < 0 || Delivered[i] < 0 || Total[i] < 0 || TickFuel[i] < 0) return false;
        return true;
    }
    internal ulong Hash(ulong hash)
    {
        for (int i = 0; i < count; ++i)
        {
            hash = Numeric.Hash(hash, Latched[i] ? 1UL : 0UL); hash = Numeric.Hash(hash, unchecked((ulong)Cycle[i]));
            hash = Numeric.Hash(hash, Target[i]); hash = Numeric.Hash(hash, Delivered[i]); hash = Numeric.Hash(hash, DeliveredCorrection[i]);
            hash = Numeric.Hash(hash, Total[i]); hash = Numeric.Hash(hash, TotalCorrection[i]); hash = Numeric.Hash(hash, TickFuel[i]);
        }
        return hash;
    }
}
