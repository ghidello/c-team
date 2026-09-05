namespace CTeam.Experiments;

public static class ExperimentPaths
{
    public static string CompanionRelativePath => Path.Combine("bin", "win-x64", "cteam-pf1.exe");
    public static string McpCompanionRelativePath => Path.Combine("bin", "win-x64", "cteam.exe");

    public static string ResolveCompanion(string pluginRoot)
    {
        if (string.IsNullOrWhiteSpace(pluginRoot))
            throw new ArgumentException("Plugin root is required.", nameof(pluginRoot));
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(pluginRoot), CompanionRelativePath));
    }

    public static string ResolveMcpCompanion(string pluginRoot)
    {
        if (string.IsNullOrWhiteSpace(pluginRoot))
            throw new ArgumentException("Plugin root is required.", nameof(pluginRoot));
        return Path.GetFullPath(Path.Combine(Path.GetFullPath(pluginRoot), McpCompanionRelativePath));
    }

    public static string GetMarkerPath(string markerName, string? localApplicationDataOverride = null)
    {
        if (string.IsNullOrWhiteSpace(markerName) || markerName.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
            throw new ArgumentException("Marker name may contain only ASCII letters, digits, '-' and '_'.", nameof(markerName));
        var localApplicationData = localApplicationDataOverride ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
            throw new InvalidOperationException("Local application data directory is unavailable.");
        return Path.Combine(Path.GetFullPath(localApplicationData), "C-Team", "experiments", "004-plugin-native-companion", markerName + ".txt");
    }
}
