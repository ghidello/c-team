# Spike experiment plan

Scope: disposable .NET 10 console observer, protocol recordings, replay, and a minimal local plugin. Stop at the decision gate in SPIKE.md. No production UI, persistence service, steering, or model routing.

## Cheapest experiments first

| Experiment | Critical questions | Evidence / acceptance |
| --- | --- | --- |
| E0 Installed capabilities | CQ10, CQ11 | CLI version/help, generated schemas, Desktop transport/process and current-session metadata; no inference calls. |
| E1 Initialize and enumerate | CQ2, CQ10, CQ11 | Recorded stdio handshake, complete paginated model catalog including hidden models, account mode and quota buckets; compare owned server visibility with Desktop's current task. |
| E2 Tiny delegated fixture, repeated | CQ1–CQ7, CQ9, CQ11 | Sol parent requests a structured plan, Face/Luna inspection, B.A./Terra trivial edit and command, Reviewer/Sol independent check. Record every incoming/outgoing message. Preserve requested versus execution evidence. |
| E3 Review + lifecycle probes | CQ4, CQ8 | Custom reviewer from E2, native detached review, controlled command failure; synthetic interrupted/failed protocol cases clearly separated from live evidence. |
| E4 Replay equivalence | CQ3, CQ9 | Same ingestion code for live and replay, deterministic final-state comparison; duplicate cumulative token and item events must not double count. |
| E5 Desktop observation | CQ10 | Test only identified documented/local endpoints; read current task without resuming it. Verify persisted-state fields and freshness. Owned-server success alone is not attachment proof. |
| E6 Plugin shell | independent acceptance | Manifest validation, local install/discovery/invocation and refresh, using installed CLI mechanisms. |

## Architecture constraints

- Timestamp and sequence protocol traffic before mapping. Store raw recordings only in ignored `.cteam/`; publish an allowlisted sanitized excerpt and provenance separately.
- Keep domain values generic, with missing values unknown. Thread configuration, accepted model, turn context, and reported execution model are different evidence strengths.
- Replace per-thread cumulative token snapshots; preserve `last` independently. Do not add reasoning tokens to totals or cached tokens to input.
- Use turn boundaries for execution duration; thread creation age is a separate measure. A completed turn does not mean a thread can never run again.
- Map structured items by identity, track plan updates and diff counts without retaining diffs in domain state.
- Distinguish CQ10 outcomes A–D from decision-gate Architectures A–D; their letters describe different choices.

## Baseline

Custom role files request Face=`gpt-5.6-luna`, B.A.=`gpt-5.6-terra`, Murdock/Reviewer=`gpt-5.6-sol`. Controlled app-server runs will explicitly request Sol. The hosting conversation cannot establish its actual execution model from these files; any mismatch is evidence, not something to normalize away.

Final deliverables will record finding, evidence, confidence, architecture impact, and uncertainty for every CQ, including a conservative CQ11 Full/Partial/Minimal classification.

## Hannibal response to Murdock

Accepted: owned runtime and Desktop observation are separate source-qualification tracks; a new causally related Desktop event is required to prove live attachment. Record sequence and source provenance, keep requested/configured/execution evidence distinct, test cumulative counters rather than presuming correctness, and verify replay against raw assertions as well as state equality. Add an inherited-model control if the baseline execution exposes the necessary mechanism. Catalog and quota naming alone cannot earn CQ11 Full.

Deferred: a general normalized-event framework and unsupported transport engineering would overbuild this disposable spike. A small version-tolerant mapper with sourced observations is sufficient. No separate Spark inference is needed when the rate-limit response explicitly names its bucket; the per-turn attribution gap can remain documented.

At the decision gate, accepted Murdock's qualification that persisted lookup alone is insufficient for automatic observation. A subsequent scoped `thread/list` found this mission among two Desktop candidates; it did not identify which was active. Select Architecture B as the Desktop-compatible direction with explicit discovery/freshness/compatibility risks, not as a claim of production readiness. Retain the owned runtime only as an experimental reference, so a hybrid product is not justified by this spike. Keep CQ11 Minimal with a richer capability matrix because effective execution is still unproven.
