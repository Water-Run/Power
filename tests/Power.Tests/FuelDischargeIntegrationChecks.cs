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

internal static class FuelDischargeIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fuel discharge documents / choked receiver and pump solenoid fired cylinder / every portable boundary", Documents),
        ("fuel discharge agent / explicit phase density diagnostics revisions and independent admission branch", Sessions)
    ];
    private static JsonElement Data(AgentReply reply) { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static double V(SampleReport row, uint id, Field field) => row.Values[Channels.Output(id, field).ToString()];
    private static void Documents()
    {
        foreach (string name in new[] { "finite-volume-fuel-discharge", "pump-fed-finite-volume-cylinder" })
        {
            var document = ModelDocument.Load(Path.Combine(AppContext.BaseDirectory, name + ".power.json")); var report = ExperimentRunner.Evaluate(document);
            Require(report.Passed && report.Replay.SampleHashesMatch && report.Model.Fidelity == "finite_volume_fuel_discharge_powertrain", name + " KPIs or replay failed.");
            var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset(name))); var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
            foreach (var row in report.Samples)
            {
                while (playback.TimeNanoseconds < row.TimeNs) Require(playback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, row.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
                Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == row.StateHash && values.All(value => value.Value == row.Values[value.Channel.ToString()]));
                Near(V(row, 0, Field.EnergyResidual), 0, 4e-6); Near(V(row, 0, Field.MassResidual), 0, 1e-14); Near(V(row, 0, Field.FuelResidual), 0, 1e-14);
                if (name == "pump-fed-finite-volume-cylinder") Near(V(row, 2, Field.Volume) + V(row, 16, Field.Volume), V(row, 10, Field.Volume), 1e-18);
            }
            if (name == "pump-fed-finite-volume-cylinder")
            {
                Require(V(report.Samples[^1], 17, Field.TotalFuelDelivered) > 1e-6 && V(report.Samples[^1], 15, Field.FuelBurned) > 1e-7);
                Require(report.Samples.Any(row => V(row, 6, Field.Displacement) > .0001) && V(report.Samples[^1], 111, Field.TotalFuelDelivered) > 1e-5);
                Console.WriteLine($"Pump/needle/discharge fired cylinder: fuel admitted {V(report.Samples[^1], 17, Field.TotalFuelDelivered):R} kg; burned {V(report.Samples[^1], 15, Field.FuelBurned):R} kg.");
            }
        }
    }
    private static void Sessions()
    {
        var capability = Data(AgentWorkspace.Capabilities()); Require(capability.GetProperty("fuel_discharge").GetProperty("phase_mode").GetString() == "frozen");
        Require(capability.GetProperty("readable_asset_formats").GetArrayLength() == AssetCodec.FormatVersion);
        var example = Data(AgentWorkspace.ExampleModel("finite-volume-fuel-discharge")); var workspace = new AgentWorkspace();
        string parent = Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!, child = Data(workspace.ForkSession(parent, "0")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(child, "0", [new("100", 0)])); Data(workspace.StepSession(child, "1", "1000000"));
        Require(Data(workspace.ReadSnapshot(parent)).GetProperty("revision").GetString() == "0");
        Require(workspace.StepSession(child, "0", "1000000").Error?.Code == "revision_conflict");
        Data(workspace.StepSession(parent, "0", "1000000")); Require(Data(workspace.ReadSnapshot(parent)).GetProperty("state_hash").GetString() != Data(workspace.ReadSnapshot(child)).GetProperty("state_hash").GetString());
        Data(workspace.CloseSession(parent, "1")); Data(workspace.CloseSession(child, "2"));
        var bad = JsonNode.Parse(example.GetRawText())!; var nozzle = bad["components"]!.AsArray().Single(c => c!["kind"]!.GetValue<string>() == "fuel_discharge")!;
        nozzle["parameters"]!["phase_mode"] = "equilibrium"; Throws<ArgumentException>(() => ModelDocument.Parse(bad.ToJsonString()));
        foreach (JsonNode? mode in new JsonNode?[] { JsonValue.Create(1), JsonValue.Create(true), null })
        {
            nozzle["parameters"]!["phase_mode"] = mode;
            Throws<ArgumentException>(() => ModelDocument.Parse(bad.ToJsonString()));
            using var malformed = JsonDocument.Parse(bad.ToJsonString());
            Require(AgentWorkspace.ValidateModel(malformed.RootElement).Error?.Code == "invalid_argument");
            Require(AgentWorkspace.ExportModelAsset(malformed.RootElement).Error?.Code == "invalid_argument");
            Require(workspace.CreateSession(malformed.RootElement).Error?.Code == "invalid_argument");
        }
        nozzle["parameters"]!["phase_mode"] = "frozen"; nozzle["parameters"]!["wall_kinetic_fraction"]!["unit"] = "k";
        Require(AgentWorkspace.ValidateModel(JsonDocument.Parse(bad.ToJsonString()).RootElement).Error?.Code == "model_unit");
        nozzle["parameters"]!["wall_kinetic_fraction"]!["unit"] = "fraction"; nozzle["parameters"]!["implicit_oem_nozzle"] = true;
        Throws<ArgumentException>(() => ModelDocument.Parse(bad.ToJsonString()));
    }
}
