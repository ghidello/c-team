# C-Team MVP architecture

## Status

This document is the production architecture baseline after completion of foundational Experiments 001-009B.

`EXPERIMENTS.md` remains authoritative for measured Codex behavior. This document turns those results into product decisions. Experiment 010 remains optional and must not block implementation.

## Product goal

C-Team is a read-only observability and evidence companion for coding-agent teamwork.

North star:

```text
Observe -> Measure -> Learn -> Improve
```

The MVP implements the first two stages well enough to support the later two. It is not an IDE, coding agent, or orchestration daemon.

Tagline:

> **See your Codex team at work.**

Codex is the first adapter. The durable product meaning remains **C-Team = Coding Team**.

## MVP user experience

A user installs C-Team once as a Codex plugin. In unrelated repositories the plugin is effectively dormant. A project opts in by containing `.cteam/config.json`.

Inside Codex, initialization is agent-first:

```text
"Initialize C-Team in this project"
        ↓
small installed onboarding skill
        ↓
explicit approval
        ↓
bundled cteam.exe init
        ↓
.cteam/config.json + managed AGENTS.md section
        ↓
backend becomes active immediately
        ↓
fresh Codex task recommended only for new project guidance
```

Portable manual fallbacks remain available through equivalent `npx` and `dnx` bootstrap packages.

Once active, the user can ask for the current mission, agent tree, and usage through text and structured responses in Desktop or CLI/headless use.

The first MVP release requires this text/structured experience. The richer Desktop widget is an optional follow-up and does not block the plugin release candidate. Its host interaction must pass a small compatibility gate before its tool contract is finalized.

## Proven constraints that shape the architecture

The architecture deliberately reflects these experiment results:

- Plugin-bundled .NET 10 NativeAOT stdio MCP execution is approval-free for normal read-only operation.
- Codex Desktop persisted rollout state can be observed near-live with incremental JSONL parsing plus watcher/reconciliation.
- MCP `thread_id` identifies the caller exactly on the tested Codex version.
- Codex currently creates a distinct C-Team MCP process for each tested root/native agent context (P3).
- A globally installed MCP can expose one compact stable `cteam` tool; the measured experimental schema was 292 bytes.
- The installed Codex state DB can resolve exact `thread_id -> cwd` and project activation with zero rollout reads on the successful path.
- `.cteam` marker creation is visible to the same running MCP without tool-catalog refresh.
- Desktop plugin reinstall/update is not hot-reloaded reliably; a full Desktop restart is the safe update boundary.
- Agent-first onboarding is viable after that restart boundary, with `npx` and `dnx` as equivalent manual bootstrap surfaces.

## Production process topology

### MVP topology

```text
Codex root/agent context
        │
        │ owns stdio lifecycle
        ▼
+------------------------------+
| cteam.exe                    |
| .NET 10 NativeAOT            |
| compact MCP facade + domain  |
+------------------------------+
        │
        ├─ exact caller/project resolution
        ├─ Codex persisted-state adapters
        ├─ incremental rollout reader
        ├─ normalized MissionState reducer
        └─ structured MCP result rendering
```

There may be several `cteam.exe` processes at once. The MVP accepts this.

### Why no shared core yet

Experiment 007 established P3, so a future shared core is likely useful once C-Team owns expensive shared work such as watchers across many contexts, historical indexing, analytics, or learning state.

Do **not** build it for MVP. Start with process-local current-mission reconstruction and measure its resource cost during implementation. Bounded observation lifetimes and caches are required; the experiments did not establish that duplicated full mission reconstruction is always cheap.

The code must nevertheless preserve this seam:

```text
Codex contexts
   │   │   │
   ▼   ▼   ▼
small MCP facades
       │
       │ future local IPC
       ▼
optional demand-started shared core
```

If introduced later, the core must be an ordinary per-user process, never a Windows Service, Scheduled Task, or permanently resident login daemon. Zombie prevention remains a hard requirement.

## Global context footprint

Production C-Team should expose one stable compact MCP entry point rather than a large global tool catalog.

Preferred shape:

```text
cteam(action)
```

Initial operations:

```text
status
mission
agents
usage
```

Add `open` only with the optional Desktop widget after its host compatibility gate passes. Keep the single-tool shape unless that gate establishes a concrete need for a separate mount operation; measure any resulting catalog growth.

Possible later operations may include history and after-action data, but the schema must stay deliberately small.

Why one compact facade:

- C-Team may be installed globally while most projects do not use it.
- Codex discovers MCP tool definitions before invocation.
- Multiple narrow tools would permanently increase unrelated model context.
- Project-scoped behavior can instead be progressively disclosed through project guidance and later project skills.

Do not add diagnostic/spike tools to the production plugin.

## Project activation

Activation is read-only and caller-specific.

Resolution precedence:

```text
1. exact caller workspace/root metadata, when reliably supplied
2. exact compatible Codex state DB row by thread_id
3. Experiment-006-style bounded exact rollout identity fallback
4. unresolved
```

Never use:

- MCP process cwd as project identity;
- latest thread;
- recent-file guessing;
- neighboring/sibling directory scans.

For the validated state DB path:

```text
thread_id
   ↓
compatible state_N.sqlite
   ↓
threads.id = exact primary-key match
   ↓
cwd / project metadata
   ↓
bounded upward root normalization
   ↓
<project-root>/.cteam/config.json
```

The state DB is private Codex implementation detail and therefore compatibility-checked. It is an efficient locator, not C-Team's canonical execution evidence.

On an explicit call, when the DB is absent, locked, incompatible, stale, or missing the caller row, degrade to the exact bounded rollout adapter. Before activation, this fallback may enumerate bounded candidate locations and read identity metadata only. It must not reconstruct activities, hydrate descendants, or start watchers. If neither path proves identity, return an explicit unresolved result. Never guess.

## Dormant behavior

Before any explicit C-Team call:

```text
C-Team installed
      ↓
MCP may be eagerly started by Codex
      ↓
no rollout scan
no watcher
no history/index work
no analytics
no shared-core startup
      ↓
explicit cteam call only
      ↓
bounded caller/project identity lookup
      ↓
activation check
      ↓
not enabled or unresolved → small status result; no observation
```

MCP initialization and tool-list serialization are acceptable. Startup performs no project lookup or rollout reads. On an explicit call, a successful state DB activation lookup still reads zero rollouts; the compatibility fallback may perform bounded identity reads even when the project ultimately proves inactive. These reads are distinct from active mission observation and must be counted separately in tests.

An inactive or unresolved result must leave no observation session running. Removing or invalidating activation also stops an existing session under the lifecycle rules below.

## Observation sources

### Canonical source roles

| Source | Production role |
| --- | --- |
| MCP caller metadata | exact invoking context identity |
| Codex state DB | exact project/cwd locator and optional metadata index |
| rollout JSONL | canonical execution evidence |
| session/index metadata | optional enrichment/fallback |
| app-server | live source when C-Team itself owns Codex |

Rollout JSONL remains the authority for execution facts such as agent creation, hierarchy, activity, lifecycle, plans, and token snapshots.

## Source adapter boundary

Codex-specific formats must stay behind adapters.

Recommended interfaces/modules:

```text
CallerContext
ProjectLocator
MissionLocator
RolloutSource
MissionReducer
MissionQueryService
```

A production read path becomes:

```text
MCP request
   ↓
CallerContext
   ↓
ProjectLocator
   ↓
activation check
   ↓
MissionLocator
   ↓
RolloutSource
   ↓
Codex reducer
   ↓
normalized MissionState
   ↓
MCP response
```

No UI or skill should parse Codex rollout/state formats directly.

## Normalized domain

The MVP domain must be vendor-neutral enough that future adapters can feed it without rewriting presentation or analytics.

Minimum useful shape:

```text
MissionState
  identity
  lifecycle
  rootAgent
  agents[]
  treeCompleteness
  coverageReasons[]
  observedAt
  plan
  usage
  currentActivity
  pendingHumanRequests[]
  evidence[]
```

Agent shape:

```text
AgentRun
  id
  parentId
  role
  nickname
  lifecycle
  model
  reasoningEffort
  currentActivity
  ownUsage
  inclusiveUsage
  startedAt
  completedAt
  evidence[]
```

Values whose meaning is weaker than their name suggests must retain provenance/confidence. Examples include configured model versus execution attestation, inferred lifecycle versus explicit persisted state, and uncertain child-token boundaries.

## Mission identity and hierarchy

Exact caller identity is thread-based, while the displayed mission normally groups a root and descendants.

Rules:

- Resolve the caller's own persisted thread exactly first.
- Preserve child identity; never silently replace a child with the root.
- Derive the mission root through persisted parent/session/spawn relationships.
- Discover children independently of parent-first load order.
- Keep malformed or ambiguous relationships visible as evidence/diagnostics rather than inventing a hierarchy.

Exact identity does not establish a complete tree. Experiment 003 left automatic child-file attachment unimplemented; Experiment 006 established child-to-root correlation for already-known child identities. Production must explicitly validate discovery from a root caller, including nested and late-created children.

Discover descendants from explicit persisted relationship records in admitted rollouts, then resolve each referenced thread exactly. Reconcile newly persisted relationships during observation. Bound traversal depth, unique thread count, candidate lookup work, bytes, and retries; detect cycles and deduplicate identities. Missing or zero-byte child files remain pending and are retried only within an active observation lease. Do not search unrelated projects to fill gaps.

Return `treeCompleteness` independently from identity confidence: `complete_for_observed_relationships`, `partial`, or `unknown`. Completeness is relative to the records read at the reported observation time, never a claim that Codex has already persisted every agent. Missing files, pending children, excluded branches, incompatible records, and exhausted bounds must appear as coverage reasons.

### Project boundary for mission traversal

For MVP, returned execution data is scoped to the exact caller's activated project root. A proven parent/session relationship may identify a mission root outside that scope, but does not activate another project.

Before reading a relative's execution records, resolve its own project using the same bounded identity and project-normalization rules. Include its activity and usage only when that normalized root equals the caller's activated root. A sibling worktree or nested repository is a separate project even when it shares Git history or also enables C-Team; recognize both `.git` files and directories as boundaries.

Represent an out-of-scope or unresolved relative as an excluded branch using only relationship identity already established from admitted evidence. Omit its paths, prompts, activity, and usage, mark coverage partial, and do not traverse through that branch. A child whose root is excluded remains displayed as the exact caller within a partial mission view. Cross-project observation is deferred to a separately designed opt-in contract.

## Incremental rollout reading

Production reading must be incremental.

Per rollout maintain:

```text
file identity/signature
byte offset
partial trailing UTF-8/JSONL bytes
reducer state
last observed lifecycle
last usage snapshot
```

For active Desktop sessions:

```text
FileSystemWatcher
      +
periodic reconciliation
      ↓
incremental parser
```

Watcher-only and timestamp-only strategies are insufficient based on Experiment 003. Replacement, truncation, partial UTF-8, incomplete JSONL records, and newly-created zero-byte child rollouts must be handled explicitly.

The MVP may initially reconstruct on explicit queries and keep a short-lived in-process cache; a continuously active watcher should only be started for an enabled project and when a query/presentation actually benefits from near-live updates.

### Observation lifetime

Experiment 007 showed that completed turns can retain reusable agents and their MCP processes. MCP process lifetime therefore cannot be the sole observation lifetime.

Use process-local demand leases for observed missions. An explicit `mission`, `agents`, or `usage` query, or a visible-view refresh, renews demand; `status`, tool discovery, and watcher events do not. Track views separately so closing one does not cancel another view's unexpired demand. Start with a 30-second idle expiry measured by a monotonic clock. When the last demand expires, dispose file watchers, reconciliation timers, open readers, pending child retries, and cached mission data. A later query revalidates activation and reconstructs the session. The MCP itself remains available.

Check activation before serving each query and before each background reconciliation pass, with a maximum one-second reconciliation interval while a lease is active. Missing, invalid, inaccessible, or unresolved activation cancels observation and evicts cached execution data. After cancellation, only another explicit query can attempt activation again. View closure may release a lease early where supported; expiry remains the fallback for a lost view or client. Cancellation and MCP shutdown also dispose sessions.

Set explicit per-process limits on retained missions, tracked files, and buffered bytes in the implementation. Reaching a limit must produce partial coverage or release an idle session, never silently grow resources without a bound. These are initial product limits to validate, not latency/resource claims from the experiments.

## Usage accounting

Expose both direct and inclusive usage.

```text
agent own usage
agent descendant usage
agent inclusive usage
mission root own usage
mission descendant usage
mission total
```

Do not double count inherited child history. Use explicit validated child-history boundaries where available; otherwise report attribution uncertainty.

When hierarchy coverage is partial, expose the observed in-scope subtotal and its coverage reasons. Do not label it as the full mission total or treat unavailable/excluded agents as zero usage. Apply the same rule to each agent's inclusive usage.

MVP usage should report tokens and observed model/effort evidence. Dollar cost and subscription quota attribution are not required for the first production slice unless a reliable source is available.

## MCP response contract

Every operation must work without a graphical UI.

Each response should provide:

```text
structuredContent -> canonical machine-readable data
content           -> compact model/human-readable summary
_meta             -> optional host/widget-only metadata
```

Required information must never exist only in `_meta` or widget resources.

Inactive/unresolved results should be tiny and stable, for example:

```json
{
  "status": "project_not_enabled"
}
```

or:

```json
{
  "status": "project_unresolved"
}
```

## Desktop and CLI presentation

The same normalized query result feeds both hosts.

```text
MissionQueryService
      │
      ├─ concise text/structured result -> Codex CLI / codex exec
      └─ MCP App resource              -> Desktop rich UI
```

The rich UI is optional for the first MVP release. Text/structured responses remain the required experience in both Desktop and CLI.

Experiments 001-009B did not validate MCP App mounting or widget-originated refresh calls. Before freezing `open` or building the view, test a minimal resource against the installed Desktop host: mount once, refresh without remounting, and retain the same exact mission when switching tasks or opening two views. Establish whether refresh carries caller metadata. If it does not, validate a process-local view binding created by an exact caller request, recheck activation on refresh, and reject expired or mismatched bindings rather than falling back to process cwd or the latest task. This binding carries no durable authorization and must not widen the project boundary.

Record the result and host-version retest trigger in the compatibility lab. If the gate fails, omit the widget/open surface from the MVP package and retain text/structured operation. Do not add a second application protocol to bypass a failed gate.

Initial CLI rendering should favor compact Unicode/text output rather than building a separate C-Team terminal application.

Initial Desktop UI should focus on one mission surface with:

- mission status;
- agent hierarchy and activity;
- plan progress where available;
- usage by root/agent;
- evidence/provenance drill-down only when needed.

Do not create a second localhost HTTP/WebSocket API. MCP remains the only local application protocol.

## Onboarding and project format

Canonical initial project footprint:

```text
.cteam/
  config.json
AGENTS.md
```

`config.json` starts as:

```json
{
  "schemaVersion": 1
}
```

The initializer owns one marker-delimited C-Team block in root `AGENTS.md`, preserving unrelated content.

Initialization must remain:

- deterministic;
- idempotent;
- safe on partial state;
- upgrade-aware;
- project-local;
- separate from global plugin installation.

Agent-first onboarding is the primary UX. `npx` and `dnx` are portable manual alternatives. The plugin-bundled executable is the canonical native implementation, but it is not exposed as a normal terminal command until there is a supported stable path/install story.

## Persistence owned by C-Team

The MVP should not create a durable C-Team database merely because one may be useful later.

Current mission state can be reconstructed from Codex persistence.

Add C-Team-owned SQLite only when implementing durable history/After Action/analytics that cannot be represented economically by the existing source state. At that point use WAL, explicit migrations, short transactions, and multi-process-safe access.

## Security and trust model

MVP is read-oriented except for explicit project initialization.

Rules:

- no administrator privileges;
- no service installation;
- no hidden global configuration mutation;
- no project mutation without explicit initialization approval;
- read Codex private state defensively and compatibility-check schema/layout;
- use bounded reads and fail closed on ambiguity;
- do not expose raw prompts/paths in normal model-facing results unless necessary;
- raw experiment/evidence diagnostics stay out of production tool responses.

## NativeAOT and runtime constraints

Production executable:

```text
.NET 10
C#
NativeAOT
win-x64 first
self-contained plugin-bundled executable
System.Text.Json source generation
```

Production code must not require Python, PowerShell, Node, npm, or the .NET SDK. `npx`/`dnx` are optional bootstrap distribution surfaces only.

Windows is the first supported platform. Keep interfaces/path abstractions portable, but do not spend MVP time implementing uncommitted platforms.

## Explicit MVP non-goals

Do not implement yet:

- shared C-Team core/daemon;
- cloud sync/accounts;
- automatic routing or agent spawning;
- automatic skill rewrites;
- historical analytics database;
- cross-vendor adapters;
- general-purpose terminal TUI;
- localhost HTTP/WebSocket service;
- Windows Service/Scheduled Task/login daemon;
- cost/billing estimates without trustworthy provenance;
- experiment-only diagnostic tools in the production plugin.

## Evolution after MVP

Expected progression:

```text
M1 Read-only companion
   current mission + agents + activity + usage

M2 After Action/history
   completed mission summaries + durable local evidence

M3 Learn
   compare missions, workflows, models, delegation choices

M4 Improve
   evidence-backed routing/skill recommendations with human approval
```

The north star remains Observe -> Measure -> Learn -> Improve, but each later stage must be earned by evidence from the stage before it.
