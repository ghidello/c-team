# Codex kickoff — Plugin MCP runtime spike

Use **Sol / High** for the primary session.

Read `PROJECT.md`, `PRODUCTION_REQUIREMENTS.md`, `EXPERIMENTS.md`, `MCP_RUNTIME_SPIKE.md`, `AGENTS.md`, and the existing experiment archive before making changes.

Execute **Experiment 005 — Plugin MCP Runtime** only.

Key rules:

- Reuse all results from experiments 001–004; do not rerun them.
- Treat PF1-C as established: shell-launched bundled EXE works, but `%LOCALAPPDATA%` durable writes caused recurring approval.
- Test whether the bundled NativeAOT executable works better when declared as the plugin's **stdio MCP server**.
- Inspect the real current MCP `initialize` request. MCP supports `roots/list` only when the client declares the `roots` capability; do not assume Codex does or does not support it on the installed version.
- If `roots` is declared, request `roots/list` and record the actual result.
- Inspect current plugin-provided environment such as `PLUGIN_ROOT`, `PLUGIN_DATA`, or equivalents and test supported plugin-owned durable storage if available.
- Verify one harmless MCP tool and one bounded read-only C-Team mission probe.
- Test two simultaneous Codex project/session contexts and record actual C-Team MCP process count, lifetime, project/thread/root signals, and cross-project behavior.
- Do not assume MCP process cwd identifies the project.
- Keep all reusable experiment/probe code in C#/.NET 10 and compiled in-repo.
- Keep raw/private evidence under ignored `.cteam/experiment-005/`; commit only sanitized evidence.
- Do not build Apps SDK UI, production SQLite, production MCP architecture, installer, service, routing analytics, or other product features.
- Avoid synthetic agent work. Every live Codex invocation must answer a specific MR1–MR8 question.

Finish by updating `EXPERIMENTS.md` and producing:

- `experiments/005-plugin-mcp-runtime/README.md`
- `docs/evidence/pf2-mcp-runtime.json`
- exactly one PF2 classification (A/B/C/D)
- exactly one multi-project classification (M1/M2/M3/M4)

Stop at that decision gate.
