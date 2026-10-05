// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Reflection;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Power.Assets;
using System.Security.Cryptography;

var root = new DirectoryInfo(AppContext.BaseDirectory);
while (root is not null && !File.Exists(Path.Combine(root.FullName, "Power.slnx"))) root = root.Parent;
if (root is null) throw new InvalidOperationException("Cannot locate the Power workspace.");
string configuration = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
string server = Path.Combine(root.FullName, "src", "Power.Mcp", "bin", configuration, "net10.0", "Power.Mcp.dll");
string dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(600));
var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "Power integration", Command = dotnet, Arguments = [server], WorkingDirectory = root.FullName
});
await using var client = await McpClient.CreateAsync(transport, cancellationToken: timeout.Token);

void Require(bool value, string message)
{
    if (!value) throw new InvalidOperationException(message);
}
var outputSchemas = new Dictionary<string, JsonElement>();
async Task<JsonElement> Call(string name, Dictionary<string, object?>? args = null, bool expectError = false)
{
    var result = await client.CallToolAsync(name, args, cancellationToken: timeout.Token);
    Require((result.IsError == true) == expectError, name + " unexpected isError: " + JsonSerializer.Serialize(result));
    Require(result.StructuredContent.HasValue, name + " missing structuredContent");
    var reply = result.StructuredContent!.Value;
    if (outputSchemas[name].TryGetProperty("required", out var required))
        foreach (var property in required.EnumerateArray())
            Require(reply.TryGetProperty(property.GetString()!, out _), name + " omitted required output field " + property.GetString());
    Require(reply.GetProperty("schema").GetString() == "power.agent.v1", "Unversioned reply");
    Require(reply.GetProperty("ok").GetBoolean() != expectError, "Reply ok/isError mismatch");
    Require(result.Content.OfType<TextContentBlock>().Any(t => JsonDocument.Parse(t.Text).RootElement.GetProperty("schema").GetString() == "power.agent.v1"),
        "Missing text fallback for structured reply");
    return reply;
}

var tools = await client.ListToolsAsync(cancellationToken: timeout.Token);
Require(tools.Count == 12, "Tool discovery incomplete");
Require(tools.All(t => t.ProtocolTool.OutputSchema.HasValue), "Every tool must advertise an output schema");
foreach (var tool in tools) outputSchemas.Add(tool.Name, tool.ProtocolTool.OutputSchema!.Value);
Console.WriteLine("PASS MCP stdio lifecycle / discovery / input and output schemas");
await Call("get_capabilities");
await Call("get_model_schema");
var example = (await Call("get_example_model")).GetProperty("data");
await Call("validate_model", new() { ["document"] = example });
var badText = example.GetRawText().Replace("kg_m2", "nm");
using var bad = JsonDocument.Parse(badText);
var invalid = await Call("validate_model", new() { ["document"] = bad.RootElement }, expectError: true);
Require(invalid.GetProperty("error").GetProperty("code").GetString() == "model_unit", "Missing actionable diagnostic");
Console.WriteLine("PASS MCP structured validation error / client recovery");
var created = (await Call("create_session", new() { ["document"] = example })).GetProperty("data");
string id = created.GetProperty("session_id").GetString()!;
var updated = (await Call("set_inputs", new()
{
    ["session_id"] = id, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "100", value = 48.0 } }
})).GetProperty("data");
Require(updated.GetProperty("revision").GetString() == "1", "Input revision not incremented");
await Call("step_session", new() { ["session_id"] = id, ["expected_revision"] = "0", ["delta_ns"] = "1000000000" }, true);
var stepped = (await Call("step_session", new() { ["session_id"] = id, ["expected_revision"] = "1", ["delta_ns"] = "1000000000" })).GetProperty("data");
Require(stepped.GetProperty("time_ns").GetString() == "1000000000", "Wrong simulated time");
var branch = (await Call("fork_session", new() { ["session_id"] = id, ["expected_revision"] = "2" })).GetProperty("data");
string child = branch.GetProperty("session_id").GetString()!;
Require(branch.GetProperty("snapshot").GetProperty("state_hash").GetString() == stepped.GetProperty("state_hash").GetString(), "Fork differs from parent");
await Call("read_snapshot", new() { ["session_id"] = child });
await Call("close_session", new() { ["session_id"] = child, ["expected_revision"] = "0" });
await Call("read_snapshot", new() { ["session_id"] = child }, true);
await Call("close_session", new() { ["session_id"] = id, ["expected_revision"] = "2" });
Console.WriteLine("PASS MCP session / input / step / revision conflict / fork / close");
var report = (await Call("run_experiment", new() { ["document"] = example })).GetProperty("data");
Require(report.GetProperty("passed").GetBoolean(), "Reference experiment failed over MCP");
Require(!report.TryGetProperty("samples", out _), "Default report should be compact");
Console.WriteLine("PASS MCP experiment / reproducibility evidence / compact report");
var exported = (await Call("export_model_asset", new() { ["document"] = example, ["name"] = "MCP model" })).GetProperty("data");
byte[] payload = Convert.FromBase64String(exported.GetProperty("content").GetString()!);
Require(Convert.ToHexStringLower(SHA256.HashData(payload)) == exported.GetProperty("asset_sha256").GetString(), "Exported asset hash differs");
var imported = AssetCodec.Decode(payload);
Require(imported.Name == "MCP model" && imported.Model.Fingerprint.ToString("x16") == report.GetProperty("model").GetProperty("fingerprint").GetString(), "Model changed across MCP asset export");
var playback = imported.CreatePlayback();
Require(playback.Advance(imported.DurationNanoseconds) == Power.Core.SimulationStatus.Ok, "Exported experiment could not run");
var snapshot = playback.ReadSnapshot(new Power.Core.Scalar[playback.Model.OutputCount]);
Require(snapshot.StateHash.ToString("x16") == report.GetProperty("final_sample").GetProperty("state_hash").GetString(), "Asset and MCP experiment differ");
Console.WriteLine("PASS MCP portable asset / digest / imported replay");
var cylinder = (await Call("get_example_model", new() { ["name"] = "sealed-cylinder" })).GetProperty("data");
var cylinderReport = (await Call("run_experiment", new() { ["document"] = cylinder })).GetProperty("data");
Require(cylinderReport.GetProperty("passed").GetBoolean(), "Cylinder experiment failed over MCP");
Require(cylinderReport.GetProperty("model").GetProperty("fidelity").GetString() == "sealed_adiabatic_gas", "Cylinder fidelity missing");
var cylinderExport = (await Call("export_model_asset", new() { ["document"] = cylinder, ["name"] = "Sealed cylinder" })).GetProperty("data");
Require(cylinderExport.GetProperty("format").GetString() == "power.asset.v28", "Wrong cylinder asset version");
var cylinderAsset = AssetCodec.Decode(Convert.FromBase64String(cylinderExport.GetProperty("content").GetString()!));
var cylinderPlayback = cylinderAsset.CreatePlayback();
Require(cylinderPlayback.Advance(cylinderAsset.DurationNanoseconds) == Power.Core.SimulationStatus.Ok, "Cylinder playback failed");
Require(cylinderPlayback.ReadSnapshot(new Power.Core.Scalar[cylinderAsset.Model.OutputCount]).StateHash.ToString("x16") ==
    cylinderReport.GetProperty("final_sample").GetProperty("state_hash").GetString(), "Cylinder asset differs from MCP report");
await Call("get_example_model", new() { ["name"] = "unknown" }, true);
Console.WriteLine("PASS MCP cylinder discovery / nonlinear experiment / portable replay / unknown example");
var capabilities = (await Call("get_capabilities")).GetProperty("data");
Require(capabilities.GetProperty("domains").EnumerateArray().Any(d => d.GetString() == "gas"), "Gas domain not discoverable");
var gas = (await Call("get_example_model", new() { ["name"] = "gas-network" })).GetProperty("data");
await Call("validate_model", new() { ["document"] = gas });
var gasReport = (await Call("run_experiment", new() { ["document"] = gas, ["include_samples"] = true })).GetProperty("data");
Require(gasReport.GetProperty("passed").GetBoolean() && gasReport.GetProperty("model").GetProperty("calibration").GetString() == "unverified", "Gas report evidence mismatch");
var gasExport = (await Call("export_model_asset", new() { ["document"] = gas })).GetProperty("data");
byte[] gasPayload = Convert.FromBase64String(gasExport.GetProperty("content").GetString()!);
Require(gasExport.GetProperty("format").GetString() == "power.asset.v28" && Convert.ToHexStringLower(SHA256.HashData(gasPayload)) == gasExport.GetProperty("asset_sha256").GetString(), "Gas asset integrity mismatch");
var gasAsset = AssetCodec.Decode(gasPayload); var gasPlayback = gasAsset.CreatePlayback();
var gasValues = new Power.Core.Scalar[gasAsset.Model.OutputCount];
foreach (var boundary in gasReport.GetProperty("samples").EnumerateArray())
{
    ulong at = boundary.GetProperty("time_ns").GetUInt64();
    while (gasPlayback.TimeNanoseconds < at)
        Require(gasPlayback.Advance(Math.Min(7_300_000, at - gasPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Gas replay failed");
    Require(gasPlayback.ReadSnapshot(gasValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString(), "Gas asset differs at report boundary");
    Require(gasValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Gas channel values differ");
}
var gasSession = (await Call("create_session", new() { ["document"] = gas })).GetProperty("data");
string gasId = gasSession.GetProperty("session_id").GetString()!;
var rejected = await Call("set_inputs", new() { ["session_id"] = gasId, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "100", value = 1.01 } } }, true);
Require(rejected.GetProperty("error").GetProperty("code").GetString() == "invalid_input", "Opening outside range accepted");
var gasUnchanged = (await Call("read_snapshot", new() { ["session_id"] = gasId })).GetProperty("data");
Require(gasUnchanged.GetProperty("state_hash").GetString() == gasSession.GetProperty("snapshot").GetProperty("state_hash").GetString() && gasUnchanged.GetProperty("revision").GetString() == "0", "Rejected gas input changed the session");
await Call("close_session", new() { ["session_id"] = gasId, ["expected_revision"] = "0" });
Console.WriteLine("PASS MCP gas discovery / all asset replay boundaries / opening rejection / unchanged session");
var moving = (await Call("get_example_model", new() { ["name"] = "moving-cylinder" })).GetProperty("data");
var movingValidated = (await Call("validate_model", new() { ["document"] = moving })).GetProperty("data");
Require(movingValidated.GetProperty("model").GetProperty("fidelity").GetString() == "moving_cylinder_gas_exchange", "Moving cylinder not discoverable");
var movingReport = (await Call("run_experiment", new() { ["document"] = moving, ["include_samples"] = true })).GetProperty("data");
Require(movingReport.GetProperty("passed").GetBoolean(), "Moving-cylinder experiment failed");
var movingExport = (await Call("export_model_asset", new() { ["document"] = moving })).GetProperty("data");
Require(movingExport.GetProperty("format").GetString() == "power.asset.v28", "Moving cylinder export must use the current format");
var movingAsset = AssetCodec.Decode(Convert.FromBase64String(movingExport.GetProperty("content").GetString()!));
var movingPlayback = movingAsset.CreatePlayback(); var movingValues = new Power.Core.Scalar[movingAsset.Model.OutputCount];
foreach (var boundary in movingReport.GetProperty("samples").EnumerateArray())
{
    ulong at = boundary.GetProperty("time_ns").GetUInt64();
    if (at > movingPlayback.TimeNanoseconds)
        Require(movingPlayback.Advance(at - movingPlayback.TimeNanoseconds) == Power.Core.SimulationStatus.Ok, "Moving-cylinder playback failed");
    Require(movingPlayback.ReadSnapshot(movingValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString(), "Moving-cylinder replay differs");
    Require(movingValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Moving-cylinder observables differ");
}
Console.WriteLine("PASS MCP moving-cylinder discovery / motored experiment / portable replay");
var timed = (await Call("get_example_model", new() { ["name"] = "crank-timed-cylinder" })).GetProperty("data");
var timedValidated = (await Call("validate_model", new() { ["document"] = timed })).GetProperty("data");
Require(timedValidated.GetProperty("model").GetProperty("fidelity").GetString() == "crank_timed_gas_exchange", "Timed model fidelity missing");
Require(capabilities.GetProperty("valve_timing").GetProperty("profile").GetString() == "sin_squared", "Timing profile missing");
var timedReport = (await Call("run_experiment", new() { ["document"] = timed, ["include_samples"] = true })).GetProperty("data");
Require(timedReport.GetProperty("passed").GetBoolean() && timedReport.GetProperty("samples").GetArrayLength() == 63, "Crank-timed experiment failed");
var timedExport = (await Call("export_model_asset", new() { ["document"] = timed })).GetProperty("data");
Require(timedExport.GetProperty("format").GetString() == "power.asset.v28", "Timing uses current asset v6");
var timedAsset = AssetCodec.Decode(Convert.FromBase64String(timedExport.GetProperty("content").GetString()!));
Require(timedAsset.Components.Count(c => c.ValveTiming != null) == 2, "Timing definitions lost in export");
var timedPlayback = timedAsset.CreatePlayback(); var timedValues = new Power.Core.Scalar[timedAsset.Model.OutputCount];
foreach (var boundary in timedReport.GetProperty("samples").EnumerateArray())
{
    ulong at = boundary.GetProperty("time_ns").GetUInt64();
    while (timedPlayback.TimeNanoseconds < at)
        Require(timedPlayback.Advance(Math.Min(1_350_000, at - timedPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Timed playback failed");
    Require(timedPlayback.ReadSnapshot(timedValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString(), "Timed replay differs");
    Require(timedValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Timed outputs differ");
}
var timedSession = (await Call("create_session", new() { ["document"] = timed })).GetProperty("data");
string timedId = timedSession.GetProperty("session_id").GetString()!;
await Call("set_inputs", new() { ["session_id"] = timedId, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "100", value = 1.01 } } }, true);
var timedUnchanged = (await Call("read_snapshot", new() { ["session_id"] = timedId })).GetProperty("data");
Require(timedUnchanged.GetProperty("revision").GetString() == "0" && timedUnchanged.GetProperty("state_hash").GetString() == timedSession.GetProperty("snapshot").GetProperty("state_hash").GetString(), "Rejected peak opening changed state");
await Call("close_session", new() { ["session_id"] = timedId, ["expected_revision"] = "0" });
Console.WriteLine("PASS MCP crank timing / changing-speed experiment / all portable boundaries / peak input contract");
var fired = (await Call("get_example_model", new() { ["name"] = "fired-cylinder" })).GetProperty("data");
var firedValidated = (await Call("validate_model", new() { ["document"] = fired })).GetProperty("data");
Require(firedValidated.GetProperty("model").GetProperty("fidelity").GetString() == "premixed_wiebe_combustion", "Combustion fidelity missing");
Require(capabilities.GetProperty("combustion").GetProperty("model").GetString() == "prescribed_wiebe", "Burn law not discoverable");
var firedReport = (await Call("run_experiment", new() { ["document"] = fired, ["include_samples"] = true })).GetProperty("data");
Require(firedReport.GetProperty("passed").GetBoolean() && firedReport.GetProperty("samples").GetArrayLength() == 63, "Fired experiment failed");
var firedExport = (await Call("export_model_asset", new() { ["document"] = fired })).GetProperty("data");
Require(firedExport.GetProperty("format").GetString() == "power.asset.v28", "Combustion requires asset v6");
byte[] firedPayload = Convert.FromBase64String(firedExport.GetProperty("content").GetString()!);
Require(Convert.ToHexStringLower(SHA256.HashData(firedPayload)) == firedExport.GetProperty("asset_sha256").GetString(), "Fired asset digest mismatch");
var firedAsset = AssetCodec.Decode(firedPayload); var firedPlayback = firedAsset.CreatePlayback(); var firedValues = new Power.Core.Scalar[firedAsset.Model.OutputCount];
foreach (var boundary in firedReport.GetProperty("samples").EnumerateArray())
{
    ulong at = boundary.GetProperty("time_ns").GetUInt64();
    while (firedPlayback.TimeNanoseconds < at)
        Require(firedPlayback.Advance(Math.Min(1_350_000, at - firedPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Fired playback failed");
    Require(firedPlayback.ReadSnapshot(firedValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString(), "Fired replay differs");
    Require(firedValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Fired observables differ");
}
var firedSession = (await Call("create_session", new() { ["document"] = fired })).GetProperty("data");
string firedId = firedSession.GetProperty("session_id").GetString()!;
await Call("set_inputs", new() { ["session_id"] = firedId, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "103", value = 1.01 } } }, true);
var firedUnchanged = (await Call("read_snapshot", new() { ["session_id"] = firedId })).GetProperty("data");
Require(firedUnchanged.GetProperty("revision").GetString() == "0" && firedUnchanged.GetProperty("state_hash").GetString() == firedSession.GetProperty("snapshot").GetProperty("state_hash").GetString(), "Rejected burn input changed state");
await Call("close_session", new() { ["session_id"] = firedId, ["expected_revision"] = "0" });
Console.WriteLine("PASS MCP combustion discovery / fired load experiment / all asset boundaries / input contract");
var clutch = (await Call("get_example_model", new() { ["name"] = "fired-clutch" })).GetProperty("data");
var clutchValidated = (await Call("validate_model", new() { ["document"] = clutch })).GetProperty("data");
Require(clutchValidated.GetProperty("model").GetProperty("fidelity").GetString() == "hybrid_clutch_powertrain", "Clutch fidelity missing");
Require(capabilities.GetProperty("clutch").GetProperty("input_max").GetDouble() == 1, "Clutch input range missing");
var clutchReport = (await Call("run_experiment", new() { ["document"] = clutch, ["include_samples"] = true })).GetProperty("data");
Require(clutchReport.GetProperty("passed").GetBoolean() && clutchReport.GetProperty("samples").GetArrayLength() == 67, "Fired-clutch experiment failed");
var clutchExport = (await Call("export_model_asset", new() { ["document"] = clutch })).GetProperty("data");
byte[] clutchPayload = Convert.FromBase64String(clutchExport.GetProperty("content").GetString()!);
Require(clutchExport.GetProperty("format").GetString() == "power.asset.v28" && Convert.ToHexStringLower(SHA256.HashData(clutchPayload)) == clutchExport.GetProperty("asset_sha256").GetString(), "Clutch asset version or digest differs");
var clutchAsset = AssetCodec.Decode(clutchPayload); var clutchPlayback = clutchAsset.CreatePlayback(); var clutchValues = new Power.Core.Scalar[clutchAsset.Model.OutputCount];
Require(clutchAsset.Components.Single(c => c.Id == 16).Friction!.StaticCapacity.Value == 100, "Clutch capacity lost");
foreach (var boundary in clutchReport.GetProperty("samples").EnumerateArray())
{
    ulong at = boundary.GetProperty("time_ns").GetUInt64();
    while (clutchPlayback.TimeNanoseconds < at)
        Require(clutchPlayback.Advance(Math.Min(1_350_000, at - clutchPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Clutch playback failed");
    Require(clutchPlayback.ReadSnapshot(clutchValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString(), "Clutch replay differs");
    Require(clutchValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Clutch observables differ");
}
var clutchSession = (await Call("create_session", new() { ["document"] = clutch })).GetProperty("data");
string clutchId = clutchSession.GetProperty("session_id").GetString()!;
await Call("set_inputs", new() { ["session_id"] = clutchId, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "104", value = 1.01 } } }, true);
var clutchUnchanged = (await Call("read_snapshot", new() { ["session_id"] = clutchId })).GetProperty("data");
Require(clutchUnchanged.GetProperty("revision").GetString() == "0" && clutchUnchanged.GetProperty("state_hash").GetString() == clutchSession.GetProperty("snapshot").GetProperty("state_hash").GetString(), "Rejected engagement changed state");
await Call("close_session", new() { ["session_id"] = clutchId, ["expected_revision"] = "0" });
Console.WriteLine("PASS MCP clutch discovery / fired load and heat / every portable boundary / engagement rejection");
var planetary = (await Call("get_example_model", new() { ["name"] = "fired-planetary" })).GetProperty("data");
var planetaryValidated = (await Call("validate_model", new() { ["document"] = planetary })).GetProperty("data");
Require(planetaryValidated.GetProperty("model").GetProperty("fidelity").GetString() == "constrained_gear_powertrain", "Gear fidelity missing");
Require(capabilities.GetProperty("gears").GetProperty("redundant_constraints").GetString() == "reject_at_compile", "Gear rank contract missing");
var planetaryReport = (await Call("run_experiment", new() { ["document"] = planetary, ["include_samples"] = true })).GetProperty("data");
Require(planetaryReport.GetProperty("passed").GetBoolean() && planetaryReport.GetProperty("samples").GetArrayLength() == 84, "Fired planetary experiment failed");
var planetaryExport = (await Call("export_model_asset", new() { ["document"] = planetary })).GetProperty("data");
byte[] planetaryPayload = Convert.FromBase64String(planetaryExport.GetProperty("content").GetString()!);
Require(planetaryExport.GetProperty("format").GetString() == "power.asset.v28" && Convert.ToHexStringLower(SHA256.HashData(planetaryPayload)) == planetaryExport.GetProperty("asset_sha256").GetString(), "Gear asset version or digest differs");
var planetaryAsset = AssetCodec.Decode(planetaryPayload); var planetaryPlayback = planetaryAsset.CreatePlayback(); var planetaryValues = new Power.Core.Scalar[planetaryAsset.Model.OutputCount];
Require(planetaryAsset.Components.Single(c => c.Id == 18).NodeC == 4, "Planetary carrier lost");
foreach (var boundary in planetaryReport.GetProperty("samples").EnumerateArray())
{
    ulong at = boundary.GetProperty("time_ns").GetUInt64();
    while (planetaryPlayback.TimeNanoseconds < at)
        Require(planetaryPlayback.Advance(Math.Min(1_350_000, at - planetaryPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Planetary playback failed");
    Require(planetaryPlayback.ReadSnapshot(planetaryValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString(), "Planetary replay differs");
    Require(planetaryValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Planetary observables differ");
}
var planetarySession = (await Call("create_session", new() { ["document"] = planetary })).GetProperty("data");
string planetaryId = planetarySession.GetProperty("session_id").GetString()!;
await Call("set_inputs", new() { ["session_id"] = planetaryId, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "105", value = 1.01 } } }, true);
var planetaryUnchanged = (await Call("read_snapshot", new() { ["session_id"] = planetaryId })).GetProperty("data");
Require(planetaryUnchanged.GetProperty("revision").GetString() == "0" && planetaryUnchanged.GetProperty("state_hash").GetString() == planetarySession.GetProperty("snapshot").GetProperty("state_hash").GetString(), "Rejected brake input changed state");
await Call("close_session", new() { ["session_id"] = planetaryId, ["expected_revision"] = "0" });
Console.WriteLine("PASS MCP planetary discovery / fired transmission shift / every portable boundary / brake input rejection");
var converter = (await Call("get_example_model", new() { ["name"] = "fired-converter" })).GetProperty("data");
var converterValidated = (await Call("validate_model", new() { ["document"] = converter })).GetProperty("data");
Require(converterValidated.GetProperty("model").GetProperty("fidelity").GetString() == "quasisteady_converter_powertrain", "Converter fidelity missing");
Require(capabilities.GetProperty("converter").GetProperty("maps").GetArrayLength() == 4, "Signed map contract missing");
var converterReport = (await Call("run_experiment", new() { ["document"] = converter, ["include_samples"] = true })).GetProperty("data");
Require(converterReport.GetProperty("passed").GetBoolean() && converterReport.GetProperty("samples").GetArrayLength() == 87, "Fired converter experiment failed");
var converterExport = (await Call("export_model_asset", new() { ["document"] = converter })).GetProperty("data");
byte[] converterPayload = Convert.FromBase64String(converterExport.GetProperty("content").GetString()!);
Require(converterExport.GetProperty("format").GetString() == "power.asset.v28" && Convert.ToHexStringLower(SHA256.HashData(converterPayload)) == converterExport.GetProperty("asset_sha256").GetString(), "Converter asset version or digest differs");
var converterAsset = AssetCodec.Decode(converterPayload); var converterPlayback = converterAsset.CreatePlayback(); var converterValues = new Power.Core.Scalar[converterAsset.Model.OutputCount];
Require(converterAsset.Components.Single(c => c.Id == 20).Converter!.PumpPositive!.Points.Count == 5, "Converter maps lost");
foreach (var boundary in converterReport.GetProperty("samples").EnumerateArray())
{
    ulong at = boundary.GetProperty("time_ns").GetUInt64();
    while (converterPlayback.TimeNanoseconds < at)
        Require(converterPlayback.Advance(Math.Min(1_350_000, at - converterPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Converter playback failed");
    Require(converterPlayback.ReadSnapshot(converterValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString(), "Converter replay differs");
    Require(converterValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Converter observables differ");
}
var converterSession = (await Call("create_session", new() { ["document"] = converter })).GetProperty("data");
string converterId = converterSession.GetProperty("session_id").GetString()!;
await Call("set_inputs", new() { ["session_id"] = converterId, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "106", value = 1.01 } } }, true);
var converterUnchanged = (await Call("read_snapshot", new() { ["session_id"] = converterId })).GetProperty("data");
Require(converterUnchanged.GetProperty("revision").GetString() == "0" && converterUnchanged.GetProperty("state_hash").GetString() == converterSession.GetProperty("snapshot").GetProperty("state_hash").GetString(), "Rejected lockup input changed state");
await Call("close_session", new() { ["session_id"] = converterId, ["expected_revision"] = "0" });
Console.WriteLine("PASS MCP converter discovery / signed maps / fired lockup and shift / every portable boundary / input rejection");
var hydraulic = (await Call("get_example_model", new() { ["name"] = "fired-hydraulic" })).GetProperty("data");
var hydraulicValidated = (await Call("validate_model", new() { ["document"] = hydraulic })).GetProperty("data");
Require(hydraulicValidated.GetProperty("model").GetProperty("fidelity").GetString() == "compliant_hydraulic_powertrain", "Hydraulic fidelity missing");
Require(capabilities.GetProperty("hydraulics").GetProperty("pressure_reference").GetString() == "nonnegative_gauge_common_tank", "Hydraulic pressure contract missing");
var hydraulicReport = (await Call("run_experiment", new() { ["document"] = hydraulic, ["include_samples"] = true })).GetProperty("data");
Require(hydraulicReport.GetProperty("passed").GetBoolean() && hydraulicReport.GetProperty("samples").GetArrayLength() == 89, "Fired hydraulic experiment failed");
var hydraulicExport = (await Call("export_model_asset", new() { ["document"] = hydraulic })).GetProperty("data");
byte[] hydraulicPayload = Convert.FromBase64String(hydraulicExport.GetProperty("content").GetString()!);
Require(hydraulicExport.GetProperty("format").GetString() == "power.asset.v28" && Convert.ToHexStringLower(SHA256.HashData(hydraulicPayload)) == hydraulicExport.GetProperty("asset_sha256").GetString(), "Hydraulic asset version or digest differs");
var hydraulicAsset = AssetCodec.Decode(hydraulicPayload); var hydraulicPlayback = hydraulicAsset.CreatePlayback(); var hydraulicValues = new Power.Core.Scalar[hydraulicAsset.Model.OutputCount];
Require(hydraulicAsset.Components.Single(c => c.Id == 21).HydraulicClutch!.PressureNode == 32, "Hydraulic pressure port lost");
foreach (var boundary in hydraulicReport.GetProperty("samples").EnumerateArray())
{
    ulong at = boundary.GetProperty("time_ns").GetUInt64();
    while (hydraulicPlayback.TimeNanoseconds < at)
        Require(hydraulicPlayback.Advance(Math.Min(1_350_000, at - hydraulicPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Hydraulic playback failed");
    Require(hydraulicPlayback.ReadSnapshot(hydraulicValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString(), "Hydraulic replay differs");
    Require(hydraulicValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Hydraulic observables differ");
}
var hydraulicSession = (await Call("create_session", new() { ["document"] = hydraulic })).GetProperty("data");
string hydraulicId = hydraulicSession.GetProperty("session_id").GetString()!;
await Call("set_inputs", new() { ["session_id"] = hydraulicId, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "106", value = 1.01 } } }, true);
var hydraulicUnchanged = (await Call("read_snapshot", new() { ["session_id"] = hydraulicId })).GetProperty("data");
Require(hydraulicUnchanged.GetProperty("revision").GetString() == "0" && hydraulicUnchanged.GetProperty("state_hash").GetString() == hydraulicSession.GetProperty("snapshot").GetProperty("state_hash").GetString(), "Rejected lockup input changed state");
await Call("close_session", new() { ["session_id"] = hydraulicId, ["expected_revision"] = "0" });
Console.WriteLine("PASS MCP hydraulic discovery / pressure chambers / fired lockup and shift / every portable boundary / input rejection");
var pump = (await Call("get_example_model", new() { ["name"] = "fired-pump" })).GetProperty("data");
var pumpValidated = (await Call("validate_model", new() { ["document"] = pump })).GetProperty("data");
Require(pumpValidated.GetProperty("model").GetProperty("fidelity").GetString() == "shaft_driven_hydraulics", "Pump fidelity missing");
Require(capabilities.GetProperty("hydraulic_pump").GetProperty("displacement_unit").GetString() == "m3_rad", "Pump displacement contract missing");
var pumpReport = (await Call("run_experiment", new() { ["document"] = pump, ["include_samples"] = true })).GetProperty("data");
Require(pumpReport.GetProperty("passed").GetBoolean() && pumpReport.GetProperty("samples").GetArrayLength() == 89, "Fired pump experiment failed");
var pumpExport = (await Call("export_model_asset", new() { ["document"] = pump })).GetProperty("data");
byte[] pumpPayload = Convert.FromBase64String(pumpExport.GetProperty("content").GetString()!);
Require(pumpExport.GetProperty("format").GetString() == "power.asset.v28" && Convert.ToHexStringLower(SHA256.HashData(pumpPayload)) == pumpExport.GetProperty("asset_sha256").GetString(), "Pump asset version or digest differs");
var pumpAsset = AssetCodec.Decode(pumpPayload); var pumpPlayback = pumpAsset.CreatePlayback(); var pumpValues = new Power.Core.Scalar[pumpAsset.Model.OutputCount];
Require(pumpAsset.Components.Single(c => c.Id == 46).HydraulicPump!.Displacement.Unit == Power.Core.Unit.CubicMeterPerRadian, "Pump pressure port lost");
foreach (var boundary in pumpReport.GetProperty("samples").EnumerateArray())
{
    ulong at = boundary.GetProperty("time_ns").GetUInt64();
    while (pumpPlayback.TimeNanoseconds < at)
        Require(pumpPlayback.Advance(Math.Min(1_350_000, at - pumpPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Pump playback failed");
    Require(pumpPlayback.ReadSnapshot(pumpValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString(), "Pump replay differs");
    Require(pumpValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Pump observables differ");
}
var pumpSession = (await Call("create_session", new() { ["document"] = pump })).GetProperty("data");
string pumpId = pumpSession.GetProperty("session_id").GetString()!;
await Call("set_inputs", new() { ["session_id"] = pumpId, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "106", value = 1.01 } } }, true);
var pumpUnchanged = (await Call("read_snapshot", new() { ["session_id"] = pumpId })).GetProperty("data");
Require(pumpUnchanged.GetProperty("revision").GetString() == "0" && pumpUnchanged.GetProperty("state_hash").GetString() == pumpSession.GetProperty("snapshot").GetProperty("state_hash").GetString(), "Rejected lockup input changed state");
await Call("close_session", new() { ["session_id"] = pumpId, ["expected_revision"] = "0" });
Console.WriteLine("PASS MCP pump discovery / pressure chambers / fired lockup and shift / every portable boundary / input rejection");
foreach (var laboratory in new[] { (Name: "fired-pump-losses", Boundaries: 89), (Name: "electric-pump", Boundaries: 106) })
{
    Require(capabilities.GetProperty("pump_assembly").GetProperty("friction_unit").GetString() == "nm_s_rad", "Pump assembly contract missing");
    var document = (await Call("get_example_model", new() { ["name"] = laboratory.Name })).GetProperty("data");
    await Call("validate_model", new() { ["document"] = document });
    var assemblyReport = (await Call("run_experiment", new() { ["document"] = document, ["include_samples"] = true })).GetProperty("data");
    Require(assemblyReport.GetProperty("passed").GetBoolean() && assemblyReport.GetProperty("samples").GetArrayLength() == laboratory.Boundaries, "Pump assembly experiment failed");
    var assemblyExport = (await Call("export_model_asset", new() { ["document"] = document })).GetProperty("data");
    byte[] assemblyPayload = Convert.FromBase64String(assemblyExport.GetProperty("content").GetString()!);
    Require(Convert.ToHexStringLower(SHA256.HashData(assemblyPayload)) == assemblyExport.GetProperty("asset_sha256").GetString(), "Pump assembly payload digest differs");
    var asset = AssetCodec.Decode(assemblyPayload); var assemblyPlayback = asset.CreatePlayback(); var values = new Power.Core.Scalar[asset.Model.OutputCount];
    foreach (var boundary in assemblyReport.GetProperty("samples").EnumerateArray())
    {
        ulong at = boundary.GetProperty("time_ns").GetUInt64();
        while (assemblyPlayback.TimeNanoseconds < at)
            Require(assemblyPlayback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, at - assemblyPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Pump assembly playback failed");
        Require(assemblyPlayback.ReadSnapshot(values).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString(), "Pump assembly replay hash differs");
        Require(values.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Pump assembly outputs differ");
    }
    var createdAssembly = (await Call("create_session", new() { ["document"] = document })).GetProperty("data");
    string assemblyId = createdAssembly.GetProperty("session_id").GetString()!;
    await Call("step_session", new() { ["session_id"] = assemblyId, ["expected_revision"] = "1", ["delta_ns"] = "10000000" }, true);
    var unchanged = (await Call("read_snapshot", new() { ["session_id"] = assemblyId })).GetProperty("data");
    Require(unchanged.GetProperty("revision").GetString() == "0" && unchanged.GetProperty("state_hash").GetString() == createdAssembly.GetProperty("snapshot").GetProperty("state_hash").GetString(), "Stale assembly step changed state");
    await Call("close_session", new() { ["session_id"] = assemblyId, ["expected_revision"] = "0" });
    Console.WriteLine("PASS MCP " + laboratory.Name + " / physical contracts / every portable boundary / revisions");
}
var regulatedDocument = (await Call("get_example_model", new() { ["name"] = "pressure-regulated-pump" })).GetProperty("data");
Require(capabilities.GetProperty("pressure_controller").GetProperty("units").GetProperty("integral_gain").GetString() == "v_pa_s", "Pressure PI contract missing");
var regulatedValidation = (await Call("validate_model", new() { ["document"] = regulatedDocument })).GetProperty("data");
Require(regulatedValidation.GetProperty("model").GetProperty("fidelity").GetString() == "sampled_pressure_control", "Controlled fidelity missing");
var regulatedReport = (await Call("run_experiment", new() { ["document"] = regulatedDocument, ["include_samples"] = true })).GetProperty("data");
Require(regulatedReport.GetProperty("passed").GetBoolean() && regulatedReport.GetProperty("samples").GetArrayLength() == 757, "Pressure-regulated experiment failed");
var regulatedExport = (await Call("export_model_asset", new() { ["document"] = regulatedDocument })).GetProperty("data");
byte[] regulatedPayload = Convert.FromBase64String(regulatedExport.GetProperty("content").GetString()!);
Require(regulatedExport.GetProperty("format").GetString() == "power.asset.v28" &&
    Convert.ToHexStringLower(SHA256.HashData(regulatedPayload)) == regulatedExport.GetProperty("asset_sha256").GetString(), "Controlled asset integrity differs");
var regulatedAsset = AssetCodec.Decode(regulatedPayload); var regulatedPlayback = regulatedAsset.CreatePlayback(); var regulatedValues = new Power.Core.Scalar[regulatedAsset.Model.OutputCount];
Require(regulatedAsset.Components.Single(c => c.Id == 20).PressureController!.SamplePeriodNanoseconds == 5_000_000, "Control period lost");
foreach (var boundary in regulatedReport.GetProperty("samples").EnumerateArray())
{
    ulong at = boundary.GetProperty("time_ns").GetUInt64();
    while (regulatedPlayback.TimeNanoseconds < at)
        Require(regulatedPlayback.Advance(Math.Min(257 * regulatedAsset.Model.StepNanoseconds, at - regulatedPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Regulated playback failed");
    Require(regulatedPlayback.ReadSnapshot(regulatedValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString(), "Controller state replay differs");
    Require(regulatedValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Controlled channels differ");
}
var regulatedSession = (await Call("create_session", new() { ["document"] = regulatedDocument })).GetProperty("data");
string regulatedId = regulatedSession.GetProperty("session_id").GetString()!;
var owned = await Call("set_inputs", new() { ["session_id"] = regulatedId, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "100", value = 0 } } }, true);
Require(owned.GetProperty("error").GetProperty("code").GetString() == "controlled_input", "Owned actuator error missing");
var regulatedBefore = (await Call("read_snapshot", new() { ["session_id"] = regulatedId })).GetProperty("data");
Require(regulatedBefore.GetProperty("revision").GetString() == "0" && regulatedBefore.GetProperty("state_hash").GetString() == regulatedSession.GetProperty("snapshot").GetProperty("state_hash").GetString(), "Rejected actuator write changed state");
await Call("set_inputs", new() { ["session_id"] = regulatedId, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "105", value = 350000 } } });
await Call("step_session", new() { ["session_id"] = regulatedId, ["expected_revision"] = "1", ["delta_ns"] = "20000000" });
var regulatedParent = (await Call("read_snapshot", new() { ["session_id"] = regulatedId })).GetProperty("data");
string regulatedFork = (await Call("fork_session", new() { ["session_id"] = regulatedId, ["expected_revision"] = "2" })).GetProperty("data").GetProperty("session_id").GetString()!;
await Call("set_inputs", new() { ["session_id"] = regulatedFork, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "105", value = 100000 } } });
await Call("step_session", new() { ["session_id"] = regulatedFork, ["expected_revision"] = "1", ["delta_ns"] = "20000000" });
Require((await Call("read_snapshot", new() { ["session_id"] = regulatedId })).GetProperty("data").GetRawText() == regulatedParent.GetRawText(), "Controller fork changed its parent");
await Call("close_session", new() { ["session_id"] = regulatedFork, ["expected_revision"] = "2" });
await Call("close_session", new() { ["session_id"] = regulatedId, ["expected_revision"] = "2" });
Console.WriteLine("PASS MCP sampled pressure control / complete asset replay / owned actuator / controller forks");
var batteryDocument = (await Call("get_example_model", new() { ["name"] = "battery-regulated-pump" })).GetProperty("data");
Require(capabilities.GetProperty("battery").GetProperty("state_of_charge_max").GetInt32() == 1, "Finite battery contract missing");
var batteryValidation = (await Call("validate_model", new() { ["document"] = batteryDocument })).GetProperty("data");
Require(batteryValidation.GetProperty("model").GetProperty("fidelity").GetString() == "battery_pressure_control", "Battery fidelity missing");
var batteryReport = (await Call("run_experiment", new() { ["document"] = batteryDocument, ["include_samples"] = true })).GetProperty("data");
Require(batteryReport.GetProperty("passed").GetBoolean() && batteryReport.GetProperty("samples").GetArrayLength() == 761, "Battery experiment failed");
var batteryExport = (await Call("export_model_asset", new() { ["document"] = batteryDocument })).GetProperty("data");
byte[] batteryPayload = Convert.FromBase64String(batteryExport.GetProperty("content").GetString()!);
Require(batteryExport.GetProperty("format").GetString() == "power.asset.v28" && Convert.ToHexStringLower(SHA256.HashData(batteryPayload)) == batteryExport.GetProperty("asset_sha256").GetString(), "Battery payload integrity differs");
var batteryAsset = AssetCodec.Decode(batteryPayload); var batteryPlayback = batteryAsset.CreatePlayback(); var batteryValues = new Power.Core.Scalar[batteryAsset.Model.OutputCount];
Require(batteryAsset.Nodes.Single(n => n.Id == 30).Battery!.PolarizationCapacitance.Unit == Power.Core.Unit.Farad, "Polarization parameters lost");
foreach (var boundary in batteryReport.GetProperty("samples").EnumerateArray())
{
    ulong at = boundary.GetProperty("time_ns").GetUInt64();
    while (batteryPlayback.TimeNanoseconds < at)
        Require(batteryPlayback.Advance(Math.Min(257 * batteryAsset.Model.StepNanoseconds, at - batteryPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Battery playback failed");
    Require(batteryPlayback.ReadSnapshot(batteryValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString(), "Battery state replay differs");
    Require(batteryValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Battery observables differ");
}
var batteryCreated = (await Call("create_session", new() { ["document"] = batteryDocument })).GetProperty("data");
string batterySessionId = batteryCreated.GetProperty("session_id").GetString()!;
var batteryOwned = await Call("set_inputs", new() { ["session_id"] = batterySessionId, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "100", value = .5 } } }, true);
Require(batteryOwned.GetProperty("error").GetProperty("code").GetString() == "controlled_input", "Duty owner error missing");
await Call("set_inputs", new() { ["session_id"] = batterySessionId, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "106", value = 1.1 } } }, true);
var batteryUnchanged = (await Call("read_snapshot", new() { ["session_id"] = batterySessionId })).GetProperty("data");
Require(batteryUnchanged.GetProperty("revision").GetString() == "0" && batteryUnchanged.GetProperty("state_hash").GetString() == batteryCreated.GetProperty("snapshot").GetProperty("state_hash").GetString(), "Rejected battery writes changed state");
await Call("step_session", new() { ["session_id"] = batterySessionId, ["expected_revision"] = "0", ["delta_ns"] = "20000000" });
var batteryParent = (await Call("read_snapshot", new() { ["session_id"] = batterySessionId })).GetProperty("data");
string batteryFork = (await Call("fork_session", new() { ["session_id"] = batterySessionId, ["expected_revision"] = "1" })).GetProperty("data").GetProperty("session_id").GetString()!;
await Call("set_inputs", new() { ["session_id"] = batteryFork, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "106", value = 1 } } });
await Call("step_session", new() { ["session_id"] = batteryFork, ["expected_revision"] = "1", ["delta_ns"] = "20000000" });
Require((await Call("read_snapshot", new() { ["session_id"] = batterySessionId })).GetProperty("data").GetRawText() == batteryParent.GetRawText(), "Battery/controller fork changed parent");
await Call("close_session", new() { ["session_id"] = batteryFork, ["expected_revision"] = "2" });
await Call("close_session", new() { ["session_id"] = batterySessionId, ["expected_revision"] = "1" });
Console.WriteLine("PASS MCP finite battery / charge and heat / complete replay / duty ownership / independent forks");
var pistonDocument = (await Call("get_example_model", new() { ["name"] = "piston-actuated-clutch" })).GetProperty("data");
Require(capabilities.GetProperty("hydraulic_piston").GetProperty("slider_domain").GetString() == "translational", "Piston contract missing");
var pistonValidation = (await Call("validate_model", new() { ["document"] = pistonDocument })).GetProperty("data");
Require(pistonValidation.GetProperty("model").GetProperty("fidelity").GetString() == "dynamic_piston_powertrain", "Piston fidelity missing");
var pistonReport = (await Call("run_experiment", new() { ["document"] = pistonDocument, ["include_samples"] = true })).GetProperty("data");
Require(pistonReport.GetProperty("passed").GetBoolean() && pistonReport.GetProperty("samples").GetArrayLength() == 761, "Piston experiment failed");
var pistonExport = (await Call("export_model_asset", new() { ["document"] = pistonDocument })).GetProperty("data");
byte[] pistonPayload = Convert.FromBase64String(pistonExport.GetProperty("content").GetString()!);
Require(pistonExport.GetProperty("format").GetString() == "power.asset.v28" && Convert.ToHexStringLower(SHA256.HashData(pistonPayload)) == pistonExport.GetProperty("asset_sha256").GetString(), "Piston payload integrity differs");
var pistonAsset = AssetCodec.Decode(pistonPayload); var pistonPlayback = pistonAsset.CreatePlayback(); var pistonValues = new Power.Core.Scalar[pistonAsset.Model.OutputCount];
Require(pistonAsset.Components.Single(c => c.Id == 17).PistonClutch!.PistonComponent == 22, "Contact ownership lost");
foreach (var boundary in pistonReport.GetProperty("samples").EnumerateArray())
{
    ulong at = boundary.GetProperty("time_ns").GetUInt64();
    while (pistonPlayback.TimeNanoseconds < at)
        Require(pistonPlayback.Advance(Math.Min(257 * pistonAsset.Model.StepNanoseconds, at - pistonPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Piston playback failed");
    Require(pistonPlayback.ReadSnapshot(pistonValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString(), "Piston state replay differs");
    Require(pistonValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Piston observables differ");
    double x = boundary.GetProperty("values").GetProperty(Power.Core.Channels.Output(40, Power.Core.Field.Displacement).ToString()).GetDouble();
    double force = boundary.GetProperty("values").GetProperty(Power.Core.Channels.Output(17, Power.Core.Field.ClampForce).ToString()).GetDouble();
    Require(Math.Abs(force - 1e6 * Math.Max(0, x - .002)) < 1e-10, "Clutch force must follow pad contact");
}
var pistonCreated = (await Call("create_session", new() { ["document"] = pistonDocument })).GetProperty("data");
string pistonSessionId = pistonCreated.GetProperty("session_id").GetString()!;
await Call("step_session", new() { ["session_id"] = pistonSessionId, ["expected_revision"] = "1", ["delta_ns"] = "20000000" }, true);
Require((await Call("read_snapshot", new() { ["session_id"] = pistonSessionId })).GetProperty("data").GetProperty("state_hash").GetString() == pistonCreated.GetProperty("snapshot").GetProperty("state_hash").GetString(), "Revision conflict changed piston state");
await Call("step_session", new() { ["session_id"] = pistonSessionId, ["expected_revision"] = "0", ["delta_ns"] = "20000000" });
var pistonParent = (await Call("read_snapshot", new() { ["session_id"] = pistonSessionId })).GetProperty("data");
string pistonFork = (await Call("fork_session", new() { ["session_id"] = pistonSessionId, ["expected_revision"] = "1" })).GetProperty("data").GetProperty("session_id").GetString()!;
await Call("set_inputs", new() { ["session_id"] = pistonFork, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "101", value = 1 } } });
await Call("step_session", new() { ["session_id"] = pistonFork, ["expected_revision"] = "1", ["delta_ns"] = "100000000" });
Require((await Call("read_snapshot", new() { ["session_id"] = pistonSessionId })).GetProperty("data").GetRawText() == pistonParent.GetRawText(), "Piston fork changed parent");
await Call("close_session", new() { ["session_id"] = pistonFork, ["expected_revision"] = "2" });
await Call("close_session", new() { ["session_id"] = pistonSessionId, ["expected_revision"] = "1" });
Console.WriteLine("PASS MCP dynamic piston / contact-derived force / every portable boundary / revision and independent fork");
var spoolDocument = (await Call("get_example_model", new() { ["name"] = "spool-regulated-pump" })).GetProperty("data");
Require(capabilities.GetProperty("hydraulic_spool_valve").GetProperty("position_unit").GetString() == "m", "Spool contract missing");
var spoolReport = (await Call("run_experiment", new() { ["document"] = spoolDocument, ["include_samples"] = true })).GetProperty("data");
Require(spoolReport.GetProperty("passed").GetBoolean() && spoolReport.GetProperty("samples").GetArrayLength() == 156, "Spool experiment failed");
var spoolExport = (await Call("export_model_asset", new() { ["document"] = spoolDocument })).GetProperty("data");
byte[] spoolPayload = Convert.FromBase64String(spoolExport.GetProperty("content").GetString()!);
Require(spoolExport.GetProperty("format").GetString() == "power.asset.v28" && Convert.ToHexStringLower(SHA256.HashData(spoolPayload)) == spoolExport.GetProperty("asset_sha256").GetString(), "Spool asset integrity differs");
var spoolAsset = AssetCodec.Decode(spoolPayload); var spoolPlayback = spoolAsset.CreatePlayback(); var spoolValues = new Power.Core.Scalar[spoolAsset.Model.OutputCount];
Require(spoolAsset.Model.HasSpoolValves && spoolAsset.Model.Fidelity == "mechanically_regulated_hydraulics" && spoolAsset.Components.Single(c => c.Id == 14).SpoolValve!.PistonComponent == 21, "Spool topology lost");
foreach (var boundary in spoolReport.GetProperty("samples").EnumerateArray())
{
    ulong at = boundary.GetProperty("time_ns").GetUInt64();
    while (spoolPlayback.TimeNanoseconds < at)
        Require(spoolPlayback.Advance(Math.Min(257 * spoolAsset.Model.StepNanoseconds, at - spoolPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Spool playback failed");
    Require(spoolPlayback.ReadSnapshot(spoolValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString(), "Spool replay hash differs");
    Require(spoolValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Spool observables differ");
}
var spoolCreated = (await Call("create_session", new() { ["document"] = spoolDocument })).GetProperty("data");
string spoolSessionId = spoolCreated.GetProperty("session_id").GetString()!;
await Call("step_session", new() { ["session_id"] = spoolSessionId, ["expected_revision"] = "1", ["delta_ns"] = "20000000" }, true);
await Call("set_inputs", new() { ["session_id"] = spoolSessionId, ["expected_revision"] = "0", ["values"] = new[] { new { channel = Power.Core.Channels.Output(14, Power.Core.Field.Opening).ToString(), value = 1 } } }, true);
Require((await Call("read_snapshot", new() { ["session_id"] = spoolSessionId })).GetProperty("data").GetProperty("state_hash").GetString() == spoolCreated.GetProperty("snapshot").GetProperty("state_hash").GetString(), "Rejected spool write changed state");
await Call("close_session", new() { ["session_id"] = spoolSessionId, ["expected_revision"] = "0" });
Console.WriteLine("PASS MCP mechanical spool regulation / passive metering / complete asset replay / physical opening ownership");
var accumulatorDocument = (await Call("get_example_model", new() { ["name"] = "gas-accumulator-pump" })).GetProperty("data");
Require(capabilities.GetProperty("gas_piston").GetProperty("motion_domain").GetString() == "translational", "Gas piston contract missing");
var accumulatorReport = (await Call("run_experiment", new() { ["document"] = accumulatorDocument, ["include_samples"] = true })).GetProperty("data");
Require(accumulatorReport.GetProperty("passed").GetBoolean() && accumulatorReport.GetProperty("samples").GetArrayLength() == 306, "Accumulator experiment failed");
var accumulatorExport = (await Call("export_model_asset", new() { ["document"] = accumulatorDocument })).GetProperty("data");
byte[] accumulatorPayload = Convert.FromBase64String(accumulatorExport.GetProperty("content").GetString()!);
Require(accumulatorExport.GetProperty("format").GetString() == "power.asset.v28" && Convert.ToHexStringLower(SHA256.HashData(accumulatorPayload)) == accumulatorExport.GetProperty("asset_sha256").GetString(), "Accumulator asset integrity differs");
var accumulatorAsset = AssetCodec.Decode(accumulatorPayload); var accumulatorPlayback = accumulatorAsset.CreatePlayback(); var accumulatorValues = new Power.Core.Scalar[accumulatorAsset.Model.OutputCount];
Require(accumulatorAsset.Model.HasGasPistons && accumulatorAsset.Model.Fidelity == "gas_accumulator_powertrain", "Gas-fluid geometry lost");
foreach (var boundary in accumulatorReport.GetProperty("samples").EnumerateArray())
{
    ulong at = boundary.GetProperty("time_ns").GetUInt64();
    while (accumulatorPlayback.TimeNanoseconds < at)
        Require(accumulatorPlayback.Advance(Math.Min(257 * accumulatorAsset.Model.StepNanoseconds, at - accumulatorPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Accumulator playback failed");
    Require(accumulatorPlayback.ReadSnapshot(accumulatorValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString(), "Accumulator replay hash differs");
    Require(accumulatorValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Accumulator observables differ");
}
var accumulatorCreated = (await Call("create_session", new() { ["document"] = accumulatorDocument })).GetProperty("data");
string accumulatorId = accumulatorCreated.GetProperty("session_id").GetString()!;
await Call("step_session", new() { ["session_id"] = accumulatorId, ["expected_revision"] = "1", ["delta_ns"] = "20000000" }, true);
Require((await Call("read_snapshot", new() { ["session_id"] = accumulatorId })).GetProperty("data").GetProperty("state_hash").GetString() == accumulatorCreated.GetProperty("snapshot").GetProperty("state_hash").GetString(), "Accumulator revision conflict changed state");
await Call("step_session", new() { ["session_id"] = accumulatorId, ["expected_revision"] = "0", ["delta_ns"] = "20000000" });
var accumulatorParent = (await Call("read_snapshot", new() { ["session_id"] = accumulatorId })).GetProperty("data");
string accumulatorFork = (await Call("fork_session", new() { ["session_id"] = accumulatorId, ["expected_revision"] = "1" })).GetProperty("data").GetProperty("session_id").GetString()!;
await Call("set_inputs", new() { ["session_id"] = accumulatorFork, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "100", value = 6 } } });
await Call("step_session", new() { ["session_id"] = accumulatorFork, ["expected_revision"] = "1", ["delta_ns"] = "20000000" });
Require((await Call("read_snapshot", new() { ["session_id"] = accumulatorId })).GetProperty("data").GetRawText() == accumulatorParent.GetRawText(), "Gas-fluid fork changed parent");
await Call("close_session", new() { ["session_id"] = accumulatorFork, ["expected_revision"] = "2" });
await Call("close_session", new() { ["session_id"] = accumulatorId, ["expected_revision"] = "1" });
Console.WriteLine("PASS MCP gas accumulator / actual energy and temperature / complete asset replay / gas-fluid fork independence");
var meteredDocument=(await Call("get_example_model",new(){["name"]="metered-fired-cylinder"})).GetProperty("data");
Require(capabilities.GetProperty("gas_fuel_injector").GetProperty("input_unit").GetString()=="kg","Fuel dose contract missing");
var meteredReport=(await Call("run_experiment",new(){["document"]=meteredDocument,["include_samples"]=true})).GetProperty("data");
Require(meteredReport.GetProperty("passed").GetBoolean()&&meteredReport.GetProperty("samples").GetArrayLength()==65,"Metered burn experiment failed");
var meteredExport=(await Call("export_model_asset",new(){["document"]=meteredDocument})).GetProperty("data");byte[] meteredPayload=Convert.FromBase64String(meteredExport.GetProperty("content").GetString()!);
Require(meteredExport.GetProperty("format").GetString()=="power.asset.v28"&&Convert.ToHexStringLower(SHA256.HashData(meteredPayload))==meteredExport.GetProperty("asset_sha256").GetString(),"Metered asset integrity differs");
var meteredAsset=AssetCodec.Decode(meteredPayload);var meteredPlayback=meteredAsset.CreatePlayback();var meteredValues=new Power.Core.Scalar[meteredAsset.Model.OutputCount];
Require(meteredAsset.Model.HasFuelInjectors&&meteredAsset.Model.Fidelity=="metered_fired_powertrain","Metered topology missing");
foreach(var boundary in meteredReport.GetProperty("samples").EnumerateArray())
{
    ulong at=boundary.GetProperty("time_ns").GetUInt64();while(meteredPlayback.TimeNanoseconds<at)Require(meteredPlayback.Advance(Math.Min(257*meteredAsset.Model.StepNanoseconds,at-meteredPlayback.TimeNanoseconds))==Power.Core.SimulationStatus.Ok,"Metered playback failed");
    Require(meteredPlayback.ReadSnapshot(meteredValues).StateHash.ToString("x16")==boundary.GetProperty("state_hash").GetString()&&meteredValues.All(v=>v.Value==boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()),"Metered fuel history differs");
}
var meteredCreated=(await Call("create_session",new(){["document"]=meteredDocument})).GetProperty("data");string meteredId=meteredCreated.GetProperty("session_id").GetString()!;
await Call("set_inputs",new(){["session_id"]=meteredId,["expected_revision"]="0",["values"]=new[]{new{channel="104",value=.001}}},true);
Require((await Call("read_snapshot",new(){["session_id"]=meteredId})).GetProperty("data").GetProperty("state_hash").GetString()==meteredCreated.GetProperty("snapshot").GetProperty("state_hash").GetString(),"Rejected quota changed fuel state");
await Call("step_session",new(){["session_id"]=meteredId,["expected_revision"]="0",["delta_ns"]="20000000"});var meteredParent=(await Call("read_snapshot",new(){["session_id"]=meteredId})).GetProperty("data");
string meteredFork=(await Call("fork_session",new(){["session_id"]=meteredId,["expected_revision"]="1"})).GetProperty("data").GetProperty("session_id").GetString()!;
await Call("set_inputs",new(){["session_id"]=meteredFork,["expected_revision"]="0",["values"]=new[]{new{channel="104",value=0.0}}});await Call("step_session",new(){["session_id"]=meteredFork,["expected_revision"]="1",["delta_ns"]="20000000"});
Require((await Call("read_snapshot",new(){["session_id"]=meteredId})).GetProperty("data").GetRawText()==meteredParent.GetRawText(),"Fuel-controller fork changed parent");
await Call("close_session",new(){["session_id"]=meteredFork,["expected_revision"]="2"});await Call("close_session",new(){["session_id"]=meteredId,["expected_revision"]="1"});
Console.WriteLine("PASS MCP finite fuel rail / cycle dose contract / every export boundary / invalid quota and independent fork");
var filmDocument=(await Call("get_example_model",new(){["name"]="film-fired-cylinder"})).GetProperty("data");Require(capabilities.GetProperty("fuel_film").GetProperty("wall").GetString()=="Finite thermal node","Film contract missing");
var filmReport=(await Call("run_experiment",new(){["document"]=filmDocument,["include_samples"]=true})).GetProperty("data");Require(filmReport.GetProperty("passed").GetBoolean()&&filmReport.GetProperty("samples").GetArrayLength()==63,"Film experiment failed");
var filmExport=(await Call("export_model_asset",new(){["document"]=filmDocument})).GetProperty("data");byte[] filmPayload=Convert.FromBase64String(filmExport.GetProperty("content").GetString()!);Require(filmExport.GetProperty("format").GetString()=="power.asset.v28"&&Convert.ToHexStringLower(SHA256.HashData(filmPayload))==filmExport.GetProperty("asset_sha256").GetString(),"Film asset digest differs");
var filmAsset=AssetCodec.Decode(filmPayload);var filmPlayback=filmAsset.CreatePlayback();var filmValues=new Power.Core.Scalar[filmAsset.Model.OutputCount];Require(filmAsset.Model.HasFuelFilms&&filmAsset.Model.Fidelity=="film_evaporation_fired_powertrain","Phase model lost");
foreach(var boundary in filmReport.GetProperty("samples").EnumerateArray())
{ulong at=boundary.GetProperty("time_ns").GetUInt64();while(filmPlayback.TimeNanoseconds<at)Require(filmPlayback.Advance(Math.Min(257*filmAsset.Model.StepNanoseconds,at-filmPlayback.TimeNanoseconds))==Power.Core.SimulationStatus.Ok,"Film playback failed");Require(filmPlayback.ReadSnapshot(filmValues).StateHash.ToString("x16")==boundary.GetProperty("state_hash").GetString()&&filmValues.All(v=>v.Value==boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()),"Phase history differs");}
var filmCreated=(await Call("create_session",new(){["document"]=filmDocument})).GetProperty("data");string filmId=filmCreated.GetProperty("session_id").GetString()!;await Call("step_session",new(){["session_id"]=filmId,["expected_revision"]="1",["delta_ns"]="20000000"},true);Require((await Call("read_snapshot",new(){["session_id"]=filmId})).GetProperty("data").GetProperty("state_hash").GetString()==filmCreated.GetProperty("snapshot").GetProperty("state_hash").GetString(),"Film conflict mutated state");
await Call("step_session",new(){["session_id"]=filmId,["expected_revision"]="0",["delta_ns"]="20000000"});var filmParent=(await Call("read_snapshot",new(){["session_id"]=filmId})).GetProperty("data");string filmFork=(await Call("fork_session",new(){["session_id"]=filmId,["expected_revision"]="1"})).GetProperty("data").GetProperty("session_id").GetString()!;await Call("set_inputs",new(){["session_id"]=filmFork,["expected_revision"]="0",["values"]=new[]{new{channel="103",value=0.0}}});await Call("step_session",new(){["session_id"]=filmFork,["expected_revision"]="1",["delta_ns"]="20000000"});Require((await Call("read_snapshot",new(){["session_id"]=filmId})).GetProperty("data").GetRawText()==filmParent.GetRawText(),"Film fork changed parent");await Call("close_session",new(){["session_id"]=filmFork,["expected_revision"]="2"});await Call("close_session",new(){["session_id"]=filmId,["expected_revision"]="1"});
Console.WriteLine("PASS MCP finite liquid phase / heat budget / vapor-only reactant / every export boundary / phase fork");
var liquidDocument=(await Call("get_example_model",new(){["name"]="liquid-injected-cylinder"})).GetProperty("data");Require(capabilities.GetProperty("liquid_fuel_injector").GetProperty("density_unit").GetString()=="kg_m3","Liquid contract missing");
var liquidReport=(await Call("run_experiment",new(){["document"]=liquidDocument,["include_samples"]=true})).GetProperty("data");Require(liquidReport.GetProperty("passed").GetBoolean()&&liquidReport.GetProperty("samples").GetArrayLength()==65,"Liquid experiment failed");
var liquidExport=(await Call("export_model_asset",new(){["document"]=liquidDocument})).GetProperty("data");byte[] liquidPayload=Convert.FromBase64String(liquidExport.GetProperty("content").GetString()!);Require(liquidExport.GetProperty("format").GetString()=="power.asset.v28"&&Convert.ToHexStringLower(SHA256.HashData(liquidPayload))==liquidExport.GetProperty("asset_sha256").GetString(),"Liquid asset digest differs");
var liquidAsset=AssetCodec.Decode(liquidPayload);var liquidPlayback=liquidAsset.CreatePlayback();var liquidValues=new Power.Core.Scalar[liquidAsset.Model.OutputCount];Require(liquidAsset.Model.HasLiquidFuelInjectors&&liquidAsset.Model.Fidelity=="liquid_injected_fired_powertrain","Phase model lost");
foreach(var boundary in liquidReport.GetProperty("samples").EnumerateArray())
{ulong at=boundary.GetProperty("time_ns").GetUInt64();while(liquidPlayback.TimeNanoseconds<at)Require(liquidPlayback.Advance(Math.Min(257*liquidAsset.Model.StepNanoseconds,at-liquidPlayback.TimeNanoseconds))==Power.Core.SimulationStatus.Ok,"Liquid playback failed");Require(liquidPlayback.ReadSnapshot(liquidValues).StateHash.ToString("x16")==boundary.GetProperty("state_hash").GetString()&&liquidValues.All(v=>v.Value==boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()),"Phase history differs");}
var liquidCreated=(await Call("create_session",new(){["document"]=liquidDocument})).GetProperty("data");string liquidId=liquidCreated.GetProperty("session_id").GetString()!;await Call("step_session",new(){["session_id"]=liquidId,["expected_revision"]="1",["delta_ns"]="20000000"},true);Require((await Call("read_snapshot",new(){["session_id"]=liquidId})).GetProperty("data").GetProperty("state_hash").GetString()==liquidCreated.GetProperty("snapshot").GetProperty("state_hash").GetString(),"Liquid conflict mutated state");
await Call("step_session",new(){["session_id"]=liquidId,["expected_revision"]="0",["delta_ns"]="20000000"});var liquidParent=(await Call("read_snapshot",new(){["session_id"]=liquidId})).GetProperty("data");string liquidFork=(await Call("fork_session",new(){["session_id"]=liquidId,["expected_revision"]="1"})).GetProperty("data").GetProperty("session_id").GetString()!;await Call("set_inputs",new(){["session_id"]=liquidFork,["expected_revision"]="0",["values"]=new[]{new{channel="104",value=0.0}}});await Call("step_session",new(){["session_id"]=liquidFork,["expected_revision"]="1",["delta_ns"]="20000000"});Require((await Call("read_snapshot",new(){["session_id"]=liquidId})).GetProperty("data").GetRawText()==liquidParent.GetRawText(),"Liquid fork changed parent");await Call("close_session",new(){["session_id"]=liquidFork,["expected_revision"]="2"});await Call("close_session",new(){["session_id"]=liquidId,["expected_revision"]="1"});
Console.WriteLine("PASS MCP finite compliant liquid rail / pressure heat and vapor-only reaction / every export boundary / independent fork");
var needleDocument=(await Call("get_example_model",new(){["name"]="needle-actuated-cylinder"})).GetProperty("data");Require(capabilities.GetProperty("needle_actuation").GetProperty("gradient_unit").GetString()=="h_m","Needle contract missing");
var needleReport=(await Call("run_experiment",new(){["document"]=needleDocument,["include_samples"]=true})).GetProperty("data");Require(needleReport.GetProperty("passed").GetBoolean()&&needleReport.GetProperty("samples").GetArrayLength()==65,"Needle experiment failed");
var needleExport=(await Call("export_model_asset",new(){["document"]=needleDocument})).GetProperty("data");byte[] needlePayload=Convert.FromBase64String(needleExport.GetProperty("content").GetString()!);Require(needleExport.GetProperty("format").GetString()=="power.asset.v28"&&Convert.ToHexStringLower(SHA256.HashData(needlePayload))==needleExport.GetProperty("asset_sha256").GetString(),"Needle asset digest differs");
var needleAsset=AssetCodec.Decode(needlePayload);var needlePlayback=needleAsset.CreatePlayback();var needleValues=new Power.Core.Scalar[needleAsset.Model.OutputCount];Require(needleAsset.Model.HasNeedleInjectors&&needleAsset.Model.Fidelity=="needle_actuated_fired_powertrain","Phase model lost");
foreach(var boundary in needleReport.GetProperty("samples").EnumerateArray())
{ulong at=boundary.GetProperty("time_ns").GetUInt64();while(needlePlayback.TimeNanoseconds<at)Require(needlePlayback.Advance(Math.Min(257*needleAsset.Model.StepNanoseconds,at-needlePlayback.TimeNanoseconds))==Power.Core.SimulationStatus.Ok,"Needle playback failed");Require(needlePlayback.ReadSnapshot(needleValues).StateHash.ToString("x16")==boundary.GetProperty("state_hash").GetString()&&needleValues.All(v=>v.Value==boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()),"Phase history differs");}
var needleCreated=(await Call("create_session",new(){["document"]=needleDocument})).GetProperty("data");string needleId=needleCreated.GetProperty("session_id").GetString()!;await Call("step_session",new(){["session_id"]=needleId,["expected_revision"]="1",["delta_ns"]="20000000"},true);Require((await Call("read_snapshot",new(){["session_id"]=needleId})).GetProperty("data").GetProperty("state_hash").GetString()==needleCreated.GetProperty("snapshot").GetProperty("state_hash").GetString(),"Needle conflict mutated state");
await Call("step_session",new(){["session_id"]=needleId,["expected_revision"]="0",["delta_ns"]="20000000"});var needleParent=(await Call("read_snapshot",new(){["session_id"]=needleId})).GetProperty("data");string needleFork=(await Call("fork_session",new(){["session_id"]=needleId,["expected_revision"]="1"})).GetProperty("data").GetProperty("session_id").GetString()!;await Call("set_inputs",new(){["session_id"]=needleFork,["expected_revision"]="0",["values"]=new[]{new{channel="104",value=0.0}}});await Call("step_session",new(){["session_id"]=needleFork,["expected_revision"]="1",["delta_ns"]="20000000"});Require((await Call("read_snapshot",new(){["session_id"]=needleId})).GetProperty("data").GetRawText()==needleParent.GetRawText(),"Needle fork changed parent");await Call("close_session",new(){["session_id"]=needleFork,["expected_revision"]="2"});await Call("close_session",new(){["session_id"]=needleId,["expected_revision"]="1"});
Console.WriteLine("PASS MCP electromagnetic needle / actual delivery magnetic heat and controlled voltage / every export boundary / independent fork");
var closureDocument=(await Call("get_example_model",new(){["name"]="closure-compensated-cylinder"})).GetProperty("data");Require(capabilities.GetProperty("closure_prediction").GetProperty("max_physical_ticks").GetInt32()==4096,"Closure contract missing");
var closureReport=(await Call("run_experiment",new(){["document"]=closureDocument,["include_samples"]=true})).GetProperty("data");Require(closureReport.GetProperty("passed").GetBoolean()&&closureReport.GetProperty("samples").GetArrayLength()==65,"Closure experiment failed");
var closureExport=(await Call("export_model_asset",new(){["document"]=closureDocument})).GetProperty("data");byte[] closurePayload=Convert.FromBase64String(closureExport.GetProperty("content").GetString()!);Require(closureExport.GetProperty("format").GetString()=="power.asset.v28"&&Convert.ToHexStringLower(SHA256.HashData(closurePayload))==closureExport.GetProperty("asset_sha256").GetString(),"Closure asset digest differs");
var closureAsset=AssetCodec.Decode(closurePayload);var closurePlayback=closureAsset.CreatePlayback();var closureValues=new Power.Core.Scalar[closureAsset.Model.OutputCount];Require(closureAsset.Model.HasClosurePrediction&&closureAsset.Model.Fidelity=="closure_compensated_fired_powertrain","Phase model lost");
foreach(var boundary in closureReport.GetProperty("samples").EnumerateArray())
{ulong at=boundary.GetProperty("time_ns").GetUInt64();while(closurePlayback.TimeNanoseconds<at)Require(closurePlayback.Advance(Math.Min(257*closureAsset.Model.StepNanoseconds,at-closurePlayback.TimeNanoseconds))==Power.Core.SimulationStatus.Ok,"Closure playback failed");Require(closurePlayback.ReadSnapshot(closureValues).StateHash.ToString("x16")==boundary.GetProperty("state_hash").GetString()&&closureValues.All(v=>v.Value==boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()),"Phase history differs");}
var closureCreated=(await Call("create_session",new(){["document"]=closureDocument})).GetProperty("data");string closureId=closureCreated.GetProperty("session_id").GetString()!;await Call("step_session",new(){["session_id"]=closureId,["expected_revision"]="1",["delta_ns"]="20000000"},true);Require((await Call("read_snapshot",new(){["session_id"]=closureId})).GetProperty("data").GetProperty("state_hash").GetString()==closureCreated.GetProperty("snapshot").GetProperty("state_hash").GetString(),"Closure conflict mutated state");
await Call("step_session",new(){["session_id"]=closureId,["expected_revision"]="0",["delta_ns"]="20000000"});var closureParent=(await Call("read_snapshot",new(){["session_id"]=closureId})).GetProperty("data");string closureFork=(await Call("fork_session",new(){["session_id"]=closureId,["expected_revision"]="1"})).GetProperty("data").GetProperty("session_id").GetString()!;await Call("set_inputs",new(){["session_id"]=closureFork,["expected_revision"]="0",["values"]=new[]{new{channel="104",value=0.0}}});await Call("step_session",new(){["session_id"]=closureFork,["expected_revision"]="1",["delta_ns"]="20000000"});Require((await Call("read_snapshot",new(){["session_id"]=closureId})).GetProperty("data").GetRawText()==closureParent.GetRawText(),"Closure fork changed parent");await Call("close_session",new(){["session_id"]=closureFork,["expected_revision"]="2"});await Call("close_session",new(){["session_id"]=closureId,["expected_revision"]="1"});
Console.WriteLine("PASS MCP closure compensation / bounded future replay and actual dose / every export boundary / independent fork");
foreach (string dctName in new[] { "dual-clutch-transmission", "fired-dual-clutch" })
{
    var dctDocument = (await Call("get_example_model", new() { ["name"] = dctName })).GetProperty("data");
    Require(capabilities.GetProperty("dual_clutch_assembly").GetProperty("components").GetString()!.Contains("ideal_gear"), "DCT contract absent");
    var dctReport = (await Call("run_experiment", new() { ["document"] = dctDocument, ["include_samples"] = true })).GetProperty("data");
    Require(dctReport.GetProperty("passed").GetBoolean() && dctReport.GetProperty("samples").GetArrayLength() == (dctName == "dual-clutch-transmission" ? 281 : 83), "DCT experiment failed");
    var dctExport = (await Call("export_model_asset", new() { ["document"] = dctDocument })).GetProperty("data");
    byte[] dctPayload = Convert.FromBase64String(dctExport.GetProperty("content").GetString()!);
    Require(dctExport.GetProperty("format").GetString() == AssetCodec.FormatName && Convert.ToHexStringLower(SHA256.HashData(dctPayload)) == dctExport.GetProperty("asset_sha256").GetString(), "DCT asset integrity differs");
    var dctAsset = AssetCodec.Decode(dctPayload); var dctPlayback = dctAsset.CreatePlayback(); var dctValues = new Power.Core.Scalar[dctAsset.Model.OutputCount];
    Require(dctAsset.Model.HasGears && dctAsset.Model.HasClutches && dctAsset.Components.Count(c => c.Kind == Power.Core.ComponentKind.IdealGear) == 12, "DCT topology lost");
    foreach (var sample in dctReport.GetProperty("samples").EnumerateArray())
    {
        ulong at = sample.GetProperty("time_ns").GetUInt64();
        while (dctPlayback.TimeNanoseconds < at) Require(dctPlayback.Advance(Math.Min(257 * dctAsset.Model.StepNanoseconds, at - dctPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "DCT playback failed");
        Require(dctPlayback.ReadSnapshot(dctValues).StateHash.ToString("x16") == sample.GetProperty("state_hash").GetString() && dctValues.All(v => v.Value == sample.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "DCT physical history differs");
    }
    string dctId = (await Call("create_session", new() { ["document"] = dctDocument })).GetProperty("data").GetProperty("session_id").GetString()!;
    await Call("set_inputs", new() { ["session_id"] = dctId, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "600", value = 2.0 } } }, true);
    await Call("step_session", new() { ["session_id"] = dctId, ["expected_revision"] = "0", ["delta_ns"] = "20000000" });
    var dctParent = (await Call("read_snapshot", new() { ["session_id"] = dctId })).GetProperty("data");
    string dctFork = (await Call("fork_session", new() { ["session_id"] = dctId, ["expected_revision"] = "1" })).GetProperty("data").GetProperty("session_id").GetString()!;
    await Call("set_inputs", new() { ["session_id"] = dctFork, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "600", value = 0.0 }, new { channel = "602", value = 1.0 } } });
    await Call("step_session", new() { ["session_id"] = dctFork, ["expected_revision"] = "1", ["delta_ns"] = "20000000" });
    Require((await Call("read_snapshot", new() { ["session_id"] = dctId })).GetProperty("data").GetRawText() == dctParent.GetRawText(), "DCT fork changed parent");
    await Call("close_session", new() { ["session_id"] = dctFork, ["expected_revision"] = "2" });
    await Call("close_session", new() { ["session_id"] = dctId, ["expected_revision"] = "1" });
    Console.WriteLine("PASS MCP DCT " + dctName + " / complete gear-clutch graph replay / selectors and independent fork");
}
foreach (string controlledName in new[] { "controlled-dual-clutch", "controlled-fired-dual-clutch" })
{
    var controlledDocument = (await Call("get_example_model", new() { ["name"] = controlledName })).GetProperty("data");
    Require(capabilities.GetProperty("dct_controller").GetProperty("input").GetString()!.Contains("requested_gear"), "DCT controller contract missing");
    var controlledReport = (await Call("run_experiment", new() { ["document"] = controlledDocument, ["include_samples"] = true })).GetProperty("data");
    Require(controlledReport.GetProperty("passed").GetBoolean(), "Controlled DCT experiment failed");
    var controlledExport = (await Call("export_model_asset", new() { ["document"] = controlledDocument })).GetProperty("data");
    byte[] tcuPayload = Convert.FromBase64String(controlledExport.GetProperty("content").GetString()!); var asset = AssetCodec.Decode(tcuPayload); var tcuPlayback = asset.CreatePlayback(); var values = new Power.Core.Scalar[asset.Model.OutputCount];
    Require(asset.Model.Fidelity == "sampled_dual_clutch_control", "Control state lost");
    foreach (var boundary in controlledReport.GetProperty("samples").EnumerateArray())
    {
        ulong at = boundary.GetProperty("time_ns").GetUInt64(); while (tcuPlayback.TimeNanoseconds < at) Require(tcuPlayback.Advance(Math.Min(257 * asset.Model.StepNanoseconds, at - tcuPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Controlled replay failed");
        Require(tcuPlayback.ReadSnapshot(values).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString() && values.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "TCU histories differ");
    }
    string tcuSession = (await Call("create_session", new() { ["document"] = controlledDocument })).GetProperty("data").GetProperty("session_id").GetString()!;
    await Call("set_inputs", new() { ["session_id"] = tcuSession, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "500", value = 1.0 } } }, true);
    await Call("set_inputs", new() { ["session_id"] = tcuSession, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "700", value = 1.5 } } }, true);
    await Call("step_session", new() { ["session_id"] = tcuSession, ["expected_revision"] = "0", ["delta_ns"] = "20000000" });
    var parent = (await Call("read_snapshot", new() { ["session_id"] = tcuSession })).GetProperty("data");
    string fork = (await Call("fork_session", new() { ["session_id"] = tcuSession, ["expected_revision"] = "1" })).GetProperty("data").GetProperty("session_id").GetString()!;
    await Call("set_inputs", new() { ["session_id"] = fork, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "700", value = 0.0 } } });
    await Call("step_session", new() { ["session_id"] = fork, ["expected_revision"] = "1", ["delta_ns"] = "20000000" });
    Require((await Call("read_snapshot", new() { ["session_id"] = tcuSession })).GetProperty("data").GetRawText() == parent.GetRawText(), "TCU fork changed parent");
    await Call("close_session", new() { ["session_id"] = fork, ["expected_revision"] = "2" }); await Call("close_session", new() { ["session_id"] = tcuSession, ["expected_revision"] = "1" });
    Console.WriteLine("PASS MCP " + controlledName + " / actual sampled TCU histories / owned commands and independent fork");
}
foreach (string name in new[] { "ravigneaux-transmission", "fired-ravigneaux-converter", "resolved-ravigneaux-transmission", "fired-resolved-ravigneaux-converter", "hydraulic-ravigneaux-transmission", "fired-hydraulic-ravigneaux" })
{
    var compoundDocument = (await Call("get_example_model", new() { ["name"] = name })).GetProperty("data");
    var compoundReport = (await Call("run_experiment", new() { ["document"] = compoundDocument, ["include_samples"] = true })).GetProperty("data");
    Require(compoundReport.GetProperty("passed").GetBoolean(), "Compound experiment failed");
    var compoundExport = (await Call("export_model_asset", new() { ["document"] = compoundDocument })).GetProperty("data");
    byte[] compoundPayload = Convert.FromBase64String(compoundExport.GetProperty("content").GetString()!);
    Require(compoundExport.GetProperty("format").GetString() == AssetCodec.FormatName && Convert.ToHexStringLower(SHA256.HashData(compoundPayload)) == compoundExport.GetProperty("asset_sha256").GetString(), "Compound asset integrity differs");
    var compoundAsset = AssetCodec.Decode(compoundPayload); var compoundPlayback = compoundAsset.CreatePlayback(); var compoundValues = new Power.Core.Scalar[compoundAsset.Model.OutputCount];
    Require(name.Contains("resolved") || name.Contains("hydraulic") ? compoundAsset.Components.Count(c => c.Kind == Power.Core.ComponentKind.CarrierGear) == 4 : compoundAsset.Components.Count(c => c.Kind == Power.Core.ComponentKind.DoublePinionPlanetaryGear) == 1, "Compound topology lost");
    foreach (var boundary in compoundReport.GetProperty("samples").EnumerateArray())
    {
        ulong at = boundary.GetProperty("time_ns").GetUInt64(); while (compoundPlayback.TimeNanoseconds < at) Require(compoundPlayback.Advance(Math.Min(257 * compoundAsset.Model.StepNanoseconds, at - compoundPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "Compound playback failed");
        Require(compoundPlayback.ReadSnapshot(compoundValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString() && compoundValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "Compound physical history differs");
    }
    string session = (await Call("create_session", new() { ["document"] = compoundDocument })).GetProperty("data").GetProperty("session_id").GetString()!;
    await Call("set_inputs", new() { ["session_id"] = session, ["expected_revision"] = "0", ["values"] = new[] { new { channel = name.Contains("hydraulic") ? "700" : "600", value = 2.0 } } }, true);
    await Call("step_session", new() { ["session_id"] = session, ["expected_revision"] = "0", ["delta_ns"] = "20000000" });
    var parent = (await Call("read_snapshot", new() { ["session_id"] = session })).GetProperty("data");
    string fork = (await Call("fork_session", new() { ["session_id"] = session, ["expected_revision"] = "1" })).GetProperty("data").GetProperty("session_id").GetString()!;
    await Call("set_inputs", new() { ["session_id"] = fork, ["expected_revision"] = "0", ["values"] = new[] { new { channel = name.Contains("hydraulic") ? "702" : "601", value = 0.0 }, new { channel = name.Contains("hydraulic") ? "703" : "603", value = name.Contains("hydraulic") ? 1.0 : 0.0 } } });
    await Call("step_session", new() { ["session_id"] = fork, ["expected_revision"] = "1", ["delta_ns"] = "20000000" });
    Require((await Call("read_snapshot", new() { ["session_id"] = session })).GetProperty("data").GetRawText() == parent.GetRawText(), "Compound fork changed parent");
    await Call("close_session", new() { ["session_id"] = fork, ["expected_revision"] = "2" }); await Call("close_session", new() { ["session_id"] = session, ["expected_revision"] = "1" });
    Console.WriteLine("PASS MCP " + name + " / compound ports reactions and heat / complete replay and independent fork");
}
foreach (string atName in new[] { "controlled-hydraulic-ravigneaux", "controlled-fired-hydraulic-ravigneaux" })
{
    var atDocument = (await Call("get_example_model", new() { ["name"] = atName })).GetProperty("data");
    var atReport = (await Call("run_experiment", new() { ["document"] = atDocument, ["include_samples"] = true })).GetProperty("data");
    Require(atReport.GetProperty("passed").GetBoolean(), "AT feedback experiment failed");
    var atExport = (await Call("export_model_asset", new() { ["document"] = atDocument })).GetProperty("data");
    byte[] atPayload = Convert.FromBase64String(atExport.GetProperty("content").GetString()!);
    Require(atExport.GetProperty("format").GetString() == AssetCodec.FormatName && Convert.ToHexStringLower(SHA256.HashData(atPayload)) == atExport.GetProperty("asset_sha256").GetString(), "AT asset integrity differs");
    var atAsset = AssetCodec.Decode(atPayload); var atPlayback = atAsset.CreatePlayback(); var atValues = new Power.Core.Scalar[atAsset.Model.OutputCount];
    Require(atAsset.Model.Fidelity == "sampled_hydraulic_at_control" && atAsset.Model.StateCount <= 128, "AT control state lost");
    foreach (var boundary in atReport.GetProperty("samples").EnumerateArray())
    {
        ulong at = boundary.GetProperty("time_ns").GetUInt64(); while (atPlayback.TimeNanoseconds < at) Require(atPlayback.Advance(Math.Min(257 * atAsset.Model.StepNanoseconds, at - atPlayback.TimeNanoseconds)) == Power.Core.SimulationStatus.Ok, "AT playback failed");
        Require(atPlayback.ReadSnapshot(atValues).StateHash.ToString("x16") == boundary.GetProperty("state_hash").GetString() && atValues.All(v => v.Value == boundary.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()), "AT sampled histories differ");
    }
    string atSession = (await Call("create_session", new() { ["document"] = atDocument })).GetProperty("data").GetProperty("session_id").GetString()!;
    var atOwned = await Call("set_inputs", new() { ["session_id"] = atSession, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "702", value = 1.0 } } }, true);
    Require(atOwned.GetProperty("error").GetProperty("message").GetString()!.Contains("900"), "AT owned-channel guidance missing");
    await Call("set_inputs", new() { ["session_id"] = atSession, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "900", value = 1.5 } } }, true);
    await Call("step_session", new() { ["session_id"] = atSession, ["expected_revision"] = "0", ["delta_ns"] = "2000000" });
    var parent = (await Call("read_snapshot", new() { ["session_id"] = atSession })).GetProperty("data");
    string fork = (await Call("fork_session", new() { ["session_id"] = atSession, ["expected_revision"] = "1" })).GetProperty("data").GetProperty("session_id").GetString()!;
    await Call("set_inputs", new() { ["session_id"] = fork, ["expected_revision"] = "0", ["values"] = new[] { new { channel = "900", value = 0.0 } } });
    await Call("step_session", new() { ["session_id"] = fork, ["expected_revision"] = "1", ["delta_ns"] = "2000000" });
    Require((await Call("read_snapshot", new() { ["session_id"] = atSession })).GetProperty("data").GetRawText() == parent.GetRawText(), "AT neutral fork changed parent");
    await Call("close_session", new() { ["session_id"] = fork, ["expected_revision"] = "2" }); await Call("close_session", new() { ["session_id"] = atSession, ["expected_revision"] = "1" });
    Console.WriteLine("PASS MCP " + atName + " / sampled pressure range and lockup histories / owned channels and independent fork");
}
foreach (string feedName in new[] { "pump-fed-liquid-cylinder", "pump-fed-needle-cylinder", "finite-tank-liquid-cylinder", "finite-tank-needle-cylinder", "recirculating-liquid-cylinder", "recirculating-needle-cylinder" })
{
    var feedDocument=(await Call("get_example_model",new(){["name"]=feedName})).GetProperty("data");
    var feedReport=(await Call("run_experiment",new(){["document"]=feedDocument,["include_samples"]=true})).GetProperty("data");Require(feedReport.GetProperty("passed").GetBoolean(),"Pump-fed experiment failed");
    var feedExport=(await Call("export_model_asset",new(){["document"]=feedDocument})).GetProperty("data");byte[] feedPayload=Convert.FromBase64String(feedExport.GetProperty("content").GetString()!);
    Require(feedExport.GetProperty("format").GetString()==AssetCodec.FormatName&&Convert.ToHexStringLower(SHA256.HashData(feedPayload))==feedExport.GetProperty("asset_sha256").GetString(),"Feed export integrity differs");
    var feedAsset=AssetCodec.Decode(feedPayload);var feedPlayback=feedAsset.CreatePlayback();var feedValues=new Power.Core.Scalar[feedAsset.Model.OutputCount];
    foreach(var row in feedReport.GetProperty("samples").EnumerateArray())
    {
        ulong at=row.GetProperty("time_ns").GetUInt64();while(feedPlayback.TimeNanoseconds<at)Require(feedPlayback.Advance(Math.Min(257*feedAsset.Model.StepNanoseconds,at-feedPlayback.TimeNanoseconds))==Power.Core.SimulationStatus.Ok,"Feed replay failed");
        Require(feedPlayback.ReadSnapshot(feedValues).StateHash.ToString("x16")==row.GetProperty("state_hash").GetString()&&feedValues.All(v=>v.Value==row.GetProperty("values").GetProperty(v.Channel.ToString()).GetDouble()),"Feed physical histories differ");
    }
    string feedSession=(await Call("create_session",new(){["document"]=feedDocument})).GetProperty("data").GetProperty("session_id").GetString()!;
    await Call("step_session",new(){["session_id"]=feedSession,["expected_revision"]="0",["delta_ns"]="1000000"});var feedParent=(await Call("read_snapshot",new(){["session_id"]=feedSession})).GetProperty("data");
    string feedFork=(await Call("fork_session",new(){["session_id"]=feedSession,["expected_revision"]="1"})).GetProperty("data").GetProperty("session_id").GetString()!;
    await Call("set_inputs",new(){["session_id"]=feedFork,["expected_revision"]="0",["values"]=new[]{new{channel="950",value=.01}}});await Call("step_session",new(){["session_id"]=feedFork,["expected_revision"]="1",["delta_ns"]="1000000"});
    Require((await Call("read_snapshot",new(){["session_id"]=feedSession})).GetProperty("data").GetRawText()==feedParent.GetRawText(),"Feed fork changed parent");
    await Call("close_session",new(){["session_id"]=feedFork,["expected_revision"]="2"});await Call("close_session",new(){["session_id"]=feedSession,["expected_revision"]="1"});
    Console.WriteLine("PASS MCP "+feedName+" / coupled shaft rail and mixed fuel inventory / complete replay and independent fork");
}
Console.WriteLine("45/45 MCP integration groups passed against an actual child server process.");
