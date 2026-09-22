// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Text.Json;
using System.Text.Json.Nodes;
using Power.Agent;
using Power.Assets;
using Power.Core;
using Power.Experiments;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class GasIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("gas JSON / explicit units / direct Core equivalence / all replay boundaries", Replay),
        ("gas JSON / strict composition and reservoir fields / scheduled opening bounds", Validation),
        ("gas agent / discovery / revision and cancellation / branch independence / failed KPI", Agent)
    ];
    private static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "gas-network.power.json"));
    private static JsonElement Data(AgentReply reply)
    {
        Require(reply.Ok, reply.Error?.Message ?? "Expected success");
        return reply.Data!.Value;
    }

    private static void Replay()
    {
        var document = ModelDocument.Parse(Source);
        var direct = CompiledModel.Compile(new()
        {
            StepNanoseconds = 100_000,
            Nodes = [NodeDefinition.GasVolume(1, 0.002, 8e5, 600), NodeDefinition.GasVolume(2, 0.001, 1e5, 300), NodeDefinition.Thermal(3, 500, 300)],
            Components = [ComponentDefinition.GasOrifice(10, 1, 2, 20e-6, 0.8, 100, 0),
                ComponentDefinition.GasReservoir(11, 2, 5e-6, 1e5, 300, 0.9, 101, 0), ComponentDefinition.GasHeatLink(12, 1, 3, 2)]
        });
        var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 14);
        Require(report.Model.Fingerprint == direct.Fingerprint.ToString("x16") && report.Model.Fidelity == "finite_volume_gas_exchange");
        Require(report.Model.Calibration == "unverified");
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Gas boundary replay")));
        var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
        Require(asset.SourceSha256 == document.SourceSha256);
        foreach (var boundary in report.Samples)
        {
            while (playback.TimeNanoseconds < boundary.TimeNs)
                Require(playback.Advance(Math.Min(7_300_000, boundary.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == boundary.StateHash);
            Require(values.All(v => v.Value == boundary.Values[v.Channel.ToString()]));
        }
        // Independent boundary identities establish ledger meaning across the JSON adapter.
        double initialMass = 8e5 * .002 / (287 * 600) + 1e5 * .001 / (287 * 300);
        double Value(uint id, Field field) => report.Samples[^1].Values[Channels.Output(id, field).ToString()];
        Require(Value(1, Field.Mass) + Value(2, Field.Mass) < initialMass && Value(0, Field.ReservoirEnthalpy) < 0);
        double deltaEnergy = Value(1, Field.InternalEnergy) + Value(2, Field.InternalEnergy) - (8e5 * .002 + 1e5 * .001) / .4
            + 500 * (Value(3, Field.Temperature) - 300);
        Near(deltaEnergy, Value(0, Field.ReservoirEnthalpy), 1e-6);
    }

    private static void Validation()
    {
        void Reject(Action<JsonNode> change, string? code = null)
        {
            var json = JsonNode.Parse(Source)!; change(json);
            var element = JsonSerializer.SerializeToElement(json);
            foreach (var reply in new[] { AgentWorkspace.ValidateModel(element), AgentWorkspace.ExportModelAsset(element), new AgentWorkspace().CreateSession(element) })
                Require(!reply.Ok && (code is null || reply.Error?.Code == code), reply.Error?.Message ?? "Invalid gas model accepted");
        }
        Reject(n => n["nodes"]![0]!.AsObject().Remove("gas"), "model_schema");
        Reject(n => n["nodes"]![0]!["gas"]!["gamma"] = 1, "model_range");
        Reject(n => n["nodes"]![0]!["gas"]!["gas_constant"]!["unit"] = "j_k", "model_unit");
        Reject(n => n["nodes"]![1]!["gas"]!["gamma"] = 1.3, "model_connection");
        Reject(n => n["nodes"]![2]!["gas"] = n["nodes"]![0]!["gas"]!.DeepClone(), "model_schema");
        Reject(n => n["nodes"]![0]!.AsObject().Remove("position"), "invalid_argument");
        Reject(n => n["nodes"]![0]!["gas"]!["species"] = "air", "invalid_argument");
        Reject(n => n["components"]![0]!.AsObject().Remove("initial_input"), "invalid_argument");
        Reject(n => n["components"]![0]!["parameters"]!["reservoir_pressure"] = JsonSerializer.SerializeToNode(new { value = 1, unit = "bar" }), "invalid_argument");
        Reject(n => n["components"]![1]!["parameters"]!.AsObject().Remove("reservoir_temperature"), "invalid_argument");
        Reject(n => n["components"]![2]!["node_b"] = 2, "model_connection");
        Reject(n => n["components"]![0]!["initial_input"]!["value"] = 1.01, "model_range");
        foreach (double invalid in new[] { -0.001, 1.001 })
            Reject(n => n["experiment"]!["events"]![4]!["values"]![0]!["value"] = invalid, "invalid_argument");
        Throws<ArgumentException>(() => ModelDocument.Parse(Source.Replace("\"gamma\": 1.4", "\"gamma\": 1.4, \"gamma\": 1.3")));
        Throws<ArgumentException>(() => ModelDocument.Parse(Source.Replace("\"gamma\": 1.4", "\"gamma\": 1e999")));

        // Omitted input channels retain the explicitly declared fixed opening.
        var fixedOpening = JsonNode.Parse(Source)!;
        foreach (var c in fixedOpening["components"]!.AsArray()) c!.AsObject().Remove("input_channel");
        fixedOpening["experiment"]!["events"] = new JsonArray();
        Require(AgentWorkspace.ValidateModel(JsonSerializer.SerializeToElement(fixedOpening)).Ok);
    }

    private static void Agent()
    {
        var capabilities = Data(AgentWorkspace.Capabilities());
        Require(capabilities.GetProperty("domains").EnumerateArray().Any(d => d.GetString() == "gas"));
        Require(capabilities.GetProperty("asset_format").GetString() == "power.asset.v11" && capabilities.GetProperty("readable_asset_formats").GetArrayLength() == 11);
        var example = Data(AgentWorkspace.ExampleModel("gas-network"));
        Require(JsonNode.DeepEquals(JsonNode.Parse(example.GetRawText()), JsonNode.Parse(Source)));
        var workspace = new AgentWorkspace();
        var created = Data(workspace.CreateSession(example)); string id = created.GetProperty("session_id").GetString()!;
        var channels = created.GetProperty("model").GetProperty("channels").EnumerateArray().ToArray();
        Require(channels.Any(c => c.GetProperty("unit").GetString() == "Fraction") && channels.Any(c => c.GetProperty("unit").GetString() == "KilogramPerSecond"));
        string initial = Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.SetInputs(id, "0", [new("100", .5), new("101", 1.001)]).Error is { Code: "invalid_input", CurrentRevision: "0" });
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Require(workspace.StepSession(id, "0", "100000000", cancelled.Token).Error is { Code: "cancelled", CurrentRevision: "0" });
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == initial);
        Data(workspace.SetInputs(id, "0", [new("100", 1)]));
        Require(workspace.StepSession(id, "0", "100000000").Error?.Code == "revision_conflict");
        string parent = Data(workspace.StepSession(id, "1", "100000000")).GetRawText();
        var fork = Data(workspace.ForkSession(id, "2")); string child = fork.GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(child, "0", [new("100", 0), new("101", 1)]));
        Data(workspace.StepSession(child, "1", "100000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == parent);
        Data(workspace.CloseSession(child, "2")); Data(workspace.CloseSession(id, "2"));
        var failure = JsonNode.Parse(Source)!;
        failure["experiment"]!["checks"]![0]!["max"] = 100001;
        Require(!Data(AgentWorkspace.RunExperiment(JsonSerializer.SerializeToElement(failure))).GetProperty("passed").GetBoolean());
    }
}
