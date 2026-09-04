# C-Team Agent Guidance

## Mission

Build C-Team through evidence-driven spikes before committing to production architecture.

The first observability spike is described in `SPIKE.md` and its findings live under `docs/`.

The current follow-up mission is the quota-sensitive Desktop near-live observation spike in `NEAR_LIVE_SPIKE.md`.

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

For the near-live spike, use **Sol with High reasoning** as the primary session unless explicitly changed.

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

For the near-live spike, use Face only when a bounded investigation materially reduces primary-context growth or when one child-agent observation is required to answer NL5.

## B.A. — Implementer

Use `ba` for normal implementation once the intended approach is clear.

Good tasks:

- implement protocol client pieces;
- implement replay;
- implement state aggregation;
- implement the persisted Desktop tailer/watch path;
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

Murdock should:

- challenge hidden assumptions;
- reframe the problem;
- propose materially different approaches;
- identify risks and second-order effects;
- push back on premature convergence.

After Murdock responds, Hannibal must explicitly decide which challenges to accept, reject, or defer.

Do not invoke Murdock for trivial implementation choices.

For the quota-sensitive near-live spike, Murdock should normally remain unused unless a genuinely consequential architectural surprise appears.

## Reviewer — Independent review

Use `reviewer` after consequential implementation.

A review is normally required when a change:

- affects architecture or protocol semantics;
- spans several components;
- changes lifecycle/state logic;
- changes concurrency/process management;
- touches security/privacy boundaries;
- contains non-trivial algorithms;
- fixes a subtle bug;
- is large enough that an independent pass adds value.

Reviewer should prioritize real defects over style preferences.

Reviewer must not assume Hannibal or B.A. is correct.

For the near-live spike, do not invoke Reviewer merely for process compliance; the extra quota must be justified by consequential implementation risk.

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

### Quota-sensitive near-live spike

```text
Hannibal / Sol High
    ↓
local observation + deterministic tests
    ↓
Face only if needed
    ↓
B.A. only for bounded implementation when useful
```

Do not create work merely to observe work.

## Delegation principles

- Do not delegate tiny tasks when coordination overhead exceeds the work.
- Parallelize only genuinely independent work.
- Avoid having multiple agents edit the same files concurrently.
- Give subagents narrow, testable objectives.
- Prefer the cheapest model capable of completing the task reliably, **after** a routing policy has been deliberately chosen.
- Do not confuse routing policy with C-Team's underlying model representation.
- Preserve evidence from experiments rather than relying on impressions.
- When protocol behavior is uncertain, test it.
- Before performing inference solely to generate telemetry, identify the unanswered acceptance criterion that requires it.

## Production runtime constraints

When production work begins, follow `PRODUCTION_REQUIREMENTS.md`.

In particular, the production local companion is expected to:

- be a .NET 10 NativeAOT per-user executable;
- avoid administrator privileges for normal observability;
- never require a Windows Service;
- have no Python, PowerShell, or shell-script runtime dependency;
- avoid recurring Codex sandbox escalation for normal read-only observation;
- preferably be bundled and launched in place by the plugin if PF1 proves that deployment path viable.

Development and reproduction scripts may remain in the repository; they are not production runtime dependencies.

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

## Definition of done for the current spike

The near-live spike is not done merely because code compiles.

It must answer NL1–NL9 in `NEAR_LIVE_SPIKE.md` with measured evidence and end with exactly one D1, D2, or D3 recommendation.

If PF1 can be answered cheaply with a hello-world NativeAOT probe, record its result too; otherwise document PF1 as the immediate next bounded experiment rather than expanding scope.

Stop at the decision gate rather than silently continuing into production development.
