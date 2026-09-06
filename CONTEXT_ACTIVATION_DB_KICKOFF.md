# Experiment 008B kickoff

Run **Experiment 008B — Exact caller project resolution via Codex state DB** from `CONTEXT_ACTIVATION_DB_SPIKE.md`.

Use **Sol / High**. Keep the mission quota-sensitive and reuse Experiments 006–008; do not repeat their paid workloads.

The narrow goal is to determine whether the installed Codex state database can map exact MCP caller `thread_id` to trustworthy `cwd` / project metadata and therefore drive `.cteam` activation when Codex supplies neither workspace metadata nor MCP Roots.

Prioritize:

1. inspect the **installed Codex 0.153.4** `state_N.sqlite` schema read-only; do not assume current upstream source matches it;
2. prove exact `thread_id → DB row → cwd/project metadata` for at least two existing real root contexts;
3. prove one naturally available child/subagent lookup and state whether child metadata alone identifies the same project or needs persisted parent/root assistance;
4. use a real inactive project to obtain `project_not_enabled` through the DB path with zero rollout reads on the successful fast path;
5. create `.cteam/` between two calls on the same MCP process and prove `project_enabled` without restart or another `tools/list`;
6. exercise safe DB failure/schema/missing-row fixtures and prove fallback to Experiment 006's exact rollout adapter or unresolved;
7. archive sanitized evidence and update `EXPERIMENTS.md`.

Important constraints:

- Reuse Experiment 008's one-tool activation server.
- Reuse Experiment 006 exact caller identity/fallback semantics.
- Read Codex SQLite only; do not mutate it or run backfill.
- Do not use MCP cwd, recency, latest-thread selection, same-cwd guessing, or unrelated filesystem scans.
- If cwd is nested, only a bounded upward normalization from the exact caller cwd is allowed; record exactly what boundary is used.
- No shared core, broker, production DB/history, onboarding package, UI, installer or service.
- Raw ids, paths and DB details stay under ignored `.cteam/experiment-008b/`.

Finish with exactly one classification:

- **D1 — Exact DB activation**
- **D2 — Exact DB locator, project normalization needed**
- **D3 — Useful optimization only**
- **D4 — Insufficient/brittle**

Also report root lookup, child lookup, inactive-project result, same-process marker transition, successful-path rollout read count, and DB failure fallback separately.

If D1 or D2 is proven, explicitly state that Experiment 009 onboarding is unblocked. Do not start Experiment 009 in the same mission.