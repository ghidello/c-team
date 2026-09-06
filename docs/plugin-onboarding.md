# C-Team plugin onboarding

## Goal

C-Team should feel natural both for a developer who wants it everywhere and for a repository that already uses C-Team as part of its agent workflow.

The plugin model must therefore distinguish:

- **personal/global installation** — the user has C-Team installed and available across Codex work;
- **repository marketplace presence** — a project advertises that it uses/recommends C-Team;
- **project activation** — the repository explicitly opts into active C-Team behavior;
- **runtime availability** — Codex may have loaded the globally installed plugin even when the current project is not activated.

This distinction is part of the product onboarding experience, not an implementation footnote.

## Current Codex model

Codex supports marketplace manifests in two important locations:

```text
personal
~/.agents/plugins/marketplace.json

repository
<repo-root>/.agents/plugins/marketplace.json
```

A repository marketplace can make C-Team discoverable from the project and can point to a local or packaged plugin source. Personal installation can make C-Team available independent of any one project.

Today, plugin installation/enabled state is user/Codex-environment scoped rather than a repository being able to silently force the plugin active merely because the repo contains a marketplace manifest.

Treat that as a useful safety property, but also recognize the context-footprint consequence: a personally enabled plugin can contribute instructions/skills and MCP tool definitions to sessions in repositories that do not use C-Team.

## Codex CLI support

Codex CLI is a first-class plugin host. Current CLI source exposes:

```text
codex plugin add
codex plugin list
codex plugin remove
codex plugin marketplace ...
```

and enabled plugins can contribute bundled `.mcp.json` servers to the CLI runtime.

Therefore C-Team onboarding must work consistently for at least:

```text
ChatGPT/Codex Desktop
Codex CLI
```

Do not design the plugin as a Desktop-only feature.

C-Team's MCP capabilities must also remain useful without a graphical widget. See `host-presentation-and-context-footprint.md`.

## Desired onboarding experiences

### 1. User installs C-Team globally/personal

Use case:

> I want C-Team ready wherever I use coding agents, but I do not want it actively observing every repository.

Desired experience:

```text
install C-Team once
      ↓
new Codex thread/task
      ↓
project contains .cteam?
   yes → activate C-Team project behavior
   no  → remain dormant
```

Dormant means:

```text
no rollout/session scans
no watchers
no history/index writes
no analytics
no shared-core startup
```

If a C-Team tool is explicitly called from an unactivated project, return a tiny result such as:

```json
{
  "status": "disabled",
  "reason": "project_not_enabled"
}
```

Do not turn the disabled result into a large onboarding/help payload.

### Context-footprint caveat

The `.cteam` check prevents runtime/data pollution but does **not** necessarily remove C-Team from model context. Codex normally discovers tools from enabled MCP servers, so the model may already have received C-Team tool names/descriptions/schemas before any tool can return `project_not_enabled`. Globally enabled plugin skills/instructions can add context too.

Therefore a global install must keep its always-visible instructions and production tool schemas deliberately small. True zero-footprint behavior requires future reliable repository-scoped plugin activation or an equivalent host feature.

### 2. Repository already uses C-Team

Use case:

> I cloned a project whose team workflow expects C-Team.

The repository may contain:

```text
.agents/plugins/marketplace.json
.cteam/
AGENTS.md / skills
```

Desired experience:

```text
open project in Codex
      ↓
Codex can discover repository marketplace
      ↓
.cteam marks project as C-Team-aware
      ↓
if C-Team already installed/enabled:
    activate project behavior immediately
else:
    present one clear install/enable path
      ↓
start a new thread when required for plugin/MCP pickup
```

Avoid making the user understand plugin cache internals, PATH, NativeAOT packaging, or marketplace mechanics.

### 3. User has global C-Team, project has its own policy

This should be the ideal path:

```text
personal C-Team plugin
        +
repository .cteam policy/config
        ↓
same installed C-Team runtime
project-specific behavior loaded
```

Do **not** require a second plugin installation merely because the project contains C-Team policy.

The plugin binary/runtime and project policy are separate concepts.

### 4. Repository advertises C-Team but user declines

The project must remain usable. C-Team integration should be additive unless the repository itself explicitly defines C-Team as a development prerequisite.

Do not create surprising automatic user-level mutations from repository content.

## `.cteam` as the activation boundary

The current preferred project marker is a top-level `.cteam/` directory.

Initial rule:

> A repository/workspace is C-Team-active only when an accepted project root contains `.cteam/`.

The directory may initially be empty and later contain versioned configuration/policy.

Possible future shape:

```text
.cteam/
  config.json
  policy/
  skills/
```

The exact contents remain undecided, but using `.cteam` gives us a vendor-neutral project marker suitable for future Codex, Claude and Copilot adapters.

Do not find `.cteam` using the MCP process cwd alone. Experiment 005 showed that plugin MCP cwd may be the versioned plugin cache root. Activation must use actual caller/workspace/project evidence when available.

## Canonical policy and adapters

As C-Team expands beyond Codex, avoid independently editable policy copies for each coding-agent runtime.

Preferred model:

```text
canonical .cteam project/team policy
             │
     ┌───────┼────────┐
     ▼       ▼        ▼
   Codex   Claude   Copilot
  adapter  adapter   adapter
```

The canonical source should remain versionable in Git. Runtime-specific packaging/adapters may translate placement or metadata conventions, but they should not become separate sources of truth.

## Project identity

Do not equate plugin process lifetime with project identity.

The plugin may be globally installed while a single `cteam.exe` process is scoped by Codex to a root conversation, project, client or agent. Experiment 007 exists specifically to measure that lifecycle.

C-Team project configuration should be selected using explicit caller/workspace/project evidence, not the mere fact that a particular MCP process exists.

## Suggested repository footprint

Keep project integration small:

```text
.agents/
  plugins/
    marketplace.json    # optional: makes C-Team discoverable from the repo

.cteam/                 # activates C-Team for this project
  config/policy         # future; optional initially

AGENTS.md / skills
  team/delegation guidance where useful
```

The marketplace file and `.cteam` have different responsibilities:

- repository marketplace → **discover/install C-Team**;
- `.cteam` → **this project opts into C-Team behavior**.

That distinction should remain obvious in onboarding and documentation.

## Installation/update behavior

Current Codex plugin development guidance treats a **new thread** as the safe boundary for picking up a reinstalled/updated plugin and its MCP tools.

Onboarding should surface that naturally when needed:

> C-Team is installed. Start a new Codex thread to activate the updated plugin.

Do not tell users to restart the whole machine or install a service.

## Versioning

A project may eventually want to express compatibility such as:

```text
C-Team policy schema version
minimum C-Team plugin version
optional recommended version
```

Prefer warnings and guided upgrades over silently replacing an installed user plugin.

Because plugin installation is user-scoped, project configuration should not assume it owns plugin update policy.

## Cross-runtime future

The brand meaning should remain:

> **C-Team = Coding Team**

Codex is the first supported runtime, not a permanent product boundary.

Future adapters may observe Claude Code, GitHub Copilot, or other coding-agent systems with different plugin/MCP/session models. The normalized C-Team domain, `.cteam` project marker and self-improvement model should remain vendor-neutral where practical.

## Open questions

- Is an empty `.cteam/` directory enough, or should activation require a tiny manifest file?
- Can Codex eventually express repository-scoped plugin enablement without user-global mutation?
- Can an enabled global plugin dynamically suppress its MCP tool catalog before the model receives it based on project identity?
- How should a repository advertise a minimum/recommended C-Team version?
- Can project onboarding be initiated by a repository skill without creating an awkward circular dependency when the plugin is not installed?
- How should Claude/Copilot adapters consume the same canonical team policy?
- Should a project marketplace reference the public C-Team package directly or a repository-local compatibility wrapper?

## Related experiments/documents

- `PLUGIN_MCP_TOPOLOGY_SPIKE.md` — measures actual plugin process scope and inactive-project footprint.
- `STATE_DB_LOCATOR_SPIKE.md` — later optional mission-location optimization.
- `docs/host-presentation-and-context-footprint.md` — CLI/Desktop presentation and global-plugin context minimization.
- `docs/self-improving-team.md` — defines the vendor-neutral learning/policy loop.
- `docs/architecture-improvements.md` — runtime architecture backlog.
- `EXPERIMENTS.md` — authoritative compatibility matrix.