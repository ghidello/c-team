# C-Team plugin onboarding

## Goal

C-Team should feel natural both for a developer who wants it everywhere and for a repository that already uses C-Team as part of its agent workflow.

The plugin model must distinguish:

- **personal/global installation** — the user has C-Team installed and available across coding-agent work;
- **repository marketplace presence** — a project advertises that it uses/recommends C-Team;
- **project activation** — the repository explicitly opts into active C-Team behavior;
- **runtime availability** — the host may have loaded the global plugin even when the current project is not activated;
- **project initialization** — creation of the canonical `.cteam` environment and any project guidance.

This distinction is part of the product onboarding experience, not an implementation footnote.

## Current Codex model

Codex supports marketplace manifests in two important locations:

```text
personal
~/.agents/plugins/marketplace.json

repository
<repo-root>/.agents/plugins/marketplace.json
```

A repository marketplace can make C-Team discoverable from the project. Personal installation can make C-Team available independent of any one project.

Today, plugin installation/enabled state is broader than repository activation. Treat that as a useful safety property, but recognize the context-footprint consequence: a personally enabled plugin can contribute instructions/skills and MCP tool definitions in repositories that do not use C-Team.

## Codex CLI and Desktop

Codex CLI is a first-class plugin host and enabled plugins can contribute bundled `.mcp.json` servers. C-Team onboarding must therefore work consistently for both Desktop and CLI.

C-Team's MCP capabilities must remain useful without a graphical widget. See `host-presentation-and-context-footprint.md`.

## Desired global-install behavior

Use case:

> I want C-Team ready wherever I work, but I do not want it actively observing every repository.

Desired behavior:

```text
install C-Team once
      ↓
new coding-agent session
      ↓
project contains .cteam?
   yes → activate C-Team project behavior
   no  → remain dormant
```

Dormant means no rollout/session scans, watchers, history/index writes, analytics or shared-core startup.

If the stable C-Team MCP facade is explicitly called from an unactivated project, it should return a tiny semantic result such as:

```json
{
  "status": "project_not_enabled"
}
```

Experiment 008 measures whether this can work with one small stable tool surface and whether creating `.cteam/` can activate the same running MCP immediately.

## Context-footprint caveat

The `.cteam` check prevents runtime/data pollution but does not necessarily remove C-Team from model context. An enabled MCP server's tool definitions can be discovered before any tool call, and globally enabled plugin guidance can also consume context.

Therefore a global install must keep its always-visible instructions and production tool schemas deliberately small. True zero-footprint behavior requires future reliable repository-scoped plugin activation or an equivalent host feature.

## Repository already uses C-Team

A repository may contain:

```text
.agents/plugins/marketplace.json
.cteam/
AGENTS.md / project skills
```

Desired experience:

```text
open project
      ↓
repository advertises C-Team if needed
      ↓
.cteam marks project as C-Team-aware
      ↓
if C-Team already installed/enabled:
    use project behavior immediately
else:
    present one clear install/enable path
```

Do not require a second plugin installation merely because the project contains C-Team policy.

The marketplace and `.cteam` have different responsibilities:

- repository marketplace → **discover/install C-Team**;
- `.cteam` → **this project opts into C-Team behavior**.

## `.cteam` as activation boundary

The current preferred project marker is a top-level `.cteam/` directory.

Initial rule:

> A repository/workspace is C-Team-active only when an accepted project root contains `.cteam/`.

Possible future shape:

```text
.cteam/
  config.json
  policy/
  skills/
```

The exact contents remain experimental. Experiment 008 validates the marker semantics; Experiment 009 determines the smallest useful generated project footprint.

Do not find `.cteam` using MCP process cwd alone. Experiment 005 showed that plugin MCP cwd may be the versioned plugin cache root. Activation must use actual caller/workspace/project evidence when available.

## Project initialization

Initialization and plugin installation are separate operations.

A developer may already have the global C-Team plugin but be entering a repository without `.cteam/`. Conversely, a repository may advertise C-Team while the user has not installed the plugin yet.

The initializer should have one canonical deterministic implementation/model, exposed through several convenient entry points rather than separate ecosystem-specific project formats.

Candidate user experiences:

```text
Inside Codex/another coding agent:
  "Initialize C-Team for this project"

JavaScript ecosystem / universal developer fallback:
  npx c-team init

.NET-oriented developer fallback:
  dnx <C-Team bootstrap package> init

Possible later direct CLI, only if deployment stays clean:
  cteam init
```

The exact package IDs and commands are not yet decided.

### Agent-first onboarding

The preferred conversational path is likely a tiny global onboarding skill:

```text
C-Team status
      ↓
project_not_enabled
      ↓
explain what initialization will create
      ↓
ask user approval
      ↓
run canonical initializer
      ↓
.cteam now exists
      ↓
backend activates immediately if Experiment 008 proves it
```

If initialization also creates or changes `AGENTS.md`, project skills or routing guidance, recommend a **fresh coding-agent session** so those instructions are present from the start. That is distinct from restarting the MCP itself.

### npx bootstrap

An npm package can provide an excellent zero-install manual entry point even for non-JavaScript repositories:

```text
npx c-team init
```

If we use this path, the package should be tiny and should only bootstrap project files. Node/npm must not become a runtime dependency of `cteam.exe`.

### .NET bootstrap

For .NET-heavy environments, a one-shot .NET package/application is equally attractive. A future `dnx`-style command could give us the same no-permanent-install experience:

```text
dnx <package> init
```

Prefer this over requiring a global dotnet tool just to scaffold `.cteam/`.

### Why support both?

The generated project environment is small, so supporting both npm and .NET bootstrap packages may be inexpensive if they share one canonical template/specification and deterministic tests.

The point is not to create two C-Team implementations. It is to make initialization natural from either ecosystem while C-Team's actual runtime remains the bundled NativeAOT executable.

Experiment 009 compares these entry points before we choose what to publish.

## Canonical policy and adapters

As C-Team expands beyond Codex, avoid independently editable policy copies for each runtime.

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

## Installation/update behavior

A project must not silently mutate the user's global plugin state. Likewise, initialization should not modify files outside the repository unless the user explicitly asks to install/enable the plugin.

Current Codex plugin development behavior treats a new thread/session as a safe boundary for picking up plugin or instruction changes. Onboarding should explain only the restart actually needed:

- MCP backend marker transition: ideally no restart after Experiment 008;
- newly installed/updated plugin: fresh session may be required;
- newly created project guidance/skills: fresh session is recommended.

## Versioning

A project may eventually express:

```text
C-Team policy schema version
minimum C-Team plugin version
optional recommended version
```

Prefer warnings and guided upgrades over silently replacing an installed user plugin.

The canonical initializer must eventually understand schema upgrades and partial initialization, but Experiment 009 only needs to establish a safe deterministic model.

## Cross-runtime future

The brand meaning remains:

> **C-Team = Coding Team**

Codex is the first supported runtime, not the permanent product boundary. The `.cteam` marker, project policy and bootstrap format should remain vendor-neutral where practical.

## Open questions

- Is an empty `.cteam/` sufficient or should activation require a tiny manifest?
- What is the final canonical bootstrap package/name in npm and NuGet/.NET?
- Should npm and .NET packages embed identical templates or consume a generated common artifact?
- Can Codex eventually provide repository-scoped plugin enablement and remove most global context footprint?
- How should a repository advertise minimum/recommended C-Team versions?
- How should Claude/Copilot adapters consume the same canonical project policy?

## Related experiments/documents

- `PLUGIN_MCP_TOPOLOGY_SPIKE.md` — Experiment 007, process scope only.
- `CONTEXT_ACTIVATION_SPIKE.md` — Experiment 008, stable MCP facade, `.cteam` activation and context footprint.
- `ONBOARDING_BOOTSTRAP_SPIKE.md` — Experiment 009, agent/npx/.NET bootstrap comparison.
- `STATE_DB_LOCATOR_SPIKE.md` — Experiment 010, later optional mission-location optimization.
- `docs/host-presentation-and-context-footprint.md` — CLI/Desktop presentation and context minimization.
- `docs/self-improving-team.md` — vendor-neutral learning/policy loop.
- `docs/runtime-topology.md` — MCP/shared-core topology and lifecycle requirements.
- `EXPERIMENTS.md` — authoritative compatibility matrix.