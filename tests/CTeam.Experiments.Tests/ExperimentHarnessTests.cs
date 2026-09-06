using CTeam.Experiments;
using Xunit;

namespace CTeam.Experiments.Tests;

public sealed class ExperimentHarnessTests : IDisposable
{
    readonly string scratch = Path.Combine(AppContext.BaseDirectory, "test-scratch", Guid.NewGuid().ToString("N"));

    public ExperimentHarnessTests() => Directory.CreateDirectory(scratch);
    public void Dispose() { if (Directory.Exists(scratch)) Directory.Delete(scratch, true); }

    [Fact]
    public void Companion_path_is_relative_to_plugin_root()
    {
        var result = ExperimentPaths.ResolveCompanion(scratch);
        Assert.Equal(Path.Combine(Path.GetFullPath(scratch), "bin", "win-x64", "cteam-pf1.exe"), result);
    }

    [Fact]
    public void Marker_path_uses_selected_per_user_root_and_rejects_path_injection()
    {
        var result = ExperimentPaths.GetMarkerPath("first", scratch);
        Assert.StartsWith(Path.GetFullPath(scratch), result);
        Assert.EndsWith(Path.Combine("004-plugin-native-companion", "first.txt"), result);
        Assert.Throws<ArgumentException>(() => ExperimentPaths.GetMarkerPath("../escape", scratch));
    }

    [Fact]
    public void Fake_plugin_layout_validates_required_relative_files()
    {
        Write(Path.Combine(scratch, ".codex-plugin", "plugin.json"), "{}");
        Write(Path.Combine(scratch, ".mcp.json"), "{}");
        Write(Path.Combine(scratch, "skills", "pf1-native-companion", "SKILL.md"), "fixture");
        Write(ExperimentPaths.ResolveCompanion(scratch), "fixture");
        Write(ExperimentPaths.ResolveMcpCompanion(scratch), "fixture");
        Assert.True(PluginLayout.Validate(scratch).IsValid);
    }

    [Fact]
    public void Stager_copies_manifest_skills_and_companion_into_plugin_layout()
    {
        var source = Path.Combine(scratch, "source");
        var destination = Path.Combine(scratch, "destination");
        var companion = Path.Combine(scratch, "built", "cteam-pf1.exe");
        Write(Path.Combine(source, ".codex-plugin", "plugin.json"), "{}");
        Write(Path.Combine(source, ".mcp.json"), "{}");
        Write(Path.Combine(source, "skills", "pf1-native-companion", "SKILL.md"), "fixture");
        Write(companion, "native-fixture");
        PluginStager.Stage(source, destination, companion);
        Assert.True(PluginLayout.Validate(destination).IsValid);
        Assert.Equal("native-fixture", File.ReadAllText(ExperimentPaths.ResolveCompanion(destination)));
        Assert.Equal("native-fixture", File.ReadAllText(ExperimentPaths.ResolveMcpCompanion(destination)));
    }

    [Fact]
    public void Activation_staging_omits_historical_skills_from_the_installed_surface()
    {
        var source = Path.Combine(scratch, "activation-source");
        var destination = Path.Combine(scratch, "activation-destination");
        var companion = Path.Combine(scratch, "activation-built", "cteam-pf1.exe");
        Write(Path.Combine(source, ".codex-plugin", "plugin.json"), "{}");
        Write(Path.Combine(source, ".mcp.json"), "{}");
        Write(Path.Combine(source, "skills", "pf1-native-companion", "SKILL.md"), "fixture");
        Write(Path.Combine(destination, "skills", "stale", "SKILL.md"), "stale");
        Write(companion, "native-fixture");

        PluginStager.Stage(source, destination, companion, includeHistoricalSkills: false);

        Assert.True(PluginLayout.Validate(destination, requireHistoricalSkill: false).IsValid);
        Assert.False(Directory.Exists(Path.Combine(destination, "skills")));
    }

    [Fact]
    public async Task Process_runner_captures_exit_code_and_output()
    {
        var executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe");
        Assert.True(File.Exists(executable), $"Expected the .NET host at {executable}.");
        var result = await ProcessRunner.RunAsync(executable, ["--version"], TestContext.Current.CancellationToken);
        Assert.Equal(0, result.ExitCode);
        Assert.NotEmpty(result.StandardOutput);
    }

    static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
