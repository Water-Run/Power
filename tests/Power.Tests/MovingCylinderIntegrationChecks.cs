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

internal static class MovingCylinderIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("moving-cylinder JSON / motored gas exchange / portable replay / all boundaries", Replay),
        ("moving-cylinder agent / strict geometry and ownership / numerical recovery", Agent)
    ];
    private static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "moving-cylinder.power.json"));
    private static JsonElement Data(AgentReply reply)
    {
        Require(reply.Ok, reply.Error?.Message ?? "Expected success");
        return reply.Data!.Value;
    }

    private static void Replay()
    {
        var document = ModelDocument.Parse(Source); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 28);
        Require(report.Model.Fidelity == "moving_cylinder_gas_exchange" && report.Model.Calibration == "unverified");
        var decoded = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Moving chamber replay")));
        Require(decoded.Components[0].MovingCylinder == MovingCylinderChecks.Geometry);
        var playback = decoded.CreatePlayback(); var values = new Scalar[decoded.Model.OutputCount];
        foreach (var boundary in report.Samples)
        {
            while (playback.TimeNanoseconds < boundary.TimeNs)
                Require(playback.Advance(Math.Min(1_350_000, boundary.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == boundary.StateHash);
            Require(values.All(v => v.Value == boundary.Values[v.Channel.ToString()]));
        }
        double[] Trace(uint id, Field field) => report.Samples.Select(s => s.Values[Channels.Output(id, field).ToString()]).ToArray();
        Require(Trace(2, Field.Mass).Max() > 3 * Trace(2, Field.Mass).Min(), "Open ports must change the moving chamber's mass");
        Require(Trace(11, Field.MassFlow).Any(v => v < 0) && Trace(12, Field.MassFlow).Any(v => v > 0), "Motored sample must admit and expel gas");
        Require(Trace(10, Field.Volume).Max() > 5 * Trace(10, Field.Volume).Min(), "Crank must move through a substantial stroke");
    }

    private static void Agent()
    {
        var example = Data(AgentWorkspace.ExampleModel("moving-cylinder"));
        Require(JsonNode.DeepEquals(JsonNode.Parse(Source), JsonNode.Parse(example.GetRawText())));
        var capabilities = Data(AgentWorkspace.Capabilities());
        Require(capabilities.GetProperty("components").EnumerateArray().Any(c => c.GetString() == "gas_cylinder"));
        void Reject(Action<JsonNode> mutate, string code)
        {
            var document = JsonNode.Parse(Source)!; mutate(document);
            var json = JsonSerializer.SerializeToElement(document);
            Require(AgentWorkspace.ValidateModel(json).Error?.Code == code);
            Require(!AgentWorkspace.ExportModelAsset(json).Ok && !new AgentWorkspace().CreateSession(json).Ok);
        }
        Reject(n => n["components"]![0]!["parameters"]!.AsObject().Remove("phase"), "invalid_argument");
        Reject(n => n["components"]![0]!["parameters"]!["gamma"] = 1.4, "invalid_argument");
        Reject(n => n["components"]![0]!["parameters"]!["back_pressure"]!["value"] = -1, "model_range");
        Reject(n => n["components"]![0]!["node_b"] = 3, "model_connection");
        Reject(n => n["nodes"]![1]!["storage"] = JsonSerializer.SerializeToNode(new { value = .001, unit = "m3" }), "model_schema");
        Reject(n => n["components"]![0]!["parameters"]!["bore"]!["unit"] = "pa", "model_unit");
        var w = new AgentWorkspace(); var created = Data(w.CreateSession(example)); string id = created.GetProperty("session_id").GetString()!;
        Data(w.SetInputs(id, "0", [new("102", 1e12)]));
        string start = Data(w.ReadSnapshot(id)).GetRawText();
        Require(w.StepSession(id, "1", "50000").Error is { Code: "numerical_failure", CurrentRevision: "1" });
        Require(Data(w.ReadSnapshot(id)).GetRawText() == start);
        Data(w.SetInputs(id, "1", [new("102", 2), new("100", 1)]));
        Data(w.StepSession(id, "2", "1000000"));
        Data(w.CloseSession(id, "3"));
    }
}
