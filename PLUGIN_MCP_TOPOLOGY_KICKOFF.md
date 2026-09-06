# Experiment 007 kickoff

Run **Experiment 007 — Plugin MCP process topology** from `PLUGIN_MCP_TOPOLOGY_SPIKE.md`.

Use **Sol / High** for the primary Hannibal session. Keep the mission quota-sensitive, but intentionally create the small named-agent fan-out required by the experiment.

The goal is narrow: determine whether Codex reuses the C-Team plugin MCP process across native subagents, independent roots in one project, and simultaneous different projects, and measure the footprint of a globally installed plugin in a project that does not contain `.cteam/`.

Required named-agent fan-out:

- **Face** — one tiny read-only task plus one C-Team MCP probe call.
- **B.A.** — one tiny bounded fixture/harness task plus one C-Team MCP probe call.
- **Reviewer** — one tiny independent verification plus one C-Team MCP probe call.

Do not invoke Murdock unless the observed topology is genuinely surprising and requires a challenge pass.

Important constraints:

- Reuse Experiment 005's NativeAOT plugin/MCP harness and packaging.
- Reuse Experiment 006 caller-correlation work if it has completed by execution time.
- Do not repeat paid telemetry workloads from Experiments 003–006.
- New reusable experiment code must remain C#/.NET 10 and NativeAOT-friendly.
- Raw ids, paths, prompts and rollout contents stay under ignored `.cteam/experiment-007/`.
- Commit only sanitized topology/context-footprint evidence and durable procedure/results.
- Do not implement a shared C-Team core, broker, Named Pipe protocol, daemon, service, production SQLite, UI or second HTTP/WebSocket API.
- For the inactive-project case, do not create `.cteam/`; its absence is the condition under test.

Prioritize, in order:

1. instrument/reuse the MCP probe to record PID + caller metadata;
2. establish the root baseline;
3. create Face/B.A./Reviewer fan-out and make each child call the MCP probe;
4. test a second independent root in the same project if cheap;
5. test one simultaneous different project/context **without `.cteam/`**;
6. in that inactive project, determine whether the MCP starts eagerly, record the visible C-Team tool inventory/schema size if reproducible, and verify one explicit probe returns `project_not_enabled` without scanning persisted Codex state;
7. verify normal process cleanup and, if cheap, one abrupt-owner-exit case;
8. archive the result as Experiment 007 and update `EXPERIMENTS.md`.

Do not claim that returning `project_not_enabled` removes model-context cost: tool definitions may already have been exposed by MCP discovery. Record observable inventory/serialized schema size rather than guessing token cost.

Finish with exactly one topology classification:

- **P1 — project-shared**
- **P2 — root-tree shared**
- **P3 — per-thread/per-agent**
- **P4 — host-dependent/unclear**

Also report same-project root sharing, cross-project isolation, normal cleanup, abrupt-exit cleanup, inactive-project MCP startup (eager/lazy), and inactive-project runtime work (dormant/active) separately.

If P1 or P2 is proven, explicitly state that a shared C-Team core remains a deferred optimization. If P3 is proven, state that facade + demand-started shared core becomes the preferred future direction once shared-state cost is real, but do not implement it in this experiment.