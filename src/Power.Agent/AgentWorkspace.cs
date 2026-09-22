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
        version = "0.13.0", model_schema = "power.model.v1", report_schema = "power.experiment_report.v2", asset_format = AssetCodec.FormatName,
        readable_asset_formats = new[] { "power.asset.v1", "power.asset.v2", "power.asset.v3", "power.asset.v4", "power.asset.v5", "power.asset.v6", "power.asset.v7", "power.asset.v8", "power.asset.v9", "power.asset.v10", AssetCodec.FormatName },
        domains = new[] { "rotational", "thermal", "gas", "hydraulic" },
        components = new[] { "shaft", "dc_motor", "torque_source", "thermal_link", "sealed_cylinder", "gas_orifice", "gas_heat_link", "gas_cylinder", "premixed_combustion", "clutch", "ideal_gear", "planetary_gear", "torque_converter", "hydraulic_resistance", "hydraulic_orifice", "hydraulic_clutch", "hydraulic_pump", "hydraulic_relief" },
        examples = new[] { "electrothermal", "sealed-cylinder", "gas-network", "moving-cylinder", "crank-timed-cylinder", "fired-cylinder", "fired-clutch", "fired-planetary", "fired-converter", "fired-hydraulic", "fired-pump" },
        limits = new { nodes = CompiledModel.MaxNodes, components = CompiledModel.MaxComponents, states = CompiledModel.MaxStates,
            sessions = MaxSessions, ticks_per_step = 1_000_000, experiment_ticks = 10_000_000, document_bytes = 1_048_576, asset_bytes = AssetCodec.MaxBytes },
        determinism = "Exact replay within the same binary/runtime/architecture. Compare tolerances across platforms.",
        fidelity = new[] { "linear_lumped", "sealed_adiabatic_gas", "finite_volume_gas_exchange", "moving_cylinder_gas_exchange", "crank_timed_gas_exchange", "premixed_gas_transport", "premixed_wiebe_combustion", "hybrid_clutch_powertrain", "constrained_gear_powertrain", "quasisteady_converter_powertrain", "compliant_hydraulic_powertrain", "shaft_driven_hydraulics" }, calibration = "unverified",
        hydraulic_pump = new { component = "hydraulic_pump", displacement_unit = "m3_rad", ports = "node_a=shaft; node_b=outlet; parameters.inlet_node=inlet or zero reservoir",
            equation = "Q=D*omega; shaft reaction=-D*(p_out-p_in); fluid power=-shaft reaction*omega",
            relief = "hydraulic_relief: Q=G*max(p_a-p_b-cracking_pressure,0); heat=Q*(p_a-p_b)",
            integration = "Joint midpoint pump, pressure, converter and cylinder solve; pressure-clutch capacities refreshed within the constraint iteration.",
            newton_iterations = 24, line_search_iterations = 12,
            outputs = "Last-tick mean inlet-to-outlet volume flow, shaft reaction and shaft-to-fluid power; cumulative signed hydraulic_work. Global hydraulic_work counts only reservoir work.",
            scope = "Ideal reversible displacement with explicit attached shaft inertia. No inferred leakage, drag, check valve, cavitation, spool dynamics or calibration." },
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
        "fired-hydraulic" => AgentReply.Success(Resource("power.fired-hydraulic.template.json")),
        "fired-converter" => AgentReply.Success(Resource("power.fired-converter.template.json")),
        "fired-planetary" => AgentReply.Success(Resource("power.fired-planetary.template.json")),
        _ => AgentReply.Failure("unknown_example", "Available examples: electrothermal, sealed-cylinder, gas-network, moving-cylinder, crank-timed-cylinder, fired-cylinder, fired-clutch, fired-planetary, fired-converter, fired-hydraulic, fired-pump.", field: "name")
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
        var status = s.Simulation.SubmitInputs(values.Select(v => new Scalar(ParseInteger(v.Channel), v.Value)).ToArray());
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
            ? "Numerical solve failed; state and revision are unchanged. Reduce step_ns and recreate the model/session. For cylinders keep crank travel below 0.25 rad per interval; for gas networks inspect flow area, volume, conductance and initial conditions. Timed valves require crank travel per interval <= min(0.25 rad, duration_angle/8). Premixed combustion also requires travel <= min(0.25 rad, burn duration/32) and heat release <= 25% of pre-burn thermal energy. For hydraulics inspect compliance, valve coefficients, gauge pressures and actuator geometry; reduce step_ns if pressures become negative. For converters inspect map slopes and speed/inertia scales. For ideal gears inspect constraint rank and inertia/ratio scales. For clutches inspect inertia/ratio scales, redundant constraints, capacity schedules and the bounded interval/constraint limits in capabilities."
            : "Operation rejected; session state and revision are unchanged. Inspect capabilities, channels and fixed step before retrying. Gas openings, burn multipliers and clutch engagement must be finite fractions in [0, 1].",
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
