# C-Team Agent Guidance

## Mission

Build the C-Team observability spike described in `PROJECT.md` and `SPIKE.md`.

The spike exists to answer architectural questions. Do not expand scope into the production product unless explicitly asked.

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

Prefer delegating bounded work rather than consuming the primary context on mechanical exploration or implementation.

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

## B.A. — Implementer

Use `ba` for normal implementation once the intended approach is clear.

Good tasks:

- implement protocol client pieces;
- implement replay;
- implement state aggregation;
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

## Delegation principles

- Do not delegate tiny tasks when coordination overhead exceeds the work.
- Parallelize only genuinely independent work.
- Avoid having multiple agents edit the same files concurrently.
- Give subagents narrow, testable objectives.
- Prefer the cheapest model capable of completing the task reliably.
- Preserve evidence from experiments rather than relying on impressions.
- When protocol behavior is uncertain, test it.

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
- cloud backend.

## Code style

- Prefer straightforward C# over clever abstractions.
- Keep types cohesive and small.
- Keep protocol DTOs separate from C-Team domain types.
- Avoid dependency-injection ceremony unless it provides clear value.
- Prefer long readable lines over aggressive wrapping; target approximately 150 characters where practical.
- Do not use an `s_` prefix for static fields.
- Do not force constructors to appear before the public API simply because of a generic style convention; organize types for readability.

## Definition of done for the spike

The work is not done merely because code compiles.

The spike must answer the critical questions in `SPIKE.md` with recorded evidence and finish with a decision recommendation rather than silently continuing into production development.
