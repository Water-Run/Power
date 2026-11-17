// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public enum FuelVolumeStatus
{
    Success,
    InvalidInput,
    TemperatureOutOfRange,
    MassOutOfRange,
    EnergyOutOfRange,
    NoConvergence,
    EntropyOutOfRange
}

public enum FuelVolumePhase { Liquid, LiquidVapor, Vapor }

public readonly record struct FuelCoexistencePoint(double TemperatureKelvin, double PressurePascals,
    double LiquidSpecificVolumeCubicMetersPerKilogram, double VaporSpecificVolumeCubicMetersPerKilogram,
    double LatentEnthalpyJoulesPerKilogram, double PressureDerivativePascalsPerKelvin);

public readonly record struct CompliantFuelPoint(double MassKilograms, double TemperatureKelvin,
    double PressurePascals, double VolumeCubicMeters, double LiquidMassKilograms, double VaporMassKilograms,
    double VaporVolumeCubicMeters, double FluidInternalEnergyJoules, double ElasticEnergyJoules,
    double StoredEnergyJoules, double SpecificEnthalpyJoulesPerKilogram, double EntropyJoulesPerKelvin,
    double HeatCapacityJoulesPerKelvin, double CaloricResidualJoules, FuelVolumePhase Phase)
{
    public double VaporMassFraction => VaporMassKilograms / MassKilograms;
    public double VaporVolumeFraction => VaporVolumeCubicMeters / VolumeCubicMeters;
}

public readonly record struct CompliantFuelHeatTransfer(CompliantFuelPoint Fluid,
    double WallTemperatureKelvin, double HeatFromWallJoules, double EntropyChangeJoulesPerKelvin,
    double EnergyResidualJoules);

/// <summary>
/// Pure fuel in thermal and phase equilibrium inside an elastic vessel with V = V0 + C p.
/// Liquid is incompressible, vapor is ideal, and the massless elastic wall stores C p² / 2.
/// This local constitutive reference has no pump, nozzle, transport or simulation clock.
/// </summary>
public sealed partial class CompliantFuelVolume
{
    public FuelPhaseEquilibrium Material { get; }
    public double ReferenceVolumeCubicMeters { get; }
    public double ComplianceCubicMetersPerPascal { get; }
    public double LiquidDensityKilogramsPerCubicMeter { get; }
    public double MaximumPressurePascals { get; }
    public double MaximumMassKilograms { get; }
    private readonly double _minimumCoexistencePressure;

    public CompliantFuelVolume(FuelPhaseEquilibrium material, double referenceVolumeCubicMeters,
        double complianceCubicMetersPerPascal, double liquidDensityKilogramsPerCubicMeter,
        double maximumPressurePascals)
    {
        Material = material ?? throw new ArgumentNullException(nameof(material));
        if (!Numeric.Finite(referenceVolumeCubicMeters) || referenceVolumeCubicMeters <= 0)
            throw new ArgumentOutOfRangeException(nameof(referenceVolumeCubicMeters), "Use positive finite reference volume.");
        if (!Numeric.Finite(complianceCubicMetersPerPascal) || complianceCubicMetersPerPascal <= 0)
            throw new ArgumentOutOfRangeException(nameof(complianceCubicMetersPerPascal), "Use positive finite wall compliance.");
        if (!Numeric.Finite(liquidDensityKilogramsPerCubicMeter) || liquidDensityKilogramsPerCubicMeter <= 0)
            throw new ArgumentOutOfRangeException(nameof(liquidDensityKilogramsPerCubicMeter), "Use positive finite liquid density.");
        double r = material.GasConstantJoulesPerKilogramKelvin;
        if (!Numeric.Finite(maximumPressurePascals) || maximumPressurePascals <= 0 ||
            maximumPressurePascals / liquidDensityKilogramsPerCubicMeter / r / material.MinimumTemperatureKelvin >= 1 - 64 * GearReference.Epsilon)
            throw new ArgumentOutOfRangeException(nameof(maximumPressurePascals), "Declare a positive wall pressure limit below rho_l R T_min; the vapor density stays below liquid density.");
        ReferenceVolumeCubicMeters = referenceVolumeCubicMeters;
        ComplianceCubicMetersPerPascal = complianceCubicMetersPerPascal;
        LiquidDensityKilogramsPerCubicMeter = liquidDensityKilogramsPerCubicMeter;
        MaximumPressurePascals = maximumPressurePascals;
        MaximumMassKilograms = liquidDensityKilogramsPerCubicMeter * (referenceVolumeCubicMeters + complianceCubicMetersPerPascal * maximumPressurePascals);
        if (!Numeric.Finite(MaximumMassKilograms) || MaximumMassKilograms <= 0 ||
            !Numeric.Finite(.5 * (complianceCubicMetersPerPascal * maximumPressurePascals) * maximumPressurePascals) ||
            Saturation(material.MinimumTemperatureKelvin, out var minimum) != FuelVolumeStatus.Success ||
            Saturation(material.MaximumTemperatureKelvin, out var maximum) != FuelVolumeStatus.Success || maximum.PressurePascals > maximumPressurePascals ||
            !Numeric.Finite(material.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin + r) ||
            (referenceVolumeCubicMeters + complianceCubicMetersPerPascal * minimum.PressurePascals) - referenceVolumeCubicMeters < 128 * GearReference.Epsilon * referenceVolumeCubicMeters)
            throw new ArgumentException("Use representable vessel/material scales and a pressure limit covering the low-density saturation curve over the complete temperature interval.");
        _minimumCoexistencePressure = minimum.PressurePascals;
        if (Evaluate(material.MinimumTemperatureKelvin, MaximumMassKilograms, out var cold) != FuelVolumeStatus.Success ||
            Evaluate(material.MaximumTemperatureKelvin, MaximumMassKilograms, out var hot) != FuelVolumeStatus.Success ||
            !Numeric.Finite(hot.StoredEnergyJoules - cold.StoredEnergyJoules))
            throw new ArgumentException("Vessel and material energy/entropy scales must be representable over the declared mass and temperature interval.");
    }

    /// <summary>Pure-fluid coexistence with equal pressure and chemical potential, including liquid p/rho.</summary>
    public FuelVolumeStatus Saturation(double temperatureKelvin, out FuelCoexistencePoint point)
    {
        point = default;
        if (!Numeric.Finite(temperatureKelvin)) return FuelVolumeStatus.InvalidInput;
        if (!Material.TrySaturationPressure(temperatureKelvin, out double reference)) return FuelVolumeStatus.TemperatureOutOfRange;
        double r = Material.GasConstantJoulesPerKilogramKelvin, density = LiquidDensityKilogramsPerCubicMeter;
        // alpha = p/(rho R T). ln(alpha) - alpha = ln(p0/(rho R T)).
        // Select vapor density below liquid density; the dense-vapor root is outside this model.
        double logRatio = Math.Log(reference) - Math.Log(density) - Math.Log(r) - Math.Log(temperatureKelvin);
        if (!Numeric.Finite(logRatio) || logRatio >= -1) return FuelVolumeStatus.NoConvergence;
        double lo = logRatio, hi = 0, z = lo;
        bool converged = false;
        for (int iteration = 0; iteration < 64; ++iteration)
        {
            double alpha = Math.Exp(z), residual = z - alpha - logRatio, correction = residual / (1 - alpha);
            if (!Numeric.Finite(correction)) return FuelVolumeStatus.NoConvergence;
            if (Math.Abs(correction) <= 16 * GearReference.Epsilon * Math.Max(1, Math.Abs(z))) { converged = true; break; }
            if (residual > 0) hi = z; else lo = z;
            double next = z - correction;
            z = next > lo && next < hi ? next : (lo + hi) / 2;
        }
        if (!converged) return FuelVolumeStatus.NoConvergence;
        double pressure = reference * Math.Exp(Math.Exp(z));
        double ratio = pressure / density / r / temperatureKelvin;
        double vaporVolume = r * temperatureKelvin / pressure;
        double enthalpy = Material.LatentInternalEnergy(temperatureKelvin) + r * temperatureKelvin - pressure / density;
        double derivative = enthalpy / temperatureKelvin / (vaporVolume - 1 / density);
        if (!GearReference.Finite(pressure, vaporVolume, enthalpy, derivative) || pressure <= 0 || enthalpy <= 0 || derivative <= 0 || ratio >= 1 - 64 * GearReference.Epsilon)
            return FuelVolumeStatus.NoConvergence;
        point = new(temperatureKelvin, pressure, 1 / density, vaporVolume, enthalpy, derivative);
        return FuelVolumeStatus.Success;
    }

    /// <summary>Equilibrium at declared total mass and temperature, including elastic wall energy.</summary>
    public FuelVolumeStatus Evaluate(double temperatureKelvin, double massKilograms, out CompliantFuelPoint point) =>
        EvaluateAtReferenceVolume(temperatureKelvin, massKilograms, ReferenceVolumeCubicMeters, out point);

    /// <summary>Evaluate the same immutable material and compliance at an explicit geometric reference volume.</summary>
    public FuelVolumeStatus EvaluateAtReferenceVolume(double temperatureKelvin, double massKilograms, double referenceVolumeCubicMeters, out CompliantFuelPoint point)
    {
        point = default;
        if (!ReferenceVolumeValid(referenceVolumeCubicMeters)) return FuelVolumeStatus.InvalidInput;
        double maximumMass = referenceVolumeCubicMeters == ReferenceVolumeCubicMeters ? MaximumMassKilograms :
            LiquidDensityKilogramsPerCubicMeter * (referenceVolumeCubicMeters + ComplianceCubicMetersPerPascal * MaximumPressurePascals);
        if (!Numeric.Finite(maximumMass)) return FuelVolumeStatus.InvalidInput;
        if (!Numeric.Finite(massKilograms) || !Numeric.Finite(temperatureKelvin)) return FuelVolumeStatus.InvalidInput;
        if (massKilograms <= 0 || massKilograms > maximumMass) return FuelVolumeStatus.MassOutOfRange;
        var status = Saturation(temperatureKelvin, out var saturation);
        if (status != FuelVolumeStatus.Success) return status;
        double density = LiquidDensityKilogramsPerCubicMeter, r = Material.GasConstantJoulesPerKilogramKelvin;
        double cv = Material.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin, cl = Material.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin;
        double a = Material.Caloric.LiquidEnergyOffsetJoulesPerKilogram, compliance = ComplianceCubicMetersPerPascal;
        double volume = referenceVolumeCubicMeters + compliance * saturation.PressurePascals;
        double liquidLimit = density * volume, vaporLimit = volume / saturation.VaporSpecificVolumeCubicMetersPerKilogram;
        double pressure, vapor, capacity;
        FuelVolumePhase phase;
        if (massKilograms >= liquidLimit)
        {
            phase = FuelVolumePhase.Liquid; vapor = 0;
            // The declared upper mass is formed from the declared upper pressure.
            // Retain that endpoint when tiny wall compliance makes the inverse ill-conditioned.
            pressure = massKilograms == maximumMass ? MaximumPressurePascals :
                (massKilograms / density - referenceVolumeCubicMeters) / compliance;
            capacity = massKilograms * cl;
        }
        else if (massKilograms <= vaporLimit)
        {
            phase = FuelVolumePhase.Vapor; vapor = massKilograms;
            double idealPressure = massKilograms / referenceVolumeCubicMeters * r * temperatureKelvin;
            double ratio = compliance / referenceVolumeCubicMeters * idealPressure;
            pressure = idealPressure / (.5 + Math.Sqrt(.25 + ratio));
            double pressureSlope = massKilograms * r / (referenceVolumeCubicMeters + 2 * compliance * pressure);
            capacity = massKilograms * cv + compliance * pressure * pressureSlope;
        }
        else
        {
            phase = FuelVolumePhase.LiquidVapor; pressure = saturation.PressurePascals;
            double deltaVolume = saturation.VaporSpecificVolumeCubicMetersPerKilogram - 1 / density;
            vapor = (volume - massKilograms / density) / deltaVolume;
            double vaporVolumeSlope = r / pressure - saturation.VaporSpecificVolumeCubicMetersPerKilogram * saturation.PressureDerivativePascalsPerKelvin / pressure;
            double vaporSlope = (compliance * saturation.PressureDerivativePascalsPerKelvin - vapor * vaporVolumeSlope) / deltaVolume;
            capacity = (massKilograms - vapor) * cl + vapor * cv + vaporSlope * Material.LatentInternalEnergy(temperatureKelvin) +
                compliance * pressure * saturation.PressureDerivativePascalsPerKelvin;
        }
        volume = referenceVolumeCubicMeters + compliance * pressure;
        double liquid = massKilograms - vapor, gasVolume = vapor == 0 ? 0 : vapor * r * temperatureKelvin / pressure;
        double liquidEnergy = cl * temperatureKelvin + a, vaporEnergy = cv * temperatureKelvin;
        double fluidEnergy = liquid * liquidEnergy + vapor * vaporEnergy;
        double elastic = .5 * (compliance * pressure) * pressure, stored = fluidEnergy + elastic;
        double enthalpy = (liquid * (liquidEnergy + pressure / density) + vapor * (vaporEnergy + r * temperatureKelvin)) / massKilograms;
        double logTemperature = Numeric.Log1p((temperatureKelvin - Material.Caloric.SaturationTemperatureKelvin) / Material.Caloric.SaturationTemperatureKelvin);
        double liquidEntropy = cl * logTemperature;
        double vaporEntropy = vapor == 0 ? 0 : (cv + r) * logTemperature - r * Math.Log(pressure / Material.ReferencePressurePascals) +
            Material.Caloric.LatentInternalEnergyJoulesPerKilogram / Material.Caloric.SaturationTemperatureKelvin + r;
        double entropy = liquid * liquidEntropy + vapor * vaporEntropy;
        if (!GearReference.Finite(pressure, volume, vapor, liquid) || !GearReference.Finite(fluidEnergy, elastic, stored, capacity) ||
            !GearReference.Finite(enthalpy, entropy, gasVolume, 0) || pressure <= 0 || pressure > MaximumPressurePascals * (1 + 64 * GearReference.Epsilon) ||
            vapor < 0 || liquid < 0 || volume <= 0 || capacity <= 0) return FuelVolumeStatus.NoConvergence;
        point = new(massKilograms, temperatureKelvin, pressure, volume, liquid, vapor, gasVolume, fluidEnergy, elastic, stored, enthalpy, entropy, capacity, 0, phase);
        return FuelVolumeStatus.Success;
    }

    /// <summary>Fixed total mass and fluid-plus-wall energy flash. No energy or mass boundary is inferred.</summary>
    public FuelVolumeStatus Flash(double massKilograms, double storedEnergyJoules, out CompliantFuelPoint point) =>
        FlashAtReferenceVolume(massKilograms, storedEnergyJoules, ReferenceVolumeCubicMeters, out point);

    public FuelVolumeStatus FlashAtReferenceVolume(double massKilograms, double storedEnergyJoules, double referenceVolumeCubicMeters, out CompliantFuelPoint point)
    {
        point = default;
        if (!Numeric.Finite(storedEnergyJoules)) return FuelVolumeStatus.InvalidInput;
        var status = Solve(massKilograms, storedEnergyJoules, 0, out var result, referenceVolumeCubicMeters);
        return status == FuelVolumeStatus.Success ? RetainEnergy(result, storedEnergyJoules, out point) : status;
    }

    /// <summary>Finite-bath heat exchange by backward Euler; phase change and wall elasticity share the energy solve.</summary>
    public FuelVolumeStatus AdvanceHeat(double massKilograms, double storedEnergyJoules,
        double wallTemperatureKelvin, double wallCapacityJoulesPerKelvin, double conductanceWattsPerKelvin,
        double seconds, out CompliantFuelHeatTransfer transfer) =>
        AdvanceHeatAtReferenceVolume(massKilograms, storedEnergyJoules, ReferenceVolumeCubicMeters, wallTemperatureKelvin,
            wallCapacityJoulesPerKelvin, conductanceWattsPerKelvin, seconds, out transfer);

    public FuelVolumeStatus AdvanceHeatAtReferenceVolume(double massKilograms, double storedEnergyJoules, double referenceVolumeCubicMeters,
        double wallTemperatureKelvin, double wallCapacityJoulesPerKelvin, double conductanceWattsPerKelvin, double seconds, out CompliantFuelHeatTransfer transfer)
    {
        transfer = default;
        if (!GearReference.Finite(wallTemperatureKelvin, wallCapacityJoulesPerKelvin, conductanceWattsPerKelvin, seconds) ||
            wallTemperatureKelvin <= 0 || wallCapacityJoulesPerKelvin <= 0 || conductanceWattsPerKelvin < 0 || seconds < 0)
            return FuelVolumeStatus.InvalidInput;
        var status = FlashAtReferenceVolume(massKilograms, storedEnergyJoules, referenceVolumeCubicMeters, out var initial);
        if (status != FuelVolumeStatus.Success) return status;
        double kh = conductanceWattsPerKelvin * seconds;
        if (!Numeric.Finite(kh)) return FuelVolumeStatus.InvalidInput;
        double alpha = kh == 0 ? 0 : wallCapacityJoulesPerKelvin / (1 + wallCapacityJoulesPerKelvin / kh);
        status = Solve(massKilograms, storedEnergyJoules + alpha * wallTemperatureKelvin, alpha, out var result, referenceVolumeCubicMeters);
        if (status != FuelVolumeStatus.Success) return status;
        double heat = alpha * (wallTemperatureKelvin - result.TemperatureKelvin);
        status = RetainEnergy(result, storedEnergyJoules + heat, out result);
        if (status != FuelVolumeStatus.Success) return status;
        double beta = kh / wallCapacityJoulesPerKelvin, weight = 1 / (1 + beta);
        double wall = weight * wallTemperatureKelvin + (beta < 1 ? beta / (1 + beta) : 1 - weight) * result.TemperatureKelvin;
        double entropy = result.EntropyJoulesPerKelvin - initial.EntropyJoulesPerKelvin +
            wallCapacityJoulesPerKelvin * Math.Log(wall / wallTemperatureKelvin);
        double residual = result.StoredEnergyJoules - storedEnergyJoules + wallCapacityJoulesPerKelvin * (wall - wallTemperatureKelvin);
        if (!GearReference.Finite(wall, heat, entropy, residual) || wall <= 0) return FuelVolumeStatus.NoConvergence;
        transfer = new(result, wall, heat, entropy, residual);
        return FuelVolumeStatus.Success;
    }

    private FuelVolumeStatus Solve(double mass, double target, double extraCapacity, out CompliantFuelPoint point, double referenceVolumeCubicMeters)
    {
        point = default;
        if (!Numeric.Finite(target)) return FuelVolumeStatus.InvalidInput;
        double lo = Material.MinimumTemperatureKelvin, hi = Material.MaximumTemperatureKelvin;
        var status = EvaluateAtReferenceVolume(lo, mass, referenceVolumeCubicMeters, out var lower);
        if (status != FuelVolumeStatus.Success) return status;
        status = EvaluateAtReferenceVolume(hi, mass, referenceVolumeCubicMeters, out var upper);
        if (status != FuelVolumeStatus.Success) return status;
        double lowerEnergy = lower.StoredEnergyJoules + extraCapacity * lo, upperEnergy = upper.StoredEnergyJoules + extraCapacity * hi;
        if (!Numeric.Finite(lowerEnergy) || !Numeric.Finite(upperEnergy)) return FuelVolumeStatus.NoConvergence;
        if (target < lowerEnergy || target > upperEnergy) return FuelVolumeStatus.EnergyOutOfRange;
        if (target == lowerEnergy) { point = lower; return FuelVolumeStatus.Success; }
        if (target == upperEnergy) { point = upper; return FuelVolumeStatus.Success; }
        double temperature = lo + (hi - lo) * ((target - lowerEnergy) / (upperEnergy - lowerEnergy));
        for (int iteration = 0; iteration < 80; ++iteration)
        {
            status = EvaluateAtReferenceVolume(temperature, mass, referenceVolumeCubicMeters, out var result);
            if (status != FuelVolumeStatus.Success) return status;
            double residual = result.StoredEnergyJoules + extraCapacity * temperature - target;
            double correction = residual / (result.HeatCapacityJoulesPerKelvin + extraCapacity);
            if (!Numeric.Finite(correction)) return FuelVolumeStatus.NoConvergence;
            if (Math.Abs(correction) <= 32 * GearReference.Epsilon * Math.Max(1, temperature))
            { point = result; return FuelVolumeStatus.Success; }
            if (residual > 0) hi = temperature; else lo = temperature;
            double next = temperature - correction;
            temperature = next > lo && next < hi ? next : (lo + hi) / 2;
        }
        return FuelVolumeStatus.NoConvergence;
    }

    private bool ReferenceVolumeValid(double referenceVolume) => Numeric.Finite(referenceVolume) && referenceVolume > 0 &&
        (referenceVolume + ComplianceCubicMetersPerPascal * _minimumCoexistencePressure) - referenceVolume >=
            128 * GearReference.Epsilon * referenceVolume;

    private static FuelVolumeStatus RetainEnergy(CompliantFuelPoint result, double energy, out CompliantFuelPoint point)
    {
        point = default;
        double residual = energy - result.StoredEnergyJoules;
        if (!Numeric.Finite(energy) || Math.Abs(residual) / result.HeatCapacityJoulesPerKelvin > 1e-8 + 128 * GearReference.Epsilon * result.TemperatureKelvin)
            return FuelVolumeStatus.NoConvergence;
        point = result with { StoredEnergyJoules = energy, FluidInternalEnergyJoules = energy - result.ElasticEnergyJoules, CaloricResidualJoules = residual };
        return FuelVolumeStatus.Success;
    }
}
