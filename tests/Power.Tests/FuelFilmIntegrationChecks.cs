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

internal static class FuelFilmIntegrationChecks
{
 internal static IEnumerable<(string Name,Action Run)> All=>[("film fired cylinder / phase heat and chemical inventory / vapor-only reaction / every boundary",Replay),("film documents / finite wall / phase quantities / explicit initial liquid inventory",Contracts),("film sessions / phase history / revision / cancellation / independent fork",Sessions)];
 private static string Source=>File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"film-fired-cylinder.power.json"));
 private static double V(SampleReport s,uint id,Field field)=>s.Values[Channels.Output(id,field).ToString()];
 private static JsonElement Data(AgentReply reply){Require(reply.Ok,reply.Error?.Message??"Expected success.");return reply.Data!.Value;}
 private static void Replay()
 {
  var d=ModelDocument.Parse(Source);var report=ExperimentRunner.Evaluate(d);Require(report.Passed&&report.Replay.SampleHashesMatch&&report.Replay.BoundariesChecked==63);Require(report.Model.Fidelity=="film_evaporation_fired_powertrain"&&report.Model.Calibration=="unverified");
  var asset=AssetCodec.Decode(AssetCodec.Encode(d.ToAsset("Film fired cylinder")));var playback=asset.CreatePlayback();var values=new Scalar[asset.Model.OutputCount];double initial=V(report.Samples[0],16,Field.Mass),thermal=V(report.Samples[0],16,Field.InternalEnergy),cv=287/.35;
  foreach(var s in report.Samples)
  {
   while(playback.TimeNanoseconds<s.TimeNs)Require(playback.Advance(Math.Min(257*asset.Model.StepNanoseconds,s.TimeNs-playback.TimeNanoseconds))==SimulationStatus.Ok);
   Require(playback.ReadSnapshot(values).StateHash.ToString("x16")==s.StateHash&&values.All(v=>v.Value==s.Values[v.Channel.ToString()]));
   double vapor=V(s,16,Field.EvaporatedFuelMass),mass=V(s,16,Field.Mass),heat=V(s,16,Field.FilmWallHeat);
   Near(mass+vapor,initial,1e-16);Near(thermal+heat-V(s,16,Field.InternalEnergy),vapor*cv*400,1e-8);Near(10*(500-V(s,5,Field.Temperature)),heat,1e-8);
   Near(mass+V(s,2,Field.FuelMass)+V(s,15,Field.FuelBurned)-V(s,0,Field.FuelEnergyIn)/44e6,initial,1e-14);
   if(vapor==0)Near(V(s,15,Field.FuelBurned),0,0);
   Near(V(s,0,Field.EnergyResidual),0,1e-6);Near(V(s,0,Field.MassResidual),0,1e-14);Near(V(s,0,Field.FuelResidual),0,1e-14);
  }
  Near(V(report.Samples[^1],16,Field.Mass),0,0);Near(V(report.Samples[^1],16,Field.FilmWallHeat),20,1e-8);Require(V(report.Samples[^1],15,Field.FuelBurned)>0&&V(report.Samples[^1],15,Field.FuelBurned)<initial);
 }
 private static void Contracts()
 {
  JsonNode Film(JsonNode n)=>n["components"]!.AsArray().Single(c=>c!["kind"]!.GetValue<string>()=="fuel_film")!;
  void Reject(Action<JsonNode> mutation,string code){var n=JsonNode.Parse(Source)!;mutation(n);var json=JsonSerializer.SerializeToElement(n);var reply=AgentWorkspace.ValidateModel(json);Require(reply.Error?.Code==code,$"Expected {code}: {reply.Error}");Require(!AgentWorkspace.ExportModelAsset(json).Ok);}
  Reject(n=>Film(n)["parameters"]!.AsObject().Remove("latent_internal_energy"),"invalid_argument");Reject(n=>Film(n)["parameters"]!["latent_internal_energy"]!["unit"]="j","model_unit");
  Reject(n=>Film(n)["parameters"]!["initial_temperature"]!["value"]=500,"model_range");Reject(n=>Film(n)["node_b"]=2,"model_connection");Reject(n=>Film(n)["parameters"]!["conductance"]!["value"]=-1,"model_range");Reject(n=>Film(n)["input_channel"]=105,"invalid_argument");
 }
 private static void Sessions()
 {
  var example=Data(AgentWorkspace.ExampleModel("film-fired-cylinder"));Require(JsonNode.DeepEquals(JsonNode.Parse(Source),JsonNode.Parse(example.GetRawText())));var workspace=new AgentWorkspace();string id=Data(workspace.CreateSession(example)).GetProperty("session_id").GetString()!;string before=Data(workspace.ReadSnapshot(id)).GetRawText();
  Require(workspace.StepSession(id,"1","20000000").Error?.Code=="revision_conflict");Require(workspace.StepSession(id,"0","20000000",new CancellationToken(true)).Error?.Code=="cancelled");Require(Data(workspace.ReadSnapshot(id)).GetRawText()==before);
  Data(workspace.StepSession(id,"0","20000000"));string parent=Data(workspace.ReadSnapshot(id)).GetRawText();string fork=Data(workspace.ForkSession(id,"1")).GetProperty("session_id").GetString()!;Data(workspace.SetInputs(fork,"0",[new("103",0)]));Data(workspace.StepSession(fork,"1","20000000"));Require(Data(workspace.ReadSnapshot(id)).GetRawText()==parent);Data(workspace.CloseSession(fork,"2"));Data(workspace.CloseSession(id,"1"));
 }
}
