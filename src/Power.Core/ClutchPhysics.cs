// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public enum ClutchMode { Disengaged, Locked, SlippingPositive, SlippingNegative }

/// <summary>Runtime rejection leaves the output default and never changes the supplied state.</summary>
public enum ClutchStepStatus { Ok, InvalidDuration, InvalidEngagement, InvalidState, InvalidTorque, NumericalFailure }

/// <summary>Instantaneous reaction at port A. Positive slip means omega_A - ratio * omega_B > 0.</summary>
public readonly record struct ClutchReaction(ClutchMode Mode, double TorqueAtANewtonMeters, double HeatFlowWatts);

/// <summary>
/// Immutable Coulomb friction law. Capacity parameters are torques at port A, not material
/// coefficients. No temperature, pressure actuator, wear or speed dependence is inferred.
/// </summary>
public sealed class DryClutch
{
    public double StaticCapacityNewtonMeters { get; }
    public double SlidingCapacityNewtonMeters { get; }

    public DryClutch(double staticCapacityNewtonMeters, double slidingCapacityNewtonMeters)
    {
        if (!Numeric.Finite(staticCapacityNewtonMeters) || staticCapacityNewtonMeters < 0)
            throw new ArgumentOutOfRangeException(nameof(staticCapacityNewtonMeters), "Static torque capacity must be finite and nonnegative.");
        if (!Numeric.Finite(slidingCapacityNewtonMeters) || slidingCapacityNewtonMeters < 0 || slidingCapacityNewtonMeters > staticCapacityNewtonMeters)
            throw new ArgumentOutOfRangeException(nameof(slidingCapacityNewtonMeters), "Sliding torque capacity must be finite, nonnegative and no greater than static capacity.");
        StaticCapacityNewtonMeters = staticCapacityNewtonMeters;
        SlidingCapacityNewtonMeters = slidingCapacityNewtonMeters;
    }

    /// <summary>
    /// Evaluate friction with engagement in [0,1]. At exactly zero slip the caller supplies
    /// the reaction needed to maintain zero relative acceleration. A nonzero slip, however
    /// small, uses kinetic friction; event detection belongs to the integrating solver.
    /// </summary>
    public ClutchReaction Evaluate(double slipRadiansPerSecond, double lockingTorqueAtANewtonMeters, double engagement)
    {
        if (!Numeric.Finite(slipRadiansPerSecond)) throw new ArgumentOutOfRangeException(nameof(slipRadiansPerSecond));
        if (!Numeric.Finite(lockingTorqueAtANewtonMeters)) throw new ArgumentOutOfRangeException(nameof(lockingTorqueAtANewtonMeters));
        if (!ValidEngagement(engagement)) throw new ArgumentOutOfRangeException(nameof(engagement), "Engagement must be a fraction in [0,1].");
        var reaction = Reaction(slipRadiansPerSecond, lockingTorqueAtANewtonMeters, engagement);
        if (!Numeric.Finite(reaction.HeatFlowWatts)) throw new ArgumentException("Friction heat flow exceeds the supported binary64 range.");
        return reaction;
    }

    internal static bool ValidEngagement(double engagement) => Numeric.Finite(engagement) && engagement >= 0 && engagement <= 1;

    internal ClutchReaction Reaction(double slip, double required, double engagement)
    {
        double limit = engagement * StaticCapacityNewtonMeters;
        if (limit == 0) return new(ClutchMode.Disengaged, 0, 0);
        if (slip == 0 && Math.Abs(required) <= limit) return new(ClutchMode.Locked, required, 0);
        bool positive = slip > 0 || (slip == 0 && required < 0);
        double torque = (positive ? -1 : 1) * engagement * SlidingCapacityNewtonMeters;
        return new(positive ? ClutchMode.SlippingPositive : ClutchMode.SlippingNegative, torque, -torque * slip);
    }
}

public readonly record struct ClutchPairState(double SpeedARadiansPerSecond, double SpeedBRadiansPerSecond);

/// <summary>Exact constant-load interval, up to binary64 rounding. Heat is generated, not yet routed to a thermal node.</summary>
public readonly record struct ClutchPairStep(
    ClutchPairState State,
    ClutchReaction EndReaction,
    double AngleAdvanceARadians,
    double AngleAdvanceBRadians,
    double ImpulseAtANewtonMeterSeconds,
    double FrictionHeatJoules,
    double ExternalWorkJoules,
    double KineticEnergyChangeJoules,
    double SlippingDurationSeconds,
    double? ZeroSlipTimeSeconds)
{
    public double EnergyResidualJoules => ExternalWorkJoules - FrictionHeatJoules - KineticEnergyChangeJoules;
}

/// <summary>
/// Independent two-inertia reference solution under constant external torques and engagement.
/// It resolves a slip-zero event inside the interval, then sticks or reverses. This primitive
/// does not integrate a CompiledModel, a shaft, a motor, gas or other clutches.
/// </summary>
public sealed class ClutchPair
{
    public double InertiaAKilogramMeterSquared { get; }
    /// <summary>Zero only for an explicitly constructed ground brake.</summary>
    public double InertiaBKilogramMeterSquared { get; }
    public double Ratio { get; }
    public bool Grounded { get; }
    public DryClutch Friction { get; }
    private readonly double _inverseA, _inverseB, _mobility, _shareA, _couplingB;

    public ClutchPair(double inertiaAKilogramMeterSquared, double inertiaBKilogramMeterSquared, DryClutch friction, double ratio = 1)
        : this(inertiaAKilogramMeterSquared, inertiaBKilogramMeterSquared, friction, ratio, false) { }

    /// <summary>A finite rotor braking against fixed ground, with ratio one.</summary>
    public static ClutchPair Brake(double inertiaKilogramMeterSquared, DryClutch friction) => new(inertiaKilogramMeterSquared, 0, friction, 1, true);

    private ClutchPair(double inertiaA, double inertiaB, DryClutch friction, double ratio, bool grounded)
    {
        if (!Numeric.Finite(inertiaA) || inertiaA <= 0) throw new ArgumentOutOfRangeException(nameof(inertiaA), "Inertia A must be positive and finite.");
        if (!grounded && (!Numeric.Finite(inertiaB) || inertiaB <= 0)) throw new ArgumentOutOfRangeException(nameof(inertiaB), "Inertia B must be positive and finite; use Brake for ground.");
        if (!Numeric.Finite(ratio) || ratio == 0) throw new ArgumentOutOfRangeException(nameof(ratio), "The signed speed ratio must be finite and nonzero.");
        Friction = friction ?? throw new ArgumentNullException(nameof(friction));
        InertiaAKilogramMeterSquared = inertiaA; InertiaBKilogramMeterSquared = inertiaB; Ratio = ratio; Grounded = grounded;
        _inverseA = 1 / inertiaA; _inverseB = grounded ? 0 : 1 / inertiaB;
        _mobility = _inverseA + ratio * (ratio * _inverseB);
        _shareA = _inverseA / _mobility;
        _couplingB = (ratio * _inverseB) / _mobility;
        if (!Numeric.Finite(_inverseA) || !Numeric.Finite(_inverseB) || !Numeric.Finite(_mobility) || _mobility <= 0 || _shareA <= 0)
            throw new ArgumentException("Inertias and ratio exceed the supported binary64 range.");
    }

    /// <summary>
    /// Advance under constant loads without mutation or allocation. Ground requires both speed B
    /// and external torque B to be zero. Duration is positive and finite; it is a local interval,
    /// not the bounded integer clock of Simulation. On rejection result is default.
    /// </summary>
    public ClutchStepStatus Advance(ClutchPairState state, double externalTorqueANewtonMeters,
        double externalTorqueBNewtonMeters, double engagement, double durationSeconds, out ClutchPairStep result)
    {
        result = default;
        if (!Numeric.Finite(durationSeconds) || durationSeconds <= 0) return ClutchStepStatus.InvalidDuration;
        if (!DryClutch.ValidEngagement(engagement)) return ClutchStepStatus.InvalidEngagement;
        if (!Numeric.Finite(state.SpeedARadiansPerSecond) || !Numeric.Finite(state.SpeedBRadiansPerSecond) ||
            (Grounded && state.SpeedBRadiansPerSecond != 0)) return ClutchStepStatus.InvalidState;
        if (!Numeric.Finite(externalTorqueANewtonMeters) || !Numeric.Finite(externalTorqueBNewtonMeters) ||
            (Grounded && externalTorqueBNewtonMeters != 0)) return ClutchStepStatus.InvalidTorque;

        double a = state.SpeedARadiansPerSecond, b = state.SpeedBRadiansPerSecond;
        double slip = a - Ratio * b;
        double freeA = externalTorqueANewtonMeters * _inverseA, freeB = externalTorqueBNewtonMeters * _inverseB;
        double freeSlipRate = freeA - Ratio * freeB, required = -freeSlipRate / _mobility;
        if (!Numeric.Finite(slip) || !Numeric.Finite(freeA) || !Numeric.Finite(freeB) || !Numeric.Finite(required))
            return ClutchStepStatus.NumericalFailure;

        var reaction = Friction.Reaction(slip, required, engagement);
        double angleA = 0, angleB = 0, impulse = 0, heat = 0, sliding = 0;
        double? zero = reaction.Mode != ClutchMode.Disengaged && slip == 0 ? 0 : null;
        double remaining = durationSeconds;
        // Constant forcing permits at most one arrival at zero: thereafter the pair either
        // remains locked or accelerates away from zero in the opposite direction.
        for (int phase = 0; phase < 2; ++phase)
        {
            bool locked = reaction.Mode == ClutchMode.Locked;
            bool slipping = reaction.Mode is ClutchMode.SlippingPositive or ClutchMode.SlippingNegative;
            double torque = reaction.TorqueAtANewtonMeters;
            double accelerationA = freeA + torque * _inverseA;
            double accelerationB = freeB - Ratio * torque * _inverseB;
            if (locked)
            {
                // Preserve the speed constraint explicitly; do not subtract two nearly equal
                // large accelerations on each rotor when the static reaction cancels the load.
                accelerationB = Grounded ? 0 : _shareA * freeB + _couplingB * freeA;
                accelerationA = Ratio * accelerationB;
            }
            double slipRate = freeSlipRate + _mobility * torque;
            if (!Numeric.Finite(accelerationA) || !Numeric.Finite(accelerationB) || !Numeric.Finite(slipRate))
                return ClutchStepStatus.NumericalFailure;
            double h = remaining;
            bool arrives = false;
            if (slipping && ((slip > 0 && slipRate < 0) || (slip < 0 && slipRate > 0)))
            {
                double crossing = -slip / slipRate;
                // Overflow means the event is beyond every finite interval. An underflowed
                // event cannot be resolved and must not silently discard relative energy.
                if (crossing == 0) return ClutchStepStatus.NumericalFailure;
                if (crossing <= remaining) { h = crossing; arrives = true; }
            }
            double nextA = a + h * accelerationA, nextB = b + h * accelerationB;
            double displacementA = h * (a + .5 * h * accelerationA);
            double displacementB = h * (b + .5 * h * accelerationB);
            double endSlip = arrives || locked ? 0 : slip + h * slipRate;
            // Integrate the nonnegative sliding power over the linear slip trajectory.
            if (slipping)
            {
                double loss = (-torque * (slip / 2 + endSlip / 2)) * h;
                if (!Numeric.Finite(loss) || loss < 0) return ClutchStepStatus.NumericalFailure;
                heat += loss;
                // A continued or reversed sliding phase covers the entire interval. Preserve
                // that exact bound instead of adding two durations that can round one ulp high.
                sliding = arrives ? h : durationSeconds;
            }
            angleA += displacementA; angleB += displacementB; impulse += h * torque;
            a = nextA; b = nextB;
            if (arrives || locked)
            {
                // Momentum-preserving projection removes only event-rounding residue.
                // Using a bounded barycentric weight avoids an inertia-weighted sum overflow.
                if (arrives) b = Grounded ? 0 : _shareA * b + _couplingB * a;
                a = Ratio * b;
            }
            if (!arrives) break;
            zero = durationSeconds - remaining + h;
            remaining -= h;
            slip = 0;
            reaction = Friction.Reaction(0, required, engagement);
            if (remaining == 0) break;
        }

        // Use the kinetic-energy difference identity to avoid subtracting large, nearly
        // equal squared speeds. Tests also compare independent absolute energies.
        double energyChange = .5 * InertiaAKilogramMeterSquared * (a - state.SpeedARadiansPerSecond) * (a + state.SpeedARadiansPerSecond)
            + .5 * InertiaBKilogramMeterSquared * (b - state.SpeedBRadiansPerSecond) * (b + state.SpeedBRadiansPerSecond);
        double work = externalTorqueANewtonMeters * angleA + externalTorqueBNewtonMeters * angleB;
        double finalSlip = a - Ratio * b;
        var endReaction = Friction.Reaction(finalSlip, required, engagement);
        if (!Numeric.Finite(a) || !Numeric.Finite(b) || !Numeric.Finite(finalSlip) ||
            !Numeric.Finite(angleA) || !Numeric.Finite(angleB) || !Numeric.Finite(impulse) || !Numeric.Finite(heat) ||
            !Numeric.Finite(energyChange) || !Numeric.Finite(work) || !Numeric.Finite(work - heat - energyChange) ||
            !Numeric.Finite(endReaction.HeatFlowWatts)) return ClutchStepStatus.NumericalFailure;
        result = new(new(a, b), endReaction, angleA, angleB, impulse, heat, work, energyChange, sliding, zero);
        return ClutchStepStatus.Ok;
    }
}
