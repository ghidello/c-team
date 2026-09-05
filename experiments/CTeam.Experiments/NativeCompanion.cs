namespace CTeam.Experiments;

public static class NativeCompanion
{
    public const string Marker = "cteam-pf1-ok";

    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
    {
        var markerName = ReadOption(args, "--marker-name");
        var baseDirectory = AppContext.BaseDirectory;
        await output.WriteLineAsync(Marker);
        await output.WriteLineAsync($"base-directory={baseDirectory}");
        await output.WriteLineAsync($"user={Environment.UserName}");

        if (markerName is not null)
        {
            var path = ExperimentPaths.GetMarkerPath(markerName);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path, $"{Marker}{Environment.NewLine}base-directory={baseDirectory}{Environment.NewLine}user={Environment.UserName}{Environment.NewLine}", cancellationToken);
                await output.WriteLineAsync($"marker-file={path}");
            }
            catch (UnauthorizedAccessException)
            {
                await error.WriteLineAsync("marker-write-denied");
                return 1;
            }
            catch (IOException exception)
            {
                await error.WriteLineAsync($"marker-write-failed={exception.GetType().Name}");
                return 1;
            }
        }

        return 0;
    }

    static string? ReadOption(string[] args, string name)
    {
        for (var index = 0; index < args.Length; index++)
            if (args[index] == name && index + 1 < args.Length)
                return args[index + 1];
        return null;
    }
}
