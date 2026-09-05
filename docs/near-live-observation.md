# Desktop near-live observation spike

## Environment

Measured 2026-09-04 on Windows 10.0.26220 with ChatGPT Desktop 26.901.4073.0, Codex 0.153.1 and .NET 10. The primary persisted turn context requested `gpt-5.6-sol` with `high` reasoning. As established in CQ2, that is configuration evidence rather than post-response execution attestation.

This follow-up accepts the first spike's CQ10 result: Desktop owns a private stdio app-server, while its rollout JSONL is readable by a normal per-user process. No app-server or synthetic Codex mission was launched for these measurements.

## Observation mechanism

`cteam watch` performs an initial reconstruction, remembers a byte offset, buffers incomplete trailing UTF-8 bytes, and parses only newline-terminated JSON objects. It combines `FileSystemWatcher` notifications with a one-second length/prefix reconciliation pass. Truncation or prefix replacement clears and rebuilds normalized state.

The persisted mapper handles session metadata, turn context, task start/completion/abort, completed command/file/subagent items, and cumulative token snapshots. It observes a single selected rollout. Root rollout activity supplies child identity, parentage and lifecycle; opening the child rollout supplies role, nickname, configured model and child-owned tokens. Records before `subagent_history_start_ordinal` are ignored so inherited parent history is not charged to the child.

Raw measurements remain under ignored `.cteam/near-live/`. The committed [aggregate evidence](evidence/near-live-measurements.json) omits paths, identifiers, prompts, commands, output and account data.

## Measurement method

Two local observers watched the Desktop mission used to implement this spike:

1. An independent 100 ms probe ran for 599.695 seconds. For every observed file length, it recorded the watcher/poll trigger and observer time. Completed JSONL line offsets were then correlated with the first observed length containing each line.
2. The implemented hybrid watcher ran for 180 seconds, including a cold reconstruction followed by live incremental reads and one-second reconciliation.

Latency is `observer detection time - top-level persisted record timestamp`. This measures record creation/persistence-to-observer delay. The source timestamp is not a documented file-flush boundary, so these figures must not be presented as model-action-to-UI end-to-end latency.

## Results and latency distribution

The independent probe produced 131 relevant post-start samples:

| Persisted record | n | Minimum | Median | Maximum | p95 |
| --- | ---: | ---: | ---: | ---: | ---: |
| All relevant | 131 | 0.574 ms | 1.404 ms | 29.328 ms | 6.668 ms |
| Command completion | 32 | 0.787 ms | 1.192 ms | 7.839 ms | 6.668 ms |
| File change | 11 | 0.798 ms | 1.121 ms | 9.017 ms | — |
| Reasoning item | 43 | 0.719 ms | 1.496 ms | 29.328 ms | 8.108 ms |
| Child completion | 2 | 0.798 ms | 1.066 ms | 1.334 ms | — |
| Token usage | 36 | 0.574 ms | 1.370 ms | 6.130 ms | 3.897 ms |

The implemented watcher independently saw 36 live relevant updates at 1 ms median, 2 ms p95 and 2 ms maximum during its three-minute window. This is **Excellent** under the spike's classification, with the timestamp limitation above.

Freshness and cadence are different. Root token state changed 44 times during the ten-minute probe: intervals were 3.504–59.525 seconds with a 13.885-second median. The UI can update activity near-immediately when Desktop writes it, while token counters will advance in visible steps.

## File update behavior

The root rollout grew monotonically by 889,366 bytes; no truncation, replacement or rotation occurred during the live window. The probe received 290 `Changed` notifications, observed 284 distinct lengths and eight duplicate same-length notifications. One post-initial length change was found by the 100 ms poll before a matching watcher notification, which is direct evidence against watcher-only correctness.

`LastWriteTimeUtc` did not change once across all 292 observations even though the file grew continuously. Timestamp-only polling is therefore invalid on this installation. Poll file length/prefix instead.

Both live observers recorded zero JSON parse failures. The implemented watcher encountered no partial trailing line during its window. Deterministic tests cover a partial JSON line and a UTF-8 code point split between reads. A newly created child rollout was briefly zero bytes, so discovery must tolerate creation before the first metadata line.

## Mission discovery

An explicit thread id or rollout path identifies the mission with **certain** confidence, and `cteam watch` supports both. Cwd alone is **ambiguous**: ten same-cwd files existed on the test day, including three root candidates and seven child/review files. Latest complete record time can provide a high-confidence suggestion, but there is no stable active-mission marker here; the observer must show ambiguity or ask the host/plugin for the current task id.

## Agent-tree fidelity

Two natural child operations occurred during this mission. Their child `session_meta` records contained parent id, depth, role, nickname and inherited-history boundary. The metadata timestamps preceded the corresponding parent `SubAgentActivity.started` records by 18 ms and 19 ms. Child-owned completion records preceded the parent's completion activity by 1 ms in both cases, and the live observer detected the two parent completion records in 0.798 ms and 1.334 ms.

The root rollout was enough to render both children while the mission remained active. Cold-reading the completed Face rollout reconstructed its role, nickname, 184-second duration, configured model and cumulative token total without attributing inherited parent history. The spike CLI does not automatically attach newly discovered child files, so per-child role/token hydration remains a small source-adapter follow-up rather than a proven product feature.

## Token fidelity

`thread_token_usage` was cumulative: all 44 root samples increased monotonically and produced 44 distinct totals. Child totals remained attributable from their own rollouts after the inherited-history boundary. Their final cumulative samples were persisted 14 ms and 8 ms before their task-completion records. No relationship to billing or quota consumption is inferred.

## Completion, failure and recovery behavior

Both child completions were observed live. Persisted `task_complete` uses the same mapper path for mission completion; prior real completion records and deterministic tests verify it. `turn_aborted` maps to interrupted in deterministic coverage. This mission did not induce a root failure or interruption merely to generate telemetry, and its own root completion cannot be measured before this report is returned.

Starting late worked: the observer reconstructed the active root plus seven known agents from 1,592 records before continuing incrementally. Two independent cold reads of a private real-rollout snapshot reconstructed byte-equivalent normalized states from 1,645 records / 5,550,443 bytes with zero parse failures. Deterministic tests also cover missed notification reconciliation, duplicate events, partial lines, truncation/replacement and restart.

## Observer overhead

The three-minute implemented run processed 5,438,111 bytes including its initial history, 1,592 records, 88 watcher notifications and 180 reconciliation checks. It performed zero full reparses and recorded zero parse failures. The ten-minute probe averaged 0.485 observations per second. CPU/memory were not separately sampled because the file, event and read counts already answered the bounded overhead question without adding instrumentation.

## PF1 packaging feasibility

The optional tiny NativeAOT probe stayed bounded. The initial attempt stopped at native linking because the Windows platform linker/Desktop Development for C++ workload was absent. After that prerequisite was installed, a 2026-09-05 retry of `dotnet publish .cteam/near-live/pf1-src/CTeam.Pf1.csproj -c Release -r win-x64 /p:PublishAot=true` succeeded. It produced a 934,400-byte `cteam-pf1.exe`. Copying that executable alone into an isolated directory and running it printed the expected marker and its new base directory, then exited with code 0. This establishes that the hello-world companion can be compiled as a standalone .NET 10 win-x64 NativeAOT executable and moved without a managed runtime payload.

PF1 remains unclassified A/B/C because this retry tested the native executable only. Plugin package inclusion, installed-root discovery, relative in-place launch, recurring approval behavior, a writable sidecar location, update replacement and a multi-platform binary layout remain untested. The next bounded PF1 experiment is to copy the executable into the local plugin fixture, refresh the cachebuster, and test execution from the installed cache without PATH or elevation.

## Known limitations

- Rollout schema and paths remain version-sensitive, private Codex implementation details.
- The source timestamp is not a documented flush boundary; measured delay is not full model-action latency.
- Automatic active-root selection and child-file attachment are not implemented.
- No live root failure/interruption was induced, and no live root completion sample was possible before ending this task.
- Live rotation/truncation did not occur; recovery for them is deterministic-test evidence.
- Rotation to a new rollout path requires mission rediscovery; the spike watcher follows one selected path.
- Same-prefix replacement beyond the checked prefix would require stronger file identity in production.

## Recommendation

Choose **D1 — Hybrid, persisted-first**. Persisted Desktop telemetry was responsive enough for normal near-live use and recovered deterministically after restart. Use `FileSystemWatcher` for latency and periodic length/prefix reconciliation for correctness. Present token updates as stepwise and surface mission-selection ambiguity. When C-Team owns Codex, retain direct app-server telemetry as the higher-fidelity source.

Stop at this decision gate. Do not proceed into SQLite, production MCP/UI, installer or lifecycle work without separate authorization.
