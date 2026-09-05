# Experiment 006 — Caller-to-mission correlation

## Objective

Prove or falsify the exact join from Codex MCP caller metadata to the corresponding persisted Codex rollout without relying on cwd freshness heuristics.

Experiment 005 established that every MCP `tools/call` carries Codex-specific `_meta.x-codex-turn-metadata`, including exact `session_id`/`thread_id`, and that independent plugin MCP processes can run concurrently without approval. Experiment 003 established that persisted Desktop rollout state is near-live and contains stable thread/session identity. This experiment must test whether those two observations can be joined directly and safely.

The intended production rule, if proven, is:

```text
x-codex-turn-metadata.thread_id
            ↓
 persisted rollout/session identity
            ↓
 exact current C-Team mission
```

Project/workspace information should then become descriptive metadata, not the primary identity mechanism.

## Scope

Answer only the bounded questions below. Reuse existing implementation/evidence from Experiments 003 and 005. Do not re-run earlier observability experiments and do not expand into production UI, SQLite, background indexing, lifecycle or routing work.

### CM1 — Exact identity join

For a real persisted Codex context, does `_meta.x-codex-turn-metadata.thread_id` exactly match one and only one persisted rollout/session identity?

Record:

- the caller thread/session id in sanitized/hash form;
- the persisted identity source field(s) used for the join;
- number of candidate rollout files examined;
- exact/ambiguous/not-found outcome;
- whether project/cwd was required.

### CM2 — Active Desktop context

Can the installed plugin MCP tool resolve the currently invoking persisted Desktop mission by caller `thread_id` alone while that mission is active?

Success means no explicit `mission_id`, project hint, cwd selection or latest-file heuristic is required.

### CM3 — Two persisted contexts

Test two real persisted Codex contexts with distinct thread ids and, if practical, distinct project/workspace roots. They may be two already-existing Desktop conversations; do not create synthetic multi-agent fan-out merely for telemetry.

For each MCP call, prove that:

- caller thread id A resolves mission A only;
- caller thread id B resolves mission B only;
- results do not cross;
- process count/lifecycle behavior remains safe even if the two calls are served by separate MCP children.

If two simultaneously active Desktop contexts cannot be exercised cheaply, use two independently persisted real contexts and state the limitation. Do not manufacture expensive workloads solely to satisfy simultaneity.

### CM4 — Child/review safety

Determine how direct correlation behaves when the invoking context is a child/review/subagent thread rather than a root mission.

The adapter must distinguish:

- exact root mission;
- exact child/review rollout;
- parent/root mission derived from persisted parent metadata, if available;
- ambiguous/unresolved.

Do not silently coerce every caller to the freshest root.

### CM5 — Missing metadata fallback

Define and test deterministic behavior when `_meta.x-codex-turn-metadata` or `thread_id` is absent.

Preferred order:

1. exact caller thread id when supplied;
2. explicit tool argument such as `mission_id` when supplied;
3. optional explicit project hint only as a user/host hint;
4. otherwise report ambiguity/not-found rather than guessing.

Cwd and recency may help produce suggestions, but must never be represented as exact identity.

### CM6 — Cost and bounded lookup

The exact-id path must not require scanning the entire Codex history on every tool call.

Measure enough to choose an implementation direction:

- files/directories examined for an exact lookup;
- bytes read where practical;
- whether a filename/path convention, session index/state DB, or small in-memory map can bound lookup;
- cold vs warm lookup if trivial to measure.

Do not build production indexing in this spike. If exact lookup currently needs a bounded recent scan, document that honestly and identify the smallest future adapter/index improvement.

## Implementation guidance

Use the existing C# experiment harness and xUnit v3 tests. New reusable logic must be C#/.NET 10 and NativeAOT-friendly.

Prefer a small resolver abstraction, for example conceptually:

```text
CallerContext
  ThreadId
  SessionId
  WorkspaceMetadata?

PersistedMissionResolver
  ResolveExactCaller(...)
```

Do not encode Codex-specific `_meta` fields directly into domain mission types; keep transport metadata separate from normalized C-Team state.

Deterministic tests should cover at least:

- exact id → one rollout;
- exact id → no rollout;
- duplicate/ambiguous identity evidence;
- child → parent/root derivation where metadata exists;
- missing caller metadata;
- project-hint fallback remains non-exact;
- bounded lookup/scan limits;
- open writer + partial trailing JSON line behavior inherited from Experiment 005.

## Evidence and privacy

Raw caller ids, rollout ids, paths, prompts, commands and account data stay under ignored `.cteam/experiment-006/`.

Commit only sanitized facts under `docs/evidence/` and the durable experiment report under `experiments/006-caller-mission-correlation/README.md`.

Do not commit raw Desktop rollouts.

## Classification

Finish with exactly one caller-correlation classification:

- **C1 — Exact**: caller `thread_id` alone reliably resolves the exact persisted rollout, including the tested multi-context case.
- **C2 — Exact with bounded adapter**: direct identity is reliable, but current Codex persistence layout requires a small bounded resolver/index step.
- **C3 — Context-assisted**: caller identity is useful but cannot uniquely resolve persisted state without workspace/project or explicit mission context.
- **C4 — Insufficient**: caller metadata cannot safely identify the persisted mission on the tested Codex version.

Also state separately whether child/review callers can be mapped to their root mission deterministically.

## Decision gate

Stop after CM1–CM6 and C1/C2/C3/C4 are answered.

If C1 or C2 is proven, consider the local runtime/identity spike phase complete unless a new blocker appears. Do not continue into production C-Team implementation without a separate task.