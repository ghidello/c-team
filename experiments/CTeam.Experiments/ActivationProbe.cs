namespace CTeam.Experiments;

public sealed record ActivationSnapshot(
    string Status,
    bool ProjectResolved,
    int WorkspaceCount,
    bool MarkerChecked,
    bool PersistedMissionRead,
    string ResolutionSource,
    string? DatabaseOutcome,
    int DatabaseRowsRead,
    long DatabaseLookupMicroseconds,
    int RolloutFilesRead,
    string? ProjectBoundary,
    int NormalizationLevels,
    bool CallerIsChild,
    bool ParentAssisted);

public static class ActivationProbe
{
    public static ActivationSnapshot Probe(CallerContext caller, string? codexHome = null, string? stateDatabasePath = null,
        Func<string, bool>? directoryExists = null)
    {
        directoryExists ??= Directory.Exists;
        var workspaces = (caller.WorkspaceRoots ?? []).Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (workspaces.Length == 1)
            return FromLocation(workspaces[0], [], "caller-workspace", workspaces.Length, null, 0, 0, 0, false, false, directoryExists);
        if (workspaces.Length > 1)
            return Unresolved("project_ambiguous", "caller-workspace", workspaces.Length, null, 0, 0, 0, false, false);

        var database = CodexStateThreadLocator.Lookup(caller.ThreadId, codexHome, stateDatabasePath);
        if (database.Outcome == "exact")
        {
            var direct = FromLocation(database.Cwd, database.ProjectRoots, "codex-state-db", 0, database.Outcome, 1, database.ElapsedMicroseconds, 0,
                database.IsChild, false, directoryExists);
            if (direct.ProjectResolved)
                return direct;

            if (database.IsChild && database.ParentThreadId is not null)
            {
                var parent = CodexStateThreadLocator.Lookup(database.ParentThreadId, codexHome, stateDatabasePath);
                if (parent.Outcome == "exact")
                {
                    var assisted = FromLocation(parent.Cwd, parent.ProjectRoots, "codex-state-db-parent", 0, database.Outcome, 2,
                        database.ElapsedMicroseconds + parent.ElapsedMicroseconds, 0, true, true, directoryExists);
                    if (assisted.ProjectResolved)
                        return assisted;
                }
            }
        }

        var rollout = PersistedCallerLocationResolver.Resolve(caller, codexHome);
        if (rollout.Outcome == "exact")
            return FromLocation(rollout.Cwd, [], "exact-rollout-fallback", 0, database.Outcome, database.MatchingRows, database.ElapsedMicroseconds,
                rollout.RolloutFilesRead, rollout.IsChild, false, directoryExists);

        return Unresolved("project_unresolved", "unresolved", 0, database.Outcome, database.MatchingRows, database.ElapsedMicroseconds,
            rollout.RolloutFilesRead, database.IsChild || rollout.IsChild, false);
    }

    static ActivationSnapshot FromLocation(string? cwd, IReadOnlyList<string> projectRoots, string source, int workspaceCount, string? databaseOutcome,
        int databaseRowsRead, long databaseMicroseconds, int rolloutFilesRead, bool callerIsChild, bool parentAssisted, Func<string, bool> directoryExists)
    {
        ProjectRootResolution root;
        try
        {
            root = ProjectRootNormalizer.Resolve(cwd, projectRoots, directoryExists: directoryExists);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Unresolved("project_unresolved", source, workspaceCount, databaseOutcome, databaseRowsRead, databaseMicroseconds, rolloutFilesRead,
                callerIsChild, parentAssisted);
        }
        if (root.Outcome != "exact" || root.Root is null)
            return Unresolved(root.Outcome == "ambiguous" ? "project_ambiguous" : "project_unresolved", source, workspaceCount, databaseOutcome,
                databaseRowsRead, databaseMicroseconds, rolloutFilesRead, callerIsChild, parentAssisted);

        try
        {
            var enabled = directoryExists(Path.Combine(root.Root, ".cteam"));
            return new(enabled ? "project_enabled" : "project_not_enabled", true, workspaceCount, true, rolloutFilesRead > 0, source, databaseOutcome,
                databaseRowsRead, databaseMicroseconds, rolloutFilesRead, root.Boundary, root.LevelsExamined, callerIsChild, parentAssisted);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Unresolved("project_unresolved", source, workspaceCount, databaseOutcome, databaseRowsRead, databaseMicroseconds, rolloutFilesRead,
                callerIsChild, parentAssisted);
        }
    }

    static ActivationSnapshot Unresolved(string status, string source, int workspaceCount, string? databaseOutcome, int databaseRowsRead,
        long databaseMicroseconds, int rolloutFilesRead, bool callerIsChild, bool parentAssisted) =>
        new(status, false, workspaceCount, false, rolloutFilesRead > 0, source, databaseOutcome, databaseRowsRead, databaseMicroseconds, rolloutFilesRead,
            null, 0, callerIsChild, parentAssisted);
}
