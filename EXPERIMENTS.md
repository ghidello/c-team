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
| 004 | Plugin-bundled NativeAOT companion | Partial | Standalone .NET 10 win-x64 NativeAOT EXE works when copied; plugin packaging/launch/approval behavior remains untested | execute now; repeat after plugin runtime/package changes |

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
    004-plugin-native-companion/
```

Build output is not experiment evidence and must not be committed.

## Current task

The immediate task is to archive experiments 001–003 from existing findings/evidence **without rerunning their Codex workloads**, establish the C# experiment harness, inspect `.cteam/` for unique evidence worth promoting, and execute the bounded remaining part of experiment 004.

See `EXPERIMENT_ARCHIVE_PLAN.md` and `EXPERIMENT_ARCHIVE_KICKOFF.md`.