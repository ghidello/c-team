using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CTeam.Experiments;

public sealed record ProjectInitializationOptions(string TargetDirectory, bool DryRun = false);

public sealed record ProjectInitializationReport(string Status, IReadOnlyList<string> ChangedFiles, IReadOnlyList<string> PlannedFiles,
    IReadOnlyList<string> NextSteps, string? Detail = null)
{
    public string ToJson()
    {
        var result = new JsonObject
        {
            ["status"] = Status,
            ["changedFiles"] = Array(ChangedFiles),
            ["plannedFiles"] = Array(PlannedFiles),
            ["nextSteps"] = Array(NextSteps)
        };
        if (Detail is not null)
            result["detail"] = Detail;
        return result.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    static JsonArray Array(IReadOnlyList<string> values) => new(values.Select(value => JsonValue.Create(value)).ToArray());
}

public static class ProjectInitializer
{
    public const int CurrentSchemaVersion = 1;
    public const string GuidanceStart = "<!-- cteam:guidance:start -->";
    public const string GuidanceEnd = "<!-- cteam:guidance:end -->";

    const string ConfigRelativePath = ".cteam/config.json";
    const string AgentsRelativePath = "AGENTS.md";
    const string Guidance = "<!-- cteam:guidance:start -->\n## C-Team\n\n"
        + "This repository is initialized for C-Team observability. Use the `cteam` MCP tool for project status and mission observation.\n"
        + "<!-- cteam:guidance:end -->\n";
    const string Config = "{\n  \"schemaVersion\": 1\n}\n";

    public static ProjectInitializationReport Initialize(ProjectInitializationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.TargetDirectory) || !Directory.Exists(options.TargetDirectory))
            return Rejected("The target directory must already exist.");

        string root;
        try
        {
            root = Path.GetFullPath(options.TargetDirectory);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Rejected("The target directory is invalid.");
        }

        var configPath = UnderRoot(root, ConfigRelativePath);
        var agentsPath = UnderRoot(root, AgentsRelativePath);
        try
        {
            if (HasExistingDescendantReparsePoint(root, configPath) || HasExistingDescendantReparsePoint(root, agentsPath))
                return Rejected("Initialization paths cannot traverse an existing reparse point below the target directory.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Rejected("Initialization paths could not be checked safely.");
        }
        var configState = ReadConfig(configPath);
        if (configState.Problem is not null)
            return Rejected(configState.Problem);

        var agents = ReadGuidance(agentsPath);
        if (agents.Problem is not null)
            return Rejected(agents.Problem);

        if (configState.SchemaVersion == 0)
        {
            var upgradePlan = new List<string> { ConfigRelativePath };
            if (agents.Content is not null)
                upgradePlan.Add(AgentsRelativePath);
            return new("upgrade_required", [], upgradePlan.OrderBy(path => path, StringComparer.Ordinal).ToArray(),
                ["Review the schemaVersion 0 to 1 upgrade plan; this initializer does not apply schema upgrades.",
                    "Plugin installation is separate from project initialization."], "Schema version 0 is recognized but was not changed.");
        }

        var proposed = new List<PlannedWrite>();
        if (configState.SchemaVersion is null)
            proposed.Add(new(ConfigRelativePath, configPath, Utf8(Config), configState.OriginalBytes));
        if (agents.Content is not null)
            proposed.Add(new(AgentsRelativePath, agentsPath, Utf8(agents.Content), agents.OriginalBytes));

        var paths = proposed.Select(write => write.RelativePath).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        var nextSteps = new[]
        {
            "Ensure the C-Team plugin is installed and enabled for this user.",
            "Start a new Codex session if the new AGENTS.md guidance should be loaded from the beginning."
        };
        if (options.DryRun)
            return new("dry_run", [], paths, nextSteps);
        if (proposed.Count == 0)
            return new("already_initialized", [], [], nextSteps);

        try
        {
            ApplyAtomically(root, proposed);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Rejected("Project files could not be written safely; inspect the selected target before retrying.");
        }
        return new("initialized", paths, [], nextSteps);
    }

    static ProjectInitializationReport Rejected(string detail) => new("rejected", [], [],
        ["Correct the project state and run initialization again.", "Plugin installation is separate from project initialization."], detail);

    static string UnderRoot(string root, string relativePath)
    {
        var candidate = Path.GetFullPath(Path.Combine(root, relativePath));
        var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Initialization path escapes the target directory.");
        return candidate;
    }

    public static bool HasExistingDescendantReparsePoint(string targetRoot, string candidatePath, Func<string, FileAttributes>? getAttributes = null)
    {
        // This preflight protects the existing layout; it cannot make a hostile concurrent filesystem swap transactional.
        var root = Path.GetFullPath(targetRoot);
        var candidate = Path.GetFullPath(candidatePath);
        var relative = Path.GetRelativePath(root, candidate);
        if (relative == "." || Path.IsPathFullyQualified(relative) || relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "" or "." or ".."))
            throw new IOException("The candidate path must be a descendant of the target directory.");

        getAttributes ??= File.GetAttributes;
        var current = root;
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if ((File.Exists(current) || Directory.Exists(current)) && (getAttributes(current) & FileAttributes.ReparsePoint) != 0)
                return true;
        }
        return false;
    }

    static ConfigState ReadConfig(string configPath)
    {
        if (!File.Exists(configPath))
            return new(null, null, null);
        try
        {
            var originalBytes = File.ReadAllBytes(configPath);
            using var document = JsonDocument.Parse(originalBytes);
            if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.TryGetProperty("schemaVersion", out var version)
                || !version.TryGetInt32(out var schemaVersion))
                return new(null, originalBytes, "The existing .cteam/config.json is malformed or lacks an integer schemaVersion.");
            if (schemaVersion > CurrentSchemaVersion || schemaVersion < 0)
                return new(null, originalBytes, $"The existing .cteam/config.json has unsupported schemaVersion {schemaVersion}.");
            return new(schemaVersion, originalBytes, null);
        }
        catch (JsonException)
        {
            return new(null, null, "The existing .cteam/config.json is malformed.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(null, null, "The existing .cteam/config.json could not be read.");
        }
    }

    static GuidanceState ReadGuidance(string agentsPath)
    {
        if (!File.Exists(agentsPath))
            return new(Guidance, null, null);
        byte[] originalBytes;
        string current;
        try
        {
            originalBytes = File.ReadAllBytes(agentsPath);
            using var reader = new StreamReader(new MemoryStream(originalBytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            current = reader.ReadToEnd();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(null, null, "The existing AGENTS.md could not be read.");
        }

        var starts = Count(current, GuidanceStart);
        var ends = Count(current, GuidanceEnd);
        var start = current.IndexOf(GuidanceStart, StringComparison.Ordinal);
        var end = current.IndexOf(GuidanceEnd, StringComparison.Ordinal);
        if (starts > 1 || ends > 1 || starts != ends || (start >= 0 && end < start))
            return new(null, originalBytes, "AGENTS.md contains duplicate, incomplete, or misaligned C-Team guidance markers.");
        if (start >= 0)
        {
            var endExclusive = EndOfFollowingLineBreak(current, end + GuidanceEnd.Length);
            var replacement = current[..start] + Guidance + current[endExclusive..];
            return new(replacement == current ? null : replacement, originalBytes, null);
        }
        return new(current.Length == 0 ? Guidance : current + (current.EndsWith('\n') ? "\n" : "\n\n") + Guidance, originalBytes, null);
    }

    static void ApplyAtomically(string root, IReadOnlyList<PlannedWrite> writes)
    {
        var originalDirectories = writes.Select(write => Path.GetDirectoryName(write.Path)!).Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(directory => directory, Directory.Exists, StringComparer.OrdinalIgnoreCase);
        var applied = new List<PlannedWrite>();
        try
        {
            foreach (var write in writes)
                ValidateWriteState(root, write);
            foreach (var write in writes)
            {
                ValidateWriteState(root, write);
                AtomicWrite(write.Path, write.ContentBytes);
                applied.Add(write);
            }
        }
        catch
        {
            foreach (var write in applied.AsEnumerable().Reverse())
            {
                try
                {
                    if (HasExistingDescendantReparsePoint(root, write.Path) || !File.Exists(write.Path)
                        || !File.ReadAllBytes(write.Path).SequenceEqual(write.ContentBytes))
                        continue;
                    if (write.ExpectedBytes is null)
                        File.Delete(write.Path);
                    else
                        AtomicWrite(write.Path, write.ExpectedBytes);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
            }
            foreach (var directory in originalDirectories.Where(item => !item.Value).Select(item => item.Key).OrderByDescending(directory => directory.Length))
            {
                try
                {
                    if (Directory.Exists(directory) && !HasExistingDescendantReparsePoint(root, directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                        Directory.Delete(directory);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
            }
            throw;
        }
    }

    static void ValidateWriteState(string root, PlannedWrite write)
    {
        if (HasExistingDescendantReparsePoint(root, write.Path))
            throw new IOException("An initialization path changed to a reparse point.");
        if (write.ExpectedBytes is null)
        {
            if (File.Exists(write.Path) || Directory.Exists(write.Path))
                throw new IOException("A project path changed after it was inspected.");
            return;
        }
        if (!File.Exists(write.Path) || !File.ReadAllBytes(write.Path).SequenceEqual(write.ExpectedBytes))
            throw new IOException("A project file changed after it was inspected.");
    }

    static void AtomicWrite(string path, byte[] content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".cteam-tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllBytes(temporary, content);
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    static byte[] Utf8(string content) => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content);

    sealed record ConfigState(int? SchemaVersion, byte[]? OriginalBytes, string? Problem);
    sealed record GuidanceState(string? Content, byte[]? OriginalBytes, string? Problem);
    sealed record PlannedWrite(string RelativePath, string Path, byte[] ContentBytes, byte[]? ExpectedBytes);

    static int Count(string value, string marker) => value.Split(marker, StringSplitOptions.None).Length - 1;

    static int EndOfFollowingLineBreak(string value, int offset) => offset < value.Length && value[offset] == '\r' && offset + 1 < value.Length && value[offset + 1] == '\n'
        ? offset + 2 : offset < value.Length && value[offset] == '\n' ? offset + 1 : offset;
}
