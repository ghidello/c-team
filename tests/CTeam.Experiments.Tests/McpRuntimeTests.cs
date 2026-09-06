using System.Text.Json;
using CTeam.Experiments;
using Xunit;

namespace CTeam.Experiments.Tests;

[Collection(EnvironmentVariableCollection.Name)]
public sealed class McpRuntimeTests : IDisposable
{
    readonly string scratch = Path.Combine(Path.GetTempPath(), "cteam-mcp-tests-" + Guid.NewGuid().ToString("N"));
    public McpRuntimeTests() => Directory.CreateDirectory(scratch);
    public void Dispose() { if (Directory.Exists(scratch)) Directory.Delete(scratch, true); }

    [Fact]
    public async Task Initialize_with_roots_sends_roots_list_and_tools_are_callable()
    {
        var input = new StringReader("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"capabilities\":{\"roots\":{}}}}\n{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}\n{\"jsonrpc\":\"2.0\",\"id\":\"cteam-roots-1\",\"result\":{\"roots\":[]}}\n{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\"}\n{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"cteam_ping\",\"arguments\":{}}}\n");
        var output = new StringWriter();
        Assert.Equal(0, await McpServer.RunAsync(input, output, TextWriter.Null, TestContext.Current.CancellationToken));
        var messages = output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => JsonDocument.Parse(line)).ToArray();
        Assert.Equal("2025-06-18", messages[0].RootElement.GetProperty("result").GetProperty("protocolVersion").GetString());
        Assert.Contains(messages, x => x.RootElement.TryGetProperty("method", out var method) && method.GetString() == "roots/list");
        var tools = messages.Single(x => x.RootElement.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number && id.GetInt32() == 2);
        Assert.Contains(tools.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray(), x => x.GetProperty("name").GetString() == "cteam_probe_current_mission");
        foreach (var name in new[] { "cteam_get_current_mission", "cteam_get_agent_tree", "cteam_get_usage" })
            Assert.True(tools.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray().Single(x => x.GetProperty("name").GetString() == name).TryGetProperty("outputSchema", out _));
        var ping = messages.Single(x => x.RootElement.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number && id.GetInt32() == 3);
        Assert.True(ping.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("ok").GetBoolean());
        Assert.DoesNotContain(messages, x => x.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task Initialize_without_roots_does_not_request_roots()
    {
        var input = new StringReader("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"capabilities\":{}}}\n{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}\n");
        var output = new StringWriter();
        Assert.Equal(0, await McpServer.RunAsync(input, output, TextWriter.Null, TestContext.Current.CancellationToken));
        var messages = output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => JsonDocument.Parse(line)).ToArray();
        Assert.DoesNotContain(messages, x => x.RootElement.TryGetProperty("method", out var method) && method.GetString() == "roots/list");
    }

    [Fact]
    public async Task Roots_error_response_is_consumed_without_a_spurious_reply()
    {
        var input = new StringReader("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"capabilities\":{\"roots\":{}}}}\n{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}\n{\"jsonrpc\":\"2.0\",\"id\":\"cteam-roots-1\",\"error\":{\"code\":-32601,\"message\":\"unsupported\"}}\n{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\"}\n");
        var output = new StringWriter();
        Assert.Equal(0, await McpServer.RunAsync(input, output, TextWriter.Null, TestContext.Current.CancellationToken));
        var messages = output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => JsonDocument.Parse(line)).ToArray();
        Assert.DoesNotContain(messages, x => x.RootElement.TryGetProperty("error", out _));
        Assert.Contains(messages, x => x.RootElement.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number && id.GetInt32() == 2);
    }

    [Fact]
    public async Task Invalid_tool_arguments_return_an_error_and_the_server_continues()
    {
        var input = new StringReader("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":\"cteam_ping\",\"arguments\":{\"hold_ms\":\"bad\"}}}\n{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/call\",\"params\":{\"name\":\"not_a_tool\",\"arguments\":{}}}\n{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"cteam_ping\",\"arguments\":[]}}\n{\"jsonrpc\":\"2.0\",\"id\":4,\"method\":\"tools/call\",\"params\":{\"name\":\"cteam_ping\",\"arguments\":{}}}\n");
        var output = new StringWriter();
        Assert.Equal(0, await McpServer.RunAsync(input, output, TextWriter.Null, TestContext.Current.CancellationToken));
        var messages = output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => JsonDocument.Parse(line)).ToArray();
        Assert.True(messages[0].RootElement.GetProperty("result").GetProperty("isError").GetBoolean());
        Assert.True(messages[1].RootElement.GetProperty("result").GetProperty("isError").GetBoolean());
        Assert.True(messages[2].RootElement.GetProperty("result").GetProperty("isError").GetBoolean());
        Assert.False(messages[3].RootElement.GetProperty("result").GetProperty("isError").GetBoolean());
        Assert.True(messages[3].RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("ok").GetBoolean());
    }

    [Fact]
    public async Task Evidence_logging_can_record_the_same_protocol_message_more_than_once()
    {
        var evidence = Path.Combine(scratch, "evidence");
        var original = Environment.GetEnvironmentVariable("CTEAM_EXPERIMENT_EVIDENCE");
        Environment.SetEnvironmentVariable("CTEAM_EXPERIMENT_EVIDENCE", evidence);
        try
        {
            var input = new StringReader("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"capabilities\":{}}}\n");
            var output = new StringWriter();
            Assert.Equal(0, await McpServer.RunAsync(input, output, TextWriter.Null, TestContext.Current.CancellationToken));
            Assert.Contains("\"result\"", output.ToString());
            var log = File.ReadAllText(Assert.Single(Directory.GetFiles(evidence, "*.jsonl")));
            Assert.Contains("\"message-received\"", log);
            Assert.Contains("\"initialize\"", log);
            Assert.Contains("\"message-sent\"", log);
            Assert.DoesNotContain("\"process-error\"", log);
        }
        finally { Environment.SetEnvironmentVariable("CTEAM_EXPERIMENT_EVIDENCE", original); }
    }

    [Fact]
    public void Mission_selection_uses_record_timestamps_and_never_exposes_identifier()
    {
        var sessions = Path.Combine(scratch, "sessions"); Directory.CreateDirectory(sessions);
        Write(Path.Combine(sessions, "old.jsonl"), Record("2026-01-01T00:00:00Z", "one", "D:\\project-a", "completed", 11));
        Write(Path.Combine(sessions, "new.jsonl"), Record("2026-03-01T00:00:00Z", "two", "D:\\project-a", "running", 22));
        File.SetLastWriteTimeUtc(Path.Combine(sessions, "old.jsonl"), DateTime.UtcNow.AddDays(2));
        var hinted = MissionProbe.Probe("D:\\project-a", null, scratch);
        Assert.Equal("ambiguous", hinted.Confidence); Assert.Equal(2, hinted.CandidateCount); Assert.Equal("running", hinted.Status); Assert.Equal(22, hinted.TotalTokens); Assert.DoesNotContain("two", hinted.MissionKey!);
        var explicitId = MissionProbe.Probe(null, "one", scratch);
        Assert.Equal("certain", explicitId.Confidence); Assert.Equal("mission_id", explicitId.SelectionSignal);
        Assert.Equal("ambiguous", MissionProbe.Probe(null, null, scratch).Confidence);
    }

    [Fact]
    public void Mission_selection_ignores_child_rollouts_and_counts_root_subagent_activity()
    {
        var sessions = Path.Combine(scratch, "sessions"); Directory.CreateDirectory(sessions);
        var root = Record("2026-03-01T00:00:00Z", "root", "D:\\project-a", "running", 22) + "\n" +
            JsonSerializer.Serialize(new { timestamp = "2026-03-01T00:00:01Z", type = "event_msg", payload = new { thread_id = "root", item = new { type = "SubAgentActivity", agent_thread_id = "child" } } });
        var child = JsonSerializer.Serialize(new { timestamp = "2026-03-01T00:00:02Z", type = "session_meta", payload = new { id = "child", session_id = "root", parent_thread_id = "root", cwd = "D:\\project-a" } });
        Write(Path.Combine(sessions, "root.jsonl"), root); Write(Path.Combine(sessions, "child.jsonl"), child);
        var result = MissionProbe.Probe("D:\\project-a", null, scratch);
        Assert.Equal(1, result.CandidateCount); Assert.Equal(2, result.AgentCount); Assert.Equal("high-confidence", result.Confidence);
    }

    [Fact]
    public void Mission_selection_tolerates_an_incomplete_appended_record()
    {
        var sessions = Path.Combine(scratch, "sessions"); Directory.CreateDirectory(sessions);
        Write(Path.Combine(sessions, "active.jsonl"), Record("2026-03-01T00:00:00Z", "root", "D:\\project-a", "running", 22) + "\n{\"timestamp\":");
        var result = MissionProbe.Probe("D:\\project-a", null, scratch);
        Assert.Equal("high-confidence", result.Confidence);
        Assert.Equal("running", result.Status);
        Assert.Equal(22, result.TotalTokens);
    }

    [Fact]
    public void Mission_selection_reads_a_rollout_held_open_for_writing()
    {
        var sessions = Path.Combine(scratch, "sessions"); Directory.CreateDirectory(sessions);
        var path = Path.Combine(sessions, "active.jsonl");
        Write(path, Record("2026-03-01T00:00:00Z", "root", "D:\\project-a", "running", 22));
        using var writer = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        var result = MissionProbe.Probe("D:\\project-a", null, scratch);
        Assert.Equal("high-confidence", result.Confidence);
        Assert.Equal("running", result.Status);
    }

    [Fact]
    public void Mission_selection_reports_when_the_history_scan_is_bounded()
    {
        var sessions = Path.Combine(scratch, "sessions"); Directory.CreateDirectory(sessions);
        for (var index = 0; index < 65; index++)
        {
            var path = Path.Combine(sessions, $"root-{index:D2}.jsonl");
            Write(path, Record($"2026-03-01T00:{index % 60:D2}:00Z", $"root-{index:D2}", $"D:\\project-{index:D2}", "completed", index));
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(index));
        }
        var result = MissionProbe.Probe(null, null, scratch);
        Assert.True(result.ScanTruncated);
        Assert.Equal(64, result.ScannedFiles);
        Assert.Equal(64, result.CandidateCount);
        Assert.Equal("ambiguous", result.Confidence);
    }

    [Fact]
    public async Task Runtime_info_filters_environment_and_plugin_data_requires_plugin_data()
    {
        var input = new StringReader("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":\"cteam_test_plugin_data\",\"arguments\":{}}}\n"); var output = new StringWriter();
        var original = Environment.GetEnvironmentVariable("PLUGIN_DATA");
        var originalSession = Environment.GetEnvironmentVariable("CODEX_SESSION_ID");
        Environment.SetEnvironmentVariable("PLUGIN_DATA", null);
        Environment.SetEnvironmentVariable("CODEX_SESSION_ID", "private-session-id");
        try
        {
            await McpServer.RunAsync(input, output, TextWriter.Null, TestContext.Current.CancellationToken);
            using var result = JsonDocument.Parse(output.ToString());
            Assert.False(result.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("available").GetBoolean());

            Environment.SetEnvironmentVariable("PLUGIN_DATA", scratch);
            input = new StringReader("{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/call\",\"params\":{\"name\":\"cteam_test_plugin_data\",\"arguments\":{}}}\n"); output = new StringWriter();
            await McpServer.RunAsync(input, output, TextWriter.Null, TestContext.Current.CancellationToken);
            using var available = JsonDocument.Parse(output.ToString());
            Assert.True(available.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("wrote_marker").GetBoolean());
            Assert.True(File.Exists(Path.Combine(scratch, "cteam-experiment-005.marker")));

            input = new StringReader("{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"cteam_runtime_info\",\"arguments\":{}}}\n"); output = new StringWriter();
            await McpServer.RunAsync(input, output, TextWriter.Null, TestContext.Current.CancellationToken);
            using var runtime = JsonDocument.Parse(output.ToString());
            Assert.Equal("[redacted]", runtime.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("environment").GetProperty("CODEX_SESSION_ID").GetString());
        }
        finally
        {
            Environment.SetEnvironmentVariable("PLUGIN_DATA", original);
            Environment.SetEnvironmentVariable("CODEX_SESSION_ID", originalSession);
        }
    }

    static string Record(string timestamp, string id, string cwd, string status, int total) => string.Join("\n", JsonSerializer.Serialize(new { timestamp, type = "session_meta", payload = new { id, session_id = id, cwd, model = "gpt-test" } }),
        JsonSerializer.Serialize(new { timestamp, type = "turn_context", payload = new { thread_id = id, cwd, model = "gpt-test", effort = "high" } }),
        JsonSerializer.Serialize(new { timestamp, type = "event_msg", payload = new { type = status == "running" ? "task_started" : "task_complete", thread_id = id } }),
        JsonSerializer.Serialize(new { timestamp, type = "token_usage_record", payload = new { thread_id = id, thread_token_usage = new { total_tokens = total } } }));
    static void Write(string path, string content) => File.WriteAllText(path, content);
}
