// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>Discrete pressure PI loop with explicit gains, integer sampling period and actuator bounds.</summary>
public sealed record PressureControllerDefinition
{
    public ulong TargetChannel { get; init; }
    public ulong SamplePeriodNanoseconds { get; init; }
    public Quantity ProportionalGain { get; init; }
    public Quantity IntegralGain { get; init; }
    public Quantity MinimumVoltage { get; init; }
    public Quantity MaximumVoltage { get; init; }
    public Quantity InitialIntegralVoltage { get; init; }
}

public sealed record PressureDutyControllerDefinition
{
    public ulong TargetChannel { get; init; }
    public ulong SamplePeriodNanoseconds { get; init; }
    public Quantity ProportionalGain { get; init; }
    public Quantity IntegralGain { get; init; }
    public Quantity MinimumDuty { get; init; }
    public Quantity MaximumDuty { get; init; }
    public Quantity InitialIntegralDuty { get; init; }
}

public readonly record struct PressureDutyPiResult(double ErrorPascals, double IntegralDuty, double CommandDuty);

/// <summary>The same scalar clamping transition, with explicitly dimensionless duty gains and bounds.</summary>
public sealed class PressureDutyPiController
{
    private readonly PressurePiController _scalar;
    public PressureDutyPiController(double proportionalDutyPerPascal, double integralDutyPerPascalSecond, double minimumDuty, double maximumDuty)
    {
        if (minimumDuty < -1 || maximumDuty > 1) throw new ArgumentException("Duty bounds must lie within [-1,1].", nameof(maximumDuty));
        _scalar = new(proportionalDutyPerPascal, integralDutyPerPascalSecond, minimumDuty, maximumDuty);
    }
    public bool TrySample(double setpointPascals, double pressurePascals, double integralDuty, double elapsedSeconds, out PressureDutyPiResult result)
    {
        result = default;
        if (!_scalar.TrySample(setpointPascals, pressurePascals, integralDuty, elapsedSeconds, out var sample)) return false;
        result = new(sample.ErrorPascals, sample.IntegralVoltage, sample.CommandVoltage); return true;
    }
}

public readonly record struct PressurePiResult(double ErrorPascals, double IntegralVoltage, double CommandVoltage);

/// <summary>Pure sampled PI transition. Conditional integration prevents growth further into actuator saturation.</summary>
public sealed class PressurePiController
{
    public double ProportionalVoltsPerPascal { get; }
    public double IntegralVoltsPerPascalSecond { get; }
    public double MinimumVoltage { get; }
    public double MaximumVoltage { get; }
    public PressurePiController(double proportionalVoltsPerPascal, double integralVoltsPerPascalSecond,
        double minimumVoltage, double maximumVoltage)
    {
        if (!Numeric.Finite(proportionalVoltsPerPascal) || proportionalVoltsPerPascal < 0)
            throw new ArgumentException("Proportional gain must be finite and nonnegative.", nameof(proportionalVoltsPerPascal));
        if (!Numeric.Finite(integralVoltsPerPascalSecond) || integralVoltsPerPascalSecond < 0)
            throw new ArgumentException("Integral gain must be finite and nonnegative.", nameof(integralVoltsPerPascalSecond));
        if (!Numeric.Finite(minimumVoltage) || !Numeric.Finite(maximumVoltage) || minimumVoltage >= maximumVoltage)
            throw new ArgumentException("Voltage bounds must be finite and strictly increasing.", nameof(maximumVoltage));
        ProportionalVoltsPerPascal = proportionalVoltsPerPascal;
        IntegralVoltsPerPascalSecond = integralVoltsPerPascalSecond;
        MinimumVoltage = minimumVoltage; MaximumVoltage = maximumVoltage;
    }
    public bool TrySample(double setpointPascals, double pressurePascals, double integralVoltage,
        double elapsedSeconds, out PressurePiResult result)
    {
        result = default;
        if (!Numeric.Finite(setpointPascals) || !Numeric.Finite(pressurePascals) || !Numeric.Finite(integralVoltage) ||
            !Numeric.Finite(elapsedSeconds) || setpointPascals < 0 || pressurePascals < 0 || elapsedSeconds < 0) return false;
        double error = setpointPascals - pressurePascals, proportional = ProportionalVoltsPerPascal * error;
        double change = IntegralVoltsPerPascalSecond * elapsedSeconds * error;
        double candidate = integralVoltage + change, voltage = proportional + candidate;
        if (!GearReference.Finite(error, proportional, candidate, voltage)) return false;
        if (voltage > MaximumVoltage && change > 0 || voltage < MinimumVoltage && change < 0)
        { candidate = integralVoltage; voltage = proportional + candidate; }
        if (!Numeric.Finite(voltage)) return false;
        result = new(error, candidate, Math.Max(MinimumVoltage, Math.Min(MaximumVoltage, voltage)));
        return true;
    }
}

internal sealed record CompiledPressureController(int Component, int Pressure, int Motor, ulong TargetChannel,
    ulong Period, double InitialIntegral, PressurePiController Law, bool Duty = false);

internal sealed class PressureControllerState(int count)
{
    internal readonly double[] Integral = new double[count], Pressure = new double[count], Error = new double[count], Command = new double[count];
    internal void CopyFrom(PressureControllerState other)
    {
        Array.Copy(other.Integral, Integral, Integral.Length); Array.Copy(other.Pressure, Pressure, Pressure.Length);
        Array.Copy(other.Error, Error, Error.Length); Array.Copy(other.Command, Command, Command.Length);
    }
    internal bool Finite()
    {
        for (int i = 0; i < Integral.Length; ++i)
            if (!GearReference.Finite(Integral[i], Pressure[i], Error[i], Command[i])) return false;
        return true;
    }
    internal ulong Hash(ulong hash)
    {
        for (int i = 0; i < Integral.Length; ++i)
        {
            hash = Numeric.Hash(hash, Integral[i]); hash = Numeric.Hash(hash, Pressure[i]);
            hash = Numeric.Hash(hash, Error[i]); hash = Numeric.Hash(hash, Command[i]);
        }
        return hash;
    }
}
