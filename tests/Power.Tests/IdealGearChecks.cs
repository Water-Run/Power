// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class IdealGearChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("ideal gear pair / signed ratio / reflected inertia / reaction power", Pair),
        ("planetary free dynamics / independent constraint-force solution", FreePlanetary),
        ("planetary held members / reduction / reverse / direct drive", TransmissionStates),
        ("ideal gears constant-load partition invariance / speed zero crossing", Partitions),
        ("ideal gear changing loads / independent integrals / convergence", Convergence),
        ("ideal gear contracts / incompatible speed rejection / finite bounds", Contracts),
        ("ideal gear deterministic range sweep / work and angular momentum", Sweep),
        ("ideal gear references allocation-free / independent shared use", AllocationAndIndependence)
    ];

    private static GearPairStep Run(IdealGearPair gear, GearPairState state, double ta, double tb, double h)
    {
        var status = gear.Advance(state, ta, tb, h, out var step);
        Require(status == GearStepStatus.Ok, $"gear reference rejected: {status}");
        return step;
    }

    private static PlanetaryStep Run(SimplePlanetaryGear gear, PlanetaryState state, double ts, double tr, double tc, double h)
    {
        var status = gear.Advance(state, ts, tr, tc, h, out var step);
        Require(status == GearStepStatus.Ok, $"planetary reference rejected: {status}; J={gear.SunInertiaKilogramMeterSquared:R},{gear.RingInertiaKilogramMeterSquared:R},{gear.CarrierInertiaKilogramMeterSquared:R}; k={gear.RingToSunTeethRatio:R}; loads={ts:R},{tr:R},{tc:R}; h={h:R}");
        return step;
    }

    private static double Energy(double inertia, double speed) => .5 * inertia * speed * speed;
    private static double Scale(params double[] terms) => Math.Max(1, terms.Sum(Math.Abs));

    private static void Balance(IdealGearPair gear, GearPairState initial, GearPairStep step, double ta, double tb, double h)
    {
        double ja = gear.InertiaAKilogramMeterSquared, jb = gear.InertiaBKilogramMeterSquared;
        double a0 = initial.SpeedARadiansPerSecond, b0 = initial.SpeedBRadiansPerSecond;
        double a1 = step.State.SpeedARadiansPerSecond, b1 = step.State.SpeedBRadiansPerSecond;
        double k0 = Energy(ja, a0) + Energy(jb, b0), k1 = Energy(ja, a1) + Energy(jb, b1);
        Near(a1, gear.Ratio * b1, 2e-13 * Scale(a1, gear.Ratio * b1));
        Near(step.AngleAdvanceARadians, gear.Ratio * step.AngleAdvanceBRadians, 2e-13 * Scale(step.AngleAdvanceARadians));
        Near(step.ExternalWorkJoules, k1 - k0, 2e-12 * Scale(k0, k1, step.ExternalWorkJoules));
        Near(step.EnergyResidualJoules, 0, 2e-12 * Scale(k0, k1, step.ExternalWorkJoules));
        Near(ja * (a1 - a0), h * (ta + step.TorqueAtANewtonMeters), 2e-12 * Scale(ja * a0, ja * a1, h * ta));
        Near(jb * (b1 - b0), h * (tb + step.TorqueAtBNewtonMeters), 2e-12 * Scale(jb * b0, jb * b1, h * tb));
        Near(step.TorqueAtANewtonMeters * a1 + step.TorqueAtBNewtonMeters * b1, 0,
            2e-12 * Scale(step.TorqueAtANewtonMeters * a1, step.TorqueAtBNewtonMeters * b1));
    }

    private static void Balance(SimplePlanetaryGear gear, PlanetaryState initial, PlanetaryStep step, double ts, double tr, double tc, double h)
    {
        double js = gear.SunInertiaKilogramMeterSquared, jr = gear.RingInertiaKilogramMeterSquared, jc = gear.CarrierInertiaKilogramMeterSquared;
        double s0 = initial.SunSpeedRadiansPerSecond, r0 = initial.RingSpeedRadiansPerSecond, c0 = initial.CarrierSpeedRadiansPerSecond;
        double s1 = step.State.SunSpeedRadiansPerSecond, r1 = step.State.RingSpeedRadiansPerSecond, c1 = step.State.CarrierSpeedRadiansPerSecond;
        double k = gear.RingToSunTeethRatio;
        double k0 = Energy(js, s0) + Energy(jr, r0) + Energy(jc, c0), k1 = Energy(js, s1) + Energy(jr, r1) + Energy(jc, c1);
        Near(s1 + k * r1 - (1 + k) * c1, 0, 2e-12 * Scale(s1, k * r1, (1 + k) * c1));
        Near(step.SunAngleAdvanceRadians + k * step.RingAngleAdvanceRadians, (1 + k) * step.CarrierAngleAdvanceRadians,
            2e-12 * Scale(step.SunAngleAdvanceRadians, k * step.RingAngleAdvanceRadians));
        Near(step.RingTorqueNewtonMeters, k * step.SunTorqueNewtonMeters, 2e-12 * Scale(step.RingTorqueNewtonMeters));
        Near(step.CarrierTorqueNewtonMeters, -(1 + k) * step.SunTorqueNewtonMeters, 2e-12 * Scale(step.CarrierTorqueNewtonMeters));
        Near(step.ExternalWorkJoules, k1 - k0, 4e-12 * Scale(k0, k1, step.ExternalWorkJoules));
        Near(step.EnergyResidualJoules, 0, 4e-12 * Scale(k0, k1, step.ExternalWorkJoules));
        Near(js * (s1 - s0), h * (ts + step.SunTorqueNewtonMeters), 3e-12 * Scale(js * s0, js * s1, h * ts));
        Near(jr * (r1 - r0), h * (tr + step.RingTorqueNewtonMeters), 3e-12 * Scale(jr * r0, jr * r1, h * tr));
        Near(jc * (c1 - c0), h * (tc + step.CarrierTorqueNewtonMeters), 3e-12 * Scale(jc * c0, jc * c1, h * tc));
        Near(js * (s1 - s0) + jr * (r1 - r0) + jc * (c1 - c0), h * (ts + tr + tc),
            3e-12 * Scale(js * s0, jr * r0, jc * c0, js * s1, jr * r1, jc * c1, h * ts, h * tr, h * tc));
        Near(step.SunTorqueNewtonMeters * s1 + step.RingTorqueNewtonMeters * r1 + step.CarrierTorqueNewtonMeters * c1, 0,
            3e-12 * Scale(step.SunTorqueNewtonMeters * s1, step.RingTorqueNewtonMeters * r1, step.CarrierTorqueNewtonMeters * c1));
    }

    private static void Pair()
    {
        foreach (double ratio in new[] { -4.0, -.5, .5, 1, 3 })
        {
            var gear = new IdealGearPair(.2, .8, ratio);
            var state = new GearPairState(ratio * 20, 20);
            var step = Run(gear, state, 10, -2, .75);
            double equivalentA = .2 + .8 / (ratio * ratio);
            double accelerationA = (10 - 2 / ratio) / equivalentA;
            Near(step.State.SpeedARadiansPerSecond, state.SpeedARadiansPerSecond + .75 * accelerationA, 2e-13);
            Near(step.AngleAdvanceARadians, .75 * state.SpeedARadiansPerSecond + .5 * .75 * .75 * accelerationA, 2e-13);
            Near(step.TorqueAtANewtonMeters, .2 * accelerationA - 10, 2e-13);
            Near(gear.EquivalentInertiaAtBKilogramMeterSquared, ratio * ratio * .2 + .8, 2e-15);
            Balance(gear, state, step, 10, -2, .75);
            var unloaded = Run(gear, state, 0, 0, 10);
            Require(unloaded.State == state && unloaded.TorqueAtANewtonMeters == 0 && unloaded.ExternalWorkJoules == 0);
        }
    }

    private static void FreePlanetary()
    {
        // Independent acceleration-space multiplier, not the implementation's reduced mass matrix.
        var gear = new SimplePlanetaryGear(.2, .3, .8, 2.5);
        var state = new PlanetaryState(70, -7, 15);
        double ts = 12, tr = -4, tc = -3, k = 2.5, h = .7;
        double lambda = -(ts / .2 + k * tr / .3 - (1 + k) * tc / .8)
            / (1 / .2 + k * k / .3 + (1 + k) * (1 + k) / .8);
        var step = Run(gear, state, ts, tr, tc, h);
        Near(step.SunTorqueNewtonMeters, lambda, 2e-13);
        Near(step.State.SunSpeedRadiansPerSecond, 70 + h * (ts + lambda) / .2, 2e-13);
        Near(step.State.RingSpeedRadiansPerSecond, -7 + h * (tr + k * lambda) / .3, 2e-13);
        Near(step.State.CarrierSpeedRadiansPerSecond, 15 + h * (tc - (1 + k) * lambda) / .8, 2e-13);
        Balance(gear, state, step, ts, tr, tc, h);
        // Co-rotation has no relative gear motion, but all three members may carry external load.
        var coast = Run(gear, new(25, 25, 25), 0, 0, 0, 2);
        Near(coast.State.CarrierSpeedRadiansPerSecond, 25, 1e-14);
        Require(coast.ExternalWorkJoules == 0 && coast.SunTorqueNewtonMeters == 0);
    }

    private static void TransmissionStates()
    {
        const double js = .2, jr = .3, jc = .8, k = 2.5, ts = 12, tc = -3, h = .4;
        var gear = new SimplePlanetaryGear(js, jr, jc, k);
        // Ring brake: omega_C = omega_S / (1+k). Explicitly supply its required holding load.
        double accelerationS = (ts + tc / (1 + k)) / (js + jc / ((1 + k) * (1 + k)));
        double reactionS = js * accelerationS - ts, brake = -k * reactionS;
        var initial = new PlanetaryState(70, 0, 20);
        var reduced = Run(gear, initial, ts, brake, tc, h);
        Near(reduced.State.RingSpeedRadiansPerSecond, 0, 5e-14);
        Near(reduced.State.SunSpeedRadiansPerSecond, (1 + k) * reduced.State.CarrierSpeedRadiansPerSecond, 5e-13);
        Near(brake * reduced.RingAngleAdvanceRadians, 0, 5e-13);
        Balance(gear, initial, reduced, ts, brake, tc, h);
        // Sun brake: ring/carrier ratio (1+k)/k.
        double tr = 8, ratio = (1 + k) / k;
        double accelerationR = (tr + tc / ratio) / (jr + jc / (ratio * ratio));
        double reactionR = jr * accelerationR - tr, sunBrake = -reactionR / k;
        initial = new(0, 28, 20);
        var sunHeld = Run(gear, initial, sunBrake, tr, tc, h);
        Near(sunHeld.State.SunSpeedRadiansPerSecond, 0, 1e-13);
        Near(sunHeld.State.RingSpeedRadiansPerSecond, ratio * sunHeld.State.CarrierSpeedRadiansPerSecond, 1e-13);
        Balance(gear, initial, sunHeld, sunBrake, tr, tc, h);
        // Carrier brake reverses sun and ring: omega_S = -k * omega_R.
        accelerationR = (-k * ts + tr) / (js * k * k + jr);
        reactionS = -k * js * accelerationR - ts;
        double carrierBrake = (1 + k) * reactionS;
        initial = new(50, -20, 0);
        var reversed = Run(gear, initial, ts, tr, carrierBrake, h);
        Near(reversed.State.CarrierSpeedRadiansPerSecond, 0, 3e-14);
        Balance(gear, initial, reversed, ts, tr, carrierBrake, h);
        // Lock sun to ring: all three accelerate together. Supply equal-and-opposite clutch torques.
        double acceleration = (ts + tc) / (js + jr + jc);
        reactionS = -(jc * acceleration - tc) / (1 + k);
        double clutch = js * acceleration - ts - reactionS;
        initial = new(20, 20, 20);
        var direct = Run(gear, initial, ts + clutch, -clutch, tc, h);
        Near(direct.State.SunSpeedRadiansPerSecond, 20 + h * acceleration, 1e-13);
        Near(direct.State.RingSpeedRadiansPerSecond, direct.State.CarrierSpeedRadiansPerSecond, 1e-13);
        Near(clutch * (direct.SunAngleAdvanceRadians - direct.RingAngleAdvanceRadians), 0, 1e-13);
        Balance(gear, initial, direct, ts + clutch, -clutch, tc, h);
    }

    private static void Partitions()
    {
        var pair = new IdealGearPair(.2, .8, -3);
        var initial = new GearPairState(-60, 20);
        var whole = Run(pair, initial, 40, -10, 2);
        var state = initial; double da = 0, db = 0, work = 0;
        for (int i = 0; i < 200; ++i)
        {
            var part = Run(pair, state, 40, -10, .01);
            state = part.State; da += part.AngleAdvanceARadians; db += part.AngleAdvanceBRadians; work += part.ExternalWorkJoules;
        }
        Near(state.SpeedARadiansPerSecond, whole.State.SpeedARadiansPerSecond, 2e-11);
        Near(da, whole.AngleAdvanceARadians, 2e-11); Near(db, whole.AngleAdvanceBRadians, 2e-11);
        Near(work, whole.ExternalWorkJoules, 2e-9);
        var gear = new SimplePlanetaryGear(.2, .3, .8, 2.5);
        var p0 = new PlanetaryState(70, -7, 15);
        var total = Run(gear, p0, -40, 10, -5, 2); var p = p0; double ds = 0, dr = 0, dc = 0;
        for (int i = 0; i < 200; ++i)
        {
            var part = Run(gear, p, -40, 10, -5, .01); p = part.State;
            ds += part.SunAngleAdvanceRadians; dr += part.RingAngleAdvanceRadians; dc += part.CarrierAngleAdvanceRadians;
        }
        Near(p.SunSpeedRadiansPerSecond, total.State.SunSpeedRadiansPerSecond, 2e-11);
        Near(p.RingSpeedRadiansPerSecond, total.State.RingSpeedRadiansPerSecond, 2e-11);
        Near(p.CarrierSpeedRadiansPerSecond, total.State.CarrierSpeedRadiansPerSecond, 2e-11);
        Near(ds, total.SunAngleAdvanceRadians, 2e-11); Near(dr, total.RingAngleAdvanceRadians, 2e-11);
        Near(dc, total.CarrierAngleAdvanceRadians, 2e-11);
        // Cancellation at a dependent speed's zero must remain a valid state for the next call.
        var zeroPair = Run(new(1, 1, 3), new(3, 1), -3, -1, 1);
        Require(zeroPair.State == new GearPairState(0, 0));
        Require(Run(new(1, 1, 3), zeroPair.State, -3, -1, 1).State == new GearPairState(-3, -1));
    }

    private static void Convergence()
    {
        var pair = new IdealGearPair(.2, .8, 3);
        double alphaScale = 3 / (9 * .2 + .8), previous = 0;
        foreach (int count in new[] { 20, 40, 80, 160, 320 })
        {
            double h = 1.0 / count, angle = 0;
            var state = new GearPairState(0, 0);
            for (int i = 0; i < count; ++i)
            {
                double torque = Math.Sin((i + .5) * h);
                var step = Run(pair, state, torque, 0, h); state = step.State; angle += step.AngleAdvanceBRadians;
            }
            // Integral sin(t) is 1-cos(t); its position integral is t-sin(t).
            double speedError = Math.Abs(state.SpeedBRadiansPerSecond - alphaScale * (1 - Math.Cos(1)));
            double angleError = Math.Abs(angle - alphaScale * (1 - Math.Sin(1)));
            double error = speedError + angleError;
            if (previous > 0) Require(previous / error > 3.99 && previous / error < 4.01);
            previous = error;
        }
        Require(previous < 2e-6, $"pair final convergence error: {previous:R}");
        var gear = new SimplePlanetaryGear(.2, .3, .8, 2.5);
        // For loads [2, -1, 3] sin(t), use the independent acceleration constraint.
        const double k = 2.5;
        double lambda = -(2 / .2 - k / .3 - (1 + k) * 3 / .8)
            / (1 / .2 + k * k / .3 + (1 + k) * (1 + k) / .8);
        double accelerationS = (2 + lambda) / .2, accelerationR = (-1 + k * lambda) / .3;
        previous = 0;
        foreach (int count in new[] { 20, 40, 80, 160, 320 })
        {
            double h = 1.0 / count, angleS = 0, angleR = 0;
            var state = new PlanetaryState(0, 0, 0);
            for (int i = 0; i < count; ++i)
            {
                double load = Math.Sin((i + .5) * h);
                var step = Run(gear, state, 2 * load, -load, 3 * load, h); state = step.State;
                angleS += step.SunAngleAdvanceRadians; angleR += step.RingAngleAdvanceRadians;
            }
            double error = Math.Abs(state.SunSpeedRadiansPerSecond - accelerationS * (1 - Math.Cos(1)))
                + Math.Abs(state.RingSpeedRadiansPerSecond - accelerationR * (1 - Math.Cos(1)))
                + Math.Abs(angleS - accelerationS * (1 - Math.Sin(1)))
                + Math.Abs(angleR - accelerationR * (1 - Math.Sin(1)));
            if (previous > 0) Require(previous / error > 3.99 && previous / error < 4.01,
                $"planetary refinement ratio: {previous / error:R}");
            previous = error;
        }
        Require(previous < 2e-5, $"planetary final convergence error: {previous:R}");
    }

    private static void Contracts()
    {
        foreach (double bad in new[] { 0.0, -1, double.NaN, double.PositiveInfinity })
        {
            Throws<ArgumentOutOfRangeException>(() => new IdealGearPair(bad, 1, 2));
            Throws<ArgumentOutOfRangeException>(() => new IdealGearPair(1, bad, 2));
            Throws<ArgumentOutOfRangeException>(() => new SimplePlanetaryGear(bad, 1, 1, 2));
            Throws<ArgumentOutOfRangeException>(() => new SimplePlanetaryGear(1, bad, 1, 2));
            Throws<ArgumentOutOfRangeException>(() => new SimplePlanetaryGear(1, 1, bad, 2));
        }
        foreach (double bad in new[] { 0.0, double.NaN, double.PositiveInfinity })
            Throws<ArgumentOutOfRangeException>(() => new IdealGearPair(1, 1, bad));
        foreach (double bad in new[] { -2.0, 0, 1, double.NaN, double.PositiveInfinity })
            Throws<ArgumentOutOfRangeException>(() => new SimplePlanetaryGear(1, 1, 1, bad));
        Throws<ArgumentException>(() => new IdealGearPair(1, 1, 1e200));
        Throws<ArgumentException>(() => new IdealGearPair(double.Epsilon, double.Epsilon, 1));
        Throws<ArgumentException>(() => new SimplePlanetaryGear(1e-100, 1e-100, 1e100, 2));
        var pair = new IdealGearPair(.2, .8, -3);
        var gear = new SimplePlanetaryGear(.2, .3, .8, 2.5);
        foreach (double bad in new[] { 0.0, -1, double.NaN, double.PositiveInfinity })
        {
            Require(pair.Advance(default, 0, 0, bad, out var p) == GearStepStatus.InvalidDuration && p == default);
            Require(gear.Advance(default, 0, 0, 0, bad, out var q) == GearStepStatus.InvalidDuration && q == default);
        }
        Require(pair.Advance(new(1, 0), 0, 0, 1, out var a) == GearStepStatus.IncompatibleState && a == default);
        Require(pair.Advance(new(1e-100, 0), 0, 0, 1, out a) == GearStepStatus.IncompatibleState && a == default);
        Require(gear.Advance(new(1, 0, 0), 0, 0, 0, 1, out var b) == GearStepStatus.IncompatibleState && b == default);
        Require(gear.Advance(new(0, 0, 1e-100), 0, 0, 0, 1, out b) == GearStepStatus.IncompatibleState && b == default);
        Require(pair.Advance(new(double.NaN, 0), 0, 0, 1, out a) == GearStepStatus.InvalidState && a == default);
        Require(gear.Advance(new(0, double.PositiveInfinity, 0), 0, 0, 0, 1, out b) == GearStepStatus.InvalidState && b == default);
        Require(pair.Advance(default, double.NaN, 0, 1, out a) == GearStepStatus.InvalidTorque && a == default);
        Require(gear.Advance(default, 0, double.NaN, 0, 1, out b) == GearStepStatus.InvalidTorque && b == default);
        Require(pair.Advance(default, double.MaxValue, 0, 1, out a) == GearStepStatus.NumericalFailure && a == default);
        Require(gear.Advance(default, double.MaxValue, 0, 0, 1, out b) == GearStepStatus.NumericalFailure && b == default);
        Require(pair.Advance(new(0, double.MaxValue), 0, 0, 1, out a) == GearStepStatus.NumericalFailure && a == default);
        Throws<ArgumentOutOfRangeException>(() => gear.CarrierSpeed(double.NaN, 0));
        // A finite solve can still lose the force balance through cancellation.
        var poorlyScaled = new SimplePlanetaryGear(1e-12, 1e-12, 1, 2);
        Require(poorlyScaled.Advance(default, 1, 0, 0, 1, out b) == GearStepStatus.NumericalFailure && b == default);
        // The gear reaction must not vanish when inertial torque nearly equals applied torque.
        var highRatio = new IdealGearPair(1, 1, 1e10);
        var sensitive = Run(highRatio, default, 1, 0, 1);
        Near(sensitive.TorqueAtANewtonMeters, -1e-20, 1e-35);
        Near(sensitive.TorqueAtBNewtonMeters, 1e-10, 1e-25);
        Balance(highRatio, default, sensitive, 1, 0, 1);
        // Validation does not consume or mutate a prior successful value or the immutable model.
        var state = new GearPairState(-30, 10); var before = Run(pair, state, 2, -1, .2);
        Require(pair.Advance(state with { SpeedARadiansPerSecond = 1 }, 2, -1, .2, out a) == GearStepStatus.IncompatibleState);
        Require(Run(pair, state, 2, -1, .2) == before && state == new GearPairState(-30, 10));
    }

    private static void Sweep()
    {
        var random = new Random(9022026);
        for (int i = 0; i < 2500; ++i)
        {
            double Inertia() => Math.Pow(10, random.NextDouble() * 4 - 2);
            double Load() => random.NextDouble() * 100 - 50;
            double ratio = Math.Pow(10, random.NextDouble() * 2 - 1) * (i % 2 == 0 ? 1 : -1);
            double h = .001 + random.NextDouble(), ta = Load(), tb = Load(), speed = Load();
            var pair = new IdealGearPair(Inertia(), Inertia(), ratio);
            var state = new GearPairState(ratio * speed, speed);
            var step = Run(pair, state, ta, tb, h); Balance(pair, state, step, ta, tb, h);
            Require(Run(pair, state, ta, tb, h) == step);
            var gear = new SimplePlanetaryGear(Inertia(), Inertia(), Inertia(), 1.01 + random.NextDouble() * 9);
            double s = Load(), r = Load(), tc = Load();
            var p = new PlanetaryState(s, r, gear.CarrierSpeed(s, r));
            var q = Run(gear, p, ta, tb, tc, h); Balance(gear, p, q, ta, tb, tc, h);
            Require(Run(gear, p, ta, tb, tc, h) == q);
        }
    }

    private static void AllocationAndIndependence()
    {
        var pair = new IdealGearPair(.2, .8, -3);
        var gear = new SimplePlanetaryGear(.2, .3, .8, 2.5);
        var a = new GearPairState(-30, 10); var b = new PlanetaryState(70, -7, 15);
        GearPairStep p = default; PlanetaryStep q = default;
        for (int i = 0; i < 1000; ++i)
        { pair.Advance(a, 3, -1, .01, out p); gear.Advance(b, 3, -1, -2, .01, out q); }
        long before = GC.GetAllocatedBytesForCurrentThread();
        int failures = 0;
        for (int i = 0; i < 10000; ++i)
        {
            if (pair.Advance(a, 3, -1, .01, out p) != GearStepStatus.Ok) ++failures;
            if (gear.Advance(b, 3, -1, -2, .01, out q) != GearStepStatus.Ok) ++failures;
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(failures == 0 && allocated == 0, $"reference allocation bytes: {allocated}; failures: {failures}");
        var expectedP = p; var expectedQ = q;
        Parallel.For(0, 100, i =>
        {
            Require(Run(pair, a, 3, -1, .01) == expectedP);
            Require(Run(gear, b, 3, -1, -2, .01) == expectedQ);
        });
    }
}
