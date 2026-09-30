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

internal static class PumpAssemblyIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fired pump losses / complete hydraulic and thermal ledger / every portable boundary", Fired),
        ("electric pump / isolated electrical power / valve-dependent clutch / every portable boundary", Electric),
        ("pump assembly agent / discovery / strict units / revisions / branches / failed batch", Contracts)
    ];
    private static string Source(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, name + ".power.json"));
    private static double Value(SampleReport sample, uint id, Field field) => sample.Values[Channels.Output(id, field).ToString()];
    private static JsonElement Data(AgentReply reply)
    { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static ExperimentReport Replay(string name, int boundaries)
    {
        var document = ModelDocument.Parse(Source(name)); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == boundaries);
        Require(report.Model.Calibration == "unverified" && report.Model.Fidelity == "shaft_driven_hydraulics");
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset(name)));
        Require(asset.Nodes.SequenceEqual(document.Model.Nodes.OrderBy(n => n.Id)) &&
            asset.Components.SequenceEqual(document.Model.Components.OrderBy(c => c.Id)), "Portable composition must retain descriptors in canonical ID order.");
        var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
        foreach (var boundary in report.Samples)
        {
            while (playback.TimeNanoseconds < boundary.TimeNs)
                Require(playback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, boundary.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == boundary.StateHash);
            Require(values.All(v => v.Value == boundary.Values[v.Channel.ToString()]));
            Near(Value(boundary, 0, Field.EnergyResidual), 0, 1e-6);
            Near(Value(boundary, 0, Field.HydraulicVolumeResidual), 0, 1e-16);
            Near(Value(boundary, 0, Field.HydraulicWork), 0, 0);
        }
        return report;
    }
    private static void Fired()
    {
        var report = Replay("fired-pump-losses", 89);
        foreach (var boundary in report.Samples)
        {
            double restrictionHeat = Value(boundary, 47, Field.FluidHeat) + Value(boundary, 48, Field.FluidHeat), storage = 0;
            for (uint id = 40; id <= 45; ++id) restrictionHeat += Value(boundary, id, Field.FluidHeat);
            for (uint id = 30; id <= 33; ++id) storage += Value(boundary, id, Field.InternalEnergy);
            Near(Value(boundary, 46, Field.HydraulicWork), storage - 3 + restrictionHeat, 1e-8);
            double pumpLosses = 100 * (Value(boundary, 9, Field.Temperature) - 300);
            Require(pumpLosses >= Value(boundary, 48, Field.FluidHeat) - 1e-8);
        }
        var original = ExperimentRunner.Evaluate(ModelDocument.Parse(Source("fired-pump")));
        var definition = ModelDocument.Parse(Source("fired-pump-losses"));
        var zeroLoss = ExperimentRunner.Evaluate(definition with
        {
            Model = definition.Model with { Components = definition.Model.Components.Select(c => c.Id == 48
                ? c with { HydraulicRestriction = c.HydraulicRestriction! with { Coefficient = new(0, Unit.CubicMeterPerSecondPascal) } }
                : c.Id == 49 ? c with { Damping = new(0, Unit.NewtonMeterSecondPerRadian) } : c).ToArray() }, Checks = []
        });
        Require(zeroLoss.Passed && zeroLoss.Samples.Length == original.Samples.Length);
        for (int i = 0; i < original.Samples.Length; ++i)
            foreach (var channel in original.Samples[i].Values)
                Near(zeroLoss.Samples[i].Values[channel.Key], channel.Value, 1e-8 + 1e-10 * Math.Abs(channel.Value));
        Require(Math.Abs(Value(report.Samples[^1], 1, Field.Speed) - Value(original.Samples[^1], 1, Field.Speed)) > .1);
    }
    private static void Electric()
    {
        var report = Replay("electric-pump", 106); double loadStart = 0;
        foreach (var boundary in report.Samples)
        {
            if (boundary.TimeNs == 350_100_000) loadStart = Value(boundary, 5, Field.Angle);
            double loadWork = boundary.TimeNs >= 350_100_000 ? -2 * (Value(boundary, 5, Field.Angle) - loadStart) : 0;
            double electricalWork = Value(boundary, 0, Field.SourceWork) - 10 * Value(boundary, 4, Field.Angle) - loadWork;
            double speed = Value(boundary, 1, Field.Speed), current = Value(boundary, 10, Field.Current);
            double motorAndDragHeat = 100 * (Value(boundary, 3, Field.Temperature) - 300)
                - Value(boundary, 12, Field.FluidHeat) - Value(boundary, 14, Field.FluidHeat);
            Near(electricalWork, .5 * .02 * speed * speed + .5 * .01 * current * current
                + Value(boundary, 11, Field.HydraulicWork) + motorAndDragHeat, 1e-7);
            Near(Value(boundary, 11, Field.HydraulicWork), Value(boundary, 2, Field.InternalEnergy) + Value(boundary, 6, Field.InternalEnergy)
                + Value(boundary, 12, Field.FluidHeat) + Value(boundary, 14, Field.FluidHeat)
                + Value(boundary, 15, Field.FluidHeat) + Value(boundary, 16, Field.FluidHeat), 1e-8);
        }
        Require(Value(report.Samples.Single(s => s.TimeNs == 800_000_000), 17, Field.ClutchMode) == 0);
        var document = ModelDocument.Parse(Source("electric-pump"));
        var sealedValves = ExperimentRunner.Evaluate(document with
        {
            Events = document.Events.Select(e => e with { Values = e.Values.Select(v => v.Channel is 101 or 102 ? v with { Value = 0 } : v).ToArray() }).ToArray(),
            Checks = [new(17, Field.FrictionHeat, 0, 0, null), new(17, Field.ClampForce, 0, 0, null)]
        });
        Require(sealedValves.Passed && Value(sealedValves.Samples[^1], 11, Field.HydraulicWork) > 0);
    }
    private static void Contracts()
    {
        var capabilities = Data(AgentWorkspace.Capabilities());
        Require(capabilities.GetProperty("pump_assembly").GetProperty("leakage_unit").GetString() == "m3_s_pa");
        foreach (string name in new[] { "fired-pump-losses", "electric-pump" })
        {
            var example = Data(AgentWorkspace.ExampleModel(name));
            Require(JsonNode.DeepEquals(JsonNode.Parse(Source(name)), JsonNode.Parse(example.GetRawText())));
            var malformed = JsonNode.Parse(Source(name))!;
            malformed["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == "shaft")!["parameters"]!["damping"]!["unit"] = "ohm";
            var invalid = JsonSerializer.SerializeToElement(malformed);
            Require(AgentWorkspace.ValidateModel(invalid).Error?.Code == "model_unit" && !AgentWorkspace.ExportModelAsset(invalid).Ok);
            var workspace = new AgentWorkspace(); string id = Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!;
            string before = Data(workspace.ReadSnapshot(id)).GetRawText();
            Require(workspace.StepSession(id, "1", "10000000").Error?.Code == "revision_conflict");
            Require(workspace.StepSession(id, "0", "10000000", new CancellationToken(true)).Error?.Code == "cancelled");
            Require(Data(workspace.ReadSnapshot(id)).GetRawText() == before);
            string fork = Data(workspace.ForkSession(id, "0")).GetProperty("session_id").GetString()!;
            Data(workspace.StepSession(fork, "0", "10000000")); Require(Data(workspace.ReadSnapshot(id)).GetRawText() == before);
            Data(workspace.CloseSession(fork, "1")); Data(workspace.CloseSession(id, "0"));
        }
        var electric = Data(AgentWorkspace.ExampleModel("electric-pump")); var w = new AgentWorkspace();
        string session = Data(w.CreateSession(electric)).GetProperty("session_id").GetString()!;
        Data(w.SetInputs(session, "0", [new("100", double.MaxValue)])); string baseline = Data(w.ReadSnapshot(session)).GetRawText();
        Require(w.StepSession(session, "1", "10000000").Error?.Code == "numerical_failure");
        Require(Data(w.ReadSnapshot(session)).GetRawText() == baseline); Data(w.CloseSession(session, "1"));
    }
}
