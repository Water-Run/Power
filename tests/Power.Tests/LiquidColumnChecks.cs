// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class LiquidColumnChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("liquid column / geometry momentum and independent lossless work", Lossless),
        ("liquid column / independent exponential and hyperbolic-tangent limits / second order", Limits),
        ("liquid column / opposing flow volumes reversal work and irreversible heat", Reversal),
        ("liquid column / passive decay finite entropy and physical steady limits", Passivity),
        ("liquid column / independent changing-pressure RK4 refinement", Reference),
        ("liquid column / bounds coarse-step rejection immutability and zero allocation", Contracts)
    ];
    private static LiquidColumn Law(double linear = 1e10, double quadratic = 1e16) => new(.1, 1e-8, 750, linear, quadratic, 300, 3000);
    private static LiquidColumnStep Step(LiquidColumn law, double q, double pa, double pb, double h)
    {
        var status = law.Advance(new(q), pa, pb, h, out var step); Require(status == LiquidColumnStatus.Success, "Liquid column interval: " + status);
        Near(step.EnergyResidualJoules, 0, 2e-15); Require(step.FrictionHeatJoules >= 0 && step.EntropyProducedJoulesPerKelvin >= 0);
        return step;
    }
    private static void Lossless()
    {
        var law = Law(0, 0); Near(law.InertancePascalSecondSquaredPerCubicMeter, 7.5e9, 0); Near(law.ColumnMassKilograms, 7.5e-7, 1e-21);
        foreach (double initial in new[] { -1e-6, 0d, 1e-6 })
        {
            var step = Step(law, initial, 2e5, 1e5, .02); double next = initial + .02 * 1e5 / 7.5e9;
            Near(step.State.VolumeFlowCubicMetersPerSecond, next, 1e-21);
            Near(step.TransferredVolumeCubicMeters, .02 * initial + .5 * 1e5 / 7.5e9 * .02 * .02, 1e-22);
            Near(step.KineticEnergyChangeJoules, .5 * 7.5e9 * (next * next - initial * initial), 1e-17);
            Near(step.FrictionHeatJoules, 0, 0);
        }
    }
    private static void Limits()
    {
        foreach (bool quadratic in new[] { false, true })
        {
            var law = quadratic ? Law(0, 1e16) : Law(1e10, 0); var errors = new List<double>(); const double duration = .2, pressure = 1e5;
            double exact = quadratic ? Math.Sqrt(pressure / 1e16) * Math.Tanh(Math.Sqrt(pressure * 1e16) * duration / 7.5e9) : pressure / 1e10 * (1 - Math.Exp(-1e10 * duration / 7.5e9));
            foreach (int count in new[] { 10, 20, 40, 80 })
            {
                double flow = 0; for (int n = 0; n < count; ++n) flow = Step(law, flow, 2e5, 1e5, duration / count).State.VolumeFlowCubicMetersPerSecond;
                errors.Add(Math.Abs(flow - exact) / exact);
            }
            Console.WriteLine("Liquid column " + (quadratic ? "quadratic" : "linear") + " normalized errors: " + string.Join(", ", errors.Select(x => x.ToString("R"))));
            for (int n = 1; n < errors.Count; ++n) Require(errors[n - 1] / errors[n] is > 3.8 and < 4.2);
        }
    }
    private static void Reversal()
    {
        var law = Law(); var step = Step(law, 1e-7, 1e5, 2e5, .02);
        Require(step.InternalIntervals == 2 && step.State.VolumeFlowCubicMetersPerSecond < 0 && step.ForwardVolumeCubicMeters > 0 && step.ReverseVolumeCubicMeters > 0 && step.FrictionHeatJoules > 0);
        Near(step.PressureWorkJoules, -1e5 * step.TransferredVolumeCubicMeters, 1e-17);
        var mirror = Step(law, -1e-7, 2e5, 1e5, .02);
        Near(step.ForwardVolumeCubicMeters, mirror.ReverseVolumeCubicMeters, 1e-22); Near(step.ReverseVolumeCubicMeters, mirror.ForwardVolumeCubicMeters, 1e-22);
        Near(step.FrictionHeatJoules, mirror.FrictionHeatJoules, 1e-17);
        var free = Step(Law(0, 0), 1e-7, 1e5, 2e5, .02); Near(free.FrictionHeatJoules, 0, 0);
    }
    private static void Passivity()
    {
        foreach (double initial in new[] { -1e-6, 1e-6 })
        {
            var law = Law(); double flow = initial, heat = 0;
            for (int n = 0; n < 1000; ++n)
            {
                var step = Step(law, flow, 1e5, 1e5, .001); Require(Math.Abs(step.State.VolumeFlowCubicMetersPerSecond) <= Math.Abs(flow));
                heat += step.FrictionHeatJoules; Near(step.EntropyProducedJoulesPerKelvin, step.FrictionHeatJoules / 300, 0); flow = step.State.VolumeFlowCubicMetersPerSecond;
            }
            Near(heat + .5 * 7.5e9 * flow * flow, .5 * 7.5e9 * initial * initial, 2e-16);
        }
        double equilibrium = (-1e10 + Math.Sqrt(1e20 + 4e21)) / 2e16;
        var fixedPoint = Step(Law(), equilibrium, 2e5, 1e5, .01); Near(fixedPoint.State.VolumeFlowCubicMetersPerSecond, equilibrium, 1e-20);
    }
    private static void Reference()
    {
        const double duration = .3;
        double Pressure(double t) => 1e5 * Math.Cos(2 * Math.PI * t / duration);
        double Rate(double t, double q) => (Pressure(t) - 1e10 * q - 1e16 * q * Math.Abs(q)) / 7.5e9;
        double Rk4(int count)
        {
            double q = 1e-7, h = duration / count;
            for (int n = 0; n < count; ++n) { double t = n * h, a = Rate(t, q), b = Rate(t + h / 2, q + h * a / 2), c = Rate(t + h / 2, q + h * b / 2), d = Rate(t + h, q + h * c); q += h / 6 * (a + 2 * b + 2 * c + d); }
            return q;
        }
        double reference = Rk4(20000); Near(reference, Rk4(40000), 1e-18); var errors = new List<double>();
        foreach (int count in new[] { 100, 200, 400, 800 })
        {
            var law = Law(); double q = 1e-7, h = duration / count;
            for (int n = 0; n < count; ++n) q = Step(law, q, 2e5 + Pressure((n + .5) * h), 2e5, h).State.VolumeFlowCubicMetersPerSecond;
            errors.Add(Math.Abs(q - reference) / 1e-6);
        }
        Console.WriteLine("Liquid column changing-pressure normalized errors: " + string.Join(", ", errors.Select(x => x.ToString("R"))));
        for (int n = 1; n < errors.Count; ++n) Require(errors[n - 1] / errors[n] is > 3.7 and < 4.3);
    }
    private static void Contracts()
    {
        var law = Law(); var state = new LiquidColumnState(1e-6);
        Require(law.Advance(state, 2999, 1e5, .01, out var rejected) == LiquidColumnStatus.PressureOutOfRange && rejected == default);
        Require(law.Advance(state, 1e5, 1e5, 100, out rejected) == LiquidColumnStatus.StepTooLarge && rejected == default);
        Require(law.Advance(state, 1e5, 1e5, double.NaN, out rejected) == LiquidColumnStatus.InvalidInput && rejected == default);
        Require(law.Advance(new(double.PositiveInfinity), 1e5, 1e5, .01, out rejected) == LiquidColumnStatus.InvalidInput && rejected == default);
        Throws<ArgumentException>(() => new LiquidColumn(.1, 0, 750, 0, 0, 300, 3000));
        Throws<ArgumentException>(() => new LiquidColumn(.1, 1e-8, 750, -1, 0, 300, 3000));
        var baseline = Step(law, state.VolumeFlowCubicMetersPerSecond, 2e5, 1e5, .01);
        for (int n = 0; n < 30; ++n) Require(Step(law, state.VolumeFlowCubicMetersPerSecond, 2e5, 1e5, .01) == baseline);
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int n = 0; n < 100; ++n) Require(law.Advance(state, 2e5, 1e5, .01, out _) == LiquidColumnStatus.Success);
        Require(GC.GetAllocatedBytesForCurrentThread() == allocated && state.VolumeFlowCubicMetersPerSecond == 1e-6);
    }
}
