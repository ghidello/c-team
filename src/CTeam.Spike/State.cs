using System.Text.Json;

namespace CTeam.Spike;

public sealed class MissionState
{
    internal object Gate { get; } = new();
    public Dictionary<string, AgentState> Threads { get; } = new();
    public Dictionary<string, int> UnknownMessageCounts { get; } = new();
    public List<ModelCapability> Models { get; } = [];
    public string? AccountType { get; set; }
    public Dictionary<string, QuotaBucket> RateLimits { get; } = new();
    public List<string> ProtocolErrors { get; } = [];

    public bool IsTurnCompleted(string threadId, string turnId)
    {
        lock (Gate)
        {
            if (!Threads.TryGetValue(threadId, out var agent) || !agent.Turns.TryGetValue(turnId, out var turn)) return false;
            if (turn.Status is "failed" or "interrupted") throw new InvalidOperationException($"Turn {turnId} {turn.Status}: {turn.Error}");
            return turn.CompletedNotificationReceived || turn.Status == "completed";
        }
    }

    public string[] ChildThreadIds()
    {
        lock (Gate) return Threads.Values.Where(x => x.ParentThreadId is not null || x.ReviewOfThreadId is not null).Select(x => x.ThreadId).ToArray();
    }

    public string ToJson()
    {
        lock (Gate) return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }

    public void Render() { lock (Gate) Renderer.WriteUnsafe(this); }
}

public sealed class AgentState(string id)
{
    public string ThreadId { get; } = id;
    public string? SessionId { get; set; }
    public string? ParentThreadId { get; set; }
    public string? ReviewOfThreadId { get; set; }
    public string? AgentPath { get; set; }
    public string? Role { get; set; }
    public string? Nickname { get; set; }
    public int? SpawnDepth { get; set; }
    public string? Cwd { get; set; }
    public string? RequestedModel { get; set; }
    public string? ConfiguredModel { get; set; }
    public string? EffectiveModel { get; set; }
    public List<ModelObservation> ModelObservations { get; } = [];
    public string? ReasoningEffort { get; set; }
    public string? ServiceTier { get; set; }
    public string Status { get; set; } = "created";
    public string? RuntimeStatus { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public Usage Usage { get; set; } = new();
    public Usage LastUsage { get; set; } = new();
    public List<PlanStep> Plan { get; set; } = [];
    public string? PlanExplanation { get; set; }
    public Dictionary<string, TurnState> Turns { get; } = new();
    public Dictionary<string, ActivityItem> Items { get; } = new();
    public int FilesChanged => ChangedPaths().Distinct(StringComparer.Ordinal).Count();
    public int Added => Turns.Values.Sum(t => t.Diff?.Added ?? SuccessfulFiles(t.Id).Sum(f => f.Added));
    public int Removed => Turns.Values.Sum(t => t.Diff?.Removed ?? SuccessfulFiles(t.Id).Sum(f => f.Removed));

    IEnumerable<FileActivity> SuccessfulFiles(string? turnId) => Items.Values
        .Where(i => i.Type == "fileChange" && i.Status == "completed" && i.TurnId == turnId).SelectMany(i => i.Files);

    IEnumerable<string> ChangedPaths()
    {
        foreach (var turn in Turns.Values)
            foreach (var path in turn.Diff?.Paths ?? SuccessfulFiles(turn.Id).Select(f => f.Path).ToList()) yield return Normalize(path);
        foreach (var file in SuccessfulFiles(null)) yield return Normalize(file.Path);
    }

    string Normalize(string path) => Cwd is not null && Path.IsPathFullyQualified(path)
        ? Path.GetRelativePath(Cwd, path).Replace('\\', '/') : path.Replace('\\', '/');

    public TurnState GetTurn(string id) => Turns.TryGetValue(id, out var turn) ? turn : Turns[id] = new TurnState(id);
}

public sealed class TurnState(string id)
{
    public string Id { get; } = id;
    public string? Status { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public string? TimingSource { get; set; }
    public bool CompletedNotificationReceived { get; set; }
    public Usage? LatestUsage { get; set; }
    public List<PlanStep> Plan { get; set; } = [];
    public string? PlanExplanation { get; set; }
    public DiffSummary? Diff { get; set; }
    public TimeSpan? Duration => DurationMs is { } ms ? TimeSpan.FromMilliseconds(ms)
        : StartedAt is { } start && CompletedAt is { } end ? end - start : null;
}

public sealed record PlanStep(string Step, string Status);
public sealed record ModelObservation(string Source, string Model, string Scope, string? TurnId, DateTimeOffset? ObservedAt, string? Reason = null);
public sealed record ModelCapability(string Id, string Model, string? DisplayName, bool? Hidden, string? DefaultEffort,
    string[] ReasoningEfforts, string[] InputModalities, string[] ServiceTiers, string? MultiAgentVersion);
public sealed record QuotaBucket(string Id, string? Name, double? UsedPercent, long? WindowMinutes, long? ResetsAt);
public sealed record FileActivity(string Path, string? Kind, int Added, int Removed);
public sealed record DiffSummary(List<string> Paths, int Added, int Removed);

public sealed class Usage
{
    public long? TotalTokens { get; set; }
    public long? InputTokens { get; set; }
    public long? CachedInputTokens { get; set; }
    public long? CacheWriteInputTokens { get; set; }
    public long? OutputTokens { get; set; }
    public long? ReasoningOutputTokens { get; set; }
    public long? ContextWindow { get; set; }
}

public sealed class ActivityItem
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public string? TurnId { get; init; }
    public string? Command { get; init; }
    public string? Tool { get; init; }
    public string? Server { get; init; }
    public string? Namespace { get; init; }
    public string? Status { get; init; }
    public long? DurationMs { get; init; }
    public int? ExitCode { get; init; }
    public string? AgentThreadId { get; init; }
    public string? AgentPath { get; init; }
    public List<FileActivity> Files { get; init; } = [];
}

public static class Renderer
{
    public static void Write(MissionState state) => state.Render();
    internal static void WriteUnsafe(MissionState state)
    {
        Console.WriteLine("C-TEAM");
        var seen = new HashSet<string>();
        foreach (var agent in state.Threads.Values.Where(x => x.ParentThreadId is null || !state.Threads.ContainsKey(x.ParentThreadId))) Write(agent, state, "", seen);
        foreach (var agent in state.Threads.Values.Where(x => !seen.Contains(x.ThreadId))) Write(agent, state, "? ", seen);
        Console.WriteLine($"Agents {state.Threads.Count}; catalog {state.Models.Count}; quota buckets {state.RateLimits.Count}; unknown events {state.UnknownMessageCounts.Values.Sum()}");
    }

    static void Write(AgentState agent, MissionState state, string indent, HashSet<string> seen)
    {
        if (!seen.Add(agent.ThreadId)) return;
        var duration = agent.Turns.Values.LastOrDefault()?.Duration?.TotalSeconds.ToString("F1") ?? "?";
        Console.WriteLine($"{indent}{agent.Nickname ?? agent.AgentPath ?? agent.ThreadId} · {agent.Role ?? "agent"} {agent.Status} {duration}s " +
            $"{agent.Usage.TotalTokens?.ToString() ?? "?"} tokens configured={agent.ConfiguredModel ?? "unknown"} effective={agent.EffectiveModel ?? "unknown"}");
        foreach (var step in agent.Plan) Console.WriteLine($"{indent}  [{step.Status}] {step.Step}");
        foreach (var child in state.Threads.Values.Where(x => x.ParentThreadId == agent.ThreadId)) Write(child, state, indent + "  ", seen);
    }
}
