using System.Formats.Tar;
using System.IO.Compression;

namespace CTeam.Experiments;

public static class NpmPackageArchive
{
    public static void Create(string packageDirectory, string outputPath)
    {
        var source = Path.GetFullPath(packageDirectory);
        if (!Directory.Exists(source))
            throw new ArgumentException("The package directory must exist.", nameof(packageDirectory));
        var destination = Path.GetFullPath(outputPath);
        var files = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetFullPath(path), destination, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetRelativePath(source, path), StringComparer.Ordinal)
            .ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        using var file = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
        using var gzip = new GZipStream(file, CompressionLevel.SmallestSize);
        using var tar = new TarWriter(gzip, TarEntryFormat.Ustar);
        foreach (var path in files)
        {
            var name = "package/" + Path.GetRelativePath(source, path).Replace('\\', '/');
            using var content = File.OpenRead(path);
            var entry = new UstarTarEntry(TarEntryType.RegularFile, name)
            {
                DataStream = content,
                ModificationTime = DateTimeOffset.UnixEpoch,
                Mode = name.EndsWith(".mjs", StringComparison.Ordinal) || name.EndsWith(".exe", StringComparison.Ordinal)
                    ? UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                        | UnixFileMode.OtherRead | UnixFileMode.OtherExecute
                    : UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead
            };
            tar.WriteEntry(entry);
        }
    }
}
