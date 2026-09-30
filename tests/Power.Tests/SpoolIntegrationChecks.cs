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

internal static class SpoolIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("spool laboratory / mechanical regulation / every boundary / independent fluid and motion energy", Replay),
        ("spool document / strict metering geometry / units / typed piston reference", Contracts),
        ("spool sessions / actual position / revisions / cancellation / independent forks", Sessions)
    ];
    private static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "spool-regulated-pump.power.json"));
    private static double Value(SampleReport sample, uint id, Field field) => sample.Values[Channels.Output(id, field).ToString()];
    private static JsonElement Data(AgentReply reply) { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static void Replay()
    {
        var document = ModelDocument.Parse(Source); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 156);
        Require(report.Model.Fidelity == "mechanically_regulated_hydraulics" && report.Model.Calibration == "unverified");
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Mechanical regulator"))); var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
        foreach (var sample in report.Samples)
        {
            while (playback.TimeNanoseconds < sample.TimeNs) Require(playback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, sample.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == sample.StateHash && values.All(v => v.Value == sample.Values[v.Channel.ToString()]));
            double x = Value(sample, 40, Field.Displacement), speed = Value(sample, 40, Field.LinearSpeed);
            Near(Value(sample, 14, Field.Opening), Math.Max(0, Math.Min(1, x / .002)), 1e-14);
            double accounted = Value(sample, 2, Field.InternalEnergy) + Value(sample, 6, Field.InternalEnergy) + .01 * speed * speed
                + 1e5 * Math.Pow(x + .001, 2) + Value(sample, 21, Field.InternalEnergy) + Value(sample, 22, Field.FrictionHeat);
            foreach (uint id in new uint[] { 12, 14, 15, 16 }) accounted += Value(sample, id, Field.FluidHeat);
            Near(Value(sample, 11, Field.HydraulicWork), accounted, 1e-8);
            Near(Value(sample, 0, Field.EnergyResidual), 0, 1e-6); Near(Value(sample, 0, Field.HydraulicVolumeResidual), 0, 1e-16);
        }
        Require(report.Samples.Any(s => Value(s, 2, Field.Pressure) > 0 && Value(s, 40, Field.Displacement) < 0 && Value(s, 14, Field.Opening) == 0));
        Require(report.Samples.Any(s => Value(s, 14, Field.Opening) > 0));
        Near(Value(report.Samples[^1], 21, Field.Force), 2e5 * (Value(report.Samples[^1], 40, Field.Displacement) + .001), .1);
    }
    private static void Contracts()
    {
        JsonNode Valve(JsonNode n) => n["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == "hydraulic_spool_valve")!;
        void Reject(Action<JsonNode> mutation, string code)
        {
            var n = JsonNode.Parse(Source)!; mutation(n); var value = JsonSerializer.SerializeToElement(n);
            Require(AgentWorkspace.ValidateModel(value).Error?.Code == code); Require(!AgentWorkspace.ExportModelAsset(value).Ok);
        }
        Reject(n => Valve(n)["parameters"]!.AsObject().Remove("full_open_position"), "invalid_argument");
        Reject(n => Valve(n)["parameters"]!["full_open_position"]!["unit"] = "rad", "model_unit");
        Reject(n => Valve(n)["parameters"]!["coefficient"]!["unit"] = "m3_s_pa", "model_unit");
        Reject(n => Valve(n)["parameters"]!["piston_component"] = 11, "model_connection");
        Reject(n => Valve(n)["parameters"]!["full_open_position"]!["value"] = 0, "model_range");
        Reject(n => Valve(n)["parameters"]!["full_open_position"]!["value"] = .01, "model_range");
        Reject(n => Valve(n)["input_channel"] = 109, "invalid_argument");
    }
    private static void Sessions()
    {
        var example = Data(AgentWorkspace.ExampleModel("spool-regulated-pump")); Require(JsonNode.DeepEquals(JsonNode.Parse(Source), JsonNode.Parse(example.GetRawText())));
        var workspace = new AgentWorkspace(); string id = Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!;
        string before = Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.StepSession(id, "1", "10000000").Error?.Code == "revision_conflict");
        Require(workspace.StepSession(id, "0", "10000000", new CancellationToken(true)).Error?.Code == "cancelled"); Require(Data(workspace.ReadSnapshot(id)).GetRawText() == before);
        Data(workspace.StepSession(id, "0", "20000000")); string baseline = Data(workspace.ReadSnapshot(id)).GetRawText();
        string fork = Data(workspace.ForkSession(id, "1")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(fork, "0", [new("100", 6)])); Data(workspace.StepSession(fork, "1", "20000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == baseline); Data(workspace.CloseSession(fork, "2")); Data(workspace.CloseSession(id, "1"));
    }
}
