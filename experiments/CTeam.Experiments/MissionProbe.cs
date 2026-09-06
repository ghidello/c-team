using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CTeam.Experiments;

public sealed record MissionSnapshot(string? MissionKey, int CandidateCount, string Confidence, string SelectionSignal, string? Status,
    int AgentCount, string? ConfiguredModel, string? Effort, long? TotalTokens, int ScannedFiles, bool ScanTruncated,
    string? CorrelationOutcome = null, string? CorrelationSelection = null, string? RootMissionKey = null, string? CallerKind = null,
    int CorrelationDirectoriesExamined = 0, int CorrelationDirectoryEntriesExamined = 0, long CorrelationBytesRead = 0, bool CorrelationScanTruncated = false);

public static class MissionProbe
{
    const int MaxFiles = 64;
    const int MaxSearchDays = 31;
    const long MaxRolloutBytes = 64L * 1024 * 1024;
    const long MaxTotalBytes = 256L * 1024 * 1024;

    public static MissionSnapshot Probe(string? projectHint, string? missionId, string? codexHome = null)
    {
        var discovery = Discover(codexHome, missionId);
        var candidates = discovery.Candidates.OrderByDescending(x => x.Timestamp).ToList();
        if (!string.IsNullOrWhiteSpace(missionId))
        {
            var match = candidates.FirstOrDefault(x => x.Id == missionId);
            return match is null ? Empty(candidates.Count, "mission_id-not-found", "ambiguous", discovery) : Snapshot(match, candidates.Count, "certain", "mission_id", discovery);
        }
        if (!string.IsNullOrWhiteSpace(projectHint))
        {
            var matched = candidates.Where(x => SamePath(x.Cwd, projectHint)).ToList();
            if (matched.Count == 0) return Empty(0, "project_hint-no-match", "ambiguous", discovery);
            return Snapshot(matched[0], matched.Count, matched.Count == 1 && !discovery.Truncated ? "high-confidence" : "ambiguous", "project_hint", discovery);
        }
        return candidates.Count == 0 ? Empty(0, "no-session-files", "ambiguous", discovery) : Snapshot(candidates[0], candidates.Count, "ambiguous", "latest-record-timestamp", discovery);
    }

    static MissionSnapshot Empty(int count, string signal, string confidence, Discovery discovery) => new(null, count, confidence, signal, null, 0, null, null, null, discovery.ScannedFiles, discovery.Truncated);

    static MissionSnapshot Snapshot(Candidate candidate, int count, string confidence, string signal, Discovery discovery) => new(Hash(candidate.Id), count, confidence, signal,
        candidate.Status, candidate.Agents.Count, candidate.Model, candidate.Effort, candidate.TotalTokens, discovery.ScannedFiles, discovery.Truncated);

    static Discovery Discover(string? codexHome, string? missionId)
    {
        var root = ResolveCodexHome(codexHome);
        var sessions = Path.Combine(root, "sessions");
        if (!Directory.Exists(sessions)) return new([], 0, false);
        IReadOnlyList<FileInfo> files;
        var traversalTruncated = false;
        try
        {
            files = DiscoverFiles(sessions, missionId, out traversalTruncated);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return new([], 0, true); }
        var candidates = new List<Candidate>();
        var scannedFiles = 0;
        var totalBytes = 0L;
        var truncated = traversalTruncated;
        foreach (var file in files)
        {
            scannedFiles++;
            var remaining = MaxTotalBytes - totalBytes;
            if (file.Length > MaxRolloutBytes || file.Length > remaining) { truncated = true; continue; }
            Candidate? candidate = null;
            try
            {
                candidate = Read(file.FullName, Math.Min(MaxRolloutBytes, remaining), out var bytesRead, out var fileTruncated);
                totalBytes += bytesRead;
                truncated |= fileTruncated;
            }
            catch (IOException) { } catch (JsonException) { } catch (UnauthorizedAccessException) { }
            if (candidate is not null) candidates.Add(candidate);
        }
        return new(candidates, scannedFiles, truncated);
    }

    static IReadOnlyList<FileInfo> DiscoverFiles(string sessions, string? missionId, out bool truncated)
    {
        var files = new List<FileInfo>(MaxFiles);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var directories = new List<string> { sessions };
        for (var offset = 0; offset < MaxSearchDays; offset++)
        {
            var date = DateTime.UtcNow.Date.AddDays(-offset);
            directories.Add(Path.Combine(sessions, date.ToString("yyyy"), date.ToString("MM"), date.ToString("dd")));
        }
        foreach (var directory in directories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(directory)) continue;
            if (Guid.TryParse(missionId, out _))
                foreach (var path in Directory.EnumerateFiles(directory, $"*{missionId}.jsonl", SearchOption.TopDirectoryOnly).Take(1))
                    if (seen.Add(path)) files.Add(new FileInfo(path));
            foreach (var path in Directory.EnumerateFiles(directory, "*.jsonl", SearchOption.TopDirectoryOnly))
            {
                if (!seen.Add(path)) continue;
                if (files.Count == MaxFiles) { truncated = true; return Prioritize(files, missionId); }
                files.Add(new FileInfo(path));
            }
        }
        truncated = false;
        return Prioritize(files, missionId);
    }

    static IReadOnlyList<FileInfo> Prioritize(IEnumerable<FileInfo> files, string? missionId) => files
        .OrderByDescending(file => !string.IsNullOrWhiteSpace(missionId) && file.Name.Contains(missionId, StringComparison.Ordinal))
        .ThenByDescending(file => file.LastWriteTimeUtc).ToArray();

    internal static string ResolveCodexHome(string? explicitPath = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath)) return explicitPath;
        var root = Environment.GetEnvironmentVariable("CODEX_HOME");
        if (!string.IsNullOrWhiteSpace(root)) return root;
        var profile = Environment.GetEnvironmentVariable("USERPROFILE");
        if (string.IsNullOrWhiteSpace(profile)) profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(profile, ".codex");
    }

    static Candidate? Read(string path, long maxBytes, out long bytesRead, out bool truncated)
    {
        bytesRead = 0;
        truncated = false;
        Candidate? candidate = null;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var length = stream.Length;
        if (length > maxBytes) { truncated = true; return null; }
        var buffer = new byte[(int)length];
        while (bytesRead < length)
        {
            var read = stream.Read(buffer, (int)bytesRead, (int)(length - bytesRead));
            if (read == 0) break;
            bytesRead += read;
        }
        using var reader = new StringReader(Encoding.UTF8.GetString(buffer, 0, (int)bytesRead));
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            JsonDocument doc;
            try { doc = JsonDocument.Parse(line); }
            catch (JsonException) { continue; }
            using (doc)
            {
                var record = doc.RootElement;
                if (!record.TryGetProperty("type", out var type)) continue;
                var timestamp = Text(record, "timestamp");
                var when = DateTimeOffset.TryParse(timestamp, out var parsed) ? parsed : DateTimeOffset.MinValue;
                if (!record.TryGetProperty("payload", out var payload) || payload.ValueKind != JsonValueKind.Object) continue;
                if (type.GetString() == "session_meta")
                {
                    var id = Text(payload, "id") ?? Text(payload, "session_id");
                    if (id is null) continue;
                    var sessionId = Text(payload, "session_id");
                    if (Text(payload, "parent_thread_id") is not null || sessionId is not null && !string.Equals(id, sessionId, StringComparison.Ordinal)) return null;
                    candidate ??= new Candidate(id);
                    candidate.Id = id; candidate.Cwd = Text(payload, "cwd") ?? candidate.Cwd; candidate.Model = Text(payload, "model") ?? candidate.Model;
                }
                if (candidate is null) continue;
                candidate.Timestamp = when > candidate.Timestamp ? when : candidate.Timestamp;
                var thread = Text(payload, "thread_id") ?? candidate.Id;
                candidate.Agents.Add(thread);
                if (payload.TryGetProperty("item", out var item) && item.ValueKind == JsonValueKind.Object && Text(item, "agent_thread_id") is { } childId)
                    candidate.Agents.Add(childId);
                if (type.GetString() == "turn_context") { candidate.Model = Text(payload, "model") ?? candidate.Model; candidate.Effort = Text(payload, "effort") ?? candidate.Effort; candidate.Status = "running"; }
                if (type.GetString() == "event_msg")
                {
                    var kind = Text(payload, "type");
                    if (kind == "task_started") candidate.Status = "running";
                    if (kind == "task_complete") candidate.Status = "completed";
                    if (kind == "turn_aborted") candidate.Status = "interrupted";
                }
                if (type.GetString() == "token_usage_record" && payload.TryGetProperty("thread_token_usage", out var usage)) candidate.TotalTokens = Number(usage, "total_tokens") ?? candidate.TotalTokens;
            }
        }
        return candidate;
    }

    static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..16];
    static bool SamePath(string? left, string right)
    {
        if (string.IsNullOrWhiteSpace(left)) return false;
        try
        {
            var normalizedLeft = Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedRight = Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException) { return false; }
    }
    static string? Text(JsonElement value, string property) => value.TryGetProperty(property, out var item) && item.ValueKind == JsonValueKind.String ? item.GetString() : null;
    static long? Number(JsonElement value, string property) => value.TryGetProperty(property, out var item) && item.TryGetInt64(out var number) ? number : null;

    sealed class Candidate(string id)
    {
        public string Id { get; set; } = id;
        public string? Cwd { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string? Status { get; set; }
        public string? Model { get; set; }
        public string? Effort { get; set; }
        public long? TotalTokens { get; set; }
        public HashSet<string> Agents { get; } = [];
    }

    sealed record Discovery(IReadOnlyList<Candidate> Candidates, int ScannedFiles, bool Truncated);
}
