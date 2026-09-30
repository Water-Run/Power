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

internal static class PistonIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("dynamic piston clutch / free fill and release / contact and energy ledgers / every boundary", Replay),
        ("piston documents / typed motion and chambers / stroke and friction units", Contracts),
        ("piston sessions / physical capacity / revisions / forks / cancellation", Sessions)
    ];
    private static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "piston-actuated-clutch.power.json"));
    private static double Value(SampleReport sample, uint id, Field field) => sample.Values[Channels.Output(id, field).ToString()];
    private static JsonElement Data(AgentReply reply) { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static void Replay()
    {
        var document = ModelDocument.Parse(Source); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 761);
        Require(report.Model.Fidelity == "dynamic_piston_powertrain" && report.Model.Calibration == "unverified");
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Piston clutch"))); var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
        double initialBattery = Value(report.Samples[0], 30, Field.InternalEnergy);
        foreach (var boundary in report.Samples)
        {
            while (playback.TimeNanoseconds < boundary.TimeNs)
                Require(playback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, boundary.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == boundary.StateHash && values.All(v => v.Value == boundary.Values[v.Channel.ToString()]));
            Near(Value(boundary, 0, Field.EnergyResidual), 0, 5e-6); Near(Value(boundary, 0, Field.HydraulicVolumeResidual), 0, 1e-16);
            double x = Value(boundary, 40, Field.Displacement), speed = Value(boundary, 40, Field.LinearSpeed), normal = 1e6 * Math.Max(0, x - .002);
            Near(Value(boundary, 17, Field.ClampForce), normal, 1e-10); Near(Value(boundary, 17, Field.StaticCapacity), .4 * 4 * .08 * normal, 1e-10);
            double contact = .5e6 * (Math.Pow(Math.Max(-x, 0), 2) + Math.Pow(Math.Max(x - .006, 0), 2) + Math.Pow(Math.Max(x - .002, 0), 2));
            Near(Value(boundary, 22, Field.InternalEnergy), contact, 1e-12);
            double springHeat = Value(boundary, 23, Field.FrictionHeat);
            Near(100 * (Value(boundary, 7, Field.Temperature) - 300), springHeat + Value(boundary, 17, Field.FrictionHeat) + Value(boundary, 15, Field.FluidHeat) + Value(boundary, 16, Field.FluidHeat), 5e-7);
            double hydraulic = Value(boundary, 2, Field.InternalEnergy) + Value(boundary, 6, Field.InternalEnergy);
            double fluidHeat = Value(boundary, 12, Field.FluidHeat) + Value(boundary, 14, Field.FluidHeat) + Value(boundary, 15, Field.FluidHeat) + Value(boundary, 16, Field.FluidHeat);
            Near(Value(boundary, 11, Field.HydraulicWork), hydraulic + .5 * .02 * speed * speed + .5 * 10000 * x * x + contact + springHeat + fluidHeat, 1e-9);
            double motorSpeed = Value(boundary, 1, Field.Speed), current = Value(boundary, 10, Field.Current);
            double motorHeat = 100 * (Value(boundary, 3, Field.Temperature) - 300) - Value(boundary, 12, Field.FluidHeat) - Value(boundary, 14, Field.FluidHeat);
            double batteryHeat = 100 * (Value(boundary, 31, Field.Temperature) + Value(boundary, 32, Field.Temperature) - 600);
            Near(initialBattery - Value(boundary, 30, Field.InternalEnergy), .5 * .02 * motorSpeed * motorSpeed + .5 * .01 * current * current + Value(boundary, 11, Field.HydraulicWork) + motorHeat + batteryHeat, 1e-7);
            Near(Value(boundary, 0, Field.SourceWork), .5 * .2 * Math.Pow(Value(boundary, 4, Field.Speed), 2) + .5 * .5 * Math.Pow(Value(boundary, 5, Field.Speed), 2) - 160 + Value(boundary, 17, Field.FrictionHeat), 2e-6);
        }
        var filling = report.Samples.Single(s => s.TimeNs == 220_000_000);
        Require(Value(filling, 6, Field.Pressure) > 0 && Value(filling, 40, Field.Displacement) < .002); Near(Value(filling, 17, Field.ClampForce), 0, 0);
        var released = report.Samples.Single(s => s.TimeNs == 800_000_000); Require(Value(released, 17, Field.ClutchMode) == 0);
        Require(Value(report.Samples[^1], 17, Field.ClutchMode) == 1);
    }
    private static void Contracts()
    {
        JsonNode Piston(JsonNode n) => n["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == "hydraulic_piston")!;
        JsonNode Clutch(JsonNode n) => n["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == "piston_clutch")!;
        void Reject(Action<JsonNode> mutate, string code)
        {
            var n = JsonNode.Parse(Source)!; mutate(n); var value = JsonSerializer.SerializeToElement(n); var reply = AgentWorkspace.ValidateModel(value);
            Require(reply.Error?.Code == code, $"Expected {code}: {reply.Error}"); Require(!AgentWorkspace.ExportModelAsset(value).Ok);
        }
        Reject(n => Piston(n)["parameters"]!.AsObject().Remove("back_pressure"), "invalid_argument");
        Reject(n => Piston(n)["parameters"]!["front_area"]!["unit"] = "m", "model_unit");
        Reject(n => Piston(n)["parameters"]!["contact_position"]!["value"] = .1, "model_range");
        Reject(n => Piston(n)["parameters"]!["back_node"] = 6, "invalid_argument");
        Reject(n => Clutch(n)["parameters"]!["piston_component"] = 11, "model_connection");
        Reject(n => Clutch(n)["parameters"]!["static_friction"] = .1, "model_range");
        Reject(n => Piston(n)["input_channel"] = 107, "invalid_argument");
        var copy = JsonNode.Parse(Source)!; copy["nodes"]!.AsArray().Single(c => c!["id"]!.GetValue<int>() == 40)!["storage"]!["unit"] = "kg_m2";
        Require(AgentWorkspace.ValidateModel(JsonSerializer.SerializeToElement(copy)).Error?.Code == "model_unit");
    }
    private static void Sessions()
    {
        var example = Data(AgentWorkspace.ExampleModel("piston-actuated-clutch")); Require(JsonNode.DeepEquals(JsonNode.Parse(Source), JsonNode.Parse(example.GetRawText())));
        var workspace = new AgentWorkspace(); string id = Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!;
        string before = Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.StepSession(id, "1", "10000000").Error?.Code == "revision_conflict");
        Require(workspace.StepSession(id, "0", "10000000", new CancellationToken(true)).Error?.Code == "cancelled"); Require(Data(workspace.ReadSnapshot(id)).GetRawText() == before);
        Data(workspace.StepSession(id, "0", "20000000")); string baseline = Data(workspace.ReadSnapshot(id)).GetRawText();
        string fork = Data(workspace.ForkSession(id, "1")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(fork, "0", [new("101", 1)])); Data(workspace.StepSession(fork, "1", "100000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == baseline); Data(workspace.CloseSession(fork, "2")); Data(workspace.CloseSession(id, "1"));
    }
}
