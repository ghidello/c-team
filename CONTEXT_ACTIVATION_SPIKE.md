# Experiment 008 — Project activation and MCP context footprint

## Purpose

Determine the smallest practical globally installed C-Team plugin surface while keeping unrelated projects dormant and preserving a smooth opt-in path for projects that contain a `.cteam/` marker.

Experiment 007 measures **process topology**. Experiment 008 measures **activation, tool discovery, context footprint, and marker-transition behavior**.

## Questions

1. Can C-Team stay globally installed while doing essentially no runtime work in a project without `.cteam/`?
2. What C-Team tool definitions/instructions are visible to Codex before any C-Team call?
3. Can one small stable MCP facade avoid a large globally visible tool catalog?
4. Once a tool call supplies caller metadata, can C-Team resolve the project and return `project_not_enabled` without reading persisted mission state?
5. If `.cteam/` is created while the MCP stays alive, can the same stable tool immediately recognize the project as enabled without restarting the MCP?
6. Which changes require a new Codex session for project guidance/skills rather than for MCP mechanics?

## Candidate stable facade

Prototype the smallest useful production-like surface, preferably one compact tool such as:

```text
cteam(action)
```

with only the actions needed for this experiment, for example:

```text
status
mission
agents
usage
open
```

Do not build the complete product API. The important property is that the tool catalog itself does not need to change when `.cteam/` appears.

## Procedure

### A — inactive project

Use a minimal project with no `.cteam/` marker while C-Team is globally installed/enabled.

Record:

- whether Codex starts the C-Team MCP before any explicit C-Team call;
- MCP initialize/client metadata;
- production-like tool count;
- serialized tool names/descriptions/input schemas in bytes/chars;
- globally injected C-Team/plugin/skill instruction text where observable;
- whether C-Team performs rollout/session scanning before the first explicit call;
- startup time/working set if cheap to measure.

Then call the stable facade once. Expected semantic result:

```json
{
  "status": "project_not_enabled"
}
```

The call must locate the caller project from caller/workspace evidence and must not parse persisted mission rollouts merely to determine activation.

### B — marker transition in the same MCP lifetime

With the same Codex/MCP context alive:

1. create the minimal `.cteam/` marker/environment;
2. call the same stable facade again;
3. verify C-Team recognizes the project as enabled;
4. verify no MCP restart and no dynamic `tools/list` refresh is required for backend activation.

### C — project guidance boundary

Distinguish:

- **MCP activation** — should ideally work immediately after `.cteam/` appears;
- **agent-context activation** — newly created project instructions/skills may require a new Codex session/thread so they are loaded from the start.

Do not conflate the two.

### D — CLI/Desktop comparison

Where cheap, repeat the minimal inactive/marker-transition check in Codex CLI and Desktop and record host differences.

## Classification

Finish with one activation classification:

- **A1 — Stable facade works**: one small tool surface remains fixed; inactive calls return `project_not_enabled`; creating `.cteam/` enables the same MCP/tool without restart.
- **A2 — Stable facade needs bounded restart**: project activation is clean but current host behavior requires a new MCP/session boundary.
- **A3 — Dynamic catalog is better and proven**: current Codex reliably supports a dynamic tool-catalog path that is clearly preferable.
- **A4 — Insufficient/host-dependent**: activation cannot yet be made reliable enough.

Also record:

```text
inactive MCP startup: eager | lazy/not-started | host-dependent
inactive runtime work: dormant | active
visible stable tool count: N
serialized stable tool definition size: N bytes/chars
marker transition without MCP restart: yes | no
new Codex session needed for project guidance: yes | no | depends
```

## Preferred outcome

A1 is the preferred current design because it avoids depending on dynamic `tools/list` refresh while keeping the globally visible schema deliberately small.

Do not force A1 if evidence contradicts it.

## Evidence

Publish sanitized evidence to:

```text
docs/evidence/pf4-context-activation.json
experiments/008-context-activation/README.md
```

## Retest triggers

- reliable repository-scoped plugin activation appears;
- MCP Roots/caller-workspace metadata changes;
- Codex reliably refreshes dynamic MCP tool catalogs;
- plugin skill/instruction injection behavior changes;
- Desktop and CLI plugin discovery behavior changes materially.

## Stop condition

Stop after A1/A2/A3/A4 and the inactive-context footprint are measured. Do not extend this into full onboarding implementation.