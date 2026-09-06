namespace CTeam.Experiments;

public static class ExperimentProgram
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
    {
        if (args.Length == 0)
        {
            await error.WriteLineAsync("Usage: cteam <plugin-native-companion|resolve-companion|validate-plugin-layout|invoke-plugin-companion|stage-plugin|pack-npm-bootstrap|inspect-state-db|inspect-state-thread|init|mcp-server|context-activation-server> [options]");
            return 2;
        }

        try
        {
            return args[0] switch
            {
                "plugin-native-companion" => await NativeCompanion.RunAsync(args[1..], output, error, cancellationToken),
                "resolve-companion" => await ResolveAsync(args[1..], output),
                "validate-plugin-layout" => await ValidateAsync(args[1..], output, error),
                "invoke-plugin-companion" => await InvokeAsync(args[1..], output, error, cancellationToken),
                "stage-plugin" => await StageAsync(args[1..], output),
                "pack-npm-bootstrap" => await PackNpmBootstrapAsync(args[1..], output),
                "inspect-state-db" => await InspectStateDatabaseAsync(args[1..], output),
                "inspect-state-thread" => await InspectStateThreadAsync(args[1..], output),
                "init" => await InitializeProjectAsync(args[1..], output),
                "mcp-server" => await McpServer.RunAsync(Console.In, output, error, cancellationToken),
                "context-activation-server" => await ActivationMcpServer.RunAsync(Console.In, output, error, cancellationToken),
                _ => throw new ArgumentException($"Unknown experiment command: {args[0]}")
            };
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException)
        {
            await error.WriteLineAsync(exception.Message);
            return 2;
        }
    }

    static async Task<int> ResolveAsync(string[] args, TextWriter output)
    {
        var pluginRoot = RequiredOption(args, "--plugin-root");
        await output.WriteLineAsync(ExperimentPaths.ResolveCompanion(pluginRoot));
        return 0;
    }

    static async Task<int> ValidateAsync(string[] args, TextWriter output, TextWriter error)
    {
        var validation = PluginLayout.Validate(RequiredOption(args, "--plugin-root"), requireHistoricalSkill: !args.Contains("--without-skills", StringComparer.Ordinal));
        foreach (var issue in validation.Issues)
            await error.WriteLineAsync(issue);
        await output.WriteLineAsync(validation.IsValid ? "plugin-layout-valid" : "plugin-layout-invalid");
        return validation.IsValid ? 0 : 1;
    }

    static async Task<int> InvokeAsync(string[] args, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        var pluginRoot = RequiredOption(args, "--plugin-root");
        var markerName = OptionalOption(args, "--marker-name") ?? "manual";
        var result = await ProcessRunner.RunAsync(ExperimentPaths.ResolveCompanion(pluginRoot), ["plugin-native-companion", "--marker-name", markerName], cancellationToken);
        await output.WriteAsync(result.StandardOutput);
        await error.WriteAsync(result.StandardError);
        return result.ExitCode;
    }

    static async Task<int> StageAsync(string[] args, TextWriter output)
    {
        var sourceRoot = RequiredOption(args, "--source-root");
        var pluginRoot = RequiredOption(args, "--plugin-root");
        var companion = RequiredOption(args, "--companion");
        PluginStager.Stage(sourceRoot, pluginRoot, companion, includeHistoricalSkills: !args.Contains("--without-skills", StringComparer.Ordinal));
        await output.WriteLineAsync($"plugin-staged={Path.GetFullPath(pluginRoot)}");
        return 0;
    }

    static async Task<int> InspectStateDatabaseAsync(string[] args, TextWriter output)
    {
        await output.WriteLineAsync(CodexStateSchemaInspector.Inspect(RequiredOption(args, "--db")).ToJsonString(new() { WriteIndented = true }));
        return 0;
    }

    static async Task<int> PackNpmBootstrapAsync(string[] args, TextWriter output)
    {
        var destination = RequiredOption(args, "--output");
        NpmPackageArchive.Create(RequiredOption(args, "--source"), destination);
        await output.WriteLineAsync($"npm-package={Path.GetFullPath(destination)}");
        return 0;
    }

    static async Task<int> InspectStateThreadAsync(string[] args, TextWriter output)
    {
        var result = CodexStateThreadLocator.Lookup(RequiredOption(args, "--thread-id"), databasePath: RequiredOption(args, "--db"));
        var json = new System.Text.Json.Nodes.JsonObject
        {
            ["outcome"] = result.Outcome,
            ["database_path"] = result.DatabasePath,
            ["cwd"] = result.Cwd,
            ["project_id"] = result.ProjectId,
            ["parent_thread_id"] = result.ParentThreadId,
            ["is_child"] = result.IsChild,
            ["project_roots"] = new System.Text.Json.Nodes.JsonArray(result.ProjectRoots.Select(path => System.Text.Json.Nodes.JsonValue.Create(path)).ToArray()),
            ["matching_rows"] = result.MatchingRows,
            ["id_primary_key"] = result.IdPrimaryKey,
            ["elapsed_microseconds"] = result.ElapsedMicroseconds
        };
        await output.WriteLineAsync(json.ToJsonString(new() { WriteIndented = true }));
        return result.Outcome == "exact" ? 0 : 1;
    }

    static async Task<int> InitializeProjectAsync(string[] args, TextWriter output)
    {
        var report = ProjectInitializer.Initialize(new ProjectInitializationOptions(RequiredOption(args, "--target"),
            args.Contains("--dry-run", StringComparer.Ordinal)));
        await output.WriteLineAsync(report.ToJson());
        return report.Status == "rejected" ? 1 : 0;
    }

    static string RequiredOption(string[] args, string name) => OptionalOption(args, name) ?? throw new ArgumentException($"Missing required option {name}.");

    static string? OptionalOption(string[] args, string name)
    {
        for (var index = 0; index < args.Length; index++)
            if (args[index] == name && index + 1 < args.Length)
                return args[index + 1];
        return null;
    }
}
