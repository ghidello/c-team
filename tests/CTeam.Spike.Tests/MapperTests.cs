using System.Text.Json;
using System.Text.Json.Nodes;
using CTeam.Spike.Codex;
using Xunit;

namespace CTeam.Spike.Tests;

public class MapperTests
{
    [Fact]
    public void Usage_replaces_and_missing_values_are_unknown()
    {
        var t = New(); t.Ingest(Thread("t"));
        t.Ingest(Message("thread/tokenUsage/updated", new { threadId = "t", turnId = "one", tokenUsage = new { total = new { totalTokens = 10, inputTokens = 8 }, last = new { outputTokens = 2 } } }));
        t.Ingest(Message("thread/tokenUsage/updated", new { threadId = "t", turnId = "one", tokenUsage = new { total = new { totalTokens = 12, inputTokens = 9 }, last = new { outputTokens = 3 } } }));
        Assert.Equal(12, t.State.Threads["t"].Usage.TotalTokens); Assert.Null(t.State.Threads["t"].Usage.CachedInputTokens); Assert.Equal(3, t.State.Threads["t"].LastUsage.OutputTokens);
    }

    [Fact]
    public void String_request_id_keeps_requested_and_configured_models()
    {
        var t = New(); t.Ingest(JsonSerializer.SerializeToNode(new { id = "s", method = "thread/start", @params = new { model = "requested" } })!, "out");
        t.Ingest(JsonSerializer.SerializeToNode(new { id = "s", result = new { thread = new { id = "t", sessionId = "s", model = "configured", status = new { type = "idle" }, createdAt = 1 } } })!, "in");
        Assert.Equal("requested", t.State.Threads["t"].RequestedModel); Assert.Equal("configured", t.State.Threads["t"].ConfiguredModel);
    }

    [Fact]
    public void Reroute_is_not_effective_execution_and_has_turn_scope()
    {
        var t = New(); t.Ingest(Message("model/rerouted", new { threadId = "t", turnId = "turn", toModel = "fallback" }));
        Assert.Null(t.State.Threads["t"].EffectiveModel); Assert.Equal("turn", Assert.Single(t.State.Threads["t"].ModelObservations).TurnId);
    }

    [Fact]
    public void Subagent_interaction_does_not_reverse_parentage()
    {
        var t = New(); t.Ingest(Thread("root")); t.Ingest(Message("item/started", new { threadId = "root", item = new { id = "spawn", type = "subAgentActivity", kind = "started", agentThreadId = "child", agentPath = "/root/child" } })); t.Ingest(Message("item/started", new { threadId = "child", item = new { id = "reply", type = "subAgentActivity", kind = "interacted", agentThreadId = "root" } }));
        Assert.Null(t.State.Threads["root"].ParentThreadId); Assert.Equal("root", t.State.Threads["child"].ParentThreadId);
    }

    [Fact]
    public void Diff_counts_only_hunk_lines_and_file_changes_fallback_when_no_turn_diff_arrives()
    {
        var t = New(); t.Ingest(Thread("t"));
        t.Ingest(Message("turn/diff/updated", new { threadId = "t", turnId = "one", diff = "diff --git a/a.cs b/a.cs\n--- a/a.cs\n+++ b/a.cs\n@@ -1 +1 @@\n-old\n+new\n+++counter" }));
        t.Ingest(Message("item/completed", new { threadId = "t", turnId = "two", item = new { id = "files", type = "fileChange", status = "completed", changes = new[] { new { path = "deleted.txt", kind = "delete", diff = "diff --git a/deleted.txt b/deleted.txt\n@@ -1 +0,0 @@\n-old" } } } }));
        Assert.Equal(2, t.State.Threads["t"].Turns["one"].Diff!.Added); Assert.Equal(1, t.State.Threads["t"].Turns["one"].Diff!.Removed); Assert.Equal(2, t.State.Threads["t"].FilesChanged); Assert.Equal(2, t.State.Threads["t"].Removed);
    }

    [Fact]
    public void Thread_read_hydrates_child_and_review_response_links_source()
    {
        var t = New(); t.Ingest(Message("item/started", new { threadId = "root", item = new { id = "spawn", type = "subAgentActivity", kind = "started", agentThreadId = "child" } }));
        t.Ingest(JsonSerializer.SerializeToNode(new { id = 1, method = "thread/read", @params = new { threadId = "child" } })!, "out"); t.Ingest(JsonSerializer.SerializeToNode(new { id = 1, result = new { thread = new { id = "child", agentRole = "explorer", agentNickname = "FACE", status = new { type = "idle" }, createdAt = 1 } } })!, "in");
        t.Ingest(JsonSerializer.SerializeToNode(new { id = 2, method = "review/start", @params = new { threadId = "root" } })!, "out"); t.Ingest(JsonSerializer.SerializeToNode(new { id = 2, result = new { reviewThreadId = "review", turn = new { id = "r" } } })!, "in");
        Assert.Equal("root", t.State.Threads["child"].ParentThreadId); Assert.Equal("FACE", t.State.Threads["child"].Nickname); Assert.Equal("root", t.State.Threads["review"].ReviewOfThreadId);
    }

    [Fact]
    public async Task Raw_string_recording_replays_through_the_same_mapper()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cteam-{Guid.NewGuid():N}.jsonl");
        await using (var recorder = new Recorder(path))
        {
            await recorder.WriteAsync("out", JsonSerializer.SerializeToNode(new { id = "s", method = "thread/start", @params = new { model = "requested" } })!);
            await recorder.WriteRawAsync("in", JsonSerializer.Serialize(new { id = "s", result = new { thread = new { id = "t", model = "configured", status = new { type = "idle" }, createdAt = 1 } } }));
        }
        var replay = await Replay.ReadAsync(path);
        Assert.Equal("requested", replay.Threads["t"].RequestedModel); Assert.Equal("configured", replay.Threads["t"].ConfiguredModel);
        File.Delete(path);
    }

    static TestMapper New() => new();
    static JsonNode Thread(string id) => Message("thread/started", new { thread = new { id, sessionId = "s", parentThreadId = (string?)null, agentRole = "planner", agentNickname = "HANNIBAL", model = "configured", status = new { type = "idle" }, createdAt = 100L } });
    static JsonNode Message(string method, object parameters) => JsonSerializer.SerializeToNode(new { method, @params = parameters })!;
    sealed class TestMapper { public MissionState State { get; } = new(); readonly CodexEventMapper mapper; public TestMapper() => mapper = new(State); public void Ingest(JsonNode message, string direction = "in") => mapper.Ingest(message, direction, DateTimeOffset.Parse("2026-01-01T00:00:00Z")); }
}
