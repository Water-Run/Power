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

internal static class ConverterIntegrationChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fired converter / joint combustion and shift / lockup / portable boundaries / ledgers", Replay),
        ("converter agent / strict maps / revisions / cancellation / forks / KPI distinction", Contracts),
        ("fired converter / timestep refinement across fluid and clutch transitions", Refinement)
    ];
    private static string Source => File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fired-converter.power.json"));
    private static JsonElement Data(AgentReply reply)
    { Require(reply.Ok,reply.Error?.Message ?? "Expected success"); return reply.Data!.Value; }
    private static void Replay()
    {
        var document=ModelDocument.Parse(Source); var report=ExperimentRunner.Evaluate(document);
        Require(report.Passed && report.Replay.SampleHashesMatch && report.Replay.BoundariesChecked==87);
        Require(report.Model.Fidelity=="quasisteady_converter_powertrain" && report.Model.Calibration=="unverified");
        var asset=AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Fired converter")));
        Require(asset.Components.Single(c=>c.Id==20).Converter!.PumpPositive!.Points.Count==5);
        var playback=asset.CreatePlayback(); var values=new Scalar[asset.Model.OutputCount];
        double previous=0; var modes=new HashSet<double>(); bool multiplication=false, locked=false;
        foreach(var boundary in report.Samples)
        {
            while(playback.TimeNanoseconds<boundary.TimeNs)
                Require(playback.Advance(Math.Min(1_350_000,boundary.TimeNs-playback.TimeNanoseconds))==SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16")==boundary.StateHash);
            Require(values.All(v=>v.Value==boundary.Values[v.Channel.ToString()]));
            double Get(uint id,Field field)=>boundary.Values[Channels.Output(id,field).ToString()];
            double fluid=Get(20,Field.FluidHeat); Require(fluid>=previous); previous=fluid;
            double heat=fluid+Get(16,Field.FrictionHeat)+Get(17,Field.FrictionHeat)+Get(21,Field.FrictionHeat);
            Near(Get(5,Field.Temperature),300+heat/200,1e-8); Near(Get(0,Field.EnergyResidual),0,1e-6);
            Near(Get(20,Field.Torque)+Get(20,Field.TorqueAtB)+Get(20,Field.TorqueAtC),0,1e-10);
            modes.Add(Get(21,Field.ClutchMode));
            multiplication|=Get(20,Field.TorqueAtB)>-Get(20,Field.Torque)*1.1 && Get(20,Field.Torque)<-1;
            if(Get(21,Field.ClutchMode)==1)
            { Near(Get(1,Field.Speed),Get(8,Field.Speed),1e-8); locked=true; }
        }
        Require(multiplication && locked && modes.Contains(0) && modes.Contains(1) && (modes.Contains(2)||modes.Contains(3)));
        var open=ExperimentRunner.Evaluate(document with { Events=document.Events.Select(e=>e with
            { Values=e.Values.Where(v=>v.Channel!=106).ToArray() }).Where(e=>e.Values.Length>0).ToArray(),Checks=[] });
        Require(open.Samples.Last().Values[Channels.Output(21,Field.FrictionHeat).ToString()]==0);
        Require(Math.Abs(open.Samples.Last().Values[Channels.Output(7,Field.Speed).ToString()]-report.Samples.Last().Values[Channels.Output(7,Field.Speed).ToString()])>.1);
    }
    private static void Contracts()
    {
        void Reject(Action<JsonNode> mutate,string code)
        {
            var node=JsonNode.Parse(Source)!; mutate(node); var json=JsonSerializer.SerializeToElement(node);
            var reply=AgentWorkspace.ValidateModel(json); Require(reply.Error?.Code==code,$"{reply.Error}");
            Require(!AgentWorkspace.ExportModelAsset(json).Ok && !new AgentWorkspace().CreateSession(json).Ok);
        }
        Reject(n=>n["components"]![10]!["parameters"]!.AsObject().Remove("pump_negative"),"invalid_argument");
        Reject(n=>n["components"]![10]!["parameters"]!["pump_positive"]![2]!["torque_ratio"]=2,"model_range");
        Reject(n=>n["components"]![10]!["parameters"]!["pump_positive"]![0]!["capacity_coefficient"]!["value"]=.01,"model_range");
        Reject(n=>n["components"]![10]!["parameters"]!["pump_positive"]![0]!["capacity_coefficient"]!["unit"]="nm","model_unit");
        Reject(n=>n["components"]![10]!["parameters"]!["pump_positive"]![0]!["efficiency"]=.9,"invalid_argument");
        Reject(n=>n["components"]![10]!["input_channel"]=107,"invalid_argument");
        Reject(n=>n["components"]![10]!["node_c"]=6,"invalid_argument");
        Reject(n=>n["components"]![10]!["node_b"]=1,"model_connection");
        var example=Data(AgentWorkspace.ExampleModel("fired-converter"));
        Require(JsonNode.DeepEquals(JsonNode.Parse(Source),JsonNode.Parse(example.GetRawText())));
        var capability=Data(AgentWorkspace.Capabilities());
        Require(capability.GetProperty("converter").GetProperty("maps").GetArrayLength()==4);
        Require(capability.GetProperty("asset_format").GetString()=="power.asset.v11" && capability.GetProperty("readable_asset_formats").GetArrayLength()==11);
        var workspace=new AgentWorkspace(); var created=Data(workspace.CreateSession(example)); string id=created.GetProperty("session_id").GetString()!;
        string before=Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.SetInputs(id,"0",[new("106",1.01)]).Error is { Code:"invalid_input",CurrentRevision:"0" });
        using var cancel=new CancellationTokenSource(); cancel.Cancel();
        Require(workspace.StepSession(id,"0","100000000",cancel.Token).Error is { Code:"cancelled",CurrentRevision:"0" });
        Require(Data(workspace.ReadSnapshot(id)).GetRawText()==before);
        Data(workspace.SetInputs(id,"0",[new("106",1)])); Data(workspace.StepSession(id,"1","100000000"));
        before=Data(workspace.ReadSnapshot(id)).GetRawText(); string child=Data(workspace.ForkSession(id,"2")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(child,"0",[new("106",0),new("102",-2)])); Data(workspace.StepSession(child,"1","100000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText()==before);
        Require(workspace.StepSession(id,"1","50000").Error?.Code=="revision_conflict");
        Data(workspace.CloseSession(id,"2")); Data(workspace.CloseSession(child,"2"));
        var failed=JsonNode.Parse(Source)!;
        failed["experiment"]!["checks"]=JsonSerializer.SerializeToNode(new[]{new{object_id=20,field="fluid_heat",max=0}});
        Require(!Data(AgentWorkspace.RunExperiment(JsonSerializer.SerializeToElement(failed))).GetProperty("passed").GetBoolean());
    }
    private static void Refinement()
    {
        var d=ModelDocument.Parse(Source); var ends=new List<double[]>();
        foreach(ulong step in new ulong[]{50_000,25_000,12_500,6_250,3_125})
        {
            var r=ExperimentRunner.Evaluate(d with { Model=d.Model with { StepNanoseconds=step } }); Require(r.Passed);
            var final=r.Samples.Last().Values;
            ends.Add([final[Channels.Output(1,Field.Speed).ToString()],final[Channels.Output(20,Field.FluidHeat).ToString()],final[Channels.Output(21,Field.FrictionHeat).ToString()]]);
        }
        // Clutch event alignment makes individual heat differences nonmonotone.
        // Check a dimensionless combined distance to the finest run, plus explicit
        // absolute bounds; smooth standalone laws have separate analytic order checks.
        var reference=ends.Last(); double previous=double.PositiveInfinity;
        for(int i=0;i<ends.Count-1;++i)
        {
            double distance=0;
            for(int k=0;k<reference.Length;++k)
            {
                double difference=Math.Abs(ends[i][k]-reference[k]);
                Require(difference<.0002,$"Fired converter refinement bound {i}/{k}: {difference:R}");
                distance+=difference/(1+Math.Abs(reference[k]));
            }
            Require(previous/distance>1.5,$"Fired converter combined refinement {i}: {previous:R} / {distance:R}");
            previous=distance;
        }
    }
}
