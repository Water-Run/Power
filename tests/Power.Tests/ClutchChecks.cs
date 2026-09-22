// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class ClutchChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("clutch law / static limit / kinetic direction / no velocity deadband", Law),
        ("clutch pair analytic engagement / momentum / heat / internal event", Engagement),
        ("clutch static load sharing / breakaway / partial engagement", StaticAndBreakaway),
        ("clutch reversal through zero / event at interval boundary", Reversal),
        ("ground brake / holding torque / kinetic energy conversion", Brake),
        ("clutch signed ratio / generalized momentum / power balance", Ratios),
        ("clutch constant-load partition invariance across hybrid events", Partitions),
        ("clutch changing-load convergence against independent integrals", Convergence),
        ("clutch invalid inputs / arithmetic failure / pure result contract", Contracts),
        ("clutch range sweep / deterministic allocation-free repeated evaluation", RangeAndAllocations)
    ];

    private static ClutchPairStep Run(ClutchPair pair, ClutchPairState state, double ta, double tb, double engagement, double seconds)
    {
        var status = pair.Advance(state, ta, tb, engagement, seconds, out var result);
        Require(status == ClutchStepStatus.Ok, $"clutch interval rejected: {status}");
        return result;
    }

    private static void Balance(ClutchPair pair, ClutchPairState initial, ClutchPairStep step, double ta, double tb, double seconds)
    {
        double ja = pair.InertiaAKilogramMeterSquared, jb = pair.InertiaBKilogramMeterSquared, r = pair.Ratio;
        double a0 = initial.SpeedARadiansPerSecond, b0 = initial.SpeedBRadiansPerSecond;
        double a1 = step.State.SpeedARadiansPerSecond, b1 = step.State.SpeedBRadiansPerSecond;
        double k0 = .5 * ja * a0 * a0 + .5 * jb * b0 * b0, k1 = .5 * ja * a1 * a1 + .5 * jb * b1 * b1;
        double scale = Math.Max(1, Math.Max(k0, Math.Max(k1, Math.Abs(step.ExternalWorkJoules))));
        Near(step.ExternalWorkJoules - step.FrictionHeatJoules, k1 - k0, 2e-12 * scale);
        Near(step.KineticEnergyChangeJoules, k1 - k0, 2e-12 * scale);
        Near(step.EnergyResidualJoules, 0, 2e-12 * scale);
        Near(ja * (a1 - a0), ta * seconds + step.ImpulseAtANewtonMeterSeconds,
            2e-12 * Math.Max(1, Math.Abs(ja * a0) + Math.Abs(ta * seconds) + Math.Abs(step.ImpulseAtANewtonMeterSeconds)));
        if (!pair.Grounded)
            Near(jb * (b1 - b0), tb * seconds - r * step.ImpulseAtANewtonMeterSeconds,
                2e-12 * Math.Max(1, Math.Abs(jb * b0) + Math.Abs(tb * seconds) + Math.Abs(r * step.ImpulseAtANewtonMeterSeconds)));
        Require(step.FrictionHeatJoules >= 0 && step.EndReaction.HeatFlowWatts >= 0);
        Require(step.SlippingDurationSeconds >= 0 && step.SlippingDurationSeconds <= seconds,
            $"sliding duration {step.SlippingDurationSeconds:R} exceeds interval {seconds:R}");
    }

    private static void Law()
    {
        var law = new DryClutch(20, 12);
        Require(law.Evaluate(0, -20, 1) == new ClutchReaction(ClutchMode.Locked, -20, 0));
        Require(law.Evaluate(0, 20, 1).Mode == ClutchMode.Locked);
        Require(law.Evaluate(0, -20.000001, 1) == new ClutchReaction(ClutchMode.SlippingPositive, -12, 0));
        Require(law.Evaluate(0, 21, 1) == new ClutchReaction(ClutchMode.SlippingNegative, 12, 0));
        Require(law.Evaluate(3, 0, .5) == new ClutchReaction(ClutchMode.SlippingPositive, -6, 18));
        Require(law.Evaluate(-3, 0, .5) == new ClutchReaction(ClutchMode.SlippingNegative, 6, 18));
        Require(law.Evaluate(1e-30, 0, 1).Mode == ClutchMode.SlippingPositive);
        Require(law.Evaluate(-1e-30, 0, 1).Mode == ClutchMode.SlippingNegative);
        Require(law.Evaluate(-0.0, -0.0, .5).Mode == ClutchMode.Locked);
        Require(law.Evaluate(50, 500, 0).Mode == ClutchMode.Disengaged);
        Require(new DryClutch(0, 0).Evaluate(0, 100, 1).Mode == ClutchMode.Disengaged);
        Require(new DryClutch(10, 0).Evaluate(1, 0, 1).TorqueAtANewtonMeters == 0);
    }

    private static void Engagement()
    {
        var pair = new ClutchPair(.2, .8, new(20, 10));
        var initial = new ClutchPairState(100, 0);
        var step = Run(pair, initial, 0, 0, 1, 4);
        // Reduced inertia is 0.16 kg m2. Sliding at 10 Nm removes 800 J in 1.6 s.
        Near(step.State.SpeedARadiansPerSecond, 20, 1e-13);
        Near(step.State.SpeedBRadiansPerSecond, 20, 1e-13);
        Near(step.ZeroSlipTimeSeconds!.Value, 1.6, 1e-15);
        Near(step.SlippingDurationSeconds, 1.6, 1e-15);
        Near(step.FrictionHeatJoules, 800, 1e-12);
        Near(step.ImpulseAtANewtonMeterSeconds, -16, 1e-14);
        Near(step.AngleAdvanceARadians, 144, 1e-12);
        Near(step.AngleAdvanceBRadians, 64, 1e-12);
        Require(step.EndReaction == new ClutchReaction(ClutchMode.Locked, 0, 0));
        Balance(pair, initial, step, 0, 0, 4);
        var partial = Run(pair, initial, 0, 0, .5, 4);
        Near(partial.ZeroSlipTimeSeconds!.Value, 3.2, 1e-14);
        Near(partial.FrictionHeatJoules, 800, 1e-12);
        Balance(pair, initial, partial, 0, 0, 4);
        var open = Run(pair, initial, 4, -8, 0, .5);
        Require(open.EndReaction.Mode == ClutchMode.Disengaged && open.ZeroSlipTimeSeconds is null && open.SlippingDurationSeconds == 0);
        Near(open.State.SpeedARadiansPerSecond, 110, 1e-13);
        Near(open.State.SpeedBRadiansPerSecond, -5, 1e-13);
        Require(open.FrictionHeatJoules == 0 && open.ImpulseAtANewtonMeterSeconds == 0);
        Balance(pair, initial, open, 4, -8, .5);
    }

    private static void StaticAndBreakaway()
    {
        var pair = new ClutchPair(1, 3, new(15, 8));
        var initial = new ClutchPairState(10, 10);
        var hold = Run(pair, initial, 20, 4, 1, .7);
        Near(hold.State.SpeedARadiansPerSecond, 14.2, 1e-13);
        Require(hold.State.SpeedARadiansPerSecond == hold.State.SpeedBRadiansPerSecond);
        Near(hold.EndReaction.TorqueAtANewtonMeters, -14, 1e-13);
        Require(hold.FrictionHeatJoules == 0 && hold.SlippingDurationSeconds == 0 && hold.ZeroSlipTimeSeconds == 0);
        Balance(pair, initial, hold, 20, 4, .7);

        pair = new(1, 1, new(15, 8));
        initial = new(0, 0);
        var limit = Run(pair, initial, 30, 0, 1, 1);
        Require(limit.EndReaction.Mode == ClutchMode.Locked);
        Near(limit.State.SpeedARadiansPerSecond, 15, 1e-14);
        var release = Run(pair, initial, 32, 0, 1, 1);
        Require(release.EndReaction.Mode == ClutchMode.SlippingPositive && release.ZeroSlipTimeSeconds == 0);
        Near(release.State.SpeedARadiansPerSecond, 24, 1e-14);
        Near(release.State.SpeedBRadiansPerSecond, 8, 1e-14);
        Near(release.FrictionHeatJoules, 64, 1e-13);
        Balance(pair, initial, release, 32, 0, 1);
        var reduced = Run(pair, initial, 20, 0, .5, 1);
        Near(reduced.State.SpeedARadiansPerSecond, 16, 1e-14);
        Near(reduced.State.SpeedBRadiansPerSecond, 4, 1e-14);
        Balance(pair, initial, reduced, 20, 0, 1);
        // The load can be supported statically, but an already sliding clutch uses its lower kinetic limit.
        Require(Run(pair, new(1e-9, 0), 24, 0, 1, 1).EndReaction.Mode == ClutchMode.SlippingPositive);
    }

    private static void Reversal()
    {
        var pair = new ClutchPair(1, 1, new(5, 3));
        var initial = new ClutchPairState(8, 0);
        double t = 4.0 / 13, remaining = 1 - t;
        var step = Run(pair, initial, -20, 0, 1, 1);
        Near(step.ZeroSlipTimeSeconds!.Value, t, 1e-15);
        Near(step.State.SpeedARadiansPerSecond, 12.0 / 13 - 17 * remaining, 1e-13);
        Near(step.State.SpeedBRadiansPerSecond, 12.0 / 13 - 3 * remaining, 1e-13);
        Near(step.FrictionHeatJoules, 12 * t + 21 * remaining * remaining, 1e-13);
        Near(step.ImpulseAtANewtonMeterSeconds, -3 * t + 3 * remaining, 1e-14);
        Require(step.EndReaction.Mode == ClutchMode.SlippingNegative);
        Near(step.SlippingDurationSeconds, 1, 1e-15);
        Balance(pair, initial, step, -20, 0, 1);
        var atZero = Run(pair, initial, -20, 0, 1, t);
        Require(atZero.State.SpeedARadiansPerSecond == atZero.State.SpeedBRadiansPerSecond);
        Require(atZero.EndReaction.Mode == ClutchMode.SlippingNegative && atZero.EndReaction.HeatFlowWatts == 0);
        var next = Run(pair, atZero.State, -20, 0, 1, remaining);
        Near(next.State.SpeedARadiansPerSecond, step.State.SpeedARadiansPerSecond, 1e-14);
        Near(atZero.FrictionHeatJoules + next.FrictionHeatJoules, step.FrictionHeatJoules, 1e-13);
        // A zero kinetic limit still allows a static latch when an externally driven slip reaches zero.
        var latch = new ClutchPair(1, 1, new(5, 0));
        var latched = Run(latch, new(1, 0), -2, 0, 1, 1);
        Require(latched.EndReaction.Mode == ClutchMode.Locked && latched.FrictionHeatJoules == 0);
        Near(latched.State.SpeedARadiansPerSecond, -.5, 1e-14);
        Balance(latch, new(1, 0), latched, -2, 0, 1);
    }

    private static void Brake()
    {
        var pair = ClutchPair.Brake(2, new(6, 4));
        var initial = new ClutchPairState(10, 0);
        var stop = Run(pair, initial, 2, 0, 1, 12);
        Require(pair.Grounded && pair.Ratio == 1 && pair.InertiaBKilogramMeterSquared == 0);
        Require(stop.State == new ClutchPairState(0, 0));
        Require(stop.EndReaction == new ClutchReaction(ClutchMode.Locked, -2, 0));
        Near(stop.ZeroSlipTimeSeconds!.Value, 10, 1e-14);
        Near(stop.AngleAdvanceARadians, 50, 1e-13);
        Near(stop.FrictionHeatJoules, 200, 1e-12);
        Balance(pair, initial, stop, 2, 0, 12);
        var release = Run(pair, new(0, 0), 8, 0, 1, 2);
        Near(release.State.SpeedARadiansPerSecond, 4, 1e-14);
        Near(release.FrictionHeatJoules, 16, 1e-13);
        Balance(pair, new(0, 0), release, 8, 0, 2);
        var reverse = Run(pair, new(-10, 0), -2, 0, 1, 12);
        Near(reverse.FrictionHeatJoules, stop.FrictionHeatJoules, 1e-12);
        Near(reverse.AngleAdvanceARadians, -stop.AngleAdvanceARadians, 1e-13);
    }

    private static void Ratios()
    {
        foreach (double r in new[] { -3.0, -.25, .25, 3.0 })
        {
            const double ja = 1.5, jb = .4;
            var pair = new ClutchPair(ja, jb, new(20, 10), r);
            var initial = new ClutchPairState(20, -3);
            double momentum = r * ja * 20 - 3 * jb;
            double b = momentum / (r * r * ja + jb);
            var step = Run(pair, initial, 0, 0, 1, 10);
            Near(step.State.SpeedBRadiansPerSecond, b, 1e-13);
            Require(step.State.SpeedARadiansPerSecond == r * step.State.SpeedBRadiansPerSecond);
            Near(r * ja * step.State.SpeedARadiansPerSecond + jb * step.State.SpeedBRadiansPerSecond, momentum, 1e-12);
            Near(step.FrictionHeatJoules, .5 * (20 + 3 * r) * (20 + 3 * r) / (1 / ja + r * r / jb), 1e-11);
            Balance(pair, initial, step, 0, 0, 10);
            var loaded = Run(pair, step.State, 5, -2, 1, .5);
            Require(loaded.EndReaction.Mode == ClutchMode.Locked && loaded.FrictionHeatJoules == 0);
            Balance(pair, step.State, loaded, 5, -2, .5);
            Near(loaded.AngleAdvanceARadians, r * loaded.AngleAdvanceBRadians, 1e-12);
        }
    }

    private static void Partitions()
    {
        foreach (double ta in new[] { -20.0, 0, 3, 20 })
            foreach (double r in new[] { -2.0, 1, 3 })
            {
                var pair = new ClutchPair(.7, 1.2, new(9, 5), r);
                var initial = new ClutchPairState(8, 0);
                var whole = Run(pair, initial, ta, -2, .8, 3.3);
                foreach (int count in new[] { 3, 17, 200 })
                {
                    var state = initial;
                    double heat = 0, work = 0, impulse = 0, angleA = 0, angleB = 0;
                    for (int i = 0; i < count; ++i)
                    {
                        var part = Run(pair, state, ta, -2, .8, 3.3 / count);
                        state = part.State;
                        heat += part.FrictionHeatJoules; work += part.ExternalWorkJoules;
                        impulse += part.ImpulseAtANewtonMeterSeconds;
                        angleA += part.AngleAdvanceARadians; angleB += part.AngleAdvanceBRadians;
                    }
                    Near(state.SpeedARadiansPerSecond, whole.State.SpeedARadiansPerSecond, 2e-11);
                    Near(state.SpeedBRadiansPerSecond, whole.State.SpeedBRadiansPerSecond, 2e-11);
                    Near(angleA, whole.AngleAdvanceARadians, 2e-11);
                    Near(angleB, whole.AngleAdvanceBRadians, 2e-11);
                    Near(heat, whole.FrictionHeatJoules, 2e-10);
                    Near(work, whole.ExternalWorkJoules, 2e-10);
                    Near(impulse, whole.ImpulseAtANewtonMeterSeconds, 2e-11);
                }
            }
    }

    private static void Convergence()
    {
        var pair = new ClutchPair(2, 3, new(2, 1));
        const double duration = 1.5;
        double exactA = 100 + 5.25 * duration / 2 + (1 - Math.Cos(2 * duration)) / 2;
        double exactB = 1.75 * duration / 3 - Math.Sin(3 * duration) / 3;
        double exactAngleA = 100 * duration + 5.25 * duration * duration / 4 + duration / 2 - Math.Sin(2 * duration) / 4;
        double exactAngleB = 1.75 * duration * duration / 6 + (Math.Cos(3 * duration) - 1) / 9;
        double exactHeat = .75 * (exactAngleA - exactAngleB), previous = double.PositiveInfinity;
        foreach (int count in new[] { 20, 40, 80, 160 })
        {
            var state = new ClutchPairState(100, 0);
            double angleA = 0, angleB = 0, heat = 0, work = 0, h = duration / count;
            for (int i = 0; i < count; ++i)
            {
                double t = (i + .5) * h;
                var step = Run(pair, state, 6 + 2 * Math.Sin(2 * t), 1 - 3 * Math.Cos(3 * t), .75, h);
                Require(step.EndReaction.Mode == ClutchMode.SlippingPositive && step.ZeroSlipTimeSeconds is null);
                state = step.State; angleA += step.AngleAdvanceARadians; angleB += step.AngleAdvanceBRadians;
                heat += step.FrictionHeatJoules; work += step.ExternalWorkJoules;
            }
            double error = Math.Max(Math.Abs(state.SpeedARadiansPerSecond - exactA), Math.Max(Math.Abs(state.SpeedBRadiansPerSecond - exactB),
                Math.Max(Math.Abs(angleA - exactAngleA), Math.Max(Math.Abs(angleB - exactAngleB), Math.Abs(heat - exactHeat)))));
            Require(error < previous / 3.8, $"midpoint load sampling failed to converge: {previous} to {error}");
            previous = error;
            Near(work - heat, state.SpeedARadiansPerSecond * state.SpeedARadiansPerSecond + 1.5 * state.SpeedBRadiansPerSecond * state.SpeedBRadiansPerSecond - 10000, 5e-9);
        }
        Require(previous < 2e-5, $"finest constant-load approximation error {previous}");
    }

    private static void Contracts()
    {
        var law = new DryClutch(10, 5);
        foreach (double bad in new[] { -1.0, double.NaN, double.PositiveInfinity })
        {
            Throws<ArgumentException>(() => new DryClutch(bad, 0));
            Throws<ArgumentException>(() => new DryClutch(10, bad));
        }
        Throws<ArgumentException>(() => new DryClutch(10, 11));
        Throws<ArgumentException>(() => law.Evaluate(double.NaN, 0, 1));
        Throws<ArgumentException>(() => law.Evaluate(0, double.PositiveInfinity, 1));
        Throws<ArgumentException>(() => law.Evaluate(0, 0, 1.00001));
        Throws<ArgumentException>(() => new DryClutch(double.MaxValue, double.MaxValue).Evaluate(2, 0, 1));
        Throws<ArgumentException>(() => new ClutchPair(0, 1, law));
        Throws<ArgumentException>(() => new ClutchPair(1, 0, law));
        Throws<ArgumentException>(() => new ClutchPair(1, 1, law, 0));
        Throws<ArgumentException>(() => new ClutchPair(1, 1, law, double.NaN));
        Throws<ArgumentException>(() => new ClutchPair(double.Epsilon, 1, law));
        Throws<ArgumentException>(() => new ClutchPair(1, 1, law, double.MaxValue));
        Throws<ArgumentNullException>(() => new ClutchPair(1, 1, null!));

        var pair = new ClutchPair(1, 1, law);
        void Reject(ClutchPairState state, double ta, double tb, double engagement, double duration, ClutchStepStatus expected)
        {
            Require(pair.Advance(state, ta, tb, engagement, duration, out var result) == expected);
            Require(result == default, "a failed local solve must not publish a partial result");
        }
        foreach (double duration in new[] { 0.0, -1, double.NaN, double.PositiveInfinity })
            Reject(new(10, 0), 0, 0, 1, duration, ClutchStepStatus.InvalidDuration);
        foreach (double engagement in new[] { -.1, 1.1, double.NaN, double.PositiveInfinity })
            Reject(new(10, 0), 0, 0, engagement, 1, ClutchStepStatus.InvalidEngagement);
        Reject(new(double.NaN, 0), 0, 0, 1, 1, ClutchStepStatus.InvalidState);
        Reject(new(10, double.NegativeInfinity), 0, 0, 1, 1, ClutchStepStatus.InvalidState);
        Reject(new(10, 0), double.NaN, 0, 1, 1, ClutchStepStatus.InvalidTorque);
        Reject(new(10, 0), 0, double.PositiveInfinity, 1, 1, ClutchStepStatus.InvalidTorque);
        Reject(new(double.MaxValue, -double.MaxValue), 0, 0, 1, 1, ClutchStepStatus.NumericalFailure);
        Reject(new(10, 0), double.MaxValue, 0, 0, 10, ClutchStepStatus.NumericalFailure);
        pair = ClutchPair.Brake(1, law);
        Reject(new(10, 1), 0, 0, 1, 1, ClutchStepStatus.InvalidState);
        Reject(new(10, 0), 0, 1, 1, 1, ClutchStepStatus.InvalidTorque);
        pair = ClutchPair.Brake(1, new(double.MaxValue, double.MaxValue));
        Reject(new(double.Epsilon, 0), 0, 0, 1, 1, ClutchStepStatus.NumericalFailure);
        // Pure evaluation cannot corrupt another branch or change future evaluations after rejection.
        pair = new(1, 1, law);
        var initial = new ClutchPairState(10, 0);
        var expected = Run(pair, initial, 0, 0, 1, 2);
        Reject(initial, 0, 0, double.NaN, 2, ClutchStepStatus.InvalidEngagement);
        Require(Run(pair, initial, 0, 0, 1, 2) == expected && initial == new ClutchPairState(10, 0));
        Require(Run(pair, initial, 0, 0, 0, 2).State != expected.State);
    }

    private static void RangeAndAllocations()
    {
        var random = new Random(413);
        for (int i = 0; i < 2000; ++i)
        {
            double ja = Math.Exp(8 * random.NextDouble() - 4), jb = Math.Exp(8 * random.NextDouble() - 4);
            double ratio = (i % 2 == 0 ? 1 : -1) * Math.Exp(4 * random.NextDouble() - 2);
            double capacity = 1 + 50 * random.NextDouble();
            var pair = new ClutchPair(ja, jb, new(capacity, .6 * capacity), ratio);
            var initial = new ClutchPairState(100 * random.NextDouble() - 50, 100 * random.NextDouble() - 50);
            double ta = 40 * random.NextDouble() - 20, tb = 40 * random.NextDouble() - 20;
            double engagement = random.NextDouble(), seconds = .01 + 3 * random.NextDouble();
            var step = Run(pair, initial, ta, tb, engagement, seconds);
            Balance(pair, initial, step, ta, tb, seconds);
            Require(Run(pair, initial, ta, tb, engagement, seconds) == step);
        }
        var same = new ClutchPair(.2, .8, new(20, 10));
        var state = new ClutchPairState(100, 0);
        // Warm the same measurement path separately from the range-sweep assertion machinery.
        MeasureAllocations(same, state);
        var measured = MeasureAllocations(same, state);
        Require(measured.Success && measured.Bytes == 0, $"clutch stepping allocated {measured.Bytes} bytes");
        Near(measured.Heat, 8_000_000, 1e-7);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static (long Bytes, double Heat, bool Success) MeasureAllocations(ClutchPair pair, ClutchPairState state)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        double totalHeat = 0; bool success = true;
        for (int i = 0; i < 10000; ++i)
        {
            success &= pair.Advance(state, 0, 0, 1, 4, out var result) == ClutchStepStatus.Ok;
            totalHeat += result.FrictionHeatJoules;
        }
        return (GC.GetAllocatedBytesForCurrentThread() - before, totalHeat, success);
    }
}
