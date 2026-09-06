# C-Team Self-Improving Team

> **C-Team makes coding-agent teamwork visible, measurable, and progressively better.**

This document defines the long-term learning loop that gives C-Team a purpose beyond observability. It is a companion to [`architecture-improvements.md`](architecture-improvements.md): that document describes how C-Team observes agent execution; this one describes **why the evidence is collected and how it should improve future delegation**.

Experiment evidence remains authoritative in `EXPERIMENTS.md`. This document is product direction and design guidance, not proof that every proposed learning capability already exists.

## 1. The north star

C-Team should not become only a session viewer, token dashboard, or agent-tree visualization.

The product loop is:

```text
Observe → Measure → Learn → Improve
```

More concretely:

```text
team policy / skills
        │
        ▼
 coding-agent mission
        │
        ▼
 agent delegation
        │
        ▼
  C-Team telemetry
        │
        ▼
   After Action
        │
        ▼
compare with history
        │
        ▼
evidence-backed insight
        │
        ▼
proposed routing / skill change
        │
        ▼
    human review
        │
        ▼
 policy / skill vN+1
        │
        └──────────────► next missions
```

The end goal is not to maximize delegation. It is to learn **when delegation improves the result enough to justify its cost and coordination overhead**.

## 2. C-Team is vendor-neutral by design

Today C-Team is implemented against Codex first, so historically `C` has naturally meant **Codex**.

The durable product meaning should be broader:

> **C-Team = Coding Team**

Codex is the first runtime adapter, not the product boundary. Future adapters may observe or coordinate Claude Code, GitHub Copilot, OpenCode, or other coding-agent runtimes when their telemetry and extension surfaces make that practical.

Do not encode Codex-only concepts into the core learning model. Runtime-specific facts should enter through adapters and normalize into common concepts such as:

```text
Mission
AgentRun
Role
Parent / child relationship
Model / effort
Activity
Usage
Outcome
Review finding
Policy version
Evidence
```

The runtime may change; the question remains the same:

> Was this team composition and delegation strategy effective for this kind of work?

## 3. What C-Team should optimize

A delegation is not successful merely because the subagent completed.

C-Team should eventually evaluate delegation along several dimensions:

```text
quality improvement
risk reduction
first-pass success
rework avoided
defects caught
primary-agent context preserved
wall-clock latency
parallelism benefit
token / quota cost
coordination overhead
failure / retry cost
```

A useful conceptual model is:

```text
Delegation value =
    quality benefit
  + risk reduction
  + context / parallelism benefit
  - token cost
  - latency cost
  - coordination overhead
  - rework / retry cost
```

This is not expected to collapse immediately into one universal numeric score. The first implementation should preserve the evidence needed to compare workflows without pretending that every benefit is directly measurable.

## 4. Why the current telemetry matters

The telemetry collected by C-Team should be chosen because it contributes to future workflow evaluation.

Important fields include:

```text
mission identity
project / task family
parent / child hierarchy
agent role and nickname
requested model
configured / observed model evidence
reasoning effort
agent start / completion / failure
own token usage
descendant / inclusive token usage
wall time
parallel overlap
tool activity
files / components touched
plan changes
tests executed and result
review findings
retries / corrections
human intervention
mission outcome
policy / skill version
```

Without the self-improvement goal, some of this risks becoming vanity analytics. With the learning loop, these fields answer concrete questions such as:

- Did Face reduce Hannibal's discovery/context burden enough to justify another agent?
- Did B.A. produce the implementation successfully with a cheaper model than Hannibal would have used?
- Did Reviewer catch defects that would otherwise have escaped?
- Did Murdock materially change a consequential design decision?
- Did a parallel workflow finish faster in wall-clock time despite higher token consumption?
- Which workflow works best for a recurring task family?

## 5. Keep evidence, insights, and policy separate

C-Team must not let one strange mission rewrite future behavior.

Use a progression with explicit boundaries:

### Evidence

Immutable or reproducible facts from a mission.

```text
Reviewer used 18k tokens.
Reviewer reported two high-severity findings.
One finding caused a code change.
All tests passed after the change.
```

### Observation

A derived statement about one or a few missions.

```text
Reviewer changed the result of this concurrency change.
```

### Insight

A pattern supported by a meaningful comparison set.

```text
Reviewer frequently finds consequential defects in concurrency/lifecycle changes.
```

### Recommendation

A proposed behavior change with evidence and expected effect.

```text
Require Reviewer when concurrency or lifecycle code changes.
```

### Skill / policy

The durable instructions used by future missions.

```text
For concurrency/lifecycle changes, run an independent Reviewer after implementation.
```

The promotion path should therefore be:

```text
Evidence → Observation → Insight → Recommendation → Approved Policy
```

## 6. Skills are procedural memory

Hermes provides a useful distinction:

- **memory** stores small durable facts;
- **skills** store longer procedures that are loaded only when relevant.

C-Team should preserve the same conceptual boundary even if its storage implementation differs.

Examples:

```text
Fact:
  "This repository uses xUnit v3."

Procedure:
  "When changing rollout parsing, add deterministic fixtures for partial lines,
   truncation and open-writer behavior before implementation is considered done."
```

The second belongs in a skill or policy because it changes **how work is performed**.

A future C-Team learning engine should primarily improve procedural knowledge rather than accumulating a giant always-loaded memory file.

## 7. Lessons from Hermes

Hermes explicitly treats skills as agent-managed procedural memory. It can create, patch, edit, or remove skills after successful workflows, recovered failures, or user corrections, and supports a review gate before proposed changes land.

Useful ideas to borrow:

- progressive disclosure: keep the skill index small and load full procedures only when relevant;
- prefer small targeted patches over complete rewrites;
- distinguish facts from procedures;
- allow successful workflows and recovered failures to become reusable skills;
- support staged changes that require human approval;
- keep skill creation/update as a normal capability rather than a special migration process.

C-Team should be **more conservative about promotion** than an individual self-improving agent because C-Team can measure repeated outcomes across missions. A single successful workflow may justify an observation; it should not automatically become a team-wide policy.

Reference:

- https://github.com/NousResearch/Hermes-Agent
- https://github.com/ZQM-Computing/hermes-agent/blob/main/website/docs/user-guide/features/skills.md

## 8. Lessons from Squad

Squad provides a useful repository-backed taxonomy for a durable team brain:

```text
team roster / identity
routing rules
shared decisions
agent charters
agent histories
skills
team wisdom
session logs
```

Conceptually, C-Team can map these to different learning targets:

| Learning target | Intended meaning |
| --- | --- |
| agent identity / charter | stable role and boundaries |
| routing / delegation policy | who should be used for which work |
| agent-specific procedural knowledge | what one role has learned to do well |
| reusable skills | repeatable workflows |
| project wisdom | stable project-specific patterns |
| decisions | architectural choices that future agents should respect |
| mission archive | raw/history evidence, not prompt context |

The most important Squad lesson is that **team identity and routing should not be the same thing**.

For example, B.A. can remain the implementer while routing evolves:

```text
v1:
  Use B.A. for most implementation.

v4:
  Use B.A. directly when scope is localized.
  Use Face first when ownership/location uncertainty is high.
  Escalate implementation only after evidence shows Terra is insufficient.
```

The role remains understandable while the policy improves.

Squad also demonstrates the value of Git-backed team knowledge: anyone cloning the repository can receive the same routing, charters, skills and accumulated decisions.

Reference:

- https://github.com/bradygaster/squad
- https://github.com/bradygaster/squad/blob/dev/docs/src/content/docs/reference/config.md

## 9. Lessons from AGI CLI

`phnx-labs/agi-cli` is a particularly relevant reference because its product promise explicitly connects **measurement** with **changing future agent instructions**:

```text
measure every run
        ↓
inspect insights
        ↓
fold learnings back into AGENTS.md and skills
        ↓
run future work
```

It supports multiple harnesses including Claude, Codex, Grok, OpenCode, Copilot and others, reinforcing the value of keeping C-Team's eventual learning model vendor-neutral.

Its session/insights surface separates several useful analytical views, including:

- harness/model mix;
- token ratios;
- resource/tool frequency;
- raw usage-event queries;
- performance/latency analysis;
- session statistics and historical indexing.

Useful ideas to borrow:

1. **Measure across runtimes with one normalized history.** Runtime adapters differ, while comparison concepts remain common.
2. **Keep raw events queryable.** Derived dashboards should not make the underlying evidence inaccessible.
3. **Separate performance analysis from counter mix.** Latency, token ratios, model selection and tool usage answer different questions.
4. **Generate machine-facing indexes/docs from the real command/tool surface.** AGI CLI generates its command index from the registered CLI tree to reduce documentation drift; C-Team should consider the same principle for MCP tools, policy schemas and skill catalogs.
5. **Track explicit agent/runtime identity in the historical index.** This is required for meaningful comparison across models and harnesses.

Important caution: AGI CLI's `sessions optimize` command is index-maintenance behavior, not evidence that it automatically optimizes agent policy. Its README promise describes a measurement-to-instruction loop, but C-Team should not assume the whole promotion step is automated there.

Another useful warning comes from the ecosystem complexity itself: distributing evolving skills across multiple harnesses can drift. AGI CLI has already had issues around skill edits not reaching the copies machines actually consume. If C-Team later supports several coding-agent runtimes, it needs **one canonical policy/skill source plus explicit adapters/synchronization**, rather than several independently editable copies.

Reference:

- https://github.com/phnx-labs/agi-cli
- https://github.com/phnx-labs/agi-cli/blob/main/cli/docs/command-index.md
- https://github.com/phnx-labs/agi-cli/issues/2927

## 10. What C-Team adds to these ideas

Hermes, Squad and AGI CLI each demonstrate useful parts of the loop:

```text
Hermes
  procedural memory that can evolve

Squad
  durable repo-backed team knowledge and routing

AGI CLI
  multi-runtime measurement and an explicit measure→learn→instructions story
```

C-Team's intended contribution is to make policy evolution **telemetry-driven and attributable to delegation outcomes**.

Instead of only recording:

```text
"This workflow felt useful."
```

C-Team should eventually be able to propose:

```text
Candidate change:
  Skip Face for localized implementation tasks.

Evidence:
  18 comparable missions
  median tokens: -28%
  median wall time: -14%
  first-pass review success: unchanged
  rework rate: unchanged

Target:
  delegation-policy skill

Confidence:
  medium
```

This should be the defining difference between **agent memory** and **team learning based on measured execution**.

## 11. Version policies and skills as experiments

Every mission that may be used for comparison should eventually record the effective team instructions that produced it.

At minimum preserve identifiers such as:

```text
routing policy version
skill versions / content hashes
AGENTS.md or equivalent commit SHA
agent charter versions
model-routing configuration
C-Team version
runtime / harness version
```

Then C-Team can compare policy generations:

```text
Delegation policy v3
  missions: 43
  median tokens: 126k
  first-pass success: 83%

Delegation policy v4
  missions: 37
  median tokens: 94k
  first-pass success: 88%
```

Git already provides an excellent versioning and review mechanism for project-local policies. C-Team should record the linkage between mission evidence and the relevant Git/content version rather than inventing a second opaque version-history system.

## 12. Policy scope matters

A learning should be promoted only to the narrowest scope justified by evidence.

Possible scopes:

```text
global / cross-project
runtime-specific
project
repository component
task family
agent role
specific skill
```

Examples:

```text
Global:
  Reviewer rarely adds value to trivial formatting-only changes.

Project-specific:
  In C-Team, changes to rollout parsing require deterministic persistence fixtures.

Task-family:
  For localized .NET implementation, Hannibal → B.A. performs better than Face → Hannibal → B.A.

Role-specific:
  Face should return file paths and evidence rather than architecture recommendations.
```

Do not generalize a project-specific pattern into a global routing policy without cross-project evidence.

## 13. Role-effectiveness questions

C-Team's named roles should eventually have explicit evaluation questions.

### Hannibal

- Did planning reduce rework?
- Was context retained rather than consumed by implementation details?
- Did Hannibal delegate too much or too little?
- How much coordination overhead did the workflow add?

### Face

- Did discovery identify the correct files/components?
- Did Face reduce Hannibal's search/context load?
- Was discovery information reused by downstream agents?
- Would Hannibal alone have completed faster/cheaper?

### B.A.

- Did implementation pass tests/review first time?
- How much rework was required?
- Was the cheaper implementation model sufficient?
- Did delegation keep primary-agent context smaller?

### Reviewer

- How many consequential findings were made?
- Which findings changed code?
- How many findings were false positives or low-value style comments?
- Which task families actually benefit from review?

### Murdock

- Did a challenge cause Hannibal to revise a consequential decision?
- Was the revision later validated by implementation/review outcomes?
- Was Murdock invoked only where architecture uncertainty justified the extra cost?

These questions are more useful than a generic per-agent leaderboard.

## 14. Outcome and task-family classification

Comparisons are meaningless if unlike work is mixed together.

C-Team should eventually classify missions into coarse task families such as:

```text
localized bug fix
feature implementation
repository discovery
architecture / design
refactoring
protocol / integration
concurrency / lifecycle
security-sensitive change
documentation
review-only
research / investigation
```

Task-family classification should begin conservatively. Prefer explicit labels or deterministic signals when available; inferred labels should carry provenance/confidence.

Outcome should also separate dimensions:

```text
completed / failed / abandoned
tests passed / failed / unavailable
review passed / findings / not run
user correction required
rework count
final accepted result
```

Do not reduce success to `process exited 0`.

## 15. Promotion guardrails

Initial C-Team learning must be **recommendation-first**, not autonomous self-modification.

A skill or routing change should require:

```text
sufficient comparable evidence
clear target scope
expected benefit
known trade-offs
provenance to supporting missions
confidence level
a reviewable patch
human approval
```

Suggested progression:

```text
Phase 1  Observe only
Phase 2  Compare and surface insights
Phase 3  Propose policy / skill patches
Phase 4  User-approved application
Phase 5  Optional automatic application of narrowly-scoped, high-confidence, low-risk changes
```

Do not jump directly from one mission to automatic skill rewriting.

## 16. Recommendation shape

A recommendation should be inspectable and reversible.

Example:

```text
Recommendation

Target:
  skills/delegation-policy/SKILL.md

Current behavior:
  Use Face before implementation when repository discovery is useful.

Proposed behavior:
  Skip Face when the initiating context already identifies the target component
  and no ownership/architecture uncertainty remains.

Evidence:
  14 comparable missions

Observed effect:
  token use       -31%
  wall time       -18%
  review pass     unchanged
  regression rate unchanged

Confidence:
  medium

Scope:
  project + localized implementation task family

Actions:
  Review patch / Apply / Ignore / Suppress similar suggestion
```

The system should retain the recommendation and decision so future learning knows whether a suggestion was accepted, rejected, or considered irrelevant.

## 17. Avoid memory bloat

Raw telemetry, historical evidence and durable procedural knowledge have different lifecycles.

Do not implement learning by appending everything forever to `AGENTS.md` or an agent `history.md`.

Prefer:

```text
raw mission evidence
    ↓
queryable historical store

repeated observations
    ↓
insight records

stable reusable procedure
    ↓
small skill / policy
```

Long procedures should be loaded on demand through skills. Always-loaded team instructions should remain concise.

Historical material may be archived without being part of model context.

## 18. Canonical skill source and cross-runtime adapters

When C-Team is Codex-only, a project skill can live naturally in the format Codex consumes.

If C-Team later supports multiple runtimes, do **not** let each runtime copy become an independent source of truth.

Preferred conceptual shape:

```text
Canonical C-Team policy / skills
             │
    ┌────────┼─────────┐
    ▼        ▼         ▼
  Codex    Claude    Copilot
 adapter   adapter    adapter
```

Each mission should record both:

```text
canonical policy version
runtime-specific rendered/synced version
```

This lets C-Team detect drift and distinguish a policy failure from a synchronization failure.

Cross-runtime skill portability should prefer standards such as Agent Skills / `SKILL.md` where the target runtimes support them, but adapters must remain explicit because discovery paths, metadata and execution semantics can differ.

## 19. The C-Team learning data model

A future normalized history should preserve enough information for comparison without coupling analytics to one runtime's raw records.

Possible entities:

```text
Mission
  id
  project
  taskFamily
  runtime
  runtimeVersion
  policyVersion
  outcome

AgentRun
  missionId
  parentAgentRunId
  role
  model evidence
  effort
  start / end
  own usage
  inclusive usage
  outcome

WorkflowLeg
  type: discovery / implementation / challenge / review / retry
  agentRunId
  predecessor / successor
  result

Validation
  tests
  review
  human correction

PolicySnapshot
  routing hash
  skill hashes
  charter hashes
  relevant instruction commit

Insight
  scope
  evidence set
  metric comparison
  confidence

Recommendation
  insight
  target file / policy
  proposed patch
  decision
```

This is intentionally more general than the current Codex rollout schema.

## 20. Evaluation before causality claims

C-Team should be careful with statements such as:

```text
"Reviewer prevented a bug."
```

Observational telemetry often cannot prove the counterfactual.

Prefer wording such as:

```text
Reviewer reported a finding that caused a code change before acceptance.
```

Over many missions, comparative evidence can support stronger recommendations, but provenance and uncertainty should remain visible.

Later experiments may intentionally compare alternative workflows to improve causal confidence, but normal user work should not be duplicated merely to manufacture benchmark data.

## 21. Learning should not distort normal work

The monitoring system must not create unnecessary agents or work just to collect telemetry.

This is particularly important because recent quota-sensitive C-Team experiments correctly avoided named-agent fan-out when delegation did not help the experiment itself.

That should not be confused with the product goal.

The principle is:

> **Observe natural delegation, and optimize it over time. Do not force delegation for the sake of the dashboard.**

For deliberate workflow experiments, the user may choose to compare policies/models, but these campaigns should be explicit, bounded and separated from ordinary development telemetry.

## 22. After Action is the bridge between telemetry and learning

The eventual After Action view should answer more than "what happened?"

Useful sections include:

```text
Outcome
Workflow / delegation tree
Agent contributions
Own vs inclusive usage
Parallelism and timing
Tests / validation
Review findings
Retries / rework
Policy / skill version
Comparison with similar missions
Potential insights
```

The first versions can be descriptive. Recommendation generation can be added only after there is enough history to avoid noisy conclusions.

## 23. Suggested implementation sequence

The self-improvement feature should not delay the current observability MVP, but the MVP must preserve the data needed by it.

Recommended sequence:

### Stage A — observable missions

- reliable current-mission identity;
- normalized agent tree;
- role/model/effort evidence;
- timing and usage;
- plan/activity/test/review evidence where available.

### Stage B — durable comparable history

- normalized C-Team history store;
- task-family/outcome metadata;
- policy/skill snapshots;
- historical queries and After Action.

### Stage C — descriptive analytics

- workflow distribution;
- model/role usage;
- own vs inclusive tokens;
- latency and parallelism;
- review/rework outcomes.

### Stage D — insights

- comparable mission cohorts;
- differences by workflow/policy/model;
- confidence and minimum-evidence rules.

### Stage E — proposed learning

- recommendation records;
- reviewable skill/routing patches;
- human approval and Git commit integration.

### Stage F — controlled adaptation

- optional automatic application for high-confidence, low-risk, narrowly-scoped changes;
- rollback and policy-version comparison.

## 24. Non-goals for the near term

Do not let this document expand the current implementation prematurely.

Not required for the initial product:

- autonomous rewriting of skills;
- generic memory system;
- automatic model routing;
- arbitrary multi-vendor orchestration;
- causal benchmark campaigns on every task;
- cloud learning service;
- universal delegation score;
- fully autonomous policy deployment.

The immediate responsibility is simpler:

> **Collect trustworthy, attributable telemetry so those capabilities are possible later.**

## 25. Product test

When considering a new telemetry field or feature, ask:

> Will this help us understand whether a coding-agent team delegated effectively or help the team perform better next time?

If the answer is no, it may still be useful, but it should not distract from C-Team's core direction.

## 26. Working product statement

External tagline:

> **See your Codex team at work.**

This remains appropriate while Codex is the first supported runtime.

Internal long-term product statement:

> **C-Team makes coding-agent teamwork visible, measurable, and progressively better.**

Durable interpretation of the name:

> **C-Team = Coding Team.**

Codex is first. The learning model should remain open to Claude, Copilot and other coding-agent runtimes without weakening the initial focus.
