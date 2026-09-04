# C-Team observability spikes

C-Team is an observability companion for Codex multi-agent execution.

The first .NET 10 console spike validated app-server telemetry, persisted Desktop observation, recordings, replay, model catalog discovery, and a minimal Codex plugin shell. Its findings are documented under `docs/`.

The **Desktop near-live observation spike** in [`NEAR_LIVE_SPIKE.md`](NEAR_LIVE_SPIKE.md) is complete and recommends D1 — Hybrid, persisted-first.

Production runtime constraints are recorded in [`PRODUCTION_REQUIREMENTS.md`](PRODUCTION_REQUIREMENTS.md).

## First-spike evidence

Read:

- [`docs/spike-findings.md`](docs/spike-findings.md) — CQ1–CQ11 findings and decision gate;
- [`docs/near-live-observation.md`](docs/near-live-observation.md) — NL1–NL9 measurements and D1 decision;
- [`docs/codex-protocol.md`](docs/codex-protocol.md) — protocol evidence;
- [`docs/desktop-observation.md`](docs/desktop-observation.md) — Desktop persisted-observation experiment.

The first spike was tested against installed Codex 0.153.1 on Windows.

## Build, test, replay

```powershell
dotnet build CTeam.Spike.sln
dotnet test CTeam.Spike.sln
dotnet run --project src/CTeam.Spike -- replay docs/evidence/example-run.jsonl
dotnet run --project src/CTeam.Spike -- watch --thread <desktop-thread-id> --duration-seconds 30 --json .cteam/near-live/watch.json
```

`docs/evidence/example-run.jsonl` is an allowlisted, lossy derivative of a real run. Its provenance file describes removed fields; it is not synthetic success evidence. Complete private recordings stay under ignored `.cteam/recordings/`.

`watch` reads persisted Desktop rollout JSONL only. It reconstructs current state, then combines filesystem notifications with one-second length/prefix reconciliation. Its JSON output contains private paths and thread identifiers and belongs under ignored `.cteam/`. Use `--file <rollout>` when the thread id is unavailable; cwd-only selection is deliberately not implemented because the measured candidate set was ambiguous.

## Reproduce first-spike live experiments

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

On the first-spike host, elevated Windows helper setup failed. `--windows-sandbox unelevated` selected the documented fallback while retaining workspace-write restrictions. Those approval/escalation issues are spike-development concerns, not the intended production runtime model.

The production companion is expected to run as a normal per-user NativeAOT executable outside the Codex task sandbox, with no Windows Service, no administrator requirement for normal observation, and no Python/PowerShell runtime dependency. See `PRODUCTION_REQUIREMENTS.md`.

## Plugin

The repository root contains the minimal `c-team` plugin shell; its `inspect-codex-run` skill directs the local replay command. Manifest validation, local installation, discovery, invocation, and refresh are documented in [`docs/plugin-validation.md`](docs/plugin-validation.md). There is no production MCP server or UI yet.

The preferred eventual deployment is for the plugin to bundle and launch `cteam.exe` in place. The bounded PF1 attempt reached native linking, then stopped because this host lacks the Windows C++ linker workload. See the near-live findings for the immediate next packaging experiment.

## Current decision gate

Do not proceed into the production system automatically.

The near-live spike selected **D1 — Hybrid, persisted-first**. Persisted record-to-observer latency was Excellent, while periodic reconciliation was necessary because file modification timestamps remained frozen and one length change was caught by polling before a watcher notification.

SQLite history, production MCP, React/Apps SDK, analytics, steering, automatic routing, installer work, and production lifecycle management remain deferred pending separate authorization and design.
