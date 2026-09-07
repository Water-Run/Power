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
Console.WriteLine("5/5 MCP integration groups passed against an actual child server process.");
