namespace CTeam.Experiments;

public sealed record PluginLayoutValidation(bool IsValid, IReadOnlyList<string> Issues);

public static class PluginLayout
{
    public static PluginLayoutValidation Validate(string pluginRoot, bool requireHistoricalSkill = true)
    {
        var root = Path.GetFullPath(pluginRoot);
        var required = new List<string>
        {
            Path.Combine(root, ".codex-plugin", "plugin.json"),
            Path.Combine(root, ".mcp.json"),
            ExperimentPaths.ResolveCompanion(root),
            ExperimentPaths.ResolveMcpCompanion(root)
        };
        if (requireHistoricalSkill)
            required.Add(Path.Combine(root, "skills", "pf1-native-companion", "SKILL.md"));
        var issues = required.Where(path => !File.Exists(path)).Select(path => $"Missing required plugin file: {path}").ToArray();
        return new PluginLayoutValidation(issues.Length == 0, issues);
    }
}

public static class PluginStager
{
    public static void Stage(string sourceRoot, string pluginRoot, string companionPath, bool includeHistoricalSkills = true)
    {
        var source = Path.GetFullPath(sourceRoot);
        var destination = Path.GetFullPath(pluginRoot);
        CopyTree(Path.Combine(source, ".codex-plugin"), Path.Combine(destination, ".codex-plugin"));
        var destinationSkills = Path.Combine(destination, "skills");
        if (includeHistoricalSkills)
            CopyTree(Path.Combine(source, "skills"), destinationSkills);
        else if (Directory.Exists(destinationSkills))
            Directory.Delete(destinationSkills, recursive: true);
        var mcpConfig = Path.Combine(source, ".mcp.json");
        if (!File.Exists(mcpConfig)) throw new InvalidOperationException($"Required source file does not exist: {mcpConfig}");
        Directory.CreateDirectory(destination);
        File.Copy(mcpConfig, Path.Combine(destination, ".mcp.json"), true);
        var destinationCompanion = ExperimentPaths.ResolveCompanion(destination);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationCompanion)!);
        File.Copy(Path.GetFullPath(companionPath), destinationCompanion, true);
        File.Copy(Path.GetFullPath(companionPath), ExperimentPaths.ResolveMcpCompanion(destination), true);
        var validation = PluginLayout.Validate(destination, includeHistoricalSkills);
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join(Environment.NewLine, validation.Issues));
    }

    static void CopyTree(string source, string destination)
    {
        if (!Directory.Exists(source))
            throw new InvalidOperationException($"Required source directory does not exist: {source}");
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }
}
