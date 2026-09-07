// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Power.Agent;

public sealed record AgentError(string Code, string Message, bool Retryable = false,
    uint? ObjectId = null, string? Field = null, string? CurrentRevision = null);

public sealed record AgentReply(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("data")] JsonElement? Data = null,
    [property: JsonPropertyName("error")] AgentError? Error = null)
{
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    public static AgentReply Success(object data) => new("power.agent.v1", true, JsonSerializer.SerializeToElement(data, JsonOptions), null);
    public static AgentReply Failure(string code, string message, bool retryable = false, uint? objectId = null,
        string? field = null, string? revision = null) => new("power.agent.v1", false, null, new(code, message, retryable, objectId, field, revision));
}

public sealed record ChannelInput(
    [property: JsonPropertyName("channel")] string Channel,
    [property: JsonPropertyName("value")] double Value);
