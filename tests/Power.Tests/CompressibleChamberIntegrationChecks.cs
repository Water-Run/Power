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
internal static class CompressibleChamberIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("compressible chamber documents / shaft phase work electric drive and every portable boundary", Documents),
        ("compressible chamber agent / typed geometry structured errors revisions and drive branches", Sessions)
    ];
    private static JsonElement Data(AgentReply r) { Require(r.Ok, r.Error?.Message ?? "Expected success."); return r.Data!.Value; }
    private static void Documents()
    {
        foreach (string name in new[] { "compressible-fuel-chamber-duct", "electric-compressible-fuel-duct" })
        {
            var document = ModelDocument.Load(Path.Combine(AppContext.BaseDirectory, name + ".power.json")); var report = ExperimentRunner.Evaluate(document);
            Require(report.Passed && report.Replay.SampleHashesMatch && report.Model.Fidelity == "shaft_driven_compressible_fuel");
            var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset(name))); var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
            foreach (var row in report.Samples)
            {
                while (playback.TimeNanoseconds < row.TimeNs) Require(playback.Advance(Math.Min(7 * asset.Model.StepNanoseconds, row.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
                Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == row.StateHash && values.All(v => v.Value == row.Values[v.Channel.ToString()]));
            }
        }
    }
    private static void Sessions()
    {
        var capabilities = Data(AgentWorkspace.Capabilities()); Require(capabilities.GetProperty("compressible_fuel_chamber").GetProperty("domain").GetString() == "compressible_fuel");
        var document = Data(AgentWorkspace.ExampleModel("electric-compressible-fuel-duct")); var workspace = new AgentWorkspace();
        string parent = Data(workspace.CreateSession(document)).GetProperty("session_id").GetString()!, child = Data(workspace.ForkSession(parent, "0")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(child, "0", [new("200", 0)])); Data(workspace.StepSession(child, "1", "10000"));
        Require(Data(workspace.ReadSnapshot(parent)).GetProperty("revision").GetString() == "0"); Require(workspace.StepSession(child, "0", "1000").Error?.Code == "revision_conflict");
        Data(workspace.StepSession(parent, "0", "10000")); Require(Data(workspace.ReadSnapshot(parent)).GetProperty("state_hash").GetString() != Data(workspace.ReadSnapshot(child)).GetProperty("state_hash").GetString());
        Data(workspace.CloseSession(parent, "1")); Data(workspace.CloseSession(child, "2"));
        var bad = JsonNode.Parse(document.GetRawText())!; var chamber = bad["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == "compressible_fuel_chamber")!;
        chamber["parameters"]!["swept_volume"]!["unit"] = "k"; Require(AgentWorkspace.ValidateModel(JsonDocument.Parse(bad.ToJsonString()).RootElement).Error?.Code == "model_unit");
        chamber["parameters"]!["swept_volume"]!["unit"] = "m3"; chamber["node_b"] = 4; Require(!AgentWorkspace.ValidateModel(JsonDocument.Parse(bad.ToJsonString()).RootElement).Ok);
    }
}
