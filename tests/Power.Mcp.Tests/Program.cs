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
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
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
Require(cylinderExport.GetProperty("format").GetString() == "power.asset.v11", "Wrong cylinder asset version");
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
Require(gasExport.GetProperty("format").GetString() == "power.asset.v11" && Convert.ToHexStringLower(SHA256.HashData(gasPayload)) == gasExport.GetProperty("asset_sha256").GetString(), "Gas asset integrity mismatch");
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
Require(movingExport.GetProperty("format").GetString() == "power.asset.v11", "Moving cylinder export must use the current format");
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
Require(timedExport.GetProperty("format").GetString() == "power.asset.v11", "Timing uses current asset v6");
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
Require(firedExport.GetProperty("format").GetString() == "power.asset.v11", "Combustion requires asset v6");
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
Require(clutchExport.GetProperty("format").GetString() == "power.asset.v11" && Convert.ToHexStringLower(SHA256.HashData(clutchPayload)) == clutchExport.GetProperty("asset_sha256").GetString(), "Clutch asset version or digest differs");
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
Require(planetaryExport.GetProperty("format").GetString() == "power.asset.v11" && Convert.ToHexStringLower(SHA256.HashData(planetaryPayload)) == planetaryExport.GetProperty("asset_sha256").GetString(), "Gear asset version or digest differs");
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
Require(converterExport.GetProperty("format").GetString() == "power.asset.v11" && Convert.ToHexStringLower(SHA256.HashData(converterPayload)) == converterExport.GetProperty("asset_sha256").GetString(), "Converter asset version or digest differs");
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
Require(hydraulicExport.GetProperty("format").GetString() == "power.asset.v11" && Convert.ToHexStringLower(SHA256.HashData(hydraulicPayload)) == hydraulicExport.GetProperty("asset_sha256").GetString(), "Hydraulic asset version or digest differs");
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
Require(pumpExport.GetProperty("format").GetString() == "power.asset.v11" && Convert.ToHexStringLower(SHA256.HashData(pumpPayload)) == pumpExport.GetProperty("asset_sha256").GetString(), "Pump asset version or digest differs");
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
Console.WriteLine("15/15 MCP integration groups passed against an actual child server process.");
