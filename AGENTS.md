# C-Team Agent Guidance

## Mission

Build C-Team through evidence-driven spikes before committing to production architecture.

The first observability spike is described in `SPIKE.md`; the Desktop near-live follow-up is described in `NEAR_LIVE_SPIKE.md`; their findings live under `docs/`.

The experiment archive is tracked in `EXPERIMENTS.md`.

The **current mission** is Experiment 005 — Plugin MCP Runtime, described in `MCP_RUNTIME_SPIKE.md`.

Production runtime constraints are captured separately in `PRODUCTION_REQUIREMENTS.md`.

Do not expand a spike into the production product unless explicitly asked.

## Primary agent — Hannibal

The primary session is Hannibal.

Hannibal owns:

- understanding the mission;
- planning;
- architecture;
- cross-cutting decisions;
- resolving ambiguity;
- task decomposition;
- integrating delegated results;
- final acceptance.

For Experiment 005, use **Sol with High reasoning** as the primary session unless explicitly changed.

Keep the primary context from growing unnecessarily. Delegate bounded work when that reduces overall context cost, but do not create subagents merely for process compliance.

## Face — Explorer

Use `face` for bounded repository/protocol discovery.

Good tasks:

- locate relevant Codex protocol/plugin/MCP implementation details;
- inspect current repository structure;
- trace execution paths;
- identify existing patterns;
- inspect configuration and build output;
- collect concrete evidence needed for a decision.

Face should normally be read-only.

Return concise findings with file paths, symbols, protocol fields, and evidence.

Do not ask Face to make architecture decisions.

For Experiment 005, use Face only if a bounded current-Codex MCP/plugin investigation materially reduces primary-context growth. Do not regenerate telemetry that already exists.

## B.A. — Implementer

Use `ba` for normal implementation once the intended approach is clear.

Good tasks:

- implement protocol client/server pieces;
- implement replay/state aggregation;
- implement persisted Desktop observation pieces;
- implement the reusable C# experiment harness;
- add focused tests;
- wire the bounded stdio MCP experiment.

Give B.A. a bounded objective and sufficient context to work independently.

If implementation reveals a material architectural ambiguity, B.A. should report it instead of silently choosing a substantially different design.

## Murdock — Challenger

Use `murdock` only for complex or consequential analysis.

Trigger Murdock when architecture is being chosen, a decision has significant trade-offs, requirements are ambiguous, a result is surprising, Hannibal has low confidence, or a choice may be hard to reverse.

Murdock's job is not ordinary review. He should challenge hidden assumptions, reframe the problem, propose materially different approaches, identify risks/second-order effects, and push back on premature convergence.

After Murdock responds, Hannibal must explicitly decide which challenges to accept, reject, or defer.

For Experiment 005, Murdock should normally remain unused unless the MCP runtime exposes a genuinely consequential architectural surprise.

## Reviewer — Independent review

Use `reviewer` after consequential implementation.

Reviewer should prioritize real defects over style preferences and must not assume Hannibal or B.A. is correct.

For Experiment 005, do not invoke Reviewer merely for process compliance.

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

Do not encode these names/models as C-Team product assumptions. C-Team must dynamically observe the model catalog and actual model used by every agent where possible. See `MODELS.md` and CQ11 in `SPIKE.md`.

Do not opportunistically switch Face/B.A./etc. to GPT-5.5, Spark, Astra, or another available model during a controlled experiment. Model comparison experiments should be deliberate and repeatable.

If the effective model differs from the agent configuration, record that as evidence rather than silently normalizing it away.

## Preferred workflows

### Simple task

```text
Hannibal → B.A.
```

### Discovery-heavy task

```text
Face → Hannibal → B.A.
```

### Consequential implementation

```text
Face (if useful) → Hannibal → B.A. → Reviewer
```

### Complex architecture

```text
Face (if useful)
    ↓
Hannibal proposal
    ↓
Murdock challenge
    ↓
Hannibal response / decision
    ↓
B.A.
    ↓
Reviewer
```

### Current Experiment 005

```text
Hannibal / Sol High
    ↓
inspect current MCP/plugin behavior
    ↓
compiled C# stdio MCP probe
    ↓
minimal live plugin calls
    ↓
two-project/session lifecycle test
    ↓
PF2 + M1/M2/M3/M4 classification
```

Do not create work merely to observe work.

## Delegation principles

- Do not delegate tiny tasks when coordination overhead exceeds the work.
- Parallelize only genuinely independent work.
- Avoid having multiple agents edit the same files concurrently.
- Give subagents narrow, testable objectives.
- Prefer the cheapest model capable of completing the task reliably, after a routing policy has been deliberately chosen.
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
- Sanitized evidence belongs under `docs/evidence/`; deterministic fixtures belong under `tests/fixtures/`; experiment procedures/results belong under `experiments/<id>-<slug>/`.
- Every durable experiment needs an explicit retest trigger.

## Production runtime constraints

When production work begins, follow `PRODUCTION_REQUIREMENTS.md`.

In particular, the production local companion is expected to:

- be a .NET 10 NativeAOT per-user executable;
- avoid administrator privileges for normal observability;
- never require a Windows Service;
- have no Python, PowerShell, or shell-script runtime dependency;
- avoid recurring Codex sandbox escalation for normal read-only observation.

Experiment 005 specifically tests whether this executable should be the plugin's stdio MCP server, with Codex owning its process lifecycle.

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
- a second custom localhost HTTP/WebSocket API.

## Code style

- Prefer straightforward C# over clever abstractions.
- Keep types cohesive and small.
- Keep protocol DTOs separate from C-Team domain types.
- Represent model identifiers generically; do not create a Sol/Terra/Luna enum.
- Keep production code NativeAOT-friendly from the beginning once the real companion is started.
- Avoid dependency-injection ceremony unless it provides clear value.
- Prefer long readable lines over aggressive wrapping; target approximately 150 characters where practical.
- Do not use an `s_` prefix for static fields.
- Do not force constructors to appear before the public API simply because of a generic style convention; organize types for readability.

## Definition of done for the current mission

Experiment 005 is done only when:

1. the real MCP initialize handshake/capabilities are captured;
2. MCP Roots/project-context behavior is tested rather than assumed;
3. the bundled NativeAOT stdio MCP server initializes and exposes at least one harmless tool;
4. supported plugin-owned data behavior is tested if available;
5. one bounded persisted-state mission read is tested from the MCP process;
6. approval behavior is recorded for startup, repeated calls and any supported plugin-data write;
7. two simultaneous project/session contexts are tested for process count, identity and isolation;
8. the result is classified exactly PF2-A/B/C/D and M1/M2/M3/M4;
9. `EXPERIMENTS.md` is updated with sanitized evidence and retest triggers.

Stop at that decision gate rather than silently continuing into production development.