# C-Team Architecture Improvements and Reference Notes

This document captures architecture improvements, implementation ideas, compatibility lessons, and follow-up opportunities discovered while reviewing C-Team spikes, OpenAI-maintained Codex plugins, third-party Codex plugins, and existing Codex monitoring/session-analysis projects.

Its purpose is to preserve useful ideas before production implementation begins. It is **not** a replacement for experiment evidence in `EXPERIMENTS.md`; experimentally proven behavior remains authoritative there.

## 1. Current architectural direction

The preferred local runtime shape is now:

```text
Codex / ChatGPT
      │
      │ plugin-managed stdio MCP lifecycle
      ▼
cteam.exe
.NET 10 NativeAOT
stdio MCP server
      │
      ├─ caller context from MCP tool metadata
      ├─ Codex persisted state
      ├─ optional app-server enrichment
      └─ future C-Team persistence
             │
             ▼
        normalized MissionState
             │
             ├─ MCP tools
             └─ Apps/widget UI
```

Key decisions already supported by spike evidence:

- `cteam.exe` should be a plugin-bundled NativeAOT executable.
- Codex should own its process lifecycle by launching it as a plugin stdio MCP server.
- Normal read-only MCP operation must not require recurring sandbox approval.
- Multiple lightweight MCP processes are acceptable; do not introduce a singleton broker unless evidence later requires one.
- Persisted Codex rollout state is the primary observation source for native Desktop work.
- Direct app-server telemetry remains useful when C-Team owns the Codex runtime.
- The UI should use MCP directly rather than introducing a second localhost HTTP/WebSocket API.
- C-Team should normalize Codex-specific records before exposing them to tools or UI.

## 2. Separate mission location from mission reading

Production code should clearly separate **finding the right persisted mission** from **parsing its rollout**.

Recommended shape:

```text
CallerContext
MCP turn metadata
      │
      ▼
MissionLocator
      │
      ├─ StateDatabaseLocator
      ├─ SessionIndexLocator
      └─ BoundedFilesystemLocator
              │
              ▼
          RolloutSource
              │
              ▼
          CodexReducer
              │
              ▼
          MissionState
```

This avoids coupling discovery heuristics to JSONL parsing and lets Codex format changes be isolated behind adapters.

### Mission locator precedence

Prefer the strongest exact signal available:

1. exact caller/thread identity from MCP metadata;
2. exact thread-to-rollout mapping from current Codex state metadata/database if validated;
3. session index metadata;
4. explicit mission/thread parameter supplied by the caller;
5. project/workspace hint;
6. bounded recent-filesystem search;
7. never silently guess when multiple candidates remain.

Cwd should be metadata, not identity.

## 3. Consider `state_N.sqlite` as an optional fast index

Existing Codex monitors show that current Codex state databases can expose useful thread metadata such as:

```text
id
title
tokens_used
model
reasoning_effort
rollout_path
updated_at
thread_source
```

This makes a validated state-database adapter attractive for an efficient:

```text
thread_id → rollout_path
```

lookup.

Guidelines:

- Discover the latest `state_N.sqlite` dynamically; do not hardcode one schema version forever.
- Treat the SQLite format as a private, version-sensitive optimization rather than C-Team's canonical source.
- Keep the rollout JSONL as execution evidence.
- If the DB is missing, locked, incompatible, stale, or lacks the requested thread, fall back safely.
- If adopted, use an in-process NativeAOT-compatible SQLite library rather than external `sqlite3` processes.

A future Experiment 007 can validate this optimization after Experiment 006 completes.

## 4. Treat rollout JSONL as canonical execution evidence

Persisted rollout JSONL remains the strongest source for observed execution facts such as:

- lifecycle;
- parent/child relationships;
- agent role/nickname metadata;
- turn context;
- tool activity;
- plan activity;
- token usage snapshots;
- timing;
- child-history boundaries;
- completion/interruption state.

Other sources should enrich or index this evidence rather than silently override it.

Recommended source roles:

| Source | Role |
| --- | --- |
| rollout JSONL | canonical execution evidence |
| MCP turn metadata | exact calling/session context |
| `state_N.sqlite` | optional fast thread/path/metadata index |
| session index | optional title/metadata enrichment |
| `logs_N.sqlite` | optional global usage corroboration |
| app-server | live enrichment and C-Team-owned runtime source |

## 5. Normalize at ingestion time

Do not expose Codex protocol objects directly to MCP tools or the UI.

A normalized domain should look approximately like:

```text
Mission
 ├─ Agents
 │   └─ Turns
 │       └─ Activities
 ├─ Plan
 ├─ Usage
 ├─ Evidence
 └─ Lifecycle
```

Useful entities include:

```text
Mission
AgentRun
Turn
Activity
PlanStep
UsageSnapshot
PendingHumanRequest
EvidenceValue<T>
```

The exact shape can evolve, but raw concepts such as `event_msg`, `response_item`, app-server notification names, and rollout-specific field names must remain behind source adapters.

This follows a useful pattern seen in existing Codex monitors: incoming live/persisted events are reduced into stable thread/turn/item maps before the UI consumes them.

## 6. Make provenance and confidence first-class

C-Team should improve on existing monitors by retaining **where a value came from and what it actually proves**.

Example:

```text
model
  value: gpt-5.6-sol
  source: turn_context
  confidence: configured-context

rolloutPath
  value: ...
  source: state_database
  confidence: indexed-private-state

tokenTotal
  value: 153220
  source: rollout
  confidence: observed
```

Important distinctions to preserve:

- requested model;
- configured model;
- routing evidence;
- observed persisted context;
- true upstream execution attestation, when unavailable;
- estimated cost versus subscription quota/billing;
- inferred activity versus explicitly persisted lifecycle.

Never collapse disagreement between sources without retaining provenance.

## 7. Improve subagent handling

Existing monitors independently confirm several behaviors already found by C-Team:

- child rollouts contain parent identity metadata;
- child discovery should not depend on having already loaded the parent;
- active children can make an otherwise missing/stale parent relevant;
- child token totals can be contaminated by inherited parent history unless a child-only boundary is applied.

Recommended behavior:

```text
discover child
      ↓
read parent thread id
      ↓
resolve parent independently
      ↓
attach into mission tree
```

Do not require discovery order to be parent-first.

### Child token boundary precedence

Prefer explicit evidence:

1. `subagent_history_start_ordinal` when present and validated;
2. a validated `world_state` boundary as compatibility fallback;
3. otherwise mark child token attribution as uncertain rather than pretending it is exact.

Add deterministic fixtures for every supported boundary variant.

## 8. Track own and inclusive usage separately

For nested agents, expose both direct usage and descendant-inclusive usage.

Example:

```text
B.A.
  own:         140k
  descendants: 30k
  inclusive:   170k
```

At mission level:

```text
root own:       120k
all descendants:370k
mission total:  490k
```

This prevents double counting and also supports the future HydraFusion-style requirement that every workflow leg, retry, critique, revision, and fallback be accounted for.

## 9. Incremental rollout parsing should remain the production strategy

Do not repeatedly parse entire large rollout files.

Maintain per-file reader state similar to:

```text
file identity/signature
byte offset
partial trailing UTF-8/JSONL data
current model/context
current usage snapshot
known lifecycle state
```

Continue from the previous byte offset.

For active Desktop observation:

```text
FileSystemWatcher
       +
periodic file-length/prefix reconciliation
       ↓
incremental JSONL parser
```

Important existing evidence:

- watcher-only correctness is insufficient;
- timestamp-only polling is insufficient on the tested Windows installation;
- partial trailing records and split UTF-8 sequences must be tolerated;
- replacement/truncation must trigger reconciliation/rebuild;
- newly created child rollouts may initially be zero bytes.

## 10. Avoid large historical rescans

Current mission observation and long-term analytics have different needs.

Use precise incremental parsing for active/recent missions. For history, prefer an index or normalized summaries rather than scanning months of rollout files repeatedly.

Possible progression:

```text
MVP
  read current/recent persisted state on demand

Later
  normalized C-Team SQLite history/index

Later still
  workflow/model comparison analytics
```

Do not add C-Team persistence solely to solve a problem that Codex's own persisted state already solves adequately.

## 11. MCP should remain the only local application protocol

OpenAI-maintained plugins demonstrate a clean pattern:

```text
model / widget
      ↓
plugin MCP
      ↓
backend/state authority
```

For C-Team:

```text
Apps/widget UI
      │
      ▼
MCP tools
      │
      ▼
cteam.exe
      │
      ▼
MissionState
```

Do not add a localhost HTTP/WebSocket API unless a concrete platform requirement cannot be met through MCP.

Benefits:

- one security surface;
- one lifecycle model;
- one structured schema contract;
- no extra listener/port;
- no CORS/origin handling;
- no duplicated backend semantics.

## 12. Widget opening and refresh behavior

A useful OpenAI plugin pattern is to separate **mounting the widget** from subsequent **data operations**.

Example C-Team shape:

```text
cteam_open
    ↓
returns/mounts widget

subsequent calls
    ↓
cteam_current_mission
cteam_agent_tree
cteam_usage
cteam_after_action
    ↓
data only
```

Do not return/recreate the widget template on every refresh.

This lets one mounted C-Team surface receive incremental structured updates without spawning replacement frames.

## 13. MCP tool design

Prefer clear read-oriented tools initially:

```text
cteam_current_mission
cteam_agent_tree
cteam_usage
cteam_missions
cteam_after_action
```

Separate tools are likely better than a single `cteam(action=...)` mega-tool for model discoverability and smaller schemas.

UI-specific state mutations, if needed later, can use dedicated actions/tools without mixing them into execution-observation reads.

The MCP backend should accept or derive exact mission identity and return ambiguity explicitly when it cannot identify one safely.

## 14. Skills and MCP have different responsibilities

Skills should explain:

- when C-Team is useful;
- which tool to call;
- workflow guidance;
- how to interpret evidence;
- how to present uncertainty.

MCP should own:

- data retrieval;
- normalization;
- state resolution;
- structured operations.

Do not hide business logic or protocol parsing inside `SKILL.md`.

## 15. Multi-process MCP is normal; design for it

Experiment 005 and OpenAI-maintained plugins both support the assumption that multiple plugin MCP processes can exist concurrently.

Default model:

```text
Codex context A → cteam.exe A
Codex context B → cteam.exe B
Codex context C → cteam.exe C
```

Do not create a singleton daemon solely to avoid this.

Read-only mission observation requires no cross-process coordination.

If shared C-Team persistence is introduced later:

- use SQLite WAL;
- keep write transactions short;
- use explicit schema/versioning;
- design for multiple readers/writers;
- add cross-process tests;
- never assume only one MCP instance exists.

## 16. Keep pending human interaction as domain state

Existing app-server monitors treat pending approvals/user requests as first-class state. C-Team should do the same when data is available.

An agent may be:

```text
running
waiting-for-tool
waiting-for-human
completed
failed
interrupted
```

This is useful both for UI and future After Action analysis.

Do not collapse all non-running states to a generic idle status.

## 17. Different data should refresh at different cadences

Not all telemetry needs the same refresh policy.

Examples:

- agent activity: near-live/event-driven;
- persisted file changes: watcher + reconciliation;
- token counters: update when Codex persists them;
- model catalog: occasional refresh/version change;
- quota/rate limits: slower polling or host notification;
- historical analytics: on demand/background index.

Avoid a global polling loop that repeatedly recomputes every data source.

## 18. Plugin package organization

A conventional eventual plugin package could be:

```text
plugins/c-team/
├─ .codex-plugin/
│  └─ plugin.json
├─ .mcp.json
├─ .app.json
├─ skills/
├─ assets/
└─ bin/
   ├─ win-x64/
   │  └─ cteam.exe
   ├─ linux-x64/
   │  └─ cteam
   └─ osx-arm64/
      └─ cteam
```

Development source should remain separate from packaged binaries:

```text
src/
tests/
experiments/
docs/
plugin/ or packaging/
artifacts/
```

The packaging step should publish/copy the correct RID binary into the plugin payload. Do not develop directly in the packaged `bin/` directory.

Initially the C-Team repository itself can also host its marketplace manifest; a separate marketplace repository is unnecessary while C-Team is the only product.

## 19. Keep experiments as a compatibility laboratory

Continue using `EXPERIMENTS.md` as a retest matrix.

For every blocked/private-Codex behavior record:

```text
what was tested
Codex/Desktop version
procedure
success condition
observed result
blocker
retest trigger
```

Examples of useful triggers:

- MCP Roots appears;
- `x-codex-turn-metadata` changes;
- plugin-owned durable data path appears;
- Desktop exposes a shared app-server endpoint;
- rollout/session schema changes;
- state database schema/version changes;
- plugin sandbox/trust behavior changes.

Preserve failed hypotheses, not transient failed build directories.

## 20. Candidate follow-up: Experiment 007 — state DB locator

Do this only if it remains useful after Experiment 006; it should not block product work.

Question:

> Given an exact caller/thread id, can current Codex state SQLite reliably resolve the corresponding rollout path without filesystem scanning?

Test:

```text
thread_id
   ↓
latest state_N.sqlite
   ↓
threads.id
   ↓
threads.rollout_path
```

Acceptance should cover:

- exact match to already-known Experiment 006 rollout identity;
- DB absent;
- DB locked/busy;
- older/incompatible schema;
- thread missing from DB;
- stale rollout path;
- subagent row/source behavior;
- read-only access from plugin MCP without approval;
- bounded fallback to session index/filesystem.

Treat success as an optimization, not a new canonical dependency.

## 21. Candidate future source: `logs_N.sqlite`

Existing projects use Codex logs SQLite to aggregate OTel-like `response.completed` token records for 24h/7d/30d usage.

Potential C-Team use:

- global usage corroboration;
- historical aggregate validation;
- perhaps quota/cost analytics.

Do **not** use it as the primary source for agent hierarchy or mission reconstruction unless future evidence demonstrates that need.

Priority: lower than `state_N.sqlite`.

## 22. Consider hooks as optional exact-session hints

Some Codex tooling receives exact session/transcript paths through hooks.

Potential pattern:

```text
Codex hook
   ↓
transcript/session path
   ↓
C-Team identity hint
```

This may be useful for lifecycle hints or after-action finalization, but should remain optional. The MCP caller/thread identity path is cleaner if Experiment 006 validates it.

Do not make C-Team dependent on hooks unless they solve a concrete missing signal.

## 23. After Action information architecture

Useful ideas from existing session viewers suggest an eventual completed-mission view with areas such as:

```text
Overview
Agents / Tree
Timeline
Usage
Plan
Evidence / Raw diagnostics
```

The exact tabs are a UX decision, but C-Team should preserve enough normalized information to support all of these without reparsing the original rollout in the UI.

Potential mission summary:

```text
Mission
  workflow
  outcome
  elapsed time
  agent count
  parallelism
  total usage
  model distribution
  retries/reviews
  test/review evidence
```

## 24. Preserve data needed for future routing intelligence

HydraFusion and deterministic subagent benchmarking reinforce that C-Team should collect enough data now to support future evidence-based recommendations.

For every meaningful agent/workflow leg retain where available:

```text
task family / role
workflow pattern
parent/coordinator
requested model
configured/observed model evidence
reasoning effort
service tier
start/end/duration
input/cache/output/reasoning tokens
own/inclusive usage
retry/revision/fallback relationship
tool/test/review evidence
outcome
infra failure vs candidate failure
```

The long-term progression should remain:

```text
Observe
  ↓
Compare
  ↓
Recommend
  ↓
User-approved routing
  ↓
Adaptive routing
```

Do not automate routing before evidence supports it.

## 25. Explicit anti-patterns

Avoid these unless new evidence changes the decision:

- Windows Service;
- administrator requirement for normal observation;
- separate local HTTP/WebSocket server just for the UI;
- singleton companion process merely to avoid multi-process MCP;
- cwd as mission identity;
- whole-session reparsing on every update;
- mtime-only live detection;
- watcher-only correctness;
- shell/Python/PowerShell runtime dependencies in production;
- external `sqlite3`, `rg`, or `tail` production dependencies;
- treating private SQLite or rollout schemas as stable contracts;
- hardcoded Sol/Terra/Luna domain enums;
- claiming configured model as upstream execution attestation;
- silently guessing among ambiguous missions;
- double-counting descendant token usage;
- throwing away evidence provenance when sources disagree;
- adding persistence before the product actually needs it.

## 26. Suggested production component boundaries

A reasonable first production decomposition is:

```text
CTeam.Domain
  Mission
  AgentRun
  Turn
  Activity
  Usage
  Evidence

CTeam.Codex
  CallerContext
  MissionLocator
  RolloutSource
  CodexReducer
  PersistedDesktopSource
  optional StateDatabaseLocator
  optional LiveAppServerSource

CTeam.Mcp
  stdio protocol host
  tool schemas
  caller metadata adapter

CTeam.Plugin
  packaging/manifests/skills/apps/assets
```

Keep these boundaries conceptual rather than creating assemblies merely for architecture aesthetics. Start with the smallest number of projects that preserves clear dependency direction and NativeAOT compatibility.

## 27. Implementation priority after the spike phase

Assuming Experiment 006 proves exact or bounded-exact caller-to-mission correlation, a sensible implementation sequence is:

1. freeze a minimal `MissionState` domain contract;
2. extract production-grade incremental rollout reading/reduction from experiment code;
3. implement exact caller-context mission resolution;
4. implement read-only MCP tools for current mission, tree and usage;
5. package the NativeAOT executable as the real plugin MCP backend;
6. build the first widget using the same MCP tools;
7. add source provenance/diagnostics;
8. measure actual UX/performance;
9. only then decide whether state SQLite indexing, C-Team persistence, or additional live app-server support is necessary.

The main rule remains: **prefer the simplest architecture already supported by measured evidence, and add complexity only when a concrete product need or compatibility gap requires it.**

## 28. Reference projects reviewed

Useful implementation/reference projects include:

- OpenAI `plugins`, especially Creative Production and OpenAI Developers;
- `manuelsh/codex-monitor`;
- `ALight777/codex-monitor`;
- `cuteribs/agent-session-viewer`;
- `VesperEngineering/codex-monitor`;
- `someonegg/codex_viewer`;
- `NemoFree/codex-hud`;
- `harveyxiacn/codex-usage-monitor`;
- `morgadoronan/codex-agents`;
- third-party plugin marketplaces such as Deepnote, Prisma, Spine, and Melodic Software.

These are reference inputs, not architectural authorities. C-Team spike evidence should win whenever observed Codex behavior differs from another project's assumptions.
