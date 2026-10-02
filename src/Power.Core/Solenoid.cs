// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed record SolenoidDefinition
{
    public Quantity ReferencePosition { get; init; }
    public Quantity InductanceGradient { get; init; }
}
public sealed record TravelStopDefinition
{
    public Quantity MinimumPosition { get; init; }
    public Quantity MaximumPosition { get; init; }
    public Quantity Stiffness { get; init; }
}
public sealed record InjectorNeedleDefinition
{
    public uint NeedleNode { get; init; }
    public Quantity ClosedPosition { get; init; }
    public Quantity FullOpenPosition { get; init; }
}
public sealed record NeedleDriverDefinition
{
    public uint InjectorComponent { get; init; }
    public uint SolenoidComponent { get; init; }
    public ulong SamplePeriodNanoseconds { get; init; }
    public Quantity DriveVoltage { get; init; }
    public ulong ClosurePredictionNanoseconds { get; init; }
}
public readonly record struct SolenoidInterval(double NextFluxWebers, double MeanCurrentAmperes,
    double ForceNewtons, double ForceDerivativeNewtonsPerMeter, double SupplyWorkJoules, double CopperHeatJoules);

/// <summary>Linear position-dependent inductance with a symmetric, energy-conjugate discrete gradient.</summary>
public sealed class LinearGapSolenoid
{
    public double ResistanceOhms { get; }
    public double ReferenceInductanceHenries { get; }
    public double ReferencePositionMeters { get; }
    public double InductanceGradientHenriesPerMeter { get; }
    public LinearGapSolenoid(double resistance, double inductance, double referencePosition, double gradient)
    {
        if (!GearReference.Finite(resistance, inductance, referencePosition, gradient) || resistance < 0 || inductance <= 0 || gradient <= 0)
            throw new ArgumentException("Use nonnegative resistance, positive reference inductance/gradient and finite reference position.");
        ResistanceOhms = resistance; ReferenceInductanceHenries = inductance;
        ReferencePositionMeters = referencePosition; InductanceGradientHenriesPerMeter = gradient;
    }
    public double Inductance(double position) => ReferenceInductanceHenries + InductanceGradientHenriesPerMeter * (position - ReferencePositionMeters);
    public double Energy(double position, double flux) => .5 * flux * (flux / Inductance(position));
    public double Current(double position, double flux) => flux / Inductance(position);
    public bool TryInterval(double oldPosition, double nextPosition, double oldFlux, double voltage, double duration, out SolenoidInterval result)
    {
        result = default;
        double oldL = Inductance(oldPosition), nextL = Inductance(nextPosition);
        if (!GearReference.Finite(oldPosition, nextPosition, oldFlux, voltage) || !Numeric.Finite(duration) || duration <= 0 ||
            !Numeric.Finite(oldL) || !Numeric.Finite(nextL) || oldL <= 0 || nextL <= 0) return false;
        double inverseMean = .25 * (1 / oldL + 1 / nextL), q = duration * ResistanceOhms * inverseMean;
        double nextFlux = ((1 - q) * oldFlux + duration * voltage) / (1 + q);
        double meanCurrent = (oldFlux + nextFlux) * inverseMean;
        double fluxDerivative = (2 * oldFlux + duration * voltage) / (1 + q) / (1 + q)
            * duration * ResistanceOhms * .25 * InductanceGradientHenriesPerMeter / nextL / nextL;
        double force = .25 * InductanceGradientHenriesPerMeter * (oldFlux * (oldFlux / oldL) + nextFlux * (nextFlux / oldL)) / nextL;
        double derivative = .5 * InductanceGradientHenriesPerMeter * nextFlux * (fluxDerivative / oldL) / nextL
            - force * InductanceGradientHenriesPerMeter / nextL;
        double work = duration * voltage * meanCurrent, heat = duration * ResistanceOhms * meanCurrent * meanCurrent;
        if (!GearReference.Finite(nextFlux, meanCurrent, force, derivative) || !Numeric.Finite(work) || !Numeric.Finite(heat)) return false;
        result = new(nextFlux, meanCurrent, force, derivative, work, heat);
        return true;
    }
}

public sealed class ElasticTravelStop
{
    public double MinimumPositionMeters { get; }
    public double MaximumPositionMeters { get; }
    public double StiffnessNewtonsPerMeter { get; }
    public ElasticTravelStop(double minimum, double maximum, double stiffness)
    {
        if (!Numeric.Finite(minimum) || !Numeric.Finite(maximum) || minimum >= maximum || !Numeric.Finite(stiffness) || stiffness <= 0)
            throw new ArgumentException("Use increasing finite stroke limits and positive stop stiffness.");
        MinimumPositionMeters = minimum; MaximumPositionMeters = maximum; StiffnessNewtonsPerMeter = stiffness;
    }
    public double Energy(double position)
    {
        double lower = Math.Max(0, MinimumPositionMeters - position), upper = Math.Max(0, position - MaximumPositionMeters);
        return .5 * StiffnessNewtonsPerMeter * (lower * lower + upper * upper);
    }
    public double Reaction(double old, double next) =>
        -HydraulicPiston.Gradient(old, next, MinimumPositionMeters, -1, StiffnessNewtonsPerMeter)
        -HydraulicPiston.Gradient(old, next, MaximumPositionMeters, 1, StiffnessNewtonsPerMeter);
    public double Derivative(double old, double next) =>
        -HydraulicPiston.GradientDerivative(old, next, MinimumPositionMeters, -1, StiffnessNewtonsPerMeter)
        -HydraulicPiston.GradientDerivative(old, next, MaximumPositionMeters, 1, StiffnessNewtonsPerMeter);
}

internal sealed record CompiledSolenoid(int Component, int Position, double InitialFlux, LinearGapSolenoid Law);
internal sealed record CompiledTravelStop(int Component, int Position, ElasticTravelStop Law);
internal sealed record CompiledNeedle(int Position, double Closed, double Open)
{
    internal double Opening(double[] dynamics) => Math.Max(0, Math.Min(1, (dynamics[Position] - Closed) / (Open - Closed)));
}
public readonly record struct NeedleClosureEstimate(double AdditionalFuelKilograms, uint PhysicalTicks, ulong HorizonNanoseconds);
internal sealed record CompiledNeedleDriver(int Component, int Injector, int Solenoid, ulong Period, double Voltage, uint PredictionTicks = 0);

internal sealed class SolenoidState(int count)
{
    internal readonly double[] Flux = new double[count], Force = new double[count], Heat = new double[count], HeatCorrection = new double[count];
    internal readonly double[] Work = new double[count], WorkCorrection = new double[count];
    internal void CopyFrom(SolenoidState other)
    {
        Array.Copy(other.Flux, Flux, count); Array.Copy(other.Force, Force, count);
        Array.Copy(other.Heat, Heat, count); Array.Copy(other.HeatCorrection, HeatCorrection, count);
        Array.Copy(other.Work, Work, count); Array.Copy(other.WorkCorrection, WorkCorrection, count);
    }
    internal void BeginTick() => Array.Clear(Force, 0, count);
    internal bool EndTick(double duration)
    {
        for (int i = 0; i < count; ++i) Force[i] /= duration;
        return Finite();
    }
    internal bool Finite()
    {
        for (int i = 0; i < count; ++i)
            if (!GearReference.Finite(Flux[i], Force[i], Heat[i], HeatCorrection[i]) || !Numeric.Finite(Work[i]) || !Numeric.Finite(WorkCorrection[i]) || Heat[i] < 0) return false;
        return true;
    }
    internal ulong Hash(ulong hash)
    {
        for (int i = 0; i < count; ++i)
        {
            hash = Numeric.Hash(hash, Flux[i]); hash = Numeric.Hash(hash, Force[i]);
            hash = Numeric.Hash(hash, Heat[i]); hash = Numeric.Hash(hash, HeatCorrection[i]);
            hash = Numeric.Hash(hash, Work[i]); hash = Numeric.Hash(hash, WorkCorrection[i]);
        }
        return hash;
    }
}
internal sealed class NeedleDriverState(int count, bool prediction = false)
{
    internal readonly double[] Target = new double[count], Delivered = new double[count], Voltage = new double[count];
    internal readonly double[] Tail = prediction ? new double[count] : Array.Empty<double>(), Ticks = prediction ? new double[count] : Array.Empty<double>();
    internal readonly bool[] Closed = prediction ? new bool[count] : Array.Empty<bool>();
    internal readonly long[] ClosedCycle = prediction ? new long[count] : Array.Empty<long>();
    internal readonly uint[] PendingTicks = prediction ? new uint[count] : Array.Empty<uint>();
    internal void CopyFrom(NeedleDriverState other)
    {
        Array.Copy(other.Target, Target, count); Array.Copy(other.Delivered, Delivered, count); Array.Copy(other.Voltage, Voltage, count);
        Array.Copy(other.Tail, Tail, Tail.Length); Array.Copy(other.Ticks, Ticks, Ticks.Length); Array.Copy(other.Closed, Closed, Closed.Length); Array.Copy(other.ClosedCycle, ClosedCycle, ClosedCycle.Length);
        Array.Copy(other.PendingTicks, PendingTicks, PendingTicks.Length);
    }
    internal ulong Hash(ulong hash)
    {
        for (int i = 0; i < count; ++i) { hash = Numeric.Hash(hash, Target[i]); hash = Numeric.Hash(hash, Delivered[i]); hash = Numeric.Hash(hash, Voltage[i]); }
        for (int i = 0; i < Tail.Length; ++i)
        { hash = Numeric.Hash(hash, Tail[i]); hash = Numeric.Hash(hash, Ticks[i]); hash = Numeric.Hash(hash, Closed[i] ? 1UL : 0UL); hash = Numeric.Hash(hash, unchecked((ulong)ClosedCycle[i])); hash = Numeric.Hash(hash, PendingTicks[i]); }
        return hash;
    }
}
