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

internal static class FuelPhaseEquilibriumIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("equilibrium fuel cell / bidirectional phase evidence / every portable boundary", Cell),
        ("equilibrium injected engine / vapor-only burn supply margins and full ledgers", Engine),
        ("equilibrium fuel agent / strict properties units ownership revision and branches", Sessions)
    ];
    private static string Source(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, name + ".power.json"));
    private static double V(SampleReport row, uint id, Field field) => row.Values[Channels.Output(id, field).ToString()];
    private static JsonElement Data(AgentReply reply) { Require(reply.Ok, reply.Error?.Message ?? "Expected success."); return reply.Data!.Value; }
    private static ExperimentReport Check(string name)
    {
        var document = ModelDocument.Parse(Source(name)); var report = ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Model.Fidelity == "equilibrium_liquid_fuel_powertrain");
        var asset = AssetCodec.Decode(AssetCodec.Encode(document.ToAsset(name))); var playback = asset.CreatePlayback(); var values = new Scalar[asset.Model.OutputCount];
        foreach (var row in report.Samples)
        {
            while (playback.TimeNanoseconds < row.TimeNs) Require(playback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, row.TimeNs - playback.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16") == row.StateHash && values.All(v => v.Value == row.Values[v.Channel.ToString()]));
            Near(V(row, 0, Field.EnergyResidual), 0, 1e-6); Near(V(row, 0, Field.MassResidual), 0, 1e-13);
            Near(V(row, 0, Field.FuelResidual), 0, 1e-13); Near(V(row, 0, Field.FreshAirResidual), 0, 1e-13);
        }
        return report;
    }
    private static void Cell()
    {
        var report = Check("equilibrium-fuel-cell"); var final = report.Samples[^1];
        Require(V(final, 10, Field.EvaporatedFuelMass) > 0 && V(final, 10, Field.CondensedFuelMass) > 0);
        Require(report.Samples.Any(row => V(row, 10, Field.MassFlow) > 0) && report.Samples.Any(row => V(row, 10, Field.MassFlow) < 0));
        foreach (var row in report.Samples)
        {
            Near(.003 - V(row, 10, Field.Mass), V(row, 10, Field.EvaporatedFuelMass) - V(row, 10, Field.CondensedFuelMass), 1e-14);
            Near(V(row, 10, Field.Temperature), V(row, 2, Field.Temperature), 1e-8);
        }
    }
    private static void Engine()
    {
        var report = Check("equilibrium-injected-cylinder"); Require(V(report.Samples[^1], 15, Field.HeatReleased) > 300);
        foreach (var row in report.Samples)
        {
            Near(V(row, 16, Field.Mass) + V(row, 16, Field.EvaporatedFuelMass) - V(row, 16, Field.CondensedFuelMass), V(row, 17, Field.TotalFuelDelivered), 1e-14);
            Require(V(row, 17, Field.CavitationMargin) >= 0 && V(row, 1513, Field.CavitationMargin) >= 0);
            Near(V(row, 1513, Field.CavitationMargin), V(row, 1513, Field.Pressure) - V(row, 1513, Field.SaturationPressure), 1e-8);
            Near(V(row, 0, Field.HydraulicVolumeResidual), 0, 1e-14);
        }
    }
    private static void Sessions()
    {
        var workspace = new AgentWorkspace(); var example = Data(AgentWorkspace.ExampleModel("equilibrium-fuel-cell"));
        var contract = Data(AgentWorkspace.Capabilities()).GetProperty("fuel_film").GetProperty("phase_equilibrium");
        Require(contract.GetProperty("required_parameters").EnumerateArray().Any(item => item.GetString() == "reference_pressure"));
        string id = Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!;
        string fork = Data(workspace.ForkSession(id, "0")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(fork, "0", [new("960", 1)]));
        Require(!workspace.StepSession(fork, "0", "10000000").Ok);
        Data(workspace.StepSession(fork, "1", "10000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetProperty("revision").GetString() == "0");
        Data(workspace.StepSession(id, "0", "10000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetProperty("state_hash").GetString() != Data(workspace.ReadSnapshot(fork)).GetProperty("state_hash").GetString());
        Data(workspace.CloseSession(fork, "2")); Data(workspace.CloseSession(id, "1"));
        var bad = workspace.CreateSession(JsonDocument.Parse(example.GetRawText().Replace("\"reference_pressure\":", "\"reference_pressure_typo\":")).RootElement);
        Require(!bad.Ok && bad.Error?.Code == "invalid_argument");
        var document = ModelDocument.Parse(Source("equilibrium-fuel-cell")); var components = document.Model.Components.ToArray(); int index = Array.FindIndex(components, c => c.Kind == ComponentKind.FuelFilm);
        components[index] = components[index] with { FuelFilm = components[index].FuelFilm! with { PhaseEquilibrium = components[index].FuelFilm!.PhaseEquilibrium! with { ReferencePressure = new(30000, Unit.Kelvin) } } };
        Require(!CompiledModel.TryCompile(document.Model with { Components = components }, out _, out var error) && error!.Code == DiagnosticCode.Unit && error.Field == "fuel_film.phase_equilibrium.reference_pressure");
    }
}
