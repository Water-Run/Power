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
internal static class EquilibriumDuctIntegrationChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("equilibrium duct documents / condensation vapor formation and every cell boundary",Documents),
        ("equilibrium duct agent / explicit velocity cell topology structured errors and forks",Sessions)];
    private static JsonElement Data(AgentReply r){Require(r.Ok,r.Error?.Message??"Expected success.");return r.Data!.Value;}
    private static void Documents()
    {
        foreach(string name in new[]{"equilibrium-fuel-condensing-duct","equilibrium-fuel-flashing-duct"})
        {
            var doc=ModelDocument.Load(Path.Combine(AppContext.BaseDirectory,name+".power.json"));var report=ExperimentRunner.Evaluate(doc);Require(report.Passed&&report.Replay.SampleHashesMatch&&report.Model.Fidelity=="equilibrium_compressible_fuel_transport");
            var asset=AssetCodec.Decode(AssetCodec.Encode(doc.ToAsset(name)));var play=asset.CreatePlayback();var values=new Scalar[asset.Model.OutputCount];
            foreach(var row in report.Samples){while(play.TimeNanoseconds<row.TimeNs)Require(play.Advance(Math.Min(7*asset.Model.StepNanoseconds,row.TimeNs-play.TimeNanoseconds))==SimulationStatus.Ok);Require(play.ReadSnapshot(values).StateHash.ToString("x16")==row.StateHash&&values.All(v=>v.Value==row.Values[v.Channel.ToString()]));Near(row.Values[Channels.Output(0,Field.EnergyResidual).ToString()],0,2e-7);}
        }
    }
    private static void Sessions()
    {
        var capability=Data(AgentWorkspace.Capabilities());Require(capability.GetProperty("equilibrium_fuel_duct").GetProperty("domain").GetString()=="compressible_fuel");
        var example=Data(AgentWorkspace.ExampleModel("equilibrium-fuel-flashing-duct"));var w=new AgentWorkspace();string parent=Data(w.CreateSession(example)).GetProperty("session_id").GetString()!,child=Data(w.ForkSession(parent,"0")).GetProperty("session_id").GetString()!;
        Data(w.StepSession(child,"0","10000"));Require(Data(w.ReadSnapshot(parent)).GetProperty("revision").GetString()=="0");Require(w.StepSession(child,"0","1000").Error?.Code=="revision_conflict");Data(w.StepSession(parent,"0","10000"));Require(Data(w.ReadSnapshot(parent)).GetProperty("state_hash").GetString()==Data(w.ReadSnapshot(child)).GetProperty("state_hash").GetString());Data(w.CloseSession(parent,"1"));Data(w.CloseSession(child,"1"));
        var bad=JsonNode.Parse(example.GetRawText())!;var node=bad["nodes"]![0]!;node.AsObject().Remove("initial_velocity");Require(!AgentWorkspace.ValidateModel(JsonDocument.Parse(bad.ToJsonString()).RootElement).Ok);
        node["initial_velocity"]=new JsonObject{["value"]=0,["unit"]="m_s"};node["fuel"]!["liquid_bulk_modulus"]!["unit"]="k";Require(AgentWorkspace.ValidateModel(JsonDocument.Parse(bad.ToJsonString()).RootElement).Error?.Code=="model_unit");
    }
}
