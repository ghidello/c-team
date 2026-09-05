# `.cteam/` scratch audit

Audited 2026-09-05 after PF1. `.cteam/` remains ignored and **nothing was deleted**. Sizes are approximate snapshots; classifications apply to each explicitly listed top-level item.

| Top-level item | Classification | Local disposition | Reason |
| --- | --- | --- | --- |
| `architecture-control.live.json` | TRANSIENT | Discard | Derived normalized state; the committed replay hash records the result. |
| `architecture-control.replay.json` | TRANSIENT | Discard | Duplicate replay derivative. |
| `architecture-control.txt` | TRANSIENT | Discard | One-off prompt/output support. |
| `build-current/` (20.4 MB) | TRANSIENT | Discard | Build output. |
| `build-currentRelease/` (10.3 MB) | TRANSIENT | Discard | Build output. |
| `catalog.json` | UNIQUE_EVIDENCE | Discard after archive acceptance | Private/raw source; allowlisted catalog facts are already committed. |
| `example-state.json` | TRANSIENT | Discard | Reconstructable normalized output. |
| `experiment-004/` | UNIQUE_EVIDENCE | Keep privately until PF1 evidence is accepted, then discard | Raw bounded approval transcript; sanitized facts are committed. |
| `final-build/` | TRANSIENT | Discard | Build output. |
| `final-prompt.txt` | TRANSIENT | Discard | One-off prompt. |
| `finish-probes.ps1` | HISTORICAL_REPRODUCTION | Discard | One-off orchestration; durable method and measurements are documented. |
| `fixture/` | TRANSIENT | Discard | Generated fixture copy; canonical telemetry fixture is already committed. |
| `fixture-2/` | TRANSIENT | Discard | Generated fixture copy. |
| `fixture-3/` | TRANSIENT | Discard | Generated fixture copy. |
| `fixture-4/` | TRANSIENT | Discard | Generated fixture copy. |
| `fixture-prompt.txt` | TRANSIENT | Discard | One-off prompt. |
| `murdock-prompt.txt` | TRANSIENT | Discard | One-off prompt. |
| `near-live/` (115.5 MB) | UNIQUE_EVIDENCE | Keep private raw measurements if future reanalysis matters; discard its build/publish subtrees | Contains the only raw latency/watch traces plus substantial transient build output. Sanitized aggregates are committed. |
| `parent-build/` | TRANSIENT | Discard | Build output. |
| `plugin-invocation.live.json` | TRANSIENT | Discard | Derived normalized state. |
| `plugin-invocation.replay.json` | TRANSIENT | Discard | Duplicate replay derivative. |
| `plugin-market/` | REUSABLE_FIXTURE | Keep | Configured local marketplace fixture used for plugin compatibility retests; contains generated NativeAOT binary and remains ignored. |
| `plugin-prompt.txt` | TRANSIENT | Discard | One-off prompt. |
| `plugin-skills.json` | TRANSIENT | Discard | Reconstructable plugin enumeration output. |
| `probe.py` | HISTORICAL_REPRODUCTION | Discard | Original one-off probe; method/results are documented and new probes are C#. |
| `pythondeps/` | HISTORICAL_REPRODUCTION | Discard when plugin validation no longer needs it | Local PyYAML copy used by the official validator; regenerable and not a product dependency. |
| `quotas.json` | UNIQUE_EVIDENCE | Discard after archive acceptance | Private/raw source; allowlisted quota identities are already committed. |
| `recordings/` (4.6 MB) | UNIQUE_EVIDENCE | Keep privately only if future protocol reanalysis is desired | Raw app-server recordings contain unique detail and must not be committed; allowlisted indexes and replay checks are committed. |
| `review-after-input/` | TRANSIENT | Discard | Review build output. |
| `review-build-final/` | TRANSIENT | Discard | Review build output. |
| `review-final2/` | TRANSIENT | Discard | Review build output. |
| `review-signoff/` | TRANSIENT | Discard | Review build output. |
| `run-1-replay-fixed.json` | TRANSIENT | Discard | Derived replay state. |
| `run-1-replay.json` | TRANSIENT | Discard | Derived replay state. |
| `run-1.live.json` | TRANSIENT | Discard | Derived live state. |
| `run-2b.live.json` | TRANSIENT | Discard | Derived live state. |
| `run-3b.current-replay.json` | TRANSIENT | Discard | Derived replay state. |
| `run-3b.live.json` | TRANSIENT | Discard | Derived live state. |
| `run-4.final-replay.json` | TRANSIENT | Discard | Derived replay state. |
| `run-4.live.json` | TRANSIENT | Discard | Derived live state. |
| `run-4.replay.json` | TRANSIENT | Discard | Derived replay state. |
| `schema/` | HISTORICAL_REPRODUCTION | Discard unless exact 0.153.1 generated source is needed | Large generated protocol-schema tree; used fields and conclusions are documented. |
| `schema-json/` | HISTORICAL_REPRODUCTION | Discard unless exact 0.153.1 JSON schema is needed | Large generated schema tree; regenerable from the matching Codex build. |
| `summarize.py` | HISTORICAL_REPRODUCTION | Discard | Original summarizer; committed allowlisted evidence is authoritative. |

## Promotion decision

No raw `.cteam/` file added unique safe facts beyond the committed evidence. The archive therefore promotes procedures and conclusions into `experiments/001` through `004`, and promotes only the allowlisted PF1 result to `docs/evidence/pf1-plugin-launch.json`. No raw rollout, prompt, command transcript, account datum, build directory or binary is committed.
