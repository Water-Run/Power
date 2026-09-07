using System.Text.Json;
using System.Text.Json.Nodes;
using Power.Agent;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class AgentChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("agent discovery / structured model repair", Discovery),
        ("agent revision conflicts / input atomicity / cancellation", Revisions),
        ("agent branch isolation / filtered snapshots / close", Branches),
        ("agent session capacity / reclamation", Capacity),
        ("concurrent agent writes compare revision atomically", ConcurrentWrites),
        ("agent compact experiment / full trace / failed KPI", Reports)
    ];
    private static JsonElement Data(AgentReply reply)
    {
        Require(reply.Ok, reply.Error?.Message ?? "Expected success");
        return reply.Data!.Value;
    }
    private static JsonElement Example => Data(AgentWorkspace.ExampleModel());
    private static (AgentWorkspace Workspace, string Id) Create()
    {
        var workspace = new AgentWorkspace();
        var result = Data(workspace.CreateSession(Example));
        Require(result.GetProperty("snapshot").GetProperty("revision").GetString() == "0");
        return (workspace, result.GetProperty("session_id").GetString()!);
    }

    private static void Discovery()
    {
        Require(Data(AgentWorkspace.Capabilities()).GetProperty("calibration").GetString() == "unverified");
        Require(Data(AgentWorkspace.ModelSchema()).GetProperty("$schema").GetString() == "https://json-schema.org/draft/2020-12/schema");
        var report = Data(AgentWorkspace.ValidateModel(Example));
        Require(report.GetProperty("model").GetProperty("channels").GetArrayLength() > 10);
        using var bad = JsonDocument.Parse(Example.GetRawText().Replace("\"kg_m2\"", "\"nm\""));
        var error = AgentWorkspace.ValidateModel(bad.RootElement);
        Require(!error.Ok && error.Error is { Code: "model_unit", ObjectId: 1, Field: "storage" });
        using var typo = JsonDocument.Parse(Example.GetRawText().Replace("\"description\"", "\"descripton\""));
        Require(AgentWorkspace.ValidateModel(typo.RootElement).Error?.Code == "invalid_argument");
    }

    private static void Revisions()
    {
        var (w, id) = Create();
        string initial = Data(w.ReadSnapshot(id)).GetRawText();
        Require(w.SetInputs(id, "1", [new("100", 48)]).Error?.Code == "revision_conflict");
        Require(w.SetInputs(id, null!, [new("100", 48)]).Error?.Code == "invalid_argument");
        Require(w.SetInputs(id, "0", [new("100", 48), new("99", 1)]).Error?.Code == "unknown_channel");
        Require(w.SetInputs(id, "0", [new("100", 48), new("100", 1)]).Error?.Code == "invalid_input");
        Require(w.StepSession(id, "0", "3").Error?.Code == "invalid_time_step");
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Require(w.StepSession(id, "0", "1000000000", cancelled.Token).Error?.Code == "cancelled");
        Require(Data(w.ReadSnapshot(id)).GetRawText() == initial);
        Require(Data(w.SetInputs(id, "0", [new("100", 48)])).GetProperty("revision").GetString() == "1");
        Require(w.SetInputs(id, "0", [new("100", 48)]).Error?.CurrentRevision == "1");
        var after = Data(w.StepSession(id, "1", "1000000000"));
        Require(after.GetProperty("time_ns").GetString() == "1000000000" && after.GetProperty("revision").GetString() == "2");
    }

    private static void Branches()
    {
        var (w, id) = Create();
        var state = Data(w.StepSession(id, "0", "1000000000"));
        var fork = Data(w.ForkSession(id, "1"));
        string child = fork.GetProperty("session_id").GetString()!;
        Require(fork.GetProperty("snapshot").GetProperty("state_hash").GetString() == state.GetProperty("state_hash").GetString());
        Require(w.SetInputs(child, "0", [new("100", 4)]).Ok && w.StepSession(child, "1", "500000000").Ok);
        Require(Data(w.ReadSnapshot(id)).GetRawText() == state.GetRawText());
        string channel = state.GetProperty("values")[0].GetProperty("channel").GetString()!;
        Require(Data(w.ReadSnapshot(child, [channel])).GetProperty("values").GetArrayLength() == 1);
        Require(w.ReadSnapshot(child, ["100"]).Error?.Code == "invalid_argument");
        Require(w.CloseSession(child, "0").Error?.Code == "revision_conflict");
        Require(w.CloseSession(child, "2").Ok && w.ReadSnapshot(child).Error?.Code == "unknown_session");
        Require(w.ReadSnapshot(id).Ok);
    }

    private static void Capacity()
    {
        var w = new AgentWorkspace();
        var ids = new List<string>();
        for (int i = 0; i < AgentWorkspace.MaxSessions; ++i)
            ids.Add(Data(w.CreateSession(Example)).GetProperty("session_id").GetString()!);
        Require(w.CreateSession(Example).Error?.Code == "session_capacity");
        Require(w.ForkSession(ids[0], "0").Error?.Code == "session_capacity");
        Require(w.CloseSession(ids[0], "0").Ok && w.CreateSession(Example).Ok);
    }

    private static void ConcurrentWrites()
    {
        var (w, id) = Create();
        var replies = new AgentReply[8];
        Parallel.For(0, replies.Length, i => replies[i] = w.StepSession(id, "0", "100000"));
        Require(replies.Count(r => r.Ok) == 1);
        Require(replies.Count(r => r.Error?.Code == "revision_conflict") == 7);
        Require(Data(w.ReadSnapshot(id)).GetProperty("time_ns").GetString() == "100000");
    }

    private static void Reports()
    {
        var compact = Data(AgentWorkspace.RunExperiment(Example));
        Require(compact.GetProperty("passed").GetBoolean() && !compact.TryGetProperty("samples", out _));
        var full = Data(AgentWorkspace.RunExperiment(Example, true));
        Require(full.GetProperty("samples").GetArrayLength() == 11);
        var failedModel = JsonNode.Parse(Example.GetRawText())!;
        failedModel["experiment"]!["checks"]![0]!["min"] = 30;
        using var failed = JsonDocument.Parse(failedModel.ToJsonString());
        Require(!Data(AgentWorkspace.RunExperiment(failed.RootElement)).GetProperty("passed").GetBoolean());
    }
}
