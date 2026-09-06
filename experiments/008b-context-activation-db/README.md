# Experiment 008B — Exact caller project resolution via Codex state DB

## Purpose

Close Experiment 008's project-resolution gap by testing whether Codex MCP caller `thread_id` can select one exact row in the installed Codex state database and obtain trustworthy project-location metadata for `.cteam` activation.

This experiment is limited to activation. It does not build the broader state-database locator proposed as Experiment 010 or any production persistence, onboarding, UI, shared core, installer, or service.

## Original environment

Executed 2026-09-06 on Windows 10.0.26220 with Codex CLI 0.153.4 and .NET SDK 10.0.400. The bounded live transition used local plugin `c-team@personal` version `0.1.0+codex.20260906205829`, model `gpt-5.6-sol`, and high reasoning. The final NativeAOT payload was republished, hash-matched into installed plugin version `0.1.0+codex.20260906210929`, and exercised directly against the installed database.

Experiments 006–008 were authoritative inputs. Their paid caller-correlation, topology, and context-footprint workloads were not repeated.

## Hypothesis

Given `_meta.x-codex-turn-metadata.thread_id`, a read-only exact query against a compatible installed `state_N.sqlite` can return the caller's persisted `cwd` without consulting recency, MCP process cwd, a latest-thread heuristic, or rollout contents. C-Team can then normalize upward only from that exact cwd to an accepted project boundary and check `<project-root>/.cteam`.

The tested activation precedence is:

```text
exact caller workspace metadata, when supplied
        ↓
exact caller row in compatible Codex state DB
        ↓
Experiment 006 exact rollout adapter
        ↓
unresolved
```

## Procedure

1. Inspect the installed `state_5.sqlite` through the repository's NativeAOT executable using Windows SQLite in read-only mode with `query_only` enabled. Do not run Codex backfill or modify the database.
2. Verify `threads.id`, `cwd`, optional `project_id`, `project_roots`, and `thread_spawn_edges` from the installed schema rather than upstream source.
3. Query existing real root and child ids from Experiments 006–008 with `WHERE threads.id = ? LIMIT 2`. Store raw ids and paths only under ignored `.cteam/experiment-008b/`.
4. Extend Experiment 008's one-tool server so its successful DB path reports lookup outcome, row count, timing, normalization boundary, and rollout read count.
5. Run one bounded real inactive-project transition: call `cteam(status)`, create `.cteam/`, and call the same tool again in the same Codex/MCP lifetime.
6. Exercise deterministic C# fixtures for absent, incompatible, missing-row, missing-id, blank-cwd, stale-cwd, ambiguous-root, inaccessible-marker, and locked-database cases. Verify exact rollout fallback or unresolved behavior without guessing.
7. Publish the .NET 10 harness as a `win-x64` NativeAOT plugin payload, validate its layout, install it, and verify the published and installed executable hashes match.

## Installed schema result

The authoritative installed database was `state_5.sqlite`, with `PRAGMA user_version = 0` and WAL journal mode. `threads.id` is the table primary key and has SQLite's unique primary-key autoindex. `threads.cwd` is `TEXT NOT NULL`; `threads.project_id` is nullable and references `projects`. `project_roots` maps a project id and ordered position to paths. `thread_spawn_edges.child_thread_id` is a primary key and maps to `parent_thread_id`.

Every real row used by this experiment had a null `project_id`. Exact `cwd` therefore supplied the useful location signal. On Windows, persisted cwd values carried the `\\?\` device prefix; the adapter removes that prefix as path canonicalization before filesystem checks.

The state DB was current enough for activation: the new inactive-project caller row was available by its first live C-Team tool call.

## Exact root and child lookups

At least two distinct existing real root ids from Experiment 006 resolved to exactly one `threads` row each. Their returned cwd matched the known repository root. Separate existing projectless and inactive-project roots also resolved exactly. Identity selection used only the caller id primary-key lookup; cwd and project information were outputs, not filters.

One naturally available Experiment 006 child id resolved directly to one row. Its own cwd matched the same project root as its parent, and `thread_spawn_edges` also exposed the exact parent relation. Parent assistance was therefore available but unnecessary for the observed child.

Managed direct lookups across five existing contexts took 7.731–8.627 ms. The live server's two warm lookups took 1.691 ms and 0.947 ms; the final installed NativeAOT payload completed a direct exact lookup in 1.958 ms. These are machine-specific observations, not service-level targets.

## Inactive project and marker transition

The single bounded live run used a real independent Git project with no `.cteam/` marker. Codex 0.153.4 still declared no MCP Roots and supplied no caller workspace map, so the DB path answered the exact gap from Experiment 008.

The first `cteam(status)` returned `project_not_enabled`. After `.cteam/` was created at the repository root, the second call returned `project_enabled`. Both calls reported:

- resolution source `codex-state-db`;
- one exact database row;
- zero rollout files read;
- `git-root` at normalization level zero;
- the same PID and process-start timestamp.

The client called `tools/list` exactly once, before either status call. The marker transition required neither an MCP restart nor a catalog refresh. The active process initially used 11,952,128 bytes of working set; this is an observed point measurement rather than a steady-state memory claim.

The decisive live cwd was already the repository root. No upward traversal was required, so the result is D1 rather than D2. Removing the Windows device prefix is path canonicalization. The reusable fallback normalizer nevertheless checks only the exact cwd and its parents, preferring the nearest `.git` boundary over `.cteam` at each level, and stops after 32 levels. It never searches siblings or unrelated projects. A projectless cwd is accepted only after the walk reaches the filesystem root without finding another boundary.

## Failure and compatibility behavior

Deterministic fixtures establish the following behavior:

| Case | Result |
| --- | --- |
| DB absent | exact rollout fallback when present; otherwise unresolved |
| incompatible schema or missing primary-key guarantee | exact rollout fallback when present; otherwise unresolved |
| caller id absent or exact row missing | unresolved unless the exact rollout adapter can prove the caller |
| blank or stale cwd | exact rollout fallback when present; otherwise unresolved |
| ambiguous project roots | unresolved; no root is guessed |
| locked/read-failed DB | exact rollout fallback when present; otherwise unresolved |
| inaccessible marker | unresolved |
| child cwd stale with exact parent edge | exact parent-row assistance, with both DB reads reported |
| upward boundary exceeds 32 levels | unresolved with `normalization-limit` |

The database is opened read-only and `PRAGMA query_only` is enabled. A deterministic mutation attempt fails, and the row remains present. The successful live path read one exact DB row and zero rollout files.

## Current status

**D1 — Exact DB activation.** On installed Codex 0.153.4, caller `thread_id` selected one unique persisted row, returned an exact root cwd for real root and child contexts, and drove a same-process inactive-to-enabled marker transition with zero rollout reads. Experiment 006's bounded exact-rollout adapter remains a safe compatibility fallback.

```text
root caller DB lookup: exact
child caller DB lookup: exact
inactive project: project_not_enabled
same-process marker transition: yes
successful-path rollout read: zero
DB failure fallback: safe
```

Experiment 009 onboarding is unblocked. It was not started in this mission.

## Evidence references

- [`docs/evidence/pf4b-context-activation-db.json`](../../docs/evidence/pf4b-context-activation-db.json)
- [`experiments/CTeam.Experiments`](../CTeam.Experiments)
- [`tests/CTeam.Experiments.Tests`](../../tests/CTeam.Experiments.Tests)
- [`experiments/006-caller-mission-correlation`](../006-caller-mission-correlation)
- [`experiments/008-context-activation`](../008-context-activation)

## Known limitations

This is a private installed-state schema, so compatibility checks and the exact rollout fallback remain necessary. Tested real rows had null `project_id`; live `project_roots` behavior was not needed or claimed. The real child was read-only historical evidence, while stale-child parent assistance was exercised deterministically.

The semantic live transition used plugin `0.1.0+codex.20260906205829`. The final package adds bounded-normalization and database-discovery failure guards plus their tests; it was republished, validated, hash-matched into the installed plugin, and used for a direct NativeAOT DB lookup without spending another inference run.

Project guidance remains a session-context concern: backend activation changes immediately, while newly created project instructions or skills may require a fresh Codex session so they are present from the start.

## Retest trigger

Retest when `state_N.sqlite` schema/version, `threads.id` uniqueness, cwd/project semantics, active-row freshness, child-edge storage, or database sharing behavior changes; when Codex supplies reliable MCP Roots/workspaces or repository-scoped plugin activation; or when a supported Codex thread-to-project API replaces private SQLite access.
