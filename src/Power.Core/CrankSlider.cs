// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public readonly record struct CrankSliderSample(double DisplacementMeters, double VolumeCubicMeters,
    double VolumeDerivativeCubicMetersPerRadian);

/// <summary>Rigid, centered slider-crank geometry in SI units. Zero angle is top dead center.</summary>
public sealed class CrankSlider
{
    private readonly double _radius, _rod, _area;
    public double ClearanceVolumeCubicMeters { get; }
    public double SweptVolumeCubicMeters { get; }

    public CrankSlider(double boreMeters, double strokeMeters, double rodLengthMeters, double compressionRatio)
    {
        if (!Numeric.Finite(boreMeters) || boreMeters <= 0 || !Numeric.Finite(strokeMeters) || strokeMeters <= 0 ||
            !Numeric.Finite(rodLengthMeters) || rodLengthMeters <= strokeMeters / 2 ||
            !Numeric.Finite(compressionRatio) || compressionRatio <= 1)
            throw new ArgumentException("Require positive bore/stroke, rod length > half stroke, and compression ratio > 1.");
        _radius = strokeMeters / 2; _rod = rodLengthMeters; _area = Math.PI * boreMeters * boreMeters / 4;
        SweptVolumeCubicMeters = _area * strokeMeters;
        ClearanceVolumeCubicMeters = SweptVolumeCubicMeters / (compressionRatio - 1);
        if (!Numeric.Finite(_rod * _rod) || !Numeric.Finite(ClearanceVolumeCubicMeters + SweptVolumeCubicMeters) ||
            ClearanceVolumeCubicMeters <= 0 || SweptVolumeCubicMeters <= 0 || _radius <= 0)
            throw new ArgumentException("Geometry exceeds the supported binary64 range.");
    }

    private double RodProjection(double angle)
    {
        double lateral = _radius * Math.Sin(angle);
        return Math.Sqrt((_rod - lateral) * (_rod + lateral));
    }

    public CrankSliderSample Evaluate(double angleRadians)
    {
        if (!Numeric.Finite(angleRadians)) throw new ArgumentOutOfRangeException(nameof(angleRadians));
        double angle = angleRadians % (2 * Math.PI), sin = Math.Sin(angle), half = Math.Sin(angle / 2);
        double projection = RodProjection(angle);
        double displacement = 2 * _radius * half * half + _radius * _radius * sin * sin / (_rod + projection);
        double derivative = _area * (_radius * sin + _radius * _radius * sin * Math.Cos(angle) / projection);
        return new(displacement, ClearanceVolumeCubicMeters + _area * displacement, derivative);
    }

    // Analytic divided difference avoids subtracting nearly equal volumes at small steps and dead centers.
    internal double VolumeGradient(double angle, double delta)
    {
        angle %= 2 * Math.PI;
        return _area * (_radius * Sinc(delta / 2) * Math.Sin(angle + delta / 2) +
            _radius * _radius * Sinc(delta) * Math.Sin(2 * angle + delta) /
            (RodProjection(angle) + RodProjection(angle + delta)));
    }

    private static double Sinc(double x) => Math.Abs(x) < 1e-4 ? 1 - x * x / 6 + x * x * x * x / 120 : Math.Sin(x) / x;
}

internal sealed class CylinderPhysics
{
    internal readonly CrankSlider Geometry;
    internal readonly double Phase, InitialPressure, InitialTemperature, GasConstant, Gamma, BackPressure;
    internal readonly double ReferenceVolume, ReferenceEnergy, Mass;
    internal readonly double[] Parameters;

    internal CylinderPhysics(SealedCylinderDefinition definition, uint id, double initialAngle)
    {
        double Q(Quantity q, Unit unit, string field) => CompiledModel.Convert(q, unit, id, "cylinder." + field);
        double bore = Q(definition.Bore, Unit.Meter, "bore"), stroke = Q(definition.Stroke, Unit.Meter, "stroke"),
            rod = Q(definition.RodLength, Unit.Meter, "rod_length");
        Phase = Q(definition.Phase, Unit.Radian, "phase");
        InitialPressure = Q(definition.InitialPressure, Unit.Pascal, "initial_pressure");
        InitialTemperature = Q(definition.InitialTemperature, Unit.Kelvin, "initial_temperature");
        GasConstant = Q(definition.GasConstant, Unit.JoulePerKilogramKelvin, "gas_constant");
        BackPressure = Q(definition.BackPressure, Unit.Pascal, "back_pressure");
        Gamma = definition.Gamma;
        try { Geometry = new(bore, stroke, rod, definition.CompressionRatio); }
        catch (ArgumentException error) { throw new ModelCompileException(DiagnosticCode.Range, id, "cylinder.geometry", error.Message); }
        if (InitialPressure <= 0 || InitialTemperature <= 0 || GasConstant <= 0 || BackPressure < 0 ||
            !Numeric.Finite(Gamma) || Gamma <= 1)
            throw new ModelCompileException(DiagnosticCode.Range, id, "cylinder.gas", "Require positive pressure, temperature and gas constant, gamma > 1, and nonnegative back pressure.");
        ReferenceVolume = Geometry.Evaluate(Angle(initialAngle)).VolumeCubicMeters;
        ReferenceEnergy = InitialPressure * ReferenceVolume / (Gamma - 1);
        Mass = InitialPressure * ReferenceVolume / (GasConstant * InitialTemperature);
        Parameters = [bore, stroke, rod, Phase, definition.CompressionRatio, InitialPressure, InitialTemperature,
            GasConstant, Gamma, BackPressure];
        double phase = Phase % (2 * Math.PI);
        if (!Numeric.Finite(Mass) || Mass <= 0 || !Numeric.Finite(ReferenceEnergy) || ReferenceEnergy <= 0 ||
            !Finite(-phase) || !Finite(Math.PI - phase) || !Finite(Math.PI / 2 - phase))
            throw new ModelCompileException(DiagnosticCode.Range, id, "cylinder.gas", "Gas state overflows or underflows binary64 across the stroke.");
    }

    private double Angle(double crank) => crank % (2 * Math.PI) + Phase % (2 * Math.PI);
    internal CrankSliderSample GeometryAt(double crank) => Geometry.Evaluate(Angle(crank));
    internal double Pressure(double crank) => InitialPressure * Math.Pow(ReferenceVolume / GeometryAt(crank).VolumeCubicMeters, Gamma);
    internal double Temperature(double crank) => InitialTemperature * Math.Pow(ReferenceVolume / GeometryAt(crank).VolumeCubicMeters, Gamma - 1);
    internal double Energy(double crank) => ReferenceEnergy * Math.Pow(ReferenceVolume / GeometryAt(crank).VolumeCubicMeters, Gamma - 1);
    internal double EnergyChange(double crank) => ReferenceEnergy * Expm1((Gamma - 1) * Math.Log(ReferenceVolume / GeometryAt(crank).VolumeCubicMeters));
    internal double Torque(double crank) => (Pressure(crank) - BackPressure) * GeometryAt(crank).VolumeDerivativeCubicMetersPerRadian;
    internal bool Finite(double crank) => Numeric.Finite(Pressure(crank)) && Pressure(crank) > 0 &&
        Numeric.Finite(Temperature(crank)) && Temperature(crank) > 0 && Numeric.Finite(Energy(crank)) && Energy(crank) > 0 && Numeric.Finite(Torque(crank));

    internal double DiscreteTorque(double crank, double delta)
    {
        double angle = Angle(crank), volume = Geometry.Evaluate(angle).VolumeCubicMeters;
        double gradient = Geometry.VolumeGradient(angle, delta), z = gradient * delta / volume;
        double pressure = InitialPressure * Math.Pow(ReferenceVolume / volume, Gamma);
        double meanPressure = z == 0 ? pressure : pressure * -Expm1(-(Gamma - 1) * Log1p(z)) / ((Gamma - 1) * z);
        return (meanPressure - BackPressure) * gradient;
    }

    internal double BackPressureWork(double crank, double delta) => -BackPressure * Geometry.VolumeGradient(Angle(crank), delta) * delta;

    private static double Log1p(double x) => Math.Abs(x) < 1e-4 ? x * (1 + x * (-0.5 + x * (1.0 / 3 + x * (-0.25 + x / 5)))) : Math.Log(1 + x);
    private static double Expm1(double x) => Math.Abs(x) < 1e-4 ? x * (1 + x * (0.5 + x * (1.0 / 6 + x * (1.0 / 24 + x / 120)))) : Math.Exp(x) - 1;
}
