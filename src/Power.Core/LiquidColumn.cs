// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public enum LiquidColumnStatus { Success, InvalidInput, PressureOutOfRange, StepTooLarge, NumericalFailure }
public readonly record struct LiquidColumnState(double VolumeFlowCubicMetersPerSecond);
public readonly record struct LiquidColumnStep(LiquidColumnState State, double ForwardVolumeCubicMeters,
    double ReverseVolumeCubicMeters, double PressureWorkJoules, double FrictionHeatJoules,
    double KineticEnergyChangeJoules, double EntropyProducedJoulesPerKelvin, int InternalIntervals)
{
    public double TransferredVolumeCubicMeters => ForwardVolumeCubicMeters - ReverseVolumeCubicMeters;
    public double EnergyResidualJoules => PressureWorkJoules - FrictionHeatJoules - KineticEnergyChangeJoules;
}

/// <summary>
/// Local single-phase momentum reference for a horizontal rigid constant-area liquid column.
/// Density and temperature are constant. Equal port velocities cancel advected kinetic flux;
/// prescribed absolute port pressures supply work and an isothermal bath receives friction heat.
/// This is not a compiled network component, a pressure-wave model or a cavitating pipe model.
/// </summary>
public sealed class LiquidColumn
{
    public double LengthMeters { get; }
    public double AreaSquareMeters { get; }
    public double DensityKilogramsPerCubicMeter { get; }
    public double InertancePascalSecondSquaredPerCubicMeter { get; }
    public double ColumnMassKilograms { get; }
    public double LinearResistancePascalSecondPerCubicMeter { get; }
    public double QuadraticResistancePascalSecondSquaredPerCubicMeterSquared { get; }
    public double TemperatureKelvin { get; }
    public double MinimumAbsolutePressurePascals { get; }

    public LiquidColumn(double lengthMeters, double areaSquareMeters, double densityKilogramsPerCubicMeter,
        double linearResistancePascalSecondPerCubicMeter, double quadraticResistancePascalSecondSquaredPerCubicMeterSquared,
        double temperatureKelvin, double minimumAbsolutePressurePascals)
    {
        if (!GearReference.Finite(lengthMeters, areaSquareMeters, densityKilogramsPerCubicMeter, temperatureKelvin) ||
            !GearReference.Finite(linearResistancePascalSecondPerCubicMeter, quadraticResistancePascalSecondSquaredPerCubicMeterSquared, minimumAbsolutePressurePascals, 0) ||
            lengthMeters <= 0 || areaSquareMeters <= 0 || densityKilogramsPerCubicMeter <= 0 || temperatureKelvin <= 0 || minimumAbsolutePressurePascals <= 0 ||
            linearResistancePascalSecondPerCubicMeter < 0 || quadraticResistancePascalSecondSquaredPerCubicMeterSquared < 0)
            throw new ArgumentException("Declare positive finite length, area, density, temperature and absolute pressure bound, with nonnegative loss coefficients.");
        LengthMeters = lengthMeters; AreaSquareMeters = areaSquareMeters; DensityKilogramsPerCubicMeter = densityKilogramsPerCubicMeter;
        LinearResistancePascalSecondPerCubicMeter = linearResistancePascalSecondPerCubicMeter;
        QuadraticResistancePascalSecondSquaredPerCubicMeterSquared = quadraticResistancePascalSecondSquaredPerCubicMeterSquared;
        TemperatureKelvin = temperatureKelvin; MinimumAbsolutePressurePascals = minimumAbsolutePressurePascals;
        InertancePascalSecondSquaredPerCubicMeter = densityKilogramsPerCubicMeter * lengthMeters / areaSquareMeters;
        ColumnMassKilograms = densityKilogramsPerCubicMeter * lengthMeters * areaSquareMeters;
        if (!Numeric.Finite(InertancePascalSecondSquaredPerCubicMeter) || !Numeric.Finite(1 / InertancePascalSecondSquaredPerCubicMeter) ||
            !Numeric.Finite(ColumnMassKilograms) || ColumnMassKilograms <= 0)
            throw new ArgumentException("Column geometry exceeds the supported binary64 range.");
    }

    /// <summary>Stateless implicit-midpoint interval. Rejections leave output default; reduce duration on StepTooLarge.</summary>
    public LiquidColumnStatus Advance(LiquidColumnState state, double pressureAPascals, double pressureBPascals,
        double durationSeconds, out LiquidColumnStep result)
    {
        result = default;
        double q0 = state.VolumeFlowCubicMetersPerSecond;
        if (!GearReference.Finite(q0, pressureAPascals, pressureBPascals, durationSeconds) || durationSeconds <= 0)
            return LiquidColumnStatus.InvalidInput;
        if (pressureAPascals < MinimumAbsolutePressurePascals || pressureBPascals < MinimumAbsolutePressurePascals)
            return LiquidColumnStatus.PressureOutOfRange;
        double pressure = pressureAPascals - pressureBPascals;
        if (!Numeric.Finite(pressure)) return LiquidColumnStatus.NumericalFailure;
        bool dissipative = LinearResistancePascalSecondPerCubicMeter != 0 || QuadraticResistancePascalSecondSquaredPerCubicMeterSquared != 0;
        double equilibrium = 0;
        if (dissipative && !Root(LinearResistancePascalSecondPerCubicMeter, QuadraticResistancePascalSecondSquaredPerCubicMeterSquared, pressure, out equilibrium))
            return LiquidColumnStatus.NumericalFailure;
        var status = Segment(q0, pressure, durationSeconds, equilibrium, dissipative, false, out var whole);
        if (status != LiquidColumnStatus.Success) return status;
        double q1 = whole.State.VolumeFlowCubicMetersPerSecond;
        if (q0 > 0 && q1 < 0 || q0 < 0 && q1 > 0)
        {
            // Solve the midpoint event equation with q1=0, rather than clipping
            // delivered volume or averaging away opposing flows and their heat.
            double half = q0 / 2, force = pressure - Loss(half);
            double crossing = -InertancePascalSecondSquaredPerCubicMeter * q0 / force;
            if (!Numeric.Finite(crossing) || crossing <= 0 || crossing >= durationSeconds) return LiquidColumnStatus.NumericalFailure;
            status = Segment(q0, pressure, crossing, equilibrium, dissipative, true, out var before);
            if (status != LiquidColumnStatus.Success) return status;
            status = Segment(0, pressure, durationSeconds - crossing, equilibrium, dissipative, false, out var after);
            if (status != LiquidColumnStatus.Success) return status;
            whole = new(after.State, before.ForwardVolumeCubicMeters + after.ForwardVolumeCubicMeters,
                before.ReverseVolumeCubicMeters + after.ReverseVolumeCubicMeters,
                before.PressureWorkJoules + after.PressureWorkJoules, before.FrictionHeatJoules + after.FrictionHeatJoules,
                before.KineticEnergyChangeJoules + after.KineticEnergyChangeJoules,
                before.EntropyProducedJoulesPerKelvin + after.EntropyProducedJoulesPerKelvin, 2);
        }
        if (!GearReference.Finite(whole.ForwardVolumeCubicMeters, whole.ReverseVolumeCubicMeters, whole.PressureWorkJoules, whole.FrictionHeatJoules) ||
            !GearReference.Finite(whole.KineticEnergyChangeJoules, whole.EntropyProducedJoulesPerKelvin, whole.EnergyResidualJoules, 0))
            return LiquidColumnStatus.NumericalFailure;
        result = whole; return LiquidColumnStatus.Success;
    }

    private double Loss(double flow) => LinearResistancePascalSecondPerCubicMeter * flow + QuadraticResistancePascalSecondSquaredPerCubicMeterSquared * flow * Math.Abs(flow);
    private static bool Root(double linear, double quadratic, double right, out double flow)
    {
        flow = 0;
        if (!GearReference.Finite(linear, quadratic, right, 0) || linear < 0 || quadratic < 0) return false;
        if (right == 0) return linear > 0 || quadratic > 0;
        if (quadratic == 0) flow = right / linear;
        else
        {
            double discriminant = linear * linear + 4 * quadratic * Math.Abs(right);
            if (!Numeric.Finite(discriminant) || discriminant <= 0) return false;
            flow = right / (.5 * linear + .5 * Math.Sqrt(discriminant));
        }
        return Numeric.Finite(flow);
    }
    private LiquidColumnStatus Segment(double q0, double pressure, double seconds, double equilibrium, bool dissipative, bool zeroEnd, out LiquidColumnStep step)
    {
        step = default;
        double inertia = InertancePascalSecondSquaredPerCubicMeter, mean;
        if (zeroEnd) mean = q0 / 2;
        else if (!Root(inertia + seconds / 2 * LinearResistancePascalSecondPerCubicMeter,
            seconds / 2 * QuadraticResistancePascalSecondSquaredPerCubicMeterSquared, inertia * q0 + seconds / 2 * pressure, out mean))
            return LiquidColumnStatus.NumericalFailure;
        double q1 = zeroEnd ? 0 : 2 * mean - q0;
        if (!Numeric.Finite(q1)) return LiquidColumnStatus.NumericalFailure;
        if (dissipative)
        {
            double tolerance = 64 * GearReference.Epsilon * Math.Max(Math.Abs(q0), Math.Max(Math.Abs(q1), Math.Abs(equilibrium)));
            if (q0 > equilibrium && q1 < equilibrium - tolerance || q0 < equilibrium && q1 > equilibrium + tolerance)
                return LiquidColumnStatus.StepTooLarge;
        }
        double volume = seconds * mean, work = pressure * volume, heat = seconds * mean * Loss(mean);
        double change = inertia * mean * (q1 - q0), initialEnergy = .5 * inertia * q0 * q0, finalEnergy = .5 * inertia * q1 * q1;
        double impulseResidual = inertia * (q1 - q0) - seconds * (pressure - Loss(mean));
        double impulseScale = inertia * (Math.Abs(q0) + Math.Abs(q1)) + seconds * (Math.Abs(pressure) + Math.Abs(Loss(mean)));
        if (!GearReference.Finite(volume, work, heat, change) || !GearReference.Finite(initialEnergy, finalEnergy, impulseResidual, impulseScale) || heat < 0 ||
            Math.Abs(impulseResidual) > 512 * GearReference.Epsilon * impulseScale ||
            Math.Abs(work - heat - change) > 512 * GearReference.Epsilon * (Math.Abs(work) + heat + initialEnergy + finalEnergy))
            return LiquidColumnStatus.NumericalFailure;
        step = new(new(q1), Math.Max(volume, 0), Math.Max(-volume, 0), work, heat, change, heat / TemperatureKelvin, 1);
        return LiquidColumnStatus.Success;
    }
}
