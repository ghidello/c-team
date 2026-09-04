# C-Team

**Working tagline:** See your Codex team at work.

**Optional completion easter egg:** “I love it when a Codex plan comes together.”

## Purpose

C-Team is an observability companion for Codex multi-agent execution.

The long-term product should make it easy to understand:

- which Codex agents ran;
- the parent/child agent hierarchy;
- each agent's role and nickname;
- the requested and effective model;
- the current Codex model catalog and capabilities;
- reasoning effort and service tier where available;
- token usage, cache reuse, and quota/rate-limit identity where observable;
- lifecycle and wall-clock timing;
- plan progress;
- tool, command, file-change and review activity;
- parallelism and fan-out;
- where usage was spent and whether model routing was efficient.

C-Team is **not** initially another coding agent, IDE, task tracker, or orchestration platform.

## Product boundary

Codex owns:

- planning;
- coding;
- subagent delegation;
- model execution;
- repository interaction;
- reviews.

C-Team owns:

- observation;
- normalization;
- metrics;
- agent hierarchy;
- history;
- comparison and analysis;
- later, possibly control and steering.

## Model strategy

C-Team must treat models as a **dynamic Codex capability**, not as a fixed enum or a permanent Sol/Terra/Luna taxonomy.

The first spike intentionally uses a controlled baseline:

```text
Hannibal   → Sol
Murdock    → Sol
Face       → Luna
B.A.       → Terra
Reviewer   → Sol
```

Those assignments are a dogfooding/routing policy only. C-Team's core model, protocol adapters, future persistence, UI, and analytics must remain generic enough to represent any model the current Codex installation/account exposes, including models such as GPT-5.5, GPT-5.3-Codex-Spark, legacy/API-key-only models, and future models.

See `MODELS.md` for the detailed policy, CQ11, and future comparison experiments.

## Product direction

The likely long-term shape is:

```text
ChatGPT / Codex
      │
   C-Team plugin/app
      │
      MCP
      │
 C-Team local service
      │
 Codex app-server / telemetry sources
      │
    SQLite
```

However, the first milestone is **not** the final plugin UI. The spike must first determine what Codex actually exposes and how reliably we can observe it.

## First milestone

Build a minimal console spike that can observe a controlled multi-agent Codex run and render something like:

```text
C-TEAM

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
```

Correctness matters more than presentation.

## Agent personas

The A-Team-inspired names are UI/personality labels. Technical roles remain explicit.

### Hannibal — Planner / Thinker

Initial model policy: Sol.

Owns:

- understanding the mission;
- architecture;
- decomposition;
- hard reasoning;
- resolving ambiguity;
- final decisions.

### Murdock — Challenger / Lateral Thinker

Initial model policy: Sol.

Used only for complex or consequential analysis.

Purpose:

- challenge Hannibal's assumptions;
- reframe the problem;
- propose materially different approaches;
- surface hidden risks;
- push back on premature convergence.

Murdock is not a normal code reviewer.

### Face — Explorer / Investigator

Initial model policy: Luna.

Purpose:

- repository reconnaissance;
- locate code and configuration;
- trace dependencies and execution paths;
- gather evidence;
- inspect logs/build output;
- report concise findings.

Prefer read-only behavior.

### B.A. — Implementer

Initial model policy: Terra.

Purpose:

- normal feature implementation;
- bug fixes with a clear failure mode;
- refactoring;
- integration work;
- tests;
- focused code changes following the approved plan.

### Reviewer — Independent reviewer

Initial model policy: Sol.

This is intentionally distinct from Hannibal.

Purpose:

- independently inspect consequential implementation;
- find correctness, architectural, security, lifecycle, compatibility, and test gaps;
- report concrete findings ordered by severity.

## Preferred reasoning workflows

### Normal implementation

```text
Hannibal → B.A. → Reviewer (when consequential)
```

### Discovery-heavy task

```text
Face → Hannibal → B.A. → Reviewer
```

### Complex architecture / analysis

```text
Face (if needed)
      ↓
Hannibal proposal
      ↓
Murdock challenge
      ↓
Hannibal response / revised decision
      ↓
B.A.
      ↓
Reviewer
```

Do not invoke Murdock for trivial decisions.

## Product terminology

Use:

- **C-Team** — product;
- **Mission** — a top-level Codex task/run;
- **Agent** — an individual Codex/subagent thread;
- **Plan** — structured Codex plan;
- **After Action** — completed-run analysis.

## Design principle

The spike exists to invalidate assumptions.

Prefer a small experiment that proves something over production-quality code that assumes it.
