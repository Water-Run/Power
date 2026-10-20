// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public readonly record struct FiniteTankPhasePoint(double TemperatureKelvin, double LiquidMassKilograms,
    double VaporMassKilograms, double GasVolumeCubicMeters, double PressurePascals,
    double VaporPressurePascals, double ThermalEnergyJoules, double HeatCapacityJoulesPerKelvin);

public readonly record struct FiniteTankPhaseTransfer(FuelFilmSample Liquid, double VaporMassKilograms,
    double GasEnergyJoules, double TemperatureKelvin, double NetEvaporatedMassKilograms);

/// <summary>
/// Rigid vessel, incompressible liquid and an ideal pseudo-species/carrier gas.
/// The liquid chemical potential includes p/rho; its caloric and entropy reference
/// is shared with the paired negligible-displacement film material.
/// </summary>
public sealed partial class FiniteTankFuelEquilibrium
{
    public FuelPhaseEquilibrium Material { get; }
    public double CapacityCubicMeters { get; }
    public double DensityKilogramsPerCubicMeter { get; }
    private readonly double _capacityMass;

    public FiniteTankFuelEquilibrium(FuelPhaseEquilibrium material, double capacityCubicMeters, double densityKilogramsPerCubicMeter)
    {
        Material = material ?? throw new ArgumentNullException(nameof(material));
        CapacityCubicMeters = capacityCubicMeters; DensityKilogramsPerCubicMeter = densityKilogramsPerCubicMeter;
        _capacityMass = capacityCubicMeters * densityKilogramsPerCubicMeter;
        if (!Numeric.Finite(capacityCubicMeters) || !Numeric.Finite(densityKilogramsPerCubicMeter) ||
            capacityCubicMeters <= 0 || densityKilogramsPerCubicMeter <= 0 || !Numeric.Finite(_capacityMass) || _capacityMass <= 0 ||
            !material.TrySaturationPressure(material.MaximumTemperatureKelvin, out double maximumPressure) || !Numeric.Finite(maximumPressure * Math.E))
            throw new ArgumentException("Use positive finite tank capacity/density and a representable pressure-corrected material curve.");
    }

    /// <summary>Pressure-corrected coexistence fuel partial pressure, within the dilute-gas density domain.</summary>
    public bool TrySaturationPressure(double temperatureKelvin, double totalPressurePascals, out double pressurePascals)
    {
        pressurePascals = 0;
        if (!Material.TrySaturationPressure(temperatureKelvin, out double reference) || !Numeric.Finite(totalPressurePascals) || totalPressurePascals < 0) return false;
        double ratio = totalPressurePascals / DensityKilogramsPerCubicMeter / Material.GasConstantJoulesPerKilogramKelvin / temperatureKelvin;
        if (!Numeric.Finite(ratio) || ratio >= 1 - 64 * GearReference.Epsilon) return false;
        pressurePascals = reference * Math.Exp(ratio);
        return Numeric.Finite(pressurePascals) && pressurePascals > 0;
    }

    /// <summary>Equilibrium energy and its temperature derivative at fixed total fuel, carrier mass and vessel volume.</summary>
    public bool TryPoint(double temperatureKelvin, double fuelMassKilograms, double carrierMassKilograms, out FiniteTankPhasePoint point) =>
        TryPointAtCapacity(temperatureKelvin, fuelMassKilograms, carrierMassKilograms, CapacityCubicMeters, out point);

    public bool TryPointAtCapacity(double temperatureKelvin, double fuelMassKilograms, double carrierMassKilograms, double capacityCubicMeters, out FiniteTankPhasePoint point)
    {
        point = default;
        if (!Numeric.Finite(capacityCubicMeters) || capacityCubicMeters <= 0) return false;
        double capacityMass = capacityCubicMeters == CapacityCubicMeters ? _capacityMass : capacityCubicMeters * DensityKilogramsPerCubicMeter;
        if (!Numeric.Finite(capacityMass) || capacityMass <= 0) return false;
        if (!GearReference.Finite(temperatureKelvin, fuelMassKilograms, carrierMassKilograms, 0) || fuelMassKilograms < 0 || carrierMassKilograms < 0 ||
            fuelMassKilograms + carrierMassKilograms <= 0 || fuelMassKilograms + carrierMassKilograms >= capacityMass * (1 - 64 * GearReference.Epsilon) ||
            !Material.TrySaturationPressure(temperatureKelvin, out double zeroVolumePressure)) return false;
        double freeMass = capacityMass - fuelMassKilograms, r = Material.GasConstantJoulesPerKilogramKelvin;
        double vapor = fuelMassKilograms, slope = 0;
        if (fuelMassKilograms > 0)
        {
            // y = m_v / (rho*V_g). For total density below liquid density,
            // ln(y) - (1-carrier/freeMass)*y is strictly increasing on the physical interval.
            double carrierRatio = carrierMassKilograms / freeMass, a = 1 - carrierRatio;
            double maximumY = fuelMassKilograms / capacityMass, logMaximum = Math.Log(maximumY);
            double logB = Math.Log(zeroVolumePressure) - Math.Log(DensityKilogramsPerCubicMeter) - Math.Log(r) - Math.Log(temperatureKelvin) + carrierRatio;
            double atDry = logMaximum - a * maximumY - logB;
            if (atDry > 0)
            {
                double lo = logB, hi = Math.Min(logMaximum, logB + a * maximumY), z = lo;
                bool converged = false;
                for (int iteration = 0; iteration < 48; ++iteration)
                {
                    double y = Math.Exp(z), residual = z - a * y - logB, derivative = 1 - a * y;
                    double correction = residual / derivative;
                    if (!Numeric.Finite(correction) || derivative <= 0) return false;
                    if (Math.Abs(correction) <= 32 * GearReference.Epsilon * Math.Max(1, Math.Abs(z))) { converged = true; break; }
                    if (residual > 0) hi = z; else lo = z;
                    double next = z - correction;
                    z = next > lo && next < hi ? next : (lo + hi) / 2;
                }
                if (!converged) return false;
                double fraction = Math.Exp(z), remaining = 1 - fraction;
                vapor = freeMass * fraction / remaining;
                double latent = Material.LatentInternalEnergy(temperatureKelvin);
                slope = freeMass * fraction / remaining / remaining * latent / r / temperatureKelvin / temperatureKelvin / (1 - a * fraction);
            }
        }
        double liquid = fuelMassKilograms - vapor, volume = capacityCubicMeters - liquid / DensityKilogramsPerCubicMeter;
        double gas = carrierMassKilograms + vapor, cv = Material.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin, cl = Material.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin;
        double energy = gas * cv * temperatureKelvin + liquid * (cl * temperatureKelvin + Material.Caloric.LiquidEnergyOffsetJoulesPerKilogram);
        double capacity = gas * cv + liquid * cl + slope * Material.LatentInternalEnergy(temperatureKelvin);
        double pressure = gas / volume * r * temperatureKelvin, partial = vapor / volume * r * temperatureKelvin;
        if (!GearReference.Finite(vapor, liquid, volume, energy) || !GearReference.Finite(capacity, pressure, partial, slope) ||
            liquid < 0 || volume <= 0 || gas <= 0 || capacity <= 0 || pressure <= 0 || partial < 0) return false;
        point = new(temperatureKelvin, liquid, vapor, volume, pressure, partial, energy, capacity); return true;
    }

    /// <summary>Closed U,V equilibrium flash. Phase expansion work is internal to the rigid vessel.</summary>
    public bool TryFlash(FuelFilmSample liquid, double vaporMassKilograms, double carrierMassKilograms,
        double gasEnergyJoules, out FiniteTankPhaseTransfer transfer)
    {
        transfer = default;
        if (!GearReference.Finite(liquid.LiquidMassKilograms, liquid.ThermalEnergyJoules, vaporMassKilograms, carrierMassKilograms) ||
            !Numeric.Finite(gasEnergyJoules) || liquid.LiquidMassKilograms < 0 || vaporMassKilograms < 0 || carrierMassKilograms < 0 ||
            vaporMassKilograms + carrierMassKilograms <= 0 || gasEnergyJoules <= 0 || liquid.LiquidMassKilograms == 0 && liquid.ThermalEnergyJoules != 0) return false;
        if (liquid.LiquidMassKilograms > 0 && !Material.TrySaturationPressure(Material.Caloric.Temperature(liquid), out _)) return false;
        double fuel = liquid.LiquidMassKilograms + vaporMassKilograms, energy = liquid.ThermalEnergyJoules + gasEnergyJoules;
        if (!Numeric.Finite(fuel) || !Numeric.Finite(energy) || fuel + carrierMassKilograms >= _capacityMass * (1 - 64 * GearReference.Epsilon)) return false;
        if (!TryFlashEnergyAtCapacity(fuel, carrierMassKilograms, energy, CapacityCubicMeters, out var state)) return false;
        transfer = new(state.Liquid, state.VaporMassKilograms, state.GasEnergyJoules, state.TemperatureKelvin,
            liquid.LiquidMassKilograms - state.Liquid.LiquidMassKilograms); return true;
    }

    public bool TryFlashEnergyAtCapacity(double fuel, double carrierMassKilograms, double energy, double capacityCubicMeters, out FiniteFuelPhaseState state)
    {
        state = default;
        if (!GearReference.Finite(fuel, carrierMassKilograms, energy, capacityCubicMeters) || fuel < 0 || carrierMassKilograms < 0 || fuel + carrierMassKilograms <= 0 || capacityCubicMeters <= 0) return false;
        double lo = Material.MinimumTemperatureKelvin, hi = Material.MaximumTemperatureKelvin;
        if (!TryPointAtCapacity(lo, fuel, carrierMassKilograms, capacityCubicMeters, out var lower) || !TryPointAtCapacity(hi, fuel, carrierMassKilograms, capacityCubicMeters, out var upper)) return false;
        FiniteTankPhasePoint result;
        double lowCorrection = (lower.ThermalEnergyJoules - energy) / lower.HeatCapacityJoulesPerKelvin;
        double highCorrection = (upper.ThermalEnergyJoules - energy) / upper.HeatCapacityJoulesPerKelvin;
        if (Math.Abs(lowCorrection) <= 32 * GearReference.Epsilon * Math.Max(1, lo)) result = lower;
        else if (Math.Abs(highCorrection) <= 32 * GearReference.Epsilon * Math.Max(1, hi)) result = upper;
        else
        {
            if (lowCorrection > 0 || highCorrection < 0) return false;
            double temperature = (lo + hi) / 2; result = default; bool converged = false;
            for (int iteration = 0; iteration < 64; ++iteration)
            {
                if (!TryPointAtCapacity(temperature, fuel, carrierMassKilograms, capacityCubicMeters, out result)) return false;
                double correction = (result.ThermalEnergyJoules - energy) / result.HeatCapacityJoulesPerKelvin;
                if (Math.Abs(correction) <= 32 * GearReference.Epsilon * Math.Max(1, temperature)) { converged = true; break; }
                if (correction > 0) hi = temperature; else lo = temperature;
                double next = temperature - correction;
                temperature = next > lo && next < hi ? next : (lo + hi) / 2;
            }
            if (!converged) return false;
        }
        double t = result.TemperatureKelvin, liquidEnergy = result.LiquidMassKilograms *
            (Material.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin * t + Material.Caloric.LiquidEnergyOffsetJoulesPerKilogram);
        double gasCapacity = (carrierMassKilograms + result.VaporMassKilograms) * Material.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin;
        double liquidCapacity = result.LiquidMassKilograms * Material.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin;
        double gasEnergy = gasCapacity * t;
        // Absorb the scalar root's energy roundoff in the larger sensible heat capacity.
        // A nearly full tank must not amplify liquid-energy cancellation into its tiny gas temperature.
        if (liquidCapacity >= gasCapacity) liquidEnergy = energy - gasEnergy;
        else gasEnergy = energy - liquidEnergy;
        double gasTemperature = gasEnergy / gasCapacity;
        double liquidTemperature = result.LiquidMassKilograms == 0 ? t : Material.Caloric.Temperature(new(result.LiquidMassKilograms, liquidEnergy));
        if (!Numeric.Finite(gasEnergy) || gasEnergy <= 0 || !Numeric.Finite(gasTemperature) ||
            Math.Abs(t - gasTemperature) > 1e-8 + 128 * GearReference.Epsilon * t || Math.Abs(t - liquidTemperature) > 1e-8 + 128 * GearReference.Epsilon * t) return false;
        state = new(new(result.LiquidMassKilograms, liquidEnergy), result.VaporMassKilograms, gasEnergy, t); return true;
    }
}
