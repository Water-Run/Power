// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>Gauge-pressure flow law. Linear conductance or regularized turbulent coefficient, with explicit units.</summary>
public sealed record HydraulicRestrictionDefinition
{
    public Quantity Coefficient { get; init; }
    public Quantity TransitionPressure { get; init; }
    public Quantity CrackingPressure { get; init; }
}

/// <summary>Rigid-contact reduction: the hydraulic node owns the effective fluid/actuator compliance.</summary>
public sealed record HydraulicClutchDefinition
{
    public uint PressureNode { get; init; }
    public Quantity PistonArea { get; init; }
    public Quantity PreloadForce { get; init; }
    public Quantity EffectiveRadius { get; init; }
    public double StaticFriction { get; init; }
    public double SlidingFriction { get; init; }
    public uint FrictionSurfaces { get; init; }
}

/// <summary>Ideal positive displacement with an explicit inlet or pressure reservoir.</summary>
public sealed record HydraulicPumpDefinition
{
    public uint InletNode { get; init; }
    public Quantity Displacement { get; init; }
}

public readonly record struct HydraulicPumpReaction(double VolumeFlowCubicMetersPerSecond, double ShaftTorqueNewtonMeters, double HydraulicPowerWatts);

/// <summary>Lossless reversible shaft/fluid power transformer. Positive flow runs from inlet to outlet.</summary>
public sealed class HydraulicPump
{
    public double DisplacementCubicMetersPerRadian { get; }
    public HydraulicPump(double displacementCubicMetersPerRadian)
    {
        if (!Numeric.Finite(displacementCubicMetersPerRadian) || displacementCubicMetersPerRadian <= 0)
            throw new ArgumentException("Displacement must be finite and positive.", nameof(displacementCubicMetersPerRadian));
        DisplacementCubicMetersPerRadian = displacementCubicMetersPerRadian;
    }
    public bool TryEvaluate(double speedRadiansPerSecond, double inletPressurePascals, double outletPressurePascals, out HydraulicPumpReaction reaction)
    {
        reaction = default;
        if (!GearReference.Finite(speedRadiansPerSecond, inletPressurePascals, outletPressurePascals, 0) || inletPressurePascals < 0 || outletPressurePascals < 0) return false;
        double flow = DisplacementCubicMetersPerRadian * speedRadiansPerSecond;
        double torque = -DisplacementCubicMetersPerRadian * (outletPressurePascals - inletPressurePascals), power = -torque * speedRadiansPerSecond;
        if (!GearReference.Finite(flow, torque, power, 0)) return false;
        reaction = new(flow, torque, power); return true;
    }
}

public readonly record struct HydraulicFlow(double VolumeFlowCubicMetersPerSecond, double HeatFlowWatts);

/// <summary>Passive restriction, optionally a one-way linear pressure relief; no hidden leakage or fluid property is inferred.</summary>
public sealed class HydraulicRestriction
{
    public double Coefficient { get; }
    public double TransitionPressurePascals { get; }
    public bool IsTurbulent { get; }
    public double? CrackingPressurePascals { get; }
    public HydraulicRestriction(double coefficient, double transitionPressurePascals = 0, double? crackingPressurePascals = null)
    {
        if (!Numeric.Finite(coefficient) || coefficient < 0) throw new ArgumentException("Flow coefficient must be finite and nonnegative.", nameof(coefficient));
        if (!Numeric.Finite(transitionPressurePascals) || transitionPressurePascals < 0) throw new ArgumentException("Transition pressure must be finite and nonnegative; zero selects linear flow.", nameof(transitionPressurePascals));
        if (crackingPressurePascals is double cracking && (!Numeric.Finite(cracking) || cracking < 0 || transitionPressurePascals != 0))
            throw new ArgumentException("Relief requires nonnegative cracking pressure and linear conductance.", nameof(crackingPressurePascals));
        CrackingPressurePascals = crackingPressurePascals;
        Coefficient = coefficient; TransitionPressurePascals = transitionPressurePascals; IsTurbulent = transitionPressurePascals > 0;
    }
    public bool TryEvaluate(double pressureAPascals, double pressureBPascals, double opening, out HydraulicFlow flow)
    {
        flow = default;
        if (!Numeric.Finite(pressureAPascals) || !Numeric.Finite(pressureBPascals) || pressureAPascals < 0 || pressureBPascals < 0 ||
            !Numeric.Finite(opening) || opening < 0 || opening > 1) return false;
        if (!Evaluate(pressureAPascals - pressureBPascals, opening, out double rate, out _)) return false;
        double heat = (pressureAPascals - pressureBPascals) * rate;
        if (!Numeric.Finite(heat)) return false;
        flow = new(rate, heat); return true;
    }
    internal bool Evaluate(double difference, double opening, out double rate, out double derivative)
    {
        double scale = Coefficient * opening;
        if (scale == 0) { rate = derivative = 0; return true; }
        if (CrackingPressurePascals is double cracking)
        {
            double excess = difference - cracking;
            rate = scale * Math.Max(0, excess); derivative = excess > 0 ? scale : 0;
        }
        else if (!IsTurbulent) { rate = scale * difference; derivative = scale; }
        else
        {
            // Scaled hypot avoids squaring large pressure differences.
            double maximum = Math.Max(Math.Abs(difference), TransitionPressurePascals);
            double a = difference / maximum, b = TransitionPressurePascals / maximum;
            double norm = Math.Sqrt(a*a + b*b), root = Math.Sqrt(maximum) * Math.Sqrt(norm);
            double conductance = scale / root, relative = a / norm;
            rate = conductance * difference;
            derivative = conductance * (1 - .5 * relative * relative);
        }
        return Numeric.Finite(rate) && Numeric.Finite(derivative);
    }
}

internal sealed class HydraulicActuator
{
    internal readonly int Pressure;
    internal readonly double Area, Preload, StaticFactor, SlidingFactor;
    internal HydraulicActuator(HydraulicClutchDefinition d, int pressure, uint id)
    {
        Pressure = pressure;
        Area = CompiledModel.Convert(d.PistonArea, Unit.SquareMeter, id, "hydraulic_clutch.piston_area");
        Preload = CompiledModel.Convert(d.PreloadForce, Unit.Newton, id, "hydraulic_clutch.preload_force");
        double radius = CompiledModel.Convert(d.EffectiveRadius, Unit.Meter, id, "hydraulic_clutch.effective_radius");
        if (!(Area > 0) || Preload < 0 || !(radius > 0) || !Numeric.Finite(d.StaticFriction) || !Numeric.Finite(d.SlidingFriction) ||
            d.SlidingFriction < 0 || d.StaticFriction < d.SlidingFriction || d.FrictionSurfaces is < 1 or > 128)
            throw new ModelCompileException(DiagnosticCode.Range, id, "hydraulic_clutch", "Use positive piston area/radius, nonnegative preload, static >= sliding >= 0 coefficients and 1..128 friction surfaces.");
        StaticFactor = radius * d.FrictionSurfaces * d.StaticFriction;
        SlidingFactor = radius * d.FrictionSurfaces * d.SlidingFriction;
        if (!Numeric.Finite(StaticFactor) || !Numeric.Finite(SlidingFactor))
            throw new ModelCompileException(DiagnosticCode.Range, id, "hydraulic_clutch", "Clutch force-to-torque conversion exceeds binary64.");
    }
    internal double ClampForce(double pressure) => Math.Max(0, Area * pressure - Preload);
    internal double Capacity(double pressure, bool sliding) => ClampForce(pressure) * (sliding ? SlidingFactor : StaticFactor);
    internal ulong Hash(ulong h)
    {
        h = Numeric.Hash(h, Area); h = Numeric.Hash(h, Preload); h = Numeric.Hash(h, StaticFactor);
        return Numeric.Hash(h, SlidingFactor);
    }
}
