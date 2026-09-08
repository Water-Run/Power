// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public enum Unit
{
    None, KilogramMeterSquared, Radian, RadianPerSecond, NewtonMeter,
    NewtonMeterPerRadian, NewtonMeterSecondPerRadian, Kelvin, JoulePerKelvin,
    WattPerKelvin, Ohm, Henry, NewtonMeterPerAmpere, Ampere, Volt, Joule, Rpm, Degree,
    Meter, Millimeter, CubicMeter, Pascal, Bar, Kilogram, JoulePerKilogramKelvin
}

public enum Domain { Rotational = 1, Thermal = 2 }
public enum ComponentKind { Shaft = 1, DcMotor, TorqueSource, ThermalLink, SealedCylinder }
public enum Field
{
    Angle = 1, Speed, Temperature, Current, Twist, Torque,
    Pressure, Volume, Mass, InternalEnergy, PistonDisplacement,
    SourceWork = 16, HeatRejected, StoredEnergyChange, EnergyResidual
}
public enum SimulationStatus { Ok, InvalidTimeStep, InvalidInput, UnknownChannel, NumericalFailure, Busy, Cancelled }
public enum DiagnosticCode { Schema, Capacity, Id, Unit, Range, Connection, Channel, Solver }

public readonly record struct Quantity(double Value, Unit Unit);
public readonly record struct Scalar(ulong Channel, double Value);
public readonly record struct ScheduledInput(ulong TimeNanoseconds, ulong Channel, double Value);
public readonly record struct ChannelInfo(ulong Id, uint ObjectId, bool IsInput, Unit Unit, string Quantity);
public readonly record struct SnapshotInfo(ulong TimeNanoseconds, ulong StateHash, int ValueCount);
public sealed record ModelDiagnostic(DiagnosticCode Code, uint ObjectId, string Field, string Message);

public sealed record NodeDefinition(uint Id, Domain Domain, Quantity Storage, Quantity Initial, Quantity Position)
{
    public static NodeDefinition Rotor(uint id, double inertia, double speed = 0, double angle = 0) =>
        new(id, Domain.Rotational, new(inertia, Unit.KilogramMeterSquared),
            new(speed, Unit.RadianPerSecond), new(angle, Unit.Radian));
    public static NodeDefinition Thermal(uint id, double capacity, double temperature) =>
        new(id, Domain.Thermal, new(capacity, Unit.JoulePerKelvin), new(temperature, Unit.Kelvin), default);
}

public sealed record SealedCylinderDefinition
{
    public Quantity Bore { get; init; }
    public Quantity Stroke { get; init; }
    public Quantity RodLength { get; init; }
    public Quantity Phase { get; init; }
    public double CompressionRatio { get; init; }
    public Quantity InitialPressure { get; init; }
    public Quantity InitialTemperature { get; init; }
    public Quantity GasConstant { get; init; }
    public double Gamma { get; init; }
    public Quantity BackPressure { get; init; }
}

public sealed record ComponentDefinition
{
    public uint Id { get; init; }
    public ComponentKind Kind { get; init; }
    public uint NodeA { get; init; }
    public uint NodeB { get; init; }
    public uint HeatNode { get; init; }
    public ulong InputChannel { get; init; }
    public Quantity InitialInput { get; init; }
    public Quantity Stiffness { get; init; }
    public Quantity Damping { get; init; }
    public Quantity RestAngle { get; init; }
    public double Ratio { get; init; } = 1;
    public Quantity Resistance { get; init; }
    public Quantity Inductance { get; init; }
    public Quantity Coupling { get; init; }
    public Quantity InitialCurrent { get; init; }
    public Quantity Conductance { get; init; }
    public Quantity AmbientTemperature { get; init; }
    public SealedCylinderDefinition? Cylinder { get; init; }

    public static ComponentDefinition SealedCylinder(uint id, uint crank, SealedCylinderDefinition cylinder) => new()
    {
        Id = id, Kind = ComponentKind.SealedCylinder, NodeA = crank, Cylinder = cylinder
    };

    public static ComponentDefinition Shaft(uint id, uint a, uint b, double stiffness,
        double damping, double ratio = 1, uint heat = 0, double rest = 0) => new()
    {
        Id = id, Kind = ComponentKind.Shaft, NodeA = a, NodeB = b, HeatNode = heat,
        Stiffness = new(stiffness, Unit.NewtonMeterPerRadian),
        Damping = new(damping, Unit.NewtonMeterSecondPerRadian),
        RestAngle = new(rest, Unit.Radian), Ratio = ratio
    };
    public static ComponentDefinition Motor(uint id, uint a, uint heat, ulong channel,
        double resistance, double inductance, double coupling, double voltage, double current = 0) => new()
    {
        Id = id, Kind = ComponentKind.DcMotor, NodeA = a, HeatNode = heat, InputChannel = channel,
        Resistance = new(resistance, Unit.Ohm), Inductance = new(inductance, Unit.Henry),
        Coupling = new(coupling, Unit.NewtonMeterPerAmpere), InitialCurrent = new(current, Unit.Ampere),
        InitialInput = new(voltage, Unit.Volt)
    };
    public static ComponentDefinition Torque(uint id, uint a, ulong channel, double torque) => new()
    {
        Id = id, Kind = ComponentKind.TorqueSource, NodeA = a, InputChannel = channel,
        InitialInput = new(torque, Unit.NewtonMeter)
    };
    public static ComponentDefinition ThermalLink(uint id, uint a, uint b, double conductance,
        double ambient = 0) => new()
    {
        Id = id, Kind = ComponentKind.ThermalLink, NodeA = a, NodeB = b,
        Conductance = new(conductance, Unit.WattPerKelvin),
        AmbientTemperature = b == 0 ? new(ambient, Unit.Kelvin) : default
    };
}

public sealed record ModelDefinition
{
    public int SchemaVersion { get; init; } = 1;
    public ulong StepNanoseconds { get; init; } = 100_000;
    public NodeDefinition[] Nodes { get; init; } = [];
    public ComponentDefinition[] Components { get; init; } = [];
}

public sealed class ModelCompileException(DiagnosticCode code, uint objectId, string field, string message)
    : ArgumentException($"Object {objectId}, {field}: {message}")
{
    public DiagnosticCode Code { get; } = code;
    public uint ObjectId { get; } = objectId;
    public string Field { get; } = field;
}

public static class Channels
{
    public static ulong Output(uint objectId, Field field) => 0x8000000000000000UL | ((ulong)objectId << 8) | (uint)field;
}
