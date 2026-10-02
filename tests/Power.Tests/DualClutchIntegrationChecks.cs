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

internal static class DualClutchIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("DCT shared graph / launch all forward handoffs and preselection / complete portable replay", Replay),
        ("fired DCT / actual engine and transmission work / timestep refinement / all boundaries", Fired),
        ("DCT agent / strict graph contracts / selector commands revisions cancellation and forks", Sessions)
    ];
    private static string Source(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, name + ".power.json"));
    private static double V(SampleReport s, uint id, Field field) => s.Values[Channels.Output(id, field).ToString()];
    private static JsonElement Data(AgentReply reply) { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static ExperimentReport Check(string name, int boundaries)
    {
        var document = ModelDocument.Parse(Source(name)); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == boundaries);
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset(name))); var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
        foreach (var s in report.Samples)
        {
            while (playback.TimeNanoseconds < s.TimeNs) Require(playback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, s.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == s.StateHash && values.All(v => v.Value == s.Values[v.Channel.ToString()]));
            Near(V(s, 0, Field.EnergyResidual), 0, 1e-6);
            foreach (uint gear in Enumerable.Range(200, 8).Select(i => (uint)i).Concat(new uint[] { 402, 403, 404, 405 })) Near(V(s, gear, Field.ConstraintError), 0, 1e-8);
        }
        return report;
    }
    private static void Replay()
    {
        var parsed = ModelDocument.Parse(Source("dual-clutch-transmission"));
        var graph = DualClutchChecks.Assembly().CreateGraph(DualClutchChecks.Ports(), DualClutchChecks.Ids(), selectedOdd: 1, selectedEven: 2);
        var expected = new ModelDefinition { StepNanoseconds = 100_000, Nodes = [NodeDefinition.Rotor(1, .2, 20 * Math.PI), NodeDefinition.Rotor(4, 10), NodeDefinition.Thermal(3, 1000, 300), ..graph.Nodes],
            Components = [..graph.Components, ComponentDefinition.Torque(10, 1, 100, 20), ComponentDefinition.Torque(11, 4, 101, -6)] };
        Require(CompiledModel.Compile(expected).Fingerprint == CompiledModel.Compile(parsed.Model).Fingerprint);
        var report = Check("dual-clutch-transmission", 281);
        foreach (var stage in new[] { (1, 200_000_000UL), (2, 400_000_000UL), (3, 600_000_000UL), (4, 800_000_000UL), (5, 1_000_000_000UL),
            (6, 1_200_000_000UL), (7, 1_400_000_000UL), (6, 1_600_000_000UL), (1, 2_600_000_000UL) })
        {
            var sample = report.Samples.Single(s => s.TimeNs == stage.Item2);
            Near(V(sample, 1, Field.Speed), DualClutchChecks.Assembly().EffectiveReduction(stage.Item1) * V(sample, 4, Field.Speed), 1e-8);
        }
        foreach (var sample in report.Samples)
        {
            double heat = V(sample, 400, Field.FrictionHeat) + V(sample, 401, Field.FrictionHeat);
            for (uint id = 300; id < 308; ++id) heat += V(sample, id, Field.FrictionHeat);
            Near(1000 * (V(sample, 3, Field.Temperature) - 300), heat, 1e-7);
        }
        Require(V(report.Samples[^1], 302, Field.FrictionHeat) > 0 && V(report.Samples[^1], 306, Field.FrictionHeat) > 0);
    }
    private static void Fired()
    {
        var report = Check("fired-dual-clutch", 83); Require(report.Model.StateCount == 61 && report.Model.Calibration == "unverified");
        var document = ModelDocument.Parse(Source("fired-dual-clutch"));
        var fine = ExperimentRunner.Evaluate(document with { Model = document.Model with { StepNanoseconds = 25_000 } });
        var finest = ExperimentRunner.Evaluate(document with { Model = document.Model with { StepNanoseconds = 12_500 } });
        Require(fine.Passed && finest.Passed);
        double Error(ExperimentReport a, ExperimentReport b)
        {
            double error = 0;
            foreach (var item in new[] { (1U, Field.Speed, 100.0), (4U, Field.Speed, 10.0), (0U, Field.SourceWork, 1000.0),
                (400U, Field.FrictionHeat, 1000.0), (401U, Field.FrictionHeat, 1000.0), (302U, Field.FrictionHeat, 1000.0) })
                error = Math.Max(error, Math.Abs(V(a.Samples[^1], item.Item1, item.Item2) - V(b.Samples[^1], item.Item1, item.Item2)) / item.Item3);
            return error;
        }
        double coarseError = Error(report, finest), refinedError = Error(fine, finest);
        Require(refinedError < coarseError, $"Fired DCT refinement did not decrease error: {coarseError:R}, {refinedError:R}");
        Console.WriteLine($"Fired DCT refinement versus 12.5 us: 50 us {coarseError:R}, 25 us {refinedError:R}.");
    }
    private static void Sessions()
    {
        var workspace = new AgentWorkspace(); var example = Data(AgentWorkspace.ExampleModel("dual-clutch-transmission"));
        Require(JsonNode.DeepEquals(JsonNode.Parse(Source("dual-clutch-transmission")), JsonNode.Parse(example.GetRawText())));
        string id = Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!; string before = Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.SetInputs(id, "0", [new("600", 2)]).Error?.Code == "invalid_input");
        Require(workspace.StepSession(id, "0", "20000000", new CancellationToken(true)).Error?.Code == "cancelled" && Data(workspace.ReadSnapshot(id)).GetRawText() == before);
        Data(workspace.StepSession(id, "0", "20000000")); string parent = Data(workspace.ReadSnapshot(id)).GetRawText();
        string fork = Data(workspace.ForkSession(id, "1")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(fork, "0", [new("600", 0), new("602", 1)])); Data(workspace.StepSession(fork, "1", "20000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == parent);
        Data(workspace.CloseSession(fork, "2")); Data(workspace.CloseSession(id, "1"));
        var bad = JsonNode.Parse(Source("dual-clutch-transmission"))!;
        bad["components"]!.AsArray().Single(c => c!["id"]!.GetValue<uint>() == 200)!["parameters"]!["ratio"] = 0;
        Require(AgentWorkspace.ValidateModel(JsonSerializer.SerializeToElement(bad)).Error?.Code == "model_range");
    }
}
