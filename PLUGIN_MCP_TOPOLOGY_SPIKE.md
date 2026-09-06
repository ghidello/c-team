# Experiment 007 — Plugin MCP process topology

## Purpose

Determine the actual Codex plugin MCP process topology before C-Team commits to either a direct-per-context runtime or a shared-core architecture, and measure what a globally installed plugin costs in projects that do not opt into C-Team.

The key unanswered questions are:

1. whether Codex starts a separate plugin MCP process for each native subagent, shares one process across a root thread and its children, or scopes plugin MCP lifetime at a broader project/client boundary;
2. whether a globally enabled C-Team plugin starts/runs in projects without `.cteam`, and how small/dormant that footprint can be.

This experiment is deliberately small and quota-sensitive, but unlike Experiment 006 it **must intentionally create bounded named-agent fan-out because fan-out itself is the subject under test**.

## Why this matters

Experiment 005 proved that two independent Codex CLI client contexts started two distinct `cteam.exe` MCP processes. It did **not** prove that every spawned Codex subagent gets its own C-Team MCP process.

The difference materially changes the runtime decision:

- one MCP per project or root mission is acceptable for a long time;
- one MCP per subagent/thread makes a thin facade plus shared per-user C-Team core much more attractive once history, analytics, watchers and self-improvement arrive;
- a globally shared MCP would require especially careful caller/project isolation.

Separately, plugin installation is currently broader than repository activation. C-Team should eventually be globally installable without actively observing unrelated projects or injecting a large permanent tool/instruction surface.

Do not build a broker/core or elaborate activation mechanism based on an unmeasured assumption.

## Environment

Record at execution time:

- date/time and OS;
- ChatGPT/Codex Desktop version if used;
- Codex CLI version;
- .NET SDK version;
- installed C-Team plugin version/cache path;
- primary model and reasoning effort as configured evidence only.

## Hypotheses

### TP1 — subagent process scope

For one native Codex root context, deliberately invoke several named agents with tiny bounded tasks:

```text
Hannibal / root
  ├─ Face
  ├─ B.A.
  └─ Reviewer
```

Record whether their C-Team MCP calls use the same `cteam.exe` PID as the root, distinct PIDs per child, or some other repeatable grouping.

### TP2 — same-project conversation scope

Within the same repository/project, open or exercise two independent root Codex contexts if this can be done cheaply.

Determine whether they reuse one C-Team MCP process or get distinct processes.

### TP3 — cross-project scope

Exercise two simultaneous Codex contexts rooted in two different projects/workspaces while the same C-Team plugin is installed/enabled.

Determine whether they share or isolate C-Team MCP processes.

A tiny throwaway fixture project is acceptable if needed. Do not create expensive work merely to generate telemetry.

### TP4 — process cleanup

For every observed C-Team MCP process, record start and exit timing relative to the owning Codex context.

Confirm whether plugin-owned MCP children terminate when their owning root/client context exits.

This experiment concerns Codex-owned stdio MCP children only. Do **not** implement a shared C-Team core yet.

### TP5 — globally installed plugin in an inactive project

Use a second minimal repository/workspace that **does not contain `.cteam/`** while the same C-Team plugin remains installed/enabled.

Determine:

- whether Codex starts the C-Team MCP process at all;
- whether root and native child contexts create additional C-Team MCP processes;
- what C-Team MCP tool inventory is visible to the host/model;
- whether startup performs any Codex-session scan or other meaningful work before a tool call;
- whether an explicit probe can return a tiny `project_not_enabled` result without scanning persisted Codex state;
- whether the C-Team process exits cleanly when the owning context exits;
- any observable Desktop versus CLI difference.

Do not claim that `project_not_enabled` removes tool-schema context cost. Tool discovery occurs before tool invocation. The experiment should record the visible tool inventory and serialized schema/description size where practical rather than guessing model-token cost.

## Instrumentation

Reuse the Experiment 005 NativeAOT MCP harness and plugin package. Extend the existing probe only as needed to emit sanitized process-topology and activation evidence.

Each relevant MCP initialize/tool call should capture at least:

```text
cteam process id
process start timestamp
MCP initialize timestamp
MCP client name/version
caller session_id
caller thread_id
caller plugin id
workspace-map presence/count
project activation: .cteam present | absent | unresolved
agent role/nickname when persisted evidence can correlate it safely
parent/root thread id when safely derivable from existing persisted evidence
tool-call timestamp
process exit timestamp when observable
```

For TP5 also capture, without private project content:

```text
MCP process started before first C-Team call: yes/no
C-Team persisted-state scan before first call: yes/no
visible production tool count
serialized production tool definitions size in bytes/chars if reproducible
explicit inactive-project result
```

Raw ids, paths, prompts and rollout contents belong under ignored `.cteam/experiment-007/` only.

Published evidence must use sanitized stable labels such as `root-A`, `face-A`, `ba-A`, `reviewer-A`, `project-A`, `project-inactive`, `pid-1`.

## Named-agent tasks

Use the configured C-Team roles deliberately. Keep every task tiny.

Suggested tasks:

- **Face** — read one known file and report one bounded fact; no edits.
- **B.A.** — make or validate one harmless fixture-only change if an implementation action is necessary; otherwise run a deterministic bounded harness action.
- **Reviewer** — review that tiny fixture/result or independently verify one known invariant.

The objective is not task quality. The objective is to force authentic native subagent creation through our configured roles while spending minimal quota.

Do not invoke Murdock unless the process topology itself becomes surprising enough to need a challenge pass.

## Procedure

### Phase A — baseline root

1. Start one fresh Codex context in the C-Team project with the C-Team plugin installed and enabled.
2. Call the C-Team topology probe from the root.
3. Record root caller metadata and `cteam.exe` PID.

### Phase B — native child fan-out

1. Spawn Face, B.A. and Reviewer using the existing named-agent configuration.
2. Ensure each child performs at least one C-Team MCP tool call.
3. Record PID and caller metadata for every child call.
4. Correlate each child to its parent/root using already-validated persisted metadata where possible.
5. Avoid repeating work or generating large outputs.

### Phase C — second root in the same project

If cheap and supported by the host under test:

1. Open a second independent root Codex context in the same C-Team project.
2. Call the topology probe from that root.
3. Compare its C-Team PID with the first root's PID.

If the host makes this expensive or ambiguous, report TP2 as not established rather than manufacturing a complicated workload.

### Phase D — simultaneous second project

1. Start one minimal Codex context in a different project/workspace.
2. Keep the original project context alive long enough to overlap process intervals.
3. If this second project is used for TP5, ensure it contains **no `.cteam/` marker**.
4. Record whether C-Team is started before any explicit C-Team call.
5. Inspect the MCP/tool inventory with the least expensive host-native mechanism available.
6. Explicitly call the small activation/topology probe once and verify that an inactive project returns `project_not_enabled` without persisted-state scanning.
7. Compare PIDs, caller/workspace metadata and isolation with project A.
8. Confirm there is no cross-project state/result leakage.

### Phase E — cleanup

1. Close/terminate each owning Codex context normally.
2. Observe whether its C-Team MCP child exits.
3. If feasible, also test one abrupt Codex client termination without creating persistent machine configuration.
4. Record any `cteam.exe` process that remains after a bounded grace period.

Do not install a Windows Service, Scheduled Task, login item or other persistent lifecycle mechanism.

## Classification

Finish with exactly one primary process-topology classification:

- **P1 — project-shared**: one C-Team MCP is shared across independent Codex contexts in the same project, while different projects are isolated.
- **P2 — root-tree shared**: a root Codex context and its native subagents share one C-Team MCP, but independent roots get distinct MCP processes.
- **P3 — per-thread/per-agent**: native child/root contexts start distinct C-Team MCP processes.
- **P4 — host-dependent/unclear**: observed grouping is inconsistent, depends materially on host/surface, or cannot be classified safely.

Also report these orthogonal results:

```text
same-project independent roots: shared | isolated | not established
cross-project contexts: shared | isolated | not established
cleanup after normal exit: clean | leaked | not established
cleanup after abrupt exit: clean | leaked | not established
inactive project MCP startup: eager | lazy/not-started | host-dependent | not established
inactive project runtime work: dormant | active | not established
inactive project explicit call: project_not_enabled | other
```

## Decision implications

### If P1

Keep direct stdio MCP. A shared core is low priority.

### If P2

Keep direct stdio MCP. One process per mission/root tree is likely acceptable for the foreseeable product; revisit only when measured shared-state cost justifies it.

### If P3

Do not immediately build a shared core, but elevate the facade/core topology as the preferred future option once C-Team gains non-trivial shared watchers, history, analytics or self-improvement state.

### If P4

Keep the production topology flexible and avoid assumptions about process ownership. Add the observed host/version distinction to the compatibility lab.

### Inactive-project decision

Regardless of P1–P4, production C-Team should treat `.cteam` absence as a hard dormant-state signal unless the user explicitly asks C-Team to initialize that project.

If Codex eagerly starts the MCP globally, startup must remain cheap and perform no heavy observation before project activation is resolved.

If the globally visible tool/skill footprint is material, reduce production tool schemas/instructions or prefer repository-scoped enablement as soon as Codex supports it reliably.

## Future shared-core lifecycle requirement

If C-Team later introduces a shared per-user core, zombie prevention is a **hard requirement**.

The expected design direction is:

```text
Codex-owned stdio MCP facade
          │
          │ local IPC (Windows: likely Named Pipe)
          ▼
   demand-started C-Team core
```

The core must not become a Windows Service or permanently resident login daemon merely because C-Team was installed.

At minimum a future lifecycle spike must prove:

- simultaneous facades race safely to one core;
- clients register live leases/connections;
- dead clients are detected by pipe disconnect, process death and/or bounded lease expiry;
- a core with no clients and no active owned work exits after a short grace period;
- a core that starts but receives no connection exits quickly;
- abrupt Codex/Desktop termination does not leave an indefinite zombie core;
- stale mutex/pipe artifacts recover automatically;
- no administrator privilege is needed;
- **an unactivated project must never start the shared core merely because the MCP facade was launched.**

Do not implement this in Experiment 007.

## Evidence

Publish sanitized evidence to:

```text
docs/evidence/pf3-plugin-mcp-topology.json
experiments/007-plugin-mcp-topology/README.md
```

Reuse deterministic C# tests under the existing experiment harness where useful.

## Retest triggers

Retest when any of these materially changes:

- Codex plugin MCP lifecycle implementation;
- repository-scoped plugin enablement;
- MCP dynamic tool-catalog behavior;
- subagent/thread runtime architecture;
- ChatGPT/Codex Desktop plugin host behavior;
- Codex CLI plugin host behavior;
- MCP connection sharing or daemon support;
- plugin process reuse semantics;
- caller metadata semantics.

## Stop condition

Stop once P1/P2/P3/P4, cleanup/cross-project dimensions, and TP5 inactive-project footprint are supported by sanitized evidence. Do not turn this experiment into the shared-core implementation.