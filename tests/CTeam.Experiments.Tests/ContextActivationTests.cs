using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CTeam.Experiments;
using Xunit;

namespace CTeam.Experiments.Tests;

[Collection(EnvironmentVariableCollection.Name)]
public sealed class ContextActivationTests : IDisposable
{
    readonly string scratch = Path.Combine(Path.GetTempPath(), "cteam-activation-tests-" + Guid.NewGuid().ToString("N"));

    public ContextActivationTests() => Directory.CreateDirectory(scratch);

    public void Dispose()
    {
        if (Directory.Exists(scratch))
            Directory.Delete(scratch, true);
    }

    [Fact]
    public void Caller_metadata_preserves_workspace_object_keys()
    {
        var parameters = JsonNode.Parse("""{"_meta":{"x-codex-turn-metadata":{"thread_id":"thread","session_id":"session","workspaces":{"D:\\one":{},"D:\\two":{}}}}}""")!.AsObject();

        var caller = CallerContext.FromToolParameters(parameters);

        Assert.True(caller.HasWorkspaceMetadata);
        Assert.Equal([@"D:\one", @"D:\two"], caller.WorkspaceRoots);
    }

    [Fact]
    public void Marker_transition_is_visible_to_the_same_probe_without_persisted_mission_reads()
    {
        var caller = new CallerContext("thread", "session", true, [scratch]);

        var inactive = ActivationProbe.Probe(caller);
        Directory.CreateDirectory(Path.Combine(scratch, ".cteam"));
        var active = ActivationProbe.Probe(caller);

        Assert.Equal("project_not_enabled", inactive.Status);
        Assert.Equal("project_enabled", active.Status);
        Assert.True(inactive.MarkerChecked);
        Assert.True(active.MarkerChecked);
        Assert.False(inactive.PersistedMissionRead);
        Assert.False(active.PersistedMissionRead);
    }

    [Fact]
    public void Missing_or_multiple_workspaces_are_not_guessed()
    {
        var missing = ActivationProbe.Probe(new CallerContext("thread", "session", false));
        var ambiguous = ActivationProbe.Probe(new CallerContext("thread", "session", true, [scratch, Path.Combine(scratch, "other")]));

        Assert.Equal("project_unresolved", missing.Status);
        Assert.False(missing.MarkerChecked);
        Assert.Equal("project_ambiguous", ambiguous.Status);
        Assert.False(ambiguous.MarkerChecked);
    }

    [Fact]
    public async Task Activation_server_advertises_one_fixed_compact_tool_and_inactive_call_does_not_read_missions()
    {
        var workspace = JsonSerializer.Serialize(scratch);
        var input = new StringReader(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"clientInfo\":{\"name\":\"test-client\",\"version\":\"1\"},\"capabilities\":{}}}\n" +
            "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}\n" +
            "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\"}\n" +
            "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"_meta\":{\"x-codex-turn-metadata\":{\"thread_id\":\"thread\",\"session_id\":\"session\",\"workspaces\":{" + workspace + ":{}}}},\"name\":\"cteam\",\"arguments\":{\"action\":\"status\"}}}\n");
        var output = new StringWriter();

        Assert.Equal(0, await ActivationMcpServer.RunAsync(input, output, TextWriter.Null, TestContext.Current.CancellationToken));

        var messages = output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => JsonDocument.Parse(line)).ToArray();
        var tools = messages.Single(message => message.RootElement.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number && id.GetInt32() == 2)
            .RootElement.GetProperty("result").GetProperty("tools");
        var definition = Assert.Single(tools.EnumerateArray());
        Assert.Equal("cteam", definition.GetProperty("name").GetString());
        Assert.Equal(["status", "mission", "agents", "usage", "open"], definition.GetProperty("inputSchema").GetProperty("properties").GetProperty("action").GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
        var compactDefinition = definition.GetRawText();
        Assert.True(Encoding.UTF8.GetByteCount(compactDefinition) < 500);

        var call = messages.Single(message => message.RootElement.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number && id.GetInt32() == 3)
            .RootElement.GetProperty("result").GetProperty("structuredContent");
        Assert.Equal("project_not_enabled", call.GetProperty("status").GetString());
        Assert.False(call.GetProperty("persisted_mission_read").GetBoolean());
        Assert.True(call.GetProperty("marker_checked").GetBoolean());
    }

    [Fact]
    public async Task Same_mcp_server_recognizes_a_marker_created_between_calls()
    {
        var workspace = JsonSerializer.Serialize(scratch);
        var call = "{\"jsonrpc\":\"2.0\",\"id\":ID,\"method\":\"tools/call\",\"params\":{\"_meta\":{\"x-codex-turn-metadata\":{\"workspaces\":{" + workspace + ":{}}}},\"name\":\"cteam\",\"arguments\":{\"action\":\"status\"}}}";
        var input = new CallbackTextReader(
            () => call.Replace("ID", "1", StringComparison.Ordinal),
            () =>
            {
                Directory.CreateDirectory(Path.Combine(scratch, ".cteam"));
                return call.Replace("ID", "2", StringComparison.Ordinal);
            },
            () => null);
        var output = new StringWriter();

        Assert.Equal(0, await ActivationMcpServer.RunAsync(input, output, TextWriter.Null, TestContext.Current.CancellationToken));

        var results = output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonDocument.Parse(line).RootElement.GetProperty("result").GetProperty("structuredContent").Clone()).ToArray();
        Assert.Equal("project_not_enabled", results[0].GetProperty("status").GetString());
        Assert.Equal("project_enabled", results[1].GetProperty("status").GetString());
        Assert.Equal(results[0].GetProperty("pid").GetInt32(), results[1].GetProperty("pid").GetInt32());
        Assert.Equal(results[0].GetProperty("process_started_at").GetString(), results[1].GetProperty("process_started_at").GetString());
    }

    [Fact]
    public async Task Activation_evidence_records_zero_persisted_mission_reads()
    {
        var evidence = Path.Combine(scratch, "evidence");
        var original = Environment.GetEnvironmentVariable("CTEAM_EXPERIMENT_EVIDENCE");
        Environment.SetEnvironmentVariable("CTEAM_EXPERIMENT_EVIDENCE", evidence);
        try
        {
            var workspace = JsonSerializer.Serialize(scratch);
            var input = new StringReader("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"_meta\":{\"x-codex-turn-metadata\":{\"workspaces\":{" + workspace + ":{}}}},\"name\":\"cteam\",\"arguments\":{\"action\":\"status\"}}}\n");

            Assert.Equal(0, await ActivationMcpServer.RunAsync(input, TextWriter.Null, TextWriter.Null, TestContext.Current.CancellationToken));

            var log = File.ReadAllText(Assert.Single(Directory.GetFiles(evidence, "*.jsonl")));
            Assert.Contains("\"activation-checked\"", log);
            Assert.Contains("\"persisted_mission_reads\":0", log);
            Assert.Contains("\"rollout_files_read\":0", log);
            Assert.DoesNotContain("exact-rollout-fallback", log, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CTEAM_EXPERIMENT_EVIDENCE", original);
        }
    }

    sealed class CallbackTextReader(params Func<string?>[] reads) : TextReader
    {
        int index;

        public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult(index < reads.Length ? reads[index++]() : null);
    }
}
