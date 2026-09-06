# C-Team host presentation and context footprint

## Goal

C-Team should work well in both rich graphical hosts and text-only/headless hosts while adding as little unnecessary context as possible to coding-agent sessions that are not actively using C-Team.

Two principles drive this document:

1. **Every C-Team capability must have a useful text/structured fallback.** Rich UI is an enhancement, not a requirement.
2. **A globally installed C-Team plugin should be dormant outside projects that explicitly opt in.** Installation availability must not imply active observation or large prompt/tool-schema overhead everywhere.

## Host presentation model

C-Team should expose one canonical MCP/domain contract and allow the host to choose the presentation.

```text
                     cteam.exe
                        │
                normalized MissionState
                        │
             ┌──────────┴──────────┐
             │                     │
             ▼                     ▼
       MCP structured data      MCP App resource
             │                     │
             ▼                     ▼
        Codex CLI             Codex Desktop
      text/TUI rendering       rich widget/UI
```

### Desktop / graphical hosts

A graphical Codex host can mount a C-Team widget and present richer views such as:

```text
Current mission
Agent tree
Timeline
Usage charts
Plan progress
After Action
Evidence/provenance
```

The widget must consume the same normalized state returned by MCP tools. It must never parse Codex rollout files directly.

### Codex CLI / headless hosts

The CLI must remain fully useful without rendering the widget.

A C-Team read tool should return:

```text
structuredContent  canonical machine-readable result
content            compact human/model-readable summary
_meta              optional host/widget-only details
```

The text should be concise and useful in the terminal, for example:

```text
C-Team — Current Mission

Status: Running
Agents: 3 active / 1 complete
Plan: 3 / 5 complete
Usage: 83k total, 35k delegated

Hannibal   running
Face       complete
B.A.       running
Reviewer   pending
```

Do not build a second C-Team terminal UI merely to duplicate Codex CLI's own TUI unless a future requirement proves that necessary.

## CLI-first fallback requirement

Every production MCP tool must satisfy all of these without a widget:

- the model can understand the result from `structuredContent` and concise `content`;
- a user running Codex CLI can understand the result in the conversation;
- automation such as `codex exec` can consume the structured result;
- no important state or action exists only inside React/widget code;
- `_meta` may optimize a rich host but must not contain the only copy of information required for normal operation.

This keeps C-Team portable to future Claude/Copilot adapters and other MCP-capable hosts.

## Global installation versus project activation

A user may want C-Team installed personally so it is immediately available in repositories that use it. However, a globally enabled plugin can contribute skills/instructions and MCP tool definitions to new Codex sessions.

That creates two different forms of unwanted footprint:

```text
runtime footprint
  process startup
  filesystem scans
  watchers
  state/index work

model-context footprint
  plugin/skill instructions
  MCP tool names/descriptions/input schemas
```

C-Team should minimize both.

## Proposed project activation marker

Use a small repository marker as the initial opt-in signal. The exact schema can evolve, but `.cteam/` is a strong candidate because it is vendor-neutral and can later work with Codex, Claude and Copilot adapters.

Example:

```text
repo/
  .cteam/
    config.json          # future; optional initially
    policy/              # future
```

The minimum activation rule can initially be:

> The repository root or an accepted workspace root contains `.cteam/`.

A marker-only empty `.cteam/` directory can be enough for first-stage activation if that proves portable and easy to onboard.

Do not rely permanently on current process cwd to find it. Experiment 005 showed plugin MCP cwd can be the versioned plugin root rather than the active project. Activation must use actual caller/workspace evidence when available.

## Dormant behavior outside C-Team projects

If C-Team is globally installed but the calling project has no valid `.cteam` activation marker, normal behavior should be:

```text
C-Team installed
      ↓
project not activated
      ↓
no rollout/session scans
no watchers
no history/index writes
no shared core startup
no analytics
      ↓
small disabled/not-enabled result only if a C-Team tool is explicitly called
```

A suitable result is intentionally tiny, for example:

```json
{
  "status": "disabled",
  "reason": "project_not_enabled"
}
```

Do not return installation instructions, telemetry, project scanning diagnostics or large help text on every disabled call.

## Important limitation: `disabled` does not mean zero model-context cost

Returning `disabled` happens only **after** the model calls the tool.

For an enabled MCP server, Codex normally performs MCP tool discovery and exposes discovered tool definitions to the model. The tool name, description and JSON input schema can therefore consume model context even if every call would later return `project_not_enabled`.

Similarly, globally enabled plugin skills/instructions may be injected independently of whether C-Team considers the current repository activated.

Therefore `.cteam` gating solves runtime/data pollution, but by itself it does **not** guarantee zero prompt/tool-schema pollution.

Until Codex supports reliable repository-scoped plugin activation or another pre-tool project-aware mechanism, C-Team should optimize the globally visible surface aggressively.

## Minimal-context design

For globally installed C-Team, prefer these rules:

1. Keep plugin-level instructions extremely short.
2. Avoid globally injected long skills; use progressive/on-demand skill content where the host permits it.
3. Keep MCP tool descriptions concise.
4. Keep input schemas small and avoid duplicating prose in descriptions/schema annotations.
5. Do not expose diagnostic/experimental tools in the production plugin.
6. Do not expose a separate tool for every tiny presentation concern.
7. Measure the actual model-visible footprint before finalizing the production tool surface.

There is a real trade-off between separate discoverable tools and context size.

Current preferred semantic API remains approximately:

```text
cteam_current_mission
cteam_agent_tree
cteam_usage
cteam_after_action
```

But if measurements show that globally exposing several schemas creates material overhead, consider a **small gateway surface** rather than blindly preserving separate tools.

For example:

```text
cteam_status
cteam_query
```

or a single compact read tool with a small operation enum may be better for a globally installed plugin. This is a measured optimization question, not a decision to adopt a mega-tool today.

## True zero-footprint target

The ideal future behavior is:

```text
project without .cteam
    → C-Team plugin/MCP not injected into that project/session at all

project with .cteam
    → C-Team automatically discoverable/enabled with user-approved installation policy
```

Current Codex repository marketplaces improve discovery but do not yet give C-Team a reliable project-local enablement switch that removes a personally enabled plugin from unrelated sessions.

Track repository-scoped plugin enablement as an important onboarding/platform retest trigger.

## Shared-core interaction

If C-Team later uses a shared per-user core, project activation gating becomes even more important.

A dormant MCP facade must **not start the core** merely because Codex started the plugin process.

Preferred sequence:

```text
Codex starts MCP facade
      ↓
no heavy initialization
      ↓
first explicit C-Team call
      ↓
resolve caller/workspace
      ↓
.cteam present?
   no  → tiny disabled result; no core
   yes → connect/start core if architecture requires it
```

This keeps global installation cheap and helps guarantee that the shared core cannot become an idle zombie simply because unrelated Codex projects were opened.

## Experiment requirement

Experiment 007 should capture the global-plugin inactive-project case in addition to process topology:

- whether Codex starts the plugin MCP in a project without `.cteam`;
- whether native subagents cause additional MCP processes there;
- what MCP tool inventory Codex sees;
- whether C-Team can remain completely idle until a tool call;
- whether an explicit probe can return `project_not_enabled` without scanning persisted Codex state;
- whether the process exits cleanly with the owning context;
- any observable difference between Desktop and CLI.

Do **not** attempt to infer exact token cost from tool definitions unless the host exposes a reliable measurement. Record schema/instruction size and model-visible inventory as reproducible evidence instead.

## Production principle

> A globally installed C-Team should behave like a dormant capability, not a globally active observer.

And:

> Rich UI must enhance C-Team; it must never be required to understand or automate C-Team.