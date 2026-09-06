# C-Team Documentation

## Production direction

- [`mvp-architecture.md`](mvp-architecture.md) — production architecture baseline after foundational experiments: compact global MCP facade, exact project activation, Codex persisted-state adapters, normalized mission domain, CLI/Desktop presentation, and explicit MVP non-goals.
- [`mvp-implementation-plan.md`](mvp-implementation-plan.md) — phased implementation plan from production extraction through mission/tree/usage, incremental observation, Desktop UI, packaging, and release candidate. Immediate next action: production PR 1.

## Foundational direction

- [`architecture-improvements.md`](architecture-improvements.md) — accumulated architecture ideas, source adapters, telemetry provenance, subagent accounting, and compatibility notes discovered during spikes. `mvp-architecture.md` now takes precedence for the production baseline where they differ.
- [`self-improving-team.md`](self-improving-team.md) — product north star for turning agent telemetry into evidence-backed delegation, routing, and skill improvements over time.
- [`runtime-topology.md`](runtime-topology.md) — direct stdio versus future shared-core topology, decision rules, and hard zombie-prevention lifecycle requirements.
- [`plugin-onboarding.md`](plugin-onboarding.md) — personal/global plugin installation, `.cteam` project activation, repository marketplace behavior, and agent/npx/.NET bootstrap ideas.
- [`host-presentation-and-context-footprint.md`](host-presentation-and-context-footprint.md) — Desktop/widget versus CLI/headless presentation and the requirement to keep globally installed C-Team dormant and context-light outside opted-in projects.

## Experiment and protocol findings

- [`experiment-plan.md`](experiment-plan.md) — original observability experiment plan.
- [`spike-findings.md`](spike-findings.md) — app-server/protocol spike findings.
- [`desktop-observation.md`](desktop-observation.md) — Desktop persisted-state observation findings.
- [`near-live-observation.md`](near-live-observation.md) — measured near-live persistence behavior and watcher strategy.
- [`codex-protocol.md`](codex-protocol.md) — protocol observations and schemas used by the experiments.
- [`plugin-validation.md`](plugin-validation.md) — plugin packaging/runtime validation notes.
- [`../experiments/009b-agent-onboarding/README.md`](../experiments/009b-agent-onboarding/README.md) — real agent-first onboarding validation, Desktop restart boundary, and completed O1 onboarding decision.
- [`cteam-scratch-audit.md`](cteam-scratch-audit.md) — audit of private experiment scratch and durable evidence.
- [`evidence/`](evidence/) — sanitized evidence artifacts supporting experiment conclusions.

## Optional compatibility follow-up

- [`../STATE_DB_LOCATOR_SPIKE.md`](../STATE_DB_LOCATOR_SPIKE.md) — Experiment 010: broader optional `state_N.sqlite` mission-location optimization; run only if profiling or a compatibility change creates a concrete need.

The foundational experiment phase is complete through Experiment 009B. New experiments should be created only for concrete host/platform questions that block implementation or are triggered by compatibility changes.

Completed experiment plans remain at the repository root and their durable results live under `experiments/`.

`EXPERIMENTS.md` at the repository root is the compatibility/retest matrix and remains authoritative for experimentally proven behavior.
