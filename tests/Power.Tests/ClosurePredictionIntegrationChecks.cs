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

internal static class ClosurePredictionIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("closure fired cylinder / predicted tail and actual flow / complete portable boundaries", Replay),
        ("closure documents / horizon dimensions tick alignment and finite budget", Contracts),
        ("closure sessions / prediction cutoff and scheduled voltage / revisions cancellation and forks", Sessions)
    ];
    private static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "closure-compensated-cylinder.power.json"));
    private static JsonElement Data(AgentReply reply) { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static double V(SampleReport sample, uint id, Field field) => sample.Values[Channels.Output(id, field).ToString()];
    private static void Replay()
    {
        var document = ModelDocument.Parse(Source); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 65 && report.Model.Fidelity == "closure_compensated_fired_powertrain");
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Closure feedback"))); var replay = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
        foreach (var sample in report.Samples)
        {
            while (replay.TimeNanoseconds < sample.TimeNs) Require(replay.Advance(Math.Min(257 * asset.Model.StepNanoseconds, sample.TimeNs - replay.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(replay.ReadSnapshot(values).StateHash.ToString("x16") == sample.StateHash && values.All(v => v.Value == sample.Values[v.Channel.ToString()]));
            Require(V(sample, 24, Field.PredictedFuelMass) >= 0 && V(sample, 24, Field.PredictionTicks) <= 2000 && V(sample, 24, Field.ClosingDelayTicks) <= 10);
            Near(V(sample, 17, Field.Mass) + V(sample, 17, Field.TotalFuelDelivered), .0005, 1e-16);
            Near(V(sample, 16, Field.Mass) + V(sample, 16, Field.EvaporatedFuelMass), V(sample, 17, Field.TotalFuelDelivered), 1e-14);
            Near(V(sample, 0, Field.EnergyResidual), 0, 1e-6); Near(V(sample, 0, Field.FuelResidual), 0, 1e-14);
        }
    }
    private static void Contracts()
    {
        void Reject(ulong horizon)
        {
            var n = JsonNode.Parse(Source)!;
            var driver = n["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == "needle_driver")!;
            driver["parameters"]!["closure_prediction_ns"] = horizon;
            Require(AgentWorkspace.ValidateModel(JsonSerializer.SerializeToElement(n)).Error?.Code == "model_range");
        }
        Reject(99_999); Reject(40_970_000); Reject(100_000);
        Require(JsonNode.DeepEquals(JsonNode.Parse(Source), JsonNode.Parse(Data(AgentWorkspace.ExampleModel("closure-compensated-cylinder")).GetRawText())));
    }
    private static void Sessions()
    {
        var workspace = new AgentWorkspace(); var example = Data(AgentWorkspace.ExampleModel("closure-compensated-cylinder"));
        string id = Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!;
        string before = Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.SetInputs(id, "0", [new("107", 6.8)]).Error?.Code == "controlled_input");
        Require(workspace.StepSession(id, "0", "20000000", new CancellationToken(true)).Error?.Code == "cancelled" && Data(workspace.ReadSnapshot(id)).GetRawText() == before);
        Data(workspace.StepSession(id, "0", "20000000")); string parent = Data(workspace.ReadSnapshot(id)).GetRawText();
        string fork = Data(workspace.ForkSession(id, "1")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(fork, "0", [new("104", 4e-6)])); Data(workspace.StepSession(fork, "1", "20000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == parent);
        Data(workspace.CloseSession(fork, "2")); Data(workspace.CloseSession(id, "1"));
    }
}
