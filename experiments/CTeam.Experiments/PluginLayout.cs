namespace CTeam.Experiments;

public sealed record PluginLayoutValidation(bool IsValid, IReadOnlyList<string> Issues);

public static class PluginLayout
{
    public static PluginLayoutValidation Validate(string pluginRoot)
    {
        var root = Path.GetFullPath(pluginRoot);
        var required = new[]
        {
            Path.Combine(root, ".codex-plugin", "plugin.json"),
            Path.Combine(root, "skills", "pf1-native-companion", "SKILL.md"),
            ExperimentPaths.ResolveCompanion(root)
        };
        var issues = required.Where(path => !File.Exists(path)).Select(path => $"Missing required plugin file: {path}").ToArray();
        return new PluginLayoutValidation(issues.Length == 0, issues);
    }
}

public static class PluginStager
{
    public static void Stage(string sourceRoot, string pluginRoot, string companionPath)
    {
        var source = Path.GetFullPath(sourceRoot);
        var destination = Path.GetFullPath(pluginRoot);
        CopyTree(Path.Combine(source, ".codex-plugin"), Path.Combine(destination, ".codex-plugin"));
        CopyTree(Path.Combine(source, "skills"), Path.Combine(destination, "skills"));
        var destinationCompanion = ExperimentPaths.ResolveCompanion(destination);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationCompanion)!);
        File.Copy(Path.GetFullPath(companionPath), destinationCompanion, true);
        var validation = PluginLayout.Validate(destination);
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
