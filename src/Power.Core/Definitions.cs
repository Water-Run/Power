// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public enum Unit
{
    None, KilogramMeterSquared, Radian, RadianPerSecond, NewtonMeter,
    NewtonMeterPerRadian, NewtonMeterSecondPerRadian, Kelvin, JoulePerKelvin,
    WattPerKelvin, Ohm, Henry, NewtonMeterPerAmpere, Ampere, Volt, Joule, Rpm, Degree,
    Meter, Millimeter, CubicMeter, Pascal, Bar, Kilogram, JoulePerKilogramKelvin,
    Liter, SquareMeter, SquareMillimeter, KilogramPerSecond, Watt, Fraction, JoulePerKilogram, StateCode, NewtonMeterSecondSquaredPerRadianSquared, CubicMeterPerPascal, CubicMeterPerSecondPascal, CubicMeterPerSecondSqrtPascal, CubicMeterPerSecond, Newton, CubicMeterPerRadian
}

public enum Domain { Rotational = 1, Thermal = 2, Gas = 3, Hydraulic = 4 }
public enum ComponentKind { Shaft = 1, DcMotor, TorqueSource, ThermalLink, SealedCylinder, GasOrifice, GasHeatLink, GasCylinder, PremixedCombustion, Clutch, IdealGear, PlanetaryGear, TorqueConverter, HydraulicResistance, HydraulicOrifice, HydraulicClutch, HydraulicPump, HydraulicRelief }
public enum Field
{
    Angle = 1, Speed, Temperature, Current, Twist, Torque,
    Pressure, Volume, Mass, InternalEnergy, PistonDisplacement,
    MassFlow = 12, HeatFlow = 13, Opening = 14,
    SourceWork = 16, HeatRejected, StoredEnergyChange, EnergyResidual,
    ReservoirEnthalpy = 20, MassResidual = 21,
    FuelMass = 22, FreshAirMass, ProductMass, ChemicalEnergy, FuelBurned, HeatReleased,
    FuelEnergyIn, FuelResidual, FreshAirResidual, BurnFrontier, SlipSpeed, ClutchMode, FrictionHeat, TorqueAtB, TorqueAtC, ConstraintError, FluidHeat, SpeedRatio, ConverterDrive, VolumeFlow, HydraulicVolumeIn, HydraulicVolumeResidual, HydraulicWork, ClampForce, StaticCapacity, SlidingCapacity, HydraulicPower
}
public enum SimulationStatus { Ok, InvalidTimeStep, InvalidInput, UnknownChannel, NumericalFailure, Busy, Cancelled }
public enum DiagnosticCode { Schema, Capacity, Id, Unit, Range, Connection, Channel, Solver }

public readonly record struct Quantity(double Value, Unit Unit);
public readonly record struct Scalar(ulong Channel, double Value);
public readonly record struct ScheduledInput(ulong TimeNanoseconds, ulong Channel, double Value);
public readonly record struct ChannelInfo(ulong Id, uint ObjectId, bool IsInput, Unit Unit, string Quantity);
public readonly record struct SnapshotInfo(ulong TimeNanoseconds, ulong StateHash, int ValueCount);
public sealed record ModelDiagnostic(DiagnosticCode Code, uint ObjectId, string Field, string Message);

/// <summary>Composition of one gas volume. Connected volumes must agree until species mixing exists.</summary>
public sealed record GasDefinition
{
    public Quantity GasConstant { get; init; }
    public double Gamma { get; init; }
    /// <summary>Optional fuel/air/products bookkeeping with constant caloric gas properties.</summary>
    public PremixedGasDefinition? Premixed { get; init; }
}

public sealed record MassFractions(double Fuel, double FreshAir);

public sealed record ClutchDefinition
{
    public Quantity StaticCapacity { get; init; }
    public Quantity SlidingCapacity { get; init; }
}

public sealed record PremixedGasDefinition
{
    public Quantity LowerHeatingValue { get; init; }
    public double StoichiometricAirFuelRatio { get; init; }
    public MassFractions? InitialFractions { get; init; }
}

/// <summary>Prescribed forward-crank Wiebe consumption of the available limiting reactant.</summary>
public sealed record WiebeCombustionDefinition
{
    public Quantity CycleAngle { get; init; }
    public Quantity StartAngle { get; init; }
    public Quantity DurationAngle { get; init; }
    public double ShapeExponent { get; init; }
    public double BurnCoefficient { get; init; }
}

public sealed record NodeDefinition(uint Id, Domain Domain, Quantity Storage, Quantity Initial, Quantity Position)
{
    /// <summary>Required for, and only for, <see cref="Domain.Gas"/> nodes.</summary>
    public GasDefinition? Gas { get; init; }

    public static NodeDefinition Rotor(uint id, double inertia, double speed = 0, double angle = 0) =>
        new(id, Domain.Rotational, new(inertia, Unit.KilogramMeterSquared),
            new(speed, Unit.RadianPerSecond), new(angle, Unit.Radian));
    public static NodeDefinition Hydraulic(uint id, double compliance, double gaugePressure = 0) =>
        new(id, Domain.Hydraulic, new(compliance, Unit.CubicMeterPerPascal), new(gaugePressure, Unit.Pascal), default);
    public static NodeDefinition Thermal(uint id, double capacity, double temperature) =>
        new(id, Domain.Thermal, new(capacity, Unit.JoulePerKelvin), new(temperature, Unit.Kelvin), default);
    /// <summary>A finite gas volume: storage is the volume, initial is temperature, position is pressure.</summary>
    public static NodeDefinition GasVolume(uint id, double volume, double pressure, double temperature,
        double gasConstant = 287, double gamma = 1.4) =>
        new(id, Domain.Gas, new(volume, Unit.CubicMeter), new(temperature, Unit.Kelvin), new(pressure, Unit.Pascal))
        {
            Gas = new() { GasConstant = new(gasConstant, Unit.JoulePerKilogramKelvin), Gamma = gamma }
        };

    /// <summary>A gas chamber whose volume is supplied by exactly one gas-cylinder component.</summary>
    public static NodeDefinition CylinderGas(uint id, double pressure, double temperature,
        double gasConstant = 287, double gamma = 1.4) =>
        new(id, Domain.Gas, default, new(temperature, Unit.Kelvin), new(pressure, Unit.Pascal))
        { Gas = new() { GasConstant = new(gasConstant, Unit.JoulePerKilogramKelvin), Gamma = gamma } };
}

public sealed record GasCylinderDefinition
{
    public Quantity Bore { get; init; }
    public Quantity Stroke { get; init; }
    public Quantity RodLength { get; init; }
    public Quantity Phase { get; init; }
    public double CompressionRatio { get; init; }
    public Quantity BackPressure { get; init; }
}

/// <summary>Crank-referenced smooth opening envelope for a gas restriction.</summary>
public sealed record ValveTimingDefinition
{
    public uint CrankNode { get; init; }
    public Quantity CycleAngle { get; init; }
    public Quantity OpenAngle { get; init; }
    public Quantity DurationAngle { get; init; }
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
    /// <summary>Carrier node for a planetary gear; zero for every other kind.</summary>
    public uint NodeC { get; init; }
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
    public Quantity Area { get; init; }
    public double DischargeCoefficient { get; init; } = 1;
    public Quantity ReservoirPressure { get; init; }
    public SealedCylinderDefinition? Cylinder { get; init; }
    public GasCylinderDefinition? MovingCylinder { get; init; }
    /// <summary>Optional gas-orifice timing. InitialInput and its channel then specify peak opening.</summary>
    public ValveTimingDefinition? ValveTiming { get; init; }
    public WiebeCombustionDefinition? Combustion { get; init; }
    public ClutchDefinition? Friction { get; init; }
    public TorqueConverterDefinition? Converter { get; init; }
    public HydraulicRestrictionDefinition? HydraulicRestriction { get; init; }
    public HydraulicClutchDefinition? HydraulicClutch { get; init; }
    public HydraulicPumpDefinition? HydraulicPump { get; init; }
    /// <summary>Required only at a reservoir boundary of a premixed gas network.</summary>
    public MassFractions? ReservoirFractions { get; init; }

    /// <summary>Quasi-steady fluid coupling; a lockup clutch is a separate component.</summary>
    public static ComponentDefinition TorqueConverter(uint id, uint pump, uint turbine, TorqueConverterDefinition maps, uint heat = 0) => new()
    { Id = id, Kind = ComponentKind.TorqueConverter, NodeA = pump, NodeB = turbine, Converter = maps, HeatNode = heat };

    /// <summary>Permanent lossless speed constraint omega_A = ratio * omega_B.</summary>
    public static ComponentDefinition IdealGear(uint id, uint a, uint b, double ratio) => new()
    { Id = id, Kind = ComponentKind.IdealGear, NodeA = a, NodeB = b, Ratio = ratio };

    /// <summary>Permanent Willis constraint: sun + ratio * ring = (1 + ratio) * carrier.</summary>
    public static ComponentDefinition PlanetaryGear(uint id, uint sun, uint ring, uint carrier, double ringToSunTeethRatio) => new()
    { Id = id, Kind = ComponentKind.PlanetaryGear, NodeA = sun, NodeB = ring, NodeC = carrier, Ratio = ringToSunTeethRatio };

    /// <summary>Controlled linear hydraulic conductance with an explicit pressure reservoir when B is zero.</summary>
    public static ComponentDefinition HydraulicResistance(uint id, uint a, uint b, double conductance, double reservoirPressure = 0,
        ulong channel = 0, double opening = 1, uint heat = 0) => new()
    {
        Id = id, Kind = ComponentKind.HydraulicResistance, NodeA = a, NodeB = b, HeatNode = heat,
        InputChannel = channel, InitialInput = new(opening, Unit.Fraction), ReservoirPressure = b == 0 ? new(reservoirPressure, Unit.Pascal) : default,
        HydraulicRestriction = new() { Coefficient = new(conductance, Unit.CubicMeterPerSecondPascal) }
    };
    public static ComponentDefinition HydraulicOrifice(uint id, uint a, uint b, double coefficient, double transitionPressure,
        double reservoirPressure = 0, ulong channel = 0, double opening = 1, uint heat = 0) => new()
    {
        Id = id, Kind = ComponentKind.HydraulicOrifice, NodeA = a, NodeB = b, HeatNode = heat,
        InputChannel = channel, InitialInput = new(opening, Unit.Fraction), ReservoirPressure = b == 0 ? new(reservoirPressure, Unit.Pascal) : default,
        HydraulicRestriction = new() { Coefficient = new(coefficient, Unit.CubicMeterPerSecondSqrtPascal), TransitionPressure = new(transitionPressure, Unit.Pascal) }
    };
    /// <summary>Ideal reversible pump: A is the shaft, B is the outlet; zero inlet selects the explicit reservoir.</summary>
    public static ComponentDefinition DisplacementPump(uint id, uint shaft, uint outlet, double displacement,
        uint inlet = 0, double reservoirPressure = 0) => new()
    {
        Id = id, Kind = ComponentKind.HydraulicPump, NodeA = shaft, NodeB = outlet,
        HydraulicPump = new() { InletNode = inlet, Displacement = new(displacement, Unit.CubicMeterPerRadian) },
        ReservoirPressure = inlet == 0 ? new(reservoirPressure, Unit.Pascal) : default
    };
    /// <summary>One-way relief: Q = conductance * max(p_A - p_B - crackingPressure, 0).</summary>
    public static ComponentDefinition PressureRelief(uint id, uint a, uint b, double conductance, double crackingPressure,
        double reservoirPressure = 0, uint heat = 0) => new()
    {
        Id = id, Kind = ComponentKind.HydraulicRelief, NodeA = a, NodeB = b, HeatNode = heat,
        ReservoirPressure = b == 0 ? new(reservoirPressure, Unit.Pascal) : default,
        HydraulicRestriction = new() { Coefficient = new(conductance, Unit.CubicMeterPerSecondPascal), CrackingPressure = new(crackingPressure, Unit.Pascal) }
    };
    public static ComponentDefinition PressureClutch(uint id, uint a, uint b, HydraulicClutchDefinition actuator, double ratio = 1, uint heat = 0) => new()
    { Id = id, Kind = ComponentKind.HydraulicClutch, NodeA = a, NodeB = b, HydraulicClutch = actuator, Ratio = ratio, HeatNode = heat };

    public static ComponentDefinition Clutch(uint id, uint a, uint b, double staticCapacity,
        double slidingCapacity, ulong channel = 0, double engagement = 1, double ratio = 1, uint heat = 0) => new()
    {
        Id = id, Kind = ComponentKind.Clutch, NodeA = a, NodeB = b, HeatNode = heat,
        InputChannel = channel, InitialInput = new(engagement, Unit.Fraction), Ratio = ratio,
        Friction = new() { StaticCapacity = new(staticCapacity, Unit.NewtonMeter), SlidingCapacity = new(slidingCapacity, Unit.NewtonMeter) }
    };

    public static ComponentDefinition PremixedCombustion(uint id, uint crank, uint gas,
        WiebeCombustionDefinition combustion, ulong channel = 0, double multiplier = 1) => new()
    {
        Id = id, Kind = ComponentKind.PremixedCombustion, NodeA = crank, NodeB = gas,
        Combustion = combustion, InputChannel = channel, InitialInput = new(multiplier, Unit.Fraction)
    };

    public static ComponentDefinition SealedCylinder(uint id, uint crank, SealedCylinderDefinition cylinder) => new()
    {
        Id = id, Kind = ComponentKind.SealedCylinder, NodeA = crank, Cylinder = cylinder
    };

    /// <summary>Couple a crank to a gas chamber with independently evolving mass and energy.</summary>
    public static ComponentDefinition GasCylinder(uint id, uint crank, uint gas, GasCylinderDefinition geometry) => new()
    { Id = id, Kind = ComponentKind.GasCylinder, NodeA = crank, NodeB = gas, MovingCylinder = geometry };

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
    /// <summary>A restriction between two finite gas volumes. Opening is a fraction in [0, 1].</summary>
    public static ComponentDefinition GasOrifice(uint id, uint a, uint b, double area,
        double dischargeCoefficient = 1, ulong channel = 0, double opening = 1) => new()
    {
        Id = id, Kind = ComponentKind.GasOrifice, NodeA = a, NodeB = b, InputChannel = channel,
        Area = new(area, Unit.SquareMeter), DischargeCoefficient = dischargeCoefficient,
        InitialInput = new(opening, Unit.Fraction)
    };
    /// <summary>A restriction between one finite gas volume and a fixed pressure/temperature reservoir.</summary>
    public static ComponentDefinition GasReservoir(uint id, uint a, double area, double pressure,
        double temperature, double dischargeCoefficient = 1, ulong channel = 0, double opening = 1) => new()
    {
        Id = id, Kind = ComponentKind.GasOrifice, NodeA = a, InputChannel = channel,
        Area = new(area, Unit.SquareMeter), DischargeCoefficient = dischargeCoefficient,
        ReservoirPressure = new(pressure, Unit.Pascal), AmbientTemperature = new(temperature, Unit.Kelvin),
        InitialInput = new(opening, Unit.Fraction)
    };
    /// <summary>Conductive heat exchange between a finite gas volume and a thermal node.</summary>
    public static ComponentDefinition GasHeatLink(uint id, uint gas, uint wall, double conductance) => new()
    {
        Id = id, Kind = ComponentKind.GasHeatLink, NodeA = gas, NodeB = wall,
        Conductance = new(conductance, Unit.WattPerKelvin)
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
