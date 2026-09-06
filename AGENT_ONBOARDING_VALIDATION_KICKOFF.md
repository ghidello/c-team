# Experiment 009B kickoff

Run **Experiment 009B — Real agent-first onboarding validation** from `AGENT_ONBOARDING_VALIDATION_SPIKE.md`.

Use **Sol / High** for one fresh Codex session. Keep the mission very small: this is a UX/product-surface validation, not another architecture workload.

## Preconditions

- Use the already installed C-Team plugin payload from Experiments 008B/009, updated only as needed to include the onboarding skill under test.
- Use one disposable or clearly bounded repository that initially has no `.cteam/` marker.
- Treat Experiment 008B D1 activation and Experiment 009 canonical initializer/package results as authoritative; do not repeat them.

## Required flow

1. Start a fresh Codex session with the installed C-Team plugin.
2. Confirm the onboarding skill is discoverable through the host's ordinary skill mechanism.
3. Ask naturally: **`Initialize C-Team in this project`**.
4. Verify Codex selects/uses the C-Team onboarding skill without requiring its internal skill name.
5. Before any mutation, verify the skill explains that it will create/merge only the canonical project files and asks for explicit approval.
6. Approve once.
7. Verify the skill invokes the bundled canonical initializer rather than recreating initialization logic itself.
8. Confirm the resulting `.cteam/config.json` and managed `AGENTS.md` block match Experiment 009's canonical output.
9. Confirm repository marketplace metadata and user-global plugin configuration are untouched.
10. In the same session/MCP lifetime, confirm C-Team reports `project_enabled` without an MCP restart or tool-catalog refresh.
11. Verify the response recommends a fresh Codex session only so the newly written project guidance is loaded from the beginning.
12. Repeat the initialization request once and confirm safe `already_initialized`/equivalent behavior with no rewrite.
13. Record the installed onboarding-skill catalog footprint if the existing no-inference harness can do so cheaply.

## Constraints

- No named-agent fan-out.
- No npx/dnx rerun unless fixing an actual regression found here.
- No shared core, broker, UI, history/index, analytics, routing, cloud, or installer work.
- Raw ids/paths/prompts/cache details stay under ignored `.cteam/experiment-009b/`.
- Commit only sanitized evidence and durable results.

## Finish with exactly one classification

- **O1 — Agent-first + portable bootstrap packages**
- **O2 — Package-first**
- **O3 — Bundled runtime init**
- **O4 — More evidence needed**

If O1 is proven, explicitly state that foundational onboarding validation is complete and that the next work should be production/MVP architecture and implementation planning. Leave Experiment 010 optional unless later profiling/compatibility evidence justifies it.