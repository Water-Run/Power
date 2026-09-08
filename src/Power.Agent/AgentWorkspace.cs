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
        version = "0.4.0", model_schema = "power.model.v1", report_schema = "power.experiment_report.v2", asset_format = AssetCodec.FormatName,
        readable_asset_formats = new[] { "power.asset.v1", AssetCodec.FormatName },
        domains = new[] { "rotational", "thermal" },
        components = new[] { "shaft", "dc_motor", "torque_source", "thermal_link", "sealed_cylinder" },
        examples = new[] { "electrothermal", "sealed-cylinder" },
        limits = new { nodes = CompiledModel.MaxNodes, components = CompiledModel.MaxComponents, states = CompiledModel.MaxStates,
            sessions = MaxSessions, ticks_per_step = 1_000_000, experiment_ticks = 10_000_000, document_bytes = 1_048_576, asset_bytes = AssetCodec.MaxBytes },
        determinism = "Exact replay within the same binary/runtime/architecture. Compare tolerances across platforms.",
        fidelity = new[] { "linear_lumped", "sealed_adiabatic_gas" }, calibration = "unverified",
        cylinder_solver = new { max_angle_step_rad = 0.25, max_iterations = 16,
            recovery = "On numerical_failure, reduce step_ns and recreate the model/session; inspect speed, inertia and gas parameters. The failed call commits nothing.",
            scope = "Sealed ideal gas, constant mass and gamma, adiabatic compression/expansion. No combustion, valves, wall heat transfer or piston inertia." },
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
        _ => AgentReply.Failure("unknown_example", "Available examples: electrothermal, sealed-cylinder.", field: "name")
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
            ? "Numerical solve failed; state and revision are unchanged. Reduce step_ns and recreate the model/session. For cylinders keep crank travel below 0.25 rad per tick; inspect speed, inertia and gas parameters."
            : "Operation rejected; session state and revision are unchanged. Inspect capabilities, channels and fixed step before retrying.",
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
