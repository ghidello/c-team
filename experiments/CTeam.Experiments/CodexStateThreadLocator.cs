using System.Diagnostics;
using System.Text.Json.Nodes;

namespace CTeam.Experiments;

public sealed record CodexStateThreadLookup(
    string Outcome,
    string? DatabasePath,
    string? Cwd,
    string? ProjectId,
    string? ParentThreadId,
    bool IsChild,
    IReadOnlyList<string> ProjectRoots,
    int MatchingRows,
    bool IdPrimaryKey,
    long ElapsedMicroseconds);

public static class CodexStateThreadLocator
{
    public static CodexStateThreadLookup Lookup(string? threadId, string? codexHome = null, string? databasePath = null)
    {
        var timer = Stopwatch.StartNew();
        if (string.IsNullOrWhiteSpace(threadId))
            return Result("missing-thread-id", databasePath, timer);

        try
        {
            databasePath ??= FindLatestStateDatabase(codexHome);
            if (databasePath is null || !File.Exists(databasePath))
                return Result("database-absent", databasePath, timer);

            using var database = new SqliteReadOnlyDatabase(databasePath);
            var tables = database.Query("SELECT name FROM sqlite_schema WHERE type = 'table'").Select(row => Text(row, "name")).Where(name => name is not null).ToHashSet(StringComparer.Ordinal);
            if (!tables.Contains("threads"))
                return Result("incompatible-schema", databasePath, timer);

            var columns = database.Query("PRAGMA table_info(threads)");
            var names = columns.Select(row => Text(row, "name")).Where(name => name is not null).ToHashSet(StringComparer.Ordinal);
            var idPrimaryKey = columns.Any(row => Text(row, "name") == "id" && Integer(row, "pk") == 1);
            if (!idPrimaryKey || !names.Contains("cwd"))
                return Result("incompatible-schema", databasePath, timer, idPrimaryKey: idPrimaryKey);

            var projectExpression = names.Contains("project_id") ? "t.project_id" : "NULL";
            var parentExpression = tables.Contains("thread_spawn_edges") ? "e.parent_thread_id" : "NULL";
            var join = tables.Contains("thread_spawn_edges") ? "LEFT JOIN thread_spawn_edges e ON e.child_thread_id = t.id" : string.Empty;
            var rows = database.Query($"SELECT t.cwd, {projectExpression} AS project_id, {parentExpression} AS parent_thread_id FROM threads t {join} WHERE t.id = ?1 LIMIT 2", threadId);
            if (rows.Count == 0)
                return Result("not-found", databasePath, timer, idPrimaryKey: true);
            if (rows.Count != 1)
                return Result("ambiguous", databasePath, timer, matchingRows: rows.Count, idPrimaryKey: true);

            var cwd = Text(rows[0], "cwd");
            if (string.IsNullOrWhiteSpace(cwd))
                return Result("blank-cwd", databasePath, timer, matchingRows: 1, idPrimaryKey: true);

            var projectId = Text(rows[0], "project_id");
            var parentThreadId = Text(rows[0], "parent_thread_id");
            var projectRoots = Array.Empty<string>();
            if (!string.IsNullOrWhiteSpace(projectId) && tables.Contains("project_roots"))
                projectRoots = database.Query("SELECT path FROM project_roots WHERE project_id = ?1 ORDER BY position", projectId)
                    .Select(row => Text(row, "path")).Where(path => !string.IsNullOrWhiteSpace(path)).Cast<string>().ToArray();

            timer.Stop();
            return new("exact", databasePath, cwd, projectId, parentThreadId, parentThreadId is not null, projectRoots, 1, true, ToMicroseconds(timer.ElapsedTicks));
        }
        catch (Exception exception) when (exception is SqliteReadException or IOException or UnauthorizedAccessException)
        {
            return Result("read-failed", databasePath, timer);
        }
    }

    public static string? FindLatestStateDatabase(string? codexHome = null)
    {
        var root = MissionProbe.ResolveCodexHome(codexHome);
        if (!Directory.Exists(root))
            return null;
        return Directory.EnumerateFiles(root, "state_*.sqlite", SearchOption.TopDirectoryOnly)
            .Select(path => new { Path = path, Version = ParseVersion(path) })
            .Where(item => item.Version is not null)
            .OrderByDescending(item => item.Version)
            .Select(item => item.Path)
            .FirstOrDefault();
    }

    static int? ParseVersion(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return name.StartsWith("state_", StringComparison.Ordinal) && int.TryParse(name.AsSpan(6), out var version) ? version : null;
    }

    static CodexStateThreadLookup Result(string outcome, string? databasePath, Stopwatch timer, int matchingRows = 0, bool idPrimaryKey = false)
    {
        timer.Stop();
        return new(outcome, databasePath, null, null, null, false, [], matchingRows, idPrimaryKey, ToMicroseconds(timer.ElapsedTicks));
    }

    static long ToMicroseconds(long ticks) => ticks * 1_000_000 / Stopwatch.Frequency;
    static string? Text(JsonObject row, string property) => row[property]?.GetValue<string>();
    static long Integer(JsonObject row, string property) => row[property]?.GetValue<long>() ?? 0;
}
