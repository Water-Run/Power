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

internal static class CombustionIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fired cylinder JSON / load work / fuel accounting / all portable boundaries", Replay),
        ("premixed JSON / strict composition and burn contract / actionable validation", Contracts),
        ("combustion agent / revision and branch independence / runtime recovery / KPI distinction", Agent)
    ];
    private static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fired-cylinder.power.json"));
    private static JsonElement Data(AgentReply reply)
    { Require(reply.Ok, reply.Error?.Message ?? "Expected success"); return reply.Data!.Value; }

    private static void Replay()
    {
        var document = ModelDocument.Parse(Source); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 63);
        Require(report.Model.Fidelity == "premixed_wiebe_combustion" && report.Model.Calibration == "unverified");
        var original = document.ToAsset("Fired replay"); var decoded = AssetCodec.Decode(AssetCodec.Encode(original));
        Require(decoded.Nodes.SequenceEqual(original.Nodes) && decoded.Components.SequenceEqual(original.Components));
        var playback = decoded.CreatePlayback(); var values = new Scalar[decoded.Model.OutputCount];
        foreach (var boundary in report.Samples)
        {
            while (playback.TimeNanoseconds < boundary.TimeNs)
                Require(playback.Advance(Math.Min(1_350_000, boundary.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == boundary.StateHash);
            Require(values.All(v => v.Value == boundary.Values[v.Channel.ToString()]));
            double Get(uint id, Field f) => boundary.Values[Channels.Output(id, f).ToString()];
            Near(Get(2, Field.Mass), Get(2, Field.FuelMass) + Get(2, Field.FreshAirMass) + Get(2, Field.ProductMass), 1e-18);
            Near(Get(15, Field.HeatReleased), 44e6 * Get(15, Field.FuelBurned), 1e-12);
            Near(Get(2, Field.ChemicalEnergy), 44e6 * Get(2, Field.FuelMass), 1e-12);
            Near(Get(0, Field.EnergyResidual), 0, 1e-6);
            Near(Get(0, Field.FuelResidual), 0, 1e-15); Near(Get(0, Field.FreshAirResidual), 0, 1e-14);
        }
        var final = report.Samples.Last().Values;
        Require(final[Channels.Output(0, Field.SourceWork).ToString()] < -300, "Engine must deliver work to its external load");
        Require(final[Channels.Output(15, Field.HeatReleased).ToString()] > 1000);
        var disabled = JsonNode.Parse(Source)!; disabled["components"]![5]!["initial_input"]!["value"] = 0;
        disabled["experiment"]!["checks"] = new JsonArray();
        var unfired = ExperimentRunner.Evaluate(ModelDocument.Parse(disabled.ToJsonString()));
        Require(unfired.Samples.Last().Values[Channels.Output(15, Field.FuelBurned).ToString()] == 0);
        Require(final[Channels.Output(1, Field.Speed).ToString()] > unfired.Samples.Last().Values[Channels.Output(1, Field.Speed).ToString()] + 10,
            "Reaction must affect crank motion, not merely a heat counter");
    }
    private static void Contracts()
    {
        void Reject(Action<JsonNode> mutate, string code)
        {
            var node = JsonNode.Parse(Source)!; mutate(node); var json = JsonSerializer.SerializeToElement(node);
            var reply = AgentWorkspace.ValidateModel(json); Require(reply.Error?.Code == code, $"Expected {code}: {reply.Error?.Message}");
            Require(!AgentWorkspace.ExportModelAsset(json).Ok && !new AgentWorkspace().CreateSession(json).Ok);
        }
        Reject(n => n["nodes"]![1]!["gas"]!["premixed"]!.AsObject().Remove("initial_fractions"), "invalid_argument");
        Reject(n => n["nodes"]![1]!["gas"]!["premixed"]!["initial_fractions"]!["fuel"] = 1.1, "model_range");
        Reject(n => n["nodes"]![1]!["gas"]!["premixed"]!["initial_fractions"]!["oxygen"] = .21, "invalid_argument");
        Reject(n => n["nodes"]![1]!["gas"]!["premixed"]!["lower_heating_value"]!["unit"] = "j", "model_unit");
        Reject(n => n["components"]![1]!.AsObject().Remove("reservoir_fractions"), "model_schema");
        Reject(n => n["components"]![2]!["reservoir_fractions"]!["fuel"] = 1.1, "model_range");
        Reject(n => n["components"]![5]!["parameters"]!["shape_exponent"] = 0, "model_range");
        Reject(n => n["components"]![5]!["parameters"]!.AsObject().Remove("burn_coefficient"), "invalid_argument");
        Reject(n => n["components"]![5]!["parameters"]!["duration_angle"]!["unit"] = "k", "model_unit");
        Reject(n => n["components"]![5]!["node_b"] = 3, "model_connection");
        Reject(n => n["components"]![5]!.AsObject().Remove("initial_input"), "invalid_argument");
        Reject(n => { n["experiment"]!["events"]![0]!["values"]![0]!["channel"] = 103; n["experiment"]!["events"]![0]!["values"]![0]!["value"] = 1.01; }, "invalid_argument");
    }
    private static void Agent()
    {
        var example = Data(AgentWorkspace.ExampleModel("fired-cylinder"));
        Require(JsonNode.DeepEquals(JsonNode.Parse(Source), JsonNode.Parse(example.GetRawText())));
        Require(Data(AgentWorkspace.Capabilities()).GetProperty("combustion").GetProperty("model").GetString() == "prescribed_wiebe");
        var workspace = new AgentWorkspace(); var created = Data(workspace.CreateSession(example)); string id = created.GetProperty("session_id").GetString()!;
        string initial = Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.SetInputs(id, "0", [new("102", -20), new("103", 1.01)]).Error is { Code: "invalid_input", CurrentRevision: "0" });
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Require(workspace.StepSession(id, "0", "100000000", cancel.Token).Error is { Code: "cancelled", CurrentRevision: "0" });
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == initial);
        Data(workspace.StepSession(id, "0", "100000000"));
        var parent = Data(workspace.ReadSnapshot(id)).GetRawText();
        var child = Data(workspace.ForkSession(id, "1")); string childId = child.GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(childId, "0", [new("103", 0)])); Data(workspace.StepSession(childId, "1", "200000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == parent);
        Require(workspace.StepSession(id, "0", "200000000").Error?.Code == "revision_conflict");
        Data(workspace.StepSession(id, "1", "200000000"));
        double Released(string session) => Data(workspace.ReadSnapshot(session, [Channels.Output(15, Field.HeatReleased).ToString()]))
            .GetProperty("values")[0].GetProperty("value").GetDouble();
        Require(Released(id) > Released(childId)); Data(workspace.CloseSession(id, "2")); Data(workspace.CloseSession(childId, "2"));
        var narrow = JsonNode.Parse(Source)!; narrow["components"]![5]!["parameters"]!["duration_angle"] = JsonSerializer.SerializeToNode(new { value = .01, unit = "rad" });
        created = Data(workspace.CreateSession(JsonSerializer.SerializeToElement(narrow))); id = created.GetProperty("session_id").GetString()!;
        initial = Data(workspace.ReadSnapshot(id)).GetRawText();
        var failure = workspace.StepSession(id, "0", "50000");
        Require(failure.Error is { Code: "numerical_failure", CurrentRevision: "0" } && failure.Error.Message.Contains("burn duration/32"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == initial); Data(workspace.CloseSession(id, "0"));
        narrow["step_ns"] = 1000; created = Data(workspace.CreateSession(JsonSerializer.SerializeToElement(narrow))); id = created.GetProperty("session_id").GetString()!;
        Data(workspace.StepSession(id, "0", "50000")); Data(workspace.CloseSession(id, "1"));
        var failedKpi = JsonNode.Parse(Source)!; failedKpi["experiment"]!["checks"]![0]!["max"] = 11;
        Require(!Data(AgentWorkspace.RunExperiment(JsonSerializer.SerializeToElement(failedKpi))).GetProperty("passed").GetBoolean());
    }
}
