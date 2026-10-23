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

internal static class AtControllerIntegrationChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>
    [
        ("controlled AT / sensor-confirmed up-down ranges pressure PI / every portable boundary", Replay),
        ("controlled fired AT / engine hydraulics and automatic physical lockup / complete state", Fired),
        ("AT feedback agent / owned-valve repair integral requests / independent neutral fork", Sessions)
    ];
    private static string Source(string name)=>File.ReadAllText(Path.Combine(AppContext.BaseDirectory,name+".power.json"));
    private static double V(SampleReport s,uint id,Field field)=>s.Values[Channels.Output(id,field).ToString()];
    private static JsonElement Data(AgentReply r){Require(r.Ok,r.Error?.Message??"Expected success.");return r.Data!.Value;}
    private static ExperimentReport Check(string name,int boundaries,int states)
    {
        var document=ModelDocument.Parse(Source(name));var report=ExperimentRunner.Evaluate(document);
        Require(report.Passed&&report.Replay.SampleHashesMatch&&report.Replay.BoundariesChecked==boundaries&&report.Model.StateCount==states&&report.Model.Fidelity=="sampled_hydraulic_at_control");
        var asset=AssetCodec.Decode(AssetCodec.Encode(document.ToAsset(name)));var playback=asset.CreatePlayback();var values=new Scalar[asset.Model.OutputCount];
        foreach(var s in report.Samples)
        {
            while(playback.TimeNanoseconds<s.TimeNs)Require(playback.Advance(Math.Min(257*asset.Model.StepNanoseconds,s.TimeNs-playback.TimeNanoseconds))==SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16")==s.StateHash&&values.All(v=>v.Value==s.Values[v.Channel.ToString()]));
            Near(V(s,0,Field.EnergyResidual),0,1e-6);Near(V(s,0,Field.HydraulicVolumeResidual),0,1e-14);Require(V(s,1400,Field.ControlFault)==0);
            foreach(uint gear in new uint[]{200,201,202,203,204})Near(V(s,gear,Field.ConstraintError),0,1e-8);
            int actual=(int)V(s,1400,Field.ActualGear);
            if(actual!=0)Near(V(s,102,Field.Speed)*new RavigneauxTransmissionAssembly(RavigneauxChecks.Parameters()).RangeReduction(actual),V(s,name.StartsWith("controlled-fired")?8U:1U,Field.Speed),1e-7);
        }
        return report;
    }
    private static void Replay()
    {
        var report=Check("controlled-hydraulic-ravigneaux",451,99);
        foreach(var item in new[]{(1,500_000_000UL),(2,1_100_000_000UL),(3,1_700_000_000UL),(4,2_300_000_000UL),(3,2_900_000_000UL),(2,3_500_000_000UL),(1,4_400_000_000UL)})
            Near(V(report.Samples.Single(s=>s.TimeNs==item.Item2),1400,Field.ActualGear),item.Item1,0);
    }
    private static void Fired()
    {
        var report=Check("controlled-fired-hydraulic-ravigneaux",221,122);var last=report.Samples[^1];
        Near(V(last,1400,Field.ActualGear),4,0);Near(V(last,1400,Field.LockupState),(int)AtLockupPhase.Locked,0);
        Require(V(last,21,Field.ClampForce)>200&&V(last,15,Field.HeatReleased)>300&&V(last,1200,Field.HydraulicWork)>0);
        Near(V(last,1,Field.Speed),V(last,8,Field.Speed),1e-7);
    }
    private static void Sessions()
    {
        var workspace=new AgentWorkspace();var example=Data(AgentWorkspace.ExampleModel("controlled-hydraulic-ravigneaux"));
        string id=Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!,before=Data(workspace.ReadSnapshot(id)).GetRawText();
        var owned=workspace.SetInputs(id,"0",[new("702",1)]);Require(owned.Error?.Code=="controlled_input"&&owned.Error.Message.Contains("900")&&owned.Error.Message.Contains("requested_gear"));
        Require(workspace.SetInputs(id,"0",[new("900",1.5)]).Error?.Code=="invalid_input");
        Require(workspace.StepSession(id,"0","2000000",new CancellationToken(true)).Error?.Code=="cancelled"&&Data(workspace.ReadSnapshot(id)).GetRawText()==before);
        Data(workspace.StepSession(id,"0","2000000"));string parent=Data(workspace.ReadSnapshot(id)).GetRawText();
        string fork=Data(workspace.ForkSession(id,"1")).GetProperty("session_id").GetString()!;Data(workspace.SetInputs(fork,"0",[new("900",0)]));Data(workspace.StepSession(fork,"1","2000000"));
        Require(Data(workspace.ReadSnapshot(id)).GetRawText()==parent);Data(workspace.CloseSession(fork,"2"));Data(workspace.CloseSession(id,"1"));
    }
}
