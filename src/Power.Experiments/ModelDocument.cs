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
        ["v_pa"] = Unit.VoltPerPascal, ["v_pa_s"] = Unit.VoltPerPascalSecond,
        ["c"] = Unit.Coulomb, ["ah"] = Unit.AmpereHour, ["f"] = Unit.Farad,
        ["fraction_pa"] = Unit.FractionPerPascal, ["fraction_pa_s"] = Unit.FractionPerPascalSecond,
        ["m_s"] = Unit.MeterPerSecond, ["n_m"] = Unit.NewtonPerMeter, ["n_s_m"] = Unit.NewtonSecondPerMeter,
        ["m3_rad"] = Unit.CubicMeterPerRadian, ["m3_pa"] = Unit.CubicMeterPerPascal, ["m3_s_pa"] = Unit.CubicMeterPerSecondPascal,
        ["m3_s_sqrt_pa"] = Unit.CubicMeterPerSecondSqrtPascal, ["m3_s"] = Unit.CubicMeterPerSecond, ["n"] = Unit.Newton,
        ["nm_s2_rad2"] = Unit.NewtonMeterSecondSquaredPerRadianSquared, ["none"] = Unit.None, ["kg_m2"] = Unit.KilogramMeterSquared, ["rad"] = Unit.Radian,
        ["rad_s"] = Unit.RadianPerSecond, ["nm"] = Unit.NewtonMeter, ["nm_rad"] = Unit.NewtonMeterPerRadian,
        ["nm_s_rad"] = Unit.NewtonMeterSecondPerRadian, ["k"] = Unit.Kelvin, ["j_k"] = Unit.JoulePerKelvin,
        ["w_k"] = Unit.WattPerKelvin, ["ohm"] = Unit.Ohm, ["h"] = Unit.Henry, ["nm_a"] = Unit.NewtonMeterPerAmpere,
        ["a"] = Unit.Ampere, ["v"] = Unit.Volt, ["j"] = Unit.Joule, ["rpm"] = Unit.Rpm, ["deg"] = Unit.Degree,
        ["m"] = Unit.Meter, ["mm"] = Unit.Millimeter, ["m3"] = Unit.CubicMeter, ["pa"] = Unit.Pascal,
        ["state_code"] = Unit.StateCode, ["h_m"] = Unit.HenryPerMeter, ["kg_m3"] = Unit.KilogramPerCubicMeter, ["bar"] = Unit.Bar, ["kg"] = Unit.Kilogram, ["j_kg_k"] = Unit.JoulePerKilogramKelvin,
        ["l"] = Unit.Liter, ["m2"] = Unit.SquareMeter, ["mm2"] = Unit.SquareMillimeter,
        ["kg_s"] = Unit.KilogramPerSecond, ["w"] = Unit.Watt, ["fraction"] = Unit.Fraction, ["j_kg"] = Unit.JoulePerKilogram
    };
    private static readonly Dictionary<string, Field> Fields = new(StringComparer.Ordinal)
    {
        ["requested_gear"] = Field.RequestedGear, ["actual_gear"] = Field.ActualGear, ["selected_odd_gear"] = Field.SelectedOddGear, ["selected_even_gear"] = Field.SelectedEvenGear, ["shift_phase"] = Field.ShiftPhase, ["sync_error"] = Field.SyncError, ["control_fault"] = Field.ControlFault,
        ["copper_heat"] = Field.CopperHeat, ["predicted_fuel_mass"] = Field.PredictedFuelMass, ["prediction_ticks"] = Field.PredictionTicks, ["driver_state"] = Field.DriverState, ["closing_delay_ticks"] = Field.ClosingDelayTicks,
        ["sampled_pressure"] = Field.SampledPressure, ["pressure_error"] = Field.PressureError,
        ["integral_voltage"] = Field.IntegralVoltage, ["command_voltage"] = Field.CommandVoltage,
        ["state_of_charge"] = Field.StateOfCharge, ["charge"] = Field.Charge, ["terminal_voltage"] = Field.TerminalVoltage,
        ["polarization_voltage"] = Field.PolarizationVoltage, ["battery_current"] = Field.BatteryCurrent,
        ["integral_duty"] = Field.IntegralDuty, ["command_duty"] = Field.CommandDuty,
        ["displacement"] = Field.Displacement, ["linear_speed"] = Field.LinearSpeed, ["force"] = Field.Force, ["requested_fuel_dose"] = Field.RequestedFuelDose, ["delivered_fuel_dose"] = Field.DeliveredFuelDose, ["total_fuel_delivered"] = Field.TotalFuelDelivered, ["evaporated_fuel_mass"] = Field.EvaporatedFuelMass, ["film_wall_heat"] = Field.FilmWallHeat,
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
            Object(n, ["id", "domain", "initial"], "storage", "position", "gas", "battery");
            var domain = Text(n.GetProperty("domain")) switch
            {
                "rotational" => Domain.Rotational, "thermal" => Domain.Thermal, "gas" => Domain.Gas, "hydraulic" => Domain.Hydraulic, "battery" => Domain.Battery, "translational" => Domain.Translational,
                _ => throw new ArgumentException("Unknown node domain.")
            };
            if (domain is Domain.Rotational or Domain.Gas or Domain.Battery or Domain.Translational && !n.TryGetProperty("position", out _))
                throw new ArgumentException("Rotational position and gas pressure must be explicit.");
            if (domain != Domain.Gas && !n.TryGetProperty("storage", out _))
                throw new ArgumentException("Rotational inertia and thermal capacity must be explicit.");
            GasDefinition? gas = null;
            BatteryDefinition? battery = null;
            if (n.TryGetProperty("battery", out var sourceBattery))
            {
                Object(sourceBattery, ["empty_voltage", "full_voltage", "series_resistance", "polarization_resistance", "polarization_capacitance"], "heat_node");
                battery = new()
                {
                    EmptyOpenCircuitVoltage = Quantity(sourceBattery.GetProperty("empty_voltage")), FullOpenCircuitVoltage = Quantity(sourceBattery.GetProperty("full_voltage")),
                    SeriesResistance = Quantity(sourceBattery.GetProperty("series_resistance")), PolarizationResistance = Quantity(sourceBattery.GetProperty("polarization_resistance")),
                    PolarizationCapacitance = Quantity(sourceBattery.GetProperty("polarization_capacitance")), HeatNode = Id(sourceBattery, "heat_node", true)
                };
            }
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
                n.TryGetProperty("position", out var p) ? Quantity(p) : default) { Gas = gas, Battery = battery };
        }).ToArray();
        var components = Array(root.GetProperty("components"), CompiledModel.MaxComponents).Select(c =>
        {
            Object(c, ["id", "kind", "node_a"], "node_b", "node_c", "heat_node", "input_channel", "initial_input", "parameters", "valve_timing", "reservoir_fractions");
            var kind = Text(c.GetProperty("kind")) switch
            {
                "hydraulic_resistance" => ComponentKind.HydraulicResistance, "hydraulic_orifice" => ComponentKind.HydraulicOrifice,
                "hydraulic_clutch" => ComponentKind.HydraulicClutch,
                "hydraulic_pump" => ComponentKind.HydraulicPump, "hydraulic_relief" => ComponentKind.HydraulicRelief,
                "pressure_controller" => ComponentKind.PressureController,
                "battery_motor" => ComponentKind.BatteryMotor, "resistive_load" => ComponentKind.ResistiveLoad, "pressure_duty_controller" => ComponentKind.PressureDutyController,
                "hydraulic_piston" => ComponentKind.HydraulicPiston, "linear_spring" => ComponentKind.LinearSpring, "piston_clutch" => ComponentKind.PistonClutch, "force_source" => ComponentKind.ForceSource, "hydraulic_spool_valve" => ComponentKind.HydraulicSpoolValve,
                "torque_converter" => ComponentKind.TorqueConverter, "shaft" => ComponentKind.Shaft, "dc_motor" => ComponentKind.DcMotor,
                "torque_source" => ComponentKind.TorqueSource, "thermal_link" => ComponentKind.ThermalLink,
                "sealed_cylinder" => ComponentKind.SealedCylinder,
                "gas_orifice" => ComponentKind.GasOrifice, "gas_fuel_injector" => ComponentKind.GasFuelInjector, "gas_heat_link" => ComponentKind.GasHeatLink, "fuel_film" => ComponentKind.FuelFilm, "liquid_fuel_injector" => ComponentKind.LiquidFuelInjector, "solenoid" => ComponentKind.Solenoid, "travel_stop" => ComponentKind.TravelStop, "needle_driver" => ComponentKind.NeedleDriver, "dct_controller" => ComponentKind.DualClutchController,
                "gas_cylinder" => ComponentKind.GasCylinder, "gas_piston" => ComponentKind.GasPiston, "premixed_combustion" => ComponentKind.PremixedCombustion,
                "clutch" => ComponentKind.Clutch,
                "ideal_gear" => ComponentKind.IdealGear, "planetary_gear" => ComponentKind.PlanetaryGear, "double_pinion_planetary_gear" => ComponentKind.DoublePinionPlanetaryGear, "carrier_gear" => ComponentKind.CarrierGear,
                _ => throw new ArgumentException("Unknown component kind.")
            };
            if (kind == ComponentKind.IdealGear) Object(c, ["id", "kind", "node_a", "node_b", "parameters"]);
            else if (kind is ComponentKind.PlanetaryGear or ComponentKind.DoublePinionPlanetaryGear or ComponentKind.CarrierGear) Object(c, ["id", "kind", "node_a", "node_b", "node_c", "parameters"]);
            else if (kind == ComponentKind.TorqueConverter) Object(c, ["id", "kind", "node_a", "node_b", "parameters"], "heat_node");
            else if (kind is ComponentKind.PressureController or ComponentKind.PressureDutyController) Object(c, ["id", "kind", "node_a", "input_channel", "initial_input", "parameters"]);
            else if (kind == ComponentKind.BatteryMotor) Object(c, ["id", "kind", "node_a", "node_b", "input_channel", "initial_input", "parameters"], "heat_node");
            else if (kind == ComponentKind.ResistiveLoad) Object(c, ["id", "kind", "node_a", "initial_input", "parameters"], "input_channel", "heat_node");
            else if (kind == ComponentKind.DualClutchController) Object(c, ["id", "kind", "node_a", "input_channel", "initial_input", "parameters"]);
            else if (kind == ComponentKind.Solenoid) Object(c, ["id", "kind", "node_a", "input_channel", "initial_input", "parameters"], "heat_node");
            else if (kind is ComponentKind.TravelStop or ComponentKind.NeedleDriver) Object(c, ["id", "kind", "node_a", "parameters"]);
            else if (kind == ComponentKind.LiquidFuelInjector) Object(c, ["id", "kind", "node_a", "input_channel", "initial_input", "parameters"]);
            else if(kind==ComponentKind.FuelFilm)Object(c,["id","kind","node_a","node_b","parameters"]);
            else if (kind == ComponentKind.GasFuelInjector) Object(c, ["id", "kind", "node_a", "node_b", "input_channel", "initial_input", "parameters"]);
            else if (kind is ComponentKind.HydraulicPiston or ComponentKind.GasPiston) Object(c, ["id", "kind", "node_a", "node_b", "parameters"]);
            else if (kind == ComponentKind.PistonClutch) Object(c, ["id", "kind", "node_a", "parameters"], "node_b", "heat_node");
            else if (kind == ComponentKind.HydraulicPump) Object(c, ["id", "kind", "node_a", "node_b", "parameters"]);
            else if (kind is ComponentKind.HydraulicRelief or ComponentKind.HydraulicSpoolValve) Object(c, ["id", "kind", "node_a", "parameters"], "node_b", "heat_node");
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
            if (kind is ComponentKind.TorqueSource or ComponentKind.ForceSource)
            {
                if (c.TryGetProperty("parameters", out var empty)) Object(empty, []);
                return result;
            }
            if (!c.TryGetProperty("parameters", out var parameters)) throw new ArgumentException("Missing component parameters.");
            if (kind == ComponentKind.DualClutchController)
            {
                Object(parameters, ["vehicle_node", "odd_clutch", "even_clutch", "selectors", "sample_period_ns", "release_ns", "engage_ns", "synchronize_timeout_ns", "synchronize_tolerance", "direction_speed_limit"]);
                var selectors = parameters.GetProperty("selectors");
                if (selectors.ValueKind != JsonValueKind.Array || selectors.GetArrayLength() != 8) throw new ArgumentException("DCT selectors require eight IDs for forward 1-7 and reverse.");
                return result with { DualClutchController = new()
                {
                    VehicleNode = Id(parameters, "vehicle_node"), OddClutch = Id(parameters, "odd_clutch"), EvenClutch = Id(parameters, "even_clutch"),
                    Selectors = selectors.EnumerateArray().Select(value => (uint)Integer(value, uint.MaxValue, 1)).ToArray(),
                    SamplePeriodNanoseconds = Integer(parameters.GetProperty("sample_period_ns"), 1_000_000_000, 1),
                    ReleaseNanoseconds = Integer(parameters.GetProperty("release_ns"), 10_000_000_000, 1), EngageNanoseconds = Integer(parameters.GetProperty("engage_ns"), 10_000_000_000, 1),
                    SynchronizeTimeoutNanoseconds = Integer(parameters.GetProperty("synchronize_timeout_ns"), 10_000_000_000, 1),
                    SynchronizeTolerance = Quantity(parameters.GetProperty("synchronize_tolerance")), DirectionChangeSpeedLimit = Quantity(parameters.GetProperty("direction_speed_limit"))
                } };
            }
            if (kind == ComponentKind.Solenoid)
            {
                Object(parameters, ["resistance", "inductance", "inductance_gradient", "reference_position", "initial_current"]);
                return result with { Resistance = Quantity(parameters.GetProperty("resistance")), Inductance = Quantity(parameters.GetProperty("inductance")),
                    InitialCurrent = Quantity(parameters.GetProperty("initial_current")), Solenoid = new()
                    { ReferencePosition = Quantity(parameters.GetProperty("reference_position")), InductanceGradient = Quantity(parameters.GetProperty("inductance_gradient")) } };
            }
            if (kind == ComponentKind.TravelStop)
            {
                Object(parameters, ["minimum_position", "maximum_position", "stiffness"]);
                return result with { TravelStop = new() { MinimumPosition = Quantity(parameters.GetProperty("minimum_position")),
                    MaximumPosition = Quantity(parameters.GetProperty("maximum_position")), Stiffness = Quantity(parameters.GetProperty("stiffness")) } };
            }
            if (kind == ComponentKind.NeedleDriver)
            {
                Object(parameters, ["injector_component", "solenoid_component", "sample_period_ns", "drive_voltage"], "closure_prediction_ns");
                return result with { NeedleDriver = new() { InjectorComponent = Id(parameters, "injector_component"), SolenoidComponent = Id(parameters, "solenoid_component"),
                    SamplePeriodNanoseconds = Integer(parameters.GetProperty("sample_period_ns"), 1_000_000_000, 1), DriveVoltage = Quantity(parameters.GetProperty("drive_voltage")),
                    ClosurePredictionNanoseconds = parameters.TryGetProperty("closure_prediction_ns", out var horizon) ? Integer(horizon, ulong.MaxValue) : 0 } };
            }
            if (kind == ComponentKind.LiquidFuelInjector)
            {
                Object(parameters, ["film_component", "crank_node", "cycle_angle", "start_angle", "duration_angle", "maximum_dose",
                    "initial_mass", "supply_temperature", "liquid_density", "initial_pressure", "pressure_compliance", "area", "discharge_coefficient"], "needle");
                InjectorNeedleDefinition? needle = null;
                if (parameters.TryGetProperty("needle", out var needleData))
                { Object(needleData, ["needle_node", "closed_position", "full_open_position"]); needle = new() { NeedleNode = Id(needleData, "needle_node"), ClosedPosition = Quantity(needleData.GetProperty("closed_position")), FullOpenPosition = Quantity(needleData.GetProperty("full_open_position")) }; }
                return result with
                {
                    Area = Quantity(parameters.GetProperty("area")), DischargeCoefficient = Number(parameters.GetProperty("discharge_coefficient")),
                    LiquidFuelInjector = new()
                    {
                        FilmComponent = Id(parameters, "film_component"), CrankNode = Id(parameters, "crank_node"),
                        CycleAngle = Quantity(parameters.GetProperty("cycle_angle")), StartAngle = Quantity(parameters.GetProperty("start_angle")),
                        DurationAngle = Quantity(parameters.GetProperty("duration_angle")), MaximumDose = Quantity(parameters.GetProperty("maximum_dose")),
                        InitialMass = Quantity(parameters.GetProperty("initial_mass")), SupplyTemperature = Quantity(parameters.GetProperty("supply_temperature")),
                        LiquidDensity = Quantity(parameters.GetProperty("liquid_density")), InitialPressure = Quantity(parameters.GetProperty("initial_pressure")),
                        PressureCompliance = Quantity(parameters.GetProperty("pressure_compliance")), Needle = needle
                    }
                };
            }
            if(kind==ComponentKind.FuelFilm)
            {Object(parameters,["initial_mass","initial_temperature","liquid_specific_heat","saturation_temperature","latent_internal_energy","conductance"]);return result with{Conductance=Quantity(parameters.GetProperty("conductance")),FuelFilm=new(){InitialMass=Quantity(parameters.GetProperty("initial_mass")),InitialTemperature=Quantity(parameters.GetProperty("initial_temperature")),LiquidSpecificHeat=Quantity(parameters.GetProperty("liquid_specific_heat")),SaturationTemperature=Quantity(parameters.GetProperty("saturation_temperature")),LatentInternalEnergy=Quantity(parameters.GetProperty("latent_internal_energy"))}};}
            if (kind == ComponentKind.GasFuelInjector)
            {
                Object(parameters, ["area", "discharge_coefficient", "crank_node", "cycle_angle", "start_angle", "duration_angle", "maximum_dose"]);
                return result with { Area = Quantity(parameters.GetProperty("area")), DischargeCoefficient = Number(parameters.GetProperty("discharge_coefficient")), FuelInjector = new()
                { CrankNode = Id(parameters, "crank_node"), CycleAngle = Quantity(parameters.GetProperty("cycle_angle")), StartAngle = Quantity(parameters.GetProperty("start_angle")),
                    DurationAngle = Quantity(parameters.GetProperty("duration_angle")), MaximumDose = Quantity(parameters.GetProperty("maximum_dose")) } };
            }
            if (kind == ComponentKind.GasPiston)
            {
                Object(parameters, ["area", "reference_volume", "reference_position", "reference_pressure"], "compression_direction");
                double direction = parameters.TryGetProperty("compression_direction", out var compression) ? Number(compression) : 1;
                if (direction is not (-1 or 1)) throw new ArgumentException("compression_direction must be -1 or 1.");
                return result with { GasPiston = new() { Area = Quantity(parameters.GetProperty("area")), ReferenceVolume = Quantity(parameters.GetProperty("reference_volume")),
                    ReferencePosition = Quantity(parameters.GetProperty("reference_position")), ReferencePressure = Quantity(parameters.GetProperty("reference_pressure")), CompressionDirection = (int)direction } };
            }
            if (kind == ComponentKind.HydraulicPiston)
            {
                uint back = Id(parameters, "back_node", true); string[] required = ["front_area", "back_area", "back_node", "minimum_position", "maximum_position", "stop_stiffness", "contact_position", "contact_stiffness"];
                if (back == 0) required = [..required, "back_pressure"];
                Object(parameters, required);
                return result with { HydraulicPiston = new()
                {
                    BackNode = back, FrontArea = Quantity(parameters.GetProperty("front_area")), BackArea = Quantity(parameters.GetProperty("back_area")),
                    BackPressure = back == 0 ? Quantity(parameters.GetProperty("back_pressure")) : default,
                    MinimumPosition = Quantity(parameters.GetProperty("minimum_position")), MaximumPosition = Quantity(parameters.GetProperty("maximum_position")),
                    StopStiffness = Quantity(parameters.GetProperty("stop_stiffness")), ContactPosition = Quantity(parameters.GetProperty("contact_position")), ContactStiffness = Quantity(parameters.GetProperty("contact_stiffness"))
                } };
            }
            if (kind == ComponentKind.PistonClutch)
            {
                Object(parameters, ["piston_component", "effective_radius", "static_friction", "sliding_friction", "friction_surfaces", "ratio"]);
                return result with { Ratio = Number(parameters.GetProperty("ratio")), PistonClutch = new()
                {
                    PistonComponent = Id(parameters, "piston_component"), EffectiveRadius = Quantity(parameters.GetProperty("effective_radius")),
                    StaticFriction = Number(parameters.GetProperty("static_friction")), SlidingFriction = Number(parameters.GetProperty("sliding_friction")), FrictionSurfaces = (uint)Integer(parameters.GetProperty("friction_surfaces"), 128, 1)
                } };
            }
            if (kind == ComponentKind.ResistiveLoad)
            { Object(parameters, ["resistance"]); return result with { Resistance = Quantity(parameters.GetProperty("resistance")) }; }
            if (kind == ComponentKind.PressureDutyController)
            {
                Object(parameters, ["target_channel", "sample_period_ns", "proportional_gain", "integral_gain", "minimum_duty", "maximum_duty", "initial_integral_duty"]);
                return result with { PressureDutyController = new()
                {
                    TargetChannel = Integer(parameters.GetProperty("target_channel"), long.MaxValue, 1), SamplePeriodNanoseconds = Integer(parameters.GetProperty("sample_period_ns"), 1_000_000_000, 1),
                    ProportionalGain = Quantity(parameters.GetProperty("proportional_gain")), IntegralGain = Quantity(parameters.GetProperty("integral_gain")),
                    MinimumDuty = Quantity(parameters.GetProperty("minimum_duty")), MaximumDuty = Quantity(parameters.GetProperty("maximum_duty")), InitialIntegralDuty = Quantity(parameters.GetProperty("initial_integral_duty"))
                } };
            }
            if (kind == ComponentKind.PressureController)
            {
                Object(parameters, ["target_channel", "sample_period_ns", "proportional_gain", "integral_gain", "minimum_voltage", "maximum_voltage", "initial_integral_voltage"]);
                return result with { PressureController = new()
                {
                    TargetChannel = Integer(parameters.GetProperty("target_channel"), long.MaxValue, 1),
                    SamplePeriodNanoseconds = Integer(parameters.GetProperty("sample_period_ns"), 1_000_000_000, 1),
                    ProportionalGain = Quantity(parameters.GetProperty("proportional_gain")), IntegralGain = Quantity(parameters.GetProperty("integral_gain")),
                    MinimumVoltage = Quantity(parameters.GetProperty("minimum_voltage")), MaximumVoltage = Quantity(parameters.GetProperty("maximum_voltage")),
                    InitialIntegralVoltage = Quantity(parameters.GetProperty("initial_integral_voltage"))
                } };
            }
            if (kind == ComponentKind.HydraulicPump)
            {
                uint inlet = Id(parameters, "inlet_node", true);
                string[] required = ["inlet_node", "displacement"];
                if (inlet == 0) required = [..required, "reservoir_pressure"];
                Object(parameters, required);
                return result with { HydraulicPump = new() { InletNode = inlet, Displacement = Quantity(parameters.GetProperty("displacement")) },
                    ReservoirPressure = inlet == 0 ? Quantity(parameters.GetProperty("reservoir_pressure")) : default };
            }
            if (kind is ComponentKind.HydraulicResistance or ComponentKind.HydraulicOrifice or ComponentKind.HydraulicRelief or ComponentKind.HydraulicSpoolValve)
            {
                string[] required = kind == ComponentKind.HydraulicRelief ? ["coefficient", "cracking_pressure"] : kind == ComponentKind.HydraulicResistance ? ["coefficient"] : ["coefficient", "transition_pressure"];
                if (kind == ComponentKind.HydraulicSpoolValve) required = [..required, "piston_component", "closed_position", "full_open_position"];
                if (result.NodeB == 0) required = [..required, "reservoir_pressure"];
                Object(parameters, required);
                return result with { ReservoirPressure = result.NodeB == 0 ? Quantity(parameters.GetProperty("reservoir_pressure")) : default,
                    SpoolValve = kind == ComponentKind.HydraulicSpoolValve ? new() { PistonComponent = Id(parameters, "piston_component"), ClosedPosition = Quantity(parameters.GetProperty("closed_position")), FullOpenPosition = Quantity(parameters.GetProperty("full_open_position")) } : null,
                    HydraulicRestriction = new() { Coefficient = Quantity(parameters.GetProperty("coefficient")),
                        TransitionPressure = kind is ComponentKind.HydraulicOrifice or ComponentKind.HydraulicSpoolValve ? Quantity(parameters.GetProperty("transition_pressure")) : default,
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
            if (kind is ComponentKind.IdealGear or ComponentKind.PlanetaryGear or ComponentKind.DoublePinionPlanetaryGear or ComponentKind.CarrierGear)
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
            if (kind is ComponentKind.Shaft or ComponentKind.LinearSpring)
            {
                Object(parameters, ["stiffness", "damping", "rest_angle", "ratio"]);
                return result with { Stiffness = Quantity(parameters.GetProperty("stiffness")), Damping = Quantity(parameters.GetProperty("damping")),
                    RestAngle = Quantity(parameters.GetProperty("rest_angle")), Ratio = Number(parameters.GetProperty("ratio")) };
            }
            if (kind is ComponentKind.DcMotor or ComponentKind.BatteryMotor)
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
