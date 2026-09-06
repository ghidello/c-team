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
| 008B | Exact caller project resolution via Codex state DB | Passed (D1) | Installed Codex 0.153.4 maps exact caller ids to unique cwd-bearing rows; a live inactive project transitioned to enabled on the same MCP process with zero rollout reads | state DB schema, caller identity, cwd/project semantics, or DB sharing changes |
| 009 | C-Team project bootstrap and onboarding | Partial (O4) | One canonical initializer produces two deterministic project files through direct native, local `dnx`, and offline NVM-managed npx; real installed-skill discovery and agent execution remain unmeasured | project schema, package runners, repository-scoped plugin activation, or stable plugin executable paths change |
| 009B | Real agent-first onboarding validation | Pending | Validate installed-skill discovery, approval, bundled initializer invocation, immediate MCP activation, repeat safety, and fresh-session guidance | execute current experiment; plugin skill discovery/loading changes |
| 010 | State database mission locator | Pending | Broader optional evaluation of `state_N.sqlite` as exact `thread_id → rollout_path` fast-path beyond activation | execute only if later profiling/compatibility work justifies it; state DB schema changes |

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

### Completed: Experiment 008B — exact caller project resolution via Codex state DB

Experiment 008B closed Experiment 008's remaining project-resolution gap. The installed Codex 0.153.4 `state_5.sqlite` stores `threads.id` as a unique primary key and a non-null cwd. Multiple existing real root callers and one natural child resolved exactly; the child's own cwd was sufficient without parent assistance.

Current upstream Codex source shows canonical thread metadata with `id`, `rollout_path`, `cwd`, and optional `project_id`, but **the installed Codex 0.153.4 state database is authoritative**.

The validated path is:

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

One bounded live inactive-project run returned `project_not_enabled`, then `project_enabled` after `.cteam/` appeared on the same MCP process. Both calls selected one exact DB row and read zero rollout files. Deterministic fixtures preserve Experiment 006's exact rollout adapter as the safe fallback for absent, incompatible, locked, missing-row, blank-cwd, and stale-cwd cases; unresolved outcomes never guess.

The result is **D1 — Exact DB activation**; see `experiments/008b-context-activation-db/`.

### Completed: Experiment 009 — project bootstrap and onboarding

Experiment 009 compared four entry points:

- agent/plugin skill initialization;
- `npx ... init`;
- a .NET one-shot equivalent such as a future `dnx` package/application;
- `cteam init` only if it can be exposed without PATH/installer friction.

The canonical initializer creates `.cteam/config.json` and creates or merges one managed C-Team section in root `AGENTS.md`. It does not create or modify repository marketplace metadata and never installs or enables the user-level plugin. Fresh, existing-file, repeated, partial, dry-run, schema-upgrade, malformed-state, rollback, and path-safety fixtures passed.

The skill-resolved payload, offline NVM-managed npx package, local `dnx` package, and direct NativeAOT command all produced the same golden files byte-for-byte. The npm carrier was 1,445,241 bytes and the zero-dependency .NET tool package was 26,886 bytes. Both package runners completed without permanent installation; npx required explicit resolution through `NVM_SYMLINK` because the Codex process PATH exposed only its bundled Node runtime.

The result is **O4 — More evidence needed**; see `experiments/009-onboarding-bootstrap/`. The sole material remaining gap is real installed-skill discovery and agent execution.

### Current: Experiment 009B — real agent-first onboarding validation

`AGENT_ONBOARDING_VALIDATION_SPIKE.md` validates the last onboarding surface rather than reopening the initializer/package design.

It must prove one real Codex flow:

```text
user asks to initialize C-Team
        ↓
installed onboarding skill is discovered/used
        ↓
explicit approval before mutation
        ↓
bundled canonical initializer
        ↓
.cteam/config.json + managed AGENTS.md block
        ↓
same MCP reports project_enabled immediately
        ↓
fresh session recommended only for newly written project guidance
```

Finish with O1/O2/O3/O4. If O1 is proven, close foundational onboarding validation and move to production/MVP architecture and implementation planning.

### Later optional: Experiment 010 — broader state database mission locator

`STATE_DB_LOCATOR_SPIKE.md` remains a broader optional optimization experiment for exact `thread_id → rollout_path` lookup, schema compatibility and fallback behavior outside the activation use case.

Experiment 008B already proves the DB path needed for activation. Do **not** run 010 automatically; revisit it only when profiling or compatibility evidence shows a concrete need.
