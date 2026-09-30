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

internal static class PressureControllerIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("regulated pump / feedback tracking / sample phase / every portable boundary / power", Replay),
        ("pressure control documents / strict units and ownership / unreachable target KPI", Contracts),
        ("pressure control sessions / owned voltage / revisions / controller forks / atomic failure", Sessions)
    ];
    private static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "pressure-regulated-pump.power.json"));
    private static JsonElement Data(AgentReply reply)
    { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static double Value(SampleReport sample, uint id, Field field) => sample.Values[Channels.Output(id, field).ToString()];
    private static void Replay()
    {
        var document = ModelDocument.Parse(Source); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 757);
        Require(report.Model.Fidelity == "sampled_pressure_control" && report.Model.Calibration == "unverified");
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Regulated pump"))); var playback = asset.CreatePlayback();
        var values = new Scalar[asset.Model.OutputCount]; double loadStart = 0;
        foreach (var boundary in report.Samples)
        {
            while (playback.TimeNanoseconds < boundary.TimeNs)
                Require(playback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, boundary.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == boundary.StateHash &&
                values.All(v => v.Value == boundary.Values[v.Channel.ToString()]));
            Near(Value(boundary, 0, Field.EnergyResidual), 0, 1e-6); Near(Value(boundary, 0, Field.HydraulicVolumeResidual), 0, 1e-16);
            Near(Value(boundary, 0, Field.HydraulicWork), 0, 0);
            double voltage = Value(boundary, 20, Field.CommandVoltage); Require(voltage >= 0 && voltage <= 12);
            if (boundary.TimeNs > 0)
            {
                ulong sampledAt = (boundary.TimeNs - 1) / 5_000_000 * 5_000_000;
                double target = sampledAt >= 6_000_100_000 ? 2e5 : sampledAt >= 3_000_100_000 ? 3.5e5 : 3e5;
                Near(Value(boundary, 20, Field.PressureError), target - Value(boundary, 20, Field.SampledPressure), 1e-8);
            }
            if (boundary.TimeNs == 350_100_000) loadStart = Value(boundary, 5, Field.Angle);
            double loadWork = boundary.TimeNs >= 350_100_000 ? -2 * (Value(boundary, 5, Field.Angle) - loadStart) : 0;
            double electricalWork = Value(boundary, 0, Field.SourceWork) - 10 * Value(boundary, 4, Field.Angle) - loadWork;
            double speed = Value(boundary, 1, Field.Speed), current = Value(boundary, 10, Field.Current);
            double motorAndDragHeat = 100 * (Value(boundary, 3, Field.Temperature) - 300) - Value(boundary, 12, Field.FluidHeat) - Value(boundary, 14, Field.FluidHeat);
            Near(electricalWork, .5 * .02 * speed * speed + .5 * .01 * current * current + Value(boundary, 11, Field.HydraulicWork) + motorAndDragHeat, 1e-6);
        }
        Near(Value(report.Samples[^1], 2, Field.Pressure), 2e5, 5000);
        Require(Value(report.Samples.Single(s => s.TimeNs == 800_000_000), 17, Field.ClutchMode) == 0);
        var atChange = report.Samples.Single(s => s.TimeNs == 6_000_100_000);
        var beforeChange = report.Samples.Single(s => s.TimeNs == 6_000_000_000);
        Near(Value(atChange, 20, Field.SampledPressure), Value(beforeChange, 2, Field.Pressure), 0);
    }
    private static void Contracts()
    {
        JsonNode Controller(JsonNode root) => root["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == "pressure_controller")!;
        void Reject(Action<JsonNode> change, string code)
        {
            var node = JsonNode.Parse(Source)!; change(node); var value = JsonSerializer.SerializeToElement(node);
            var reply = AgentWorkspace.ValidateModel(value); Require(reply.Error?.Code == code, $"Expected {code}: {reply.Error}");
            Require(!AgentWorkspace.ExportModelAsset(value).Ok);
        }
        Reject(n => Controller(n)["parameters"]!.AsObject().Remove("target_channel"), "invalid_argument");
        Reject(n => Controller(n)["parameters"]!["proportional_gain"]!["unit"] = "v", "model_unit");
        Reject(n => Controller(n)["parameters"]!["integral_gain"]!["value"] = -1, "model_range");
        Reject(n => Controller(n)["parameters"]!["sample_period_ns"] = 5_000_001, "model_range");
        Reject(n => Controller(n)["parameters"]!["target_channel"] = 105, "model_connection");
        Reject(n => Controller(n)["node_a"] = 1, "model_connection");
        Reject(n => Controller(n)["initial_input"]!["value"] = -1, "model_range");
        Reject(n => { var duplicate = Controller(n).DeepClone(); duplicate["id"] = 21; duplicate["input_channel"] = 106; n["components"]!.AsArray().Add(duplicate); }, "model_channel");
        Reject(n => n["experiment"]!["events"]![0]!["values"]![0]!["channel"] = 100, "invalid_argument");
        var document = ModelDocument.Parse(Source);
        var unreachable = document with
        {
            Model = document.Model with { Components = document.Model.Components.Select(c => c.Kind == ComponentKind.PressureController
                ? c with { InitialInput = new(2e6, Unit.Pascal) } : c).ToArray() },
            DurationNanoseconds = 1_000_000_000, SampleEveryNanoseconds = 10_000_000, Events = [],
            Checks = [new(20, Field.PressureError, null, null, 5000)]
        };
        var report = ExperimentRunner.Evaluate(unreachable); Require(!report.Passed && report.Replay.SampleHashesMatch);
        Near(Value(report.Samples[^1], 20, Field.CommandVoltage), 12, 0);
        Near(Value(report.Samples[^1], 20, Field.IntegralVoltage), 0, 0);
    }
    private static void Sessions()
    {
        var example = Data(AgentWorkspace.ExampleModel("pressure-regulated-pump"));
        Require(JsonNode.DeepEquals(JsonNode.Parse(Source), JsonNode.Parse(example.GetRawText())));
        var capabilities = Data(AgentWorkspace.Capabilities()); Require(capabilities.GetProperty("pressure_controller").GetProperty("units").GetProperty("integral_gain").GetString() == "v_pa_s");
        var workspace = new AgentWorkspace(); var created = Data(workspace.CreateSession(example)); string id = created.GetProperty("session_id").GetString()!;
        string before = Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.SetInputs(id, "0", [new("100", 0)]).Error?.Code == "controlled_input");
        Require(workspace.SetInputs(id, "0", [new("105", -1)]).Error?.Code == "invalid_input");
        Require(workspace.StepSession(id, "1", "10000000").Error?.Code == "revision_conflict");
        Require(workspace.StepSession(id, "0", "10000000", new CancellationToken(true)).Error?.Code == "cancelled");
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == before);
        Data(workspace.StepSession(id, "0", "20000000")); string baseline = Data(workspace.ReadSnapshot(id)).GetRawText();
        string fork = Data(workspace.ForkSession(id, "1")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(fork, "0", [new("105", 1e5)])); Data(workspace.StepSession(fork, "1", "20000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == baseline); Data(workspace.CloseSession(fork, "2")); Data(workspace.CloseSession(id, "1"));
        var invalidScale = JsonNode.Parse(Source)!;
        var parameters = invalidScale["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == "pressure_controller")!["parameters"]!;
        parameters["proportional_gain"]!["value"] = 1; parameters["integral_gain"]!["value"] = 100;
        string scaled = Data(workspace.CreateSession(JsonSerializer.SerializeToElement(invalidScale))).GetProperty("session_id").GetString()!;
        Data(workspace.StepSession(scaled, "0", "20000000")); Data(workspace.SetInputs(scaled, "1", [new("105", double.MaxValue)]));
        string checkpoint = Data(workspace.ReadSnapshot(scaled)).GetRawText();
        Require(workspace.StepSession(scaled, "2", "10000000").Error?.Code == "numerical_failure");
        Require(Data(workspace.ReadSnapshot(scaled)).GetRawText() == checkpoint); Data(workspace.CloseSession(scaled, "2"));
    }
}
