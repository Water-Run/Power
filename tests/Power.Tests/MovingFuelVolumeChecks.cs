// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class MovingFuelVolumeChecks
{
    private const double R = 287, Cv = 717.5, Cl = 2000, Offset = -813000, Density = 750, C = 5e-13, V0 = 8e-7;
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("moving fuel volume / incompressible reversible pressure work / unchanged constitutive wrappers", Liquid),
        ("moving fuel volume / independent ideal-vapor adiabat with elastic wall", Vapor),
        ("moving fuel volume / independent two-phase entropy inversion / closed work", Phase),
        ("moving fuel volume / small travel closed cycles validity and zero allocations", Limits)
    ];
    internal static CompliantFuelVolume Law() => new(new(new(Cl, Cv, 400, 300000, 0), R, 100000, 200, 600), V0, C, Density, 20e6);
    private static void Ok(FuelVolumeStatus status) { if (status != FuelVolumeStatus.Success) Require(false, status.ToString()); }
    private static void Liquid()
    {
        var law = Law(); Ok(law.Evaluate(300, .00065, out var initial));
        Ok(law.EvaluateAtReferenceVolume(300, .00065, V0, out var same)); Require(initial == same);
        foreach (double next in new[] { V0 - 1e-8, V0 + 1e-8 })
        {
            Ok(law.AdiabaticReferenceVolume(initial.MassKilograms, initial.StoredEnergyJoules, V0, next, out var result));
            double p = (initial.MassKilograms / Density - next) / C;
            Near(result.Fluid.PressurePascals, p, 1e-8); Near(result.Fluid.TemperatureKelvin, 300, 1e-10);
            Near(result.EnergyChangeJoules, -(next - V0) * (initial.PressurePascals + p) / 2, 1e-14);
            Near(result.Fluid.StoredEnergyJoules, initial.StoredEnergyJoules + result.EnergyChangeJoules, 0);
            Near(result.EntropyResidualJoulesPerKelvin, 0, 1e-13);
        }
    }
    private static void Vapor()
    {
        var law = Law(); Ok(law.Evaluate(500, 1e-8, out var initial));
        foreach (double next in new[] { V0 * .8, V0 * 1.25 })
        {
            const double gamma = 1.4; double invariant = initial.PressurePascals * Math.Pow(initial.VolumeCubicMeters, gamma);
            double lo = 0, hi = initial.PressurePascals * 10;
            for (int i = 0; i < 80; ++i) { double p = (lo + hi) / 2; if (p * Math.Pow(next + C * p, gamma) > invariant) hi = p; else lo = p; }
            double pressure = (lo + hi) / 2, volume = next + C * pressure, t = pressure * volume / (initial.MassKilograms * R);
            double energy = initial.MassKilograms * Cv * t + .5 * C * pressure * pressure;
            Ok(law.AdiabaticReferenceVolume(initial.MassKilograms, initial.StoredEnergyJoules, V0, next, out var result));
            Require(result.Fluid.Phase == FuelVolumePhase.Vapor);
            Near(result.Fluid.TemperatureKelvin, t, 1e-9); Near(result.Fluid.PressurePascals, pressure, 1e-7);
            Near(result.EnergyChangeJoules, energy - initial.StoredEnergyJoules, 1e-13); Near(result.EntropyResidualJoulesPerKelvin, 0, 1e-15);
        }
    }
    private static (double S, double E, double P) Reference(double temperature, double mass, double referenceVolume)
    {
        double p0 = 100000 * Math.Pow(temperature / 400, (Cv + R - Cl) / R) * Math.Exp(Offset / R * (1 / temperature - 1 / 400d));
        double lo = p0, hi = p0 * Math.E;
        for (int i = 0; i < 75; ++i)
        { double p = (lo + hi) / 2; if (Math.Log(p / p0) > p / (Density * R * temperature)) hi = p; else lo = p; }
        double pressure = (lo + hi) / 2, volume = referenceVolume + C * pressure;
        double vapor;
        if (mass >= Density * volume) { pressure = (mass / Density - referenceVolume) / C; vapor = 0; }
        else if (mass <= pressure * volume / (R * temperature))
        { pressure = (Math.Sqrt(referenceVolume * referenceVolume + 4 * C * mass * R * temperature) - referenceVolume) / (2 * C); vapor = mass; }
        else vapor = (Density * volume - mass) / (Density * R * temperature / pressure - 1);
        double entropy = (mass - vapor) * Cl * Math.Log(temperature / 400) + vapor * ((Cv + R) * Math.Log(temperature / 400) - R * Math.Log(pressure / 100000) + 300000 / 400d + R);
        double energy = (mass - vapor) * (Cl * temperature + Offset) + vapor * Cv * temperature + .5 * C * pressure * pressure;
        return (entropy, energy, pressure);
    }
    private static void Phase()
    {
        var law = Law(); const double mass = .0003;
        foreach (double t in new[] { 280d, 400d, 520d })
        {
            var initial = Reference(t, mass, V0);
            foreach (double next in new[] { V0 * .75, V0 * 1.25 })
            {
                double lo = 200, hi = 600;
                for (int i = 0; i < 65; ++i) { double mid = (lo + hi) / 2; if (Reference(mid, mass, next).S > initial.S) hi = mid; else lo = mid; }
                double expectedT = (lo + hi) / 2; var expected = Reference(expectedT, mass, next);
                Ok(law.AdiabaticReferenceVolume(mass, initial.E, V0, next, out var result));
                Require(result.Fluid.Phase == FuelVolumePhase.LiquidVapor);
                Near(result.Fluid.TemperatureKelvin, expectedT, 1e-9); Near(result.Fluid.PressurePascals, expected.P, 1e-6);
                Near(result.EnergyChangeJoules, expected.E - initial.E, 2e-10); Near(result.EntropyResidualJoulesPerKelvin, 0, 2e-12);
            }
        }
    }
    private static void Limits()
    {
        var law = Law(); Ok(law.Evaluate(400, .0003, out var initial));
        foreach (double relative in new[] { 1e-6, 1e-8, 1e-10, 1e-12, 1e-14 })
        {
            double next = V0 * (1 + relative); Ok(law.AdiabaticReferenceVolume(initial.MassKilograms, initial.StoredEnergyJoules, V0, next, out var move));
            Near(move.MeanPressurePascals, initial.PressurePascals, .5);
        }
        double volume = V0, energy = initial.StoredEnergyJoules;
        for (int n = 1; n <= 1000; ++n)
        {
            double next = n == 1000 ? V0 : V0 + .1 * V0 * Math.Sin(n * 2 * Math.PI / 1000);
            Ok(law.AdiabaticReferenceVolume(initial.MassKilograms, energy, volume, next, out var result)); energy = result.Fluid.StoredEnergyJoules; volume = next;
        }
        Near(energy, initial.StoredEnergyJoules, 2e-9);
        Require(law.AdiabaticReferenceVolume(.0003, initial.StoredEnergyJoules, V0, -1, out var bad) == FuelVolumeStatus.InvalidInput && bad == default);
        for (int n = 0; n < 20; ++n) Ok(law.AdiabaticReferenceVolume(initial.MassKilograms, initial.StoredEnergyJoules, V0, V0 * 1.01, out _));
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int n = 0; n < 50; ++n) Ok(law.AdiabaticReferenceVolume(initial.MassKilograms, initial.StoredEnergyJoules, V0, V0 * 1.01, out _));
        Require(GC.GetAllocatedBytesForCurrentThread() == allocated, "Moving-volume work allocated.");
    }
}
