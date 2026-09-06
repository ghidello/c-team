# Experiment 009B — Real agent-first onboarding validation

## Purpose

Close Experiment 009's only remaining material onboarding gap by validating the actual C-Team onboarding skill inside a real Codex session.

Experiment 009 already proved that the canonical initializer produces identical deterministic project state through:

- the bundled NativeAOT command;
- a local `.NET` one-shot `dnx` package;
- an offline NVM-managed `npx` package;
- the plugin-shaped skill payload mechanics.

The exact `npx` transport is now proven. The remaining question is whether Codex can discover the installed onboarding skill, use it at the right time, obtain user approval before mutation, invoke the bundled initializer, and guide the user through the post-init session boundary cleanly.

This is deliberately a **small product-surface validation**, not another architecture spike.

## Hypothesis

A globally installed C-Team plugin can keep its ordinary context footprint minimal while exposing one narrowly scoped onboarding skill that is used only when the user explicitly asks to initialize C-Team or when C-Team reports `project_not_enabled` and the user wants to proceed.

Expected user flow:

```text
fresh repository, no .cteam/
        ↓
user: "Initialize C-Team in this project"
        ↓
Codex discovers C-Team onboarding skill
        ↓
skill explains intended project changes
        ↓
asks user approval
        ↓
invokes bundled canonical initializer
        ↓
creates / merges:
  .cteam/config.json
  AGENTS.md managed C-Team section
        ↓
existing MCP immediately reports project_enabled
        ↓
Codex recommends a fresh session so new project guidance is loaded from the start
```

## Required validation

Use one disposable or clearly bounded test repository that does not initially contain `.cteam/`.

Validate all of the following:

1. The installed C-Team plugin exposes the onboarding skill to a fresh Codex session.
2. A natural user request such as `Initialize C-Team in this project` causes Codex to select/use that skill without requiring the user to know its internal file name.
3. Before mutation, the skill clearly states the intended repository changes and obtains explicit user approval.
4. The skill invokes the bundled canonical initializer rather than reimplementing project-file generation in prose/shell commands.
5. The resulting `.cteam/config.json` and managed `AGENTS.md` block match Experiment 009's canonical golden bytes/semantics.
6. No repository marketplace file is created or changed by default.
7. No user-global plugin configuration is changed as part of project initialization.
8. The already-running C-Team MCP reports `project_enabled` immediately after initialization, without MCP restart or a second `tools/list` dependency.
9. Codex recommends a **fresh Codex session only for newly created/updated project guidance**, not because the MCP backend requires restart.
10. Repeating the initialization request is safe and reports the already-initialized state without rewriting canonical files.

## Context-footprint requirement

The onboarding skill must remain narrowly scoped. Its existence must not undo Experiment 008's context-footprint goal.

Record the installed skill catalog entry and approximate serialized/catalog size if available through the existing no-inference harness. Do not add broad C-Team operational instructions to the global plugin merely to make the skill easier to trigger.

The desired split remains:

```text
always visible globally
  → one tiny `cteam` MCP tool
  → one tiny onboarding skill

project-specific after initialization
  → AGENTS.md managed C-Team guidance
  → future .cteam policy/skills as needed
```

## Quota discipline

This experiment needs only one or two tiny real Codex interactions.

Do not:

- spawn named subagents;
- repeat Experiments 006–008B telemetry workloads;
- benchmark models;
- rerun npx or dnx packaging unless needed to fix a discovered regression;
- build the production UI/shared core/history layer.

Use the already validated canonical initializer and activation behavior as authoritative inputs.

## Classification

Finish with exactly one onboarding classification:

- **O1 — Agent-first + portable bootstrap packages**: real installed-skill discovery/execution works cleanly; agent-first becomes the primary in-agent UX, with `npx` and `dnx` as equivalent manual entry points.
- **O2 — Package-first**: installed-skill discovery/execution is materially awkward or unreliable; use package bootstrap as primary onboarding and keep agent guidance minimal.
- **O3 — Bundled runtime init**: Codex can expose/invoke the bundled initializer cleanly without a separate skill/package UX and this is demonstrably simpler.
- **O4 — More evidence needed**: the real skill path remains ambiguous or host-dependent.

The expected result is O1, but do not force it.

## Evidence

Publish sanitized evidence to:

```text
experiments/009b-agent-onboarding/README.md
docs/evidence/pf5b-agent-onboarding.json
```

Raw prompts, task ids, local paths, and plugin cache details stay under ignored `.cteam/experiment-009b/`.

## Production decision gate

If O1 is proven, treat the foundational onboarding question as closed and move to production/MVP architecture work rather than adding more onboarding experiments.

Experiment 010 remains optional and should run only if profiling or compatibility work later justifies the broader state-DB mission-locator optimization.

## Retest triggers

Retest when:

- Codex plugin skill discovery or skill-loading behavior changes materially;
- repository-scoped plugin activation becomes available;
- plugin update/hot-reload semantics change;
- C-Team's canonical project initializer/schema changes;
- another supported coding-agent runtime establishes a better vendor-neutral onboarding convention.

## Stop condition

Stop after O1/O2/O3/O4 is supported by one real installed-skill onboarding flow, repeat/idempotence check, and sanitized evidence. Do not turn this into production implementation.