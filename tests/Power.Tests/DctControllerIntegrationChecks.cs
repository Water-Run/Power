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

internal static class DctControllerIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("controlled DCT / sensor-confirmed paths and all histories / complete portable replay", Replay),
        ("controlled fired DCT / combined plant beyond prior state bound / complete evidence", Fired),
        ("DCT control agent / integral target ownership errors / revisions cancellation independent forks", Sessions)
    ];
    private static string Source(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, name + ".power.json"));
    private static JsonElement Data(AgentReply reply) { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static double V(SampleReport s, uint id, Field field) => s.Values[Channels.Output(id, field).ToString()];
    private static ExperimentReport Check(string name)
    {
        var document = ModelDocument.Parse(Source(name)); var report = ExperimentRunner.Evaluate(document); Require(report.Passed && report.Replay.SampleHashesMatch && report.Model.Fidelity == "sampled_dual_clutch_control");
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset(name))); var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
        foreach (var s in report.Samples)
        {
            while (playback.TimeNanoseconds < s.TimeNs) Require(playback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, s.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == s.StateHash && values.All(v => v.Value == s.Values[v.Channel.ToString()]));
            Near(V(s, 0, Field.EnergyResidual), 0, 1e-6); Require(V(s, 800, Field.ControlFault) == 0);
            int odd = (int)V(s, 800, Field.SelectedOddGear), even = (int)V(s, 800, Field.SelectedEvenGear);
            Require(odd == 0 || odd > 0 && odd % 2 == 1); Require(even == 0 || even == -1 || even > 0 && even % 2 == 0);
        }
        return report;
    }
    private static void Replay()
    {
        var report = Check("controlled-dual-clutch"); Require(report.Model.StateCount == 64);
        foreach (var stage in new[] { (1, 300_000_000UL), (2, 700_000_000UL), (3, 1_100_000_000UL), (4, 1_500_000_000UL),
            (5, 1_900_000_000UL), (6, 2_300_000_000UL), (7, 2_700_000_000UL) })
        { var s = report.Samples.Single(s => s.TimeNs == stage.Item2); Near(V(s, 800, Field.ActualGear), stage.Item1, 0); Near(V(s, 1, Field.Speed), DualClutchChecks.Assembly().EffectiveReduction(stage.Item1) * V(s, 4, Field.Speed), 1e-7); }
    }
    private static void Fired()
    {
        var report = Check("controlled-fired-dual-clutch"); Require(report.Model.StateCount == 70 && report.Model.StateCount <= CompiledModel.MaxStates);
        Near(V(report.Samples[^1], 800, Field.ActualGear), 3, 0); Require(V(report.Samples[^1], 15, Field.HeatReleased) > 300);
    }
    private static void Sessions()
    {
        var workspace = new AgentWorkspace(); var example = Data(AgentWorkspace.ExampleModel("controlled-dual-clutch"));
        string id = Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!; string before = Data(workspace.ReadSnapshot(id)).GetRawText();
        var owned = workspace.SetInputs(id, "0", [new("500", 1)]); Require(owned.Error?.Code == "controlled_input" && owned.Error.Message.Contains("700") && owned.Error.Message.Contains("requested_gear"));
        Require(workspace.SetInputs(id, "0", [new("700", 1.5)]).Error?.Code == "invalid_input");
        Require(workspace.StepSession(id, "0", "20000000", new CancellationToken(true)).Error?.Code == "cancelled" && Data(workspace.ReadSnapshot(id)).GetRawText() == before);
        Data(workspace.StepSession(id, "0", "20000000")); string parent = Data(workspace.ReadSnapshot(id)).GetRawText();
        string fork = Data(workspace.ForkSession(id, "1")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(fork, "0", [new("700", 2)])); Data(workspace.StepSession(fork, "1", "20000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == parent); Data(workspace.CloseSession(fork, "2")); Data(workspace.CloseSession(id, "1"));
        var n = JsonNode.Parse(Source("controlled-dual-clutch"))!; n["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == "dct_controller")!["parameters"]!["sample_period_ns"] = 99999;
        Require(AgentWorkspace.ValidateModel(JsonSerializer.SerializeToElement(n)).Error?.Code == "model_range");
    }
}
