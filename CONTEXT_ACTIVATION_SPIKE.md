# Experiment 008 — Project activation and MCP context footprint

## Purpose

Determine the smallest practical globally installed C-Team plugin surface while keeping unrelated projects dormant and preserving a smooth opt-in path for projects that contain a `.cteam/` marker.

This experiment is separate from Experiment 007. Experiment 007 measures **process topology**. Experiment 008 measures **activation, tool discovery, context footprint, and marker transition behavior**.

## Questions

1. Can the plugin MCP remain globally installed while doing essentially no runtime work in a project without `.cteam/`?
2. What C-Team tool definitions/instructions are visible to Codex before any C-Team tool is called?
3. Can one small stable MCP facade avoid a large globally visible tool catalog?
4. After the first explicit tool call provides caller metadata, can C-Team resolve the project and return `project_not_enabled` without reading persisted Codex mission state?
5. If `.cteam/` is created while the MCP process remains alive, can the same stable tool immediately recognize the project as enabled without restarting the MCP?
6. What changes, if anything, require a new Codex session for project guidance/skills rather than for MCP mechanics?

## Candidate stable facade

Prototype the smallest useful production-like surface, preferably one tool such as:

```text
cteam(action)
```

with a compact action enum sufficient for the experiment, for example:

```text
status
mission
agents
usage
open
```

Do not build the complete product API. The purpose is to compare one compact stable schema against the larger experimental tool inventory.

The key property is that the tool catalog itself does not need to change when `.cteam/` appears.

## Procedure

### A — inactive project

Use a small project/workspace with no `.cteam/` marker while C-Team is globally installed/enabled.

Record:

- whether Codex starts the C-Team MCP before any explicit C-Team call;
- MCP initialize/client metadata;
- production-like tool count;
- serialized tool names/descriptions/input schemas in bytes/chars;
- any plugin/skill instruction text that is globally injected where observable;
- whether C-Team performs rollout/session scanning before the first explicit call;
- process working set/startup time if cheap to measure.

Then call the stable facade once.

Expected result:

```json
{
  "status": "project_not_enabled"
}
```

The call must use caller/project evidence to locate the project and must not parse persisted mission rollouts merely to determine activation.

### B — marker transition in the same MCP lifetime

With the same Codex/MCP context still alive:

1. create the minimal `.cteam/` marker/environment;
2. call the same stable facade again;
3. verify C-Team recognizes the project as enabled;
4. verify no MCP restart and no dynamic `tools/list` refresh is required for backend activation.

If the host forces a restart for unrelated reasons, report that rather than claiming stable-transition success.

### C — project guidance boundary

Determine what project files the bootstrap is expected to create or modify, such as `.cteam/`, C-Team policy, project skills, or `AGENTS.md` guidance.

Distinguish:

- **MCP activation** — should ideally work immediately after `.cteam/` appears using the stable facade;
- **agent-context activation** — newly created project instructions/skills may require a new Codex session/thread to be loaded cleanly from the start.

Do not conflate the two.

### D — CLI/Desktop comparison

Where cheap, repeat the minimal inactive/marker-transition check in Codex CLI and Desktop. Record host differences without requiring full duplicate workloads.

## Classifications

Finish with one activation classification:

- **A1 — Stable facade works**: one small tool surface remains fixed; inactive calls return `project_not_enabled`; creating `.cteam/` enables the same MCP/tool without restart.
- **A2 — Stable facade needs bounded host/session restart**: project activation is clean but current host behavior requires a new MCP/session boundary.
- **A3 — Dynamic catalog required/viable**: current Codex reliably supports a better dynamic tool-catalog path than the stable facade.
- **A4 — Insufficient/host-dependent**: activation cannot yet be made reliable enough across tested hosts.

Also record:

```text
inactive MCP startup: eager | lazy/not-started | host-dependent
inactive runtime work: dormant | active
visible stable tool count: N
serialized stable tool definition size: N bytes/chars
marker transition without MCP restart: yes | no
new Codex session needed for project guidance: yes | no | depends
```

## Success preference

A1 is the preferred current design because it does not depend on Codex dynamically refreshing `tools/list` and keeps the globally visible schema deliberately small.

Do not force A1 if current evidence contradicts it.

## Evidence

Publish sanitized evidence to:

```text
docs/evidence/pf4-context-activation.json
experiments/008-context-activation/README.md
```

## Retest triggers

- Codex adds reliable repository-scoped plugin activation;
- Codex changes MCP Roots/caller-workspace metadata;
- Codex reliably implements `notifications/tools/list_changed` refresh;
- plugin skill/instruction injection behavior changes;
- Desktop and CLI converge/diverge materially in plugin discovery.

## Stop condition

Stop after A1/A2/A3/A4 and the measured inactive-context footprint are recorded. Do not turn this into full onboarding implementation.