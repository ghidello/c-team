namespace CTeam.Experiments;

public sealed record ProjectRootResolution(string Outcome, string? Root, string? Boundary, int LevelsExamined);

public static class ProjectRootNormalizer
{
    public const int DefaultMaximumLevels = 32;

    public static ProjectRootResolution Resolve(string? exactCwd, IReadOnlyList<string>? projectRoots = null, int maximumLevels = DefaultMaximumLevels,
        Func<string, bool>? directoryExists = null)
    {
        if (maximumLevels < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumLevels));
        directoryExists ??= Directory.Exists;
        if (string.IsNullOrWhiteSpace(exactCwd))
            return new("missing-cwd", null, null, 0);

        string cwd;
        try
        {
            cwd = Normalize(exactCwd);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new("invalid-cwd", null, null, 0);
        }
        if (!directoryExists(cwd))
            return new("stale-cwd", null, null, 0);

        string[] declaredRoots;
        try
        {
            declaredRoots = (projectRoots ?? []).Where(path => !string.IsNullOrWhiteSpace(path)).Select(Normalize).Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(root => directoryExists(root) && IsSameOrDescendant(cwd, root)).ToArray();
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new("invalid-project-root", null, "project-roots", 0);
        }
        if (declaredRoots.Length == 1)
            return new("exact", declaredRoots[0], "project-root", 0);
        if (declaredRoots.Length > 1)
            return new("ambiguous", null, "project-roots", 0);

        var current = cwd;
        var reachedFileSystemRoot = false;
        for (var level = 0; level < maximumLevels; level++)
        {
            if (directoryExists(Path.Combine(current, ".git")))
                return new("exact", current, "git-root", level);
            if (directoryExists(Path.Combine(current, ".cteam")))
                return new("exact", current, "cteam-marker", level);
            var parent = Directory.GetParent(current)?.FullName;
            if (parent is null || string.Equals(parent, current, StringComparison.OrdinalIgnoreCase))
            {
                reachedFileSystemRoot = true;
                break;
            }
            current = parent;
        }

        return reachedFileSystemRoot ? new("exact", cwd, "exact-cwd", 0) : new("normalization-limit", null, null, maximumLevels);
    }

    static string Normalize(string path)
    {
        var full = Path.GetFullPath(path);
        return OperatingSystem.IsWindows() && full.StartsWith(@"\\?\", StringComparison.Ordinal) ? full[4..] : full;
    }

    static bool IsSameOrDescendant(string path, string root) => string.Equals(path, root, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
