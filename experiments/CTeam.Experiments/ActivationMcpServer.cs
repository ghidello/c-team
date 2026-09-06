using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CTeam.Experiments;

public static class ActivationMcpServer
{
    const string ProtocolVersion = "2025-06-18";
    const string ToolName = "cteam";
    static readonly DateTimeOffset ProcessStartedAt = DateTimeOffset.UtcNow;
    static readonly long StartupWorkingSetBytes = ReadWorkingSet();

    public static async Task<int> RunAsync(TextReader input, TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
    {
        using var evidence = EvidenceLog.Create();
        var totalRolloutReads = 0;
        evidence.Write("process-start", new JsonObject
        {
            ["pid"] = Environment.ProcessId,
            ["started_at"] = ProcessStartedAt.ToString("O"),
            ["startup_working_set_bytes"] = StartupWorkingSetBytes,
            ["persisted_mission_reads"] = 0
        });

        try
        {
            string? line;
            while ((line = await input.ReadLineAsync(cancellationToken)) is not null)
            {
                JsonObject request;
                try
                {
                    request = JsonNode.Parse(line)?.AsObject() ?? throw new JsonException("Request must be an object.");
                }
                catch (JsonException)
                {
                    await SendAsync(output, evidence, Error(null, -32700, "Parse error"));
                    continue;
                }

                evidence.Write("message-received", request);
                var method = request["method"]?.GetValue<string>();
                var id = request["id"]?.DeepClone();
                var parameters = request["params"] as JsonObject;

                if (method == "initialize")
                {
                    evidence.Write("initialize", request);
                    await SendAsync(output, evidence, Response(id, new JsonObject
                    {
                        ["protocolVersion"] = ProtocolVersion,
                        ["capabilities"] = new JsonObject { ["tools"] = new JsonObject() },
                        ["serverInfo"] = new JsonObject { ["name"] = "cteam", ["version"] = "0.1.0-experiment-008b" }
                    }));
                    continue;
                }

                if (method == "notifications/initialized")
                {
                    evidence.Write("initialized", new JsonObject());
                    continue;
                }

                if (method == "tools/list")
                {
                    var definition = ToolDefinition();
                    var serialized = definition.ToJsonString();
                    evidence.Write("tools-listed", new JsonObject
                    {
                        ["tool_count"] = 1,
                        ["definition_chars"] = serialized.Length,
                        ["definition_utf8_bytes"] = System.Text.Encoding.UTF8.GetByteCount(serialized)
                    });
                    await SendAsync(output, evidence, Response(id, new JsonObject { ["tools"] = new JsonArray { (JsonNode)definition } }));
                    continue;
                }

                if (method == "tools/call")
                {
                    JsonObject result;
                    var isError = false;
                    try
                    {
                        var name = parameters?["name"]?.GetValue<string>();
                        if (name != ToolName)
                            throw new ArgumentException("Unknown tool.");
                        if (parameters?["arguments"] is not JsonObject arguments)
                            throw new ArgumentException("Tool arguments must be an object.");

                        var action = arguments["action"]?.GetValue<string>();
                        if (action is null || !Actions.Contains(action, StringComparer.Ordinal))
                            throw new ArgumentException("Unknown or missing action.");

                        var caller = CallerContext.FromToolParameters(parameters);
                        var activation = ActivationProbe.Probe(caller);
                        totalRolloutReads += activation.RolloutFilesRead;
                        result = Result(action, activation);
                        evidence.Write("activation-checked", new JsonObject
                        {
                            ["action"] = action,
                            ["status"] = activation.Status,
                            ["workspace_count"] = activation.WorkspaceCount,
                            ["marker_checked"] = activation.MarkerChecked,
                            ["resolution_source"] = activation.ResolutionSource,
                            ["database_outcome"] = activation.DatabaseOutcome,
                            ["database_rows_read"] = activation.DatabaseRowsRead,
                            ["database_lookup_microseconds"] = activation.DatabaseLookupMicroseconds,
                            ["rollout_files_read"] = activation.RolloutFilesRead,
                            ["project_boundary"] = activation.ProjectBoundary,
                            ["normalization_levels"] = activation.NormalizationLevels,
                            ["caller_is_child"] = activation.CallerIsChild,
                            ["parent_assisted"] = activation.ParentAssisted,
                            ["persisted_mission_reads"] = activation.RolloutFilesRead,
                            ["has_thread_id"] = caller.ThreadId is not null,
                            ["has_session_id"] = caller.SessionId is not null
                        });
                    }
                    catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or JsonException or IOException or UnauthorizedAccessException)
                    {
                        isError = true;
                        result = new JsonObject { ["error"] = "invalid-tool-request", ["detail"] = exception.GetType().Name };
                        evidence.Write("tool-error", new JsonObject { ["type"] = exception.GetType().Name });
                    }

                    await SendAsync(output, evidence, Response(id, ToolResult(result, isError)));
                    continue;
                }

                if (id is not null)
                    await SendAsync(output, evidence, Error(id, -32601, "Method not found"));
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception exception)
        {
            evidence.Write("process-error", new JsonObject { ["type"] = exception.GetType().Name, ["message"] = exception.Message });
            await error.WriteLineAsync($"{exception.GetType().Name}: {exception.Message}");
            return 1;
        }
        finally
        {
            evidence.Write("process-stop", new JsonObject { ["pid"] = Environment.ProcessId, ["persisted_mission_reads"] = totalRolloutReads });
        }
    }

    public static JsonObject ToolDefinition() => new()
    {
        ["name"] = ToolName,
        ["description"] = "Check C-Team status for the calling project.",
        ["inputSchema"] = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["action"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = new JsonArray(Actions.Select(action => JsonValue.Create(action)).ToArray()),
                    ["description"] = "C-Team view to query."
                }
            },
            ["required"] = new JsonArray("action"),
            ["additionalProperties"] = false
        }
    };

    static readonly string[] Actions = ["status", "mission", "agents", "usage", "open"];

    static JsonObject Result(string action, ActivationSnapshot activation)
    {
        var status = activation.Status;
        var implemented = action == "status";
        if (status == "project_enabled" && !implemented)
            status = "experiment_action_not_implemented";

        return new JsonObject
        {
            ["status"] = status,
            ["action"] = action,
            ["action_implemented"] = implemented,
            ["project_resolved"] = activation.ProjectResolved,
            ["workspace_count"] = activation.WorkspaceCount,
            ["marker_checked"] = activation.MarkerChecked,
            ["persisted_mission_read"] = activation.PersistedMissionRead,
            ["resolution_source"] = activation.ResolutionSource,
            ["database_outcome"] = activation.DatabaseOutcome,
            ["database_rows_read"] = activation.DatabaseRowsRead,
            ["database_lookup_microseconds"] = activation.DatabaseLookupMicroseconds,
            ["rollout_files_read"] = activation.RolloutFilesRead,
            ["project_boundary"] = activation.ProjectBoundary,
            ["normalization_levels"] = activation.NormalizationLevels,
            ["caller_is_child"] = activation.CallerIsChild,
            ["parent_assisted"] = activation.ParentAssisted,
            ["pid"] = Environment.ProcessId,
            ["process_started_at"] = ProcessStartedAt.ToString("O")
        };
    }

    static long ReadWorkingSet()
    {
        using var process = Process.GetCurrentProcess();
        return process.WorkingSet64;
    }

    static JsonObject ToolResult(JsonObject value, bool isError) => new()
    {
        ["content"] = new JsonArray { (JsonNode)new JsonObject { ["type"] = "text", ["text"] = value.ToJsonString() } },
        ["structuredContent"] = value.DeepClone(),
        ["isError"] = isError
    };

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
            if (string.IsNullOrWhiteSpace(directory))
                return new(null);
            Directory.CreateDirectory(directory);
            var filename = $"activation-mcp-{Environment.ProcessId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfffffff}.jsonl";
            return new(new StreamWriter(new FileStream(Path.Combine(directory, filename), FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { AutoFlush = true });
        }

        public void Write(string kind, JsonNode data) => writer?.WriteLine(new JsonObject
        {
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
            ["kind"] = kind,
            ["data"] = data.DeepClone()
        }.ToJsonString());

        public void Dispose() => writer?.Dispose();
    }
}
