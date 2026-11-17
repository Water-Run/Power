// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public readonly record struct FiniteFuelPhaseState(FuelFilmSample Liquid, double VaporMassKilograms,
    double GasEnergyJoules, double TemperatureKelvin);
public readonly record struct FiniteFuelReceiverWork(FiniteFuelPhaseState State, double EnergyChangeJoules,
    double MeanPressurePascals, double EntropyResidualJoulesPerKelvin);

public sealed partial class FiniteTankFuelEquilibrium
{
    public double Entropy(FiniteTankPhasePoint point, double carrierMassKilograms)
    {
        double t = point.TemperatureKelvin, r = Material.GasConstantJoulesPerKilogramKelvin;
        double cv = Material.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin, cl = Material.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin;
        double reference = Material.Caloric.SaturationTemperatureKelvin;
        double logarithm = Numeric.Log1p((t - reference) / reference);
        double entropy = point.LiquidMassKilograms * cl * logarithm;
        if (point.VaporMassKilograms > 0)
            entropy += point.VaporMassKilograms * ((cv + r) * logarithm - r * Math.Log(point.VaporPressurePascals / Material.ReferencePressurePascals) +
                Material.Caloric.LatentInternalEnergyJoulesPerKilogram / reference + r);
        if (carrierMassKilograms > 0)
            entropy += carrierMassKilograms * (cv * logarithm + r * Math.Log(point.GasVolumeCubicMeters * Material.ReferencePressurePascals / (carrierMassKilograms * r * reference)));
        return entropy;
    }

    public bool TryAdiabaticCapacity(double fuelMassKilograms, double carrierMassKilograms, double energyJoules,
        double oldCapacityCubicMeters, double newCapacityCubicMeters, out FiniteFuelReceiverWork work)
    {
        work = default;
        if (!TryFlashEnergyAtCapacity(fuelMassKilograms, carrierMassKilograms, energyJoules, oldCapacityCubicMeters, out var initial) ||
            !TryPointAtCapacity(initial.TemperatureKelvin, fuelMassKilograms, carrierMassKilograms, oldCapacityCubicMeters, out var before)) return false;
        double delta = newCapacityCubicMeters - oldCapacityCubicMeters;
        if (delta == 0) { work = new(initial, 0, before.PressurePascals, 0); return true; }
        double target = Entropy(before, carrierMassKilograms), lo = Material.MinimumTemperatureKelvin, hi = Material.MaximumTemperatureKelvin;
        if (!TryPointAtCapacity(lo, fuelMassKilograms, carrierMassKilograms, newCapacityCubicMeters, out var lower) ||
            !TryPointAtCapacity(hi, fuelMassKilograms, carrierMassKilograms, newCapacityCubicMeters, out var upper) ||
            target < Entropy(lower, carrierMassKilograms) || target > Entropy(upper, carrierMassKilograms)) return false;
        double t = initial.TemperatureKelvin; FiniteTankPhasePoint result = default; bool converged = false;
        for (int iteration = 0; iteration < 80; ++iteration)
        {
            if (!TryPointAtCapacity(t, fuelMassKilograms, carrierMassKilograms, newCapacityCubicMeters, out result)) return false;
            double residual = Entropy(result, carrierMassKilograms) - target, correction = residual * t / result.HeatCapacityJoulesPerKelvin;
            if (!Numeric.Finite(correction)) return false;
            if (Math.Abs(correction) <= 32 * GearReference.Epsilon * Math.Max(1, t)) { converged = true; break; }
            if (residual > 0) hi = t; else lo = t;
            double next = t - correction; t = next > lo && next < hi ? next : (lo + hi) / 2;
        }
        if (!converged) return false;
        double capacity = (carrierMassKilograms + before.VaporMassKilograms) * Material.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin +
            before.LiquidMassKilograms * Material.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin;
        double change = capacity * (result.TemperatureKelvin - before.TemperatureKelvin) +
            (result.VaporMassKilograms - before.VaporMassKilograms) * Material.LatentInternalEnergy(result.TemperatureKelvin);
        double scale = capacity * before.TemperatureKelvin + fuelMassKilograms * Material.LatentInternalEnergy(before.TemperatureKelvin);
        if (Math.Abs(change) <= 1024 * GearReference.Epsilon * scale && Math.Abs(delta) <= Math.Sqrt(GearReference.Epsilon) * oldCapacityCubicMeters &&
            (before.LiquidMassKilograms == 0) == (result.LiquidMassKilograms == 0))
            change = -delta * (.5 * before.PressurePascals + .5 * result.PressurePascals);
        double mean = -change / delta;
        if (!Numeric.Finite(mean) || mean <= 0 || !TryFlashEnergyAtCapacity(fuelMassKilograms, carrierMassKilograms, energyJoules + change, newCapacityCubicMeters, out var state) ||
            !TryPointAtCapacity(state.TemperatureKelvin, fuelMassKilograms, carrierMassKilograms, newCapacityCubicMeters, out var final)) return false;
        double entropyResidual = Entropy(final, carrierMassKilograms) - target;
        if (Math.Abs(entropyResidual) > 512 * GearReference.Epsilon * (Math.Abs(target) + before.HeatCapacityJoulesPerKelvin + final.HeatCapacityJoulesPerKelvin)) return false;
        work = new(state, change, mean, entropyResidual); return true;
    }

    public bool TryHeatAtCapacity(double fuelMassKilograms, double carrierMassKilograms, double energyJoules, double capacityCubicMeters,
        double wallTemperatureKelvin, double wallCapacityJoulesPerKelvin, double conductanceWattsPerKelvin, double seconds, out FuelPhaseTransfer transfer)
    {
        transfer = default;
        if (!GearReference.Finite(wallTemperatureKelvin, wallCapacityJoulesPerKelvin, conductanceWattsPerKelvin, seconds) ||
            wallTemperatureKelvin <= 0 || wallCapacityJoulesPerKelvin <= 0 || conductanceWattsPerKelvin < 0 || seconds < 0) return false;
        double kh = conductanceWattsPerKelvin * seconds;
        if (!Numeric.Finite(kh)) return false;
        double alpha = kh == 0 ? 0 : wallCapacityJoulesPerKelvin / (1 + wallCapacityJoulesPerKelvin / kh), target = energyJoules + alpha * wallTemperatureKelvin;
        double lo = Material.MinimumTemperatureKelvin, hi = Material.MaximumTemperatureKelvin;
        if (!TryPointAtCapacity(lo, fuelMassKilograms, carrierMassKilograms, capacityCubicMeters, out var lower) ||
            !TryPointAtCapacity(hi, fuelMassKilograms, carrierMassKilograms, capacityCubicMeters, out var upper) ||
            target < lower.ThermalEnergyJoules + alpha * lo || target > upper.ThermalEnergyJoules + alpha * hi) return false;
        double t = (lo + hi) / 2; bool converged = false;
        for (int iteration = 0; iteration < 80; ++iteration)
        {
            if (!TryPointAtCapacity(t, fuelMassKilograms, carrierMassKilograms, capacityCubicMeters, out var point)) return false;
            double residual = point.ThermalEnergyJoules + alpha * t - target, correction = residual / (point.HeatCapacityJoulesPerKelvin + alpha);
            if (!Numeric.Finite(correction)) return false;
            if (Math.Abs(correction) <= 32 * GearReference.Epsilon * Math.Max(1, t)) { converged = true; break; }
            if (residual > 0) hi = t; else lo = t;
            double next = t - correction; t = next > lo && next < hi ? next : (lo + hi) / 2;
        }
        if (!converged) return false;
        double heat = alpha * (wallTemperatureKelvin - t), wall = wallTemperatureKelvin - heat / wallCapacityJoulesPerKelvin;
        if (!Numeric.Finite(wall) || wall <= 0 || !TryFlashEnergyAtCapacity(fuelMassKilograms, carrierMassKilograms, energyJoules + heat, capacityCubicMeters, out var state)) return false;
        transfer = new(state.Liquid, state.VaporMassKilograms, state.GasEnergyJoules, state.TemperatureKelvin, wall, 0, heat); return true;
    }
}
