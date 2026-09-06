# Experiment 010 — State database mission locator

## Purpose

Validate whether Codex's current versioned state SQLite can serve as a cheap optional `thread_id → rollout_path` locator after exact caller-to-mission identity is proven.

This is an optimization experiment, not a product blocker and not a replacement for rollout JSONL as execution evidence.

Run only after Experiment 006 has established an exact or bounded caller identity path and after the more product-defining Experiments 007–009 have answered process topology, project activation/context footprint, and onboarding/bootstrap.

## Hypothesis

Given an exact Codex thread id, the latest compatible `state_N.sqlite` can resolve the matching rollout path cheaply and read-only, allowing C-Team to avoid bounded filesystem scanning in the common case.

Expected path:

```text
caller thread_id
      ↓
latest state_N.sqlite
      ↓
threads.id
      ↓
threads.rollout_path
      ↓
validate path/session identity
```

## Requirements

Record the current state DB filename/schema shape at execution time. Do not hardcode one schema version as a permanent product contract.

Test at least:

- exact known root thread match;
- exact known child/subagent match if represented in the DB;
- missing thread;
- missing DB;
- incompatible/older schema fixture;
- stale/nonexistent rollout path fixture;
- database busy/locked behavior where safely reproducible;
- read-only access from the plugin MCP runtime without approval;
- bounded fallback to session index/filesystem when the DB path cannot be trusted.

## Success criteria

Classify the state DB locator as:

- **S1 — Exact fast-path**: exact known identities resolve correctly; failures degrade safely to fallback.
- **S2 — Useful metadata only**: DB helps narrow/enrich candidates but cannot safely act as exact locator.
- **S3 — Too brittle**: schema/access/staleness make the dependency not worth using.

Even for S1, rollout JSONL remains canonical execution evidence, the DB adapter remains optional/version-sensitive, returned paths must be validated, and production code must retain a non-SQLite fallback.

## Implementation constraints

- Reuse the compiled C# experiment harness.
- No Python/PowerShell implementation.
- Do not invoke external `sqlite3` in production-path code.
- If a library is evaluated, confirm .NET 10 NativeAOT viability before recommending it.
- Do not build the full production index/history layer.

## Evidence

Publish sanitized evidence under:

```text
docs/evidence/pf6-state-db-locator.json
experiments/010-state-db-locator/README.md
```

## Retest triggers

- new `state_N.sqlite` schema/version;
- `threads` table/columns materially change;
- rollout path storage changes;
- plugin sandbox read permissions change;
- Codex exposes a stable supported thread→rollout API that makes this optimization unnecessary.

## Stop condition

Stop after S1/S2/S3 classification and fallback behavior are evidenced. Do not extend this into production persistence or analytics.