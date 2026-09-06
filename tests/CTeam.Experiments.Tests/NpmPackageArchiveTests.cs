using System.Formats.Tar;
using System.IO.Compression;
using CTeam.Experiments;
using Xunit;

namespace CTeam.Experiments.Tests;

public sealed class NpmPackageArchiveTests : IDisposable
{
    readonly string scratch = Path.Combine(AppContext.BaseDirectory, "test-scratch", Guid.NewGuid().ToString("N"));

    public NpmPackageArchiveTests() => Directory.CreateDirectory(scratch);
    public void Dispose() { if (Directory.Exists(scratch)) Directory.Delete(scratch, true); }

    [Fact]
    public void Archive_has_the_npm_package_prefix_and_expected_files()
    {
        var package = Path.Combine(scratch, "arbitrary-source-name");
        Write(Path.Combine(package, "package.json"), "{}");
        Write(Path.Combine(package, "bin", "cteam-init.mjs"), "fixture");
        Write(Path.Combine(package, "native", "win-x64", "cteam.exe"), "fixture");
        var archive = Path.Combine(scratch, "cteam-init.tgz");
        var secondArchive = Path.Combine(scratch, "cteam-init-second.tgz");

        NpmPackageArchive.Create(package, archive);
        NpmPackageArchive.Create(package, secondArchive);

        Assert.Equal(File.ReadAllBytes(archive), File.ReadAllBytes(secondArchive));

        using var file = File.OpenRead(archive);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var tar = new TarReader(gzip);
        var names = new List<string>();
        while (tar.GetNextEntry() is { } entry)
            names.Add(entry.Name.Replace('\\', '/'));
        Assert.Contains("package/package.json", names);
        Assert.Contains("package/bin/cteam-init.mjs", names);
        Assert.Contains("package/native/win-x64/cteam.exe", names);
        Assert.DoesNotContain(names, name => name.StartsWith("arbitrary-source-name/", StringComparison.Ordinal));
    }

    static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
