# C-Team runtime topology

## Status

The current proven runtime is a plugin-bundled NativeAOT stdio MCP process started and owned by Codex.

Experiment 005 proved that multiple independent Codex clients can start independent `cteam.exe` MCP processes cleanly. It did **not** establish whether native subagents inside one Codex root context share that MCP process or start additional ones.

Therefore:

> Multiple lightweight MCP processes are operationally viable, but they are not yet the desired or permanent production topology.

Experiment 007 (`PLUGIN_MCP_TOPOLOGY_SPIKE.md`) exists to measure the actual native process scope before C-Team chooses a shared-core architecture.

## Option A — direct stdio MCP

```text
Codex context A ──stdio──► cteam.exe A
Codex context B ──stdio──► cteam.exe B
Codex context C ──stdio──► cteam.exe C
```

Each `cteam.exe` owns the C-Team work needed by its context.

### Advantages

- already validated;
- no custom IPC;
- no discovery/singleton protocol;
- Codex owns process lifetime;
- crashes are isolated;
- no persistent background process;
- minimal security surface.

### Costs

If Codex starts many processes, each may eventually duplicate:

- active rollout watchers;
- parsing/index caches;
- history access;
- analytics state;
- self-improvement/recommendation computations.

This cost may be negligible if Codex scopes one MCP to a project or root mission. It may become wasteful if Codex starts one per native subagent.

A globally installed direct MCP must still be **lazy**. Outside an activated `.cteam` project it should do no rollout scans, watchers, indexing or analytics before an explicit call, and an inactive-project call should terminate quickly with a small disabled result.

## Option B — thin MCP facade + demand-started shared per-user core

```text
Codex A ──stdio──► cteam.exe facade ─┐
Codex B ──stdio──► cteam.exe facade ─┼──local IPC──► C-Team core
Codex C ──stdio──► cteam.exe facade ─┘                │
                                                       ├─ rollout watchers
                                                       ├─ normalized mission state
                                                       ├─ historical index
                                                       ├─ analytics
                                                       └─ learning/recommendations
```

This is the likely long-term option **only if measured topology or shared-state cost justifies it**.

The facade should remain intentionally small:

```text
MCP framing
caller/session metadata capture
project-activation resolution
local-core discovery/start
request forwarding
response translation
```

It should not own rollout parsing, durable history, analytics or the learning engine.

On Windows, a Named Pipe is the preferred IPC candidate because it avoids a TCP listener, firewall/CORS concerns and a second HTTP/WebSocket application protocol.

One binary can still support both modes:

```text
cteam.exe          # stdio MCP facade/default
cteam.exe core     # shared ordinary per-user process
```

Do not create separate installed services/executables unless evidence later requires it.

Critically, the facade must resolve project activation **before** starting the core. A globally installed C-Team plugin in an unrelated project must not create a background core merely because Codex launched the stdio MCP process.

## Option C — independent MCP processes sharing only SQLite

```text
cteam.exe A ─┐
cteam.exe B ─┼──► shared C-Team SQLite
cteam.exe C ─┘
```

This can share durable history and policy state, but does not eliminate duplicated live watchers, caches or analytics computations.

Avoid building leader election, heartbeat ownership and watcher hand-off into this topology merely to simulate a core. If those responsibilities become necessary, an explicit shared core is clearer.

SQLite remains useful as durable state underneath either Option A or B when C-Team eventually needs history/self-improvement persistence.

## Option D — use a future shared Codex runtime

If Codex eventually exposes a supported shared app-server/plugin runtime across clients/platforms, C-Team may be able to consume that directly and avoid some persisted-state/watch responsibilities.

Treat this as a retest opportunity, not a current dependency.

## Decision rule

Use Experiment 007 classification:

- **P1 — project-shared:** keep Option A; shared core low priority.
- **P2 — root-tree shared:** keep Option A; revisit only after measured shared-state cost appears.
- **P3 — per-thread/per-agent:** keep Option A for initial product work, but design toward Option B once watchers/history/analytics become non-trivial.
- **P4 — host-dependent/unclear:** keep interfaces/process boundaries flexible and avoid hard process-scope assumptions.

C-Team should not build infrastructure merely to optimize a topology Codex may already handle well.

## Shared-core lifecycle requirements

If Option B is ever implemented, zombie prevention is a hard product requirement.

The core is an **ordinary demand-started per-user process**, never a Windows Service.

Expected lifecycle:

```text
activated C-Team project makes first real request
    │
    ├─ core available → connect
    │
    └─ core absent → race-safe start → connect

last live facade disconnects
    │
    ├─ active core-owned work exists → remain
    │
    └─ no active work → short idle grace → exit
```

A future lifecycle experiment must prove all of these:

1. Multiple facades racing at startup create at most one usable core.
2. Core discovery uses per-user scope; one user's core cannot be reused by another user.
3. Clients are tracked through live connections/leases.
4. Pipe disconnect and/or client-process death removes a lease promptly.
5. Lease expiry handles cases where ordinary disconnect detection fails.
6. A core started accidentally but never connected exits quickly.
7. A core with zero clients and zero active owned work exits after a bounded grace period.
8. Abrupt Codex/Desktop/CLI termination does not leave a core indefinitely alive.
9. Stale mutex/pipe ownership recovers automatically after crashes.
10. Normal operation needs no administrator privilege, Scheduled Task, login startup entry or service registration.
11. Opening or using a project without `.cteam` does not start the shared core.

Potential Windows primitives:

```text
Named Pipe    → local IPC
named mutex   → race-safe singleton ownership
client PID    → optional liveness corroboration
connection/lease id → authoritative client ownership
```

Exact names/implementation should avoid exposing sensitive account data and should be scoped safely per user/session.

## Caller identity

Whether Option A or B is used, each MCP call must retain its caller context before entering the normalized C-Team core/domain:

```text
caller thread/session
workspace/project evidence
plugin identity
agent/root correlation where available
```

A shared core must never infer the current project solely from whichever MCP facade connected first.

## Project versus plugin scope

Plugin installation and project activation are separate concerns. A user may install C-Team personally while each project explicitly opts in with `.cteam` and supplies its own team/routing policy.

See `plugin-onboarding.md` and `host-presentation-and-context-footprint.md` for the desired local/global onboarding and dormant-plugin model.

## Implemented references

Several existing systems validate the thin-client/shared-core pattern:

- MCP proxy/broker projects use a per-client stdio shim over a shared local process to avoid repeatedly loading expensive state.
- Codex peer/orchestration projects use per-session MCP frontends with shared broker/storage.
- Other projects deliberately remove daemons when concurrency can be handled safely by the storage layer, reinforcing that C-Team should introduce a core only for concrete shared responsibilities.

C-Team should borrow the topology, not blindly copy any particular transport or daemon implementation.

## Production principle

> Keep the current direct stdio architecture simple, but place parsing, normalization, history and analytics behind boundaries that can move into a shared core later without changing MCP tool semantics or the UI.

And:

> Global installation must not imply global activity: projects without `.cteam` remain dormant, and a future shared core must never be started for them.