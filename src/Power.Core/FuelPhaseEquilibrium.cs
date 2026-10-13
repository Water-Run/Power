// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed record FuelPhaseEquilibriumDefinition
{
    public Quantity ReferencePressure { get; init; }
    public Quantity MinimumTemperature { get; init; }
    public Quantity MaximumTemperature { get; init; }
}

public readonly record struct FuelPhasePoint(double TemperatureKelvin, double LiquidMassKilograms,
    double VaporMassKilograms, double ThermalEnergyJoules, double HeatCapacityJoulesPerKelvin);

public readonly record struct FuelPhaseTransfer(FuelFilmSample Liquid, double VaporMassKilograms,
    double GasEnergyJoules, double TemperatureKelvin, double WallTemperatureKelvin,
    double NetEvaporatedMassKilograms, double HeatFromWallJoules);

/// <summary>
/// One ideal pseudo-species in a negligible-volume liquid phase and a tracked ideal-gas receiver.
/// The saturation curve and latent energy share the same constant-heat-capacity reference.
/// </summary>
public sealed class FuelPhaseEquilibrium
{
    public EquilibriumFuelFilm Caloric { get; }
    public double GasConstantJoulesPerKilogramKelvin { get; }
    public double ReferencePressurePascals { get; }
    public double MinimumTemperatureKelvin { get; }
    public double MaximumTemperatureKelvin { get; }

    public FuelPhaseEquilibrium(EquilibriumFuelFilm caloric, double gasConstant,
        double referencePressure, double minimumTemperature, double maximumTemperature)
    {
        Caloric = caloric ?? throw new ArgumentNullException(nameof(caloric));
        GasConstantJoulesPerKilogramKelvin = gasConstant;
        ReferencePressurePascals = referencePressure;
        MinimumTemperatureKelvin = minimumTemperature;
        MaximumTemperatureKelvin = maximumTemperature;
        if (!GearReference.Finite(gasConstant, referencePressure, minimumTemperature, maximumTemperature) ||
            gasConstant <= 0 || referencePressure <= 0 || minimumTemperature <= 0 || maximumTemperature <= minimumTemperature ||
            caloric.SaturationTemperatureKelvin < minimumTemperature || caloric.SaturationTemperatureKelvin > maximumTemperature ||
            !(LatentInternalEnergy(minimumTemperature) > 0) || !(LatentInternalEnergy(maximumTemperature) > 0) ||
            !TrySaturationPressure(minimumTemperature, out _) || !TrySaturationPressure(maximumTemperature, out _))
            throw new ArgumentException("Use positive reference pressure and a finite temperature interval containing saturation_temperature, with positive latent energy throughout.");
    }

    public double LatentInternalEnergy(double temperatureKelvin) =>
        (Caloric.VaporSpecificHeatJoulesPerKilogramKelvin - Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin) * temperatureKelvin - Caloric.LiquidEnergyOffsetJoulesPerKilogram;

    public double LatentEnthalpy(double temperatureKelvin) =>
        LatentInternalEnergy(temperatureKelvin) + GasConstantJoulesPerKilogramKelvin * temperatureKelvin;

    private double LogSaturationPressure(double temperature)
    {
        double reference = Caloric.SaturationTemperatureKelvin, r = GasConstantJoulesPerKilogramKelvin;
        double deltaCp = Caloric.VaporSpecificHeatJoulesPerKilogramKelvin + r - Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin;
        // Equality of chemical potentials, including the temperature-dependent latent enthalpy.
        return Math.Log(ReferencePressurePascals) + deltaCp / r * Numeric.Log1p((temperature - reference) / reference) +
            Caloric.LiquidEnergyOffsetJoulesPerKilogram / r * ((reference - temperature) / reference / temperature);
    }

    public bool TrySaturationPressure(double temperatureKelvin, out double pressurePascals)
    {
        pressurePascals = 0;
        if (!Numeric.Finite(temperatureKelvin) || temperatureKelvin < MinimumTemperatureKelvin || temperatureKelvin > MaximumTemperatureKelvin) return false;
        pressurePascals = temperatureKelvin == Caloric.SaturationTemperatureKelvin ? ReferencePressurePascals : Math.Exp(LogSaturationPressure(temperatureKelvin));
        return Numeric.Finite(pressurePascals) && pressurePascals > 0;
    }

    internal static bool PressureAllowsLiquid(double pressure, double saturation) =>
        Numeric.Finite(pressure) && Numeric.Finite(saturation) && pressure >= 0 && saturation > 0 &&
        pressure - saturation >= -64 * GearReference.Epsilon * Math.Max(pressure, saturation);

    public FuelFilmSample CreateLiquidState(double massKilograms, double temperatureKelvin)
    {
        if (!Numeric.Finite(massKilograms) || massKilograms < 0 || !TrySaturationPressure(temperatureKelvin, out _))
            throw new ArgumentException("Use nonnegative liquid mass and temperature within the declared phase-equilibrium interval.");
        double energy = massKilograms * (Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin * temperatureKelvin + Caloric.LiquidEnergyOffsetJoulesPerKilogram);
        if (!Numeric.Finite(energy)) throw new ArgumentException("Liquid energy exceeds the supported scale.");
        return new(massKilograms, energy);
    }

    private bool Point(double temperature, double fuelMass, double carrierMass, double volume, out FuelPhasePoint point)
    {
        point = default;
        double cv = Caloric.VaporSpecificHeatJoulesPerKilogramKelvin, cl = Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin;
        double logMass = LogSaturationPressure(temperature) + Math.Log(volume) - Math.Log(GasConstantJoulesPerKilogramKelvin) - Math.Log(temperature);
        double vapor = fuelMass == 0 || logMass >= Math.Log(fuelMass) ? fuelMass : Math.Exp(logMass);
        double liquid = fuelMass - vapor, gas = carrierMass + vapor;
        double energy = gas * cv * temperature + liquid * (cl * temperature + Caloric.LiquidEnergyOffsetJoulesPerKilogram);
        double latent = LatentInternalEnergy(temperature);
        double capacity = gas * cv + liquid * cl + (liquid == 0 ? 0 : vapor * (latent / temperature) * (latent / temperature / GasConstantJoulesPerKilogramKelvin));
        if (!GearReference.Finite(vapor, liquid, energy, capacity) || gas <= 0 || capacity <= 0) return false;
        point = new(temperature, liquid, vapor, energy, capacity); return true;
    }

    /// <summary>Equilibrium caloric relation at fixed volume, total fuel and noncondensable carrier mass.</summary>
    public bool TryPoint(double temperatureKelvin, double totalFuelMassKilograms, double carrierMassKilograms,
        double volumeCubicMeters, out FuelPhasePoint point)
    {
        point = default;
        return GearReference.Finite(temperatureKelvin, totalFuelMassKilograms, carrierMassKilograms, volumeCubicMeters) &&
            temperatureKelvin >= MinimumTemperatureKelvin && temperatureKelvin <= MaximumTemperatureKelvin &&
            totalFuelMassKilograms >= 0 && carrierMassKilograms >= 0 && volumeCubicMeters > 0 &&
            Point(temperatureKelvin, totalFuelMassKilograms, carrierMassKilograms, volumeCubicMeters, out point);
    }

    /// <summary>
    /// Conserve fuel and internal energy while finding thermal and phase equilibrium.
    /// Wall heat uses bounded backward Euler; zero duration performs an adiabatic flash.
    /// </summary>
    public bool TryAdvance(FuelFilmSample liquid, double vaporMassKilograms, double carrierMassKilograms,
        double gasEnergyJoules, double volumeCubicMeters, double wallTemperatureKelvin,
        double wallCapacityJoulesPerKelvin, double seconds, out FuelPhaseTransfer transfer)
    {
        transfer = default;
        if (!GearReference.Finite(liquid.LiquidMassKilograms, liquid.ThermalEnergyJoules, vaporMassKilograms, carrierMassKilograms) ||
            !GearReference.Finite(gasEnergyJoules, volumeCubicMeters, wallTemperatureKelvin, wallCapacityJoulesPerKelvin) ||
            liquid.LiquidMassKilograms < 0 || vaporMassKilograms < 0 || carrierMassKilograms < 0 || vaporMassKilograms + carrierMassKilograms <= 0 ||
            gasEnergyJoules <= 0 || volumeCubicMeters <= 0 || wallTemperatureKelvin <= 0 || wallCapacityJoulesPerKelvin <= 0 ||
            !Numeric.Finite(seconds) || seconds < 0 || liquid.LiquidMassKilograms == 0 && liquid.ThermalEnergyJoules != 0) return false;
        if (liquid.LiquidMassKilograms > 0)
        {
            double temperature = Caloric.Temperature(liquid);
            if (!Numeric.Finite(temperature) || temperature < MinimumTemperatureKelvin - 1e-9 || temperature > MaximumTemperatureKelvin + 1e-9) return false;
        }
        double fuel = liquid.LiquidMassKilograms + vaporMassKilograms, energy = liquid.ThermalEnergyJoules + gasEnergyJoules;
        double kh = seconds * Caloric.ConductanceWattsPerKelvin;
        if (!Numeric.Finite(kh) || !Numeric.Finite(fuel) || !Numeric.Finite(energy)) return false;
        double alpha = kh == 0 ? 0 : wallCapacityJoulesPerKelvin / (1 + wallCapacityJoulesPerKelvin / kh);
        double target = energy + alpha * wallTemperatureKelvin;
        double dryCapacity = (carrierMassKilograms + fuel) * Caloric.VaporSpecificHeatJoulesPerKilogramKelvin;
        double dryTemperature = target / (dryCapacity + alpha);
        FuelPhasePoint result;
        if (fuel == 0)
            result = new(dryTemperature, 0, 0, dryCapacity * dryTemperature, dryCapacity);
        else
        {
            bool dry = false;
            if (dryTemperature >= MinimumTemperatureKelvin && Numeric.Finite(dryTemperature))
            {
                double checkTemperature = Math.Min(dryTemperature, MaximumTemperatureKelvin);
                if (!Point(checkTemperature, fuel, carrierMassKilograms, volumeCubicMeters, out var upper)) return false;
                dry = upper.LiquidMassKilograms == 0;
            }
            if (dry) result = new(dryTemperature, 0, fuel, dryCapacity * dryTemperature, dryCapacity);
            else
            {
                double lo = MinimumTemperatureKelvin, hi = MaximumTemperatureKelvin;
                if (!Point(lo, fuel, carrierMassKilograms, volumeCubicMeters, out var lower) ||
                    !Point(hi, fuel, carrierMassKilograms, volumeCubicMeters, out var upper) ||
                    lower.ThermalEnergyJoules + alpha * lo > target || upper.ThermalEnergyJoules + alpha * hi < target) return false;
                double temperature = (lo + hi) / 2;
                result = default;
                bool converged = false;
                for (int iteration = 0; iteration < 64; ++iteration)
                {
                    if (!Point(temperature, fuel, carrierMassKilograms, volumeCubicMeters, out result)) return false;
                    double residual = result.ThermalEnergyJoules + alpha * temperature - target;
                    double correction = residual / (result.HeatCapacityJoulesPerKelvin + alpha);
                    if (Math.Abs(correction) <= 32 * GearReference.Epsilon * Math.Max(1, temperature)) { converged = true; break; }
                    if (residual > 0) hi = temperature; else lo = temperature;
                    double next = temperature - correction;
                    temperature = next > lo && next < hi ? next : (lo + hi) / 2;
                }
                if (!converged) return false;
            }
        }
        double t = result.TemperatureKelvin, heat = alpha * (wallTemperatureKelvin - t);
        double liquidEnergy = result.LiquidMassKilograms * (Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin * t + Caloric.LiquidEnergyOffsetJoulesPerKilogram);
        // Put the scalar root's roundoff into gas energy, retaining the exact heat/mass transaction.
        double gasEnergy = energy + heat - liquidEnergy, gasMass = carrierMassKilograms + result.VaporMassKilograms;
        double gasTemperature = gasEnergy / gasMass / Caloric.VaporSpecificHeatJoulesPerKilogramKelvin;
        double wall = wallTemperatureKelvin - heat / wallCapacityJoulesPerKelvin;
        if (!GearReference.Finite(t, heat, liquidEnergy, gasEnergy) || !Numeric.Finite(wall) ||
            t <= 0 || gasEnergy <= 0 || wall <= 0 || Math.Abs(gasTemperature - t) > 1e-8 + 128 * GearReference.Epsilon * t) return false;
        transfer = new(new(result.LiquidMassKilograms, liquidEnergy), result.VaporMassKilograms, gasEnergy, t, wall,
            liquid.LiquidMassKilograms - result.LiquidMassKilograms, heat);
        return true;
    }
}
