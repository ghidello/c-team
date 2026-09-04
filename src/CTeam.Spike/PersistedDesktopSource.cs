using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CTeam.Spike;

public sealed class PersistedDesktopSource : IDisposable
{
    readonly string path;
    readonly PersistedDesktopMapper mapper;
    readonly FileSystemWatcher? watcher;
    readonly Timer? reconciliation;
    readonly object gate = new();
    readonly List<PersistedMeasurement> measurements = [];
    long offset;
    byte[] trailing = [];
    byte[]? prefix;
    bool disposed;

    public MissionState State { get; } = new();
    public PersistedObserverMetrics Metrics { get; } = new();
    public IReadOnlyList<PersistedMeasurement> Measurements { get { lock (gate) return measurements.ToList(); } }

    public PersistedDesktopSource(string rolloutPath, bool watch = false, TimeSpan? reconciliationInterval = null)
    {
        path = Path.GetFullPath(rolloutPath);
        mapper = new PersistedDesktopMapper(State);
        if (!watch) return;
        watcher = new FileSystemWatcher(Path.GetDirectoryName(path)!, Path.GetFileName(path)) { NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite };
        watcher.Changed += (_, _) => { lock (gate) { Metrics.WatcherNotifications++; } _ = SynchronizeAsync(false); };
        watcher.Created += (_, _) => { lock (gate) { Metrics.WatcherNotifications++; } _ = SynchronizeAsync(false); };
        watcher.Renamed += (_, _) => { lock (gate) { Metrics.WatcherNotifications++; } _ = SynchronizeAsync(false); };
        watcher.EnableRaisingEvents = true;
        reconciliation = new Timer(_ => _ = SynchronizeAsync(true), null, reconciliationInterval ?? TimeSpan.FromSeconds(1), reconciliationInterval ?? TimeSpan.FromSeconds(1));
    }

    public Task InitializeAsync() => SynchronizeAsync(true, initial: true);

    public async Task SynchronizeAsync(bool reconciliationPass, bool initial = false)
    {
        if (disposed) return;
        await Task.Yield();
        lock (gate)
        {
            if (disposed) return;
            if (reconciliationPass && !initial) Metrics.Reconciliations++;
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists) return;
                var replace = info.Length < offset || (offset > 0 && PrefixChanged());
                if (replace) Rebuild();
                if (info.Length == offset && !initial) return;
                ReadAvailable(info);
                prefix = ReadPrefix();
            }
            catch (IOException) { /* A writer may be between replace and flush; reconciliation retries. */ }
        }
    }

    bool PrefixChanged()
    {
        var current = ReadPrefix();
        return prefix is not null && (current.Length < prefix.Length || !current.AsSpan(0, prefix.Length).SequenceEqual(prefix));
    }

    byte[] ReadPrefix()
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var buffer = new byte[Math.Min(256, (int)stream.Length)];
        _ = stream.Read(buffer, 0, buffer.Length);
        return buffer;
    }

    void ReadAvailable(FileInfo info)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (stream.Length < offset) { Rebuild(); stream.Position = 0; }
        else stream.Position = offset;
        var count = checked((int)(stream.Length - stream.Position));
        if (count == 0) return;
        var bytes = new byte[count];
        var read = 0;
        while (read < count) { var n = stream.Read(bytes, read, count - read); if (n == 0) break; read += n; }
        var bufferStartOffset = offset - trailing.Length;
        offset += read;
        Metrics.BytesRead += read;
        var buffer = new byte[trailing.Length + read];
        trailing.CopyTo(buffer, 0);
        bytes.AsSpan(0, read).CopyTo(buffer.AsSpan(trailing.Length));
        var lineStart = 0;
        while (Array.IndexOf(buffer, (byte)'\n', lineStart) is var newline && newline >= 0)
        {
            var length = newline - lineStart;
            if (length > 0 && buffer[newline - 1] == '\r') length--;
            ProcessLine(Encoding.UTF8.GetString(buffer, lineStart, length), info.LastWriteTimeUtc, bufferStartOffset + newline + 1);
            lineStart = newline + 1;
        }
        trailing = buffer.AsSpan(lineStart).ToArray();
        if (trailing.Length > 0) Metrics.PartialTrailingLines++;
    }

    void ProcessLine(string line, DateTime fileWriteUtc, long lineEndOffset)
    {
        if (line.Length == 0) return;
        Metrics.RecordsObserved++;
        try
        {
            var record = JsonNode.Parse(line) as JsonObject ?? throw new JsonException("Record is not an object.");
            var observed = DateTimeOffset.UtcNow;
            var sourceTime = PersistedDesktopMapper.Timestamp(record);
            mapper.Map(record, observed);
            var kind = PersistedDesktopMapper.MeasurementKind(record);
            if (kind is not null) measurements.Add(new PersistedMeasurement(observed, State.RootThreadId, PersistedDesktopMapper.ThreadId(record) ?? mapper.CurrentThreadId, kind,
                sourceTime, sourceTime is null ? null : (long?)(observed - sourceTime.Value).TotalMilliseconds, path, lineEndOffset, new DateTimeOffset(fileWriteUtc)));
        }
        catch (JsonException) { Metrics.ParseFailures++; }
    }

    void Rebuild()
    {
        Metrics.FullReparses++;
        lock (State.Gate)
        {
            State.Threads.Clear(); State.UnknownMessageCounts.Clear(); State.Models.Clear(); State.RateLimits.Clear(); State.ProtocolErrors.Clear(); State.RootThreadId = null;
        }
        trailing = []; offset = 0; prefix = null; measurements.Clear();
    }

    public PersistedWatchResult Result() { lock (gate) return new(State, Metrics, measurements.ToList()); }
    public void Dispose()
    {
        lock (gate) { if (disposed) return; disposed = true; }
        watcher?.Dispose(); reconciliation?.Dispose();
    }
}

public sealed class PersistedObserverMetrics
{
    public long WatcherNotifications { get; set; }
    public long Reconciliations { get; set; }
    public long BytesRead { get; set; }
    public long FullReparses { get; set; }
    public long PartialTrailingLines { get; set; }
    public long ParseFailures { get; set; }
    public long RecordsObserved { get; set; }
}

public sealed record PersistedMeasurement(DateTimeOffset ObservedTimestamp, string? RootThreadId, string? AgentId, string EventKind,
    DateTimeOffset? SourceEventTimestamp, long? DelayMs, string SourceFile, long FileOffset, DateTimeOffset FileWriteTimestamp);
public sealed record PersistedWatchResult(MissionState State, PersistedObserverMetrics Metrics, List<PersistedMeasurement> Measurements);

public sealed class PersistedDesktopMapper(MissionState state)
{
    long? historyStartOrdinal;
    public string? CurrentThreadId { get; private set; }

    public static DateTimeOffset? Timestamp(JsonObject record) => DateTimeOffset.TryParse(Text(record["timestamp"]), out var value) ? value : null;
    public static string? ThreadId(JsonObject record) => Text((record["payload"] as JsonObject)?["thread_id"]);
    public static string? MeasurementKind(JsonObject record)
    {
        var type = Text(record["type"]); var payload = record["payload"] as JsonObject;
        if (type is "session_meta" or "turn_context" or "token_usage_record") return type;
        if (type == "item_completed") return Text(payload?["item"]?["type"]) ?? type;
        if (type != "event_msg") return null;
        var eventType = Text(payload?["type"]);
        if (eventType == "item_completed")
        {
            var itemType = Text(payload?["item"]?["type"]) ?? eventType;
            var activity = itemType.Equals("SubAgentActivity", StringComparison.OrdinalIgnoreCase) ? Text(payload?["item"]?["kind"]) : null;
            return activity is null ? itemType : $"{itemType}.{activity}";
        }
        return eventType is "task_started" or "task_complete" or "turn_aborted" ? eventType : null;
    }

    public void Map(JsonObject record, DateTimeOffset observed)
    {
        var type = Text(record["type"]); var p = record["payload"] as JsonObject;
        if (type is null || p is null) return;
        lock (state.Gate)
        {
            if (type == "session_meta") { Session(p); return; }
            if (historyStartOrdinal is { } boundary && Number(record["ordinal"]) is { } ordinal && ordinal < boundary) return;
            if (type == "turn_context") { TurnContext(p, observed); return; }
            if (type == "token_usage_record") { Tokens(p); return; }
            if (type == "event_msg") { Event(p, observed); return; }
            if (type == "item_completed") Item(p, observed);
        }
    }

    void Session(JsonObject p)
    {
        var id = Text(p["id"]); if (id is null) return;
        CurrentThreadId = id; historyStartOrdinal = Number(p["subagent_history_start_ordinal"]);
        var source = p["source"] as JsonObject; var subagent = source?["subagent"] as JsonObject; var spawn = subagent?["thread_spawn"] as JsonObject;
        var agent = Agent(id); agent.SessionId = Text(p["session_id"]) ?? agent.SessionId; agent.ParentThreadId = Text(p["parent_thread_id"]) ?? Text(spawn?["parent_thread_id"]) ?? agent.ParentThreadId;
        agent.Cwd = Text(p["cwd"]) ?? agent.Cwd; agent.AgentPath = Text(p["agent_path"]) ?? Text(spawn?["agent_path"]) ?? agent.AgentPath;
        agent.Role = Text(p["agent_role"]) ?? Text(spawn?["agent_role"]) ?? agent.Role; agent.Nickname = Text(p["agent_nickname"]) ?? Text(spawn?["agent_nickname"]) ?? agent.Nickname;
        agent.SpawnDepth = (int?)Number(spawn?["depth"]) ?? agent.SpawnDepth;
        agent.ConfiguredModel = Text(p["model"]) ?? agent.ConfiguredModel;
        if (agent.ParentThreadId is null && agent.SessionId == id) state.RootThreadId = id;
        state.RootThreadId ??= agent.SessionId;
    }

    void TurnContext(JsonObject p, DateTimeOffset observed)
    {
        var agent = Agent(Text(p["thread_id"]) ?? CurrentThreadId ?? state.RootThreadId ?? "unknown-root"); var turn = agent.GetTurn(Text(p["turn_id"]) ?? "unknown-turn");
        agent.Cwd = Text(p["cwd"]) ?? agent.Cwd; agent.ConfiguredModel = Text(p["model"]) ?? agent.ConfiguredModel; agent.ReasoningEffort = Text(p["effort"]) ?? agent.ReasoningEffort;
        turn.StartedAt ??= TimestampFromMillis(p["started_at"]) ?? observed; turn.Status ??= "inProgress"; agent.Status = "running";
    }

    void Event(JsonObject p, DateTimeOffset observed)
    {
        var kind = Text(p["type"]); if (kind == "item_completed") { Item(p, observed); return; }
        var threadId = Text(p["thread_id"]) ?? CurrentThreadId ?? state.RootThreadId; if (kind is null || threadId is null) return;
        var agent = Agent(threadId); var turn = agent.GetTurn(Text(p["turn_id"]) ?? "unknown-turn");
        if (kind == "task_started") { turn.Status = "inProgress"; turn.StartedAt = TimestampFromSeconds(p["started_at"]) ?? turn.StartedAt ?? observed; agent.Status = "running"; }
        if (kind is "task_complete" or "turn_aborted") { turn.Status = kind == "task_complete" ? "completed" : "interrupted"; turn.CompletedAt = TimestampFromSeconds(p["completed_at"]) ?? observed; turn.DurationMs = Number(p["duration_ms"]) ?? turn.DurationMs; agent.Status = turn.Status; }
    }

    void Item(JsonObject p, DateTimeOffset observed)
    {
        var threadId = Text(p["thread_id"]) ?? CurrentThreadId ?? state.RootThreadId; var item = p["item"] as JsonObject; if (threadId is null || item is null || Text(item["id"]) is not { } id) return;
        var agent = Agent(threadId); var turnId = Text(p["turn_id"]); var type = Text(item["type"]) ?? "unknown";
        agent.Items[id] = new ActivityItem { Id = id, Type = type, TurnId = turnId, Status = Text(item["status"]) ?? "completed", Command = Text(item["command"]), Tool = Text(item["tool"]),
            ExitCode = (int?)Number(item["exit_code"]), DurationMs = Number(p["completed_at_ms"]) is { } end && Number(p["started_at_ms"]) is { } start ? end - start : null,
            AgentThreadId = Text(item["agent_thread_id"]), AgentPath = Text(item["agent_path"]) };
        if (type.Equals("SubAgentActivity", StringComparison.OrdinalIgnoreCase) && Text(item["agent_thread_id"]) is { } childId && childId != threadId)
        {
            var child = Agent(childId); child.ParentThreadId ??= threadId; child.SessionId ??= agent.SessionId; child.AgentPath ??= Text(item["agent_path"]);
            child.Status = Text(item["kind"]) switch { "started" => "running", "completed" => "completed", "errored" => "failed", _ => child.Status };
        }
    }

    void Tokens(JsonObject p)
    {
        var threadId = Text(p["thread_id"]) ?? state.RootThreadId; if (threadId is null) return;
        var agent = Agent(threadId); agent.Usage = Usage(p["thread_token_usage"] ?? p["usage"]);
        if (Text(p["turn_id"]) is { } turnId) agent.GetTurn(turnId).LatestUsage = agent.Usage;
    }

    AgentState Agent(string id) => state.Threads.TryGetValue(id, out var agent) ? agent : state.Threads[id] = new AgentState(id);
    static Usage Usage(JsonNode? node) => new() { InputTokens = Number(node?["input_tokens"]), CachedInputTokens = Number(node?["cached_input_tokens"]), CacheWriteInputTokens = Number(node?["cache_write_input_tokens"]), OutputTokens = Number(node?["output_tokens"]), ReasoningOutputTokens = Number(node?["reasoning_output_tokens"]), TotalTokens = Number(node?["total_tokens"]) };
    static DateTimeOffset? TimestampFromMillis(JsonNode? node) => Number(node) is { } value ? DateTimeOffset.FromUnixTimeMilliseconds(value) : null;
    static DateTimeOffset? TimestampFromSeconds(JsonNode? node) => Number(node) is { } value ? DateTimeOffset.FromUnixTimeSeconds(value) : null;
    static long? Number(JsonNode? node) => node is JsonValue value && value.TryGetValue<long>(out var result) ? result : null;
    static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var result) ? result : null;
}
