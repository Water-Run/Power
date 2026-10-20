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

internal static class EquilibriumFuelNetworkIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fuel network documents / two-phase and electromechanical heat / every portable boundary", Documents),
        ("fuel network agent / units strict material errors revisions and independent branches", Sessions)
    ];
    private static JsonElement Data(AgentReply reply) { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static double Value(SampleReport row, uint id, Field field) => row.Values[Channels.Output(id, field).ToString()];
    private static void Documents()
    {
        foreach (string name in new[] { "equilibrium-fuel-network", "electrothermal-fuel-network" })
        {
            var document = ModelDocument.Load(Path.Combine(AppContext.BaseDirectory, name + ".power.json")); var report = ExperimentRunner.Evaluate(document);
            Require(report.Passed && report.Replay.SampleHashesMatch && report.Model.Fidelity == "homogeneous_equilibrium_fuel_network");
            var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset(name))); var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
            foreach (var row in report.Samples)
            {
                while (playback.TimeNanoseconds < row.TimeNs) Require(playback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, row.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
                Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == row.StateHash && values.All(value => value.Value == row.Values[value.Channel.ToString()]));
                Near(Value(row, 0, Field.EnergyResidual), 0, 2e-7); Near(Value(row, 0, Field.MassResidual), 0, 1e-14); Near(Value(row, 0, Field.FuelResidual), 0, 1e-14);
            }
            uint source = name == "equilibrium-fuel-network" ? 1U : 1002U;
            Require(Value(report.Samples[^1], source, Field.VaporMass) > 0);
        }
    }
    private static void Sessions()
    {
        var capability = Data(AgentWorkspace.Capabilities());
        Require(capability.GetProperty("equilibrium_fuel_network").GetProperty("domain").GetString() == "fuel" && capability.GetProperty("asset_format").GetString() == AssetCodec.FormatName);
        var example = Data(AgentWorkspace.ExampleModel("equilibrium-fuel-network")); var workspace = new AgentWorkspace();
        string parent = Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!;
        string child = Data(workspace.ForkSession(parent, "0")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(child, "0", [new("100", 0)])); Data(workspace.StepSession(child, "1", "10000000"));
        Require(Data(workspace.ReadSnapshot(parent)).GetProperty("revision").GetString() == "0");
        Data(workspace.StepSession(parent, "0", "10000000"));
        Require(Data(workspace.ReadSnapshot(parent)).GetProperty("state_hash").GetString() != Data(workspace.ReadSnapshot(child)).GetProperty("state_hash").GetString());
        var snapshot = Data(workspace.ReadSnapshot(parent)); var badInput = workspace.SetInputs(parent, "1", [new("100", -1)]);
        Require(!badInput.Ok && Data(workspace.ReadSnapshot(parent)).GetRawText() == snapshot.GetRawText());
        Data(workspace.CloseSession(parent, "1")); Data(workspace.CloseSession(child, "2"));
        var bad = JsonNode.Parse(example.GetRawText())!;
        bad["nodes"]![0]!["fuel"]!["compliance"]!["unit"] = "k";
        var validation = AgentWorkspace.ValidateModel(JsonDocument.Parse(bad.ToJsonString()).RootElement); Require(!validation.Ok);
        bad = JsonNode.Parse(example.GetRawText())!; bad["nodes"]![0]!["fuel"]!["implicit_oem_density"] = 750;
        Throws<ArgumentException>(() => ModelDocument.Parse(bad.ToJsonString()));
    }
}
