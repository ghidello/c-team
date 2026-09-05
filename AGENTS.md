# C-Team Agent Guidance

## Mission

Build C-Team through evidence-driven spikes before committing to production architecture.

The first observability spike is described in `SPIKE.md`; the Desktop near-live follow-up is described in `NEAR_LIVE_SPIKE.md`; their findings live under `docs/`.

The **current mission** is the experiment archive + PF1 task described in `EXPERIMENT_ARCHIVE_PLAN.md` and `EXPERIMENTS.md`.

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

For the current archive/PF1 task, use **Sol with High reasoning** as the primary session unless explicitly changed.

Keep the primary context from growing unnecessarily. Delegate bounded work when that reduces overall context cost, but do not create subagents merely for process compliance.

## Face — Explorer

Use `face` for bounded repository/protocol discovery.

Good tasks:

- locate relevant Codex protocol schemas and event types;
- inspect current repository structure;
- trace execution paths;
- identify existing patterns;
- inspect configuration and build output;
- collect concrete evidence needed for a decision.

Face should normally be read-only.

Return concise findings with file paths, symbols, protocol fields, and evidence.

Do not ask Face to make architecture decisions.

For the current archive/PF1 task, use Face only if a bounded inventory or plugin-layout investigation materially reduces primary-context growth. Do not use Face to regenerate telemetry that already exists.

## B.A. — Implementer

Use `ba` for normal implementation once the intended approach is clear.

Good tasks:

- implement protocol client pieces;
- implement replay/state aggregation;
- implement persisted Desktop observation pieces;
- implement the reusable C# experiment harness;
- add focused tests;
- refactor code following an approved plan;
- wire small integrations.

Give B.A. a bounded objective and sufficient context to work independently.

If implementation reveals a material architectural ambiguity, B.A. should report it instead of silently choosing a substantially different design.

## Murdock — Challenger

Use `murdock` only for complex or consequential analysis.

Trigger Murdock when:

- architecture is being chosen;
- a decision has significant trade-offs;
- requirements are ambiguous;
- a result is surprising;
- the primary approach may be over-constrained by assumptions;
- Hannibal has low confidence;
- a choice may be hard to reverse.

Murdock's job is not ordinary review.

Murdock should challenge hidden assumptions, reframe the problem, propose materially different approaches, identify risks/second-order effects, and push back on premature convergence.

After Murdock responds, Hannibal must explicitly decide which challenges to accept, reject, or defer.

Do not invoke Murdock for trivial implementation choices. For the current archive/PF1 task, Murdock should normally remain unused unless plugin-native-companion behavior exposes a genuinely consequential architectural surprise.

## Reviewer — Independent review

Use `reviewer` after consequential implementation.

A review is normally required when a change affects architecture/protocol semantics, spans several components, changes lifecycle/concurrency/security behavior, contains non-trivial algorithms, fixes a subtle bug, or is large enough that an independent pass adds value.

Reviewer should prioritize real defects over style preferences and must not assume Hannibal or B.A. is correct.

For the current archive/PF1 task, do not invoke Reviewer merely for process compliance.

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

Do not opportunistically switch Face/B.A./etc. to GPT-5.5, Spark, Astra, or another available model during a controlled baseline experiment. Model comparison experiments should be deliberate and repeatable.

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

### Current experiment archive + PF1 task

```text
Hannibal / Sol High
    ↓
archive existing evidence without reruns
    ↓
C# experiment harness + deterministic tests
    ↓
small real PF1 plugin launch test
    ↓
PF1 A/B/C/D classification
```

Do not create work merely to observe work.

## Delegation principles

- Do not delegate tiny tasks when coordination overhead exceeds the work.
- Parallelize only genuinely independent work.
- Avoid having multiple agents edit the same files concurrently.
- Give subagents narrow, testable objectives.
- Prefer the cheapest model capable of completing the task reliably, after a routing policy has been deliberately chosen.
- Do not confuse routing policy with C-Team's underlying model representation.
- Preserve evidence from experiments rather than relying on impressions.
- When protocol behavior is uncertain, test it.
- Before performing inference solely to generate telemetry, identify the unanswered acceptance criterion that requires it.
- For archival work, **reuse existing paid evidence; do not rerun it**.

## Experiment preservation

Follow `EXPERIMENTS.md` and `EXPERIMENT_ARCHIVE_PLAN.md`.

Key rules:

- `.cteam/` is disposable/private scratch, not the durable experiment archive.
- Preserve failed hypotheses when informative; do not preserve transient failed build directories.
- New reusable probes/tests should be C#/.NET 10 and compiled in-repo.
- Sanitized evidence belongs under `docs/evidence/`; deterministic fixtures belong under `tests/fixtures/`; experiment procedures/results belong under `experiments/<id>-<slug>/`.
- Do not delete the user's local `.cteam/` scratch during the archive task; produce a keep/discard audit.
- Every durable experiment needs an explicit retest trigger.

## Production runtime constraints

When production work begins, follow `PRODUCTION_REQUIREMENTS.md`.

In particular, the production local companion is expected to:

- be a .NET 10 NativeAOT per-user executable;
- avoid administrator privileges for normal observability;
- never require a Windows Service;
- have no Python, PowerShell, or shell-script runtime dependency;
- avoid recurring Codex sandbox escalation for normal read-only observation;
- preferably be bundled and launched in place by the plugin if PF1 proves that deployment path viable.

Development/historical reproduction scripts may remain in the repository when justified; they are not production runtime dependencies and are not the default for new experiments.

## Spike scope guardrails

Do not build these unless explicitly requested:

- SQLite history;
- production MCP server;
- React UI;
- Apps SDK widget;
- analytics dashboard;
- steering/cancel controls;
- automatic model routing;
- worktree manager;
- cloud backend;
- production installer;
- Windows Service.

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

The archive/PF1 task is done only when:

1. experiments 001–003 are durably documented from existing evidence without expensive reruns;
2. `.cteam/` has a written keep/discard audit without deleting the user's scratch data;
3. reusable experiment probes/tests are C# and compile in-repo;
4. PF1 is tested with one bounded real plugin invocation and classified exactly A/B/C/D;
5. `EXPERIMENTS.md` reflects the final status and retest triggers.

Stop at that decision gate rather than silently continuing into production development.