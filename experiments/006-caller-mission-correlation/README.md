# Experiment 006 — Caller-to-mission correlation

## Purpose

Determine whether Codex MCP caller metadata identifies one persisted Codex rollout exactly, without using cwd, project, workspace, recency, or latest-file selection as identity.

## Original environment

Executed 2026-09-06 on Windows 10.0.26220 with .NET SDK 10.0.400, Codex CLI 0.153.4, and local plugin `c-team@personal`. The live call used `0.1.0+codex.20260906190026`; the final strictly bounded payload was hash-verified as `0.1.0+codex.20260906191756` without another model invocation. Experiments 003 and 005 were treated as established inputs; their paid workloads were not repeated.

## Hypothesis

The Codex-specific MCP field `_meta.x-codex-turn-metadata.thread_id` equals `session_meta.payload.id` in the caller's persisted rollout. A child caller should match its own rollout first and derive the root from persisted `session_id` or `parent_thread_id`.

## Procedure

1. Inspect the existing Experiment 005 MCP envelopes and current persisted root/child `session_meta` shapes.
2. Add a transport-only `CallerContext`, a bounded exact resolver, and deterministic xUnit v3 coverage to the shared .NET 10 experiment harness.
3. Publish and install the updated `win-x64` NativeAOT MCP companion, verifying that the published and installed hashes match.
4. Resolve two existing persisted Desktop roots by their real thread ids through the installed binary's stdio MCP path, storing the raw requests and responses only under ignored `.cteam/experiment-006/`.
5. Exercise one fresh bounded Codex call to prove that actual per-call metadata reaches the updated server and resolves the actively written rollout.
6. Resolve two naturally created child rollouts from this mission and verify their root derivation. No agent was created solely for telemetry.

## Success criteria

- Caller `thread_id` matches exactly one persisted `session_meta.payload.id` for real contexts.
- Two distinct persisted roots produce distinct exact mission keys with no cross-match.
- The live MCP path needs no explicit mission id, project hint, cwd, or recency choice.
- Child identity remains distinct and maps to its root only through persisted parent/session metadata.
- Missing metadata and hint fallbacks are deterministic and never mislabeled exact.
- Lookup work is bounded and observable without a production history index.

## Observed result

### CM1 — Exact identity join

Experiment 005's real MCP envelopes established that Codex sends `thread_id` and `session_id` on every tested `tools/call`, and that both equal that caller's `thread.started` id. Current persistence inspection found the same value in root `session_meta.payload.id` and `session_meta.payload.session_id`.

The updated resolver used the rollout filename suffix only to locate candidates, then parsed and verified `session_meta.payload.id` before returning an exact result. The active primary Desktop root resolved to one candidate after examining one candidate file, 32 bounded directory locations, and 22,176 bytes. It required no project, workspace, cwd, or recency hint.

### CM2 — Active context

A fresh bounded Codex client loaded server `0.1.0-experiment-006`. Its real MCP call contained both caller ids, they matched one another and the client's `thread.started` id, and the no-argument mission probe resolved its actively written rollout exactly. It examined one candidate file and 18,639 bytes across the same 32 bounded directory locations, with no truncation or process error.

The already-running ChatGPT Desktop host retained the pre-006 plugin cache path after plugin reinstall, including for a newly created Desktop task. Its old result schema therefore could not exercise the new resolver. Resuming that Desktop-owned context through a fresh CLI client was rejected because Desktop still held the rollout writer. These are plugin refresh and writer-ownership observations; neither contradicted the identity join. The updated installed binary separately resolved the active primary Desktop context and a second real persisted Desktop context exactly from their task ids.

### CM3 — Two persisted contexts

Two distinct real persisted Desktop root contexts were passed independently through the installed stdio MCP binary. Each produced one exact candidate, one examined file, 32 bounded directory locations, and no truncation. Their sanitized mission keys were distinct, so neither result crossed to the other context. The second lookup read 22,119 bytes.

The two Desktop calls were made sequentially because the running Desktop host had not refreshed the plugin payload. Experiment 005 remains the authoritative simultaneous-process evidence: two concurrent Codex clients used distinct MCP children safely. The fresh Experiment 006 live call added a third distinct persisted root and one actual updated-host metadata path; it did not repeat the earlier concurrency workload.

### CM4 — Child and review safety

Root rollouts use `payload.id == payload.session_id` with no parent. Child rollouts use their own `payload.id`, while `payload.session_id` and `payload.parent_thread_id` point to the root. The resolver first matches the child's exact `payload.id`, returns a distinct child mission key, and derives the root key from those persisted fields.

Three natural children existed from this mission. Two were tested through the installed binary; both resolved exactly to one child file and both derived the active primary root. They read 22,638 and 22,634 bytes respectively, with no truncation. No separate review rollout was naturally available, so review behavior is defined by the same persisted child fields and covered deterministically rather than claimed as a separate live observation.

**Child-to-root behavior:** deterministic for the observed child/subagent rollout shape. An exact child stays identifiable as a child; it is never silently replaced by the freshest root.

### CM5 — Missing metadata fallback

The tested order is caller `thread_id`, explicit `mission_id`, explicit project hint, then unresolved. A supplied caller id that has no persisted exact match returns `not-found` and does not fall through to a heuristic. An explicit project hint returns `context-assisted` and never `certain`; absent metadata and arguments return `unresolved`/`ambiguous`.

### CM6 — Cost and bounded lookup

The compatibility adapter checks the sessions root plus 31 UTC date directories, examines at most 4,096 directory entries, considers at most eight filename-suffix candidates, and reads at most 64 KiB from each candidate's first physical record, which must be `session_meta`. Every real exact root or child lookup examined one candidate file. Observed identity reads ranged from 18,639 to 22,638 bytes before the strict byte-reader refinement. Deterministic tests prove that the directory-entry, candidate-file, and identity-byte caps report truncation, and any truncated or unreadable candidate set is forbidden from returning exact.

No cold/warm timing was needed to choose the direction: directory locations, entries, candidates, and bytes are all capped, and the successful path reads one small identity record. A future optional locator may reduce directory work, but no index or database is required for correctness at this gate.

## Current status

**C2 — Exact with bounded adapter.** Caller `thread_id` is a reliable exact persisted identity on the tested Codex version. Current persistence exposes that identity through dated rollout files, so a small bounded filename-and-`session_meta` compatibility adapter is required.

The local runtime/identity spike phase is **not yet complete**. The identity contract and adapter are established, but one updated no-argument tool call must still be observed directly from ChatGPT Desktop after a Desktop/plugin restart boundary. That narrow retest must not repeat the correlation or concurrency workloads.

## Evidence references

- [`docs/evidence/caller-mission-correlation.json`](../../docs/evidence/caller-mission-correlation.json)
- [`experiments/CTeam.Experiments`](../CTeam.Experiments)
- [`tests/CTeam.Experiments.Tests`](../../tests/CTeam.Experiments.Tests)
- [`experiments/003-persisted-near-live`](../003-persisted-near-live)
- [`experiments/005-plugin-mcp-runtime`](../005-plugin-mcp-runtime)

## Known limitations

The result is version-scoped to the current Windows Codex persistence and MCP extension. ChatGPT Desktop did not hot-load the reinstalled plugin during this session, so updated Desktop-hosted correlation is inferred by combining the already-proven Experiment 005 per-call metadata contract with exact live resolution of two Desktop-owned rollouts; a fresh Codex client provided the full updated end-to-end call. CM2 therefore remains unconfirmed specifically at the updated Desktop host boundary. Only naturally available subagent children were live-tested, not a separate review rollout. The adapter searches a 31-day window and reports truncation instead of guessing outside its bounds.

## Retest trigger

Immediate retest: after ChatGPT Desktop restarts or demonstrably reloads the installed plugin, make one no-argument mission call and confirm exact caller correlation. Otherwise retest when Codex changes `_meta.x-codex-turn-metadata`, the rollout filename convention, `session_meta` identity or parent fields, sessions storage location, active-writer sharing, plugin hot-reload behavior, or the bounded adapter reports not-found/truncation for a caller known to be persisted.
