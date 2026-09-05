# Experiment 003 — Persisted Desktop near-live observation

## Purpose

Determine whether persisted ChatGPT Desktop Codex state is responsive and reliable enough for a near-live C-Team experience.

## Original environment

Measured 2026-09-04 on Windows 10.0.26220 with ChatGPT Desktop 26.901.4073.0, Codex 0.153.1 and .NET 10. The primary turn requested `gpt-5.6-sol` with high reasoning; that is configuration evidence rather than post-response model attestation.

## Hypothesis

A reader combining filesystem notifications with periodic file-length and prefix reconciliation can reconstruct an active Desktop mission, follow appended records with low delay and recover deterministically after missed events or restart.

## Procedure

The spike observed its own natural Desktop mission. An independent 100 ms probe measured persistence-to-observer delay for ten minutes, while the C# watcher ran for three minutes. Existing real snapshots and deterministic tests covered cold reconstruction, duplicate/missed notifications, partial UTF-8 and JSON lines, truncation/replacement and restart. This archive reused those results without creating telemetry.

## Success criteria

- Persisted records arrive fast enough for near-live status.
- Missed/duplicate filesystem notifications do not corrupt state.
- Cold reconstruction is deterministic.
- Agent parentage/lifecycle and cumulative tokens remain usable, with limitations stated.

## Observed result

For 131 relevant persisted records, delay was 0.574 ms minimum, 1.404 ms median, 6.668 ms p95 and 29.328 ms maximum. The implemented watcher independently saw 36 live relevant updates at 1 ms median and 2 ms p95. One length change was found by polling before a matching watcher event, while `LastWriteTimeUtc` never changed, proving that watcher-only and timestamp-only designs are insufficient.

The root rollout rendered natural child activity near-live. Child rollouts provided role, nickname, configured model and child-owned cumulative tokens after the inherited-history boundary. Token persistence was responsive when written, but root token totals advanced in steps with a 13.885-second median cadence. Cwd-only mission selection was ambiguous: ten same-cwd files included three root candidates. Two cold reads of a 1,645-record snapshot produced byte-identical state with zero parse failures.

## Current status

**Passed.** Decision D1: use a hybrid persisted-first observer with `FileSystemWatcher` for latency and length/prefix reconciliation for correctness.

## Evidence references

- [`docs/near-live-observation.md`](../../docs/near-live-observation.md)
- [`docs/evidence/near-live-measurements.json`](../../docs/evidence/near-live-measurements.json)
- [`tests/CTeam.Spike.Tests`](../../tests/CTeam.Spike.Tests)

## Known limitations

Record timestamps are not documented flush timestamps, so measurements are not model-action-to-UI latency. Automatic active-root selection and automatic child-file attachment remain unimplemented. No live root failure, interruption, rotation or completion was induced solely for telemetry.

## Retest trigger

Retest when rollout paths or schemas change, Desktop persistence/flush behavior changes, file replacement or rotation becomes common, a stable active-mission identifier appears, or child discovery/token semantics change.
