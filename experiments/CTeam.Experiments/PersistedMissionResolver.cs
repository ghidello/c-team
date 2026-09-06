using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CTeam.Experiments;

public sealed record CorrelationResolution(string Outcome, string SelectionSignal, string? MissionKey, string? RootMissionKey, string? CallerKind,
    int CandidateCount, int ScannedFiles, int ExaminedDirectories, int DirectoryEntriesExamined, long BytesRead, bool ScanTruncated);

public sealed record CorrelationLookupLimits(int SearchDays, int DirectoryEntries, int CandidateFiles, int IdentityBytesPerFile)
{
    public static CorrelationLookupLimits Default { get; } = new(31, 4096, 8, 64 * 1024);
}

// A bounded compatibility adapter for the current rollout filename and session_meta layout. It deliberately has no history index.
public static class PersistedMissionResolver
{
    public static CorrelationResolution ResolveExactCaller(CallerContext caller, string? codexHome = null) => string.IsNullOrWhiteSpace(caller.ThreadId)
        ? Missing("missing-caller-thread-id")
        : ResolveExactId(caller.ThreadId, "caller-thread-id", codexHome);

    public static CorrelationResolution ResolveExactId(string? id, string selectionSignal = "explicit-mission-id", string? codexHome = null,
        CorrelationLookupLimits? limits = null)
    {
        if (string.IsNullOrWhiteSpace(id)) return Missing("missing-identity");
        limits ??= CorrelationLookupLimits.Default;
        if (limits.SearchDays < 1 || limits.DirectoryEntries < 1 || limits.CandidateFiles < 1 || limits.IdentityBytesPerFile < 1)
            throw new ArgumentOutOfRangeException(nameof(limits));

        var sessions = Path.Combine(MissionProbe.ResolveCodexHome(codexHome), "sessions");
        if (!Directory.Exists(sessions)) return new("not-found", selectionSignal, null, null, null, 0, 0, 0, 0, 0, false);

        var directories = RecentSessionDirectories(sessions, limits.SearchDays);
        var files = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var directoryEntriesExamined = 0;
        var truncated = false;
        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory)) continue;
            try
            {
                foreach (var path in Directory.EnumerateFiles(directory, "*.jsonl", SearchOption.TopDirectoryOnly))
                {
                    if (directoryEntriesExamined == limits.DirectoryEntries) { truncated = true; break; }
                    directoryEntriesExamined++;
                    var filename = Path.GetFileNameWithoutExtension(path);
                    if (!(string.Equals(filename, id, StringComparison.Ordinal) || filename.EndsWith("-" + id, StringComparison.Ordinal)) || !seen.Add(path)) continue;
                    if (files.Count == limits.CandidateFiles) { truncated = true; break; }
                    files.Add(path);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { truncated = true; }
            if (truncated) break;
        }

        var matches = new List<PersistedIdentity>();
        var bytesRead = 0L;
        foreach (var path in files)
        {
            try
            {
                var identity = ReadIdentity(path, limits.IdentityBytesPerFile, out var read, out var fileTruncated);
                bytesRead += read;
                truncated |= fileTruncated;
                if (identity is not null && string.Equals(identity.Id, id, StringComparison.Ordinal)) matches.Add(identity);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException) { truncated = true; }
        }

        if (truncated || matches.Count > 1) return new("ambiguous", selectionSignal, null, null, null, matches.Count, files.Count, directories.Count,
            directoryEntriesExamined, bytesRead, truncated);
        if (matches.Count == 0) return new("not-found", selectionSignal, null, null, null, 0, files.Count, directories.Count, directoryEntriesExamined, bytesRead, false);
        var match = matches[0];
        var callerKind = match.IsRoot ? "root" : match.IsChild ? "child" : "unknown";
        return new("exact", selectionSignal, Hash(match.Id), Hash(match.DerivedRootId), callerKind, 1, files.Count, directories.Count, directoryEntriesExamined,
            bytesRead, false);
    }

    static CorrelationResolution Missing(string selectionSignal) => new("unresolved", selectionSignal, null, null, null, 0, 0, 0, 0, 0, false);

    static List<string> RecentSessionDirectories(string sessions, int searchDays)
    {
        var result = new List<string> { sessions };
        for (var offset = 0; offset < searchDays; offset++)
        {
            var date = DateTime.UtcNow.Date.AddDays(-offset);
            result.Add(Path.Combine(sessions, date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd")));
        }
        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    static PersistedIdentity? ReadIdentity(string path, int maximumBytes, out long bytesRead, out bool truncated)
    {
        bytesRead = 0;
        truncated = false;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var buffer = new byte[maximumBytes];
        var length = 0;
        while (length < buffer.Length)
        {
            var read = stream.Read(buffer, length, Math.Min(4096, buffer.Length - length));
            if (read == 0) break;
            var newline = Array.IndexOf(buffer, (byte)'\n', length, read);
            length += read;
            bytesRead += read;
            if (newline >= 0)
            {
                length = newline;
                break;
            }
        }
        if (length == buffer.Length && Array.IndexOf(buffer, (byte)'\n') < 0) { truncated = true; return null; }
        if (length > 0 && buffer[length - 1] == '\r') length--;
        if (length == 0) return null;
        using var document = JsonDocument.Parse(buffer.AsMemory(0, length));
        var record = document.RootElement;
        if (Text(record, "type") != "session_meta" || !record.TryGetProperty("payload", out var payload) || payload.ValueKind != JsonValueKind.Object) return null;
        var id = Text(payload, "id") ?? Text(payload, "session_id");
        return string.IsNullOrWhiteSpace(id) ? null : new PersistedIdentity(id, Text(payload, "session_id"), Text(payload, "parent_thread_id"));
    }

    static string? Text(JsonElement value, string property) => value.TryGetProperty(property, out var item) && item.ValueKind == JsonValueKind.String ? item.GetString() : null;
    static string? Hash(string? value) => string.IsNullOrWhiteSpace(value) ? null : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..16];

    sealed record PersistedIdentity(string Id, string? SessionId, string? ParentThreadId)
    {
        public bool IsRoot => string.Equals(Id, SessionId, StringComparison.Ordinal) && ParentThreadId is null;
        public bool IsChild => ParentThreadId is not null || SessionId is not null && !string.Equals(Id, SessionId, StringComparison.Ordinal);
        public string? DerivedRootId => IsRoot ? Id : SessionId is not null && ParentThreadId is not null && !string.Equals(SessionId, ParentThreadId, StringComparison.Ordinal)
            ? null
            : SessionId ?? ParentThreadId;
    }
}
