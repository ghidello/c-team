# Experiment 007 kickoff

Run **Experiment 007 — Plugin MCP process topology** from `PLUGIN_MCP_TOPOLOGY_SPIKE.md`.

Use **Sol / High** for the primary Hannibal session. Keep the mission quota-sensitive, but intentionally create the small named-agent fan-out required by the experiment.

The goal is narrow: determine whether Codex reuses the C-Team plugin MCP process across native subagents, independent roots in one project, and simultaneous different projects.

Required named-agent fan-out:

- **Face** — one tiny read-only task plus one C-Team MCP probe call.
- **B.A.** — one tiny bounded fixture/harness task plus one C-Team MCP probe call.
- **Reviewer** — one tiny independent verification plus one C-Team MCP probe call.

Do not invoke Murdock unless topology itself is genuinely surprising.

Important constraints:

- Reuse Experiment 005's NativeAOT plugin/MCP harness and packaging.
- Reuse Experiment 006 caller-correlation work if completed by execution time.
- Do not repeat paid telemetry workloads from Experiments 003–006.
- New reusable experiment code must remain C#/.NET 10 and NativeAOT-friendly.
- Raw ids, paths, prompts and rollout contents stay under ignored `.cteam/experiment-007/`.
- Commit only sanitized topology evidence and durable procedure/results.
- Do not implement project activation/context-footprint behavior here; that is Experiment 008.
- Do not implement a shared core, broker, Named Pipe protocol, daemon, service, production SQLite, UI or second HTTP/WebSocket API.

Prioritize:

1. record PID + caller metadata for the root;
2. create Face/B.A./Reviewer fan-out and make each child call the probe;
3. test a second independent root in the same project if cheap;
4. test one simultaneous different project/context;
5. verify normal cleanup and, if cheap, one abrupt-owner-exit case;
6. archive Experiment 007 and update `EXPERIMENTS.md`.

Finish with exactly one topology classification:

- **P1 — project-shared**
- **P2 — root-tree shared**
- **P3 — per-thread/per-agent**
- **P4 — host-dependent/unclear**

Also report same-project root sharing, cross-project isolation, normal cleanup and abrupt-exit cleanup separately.

If P1 or P2 is proven, explicitly state that a shared C-Team core remains a deferred optimization. If P3 is proven, state that facade + demand-started shared core becomes the preferred future direction once shared-state cost is real, but do not implement it here.