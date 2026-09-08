// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Power.Agent;

namespace Power.Mcp;

[McpServerToolType]
public sealed class PowerTools(AgentWorkspace workspace)
{
    private static CallToolResult Reply(AgentReply reply)
    {
        string json = JsonSerializer.Serialize(reply, AgentReply.JsonOptions);
        return new() { IsError = !reply.Ok, StructuredContent = JsonSerializer.SerializeToElement(reply, AgentReply.JsonOptions), Content = [new TextContentBlock { Text = json }] };
    }

    [McpServerTool(Name = "get_capabilities", ReadOnly = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(AgentReply)), Description("Discover Power's physical models, limits, exact time/revision conventions and agent workflow. Start here.")]
    public CallToolResult Capabilities() => Reply(AgentWorkspace.Capabilities());

    [McpServerTool(Name = "get_model_schema", ReadOnly = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(AgentReply)), Description("Get JSON Schema 2020-12 for power.model.v1. The compiler additionally checks dimensions, topology and numerical feasibility.")]
    public CallToolResult ModelSchema() => Reply(AgentWorkspace.ModelSchema());

    [McpServerTool(Name = "get_example_model", ReadOnly = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(AgentReply)), Description("Get an editable synthetic model and experiment. Names: electrothermal, sealed-cylinder. Parameters are uncalibrated.")]
    public CallToolResult ExampleModel(string name = "electrothermal") => Reply(AgentWorkspace.ExampleModel(name));

    [McpServerTool(Name = "validate_model", ReadOnly = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(AgentReply)), Description("Validate a model document and experiment without running it. Returns fingerprint, channel IDs/units and structured repair diagnostics.")]
    public CallToolResult ValidateModel([Description("Complete power.model.v1 JSON object, including experiment. Get an example and schema first.")] JsonElement document) => Reply(AgentWorkspace.ValidateModel(document));

    [McpServerTool(Name = "run_experiment", ReadOnly = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(AgentReply)), Description("Run a bounded headless experiment and independent batch replay. Returns KPIs, provenance and final snapshot. Check data.passed separately from ok.")]
    public CallToolResult RunExperiment(JsonElement document, [Description("Include all sample boundaries; false keeps the report compact.")] bool include_samples = false,
        CancellationToken cancellationToken = default) => Reply(AgentWorkspace.RunExperiment(document, include_samples, cancellationToken));

    [McpServerTool(Name = "export_model_asset", ReadOnly = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(AgentReply)),
        Description("Validate and export a portable Unity model/experiment asset as base64 with SHA-256 and model fingerprint. Does not run the experiment or write files. Decode to a .powerasset file in Unity/Assets.")]
    public CallToolResult ExportModelAsset(JsonElement document, string name = "Power model") => Reply(AgentWorkspace.ExportModelAsset(document, name));

    [McpServerTool(Name = "create_session", Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(AgentReply)), Description("Create an independent simulation at time zero. Returns session_id, revision and all channel metadata. At most 16 process-local sessions.")]
    public CallToolResult CreateSession(JsonElement document) => Reply(workspace.CreateSession(document));

    [McpServerTool(Name = "read_snapshot", ReadOnly = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(AgentReply)), Description("Read time, revision, state hash and selected outputs. Omit channels for all outputs. IDs are decimal strings.")]
    public CallToolResult ReadSnapshot(string session_id, string[]? channels = null) => Reply(workspace.ReadSnapshot(session_id, channels));

    [McpServerTool(Name = "set_inputs", Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(AgentReply)), Description("Atomically change input values at the current simulation time. Supply the last revision; stale revisions fail without changes. Does not advance time.")]
    public CallToolResult SetInputs(string session_id, string expected_revision, ChannelInput[] values) => Reply(workspace.SetInputs(session_id, expected_revision, values));

    [McpServerTool(Name = "step_session", Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(AgentReply)), Description("Advance an exact integer number of fixed ticks, at most 1,000,000. delta_ns is a decimal string. Failed/cancelled calls preserve the whole state and revision.")]
    public CallToolResult StepSession(string session_id, string expected_revision, string delta_ns, CancellationToken cancellationToken = default) =>
        Reply(workspace.StepSession(session_id, expected_revision, delta_ns, cancellationToken));

    [McpServerTool(Name = "fork_session", Destructive = false, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(AgentReply)), Description("Branch from an exact state for counterfactual experiments. Child gets its own session_id and revision 0; parent remains unchanged.")]
    public CallToolResult ForkSession(string session_id, string expected_revision) => Reply(workspace.ForkSession(session_id, expected_revision));

    [McpServerTool(Name = "close_session", Destructive = true, OpenWorld = false, UseStructuredContent = true, OutputSchemaType = typeof(AgentReply)), Description("Release an in-memory simulation and its state. Requires the current revision. Other sessions are unaffected.")]
    public CallToolResult CloseSession(string session_id, string expected_revision) => Reply(workspace.CloseSession(session_id, expected_revision));
}
