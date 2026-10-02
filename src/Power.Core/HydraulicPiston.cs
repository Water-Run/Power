// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed record HydraulicPistonDefinition
{
    public Quantity FrontArea { get; init; }
    public Quantity BackArea { get; init; }
    public uint BackNode { get; init; }
    public Quantity BackPressure { get; init; }
    public Quantity MinimumPosition { get; init; }
    public Quantity MaximumPosition { get; init; }
    public Quantity StopStiffness { get; init; }
    public Quantity ContactPosition { get; init; }
    public Quantity ContactStiffness { get; init; }
}

public sealed record PistonClutchDefinition
{
    public uint PistonComponent { get; init; }
    public Quantity EffectiveRadius { get; init; }
    public double StaticFriction { get; init; }
    public double SlidingFriction { get; init; }
    public uint FrictionSurfaces { get; init; }
}

/// <summary>Pressure-volume transformer and elastic pad/end stops. Stop penetration stores explicit energy.</summary>
public sealed class HydraulicPiston
{
    public double FrontAreaSquareMeters { get; }
    public double BackAreaSquareMeters { get; }
    public double MinimumPositionMeters { get; }
    public double MaximumPositionMeters { get; }
    public double StopStiffnessNewtonsPerMeter { get; }
    public double ContactPositionMeters { get; }
    public double ContactStiffnessNewtonsPerMeter { get; }
    public HydraulicPiston(double frontAreaSquareMeters, double backAreaSquareMeters, double minimumPositionMeters,
        double maximumPositionMeters, double stopStiffnessNewtonsPerMeter, double contactPositionMeters, double contactStiffnessNewtonsPerMeter)
    {
        if (!Numeric.Finite(frontAreaSquareMeters) || frontAreaSquareMeters <= 0 || !Numeric.Finite(backAreaSquareMeters) || backAreaSquareMeters < 0)
            throw new ArgumentException("Use a positive front area and nonnegative back area.", nameof(frontAreaSquareMeters));
        if (!Numeric.Finite(minimumPositionMeters) || !Numeric.Finite(maximumPositionMeters) || minimumPositionMeters >= maximumPositionMeters)
            throw new ArgumentException("Finite stroke limits must be strictly increasing.", nameof(maximumPositionMeters));
        if (!Numeric.Finite(stopStiffnessNewtonsPerMeter) || stopStiffnessNewtonsPerMeter < 0 || !Numeric.Finite(contactStiffnessNewtonsPerMeter) || contactStiffnessNewtonsPerMeter < 0)
            throw new ArgumentException("Contact/stop stiffness must be finite and nonnegative.", nameof(contactStiffnessNewtonsPerMeter));
        if (!Numeric.Finite(contactPositionMeters) || contactPositionMeters < minimumPositionMeters || contactPositionMeters > maximumPositionMeters)
            throw new ArgumentException("Contact position must lie within the nominal stroke.", nameof(contactPositionMeters));
        FrontAreaSquareMeters = frontAreaSquareMeters; BackAreaSquareMeters = backAreaSquareMeters;
        MinimumPositionMeters = minimumPositionMeters; MaximumPositionMeters = maximumPositionMeters;
        StopStiffnessNewtonsPerMeter = stopStiffnessNewtonsPerMeter; ContactPositionMeters = contactPositionMeters;
        ContactStiffnessNewtonsPerMeter = contactStiffnessNewtonsPerMeter;
    }
    private static double Hinge(double x, double limit, double direction) => Math.Max(0, direction * (x - limit));
    internal static double Gradient(double old, double next, double limit, double direction, double stiffness)
    {
        double a = Hinge(old, limit, direction), b = Hinge(next, limit, direction), delta = next - old;
        if (a > 0 && b > 0) return .5 * stiffness * direction * (a + b);
        if (a == 0 && b == 0) return 0;
        return delta == 0 ? stiffness * direction * a : .5 * stiffness * (a + b) * ((b - a) / delta);
    }
    internal static double GradientDerivative(double old, double next, double limit, double direction, double stiffness)
    {
        double a = Hinge(old, limit, direction), b = Hinge(next, limit, direction), delta = next - old;
        if (a > 0 && b > 0) return .5 * stiffness;
        if (a == 0 && b == 0)
            return delta == 0 && old == limit ? .25 * stiffness : 0;
        if (a == 0)
        {
            double ratio = b / delta;
            return stiffness * (direction * ratio - .5 * ratio * ratio);
        }
        double released = a / delta;
        return .5 * stiffness * released * released;
    }
    public double StoredContactEnergy(double positionMeters)
    {
        double lower = Hinge(positionMeters, MinimumPositionMeters, -1), upper = Hinge(positionMeters, MaximumPositionMeters, 1), pad = Hinge(positionMeters, ContactPositionMeters, 1);
        return .5 * StopStiffnessNewtonsPerMeter * (lower * lower + upper * upper) + .5 * ContactStiffnessNewtonsPerMeter * pad * pad;
    }
    public double ContactForce(double positionMeters) => ContactStiffnessNewtonsPerMeter * Hinge(positionMeters, ContactPositionMeters, 1);
    public double DiscreteContactForce(double oldPositionMeters, double nextPositionMeters) =>
        Gradient(oldPositionMeters, nextPositionMeters, ContactPositionMeters, 1, ContactStiffnessNewtonsPerMeter);
    public double DiscreteElasticReaction(double oldPositionMeters, double nextPositionMeters) =>
        -DiscreteContactForce(oldPositionMeters, nextPositionMeters)
        - Gradient(oldPositionMeters, nextPositionMeters, MinimumPositionMeters, -1, StopStiffnessNewtonsPerMeter)
        - Gradient(oldPositionMeters, nextPositionMeters, MaximumPositionMeters, 1, StopStiffnessNewtonsPerMeter);
    /// <summary>Derivative with respect to the next position; at a stationary hinge use the mean one-sided slope.</summary>
    public double DiscreteElasticDerivative(double oldPositionMeters, double nextPositionMeters) =>
        -GradientDerivative(oldPositionMeters, nextPositionMeters, ContactPositionMeters, 1, ContactStiffnessNewtonsPerMeter)
        - GradientDerivative(oldPositionMeters, nextPositionMeters, MinimumPositionMeters, -1, StopStiffnessNewtonsPerMeter)
        - GradientDerivative(oldPositionMeters, nextPositionMeters, MaximumPositionMeters, 1, StopStiffnessNewtonsPerMeter);
    public double PressureForce(double frontPressurePascals, double backPressurePascals) =>
        FrontAreaSquareMeters * frontPressurePascals - BackAreaSquareMeters * backPressurePascals;
}

internal sealed record PistonFriction(int Piston, double Radius, double StaticFriction, double SlidingFriction, uint Surfaces)
{
    internal double Capacity(double normalForce, bool sliding) => (sliding ? SlidingFriction : StaticFriction) * Surfaces * Radius * normalForce;
}
