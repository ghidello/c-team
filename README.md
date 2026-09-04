# C-Team observability spike

A .NET 10 console experiment for Codex app-server telemetry, recordings, and deterministic replay. This is the disposable spike described in [SPIKE.md](SPIKE.md), not the production C-Team system.

Read [the findings and decision gate](docs/spike-findings.md), [protocol evidence](docs/codex-protocol.md), and [Desktop observation](docs/desktop-observation.md). Tested against installed Codex 0.153.1 on Windows.

## Build, test, replay

```powershell
dotnet build CTeam.Spike.sln
dotnet test CTeam.Spike.sln
dotnet run --project src/CTeam.Spike -- replay docs/evidence/example-run.jsonl
```

`docs/evidence/example-run.jsonl` is an allowlisted, lossy derivative of a real run. Its provenance file describes the removed fields; it is not synthetic success evidence. The complete private recordings stay under ignored `.cteam/recordings/`.

## Reproduce live experiments

Use a signed-in Codex installation and a .NET 10 SDK. Resolve the installed executable rather than assuming its versioned path:

```powershell
$codexExe = (Get-Command codex).Source
dotnet run --project src/CTeam.Spike -- capabilities --codex $codexExe --output .cteam/recordings/capabilities-new.jsonl --json .cteam/capabilities-new.json
./scripts/prepare-fixture.ps1 -Name repeat-1
$fixture = (Resolve-Path .cteam/repeat-1).Path
dotnet run --project src/CTeam.Spike -- run --codex $codexExe --cwd $fixture --prompt-file fixtures/telemetry/prompt.txt --model gpt-5.6-sol --effort low --output .cteam/recordings/repeat-1.jsonl --json .cteam/repeat-1.live.json
dotnet run --project src/CTeam.Spike -- replay .cteam/recordings/repeat-1.jsonl --json .cteam/repeat-1.replay.json
(Get-FileHash .cteam/repeat-1.live.json).Hash -eq (Get-FileHash .cteam/repeat-1.replay.json).Hash
```

On this host, elevated Windows helper setup failed. `--windows-sandbox unelevated` selects the [documented fallback](https://learn.chatgpt.com/docs/config-file/config-basic) for this run while retaining workspace-write restrictions. Patch failures remain experiment evidence; a completed Codex turn does not mean the fixture task succeeded.

`--review detached` selects legacy history and performs native review. Paginated history rejects detached review in this version. Run commands explicitly opt into experimental API because `runtimeWorkspaceRoots` and `historyMode` require it. Capability enumeration does not. The process working directory and runtime roots are set to the fixture.

Use a fresh recording filename: recordings are development-only and contain private prompts, tool output, account data, and code. The console never resumes Desktop tasks. It owns only the app-server it launches, rejects unsupported server requests, and records the resulting errors. This is not an interactive approval client.

## Plugin

The repository root is the minimal `c-team` plugin; its `inspect-codex-run` skill directs the local replay command. Manifest validation, local installation, discovery, invocation and refresh are documented in [plugin-validation.md](docs/plugin-validation.md). There is no MCP server or UI.

The installed catalog is recorded in [model-catalog.json](docs/evidence/model-catalog.json). Model identifiers remain strings. The Sol/Terra/Luna custom-role files are an experiment policy, and configured models are never presented as confirmed execution models.

Stop at the decision gate. SQLite history, production MCP, React/Apps SDK, analytics, steering, and automatic model routing are outside this repository's implemented spike.
