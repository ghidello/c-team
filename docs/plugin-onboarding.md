# C-Team plugin onboarding

## Goal

C-Team should feel natural both for a developer who wants it everywhere and for a repository that already uses C-Team as part of its agent workflow.

The plugin model must therefore distinguish:

- **personal/global installation** — the user has C-Team installed and available across Codex work;
- **repository marketplace presence** — a project advertises that it uses/recommends C-Team;
- **project activation** — the user chooses to install/enable C-Team for their Codex environment; the repository should not silently force a user-level install.

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

Treat that as a useful safety and onboarding property.

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

## Desired onboarding experiences

### 1. User installs C-Team globally/personal

Use case:

> I want C-Team available whenever I use Codex.

Desired experience:

```text
install C-Team once
      ↓
new Codex thread/task
      ↓
C-Team tools available
      ↓
when current project has no C-Team policy/config
      ↓
observe only / use sensible defaults
```

C-Team must not require every repository to commit C-Team-specific files just to show telemetry.

### 2. Repository already uses C-Team

Use case:

> I cloned a project whose team workflow expects C-Team.

The repository may contain:

```text
.agents/plugins/marketplace.json
CTeam project policy/configuration
skills / routing policy references
```

Desired experience:

```text
open project in Codex
      ↓
Codex can discover repository marketplace
      ↓
C-Team detects project integration intent
      ↓
if C-Team already installed/enabled:
    use project configuration immediately
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
repository C-Team policy/skills
        ↓
C-Team runtime reused
project-specific behavior loaded
```

Do **not** require a second plugin installation merely because the project contains C-Team policy.

The plugin binary/runtime and project policy should be separate concepts.

### 4. Repository advertises C-Team but user declines

The project must remain usable. C-Team integration should be additive unless the repository itself explicitly defines C-Team as a development prerequisite.

Do not create surprising automatic user-level mutations from repository content.

## Canonical policy and adapters

As C-Team expands beyond Codex, avoid independently editable policy copies for each coding-agent runtime.

Preferred model:

```text
canonical C-Team project/team policy
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

Keep project integration small. A future project that opts into C-Team might need only something like:

```text
.agents/
  plugins/
    marketplace.json    # optional: makes C-Team discoverable from the repo

.cteam/ or .agents/...
  policy/config         # exact production location still to be decided

AGENTS.md / skills
  team/delegation guidance
```

Do not decide the final C-Team config location until we understand how Codex, Claude and Copilot can share a canonical source cleanly.

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

Future adapters may observe Claude Code, GitHub Copilot, or other coding-agent systems with different plugin/MCP/session models. The normalized C-Team domain and self-improvement model should remain vendor-neutral.

## Open questions

- What exact repository file should declare C-Team policy?
- Can Codex eventually express repository-scoped plugin enablement without user-global mutation?
- How should a repository advertise a minimum/recommended C-Team version?
- Can project onboarding be initiated by a C-Team skill without creating an awkward circular dependency when the plugin is not installed?
- How should Claude/Copilot adapters consume the same canonical team policy?
- Should a project marketplace reference the public C-Team package directly or a repository-local compatibility wrapper?

## Related experiments/documents

- `PLUGIN_MCP_TOPOLOGY_SPIKE.md` — measures actual plugin process scope.
- `STATE_DB_LOCATOR_SPIKE.md` — later optional mission-location optimization.
- `docs/self-improving-team.md` — defines the vendor-neutral learning/policy loop.
- `docs/architecture-improvements.md` — runtime architecture backlog.
- `EXPERIMENTS.md` — authoritative compatibility matrix.