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

internal static class ValveTimingIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("valve timing JSON / variable-speed motoring / portable replay / all boundaries", Replay),
        ("valve timing agent / strict contract / peak input / numerical recovery / KPI distinction", Agent)
    ];
    private static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "crank-timed-cylinder.power.json"));
    private static JsonElement Data(AgentReply reply)
    {
        Require(reply.Ok, reply.Error?.Message ?? "Expected success");
        return reply.Data!.Value;
    }

    private static void Replay()
    {
        var document = ModelDocument.Parse(Source); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 63);
        Require(report.Model.Fidelity == "crank_timed_gas_exchange" && report.Model.Calibration == "unverified");
        var original = document.ToAsset("Crank-timed replay");
        Require(original.Inputs.All(i => i.Channel == 102), "Valve timing must follow angle, not scheduled openings");
        var decoded = AssetCodec.Decode(AssetCodec.Encode(original));
        Require(decoded.Components.SequenceEqual(original.Components));
        var playback = decoded.CreatePlayback(); var values = new Scalar[decoded.Model.OutputCount];
        foreach (var boundary in report.Samples)
        {
            while (playback.TimeNanoseconds < boundary.TimeNs)
                Require(playback.Advance(Math.Min(1_350_000, boundary.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == boundary.StateHash);
            Require(values.All(v => v.Value == boundary.Values[v.Channel.ToString()]));
            double angle = boundary.Values[Channels.Output(1, Field.Angle).ToString()] * 180 / Math.PI;
            foreach (var (id, opening, duration) in new[] { (11U, 0.0, 210.0), (12U, 520.0, 200.0) })
            {
                double phase = angle - opening; phase -= 720 * Math.Floor(phase / 720);
                double expected = phase < duration ? Math.Pow(Math.Sin(Math.PI * phase / duration), 2) : 0;
                Near(boundary.Values[Channels.Output(id, Field.Opening).ToString()], expected, 1e-13);
            }
        }
        double[] Trace(uint id, Field field) => report.Samples.Select(s => s.Values[Channels.Output(id, field).ToString()]).ToArray();
        Require(Trace(1, Field.Speed).Max() - Trace(1, Field.Speed).Min() > 5, "Motor must change speed substantially");
        Require(Trace(11, Field.MassFlow).Any(v => v < 0) && Trace(12, Field.MassFlow).Any(v => v > 0), "Cycle must admit and expel gas");
        Require(Trace(1, Field.Angle).Last() > 8 * Math.PI, "Cycle must repeat");
    }

    private static void Agent()
    {
        var example = Data(AgentWorkspace.ExampleModel("crank-timed-cylinder"));
        Require(JsonNode.DeepEquals(JsonNode.Parse(Source), JsonNode.Parse(example.GetRawText())));
        var capabilities = Data(AgentWorkspace.Capabilities());
        Require(capabilities.GetProperty("valve_timing").GetProperty("cycle_degrees").EnumerateArray().Select(x => x.GetInt32()).SequenceEqual([360, 720]));
        void Reject(Action<JsonNode> mutate, string code)
        {
            var document = JsonNode.Parse(Source)!; mutate(document);
            var json = JsonSerializer.SerializeToElement(document);
            Require(AgentWorkspace.ValidateModel(json).Error?.Code == code, $"Expected {code}");
            Require(!AgentWorkspace.ExportModelAsset(json).Ok && !new AgentWorkspace().CreateSession(json).Ok);
        }
        Reject(n => n["components"]![1]!["valve_timing"]!.AsObject().Remove("crank_node"), "invalid_argument");
        Reject(n => n["components"]![1]!["valve_timing"]!["profile"] = "unknown", "invalid_argument");
        Reject(n => n["components"]![1]!["valve_timing"]!["crank_node"] = 2, "model_connection");
        Reject(n => n["components"]![1]!["valve_timing"]!["cycle_angle"]!["value"] = 180, "model_range");
        Reject(n => n["components"]![1]!["valve_timing"]!["duration_angle"]!["unit"] = "k", "model_unit");
        Reject(n => n["components"]![1]!["valve_timing"]!["duration_angle"]!["value"] = 721, "model_range");
        Reject(n => n["components"]![0]!["valve_timing"] = n["components"]![1]!["valve_timing"]!.DeepClone(), "model_schema");
        var w = new AgentWorkspace(); var created = Data(w.CreateSession(example)); string id = created.GetProperty("session_id").GetString()!;
        var channels = created.GetProperty("model").GetProperty("channels").EnumerateArray().ToArray();
        Require(channels.Count(c => c.GetProperty("quantity").GetString() == "peak_opening") == 2);
        Require(channels.Count(c => c.GetProperty("quantity").GetString() == "effective_opening" && c.GetProperty("unit").GetString() == "Fraction") == 2);
        string start = Data(w.ReadSnapshot(id)).GetRawText();
        Require(w.SetInputs(id, "0", [new("100", .5), new("101", 1.01)]).Error is { Code: "invalid_input", CurrentRevision: "0" });
        Require(Data(w.ReadSnapshot(id)).GetRawText() == start);
        Data(w.SetInputs(id, "0", [new("100", 0), new("101", 0)]));
        var result = Data(w.StepSession(id, "1", "10000000"));
        var selected = Data(w.ReadSnapshot(id, [Channels.Output(11, Field.Opening).ToString(), Channels.Output(12, Field.Opening).ToString()]));
        Require(selected.GetProperty("values").EnumerateArray().All(v => v.GetProperty("value").GetDouble() == 0));
        Require(w.StepSession(id, "1", "10000000").Error?.Code == "revision_conflict");
        Data(w.CloseSession(id, "2"));

        var narrow = JsonNode.Parse(Source)!;
        narrow["components"]![1]!["valve_timing"]!["duration_angle"] = JsonSerializer.SerializeToNode(new { value = .01, unit = "rad" });
        var narrowJson = JsonSerializer.SerializeToElement(narrow);
        Data(AgentWorkspace.ValidateModel(narrowJson)); created = Data(w.CreateSession(narrowJson)); id = created.GetProperty("session_id").GetString()!;
        start = Data(w.ReadSnapshot(id)).GetRawText();
        var failure = w.StepSession(id, "0", "50000");
        Require(failure.Error is { Code: "numerical_failure", CurrentRevision: "0" } && failure.Error.Message.Contains("duration_angle/8"));
        Require(Data(w.ReadSnapshot(id)).GetRawText() == start); Data(w.CloseSession(id, "0"));
        narrow["step_ns"] = 10000;
        created = Data(w.CreateSession(JsonSerializer.SerializeToElement(narrow))); id = created.GetProperty("session_id").GetString()!;
        Data(w.StepSession(id, "0", "50000")); Data(w.CloseSession(id, "1"));
        var kpiFailure = JsonNode.Parse(Source)!; kpiFailure["experiment"]!["checks"]![0]!["max"] = 11;
        Require(!Data(AgentWorkspace.RunExperiment(JsonSerializer.SerializeToElement(kpiFailure))).GetProperty("passed").GetBoolean());
    }
}
