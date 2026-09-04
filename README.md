# C-Team observability spikes

C-Team is an observability companion for Codex multi-agent execution.

The first .NET 10 console spike validated app-server telemetry, persisted Desktop observation, recordings, replay, model catalog discovery, and a minimal Codex plugin shell. Its findings are documented under `docs/`.

The next bounded experiment is the **Desktop near-live observation spike** in [`NEAR_LIVE_SPIKE.md`](NEAR_LIVE_SPIKE.md). Use [`NEAR_LIVE_KICKOFF.md`](NEAR_LIVE_KICKOFF.md) as the kickoff prompt.

Production runtime constraints are recorded in [`PRODUCTION_REQUIREMENTS.md`](PRODUCTION_REQUIREMENTS.md).

## First-spike evidence

Read:

- [`docs/spike-findings.md`](docs/spike-findings.md) — CQ1–CQ11 findings and decision gate;
- [`docs/codex-protocol.md`](docs/codex-protocol.md) — protocol evidence;
- [`docs/desktop-observation.md`](docs/desktop-observation.md) — Desktop persisted-observation experiment.

The first spike was tested against installed Codex 0.153.1 on Windows.

## Build, test, replay

```powershell
dotnet build CTeam.Spike.sln
dotnet test CTeam.Spike.sln
dotnet run --project src/CTeam.Spike -- replay docs/evidence/example-run.jsonl
```

`docs/evidence/example-run.jsonl` is an allowlisted, lossy derivative of a real run. Its provenance file describes removed fields; it is not synthetic success evidence. Complete private recordings stay under ignored `.cteam/recordings/`.

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

The preferred eventual deployment is for the plugin to bundle and launch `cteam.exe` in place. PF1 in `PRODUCTION_REQUIREMENTS.md` exists to validate that narrowly with a tiny NativeAOT executable before we commit to the packaging mechanism.

## Current decision gate

Do not proceed into the production system automatically.

Run the near-live spike first and decide whether Desktop persisted observation should be:

- **D1 — Hybrid, persisted-first**;
- **D2 — Hybrid, after-action-first**;
- **D3 — Owned-runtime-first**.

SQLite history, production MCP, React/Apps SDK, analytics, steering, automatic routing, installer work, and production lifecycle management remain deferred until that decision is evidence-backed.
