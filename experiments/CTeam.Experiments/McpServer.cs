using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CTeam.Experiments;

public static class McpServer
{
    const string ProtocolVersion = "2025-06-18";
    static readonly string[] ToolNames = ["cteam_ping", "cteam_runtime_info", "cteam_test_plugin_data", "cteam_probe_current_mission", "cteam_get_current_mission", "cteam_get_agent_tree", "cteam_get_usage"];

    public static async Task<int> RunAsync(TextReader input, TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
    {
        var evidence = EvidenceLog.Create(); evidence.Write("process-start", RuntimeInfo());
        var rootsDeclared = false;
        try
        {
            string? line;
            while ((line = await input.ReadLineAsync(cancellationToken)) is not null)
            {
                JsonObject request;
                try { request = JsonNode.Parse(line)?.AsObject() ?? throw new JsonException("Request must be an object."); }
                catch (JsonException) { await SendAsync(output, evidence, Error(null, -32700, "Parse error")); continue; }
                evidence.Write("message-received", request);
                var method = request["method"]?.GetValue<string>(); var id = request["id"]?.DeepClone(); var parameters = request["params"] as JsonObject;
                if ((request["result"] is not null || request["error"] is not null) && request["id"] is JsonValue responseId && responseId.TryGetValue<string>(out var responseIdText) && responseIdText == "cteam-roots-1")
                {
                    evidence.Write(request["error"] is null ? "roots-result" : "roots-error", request);
                    continue;
                }
                if (method == "initialize")
                {
                    evidence.Write("initialize", request);
                    await SendAsync(output, evidence, Response(id, new JsonObject { ["protocolVersion"] = ProtocolVersion, ["capabilities"] = new JsonObject { ["tools"] = new JsonObject() }, ["serverInfo"] = new JsonObject { ["name"] = "cteam", ["version"] = "0.1.0-experiment-006" } }));
                    rootsDeclared = parameters?["capabilities"]?["roots"] is not null;
                    continue;
                }
                if (method == "notifications/initialized")
                {
                    evidence.Write("initialized", new JsonObject());
                    if (rootsDeclared)
                        await SendAsync(output, evidence, new JsonObject { ["jsonrpc"] = "2.0", ["id"] = "cteam-roots-1", ["method"] = "roots/list", ["params"] = new JsonObject() });
                    continue;
                }
                if (method == "tools/list") { await SendAsync(output, evidence, Response(id, new JsonObject { ["tools"] = ToolList() })); continue; }
                if (method == "tools/call")
                {
                    string? name = null;
                    JsonObject result;
                    var isError = false;
                    try
                    {
                        name = parameters?["name"]?.GetValue<string>();
                        if (name is null || !ToolNames.Contains(name, StringComparer.Ordinal)) throw new ArgumentException("Unknown tool.");
                        var argumentNode = parameters?["arguments"];
                        if (argumentNode is not null && argumentNode is not JsonObject) throw new ArgumentException("Tool arguments must be an object.");
                        var arguments = argumentNode as JsonObject;
                        result = await CallToolAsync(name, arguments, CallerContext.FromToolParameters(parameters), cancellationToken);
                    }
                    catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or JsonException or IOException or UnauthorizedAccessException)
                    {
                        isError = true;
                        result = new JsonObject { ["error"] = "invalid-tool-request", ["detail"] = exception.GetType().Name };
                        evidence.Write("tool-error", new JsonObject { ["tool"] = name, ["type"] = exception.GetType().Name });
                    }
                    evidence.Write("tool-result", new JsonObject { ["tool"] = name, ["result"] = result });
                    await SendAsync(output, evidence, Response(id, ToolResult(result, isError)));
                    continue;
                }
                if (id is not null) await SendAsync(output, evidence, Error(id, -32601, "Method not found"));
            }
            return 0;
        }
        catch (OperationCanceledException) { return 0; }
        catch (Exception exception)
        {
            evidence.Write("process-error", new JsonObject { ["type"] = exception.GetType().Name, ["message"] = exception.Message });
            await error.WriteLineAsync($"{exception.GetType().Name}: {exception.Message}");
            return 1;
        }
        finally { evidence.Write("process-stop", RuntimeInfo()); evidence.Dispose(); }
    }

    static async Task<JsonObject> CallToolAsync(string name, JsonObject? arguments, CallerContext caller, CancellationToken cancellationToken)
    {
        if (name == "cteam_ping")
        {
            var hold = Math.Clamp(arguments?["hold_ms"]?.GetValue<int>() ?? 0, 0, 15000);
            if (hold > 0) await Task.Delay(hold, cancellationToken);
            return new JsonObject { ["ok"] = true, ["pid"] = Environment.ProcessId, ["context_label"] = Environment.GetEnvironmentVariable("CTEAM_CONTEXT_LABEL") };
        }
        if (name == "cteam_runtime_info") return RuntimeInfo();
        if (name == "cteam_test_plugin_data") return TestPluginData();
        var snapshot = CallerMissionProbe.Probe(caller, arguments?["project_hint"]?.GetValue<string>(), arguments?["mission_id"]?.GetValue<string>());
        return MissionResult(snapshot, name);
    }

    static JsonObject RuntimeInfo()
    {
        using var process = Process.GetCurrentProcess();
        var environment = new JsonObject();
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key = entry.Key?.ToString();
            if (key is not null && (key.StartsWith("PLUGIN_", StringComparison.OrdinalIgnoreCase) || key.StartsWith("CODEX_", StringComparison.OrdinalIgnoreCase) || key.StartsWith("MCP_", StringComparison.OrdinalIgnoreCase) || key.StartsWith("CTEAM_", StringComparison.OrdinalIgnoreCase)))
                environment[key] = IsSensitiveEnvironmentKey(key) ? "[redacted]" : entry.Value?.ToString();
        }
        var codexHome = MissionProbe.ResolveCodexHome();
        return new JsonObject { ["pid"] = Environment.ProcessId, ["started_at"] = process.StartTime.ToUniversalTime().ToString("O"), ["cwd"] = Environment.CurrentDirectory,
            ["base_directory"] = AppContext.BaseDirectory, ["context_label"] = Environment.GetEnvironmentVariable("CTEAM_CONTEXT_LABEL"), ["environment"] = environment,
            ["codex_home_source"] = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CODEX_HOME")) ? "user-profile" : "CODEX_HOME",
            ["sessions_directory_exists"] = Directory.Exists(Path.Combine(codexHome, "sessions")) };
    }

    static bool IsSensitiveEnvironmentKey(string key) => key.Contains("TOKEN", StringComparison.OrdinalIgnoreCase) || key.Contains("SECRET", StringComparison.OrdinalIgnoreCase)
        || key.Contains("KEY", StringComparison.OrdinalIgnoreCase) || key.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) || key.Contains("AUTH", StringComparison.OrdinalIgnoreCase)
        || key.EndsWith("_ID", StringComparison.OrdinalIgnoreCase) || key.Contains("PIPE_PATH", StringComparison.OrdinalIgnoreCase) || key.Contains("CREDENTIAL", StringComparison.OrdinalIgnoreCase);

    static JsonObject TestPluginData()
    {
        var path = Environment.GetEnvironmentVariable("PLUGIN_DATA");
        if (string.IsNullOrWhiteSpace(path)) return new JsonObject { ["available"] = false, ["reason"] = "PLUGIN_DATA-not-provided" };
        try { Directory.CreateDirectory(path); File.WriteAllText(Path.Combine(path, "cteam-experiment-005.marker"), "cteam-experiment-005"); return new JsonObject { ["available"] = true, ["wrote_marker"] = true }; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return new JsonObject { ["available"] = true, ["wrote_marker"] = false, ["error"] = exception.GetType().Name }; }
    }

    static JsonObject MissionResult(MissionSnapshot snapshot, string tool) => new() { ["tool"] = tool, ["mission_key"] = snapshot.MissionKey, ["candidate_count"] = snapshot.CandidateCount,
        ["confidence"] = snapshot.Confidence, ["selection_signal"] = snapshot.SelectionSignal, ["status"] = snapshot.Status, ["agent_count"] = snapshot.AgentCount,
        ["configured_model"] = snapshot.ConfiguredModel, ["effort"] = snapshot.Effort, ["total_tokens"] = snapshot.TotalTokens,
        ["scanned_files"] = snapshot.ScannedFiles, ["scan_truncated"] = snapshot.ScanTruncated, ["correlation_outcome"] = snapshot.CorrelationOutcome,
        ["correlation_selection"] = snapshot.CorrelationSelection, ["root_mission_key"] = snapshot.RootMissionKey, ["caller_kind"] = snapshot.CallerKind,
        ["correlation_directories_examined"] = snapshot.CorrelationDirectoriesExamined,
        ["correlation_directory_entries_examined"] = snapshot.CorrelationDirectoryEntriesExamined, ["correlation_bytes_read"] = snapshot.CorrelationBytesRead,
        ["correlation_scan_truncated"] = snapshot.CorrelationScanTruncated };

    static JsonArray ToolList()
    {
        var tools = new JsonArray();
        foreach (var name in ToolNames)
        {
            var isMissionTool = name is "cteam_probe_current_mission" or "cteam_get_current_mission" or "cteam_get_agent_tree" or "cteam_get_usage";
            var properties = name == "cteam_ping"
                ? new JsonObject { ["hold_ms"] = new JsonObject { ["type"] = "integer", ["minimum"] = 0, ["maximum"] = 15000 } }
                : isMissionTool
                    ? new JsonObject { ["project_hint"] = new JsonObject { ["type"] = "string" }, ["mission_id"] = new JsonObject { ["type"] = "string" } }
                    : new JsonObject();
            var description = name switch
            {
                "cteam_ping" => "Harmless C-Team MCP ping.",
                "cteam_test_plugin_data" => "Write one harmless marker only when the host supplies PLUGIN_DATA.",
                "cteam_runtime_info" => "Return bounded process and plugin environment metadata.",
                _ => "Return a sanitized read-only C-Team mission snapshot."
            };
            var tool = new JsonObject { ["name"] = name, ["description"] = description, ["inputSchema"] = new JsonObject { ["type"] = "object", ["properties"] = properties } };
            if (isMissionTool) tool["outputSchema"] = MissionOutputSchema();
            tools.Add((JsonNode)tool);
        }
        return tools;
    }
    static JsonObject MissionOutputSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["tool"] = new JsonObject { ["type"] = "string" },
            ["mission_key"] = Nullable("string"),
            ["candidate_count"] = new JsonObject { ["type"] = "integer", ["minimum"] = 0 },
            ["confidence"] = new JsonObject { ["type"] = "string" },
            ["selection_signal"] = new JsonObject { ["type"] = "string" },
            ["status"] = Nullable("string"),
            ["agent_count"] = new JsonObject { ["type"] = "integer", ["minimum"] = 0 },
            ["configured_model"] = Nullable("string"),
            ["effort"] = Nullable("string"),
            ["total_tokens"] = new JsonObject { ["type"] = new JsonArray("integer", "null"), ["minimum"] = 0 },
            ["scanned_files"] = new JsonObject { ["type"] = "integer", ["minimum"] = 0 },
            ["scan_truncated"] = new JsonObject { ["type"] = "boolean" },
            ["correlation_outcome"] = Nullable("string"),
            ["correlation_selection"] = Nullable("string"),
            ["root_mission_key"] = Nullable("string"),
            ["caller_kind"] = Nullable("string"),
            ["correlation_directories_examined"] = new JsonObject { ["type"] = "integer", ["minimum"] = 0 },
            ["correlation_directory_entries_examined"] = new JsonObject { ["type"] = "integer", ["minimum"] = 0 },
            ["correlation_bytes_read"] = new JsonObject { ["type"] = "integer", ["minimum"] = 0 },
            ["correlation_scan_truncated"] = new JsonObject { ["type"] = "boolean" }
        },
        ["required"] = new JsonArray("tool", "mission_key", "candidate_count", "confidence", "selection_signal", "status", "agent_count", "configured_model", "effort", "total_tokens", "scanned_files", "scan_truncated", "correlation_outcome", "correlation_selection", "root_mission_key", "caller_kind", "correlation_directories_examined", "correlation_directory_entries_examined", "correlation_bytes_read", "correlation_scan_truncated")
    };
    static JsonObject Nullable(string type) => new() { ["type"] = new JsonArray(type, "null") };
    static JsonObject ToolResult(JsonObject value, bool isError = false) => new() { ["content"] = new JsonArray { (JsonNode)new JsonObject { ["type"] = "text", ["text"] = value.ToJsonString() } }, ["structuredContent"] = value.DeepClone(), ["isError"] = isError };
    static JsonObject Response(JsonNode? id, JsonNode result) => new() { ["jsonrpc"] = "2.0", ["id"] = id, ["result"] = result };
    static JsonObject Error(JsonNode? id, int code, string message) => new() { ["jsonrpc"] = "2.0", ["id"] = id, ["error"] = new JsonObject { ["code"] = code, ["message"] = message } };
    static async Task SendAsync(TextWriter output, EvidenceLog evidence, JsonObject message)
    {
        evidence.Write("message-sent", message);
        await output.WriteLineAsync(message.ToJsonString());
        await output.FlushAsync();
    }

    sealed class EvidenceLog : IDisposable
    {
        readonly StreamWriter? writer;
        EvidenceLog(StreamWriter? writer) => this.writer = writer;
        public static EvidenceLog Create()
        {
            var directory = Environment.GetEnvironmentVariable("CTEAM_EXPERIMENT_EVIDENCE");
            if (string.IsNullOrWhiteSpace(directory)) return new(null);
            Directory.CreateDirectory(directory);
            var filename = $"mcp-{Environment.ProcessId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfffffff}.jsonl";
            return new(new StreamWriter(new FileStream(Path.Combine(directory, filename), FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { AutoFlush = true });
        }
        public void Write(string kind, JsonNode data) => writer?.WriteLine(new JsonObject { ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"), ["kind"] = kind, ["data"] = data.DeepClone() }.ToJsonString());
        public void Dispose() => writer?.Dispose();
    }
}
