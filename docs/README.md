# C-Team Documentation

## Foundational direction

- [`architecture-improvements.md`](architecture-improvements.md) — production architecture improvements, source adapters, MCP/runtime decisions, telemetry provenance, subagent accounting, and follow-up opportunities.
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
- [`cteam-scratch-audit.md`](cteam-scratch-audit.md) — audit of private experiment scratch and durable evidence.
- [`evidence/`](evidence/) — sanitized evidence artifacts supporting experiment conclusions.

## Planned follow-ups

- [`../PLUGIN_MCP_TOPOLOGY_SPIKE.md`](../PLUGIN_MCP_TOPOLOGY_SPIKE.md) — Experiment 007: plugin MCP process scope and cleanup.
- [`../CONTEXT_ACTIVATION_SPIKE.md`](../CONTEXT_ACTIVATION_SPIKE.md) — Experiment 008: stable minimal MCP facade, `.cteam` activation, marker transition and inactive-project context/runtime footprint.
- [`../ONBOARDING_BOOTSTRAP_SPIKE.md`](../ONBOARDING_BOOTSTRAP_SPIKE.md) — Experiment 009: compare agent-first, `npx`, .NET one-shot and possible `cteam init` bootstrap paths.
- [`../STATE_DB_LOCATOR_SPIKE.md`](../STATE_DB_LOCATOR_SPIKE.md) — Experiment 010: optional `state_N.sqlite` fast-path from exact thread identity to rollout path.

`EXPERIMENTS.md` at the repository root is the compatibility/retest matrix and remains authoritative for experimentally proven behavior.