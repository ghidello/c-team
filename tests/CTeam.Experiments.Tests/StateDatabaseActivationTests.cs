using System.Text.Json;
using CTeam.Experiments;
using Xunit;

namespace CTeam.Experiments.Tests;

[Collection(EnvironmentVariableCollection.Name)]
public sealed class StateDatabaseActivationTests : IDisposable
{
    readonly string scratch = Path.Combine(Path.GetTempPath(), "cteam-state-db-tests-" + Guid.NewGuid().ToString("N"));
    readonly string codexHome;
    readonly string project;
    readonly string databasePath;

    public StateDatabaseActivationTests()
    {
        codexHome = Path.Combine(scratch, "codex");
        project = Path.Combine(scratch, "project");
        databasePath = Path.Combine(codexHome, "state_5.sqlite");
        Directory.CreateDirectory(codexHome);
        Directory.CreateDirectory(project);
        Directory.CreateDirectory(Path.Combine(project, ".git"));
    }

    public void Dispose()
    {
        if (Directory.Exists(scratch))
            Directory.Delete(scratch, true);
    }

    [Fact]
    public void Exact_root_and_child_rows_use_the_primary_key_and_child_cwd_is_self_sufficient()
    {
        using (var database = CreateCompatibleDatabase())
        {
            database.Execute($"INSERT INTO threads(id, cwd, project_id) VALUES ('root', {Quote(project)}, NULL), ('child', {Quote(project)}, NULL)");
            database.Execute("INSERT INTO thread_spawn_edges(parent_thread_id, child_thread_id, status) VALUES ('root', 'child', 'completed')");
        }

        var root = CodexStateThreadLocator.Lookup("root", databasePath: databasePath);
        var child = CodexStateThreadLocator.Lookup("child", databasePath: databasePath);

        Assert.Equal("exact", root.Outcome);
        Assert.True(root.IdPrimaryKey);
        Assert.Equal(project, root.Cwd);
        Assert.False(root.IsChild);
        Assert.Equal("exact", child.Outcome);
        Assert.True(child.IsChild);
        Assert.Equal(project, child.Cwd);
        Assert.Equal("root", child.ParentThreadId);

        using var readOnly = new SqliteReadOnlyDatabase(databasePath);
        Assert.Throws<SqliteReadException>(() => readOnly.Query("DELETE FROM threads"));
        Assert.Single(readOnly.Query("SELECT id FROM threads WHERE id = ?1", "root"));
    }

    [Fact]
    public async Task Same_mcp_process_uses_the_db_fast_path_and_recognizes_marker_transition_without_a_second_tool_list()
    {
        using (var database = CreateCompatibleDatabase())
            database.Execute($"INSERT INTO threads(id, cwd, project_id) VALUES ('caller', {Quote(project)}, NULL)");
        var original = Environment.GetEnvironmentVariable("CODEX_HOME");
        Environment.SetEnvironmentVariable("CODEX_HOME", codexHome);
        try
        {
            var call = "{\"jsonrpc\":\"2.0\",\"id\":ID,\"method\":\"tools/call\",\"params\":{\"_meta\":{\"x-codex-turn-metadata\":{\"thread_id\":\"caller\",\"session_id\":\"caller\"}},\"name\":\"cteam\",\"arguments\":{\"action\":\"status\"}}}";
            var input = new CallbackTextReader(
                () => "{\"jsonrpc\":\"2.0\",\"id\":0,\"method\":\"tools/list\"}",
                () => call.Replace("ID", "1", StringComparison.Ordinal),
                () =>
                {
                    Directory.CreateDirectory(Path.Combine(project, ".cteam"));
                    return call.Replace("ID", "2", StringComparison.Ordinal);
                },
                () => null);
            var output = new StringWriter();

            Assert.Equal(0, await ActivationMcpServer.RunAsync(input, output, TextWriter.Null, TestContext.Current.CancellationToken));

            var messages = output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(line => JsonDocument.Parse(line)).ToArray();
            Assert.Single(messages, message => message.RootElement.GetProperty("id").GetInt32() == 0);
            var results = messages.Where(message => message.RootElement.GetProperty("id").GetInt32() is 1 or 2)
                .Select(message => message.RootElement.GetProperty("result").GetProperty("structuredContent").Clone()).ToArray();
            Assert.Equal("project_not_enabled", results[0].GetProperty("status").GetString());
            Assert.Equal("project_enabled", results[1].GetProperty("status").GetString());
            Assert.All(results, result =>
            {
                Assert.Equal("codex-state-db", result.GetProperty("resolution_source").GetString());
                Assert.Equal(1, result.GetProperty("database_rows_read").GetInt32());
                Assert.Equal(0, result.GetProperty("rollout_files_read").GetInt32());
                Assert.False(result.GetProperty("persisted_mission_read").GetBoolean());
                Assert.Equal("git-root", result.GetProperty("project_boundary").GetString());
            });
            Assert.Equal(results[0].GetProperty("pid").GetInt32(), results[1].GetProperty("pid").GetInt32());
            Assert.Equal(results[0].GetProperty("process_started_at").GetString(), results[1].GetProperty("process_started_at").GetString());
        }
        finally
        {
            Environment.SetEnvironmentVariable("CODEX_HOME", original);
        }
    }

    [Fact]
    public void Database_absent_incompatible_missing_blank_and_stale_cases_fall_back_or_remain_unresolved()
    {
        WriteRollout("fallback", project);
        var absent = ActivationProbe.Probe(new CallerContext("fallback", "fallback", false), codexHome, Path.Combine(codexHome, "missing.sqlite"));
        AssertFallback(absent);

        using (var incompatibleDatabase = new SqliteFixture(databasePath))
            incompatibleDatabase.Execute("CREATE TABLE threads(id TEXT, cwd TEXT)");
        var incompatible = ActivationProbe.Probe(new CallerContext("fallback", "fallback", false), codexHome, databasePath);
        AssertFallback(incompatible);
        Assert.Equal("incompatible-schema", incompatible.DatabaseOutcome);
        File.Delete(databasePath);

        using (var database = CreateCompatibleDatabase())
            database.Execute($"INSERT INTO threads(id, cwd, project_id) VALUES ('blank', '', NULL), ('stale', {Quote(Path.Combine(scratch, "gone"))}, NULL)");
        WriteRollout("blank", project);
        WriteRollout("stale", project);

        AssertFallback(ActivationProbe.Probe(new CallerContext("blank", "blank", false), codexHome, databasePath));
        AssertFallback(ActivationProbe.Probe(new CallerContext("stale", "stale", false), codexHome, databasePath));
        var missingRow = ActivationProbe.Probe(new CallerContext("unknown", "unknown", false), codexHome, databasePath);
        Assert.Equal("project_unresolved", missingRow.Status);
        Assert.Equal(0, missingRow.RolloutFilesRead);

        var missingId = ActivationProbe.Probe(new CallerContext(null, null, false), codexHome, databasePath);
        Assert.Equal("project_unresolved", missingId.Status);
        Assert.Equal("missing-thread-id", missingId.DatabaseOutcome);
        Assert.Equal(0, missingId.RolloutFilesRead);
    }

    [Fact]
    public void Ambiguous_project_roots_and_inaccessible_marker_do_not_guess()
    {
        var nested = Path.Combine(project, "nested");
        Directory.CreateDirectory(nested);
        using (var database = CreateCompatibleDatabase())
        {
            database.Execute($"INSERT INTO threads(id, cwd, project_id) VALUES ('ambiguous', {Quote(nested)}, 'project')");
            database.Execute($"INSERT INTO project_roots(project_id, position, path) VALUES ('project', 0, {Quote(project)}), ('project', 1, {Quote(nested)})");
            database.Execute($"INSERT INTO threads(id, cwd, project_id) VALUES ('inaccessible', {Quote(project)}, NULL)");
        }

        var ambiguous = ActivationProbe.Probe(new CallerContext("ambiguous", "ambiguous", false), codexHome, databasePath);
        Assert.Equal("project_unresolved", ambiguous.Status);
        Assert.False(ambiguous.ProjectResolved);

        bool Access(string path) => path.EndsWith(Path.DirectorySeparatorChar + ".cteam", StringComparison.OrdinalIgnoreCase)
            ? throw new UnauthorizedAccessException()
            : Directory.Exists(path);
        var inaccessible = ActivationProbe.Probe(new CallerContext("inaccessible", "inaccessible", false), codexHome, databasePath, Access);
        Assert.Equal("project_unresolved", inaccessible.Status);
        Assert.False(inaccessible.ProjectResolved);
    }

    [Fact]
    public void Busy_database_falls_back_to_the_exact_rollout_adapter()
    {
        using var database = CreateCompatibleDatabase();
        database.Execute("PRAGMA journal_mode = DELETE");
        database.Execute($"INSERT INTO threads(id, cwd, project_id) VALUES ('busy', {Quote(project)}, NULL)");
        WriteRollout("busy", project);
        database.Execute("BEGIN EXCLUSIVE");

        var result = ActivationProbe.Probe(new CallerContext("busy", "busy", false), codexHome, databasePath);

        AssertFallback(result);
        Assert.Equal("read-failed", result.DatabaseOutcome);
    }

    [Fact]
    public void Child_with_stale_cwd_can_use_its_exact_parent_row()
    {
        using (var database = CreateCompatibleDatabase())
        {
            database.Execute($"INSERT INTO threads(id, cwd, project_id) VALUES ('root', {Quote(project)}, NULL), ('child', {Quote(Path.Combine(scratch, "gone"))}, NULL)");
            database.Execute("INSERT INTO thread_spawn_edges(parent_thread_id, child_thread_id, status) VALUES ('root', 'child', 'running')");
        }

        var result = ActivationProbe.Probe(new CallerContext("child", "root", false), codexHome, databasePath);

        Assert.Equal("project_not_enabled", result.Status);
        Assert.Equal("codex-state-db-parent", result.ResolutionSource);
        Assert.True(result.CallerIsChild);
        Assert.True(result.ParentAssisted);
        Assert.Equal(2, result.DatabaseRowsRead);
        Assert.Equal(0, result.RolloutFilesRead);
    }

    [Fact]
    public void Latest_state_database_selection_is_numeric_and_upward_normalization_is_bounded()
    {
        File.WriteAllText(Path.Combine(codexHome, "state_2.sqlite"), string.Empty);
        File.WriteAllText(Path.Combine(codexHome, "state_10.sqlite"), string.Empty);
        File.WriteAllText(Path.Combine(codexHome, "state_bad.sqlite"), string.Empty);
        Assert.EndsWith("state_10.sqlite", CodexStateThreadLocator.FindLatestStateDatabase(codexHome));

        var nested = project;
        for (var index = 0; index < 4; index++)
            nested = Path.Combine(nested, $"level-{index}");
        Directory.CreateDirectory(nested);
        var bounded = ProjectRootNormalizer.Resolve(nested, maximumLevels: 2);
        Assert.Equal("normalization-limit", bounded.Outcome);
        Assert.Null(bounded.Root);
    }

    SqliteFixture CreateCompatibleDatabase()
    {
        var database = new SqliteFixture(databasePath);
        database.Execute("CREATE TABLE threads(id TEXT PRIMARY KEY, cwd TEXT NOT NULL, project_id TEXT)");
        database.Execute("CREATE TABLE thread_spawn_edges(parent_thread_id TEXT NOT NULL, child_thread_id TEXT NOT NULL PRIMARY KEY, status TEXT NOT NULL)");
        database.Execute("CREATE TABLE project_roots(project_id TEXT NOT NULL, position INTEGER NOT NULL, path TEXT NOT NULL, PRIMARY KEY(project_id, position))");
        return database;
    }

    void WriteRollout(string id, string cwd)
    {
        var directory = Path.Combine(codexHome, "sessions", DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"), DateTime.UtcNow.ToString("dd"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, $"rollout-test-{id}.jsonl"), JsonSerializer.Serialize(new
        {
            timestamp = DateTimeOffset.UtcNow,
            type = "session_meta",
            payload = new { id, session_id = id, cwd }
        }));
    }

    static void AssertFallback(ActivationSnapshot result)
    {
        Assert.Equal("project_not_enabled", result.Status);
        Assert.Equal("exact-rollout-fallback", result.ResolutionSource);
        Assert.Equal(1, result.RolloutFilesRead);
        Assert.True(result.PersistedMissionRead);
    }

    static string Quote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    sealed class CallbackTextReader(params Func<string?>[] reads) : TextReader
    {
        int index;
        public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken) => ValueTask.FromResult(index < reads.Length ? reads[index++]() : null);
    }
}
