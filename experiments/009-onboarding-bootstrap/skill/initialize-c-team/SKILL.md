---
name: initialize-c-team
description: Initialize C-Team project files after its MCP status reports that the current repository is not enabled.
---

# Initialize C-Team

Use this skill only when the user asks to initialize C-Team or agrees after the `cteam` MCP tool returns `project_not_enabled`.

Before changing the repository, identify its exact root and tell the user that initialization creates `.cteam/config.json` and creates or merges a managed C-Team section in the root `AGENTS.md`. It does not install or enable a plugin, change a repository marketplace, or write outside the repository. After this concrete explanation, always pause for explicit approval immediately before making those changes; treat the initial request as intent to begin the flow rather than approval of the file mutation.

Resolve the plugin root from this `SKILL.md` path, then run its bundled `bin/win-x64/cteam.exe` with:

```text
init --target <repository-root>
```

Report the command's structured result. If it reports `upgrade_required`, explain the planned files and stop; this experimental initializer deliberately does not apply schema upgrades. Do not bypass rejected or malformed project state.

After a successful initialization, the existing MCP process can recognize `.cteam` immediately. Recommend a fresh Codex session so the new `AGENTS.md` guidance is loaded from the beginning. Treat C-Team plugin installation as a separate user-level action.
