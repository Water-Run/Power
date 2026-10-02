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

internal static class HydraulicActuationIntegrationChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>
    [
        ("AT five hydraulic paths / helper identity / actual piston capacities / portable histories", Replay),
        ("fired six hydraulic actuators / complete pump piston converter energy / refinement", Fired),
        ("AT valve agent / actuator ownership guidance / revisions cancellation and independent release", Sessions)
    ];
    private static string Source(string name)=>File.ReadAllText(Path.Combine(AppContext.BaseDirectory,name+".power.json"));
    private static double V(SampleReport s,uint id,Field field)=>s.Values[Channels.Output(id,field).ToString()];
    private static JsonElement Data(AgentReply r){Require(r.Ok,r.Error?.Message??"Expected success.");return r.Data!.Value;}
    private static ExperimentReport Check(string name,int branches,int boundaries,int states)
    {
        var document=ModelDocument.Parse(Source(name));var report=ExperimentRunner.Evaluate(document);
        Require(report.Passed&&report.Replay.SampleHashesMatch&&report.Replay.BoundariesChecked==boundaries&&report.Model.StateCount==states);
        var asset=AssetCodec.Decode(AssetCodec.Encode(document.ToAsset(name)));var playback=asset.CreatePlayback();var values=new Scalar[asset.Model.OutputCount];
        foreach(var sample in report.Samples)
        {
            while(playback.TimeNanoseconds<sample.TimeNs)Require(playback.Advance(Math.Min(257*asset.Model.StepNanoseconds,sample.TimeNs-playback.TimeNanoseconds))==SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16")==sample.StateHash&&values.All(v=>v.Value==sample.Values[v.Channel.ToString()]));
            Near(V(sample,0,Field.EnergyResidual),0,1e-6);Near(V(sample,0,Field.HydraulicVolumeResidual),0,1e-14);
            double volume=2e-11*(V(sample,1100,Field.Pressure)-1e6);
            for(int i=0;i<branches;++i)
            {
                volume+=2e-12*V(sample,(uint)(1110+i),Field.Pressure)+.001*V(sample,(uint)(1120+i),Field.Displacement);
                uint clutch=i==5?21U:(uint)(300+i);
                double force=1e6*Math.Max(0,V(sample,(uint)(1120+i),Field.Displacement)-.002);
                Near(V(sample,clutch,Field.ClampForce),force,1e-7);Near(V(sample,clutch,Field.StaticCapacity),force*.4*4*.08,1e-7);
            }
            Near(volume,V(sample,0,Field.HydraulicVolumeIn),1e-14);
            foreach(uint gear in new uint[]{200,201,202,203,204})Near(V(sample,gear,Field.ConstraintError),0,1e-8);
        }
        Require(V(report.Samples[^1],1200,Field.HydraulicWork)>0);
        return report;
    }
    private static void Replay()
    {
        var mechanical=ResolvedPlanetChecks.Graph(range:1);var targets=mechanical.Components.Where(c=>c.Kind==ComponentKind.Clutch).OrderBy(c=>c.Id)
            .Select((c,i)=>HydraulicActuationChecks.Branch(c,i,i is 1 or 3?1:0)).ToArray();
        var hydraulic=new HydraulicActuationAssembly(HydraulicActuationChecks.Parameters()).CreateGraph(HydraulicActuationChecks.Ports(),targets);
        var model=new ModelDefinition{StepNanoseconds=20_000,Nodes=[NodeDefinition.Rotor(1,.2,60),NodeDefinition.Rotor(4,10),NodeDefinition.Thermal(3,1000,300),..mechanical.Nodes,..hydraulic.Nodes],
            Components=[..mechanical.Components.Where(c=>c.Kind!=ComponentKind.Clutch),..hydraulic.Components,ComponentDefinition.Torque(10,1,500,20),ComponentDefinition.Torque(11,4,501,-6)]};
        Require(CompiledModel.Compile(model).Fingerprint==CompiledModel.Compile(ModelDocument.Parse(Source("hydraulic-ravigneaux-transmission")).Model).Fingerprint);
        var report=Check("hydraulic-ravigneaux-transmission",5,201,83);
        Require(V(report.Samples[0],301,Field.StaticCapacity)==0&&V(report.Samples[0],303,Field.StaticCapacity)==0);
        foreach(var stage in new[]{(3.0,200_000_000UL),(5.0/3,450_000_000UL),(1.0,700_000_000UL),(2.0/3,950_000_000UL),(1.0,1_200_000_000UL),(5.0/3,1_450_000_000UL),(3.0,1_800_000_000UL)})
        {var sample=report.Samples.Single(s=>s.TimeNs==stage.Item2);Near(V(sample,1,Field.Speed),stage.Item1*4*V(sample,4,Field.Speed),1e-7);}
    }
    private static void Fired()
    {
        var report=Check("fired-hydraulic-ravigneaux",6,87,106);Require(V(report.Samples[^1],15,Field.HeatReleased)>300);
        var document=ModelDocument.Parse(Source("fired-hydraulic-ravigneaux"));
        var coarse=ExperimentRunner.Evaluate(document with{Model=document.Model with{StepNanoseconds=10_000}});
        var fine=ExperimentRunner.Evaluate(document with{Model=document.Model with{StepNanoseconds=5_000}});Require(coarse.Passed&&fine.Passed);
        double Error(ExperimentReport a,ExperimentReport b)
        {
            double error=0;
            foreach(var item in new[]{(1U,Field.Speed,100.0),(4U,Field.Speed,10.0),(1100U,Field.Pressure,1e6),(1115U,Field.Pressure,1e6),(1125U,Field.Displacement,.01),
                (1200U,Field.HydraulicWork,1000.0),(20U,Field.FluidHeat,1000.0),(21U,Field.FrictionHeat,1000.0)})
                error=Math.Max(error,Math.Abs(V(a.Samples[^1],item.Item1,item.Item2)-V(b.Samples[^1],item.Item1,item.Item2))/item.Item3);
            return error;
        }
        double a=Error(coarse,fine),b=Error(report,fine);Require(a<b,$"AT coupled refinement 10 us {a:R}, 20 us {b:R}");
        Console.WriteLine($"AT coupled refinement versus 5 us: 10 us {a:R}, 20 us {b:R}.");
    }
    private static void Sessions()
    {
        var workspace=new AgentWorkspace();var example=Data(AgentWorkspace.ExampleModel("hydraulic-ravigneaux-transmission"));
        string id=Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!,before=Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.SetInputs(id,"0",[new("600",1)]).Error?.Code=="unknown_channel");
        Require(workspace.SetInputs(id,"0",[new("700",2)]).Error?.Code=="invalid_input");
        Require(workspace.StepSession(id,"0","2000000",new CancellationToken(true)).Error?.Code=="cancelled"&&Data(workspace.ReadSnapshot(id)).GetRawText()==before);
        Data(workspace.StepSession(id,"0","2000000"));string parent=Data(workspace.ReadSnapshot(id)).GetRawText();
        string fork=Data(workspace.ForkSession(id,"1")).GetProperty("session_id").GetString()!;Data(workspace.SetInputs(fork,"0",[new("702",0),new("703",1)]));Data(workspace.StepSession(fork,"1","2000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText()==parent);Require(workspace.SetInputs(id,"0",[new("700",1)]).Error?.Code=="revision_conflict");
        Data(workspace.CloseSession(fork,"2"));Data(workspace.CloseSession(id,"1"));
        var bad=JsonNode.Parse(Source("hydraulic-ravigneaux-transmission"))!;bad["components"]!.AsArray().Single(c=>c!["id"]!.GetValue<uint>()==300)!["parameters"]!["piston_component"]=999;
        Require(!AgentWorkspace.ValidateModel(JsonSerializer.SerializeToElement(bad)).Ok);
    }
}
