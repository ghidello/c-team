# Experiment 001 — App-server observability

## Purpose

Determine what C-Team can observe when it owns a Codex app-server process, including agent hierarchy, configured models, tokens, lifecycle, tools, replay, the model catalog and quota identities.

## Original environment

Measured 2026-09-04 on Windows 10.0.26220 with ChatGPT Desktop 26.901.4073.0, Codex 0.153.1 and .NET 10. The controlled roles were configured as Hannibal/Sol, Face/Luna, B.A./Terra, Murdock/Sol and Reviewer/Sol.

## Hypothesis

An app-server owned by C-Team emits enough structured events to maintain a useful live agent tree and to reproduce the same normalized state by replaying the recording.

## Procedure

The original spike launched a separate stdio app-server, initialized it, recorded JSONL notifications and responses, ran bounded agent scenarios, normalized the stream, replayed the saved recordings and compared live and replay state hashes. It also queried the model catalog and rate-limit state. This archive reused the committed evidence; it did not rerun any Codex workload.

## Success criteria

- Structured hierarchy, lifecycle, command/tool and cumulative token events are observable.
- Per-agent configured model identifiers remain generic strings.
- A recording replays to the same normalized state as the live aggregation.
- Catalog and quota identities can be discovered without hard-coded product enums.

## Observed result

The owned app-server emitted structured root and subagent activity, lifecycle, command/tool and cumulative token events. Child snapshots exposed configured role/model combinations. Three representative recordings (`run-4`, `architecture-control`, and `plugin-invocation`) replayed to byte-identical normalized-state hashes.

Catalog discovery and separately named quota buckets worked, but CQ11 remained **Minimal** because the effective execution model of every response was not independently attested. No structured plan events appeared. Two requested thread-start fields required the experimental API capability, paginated native review rejected detached review, and sandboxed file-write attempts failed. Those negative results remain part of the compatibility record.

## Current status

**Passed**, with the CQ5 plan gap and CQ11 attribution limit above. This is an owned-runtime laboratory result, not proof that a second server can subscribe to a Desktop-owned live task.

## Evidence references

- [`docs/spike-findings.md`](../../docs/spike-findings.md)
- [`docs/codex-protocol.md`](../../docs/codex-protocol.md)
- [`docs/evidence/experiment-index.json`](../../docs/evidence/experiment-index.json)
- [`docs/evidence/replay-checks.json`](../../docs/evidence/replay-checks.json)
- [`docs/evidence/model-catalog.json`](../../docs/evidence/model-catalog.json)
- [`docs/evidence/quota-buckets.json`](../../docs/evidence/quota-buckets.json)

## Known limitations

The observations are private-protocol and version scoped. A configured model is not an execution attestation, token totals are not billing, and absence of plan events in this build does not prove permanent absence.

## Retest trigger

Retest when the app-server protocol or lifecycle materially changes, model attribution fields are added, structured plan events appear, quota schemas change, or recording/replay event semantics change.
