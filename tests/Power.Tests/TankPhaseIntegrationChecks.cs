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

internal static class TankPhaseIntegrationChecks
{
    internal static IEnumerable<(string Name,Action Run)> All =>
    [
        ("equilibrium tank supply / reversal vented chemistry and every portable boundary",Supply),
        ("equilibrium tank engine / finite liquid displacement pressure work and full fuel ledger",Engine),
        ("equilibrium tank agent / strict flag material errors revisions and independent vent branches",Sessions)
    ];
    private static string Source(string name)=>File.ReadAllText(Path.Combine(AppContext.BaseDirectory,name+".power.json"));
    private static double V(SampleReport row,uint id,Field field)=>row.Values[Channels.Output(id,field).ToString()];
    private static JsonElement Data(AgentReply reply){Require(reply.Ok,reply.Error?.Message??"Expected success.");return reply.Data!.Value;}
    private static ExperimentReport Check(string name,uint tank,uint gas,uint feed,uint? returned)
    {
        var doc=ModelDocument.Parse(Source(name));var report=ExperimentRunner.Evaluate(doc);
        Require(report.Passed&&report.Replay.SampleHashesMatch&&report.Model.Fidelity=="equilibrium_tank_liquid_fuel_powertrain");
        var asset=AssetCodec.Decode(AssetCodec.Encode(doc.ToAsset(name)));var playback=asset.CreatePlayback();var values=new Scalar[asset.Model.OutputCount];
        var definition=doc.Model.Components.Single(c=>c.Id==tank).LiquidFuelTank!;
        foreach(var row in report.Samples)
        {
            while(playback.TimeNanoseconds<row.TimeNs)Require(playback.Advance(Math.Min(257*asset.Model.StepNanoseconds,row.TimeNs-playback.TimeNanoseconds))==SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16")==row.StateHash&&values.All(v=>v.Value==row.Values[v.Channel.ToString()]));
            double phase=V(row,tank,Field.EvaporatedFuelMass)-V(row,tank,Field.CondensedFuelMass);
            Near(definition.InitialMass.Value-V(row,tank,Field.Mass),V(row,feed,Field.TotalFuelDelivered)-(returned is null?0:V(row,returned.Value,Field.TotalFuelDelivered))+phase,1e-13);
            Near(V(row,tank,Field.Volume)+V(row,gas,Field.Volume),definition.Headspace!.Capacity.Value,1e-20);
            Near(V(row,tank,Field.Temperature),V(row,gas,Field.Temperature),1e-8);
            if(V(row,tank,Field.Mass)>0)Near(V(row,tank,Field.VaporPartialPressure),V(row,tank,Field.SaturationPressure),1e-5);
            Near(V(row,0,Field.EnergyResidual),0,1e-6);Near(V(row,0,Field.MassResidual),0,1e-13);Near(V(row,0,Field.FuelResidual),0,1e-13);Near(V(row,0,Field.HydraulicVolumeResidual),0,1e-14);
        }
        return report;
    }
    private static void Supply()
    {
        var report=Check("equilibrium-tank-supply",22,40,21,null);
        Require(report.Samples.Any(row=>V(row,22,Field.MassFlow)>0)&&report.Samples.Any(row=>V(row,22,Field.MassFlow)<0));
        Require(report.Samples.Any(row=>V(row,4,Field.Speed)>0)&&report.Samples.Any(row=>V(row,4,Field.Speed)<0));
        Require(V(report.Samples[^1],0,Field.FuelEnergyIn)<0,"Fuel-bearing source vent lost its chemical boundary.");
    }
    private static void Engine()
    {
        var report=Check("equilibrium-tank-fired-cylinder",1513,1520,1511,1515);
        Require(V(report.Samples[^1],15,Field.HeatReleased)>300&&V(report.Samples[^1],1520,Field.FuelMass)>0);
    }
    private static void Sessions()
    {
        var workspace=new AgentWorkspace();var example=Data(AgentWorkspace.ExampleModel("equilibrium-tank-supply"));
        Require(Data(AgentWorkspace.Capabilities()).GetProperty("liquid_fuel_tank").GetProperty("headspace").GetProperty("phase_equilibrium").GetProperty("optional_boolean").GetString()=="phase_equilibrium");
        string id=Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!;
        string fork=Data(workspace.ForkSession(id,"0")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(fork,"0",[new("110",1)]));Data(workspace.StepSession(fork,"1","10000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetProperty("revision").GetString()=="0");
        Data(workspace.StepSession(id,"0","10000000"));Require(Data(workspace.ReadSnapshot(id)).GetProperty("state_hash").GetString()!=Data(workspace.ReadSnapshot(fork)).GetProperty("state_hash").GetString());
        Data(workspace.CloseSession(fork,"2"));Data(workspace.CloseSession(id,"1"));
        var invalid=JsonNode.Parse(example.GetRawText())!;
        var tank=invalid["components"]!.AsArray().Single(c=>c!["kind"]!.GetValue<string>()=="liquid_fuel_tank")!;
        tank["parameters"]!["headspace"]!["phase_equilibrium"]="true";
        var bad=workspace.CreateSession(JsonDocument.Parse(invalid.ToJsonString()).RootElement);Require(!bad.Ok&&bad.Error?.Code=="invalid_argument");
        var doc=ModelDocument.Parse(Source("equilibrium-tank-supply"));var nodes=doc.Model.Nodes.Select(n=>n.Id==40?n with{Gas=n.Gas! with{Gamma=1.35}}:n).ToArray();
        Require(!CompiledModel.TryCompile(doc.Model with{Nodes=nodes},out _,out var error)&&error!.Field=="liquid_fuel_tank.headspace.phase_equilibrium");
    }
}
