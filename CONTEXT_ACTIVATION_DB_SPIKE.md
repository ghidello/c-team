# Experiment 008B — Exact caller project resolution via Codex state DB

## Purpose

Close Experiment 008's only material blocker: a globally installed C-Team MCP can receive exact `thread_id` / `session_id` while Codex supplies neither MCP Roots nor caller workspace metadata.

Determine whether the installed Codex state database can map the exact caller thread directly to trustworthy project-location metadata (`cwd` and, where present, `project_id`) cheaply enough to drive `.cteam/` activation without heuristic project discovery.

This is an activation experiment, not the broader optional state-database optimization planned as Experiment 010.

## Why this experiment exists

Experiment 006 proved:

```text
MCP caller thread_id
        ↓ exact
persisted Codex thread identity
```

Experiment 008 proved:

- a globally installed C-Team plugin can expose one 292-byte stable tool;
- global C-Team skills can be zero;
- the MCP may start eagerly but remain dormant;
- project activation fails only when the host omits both workspace metadata and MCP Roots.

Current upstream Codex source also shows canonical persisted `ThreadMetadata` containing:

```text
id
rollout_path
cwd
project_id (optional)
```

and state APIs that filter/list by cwd/project metadata. That is promising evidence, but the installed Codex 0.153.4 database on the test machine is authoritative for this experiment.

## Hypothesis

Given `_meta.x-codex-turn-metadata.thread_id`, C-Team can perform an exact read-only lookup in the current compatible `state_N.sqlite`, obtain the caller thread's persisted `cwd` (and `project_id` when useful), derive an accepted project root deterministically, and check `<project-root>/.cteam`.

Preferred activation precedence if proven:

```text
1. exact caller workspace metadata, when supplied
2. exact caller thread row from compatible Codex state DB
3. Experiment 006 exact rollout/session_meta compatibility adapter
4. unresolved
```

Never use:

```text
MCP process cwd
latest/recent thread
same-cwd candidate guessing
arbitrary filesystem scanning
```

## Questions

### DB1 — installed schema

Inspect the actual installed Codex state database read-only.

Record:

- filename/version (`state_N.sqlite`);
- relevant `threads` schema/columns;
- whether exact thread id is unique/indexed;
- availability and meaning of `cwd`;
- availability and meaning of `project_id`;
- whether child/subagent rows carry their own cwd/project metadata;
- whether active threads are visible promptly enough for activation.

Do not assume current upstream source equals installed 0.153.4.

### DB2 — exact root caller lookup

For at least two real persisted root callers already available from Experiments 006–008:

```text
thread_id → one exact DB row → cwd/project metadata
```

Compare the returned cwd to the known actual repository/workspace root.

No recency or cwd filtering may participate in identity.

### DB3 — child caller lookup

For at least one naturally available child/subagent from Experiment 007, resolve the child thread id directly through the DB.

Determine whether:

- child cwd directly identifies the same project root as its parent; or
- child metadata requires a persisted parent/root relation before choosing the project root.

Do not silently replace child identity with a root merely because the root is newer.

### DB4 — inactive marker result

Use a real project without `.cteam/` whose caller thread can be resolved exactly.

Required result:

```json
{
  "status": "project_not_enabled"
}
```

The call may read the single exact state row (and minimal metadata needed for root normalization) but must not scan rollouts or unrelated threads in the successful DB fast path.

### DB5 — same-process marker transition

Within one live MCP process:

1. resolve caller project exactly through DB;
2. observe `.cteam/` absent → `project_not_enabled`;
3. create `.cteam/` using the experiment fixture/action;
4. call the same stable `cteam(status)` tool again;
5. prove same PID and no new `tools/list` are required;
6. observe `project_enabled`.

This tests backend activation only. A fresh Codex session may still be recommended when initialization creates AGENTS.md/skills/policy that should be present from session start.

### DB6 — failure and compatibility behavior

Test deterministic fixtures or safe local cases for:

- DB absent;
- incompatible schema;
- missing thread id;
- blank/missing cwd;
- stale/nonexistent cwd;
- ambiguous accepted project root;
- database busy/locked/read failure;
- DB row present but project marker inaccessible.

All failures must fall back safely to the Experiment 006 exact rollout adapter or unresolved. Never guess.

## Project-root normalization

Do not automatically assume `cwd` itself is always the desired project root.

For the experiment, record whether the observed cwd is:

- repository root;
- a nested working directory inside the repository;
- outside a Git repository;
- projectless.

A permitted deterministic normalization may walk **upward only from the exact caller cwd** to an accepted boundary such as the nearest `.git` root and/or `.cteam` marker. Bound the walk and document precedence.

Do not search neighboring directories or other projects.

## Implementation constraints

- Reuse the Experiment 008 stable one-tool activation server.
- Reuse Experiment 006 caller identity semantics and fallback adapter.
- Read Codex SQLite in read-only/non-owning mode only.
- Do not run Codex's own state backfill or mutate its DB.
- New reusable code remains C#/.NET 10 and NativeAOT-friendly.
- Do not build production history, analytics, shared core, broker, installer or onboarding packages.
- Keep raw ids/paths/database details under ignored `.cteam/experiment-008b/`; publish sanitized evidence only.

## Classification

Finish with exactly one primary result:

- **D1 — Exact DB activation**: exact caller thread lookup reliably returns project metadata and live `.cteam` activation works; rollout adapter is fallback only.
- **D2 — Exact DB locator, project normalization needed**: thread row is exact and useful but deterministic bounded root normalization/parent resolution is required.
- **D3 — Useful optimization only**: DB helps but cannot safely drive activation alone; exact rollout metadata remains primary.
- **D4 — Insufficient/brittle**: installed schema/access/freshness is not reliable enough for activation.

Also report separately:

```text
root caller DB lookup: exact | not established
child caller DB lookup: exact | parent-assisted | not established
inactive project: project_not_enabled | unresolved | other
same-process marker transition: yes | no | not established
successful-path rollout read: zero | nonzero
DB failure fallback: safe | unsafe | not established
```

## Decision rule

If D1 or D2 is proven, the production activation direction becomes:

```text
caller workspace (if present)
        ↓
exact Codex DB thread metadata
        ↓
bounded exact-rollout fallback
        ↓
.cteam activation
```

Then proceed to Experiment 009 onboarding.

If D3/D4, review whether rollout-derived cwd should be the primary activation source before onboarding work.

## Evidence

Publish sanitized evidence to:

```text
docs/evidence/pf4b-context-activation-db.json
experiments/008b-context-activation-db/README.md
```

## Retest triggers

- `state_N.sqlite` schema/version changes;
- thread `cwd` or `project_id` semantics change;
- thread id uniqueness/storage changes;
- active-thread DB freshness changes;
- Codex supplies reliable MCP Roots/workspaces or repository-scoped plugin activation;
- supported Codex thread→project APIs make private SQLite unnecessary.

## Stop condition

Stop after D1/D2/D3/D4 plus the orthogonal results are evidenced. Do not expand into Experiment 009 onboarding or the shared core.