// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Text.Json;
using Power.Agent;
using Power.Assets;
using Power.Core;
using Power.Experiments;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class TankHeadspaceIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("vented tank liquid engine / dynamic inlet gas liquid energy and every-boundary replay", Liquid),
        ("vented tank needle engine / actual delivery phase and independent gas inventory", Needle),
        ("tank headspace agent / ownership units strict geometry and vent branches", Sessions)
    ];
    private static string Source(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, name + ".power.json"));
    private static double V(SampleReport sample, uint id, Field field) => sample.Values[Channels.Output(id, field).ToString()];
    private static JsonElement Data(AgentReply reply) { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static ExperimentReport Check(string name)
    {
        var document = ModelDocument.Parse(Source(name)); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Model.Fidelity == "geometric_tank_liquid_fuel_powertrain");
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset(name))); var play = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
        uint injector = document.Model.Components.Single(c => c.Kind == ComponentKind.LiquidFuelInjector).Id;
        double initialRail = document.Model.Components.Single(c => c.Id == injector).LiquidFuelInjector!.InitialMass.Value;
        foreach (var row in report.Samples)
        {
            while (play.TimeNanoseconds < row.TimeNs) Require(play.Advance(Math.Min(257 * asset.Model.StepNanoseconds, row.TimeNs - play.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(play.ReadSnapshot(values).StateHash.ToString("x16") == row.StateHash && values.All(v => v.Value == row.Values[v.Channel.ToString()]));
            Near(V(row, 1513, Field.Mass) + V(row, 1511, Field.TotalFuelDelivered) - V(row, 1515, Field.TotalFuelDelivered), 2e-5, 1e-14);
            Near(V(row, injector, Field.Mass) + V(row, injector, Field.TotalFuelDelivered), initialRail + V(row, 1511, Field.TotalFuelDelivered) - V(row, 1515, Field.TotalFuelDelivered), 1e-13);
            Near(V(row, 1513, Field.Volume) + V(row, 1520, Field.Volume), 2e-7, 1e-20);
            Near(V(row, 1513, Field.Pressure), V(row, 1520, Field.Pressure), 0);
            Near(V(row, 0, Field.EnergyResidual), 0, 1e-6); Near(V(row, 0, Field.MassResidual), 0, 1e-13);
            Near(V(row, 0, Field.FuelResidual), 0, 1e-13); Near(V(row, 0, Field.HydraulicVolumeResidual), 0, 1e-14);
            Near(V(row, 0, Field.HydraulicWork), 0, 0);
        }
        Require(V(report.Samples[^1], 15, Field.HeatReleased) > 300 && report.Model.StateCount <= 128);
        Require(report.Samples.Max(row => V(row, 1520, Field.Pressure)) - report.Samples.Min(row => V(row, 1520, Field.Pressure)) > 100);
        return report;
    }
    private static void Liquid() => Check("vented-tank-liquid-cylinder");
    private static void Needle() => Check("vented-tank-needle-cylinder");
    private static void Sessions()
    {
        var workspace = new AgentWorkspace(); var example = Data(AgentWorkspace.ExampleModel("vented-tank-liquid-cylinder"));
        string id = Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!;
        string fork = Data(workspace.ForkSession(id, "0")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(fork, "0", [new("960", 0)])); Data(workspace.StepSession(fork, "1", "10000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetProperty("revision").GetString() == "0");
        Data(workspace.StepSession(id, "0", "10000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetProperty("state_hash").GetString() != Data(workspace.ReadSnapshot(fork)).GetProperty("state_hash").GetString());
        Data(workspace.CloseSession(fork, "2")); Data(workspace.CloseSession(id, "1"));
        var bad = workspace.CreateSession(JsonDocument.Parse(example.GetRawText().Replace("\"capacity\":", "\"capacity_typo\":")).RootElement);
        Require(!bad.Ok && bad.Error?.Code == "invalid_argument");
        var document = ModelDocument.Parse(Source("vented-tank-liquid-cylinder")); var components = document.Model.Components.ToArray();
        int tank = Array.FindIndex(components, c => c.Id == 1513);
        components[tank] = components[tank] with { LiquidFuelTank = components[tank].LiquidFuelTank! with { Headspace = components[tank].LiquidFuelTank!.Headspace! with { Capacity = new(2e-7, Unit.Pascal) } } };
        Require(!CompiledModel.TryCompile(document.Model with { Components = components }, out _, out var error) && error!.Code == DiagnosticCode.Unit);
    }
}
