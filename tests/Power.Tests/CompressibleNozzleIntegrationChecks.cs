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
internal static class CompressibleNozzleIntegrationChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("compressible nozzle documents / finite discharge pump needle every portable boundary",Documents),
        ("compressible nozzle agent / frozen phase structured repair opening revisions and forks",Sessions)];
    private static JsonElement Data(AgentReply reply){Require(reply.Ok,reply.Error?.Message??"Expected success.");return reply.Data!.Value;}
    private static void Documents()
    {
        foreach(string name in new[]{"compressible-fuel-nozzle","compressible-pump-needle"})
        {
            var d=ModelDocument.Load(Path.Combine(AppContext.BaseDirectory,name+".power.json"));var report=ExperimentRunner.Evaluate(d);Require(report.Passed&&report.Replay.SampleHashesMatch&&report.Model.Fidelity=="frozen_compressible_fuel_nozzle_network");
            var a=AssetCodec.Decode(AssetCodec.Encode(d.ToAsset(name)));var p=a.CreatePlayback();var values=new Scalar[a.Model.OutputCount];
            foreach(var row in report.Samples){while(p.TimeNanoseconds<row.TimeNs)Require(p.Advance(Math.Min(7*a.Model.StepNanoseconds,row.TimeNs-p.TimeNanoseconds))==SimulationStatus.Ok);Require(p.ReadSnapshot(values).StateHash.ToString("x16")==row.StateHash&&values.All(v=>v.Value==row.Values[v.Channel.ToString()]));}
        }
    }
    private static void Sessions()
    {
        var capability=Data(AgentWorkspace.Capabilities());Require(capability.GetProperty("compressible_fuel_nozzle").GetProperty("phase_mode").GetString()=="frozen");
        var d=Data(AgentWorkspace.ExampleModel("compressible-fuel-nozzle"));var w=new AgentWorkspace();string parent=Data(w.CreateSession(d)).GetProperty("session_id").GetString()!,child=Data(w.ForkSession(parent,"0")).GetProperty("session_id").GetString()!;
        Data(w.SetInputs(child,"0",[new("100",0)]));Data(w.StepSession(child,"1","100000"));Require(Data(w.ReadSnapshot(parent)).GetProperty("revision").GetString()=="0");Require(w.StepSession(child,"0","10000").Error?.Code=="revision_conflict");
        Data(w.StepSession(parent,"0","100000"));Require(Data(w.ReadSnapshot(parent)).GetProperty("state_hash").GetString()!=Data(w.ReadSnapshot(child)).GetProperty("state_hash").GetString());Data(w.CloseSession(parent,"1"));Data(w.CloseSession(child,"2"));
        var bad=JsonNode.Parse(d.GetRawText())!;bad["components"]![0]!["parameters"]!["phase_mode"]="equilibrium";Require(!AgentWorkspace.ValidateModel(JsonDocument.Parse(bad.ToJsonString()).RootElement).Ok);
        bad["components"]![0]!["parameters"]!["phase_mode"]="frozen";bad["components"]![0]!["parameters"]!["area"]!["unit"]="k";Require(AgentWorkspace.ValidateModel(JsonDocument.Parse(bad.ToJsonString()).RootElement).Error?.Code=="model_unit");
    }
}
