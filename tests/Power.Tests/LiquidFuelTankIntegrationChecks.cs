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

internal static class LiquidFuelTankIntegrationChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>
    [
        ("finite tank liquid engine / pressure caloric chemical and mass ledgers / every boundary", Liquid),
        ("finite tank physical needle engine / actual lift dose and rail replenishment / every boundary", Needle),
        ("finite tank agent / actionable pressure ownership and thermal errors / forks", Sessions)
    ];
    private static string Source(string name)=>File.ReadAllText(Path.Combine(AppContext.BaseDirectory,name+".power.json"));
    private static double V(SampleReport s,uint id,Field f)=>s.Values[Channels.Output(id,f).ToString()];
    private static JsonElement Data(AgentReply r){Require(r.Ok,r.Error?.Message??"Expected success.");return r.Data!.Value;}
    private static ExperimentReport Check(string name)
    {
        var doc=ModelDocument.Parse(Source(name));var report=ExperimentRunner.Evaluate(doc);Require(report.Passed&&report.Replay.SampleHashesMatch&&report.Model.Fidelity=="finite_tank_liquid_fuel_powertrain");
        var asset=AssetCodec.Decode(AssetCodec.Encode(doc.ToAsset(name)));var play=asset.CreatePlayback();var values=new Scalar[asset.Model.OutputCount];
        uint injector=doc.Model.Components.Single(c=>c.Kind==ComponentKind.LiquidFuelInjector).Id;
        double initial=doc.Model.Components.Single(c=>c.Id==injector).LiquidFuelInjector!.InitialMass.Value;
        foreach(var row in report.Samples)
        {
            while(play.TimeNanoseconds<row.TimeNs)Require(play.Advance(Math.Min(257*asset.Model.StepNanoseconds,row.TimeNs-play.TimeNanoseconds))==SimulationStatus.Ok);
            Require(play.ReadSnapshot(values).StateHash.ToString("x16")==row.StateHash&&values.All(v=>v.Value==row.Values[v.Channel.ToString()]));
            Near(V(row,injector,Field.Mass)+V(row,injector,Field.TotalFuelDelivered),initial+V(row,1511,Field.TotalFuelDelivered),1e-12);
            Near(V(row,1513,Field.Mass)+V(row,1511,Field.TotalFuelDelivered),2e-5,1e-14);
            Near(V(row,0,Field.FuelEnergyIn),V(row,0,Field.ChemicalEnergy)-V(report.Samples[0],0,Field.ChemicalEnergy)+V(row,15,Field.HeatReleased),2e-7);
            Near(V(row,injector,Field.Pressure),V(row,1501,Field.Pressure),0);Near(V(row,0,Field.EnergyResidual),0,1e-6);
            Near(V(row,0,Field.MassResidual),0,1e-13);Near(V(row,0,Field.FuelResidual),0,1e-13);Near(V(row,0,Field.HydraulicVolumeResidual),0,1e-14);
        }
        Require(V(report.Samples[^1],1513,Field.TankState)==1&&V(report.Samples[^1],1513,Field.Mass)==0);
        Require(V(report.Samples[^1],1510,Field.HydraulicWork)>0&&V(report.Samples[^1],1511,Field.TotalFuelDelivered)>0);return report;
    }
    private static void Liquid(){var r=Check("finite-tank-liquid-cylinder");Require(V(r.Samples[^1],15,Field.HeatReleased)>300);}
    private static void Needle(){var r=Check("finite-tank-needle-cylinder");Require(V(r.Samples[^1],15,Field.HeatReleased)>300&&r.Model.StateCount<=128);}
    private static void Sessions()
    {
        var w=new AgentWorkspace();var example=Data(AgentWorkspace.ExampleModel("finite-tank-liquid-cylinder"));string id=Data(w.CreateSession(example)).GetProperty("session_id").GetString()!;
        Data(w.StepSession(id,"0","1000000"));string parent=Data(w.ReadSnapshot(id)).GetRawText();string fork=Data(w.ForkSession(id,"1")).GetProperty("session_id").GetString()!;
        Data(w.SetInputs(fork,"0",[new("950",.01)]));Data(w.StepSession(fork,"1","1000000"));Require(Data(w.ReadSnapshot(id)).GetRawText()==parent);
        Data(w.CloseSession(fork,"2"));Data(w.CloseSession(id,"1"));
        var doc=ModelDocument.Parse(Source("finite-tank-liquid-cylinder"));var nodes=doc.Model.Nodes.ToArray();int rail=Array.FindIndex(nodes,n=>n.Id==1501);nodes[rail]=nodes[rail] with{Storage=new(1e-13,Unit.CubicMeterPerPascal)};
        Require(!CompiledModel.TryCompile(doc.Model with{Nodes=nodes},out _,out var error)&&error!.Code==DiagnosticCode.Connection);
    }
}
