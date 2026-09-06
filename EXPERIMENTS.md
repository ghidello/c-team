# C-Team experiments

C-Team keeps a small, reproducible compatibility laboratory for Codex behavior that may change over time.

The purpose is to preserve **what was tested, how it was tested, what failed or succeeded, and what future Codex change should trigger a retest** without keeping entire transient working directories.

## Rules

- `.cteam/` is private/disposable runtime scratch and raw evidence during development; when used as the future project activation marker, only its intended project configuration/policy contents are committed.
- `experiments/` is committed, sanitized, reproducible history.
- `docs/evidence/` contains sanitized evidence that supports published findings.
- `tests/fixtures/` contains deterministic fixtures required by compiled C# tests.
- Preserve failed hypotheses when the failure is informative; do not preserve failed build directories merely because they exist.
- Do not rerun an old paid Codex workload just to archive it. Reuse the evidence already produced.
- New reusable probes/tests should be written in C# and compiled as part of the repository unless an external shell/package command is intrinsically the subject of the experiment.
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
| 006 | Caller-to-mission correlation | Passed (C2) | Caller `thread_id` maps exactly through a bounded adapter; two Desktop roots remained distinct and a post-restart no-argument Desktop call resolved the active root exactly | Desktop plugin reload, caller metadata, or rollout identity/layout changes |
| 007 | Plugin MCP process topology | Passed (P3) | Native subagents and independent roots each used distinct MCP processes; same-project and cross-context roots were isolated, with clean normal and abrupt-owner cleanup | plugin/subagent process reuse or lifecycle changes |
| 008 | Project activation and MCP context footprint | Pending | Test a tiny stable globally installed MCP facade, `.cteam` activation, inactive-project dormancy and marker transition without MCP restart | execute after 007; repo-scoped activation/tool-refresh behavior changes |
| 009 | C-Team project bootstrap and onboarding | Pending | Compare agent-first initialization with npx, .NET one-shot and bundled-runtime bootstrap paths while keeping one canonical project layout | execute after 008; package/runtime onboarding mechanisms change |
| 010 | State database mission locator | Pending | Test whether latest compatible `state_N.sqlite` is a safe optional exact `thread_id → rollout_path` fast-path with bounded fallback | execute if still useful; state DB schema changes |

## Required experiment folder contract

Each durable experiment should live under `experiments/<id>-<slug>/` and contain at minimum `README.md` with purpose, environment, hypothesis, procedure, success criteria, observed result, status, evidence, limitations and retest trigger.

When executable code is worth keeping, prefer the shared compiled C# harness under `experiments/CTeam.Experiments/` and deterministic tests under `tests/CTeam.Experiments.Tests/` rather than one-off scripts.

## Experiment order

### Completed: Experiment 006 — caller-to-mission correlation

Experiment 006 established caller `thread_id` as the stable exact key for persisted mission lookup on the tested Codex version. The dated rollout layout requires a bounded compatibility adapter. A post-restart no-argument Desktop call directly confirmed the final installed plugin and completed the local runtime/identity phase; see `experiments/006-caller-mission-correlation/`.

### Completed: Experiment 007 — plugin MCP process topology

Experiment 007 found one distinct plugin MCP process per tested root or native agent. Same-project and simultaneous different-context roots were isolated, and bounded normal/abrupt owner cleanup checks were clean. A facade plus demand-started shared core is deferred until shared-state cost becomes real; see `experiments/007-plugin-mcp-topology/`.

A shared C-Team core remains deferred. If ever introduced, zombie prevention and demand-started/idle-stopped lifecycle are hard requirements; see `docs/runtime-topology.md`.

### Current: Experiment 008 — project activation and context footprint

`CONTEXT_ACTIVATION_SPIKE.md` answers a separate question: how a globally installed C-Team can stay almost invisible in projects that do not opt in.

It tests the preferred stable-facade idea:

```text
one tiny MCP tool surface
      ↓ first explicit call carries caller context
resolve project
      ↓
.cteam absent → project_not_enabled
.cteam present → C-Team active
```

It also tests whether creating `.cteam/` during the same MCP lifetime activates the backend without restarting the MCP, while recognizing that newly created project guidance/skills may still justify a fresh Codex session.

Finish with A1/A2/A3/A4.

### Then: Experiment 009 — project bootstrap and onboarding

`ONBOARDING_BOOTSTRAP_SPIKE.md` compares four entry points:

- agent/plugin skill initialization;
- `npx ... init`;
- a .NET one-shot equivalent such as a future `dnx` package/application;
- `cteam init` only if it can be exposed without PATH/installer friction.

All paths must share one canonical deterministic initializer/project footprint. The expected product shape is agent-first UX with portable manual bootstrap commands, but the experiment must compare actual friction rather than assuming it.

Finish with O1/O2/O3/O4.

### Later optional: Experiment 010 — state database mission locator

`STATE_DB_LOCATOR_SPIKE.md` tests whether current Codex `state_N.sqlite` can be an exact and cheap optional locator from caller thread id to rollout path.

This is an optimization, not a product blocker. Rollout JSONL remains execution evidence and a bounded fallback remains required.

Finish with S1/S2/S3. Do not run it ahead of 007–009 merely because it is prepared.
