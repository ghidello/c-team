using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace CTeam.Spike.Codex;

public sealed class CodexEventMapper(MissionState state)
{
    readonly Dictionary<string, JsonObject> requests = new();

    public void Ingest(JsonNode message, string direction = "in", DateTimeOffset? timestamp = null)
    {
        if (message is JsonObject obj) lock (state.Gate) Map(obj, direction, timestamp);
    }

    void Map(JsonObject message, string direction, DateTimeOffset? timestamp)
    {
        var method = Text(message["method"]);
        var id = message["id"]?.ToJsonString();
        var p = message["params"] as JsonObject ?? new JsonObject();
        if (method is not null && id is not null)
        {
            if (direction == "out") requests[id] = (JsonObject)message.DeepClone();
            else Count("server-request:" + method);
            return;
        }
        if (id is not null)
        {
            if (direction != "in" || !requests.Remove(id, out var request)) return;
            if (message["error"] is { } error) { state.ProtocolErrors.Add(error.ToJsonString()); return; }
            if (message["result"] is JsonObject result) Response(request, result, timestamp);
            return;
        }
        if (method is null || direction != "in") return;
        if (method == "thread/started") { AddThread(p["thread"] as JsonObject, timestamp); return; }
        if (method == "account/rateLimits/updated") { Quotas(p); return; }
        var agent = Text(p["threadId"]) is { } threadId ? Agent(threadId) : null;
        var turnId = Text(p["turnId"]);
        if (method == "thread/status/changed" && agent is not null)
        {
            agent.RuntimeStatus = Text(p["status"]);
            if (p["status"] is JsonObject status && status["activeFlags"] is JsonArray flags && flags.Count > 0) agent.Status = "waiting";
            return;
        }
        if (method is "turn/started" or "turn/completed" && agent is not null)
        {
            MapTurn(agent, p["turn"] as JsonObject, timestamp, method == "turn/completed");
            return;
        }
        if (method == "thread/tokenUsage/updated" && agent is not null)
        {
            var usage = p["tokenUsage"];
            agent.Usage = Tokens(usage?["total"], usage?["modelContextWindow"]);
            agent.LastUsage = Tokens(usage?["last"], usage?["modelContextWindow"]);
            if (turnId is not null) agent.GetTurn(turnId).LatestUsage = agent.LastUsage;
            return;
        }
        if (method == "turn/plan/updated" && agent is not null)
        {
            agent.Plan = (p["plan"] as JsonArray)?.Select(s => new PlanStep(Text(s?["step"]) ?? "", Text(s?["status"]) ?? "unknown")).ToList() ?? [];
            agent.PlanExplanation = Text(p["explanation"]);
            if (turnId is not null) { var turn = agent.GetTurn(turnId); turn.Plan = agent.Plan.ToList(); turn.PlanExplanation = agent.PlanExplanation; }
            return;
        }
        if (method == "turn/diff/updated" && agent is not null)
        {
            agent.GetTurn(turnId ?? "unknown-turn").Diff = Diff(Text(p["diff"]) ?? "");
            return;
        }
        if (method is "item/started" or "item/completed" && agent is not null)
        {
            MapItem(agent, p["item"] as JsonObject, turnId);
            return;
        }
        if (method == "model/rerouted" && agent is not null && Text(p["toModel"]) is { } target)
        {
            Observe(agent, "model/rerouted.toModel", target, turnId, timestamp, Text(p["reason"]));
            return;
        }
        Count(method);
    }

    void Response(JsonObject request, JsonObject result, DateTimeOffset? timestamp)
    {
        var method = Text(request["method"]);
        var p = request["params"];
        if (method is "thread/start" or "thread/read" or "thread/resume")
        {
            var agent = AddThread(result["thread"] as JsonObject, timestamp);
            if (agent is null) return;
            if (Text(p?["model"]) is { } model) { agent.RequestedModel = model; Observe(agent, method + ".request.model", model, null, timestamp); }
            agent.ServiceTier = Text(result["serviceTier"]) ?? agent.ServiceTier;
            return;
        }
        if (method == "turn/start" && Text(p?["threadId"]) is { } threadId)
        {
            var agent = Agent(threadId);
            var turnId = Text(result["turn"]?["id"]);
            if (Text(p?["model"]) is { } model) { agent.RequestedModel = model; Observe(agent, "turn/start.request.model", model, turnId, timestamp); }
            agent.ReasoningEffort = Text(p?["effort"]) ?? agent.ReasoningEffort;
            agent.ServiceTier = Text(p?["serviceTier"]) ?? agent.ServiceTier;
            MapTurn(agent, result["turn"] as JsonObject, timestamp, false);
            return;
        }
        if (method == "review/start" && Text(result["reviewThreadId"]) is { } reviewId)
        {
            var agent = Agent(reviewId);
            agent.Role = "native-review";
            agent.ReviewOfThreadId = Text(p?["threadId"]);
            MapTurn(agent, result["turn"] as JsonObject, timestamp, false);
            return;
        }
        if (method == "model/list" && result["data"] is JsonArray models)
        {
            foreach (var m in models)
            {
                if (Text(m?["id"]) is not { } id || Text(m?["model"]) is not { } model) continue;
                state.Models.RemoveAll(x => x.Id == id);
                state.Models.Add(new ModelCapability(id, model, Text(m?["displayName"]), Bool(m?["hidden"]), Text(m?["defaultReasoningEffort"]),
                    Values(m?["supportedReasoningEfforts"], "reasoningEffort"), Values(m?["inputModalities"]), Values(m?["serviceTiers"], "id"), Text(m?["multiAgentVersion"])));
            }
        }
        if (method == "account/read") state.AccountType = Text(result["account"]?["type"]);
        if (method == "account/rateLimits/read") Quotas(result);
    }

    AgentState? AddThread(JsonObject? t, DateTimeOffset? timestamp)
    {
        if (t is null || Text(t["id"]) is not { } id) return null;
        var agent = Agent(id);
        agent.SessionId = Text(t["sessionId"]) ?? agent.SessionId;
        var spawn = t["source"] is JsonObject source && source["subAgent"] is JsonObject sub ? sub["thread_spawn"] : null;
        agent.ParentThreadId = Text(t["parentThreadId"]) ?? Text(spawn?["parent_thread_id"]) ?? agent.ParentThreadId;
        agent.SpawnDepth = (int?)Number(spawn?["depth"]) ?? agent.SpawnDepth;
        agent.Role = Text(t["agentRole"]) ?? agent.Role;
        agent.Nickname = Text(t["agentNickname"]) ?? agent.Nickname;
        agent.Cwd = Text(t["cwd"]) ?? agent.Cwd;
        if (Text(t["model"]) is { } model) { agent.ConfiguredModel = model; Observe(agent, "Thread.model (configuration)", model, null, timestamp); }
        agent.ReasoningEffort = Text(t["reasoningEffort"]) ?? agent.ReasoningEffort;
        agent.RuntimeStatus = Text(t["status"]) ?? agent.RuntimeStatus;
        agent.CreatedAt = Unix(t["createdAt"]) ?? agent.CreatedAt;
        if (t["turns"] is JsonArray turns)
            foreach (var turn in turns.OfType<JsonObject>()) MapTurn(agent, turn, null, false);
        return agent;
    }

    void MapTurn(AgentState agent, JsonObject? t, DateTimeOffset? timestamp, bool completed)
    {
        if (t is null || Text(t["id"]) is not { } id) return;
        var turn = agent.GetTurn(id);
        if (!turn.CompletedNotificationReceived || completed) turn.Status = Text(t["status"]) ?? turn.Status;
        var start = Unix(t["startedAt"]);
        var end = Unix(t["completedAt"]);
        turn.StartedAt = start ?? turn.StartedAt ?? timestamp;
        turn.CompletedAt = end ?? turn.CompletedAt ?? (completed ? timestamp : null);
        turn.DurationMs = Number(t["durationMs"]) ?? turn.DurationMs;
        turn.TimingSource = start is not null || end is not null ? "protocol" : turn.TimingSource ?? "recorded-receive-time";
        turn.CompletedNotificationReceived |= completed;
        turn.Error = Text(t["error"]?["message"]) ?? turn.Error;
        agent.Status = turn.Status == "inProgress" ? "running" : turn.Status ?? agent.Status;
        if (t["items"] is JsonArray items) foreach (var item in items.OfType<JsonObject>()) MapItem(agent, item, id);
    }

    void MapItem(AgentState agent, JsonObject? item, string? turnId)
    {
        if (item is null || Text(item["id"]) is not { } id) return;
        if (turnId is not null) agent.GetTurn(turnId);
        var type = Text(item["type"]) ?? "unknown";
        var files = new List<FileActivity>();
        if (item["changes"] is JsonArray changes)
            foreach (var file in changes)
            {
                var diff = Diff(Text(file?["diff"]) ?? "");
                files.Add(new FileActivity(Text(file?["path"]) ?? "unknown", Text(file?["kind"]), diff.Added, diff.Removed));
            }
        agent.Items[id] = new ActivityItem { Id = id, Type = type, TurnId = turnId, Status = Text(item["status"]),
            Command = Text(item["command"]), Tool = Text(item["tool"]), Server = Text(item["server"]), Namespace = Text(item["namespace"]),
            ExitCode = (int?)Number(item["exitCode"]), DurationMs = Number(item["durationMs"]), AgentThreadId = Text(item["agentThreadId"]),
            AgentPath = Text(item["agentPath"]), Files = files };
        if (type == "subAgentActivity" && Text(item["agentThreadId"]) is { } childId && childId != agent.ThreadId)
        {
            var child = Agent(childId);
            if (Text(item["kind"]) == "started" && child.ParentThreadId is null)
            {
                child.ParentThreadId = agent.ThreadId;
                child.SessionId ??= agent.SessionId;
                child.AgentPath = Text(item["agentPath"]);
                child.SpawnDepth = (agent.SpawnDepth ?? 0) + 1;
            }
        }
        if (type == "enteredReviewMode") agent.Role ??= "native-review";
    }

    void Observe(AgentState agent, string source, string model, string? turnId, DateTimeOffset? timestamp, string? reason = null)
        => agent.ModelObservations.Add(new ModelObservation(source, model, turnId is null ? "thread" : "turn", turnId, timestamp, reason));

    void Quotas(JsonObject result)
    {
        if (result["rateLimitsByLimitId"] is JsonObject buckets)
            foreach (var bucket in buckets) AddQuota(bucket.Key, bucket.Value);
        if (result["rateLimits"] is { } legacy && Text(legacy["limitId"]) is { } id) AddQuota(id, legacy);
    }

    void AddQuota(string id, JsonNode? bucket) => state.RateLimits[id] = new QuotaBucket(id, Text(bucket?["limitName"]),
        bucket?["primary"]?["usedPercent"]?.GetValue<double?>(), Number(bucket?["primary"]?["windowDurationMins"]), Number(bucket?["primary"]?["resetsAt"]));

    AgentState Agent(string id) => state.Threads.TryGetValue(id, out var agent) ? agent : state.Threads[id] = new AgentState(id);
    void Count(string method) => state.UnknownMessageCounts[method] = state.UnknownMessageCounts.GetValueOrDefault(method) + 1;
    static string[] Values(JsonNode? node, string? field = null) => node is JsonArray array
        ? array.Select(x => Text(field is null ? x : x?[field])).OfType<string>().ToArray() : [];
    static bool? Bool(JsonNode? node) => node is JsonValue value && value.TryGetValue<bool>(out var b) ? b : null;
    static long? Number(JsonNode? node) => node is JsonValue value && value.TryGetValue<long>(out var n) ? n : null;
    static DateTimeOffset? Unix(JsonNode? node) => Number(node) is { } n ? DateTimeOffset.FromUnixTimeSeconds(n) : null;
    static string? Text(JsonNode? node) => node switch
    {
        JsonValue value when value.TryGetValue<string>(out var text) => text,
        JsonObject obj => Text(obj["type"]),
        _ => null
    };

    static Usage Tokens(JsonNode? value, JsonNode? context) => new()
    {
        TotalTokens = Number(value?["totalTokens"]), InputTokens = Number(value?["inputTokens"]),
        CachedInputTokens = Number(value?["cachedInputTokens"]), CacheWriteInputTokens = Number(value?["cacheWriteInputTokens"]),
        OutputTokens = Number(value?["outputTokens"]), ReasoningOutputTokens = Number(value?["reasoningOutputTokens"]), ContextWindow = Number(context)
    };

    public static DiffSummary Diff(string diff)
    {
        var paths = new List<string>(); var added = 0; var removed = 0; var inHunk = false;
        foreach (var rawLine in diff.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.StartsWith("diff --git ", StringComparison.Ordinal))
            {
                inHunk = false;
                var match = Regex.Match(line, "^diff --git (?:\\\"a/(.*?)\\\"|a/(.*?)) (?:\\\"b/(.*?)\\\"|b/(.*))$");
                paths.Add(match.Success ? (match.Groups[3].Success ? match.Groups[3].Value : match.Groups[4].Value) : line[11..]);
            }
            else if (line.StartsWith("@@", StringComparison.Ordinal)) inHunk = true;
            else if (inHunk && line.StartsWith('+')) added++;
            else if (inHunk && line.StartsWith('-')) removed++;
        }
        return new DiffSummary(paths, added, removed);
    }
}
