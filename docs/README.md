# C-Team Documentation

## Foundational direction

- [`architecture-improvements.md`](architecture-improvements.md) — production architecture improvements, source adapters, MCP/runtime decisions, telemetry provenance, subagent accounting, and follow-up opportunities.
- [`self-improving-team.md`](self-improving-team.md) — product north star for turning agent telemetry into evidence-backed delegation, routing, and skill improvements over time.
- [`runtime-topology.md`](runtime-topology.md) — direct stdio versus future shared-core topology, decision rules, and hard zombie-prevention lifecycle requirements.
- [`plugin-onboarding.md`](plugin-onboarding.md) — personal/global plugin installation versus repository marketplace/project activation, including `.cteam` as the current preferred opt-in boundary.
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

- [`../PLUGIN_MCP_TOPOLOGY_SPIKE.md`](../PLUGIN_MCP_TOPOLOGY_SPIKE.md) — Experiment 007: determine whether plugin MCP process lifetime is project-, root-tree-, or per-agent scoped, verify cleanup, and measure inactive-project/global-plugin footprint.
- [`../STATE_DB_LOCATOR_SPIKE.md`](../STATE_DB_LOCATOR_SPIKE.md) — Experiment 008: optional `state_N.sqlite` fast-path from exact thread identity to rollout path.

`EXPERIMENTS.md` at the repository root is the compatibility/retest matrix and remains authoritative for experimentally proven behavior.