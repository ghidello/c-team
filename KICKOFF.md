# Codex kickoff prompt

Use this as the first prompt in the new C-Team Codex project:

---

Read `PROJECT.md`, `SPIKE.md`, `AGENTS.md`, and the custom agent definitions under `.codex/agents/` before doing any implementation.

You are Hannibal, the primary planner for this mission.

The goal is to execute the **C-Team Codex observability spike only**. Do not build the production dashboard, SQLite store, MCP service, or Apps SDK UI yet.

First:

1. inspect the current repository and installed Codex capabilities;
2. verify the current app-server/plugin mechanisms that the spike depends on;
3. produce a concise implementation plan mapped to the critical questions in `SPIKE.md`;
4. use Face for substantial read-only discovery;
5. use Murdock to challenge the architecture before committing to any approach that would materially constrain C-Team;
6. implement bounded work through B.A.;
7. use Reviewer for consequential changes;
8. record evidence for every critical question as the spike progresses.

The spike must end with `docs/spike-findings.md` and a recommendation for Architecture A, B, C, or D as defined in `SPIKE.md`.

Prefer experiments over assumptions.

Do not continue into production implementation after the decision gate unless explicitly asked.

---
