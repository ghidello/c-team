using System.Text;
using System.Text.Json;
using CTeam.Experiments;
using Xunit;

namespace CTeam.Experiments.Tests;

public sealed class ProjectInitializerTests : IDisposable
{
    readonly string scratch = Path.Combine(Path.GetTempPath(), "cteam-project-initializer-" + Guid.NewGuid().ToString("N"));

    public ProjectInitializerTests() => Directory.CreateDirectory(scratch);

    public void Dispose()
    {
        if (Directory.Exists(scratch))
            Directory.Delete(scratch, true);
    }

    [Fact]
    public void Fresh_project_gets_the_canonical_marker_and_guidance()
    {
        var report = ProjectInitializer.Initialize(new(scratch));

        Assert.Equal("initialized", report.Status);
        Assert.Equal([".cteam/config.json", "AGENTS.md"], report.ChangedFiles);
        Assert.Equal(File.ReadAllBytes(Fixture(".cteam", "config.json")), File.ReadAllBytes(Path.Combine(scratch, ".cteam", "config.json")));
        Assert.Equal(File.ReadAllBytes(Fixture("AGENTS.md")), File.ReadAllBytes(Path.Combine(scratch, "AGENTS.md")));
        Assert.Contains("plugin is installed", string.Join('\n', report.NextSteps));
    }

    [Fact]
    public void Existing_agents_content_is_preserved_and_c_team_section_is_appended()
    {
        var original = "# Existing instructions\n\nKeep this exact.";
        File.WriteAllText(Path.Combine(scratch, "AGENTS.md"), original);

        ProjectInitializer.Initialize(new(scratch));

        var content = File.ReadAllText(Path.Combine(scratch, "AGENTS.md"));
        Assert.StartsWith(original, content, StringComparison.Ordinal);
        Assert.Equal(1, Count(content, ProjectInitializer.GuidanceStart));
        Assert.Equal(1, Count(content, ProjectInitializer.GuidanceEnd));
    }

    [Fact]
    public void Existing_marketplace_file_is_untouched()
    {
        var marketplace = Path.Combine(scratch, ".agents", "plugins", "marketplace.json");
        Directory.CreateDirectory(Path.GetDirectoryName(marketplace)!);
        var bytes = Encoding.UTF8.GetBytes("{\r\n  \"user\": true\r\n}\r\n");
        File.WriteAllBytes(marketplace, bytes);

        ProjectInitializer.Initialize(new(scratch));

        Assert.Equal(bytes, File.ReadAllBytes(marketplace));
    }

    [Fact]
    public void Repeat_is_idempotent()
    {
        ProjectInitializer.Initialize(new(scratch));
        var config = File.ReadAllBytes(Path.Combine(scratch, ".cteam", "config.json"));
        var agents = File.ReadAllBytes(Path.Combine(scratch, "AGENTS.md"));

        var report = ProjectInitializer.Initialize(new(scratch));

        Assert.Equal("already_initialized", report.Status);
        Assert.Empty(report.ChangedFiles);
        Assert.Equal(config, File.ReadAllBytes(Path.Combine(scratch, ".cteam", "config.json")));
        Assert.Equal(agents, File.ReadAllBytes(Path.Combine(scratch, "AGENTS.md")));
    }

    [Fact]
    public void Partial_project_is_repaired()
    {
        Directory.CreateDirectory(Path.Combine(scratch, ".cteam"));
        File.WriteAllText(Path.Combine(scratch, ".cteam", "config.json"), "{\n  \"schemaVersion\": 1\n}\n");

        var report = ProjectInitializer.Initialize(new(scratch));

        Assert.Equal("initialized", report.Status);
        Assert.Equal(["AGENTS.md"], report.ChangedFiles);
    }

    [Fact]
    public void Dry_run_has_no_writes_and_reports_the_plan()
    {
        var report = ProjectInitializer.Initialize(new(scratch, DryRun: true));

        Assert.Equal("dry_run", report.Status);
        Assert.Equal([".cteam/config.json", "AGENTS.md"], report.PlannedFiles);
        Assert.False(Directory.Exists(Path.Combine(scratch, ".cteam")));
        Assert.False(File.Exists(Path.Combine(scratch, "AGENTS.md")));
    }

    [Fact]
    public void Schema_zero_has_an_explicit_non_mutating_upgrade_plan()
    {
        Directory.CreateDirectory(Path.Combine(scratch, ".cteam"));
        var config = Path.Combine(scratch, ".cteam", "config.json");
        File.WriteAllText(config, "{\"schemaVersion\":0}");

        var report = ProjectInitializer.Initialize(new(scratch));

        Assert.Equal("upgrade_required", report.Status);
        Assert.Equal([".cteam/config.json", "AGENTS.md"], report.PlannedFiles);
        Assert.Equal("{\"schemaVersion\":0}", File.ReadAllText(config));
        Assert.False(File.Exists(Path.Combine(scratch, "AGENTS.md")));
    }

    [Fact]
    public void Schema_zero_with_existing_guidance_plans_only_the_config_upgrade()
    {
        Directory.CreateDirectory(Path.Combine(scratch, ".cteam"));
        File.WriteAllText(Path.Combine(scratch, ".cteam", "config.json"), "{\"schemaVersion\":0}");
        File.WriteAllBytes(Path.Combine(scratch, "AGENTS.md"), File.ReadAllBytes(Fixture("AGENTS.md")));

        var report = ProjectInitializer.Initialize(new(scratch));

        Assert.Equal("upgrade_required", report.Status);
        Assert.Equal([".cteam/config.json"], report.PlannedFiles);
        Assert.Equal("{\"schemaVersion\":0}", File.ReadAllText(Path.Combine(scratch, ".cteam", "config.json")));
    }

    [Theory]
    [InlineData("{not-json")]
    [InlineData("{\"schemaVersion\": 2}")]
    public void Bad_or_future_config_is_rejected_without_partial_writes(string configContent)
    {
        Directory.CreateDirectory(Path.Combine(scratch, ".cteam"));
        var config = Path.Combine(scratch, ".cteam", "config.json");
        File.WriteAllText(config, configContent);

        var report = ProjectInitializer.Initialize(new(scratch));

        Assert.Equal("rejected", report.Status);
        Assert.Equal(configContent, File.ReadAllText(config));
        Assert.False(File.Exists(Path.Combine(scratch, "AGENTS.md")));
    }

    [Fact]
    public void Duplicate_or_misaligned_guidance_markers_are_rejected_without_writes()
    {
        var agents = Path.Combine(scratch, "AGENTS.md");
        const string malformed = "<!-- cteam:guidance:end -->\n<!-- cteam:guidance:start -->\n<!-- cteam:guidance:start -->\n";
        File.WriteAllText(agents, malformed);

        var report = ProjectInitializer.Initialize(new(scratch));

        Assert.Equal("rejected", report.Status);
        Assert.Equal(malformed, File.ReadAllText(agents));
        Assert.False(Directory.Exists(Path.Combine(scratch, ".cteam")));
    }

    [Fact]
    public void Descendant_reparse_point_check_rejects_a_reparse_component_without_lexical_escape()
    {
        var candidate = Path.Combine(scratch, ".cteam", "config.json");
        Directory.CreateDirectory(Path.GetDirectoryName(candidate)!);

        var hasReparsePoint = ProjectInitializer.HasExistingDescendantReparsePoint(scratch, candidate,
            path => string.Equals(path, Path.Combine(scratch, ".cteam"), StringComparison.OrdinalIgnoreCase) ? FileAttributes.ReparsePoint : FileAttributes.Directory);

        Assert.True(hasReparsePoint);
    }

    [Fact]
    public void Failed_second_write_restores_config_and_removes_new_marker_directory()
    {
        Directory.CreateDirectory(Path.Combine(scratch, "AGENTS.md"));

        var report = ProjectInitializer.Initialize(new(scratch));

        Assert.Equal("rejected", report.Status);
        Assert.False(File.Exists(Path.Combine(scratch, ".cteam", "config.json")));
        Assert.False(Directory.Exists(Path.Combine(scratch, ".cteam")));
        Assert.True(Directory.Exists(Path.Combine(scratch, "AGENTS.md")));
    }

    [Fact]
    public async Task Init_command_emits_a_deterministic_json_report()
    {
        var output = new StringWriter();

        Assert.Equal(0, await ExperimentProgram.RunAsync(["init", "--target", scratch, "--dry-run"], output, TextWriter.Null, TestContext.Current.CancellationToken));

        using var document = JsonDocument.Parse(output.ToString());
        Assert.Equal("dry_run", document.RootElement.GetProperty("status").GetString());
        Assert.Equal([".cteam/config.json", "AGENTS.md"], document.RootElement.GetProperty("plannedFiles").EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public void Target_must_already_exist()
    {
        var missing = Path.Combine(scratch, "does-not-exist");

        var report = ProjectInitializer.Initialize(new(missing));

        Assert.Equal("rejected", report.Status);
        Assert.False(Directory.Exists(missing));
    }

    static int Count(string value, string searched) => value.Split(searched, StringSplitOptions.None).Length - 1;

    static string Fixture(params string[] segments) => Path.Combine([AppContext.BaseDirectory, "..", "..", "..", "..", "fixtures", "onboarding", "fresh", .. segments]);
}
