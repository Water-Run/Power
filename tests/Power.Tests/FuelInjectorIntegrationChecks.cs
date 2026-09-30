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

internal static class FuelInjectorIntegrationChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>
    [
        ("metered fired cylinder / finite rail depletion / constituent ledger / every boundary",Replay),
        ("fuel dose documents / kg command / finite compatible source / timing limits",Contracts),
        ("fuel meter sessions / live input and sampled quota / revision / cancellation / fork",Sessions)
    ];
    private static string Source=>File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"metered-fired-cylinder.power.json"));
    private static double Value(SampleReport sample,uint id,Field field)=>sample.Values[Channels.Output(id,field).ToString()];
    private static JsonElement Data(AgentReply reply){Require(reply.Ok,reply.Error?.Message??"Expected success.");return reply.Data!.Value;}
    private static void Replay()
    {
        var document=ModelDocument.Parse(Source);var report=ExperimentRunner.Evaluate(document);Require(report.Passed&&report.Replay.SampleHashesMatch&&report.Replay.BoundariesChecked==65);
        Require(report.Model.Fidelity=="metered_fired_powertrain"&&report.Model.Calibration=="unverified");
        var asset=AssetCodec.Decode(AssetCodec.Encode(document.ToAsset("Metered fired cylinder")));var playback=asset.CreatePlayback();var values=new Scalar[asset.Model.OutputCount];double rail=Value(report.Samples[0],4,Field.FuelMass);
        foreach(var sample in report.Samples)
        {
            while(playback.TimeNanoseconds<sample.TimeNs)Require(playback.Advance(Math.Min(257*asset.Model.StepNanoseconds,sample.TimeNs-playback.TimeNanoseconds))==SimulationStatus.Ok);
            Require(playback.ReadSnapshot(values).StateHash.ToString("x16")==sample.StateHash&&values.All(v=>v.Value==sample.Values[v.Channel.ToString()]));
            double delivered=Value(sample,16,Field.TotalFuelDelivered),requested=Value(sample,16,Field.RequestedFuelDose),cycle=Value(sample,16,Field.DeliveredFuelDose);
            Require(cycle<=requested+1e-18&&requested<=4e-5);Near(rail-Value(sample,4,Field.FuelMass),delivered,1e-16);
            Near(Value(sample,2,Field.FuelMass)+Value(sample,15,Field.FuelBurned)-Value(sample,0,Field.FuelEnergyIn)/44e6,delivered,1e-14);
            Near(Value(sample,0,Field.EnergyResidual),0,1e-6);Near(Value(sample,0,Field.MassResidual),0,1e-14);Near(Value(sample,0,Field.FuelResidual),0,1e-14);
        }
        Near(Value(report.Samples[^1],16,Field.TotalFuelDelivered),28e-6,1e-16);
        Near(Value(report.Samples[^1],16,Field.RequestedFuelDose),12e-6,0);
        Require(Value(report.Samples[^1],15,Field.FuelBurned)<=Value(report.Samples[^1],16,Field.TotalFuelDelivered));
    }
    private static void Contracts()
    {
        JsonNode Meter(JsonNode n)=>n["components"]!.AsArray().Single(c=>c!["kind"]!.GetValue<string>()=="gas_fuel_injector")!;
        void Reject(Action<JsonNode> mutation,string code){var n=JsonNode.Parse(Source)!;mutation(n);var value=JsonSerializer.SerializeToElement(n);var reply=AgentWorkspace.ValidateModel(value);Require(reply.Error?.Code==code,$"Expected {code}: {reply.Error}");Require(!AgentWorkspace.ExportModelAsset(value).Ok);}
        Reject(n=>Meter(n)["initial_input"]!["unit"]="fraction","model_unit");
        Reject(n=>Meter(n)["initial_input"]!["value"]=.001,"model_range");
        Reject(n=>Meter(n)["parameters"]!.AsObject().Remove("maximum_dose"),"invalid_argument");
        Reject(n=>Meter(n)["parameters"]!["crank_node"]=2,"model_connection");
        Reject(n=>Meter(n)["parameters"]!["cycle_angle"]!["value"]=180,"model_range");
        Reject(n=>Meter(n)["node_b"]=0,"model_connection");
        Reject(n=>Meter(n)["parameters"]!["maximum_dose"]!["unit"]="j","model_unit");
        Reject(n=>n["nodes"]!.AsArray().Single(v=>v!["id"]!.GetValue<int>()==4)!["gas"]!["gamma"]=1.4,"model_connection");
    }
    private static void Sessions()
    {
        var example=Data(AgentWorkspace.ExampleModel("metered-fired-cylinder"));Require(JsonNode.DeepEquals(JsonNode.Parse(Source),JsonNode.Parse(example.GetRawText())));
        var workspace=new AgentWorkspace();string id=Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!;string before=Data(workspace.ReadSnapshot(id)).GetRawText();
        Require(workspace.StepSession(id,"1","10000000").Error?.Code=="revision_conflict");Require(workspace.StepSession(id,"0","10000000",new CancellationToken(true)).Error?.Code=="cancelled");Require(Data(workspace.ReadSnapshot(id)).GetRawText()==before);
        Data(workspace.StepSession(id,"0","20000000"));string parent=Data(workspace.ReadSnapshot(id)).GetRawText();string fork=Data(workspace.ForkSession(id,"1")).GetProperty("session_id").GetString()!;
        Data(workspace.SetInputs(fork,"0",[new("104",0)]));Data(workspace.StepSession(fork,"1","20000000"));Require(Data(workspace.ReadSnapshot(id)).GetRawText()==parent);Data(workspace.CloseSession(fork,"2"));Data(workspace.CloseSession(id,"1"));
    }
}
