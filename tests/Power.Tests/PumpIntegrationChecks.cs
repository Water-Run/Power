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

internal static class PumpIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fired pump / crank-driven hydraulic supply / every portable boundary / thermal ledger", Replay),
        ("pump agent / explicit inlet and relief contracts / revisions / KPI distinction", Contracts)
    ];
    private static string Source=>File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fired-pump.power.json"));
    private static JsonElement Data(AgentReply r){Require(r.Ok,r.Error?.Message??"Expected success");return r.Data!.Value;}
    private static void Replay()
    {
        var d=ModelDocument.Parse(Source);var report=ExperimentRunner.Evaluate(d);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked==89);
        Require(report.Model.Fidelity=="shaft_driven_hydraulics" && report.Model.Calibration=="unverified");
        var asset=AssetCodec.Decode(AssetCodec.Encode(d.ToAsset("Fired pump")));var playback=asset.CreatePlayback();var values=new Scalar[asset.Model.OutputCount];
        foreach(var boundary in report.Samples)
        {
            while(playback.TimeNanoseconds<boundary.TimeNs)Require(playback.Advance(Math.Min(1_350_000,boundary.TimeNs-playback.TimeNanoseconds))==SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16")==boundary.StateHash && values.All(v=>v.Value==boundary.Values[v.Channel.ToString()]));
            double Get(uint id,Field field)=>boundary.Values[Channels.Output(id,field).ToString()];
            double heat=Get(20,Field.FluidHeat)+Get(16,Field.FrictionHeat)+Get(17,Field.FrictionHeat)+Get(21,Field.FrictionHeat);
            for(uint id=40;id<=45;++id)heat+=Get(id,Field.FluidHeat);heat+=Get(47,Field.FluidHeat);
            Near(Get(5,Field.Temperature),300+heat/200,2e-8);Near(Get(0,Field.EnergyResidual),0,1e-6);Near(Get(0,Field.HydraulicWork),0,0);
            double restriction=Get(47,Field.FluidHeat),stored=0;for(uint id=40;id<=45;++id)restriction+=Get(id,Field.FluidHeat);
            for(uint id=30;id<=33;++id)stored+=Get(id,Field.InternalEnergy);
            Near(Get(46,Field.HydraulicWork),stored-3+restriction,1e-8);Near(Get(0,Field.HydraulicVolumeResidual),0,1e-16);
        }
    }
    private static void Contracts()
    {
        void Reject(Action<JsonNode> change,string code)
        {
            var n=JsonNode.Parse(Source)!;change(n);var json=JsonSerializer.SerializeToElement(n);
            var r=AgentWorkspace.ValidateModel(json);Require(r.Error?.Code==code,$"{r.Error}");Require(!AgentWorkspace.ExportModelAsset(json).Ok);
        }
        JsonNode Pump(JsonNode n)=>n["components"]!.AsArray().Single(c=>c!["id"]!.GetValue<int>()==46)!;
        JsonNode Relief(JsonNode n)=>n["components"]!.AsArray().Single(c=>c!["id"]!.GetValue<int>()==47)!;
        Reject(n=>Pump(n)["parameters"]!.AsObject().Remove("inlet_node"),"invalid_argument");
        Reject(n=>Pump(n)["parameters"]!.AsObject().Remove("reservoir_pressure"),"invalid_argument");
        Reject(n=>Pump(n)["parameters"]!["displacement"]!["unit"]="m3","model_unit");
        Reject(n=>Pump(n)["parameters"]!["displacement"]!["value"]=0,"model_range");
        Reject(n=>Pump(n)["node_b"]=1,"model_connection");Reject(n=>Pump(n)["heat_node"]=5,"invalid_argument");
        Reject(n=>Relief(n)["parameters"]!["cracking_pressure"]!["value"]=-1,"model_range");Reject(n=>Relief(n)["input_channel"]=200,"invalid_argument");
        var example=Data(AgentWorkspace.ExampleModel("fired-pump"));Require(JsonNode.DeepEquals(JsonNode.Parse(Source),JsonNode.Parse(example.GetRawText())));
        Require(Data(AgentWorkspace.Capabilities()).GetProperty("hydraulic_pump").GetProperty("displacement_unit").GetString()=="m3_rad");
        var w=new AgentWorkspace();string id=Data(w.CreateSession(example)).GetProperty("session_id").GetString()!;
        string before=Data(w.ReadSnapshot(id)).GetRawText();Require(w.StepSession(id,"1","50000").Error?.Code=="revision_conflict");
        Require(w.StepSession(id,"0","50000",new CancellationToken(true)).Error?.Code=="cancelled");Require(Data(w.ReadSnapshot(id)).GetRawText()==before);
        Data(w.StepSession(id,"0","10000000"));Data(w.CloseSession(id,"1"));
        var failed=JsonNode.Parse(Source)!;failed["experiment"]!["checks"]=JsonSerializer.SerializeToNode(new[]{new{object_id=46,field="hydraulic_work",max=0}});
        Require(!Data(AgentWorkspace.RunExperiment(JsonSerializer.SerializeToElement(failed))).GetProperty("passed").GetBoolean());
    }
}
