# C-Team — Desktop Near-Live Observation Spike

## Goal

Determine whether C-Team can provide a sufficiently responsive near-live view of a Codex mission running normally in ChatGPT Desktop by observing persisted Codex state only.

This spike is deliberately quota-sensitive. It must minimize Codex inference and reuse the existing observability work wherever possible.

The preferred test subject is the Codex session used to implement this spike itself.

## Core question

Can C-Team observe a ChatGPT Desktop Codex mission with enough fidelity and low enough latency that the experience feels live?

Conceptually:

```text
Codex event
    ↓
Desktop persistence
    ↓
C-Team detects change
    ↓
C-Team updates mission state

observed latency = ?
```

## Cost constraint

The first observability spike consumed a noticeable portion of the weekly Codex allowance. Do not assume that was caused by any one model; this spike must control workload shape explicitly.

Rules:

1. Do not run repeated multi-agent synthetic missions.
2. Do not deliberately create agent fan-out to populate telemetry.
3. Reuse the mission implementing this spike as the main observation target.
4. Prefer filesystem/process experiments over Codex inference.
5. Reuse existing recordings for deterministic tests.
6. Any additional live Codex run must answer a specific unresolved acceptance criterion.
7. If one child-agent observation is required, use one small bounded Face task only.
8. Stop when the acceptance criteria are answered.

The default primary model for this mission is **Sol, High reasoning**. Astra is not required for this spike.

## Existing evidence

Treat the first spike findings as established unless new evidence contradicts them:

- ChatGPT Desktop owns a private stdio app-server on the tested Windows installation.
- C-Team cannot currently subscribe directly to that Desktop-owned app-server through a supported mechanism.
- A separately launched app-server can discover and read persisted Desktop threads.
- Desktop rollout/session files are updated while a mission is active.
- Persisted data contains thread, turn, token, model-context, tool and subagent information.
- Rollout paths and exact persistence details are not stable public contracts.

Do not re-prove these from scratch.

## NL1 — Persistence latency

Measure the delay between activity in the Desktop mission and the corresponding persisted-state change first observed by C-Team.

Measure separately where possible:

- root turn activity;
- tool execution;
- token usage updates;
- subagent creation;
- subagent completion;
- root turn completion.

Report minimum, median, maximum, and p95 only if the sample size makes the percentile meaningful. Otherwise report raw observations and sample count.

## NL2 — File update behavior

Determine how Desktop writes persisted session/rollout data.

Investigate:

- append behavior;
- flush frequency;
- file timestamp behavior;
- partial trailing JSON lines;
- file replacement or rotation;
- multiple files belonging to one mission;
- whether writes are sufficiently atomic for continuous reading.

The observer must tolerate a partially written trailing record without treating the file as corrupted.

## NL3 — Best observation mechanism

Compare the smallest viable mechanisms:

### A — FileSystemWatcher

Watch relevant paths and react to changes.

### B — Polling

Periodically inspect file length/timestamps/state.

### C — Hybrid

Use FileSystemWatcher for low latency and periodic reconciliation for reliability.

Recommend one based on measured behavior rather than preference.

## NL4 — Mission discovery

Determine how reliably C-Team can identify the current Desktop mission without requiring a pasted thread id.

Candidate signals may include:

- repository/cwd;
- most recently updated thread;
- active rollout file;
- thread source;
- persisted timestamps;
- process information;
- known project path.

Classify discovery confidence as:

```text
certain
high-confidence
ambiguous
```

Do not silently hide ambiguity behind a heuristic.

## NL5 — Agent tree freshness

Determine whether child/subagent activity appears quickly enough in persisted state to render the agent tree while the mission is still active.

Measure where possible:

- child discovery delay;
- role/nickname availability;
- parent relationship availability;
- child completion delay.

Inherited parent history must not be attributed to child-owned work.

## NL6 — Token telemetry freshness

Determine whether persisted token information is useful while the mission is running.

Answer:

- how frequently token state changes;
- whether updates are cumulative;
- whether per-agent attribution remains possible;
- how delayed final totals are after completion.

Do not infer billing or quota consumption from token counts unless the protocol explicitly supports that relationship.

## NL7 — Completion and failure detection

Determine how quickly and reliably C-Team can identify:

```text
agent completed
mission completed
agent failed
mission interrupted
```

Persisted state should not remain apparently running for an unreasonable time after Desktop has completed.

## NL8 — Reconciliation and restart

Determine whether C-Team can recover if it:

- starts after the mission has already begun;
- misses filesystem events;
- restarts;
- encounters a partial trailing write;
- sees a file replaced or truncated.

A restart must reconstruct the current mission state from persisted data.

## NL9 — Observer overhead

Measure enough to ensure the observer itself is lightweight.

At minimum record:

- number of watcher notifications;
- reconciliation count;
- bytes read;
- full-file reparses, if any;
- parse failures;
- CPU/memory only if trivial to capture without expanding the spike.

Do not optimize before correctness is established.

## Implementation scope

Extend the existing spike only enough to answer NL1–NL9.

Suggested command:

```text
cteam watch
```

Potential options:

```text
cteam watch --cwd <repository>
cteam watch --thread <id>
cteam watch --json <output>
```

Exact CLI shape is not important. Do not introduce a production configuration framework.

## Suggested architecture

Keep persisted Desktop observation behind the normalized C-Team model:

```text
PersistedDesktopSource
        ↓
Persisted event/state mapper
        ↓
MissionState
        ↓
Console renderer
```

Keep the architecture compatible with the hybrid direction established by the first spike:

```text
Desktop-owned Codex ── PersistedDesktopSource ─┐
                                               ├─ normalized C-Team state
C-Team-owned Codex ── LiveAppServerSource ─────┘
```

The UI and future storage must not need to know which source supplied the telemetry.

Do not build SQLite, MCP, React, Apps SDK UI, analytics, steering, or automatic routing in this spike.

## Tailer experiment

Start with the minimum robust design:

```text
initial read
    ↓
remember byte offset
    ↓
watch for changes
    ↓
read appended bytes
    ↓
buffer incomplete trailing line
    ↓
parse complete JSONL records
    ↓
update MissionState
```

If watcher-only observation proves unreliable, add a periodic reconciliation pass.

Avoid repeatedly parsing entire large rollout files unless correctness requires it.

## Timestamping

C-Team needs its own observation timestamp.

For relevant records capture, where available:

```text
source_event_timestamp
file_observed_at
```

Derived:

```text
observation_delay = file_observed_at - source_event_timestamp
```

If the source timestamp does not represent the event creation/persistence boundary reliably, state that limitation and do not pretend the derived number is end-to-end latency.

Where useful, add file-write timestamps and distinguish:

```text
model/event time
file write time
observer detection time
```

## Self-dogfooding experiment

Use the current Codex mission implementing this spike as the main live workload.

1. Implement enough of `cteam watch` to observe a Desktop mission.
2. Run a separate watcher instance against the C-Team repository/session.
3. Continue implementing the spike normally in ChatGPT Desktop.
4. Let ordinary reasoning, commands, edits and token updates generate observation data.
5. Capture measurement output under ignored `.cteam/` files.
6. Trigger one bounded Face operation only if NL5 cannot otherwise be answered.

Do not create work merely to observe work.

## Optional minimal child-agent probe

Only if NL5 has no natural evidence, ask Face to perform one cheap read-only task, for example:

```text
Locate the persisted Desktop observation implementation and report the relevant files. Do not modify anything.
```

One child is enough to measure child discovery and completion behavior.

Do not invoke B.A., Murdock and Reviewer solely to populate the tree.

## Console output

A useful development view could look like:

```text
C-TEAM WATCH

Mission: Desktop near-live observation spike

HANNIBAL   running       observed 180 ms ago
└─ FACE    completed     observed 240 ms ago

Persistence
last write              21:14:32.184
last observed           21:14:32.356
delay                           172 ms

Tokens
root                         142,381
Face                          18,224

Observer
watcher events                   37
reconciliations                   2
partial lines                     3
parse errors                      0
```

Correctness matters more than presentation.

## Measurement log

Store private measurements under ignored `.cteam/`, for example:

```text
.cteam/
  near-live/
    measurements.jsonl
```

A measurement may include:

```text
timestamp
thread_id
agent_id
event_kind
source_event_timestamp
observed_timestamp
delay_ms
source_file
file_offset
```

Do not commit private raw recordings. Commit only sanitized aggregate findings or explicitly allowlisted evidence.

## Tests

Prefer deterministic local tests using existing or purpose-built file fixtures.

Cover at least:

- incremental append parsing;
- partial JSON lines;
- duplicate watcher events;
- missed event reconciliation;
- restart reconstruction;
- file truncation/replacement;
- child-parent attribution;
- cumulative token updates.

No Codex inference should be required for these tests.

## Acceptance criteria

The spike succeeds if it establishes evidence for all of these:

1. C-Team can discover or be pointed to an active Desktop mission.
2. C-Team can start after the mission has already begun.
3. Persisted changes can be detected incrementally.
4. Partial writes do not break observation.
5. Agent-tree changes can be observed during the mission.
6. Token changes can be observed during the mission.
7. Completion/failure state is detected reliably.
8. Restart/reconciliation reconstructs correct current state.
9. Typical observation latency is measured honestly.
10. One concrete watcher/polling/reconciliation strategy is recommended.
11. Observer overhead is small enough not to distort the experiment.

## Latency classification

Use measured results rather than forcing a target.

### Excellent

Typical delay below 1 second.

Proceed confidently with a near-live Desktop UI.

### Good

Typical delay around 1–3 seconds.

Suitable for C-Team. Present it as near-live rather than real-time.

### Acceptable

Typical delay around 3–5 seconds.

Potentially usable, but the UI must not imply instantaneous state. Investigate cheap refresh improvements.

### Poor

Typical delay above 5 seconds or highly inconsistent.

Persisted observation alone is insufficient for the primary live experience.

## Deliverables

Add/update:

```text
docs/near-live-observation.md
docs/spike-findings.md

cteam watch (or equivalent)
tests
sanitized latency/fidelity measurements
```

`docs/near-live-observation.md` should include:

```text
Environment
Observation mechanism
Measurement method
Results
Latency distribution
Failure/recovery behavior
Agent-tree fidelity
Token fidelity
Known limitations
Recommendation
```

## Decision gate

At the end answer:

> Is persisted Desktop observation responsive and reliable enough to be C-Team's primary telemetry source for native ChatGPT Desktop Codex missions?

Recommend exactly one:

### D1 — Hybrid, persisted-first

Persisted Desktop telemetry is sufficiently responsive and reliable for normal near-live use. Use direct app-server telemetry when C-Team owns the runtime.

### D2 — Hybrid, after-action-first

Persisted telemetry is reliable but too delayed/incomplete for a convincing live UI. Use it mainly for history and After Action.

### D3 — Owned-runtime-first

Persisted observation is too weak. C-Team must own Codex execution for the primary live experience.

Stop after this recommendation. Do not continue into production implementation automatically.

## Quota guardrail

The spike is complete when the observation questions are answered.

Do not spend model quota improving code quality beyond what is necessary for the experiment.

> Do not create work merely to observe work.
