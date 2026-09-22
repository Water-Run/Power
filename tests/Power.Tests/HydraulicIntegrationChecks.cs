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

internal static class HydraulicIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fired hydraulics / delayed pressure actuation / full ledgers / every portable boundary", Replay),
        ("hydraulic agent / strict physical contracts / revisions / cancellation / branches / KPI distinction", Contracts)
    ];
    private static string Source=>File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fired-hydraulic.power.json"));
    private static JsonElement Data(AgentReply reply)
    { Require(reply.Ok,reply.Error?.Message??"Expected success"); return reply.Data!.Value; }
    private static void Replay()
    {
        var document=ModelDocument.Parse(Source); var report=ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked==89);
        Require(report.Model.Fidelity=="compliant_hydraulic_powertrain" && report.Model.Calibration=="unverified");
        var asset=AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Fired hydraulics")));
        Require(asset.Components.Single(c=>c.Id==21).HydraulicClutch!.PressureNode==32);
        var playback=asset.CreatePlayback(); var values=new Scalar[asset.Model.OutputCount]; double prior=0;
        bool delayed=false,charging=false,draining=false,locked=false;
        foreach(var boundary in report.Samples)
        {
            while(playback.TimeNanoseconds<boundary.TimeNs)
                Require(playback.Advance(Math.Min(1_350_000,boundary.TimeNs-playback.TimeNanoseconds))==SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16")==boundary.StateHash);
            Require(values.All(v=>v.Value==boundary.Values[v.Channel.ToString()]));
            double Get(uint id,Field field)=>boundary.Values[Channels.Output(id,field).ToString()];
            double heat=Get(20,Field.FluidHeat)+Get(16,Field.FrictionHeat)+Get(17,Field.FrictionHeat)+Get(21,Field.FrictionHeat);
            for(uint id=40;id<=45;++id) heat+=Get(id,Field.FluidHeat);
            Require(heat>=prior); prior=heat; Near(Get(5,Field.Temperature),300+heat/200,2e-8);
            Near(Get(0,Field.EnergyResidual),0,1e-6); Near(Get(0,Field.HydraulicVolumeResidual),0,1e-16);
            Near(Get(21,Field.ClampForce),Math.Max(0,.001*Get(32,Field.Pressure)-100),1e-10);
            if(boundary.TimeNs==100_050_000) { Require(Get(32,Field.Pressure)==0 && Get(21,Field.ClampForce)==0); delayed=true; }
            if(boundary.TimeNs==110_000_000) { Require(Get(32,Field.Pressure)>1e5 && Get(32,Field.Pressure)<1e6); charging=true; }
            if(boundary.TimeNs==210_000_000) { Require(Get(31,Field.Pressure)>0 && Get(31,Field.Pressure)<1e6); draining=true; }
            locked|=Get(21,Field.ClutchMode)==1;
        }
        Require(delayed && charging && draining && locked);
        // Closing every reservoir path preserves initial pressure; it must not act like a command-following lag.
        var trapped=document with {Model=document.Model with{Components=document.Model.Components.Select(c=>c.HydraulicRestriction is null ? c : c with{InitialInput=new(0,Unit.Fraction)}).ToArray()},Events=[],Checks=[]};
        var held=ExperimentRunner.Evaluate(trapped); var final=held.Samples.Last().Values;
        Require(final[Channels.Output(32,Field.Pressure).ToString()]==0 && final[Channels.Output(21,Field.FrictionHeat).ToString()]==0);
        Near(final[Channels.Output(31,Field.Pressure).ToString()],1e6,0);
        Require(final[Channels.Output(0,Field.HydraulicWork).ToString()]==0);
    }
    private static void Contracts()
    {
        void Reject(Action<JsonNode> change,string code)
        {
            var n=JsonNode.Parse(Source)!; change(n); var json=JsonSerializer.SerializeToElement(n);
            var result=AgentWorkspace.ValidateModel(json); Require(result.Error?.Code==code,$"{result.Error}");
            Require(!AgentWorkspace.ExportModelAsset(json).Ok && !new AgentWorkspace().CreateSession(json).Ok);
        }
        Reject(n=>n["components"]![6]!["parameters"]!["pressure_node"]=5,"model_connection");
        Reject(n=>n["components"]![6]!["parameters"]!["sliding_friction"]=2,"model_range");
        Reject(n=>n["components"]![6]!["parameters"]!["preload_force"]!["unit"]="nm","model_unit");
        Reject(n=>n["components"]![6]!["input_channel"]=104,"invalid_argument");
        Reject(n=>n["components"]![12]!["parameters"]!.AsObject().Remove("reservoir_pressure"),"invalid_argument");
        Reject(n=>n["components"]![12]!["parameters"]!["coefficient"]!["unit"]="m3_s_pa","model_unit");
        Reject(n=>n["components"]![12]!["parameters"]!["transition_pressure"]!["value"]=0,"model_range");
        Reject(n=>n["nodes"]![8]!["initial"]!["value"]=-1,"model_range");
        var example=Data(AgentWorkspace.ExampleModel("fired-hydraulic")); Require(JsonNode.DeepEquals(JsonNode.Parse(Source),JsonNode.Parse(example.GetRawText())));
        var capability=Data(AgentWorkspace.Capabilities()); Require(capability.GetProperty("hydraulics").GetProperty("pressure_reference").GetString()=="nonnegative_gauge_common_tank");
        Require(capability.GetProperty("asset_format").GetString()=="power.asset.v11" && capability.GetProperty("readable_asset_formats").GetArrayLength()==11);
        var workspace=new AgentWorkspace(); var created=Data(workspace.CreateSession(example)); string id=created.GetProperty("session_id").GetString()!;
        string before=Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.SetInputs(id,"0",[new("106",1),new("116",-1)]).Error is {Code:"invalid_input",CurrentRevision:"0"});
        using var cancel=new CancellationTokenSource(); cancel.Cancel();
        Require(workspace.StepSession(id,"0","100000000",cancel.Token).Error is {Code:"cancelled",CurrentRevision:"0"});
        Require(Data(workspace.ReadSnapshot(id)).GetRawText()==before);
        Data(workspace.SetInputs(id,"0",[new("106",1),new("116",0)])); Data(workspace.StepSession(id,"1","100000000"));
        before=Data(workspace.ReadSnapshot(id)).GetRawText(); string child=Data(workspace.ForkSession(id,"2")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(child,"0",[new("106",0),new("116",1)])); Data(workspace.StepSession(child,"1","100000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText()==before && workspace.StepSession(id,"1","50000").Error?.Code=="revision_conflict");
        Data(workspace.CloseSession(id,"2")); Data(workspace.CloseSession(child,"2"));
        var failed=JsonNode.Parse(Source)!; failed["experiment"]!["checks"]=JsonSerializer.SerializeToNode(new[]{new{object_id=0,field="hydraulic_work",max=0}});
        Require(!Data(AgentWorkspace.RunExperiment(JsonSerializer.SerializeToElement(failed))).GetProperty("passed").GetBoolean());
    }
}
