// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
using System.Text.Json;
using System.Text.Json.Nodes;
using Power.Core;
using Power.Agent;
using Power.Assets;
using Power.Experiments;
using static Power.Tests.CoreChecks;
namespace Power.Tests;
internal static class RelaxingFuelIntegrationChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("relaxing documents / isolated source and phase transport complete portable histories",Documents),
        ("relaxing agent / explicit quality time units structured errors revisions and forks",Sessions)];
    private static JsonElement Data(AgentReply r){Require(r.Ok,r.Error?.Message??"Expected success.");return r.Data!.Value;}
    private static void Documents()
    {
        foreach(string name in new[]{"relaxing-fuel-evaporation","relaxing-fuel-duct"})
        {
            var d=ModelDocument.Load(Path.Combine(AppContext.BaseDirectory,name+".power.json"));var r=ExperimentRunner.Evaluate(d);Require(r.Passed&&r.Replay.SampleHashesMatch&&r.Model.Fidelity=="homogeneous_relaxing_fuel_transport");var a=AssetCodec.Decode(AssetCodec.Encode(d.ToAsset(name)));var p=a.CreatePlayback();var values=new Scalar[a.Model.OutputCount];
            foreach(var row in r.Samples){while(p.TimeNanoseconds<row.TimeNs)Require(p.Advance(Math.Min(7*a.Model.StepNanoseconds,row.TimeNs-p.TimeNanoseconds))==SimulationStatus.Ok);Require(p.ReadSnapshot(values).StateHash.ToString("x16")==row.StateHash&&values.All(v=>v.Value==row.Values[v.Channel.ToString()]));}
        }
    }
    private static void Sessions()
    {
        Require(Data(AgentWorkspace.Capabilities()).GetProperty("relaxing_fuel").GetProperty("domain").GetString()=="relaxing_fuel");var d=Data(AgentWorkspace.ExampleModel("relaxing-fuel-evaporation"));var w=new AgentWorkspace();string parent=Data(w.CreateSession(d)).GetProperty("session_id").GetString()!,child=Data(w.ForkSession(parent,"0")).GetProperty("session_id").GetString()!;
        Data(w.StepSession(child,"0","200000"));Require(Data(w.ReadSnapshot(parent)).GetProperty("revision").GetString()=="0");Require(w.StepSession(child,"0","100000").Error?.Code=="revision_conflict");Data(w.StepSession(parent,"0","200000"));Require(Data(w.ReadSnapshot(parent)).GetProperty("state_hash").GetString()==Data(w.ReadSnapshot(child)).GetProperty("state_hash").GetString());Data(w.CloseSession(parent,"1"));Data(w.CloseSession(child,"1"));
        var bad=JsonNode.Parse(d.GetRawText())!;bad["nodes"]![0]!["fuel"]!["phase_relaxation_time_ns"]=0;Require(!AgentWorkspace.ValidateModel(JsonDocument.Parse(bad.ToJsonString()).RootElement).Ok);
        bad["nodes"]![0]!["fuel"]!["phase_relaxation_time_ns"]=1000000;bad["nodes"]![0]!["initial_vapor_mass_fraction"]!["unit"]="k";Require(AgentWorkspace.ValidateModel(JsonDocument.Parse(bad.ToJsonString()).RootElement).Error?.Code=="model_unit");
    }
}
