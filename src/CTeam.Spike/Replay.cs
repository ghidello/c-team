using System.Text.Json;
using System.Text.Json.Nodes;
using CTeam.Spike.Codex;

namespace CTeam.Spike;

public static class Replay
{
    public static async Task<MissionState> ReadAsync(string path)
    {
        var state = new MissionState();
        var mapper = new CodexEventMapper(state);
        using var reader = File.OpenText(path);
        while (await reader.ReadLineAsync() is { } line)
        {
            var entry = JsonNode.Parse(line) as JsonObject ?? throw new InvalidDataException("Invalid recording envelope");
            var direction = entry["direction"]?.GetValue<string>() ?? "in";
            if (direction == "stderr") continue;
            var raw = entry["raw"] ?? entry["message"];
            if (raw is JsonValue value && value.TryGetValue<string>(out var text))
            {
                try { raw = JsonNode.Parse(text); }
                catch (JsonException) { continue; }
            }
            if (raw is JsonObject) mapper.Ingest(raw, direction, entry["timestamp"]?.GetValue<DateTimeOffset>());
        }
        return state;
    }
}
