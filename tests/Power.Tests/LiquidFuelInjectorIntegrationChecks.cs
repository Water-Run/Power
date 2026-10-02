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

internal static class LiquidFuelInjectorIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("liquid injected cylinder / finite pressure energy and vapor-only burn / every boundary", Replay),
        ("liquid documents / explicit rail properties / density and compliance / film ownership", Contracts),
        ("liquid sessions / quota and pressure histories / revision cancellation and independent fork", Sessions)
    ];
    private static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "liquid-injected-cylinder.power.json"));
    private static double V(SampleReport sample, uint id, Field field) => sample.Values[Channels.Output(id, field).ToString()];
    private static JsonElement Data(AgentReply reply) { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static void Replay()
    {
        var document = ModelDocument.Parse(Source); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked == 65);
        Require(report.Model.Fidelity == "liquid_injected_fired_powertrain" && report.Model.Calibration == "unverified");
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Liquid injection")));
        var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
        double initialSourceEnergy = V(report.Samples[0], 17, Field.InternalEnergy);
        double supplyEnergy = 2000 * 300 + (287 / .35 - 2000) * 400 - 300000;
        foreach (var sample in report.Samples)
        {
            while (playback.TimeNanoseconds < sample.TimeNs)
                Require(playback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, sample.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == sample.StateHash && values.All(v => v.Value == sample.Values[v.Channel.ToString()]));
            double delivered = V(sample, 17, Field.TotalFuelDelivered), vapor = V(sample, 16, Field.EvaporatedFuelMass);
            Near(V(sample, 17, Field.Mass) + delivered, .0005, 1e-16);
            Near(V(sample, 16, Field.Mass) + vapor, delivered, 1e-15);
            Near(initialSourceEnergy - V(sample, 17, Field.InternalEnergy), supplyEnergy * delivered + V(sample, 17, Field.SourceWork), 1e-8);
            Near(supplyEnergy * delivered + V(sample, 16, Field.FilmWallHeat) - vapor * (287 / .35) * 400, V(sample, 16, Field.InternalEnergy), 1e-8);
            Near(V(sample, 17, Field.SourceWork), V(sample, 17, Field.HydraulicWork) + V(sample, 17, Field.FluidHeat), 1e-12);
            Near(10 * (V(sample, 5, Field.Temperature) - 500), V(sample, 17, Field.FluidHeat) - V(sample, 16, Field.FilmWallHeat), 1e-8);
            Near(.0005, V(sample, 17, Field.Mass) + V(sample, 16, Field.Mass) + V(sample, 2, Field.FuelMass) + V(sample, 15, Field.FuelBurned) - V(sample, 0, Field.FuelEnergyIn) / 44e6, 1e-14);
            if (vapor == 0) Near(V(sample, 15, Field.FuelBurned), 0, 0);
            Near(V(sample, 0, Field.EnergyResidual), 0, 1e-6); Near(V(sample, 0, Field.MassResidual), 0, 1e-14);
            Near(V(sample, 0, Field.FuelResidual), 0, 1e-14); Near(V(sample, 0, Field.FreshAirResidual), 0, 1e-14);
        }
        Require(V(report.Samples[^1], 17, Field.TotalFuelDelivered) > 8e-6 && V(report.Samples[^1], 17, Field.Pressure) < 800_000);
    }
    private static void Contracts()
    {
        JsonNode Injector(JsonNode node) => node["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == "liquid_fuel_injector")!;
        void Reject(Action<JsonNode> mutation, string code)
        {
            var node = JsonNode.Parse(Source)!; mutation(node); var element = JsonSerializer.SerializeToElement(node);
            var reply = AgentWorkspace.ValidateModel(element); Require(reply.Error?.Code == code, $"Expected {code}: {reply.Error}");
            Require(!AgentWorkspace.ExportModelAsset(element).Ok);
        }
        Reject(n => Injector(n)["parameters"]!.AsObject().Remove("pressure_compliance"), "invalid_argument");
        Reject(n => Injector(n)["parameters"]!["liquid_density"]!["unit"] = "kg", "model_unit");
        Reject(n => Injector(n)["parameters"]!["supply_temperature"]!["value"] = 500, "model_range");
        Reject(n => Injector(n)["parameters"]!["film_component"] = 13, "model_connection");
        Reject(n => Injector(n)["parameters"]!["pressure_compliance"]!["value"] = 1e-6, "model_range");
        Reject(n => Injector(n)["node_b"] = 2, "invalid_argument");
    }
    private static void Sessions()
    {
        var example = Data(AgentWorkspace.ExampleModel("liquid-injected-cylinder"));
        Require(JsonNode.DeepEquals(JsonNode.Parse(Source), JsonNode.Parse(example.GetRawText())));
        var workspace = new AgentWorkspace(); string id = Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!;
        string before = Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.StepSession(id, "1", "20000000").Error?.Code == "revision_conflict");
        Require(workspace.StepSession(id, "0", "20000000", new CancellationToken(true)).Error?.Code == "cancelled");
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == before);
        Data(workspace.StepSession(id, "0", "20000000")); string parent = Data(workspace.ReadSnapshot(id)).GetRawText();
        string fork = Data(workspace.ForkSession(id, "1")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(fork, "0", [new("104", 0)])); Data(workspace.StepSession(fork, "1", "20000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText() == parent);
        Data(workspace.CloseSession(fork, "2")); Data(workspace.CloseSession(id, "1"));
    }
}
