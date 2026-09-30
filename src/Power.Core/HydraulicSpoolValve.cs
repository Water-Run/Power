// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed record HydraulicSpoolValveDefinition
{
    public uint PistonComponent { get; init; }
    public Quantity ClosedPosition { get; init; }
    public Quantity FullOpenPosition { get; init; }
}

public readonly record struct HydraulicSpoolFlow(double Opening, double VolumeFlowCubicMetersPerSecond,
    double HeatFlowWatts, double PressureDerivativeCubicMetersPerSecondPascal, double PositionDerivativeSquareMetersPerSecond);

/// <summary>Pressure-balanced metering land with explicit travel and a passive regularized turbulent port.</summary>
public sealed class HydraulicSpoolValve
{
    public double ClosedPositionMeters { get; }
    public double FullOpenPositionMeters { get; }
    public HydraulicRestriction Restriction { get; }
    private readonly double _inverseTravel;
    public HydraulicSpoolValve(double coefficientCubicMetersPerSecondSqrtPascal, double transitionPressurePascals,
        double closedPositionMeters, double fullOpenPositionMeters)
    {
        if (!GearReference.Finite(closedPositionMeters, fullOpenPositionMeters, transitionPressurePascals, coefficientCubicMetersPerSecondSqrtPascal) ||
            transitionPressurePascals <= 0 || coefficientCubicMetersPerSecondSqrtPascal < 0)
            throw new ArgumentException("Use finite land positions, nonnegative coefficient and positive transition pressure.");
        double travel = fullOpenPositionMeters - closedPositionMeters;
        _inverseTravel = 1 / travel;
        if (!Numeric.Finite(travel) || travel == 0 || !Numeric.Finite(_inverseTravel))
            throw new ArgumentException("Closed and full-open positions need finite nonzero travel.", nameof(fullOpenPositionMeters));
        ClosedPositionMeters = closedPositionMeters; FullOpenPositionMeters = fullOpenPositionMeters;
        Restriction = new(coefficientCubicMetersPerSecondSqrtPascal, transitionPressurePascals);
    }
    public double Opening(double positionMeters) => !Numeric.Finite(positionMeters) ? double.NaN
        : Math.Max(0, Math.Min(1, (positionMeters - ClosedPositionMeters) * _inverseTravel));
    private double OpeningDerivative(double position)
    {
        double fraction = (position - ClosedPositionMeters) * _inverseTravel;
        return fraction > 0 && fraction < 1 ? _inverseTravel : fraction == 0 || fraction == 1 ? .5 * _inverseTravel : 0;
    }
    internal bool Evaluate(double difference, double position, out double flow, out double pressureDerivative, out double positionDerivative)
    {
        flow = pressureDerivative = positionDerivative = 0;
        if (!Numeric.Finite(difference) || !Numeric.Finite(position)) return false;
        double opening = Opening(position), slope = OpeningDerivative(position);
        if (opening == 0 && slope == 0) return true;
        if (!Restriction.Evaluate(difference, 1, out double fullFlow, out double fullDerivative)) return false;
        flow = opening * fullFlow; pressureDerivative = opening * fullDerivative; positionDerivative = slope * fullFlow;
        return Numeric.Finite(flow) && Numeric.Finite(pressureDerivative) && Numeric.Finite(positionDerivative);
    }
    public bool TryEvaluate(double pressureAPascals, double pressureBPascals, double positionMeters, out HydraulicSpoolFlow flow)
    {
        flow = default;
        if (!Numeric.Finite(pressureAPascals) || !Numeric.Finite(pressureBPascals) || pressureAPascals < 0 || pressureBPascals < 0 ||
            !Evaluate(pressureAPascals - pressureBPascals, positionMeters, out double rate, out double pressureSlope, out double positionSlope)) return false;
        double heat = rate * (pressureAPascals - pressureBPascals);
        if (!Numeric.Finite(heat) || heat < 0) return false;
        flow = new(Opening(positionMeters), rate, heat, pressureSlope, positionSlope); return true;
    }
}

internal sealed record SpoolMetering(int Piston, int Coordinate, int Position, HydraulicSpoolValve Law);
