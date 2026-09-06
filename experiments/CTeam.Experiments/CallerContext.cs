using System.Text.Json.Nodes;

namespace CTeam.Experiments;

// Transport-only metadata supplied by Codex on an MCP tools/call request.
public sealed record CallerContext(string? ThreadId, string? SessionId, bool HasWorkspaceMetadata)
{
    public static CallerContext FromToolParameters(JsonObject? parameters)
    {
        var metadata = parameters?["_meta"]?["x-codex-turn-metadata"] as JsonObject;
        return new(Text(metadata, "thread_id"), Text(metadata, "session_id"), metadata?["workspaces"] is JsonObject);
    }

    static string? Text(JsonObject? value, string property) => value?[property] is JsonValue item && item.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text)
        ? text
        : null;
}
