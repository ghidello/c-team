# C-Team — Codex Observability Spike

## Goal

Validate whether C-Team can reliably observe native Codex multi-agent execution before investing in persistence, MCP, Apps SDK UI, analytics, or orchestration.

The spike should be small, disposable where appropriate, and evidence-driven.

Read `MODELS.md` as part of the spike brief. Sol/Terra/Luna are the controlled initial dogfooding policy, not a fixed C-Team model taxonomy.

## Critical questions

### CQ1 — Agent hierarchy

Can Codex app-server expose enough structured information to reconstruct the parent/child agent tree without parsing assistant prose?

Investigate and record the behavior of fields/events related to:

- session id;
- thread id;
- parent thread id;
- agent role;
- agent nickname;
- subagent source metadata;
- spawn depth.

Expected conceptual tree:

```text
HANNIBAL · planner
├─ FACE · explorer
├─ B.A. · implementer
└─ REVIEWER
```

### CQ2 — Effective model

Can C-Team determine the model that **actually executed** each agent?

Keep separate:

- configured/requested model;
- effective model.

Also determine whether these are observable:

- reasoning effort;
- service tier;
- inheritance;
- rerouting/fallback.

Document the exact source of every value.

Do not infer an effective model merely from the custom agent configuration.

### CQ3 — Per-agent token usage

Determine whether C-Team can accurately observe, per agent/thread:

- input tokens;
- cached input tokens;
- cache-write input tokens;
- output tokens;
- reasoning output tokens;
- total tokens;
- model context window.

Establish whether reported values are deltas, cumulative counters, or both.

Do not accidentally aggregate cumulative counters as deltas.

### CQ4 — Lifecycle and timing

Determine reliable state transitions for:

- created;
- waiting;
- running;
- completed;
- failed;
- interrupted.

Calculate at minimum:

- started timestamp;
- completed timestamp;
- wall duration.

Record which protocol events define those boundaries.

### CQ5 — Structured plan

Determine whether current Codex events are sufficient to render live plan progress such as:

```text
✓ Inspect implementation
✓ Design approach
● Implement
○ Test
○ Review
```

Capture step text, status, and explanation where available.

### CQ6 — Tool activity

Determine what can be measured without parsing terminal prose.

Investigate structured events/items for:

- commands;
- command duration;
- command result/status;
- file changes;
- MCP/tool calls;
- failures;
- approvals;
- tests where identifiable.

Perfect test classification is not required for this spike.

### CQ7 — Diff metadata

Determine what can be derived from Codex diff updates.

At minimum investigate whether C-Team can calculate:

- files changed;
- lines added;
- lines removed.

Do not persist source-code diffs beyond raw diagnostic recordings unless needed to answer the question.

### CQ8 — Review agents

Determine how review work appears in the thread hierarchy.

Test:

- a custom Sol reviewer subagent;
- native/detached Codex review if available.

Document how C-Team should represent these.

### CQ9 — Replay

Can a recorded stream of app-server messages reproduce the same final C-Team state without Codex running?

Replay is a required acceptance criterion.

Live and replay ingestion must share the same state-building path.

### CQ10 — Existing ChatGPT Desktop Codex session

This is the most important architectural question.

Determine whether an external C-Team process can observe the Codex app-server/session already owned by the ChatGPT Desktop Codex experience using a supported or reasonably stable mechanism.

Possible outcomes:

#### A — Direct supported attachment

Best case.

```text
ChatGPT Desktop
        │
Codex app-server
        │
   ┌────┴────┐
ChatGPT    C-Team
```

#### B — Shared/local app-server

A shared daemon/socket or similar local transport can be used by both.

#### C — Persisted-state observation

C-Team cannot join the live server but can reconstruct useful state from persisted Codex state with reduced real-time fidelity.

#### D — C-Team must own app-server

C-Team must launch Codex itself to obtain full fidelity.

The spike must produce a clear conclusion for CQ10 even if the conclusion is “not currently supported.”

### CQ11 — Model catalog and quota identity

Can C-Team discover the models currently available to the signed-in Codex user/account and associate actual agent execution with the correct model and usage/rate-limit identity?

Investigate and document:

- how the current app-server exposes the model catalog;
- model identifiers and display names;
- supported reasoning-effort values and relevant capabilities;
- whether the catalog reflects the actual signed-in account/product surface;
- configured/requested model vs effective model per thread/turn;
- inheritance, explicit overrides, rerouting, and fallback where observable;
- model context-window information;
- rate-limit/quota buckets and whether they can be associated with a model/model family;
- whether models with separate limits, such as GPT-5.3-Codex-Spark when available, can be distinguished in account/rate-limit telemetry;
- how additional models such as GPT-5.5, legacy/API-key-only models, and future models should be represented without code changes.

Do not hard-code a fixed list of Codex models into the C-Team domain.

Record CQ11 as one of:

- **Full** — catalog, effective model, and quota identity are observable;
- **Partial** — catalog/effective model are observable but quota identity is incomplete;
- **Minimal** — only configured/requested model can be determined reliably.

See `MODELS.md` for the broader model strategy and post-spike comparison ideas.

## Explicit non-goals

Do not implement:

- SQLite;
- permanent history;
- cloud backend;
- authentication;
- React;
- Apps SDK widget;
- analytics dashboard;
- model-routing engine;
- agent steering;
- cancellation UI;
- worktree management;
- issue tracking;
- own coding agent;
- own planner;
- production-grade configuration;
- polished TUI.

## Technology

Use:

```text
.NET 10
C#
System.Text.Json
System.Diagnostics.Process
xUnit v3
```

NativeAOT compatibility is desirable but is not a blocking requirement for the spike.

Do not introduce EF Core.

Do not introduce a large/general-purpose messaging framework unless direct protocol handling proves unreasonable.

## Codex protocol integration

Use the current Codex app-server and current v2 protocol where available.

Prefer schemas generated from the installed Codex version when useful so the spike matches the exact local version.

Primary transport for the spike:

```text
stdio
```

Launch the current documented equivalent of:

```text
codex app-server --listen stdio://
```

Use JSON-RPC-like JSONL over stdin/stdout.

Do not use experimental WebSocket transport unless required for a specific experiment.

## Initialization

Implement the required app-server initialization handshake.

Identify the client approximately as:

```text
name: cteam
title: C-Team
version: spike
```

Enable experimental capabilities only when needed and record which experimental APIs were required.

Record the Codex CLI/app-server version and **complete current model catalog** during the run. Do not assume the model names configured in `.codex/agents/` are the only available models.

## Suggested structure

```text
src/
  CTeam.Spike/
    Program.cs

    Codex/
      CodexProcess.cs
      AppServerConnection.cs
      RpcRequest.cs
      RpcResponse.cs
      RpcNotification.cs
      Protocol/
      CodexEventMapper.cs

    Domain/
      MissionState.cs
      AgentState.cs
      TurnState.cs
      TokenUsage.cs
      PlanStep.cs

    Recording/
      ProtocolRecorder.cs
      ProtocolReplay.cs

    Terminal/
      MissionRenderer.cs

tests/
  CTeam.Spike.Tests/
```

Avoid excessive project decomposition.

## Protocol boundary

Do not make C-Team domain types depend directly on Codex protocol DTOs.

Use a boundary like:

```text
Codex protocol
      ↓
CodexEventMapper
      ↓
normalized C-Team state/events
```

Codex protocol evolution must not leak through the whole codebase.

The same rule applies to models: protocol-specific model catalog DTOs must not become a fixed C-Team model enum.

## Event recording

Record every incoming and outgoing app-server message during the spike.

Suggested path:

```text
.cteam/
  recordings/
    <timestamp>-<session>.jsonl
```

Each record should contain:

- timestamp;
- direction;
- raw message.

This recording may contain sensitive Codex/session data. Mark it development-only and ignore it from source control by default.

## Replay

Implement an equivalent of:

```text
cteam replay <recording>
```

Replay recorded protocol messages through exactly the same event/state mapper used by live observation.

Do not maintain separate aggregation logic for replay.

## Minimal state model

A small in-memory model is sufficient.

Conceptually:

```text
MissionState
  RootThreadId
  SessionId
  StartedAt
  Status
  Agents[]

AgentState
  ThreadId
  ParentThreadId
  SessionId

  Role
  Nickname

  RequestedModel
  EffectiveModel
  ReasoningEffort
  ServiceTier

  StartedAt
  CompletedAt
  Status

  Usage

  Commands
  ToolCalls
  FileChanges

  Plan[]

  Children[]
```

Model identifiers should be strings/value objects derived from Codex telemetry, not a fixed Sol/Terra/Luna enum.

Change this model when protocol evidence requires it.

## Terminal output

Render a live textual hierarchy.

Target shape:

```text
C-TEAM
I love it when a Codex plan comes together.

Mission: Implement cancellation support

HANNIBAL · planner                  ✓   18.2s      94k
├─ FACE · explorer                  ✓    5.1s      31k
├─ B.A. · implementer               ●   28.7s     184k
└─ REVIEWER                         ○      —         —

Plan
✓ Inspect existing implementation
✓ Design approach
● Implement
○ Test
○ Review

Usage
Input                                  277,813
Cached                                 246,991
Output                                  24,102
Reasoning                                8,817
Total                                  310,732

Agents                                       4
Wall time                                34.1s
```

Formatting is secondary to correctness.

## Controlled experiment

Use a very small fixture repository or a deliberately trivial part of C-Team itself.

Configure the initial controlled baseline:

```text
main session: Sol
Face: Luna
B.A.: Terra
Reviewer: Sol
Murdock: Sol
```

Give Sol a task complex enough to justify delegation but cheap enough to repeat.

Example:

```text
Inspect the fixture repository.
Plan a small implementation.
Delegate repository exploration to Face.
Delegate implementation to B.A.
After implementation, delegate an independent review to Reviewer.
Resolve blocking review findings.
```

For a separate architecture experiment, deliberately trigger Murdock:

```text
Before finalizing the architecture, ask Murdock to challenge the plan,
propose materially different approaches, and identify hidden assumptions.
Hannibal must then respond and explicitly keep, revise, or reject the challenge.
```

Run the controlled experiment multiple times.

Do not introduce Spark/GPT-5.5/etc. into the baseline merely to exercise them. Once telemetry correctness is established, model comparisons become a separate post-spike experiment described in `MODELS.md`.

## Dogfooding

Use the same delegation setup to build the spike itself.

Main agent:

```text
Hannibal / Sol
```

Supporting agents:

```text
Face       → Luna   → exploration
B.A.       → Terra  → implementation
Murdock    → Sol    → adversarial/lateral analysis
Reviewer   → Sol    → independent review
```

These are initial policy choices, not C-Team product constraints.

Unexpected delegation/model behavior is product evidence. Record it.

## Minimal Codex plugin shell

The repository should contain the smallest valid local Codex plugin so the project is plugin-shaped from the beginning.

Suggested structure:

```text
.codex-plugin/
  plugin.json

skills/
  inspect-codex-run/
    SKILL.md
```

The plugin should initially do almost nothing beyond proving that:

1. C-Team can be installed locally as a Codex plugin;
2. Codex discovers the C-Team skill;
3. the skill can invoke or direct the local spike executable;
4. the workflow is reasonable inside Codex.

Do not build the Apps SDK UI in this spike.

## Plugin validation

Validate the local Codex plugin independently from telemetry.

Test:

- manifest discovery;
- local installation;
- skill discovery;
- skill invocation;
- invocation from a normal Codex task;
- refresh/reinstall behavior.

Document the exact steps that work with the installed Codex version rather than relying on stale docs.

## Deferred Apps SDK feasibility experiment

Only after the protocol questions are answered, perform a separate minimal UI experiment using fake data.

Build one widget that can render:

```text
HANNIBAL
├─ FACE
├─ B.A.
└─ REVIEWER
```

Show only:

- model;
- state;
- duration;
- tokens.

No real Codex connection is necessary for the first widget experiment.

Purpose: determine whether C-Team feels good inside ChatGPT/Codex before building production UI.

## Spike acceptance criteria

The spike succeeds if it provides evidence for all critical questions.

Minimum technical acceptance:

1. C-Team starts or connects to Codex app-server.
2. Initialization succeeds.
3. A controlled multi-agent Codex task can be executed.
4. Subagent threads are detected.
5. Parent/child hierarchy is correct.
6. Role is captured.
7. Effective model is determined, or the exact gap is documented.
8. Reasoning effort is determined, or the exact gap is documented.
9. Per-agent token usage is captured.
10. Agent duration is captured.
11. Structured plan updates are captured.
12. Tool/file activity is captured at a useful aggregate level.
13. Review threads can be identified.
14. Protocol traffic is recorded.
15. Replay reproduces the same final state.
16. A minimal C-Team Codex plugin can be installed locally.
17. A C-Team skill can be invoked from Codex.
18. CQ10 has a definitive documented conclusion about observing ChatGPT Desktop-owned Codex sessions.
19. The current model catalog is enumerated dynamically rather than assumed from configuration.
20. CQ11 has a documented Full / Partial / Minimal conclusion for model catalog, effective model, and quota identity.

## Deliverables

Produce:

```text
README.md
docs/spike-findings.md
docs/codex-protocol.md
docs/desktop-observation.md

working C-Team spike executable
recorded sanitized example run
tests
minimal Codex plugin
```

`docs/spike-findings.md` should use this structure for every critical question, including CQ11:

```text
Question
Finding
Evidence
Confidence
Impact on C-Team architecture
Remaining uncertainty
```

## Decision gate

Do not automatically continue into the production architecture.

At the end of the spike, recommend exactly one of:

### Architecture A — Passive observer

Observe ChatGPT Desktop Codex directly.

### Architecture B — Persisted-state observer

Observe persisted Codex state with reduced live fidelity.

### Architecture C — C-Team-owned Codex runtime

C-Team launches/owns app-server to obtain complete telemetry.

### Architecture D — Hybrid

Use multiple sources based on available fidelity.

Explain the trade-offs and evidence.

## Guiding principle

The spike exists to invalidate assumptions.

Prefer a small experiment that proves something over production-quality code that assumes it.
