using System.Text.Json;

namespace CTeam.Experiments;

public sealed record PersistedCallerLocationResolution(
    string Outcome,
    string? Cwd,
    string? ParentThreadId,
    bool IsChild,
    int RolloutFilesRead,
    int ExaminedDirectories,
    int DirectoryEntriesExamined,
    long BytesRead,
    bool ScanTruncated);

public static class PersistedCallerLocationResolver
{
    public static PersistedCallerLocationResolution Resolve(CallerContext caller, string? codexHome = null, CorrelationLookupLimits? limits = null)
    {
        if (string.IsNullOrWhiteSpace(caller.ThreadId))
            return Missing("missing-thread-id");
        limits ??= CorrelationLookupLimits.Default;
        var sessions = Path.Combine(MissionProbe.ResolveCodexHome(codexHome), "sessions");
        if (!Directory.Exists(sessions))
            return Missing("not-found");

        var directories = RecentDirectories(sessions, limits.SearchDays);
        var candidates = new List<string>();
        var entries = 0;
        var truncated = false;
        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
                continue;
            try
            {
                foreach (var path in Directory.EnumerateFiles(directory, "*.jsonl", SearchOption.TopDirectoryOnly))
                {
                    if (entries == limits.DirectoryEntries)
                    {
                        truncated = true;
                        break;
                    }
                    entries++;
                    var filename = Path.GetFileNameWithoutExtension(path);
                    if (!filename.EndsWith("-" + caller.ThreadId, StringComparison.Ordinal) && !string.Equals(filename, caller.ThreadId, StringComparison.Ordinal))
                        continue;
                    if (candidates.Count == limits.CandidateFiles)
                    {
                        truncated = true;
                        break;
                    }
                    candidates.Add(path);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                truncated = true;
            }
            if (truncated)
                break;
        }

        var matches = new List<Location>();
        var bytesRead = 0L;
        foreach (var path in candidates)
        {
            try
            {
                var location = Read(path, limits.IdentityBytesPerFile, out var read, out var fileTruncated);
                bytesRead += read;
                truncated |= fileTruncated;
                if (location is not null && string.Equals(location.Id, caller.ThreadId, StringComparison.Ordinal))
                    matches.Add(location);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                truncated = true;
            }
        }

        if (truncated || matches.Count > 1)
            return new("ambiguous", null, null, false, candidates.Count, directories.Count, entries, bytesRead, truncated);
        if (matches.Count == 0)
            return new("not-found", null, null, false, candidates.Count, directories.Count, entries, bytesRead, false);

        var match = matches[0];
        var isChild = match.ParentThreadId is not null || match.SessionId is not null && !string.Equals(match.Id, match.SessionId, StringComparison.Ordinal);
        return new("exact", match.Cwd, match.ParentThreadId, isChild, candidates.Count, directories.Count, entries, bytesRead, false);
    }

    static PersistedCallerLocationResolution Missing(string outcome) => new(outcome, null, null, false, 0, 0, 0, 0, false);

    static List<string> RecentDirectories(string sessions, int days)
    {
        var result = new List<string> { sessions };
        for (var offset = 0; offset < days; offset++)
        {
            var date = DateTime.UtcNow.Date.AddDays(-offset);
            result.Add(Path.Combine(sessions, date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd")));
        }
        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    static Location? Read(string path, int maximumBytes, out long bytesRead, out bool truncated)
    {
        bytesRead = 0;
        truncated = false;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var buffer = new byte[maximumBytes];
        var length = 0;
        while (length < buffer.Length)
        {
            var read = stream.Read(buffer, length, Math.Min(4096, buffer.Length - length));
            if (read == 0)
                break;
            var newline = Array.IndexOf(buffer, (byte)'\n', length, read);
            length += read;
            bytesRead += read;
            if (newline >= 0)
            {
                length = newline;
                break;
            }
        }
        if (length == buffer.Length && Array.IndexOf(buffer, (byte)'\n') < 0)
        {
            truncated = true;
            return null;
        }
        if (length > 0 && buffer[length - 1] == '\r')
            length--;
        if (length == 0)
            return null;

        using var document = JsonDocument.Parse(buffer.AsMemory(0, length));
        var record = document.RootElement;
        if (Text(record, "type") != "session_meta" || !record.TryGetProperty("payload", out var payload) || payload.ValueKind != JsonValueKind.Object)
            return null;
        var id = Text(payload, "id") ?? Text(payload, "session_id");
        return string.IsNullOrWhiteSpace(id) ? null : new(id, Text(payload, "session_id"), Text(payload, "parent_thread_id"), Text(payload, "cwd"));
    }

    static string? Text(JsonElement value, string property) => value.TryGetProperty(property, out var item) && item.ValueKind == JsonValueKind.String ? item.GetString() : null;
    sealed record Location(string Id, string? SessionId, string? ParentThreadId, string? Cwd);
}
