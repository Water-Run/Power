// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class FrozenFuelNozzleChecks
{
    private const double R = 287, Cv = 717.5, Cl = 2000, Density = 750, Area = 1e-8, Cd = .7;
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("frozen nozzle / incompressible Bernoulli and effective-area limit", Liquid),
        ("frozen nozzle / independent ideal-gas subcritical and choked limits", Gas),
        ("frozen nozzle / independent mixture flux maximum and frozen sonic condition", Mixture),
        ("frozen nozzle / energy entropy momentum phase freezing and allocation-free bounds", Contracts)
    ];
    internal static FuelPhaseEquilibrium Material() => new(new(Cl, Cv, 400, 300000, 0), R, 100000, 200, 600);
    internal static FrozenFuelNozzle Law() => new(Material(), Density, Area, Cd);
    private static void Ok(FuelNozzleStatus status) { if (status != FuelNozzleStatus.Success) Require(false, status.ToString()); }
    private static void Liquid()
    {
        var law = Law(); Ok(law.Evaluate(8e5, 300, 0, 1e5, 1, out var point));
        double velocity = Math.Sqrt(2 * (8e5 - 1e5) / Density);
        Near(point.ExitVelocityMetersPerSecond, velocity, 1e-12); Near(point.MassFlowKilogramsPerSecond, Cd * Area * Density * velocity, 1e-16);
        Near(point.ExitTemperatureKelvin, 300, 0); Near(point.KineticEnergyJoulesPerKilogram, 7e5 / Density, 1e-10); Require(!point.Choked);
        Ok(law.Evaluate(8e5, 300, 0, 1e5, .3, out var partial)); Near(partial.MassFlowKilogramsPerSecond, .3 * point.MassFlowKilogramsPerSecond, 1e-17);
        Ok(law.Evaluate(8e5, 300, 1e-14, 1e5, 1, out var limit)); Near(limit.MassFlowKilogramsPerSecond, point.MassFlowKilogramsPerSecond, 1e-14);
    }
    private static void Gas()
    {
        var law = Law(); const double pressure = 6e5, temperature = 500, gamma = 1.4;
        double critical = Math.Pow(2 / (gamma + 1), gamma / (gamma - 1));
        foreach (double ratio in new[] { .95, .8, critical, .3, .2 })
        {
            Ok(law.Evaluate(pressure, temperature, 1, pressure * ratio, 1, out var point));
            double effectiveRatio = Math.Max(ratio, critical);
            double flux = pressure / Math.Sqrt(R * temperature) * Math.Sqrt(2 * gamma / (gamma - 1) *
                (Math.Pow(effectiveRatio, 2 / gamma) - Math.Pow(effectiveRatio, (gamma + 1) / gamma)));
            Near(point.MassFlowKilogramsPerSecond, Cd * Area * flux, 1e-15);
            Near(point.ThroatPressurePascals, pressure * effectiveRatio, 1e-7);
            Near(point.ExitTemperatureKelvin, temperature * Math.Pow(ratio, (gamma - 1) / gamma), 1e-10);
        }
        Ok(law.Evaluate(pressure, temperature, 1, pressure * .2, 1, out var low)); Ok(law.Evaluate(pressure, temperature, 1, pressure * .3, 1, out var high));
        Require(low.Choked && high.Choked); Near(low.MassFlowKilogramsPerSecond, high.MassFlowKilogramsPerSecond, 1e-17);
    }
    private static void Mixture()
    {
        var law = Law(); const double pressure = 8e5, temperature = 500, ratio = .08;
        foreach (double quality in new[] { .0001, .01, .1, .5, .9 })
        {
            double cp = (1 - quality) * Cl + quality * (Cv + R), exponent = quality * R / cp, liquidVolume = (1 - quality) / Density;
            double Flux(double r)
            {
                double t = temperature * Math.Pow(r, exponent), v = liquidVolume + quality * R * t / (pressure * r);
                double drop = cp * (temperature - t) + liquidVolume * pressure * (1 - r);
                return Math.Sqrt(2 * drop) / v;
            }
            // Independent pressure-ratio golden-section maximum of the direct energy law.
            double lo = ratio, hi = 1, golden = (Math.Sqrt(5) - 1) / 2, a = hi - golden * (hi - lo), b = lo + golden * (hi - lo);
            for (int i = 0; i < 120; ++i)
                if (Flux(a) > Flux(b)) { hi = b; b = a; a = hi - golden * (hi - lo); }
                else { lo = a; a = b; b = lo + golden * (hi - lo); }
            double maximumAt = (lo + hi) / 2; if (Flux(ratio) >= Flux(maximumAt)) maximumAt = ratio;
            Ok(law.Evaluate(pressure, temperature, quality, pressure * ratio, 1, out var point));
            Near(point.MassFlowKilogramsPerSecond, Cd * Area * Flux(maximumAt), 1e-12);
            Near(point.ThroatPressurePascals, pressure * maximumAt, .2);
            if (point.Choked)
            {
                double rc = point.ThroatPressurePascals / pressure, tc = temperature * Math.Pow(rc, exponent), v = liquidVolume + quality * R * tc / point.ThroatPressurePascals;
                double soundSquared = v * v * point.ThroatPressurePascals * point.ThroatPressurePascals / (quality * R * tc * (1 - exponent));
                double velocitySquared = 2 * (cp * (temperature - tc) + liquidVolume * pressure * (1 - rc));
                Near(velocitySquared, soundSquared, 2e-7);
            }
        }
    }
    private static void Contracts()
    {
        var law = Law();
        foreach (double quality in new[] { 0d, .01, .5, 1d })
        {
            Ok(law.Evaluate(8e5, 500, quality, 2e5, 1, out var p));
            Near(p.StagnationEnthalpyJoulesPerKilogram, p.ExitInternalEnergyJoulesPerKilogram + p.ExitPressureWorkJoulesPerKilogram + p.KineticEnergyJoulesPerKilogram, 2e-9);
            double cp = (1 - quality) * Cl + quality * (Cv + R);
            Near(cp * Math.Log(p.ExitTemperatureKelvin / 500) - quality * R * Math.Log(.25), 0, 1e-10);
            Near(p.JetMomentumNewtons, p.MassFlowKilogramsPerSecond * p.ExitVelocityMetersPerSecond, 0);
            Near(p.VaporMassFraction, quality, 0);
        }
        Require(law.Evaluate(8e5, 100, .5, 2e5, 1, out var failed) == FuelNozzleStatus.TemperatureOutOfRange && failed == default);
        Require(law.Evaluate(8e5, 500, 1, 1, 1, out failed) == FuelNozzleStatus.TemperatureOutOfRange && failed == default);
        Require(law.Evaluate(8e5, 500, 1.1, 1e5, 1, out failed) == FuelNozzleStatus.InvalidInput && failed == default);
        Ok(law.Evaluate(8e5, 500, .5, 9e5, 1, out var closed)); Near(closed.MassFlowKilogramsPerSecond, 0, 0);
        for (int i = 0; i < 30; ++i) Ok(law.Evaluate(8e5, 500, .5, 1e5, 1, out _));
        long allocated = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 100; ++i) Ok(law.Evaluate(8e5, 500, .5, 1e5, 1, out _));
        Require(GC.GetAllocatedBytesForCurrentThread() == allocated, "Frozen nozzle allocated.");
    }
}
