using System.Text.Json.Nodes;

namespace CTeam.Experiments;

// Transport-only metadata supplied by Codex on an MCP tools/call request.
public sealed record CallerContext(string? ThreadId, string? SessionId, bool HasWorkspaceMetadata, IReadOnlyList<string>? WorkspaceRoots = null)
{
    public static CallerContext FromToolParameters(JsonObject? parameters)
    {
        var metadata = parameters?["_meta"]?["x-codex-turn-metadata"] as JsonObject;
        var workspaces = metadata?["workspaces"] as JsonObject;
        var roots = workspaces?.Select(entry => entry.Key).Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
        return new(Text(metadata, "thread_id"), Text(metadata, "session_id"), workspaces is not null, roots);
    }

    static string? Text(JsonObject? value, string property) => value?[property] is JsonValue item && item.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text)
        ? text
        : null;
}
