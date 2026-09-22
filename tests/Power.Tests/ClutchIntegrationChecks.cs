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

internal static class ClutchIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fired clutch JSON / load work / friction heat / every portable boundary", Replay),
        ("clutch agent / strict capacity contract / input atomicity / revisions / forks", Contracts)
    ];
    private static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fired-clutch.power.json"));
    private static JsonElement Data(AgentReply reply)
    { Require(reply.Ok, reply.Error?.Message ?? "Expected success"); return reply.Data!.Value; }
    private static void Replay()
    {
        var document = ModelDocument.Parse(Source); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed, string.Join(", ", report.Checks.Where(c => !c.Passed)));
        Require(report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 67);
        Require(report.Model.Fidelity == "hybrid_clutch_powertrain" && report.Model.Calibration == "unverified");
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Fired clutch")));
        var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
        double previousHeat = 0; var modes = new HashSet<double>();
        foreach (var boundary in report.Samples)
        {
            while (playback.TimeNanoseconds < boundary.TimeNs)
                Require(playback.Advance(Math.Min(1_350_000, boundary.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == boundary.StateHash);
            Require(values.All(v => v.Value == boundary.Values[v.Channel.ToString()]));
            double Get(uint id, Field field) => boundary.Values[Channels.Output(id, field).ToString()];
            double heat = Get(16, Field.FrictionHeat); Require(heat >= previousHeat); previousHeat = heat;
            Near(Get(5, Field.Temperature), 300 + heat / 200, 2e-9);
            Near(Get(0, Field.EnergyResidual), 0, 1e-6);
            modes.Add(Get(16, Field.ClutchMode));
        }
        Require(modes.Contains(0) && modes.Contains(1) && modes.Contains(2));
        var final = report.Samples.Last().Values;
        Require(final[Channels.Output(0, Field.SourceWork).ToString()] < -1 && previousHeat > 1);
        // Disengaging must change transmitted motion while the same engine still burns fuel.
        var disconnected = document with { Events = document.Events.Select(e => e with { Values = e.Values.Where(v => v.Channel != 104).ToArray() }).Where(e => e.Values.Length > 0).ToArray(), Checks = [] };
        var open = ExperimentRunner.Evaluate(disconnected);
        Require(open.Samples.Last().Values[Channels.Output(16, Field.FrictionHeat).ToString()] == 0);
        Require(open.Samples.Last().Values[Channels.Output(4, Field.Speed).ToString()] < final[Channels.Output(4, Field.Speed).ToString()] - 10);
    }
    private static void Contracts()
    {
        void Reject(Action<JsonNode> mutate, string code)
        {
            var node = JsonNode.Parse(Source)!; mutate(node); var json = JsonSerializer.SerializeToElement(node);
            var reply = AgentWorkspace.ValidateModel(json); Require(reply.Error?.Code == code, reply.Error?.Message ?? "Expected rejection");
            Require(!AgentWorkspace.ExportModelAsset(json).Ok && !new AgentWorkspace().CreateSession(json).Ok);
        }
        Reject(n => n["components"]![6]!["parameters"]!.AsObject().Remove("static_capacity"), "invalid_argument");
        Reject(n => n["components"]![6]!["parameters"]!["sliding_capacity"]!["value"] = 101, "model_range");
        Reject(n => n["components"]![6]!["parameters"]!["static_capacity"]!["unit"] = "j", "model_unit");
        Reject(n => n["components"]![6]!["parameters"]!["ratio"] = 0, "model_range");
        Reject(n => n["components"]![6]!["parameters"]!["pressure"] = 1, "invalid_argument");
        Reject(n => n["components"]![6]!.AsObject().Remove("initial_input"), "invalid_argument");
        Reject(n => n["components"]![6]!["heat_node"] = 4, "model_connection");
        Reject(n => n["experiment"]!["events"]![0]!["values"]![0]!["value"] = 1.01, "invalid_argument");
        var example = Data(AgentWorkspace.ExampleModel("fired-clutch"));
        Require(JsonNode.DeepEquals(JsonNode.Parse(Source), JsonNode.Parse(example.GetRawText())));
        Require(Data(AgentWorkspace.Capabilities()).GetProperty("clutch").GetProperty("input_max").GetInt32() == 1);
        var workspace = new AgentWorkspace(); var created = Data(workspace.CreateSession(example)); string id = created.GetProperty("session_id").GetString()!;
        string before = Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.SetInputs(id, "0", [new("102", -20), new("104", 1.01)]).Error is { Code: "invalid_input", CurrentRevision: "0" });
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Require(workspace.StepSession(id, "0", "100000000", cancel.Token).Error is { Code: "cancelled", CurrentRevision: "0" });
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == before);
        Data(workspace.SetInputs(id, "0", [new("104", 1)])); Data(workspace.StepSession(id, "1", "100000000"));
        before = Data(workspace.ReadSnapshot(id)).GetRawText();
        var child = Data(workspace.ForkSession(id, "2")); string childId = child.GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(childId, "0", [new("104", 0)])); Data(workspace.StepSession(childId, "1", "100000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == before);
        Require(workspace.StepSession(id, "1", "50000").Error?.Code == "revision_conflict");
        Data(workspace.CloseSession(id, "2")); Data(workspace.CloseSession(childId, "2"));
        var failed = JsonNode.Parse(Source)!; failed["experiment"]!["checks"] = JsonSerializer.SerializeToNode(new[] { new { object_id = 16, field = "friction_heat", max = 0 } });
        Require(!Data(AgentWorkspace.RunExperiment(JsonSerializer.SerializeToElement(failed))).GetProperty("passed").GetBoolean());
    }
}
