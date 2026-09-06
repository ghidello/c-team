namespace CTeam.Experiments;

public sealed record ActivationSnapshot(
    string Status,
    bool ProjectResolved,
    int WorkspaceCount,
    bool MarkerChecked,
    bool PersistedMissionRead);

public static class ActivationProbe
{
    public static ActivationSnapshot Probe(CallerContext caller)
    {
        var roots = (caller.WorkspaceRoots ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (roots.Length == 0)
            return new("project_unresolved", false, 0, false, false);
        if (roots.Length > 1)
            return new("project_ambiguous", false, roots.Length, false, false);

        var enabled = Directory.Exists(Path.Combine(roots[0], ".cteam"));
        return new(enabled ? "project_enabled" : "project_not_enabled", true, 1, true, false);
    }
}
