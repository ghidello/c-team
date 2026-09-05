# Experiment 006 kickoff

Run **Experiment 006 — Caller-to-mission correlation** from `CALLER_MISSION_CORRELATION_SPIKE.md`.

Use **Sol / High** for the primary session. Keep this mission quota-sensitive.

The goal is narrow: prove or falsify the exact join between per-call Codex MCP metadata (`_meta.x-codex-turn-metadata.thread_id`) and persisted Codex rollout/session identity.

Important constraints:

- Reuse Experiments 003 and 005; do not repeat their paid workloads.
- Use the existing compiled C# experiment harness and xUnit v3 tests.
- Do not add Python/PowerShell as the new experiment implementation.
- Prefer existing real persisted Desktop contexts. Do not create synthetic agent fan-out just to generate telemetry.
- If a second persisted context is needed, use the cheapest bounded real interaction that answers CM3.
- Keep raw ids, paths, prompts, commands and rollout data under ignored `.cteam/experiment-006/`.
- Commit only sanitized aggregate evidence and durable procedure/results.
- Do not build production SQLite, UI, indexing, daemon/lifecycle, routing or a second HTTP/WebSocket API.

Prioritize, in order:

1. inspect current persisted identity fields and existing Experiment 005 caller metadata handling;
2. implement deterministic C# correlation tests;
3. perform the minimum live MCP calls needed for CM1–CM3;
4. verify child/review and missing-metadata behavior without expensive synthetic work where existing evidence suffices;
5. measure only enough lookup cost to decide C1/C2/C3/C4;
6. archive the result as Experiment 006 and update `EXPERIMENTS.md`.

Do not treat cwd, recency or project hints as exact identity. If caller `thread_id` cannot produce a unique persisted match, report that honestly.

Stop at the decision gate with exactly one classification:

- C1 — Exact
- C2 — Exact with bounded adapter
- C3 — Context-assisted
- C4 — Insufficient

If C1 or C2 is proven, explicitly state whether the local runtime/identity spike phase is now complete.