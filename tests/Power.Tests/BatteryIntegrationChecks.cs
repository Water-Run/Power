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

internal static class BatteryIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("battery regulated pump / sag disturbance / internal electrical energy / every portable boundary", Replay),
        ("battery documents / explicit units / source and duty ownership / strict bounds", Contracts),
        ("battery sessions / duty and load atomicity / controller forks / cancellation", Sessions)
    ];
    private static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "battery-regulated-pump.power.json"));
    private static double Value(SampleReport sample, uint id, Field field) => sample.Values[Channels.Output(id, field).ToString()];
    private static JsonElement Data(AgentReply reply) { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static void Replay()
    {
        var document = ModelDocument.Parse(Source); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 761);
        Require(report.Model.Fidelity == "battery_pressure_control" && report.Model.Calibration == "unverified");
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Battery regulated pump"))); var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
        double initial = Value(report.Samples[0], 30, Field.InternalEnergy), loadStart = 0;
        foreach (var boundary in report.Samples)
        {
            while (playback.TimeNanoseconds < boundary.TimeNs)
                Require(playback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, boundary.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == boundary.StateHash && values.All(v => v.Value == boundary.Values[v.Channel.ToString()]));
            Near(Value(boundary, 0, Field.EnergyResidual), 0, 1e-6); Near(Value(boundary, 0, Field.HydraulicWork), 0, 0);
            if (boundary.TimeNs == 350_100_000) loadStart = Value(boundary, 5, Field.Angle);
            double loadWork = boundary.TimeNs >= 350_100_000 ? -2 * (Value(boundary, 5, Field.Angle) - loadStart) : 0;
            Near(Value(boundary, 0, Field.SourceWork), 10 * Value(boundary, 4, Field.Angle) + loadWork, 1e-6);
            double speed = Value(boundary, 1, Field.Speed), current = Value(boundary, 10, Field.Current);
            double motorAndDragHeat = 100 * (Value(boundary, 3, Field.Temperature) - 300) - Value(boundary, 12, Field.FluidHeat) - Value(boundary, 14, Field.FluidHeat);
            double batteryAndAccessoryHeat = 100 * (Value(boundary, 31, Field.Temperature) + Value(boundary, 32, Field.Temperature) - 600);
            Near(initial - Value(boundary, 30, Field.InternalEnergy), .5 * .02 * speed * speed + .5 * .01 * current * current
                + Value(boundary, 11, Field.HydraulicWork) + motorAndDragHeat + batteryAndAccessoryHeat, 1e-7);
            double duty = Value(boundary, 20, Field.CommandDuty);
            Require(duty >= 0 && duty <= 1 && Value(boundary, 30, Field.StateOfCharge) >= 0 && Value(boundary, 30, Field.StateOfCharge) <= 1);
            Near(Value(boundary, 10, Field.TerminalVoltage), duty * Value(boundary, 30, Field.TerminalVoltage), 1e-12);
            Near(Value(boundary, 30, Field.BatteryCurrent), duty * current + Value(boundary, 21, Field.Current), 1e-12);
        }
        double before = Value(report.Samples.Single(s => s.TimeNs == 2_000_000_000), 30, Field.TerminalVoltage);
        double after = Value(report.Samples.Single(s => s.TimeNs == 2_000_100_000), 30, Field.TerminalVoltage);
        Require(after < before - .05, "Accessory load must cause observable supply sag.");
        Near(Value(report.Samples[^1], 2, Field.Pressure), 2e5, 5000);
        Require(Value(report.Samples[^1], 30, Field.StateOfCharge) < .8);
    }
    private static void Contracts()
    {
        JsonNode Battery(JsonNode node) => node["nodes"]!.AsArray().Single(n => n!["domain"]!.GetValue<string>() == "battery")!;
        JsonNode Controller(JsonNode node) => node["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == "pressure_duty_controller")!;
        void Reject(Action<JsonNode> mutate, string code)
        {
            var node = JsonNode.Parse(Source)!; mutate(node); var value = JsonSerializer.SerializeToElement(node);
            var reply = AgentWorkspace.ValidateModel(value); Require(reply.Error?.Code == code, $"Expected {code}: {reply.Error}"); Require(!AgentWorkspace.ExportModelAsset(value).Ok);
        }
        Reject(n => Battery(n)["battery"]!.AsObject().Remove("polarization_capacitance"), "invalid_argument");
        Reject(n => Battery(n)["storage"]!["unit"] = "a", "model_unit");
        Reject(n => Battery(n)["initial"]!["value"] = 1.1, "model_range");
        Reject(n => Battery(n)["battery"]!["series_resistance"]!["value"] = -1, "model_range");
        Reject(n => Battery(n)["battery"]!["heat_node"] = 1, "model_connection");
        Reject(n => Controller(n)["parameters"]!["maximum_duty"]!["value"] = 1.1, "model_range");
        Reject(n => Controller(n)["parameters"]!["integral_gain"]!["unit"] = "v_pa_s", "model_unit");
        Reject(n => n["components"]!.AsArray().Single(c => c!["id"]!.GetValue<int>() == 10)!["node_b"] = 2, "model_connection");
        var capabilities = Data(AgentWorkspace.Capabilities()); Require(capabilities.GetProperty("battery").GetProperty("capacity_units").GetArrayLength() == 2);
        var failed = ModelDocument.Parse(Source) with { Checks = [new(30, Field.StateOfCharge, .9, null, null)] };
        Require(!ExperimentRunner.Evaluate(failed).Passed);
    }
    private static void Sessions()
    {
        var document = Data(AgentWorkspace.ExampleModel("battery-regulated-pump")); Require(JsonNode.DeepEquals(JsonNode.Parse(Source), JsonNode.Parse(document.GetRawText())));
        var workspace = new AgentWorkspace(); string id = Data(workspace.CreateSession(document)).GetProperty("session_id").GetString()!;
        string before = Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.SetInputs(id, "0", [new("100", .5)]).Error?.Code == "controlled_input");
        Require(workspace.SetInputs(id, "0", [new("106", 1.1)]).Error?.Code == "invalid_input");
        Require(workspace.StepSession(id, "1", "10000000").Error?.Code == "revision_conflict");
        Require(workspace.StepSession(id, "0", "10000000", new CancellationToken(true)).Error?.Code == "cancelled");
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == before);
        Data(workspace.StepSession(id, "0", "20000000")); string baseline = Data(workspace.ReadSnapshot(id)).GetRawText();
        string fork = Data(workspace.ForkSession(id, "1")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(fork, "0", [new("106", 1)])); Data(workspace.StepSession(fork, "1", "100000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == baseline); Data(workspace.CloseSession(fork, "2")); Data(workspace.CloseSession(id, "1"));
    }
}
