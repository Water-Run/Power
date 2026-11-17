// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public readonly record struct CompliantFuelWorkTransfer(CompliantFuelPoint Fluid,
    double EnergyChangeJoules, double MeanPressurePascals, double EntropyResidualJoulesPerKelvin);

public sealed partial class CompliantFuelVolume
{
    /// <summary>
    /// Closed reversible reference-volume work with fixed mass and entropy. Elastic
    /// wall energy is included, so work on the fluid-plus-wall system is -integral(p dV0).
    /// The returned energy change is also the mechanical work; no extra wall work is added.
    /// </summary>
    public FuelVolumeStatus AdiabaticReferenceVolume(double massKilograms, double storedEnergyJoules,
        double oldReferenceVolumeCubicMeters, double newReferenceVolumeCubicMeters, out CompliantFuelWorkTransfer transfer)
    {
        transfer = default;
        var status = FlashAtReferenceVolume(massKilograms, storedEnergyJoules, oldReferenceVolumeCubicMeters, out var initial);
        if (status != FuelVolumeStatus.Success) return status;
        if (!ReferenceVolumeValid(newReferenceVolumeCubicMeters)) return FuelVolumeStatus.InvalidInput;
        double delta = newReferenceVolumeCubicMeters - oldReferenceVolumeCubicMeters;
        if (delta == 0)
        { transfer = new(initial, 0, initial.PressurePascals, 0); return FuelVolumeStatus.Success; }

        double temperature = initial.TemperatureKelvin, target = initial.EntropyJoulesPerKelvin;
        // An incompressible pure-liquid adiabat has exactly constant temperature.
        status = EvaluateAtReferenceVolume(temperature, massKilograms, newReferenceVolumeCubicMeters, out var result);
        if (status != FuelVolumeStatus.Success) return status;
        if (initial.Phase != FuelVolumePhase.Liquid || result.Phase != FuelVolumePhase.Liquid)
        {
            double lo = Material.MinimumTemperatureKelvin, hi = Material.MaximumTemperatureKelvin;
            status = EvaluateAtReferenceVolume(lo, massKilograms, newReferenceVolumeCubicMeters, out var lower);
            if (status != FuelVolumeStatus.Success) return status;
            status = EvaluateAtReferenceVolume(hi, massKilograms, newReferenceVolumeCubicMeters, out var upper);
            if (status != FuelVolumeStatus.Success) return status;
            if (target < lower.EntropyJoulesPerKelvin || target > upper.EntropyJoulesPerKelvin) return FuelVolumeStatus.EntropyOutOfRange;
            bool converged = false;
            for (int iteration = 0; iteration < 80; ++iteration)
            {
                status = EvaluateAtReferenceVolume(temperature, massKilograms, newReferenceVolumeCubicMeters, out result);
                if (status != FuelVolumeStatus.Success) return status;
                double residual = result.EntropyJoulesPerKelvin - target;
                double correction = residual * temperature / result.HeatCapacityJoulesPerKelvin;
                if (!Numeric.Finite(correction)) return FuelVolumeStatus.NoConvergence;
                if (Math.Abs(correction) <= 32 * GearReference.Epsilon * Math.Max(1, temperature)) { converged = true; break; }
                if (residual > 0) hi = temperature; else lo = temperature;
                double next = temperature - correction;
                temperature = next > lo && next < hi ? next : (lo + hi) / 2;
            }
            if (!converged) return FuelVolumeStatus.NoConvergence;
        }
        double work;
        if (initial.Phase == FuelVolumePhase.Liquid && result.Phase == FuelVolumePhase.Liquid)
            work = -delta * (.5 * initial.PressurePascals + .5 * result.PressurePascals);
        else
        {
            double cv = Material.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin, cl = Material.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin;
            // Difference the phase variables, not the two large caloric reference energies.
            double sensibleCapacity = initial.LiquidMassKilograms * cl + initial.VaporMassKilograms * cv;
            work = sensibleCapacity * (result.TemperatureKelvin - initial.TemperatureKelvin) +
                (result.VaporMassKilograms - initial.VaporMassKilograms) * Material.LatentInternalEnergy(result.TemperatureKelvin) +
                .5 * ComplianceCubicMetersPerPascal * (result.PressurePascals - initial.PressurePascals) * (result.PressurePascals + initial.PressurePascals);
            double energyScale = sensibleCapacity * initial.TemperatureKelvin + initial.ElasticEnergyJoules +
                massKilograms * Material.LatentInternalEnergy(initial.TemperatureKelvin);
            // At roundoff-scale travel, endpoint entropy inversion cannot resolve the
            // energy difference. The pressure integral has a stable differential limit.
            if (Math.Abs(work) <= 1024 * GearReference.Epsilon * energyScale &&
                Math.Abs(delta) <= Math.Sqrt(GearReference.Epsilon) * oldReferenceVolumeCubicMeters && initial.Phase == result.Phase)
                work = -delta * (.5 * initial.PressurePascals + .5 * result.PressurePascals);
        }
        double mean = -work / delta;
        if (!Numeric.Finite(work) || !Numeric.Finite(mean) || mean <= 0) return FuelVolumeStatus.NoConvergence;
        status = FlashAtReferenceVolume(massKilograms, storedEnergyJoules + work, newReferenceVolumeCubicMeters, out result);
        if (status != FuelVolumeStatus.Success) return status;
        double entropyResidual = result.EntropyJoulesPerKelvin - target;
        double tolerance = 512 * GearReference.Epsilon * (Math.Abs(target) + initial.HeatCapacityJoulesPerKelvin + result.HeatCapacityJoulesPerKelvin);
        if (Math.Abs(entropyResidual) > tolerance) return FuelVolumeStatus.NoConvergence;
        transfer = new(result, work, mean, entropyResidual);
        return FuelVolumeStatus.Success;
    }
}
