// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public enum FuelNozzleStatus { Success, InvalidInput, TemperatureOutOfRange, NoConvergence }

public readonly record struct FrozenFuelNozzlePoint(double MassFlowKilogramsPerSecond,
    double ThroatPressurePascals, double ExitTemperatureKelvin, double ExitVelocityMetersPerSecond,
    double VaporMassFraction, double StagnationEnthalpyJoulesPerKilogram, double ExitInternalEnergyJoulesPerKilogram,
    double ExitPressureWorkJoulesPerKilogram, double KineticEnergyJoulesPerKilogram,
    double JetMomentumNewtons, bool Choked);

/// <summary>
/// Homogeneous frozen-quality, common-temperature isentropic nozzle reference.
/// Liquid is incompressible and vapor ideal; phase mass transfer during transit is frozen.
/// A minimum-area throat limits mass flux. Fully expanded jet kinetic energy is reported
/// separately for an explicit downstream thermalization/momentum boundary.
/// </summary>
public sealed class FrozenFuelNozzle
{
    public FuelPhaseEquilibrium Material { get; }
    public double LiquidDensityKilogramsPerCubicMeter { get; }
    public double AreaSquareMeters { get; }
    public double DischargeCoefficient { get; }

    public FrozenFuelNozzle(FuelPhaseEquilibrium material, double liquidDensityKilogramsPerCubicMeter,
        double areaSquareMeters, double dischargeCoefficient)
    {
        Material = material ?? throw new ArgumentNullException(nameof(material));
        if (!GearReference.Finite(liquidDensityKilogramsPerCubicMeter, areaSquareMeters, dischargeCoefficient, 0) ||
            liquidDensityKilogramsPerCubicMeter <= 0 || areaSquareMeters <= 0 || dischargeCoefficient <= 0 || dischargeCoefficient > 1)
            throw new ArgumentException("Use positive finite liquid density and nozzle area, with discharge coefficient in (0,1].");
        LiquidDensityKilogramsPerCubicMeter = liquidDensityKilogramsPerCubicMeter;
        AreaSquareMeters = areaSquareMeters; DischargeCoefficient = dischargeCoefficient;
    }

    public FuelNozzleStatus Evaluate(double upstreamPressurePascals, double upstreamTemperatureKelvin,
        double vaporMassFraction, double receiverPressurePascals, double opening, out FrozenFuelNozzlePoint point)
    {
        point = default;
        if (!GearReference.Finite(upstreamPressurePascals, upstreamTemperatureKelvin, vaporMassFraction, receiverPressurePascals) ||
            !Numeric.Finite(opening) || upstreamPressurePascals <= 0 || receiverPressurePascals <= 0 || vaporMassFraction < 0 || vaporMassFraction > 1 || opening < 0 || opening > 1)
            return FuelNozzleStatus.InvalidInput;
        if (!Material.TrySaturationPressure(upstreamTemperatureKelvin, out _)) return FuelNozzleStatus.TemperatureOutOfRange;
        double x = vaporMassFraction, r = Material.GasConstantJoulesPerKilogramKelvin;
        double cv = Material.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin, cl = Material.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin;
        double cp = (1 - x) * cl + x * (cv + r), exponent = x * r / cp;
        double liquidVolume = (1 - x) / LiquidDensityKilogramsPerCubicMeter;
        double pressureEnergy = liquidVolume * upstreamPressurePascals, gasEnergy = x * r * upstreamTemperatureKelvin;
        double heatEnergy = cp * upstreamTemperatureKelvin;
        double upstreamInternal = ((1 - x) * cl + x * cv) * upstreamTemperatureKelvin + (1 - x) * Material.Caloric.LiquidEnergyOffsetJoulesPerKilogram;
        double enthalpy = upstreamInternal + pressureEnergy + gasEnergy;
        if (!GearReference.Finite(cp, exponent, heatEnergy, enthalpy) || cp <= 0 || exponent < 0 || exponent >= 1 ||
            upstreamPressurePascals / LiquidDensityKilogramsPerCubicMeter / r / upstreamTemperatureKelvin >= 1 - 64 * GearReference.Epsilon)
            return FuelNozzleStatus.InvalidInput;
        if (receiverPressurePascals >= upstreamPressurePascals || opening == 0)
        {
            point = new(0, upstreamPressurePascals, upstreamTemperatureKelvin, 0, x, enthalpy, upstreamInternal, pressureEnergy + gasEnergy, 0, 0, false);
            return FuelNozzleStatus.Success;
        }
        double logBack = Numeric.Log1p(-(upstreamPressurePascals - receiverPressurePascals) / upstreamPressurePascals);
        // Far below upstream pressure, subtraction may round to one; use the ratio's logs.
        if (!Numeric.Finite(logBack)) logBack = Math.Log(receiverPressurePascals) - Math.Log(upstreamPressurePascals);
        double exitTemperature = upstreamTemperatureKelvin * Math.Exp(exponent * logBack);
        if (!Numeric.Finite(exitTemperature) || exitTemperature < Material.MinimumTemperatureKelvin || exitTemperature > Material.MaximumTemperatureKelvin)
            return FuelNozzleStatus.TemperatureOutOfRange;
        double Drop(double logRatio) => -heatEnergy * Numeric.Expm1(exponent * logRatio) - pressureEnergy * Numeric.Expm1(logRatio);
        double LogVolumeEnergy(double logRatio)
        {
            double a = pressureEnergy == 0 ? double.NegativeInfinity : Math.Log(pressureEnergy);
            double b = gasEnergy == 0 ? double.NegativeInfinity : Math.Log(gasEnergy) + (exponent - 1) * logRatio;
            if (double.IsNegativeInfinity(a)) return b;
            if (double.IsNegativeInfinity(b)) return a;
            double high = Math.Max(a, b); return high + Numeric.Log1p(Math.Exp(Math.Min(a, b) - high));
        }
        double CriticalResidual(double logRatio)
        {
            double drop = Drop(logRatio);
            if (drop == 0) return double.PositiveInfinity;
            return 2 * LogVolumeEnergy(logRatio) - (Math.Log(2) + Math.Log(drop) + Math.Log(gasEnergy) + Math.Log(1 - exponent) + (exponent - 2) * logRatio);
        }
        double throatLog = logBack; bool choked = false;
        if (x > 0 && CriticalResidual(logBack) < 0)
        {
            double lo = logBack, hi = 0;
            // The frozen-mixture flux has a unique maximum: for D=h0-h,
            // d/dr[(p0 v)^2 + 2 D d(p0 v)/dr] = 2 D d²(p0 v)/dr² > 0.
            for (int iteration = 0; iteration < 80; ++iteration)
            {
                double mid = (lo + hi) / 2;
                if (CriticalResidual(mid) > 0) hi = mid; else lo = mid;
            }
            throatLog = (lo + hi) / 2; choked = true;
        }
        double throatDrop = Drop(throatLog), kinetic = Drop(logBack);
        double velocity = Math.Sqrt(2 * kinetic), throatVelocity = Math.Sqrt(2 * throatDrop);
        double throatSpecificVolume = Math.Exp(LogVolumeEnergy(throatLog) - Math.Log(upstreamPressurePascals));
        double rate = opening * DischargeCoefficient * AreaSquareMeters * throatVelocity / throatSpecificVolume;
        double exitInternal = ((1 - x) * cl + x * cv) * exitTemperature + (1 - x) * Material.Caloric.LiquidEnergyOffsetJoulesPerKilogram;
        double exitPressureWork = liquidVolume * receiverPressurePascals + x * r * exitTemperature;
        double throatPressure = choked ? upstreamPressurePascals * Math.Exp(throatLog) : receiverPressurePascals, momentum = rate * velocity;
        if (!GearReference.Finite(rate, kinetic, velocity, throatPressure) || !GearReference.Finite(exitInternal, exitPressureWork, momentum, throatSpecificVolume) ||
            rate < 0 || kinetic < 0 || throatPressure < receiverPressurePascals || throatSpecificVolume <= 0)
            return FuelNozzleStatus.NoConvergence;
        point = new(rate, throatPressure, exitTemperature, velocity, x, enthalpy, exitInternal, exitPressureWork, kinetic, momentum, choked);
        return FuelNozzleStatus.Success;
    }
}
