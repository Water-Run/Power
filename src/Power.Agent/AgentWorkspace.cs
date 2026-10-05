// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Globalization;
using System.Text.Json;
using Power.Core;
using Power.Experiments;
using Power.Assets;
using System.Security.Cryptography;

namespace Power.Agent;

/// <summary>Transport-independent, process-local agent workspace. No Unity, credentials, network, or file access.</summary>
public sealed class AgentWorkspace
{
    public const int MaxSessions = 16;
    private sealed class Session(Simulation simulation)
    {
        internal readonly Simulation Simulation = simulation;
        internal readonly Scalar[] Buffer = new Scalar[simulation.Model.OutputCount];
        internal ulong Revision;
    }
    private readonly object _gate = new();
    private readonly Dictionary<string, Session> _sessions = new(StringComparer.Ordinal);

    public static AgentReply Capabilities() => AgentReply.Success(new
    {
        version = "0.33.0", model_schema = "power.model.v1", report_schema = "power.experiment_report.v2", asset_format = AssetCodec.FormatName,
        readable_asset_formats = new[] { "power.asset.v1", "power.asset.v2", "power.asset.v3", "power.asset.v4", "power.asset.v5", "power.asset.v6", "power.asset.v7", "power.asset.v8", "power.asset.v9", "power.asset.v10", "power.asset.v11", "power.asset.v12", "power.asset.v13", "power.asset.v14", "power.asset.v15", "power.asset.v16", "power.asset.v17", "power.asset.v18", "power.asset.v19", "power.asset.v20", "power.asset.v21", "power.asset.v22", "power.asset.v23", "power.asset.v24", "power.asset.v25", "power.asset.v26", "power.asset.v27", AssetCodec.FormatName },
        domains = new[] { "rotational", "thermal", "gas", "hydraulic", "battery", "translational" },
        components = new[] { "shaft", "dc_motor", "torque_source", "thermal_link", "sealed_cylinder", "gas_orifice", "gas_heat_link", "gas_cylinder", "premixed_combustion", "clutch", "ideal_gear", "planetary_gear", "torque_converter", "hydraulic_resistance", "hydraulic_orifice", "hydraulic_clutch", "hydraulic_pump", "hydraulic_relief", "pressure_controller", "battery_motor", "resistive_load", "pressure_duty_controller", "linear_spring", "hydraulic_piston", "piston_clutch", "force_source", "hydraulic_spool_valve", "gas_piston", "gas_fuel_injector", "fuel_film", "liquid_fuel_injector", "solenoid", "travel_stop", "needle_driver", "dct_controller", "double_pinion_planetary_gear", "carrier_gear", "at_controller", "liquid_rail_feed" },
        examples = new[] { "electrothermal", "sealed-cylinder", "gas-network", "moving-cylinder", "crank-timed-cylinder", "fired-cylinder", "fired-clutch", "fired-planetary", "fired-converter", "fired-hydraulic", "fired-pump", "fired-pump-losses", "electric-pump", "pressure-regulated-pump", "battery-regulated-pump", "piston-actuated-clutch", "spool-regulated-pump", "gas-accumulator-pump", "metered-fired-cylinder", "film-fired-cylinder", "liquid-injected-cylinder", "needle-actuated-cylinder", "closure-compensated-cylinder", "dual-clutch-transmission", "fired-dual-clutch", "controlled-dual-clutch", "controlled-fired-dual-clutch", "ravigneaux-transmission", "fired-ravigneaux-converter", "resolved-ravigneaux-transmission", "fired-resolved-ravigneaux-converter", "hydraulic-ravigneaux-transmission", "fired-hydraulic-ravigneaux", "controlled-hydraulic-ravigneaux", "controlled-fired-hydraulic-ravigneaux", "pump-fed-liquid-cylinder", "pump-fed-needle-cylinder" },
        limits = new { nodes = CompiledModel.MaxNodes, components = CompiledModel.MaxComponents, states = CompiledModel.MaxStates,
            sessions = MaxSessions, ticks_per_step = 1_000_000, experiment_ticks = 10_000_000, document_bytes = 1_048_576, asset_bytes = AssetCodec.MaxBytes },
        determinism = "Exact replay within the same binary/runtime/architecture. Compare tolerances across platforms.",
        fidelity = new[] { "linear_lumped", "sealed_adiabatic_gas", "finite_volume_gas_exchange", "moving_cylinder_gas_exchange", "crank_timed_gas_exchange", "premixed_gas_transport", "premixed_wiebe_combustion", "hybrid_clutch_powertrain", "constrained_gear_powertrain", "quasisteady_converter_powertrain", "compliant_hydraulic_powertrain", "shaft_driven_hydraulics", "sampled_pressure_control", "battery_electromechanical", "battery_pressure_control", "dynamic_piston_powertrain", "mechanically_regulated_hydraulics", "linear_gas_actuation", "gas_accumulator_powertrain", "cycle_fuel_metering", "metered_fired_powertrain", "finite_liquid_film_evaporation", "film_evaporation_fired_powertrain", "finite_liquid_fuel_delivery", "liquid_injected_fired_powertrain", "electromagnetic_linear_actuation", "elastic_translational_contact", "needle_actuated_liquid_delivery", "needle_actuated_fired_powertrain", "closure_compensated_liquid_delivery", "closure_compensated_fired_powertrain", "sampled_dual_clutch_control", "sampled_hydraulic_at_control", "pump_fed_liquid_fuel_powertrain", "finite_tank_liquid_fuel_powertrain", "recirculating_liquid_fuel_powertrain" }, calibration = "unverified",
        at_controller = new { component = "at_controller", input = "Integral requested_gear state_code in [-1,4]; zero requests vented neutral.",
            ownership = "Owns all fill/drain pairs; write requested gear instead of owned valve fractions.",
            phases = new[] { "Neutral", "Releasing", "Applying", "Driving", "Fault" },
            faults = new[] { "None", "ReleaseTimeout", "ApplyTimeout", "LowSupplyPressure", "DirectionChangeBlocked", "ConfirmedLockLost" },
            lockup = "Optional sixth piston route with speed/dwell hysteresis; physical Released/Applying/Locked/Releasing output." },
        dct_controller = new { component = "dct_controller", input = "Integral requested_gear state_code in [-1,7]; zero is neutral.",
            ownership = "Owns both drive clutches and all eight selectors; write requested gear instead of owned engagement channels.",
            policy = "Sampled physical selector/drive lock confirmation; unloaded preselection, exclusive release/engagement ramps, neutral abort, timeout/direction faults and new-request recovery.",
            scope = "Conservative torque-interrupted handoff, ideal command actuators; no complete ECU torque blending, dog/baulk-ring hardware, calibrated TCU or comprehensive faults." },
        dual_clutch_assembly = new { topology = "Seven forward constant-mesh hubs, even-path reverse idler, two input shafts and three output/final-drive branches.",
            components = "Ordinary rotor, ideal_gear and clutch records with explicit IDs, inertias, ratios, capacities and actuator channels.",
            operation = "Selectors preselect unloaded input paths; drive-clutch handoff chooses actual torque paths. Ratios are permanent constraints, not runtime gear-number multiplication.",
            scope = "Unverified research topology/parameters. Friction selectors, prescribed schedules, no detailed dog/baulk-ring, measured actuation, complete ECU/TCU or calibrated DQ200." },
        closure_prediction = new { optional_parameter = "closure_prediction_ns", max_physical_ticks = 4096,
            sampling = "Horizon aligns to physical ticks and covers at least two sample periods. A bounded monotone bracket chooses a physical-tick cutoff before the next sample.",
            scope = "Full plant replay in separate preallocated state; zero-voltage or delayed-cutoff coil, other actuator commands held, no future external events. Real flow is never clipped.",
            evidence = "Predicted additional fuel, forecast ticks, cutoff latch and pending closing ticks are observable. Failed/cancelled prediction preserves the entire real batch." },
        needle_actuation = new { solenoid = "solenoid", stop = "travel_stop", driver = "needle_driver", gradient_unit = "h_m",
            magnetics = "L(x)=L_ref+gradient*(x-x_ref)>0. Flux state, reciprocal magnetic force, copper heat and electrical work use an energy-conjugate discrete gradient.",
            flow = "Needle position sets actual nozzle opening. Fluid can continue after target/window closure or reversal; actual delivery is not clipped to requested dose.",
            control = "Sampled forward-window/delivered-dose feedback owns coil voltage; write the injector kg request. Held voltage, sampled dose and every physical history share integer clocks, rollback and forks.",
            scope = "Pressure-balanced needle, constant R and linear unsaturated inductance. No nonlinear magnetic maps, hysteresis/eddy losses, flyback/PWM hardware, axial fluid force or calibrated actuation." },
        liquid_rail_return = new { component = "liquid_rail_return", valve = "hydraulic_relief",
            flow = "Explicit exclusive one-way relief from rail to the feed source at the pump prescribed inlet pressure. Unregistered fluid paths reject.",
            thermal = "Simultaneous rail/tank caloric mixing. Explicit fraction [0,1] of valve loss travels with returned fuel; the remainder follows the valve heat route.",
            evidence = "Returned mass, caloric/chemical energy, carried fluid heat and mean flow remain observable. Research valve law; no measured regulator, cavitation or liquid boiling model." },
        liquid_fuel_tank = new { component = "liquid_fuel_tank", feed_parameter = "tank_component",
            inventory = "Finite mixed liquid mass, caloric and chemical storage at the pump prescribed inlet pressure. Return flow mixes current rail temperature.",
            filling = "Interval-average fill fraction limits positive displacement flow by remaining inventory and scales shaft reaction with the same work-conjugate displacement. Dry forward rotation transfers no fluid work.",
            scope = "Explicit prescribed inlet pressure-work boundary; no finite geometric capacity, vent/slosh, cavitation, measured filling dynamics or calibrated tank/pump hardware." },
        liquid_fuel_injector = new { component = "liquid_fuel_injector", source = "Finite compliant liquid inventory; absolute pressure falls with discharged volume.",
            input_unit = "kg", density_unit = "kg_m3", compliance_unit = "m3_pa",
            delivery = "Forward cycle quota is latched once. Exact fixed-receiver pressure-head decay limits delivery by pressure, finite inventory and remaining dose.",
            energy = "Rail caloric/chemical and pressure energy are stored. Released pressure work equals nozzle heat plus exported receiver pressure work. Only liquid joins the film; evaporation remains separate.",
            scope = "Constant density/compliance/caloric properties; negligible liquid receiver volume with explicit exported displacement work. No pump/refill, needle/electrical actuation, spray/cavitation or calibrated hardware." },
        fuel_film = new { component = "fuel_film", receiver = "Tracked gas", wall = "Finite thermal node",
            phases = "Analytic finite-bath sensible heating, declared saturation plateau, heat-limited vaporization and dryout. Only vapor joins the gas reactants.",
            energy = "Latent input is internal-energy difference in j_kg. Liquid phase offset matches the receiver vapor cv at saturation; signed liquid thermal energy is valid.",
            units = new { mass = "kg", temperature = "k", specific_heat = "j_kg_k", latent_internal_energy = "j_kg", conductance = "w_k" },
            integration = "Symmetric film/gas/mechanics/gas/film split; reverse film order on the second half-step. Other wall heat sources use the explicit outer-tick wall temperature and retain first-order coupling accuracy.",
            scope = "Negligible liquid volume, constant prescribed saturation temperature/properties, no condensation, no liquid injection/rail/needle/spray dynamics or calibrated fuel properties." },
        gas_fuel_injector = new { component = "gas_fuel_injector", source = "Finite tracked gas rail; pressure-dependent one-way fuel/enthalpy transfer.", input_unit = "kg",
            quota = "Requested fuel dose is latched once at each forward metering window. Mid-window writes apply to the next cycle; reversal cannot reissue an observed cycle quota.",
            limiter = "One half-step fuel-rate ceiling preserves the per-cycle budget. The same flow factor transports total mass, constituents and upstream thermal enthalpy.",
            limits = new { maximum_crank_travel = "min(0.25 rad,duration/8)", representable_cycle_ordinal = "abs(cycle)<2^52", nonnegative_dose = true },
            evidence = "Delivered-cycle/total fuel, requested-cycle dose and mean delivered flow are observable. Tool success does not imply the requested dose was delivered.",
            scope = "Ideal gaseous metering and common constant R/gamma/LHV; no liquid spray, evaporation, needle dynamics, rail pump/tank liquid physics or calibrated gasoline injector." },
        gas_piston = new { component = "gas_piston", motion_domain = "translational", geometry = "V=V_reference-compression_direction*area*(x-x_reference); direction is -1 or 1.",
            thermodynamics = "Finite ideal-gas mass/internal energy; adiabatic discrete pressure work, explicit absolute reference-pressure work, existing gas ports and wall heat links.",
            integration = "One shared coordinate for gas and hydraulic piston forces, analytic gas force Jacobian and stable small-travel series; up to 25% current gas-volume change per interval.",
            units = new { area = "m2", reference_volume = "m3", reference_position = "m", reference_pressure = "pa" },
            scope = "Constant-caloric ideal gas, explicit separator mass/damping and compliant hydraulic stroke ends; no bladder geometry, seal friction, cavitation, gas dissolution or OEM calibration." },
        hydraulic_spool_valve = new { component = "hydraulic_spool_valve", owner = "piston_component", position_unit = "m", coefficient_unit = "m3_s_sqrt_pa",
            metering = "opening=clamp((x-closed_position)/(full_open_position-closed_position),0,1); signed travel permits reversed lands.",
            integration = "Position and pressure evaluated at the same joint midpoint, with analytic pressure and land-position derivatives; no engagement or opening command channel.",
            physics = "Pressure-balanced metering land; passive bidirectional turbulent restriction heat, explicit pressure-actuator piston, mass, spring/damping and stroke ends.",
            scope = "No axial jet force, seal friction, cavitation, viscosity/temperature maps or calibrated valve geometry." },
        hydraulic_piston = new { component = "hydraulic_piston", slider_domain = "translational", mass_unit = "kg", velocity_unit = "m_s", stiffness_unit = "n_m", damping_unit = "n_s_m",
            pressure_force = "A_front*p_front-A_back*p_back", volume = "Front expansion draws A_front*dx; back contraction delivers A_back*dx; swept and compliant reference volumes share the ledger.",
            contact = "Unilateral elastic pad at contact_position, plus compliant nominal stroke ends; discrete potential gradients conserve pressure/contact work.",
            clutch = "piston_clutch capacity=friction*surfaces*radius*pad force. Pressure alone cannot engage a clutch before pad clearance closes.",
            integration = "Joint midpoint/discrete-gradient piston, fluid pressure, motor/converter/cylinder and clutch constraints; contact capacity refreshed within constraint iterations.",
            recovery = "Negative accepted gauge pressure rejects the whole batch. Inspect valve supply, slider mass/damping, areas/compliance and tick resolution; no cavitation clamp.",
            scope = "Constant effective fluid compliance, explicit slider mass, linear return spring/damping, elastic pad and compliant ends. No cavitation, dry seal friction, plate flexural modes, wear or OEM calibration." },
        battery = new { domain = "battery", capacity_units = new[] { "c", "ah" }, capacitance_unit = "f", state_of_charge_min = 0, state_of_charge_max = 1,
            model = "Affine OCV/SOC, explicit series resistance and one polarization RC branch; positive current discharges charge inventory.",
            energy = "Chemical energy Q*(V_empty*z+0.5*(V_full-V_empty)*z^2), polarization C*v_p^2/2; series/polarization and accessory heat share the ledger. Battery motor power is internal, not external source_work.",
            motor = "battery_motor: node_a=shaft, node_b=battery; duty in [-1,1], motor voltage=duty*bus voltage, battery current=duty*motor current",
            accessory = "resistive_load: explicit positive resistance and opening in [0,1] on a battery bus",
            integration = "Held duty/load inputs update simulation-owned coupled midpoint factors, including gear, cylinder, converter and clutch responses.",
            recovery = "SOC outside [0,1], negative bus voltage or nonfinite state rejects the whole batch. Shorten the step or change duty/load/initial charge; no silent capacity clamp.",
            scope = "Research equivalent circuit and ideal averaged bidirectional duty conversion. No PWM switching, contactors, charging/BMS strategy, ageing, temperature-dependent OCV or calibrated chemistry." },
        pressure_duty_controller = new { target = "battery_motor duty input", gain_units = new[] { "fraction_pa", "fraction_pa_s" }, output_range = "Explicit bounds within [-1,1]",
            ownership = "Owned duty cannot be written directly; use pressure_setpoint. Integral duty and held command share sample clock, rollback and fork semantics." },
        pressure_controller = new { component = "pressure_controller", sensor = "node_a=hydraulic gauge pressure", input = "pressure_setpoint in pa or bar",
            target = "parameters.target_channel owns a dc_motor voltage input; that channel cannot be written externally",
            sampling = "Samples at time zero and integer multiples of sample_period_ns; period 1 ns..1 s and tick-aligned. Holds output between samples.",
            integration = "I_candidate=I+Ki*sample_period_s*error; first sample preserves initial I. Conditional integration rejects increments further into voltage saturation.",
            units = new { proportional_gain = "v_pa", integral_gain = "v_pa_s", voltage_limits = "v", integral_state = "v" },
            state = "Last sampled pressure/error, integral voltage and held command share hashes, forks, cancellation and whole-batch rollback.",
            scope = "Ideal sampled pressure measurement and explicit voltage limits. No battery, sensor filtering/delay, PWM/current-loop dynamics or complete ECU/TCU." },
        hydraulic_pump = new { component = "hydraulic_pump", displacement_unit = "m3_rad", ports = "node_a=shaft; node_b=outlet; parameters.inlet_node=inlet or zero reservoir",
            equation = "Q=D*omega; shaft reaction=-D*(p_out-p_in); fluid power=-shaft reaction*omega",
            relief = "hydraulic_relief: Q=G*max(p_a-p_b-cracking_pressure,0); heat=Q*(p_a-p_b)",
            integration = "Joint midpoint pump, pressure, converter and cylinder solve; pressure-clutch capacities refreshed within the constraint iteration.",
            newton_iterations = 24, line_search_iterations = 12,
            outputs = "Last-tick mean inlet-to-outlet volume flow, shaft reaction and shaft-to-fluid power; cumulative signed hydraulic_work. Global hydraulic_work counts only reservoir work.",
            scope = "Ideal reversible displacement with explicit attached shaft inertia. No inferred leakage, drag, check valve, cavitation, spool dynamics or calibration." },
        pump_assembly = new { components = new[] { "hydraulic_pump", "hydraulic_resistance", "shaft" },
            net_flow = "D*omega-G*(p_out-p_in)", shaft_reaction = "-D*(p_out-p_in)-B*omega",
            losses = "G*(p_out-p_in)^2+B*omega^2; explicit thermal sinks or heat rejection",
            leakage_unit = "m3_s_pa", friction_unit = "nm_s_rad",
            electric_supply = "dc_motor on the pump shaft: RL current, back EMF, torque and copper heat; voltage is an explicit input",
            scope = "Constant supplied loss coefficients and explicit component IDs. No inferred efficiencies, battery, voltage controller, temperature-dependent viscosity or OEM calibration." },
        hydraulics = new { pressure_reference = "nonnegative_gauge_common_tank", storage = "linear_reference_volume_compliance_m3_per_pa",
            restrictions = new[] { "linear_conductance", "regularized_turbulent_coefficient", "one_way_linear_relief" }, opening_min = 0, opening_max = 1, friction_surfaces_max = 128,
            energy = "Stored energy C*p^2/2; reservoir work p_res*volume_in; restriction loss Q*delta_p. Routed thermal heat and global energy ledger share the accepted transfers.",
            volume_ledger = "Reference-volume inventory C*p; not a full variable-density liquid mass model.",
            newton_iterations = 24, line_search_iterations = 16, pressure_absolute_tolerance_pa = 2e-7, pressure_rounding_epsilon_multiplier = 64,
            pressure_clutch = "clamp=max(area*pressure-preload,0); capacity=friction*surfaces*effective_radius*clamp; pressure comes from the connected compliance node",
            integration = "Midpoint hydraulic flow and pressure-derived interval clutch capacities; internal clutch trials include hydraulic state and ledgers.",
            recovery = "On numerical_failure reduce step_ns and inspect compliance, valve coefficients, pressure scales and capacity geometry. Negative final gauge pressure rejects the entire batch; no cavitation clamp.",
            scope = "Constant effective compliance and rigid-contact pressure clutch. No internal pump losses/dynamics, piston stroke/inertia, gas accumulator law, cavitation, viscosity/temperature feedback, automatic valve controller or OEM calibration." },
        converter = new { component = "torque_converter", ports = new[] { "pump", "turbine" }, stator = "stationary_ground_reaction",
            maps = new[] { "pump_positive", "pump_negative", "turbine_positive", "turbine_negative" },
            reference_member = "larger_absolute_speed_pump_on_tie", speed_ratio_min = -1, speed_ratio_max = 1,
            coefficient_unit = "nm_s2_rad2", max_components = CompiledModel.MaxConverters, max_points_per_map = ConverterMap.MaxPoints,
            interpolation = "piecewise_linear_coefficient_and_torque_ratio_passive_between_knots", equal_speed = "zero_fluid_torque",
            newton_iterations = 24, line_search_iterations = 12, speed_absolute_tolerance_rad_s = 2e-12, speed_rounding_epsilon_multiplier = 64,
            modes = new { stopped = 0, pump_positive = 1, pump_negative = 2, turbine_positive = 3, turbine_negative = 4 },
            outputs = "Last-tick mean pump/turbine/stator torque and fluid heat power; cumulative fluid heat; current speed ratio and reference-member code. Initial mean outputs are zero.",
            integration = "Joint implicit midpoint converter/cylinder solve with gear projection and clutch events. Lockup uses a separate parallel clutch.",
            recovery = "On numerical_failure reduce step_ns and inspect map slopes, speed/inertia scales and clutch constraints. Failed or cancelled batches commit nothing.",
            scope = "Quasi-steady passive maps. No fluid inertia, fill/pressure dynamics, rotating stator, temperature fade, hydraulic actuation, automatic shifts or measured calibration." },
        gears = new { ideal_gear_relation = "omega_a = ratio * omega_b", planetary_relation = "omega_a + ratio * omega_b = (1 + ratio) * omega_c",
            planetary_ports = new[] { "sun", "ring", "carrier" }, initial_speed_policy = "require_compatible_no_impulse",
            phase_policy = "preserve_initial_relative_phase", redundant_constraints = "reject_at_compile",
            reaction_outputs = "last_tick_mean_torque_on_each_rotor", initial_reactions = "zero_before_first_tick",
            normalized_speed_residual_absolute_tolerance = 2e-12, speed_rounding_epsilon_multiplier = 512,
            normalized_phase_residual_absolute_tolerance = 2e-10, phase_rounding_epsilon_multiplier = 1024,
            scope = "Permanent massless lossless constraints with explicit attached inertias. No mesh losses, backlash, hydraulic actuator or TCU." },
        clutch = new { input_quantity = "engagement", input_min = 0, input_max = 1, max_intervals_per_tick = 32, root_iterations = 56, constraint_iterations = 256,
            modes = new { disengaged = 0, locked = 1, slipping_positive = 2, slipping_negative = 3 },
            outputs = "Actual slip speed; last interval mode; last-tick mean torque and heat power; cumulative friction heat. Initial torque/power are zero. Inputs do not rewrite the preceding tick's outputs.",
            integration = "Implicit midpoint constrained torques and coupled cylinder work, with bounded internal slip-zero bracketing and thermal routing. Nonlinear breakaway uses interval-average demand; refine ticks near transitions.",
            recovery = "On numerical_failure, reduce step_ns and recreate the session; inspect inertia/ratio scales, redundant constraints and capacity schedules. The failed or cancelled call commits no physical state, phase, heat or input changes.",
            scope = "Constant static/sliding torque capacities scaled by engagement. No hydraulics, wear, temperature fade, transmission controller or calibration." },
        cylinder_solver = new { max_angle_step_rad = 0.25, max_iterations = 16,
            recovery = "On numerical_failure, reduce step_ns and recreate the model/session; inspect speed, inertia and gas parameters. The failed call commits nothing.",
            scope = "Sealed ideal gas, constant mass and gamma, adiabatic compression/expansion. No combustion, valves, wall heat transfer or piston inertia." },
        gas_solver = new { max_substeps_per_tick = 4096, target_relative_change = 0.02, max_corrected_relative_change = 0.25,
            opening_min = 0, opening_max = 1, moving_cylinder_max_angle_step_rad = 0.25,
            integration = "Moving chambers and crank-timed restrictions use symmetric flow / crank-work / flow splitting; wall coupling is first order. Models without those features retain their previous stepping.",
            recovery = "On numerical_failure, reduce step_ns and recreate the model/session; inspect flow area, volume, conductance and initial conditions. The failed call commits nothing.",
            scope = "Fixed volumes and crank-coupled moving chambers with constant ideal-gas R/gamma, bidirectional restrictions, fixed reservoirs and wall links. Connected volumes must share gas constant and gamma. Crank-angle opening profiles are supported. Optional premixed fuel/air/product tracking and prescribed Wiebe combustion. No detailed chemistry or mechanical valve-train dynamics." },
        valve_timing = new { component = "gas_orifice", property = "valve_timing", cycle_degrees = new[] { 360, 720 },
            profile = "sin_squared", input_quantity = "peak_opening", output_quantity = "effective_opening",
            min_duration_rad = CrankValveProfile.MinimumDurationRadians, max_travel_rad = 0.25, duration_samples_min = 8,
            recovery = "On numerical_failure, reduce step_ns so crank travel and endpoint-speed travel per tick stay within min(0.25 rad, duration_angle/8). The failed call commits nothing." },
        combustion = new { component = "premixed_combustion", model = "prescribed_wiebe", constituents = new[] { "fuel", "fresh_air", "products" },
            input_quantity = "burn_multiplier", input_min = 0, input_max = 1, duration_samples_min = 32, max_heat_fraction_per_tick = .25,
            energy = "Gas internal energy is thermal. Chemical energy is unburned fuel mass times LHV. Reservoir enthalpy includes both; reaction converts chemical energy internally.",
            direction = "Only crank angles beyond the recorded forward frontier burn; reversing or retracing cannot release heat twice. Disabled angles are passed without catch-up.",
            scope = "All constituents share the gas node's fixed R and gamma. Fuel and fresh air limit reaction stoichiometrically. Prescribed burn profile, not kinetics, knock, emissions or calibration." },
        workflow = new[] { "get_model_schema", "get_example_model", "validate_model", "run_experiment", "export_model_asset",
            "create_session", "set_inputs", "step_session", "read_snapshot", "fork_session", "close_session" },
        semantics = new
        {
            time = "Integer nanoseconds. Session times, revisions and channel IDs are decimal strings to avoid JSON precision loss.",
            writes = "Use the revision returned by the last successful call. Stale revisions never mutate state. Read after any transport interruption.",
            atomicity = "Input submission and each step call are individually atomic. Failed or cancelled steps preserve all state.",
            scope = "Sessions belong to this local server process; shutdown discards them. No Unity Editor is needed.",
            evidence = "A passing experiment checks the declared KPIs and replay; it does not establish physical calibration."
        }
    });

    private static JsonElement Resource(string name)
    {
        using var stream = typeof(AgentWorkspace).Assembly.GetManifestResourceStream(name)!;
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.Clone();
    }
    public static AgentReply ModelSchema() => AgentReply.Success(Resource("power.model.schema.json"));
    public static AgentReply ExampleModel(string name = "electrothermal") => name switch
    {
        "electrothermal" => AgentReply.Success(Resource("power.template.json")),
        "sealed-cylinder" => AgentReply.Success(Resource("power.cylinder.template.json")),
        "gas-network" => AgentReply.Success(Resource("power.gas.template.json")),
        "moving-cylinder" => AgentReply.Success(Resource("power.moving-cylinder.template.json")),
        "crank-timed-cylinder" => AgentReply.Success(Resource("power.crank-timed-cylinder.template.json")),
        "fired-cylinder" => AgentReply.Success(Resource("power.fired-cylinder.template.json")),
        "fired-clutch" => AgentReply.Success(Resource("power.fired-clutch.template.json")),
        "fired-pump" => AgentReply.Success(Resource("power.fired-pump.template.json")),
        "fired-pump-losses" => AgentReply.Success(Resource("power.fired-pump-losses.template.json")),
        "electric-pump" => AgentReply.Success(Resource("power.electric-pump.template.json")),
        "pressure-regulated-pump" => AgentReply.Success(Resource("power.pressure-regulated-pump.template.json")),
        "battery-regulated-pump" => AgentReply.Success(Resource("power.battery-regulated-pump.template.json")),
        "piston-actuated-clutch" => AgentReply.Success(Resource("power.piston-actuated-clutch.template.json")),
        "spool-regulated-pump" => AgentReply.Success(Resource("power.spool-regulated-pump.template.json")),
        "gas-accumulator-pump" => AgentReply.Success(Resource("power.gas-accumulator-pump.template.json")),
        "metered-fired-cylinder" => AgentReply.Success(Resource("power.metered-fired-cylinder.template.json")),
        "film-fired-cylinder" => AgentReply.Success(Resource("power.film-fired-cylinder.template.json")),
        "liquid-injected-cylinder" => AgentReply.Success(Resource("power.liquid-injected-cylinder.template.json")),
        "needle-actuated-cylinder" => AgentReply.Success(Resource("power.needle-actuated-cylinder.template.json")),
        "closure-compensated-cylinder" => AgentReply.Success(Resource("power.closure-compensated-cylinder.template.json")),
        "dual-clutch-transmission" => AgentReply.Success(Resource("power.dual-clutch-transmission.template.json")),
        "fired-dual-clutch" => AgentReply.Success(Resource("power.fired-dual-clutch.template.json")),
        "recirculating-liquid-cylinder" => AgentReply.Success(Resource("power.recirculating-liquid-cylinder.template.json")),
        "recirculating-needle-cylinder" => AgentReply.Success(Resource("power.recirculating-needle-cylinder.template.json")),
        "finite-tank-liquid-cylinder" => AgentReply.Success(Resource("power.finite-tank-liquid-cylinder.template.json")),
        "finite-tank-needle-cylinder" => AgentReply.Success(Resource("power.finite-tank-needle-cylinder.template.json")),
        "pump-fed-liquid-cylinder" => AgentReply.Success(Resource("power.pump-fed-liquid-cylinder.template.json")),
        "pump-fed-needle-cylinder" => AgentReply.Success(Resource("power.pump-fed-needle-cylinder.template.json")),
        "controlled-hydraulic-ravigneaux" => AgentReply.Success(Resource("power.controlled-hydraulic-ravigneaux.template.json")),
        "controlled-fired-hydraulic-ravigneaux" => AgentReply.Success(Resource("power.controlled-fired-hydraulic-ravigneaux.template.json")),
        "hydraulic-ravigneaux-transmission" => AgentReply.Success(Resource("power.hydraulic-ravigneaux-transmission.template.json")),
        "fired-hydraulic-ravigneaux" => AgentReply.Success(Resource("power.fired-hydraulic-ravigneaux.template.json")),
        "resolved-ravigneaux-transmission" => AgentReply.Success(Resource("power.resolved-ravigneaux-transmission.template.json")),
        "fired-resolved-ravigneaux-converter" => AgentReply.Success(Resource("power.fired-resolved-ravigneaux-converter.template.json")),
        "ravigneaux-transmission" => AgentReply.Success(Resource("power.ravigneaux-transmission.template.json")),
        "fired-ravigneaux-converter" => AgentReply.Success(Resource("power.fired-ravigneaux-converter.template.json")),
        "controlled-dual-clutch" => AgentReply.Success(Resource("power.controlled-dual-clutch.template.json")),
        "controlled-fired-dual-clutch" => AgentReply.Success(Resource("power.controlled-fired-dual-clutch.template.json")),
        "fired-hydraulic" => AgentReply.Success(Resource("power.fired-hydraulic.template.json")),
        "fired-converter" => AgentReply.Success(Resource("power.fired-converter.template.json")),
        "fired-planetary" => AgentReply.Success(Resource("power.fired-planetary.template.json")),
        _ => AgentReply.Failure("unknown_example", "Available examples: electrothermal, sealed-cylinder, gas-network, moving-cylinder, crank-timed-cylinder, fired-cylinder, fired-clutch, fired-planetary, fired-converter, fired-hydraulic, fired-pump, fired-pump-losses, electric-pump, pressure-regulated-pump, battery-regulated-pump, piston-actuated-clutch, spool-regulated-pump, gas-accumulator-pump, metered-fired-cylinder, film-fired-cylinder, liquid-injected-cylinder, needle-actuated-cylinder, closure-compensated-cylinder, dual-clutch-transmission, fired-dual-clutch, controlled-dual-clutch, controlled-fired-dual-clutch, ravigneaux-transmission, fired-ravigneaux-converter, resolved-ravigneaux-transmission, fired-resolved-ravigneaux-converter, hydraulic-ravigneaux-transmission, fired-hydraulic-ravigneaux, controlled-hydraulic-ravigneaux, controlled-fired-hydraulic-ravigneaux, pump-fed-liquid-cylinder, pump-fed-needle-cylinder, finite-tank-liquid-cylinder, finite-tank-needle-cylinder, recirculating-liquid-cylinder, recirculating-needle-cylinder.", field: "name")
    };

    private static AgentReply Guard(Func<AgentReply> work)
    {
        try { return work(); }
        catch (ModelCompileException e) { return AgentReply.Failure("model_" + e.Code.ToString().ToLowerInvariant(), e.Message, objectId: e.ObjectId, field: e.Field); }
        catch (JsonException e) { return AgentReply.Failure("invalid_json", e.Message, field: e.Path); }
        catch (OperationCanceledException) { return AgentReply.Failure("cancelled", "Operation cancelled; no session changes were committed.", true); }
        catch (Exception e) when (e is ArgumentException or FormatException or OverflowException)
        { return AgentReply.Failure("invalid_argument", e.Message); }
    }

    private static object Describe(CompiledModel model) => new
    {
        fingerprint = model.Fingerprint.ToString("x16"), step_ns = model.StepNanoseconds.ToString(CultureInfo.InvariantCulture),
        node_count = model.NodeCount, component_count = model.ComponentCount, state_count = model.StateCount,
        fidelity = model.Fidelity, calibration = model.Calibration,
        channels = model.Channels.Select(c => new
        {
            channel = c.Id.ToString(CultureInfo.InvariantCulture), object_id = c.ObjectId,
            direction = c.IsInput ? "input" : "output", unit = c.Unit.ToString(), quantity = c.Quantity
        }).ToArray()
    };

    public static AgentReply ValidateModel(JsonElement document) => Guard(() =>
    {
        var parsed = ModelDocument.Parse(document.GetRawText());
        return AgentReply.Success(new { model = Describe(ExperimentRunner.Validate(parsed)), source_sha256 = parsed.SourceSha256 });
    });

    public static AgentReply RunExperiment(JsonElement document, bool includeSamples = false, CancellationToken cancellationToken = default) => Guard(() =>
    {
        var report = ExperimentRunner.Evaluate(ModelDocument.Parse(document.GetRawText()), cancellationToken);
        // The default report remains small enough for a model context. Full traces are opt-in.
        return AgentReply.Success(new
        {
            passed = report.Passed, report.Schema, report.Runtime, report.Machine, report.SourceSha256, report.Model,
            report.Replay, report.Checks, report.ElapsedSeconds,
            final_sample = report.Samples[^1], samples = includeSamples ? report.Samples : null
        });
    });

    public static AgentReply ExportModelAsset(JsonElement document, string name = "Power model") => Guard(() =>
    {
        var asset = ModelDocument.Parse(document.GetRawText()).ToAsset(name);
        byte[] bytes = AssetCodec.Encode(asset);
        return AgentReply.Success(new
        {
            format = AssetCodec.FormatName, file_name = "model.powerasset", asset.Name, byte_count = bytes.Length,
            model_fingerprint = asset.Model.Fingerprint.ToString("x16"), source_sha256 = asset.SourceSha256,
            asset_sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), encoding = "base64", content = Convert.ToBase64String(bytes),
            use = "Decode content to a .powerasset file inside Unity/Assets, then select it in the Studio. The service has not written a file.",
            calibration = asset.Model.Calibration
        });
    });

    public AgentReply CreateSession(JsonElement document) => Guard(() =>
    {
        var model = ExperimentRunner.Validate(ModelDocument.Parse(document.GetRawText()));
        lock (_gate)
        {
            if (_sessions.Count >= MaxSessions) return AgentReply.Failure("session_capacity", "Close a session before creating another.");
            string id = Guid.NewGuid().ToString("N");
            var session = new Session(model.CreateSimulation());
            _sessions.Add(id, session);
            return AgentReply.Success(new { session_id = id, model = Describe(model), snapshot = Snapshot(session) });
        }
    });

    // The workspace serializes access so compare-revision and mutation form one transaction.
    // Independent workspaces can run in parallel, and standalone experiment runs don't hold this gate.
    private AgentReply Use(string id, string? expectedRevision, Func<Session, AgentReply> action, bool readOnly = false) => Guard(() =>
    {
        lock (_gate)
        {
            if (!_sessions.TryGetValue(id, out var session)) return AgentReply.Failure("unknown_session", "Session is absent or closed. Create a new session.");
            if (!readOnly && expectedRevision is null) return AgentReply.Failure("invalid_argument", "An expected revision is required for mutations.");
            if (expectedRevision is not null && expectedRevision != session.Revision.ToString(CultureInfo.InvariantCulture))
                return AgentReply.Failure("revision_conflict", "Read the snapshot, then use its current revision.", true,
                    revision: session.Revision.ToString(CultureInfo.InvariantCulture));
            return action(session);
        }
    });

    private static object Snapshot(Session session, string[]? requestedChannels = null)
    {
        HashSet<ulong>? selected = null;
        if (requestedChannels is not null)
        {
            selected = requestedChannels.Select(ParseInteger).ToHashSet();
            var known = session.Simulation.Model.Channels.Where(c => !c.IsInput).Select(c => c.Id).ToHashSet();
            if (selected.Count != requestedChannels.Length || !selected.IsSubsetOf(known))
                throw new ArgumentException("Requested channels must be unique known output IDs.");
        }
        var snapshot = session.Simulation.ReadSnapshot(session.Buffer);
        return new
        {
            revision = session.Revision.ToString(CultureInfo.InvariantCulture),
            time_ns = snapshot.TimeNanoseconds.ToString(CultureInfo.InvariantCulture), state_hash = snapshot.StateHash.ToString("x16"),
            values = session.Buffer.Where(v => selected is null || selected.Contains(v.Channel)).Select(v => new
            { channel = v.Channel.ToString(CultureInfo.InvariantCulture), value = v.Value }).ToArray()
        };
    }

    private static ulong ParseInteger(string value) => ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out ulong result)
        ? result : throw new ArgumentException("Expected an unsigned integer encoded as a decimal string.");

    public AgentReply ReadSnapshot(string id, string[]? channels = null) => Use(id, null,
        s => AgentReply.Success(Snapshot(s, channels)), readOnly: true);

    public AgentReply SetInputs(string id, string expectedRevision, ChannelInput[] values) => Use(id, expectedRevision, s =>
    {
        if (values is null || values.Length == 0 || values.Length > CompiledModel.MaxComponents || values.Any(v => v is null))
            throw new ArgumentException("Provide 1..64 input values.");
        var parsed = values.Select(v => new Scalar(ParseInteger(v.Channel), v.Value)).ToArray();
        var status = s.Simulation.SubmitInputs(parsed);
        if (status == SimulationStatus.ControlledInput)
            foreach (var value in parsed)
                if (s.Simulation.Model.TryGetControlCommand(value.Channel, out var command))
                    return AgentReply.Failure("controlled_input", $"Input channel {value.Channel} is controller-owned. Write {command.Quantity} on channel {command.Id} ({command.Unit}) instead; state and revision are unchanged.",
                        revision: s.Revision.ToString(CultureInfo.InvariantCulture));
        if (status != SimulationStatus.Ok) return Status(status, s);
        ++s.Revision;
        return AgentReply.Success(Snapshot(s));
    });

    public AgentReply StepSession(string id, string expectedRevision, string deltaNanoseconds, CancellationToken cancellationToken = default) =>
        Use(id, expectedRevision, s =>
        {
            var status = s.Simulation.Step(ParseInteger(deltaNanoseconds), cancellationToken);
            if (status != SimulationStatus.Ok) return Status(status, s);
            ++s.Revision;
            return AgentReply.Success(Snapshot(s));
        });

    private static AgentReply Status(SimulationStatus status, Session s) => AgentReply.Failure(
        JsonNamingPolicy.SnakeCaseLower.ConvertName(status.ToString()),
        status == SimulationStatus.NumericalFailure
            ? "Numerical solve failed; state and revision are unchanged. Reduce step_ns and recreate the model/session. For cylinders keep crank travel below 0.25 rad per interval; for gas networks inspect flow area, volume, conductance and initial conditions. Timed valves require crank travel per interval <= min(0.25 rad, duration_angle/8). Premixed combustion also requires travel <= min(0.25 rad, burn duration/32) and heat release <= 25% of pre-burn thermal energy. For hydraulics inspect compliance, valve coefficients, gauge pressures and actuator geometry; reduce step_ns if pressures become negative. For closure prediction inspect the aligned finite horizon, available integer time and monotone cutoff bracket; reduce the sample period/horizon when control assumptions fail. For solenoids keep L(x)>0, resolve needle travel and inspect R/L/gradient and stroke scales; reducing step_ns also resolves controller/closing dynamics. For converters inspect map slopes and speed/inertia scales. For ideal gears inspect constraint rank and inertia/ratio scales. For clutches inspect inertia/ratio scales, redundant constraints, capacity schedules and the bounded interval/constraint limits in capabilities."
            : status == SimulationStatus.ControlledInput ? "This actuator input is owned by a pressure controller. Write its pressure_setpoint input instead; state and revision are unchanged."
            : "Operation rejected; session state and revision are unchanged. Inspect capabilities, channels and fixed step before retrying. Gas openings, burn multipliers and clutch engagement must be finite fractions in [0, 1]; pressure setpoints must be nonnegative.",
        status is SimulationStatus.Cancelled or SimulationStatus.Busy, revision: s.Revision.ToString(CultureInfo.InvariantCulture));

    public AgentReply ForkSession(string id, string expectedRevision) => Use(id, expectedRevision, s =>
    {
        if (_sessions.Count >= MaxSessions) return AgentReply.Failure("session_capacity", "Close a session before creating another.");
        string branchId = Guid.NewGuid().ToString("N");
        var branch = new Session(s.Simulation.Fork());
        _sessions.Add(branchId, branch);
        return AgentReply.Success(new { session_id = branchId, parent_session_id = id, model = Describe(branch.Simulation.Model), snapshot = Snapshot(branch) });
    });

    public AgentReply CloseSession(string id, string expectedRevision) => Use(id, expectedRevision, _ =>
    {
        _sessions.Remove(id);
        return AgentReply.Success(new { session_id = id, closed = true });
    });
}
