// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>
/// Calorically perfect ideal gas of one fixed composition. Instances are immutable and
/// carry the precomputed nozzle coefficients used by <see cref="Orifice"/>.
/// </summary>
public sealed class IdealGas
{
    /// <summary>Specific gas constant R in J/(kg*K).</summary>
    public double GasConstantJoulePerKilogramKelvin { get; }
    /// <summary>Ratio of specific heats, strictly greater than one.</summary>
    public double Gamma { get; }
    /// <summary>Isochoric specific heat cv = R/(gamma-1) in J/(kg*K).</summary>
    public double IsochoricHeatCapacityJoulePerKilogramKelvin { get; }
    /// <summary>Isobaric specific heat cp = gamma*R/(gamma-1) in J/(kg*K).</summary>
    public double IsobaricHeatCapacityJoulePerKilogramKelvin { get; }
    /// <summary>Downstream/upstream static pressure ratio at which an ideal nozzle chokes.</summary>
    public double CriticalPressureRatio { get; }

    /// <summary>Choked mass flux per unit effective area: mdot = coefficient * p_upstream / sqrt(T_upstream).</summary>
    public double ChokedMassFluxCoefficient { get; }
    /// <summary>Subcritical companion, multiplying sqrt of <see cref="Orifice.SubcriticalFlowFunction"/>.</summary>
    public double SubcriticalMassFluxCoefficient { get; }

    public IdealGas(double gasConstantJoulePerKilogramKelvin, double gamma)
    {
        if (!Numeric.Finite(gasConstantJoulePerKilogramKelvin) || gasConstantJoulePerKilogramKelvin <= 0 ||
            !Numeric.Finite(gamma) || gamma <= 1)
            throw new ArgumentException("Require a positive specific gas constant and gamma > 1.");
        GasConstantJoulePerKilogramKelvin = gasConstantJoulePerKilogramKelvin;
        Gamma = gamma;
        IsochoricHeatCapacityJoulePerKilogramKelvin = gasConstantJoulePerKilogramKelvin / (gamma - 1);
        IsobaricHeatCapacityJoulePerKilogramKelvin = gamma * IsochoricHeatCapacityJoulePerKilogramKelvin;
        CriticalPressureRatio = Math.Pow(2 / (gamma + 1), gamma / (gamma - 1));
        ChokedMassFluxCoefficient = Math.Sqrt(gamma / gasConstantJoulePerKilogramKelvin) *
            Math.Pow(2 / (gamma + 1), (gamma + 1) / (2 * (gamma - 1)));
        SubcriticalMassFluxCoefficient = Math.Sqrt(2 * gamma / (gasConstantJoulePerKilogramKelvin * (gamma - 1)));
        if (!Numeric.Finite(IsobaricHeatCapacityJoulePerKilogramKelvin) || !Numeric.Finite(ChokedMassFluxCoefficient) ||
            !Numeric.Finite(SubcriticalMassFluxCoefficient) || !(CriticalPressureRatio > 0) || !(CriticalPressureRatio < 1))
            throw new ArgumentException("Gas properties exceed the supported binary64 range.");
    }

    /// <summary>Air used by the existing research samples. Parameters remain <c>unverified</c>.</summary>
    public static IdealGas Air { get; } = new(287, 1.4);

    /// <summary>Static pressure from density and temperature.</summary>
    public double Pressure(double densityKilogramsPerCubicMeter, double temperatureKelvin) =>
        densityKilogramsPerCubicMeter * GasConstantJoulePerKilogramKelvin * temperatureKelvin;
    /// <summary>Specific enthalpy h = cp*T of a stream leaving a volume at this temperature.</summary>
    public double SpecificEnthalpy(double temperatureKelvin) =>
        IsobaricHeatCapacityJoulePerKilogramKelvin * temperatureKelvin;
    /// <summary>Internal energy of <paramref name="massKilograms"/> at this temperature.</summary>
    public double InternalEnergy(double massKilograms, double temperatureKelvin) =>
        massKilograms * IsochoricHeatCapacityJoulePerKilogramKelvin * temperatureKelvin;
}

/// <summary>
/// Extensive state of one finite gas volume. Mass and internal energy are the independent states,
/// so gas exchange and wall heating change the state without an assumed isentropic history.
/// </summary>
public readonly record struct GasVolumeState(double MassKilograms, double InternalEnergyJoules, double VolumeCubicMeters)
{
    /// <summary>A volume filled to the requested pressure and temperature.</summary>
    public static GasVolumeState FromPressure(IdealGas gas, double volumeCubicMeters, double pressurePascals, double temperatureKelvin)
    {
        if (gas is null) throw new ArgumentNullException(nameof(gas));
        if (!Numeric.Finite(volumeCubicMeters) || volumeCubicMeters <= 0 || !Numeric.Finite(pressurePascals) || pressurePascals <= 0 ||
            !Numeric.Finite(temperatureKelvin) || temperatureKelvin <= 0)
            throw new ArgumentException("Require a positive volume, absolute pressure and absolute temperature.");
        double mass = pressurePascals * volumeCubicMeters / (gas.GasConstantJoulePerKilogramKelvin * temperatureKelvin);
        return new(mass, gas.InternalEnergy(mass, temperatureKelvin), volumeCubicMeters);
    }

    /// <summary>True when mass, internal energy and volume are finite and strictly positive.</summary>
    public bool IsPhysical =>
        Numeric.Finite(MassKilograms) && MassKilograms > 0 &&
        Numeric.Finite(InternalEnergyJoules) && InternalEnergyJoules > 0 &&
        Numeric.Finite(VolumeCubicMeters) && VolumeCubicMeters > 0;

    public double DensityKilogramsPerCubicMeter => MassKilograms / VolumeCubicMeters;

    public double Temperature(IdealGas gas)
    {
        if (gas is null) throw new ArgumentNullException(nameof(gas));
        return InternalEnergyJoules / (MassKilograms * gas.IsochoricHeatCapacityJoulePerKilogramKelvin);
    }

    /// <summary>Static pressure (gamma-1)*U/V, which is exact for a calorically perfect ideal gas.</summary>
    public double Pressure(IdealGas gas)
    {
        if (gas is null) throw new ArgumentNullException(nameof(gas));
        return (gas.Gamma - 1) * InternalEnergyJoules / VolumeCubicMeters;
    }

    public double SpecificEnthalpy(IdealGas gas) => (gas ?? throw new ArgumentNullException(nameof(gas))).SpecificEnthalpy(Temperature(gas));

    /// <summary>Add transported mass and energy. Volume is unchanged; a moving boundary is applied by the owner.</summary>
    public GasVolumeState Add(double massKilograms, double energyJoules) =>
        new(MassKilograms + massKilograms, InternalEnergyJoules + energyJoules, VolumeCubicMeters);
}

/// <summary>Signed flow through a restriction. Positive means the first endpoint feeds the second.</summary>
public readonly record struct OrificeFlow(double MassFlowKilogramsPerSecond, double EnthalpyFlowWatts, bool Choked);

/// <summary>
/// Ideal compressible flow through a restriction, using the standard isentropic nozzle relations with
/// a discharge coefficient and a dimensionless opening fraction. Flow is evaluated in both directions
/// from static endpoint states; upstream stagnation and static conditions are treated as equal, which
/// is the usual quasi-steady control-volume approximation and is not valid for high-Mach chamber flow.
/// The subcritical branch has an infinite derivative at unit pressure ratio; an implicit solver that
/// uses this model must bracket or damp that point rather than rely on a Newton step through it.
/// </summary>
public sealed class Orifice
{
    /// <summary>Geometric reference area at full opening, in square metres.</summary>
    public double AreaSquareMeters { get; }
    /// <summary>Dimensionless discharge coefficient in (0, 1].</summary>
    public double DischargeCoefficient { get; }

    public Orifice(double areaSquareMeters, double dischargeCoefficient = 1)
    {
        if (!Numeric.Finite(areaSquareMeters) || areaSquareMeters <= 0 ||
            !Numeric.Finite(dischargeCoefficient) || dischargeCoefficient <= 0 || dischargeCoefficient > 1)
            throw new ArgumentException("Require a positive area and a discharge coefficient in (0, 1].");
        AreaSquareMeters = areaSquareMeters;
        DischargeCoefficient = dischargeCoefficient;
    }

    /// <summary>Effective flow area for a dimensionless opening fraction in [0, 1].</summary>
    public double EffectiveArea(double opening)
    {
        if (!Numeric.Finite(opening) || opening < 0 || opening > 1)
            throw new ArgumentOutOfRangeException(nameof(opening), "Opening must be a fraction in [0, 1].");
        return AreaSquareMeters * DischargeCoefficient * opening;
    }

    /// <summary>
    /// Signed mass and enthalpy flow from endpoint A to endpoint B. Both endpoints supply static
    /// pressure and absolute temperature, so a fixed reservoir is passed the same way as a finite volume.
    /// </summary>
    public OrificeFlow Evaluate(IdealGas gas, double pressureAPascals, double temperatureAKelvin,
        double pressureBPascals, double temperatureBKelvin, double opening)
    {
        if (gas is null) throw new ArgumentNullException(nameof(gas));
        double area = EffectiveArea(opening);
        if (!Numeric.Finite(pressureAPascals) || pressureAPascals <= 0 || !Numeric.Finite(temperatureAKelvin) || temperatureAKelvin <= 0 ||
            !Numeric.Finite(pressureBPascals) || pressureBPascals <= 0 || !Numeric.Finite(temperatureBKelvin) || temperatureBKelvin <= 0)
            throw new ArgumentException("Require positive absolute pressures and temperatures at both endpoints.");
        if (area == 0 || pressureAPascals == pressureBPascals) return new(0, 0, false);
        bool forward = pressureAPascals > pressureBPascals;
        double upstream = forward ? pressureAPascals : pressureBPascals;
        double downstream = forward ? pressureBPascals : pressureAPascals;
        double temperature = forward ? temperatureAKelvin : temperatureBKelvin;
        double ratio = downstream / upstream;
        bool choked = ratio <= gas.CriticalPressureRatio;
        double flux = choked
            ? gas.ChokedMassFluxCoefficient
            : gas.SubcriticalMassFluxCoefficient * Math.Sqrt(SubcriticalFlowFunction(ratio, gas.Gamma));
        double massFlow = area * flux * upstream / Math.Sqrt(temperature);
        if (!Numeric.Finite(massFlow) || massFlow < 0)
            throw new ArgumentException("Orifice flow exceeds the supported binary64 range.");
        double enthalpyFlow = massFlow * gas.SpecificEnthalpy(temperature);
        return forward ? new(massFlow, enthalpyFlow, choked) : new(-massFlow, -enthalpyFlow, choked);
    }

    /// <summary>The dimensionless subcritical flow function pr^(2/gamma) - pr^((gamma+1)/gamma).</summary>
    // Written as pr^(2/g) * (1 - pr^((g-1)/g)) so that the difference of two
    // nearly equal powers does not cancel as the pressure ratio approaches one.
    public static double SubcriticalFlowFunction(double ratio, double gamma)
    {
        if (!Numeric.Finite(ratio) || ratio <= 0 || ratio > 1)
            throw new ArgumentOutOfRangeException(nameof(ratio), "The pressure ratio must lie in (0, 1].");
        if (!Numeric.Finite(gamma) || gamma <= 1)
            throw new ArgumentOutOfRangeException(nameof(gamma), "Gamma must be greater than one.");
        double logRatio = Math.Log(ratio);
        double value = Math.Exp(2 * logRatio / gamma) * -Numeric.Expm1((gamma - 1) * logRatio / gamma);
        return value > 0 ? value : 0;
    }
}
