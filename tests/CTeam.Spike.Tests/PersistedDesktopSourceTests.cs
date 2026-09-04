using System.Text.Json;
using CTeam.Spike;
using Xunit;

namespace CTeam.Spike.Tests;

public sealed class PersistedDesktopSourceTests : IDisposable
{
    readonly string directory = Path.Combine(Path.GetTempPath(), "cteam-persisted-" + Guid.NewGuid().ToString("N"));
    readonly string path;
    public PersistedDesktopSourceTests() { Directory.CreateDirectory(directory); path = Path.Combine(directory, "rollout.jsonl"); }
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

    [Fact]
    public async Task Incremental_append_and_duplicate_reconciliation_observe_each_record_once()
    {
        await Write(Session("root"));
        using var source = new PersistedDesktopSource(path);
        await source.InitializeAsync();
        await Append(Event("task_started", "root", "turn"));
        await source.SynchronizeAsync(false);
        await source.SynchronizeAsync(true);
        Assert.Equal("running", source.State.Threads["root"].Status);
        Assert.Equal(2, source.Metrics.RecordsObserved);
        Assert.Equal(1, source.Metrics.Reconciliations);
    }

    [Fact]
    public async Task Partial_line_is_buffered_until_its_newline_arrives()
    {
        await File.WriteAllTextAsync(path, Session("root") + "\n" + Event("task_started", "root", "turn"), TestContext.Current.CancellationToken);
        using var source = new PersistedDesktopSource(path);
        await source.InitializeAsync();
        Assert.DoesNotContain("turn", source.State.Threads["root"].Turns.Keys);
        await File.AppendAllTextAsync(path, "\n", TestContext.Current.CancellationToken); await source.SynchronizeAsync(false);
        Assert.Equal("running", source.State.Threads["root"].Status);
        Assert.True(source.Metrics.PartialTrailingLines > 0);
    }

    [Fact]
    public async Task Reconciliation_reads_missed_append_and_restart_reconstructs_state()
    {
        await Write(Session("root"), Event("task_started", "root", "turn"), Token("root", "turn", 10));
        using (var first = new PersistedDesktopSource(path))
        {
            await first.InitializeAsync();
            await Append(Token("root", "turn", 12));
            await first.SynchronizeAsync(true);
            Assert.Equal(12, first.State.Threads["root"].Usage.TotalTokens);
        }
        using var restarted = new PersistedDesktopSource(path);
        await restarted.InitializeAsync();
        Assert.Equal("running", restarted.State.Threads["root"].Status);
        Assert.Equal(12, restarted.State.Threads["root"].Usage.TotalTokens);
    }

    [Fact]
    public async Task Truncation_or_replacement_rebuilds_from_new_file()
    {
        await Write(Session("root"), Event("task_started", "root", "old"));
        using var source = new PersistedDesktopSource(path);
        await source.InitializeAsync();
        await File.WriteAllTextAsync(path, Session("replacement") + "\n" + Event("task_complete", "replacement", "new") + "\n", TestContext.Current.CancellationToken);
        await source.SynchronizeAsync(true);
        Assert.DoesNotContain("root", source.State.Threads.Keys);
        Assert.Equal("completed", source.State.Threads["replacement"].Status);
        Assert.True(source.Metrics.FullReparses > 0);
    }

    [Fact]
    public async Task Child_activity_is_owned_by_child_and_parent_is_preserved()
    {
        await Write(Session("root"), Item("root", "turn", "SubAgentActivity", "spawn", "child"), Session("child", "root", "ba", "LOCKE"), Item("child", "inherited", "CommandExecution", "command", null));
        using var source = new PersistedDesktopSource(path);
        await source.InitializeAsync();
        var child = source.State.Threads["child"];
        Assert.Equal("root", child.ParentThreadId);
        Assert.Equal("ba", child.Role);
        Assert.Equal("LOCKE", child.Nickname);
        Assert.DoesNotContain(source.State.Threads["root"].Items.Values, x => x.Id == "command");
        Assert.Single(child.Items);
    }

    [Fact]
    public async Task Real_child_metadata_skips_inherited_history_and_uses_child_for_unscoped_lifecycle()
    {
        var metadata = JsonSerializer.Serialize(new { timestamp = "2026-09-04T21:00:00.000Z", ordinal = 0, type = "session_meta", payload = new
        {
            id = "child", session_id = "root", parent_thread_id = "root", subagent_history_start_ordinal = 2,
            source = new { subagent = new { thread_spawn = new { parent_thread_id = "root", depth = 1, agent_path = "/root/child", agent_nickname = "LOCKE", agent_role = "ba" } } }
        } });
        var inherited = JsonSerializer.Serialize(new { timestamp = "2026-09-04T21:00:00.000Z", ordinal = 1, type = "event_msg", payload = new { type = "task_complete", turn_id = "parent-turn", completed_at = 1788555600L } });
        var owned = JsonSerializer.Serialize(new { timestamp = "2026-09-04T21:00:01.000Z", ordinal = 2, type = "event_msg", payload = new { type = "task_started", turn_id = "child-turn", started_at = 1788555601L } });
        await Write(metadata, inherited, owned);
        using var source = new PersistedDesktopSource(path);
        await source.InitializeAsync();
        var child = source.State.Threads["child"];
        Assert.Equal("ba", child.Role); Assert.Equal("LOCKE", child.Nickname); Assert.Equal(1, child.SpawnDepth);
        Assert.DoesNotContain("parent-turn", child.Turns.Keys); Assert.Equal("running", child.Status);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1788555601L), child.Turns["child-turn"].StartedAt);
    }

    [Fact]
    public async Task Utf8_character_split_across_reads_remains_valid()
    {
        await Write(Session("root"));
        using var source = new PersistedDesktopSource(path);
        await source.InitializeAsync();
        var line = JsonSerializer.Serialize(new { timestamp = "2026-09-04T21:00:00.000Z", type = "event_msg", payload = new { type = "task_started", turn_id = "t", note = "café", started_at = 1788555600L } }) + "\n";
        var bytes = System.Text.Encoding.UTF8.GetBytes(line); var split = Array.IndexOf(bytes, (byte)0xC3) + 1;
        await using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { await stream.WriteAsync(bytes.AsMemory(0, split), TestContext.Current.CancellationToken); }
        await source.SynchronizeAsync(false);
        await using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { await stream.WriteAsync(bytes.AsMemory(split), TestContext.Current.CancellationToken); }
        await source.SynchronizeAsync(false);
        Assert.Equal("running", source.State.Threads["root"].Status); Assert.Equal(0, source.Metrics.ParseFailures);
    }

    [Fact]
    public async Task Cumulative_token_snapshots_replace_prior_totals()
    {
        await Write(Session("root"), Token("root", "turn", 10), Token("root", "turn", 15));
        using var source = new PersistedDesktopSource(path);
        await source.InitializeAsync();
        Assert.Equal(15, source.State.Threads["root"].Usage.TotalTokens);
        Assert.Equal(15, source.State.Threads["root"].Turns["turn"].LatestUsage!.TotalTokens);
    }

    [Fact]
    public async Task Root_string_source_and_interrupted_turn_are_supported()
    {
        var metadata = JsonSerializer.Serialize(new { timestamp = "2026-09-04T21:00:00.000Z", ordinal = 0, type = "session_meta", payload = new { id = "root", session_id = "root", source = "vscode" } });
        await Write(metadata, Event("task_started", "root", "turn"), Event("turn_aborted", "root", "turn"));
        using var source = new PersistedDesktopSource(path);
        await source.InitializeAsync();
        Assert.Equal("interrupted", source.State.Threads["root"].Status);
        Assert.Equal("interrupted", source.State.Threads["root"].Turns["turn"].Status);
    }

    Task Write(params string[] lines) => File.WriteAllTextAsync(path, string.Join("\n", lines) + "\n", TestContext.Current.CancellationToken);
    Task Append(string line) => File.AppendAllTextAsync(path, line + "\n", TestContext.Current.CancellationToken);
    static string Record(string type, object payload) => JsonSerializer.Serialize(new { timestamp = "2026-09-04T21:00:00.000Z", type, payload });
    static string Session(string id, string? parent = null, string? role = null, string? nickname = null) => Record("session_meta", new { id, session_id = parent is null ? id : "root", parent_thread_id = parent, agent_role = role, agent_nickname = nickname, model = "gpt-test" });
    static string Event(string type, string thread, string turn) => Record("event_msg", new { type, thread_id = thread, turn_id = turn, started_at = 1788555600L, completed_at = 1788555601L, duration_ms = 1000L });
    static string Token(string thread, string turn, long total) => Record("token_usage_record", new { thread_id = thread, turn_id = turn, thread_token_usage = new { total_tokens = total, input_tokens = total - 1, output_tokens = 1 } });
    static string Item(string thread, string turn, string type, string id, string? child) => Record("event_msg", new { type = "item_completed", thread_id = thread, turn_id = turn, started_at_ms = 1L, completed_at_ms = 2L, item = new { type, id, kind = type == "SubAgentActivity" ? "started" : null, agent_thread_id = child } });
}
