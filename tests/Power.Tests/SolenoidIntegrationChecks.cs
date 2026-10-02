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

internal static class SolenoidIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("needle fired cylinder / magnetic electrical and fuel ledgers / complete portable boundaries", Replay),
        ("needle documents / typed magnetic slope stroke ownership period and actionable errors", Contracts),
        ("needle sessions / controlled voltage guidance / sampled state / revision cancellation and independent fork", Sessions)
    ];
    private static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "needle-actuated-cylinder.power.json"));
    private static double V(SampleReport s, uint id, Field field) => s.Values[Channels.Output(id, field).ToString()];
    private static JsonElement Data(AgentReply reply) { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static void Replay()
    {
        var document = ModelDocument.Parse(Source); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 65 && report.Model.Fidelity == "needle_actuated_fired_powertrain");
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Needle actuation"))); var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
        double previousHeat = 0, previousDelivered = 0;
        foreach (var sample in report.Samples)
        {
            while (playback.TimeNanoseconds < sample.TimeNs) Require(playback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, sample.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == sample.StateHash && values.All(v => v.Value == sample.Values[v.Channel.ToString()]));
            double x = V(sample, 6, Field.Displacement), current = V(sample, 23, Field.Current), magnetic = V(sample, 23, Field.InternalEnergy);
            Near(magnetic, .5 * (.002 + 8 * x) * current * current, 1e-12);
            double heat = V(sample, 23, Field.CopperHeat), delivered = V(sample, 17, Field.TotalFuelDelivered);
            Require(heat >= previousHeat && delivered >= previousDelivered); previousHeat = heat; previousDelivered = delivered;
            Near(V(sample, 17, Field.Mass) + delivered, .0005, 1e-16);
            Near(V(sample, 16, Field.Mass) + V(sample, 16, Field.EvaporatedFuelMass), delivered, 1e-14);
            Near(V(sample, 17, Field.SourceWork), V(sample, 17, Field.HydraulicWork) + V(sample, 17, Field.FluidHeat), 1e-12);
            Near(V(sample, 0, Field.EnergyResidual), 0, 1e-6); Near(V(sample, 0, Field.MassResidual), 0, 1e-14); Near(V(sample, 0, Field.FuelResidual), 0, 1e-14);
        }
        Require(report.Model.Calibration == "unverified" && V(report.Samples[^1], 23, Field.CopperHeat) > 0);
    }
    private static void Contracts()
    {
        JsonNode Component(JsonNode n, string kind) => n["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == kind)!;
        void Reject(Action<JsonNode> mutate, string code)
        {
            var n = JsonNode.Parse(Source)!; mutate(n); var json = JsonSerializer.SerializeToElement(n);
            var reply = AgentWorkspace.ValidateModel(json); Require(reply.Error?.Code == code, $"Expected {code}: {reply.Error}");
            Require(!AgentWorkspace.ExportModelAsset(json).Ok);
        }
        Reject(n => Component(n, "solenoid")["parameters"]!["inductance_gradient"]!["unit"] = "h", "model_unit");
        Reject(n => Component(n, "solenoid")["parameters"]!.AsObject().Remove("reference_position"), "invalid_argument");
        Reject(n => Component(n, "liquid_fuel_injector")["parameters"]!["needle"]!["needle_node"] = 3, "model_connection");
        Reject(n => Component(n, "needle_driver")["parameters"]!["solenoid_component"] = 21, "model_connection");
        Reject(n => Component(n, "needle_driver")["parameters"]!["sample_period_ns"] = 99999, "model_range");
    }
    private static void Sessions()
    {
        var workspace = new AgentWorkspace(); var example = Data(AgentWorkspace.ExampleModel("needle-actuated-cylinder"));
        Require(JsonNode.DeepEquals(JsonNode.Parse(Source), JsonNode.Parse(example.GetRawText())));
        string id = Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!; string before = Data(workspace.ReadSnapshot(id)).GetRawText();
        var blocked = workspace.SetInputs(id, "0", [new("107", 6.8)]);
        Require(blocked.Error?.Code == "controlled_input" && blocked.Error.Message.Contains("104") && blocked.Error.Message.Contains("liquid_fuel_dose_per_cycle"));
        Require(workspace.StepSession(id, "0", "20000000", new CancellationToken(true)).Error?.Code == "cancelled" && Data(workspace.ReadSnapshot(id)).GetRawText() == before);
        Data(workspace.StepSession(id, "0", "20000000")); string parent = Data(workspace.ReadSnapshot(id)).GetRawText();
        string fork = Data(workspace.ForkSession(id, "1")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(fork, "0", [new("104", 0)])); Data(workspace.StepSession(fork, "1", "20000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == parent);
        Data(workspace.CloseSession(fork, "2")); Data(workspace.CloseSession(id, "1"));
    }
}
