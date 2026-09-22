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

internal static class GearIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fired planetary JSON / reduction and direct shift / every portable boundary / heat", Replay),
        ("gear agent / strict topology / rank diagnostics / revisions / cancellation / KPI distinction", Contracts)
    ];
    private static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fired-planetary.power.json"));
    private static JsonElement Data(AgentReply reply)
    { Require(reply.Ok, reply.Error?.Message ?? "Expected success"); return reply.Data!.Value; }
    private static void Replay()
    {
        var document = ModelDocument.Parse(Source); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 84);
        Require(report.Model.Fidelity == "constrained_gear_powertrain" && report.Model.Calibration == "unverified");
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Fired planetary")));
        Require(asset.Components.Single(c => c.Id == 18).NodeC == 4 && asset.Components.Single(c => c.Id == 19).Ratio == 3);
        var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
        double previousHeat = 0; var directModes = new HashSet<double>(); var brakeModes = new HashSet<double>(); bool sawDirect = false;
        foreach (var boundary in report.Samples)
        {
            while (playback.TimeNanoseconds < boundary.TimeNs)
                Require(playback.Advance(Math.Min(1_350_000, boundary.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == boundary.StateHash);
            Require(values.All(v => v.Value == boundary.Values[v.Channel.ToString()]));
            double Get(uint id, Field field) => boundary.Values[Channels.Output(id, field).ToString()];
            double heat = Get(16, Field.FrictionHeat) + Get(17, Field.FrictionHeat); Require(heat >= previousHeat); previousHeat = heat;
            Near(Get(5, Field.Temperature), 300 + heat / 200, 2e-8);
            Near(Get(0, Field.EnergyResidual), 0, 1e-6);
            foreach (uint id in new uint[] { 18, 19 })
            { Near(Get(id, Field.SlipSpeed), 0, 1e-8); Near(Get(id, Field.ConstraintError), 0, 1e-8); }
            directModes.Add(Get(16, Field.ClutchMode)); brakeModes.Add(Get(17, Field.ClutchMode));
            if (boundary.TimeNs > 200_050_000 && boundary.TimeNs < 450_050_000 && Get(16, Field.ClutchMode) == 1)
            {
                Near(Get(1, Field.Speed), Get(4, Field.Speed), 1e-8);
                Near(Get(1, Field.Speed), 3 * Get(7, Field.Speed), 1e-8); sawDirect = true;
            }
        }
        Require(sawDirect && directModes.SetEquals(new double[] { 0, 1, 2 }) && brakeModes.SetEquals(new double[] { 0, 1, 2 }));
        var final = report.Samples.Last().Values;
        double End(uint id, Field field) => final[Channels.Output(id, field).ToString()];
        Near(End(1, Field.Speed), 10.5 * End(7, Field.Speed), 1e-8);
        Require(End(0, Field.SourceWork) < -1 && End(15, Field.HeatReleased) > 300);
        // Holding reduction throughout must remove the shift heat and change the load motion.
        var held = ExperimentRunner.Evaluate(document with { Events = document.Events.Select(e => e with
            { Values = e.Values.Where(v => v.Channel == 102).ToArray() }).Where(e => e.Values.Length > 0).ToArray(), Checks = [] });
        var noShift = held.Samples.Last().Values;
        Require(noShift[Channels.Output(16, Field.FrictionHeat).ToString()] == 0);
        Require(Math.Abs(noShift[Channels.Output(7, Field.Speed).ToString()] - End(7, Field.Speed)) > .1);
    }
    private static void Contracts()
    {
        void Reject(Action<JsonNode> mutate, string code)
        {
            var node = JsonNode.Parse(Source)!; mutate(node); var json = JsonSerializer.SerializeToElement(node);
            var reply = AgentWorkspace.ValidateModel(json); Require(reply.Error?.Code == code, reply.Error?.Message ?? "Expected rejection");
            Require(!AgentWorkspace.ExportModelAsset(json).Ok && !new AgentWorkspace().CreateSession(json).Ok);
        }
        Reject(n => n["components"]![8]!.AsObject().Remove("node_c"), "invalid_argument");
        Reject(n => n["components"]![8]!["node_c"] = 6, "model_connection");
        Reject(n => n["components"]![8]!["parameters"]!["ratio"] = 1, "model_range");
        Reject(n => n["components"]![9]!["parameters"]!["ratio"] = 0, "model_range");
        Reject(n => n["components"]![8]!["parameters"]!["efficiency"] = .95, "invalid_argument");
        Reject(n => n["components"]![9]!["node_c"] = 0, "invalid_argument");
        Reject(n => n["components"]![9]!["heat_node"] = 5, "invalid_argument");
        Reject(n => n["nodes"]![6]!["initial"]!["value"] = 10, "model_connection");
        Reject(n => { var gear = n["components"]![9]!.DeepClone(); gear["id"] = 20; n["components"]!.AsArray().Add(gear); }, "model_solver");
        var example = Data(AgentWorkspace.ExampleModel("fired-planetary"));
        Require(JsonNode.DeepEquals(JsonNode.Parse(Source), JsonNode.Parse(example.GetRawText())));
        var capabilities = Data(AgentWorkspace.Capabilities());
        Require(capabilities.GetProperty("gears").GetProperty("initial_speed_policy").GetString() == "require_compatible_no_impulse");
        Require(capabilities.GetProperty("asset_format").GetString() == "power.asset.v11" && capabilities.GetProperty("readable_asset_formats").GetArrayLength() == 11);
        var workspace = new AgentWorkspace(); var created = Data(workspace.CreateSession(example)); string id = created.GetProperty("session_id").GetString()!;
        string before = Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.SetInputs(id, "0", [new("104", 1), new("105", 1.01)]).Error is { Code: "invalid_input", CurrentRevision: "0" });
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Require(workspace.StepSession(id, "0", "100000000", cancel.Token).Error is { Code: "cancelled", CurrentRevision: "0" });
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == before);
        Data(workspace.SetInputs(id, "0", [new("105", 0), new("104", 1)])); Data(workspace.StepSession(id, "1", "100000000"));
        before = Data(workspace.ReadSnapshot(id)).GetRawText();
        string child = Data(workspace.ForkSession(id, "2")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(child, "0", [new("102", -2)])); Data(workspace.StepSession(child, "1", "100000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == before);
        Require(workspace.StepSession(id, "1", "50000").Error?.Code == "revision_conflict");
        Data(workspace.CloseSession(id, "2")); Data(workspace.CloseSession(child, "2"));
        var failed = JsonNode.Parse(Source)!;
        failed["experiment"]!["checks"] = JsonSerializer.SerializeToNode(new[] { new { object_id = 16, field = "friction_heat", max = 0 } });
        Require(!Data(AgentWorkspace.RunExperiment(JsonSerializer.SerializeToElement(failed))).GetProperty("passed").GetBoolean());
    }
}
