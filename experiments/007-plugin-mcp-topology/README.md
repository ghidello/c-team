# Experiment 007 — Plugin MCP process topology

## Purpose

Determine whether Codex reuses the C-Team plugin MCP process across native subagents, independent roots in one project, and simultaneous different project/context roots.

## Original environment

Executed 2026-09-06 on Windows 10.0.26220 with Codex CLI 0.153.4, .NET SDK 10.0.400, ChatGPT Desktop, and installed C-Team plugin `0.1.0+codex.20260906191756`. Desktop did not expose a product version through the inspected process/package metadata. The primary persisted turn reported model `gpt-5.6-sol` and high effort as configured evidence only.

Experiments 005 and 006 were treated as authoritative. The existing .NET 10 `win-x64` NativeAOT stdio MCP harness, approval behavior, per-call caller metadata, and exact bounded caller correlation were reused without repeating their paid workloads.

## Hypothesis

Codex may scope one C-Team MCP process to a project, a root tree, or each thread/agent. Distinct process ids paired with exact caller identity can distinguish those scopes without building a shared core.

## Procedure

1. From the active Desktop root, pair `cteam_runtime_info` with the no-argument current-mission probe to record MCP process identity and exact caller kind.
2. Run the required native Face, B.A., and Reviewer children. Give each one tiny bounded work and the same runtime/probe pair.
3. Start a second independent Desktop root directly in the saved C-Team project.
4. Start a projectless Desktop context nine seconds later. Hold both roots in `cteam_ping` for 15 seconds so their MCP process intervals overlap.
5. Archive both bounded root tasks and check their observed C-Team processes after five seconds.
6. Start one disposable Codex CLI owner with private MCP evidence enabled, verify its C-Team child is alive, terminate only that owner forcibly, and check for an orphan after five seconds.
7. Keep actual PIDs, task ids, prompts, paths, command output, and MCP envelopes under ignored `.cteam/experiment-007/`; publish only role/root and `pid-N` aliases.

No shared core, broker, pipe protocol, daemon, service, database, UI, activation logic, or second HTTP/WebSocket API was implemented.

## Success criteria

- The active root and all three named native children produce usable process observations.
- A second same-project root distinguishes project sharing from root/thread isolation.
- Simultaneous different contexts distinguish cross-project sharing from isolation.
- Completed owners are checked for normal cleanup, and one bounded abrupt-owner exit is checked if feasible.
- Caller and process observations remain sanitized and do not rely on cwd as identity.

## Observed result

### TP1 — Native subagent scope

The active root used `pid-1`. Face, Reviewer, and B.A. used `pid-2`, `pid-3`, and `pid-4` respectively. All four process ids and start timestamps were distinct. Face and Reviewer resolved as exact children with distinct child mission keys and a derived root. B.A. made the required calls and supplied an unambiguous distinct PID, but its mission probe returned `not-found`; this correlation-side miss is preserved and does not change the process observation.

The result rules out one MCP shared by the native root tree on this Desktop version. A completed agent turn alone did not consistently close its reusable agent context: B.A.'s MCP process exited, while Face and Reviewer processes remained alive during the bounded post-turn check. They are not labeled leaks because the parent still retained those reusable agent contexts and no explicit agent-dispose operation was available.

### TP2 — Same-project independent roots

A second independent Desktop root ran directly in the same saved C-Team project and used `pid-5`, distinct from the active root's `pid-1`. Ping and runtime calls agreed on `pid-5`; the no-argument mission probe resolved one exact root with no truncation.

**Same-project independent roots: isolated.**

### TP3 — Simultaneous different contexts

The same-project root and a projectless Desktop context used `pid-5` and `pid-6`. Both held their ping for 15 seconds, their MCP start times were about 9.04 seconds apart, and a single process snapshot confirmed both alive simultaneously before archival. Both runtime calls agreed with their ping PIDs, and both mission probes resolved exact roots with no truncation. Their raw root ids were distinct; Experiment 006's exact join therefore identifies distinct missions without relying on cwd.

**Cross-project contexts: isolated.** No shared PID or cross-context mission result was observed.

### TP4 — Process cleanup

After the two bounded Desktop roots completed and were archived, both `pid-5` and `pid-6` were gone within the five-second check window.

**Cleanup after normal exit: clean.** This directly covers the two additional Desktop roots and reuses Experiment 005's clean CLI-owner exits. The still-active primary root and retained reusable native-agent contexts were not treated as exited owners.

The first abrupt-test launcher failed before Codex or MCP startup because `Start-Process` split a spaced prompt into arguments. The corrected attempt supplied the prompt through redirected stdin. Its disposable owner and `pid-7` were both alive before forced owner termination; `pid-7` was gone five seconds later.

**Cleanup after abrupt exit: clean.** No orphan was observed after the bounded grace period.

## Current status

**P3 — per-thread/per-agent.** Every tested native child and independent root received a distinct C-Team MCP process. Same-project roots and simultaneous different contexts were also isolated.

A thin facade plus a demand-started shared C-Team core becomes the preferred future direction once shared-state cost is real. That shared core remains deferred; Experiment 007 implemented none of it.

## Evidence references

- [`docs/evidence/pf3-plugin-mcp-topology.json`](../../docs/evidence/pf3-plugin-mcp-topology.json)
- [`experiments/005-plugin-mcp-runtime`](../005-plugin-mcp-runtime)
- [`experiments/006-caller-mission-correlation`](../006-caller-mission-correlation)
- [`experiments/CTeam.Experiments`](../CTeam.Experiments)

## Known limitations

Pairing runtime information with caller correlation assumes the two sequential calls stay on one stdio connection; the mission result does not repeat the PID. All observed pairs were internally consistent where ping also returned a PID. Workspace-map details and exact tool-call timestamps were not returned by the current tools and remain unavailable or reused from Experiment 005 rather than newly measured. B.A.'s caller lookup miss was not investigated with extra paid work because PID topology was already unambiguous. Archiving is the tested normal Desktop-owner cleanup boundary; simply completing a turn may retain a reusable task or agent context.

## Retest trigger

Retest when Codex changes plugin MCP lifecycle, subagent/thread runtime architecture, Desktop/CLI hosting, task archival semantics, per-call metadata, or process reuse behavior.
