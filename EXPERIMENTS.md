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
| 008 | Project activation and MCP context footprint | Partial (A4) | One 292-byte tool and zero global C-Team skills keep context small; MCP startup is eager but dormant, while missing CLI workspace/Roots data prevents reliable marker activation | repository-scoped activation, Roots/caller workspace metadata, tool refresh, skill injection, or Desktop/CLI discovery changes |
| 008B | Exact caller project resolution via Codex state DB | Pending | Test exact `thread_id → state DB thread row → cwd/project → .cteam` activation with rollout fallback only on DB failure | execute now; state DB schema/project metadata changes |
| 009 | C-Team project bootstrap and onboarding | Pending | Compare agent-first initialization with npx, .NET one-shot and bundled-runtime bootstrap paths while keeping one canonical project layout | execute after 008B; package/runtime onboarding mechanisms change |
| 010 | State database mission locator | Pending | Broader optional evaluation of `state_N.sqlite` as exact `thread_id → rollout_path` fast-path beyond activation | execute only if still useful after 008B; state DB schema changes |

## Required experiment folder contract

Each durable experiment should live under `experiments/<id>-<slug>/` and contain at minimum `README.md` with purpose, environment, hypothesis, procedure, success criteria, observed result, status, evidence, limitations and retest trigger.

When executable code is worth keeping, prefer the shared compiled C# harness under `experiments/CTeam.Experiments/` and deterministic tests under `tests/CTeam.Experiments.Tests/` rather than one-off scripts.

## Experiment order

### Completed: Experiment 006 — caller-to-mission correlation

Experiment 006 established caller `thread_id` as the stable exact key for persisted mission lookup on the tested Codex version. The dated rollout layout requires a bounded compatibility adapter. A post-restart no-argument Desktop call directly confirmed the final installed plugin and completed the local runtime/identity phase; see `experiments/006-caller-mission-correlation/`.

### Completed: Experiment 007 — plugin MCP process topology

Experiment 007 found one distinct plugin MCP process per tested root or native agent. Same-project and simultaneous different-context roots were isolated, and bounded normal/abrupt owner cleanup checks were clean. A facade plus demand-started shared core is deferred until shared-state cost becomes real; see `experiments/007-plugin-mcp-topology/`.

A shared C-Team core remains deferred. If ever introduced, zombie prevention and demand-started/idle-stopped lifecycle are hard requirements; see `docs/runtime-topology.md`.

### Completed: Experiment 008 — project activation and context footprint

Experiment 008 produced a one-tool, 292-byte NativeAOT facade with zero globally listed C-Team skills. The inactive MCP starts eagerly but performs no rollout work. The tested independent CLI repository supplied exact caller ids but neither a workspace map nor MCP Roots, so the live facade could not distinguish inactive from enabled without persisted state and was classified A4; see `experiments/008-context-activation/`.

The marker logic itself is deterministic: if an exact project root is known, `.cteam/` can appear between two calls on the same MCP process and no tool-catalog refresh is required.

### Current: Experiment 008B — exact caller project resolution via Codex state DB

`CONTEXT_ACTIVATION_DB_SPIKE.md` closes Experiment 008's remaining project-resolution gap.

Current upstream Codex source shows canonical thread metadata with `id`, `rollout_path`, `cwd`, and optional `project_id`, but **the installed Codex 0.153.4 state database is authoritative**.

Test this preferred path:

```text
MCP caller thread_id
        ↓ exact
compatible Codex state DB thread row
        ↓
cwd / project metadata
        ↓
bounded project-root normalization if needed
        ↓
.cteam absent → project_not_enabled
.cteam present → project_enabled
```

The successful DB fast path should read no rollout. Experiment 006's exact rollout adapter remains the fallback when DB access/schema/metadata is unavailable.

Finish with D1/D2/D3/D4 from `CONTEXT_ACTIVATION_DB_SPIKE.md`.

### Then: Experiment 009 — project bootstrap and onboarding

`ONBOARDING_BOOTSTRAP_SPIKE.md` compares four entry points:

- agent/plugin skill initialization;
- `npx ... init`;
- a .NET one-shot equivalent such as a future `dnx` package/application;
- `cteam init` only if it can be exposed without PATH/installer friction.

Do not run 009 until 008B establishes what creating `.cteam/` means end to end.

All paths must share one canonical deterministic initializer/project footprint. The expected product shape is agent-first UX with portable manual bootstrap commands, but the experiment must compare actual friction rather than assuming it.

Finish with O1/O2/O3/O4.

### Later optional: Experiment 010 — broader state database mission locator

`STATE_DB_LOCATOR_SPIKE.md` remains a broader optional optimization experiment for exact `thread_id → rollout_path` lookup, schema compatibility and fallback behavior outside the activation use case.

Experiment 008B may partially or completely subsume it. Re-evaluate 010 after 008B rather than running it automatically.
