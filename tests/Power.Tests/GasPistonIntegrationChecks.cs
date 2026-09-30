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

internal static class GasPistonIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("gas accumulator pump / every boundary / adiabatic inventory / independent transfer energy / recovery", Replay),
        ("gas piston documents / volume owner / explicit absolute reference / units and direction", Contracts),
        ("gas accumulator sessions / complete gas-fluid history / revision / cancellation / forks", Sessions)
    ];
    private static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "gas-accumulator-pump.power.json"));
    private static double Value(SampleReport sample, uint id, Field field) => sample.Values[Channels.Output(id, field).ToString()];
    private static JsonElement Data(AgentReply reply) { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static void Replay()
    {
        var document = ModelDocument.Parse(Source); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 306);
        Require(report.Model.Fidelity == "gas_accumulator_powertrain" && report.Model.Calibration == "unverified");
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Gas accumulator"))); var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
        var initial = report.Samples[0]; double gasEnergy = Value(initial, 44, Field.InternalEnergy), gasMass = Value(initial, 44, Field.Mass), gasVolume = Value(initial, 24, Field.Volume), stopEnergy = Value(initial, 23, Field.InternalEnergy);
        foreach (var sample in report.Samples)
        {
            while (playback.TimeNanoseconds < sample.TimeNs) Require(playback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, sample.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == sample.StateHash && values.All(v => v.Value == sample.Values[v.Channel.ToString()]));
            double x = Value(sample, 42, Field.Displacement), volume = Value(sample, 24, Field.Volume), energy = Value(sample, 44, Field.InternalEnergy);
            Near(volume, 5e-5 - .001 * (x + .0001), 1e-18); Near(Value(sample, 44, Field.Mass), gasMass, 0);
            Near(energy * Math.Pow(volume / gasVolume, .4), gasEnergy, 1e-8);
            Near(Value(sample, 44, Field.Temperature), 300 * Math.Pow(gasVolume / volume, .4), 1e-7);
            Near(Value(sample, 24, Field.Force), -.001 * (Value(sample, 44, Field.Pressure) - 1e5), 1e-10);
            double spool = Value(sample, 40, Field.Displacement), speed = Value(sample, 40, Field.LinearSpeed), accumulatorSpeed = Value(sample, 42, Field.LinearSpeed);
            double accounted = Value(sample, 2, Field.InternalEnergy) + Value(sample, 6, Field.InternalEnergy) + .01 * speed * speed
                + 1e5 * Math.Pow(spool + .001, 2) + Value(sample, 21, Field.InternalEnergy) + Value(sample, 22, Field.FrictionHeat)
                + .025 * accumulatorSpeed * accumulatorSpeed + Value(sample, 23, Field.InternalEnergy) - stopEnergy
                + energy - gasEnergy + Value(sample, 25, Field.FrictionHeat) - Value(sample, 24, Field.SourceWork);
            foreach (uint id in new uint[] { 12, 14, 15, 16 }) accounted += Value(sample, id, Field.FluidHeat);
            Near(Value(sample, 11, Field.HydraulicWork), accounted, 1e-8); Near(Value(sample, 0, Field.EnergyResidual), 0, 1e-6);
            Near(Value(sample, 0, Field.HydraulicVolumeResidual), 0, 1e-16); Near(Value(sample, 0, Field.MassResidual), 0, 1e-18);
        }
        var charged = report.Samples.Single(s => s.TimeNs == 3_000_000_000); var released = report.Samples.Single(s => s.TimeNs == 4_000_000_000);
        double referenceWork = Value(released, 24, Field.SourceWork) - Value(charged, 24, Field.SourceWork);
        double delivery = Value(charged, 44, Field.InternalEnergy) - Value(released, 44, Field.InternalEnergy) + referenceWork
            - .025 * (Math.Pow(Value(released, 42, Field.LinearSpeed), 2) - Math.Pow(Value(charged, 42, Field.LinearSpeed), 2))
            - (Value(released, 23, Field.InternalEnergy) - Value(charged, 23, Field.InternalEnergy))
            - (Value(released, 25, Field.FrictionHeat) - Value(charged, 25, Field.FrictionHeat));
        Require(delivery > .1 && Value(released, 42, Field.Displacement) < Value(charged, 42, Field.Displacement));
        Require(Value(report.Samples[^1], 17, Field.ClutchMode) == 1);
    }
    private static void Contracts()
    {
        JsonNode Piston(JsonNode n) => n["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == "gas_piston")!;
        void Reject(Action<JsonNode> mutation, string code)
        {
            var n = JsonNode.Parse(Source)!; mutation(n); var value = JsonSerializer.SerializeToElement(n); var reply = AgentWorkspace.ValidateModel(value);
            Require(reply.Error?.Code == code, $"Expected {code}: {reply.Error}"); Require(!AgentWorkspace.ExportModelAsset(value).Ok);
        }
        Reject(n => Piston(n)["parameters"]!.AsObject().Remove("reference_pressure"), "invalid_argument");
        Reject(n => Piston(n)["parameters"]!["area"]!["unit"] = "m", "model_unit");
        Reject(n => Piston(n)["parameters"]!["reference_volume"]!["value"] = -1, "model_range");
        Reject(n => Piston(n)["parameters"]!["compression_direction"] = 0, "invalid_argument");
        Reject(n => Piston(n)["node_a"] = 2, "model_connection");
        Reject(n => Piston(n)["parameters"]!["reference_volume"]!["value"] = 1e-6, "model_range");
        Reject(n => Piston(n)["input_channel"] = 110, "invalid_argument");
        Reject(n => n["nodes"]!.AsArray().Single(v => v!["id"]!.GetValue<int>() == 44)!["storage"] = JsonSerializer.SerializeToNode(new { value = 1, unit = "m3" }), "model_schema");
    }
    private static void Sessions()
    {
        var example = Data(AgentWorkspace.ExampleModel("gas-accumulator-pump")); Require(JsonNode.DeepEquals(JsonNode.Parse(Source), JsonNode.Parse(example.GetRawText())));
        var workspace = new AgentWorkspace(); string id = Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!;
        string before = Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.StepSession(id, "1", "20000000").Error?.Code == "revision_conflict");
        Require(workspace.StepSession(id, "0", "20000000", new CancellationToken(true)).Error?.Code == "cancelled"); Require(Data(workspace.ReadSnapshot(id)).GetRawText() == before);
        Data(workspace.StepSession(id, "0", "20000000")); string parent = Data(workspace.ReadSnapshot(id)).GetRawText();
        string fork = Data(workspace.ForkSession(id, "1")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(fork, "0", [new("100", 6)])); Data(workspace.StepSession(fork, "1", "20000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == parent); Data(workspace.CloseSession(fork, "2")); Data(workspace.CloseSession(id, "1"));
    }
}
