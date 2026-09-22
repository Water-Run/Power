// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>A rejected reference interval leaves its output default and its input unchanged.</summary>
public enum GearStepStatus { Ok, InvalidDuration, InvalidState, IncompatibleState, InvalidTorque, NumericalFailure }

public readonly record struct GearPairState(double SpeedARadiansPerSecond, double SpeedBRadiansPerSecond);

/// <summary>Torques are the reactions applied by the ideal gear to the attached rotors.</summary>
public readonly record struct GearPairStep(
    GearPairState State,
    double AngleAdvanceARadians,
    double AngleAdvanceBRadians,
    double TorqueAtANewtonMeters,
    double TorqueAtBNewtonMeters,
    double ExternalWorkJoules,
    double KineticEnergyChangeJoules)
{
    public double EnergyResidualJoules => ExternalWorkJoules - KineticEnergyChangeJoules;
}

/// <summary>
/// An independent, exact constant-load reference for two inertias connected by a massless,
/// lossless gear: omega_A = ratio * omega_B. Positive and negative ratios are supported.
/// There is no gear-mesh compliance, backlash, drag, friction, or engagement event.
/// </summary>
public sealed class IdealGearPair
{
    public double InertiaAKilogramMeterSquared { get; }
    public double InertiaBKilogramMeterSquared { get; }
    public double Ratio { get; }
    public double EquivalentInertiaAtBKilogramMeterSquared { get; }

    public IdealGearPair(double inertiaAKilogramMeterSquared, double inertiaBKilogramMeterSquared, double ratio)
    {
        GearReference.RequireInertia(inertiaAKilogramMeterSquared, nameof(inertiaAKilogramMeterSquared));
        GearReference.RequireInertia(inertiaBKilogramMeterSquared, nameof(inertiaBKilogramMeterSquared));
        if (!Numeric.Finite(ratio) || ratio == 0)
            throw new ArgumentOutOfRangeException(nameof(ratio), "The signed speed ratio must be finite and nonzero.");
        InertiaAKilogramMeterSquared = inertiaAKilogramMeterSquared;
        InertiaBKilogramMeterSquared = inertiaBKilogramMeterSquared;
        Ratio = ratio;
        EquivalentInertiaAtBKilogramMeterSquared = (inertiaAKilogramMeterSquared * ratio) * ratio + inertiaBKilogramMeterSquared;
        if (!Numeric.Finite(EquivalentInertiaAtBKilogramMeterSquared) ||
            !Numeric.Finite(1 / EquivalentInertiaAtBKilogramMeterSquared) ||
            !Numeric.Finite(ratio / EquivalentInertiaAtBKilogramMeterSquared))
            throw new ArgumentException("Inertias and ratio exceed the supported binary64 range.");
    }

    /// <summary>
    /// Advance without mutation or allocation. Initial speeds must already satisfy the gear
    /// relation to binary64 rounding; no synchronization impulse or energy loss is invented.
    /// Dependent speed and angle advance are reconstructed from port B, removing only accepted
    /// rounding residue. Duration is a local interval, not Simulation's integer clock.
    /// </summary>
    public GearStepStatus Advance(GearPairState state, double externalTorqueANewtonMeters,
        double externalTorqueBNewtonMeters, double durationSeconds, out GearPairStep result)
    {
        result = default;
        if (!Numeric.Finite(durationSeconds) || durationSeconds <= 0) return GearStepStatus.InvalidDuration;
        double a = state.SpeedARadiansPerSecond, b = state.SpeedBRadiansPerSecond;
        if (!Numeric.Finite(a) || !Numeric.Finite(b)) return GearStepStatus.InvalidState;
        double rb = Ratio * b;
        if (!Numeric.Finite(rb)) return GearStepStatus.NumericalFailure;
        if (!GearReference.Compatible(a, rb)) return GearStepStatus.IncompatibleState;
        double ta = externalTorqueANewtonMeters, tb = externalTorqueBNewtonMeters;
        if (!Numeric.Finite(ta) || !Numeric.Finite(tb)) return GearStepStatus.InvalidTorque;
        double accelerationB = (Ratio * ta + tb) / EquivalentInertiaAtBKilogramMeterSquared;
        double accelerationA = Ratio * accelerationB;
        // This force expression avoids subtracting nearly equal applied and inertial
        // torques when most of the load accelerates the reflected inertia.
        double reactionA = (InertiaAKilogramMeterSquared * Ratio / EquivalentInertiaAtBKilogramMeterSquared) * tb
            - (InertiaBKilogramMeterSquared / EquivalentInertiaAtBKilogramMeterSquared) * ta;
        double reactionB = -Ratio * reactionA;
        double db = durationSeconds * (b + .5 * durationSeconds * accelerationB);
        double da = Ratio * db;
        double nextB = b + durationSeconds * accelerationB, nextA = Ratio * nextB;
        double energy = GearReference.EnergyChange(InertiaAKilogramMeterSquared, a, nextA)
            + GearReference.EnergyChange(InertiaBKilogramMeterSquared, b, nextB);
        double work = ta * da + tb * db;
        if (!GearReference.Finite(accelerationA, accelerationB, reactionA, reactionB) ||
            !GearReference.Finite(da, db, nextA, nextB) || !GearReference.Finite(energy, work, work - energy, Ratio * nextB) ||
            !GearReference.Compatible(nextA, Ratio * nextB) ||
            !GearReference.ForceBalance(InertiaAKilogramMeterSquared * accelerationA, ta, reactionA) ||
            !GearReference.ForceBalance(InertiaBKilogramMeterSquared * accelerationB, tb, reactionB) ||
            !GearReference.ImpulseBalance(InertiaAKilogramMeterSquared, a, nextA, ta, reactionA, durationSeconds) ||
            !GearReference.ImpulseBalance(InertiaBKilogramMeterSquared, b, nextB, tb, reactionB, durationSeconds)) return GearStepStatus.NumericalFailure;
        result = new(new(nextA, nextB), da, db, reactionA, reactionB, work, energy);
        return GearStepStatus.Ok;
    }
}

public readonly record struct PlanetaryState(
    double SunSpeedRadiansPerSecond, double RingSpeedRadiansPerSecond, double CarrierSpeedRadiansPerSecond);

/// <summary>All member axes use the same positive direction; reactions act on the attached rotors.</summary>
public readonly record struct PlanetaryStep(
    PlanetaryState State,
    double SunAngleAdvanceRadians,
    double RingAngleAdvanceRadians,
    double CarrierAngleAdvanceRadians,
    double SunTorqueNewtonMeters,
    double RingTorqueNewtonMeters,
    double CarrierTorqueNewtonMeters,
    double ExternalWorkJoules,
    double KineticEnergyChangeJoules)
{
    public double EnergyResidualJoules => ExternalWorkJoules - KineticEnergyChangeJoules;
}

/// <summary>
/// Exact constant-load reference for a simple massless planetary gear with three attached
/// inertias. The ring/sun tooth ratio k is greater than one and the Willis relation is
/// omega_S + k * omega_R = (1+k) * omega_C. This is a two-degree-of-freedom device;
/// brakes, locking clutches, shift actuation and planet inertia are not supplied implicitly.
/// </summary>
public sealed class SimplePlanetaryGear
{
    public double SunInertiaKilogramMeterSquared { get; }
    public double RingInertiaKilogramMeterSquared { get; }
    public double CarrierInertiaKilogramMeterSquared { get; }
    public double RingToSunTeethRatio { get; }
    private readonly double _sunShare, _ringShare, _scale, _js, _jr, _jc, _determinant;

    public SimplePlanetaryGear(double sunInertiaKilogramMeterSquared, double ringInertiaKilogramMeterSquared,
        double carrierInertiaKilogramMeterSquared, double ringToSunTeethRatio)
    {
        GearReference.RequireInertia(sunInertiaKilogramMeterSquared, nameof(sunInertiaKilogramMeterSquared));
        GearReference.RequireInertia(ringInertiaKilogramMeterSquared, nameof(ringInertiaKilogramMeterSquared));
        GearReference.RequireInertia(carrierInertiaKilogramMeterSquared, nameof(carrierInertiaKilogramMeterSquared));
        if (!Numeric.Finite(ringToSunTeethRatio) || ringToSunTeethRatio <= 1)
            throw new ArgumentOutOfRangeException(nameof(ringToSunTeethRatio), "A simple planetary requires a finite ring/sun tooth ratio greater than one.");
        SunInertiaKilogramMeterSquared = sunInertiaKilogramMeterSquared;
        RingInertiaKilogramMeterSquared = ringInertiaKilogramMeterSquared;
        CarrierInertiaKilogramMeterSquared = carrierInertiaKilogramMeterSquared;
        RingToSunTeethRatio = ringToSunTeethRatio;
        _sunShare = 1 / (1 + ringToSunTeethRatio);
        _ringShare = ringToSunTeethRatio * _sunShare;
        // Eliminate carrier speed before forming the kinetic-energy mass matrix. This
        // reduced-coordinate reference is independent of a constraint-force projection.
        double m00 = sunInertiaKilogramMeterSquared + carrierInertiaKilogramMeterSquared * _sunShare * _sunShare;
        double m11 = ringInertiaKilogramMeterSquared + carrierInertiaKilogramMeterSquared * _ringShare * _ringShare;
        _scale = Math.Max(m00, m11);
        // Expand the determinant into positive terms instead of cancelling two
        // almost equal products when carrier inertia dominates both other members.
        _js = sunInertiaKilogramMeterSquared / _scale; _jr = ringInertiaKilogramMeterSquared / _scale;
        _jc = carrierInertiaKilogramMeterSquared / _scale;
        _determinant = _js * _jr + _jc * (_js * _ringShare * _ringShare + _jr * _sunShare * _sunShare);
        if (!GearReference.Finite(_scale, 1 / _scale, _sunShare, _ringShare) || _sunShare <= 0 || _ringShare <= 0 ||
            !Numeric.Finite(_determinant) || _determinant <= 64 * GearReference.Epsilon)
            throw new ArgumentException("Planetary inertias and ratio produce a nonfinite or ill-conditioned reduced mass matrix.");
    }

    /// <summary>Carrier speed from the Willis relation, using normalized weights to limit overflow.</summary>
    public double CarrierSpeed(double sunSpeedRadiansPerSecond, double ringSpeedRadiansPerSecond)
    {
        if (!Numeric.Finite(sunSpeedRadiansPerSecond)) throw new ArgumentOutOfRangeException(nameof(sunSpeedRadiansPerSecond));
        if (!Numeric.Finite(ringSpeedRadiansPerSecond)) throw new ArgumentOutOfRangeException(nameof(ringSpeedRadiansPerSecond));
        double speed = Carrier(sunSpeedRadiansPerSecond, ringSpeedRadiansPerSecond);
        if (!Numeric.Finite(speed)) throw new ArgumentException("Carrier speed exceeds the supported binary64 range.");
        return speed;
    }

    private double Carrier(double sun, double ring) => _sunShare * sun + _ringShare * ring;
    private bool Compatible(double sun, double ring, double carrier) =>
        GearReference.Compatible(carrier, Carrier(sun, ring), Math.Abs(_sunShare * sun), Math.Abs(_ringShare * ring));

    /// <summary>
    /// Advance under three constant external torques without allocation or mutation. Speeds
    /// must already satisfy the Willis relation to rounding. Member holding/locking loads
    /// must be supplied explicitly; this method never changes the number of constraints. Carrier
    /// speed and angle advance are reconstructed from sun and ring, removing rounding residue.
    /// </summary>
    public GearStepStatus Advance(PlanetaryState state, double sunExternalTorqueNewtonMeters,
        double ringExternalTorqueNewtonMeters, double carrierExternalTorqueNewtonMeters,
        double durationSeconds, out PlanetaryStep result)
    {
        result = default;
        if (!Numeric.Finite(durationSeconds) || durationSeconds <= 0) return GearStepStatus.InvalidDuration;
        double s = state.SunSpeedRadiansPerSecond, r = state.RingSpeedRadiansPerSecond, c = state.CarrierSpeedRadiansPerSecond;
        if (!GearReference.Finite(s, r, c, 0)) return GearStepStatus.InvalidState;
        if (!Compatible(s, r, c)) return GearStepStatus.IncompatibleState;
        double ts = sunExternalTorqueNewtonMeters, tr = ringExternalTorqueNewtonMeters, tc = carrierExternalTorqueNewtonMeters;
        if (!GearReference.Finite(ts, tr, tc, 0)) return GearStepStatus.InvalidTorque;
        double fs = ts / _scale, fr = tr / _scale, fc = tc / _scale;
        // Expand the inverse reduced system so the carrier terms that cancel
        // symbolically do not become subtraction of large floating-point products.
        double accelerationS = ((_jr + _jc * _ringShare * _ringShare) * fs
            - _jc * _sunShare * _ringShare * fr + _sunShare * _jr * fc) / _determinant;
        double accelerationR = (-_jc * _sunShare * _ringShare * fs
            + (_js + _jc * _sunShare * _sunShare) * fr + _ringShare * _js * fc) / _determinant;
        double accelerationC = (_sunShare * _jr * fs + _ringShare * _js * fr
            + (_sunShare * _sunShare * _jr + _ringShare * _ringShare * _js) * fc) / _determinant;
        double reactionC = (_jc * _sunShare * _jr * ts + _jc * _ringShare * _js * tr - _js * _jr * tc) / _determinant;
        double reactionS = -_sunShare * reactionC, reactionR = -_ringShare * reactionC;
        double ds = durationSeconds * (s + .5 * durationSeconds * accelerationS);
        double dr = durationSeconds * (r + .5 * durationSeconds * accelerationR);
        double dc = Carrier(ds, dr);
        double nextS = s + durationSeconds * accelerationS, nextR = r + durationSeconds * accelerationR;
        double nextC = Carrier(nextS, nextR);
        double energy = GearReference.EnergyChange(SunInertiaKilogramMeterSquared, s, nextS)
            + GearReference.EnergyChange(RingInertiaKilogramMeterSquared, r, nextR)
            + GearReference.EnergyChange(CarrierInertiaKilogramMeterSquared, c, nextC);
        double work = ts * ds + tr * dr + tc * dc;
        if (!GearReference.Finite(accelerationS, accelerationR, accelerationC, reactionC) ||
            !GearReference.Finite(reactionS, reactionR, ds, dr) || !GearReference.Finite(dc, nextS, nextR, nextC) ||
            !GearReference.Finite(energy, work, work - energy, 0) || !Compatible(nextS, nextR, nextC) ||
            !GearReference.ForceBalance(SunInertiaKilogramMeterSquared * accelerationS, ts, reactionS) ||
            !GearReference.ForceBalance(RingInertiaKilogramMeterSquared * accelerationR, tr, reactionR) ||
            !GearReference.ForceBalance(CarrierInertiaKilogramMeterSquared * accelerationC, tc, reactionC) ||
            !GearReference.ImpulseBalance(SunInertiaKilogramMeterSquared, s, nextS, ts, reactionS, durationSeconds) ||
            !GearReference.ImpulseBalance(RingInertiaKilogramMeterSquared, r, nextR, tr, reactionR, durationSeconds) ||
            !GearReference.ImpulseBalance(CarrierInertiaKilogramMeterSquared, c, nextC, tc, reactionC, durationSeconds))
            return GearStepStatus.NumericalFailure;
        result = new(new(nextS, nextR, nextC), ds, dr, dc, reactionS, reactionR, reactionC, work, energy);
        return GearStepStatus.Ok;
    }
}

internal static class GearReference
{
    internal const double Epsilon = 2.2204460492503131e-16;
    internal static void RequireInertia(double value, string name)
    {
        if (!Numeric.Finite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(name, "Attached rotor inertia must be positive and finite.");
    }
    internal static bool Finite(double a, double b, double c, double d) =>
        Numeric.Finite(a) && Numeric.Finite(b) && Numeric.Finite(c) && Numeric.Finite(d);
    // Scale before adding so the tolerance itself cannot overflow for finite speeds.
    // This is a rounding check, without an absolute low-speed deadband or state projection.
    internal static bool Compatible(double a, double b, double termA = 0, double termB = 0) =>
        Numeric.Finite(b) && Math.Abs(a - b) <= 64 * Epsilon * Math.Abs(a) + 64 * Epsilon * Math.Abs(b)
            + 64 * Epsilon * termA + 64 * Epsilon * termB;
    internal static bool ForceBalance(double inertial, double applied, double reaction) =>
        Numeric.Finite(inertial) && Math.Abs(inertial - applied - reaction) <= 512 * Epsilon * Math.Abs(inertial)
            + 512 * Epsilon * Math.Abs(applied) + 512 * Epsilon * Math.Abs(reaction);
    internal static bool ImpulseBalance(double inertia, double initial, double final, double applied, double reaction, double duration)
    {
        double oldMomentum = inertia * initial, newMomentum = inertia * final;
        double sourceImpulse = duration * applied, reactionImpulse = duration * reaction;
        if (!Finite(oldMomentum, newMomentum, sourceImpulse, reactionImpulse)) return false;
        return Math.Abs(inertia * (final - initial) - sourceImpulse - reactionImpulse) <=
            512 * Epsilon * Math.Abs(oldMomentum) + 512 * Epsilon * Math.Abs(newMomentum)
            + 512 * Epsilon * Math.Abs(sourceImpulse) + 512 * Epsilon * Math.Abs(reactionImpulse);
    }
    internal static double EnergyChange(double inertia, double initial, double final) =>
        (inertia * (final - initial)) * (.5 * final + .5 * initial);
}
