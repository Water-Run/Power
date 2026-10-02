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

internal static class ResolvedPlanetIntegrationChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>
    [
        ("resolved planet graph / helper identity / all handoffs and spin energy / portable replay", Replay),
        ("fired resolved planets / combined converter burn / refinement and complete heat ledger", Fired),
        ("resolved planet agent / signed typed contracts / revisions cancellation and neutral fork", Sessions)
    ];
    private static string Source(string name)=>File.ReadAllText(Path.Combine(AppContext.BaseDirectory,name+".power.json"));
    private static double V(SampleReport s,uint id,Field field)=>s.Values[Channels.Output(id,field).ToString()];
    private static JsonElement Data(AgentReply r){Require(r.Ok,r.Error?.Message??"Expected success.");return r.Data!.Value;}
    private static ExperimentReport Check(string name,int boundaries,bool fired)
    {
        var document=ModelDocument.Parse(Source(name));var report=ExperimentRunner.Evaluate(document);
        Require(report.Passed&&report.Replay.SampleHashesMatch&&report.Replay.BoundariesChecked==boundaries);
        var asset=AssetCodec.Decode(AssetCodec.Encode(document.ToAsset(name)));var playback=asset.CreatePlayback();var values=new Scalar[asset.Model.OutputCount];
        foreach(var sample in report.Samples)
        {
            while(playback.TimeNanoseconds<sample.TimeNs)Require(playback.Advance(Math.Min(257*asset.Model.StepNanoseconds,sample.TimeNs-playback.TimeNanoseconds))==SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16")==sample.StateHash&&values.All(v=>v.Value==sample.Values[v.Channel.ToString()]));
            Near(V(sample,0,Field.EnergyResidual),0,1e-6);
            foreach(uint id in new uint[]{200,201,202,203,204})Near(V(sample,id,Field.ConstraintError),0,1e-8);
            double ring=V(sample,102,Field.Speed),carrier=V(sample,103,Field.Speed);
            Near(V(sample,104,Field.Speed),-12*ring+13*carrier,1e-8);Near(V(sample,105,Field.Speed),4*ring-3*carrier,1e-8);
            double heat=Enumerable.Range(300,5).Sum(i=>V(sample,(uint)i,Field.FrictionHeat));if(fired)heat+=V(sample,20,Field.FluidHeat)+V(sample,21,Field.FrictionHeat);
            Near(1000*(V(sample,fired?5U:3U,Field.Temperature)-300),heat,1e-7);
        }
        return report;
    }
    private static void Replay()
    {
        var graph=ResolvedPlanetChecks.Graph(range:1);var definition=new ModelDefinition{StepNanoseconds=100_000,
            Nodes=[NodeDefinition.Rotor(1,.2,60),NodeDefinition.Rotor(4,10),NodeDefinition.Thermal(3,1000,300),..graph.Nodes],
            Components=[..graph.Components,ComponentDefinition.Torque(10,1,500,20),ComponentDefinition.Torque(11,4,501,-6)]};
        Require(CompiledModel.Compile(definition).Fingerprint==CompiledModel.Compile(ModelDocument.Parse(Source("resolved-ravigneaux-transmission")).Model).Fingerprint);
        var report=Check("resolved-ravigneaux-transmission",201,false);Require(report.Model.StateCount==27);
        foreach(var stage in new[]{(3.0,200_000_000UL),(5.0/3,450_000_000UL),(1.0,700_000_000UL),(2.0/3,950_000_000UL),(1.0,1_200_000_000UL),(5.0/3,1_450_000_000UL),(3.0,1_800_000_000UL)})
        {var sample=report.Samples.Single(s=>s.TimeNs==stage.Item2);Near(V(sample,1,Field.Speed),stage.Item1*4*V(sample,4,Field.Speed),1e-8);}
        Require(Math.Abs(V(report.Samples[^1],104,Field.Speed))>10&&Math.Abs(V(report.Samples[^1],105,Field.Speed))>10);
    }
    private static void Fired()
    {
        var report=Check("fired-resolved-ravigneaux-converter",87,true);Require(report.Model.StateCount==41&&V(report.Samples[^1],15,Field.HeatReleased)>300);
        var document=ModelDocument.Parse(Source("fired-resolved-ravigneaux-converter"));
        var fine=ExperimentRunner.Evaluate(document with{Model=document.Model with{StepNanoseconds=25_000}});
        var finest=ExperimentRunner.Evaluate(document with{Model=document.Model with{StepNanoseconds=12_500}});Require(fine.Passed&&finest.Passed);
        double Error(ExperimentReport a,ExperimentReport b)
        {
            double error=0;
            foreach(var item in new[]{(1U,Field.Speed,100.0),(4U,Field.Speed,10.0),(104U,Field.Speed,1000.0),(105U,Field.Speed,1000.0),(0U,Field.SourceWork,1000.0),(20U,Field.FluidHeat,1000.0),(21U,Field.FrictionHeat,1000.0)})
                error=Math.Max(error,Math.Abs(V(a.Samples[^1],item.Item1,item.Item2)-V(b.Samples[^1],item.Item1,item.Item2))/item.Item3);
            return error;
        }
        double coarse=Error(report,finest),refined=Error(fine,finest);Require(refined<coarse,$"Resolved planet refinement: {coarse:R}, {refined:R}");
        Console.WriteLine($"Resolved planet refinement versus 12.5 us: 50 us {coarse:R}, 25 us {refined:R}.");
    }
    private static void Sessions()
    {
        var workspace=new AgentWorkspace();var example=Data(AgentWorkspace.ExampleModel("resolved-ravigneaux-transmission"));
        Require(JsonNode.DeepEquals(JsonNode.Parse(Source("resolved-ravigneaux-transmission")),JsonNode.Parse(example.GetRawText())));
        string id=Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!,before=Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.StepSession(id,"0","20000000",new CancellationToken(true)).Error?.Code=="cancelled"&&Data(workspace.ReadSnapshot(id)).GetRawText()==before);
        Data(workspace.StepSession(id,"0","20000000"));string parent=Data(workspace.ReadSnapshot(id)).GetRawText();
        string fork=Data(workspace.ForkSession(id,"1")).GetProperty("session_id").GetString()!;Data(workspace.SetInputs(fork,"0",[new("601",0),new("603",0)]));Data(workspace.StepSession(fork,"1","20000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText()==parent);Require(workspace.SetInputs(id,"0",[new("600",1)]).Error?.Code=="revision_conflict");
        Data(workspace.CloseSession(fork,"2"));Data(workspace.CloseSession(id,"1"));
        var bad=JsonNode.Parse(Source("resolved-ravigneaux-transmission"))!;bad["components"]!.AsArray().Single(c=>c!["id"]!.GetValue<uint>()==200)!["parameters"]!["ratio"]=0;
        Require(AgentWorkspace.ValidateModel(JsonSerializer.SerializeToElement(bad)).Error?.Code=="model_range");
    }
}
