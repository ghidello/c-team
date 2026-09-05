# C-Team experiments

C-Team keeps a small, reproducible compatibility laboratory for Codex behavior that may change over time.

The purpose is to preserve **what was tested, how it was tested, what failed or succeeded, and what future Codex change should trigger a retest** without keeping entire transient working directories.

## Rules

- `.cteam/` is private/disposable runtime scratch and raw evidence.
- `experiments/` is committed, sanitized, reproducible history.
- `docs/evidence/` contains sanitized evidence that supports published findings.
- `tests/fixtures/` contains deterministic fixtures required by compiled C# tests.
- Preserve failed hypotheses when the failure is informative; do not preserve failed build directories merely because they exist.
- Do not rerun an old paid Codex workload just to archive it. Reuse the evidence already produced.
- New reusable probes/tests should be written in C# and compiled as part of the repository unless an external shell command is intrinsically the subject of the experiment.
- Python/PowerShell may remain as historical reproduction artifacts when necessary, but they must not become production runtime dependencies and should not be the default for new experiments.

## Status vocabulary

- **Passed** — capability was demonstrated with evidence.
- **Blocked** — the approach is currently unavailable for a known reason.
- **Partial** — part of the hypothesis was demonstrated; material questions remain.
- **Pending** — experiment has not yet been executed.
- **Retest** — previous result may have been invalidated by a Codex/platform change.

## Experiment matrix

| ID | Experiment | Current status | Key result | Retest trigger |
| --- | --- | --- | --- | --- |
| 001 | Codex app-server observability | Passed | Structured telemetry, replay and model/quota discovery are viable when C-Team owns an app-server | app-server protocol/lifecycle materially changes |
| 002 | Direct attach to ChatGPT Desktop app-server | Blocked | Tested Windows Desktop instance used a private stdio app-server; no supported shared subscription endpoint was found | shared endpoint, Windows daemon, thread subscription, or Desktop integration appears |
| 003 | Persisted Desktop near-live observation | Passed | D1 persisted-first hybrid is viable; persisted records were observed in milliseconds, with watcher + reconciliation required | rollout/state format or Desktop persistence behavior changes |
| 004 | Plugin-bundled NativeAOT companion | Passed (PF1-C) | Installed payload, relative launch, current-user execution and versioned refresh work; `%LOCALAPPDATA%` durable state required approval on both tested commands | plugin trust, sandbox writable roots, approval persistence or package cache changes |
| 005 | Plugin-bundled NativeAOT stdio MCP runtime | Partial (PF2-B, M2) | Plugin-managed NativeAOT stdio MCP, structured tools, persisted reads and concurrent independent MCP children work without recurring approval; Roots/plugin data are absent and cross-project attribution still needs explicit context | Roots/plugin data support or MCP caller/workspace metadata changes |
| 006 | Caller-to-mission correlation | Pending | Test whether per-call `x-codex-turn-metadata.thread_id` maps exactly to persisted rollout/session identity, including multiple real persisted contexts | execute current experiment |

## Required experiment folder contract

Each durable experiment should live under:

```text
experiments/<id>-<slug>/
```

and contain at minimum `README.md` with:

```text
Purpose
Original environment
Hypothesis
Procedure
Success criteria
Observed result
Current status
Evidence references
Known limitations
Retest trigger
```

When executable code is worth keeping, prefer a shared compiled C# harness under `experiments/CTeam.Experiments/` and deterministic tests under `tests/CTeam.Experiments.Tests/` rather than one-off scripts per experiment.

## Build artifacts

Reusable experiment code should place generated build/publish output under a repository-level ignored artifacts location where practical, for example:

```text
artifacts/
  experiments/
    006-caller-mission-correlation/
```

Build output is not experiment evidence and must not be committed.

## Current decision gate

Experiment 005 established **PF2-B** with multi-project result **M2**: the plugin's bundled NativeAOT executable works as an approval-free stdio MCP backend, and every tested tool call carries an exact caller `thread_id`, but automatic project-to-persisted-rollout attribution was not yet proven.

The current mission is **Experiment 006**, described in `CALLER_MISSION_CORRELATION_SPIKE.md`. It must determine whether caller `thread_id` is itself the stable exact key for persisted mission lookup, with cwd/project hints relegated to fallback metadata.

Finish Experiment 006 with exactly one classification:

- **C1 — Exact**
- **C2 — Exact with bounded adapter**
- **C3 — Context-assisted**
- **C4 — Insufficient**

If C1 or C2 is proven, the local runtime/identity spike phase should be considered complete unless a new blocker appears. Production implementation remains a separate task.