// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public readonly record struct CompressibleFuelPoint(double DensityKilogramsPerCubicMeter, double TemperatureKelvin,
    double PressurePascals, double SpecificInternalEnergyJoulesPerKilogram, double SpecificEntropyJoulesPerKilogramKelvin,
    double IsochoricHeatCapacityJoulesPerKilogramKelvin, double SoundSpeedMetersPerSecond, double VaporMassFraction, FuelVolumePhase Phase);

public readonly record struct CompressibleFuelSaturation(double PressurePascals, double LiquidSpecificVolumeCubicMetersPerKilogram,
    double VaporSpecificVolumeCubicMetersPerKilogram, double LatentEnthalpyJoulesPerKilogram, double PressureDerivativePascalsPerKelvin);

/// <summary>
/// Homogeneous equilibrium of an ideal vapor and a compressible liquid with zero thermal
/// expansion. The liquid cold energy is the integral of p=K*(rho/rho0-1), not an extra
/// wall energy. Chemical potentials, caloric energy, entropy and sound speed share it.
/// </summary>
public sealed partial class CompressibleFuel
{
    public FuelPhaseEquilibrium Material { get; }
    public double ReferenceLiquidDensityKilogramsPerCubicMeter { get; }
    public double LiquidBulkModulusPascals { get; }
    public double MaximumPressurePascals { get; }
    public double AcousticSpeedBoundMetersPerSecond { get; }

    public CompressibleFuel(FuelPhaseEquilibrium material, double referenceLiquidDensity, double liquidBulkModulus, double maximumPressure)
    {
        Material = material ?? throw new ArgumentNullException(nameof(material));
        ReferenceLiquidDensityKilogramsPerCubicMeter = referenceLiquidDensity; LiquidBulkModulusPascals = liquidBulkModulus; MaximumPressurePascals = maximumPressure;
        if (!GearReference.Finite(referenceLiquidDensity, liquidBulkModulus, maximumPressure, 0) || referenceLiquidDensity <= 0 || liquidBulkModulus <= 0 || maximumPressure <= 0 ||
            maximumPressure >= referenceLiquidDensity * material.GasConstantJoulesPerKilogramKelvin * material.MinimumTemperatureKelvin ||
            !TrySaturation(material.MinimumTemperatureKelvin, out _) || !TrySaturation(material.MaximumTemperatureKelvin, out _))
            throw new ArgumentException("Use positive finite density, liquid bulk modulus and pressure bound, with the complete coexistence curve inside the dilute-vapor domain.");
        double gamma = 1 + material.GasConstantJoulesPerKilogramKelvin / material.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin;
        AcousticSpeedBoundMetersPerSecond = Math.Sqrt(Math.Max(liquidBulkModulus / referenceLiquidDensity, gamma * material.GasConstantJoulesPerKilogramKelvin * material.MaximumTemperatureKelvin));
        if (!Numeric.Finite(AcousticSpeedBoundMetersPerSecond) || AcousticSpeedBoundMetersPerSecond <= 0) throw new ArgumentException("Acoustic bound is not representable.");
    }
    private double LiquidVolume(double pressure) => 1 / ReferenceLiquidDensityKilogramsPerCubicMeter / (1 + pressure / LiquidBulkModulusPascals);
    private double CompressionEnthalpy(double pressure) => LiquidBulkModulusPascals / ReferenceLiquidDensityKilogramsPerCubicMeter * Numeric.Log1p(pressure / LiquidBulkModulusPascals);
    private double ColdEnergy(double pressure)
    {
        double y = pressure / LiquidBulkModulusPascals, value;
        if (Math.Abs(y) < 1e-3)
        {
            // log(1+y)-y/(1+y), evaluated without cancellation near zero pressure.
            value = 0; double power = y * y;
            for (int n = 2; n <= 9; ++n) { value += (n % 2 == 0 ? 1 : -1) * (n - 1d) / n * power; power *= y; }
        }
        else value = Numeric.Log1p(y) - y / (1 + y);
        return LiquidBulkModulusPascals / ReferenceLiquidDensityKilogramsPerCubicMeter * value;
    }
    public bool TrySaturation(double temperatureKelvin, out CompressibleFuelSaturation saturation)
    {
        saturation = default;
        if (!Material.TrySaturationPressure(temperatureKelvin, out double negligible) || negligible >= MaximumPressurePascals) return false;
        double rt = Material.GasConstantJoulesPerKilogramKelvin * temperatureKelvin, lo = Math.Log(negligible), hi = Math.Log(MaximumPressurePascals);
        double Residual(double logPressure) => logPressure - Math.Log(negligible) - CompressionEnthalpy(Math.Exp(logPressure)) / rt;
        if (!(Residual(hi) > 0)) return false;
        double x = lo;
        for (int i = 0; i < 64; ++i)
        {
            double p = Math.Exp(x), residual = Residual(x), derivative = 1 - p * LiquidVolume(p) / rt;
            if (!Numeric.Finite(residual) || derivative <= 0) return false;
            if (Math.Abs(residual) <= 16 * GearReference.Epsilon * Math.Max(1, Math.Abs(x)))
            {
                double vl = LiquidVolume(p), vv = rt / p, latent = Material.LatentEnthalpy(temperatureKelvin) - CompressionEnthalpy(p);
                double slope = latent / (temperatureKelvin * (vv - vl));
                if (!GearReference.Finite(vl, vv, latent, slope) || vv <= vl || latent <= 0 || slope <= 0) return false;
                saturation = new(p, vl, vv, latent, slope); return true;
            }
            if (residual > 0) hi = x; else lo = x;
            double next = x - residual / derivative; x = next > lo && next < hi ? next : (lo + hi) / 2;
        }
        return false;
    }
    public bool Evaluate(double densityKilogramsPerCubicMeter, double temperatureKelvin, out CompressibleFuelPoint point)
    {
        point = default;
        if (!Numeric.Finite(densityKilogramsPerCubicMeter) || densityKilogramsPerCubicMeter <= 0 || !TrySaturation(temperatureKelvin, out var sat)) return false;
        double v = 1 / densityKilogramsPerCubicMeter, t = temperatureKelvin, r = Material.GasConstantJoulesPerKilogramKelvin;
        var c = Material.Caloric; double cv = c.VaporSpecificHeatJoulesPerKilogramKelvin, cl = c.LiquidSpecificHeatJoulesPerKilogramKelvin;
        double logT = Numeric.Log1p((t - c.SaturationTemperatureKelvin) / c.SaturationTemperatureKelvin), p, u, s, capacity, soundSquared, quality;
        FuelVolumePhase phase;
        if (v < sat.LiquidSpecificVolumeCubicMetersPerKilogram)
        {
            p = LiquidBulkModulusPascals * (densityKilogramsPerCubicMeter / ReferenceLiquidDensityKilogramsPerCubicMeter - 1);
            u = cl * t + c.LiquidEnergyOffsetJoulesPerKilogram + ColdEnergy(p); s = cl * logT;
            capacity = cl; soundSquared = LiquidBulkModulusPascals / ReferenceLiquidDensityKilogramsPerCubicMeter; quality = 0; phase = FuelVolumePhase.Liquid;
        }
        else if (v > sat.VaporSpecificVolumeCubicMetersPerKilogram)
        {
            p = r * t / v; u = cv * t; s = (cv + r) * logT - r * Math.Log(p / Material.ReferencePressurePascals) + c.LatentInternalEnergyJoulesPerKilogram / c.SaturationTemperatureKelvin + r;
            capacity = cv; soundSquared = (1 + r / cv) * r * t; quality = 1; phase = FuelVolumePhase.Vapor;
        }
        else
        {
            p = sat.PressurePascals; double vl = sat.LiquidSpecificVolumeCubicMetersPerKilogram, vv = sat.VaporSpecificVolumeCubicMetersPerKilogram;
            quality = (v - vl) / (vv - vl); double ul = cl * t + c.LiquidEnergyOffsetJoulesPerKilogram + ColdEnergy(p), uv = cv * t;
            double sl = cl * logT, sv = (cv + r) * logT - r * Math.Log(p / Material.ReferencePressurePascals) + c.LatentInternalEnergyJoulesPerKilogram / c.SaturationTemperatureKelvin + r;
            u = (1 - quality) * ul + quality * uv; s = (1 - quality) * sl + quality * sv;
            double vlT = -vl * sat.PressureDerivativePascalsPerKelvin / (LiquidBulkModulusPascals + p), vvT = vv / t - vv / p * sat.PressureDerivativePascalsPerKelvin;
            double qualityT = -((1 - quality) * vlT + quality * vvT) / (vv - vl);
            capacity = (1 - quality) * (cl - p * vlT) + quality * cv + qualityT * (uv - ul);
            soundSquared = v * v * t * sat.PressureDerivativePascalsPerKelvin * sat.PressureDerivativePascalsPerKelvin / capacity;
            phase = FuelVolumePhase.LiquidVapor;
        }
        if (!GearReference.Finite(p, u, s, capacity) || !GearReference.Finite(soundSquared, quality, v, 0) || p <= 0 || p > MaximumPressurePascals || capacity <= 0 || soundSquared <= 0 || quality < 0 || quality > 1) return false;
        point = new(densityKilogramsPerCubicMeter, t, p, u, s, capacity, Math.Sqrt(soundSquared), quality, phase); return true;
    }
    public bool Flash(double densityKilogramsPerCubicMeter, double specificInternalEnergyJoulesPerKilogram, out CompressibleFuelPoint point)
    {
        point = default;
        if (!Numeric.Finite(specificInternalEnergyJoulesPerKilogram)) return false;
        double lo = Material.MinimumTemperatureKelvin, hi = Material.MaximumTemperatureKelvin;
        if (!Evaluate(densityKilogramsPerCubicMeter, lo, out var lower) || !Evaluate(densityKilogramsPerCubicMeter, hi, out var upper) ||
            specificInternalEnergyJoulesPerKilogram < lower.SpecificInternalEnergyJoulesPerKilogram || specificInternalEnergyJoulesPerKilogram > upper.SpecificInternalEnergyJoulesPerKilogram) return false;
        double t = (lo + hi) / 2;
        for (int i = 0; i < 80; ++i)
        {
            if (!Evaluate(densityKilogramsPerCubicMeter, t, out var candidate)) return false;
            double error = candidate.SpecificInternalEnergyJoulesPerKilogram - specificInternalEnergyJoulesPerKilogram, correction = error / candidate.IsochoricHeatCapacityJoulesPerKilogramKelvin;
            if (Math.Abs(correction) <= 32 * GearReference.Epsilon * Math.Max(1, t)) { point = candidate with { SpecificInternalEnergyJoulesPerKilogram = specificInternalEnergyJoulesPerKilogram }; return true; }
            if (error > 0) hi = t; else lo = t;
            double next = t - correction; t = next > lo && next < hi ? next : (lo + hi) / 2;
        }
        return false;
    }
}
