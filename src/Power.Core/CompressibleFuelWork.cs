// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public readonly record struct CompressibleFuelWorkTransfer(CompressibleFuelPoint Fluid,
    double EnergyChangeJoules, double MeanPressurePascals, double EntropyResidualJoulesPerKelvin);

public sealed partial class CompressibleFuel
{
    /// <summary>Closed reversible volume work at fixed mass and entropy, including liquid cold compression energy.</summary>
    public FuelVolumeStatus AdiabaticVolume(double massKilograms, double internalEnergyJoules,
        double oldVolumeCubicMeters, double newVolumeCubicMeters, out CompressibleFuelWorkTransfer transfer)
    {
        transfer = default;
        if (!GearReference.Finite(massKilograms, internalEnergyJoules, oldVolumeCubicMeters, newVolumeCubicMeters) ||
            massKilograms <= 0 || oldVolumeCubicMeters <= 0 || newVolumeCubicMeters <= 0) return FuelVolumeStatus.InvalidInput;
        if (!Flash(massKilograms / oldVolumeCubicMeters, internalEnergyJoules / massKilograms, out var initial)) return FuelVolumeStatus.EnergyOutOfRange;
        double delta = newVolumeCubicMeters - oldVolumeCubicMeters;
        if (delta == 0) { transfer = new(initial, 0, initial.PressurePascals, 0); return FuelVolumeStatus.Success; }
        double density = massKilograms / newVolumeCubicMeters, temperature = initial.TemperatureKelvin, target = initial.SpecificEntropyJoulesPerKilogramKelvin;
        if (!Evaluate(density, temperature, out var result)) return FuelVolumeStatus.EnergyOutOfRange;
        if (initial.Phase != FuelVolumePhase.Liquid || result.Phase != FuelVolumePhase.Liquid)
        {
            double lo = Material.MinimumTemperatureKelvin, hi = Material.MaximumTemperatureKelvin;
            if (!Evaluate(density, lo, out var lower) || !Evaluate(density, hi, out var upper)) return FuelVolumeStatus.EnergyOutOfRange;
            if (target < lower.SpecificEntropyJoulesPerKilogramKelvin || target > upper.SpecificEntropyJoulesPerKilogramKelvin) return FuelVolumeStatus.EntropyOutOfRange;
            bool converged = false;
            for (int iteration = 0; iteration < 80; ++iteration)
            {
                if (!Evaluate(density, temperature, out result)) return FuelVolumeStatus.EnergyOutOfRange;
                double error = result.SpecificEntropyJoulesPerKilogramKelvin - target;
                double correction = error * temperature / result.IsochoricHeatCapacityJoulesPerKilogramKelvin;
                if (!Numeric.Finite(correction)) return FuelVolumeStatus.NoConvergence;
                if (Math.Abs(correction) <= 32 * GearReference.Epsilon * Math.Max(1, temperature)) { converged = true; break; }
                if (error > 0) hi = temperature; else lo = temperature;
                double next = temperature - correction; temperature = next > lo && next < hi ? next : (lo + hi) / 2;
            }
            if (!converged) return FuelVolumeStatus.NoConvergence;
        }
        double work, ratio = delta / oldVolumeCubicMeters;
        var caloric = Material.Caloric;
        if (initial.Phase == FuelVolumePhase.Liquid && result.Phase == FuelVolumePhase.Liquid)
        {
            // Exact integral -integral(p dV). Evaluate r-log(1+r) without cancellation.
            double remainder = ratio - Numeric.Log1p(ratio);
            if (Math.Abs(ratio) < 1e-3)
            {
                remainder = 0; double power = ratio * ratio;
                for (int n = 2; n <= 10; ++n) { remainder += (n % 2 == 0 ? 1 : -1) * power / n; power *= ratio; }
            }
            work = -initial.PressurePascals * delta + massKilograms * LiquidBulkModulusPascals / ReferenceLiquidDensityKilogramsPerCubicMeter * remainder;
        }
        else if (initial.Phase == FuelVolumePhase.Vapor && result.Phase == FuelVolumePhase.Vapor)
            work = massKilograms * caloric.VaporSpecificHeatJoulesPerKilogramKelvin * initial.TemperatureKelvin *
                Numeric.Expm1(-Material.GasConstantJoulesPerKilogramKelvin / caloric.VaporSpecificHeatJoulesPerKilogramKelvin * Numeric.Log1p(ratio));
        else
        {
            double qualityChange = result.VaporMassFraction - initial.VaporMassFraction;
            double capacity = (1 - initial.VaporMassFraction) * caloric.LiquidSpecificHeatJoulesPerKilogramKelvin + initial.VaporMassFraction * caloric.VaporSpecificHeatJoulesPerKilogramKelvin;
            double oldCold = ColdEnergy(initial.PressurePascals), nextCold = ColdEnergy(result.PressurePascals);
            work = massKilograms * (capacity * (result.TemperatureKelvin - initial.TemperatureKelvin) + qualityChange * Material.LatentInternalEnergy(result.TemperatureKelvin) +
                (1 - initial.VaporMassFraction) * (nextCold - oldCold) - qualityChange * nextCold);
            double scale = massKilograms * (capacity * initial.TemperatureKelvin + Material.LatentInternalEnergy(initial.TemperatureKelvin) + oldCold);
            if (Math.Abs(work) <= 1024 * GearReference.Epsilon * scale && Math.Abs(ratio) <= Math.Sqrt(GearReference.Epsilon) && initial.Phase == result.Phase)
                work = -delta * (.5 * initial.PressurePascals + .5 * result.PressurePascals);
        }
        double mean = -work / delta;
        if (!Numeric.Finite(work) || !Numeric.Finite(mean) || mean <= 0) return FuelVolumeStatus.NoConvergence;
        if (!Flash(density, (internalEnergyJoules + work) / massKilograms, out result)) return FuelVolumeStatus.EnergyOutOfRange;
        double residual = massKilograms * (result.SpecificEntropyJoulesPerKilogramKelvin - target);
        double tolerance = 512 * GearReference.Epsilon * massKilograms * (Math.Abs(target) + initial.IsochoricHeatCapacityJoulesPerKilogramKelvin + result.IsochoricHeatCapacityJoulesPerKilogramKelvin);
        if (Math.Abs(residual) > tolerance) return FuelVolumeStatus.NoConvergence;
        transfer = new(result, work, mean, residual); return FuelVolumeStatus.Success;
    }
}
