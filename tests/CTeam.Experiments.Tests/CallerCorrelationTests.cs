using System.Text.Json;
using System.Text.Json.Nodes;
using CTeam.Experiments;
using Xunit;

namespace CTeam.Experiments.Tests;

public sealed class CallerCorrelationTests : IDisposable
{
    readonly string scratch = Path.Combine(Path.GetTempPath(), "cteam-correlation-tests-" + Guid.NewGuid().ToString("N"));

    public CallerCorrelationTests() => Directory.CreateDirectory(Path.Combine(scratch, "sessions"));
    public void Dispose() { if (Directory.Exists(scratch)) Directory.Delete(scratch, true); }

    [Fact]
    public void Caller_context_reads_only_the_Codex_transport_extension()
    {
        var parameters = JsonNode.Parse("""{"_meta":{"x-codex-turn-metadata":{"thread_id":"thread-a","session_id":"session-a","workspaces":{"workspace":{}}}},"thread_id":"wrong"}""")!.AsObject();
        var caller = CallerContext.FromToolParameters(parameters);
        Assert.Equal("thread-a", caller.ThreadId);
        Assert.Equal("session-a", caller.SessionId);
        Assert.True(caller.HasWorkspaceMetadata);
    }

    [Fact]
    public void Exact_caller_id_resolves_one_root_rollout_by_session_meta_id()
    {
        Write("rollout-2026-09-06-thread-a.jsonl", SessionMeta("thread-a", "thread-a", null));
        var result = PersistedMissionResolver.ResolveExactCaller(new CallerContext("thread-a", "thread-a", false), scratch);
        Assert.Equal("exact", result.Outcome);
        Assert.Equal("caller-thread-id", result.SelectionSignal);
        Assert.Equal("root", result.CallerKind);
        Assert.Equal(result.MissionKey, result.RootMissionKey);
        Assert.Equal(1, result.CandidateCount);
        Assert.DoesNotContain("thread-a", result.MissionKey!);
    }

    [Fact]
    public void Exact_caller_id_reports_not_found_without_project_or_recency_fallback()
    {
        Write("rollout-2026-09-06-other.jsonl", SessionMeta("other", "other", null));
        var result = PersistedMissionResolver.ResolveExactCaller(new CallerContext("missing", null, true), scratch);
        Assert.Equal("not-found", result.Outcome);
        Assert.Null(result.MissionKey);
        Assert.Equal(0, result.CandidateCount);
    }

    [Fact]
    public void Duplicate_persisted_identity_is_ambiguous()
    {
        Write("rollout-first-duplicate.jsonl", SessionMeta("duplicate", "duplicate", null));
        Write("rollout-second-duplicate.jsonl", SessionMeta("duplicate", "duplicate", null));
        var result = PersistedMissionResolver.ResolveExactId("duplicate", codexHome: scratch);
        Assert.Equal("ambiguous", result.Outcome);
        Assert.Equal(2, result.CandidateCount);
        Assert.Null(result.MissionKey);
    }

    [Fact]
    public void Exact_child_identity_preserves_child_and_derives_root_from_session_metadata()
    {
        Write("rollout-child.jsonl", SessionMeta("child", "root", "root"));
        var result = PersistedMissionResolver.ResolveExactCaller(new CallerContext("child", "root", false), scratch);
        Assert.Equal("exact", result.Outcome);
        Assert.Equal("child", result.CallerKind);
        Assert.NotEqual(result.MissionKey, result.RootMissionKey);
        Assert.NotNull(result.RootMissionKey);
    }

    [Fact]
    public void Missing_context_uses_explicit_mission_before_project_hint_and_never_labels_hint_exact()
    {
        Write("rollout-explicit.jsonl", SessionMeta("explicit", "explicit", null));
        var explicitResult = CallerMissionProbe.Probe(new CallerContext(null, null, false), "D:\\hint", "explicit", scratch);
        Assert.Equal("exact", explicitResult.CorrelationOutcome);
        Assert.Equal("explicit-mission-id", explicitResult.CorrelationSelection);

        var hintResult = CallerMissionProbe.Probe(new CallerContext(null, null, false), "D:\\hint", null, scratch);
        Assert.Equal("context-assisted", hintResult.CorrelationOutcome);
        Assert.NotEqual("certain", hintResult.Confidence);

        var missingResult = CallerMissionProbe.Probe(new CallerContext(null, null, false), null, null, scratch);
        Assert.Equal("unresolved", missingResult.CorrelationOutcome);
        Assert.Equal("ambiguous", missingResult.Confidence);
    }

    [Fact]
    public void Exact_lookup_is_bounded_and_tolerates_open_writer_with_partial_trailing_line()
    {
        var path = Path.Combine(scratch, "sessions", "rollout-active.jsonl");
        File.WriteAllText(path, SessionMeta("active", "active", null) + "\n{\"type\":");
        using var writer = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        var result = PersistedMissionResolver.ResolveExactId("active", codexHome: scratch);
        Assert.Equal("exact", result.Outcome);
        Assert.InRange(result.ScannedFiles, 1, 8);
        Assert.InRange(result.BytesRead, 1, 64 * 1024);
        Assert.InRange(result.ExaminedDirectories, 1, 32);
        Assert.False(result.ScanTruncated);
    }

    [Fact]
    public void Exact_lookup_reports_the_eight_candidate_file_limit()
    {
        for (var index = 0; index < 9; index++)
            Write($"rollout-{index}-bounded.jsonl", SessionMeta("bounded", "bounded", null));

        var result = PersistedMissionResolver.ResolveExactId("bounded", codexHome: scratch);

        Assert.Equal("ambiguous", result.Outcome);
        Assert.Equal(8, result.ScannedFiles);
        Assert.Equal(8, result.CandidateCount);
        Assert.True(result.ScanTruncated);
    }

    [Fact]
    public void Directory_entry_limit_is_bounded_and_prevents_an_exact_claim()
    {
        Write("rollout-0-bounded.jsonl", SessionMeta("bounded", "bounded", null));
        Write("rollout-1-bounded.jsonl", SessionMeta("other-1", "other-1", null));
        Write("rollout-2-bounded.jsonl", SessionMeta("other-2", "other-2", null));

        var result = PersistedMissionResolver.ResolveExactId("bounded", codexHome: scratch, limits: new(31, 2, 8, 64 * 1024));

        Assert.Equal("ambiguous", result.Outcome);
        Assert.Equal(2, result.DirectoryEntriesExamined);
        Assert.Null(result.MissionKey);
        Assert.True(result.ScanTruncated);
    }

    [Fact]
    public void Identity_byte_limit_is_strict_and_prevents_an_exact_claim()
    {
        Write("rollout-bounded.jsonl", SessionMeta("bounded", "bounded", null));

        var result = PersistedMissionResolver.ResolveExactId("bounded", codexHome: scratch, limits: new(31, 8, 8, 32));

        Assert.Equal("ambiguous", result.Outcome);
        Assert.Equal(32, result.BytesRead);
        Assert.Null(result.MissionKey);
        Assert.True(result.ScanTruncated);
    }

    [Fact]
    public void Unreadable_candidate_prevents_an_exact_claim_even_when_another_candidate_matches()
    {
        var lockedPath = Path.Combine(scratch, "sessions", "rollout-0-locked.jsonl");
        File.WriteAllText(lockedPath, SessionMeta("locked", "locked", null));
        Write("rollout-1-locked.jsonl", SessionMeta("locked", "locked", null));
        using var exclusive = new FileStream(lockedPath, FileMode.Open, FileAccess.Read, FileShare.None);

        var result = PersistedMissionResolver.ResolveExactId("locked", codexHome: scratch);

        Assert.Equal("ambiguous", result.Outcome);
        Assert.Null(result.MissionKey);
        Assert.True(result.ScanTruncated);
    }

    [Fact]
    public void Truncated_candidate_set_never_claims_an_exact_match()
    {
        Write("rollout-0-bounded.jsonl", SessionMeta("bounded", "bounded", null));
        for (var index = 1; index < 9; index++)
            Write($"rollout-{index}-bounded.jsonl", SessionMeta($"other-{index}", $"other-{index}", null));

        var result = PersistedMissionResolver.ResolveExactId("bounded", codexHome: scratch);

        Assert.Equal("ambiguous", result.Outcome);
        Assert.Null(result.MissionKey);
        Assert.True(result.ScanTruncated);
    }

    [Fact]
    public void Conflicting_child_root_fields_do_not_guess_a_root()
    {
        Write("rollout-child.jsonl", SessionMeta("child", "root-a", "root-b"));

        var result = PersistedMissionResolver.ResolveExactId("child", codexHome: scratch);

        Assert.Equal("exact", result.Outcome);
        Assert.Equal("child", result.CallerKind);
        Assert.NotNull(result.MissionKey);
        Assert.Null(result.RootMissionKey);
    }

    [Fact]
    public async Task Mcp_mission_tool_uses_caller_metadata_automatically_and_exposes_only_sanitized_correlation()
    {
        Write("rollout-caller.jsonl", SessionMeta("caller", "caller", null));
        var old = Environment.GetEnvironmentVariable("CODEX_HOME");
        Environment.SetEnvironmentVariable("CODEX_HOME", scratch);
        try
        {
            var input = new StringReader("""{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"_meta":{"x-codex-turn-metadata":{"thread_id":"caller","session_id":"caller"}},"name":"cteam_get_current_mission","arguments":{}}}""" + Environment.NewLine);
            var output = new StringWriter();
            Assert.Equal(0, await McpServer.RunAsync(input, output, TextWriter.Null, TestContext.Current.CancellationToken));
            using var response = JsonDocument.Parse(output.ToString());
            var content = response.RootElement.GetProperty("result").GetProperty("structuredContent");
            Assert.Equal("exact", content.GetProperty("correlation_outcome").GetString());
            Assert.Equal("caller-thread-id", content.GetProperty("correlation_selection").GetString());
            Assert.Equal(1, content.GetProperty("correlation_directory_entries_examined").GetInt32());
            Assert.DoesNotContain("\"mission_key\":\"caller\"", output.ToString());
        }
        finally { Environment.SetEnvironmentVariable("CODEX_HOME", old); }
    }

    void Write(string file, string content) => File.WriteAllText(Path.Combine(scratch, "sessions", file), content);
    static string SessionMeta(string id, string sessionId, string? parent) => JsonSerializer.Serialize(new { timestamp = "2026-09-06T00:00:00Z", type = "session_meta", payload = new { id, session_id = sessionId, parent_thread_id = parent } });
}
