# Experiment 007 — Plugin MCP process topology

## Purpose

Determine the actual Codex plugin MCP process topology before C-Team commits to either a direct-per-context runtime or a shared-core architecture.

The key unanswered question is whether Codex starts a separate plugin MCP process for each native subagent, shares one process across a root thread and its children, or scopes plugin MCP lifetime at some broader project/client boundary.

This experiment is deliberately small and quota-sensitive, but unlike Experiment 006 it **must intentionally create bounded named-agent fan-out because fan-out itself is the subject under test**.

Project activation/context footprint is now a separate Experiment 008. Do not mix `.cteam` activation or tool-catalog design into this experiment unless needed only to keep the topology probe operational.

## Why this matters

Experiment 005 proved that two independent Codex CLI client contexts started two distinct `cteam.exe` MCP processes. It did **not** prove that every spawned Codex subagent gets its own C-Team MCP process.

The difference materially changes the runtime decision:

- one MCP per project or root mission is acceptable for a long time;
- one MCP per subagent/thread makes a thin facade plus shared per-user C-Team core more attractive once history, analytics, watchers and self-improvement arrive;
- a globally shared MCP requires careful caller/project isolation.

Do not build a broker/core based on an unmeasured assumption.

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

Record whether their C-Team MCP calls use the same `cteam.exe` PID as the root, distinct PIDs per child, or another repeatable grouping.

### TP2 — same-project conversation scope

Within the same repository/project, exercise two independent root Codex contexts if this can be done cheaply.

Determine whether they reuse one C-Team MCP process or get distinct processes.

### TP3 — cross-project scope

Exercise two simultaneous Codex contexts rooted in two different projects/workspaces while the same C-Team plugin is installed/enabled.

Determine whether they share or isolate C-Team MCP processes. A tiny fixture project is acceptable.

### TP4 — process cleanup

For every observed C-Team MCP process, record start and exit timing relative to the owning Codex context.

Confirm whether plugin-owned MCP children terminate when their owning root/client exits. If cheap, include one abrupt-owner-exit case.

This experiment concerns Codex-owned stdio MCP children only. Do **not** implement a shared C-Team core yet.

## Instrumentation

Reuse the Experiment 005 NativeAOT MCP harness and plugin package. Extend the existing probe only as needed to emit sanitized process-topology evidence.

Capture at least:

```text
cteam process id
process start timestamp
MCP initialize timestamp
MCP client name/version
caller session_id
caller thread_id
caller plugin id
workspace-map presence/count
agent role/nickname when safely correlated
parent/root thread id when safely derived
tool-call timestamp
process exit timestamp when observable
```

Raw ids, paths, prompts and rollout contents belong under ignored `.cteam/experiment-007/` only.

Published evidence should use labels such as `root-A`, `face-A`, `ba-A`, `reviewer-A`, `root-B`, `project-A`, `project-B`, `pid-1`.

## Named-agent tasks

Keep every task tiny:

- **Face** — one bounded read-only fact plus one C-Team MCP probe call.
- **B.A.** — one bounded fixture/harness action plus one probe call.
- **Reviewer** — one independent verification plus one probe call.

Do not invoke Murdock unless topology itself is surprising enough to warrant a challenge pass.

## Procedure

### Phase A — baseline root

1. Start one fresh Codex context in the C-Team project with the plugin enabled.
2. Call the topology probe from the root.
3. Record caller metadata and PID.

### Phase B — native child fan-out

1. Spawn Face, B.A. and Reviewer.
2. Ensure each child performs at least one topology probe call.
3. Record PID and caller metadata for each child.
4. Correlate each child to its parent/root using existing persisted evidence where possible.

### Phase C — second root in the same project

If cheap:

1. Open a second independent root context in the same project.
2. Call the topology probe.
3. Compare its PID with the first root.

### Phase D — simultaneous second project

1. Start one minimal Codex context in another project/workspace.
2. Keep project A alive long enough to overlap process intervals.
3. Call the topology probe in project B.
4. Compare PIDs and caller/workspace isolation.
5. Confirm no cross-project result leakage.

### Phase E — cleanup

1. Close owning contexts normally and observe child-process exit.
2. If feasible, abruptly terminate one owner and observe cleanup.
3. Record any C-Team process surviving a bounded grace period.

## Classification

Finish with exactly one topology classification:

- **P1 — project-shared**: one C-Team MCP is shared across independent Codex contexts in the same project while different projects are isolated.
- **P2 — root-tree shared**: a root context and its native subagents share one C-Team MCP, but independent roots get distinct processes.
- **P3 — per-thread/per-agent**: native child/root contexts start distinct C-Team MCP processes.
- **P4 — host-dependent/unclear**: grouping is inconsistent, materially host-specific or cannot be classified safely.

Also report:

```text
same-project independent roots: shared | isolated | not established
cross-project contexts: shared | isolated | not established
cleanup after normal exit: clean | leaked | not established
cleanup after abrupt exit: clean | leaked | not established
```

## Decision implications

- **P1** — keep direct stdio; shared core is low priority.
- **P2** — keep direct stdio; one process per root tree is likely acceptable for the foreseeable product.
- **P3** — keep direct stdio initially, but elevate thin-facade + demand-started shared core once shared-state work is material.
- **P4** — keep process boundaries flexible and record the host/version distinction.

## Future shared-core lifecycle requirement

If C-Team later introduces a shared per-user core, zombie prevention is a hard requirement. It must be demand-started, ordinary per-user, race-safe, client-leased, dead-client-aware and idle-stopped. Abrupt Codex/Desktop/CLI termination must not leave an indefinite zombie core. It must never be a Windows Service.

See `docs/runtime-topology.md` for the complete requirements. Do not implement the core in Experiment 007.

## Evidence

Publish:

```text
docs/evidence/pf3-plugin-mcp-topology.json
experiments/007-plugin-mcp-topology/README.md
```

## Retest triggers

- Codex plugin MCP lifecycle changes;
- subagent/thread runtime architecture changes;
- Desktop/CLI plugin host behavior changes;
- MCP process reuse semantics change;
- caller metadata semantics change.

## Stop condition

Stop once P1/P2/P3/P4 and cleanup/cross-project dimensions are supported by sanitized evidence.