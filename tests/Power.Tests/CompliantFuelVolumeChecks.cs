// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class CompliantFuelVolumeChecks
{
    private const double R = 287, Cv = 717.5, Cl = 2000, Tr = 400, Pref = 100000, Latent = 300000;
    private const double Density = 750, V0 = 8e-7, Compliance = 5e-13, Limit = 20_000_000;
    private const double Offset = (Cv - Cl) * Tr - Latent;
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("compliant fuel / pure-fluid chemical potentials and Clapeyron relation", Coexistence),
        ("compliant fuel / three phase branches / caloric entropy and volume derivatives", Branches),
        ("compliant fuel / mass-energy flash / phase boundaries and explicit limits", RoundTrip),
        ("compliant fuel / independent entropy maximization with elastic wall", EntropyMaximum),
        ("compliant fuel / open-system Gibbs derivatives include wall energy", Gibbs),
        ("compliant fuel / finite-bath heat / conservation second law and first-order refinement", HeatReference),
        ("compliant fuel / evaporation condensation / heat direction and finite bath bounds", HeatLimits),
        ("compliant fuel / rigid ideal-gas and incompressible density limits / extensivity", Limits),
        ("compliant fuel / actionable rejections / immutable concurrent allocation-free closure", Contracts)
    ];
    private static FuelPhaseEquilibrium Material(double pressure = Pref) => new(new(Cl, Cv, Tr, Latent, 0), R, pressure, 200, 600);
    private static CompliantFuelVolume Law(double volume = V0, double compliance = Compliance, double density = Density, double limit = Limit) =>
        new(Material(), volume, compliance, density, limit);
    private static void Ok(FuelVolumeStatus status)
    { if (status != FuelVolumeStatus.Success) Require(false, status.ToString()); }
    private static double P0(double t) => Pref * Math.Pow(t / Tr, (Cv + R - Cl) / R) * Math.Exp(Offset / R * (1 / t - 1 / Tr));
    private static double Sl(double t) => Cl * Math.Log(t / Tr);
    private static double Sv(double t, double p) => (Cv + R) * Math.Log(t / Tr) - R * Math.Log(p / Pref) + Latent / Tr + R;

    // Independent pressure-space bisection, rather than the production log-Newton solve.
    private static double ReferenceSaturation(double t)
    {
        double reference = P0(t), lo = reference, hi = Math.E * reference;
        for (int i = 0; i < 75; ++i)
        {
            double p = (lo + hi) / 2;
            if (Math.Log(p / reference) > p / (Density * R * t)) hi = p; else lo = p;
        }
        return (lo + hi) / 2;
    }

    private static void Coexistence()
    {
        var law = Law();
        foreach (double t in new[] { 200d, 220, 300, 400, 550, 600 })
        {
            Ok(law.Saturation(t, out var sat));
            Near(sat.PressurePascals, ReferenceSaturation(t), 2e-8);
            double hl = Cl * t + Offset + sat.PressurePascals / Density, hv = (Cv + R) * t;
            Near(hv - t * Sv(t, sat.PressurePascals), hl - t * Sl(t), 1e-8);
            Near(sat.LatentEnthalpyJoulesPerKilogram, hv - hl, 1e-9);
            Near(Sv(t, sat.PressurePascals) - Sl(t), sat.LatentEnthalpyJoulesPerKilogram / t, 1e-10);
            Near(sat.PressureDerivativePascalsPerKelvin,
                (hv - hl) / (t * (R * t / sat.PressurePascals - 1 / Density)), 1e-10);
            if (t is > 200 and < 600)
                Near(sat.PressureDerivativePascalsPerKelvin, (ReferenceSaturation(t + .001) - ReferenceSaturation(t - .001)) / .002, 2e-7);
            Require(sat.PressurePascals > P0(t) && sat.VaporSpecificVolumeCubicMetersPerKilogram > sat.LiquidSpecificVolumeCubicMetersPerKilogram);
        }
    }

    private static void CheckPoint(CompliantFuelPoint point)
    {
        double t = point.TemperatureKelvin, p = point.PressurePascals, liquid = point.LiquidMassKilograms, vapor = point.VaporMassKilograms;
        Near(liquid + vapor, point.MassKilograms, 1e-18);
        Near(liquid / Density + vapor * R * t / p, point.VolumeCubicMeters, 1e-20);
        Near(point.VolumeCubicMeters, V0 + Compliance * p, 1e-20);
        Near(point.StoredEnergyJoules, liquid * (Cl * t + Offset) + vapor * Cv * t + .5 * Compliance * p * p, 1e-11);
        Near(point.EntropyJoulesPerKelvin, liquid * Sl(t) + vapor * Sv(t, p), 1e-13);
        Near(point.SpecificEnthalpyJoulesPerKilogram,
            (point.FluidInternalEnergyJoules + p * point.VolumeCubicMeters) / point.MassKilograms, 1e-8);
        Require(point.HeatCapacityJoulesPerKelvin > 0 && point.VaporMassFraction is >= 0 and <= 1 && point.VaporVolumeFraction is >= 0 and <= 1.000000000000001);
    }

    private static void Branches()
    {
        var law = Law();
        foreach (var (mass, expected) in new[] { (.001, FuelVolumePhase.Liquid), (.0003, FuelVolumePhase.LiquidVapor), (1e-8, FuelVolumePhase.Vapor) })
        {
            foreach (double t in new[] { 250d, 350, 450, 550 })
            {
                Ok(law.Evaluate(t, mass, out var point)); CheckPoint(point); Require(point.Phase == expected);
                Ok(law.Evaluate(t - .001, mass, out var low)); Ok(law.Evaluate(t + .001, mass, out var high));
                Near((high.StoredEnergyJoules - low.StoredEnergyJoules) / .002, point.HeatCapacityJoulesPerKelvin, 2e-8);
                Near(t * (high.EntropyJoulesPerKelvin - low.EntropyJoulesPerKelvin) / .002, point.HeatCapacityJoulesPerKelvin, 2e-8);
                if (expected == FuelVolumePhase.Liquid)
                { Near(point.PressurePascals, (mass / Density - V0) / Compliance, 1e-7); Require(point.PressurePascals > ReferenceSaturation(t)); }
                else if (expected == FuelVolumePhase.Vapor)
                    Require(point.PressurePascals < ReferenceSaturation(t));
                else Near(point.PressurePascals, ReferenceSaturation(t), 2e-8);
            }
        }
    }

    private static void RoundTrip()
    {
        var law = Law();
        foreach (double t in new[] { 200d, 200.00001, 250, 350, 400, 500, 599.99999, 600 })
        {
            Ok(law.Saturation(t, out var saturation));
            double volume = V0 + Compliance * saturation.PressurePascals;
            double wet = Density * volume, dry = volume / saturation.VaporSpecificVolumeCubicMetersPerKilogram;
            foreach (double mass in new[] { 1e-10, dry * (1 - 1e-7), dry, dry * (1 + 1e-7), .0003, wet * (1 - 1e-7), wet, wet * (1 + 1e-7), .003, law.MaximumMassKilograms })
            {
                Ok(law.Evaluate(t, mass, out var original)); Ok(law.Flash(mass, original.StoredEnergyJoules, out var flashed));
                Near(flashed.TemperatureKelvin, t, 1e-9); Near(flashed.StoredEnergyJoules, original.StoredEnergyJoules, 0);
                Near(flashed.VaporMassKilograms, original.VaporMassKilograms, 1e-15); CheckPoint(flashed);
                Require(flashed == Flash(law, mass, original.StoredEnergyJoules), "Flash was not deterministic.");
            }
            foreach (double boundary in new[] { dry, wet })
            {
                Ok(law.Evaluate(t, boundary * (1 - 1e-9), out var left)); Ok(law.Evaluate(t, boundary * (1 + 1e-9), out var right));
                Near(left.StoredEnergyJoules, right.StoredEnergyJoules, 2e-6); Near(left.PressurePascals, right.PressurePascals, .003);
            }
        }
    }

    private static CompliantFuelPoint Flash(CompliantFuelVolume law, double mass, double energy)
    { Ok(law.Flash(mass, energy, out var point)); return point; }

    // For each trial phase mass, eliminate temperature from energy and solve the
    // resulting quadratic mechanical balance. No saturation relation is used here.
    private static (double T, double P, double S) Trial(double mass, double vapor, double energy)
    {
        double liquid = mass - vapor, capacity = liquid * Cl + vapor * Cv;
        double p;
        if (vapor == 0) p = (mass / Density - V0) / Compliance;
        else
        {
            double b = V0 - liquid / Density, a = Compliance * (1 + vapor * R / (2 * capacity));
            double d = vapor * R / capacity * (energy - liquid * Offset);
            double root = Math.Sqrt(b * b + 4 * a * d);
            p = b >= 0 ? 2 * d / (b + root) : (root - b) / (2 * a);
        }
        double t = (energy - liquid * Offset - .5 * Compliance * p * p) / capacity;
        if (!double.IsFinite(t) || t is < 200 or > 600 || p <= 0 || p > Limit || p / (Density * R * t) >= 1)
            return (t, p, double.NegativeInfinity);
        return (t, p, liquid * Sl(t) + (vapor == 0 ? 0 : vapor * Sv(t, p)));
    }

    private static void EntropyMaximum()
    {
        var law = Law();
        foreach (var (t, mass) in new[] { (300d, .001), (420d, .0003), (530d, .0002), (350d, 1e-8) })
        {
            Ok(law.Evaluate(t, mass, out var original)); double energy = original.StoredEnergyJoules;
            int best = 0; double maximum = double.NegativeInfinity;
            for (int i = 0; i <= 512; ++i)
            { double s = Trial(mass, mass * i / 512, energy).S; if (s > maximum) { best = i; maximum = s; } }
            double lo = mass * Math.Max(0, best - 1) / 512, hi = mass * Math.Min(512, best + 1) / 512;
            double ratio = (Math.Sqrt(5) - 1) / 2, a = hi - ratio * (hi - lo), b = lo + ratio * (hi - lo);
            for (int i = 0; i < 110; ++i)
                if (Trial(mass, a, energy).S < Trial(mass, b, energy).S) { lo = a; a = b; b = lo + ratio * (hi - lo); }
                else { hi = b; b = a; a = hi - ratio * (hi - lo); }
            double vapor = (lo + hi) / 2;
            if (Trial(mass, 0, energy).S >= Trial(mass, vapor, energy).S) vapor = 0;
            if (Trial(mass, mass, energy).S >= Trial(mass, vapor, energy).S) vapor = mass;
            var result = Trial(mass, vapor, energy);
            Near(result.S, original.EntropyJoulesPerKelvin, 1e-10);
            // An entropy maximum is flat: binary64 entropy rounding bounds its
            // location through curvature, rather than through root-solver precision.
            double distance = Math.Min(original.VaporMassKilograms, mass - original.VaporMassKilograms) * .001;
            double tolerance = 1e-12 * mass;
            if (distance > 0)
            {
                double curvature = (2 * original.EntropyJoulesPerKelvin - Trial(mass, original.VaporMassKilograms - distance, energy).S -
                    Trial(mass, original.VaporMassKilograms + distance, energy).S) / (distance * distance);
                Require(curvature > 0);
                tolerance += Math.Sqrt(32 * 2.2204460492503131e-16 * Math.Abs(original.EntropyJoulesPerKelvin) / curvature);
            }
            Near(vapor, original.VaporMassKilograms, tolerance);
            Near(result.T, t, 2e-5);
            for (int i = 0; i <= 256; ++i)
                Require(Trial(mass, mass * i / 256, energy).S <= original.EntropyJoulesPerKelvin + 2e-12, "Flash lowered total entropy.");
        }
        // Superheated liquid at low vessel pressure must evaporate rather than stay metastable.
        const double initialMass = .00065, initialT = 500;
        double pressure = (initialMass / Density - V0) / Compliance;
        double initialEnergy = initialMass * (Cl * initialT + Offset) + .5 * Compliance * pressure * pressure;
        var flashed = Flash(law, initialMass, initialEnergy);
        Require(flashed.Phase == FuelVolumePhase.LiquidVapor && flashed.VaporMassKilograms > 0);
        Require(flashed.EntropyJoulesPerKelvin > initialMass * Sl(initialT));
    }

    private static void Gibbs()
    {
        var law = Law();
        foreach (double mass in new[] { .001, .0003, 1e-8 })
        {
            Ok(law.Evaluate(400, mass, out var point));
            double de = mass * .1, dm = mass * 1e-6;
            var higher = Flash(law, mass, point.StoredEnergyJoules + de); var lower = Flash(law, mass, point.StoredEnergyJoules - de);
            Near((higher.EntropyJoulesPerKelvin - lower.EntropyJoulesPerKelvin) / (2 * de), 1 / 400d, 1e-10);
            higher = Flash(law, mass + dm, point.StoredEnergyJoules); lower = Flash(law, mass - dm, point.StoredEnergyJoules);
            double chemicalPotential = point.SpecificEnthalpyJoulesPerKilogram - 400 * point.EntropyJoulesPerKelvin / mass;
            Near(-400 * (higher.EntropyJoulesPerKelvin - lower.EntropyJoulesPerKelvin) / (2 * dm), chemicalPotential, .01);
        }
    }

    private static (double Energy, double Vapor, double Pressure) ReferencePoint(double t, double mass)
    {
        double p = ReferenceSaturation(t), volume = V0 + Compliance * p;
        double vapor;
        if (mass >= Density * volume) { p = (mass / Density - V0) / Compliance; vapor = 0; }
        else if (mass <= p * volume / (R * t))
        { p = (-V0 + Math.Sqrt(V0 * V0 + 4 * Compliance * mass * R * t)) / (2 * Compliance); vapor = mass; }
        else vapor = (Density * volume - mass) / (Density * R * t / p - 1);
        return ((mass - vapor) * (Cl * t + Offset) + vapor * Cv * t + .5 * Compliance * p * p, vapor, p);
    }

    private static double ReferenceTemperature(double energy, double mass)
    {
        double lo = 200, hi = 600;
        for (int i = 0; i < 52; ++i)
        { double t = (lo + hi) / 2; if (ReferencePoint(t, mass).Energy > energy) hi = t; else lo = t; }
        return (lo + hi) / 2;
    }

    private static void HeatReference()
    {
        var law = Law(); const double mass = .00065, wallCapacity = 3, conductance = .6, duration = 6;
        Ok(law.Evaluate(300, mass, out var initial));
        double Rate(double energy) => conductance * ((600 + (initial.StoredEnergyJoules - energy) / wallCapacity) - ReferenceTemperature(energy, mass));
        double Integrate(int steps)
        {
            double energy = initial.StoredEnergyJoules, h = duration / steps;
            for (int i = 0; i < steps; ++i)
            {
                double a = Rate(energy), b = Rate(energy + h / 2 * a), c = Rate(energy + h / 2 * b), d = Rate(energy + h * c);
                energy += h / 6 * (a + 2 * b + 2 * c + d);
            }
            return energy;
        }
        double reference = Integrate(2000); Near(reference, Integrate(4000), 2e-6);
        double previous = 0; var errors = new List<double>();
        foreach (int steps in new[] { 32, 64, 128, 256 })
        {
            double energy = initial.StoredEnergyJoules, wall = 600; var point = initial;
            for (int i = 0; i < steps; ++i)
            {
                Ok(law.AdvanceHeat(mass, energy, wall, wallCapacity, conductance, duration / steps, out var transfer));
                point = transfer.Fluid; energy = point.StoredEnergyJoules; wall = transfer.WallTemperatureKelvin;
                Require(transfer.EntropyChangeJoulesPerKelvin >= -1e-13);
                Near(transfer.EnergyResidualJoules, 0, 5e-13);
            }
            Require(point.Phase == FuelVolumePhase.LiquidVapor && point.VaporMassKilograms > 0);
            Near(energy + wallCapacity * wall, initial.StoredEnergyJoules + wallCapacity * 600, 2e-10);
            double error = Math.Abs(energy - reference);
            if (previous > 0) Require(previous / error is > 1.8 and < 2.2, $"Compliant volume heat refinement: {previous:R} / {error:R}");
            previous = error; errors.Add(error);
        }
        Console.WriteLine("Compliant fuel finite-bath energy errors (J): " + string.Join(", ", errors.Select(v => v.ToString("R"))));
    }

    private static void HeatLimits()
    {
        var law = Law(); const double mass = .0003;
        Ok(law.Evaluate(400, mass, out var initial));
        foreach (double bath in new[] { 250d, 400d, 550d })
        {
            foreach (double h in new[] { 0d, .001, 1d, 1e8 })
            {
                Ok(law.AdvanceHeat(mass, initial.StoredEnergyJoules, bath, 1.5, .2, h, out var transfer));
                var point = transfer.Fluid;
                Require(point.TemperatureKelvin >= Math.Min(bath, 400) - 1e-10 && point.TemperatureKelvin <= Math.Max(bath, 400) + 1e-10);
                Require(transfer.WallTemperatureKelvin >= Math.Min(bath, 400) - 1e-10 && transfer.WallTemperatureKelvin <= Math.Max(bath, 400) + 1e-10);
                Require(transfer.EntropyChangeJoulesPerKelvin >= -1e-13); Near(transfer.EnergyResidualJoules, 0, 1e-12);
                if (h == 0) { Near(transfer.HeatFromWallJoules, 0, 0); Near(point.StoredEnergyJoules, initial.StoredEnergyJoules, 0); }
                else if (bath > 400) Require(point.VaporMassKilograms > initial.VaporMassKilograms && transfer.HeatFromWallJoules > 0);
                else if (bath < 400) Require(point.VaporMassKilograms < initial.VaporMassKilograms && transfer.HeatFromWallJoules < 0);
            }
        }
    }

    private static void Limits()
    {
        var rigid = Law(compliance: 1e-20); var tank = new FiniteTankFuelEquilibrium(Material(), V0, Density);
        foreach (double mass in new[] { .0003, 1e-8 })
        {
            Ok(rigid.Evaluate(400, mass, out var point)); Require(tank.TryPoint(400, mass, 0, out var reference));
            Near(point.PressurePascals, reference.PressurePascals, 1e-5); Near(point.VaporMassKilograms, reference.VaporMassKilograms, 1e-15);
            Near(point.FluidInternalEnergyJoules, reference.ThermalEnergyJoules, 1e-9);
            if (point.Phase == FuelVolumePhase.Vapor) Near(point.PressurePascals, mass * R * 400 / V0, 1e-7);
        }
        Ok(Law(density: 1e15).Saturation(400, out var dense)); Near(dense.PressurePascals, Pref, 1e-7);
        var law = Law(); Ok(law.Evaluate(400, .0003, out var original));
        foreach (double factor in new[] { 1e-6, 1e6 })
        {
            var scaled = Law(volume: V0 * factor, compliance: Compliance * factor);
            Ok(scaled.Evaluate(400, .0003 * factor, out var point));
            Near(point.PressurePascals, original.PressurePascals, 1e-7);
            Near(point.StoredEnergyJoules / factor, original.StoredEnergyJoules, 1e-10);
            Near(point.EntropyJoulesPerKelvin / factor, original.EntropyJoulesPerKelvin, 1e-13);
            Near(point.VaporMassFraction, original.VaporMassFraction, 1e-14);
        }
    }

    private static void Contracts()
    {
        Throws<ArgumentNullException>(() => new CompliantFuelVolume(null!, V0, Compliance, Density, Limit));
        foreach (double bad in new[] { -1d, 0, double.NaN, double.PositiveInfinity })
        {
            Throws<ArgumentException>(() => Law(volume: bad)); Throws<ArgumentException>(() => Law(compliance: bad));
            Throws<ArgumentException>(() => Law(density: bad)); Throws<ArgumentException>(() => Law(limit: bad));
        }
        Throws<ArgumentException>(() => Law(limit: Density * R * 200));
        Throws<ArgumentException>(() => Law(limit: 100000));
        Throws<ArgumentException>(() => Law(compliance: 1e-30));
        Throws<ArgumentException>(() => new CompliantFuelVolume(Material(1e8), V0, Compliance, Density, Limit));
        var law = Law();
        Require(law.Evaluate(400, 0, out var failed) == FuelVolumeStatus.MassOutOfRange && failed == default);
        Require(law.Evaluate(400, law.MaximumMassKilograms * 1.001, out failed) == FuelVolumeStatus.MassOutOfRange && failed == default);
        Require(law.Evaluate(199, .0003, out failed) == FuelVolumeStatus.TemperatureOutOfRange && failed == default);
        Require(law.Flash(.0003, double.NaN, out failed) == FuelVolumeStatus.InvalidInput && failed == default);
        Ok(law.Evaluate(200, .0003, out var lower)); Ok(law.Evaluate(600, .0003, out var upper));
        Require(law.Flash(.0003, lower.StoredEnergyJoules - 1, out failed) == FuelVolumeStatus.EnergyOutOfRange && failed == default);
        Require(law.Flash(.0003, upper.StoredEnergyJoules + 1, out failed) == FuelVolumeStatus.EnergyOutOfRange && failed == default);
        Require(law.AdvanceHeat(.0003, lower.StoredEnergyJoules, 300, 0, 1, .1, out var heat) == FuelVolumeStatus.InvalidInput && heat == default);
        Require(law.AdvanceHeat(.0003, upper.StoredEnergyJoules, 1000, 1, 10, 100, out heat) == FuelVolumeStatus.EnergyOutOfRange && heat == default);
        Ok(law.Evaluate(400, .0003, out var point)); var expected = Flash(law, .0003, point.StoredEnergyJoules);
        Parallel.For(0, 32, _ => Require(Flash(law, .0003, point.StoredEnergyJoules) == expected));
        for (int i = 0; i < 40; ++i) Ok(law.AdvanceHeat(.0003, point.StoredEnergyJoules, 450, 2, .1, .01, out _));
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; ++i)
        {
            Ok(law.Evaluate(400, .0003, out _)); Ok(law.Flash(.0003, point.StoredEnergyJoules, out _));
            Ok(law.AdvanceHeat(.0003, point.StoredEnergyJoules, 450, 2, .1, .01, out _));
        }
        Require(GC.GetAllocatedBytesForCurrentThread() == allocated, "Thermodynamic closure allocated.");
    }
}
