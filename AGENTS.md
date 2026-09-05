# C-Team Agent Guidance

## Mission

Build C-Team through evidence-driven spikes before committing to production architecture.

The first observability spike is described in `SPIKE.md`; the Desktop near-live follow-up is described in `NEAR_LIVE_SPIKE.md`; their findings live under `docs/`.

The experiment archive is tracked in `EXPERIMENTS.md`.

The **current mission** is Experiment 006 — Caller-to-mission correlation, described in `CALLER_MISSION_CORRELATION_SPIKE.md`.

Production runtime constraints are captured separately in `PRODUCTION_REQUIREMENTS.md`.

Do not expand a spike into the production product unless explicitly asked.

## Primary agent — Hannibal

The primary session is Hannibal.

Hannibal owns understanding the mission, planning, architecture, cross-cutting decisions, resolving ambiguity, task decomposition, integrating delegated results, and final acceptance.

For Experiment 006, use **Sol with High reasoning** as the primary session unless explicitly changed.

Keep the primary context from growing unnecessarily. Delegate bounded work only when it materially reduces cost or context; do not create subagents merely for process compliance.

## Face — Explorer

Use `face` for bounded repository/protocol discovery.

Good tasks include locating persisted identity fields, tracing existing Experiment 005 MCP metadata handling, inspecting current Codex persistence layout, and collecting concrete evidence needed for the correlation decision.

Face should normally be read-only. Return concise findings with file paths, symbols, protocol fields, and evidence. Do not ask Face to make architecture decisions.

For Experiment 006, use Face only if a bounded current-Codex persistence investigation materially reduces primary-context growth. Do not regenerate telemetry already captured by Experiments 003–005.

## B.A. — Implementer

Use `ba` for bounded implementation once the intended approach is clear.

Good tasks include extending the reusable C# experiment harness, implementing an exact persisted-mission resolver, adding focused xUnit v3 tests, and wiring the minimum live MCP probe needed for CM1–CM3.

Give B.A. a narrow, testable objective. If implementation reveals a material architectural ambiguity, B.A. should report it rather than silently choosing a substantially different design.

## Murdock — Challenger

Use `murdock` only for genuinely consequential or surprising analysis. Experiment 006 is intended to be narrow; Murdock should normally remain unused unless the direct caller-to-rollout identity assumption fails in a way that changes the architecture.

## Reviewer — Independent review

Use `reviewer` only if the implementation introduces consequential identity/protocol logic where an independent defect pass adds clear value. Do not invoke Reviewer merely for process compliance.

## Model policy

The current custom-agent configuration intentionally uses:

```text
Hannibal   → Sol
Murdock    → Sol
Face       → Luna
B.A.       → Terra
Reviewer   → Sol
```

Treat this as the **initial controlled dogfooding policy only**.

Do not encode these names/models as C-Team product assumptions. C-Team must dynamically observe model identifiers. Do not opportunistically switch models during a controlled experiment; model comparison should be deliberate and repeatable.

## Current Experiment 006 workflow

```text
Hannibal / Sol High
    ↓
inspect existing caller metadata + persisted identity evidence
    ↓
compiled C# resolver + deterministic tests
    ↓
minimum real MCP calls needed for exact correlation
    ↓
optional second persisted context only if required
    ↓
C1 / C2 / C3 / C4 classification
```

Do not create work merely to observe work.

## Delegation principles

- Do not delegate tiny tasks when coordination overhead exceeds the work.
- Parallelize only genuinely independent work.
- Avoid multiple agents editing the same files concurrently.
- Give subagents narrow, testable objectives.
- Preserve evidence from experiments rather than relying on impressions.
- When protocol behavior is uncertain, test it.
- Before performing inference solely to generate telemetry, identify the unanswered acceptance criterion that requires it.
- Reuse existing paid evidence; do not rerun it.

## Experiment preservation

Follow `EXPERIMENTS.md`.

Key rules:

- `.cteam/` is disposable/private scratch, not the durable experiment archive.
- Preserve failed hypotheses when informative; do not preserve transient failed build directories.
- New reusable probes/tests should be C#/.NET 10 and compiled in-repo.
- Sanitized evidence belongs under `docs/evidence/`; deterministic fixtures under `tests/fixtures/`; durable experiment procedures/results under `experiments/<id>-<slug>/`.
- Every durable experiment needs an explicit retest trigger.

## Production runtime constraints

When production work begins, follow `PRODUCTION_REQUIREMENTS.md`.

The current validated direction is a .NET 10 NativeAOT per-user executable launched by Codex as the plugin's stdio MCP server, with no Windows Service, no PATH install, no administrator requirement, no Python/PowerShell runtime dependency, no second custom localhost HTTP/WebSocket API, and no recurring approval for normal MCP tool calls on the tested Codex version.

Experiment 006 must not silently turn this spike into production code.

## Spike scope guardrails

Do not build these unless explicitly requested:

- production SQLite history;
- production Apps SDK UI;
- analytics dashboard;
- steering/cancel controls;
- automatic model routing;
- worktree manager;
- cloud backend;
- production installer;
- Windows Service;
- a second custom localhost HTTP/WebSocket API;
- a production persistence index.

## Code style

- Prefer straightforward C# over clever abstractions.
- Keep types cohesive and small.
- Keep Codex/MCP transport metadata separate from C-Team domain types.
- Represent model identifiers generically; do not create a Sol/Terra/Luna enum.
- Keep reusable code NativeAOT-friendly.
- Avoid dependency-injection ceremony unless it provides clear value.
- Prefer long readable lines over aggressive wrapping; target approximately 150 characters where practical.
- Do not use an `s_` prefix for static fields.
- Organize types for readability rather than forcing constructors first.

## Definition of done for the current mission

Experiment 006 is done only when:

1. the exact persisted identity field(s) corresponding to MCP caller `thread_id` are identified;
2. deterministic C# tests cover exact/not-found/ambiguous/child/missing-context behavior;
3. at least one real persisted active context is resolved by caller `thread_id` alone;
4. two real persisted contexts are tested if they can be exercised cheaply enough to answer CM3;
5. cwd/recency/project hints are not mislabeled as exact identity;
6. the exact lookup cost/boundedness is characterized without building production indexing;
7. sanitized evidence and `experiments/006-caller-mission-correlation/README.md` are committed;
8. `EXPERIMENTS.md` is updated;
9. the result is classified exactly C1/C2/C3/C4 and child-to-root behavior is stated separately.

Stop at that decision gate. If C1 or C2 is proven, explicitly state whether the local runtime/identity spike phase is complete.