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

internal static class FuelChamberIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fuel pump documents / inlet-starved and electric supply / every portable boundary", Documents),
        ("fuel pump agent / geometry units ownership revisions and independent inlet branch", Sessions)
    ];
    private static JsonElement Data(AgentReply reply) { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static double V(SampleReport row, uint id, Field field) => row.Values[Channels.Output(id, field).ToString()];
    private static void Documents()
    {
        foreach (string name in new[] { "equilibrium-fuel-pump", "electric-equilibrium-fuel-pump" })
        {
            var document = ModelDocument.Load(Path.Combine(AppContext.BaseDirectory, name + ".power.json")); var report = ExperimentRunner.Evaluate(document);
            Require(report.Passed && report.Replay.SampleHashesMatch && report.Model.Fidelity == "shaft_driven_equilibrium_fuel_pump");
            var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset(name))); var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
            foreach (var row in report.Samples)
            {
                while (playback.TimeNanoseconds < row.TimeNs) Require(playback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, row.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
                Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == row.StateHash && values.All(value => value.Value == row.Values[value.Channel.ToString()]));
                Near(V(row, 0, Field.EnergyResidual), 0, 3e-6); Near(V(row, 0, Field.MassResidual), 0, 1e-14); Near(V(row, 0, Field.FuelResidual), 0, 1e-14);
                Require(V(row, 10, Field.MassFlow) >= 0 && V(row, 11, Field.MassFlow) >= 0);
            }
            Require(V(report.Samples[^1], 11, Field.TotalFuelDelivered) > 0 && V(report.Samples[^1], 12, Field.HydraulicWork) > 0);
            if (name == "equilibrium-fuel-pump") Require(report.Samples.Any(row => V(row, 2, Field.VaporMass) > 0));
        }
    }
    private static void Sessions()
    {
        var capability = Data(AgentWorkspace.Capabilities()); Require(capability.GetProperty("fuel_chamber").GetProperty("component").GetString() == "fuel_chamber");
        var example = Data(AgentWorkspace.ExampleModel("equilibrium-fuel-pump")); var workspace = new AgentWorkspace();
        string parent = Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!, child = Data(workspace.ForkSession(parent, "0")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(child, "0", [new("100", 0)])); Data(workspace.StepSession(child, "1", "10000000"));
        Require(Data(workspace.ReadSnapshot(parent)).GetProperty("revision").GetString() == "0");
        Data(workspace.StepSession(parent, "0", "10000000")); Require(Data(workspace.ReadSnapshot(parent)).GetProperty("state_hash").GetString() != Data(workspace.ReadSnapshot(child)).GetProperty("state_hash").GetString());
        Data(workspace.CloseSession(parent, "1")); Data(workspace.CloseSession(child, "2"));
        var bad = JsonNode.Parse(example.GetRawText())!; var chamber = bad["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == "fuel_chamber")!;
        chamber["parameters"]!["swept_volume"]!["unit"] = "k"; Require(!AgentWorkspace.ValidateModel(JsonDocument.Parse(bad.ToJsonString()).RootElement).Ok);
        chamber["parameters"]!["swept_volume"]!["unit"] = "m3"; chamber["parameters"]!["implicit_oem_cam"] = true;
        Throws<ArgumentException>(() => ModelDocument.Parse(bad.ToJsonString()));
    }
}
