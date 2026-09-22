// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Power.Core;
using Power.Assets;

namespace Power.Experiments;

public sealed record InputEvent(ulong TimeNanoseconds, Scalar[] Values);
public sealed record OutputCheck(uint ObjectId, Field Field, double? Min, double? Max, double? AbsMax);
public sealed record ModelDocument(ModelDefinition Model, ulong DurationNanoseconds, ulong SampleEveryNanoseconds,
    InputEvent[] Events, OutputCheck[] Checks, string SourceSha256)
{
    public PowerAsset ToAsset(string name)
    {
        ExperimentRunner.Validate(this);
        return PowerAsset.Create(Model, name, SourceSha256, DurationNanoseconds, SampleEveryNanoseconds,
            Events.SelectMany(e => e.Values.Select(v => new ScheduledInput(e.TimeNanoseconds, v.Channel, v.Value))),
            Checks.Select(c => new AssetCheck(c.ObjectId, c.Field, c.Min, c.Max, c.AbsMax)));
    }

    private static readonly Dictionary<string, Unit> Units = new(StringComparer.Ordinal)
    {
        ["m3_rad"] = Unit.CubicMeterPerRadian, ["m3_pa"] = Unit.CubicMeterPerPascal, ["m3_s_pa"] = Unit.CubicMeterPerSecondPascal,
        ["m3_s_sqrt_pa"] = Unit.CubicMeterPerSecondSqrtPascal, ["m3_s"] = Unit.CubicMeterPerSecond, ["n"] = Unit.Newton,
        ["nm_s2_rad2"] = Unit.NewtonMeterSecondSquaredPerRadianSquared, ["none"] = Unit.None, ["kg_m2"] = Unit.KilogramMeterSquared, ["rad"] = Unit.Radian,
        ["rad_s"] = Unit.RadianPerSecond, ["nm"] = Unit.NewtonMeter, ["nm_rad"] = Unit.NewtonMeterPerRadian,
        ["nm_s_rad"] = Unit.NewtonMeterSecondPerRadian, ["k"] = Unit.Kelvin, ["j_k"] = Unit.JoulePerKelvin,
        ["w_k"] = Unit.WattPerKelvin, ["ohm"] = Unit.Ohm, ["h"] = Unit.Henry, ["nm_a"] = Unit.NewtonMeterPerAmpere,
        ["a"] = Unit.Ampere, ["v"] = Unit.Volt, ["j"] = Unit.Joule, ["rpm"] = Unit.Rpm, ["deg"] = Unit.Degree,
        ["m"] = Unit.Meter, ["mm"] = Unit.Millimeter, ["m3"] = Unit.CubicMeter, ["pa"] = Unit.Pascal,
        ["bar"] = Unit.Bar, ["kg"] = Unit.Kilogram, ["j_kg_k"] = Unit.JoulePerKilogramKelvin,
        ["l"] = Unit.Liter, ["m2"] = Unit.SquareMeter, ["mm2"] = Unit.SquareMillimeter,
        ["kg_s"] = Unit.KilogramPerSecond, ["w"] = Unit.Watt, ["fraction"] = Unit.Fraction, ["j_kg"] = Unit.JoulePerKilogram
    };
    private static readonly Dictionary<string, Field> Fields = new(StringComparer.Ordinal)
    {
        ["hydraulic_power"] = Field.HydraulicPower, ["volume_flow"] = Field.VolumeFlow, ["hydraulic_volume_in"] = Field.HydraulicVolumeIn,
        ["hydraulic_volume_residual"] = Field.HydraulicVolumeResidual, ["hydraulic_work"] = Field.HydraulicWork,
        ["clamp_force"] = Field.ClampForce, ["static_capacity"] = Field.StaticCapacity, ["sliding_capacity"] = Field.SlidingCapacity,
        ["fluid_heat"] = Field.FluidHeat, ["speed_ratio"] = Field.SpeedRatio, ["converter_drive"] = Field.ConverterDrive,
        ["angle"] = Field.Angle, ["speed"] = Field.Speed, ["temperature"] = Field.Temperature,
        ["current"] = Field.Current, ["twist"] = Field.Twist, ["torque"] = Field.Torque,
        ["pressure"] = Field.Pressure, ["volume"] = Field.Volume, ["mass"] = Field.Mass,
        ["internal_energy"] = Field.InternalEnergy, ["piston_displacement"] = Field.PistonDisplacement,
        ["source_work"] = Field.SourceWork, ["heat_rejected"] = Field.HeatRejected,
        ["stored_energy_change"] = Field.StoredEnergyChange, ["energy_residual"] = Field.EnergyResidual,
        ["mass_flow"] = Field.MassFlow, ["heat_flow"] = Field.HeatFlow,
        ["reservoir_enthalpy"] = Field.ReservoirEnthalpy, ["mass_residual"] = Field.MassResidual,
        ["opening"] = Field.Opening, ["fuel_mass"] = Field.FuelMass, ["fresh_air_mass"] = Field.FreshAirMass,
        ["product_mass"] = Field.ProductMass, ["chemical_energy"] = Field.ChemicalEnergy,
        ["fuel_burned"] = Field.FuelBurned, ["heat_released"] = Field.HeatReleased,
        ["fuel_energy_in"] = Field.FuelEnergyIn, ["fuel_residual"] = Field.FuelResidual, ["fresh_air_residual"] = Field.FreshAirResidual,
        ["burn_frontier"] = Field.BurnFrontier, ["slip_speed"] = Field.SlipSpeed,
        ["clutch_mode"] = Field.ClutchMode, ["friction_heat"] = Field.FrictionHeat,
        ["torque_at_b"] = Field.TorqueAtB, ["torque_at_c"] = Field.TorqueAtC, ["constraint_error"] = Field.ConstraintError
    };

    public static ModelDocument Load(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > 1_048_576) throw new ArgumentException("Model document exceeds 1 MiB.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    private static void Object(JsonElement e, string[] required, params string[] optional)
    {
        if (e.ValueKind != JsonValueKind.Object) throw new ArgumentException("Expected a JSON object.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in e.EnumerateObject())
            if (!seen.Add(p.Name) || (!required.Contains(p.Name) && !optional.Contains(p.Name)))
                throw new ArgumentException($"Duplicate or unknown JSON field '{p.Name}'.");
        foreach (string field in required)
            if (!seen.Contains(field)) throw new ArgumentException($"Missing JSON field '{field}'.");
    }
    private static string Text(JsonElement e) => e.ValueKind == JsonValueKind.String ? e.GetString()! :
        throw new ArgumentException("Expected a string.");
    private static ulong Integer(JsonElement e, ulong maximum = ulong.MaxValue, ulong minimum = 0) =>
        e.ValueKind == JsonValueKind.Number && e.TryGetUInt64(out ulong value) && value >= minimum && value <= maximum
            ? value : throw new ArgumentException($"Expected an integer in [{minimum}, {maximum}].");
    private static double Number(JsonElement e) => e.ValueKind == JsonValueKind.Number && e.TryGetDouble(out double value) && double.IsFinite(value)
        ? value : throw new ArgumentException("Expected a finite number.");
    private static JsonElement[] Array(JsonElement e, int maximum, int minimum = 0) =>
        e.ValueKind == JsonValueKind.Array && e.GetArrayLength() >= minimum && e.GetArrayLength() <= maximum
            ? e.EnumerateArray().ToArray() : throw new ArgumentException($"Expected {minimum}..{maximum} array elements.");
    private static Quantity Quantity(JsonElement e)
    {
        Object(e, ["value", "unit"]);
        if (!Units.TryGetValue(Text(e.GetProperty("unit")), out Unit unit)) throw new ArgumentException("Unknown unit.");
        return new(Number(e.GetProperty("value")), unit);
    }
    private static MassFractions Fractions(JsonElement e)
    {
        Object(e, ["fuel", "fresh_air"]);
        return new(Number(e.GetProperty("fuel")), Number(e.GetProperty("fresh_air")));
    }
    private static uint Id(JsonElement e, string field, bool optional = false) =>
        e.TryGetProperty(field, out var p) ? (uint)Integer(p, uint.MaxValue, optional ? 0UL : 1UL) : optional ? 0U :
            throw new ArgumentException($"Missing {field}.");

    public static ModelDocument Parse(string source)
    {
        if (Encoding.UTF8.GetByteCount(source) > 1_048_576) throw new ArgumentException("Model document exceeds 1 MiB.");
        using var json = JsonDocument.Parse(source, new JsonDocumentOptions { MaxDepth = 32 });
        JsonElement root = json.RootElement;
        Object(root, ["schema", "step_ns", "nodes", "components", "experiment"], "description");
        if (Text(root.GetProperty("schema")) != "power.model.v1") throw new ArgumentException("Unsupported schema.");
        var nodes = Array(root.GetProperty("nodes"), CompiledModel.MaxNodes, 1).Select(n =>
        {
            Object(n, ["id", "domain", "initial"], "storage", "position", "gas");
            var domain = Text(n.GetProperty("domain")) switch
            {
                "rotational" => Domain.Rotational, "thermal" => Domain.Thermal, "gas" => Domain.Gas, "hydraulic" => Domain.Hydraulic,
                _ => throw new ArgumentException("Unknown node domain.")
            };
            if (domain is Domain.Rotational or Domain.Gas && !n.TryGetProperty("position", out _))
                throw new ArgumentException("Rotational position and gas pressure must be explicit.");
            if (domain != Domain.Gas && !n.TryGetProperty("storage", out _))
                throw new ArgumentException("Rotational inertia and thermal capacity must be explicit.");
            GasDefinition? gas = null;
            if (n.TryGetProperty("gas", out var composition))
            {
                Object(composition, ["gas_constant", "gamma"], "premixed");
                PremixedGasDefinition? premixed = null;
                if (composition.TryGetProperty("premixed", out var mixture))
                {
                    Object(mixture, ["lower_heating_value", "stoichiometric_air_fuel_ratio", "initial_fractions"]);
                    premixed = new() { LowerHeatingValue = Quantity(mixture.GetProperty("lower_heating_value")),
                        StoichiometricAirFuelRatio = Number(mixture.GetProperty("stoichiometric_air_fuel_ratio")),
                        InitialFractions = Fractions(mixture.GetProperty("initial_fractions")) };
                }
                gas = new() { GasConstant = Quantity(composition.GetProperty("gas_constant")), Gamma = Number(composition.GetProperty("gamma")), Premixed = premixed };
            }
            return new NodeDefinition(Id(n, "id"), domain, n.TryGetProperty("storage", out var storage) ? Quantity(storage) : default, Quantity(n.GetProperty("initial")),
                n.TryGetProperty("position", out var p) ? Quantity(p) : default) { Gas = gas };
        }).ToArray();
        var components = Array(root.GetProperty("components"), CompiledModel.MaxComponents).Select(c =>
        {
            Object(c, ["id", "kind", "node_a"], "node_b", "node_c", "heat_node", "input_channel", "initial_input", "parameters", "valve_timing", "reservoir_fractions");
            var kind = Text(c.GetProperty("kind")) switch
            {
                "hydraulic_resistance" => ComponentKind.HydraulicResistance, "hydraulic_orifice" => ComponentKind.HydraulicOrifice,
                "hydraulic_clutch" => ComponentKind.HydraulicClutch,
                "hydraulic_pump" => ComponentKind.HydraulicPump, "hydraulic_relief" => ComponentKind.HydraulicRelief,
                "torque_converter" => ComponentKind.TorqueConverter, "shaft" => ComponentKind.Shaft, "dc_motor" => ComponentKind.DcMotor,
                "torque_source" => ComponentKind.TorqueSource, "thermal_link" => ComponentKind.ThermalLink,
                "sealed_cylinder" => ComponentKind.SealedCylinder,
                "gas_orifice" => ComponentKind.GasOrifice, "gas_heat_link" => ComponentKind.GasHeatLink,
                "gas_cylinder" => ComponentKind.GasCylinder, "premixed_combustion" => ComponentKind.PremixedCombustion,
                "clutch" => ComponentKind.Clutch,
                "ideal_gear" => ComponentKind.IdealGear, "planetary_gear" => ComponentKind.PlanetaryGear,
                _ => throw new ArgumentException("Unknown component kind.")
            };
            if (kind == ComponentKind.IdealGear) Object(c, ["id", "kind", "node_a", "node_b", "parameters"]);
            else if (kind == ComponentKind.PlanetaryGear) Object(c, ["id", "kind", "node_a", "node_b", "node_c", "parameters"]);
            else if (kind == ComponentKind.TorqueConverter) Object(c, ["id", "kind", "node_a", "node_b", "parameters"], "heat_node");
            else if (kind == ComponentKind.HydraulicPump) Object(c, ["id", "kind", "node_a", "node_b", "parameters"]);
            else if (kind == ComponentKind.HydraulicRelief) Object(c, ["id", "kind", "node_a", "parameters"], "node_b", "heat_node");
            else if (kind == ComponentKind.HydraulicClutch) Object(c, ["id", "kind", "node_a", "parameters"], "node_b", "heat_node");
            else if (kind is ComponentKind.HydraulicResistance or ComponentKind.HydraulicOrifice) Object(c, ["id", "kind", "node_a", "parameters", "initial_input"], "node_b", "heat_node", "input_channel");
            else if (c.TryGetProperty("node_c", out _)) throw new ArgumentException("node_c applies only to planetary gears.");
            ValveTimingDefinition? timing = null;
            if (c.TryGetProperty("valve_timing", out var valve))
            {
                Object(valve, ["crank_node", "cycle_angle", "open_angle", "duration_angle"]);
                timing = new() { CrankNode = Id(valve, "crank_node"), CycleAngle = Quantity(valve.GetProperty("cycle_angle")),
                    OpenAngle = Quantity(valve.GetProperty("open_angle")), DurationAngle = Quantity(valve.GetProperty("duration_angle")) };
            }
            var result = new ComponentDefinition
            {
                Id = Id(c, "id"), Kind = kind, NodeA = Id(c, "node_a"), NodeB = Id(c, "node_b", true), NodeC = Id(c, "node_c", true),
                HeatNode = Id(c, "heat_node", true), ValveTiming = timing,
                ReservoirFractions = c.TryGetProperty("reservoir_fractions", out var fractions) ? Fractions(fractions) : null,
                InputChannel = c.TryGetProperty("input_channel", out var channel) ? Integer(channel, long.MaxValue) : 0,
                InitialInput = c.TryGetProperty("initial_input", out var initial) ? Quantity(initial) : default
            };
            if (kind == ComponentKind.TorqueSource)
            {
                if (c.TryGetProperty("parameters", out var empty)) Object(empty, []);
                return result;
            }
            if (!c.TryGetProperty("parameters", out var parameters)) throw new ArgumentException("Missing component parameters.");
            if (kind == ComponentKind.HydraulicPump)
            {
                uint inlet = Id(parameters, "inlet_node", true);
                string[] required = ["inlet_node", "displacement"];
                if (inlet == 0) required = [..required, "reservoir_pressure"];
                Object(parameters, required);
                return result with { HydraulicPump = new() { InletNode = inlet, Displacement = Quantity(parameters.GetProperty("displacement")) },
                    ReservoirPressure = inlet == 0 ? Quantity(parameters.GetProperty("reservoir_pressure")) : default };
            }
            if (kind is ComponentKind.HydraulicResistance or ComponentKind.HydraulicOrifice or ComponentKind.HydraulicRelief)
            {
                string[] required = kind == ComponentKind.HydraulicRelief ? ["coefficient", "cracking_pressure"] : kind == ComponentKind.HydraulicResistance ? ["coefficient"] : ["coefficient", "transition_pressure"];
                if (result.NodeB == 0) required = [..required, "reservoir_pressure"];
                Object(parameters, required);
                return result with { ReservoirPressure = result.NodeB == 0 ? Quantity(parameters.GetProperty("reservoir_pressure")) : default,
                    HydraulicRestriction = new() { Coefficient = Quantity(parameters.GetProperty("coefficient")),
                        TransitionPressure = kind == ComponentKind.HydraulicOrifice ? Quantity(parameters.GetProperty("transition_pressure")) : default,
                        CrackingPressure = kind == ComponentKind.HydraulicRelief ? Quantity(parameters.GetProperty("cracking_pressure")) : default } };
            }
            if (kind == ComponentKind.HydraulicClutch)
            {
                Object(parameters, ["pressure_node", "piston_area", "preload_force", "effective_radius", "static_friction", "sliding_friction", "friction_surfaces", "ratio"]);
                return result with { Ratio = Number(parameters.GetProperty("ratio")), HydraulicClutch = new()
                {
                    PressureNode = Id(parameters, "pressure_node"), PistonArea = Quantity(parameters.GetProperty("piston_area")),
                    PreloadForce = Quantity(parameters.GetProperty("preload_force")), EffectiveRadius = Quantity(parameters.GetProperty("effective_radius")),
                    StaticFriction = Number(parameters.GetProperty("static_friction")), SlidingFriction = Number(parameters.GetProperty("sliding_friction")),
                    FrictionSurfaces = (uint)Integer(parameters.GetProperty("friction_surfaces"), 128, 1)
                } };
            }
            if (kind == ComponentKind.TorqueConverter)
            {
                Object(parameters, ["pump_positive", "pump_negative", "turbine_positive", "turbine_negative"]);
                ConverterMap Map(string name) => new(Array(parameters.GetProperty(name), ConverterMap.MaxPoints, 2).Select(point =>
                {
                    Object(point, ["speed_ratio", "torque_ratio", "capacity_coefficient"]);
                    return new ConverterMapPoint(Number(point.GetProperty("speed_ratio")), Number(point.GetProperty("torque_ratio")), Quantity(point.GetProperty("capacity_coefficient")));
                }));
                return result with { Converter = new() { PumpPositive = Map("pump_positive"), PumpNegative = Map("pump_negative"),
                    TurbinePositive = Map("turbine_positive"), TurbineNegative = Map("turbine_negative") } };
            }
            if (kind is ComponentKind.IdealGear or ComponentKind.PlanetaryGear)
            {
                Object(parameters, ["ratio"]);
                return result with { Ratio = Number(parameters.GetProperty("ratio")) };
            }
            if (kind == ComponentKind.Clutch)
            {
                if (!c.TryGetProperty("initial_input", out _)) throw new ArgumentException("Clutch engagement must be explicit in initial_input (fraction).");
                Object(parameters, ["static_capacity", "sliding_capacity", "ratio"]);
                return result with { Ratio = Number(parameters.GetProperty("ratio")), Friction = new()
                { StaticCapacity = Quantity(parameters.GetProperty("static_capacity")), SlidingCapacity = Quantity(parameters.GetProperty("sliding_capacity")) } };
            }
            if (kind == ComponentKind.PremixedCombustion)
            {
                if (!c.TryGetProperty("initial_input", out _)) throw new ArgumentException("Burn multiplier must be explicit in initial_input (fraction).");
                Object(parameters, ["cycle_angle", "start_angle", "duration_angle", "shape_exponent", "burn_coefficient"]);
                return result with { Combustion = new()
                {
                    CycleAngle = Quantity(parameters.GetProperty("cycle_angle")), StartAngle = Quantity(parameters.GetProperty("start_angle")),
                    DurationAngle = Quantity(parameters.GetProperty("duration_angle")), ShapeExponent = Number(parameters.GetProperty("shape_exponent")),
                    BurnCoefficient = Number(parameters.GetProperty("burn_coefficient"))
                } };
            }
            if (kind == ComponentKind.GasCylinder)
            {
                Object(parameters, ["bore", "stroke", "rod_length", "phase", "compression_ratio", "back_pressure"]);
                return result with { MovingCylinder = new()
                {
                    Bore = Quantity(parameters.GetProperty("bore")), Stroke = Quantity(parameters.GetProperty("stroke")),
                    RodLength = Quantity(parameters.GetProperty("rod_length")), Phase = Quantity(parameters.GetProperty("phase")),
                    CompressionRatio = Number(parameters.GetProperty("compression_ratio")), BackPressure = Quantity(parameters.GetProperty("back_pressure"))
                } };
            }
            if (kind == ComponentKind.GasOrifice)
            {
                if (!c.TryGetProperty("initial_input", out _)) throw new ArgumentException("Gas opening must be explicit in initial_input (fraction).");
                Object(parameters, result.NodeB == 0
                    ? ["area", "discharge_coefficient", "reservoir_pressure", "reservoir_temperature"]
                    : ["area", "discharge_coefficient"]);
                return result with
                {
                    Area = Quantity(parameters.GetProperty("area")), DischargeCoefficient = Number(parameters.GetProperty("discharge_coefficient")),
                    ReservoirPressure = result.NodeB == 0 ? Quantity(parameters.GetProperty("reservoir_pressure")) : default,
                    AmbientTemperature = result.NodeB == 0 ? Quantity(parameters.GetProperty("reservoir_temperature")) : default
                };
            }
            if (kind == ComponentKind.GasHeatLink)
            {
                Object(parameters, ["conductance"]);
                return result with { Conductance = Quantity(parameters.GetProperty("conductance")) };
            }
            if (kind == ComponentKind.SealedCylinder)
            {
                Object(parameters, ["bore", "stroke", "rod_length", "phase", "compression_ratio", "initial_pressure",
                    "initial_temperature", "gas_constant", "gamma", "back_pressure"]);
                return result with { Cylinder = new()
                {
                    Bore = Quantity(parameters.GetProperty("bore")), Stroke = Quantity(parameters.GetProperty("stroke")),
                    RodLength = Quantity(parameters.GetProperty("rod_length")), Phase = Quantity(parameters.GetProperty("phase")),
                    CompressionRatio = Number(parameters.GetProperty("compression_ratio")), InitialPressure = Quantity(parameters.GetProperty("initial_pressure")),
                    InitialTemperature = Quantity(parameters.GetProperty("initial_temperature")), GasConstant = Quantity(parameters.GetProperty("gas_constant")),
                    Gamma = Number(parameters.GetProperty("gamma")), BackPressure = Quantity(parameters.GetProperty("back_pressure"))
                } };
            }
            if (kind == ComponentKind.Shaft)
            {
                Object(parameters, ["stiffness", "damping", "rest_angle", "ratio"]);
                return result with { Stiffness = Quantity(parameters.GetProperty("stiffness")), Damping = Quantity(parameters.GetProperty("damping")),
                    RestAngle = Quantity(parameters.GetProperty("rest_angle")), Ratio = Number(parameters.GetProperty("ratio")) };
            }
            if (kind == ComponentKind.DcMotor)
            {
                Object(parameters, ["resistance", "inductance", "coupling", "initial_current"]);
                return result with { Resistance = Quantity(parameters.GetProperty("resistance")), Inductance = Quantity(parameters.GetProperty("inductance")),
                    Coupling = Quantity(parameters.GetProperty("coupling")), InitialCurrent = Quantity(parameters.GetProperty("initial_current")) };
            }
            Object(parameters, result.NodeB == 0 ? ["conductance", "ambient_temperature"] : ["conductance"]);
            return result with { Conductance = Quantity(parameters.GetProperty("conductance")),
                AmbientTemperature = result.NodeB == 0 ? Quantity(parameters.GetProperty("ambient_temperature")) : default };
        }).ToArray();
        var model = new ModelDefinition { StepNanoseconds = Integer(root.GetProperty("step_ns"), 1_000_000_000, 1), Nodes = nodes, Components = components };
        JsonElement exp = root.GetProperty("experiment");
        Object(exp, ["duration_ns", "sample_every_ns"], "events", "checks");
        ulong duration = Integer(exp.GetProperty("duration_ns"), 3_600_000_000_000, 1);
        ulong sample = Integer(exp.GetProperty("sample_every_ns"), duration, 1);
        InputEvent[] events = exp.TryGetProperty("events", out var es) ? Array(es, 10000).Select(e =>
        {
            Object(e, ["time_ns", "values"]);
            return new InputEvent(Integer(e.GetProperty("time_ns"), duration - 1), Array(e.GetProperty("values"), 64, 1).Select(v =>
            {
                Object(v, ["channel", "value"]);
                return new Scalar(Integer(v.GetProperty("channel"), long.MaxValue, 1), Number(v.GetProperty("value")));
            }).ToArray());
        }).ToArray() : [];
        OutputCheck[] checks = exp.TryGetProperty("checks", out var cs) ? Array(cs, 256).Select(c =>
        {
            Object(c, ["object_id", "field"], "min", "max", "abs_max");
            if (!Fields.TryGetValue(Text(c.GetProperty("field")), out var field)) throw new ArgumentException("Unknown check field.");
            double? Get(string name) => c.TryGetProperty(name, out var bound) ? Number(bound) : null;
            return new OutputCheck((uint)Integer(c.GetProperty("object_id"), uint.MaxValue), field, Get("min"), Get("max"), Get("abs_max"));
        }).ToArray() : [];
        return new(model, duration, sample, events, checks, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(source))));
    }
}
