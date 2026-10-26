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

internal static class FuelPipeIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fuel pipe documents / finite thermal line and actual pump needle fired cylinder / every portable boundary", Documents),
        ("fuel pipe agent / no direct opening liquid-only units and independent inlet branch", Sessions)
    ];
    private static JsonElement Data(AgentReply reply) { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static double V(SampleReport sample, uint id, Field field) => sample.Values[Channels.Output(id, field).ToString()];
    private static void Documents()
    {
        foreach (string name in new[] { "inertial-fuel-line", "inertial-pump-fed-cylinder" })
        {
            var document = ModelDocument.Load(Path.Combine(AppContext.BaseDirectory, name + ".power.json")); var report = ExperimentRunner.Evaluate(document);
            Require(report.Passed && report.Replay.SampleHashesMatch && report.Model.Fidelity == "liquid_inertial_fuel_network");
            var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset(name))); var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
            foreach (var sample in report.Samples)
            {
                while (playback.TimeNanoseconds < sample.TimeNs) Require(playback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, sample.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
                Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == sample.StateHash && values.All(v => v.Value == sample.Values[v.Channel.ToString()]));
                Near(V(sample, 0, Field.EnergyResidual), 0, 4e-6); Near(V(sample, 0, Field.MassResidual), 0, 1e-14); Near(V(sample, 0, Field.FuelResidual), 0, 1e-14);
            }
            uint pipe = name == "inertial-fuel-line" ? 10u : 116u; var final = report.Samples[^1];
            Require(V(final, pipe, Field.FrictionHeat) > 0 && V(final, pipe, Field.EntropyProduced) > 0 && V(final, pipe, Field.ForwardVolume) > 0);
            if (name == "inertial-fuel-line") Require(V(final, pipe, Field.ReverseVolume) > 1e-9);
            else Require(V(final, 17, Field.TotalFuelDelivered) > 1e-6 && V(final, 15, Field.FuelBurned) > 1e-7 && V(final, 112, Field.HydraulicWork) > 0);
        }
    }
    private static void Sessions()
    {
        var capability = Data(AgentWorkspace.Capabilities()); Require(capability.GetProperty("fuel_inertial_pipe").GetProperty("component").GetString() == "fuel_inertial_pipe");
        var example = Data(AgentWorkspace.ExampleModel("inertial-fuel-line")); var workspace = new AgentWorkspace();
        string parent = Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!, child = Data(workspace.ForkSession(parent, "0")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(child, "0", [new("100", 1)])); Data(workspace.StepSession(child, "1", "10000000"));
        Require(Data(workspace.ReadSnapshot(parent)).GetProperty("revision").GetString() == "0"); Data(workspace.StepSession(parent, "0", "10000000"));
        Require(Data(workspace.ReadSnapshot(parent)).GetProperty("state_hash").GetString() != Data(workspace.ReadSnapshot(child)).GetProperty("state_hash").GetString());
        Require(workspace.StepSession(child, "0", "1000000").Error?.Code == "revision_conflict"); Data(workspace.CloseSession(parent, "1")); Data(workspace.CloseSession(child, "2"));
        var bad = JsonNode.Parse(example.GetRawText())!; var pipe = bad["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == "fuel_inertial_pipe")!;
        pipe["parameters"]!["linear_resistance"]!["unit"] = "k"; Require(AgentWorkspace.ValidateModel(JsonDocument.Parse(bad.ToJsonString()).RootElement).Error?.Code == "model_unit");
        pipe["parameters"]!["linear_resistance"]!["unit"] = "pa_s_m3"; pipe["initial_input"] = new JsonObject { ["value"] = 0, ["unit"] = "fraction" };
        Throws<ArgumentException>(() => ModelDocument.Parse(bad.ToJsonString())); pipe.AsObject().Remove("initial_input");
        foreach (JsonNode? invalid in new JsonNode?[] { JsonValue.Create(1), JsonValue.Create(true), null })
        { pipe["parameters"]!["fluid_heat_fraction"] = invalid; Require(!AgentWorkspace.ValidateModel(JsonDocument.Parse(bad.ToJsonString()).RootElement).Ok); }
    }
}
