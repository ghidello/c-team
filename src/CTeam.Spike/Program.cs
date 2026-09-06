using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CTeam.Spike.Codex;

namespace CTeam.Spike;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0) return Usage();
        var command = args[0]; var options = Options.Parse(args[1..]);
        if (command == "replay")
        {
            if (options.Positionals.Count != 1) return Usage();
            var state = await Replay.ReadAsync(options.Positionals[0]);
            Renderer.Write(state);
            if (options.Get("json") is { } json) await File.WriteAllTextAsync(json, state.ToJson());
            return 0;
        }
        if (command == "watch")
        {
            var rollout = options.Get("file") ?? ResolveRollout(options);
            var seconds = int.TryParse(options.Get("duration-seconds"), out var requestedSeconds) ? requestedSeconds : 30;
            using var source = new PersistedDesktopSource(rollout, watch: true);
            await source.InitializeAsync();
            Renderer.Write(source.State);
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            source.Dispose();
            Renderer.Write(source.State);
            if (options.Get("json") is { } resultFile)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(resultFile))!);
                await File.WriteAllTextAsync(resultFile, JsonSerializer.Serialize(source.Result(), new JsonSerializerOptions { WriteIndented = true }));
            }
            return 0;
        }
        var codex = options.Required("codex"); var output = options.Required("output"); var serverCwd = command is "run" or "skills" ? options.Required("cwd") : Environment.CurrentDirectory;
        await using var client = await AppServer.StartAsync(codex, output, serverCwd);
        var experimentalApi = command == "run";
        await client.RequestAsync("initialize", new { clientInfo = new { name = "cteam", title = "C-Team", version = "spike" }, capabilities = new { experimentalApi, requestAttestation = false } });
        await client.NotifyAsync("initialized");
        if (command == "capabilities")
        {
            await client.RequestAsync("account/read", new { refreshToken = false });
            await client.RequestAsync("account/rateLimits/read", null);
            string? cursor = null;
            do { var result = await client.RequestAsync("model/list", new { cursor, limit = 100, includeHidden = true }); cursor = result?["nextCursor"]?.GetValue<string>(); } while (cursor is not null);
        }
        else if (command == "skills")
        {
            var cwd = options.Required("cwd");
            var result = await client.RequestAsync("skills/list", new { cwds = new[] { cwd }, forceReload = true });
            var skills = result?["data"]?[0]?["skills"] as JsonArray;
            var cteamSkills = skills?.OfType<JsonObject>().Where(skill => skill["pluginId"]?.GetValue<string>() == "c-team@personal").ToArray() ?? [];
            var catalogText = string.Join("\n", cteamSkills.Select(skill => $"{skill["name"]?.GetValue<string>()}\n{skill["description"]?.GetValue<string>()}"));
            Console.WriteLine(new JsonObject
            {
                ["cteam_skill_count"] = cteamSkills.Length,
                ["catalog_chars"] = catalogText.Length,
                ["catalog_utf8_bytes"] = Encoding.UTF8.GetByteCount(catalogText),
                ["skill_names"] = new JsonArray(cteamSkills.Select(skill => JsonValue.Create(skill["name"]?.GetValue<string>())).ToArray())
            }.ToJsonString());
        }
        else if (command == "run")
        {
            var cwd = options.Required("cwd"); var prompt = await File.ReadAllTextAsync(options.Required("prompt-file")); var model = options.Required("model");
            var config = options.Get("windows-sandbox") == "unelevated" ? new Dictionary<string, object?> { ["windows.sandbox"] = "unelevated" } : null;
            var historyMode = options.Get("history-mode") ?? (options.Get("review") == "detached" ? "legacy" : null);
            var thread = await client.RequestAsync("thread/start", new { cwd, runtimeWorkspaceRoots = new[] { cwd }, model, approvalPolicy = "never", sandbox = "workspace-write", historyMode, config });
            var threadId = thread?["thread"]?["id"]?.GetValue<string>() ?? throw new InvalidOperationException("thread/start returned no thread id");
            var startedTurn = await client.RequestAsync("turn/start", new { threadId, input = new[] { new { type = "text", text = prompt, text_elements = Array.Empty<object>() } }, model, effort = options.Get("effort") });
            var turnId = startedTurn?["turn"]?["id"]?.GetValue<string>() ?? throw new InvalidOperationException("turn/start returned no turn id"); await client.WaitForTurnAsync(threadId, turnId, TimeSpan.FromMinutes(15));
            await client.HydrateChildrenAsync();
            if (options.Get("review") == "detached") { var review = await client.RequestAsync("review/start", new { threadId, target = new { type = "uncommittedChanges" }, delivery = "detached" }); var reviewThread = review?["reviewThreadId"]?.GetValue<string>(); var reviewTurn = review?["turn"]?["id"]?.GetValue<string>(); if (reviewThread is not null && reviewTurn is not null) await client.WaitForTurnAsync(reviewThread, reviewTurn, TimeSpan.FromMinutes(15)); }
        }
        else return Usage();
        await client.StopAsync();
        if (command != "skills") Renderer.Write(client.State);
        if (options.Get("json") is { } stateFile) await File.WriteAllTextAsync(stateFile, client.State.ToJson());
        return 0;
    }
    static string ResolveRollout(Options options)
    {
        var thread = options.Required("thread"); var root = options.Get("sessions-root") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "sessions");
        var candidates = Directory.EnumerateFiles(root, $"*{thread}*.jsonl", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).ToArray();
        return candidates.FirstOrDefault() ?? throw new FileNotFoundException($"No rollout found for thread {thread} under {root}.");
    }
    static int Usage() { Console.Error.WriteLine("cteam watch (--file <rollout> | --thread <id> [--sessions-root <root>]) [--duration-seconds <seconds>] [--json <private measurement>] | capabilities --codex <path> --output <recording> | skills --codex <path> --cwd <project> --output <recording> | run --codex <path> --cwd <fixture> --prompt-file <file> --model <model> --output <recording> | replay <recording> [--json <state>]"); return 2; }
}

public sealed class Options
{
    readonly Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase); public List<string> Positionals { get; } = [];
    public static Options Parse(string[] args) { var o = new Options(); for (var i = 0; i < args.Length; i++) if (args[i].StartsWith("--")) o.values[args[i][2..]] = ++i < args.Length ? args[i] : throw new ArgumentException($"Missing {args[i]}"); else o.Positionals.Add(args[i]); return o; }
    public string Required(string key) => Get(key) ?? throw new ArgumentException($"Missing --{key}"); public string? Get(string key) => values.GetValueOrDefault(key);
}

public sealed class AppServer : IAsyncDisposable
{
    readonly SemaphoreSlim inputGate = new(1, 1);
    readonly Process process; readonly StreamWriter input; readonly Recorder recorder; readonly CodexEventMapper mapper; readonly SemaphoreSlim ingress = new(1, 1); readonly ConcurrentDictionary<long, TaskCompletionSource<JsonNode?>> pending = new(); readonly TaskCompletionSource stdoutClosed = new(TaskCreationOptions.RunContinuationsAsynchronously); readonly Task stdout; readonly Task stderr; long nextId; int stopped; public MissionState State { get; } = new();
    AppServer(Process process, Recorder recorder) { this.process = process; this.recorder = recorder; mapper = new CodexEventMapper(State); input = process.StandardInput; stdout = DrainAsync(process.StandardOutput, false); stderr = DrainAsync(process.StandardError, true); }
    public static async Task<AppServer> StartAsync(string executable, string recording, string workingDirectory)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(recording))!);
        var p = Process.Start(new ProcessStartInfo(executable, "app-server --listen stdio://") { WorkingDirectory = workingDirectory, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }) ?? throw new InvalidOperationException("Could not start Codex.");
        var server = new AppServer(p, new Recorder(recording)); await Task.Yield(); return server;
    }
    public async Task<JsonNode?> RequestAsync(string method, object? parameters)
    {
        var id = Interlocked.Increment(ref nextId); var completion = new TaskCompletionSource<JsonNode?>(TaskCreationOptions.RunContinuationsAsynchronously); pending[id] = completion;
        var message = JsonSerializer.SerializeToNode(new { jsonrpc = "2.0", id, method, @params = parameters })!; await SendAsync(message);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45)); await using var _ = timeout.Token.Register(() => completion.TrySetException(new TimeoutException(method)));
        return await completion.Task;
    }
    public async Task WaitForTurnAsync(string threadId, string turnId, TimeSpan timeout)
    {
        var until = DateTimeOffset.UtcNow + timeout; var nextRender = DateTimeOffset.UtcNow;
        while (DateTimeOffset.UtcNow < until)
        {
            if (State.IsTurnCompleted(threadId, turnId)) return;
            if (stdoutClosed.Task.IsCompleted) await stdoutClosed.Task;
            if (DateTimeOffset.UtcNow >= nextRender) { Renderer.Write(State); nextRender = DateTimeOffset.UtcNow.AddSeconds(5); }
            await Task.Delay(100);
        }
        throw new TimeoutException($"turn completion: {turnId}");
    }
    public async Task HydrateChildrenAsync()
    {
        foreach (var threadId in State.ChildThreadIds()) await RequestAsync("thread/read", new { threadId, includeTurns = false });
    }
    async Task RecordAndMapAsync(string direction, JsonNode message)
    {
        await ingress.WaitAsync();
        try { var timestamp = await recorder.WriteAsync(direction, message); mapper.Ingest(message, direction, timestamp); }
        finally { ingress.Release(); }
    }
    async Task SendAsync(JsonNode message)
    {
        await inputGate.WaitAsync();
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref stopped) != 0, this);
            await RecordAndMapAsync("out", message); await input.WriteLineAsync(message.ToJsonString()); await input.FlushAsync();
        }
        finally { inputGate.Release(); }
    }
    async Task SendResponseAsync(JsonNode id, int code, string message)
    {
        var response = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = id.DeepClone(), ["error"] = new JsonObject { ["code"] = code, ["message"] = message } };
        await inputGate.WaitAsync();
        try
        {
            if (Volatile.Read(ref stopped) != 0) return;
            await RecordAndMapAsync("out", response); await input.WriteLineAsync(response.ToJsonString()); await input.FlushAsync();
        }
        finally { inputGate.Release(); }
    }
    public async Task NotifyAsync(string method) => await SendAsync(new JsonObject { ["jsonrpc"] = "2.0", ["method"] = method });
    async Task DrainAsync(StreamReader reader, bool isError)
    { try { string? line; while ((line = await reader.ReadLineAsync()) is not null) { if (isError) { await recorder.WriteAsync("stderr", new JsonObject { ["stderr"] = line }); continue; } JsonObject? message = null; await ingress.WaitAsync(); try { var timestamp = await recorder.WriteRawAsync("in", line); try { message = JsonNode.Parse(line) as JsonObject; } catch (JsonException) { } if (message is not null) mapper.Ingest(message, "in", timestamp); } finally { ingress.Release(); } if (message is null) continue; if (message["method"] is not null && message["id"] is JsonNode requestId) { await SendResponseAsync(requestId, -32601, $"Unsupported server request: {message["method"]!.GetValue<string>()}"); continue; } if (message["id"] is JsonValue idValue && idValue.TryGetValue<long>(out var id) && pending.TryRemove(id, out var tcs)) { if (message["error"] is JsonNode error) tcs.TrySetException(new RpcException(error.ToJsonString())); else tcs.TrySetResult(message["result"]); } } } finally { if (!isError) { var error = new EndOfStreamException("Codex app-server closed stdout."); foreach (var tcs in pending.Values) tcs.TrySetException(error); stdoutClosed.TrySetException(error); } } }
    public async Task StopAsync()
    {
        if (Interlocked.Exchange(ref stopped, 1) != 0) return;
        await inputGate.WaitAsync();
        try { input.Close(); }
        finally { inputGate.Release(); }
        var drains = Task.WhenAll(stdout, stderr); if (await Task.WhenAny(drains, Task.Delay(TimeSpan.FromSeconds(2))) != drains && !process.HasExited) process.Kill(entireProcessTree: true); await drains;
    }
    public async ValueTask DisposeAsync() { await StopAsync(); await recorder.DisposeAsync(); ingress.Dispose(); inputGate.Dispose(); process.Dispose(); }
}

public sealed class Recorder(string path) : IAsyncDisposable
{
    readonly StreamWriter writer = new(path, append: false) { AutoFlush = true }; readonly SemaphoreSlim gate = new(1, 1); long sequence;
    public Task<DateTimeOffset> WriteAsync(string direction, JsonNode raw) => WriteCoreAsync(direction, raw);
    public Task<DateTimeOffset> WriteRawAsync(string direction, string raw) => WriteCoreAsync(direction, JsonValue.Create(raw)!);
    async Task<DateTimeOffset> WriteCoreAsync(string direction, JsonNode raw) { await gate.WaitAsync(); try { var timestamp = DateTimeOffset.UtcNow; await writer.WriteLineAsync(JsonSerializer.Serialize(new { timestamp, sequence = ++sequence, direction, raw })); return timestamp; } finally { gate.Release(); } }
    public ValueTask DisposeAsync() { writer.Dispose(); gate.Dispose(); return ValueTask.CompletedTask; }
}
public sealed class RpcException(string error) : Exception($"Codex app-server RPC error: {error}");
