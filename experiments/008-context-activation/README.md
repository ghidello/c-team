# Experiment 008 — Project activation and MCP context footprint

## Purpose

Determine the smallest practical globally installed C-Team plugin surface while keeping unrelated projects dormant and preserving a smooth opt-in path for projects containing a `.cteam/` marker.

Experiment 007 established process topology. This experiment measures activation, tool discovery, context footprint, marker-transition behavior, and the boundary between backend activation and agent guidance.

## Original environment

Executed 2026-09-06 on Windows 10.0.26220 with Codex CLI 0.153.4, .NET SDK 10.0.400, and local plugin `c-team@personal`. The two bounded live calls used plugin `0.1.0+codex.20260906201401`, model `gpt-5.6-sol`, and high reasoning. The final activation-only package is `0.1.0+codex.20260906202914`.

Experiments 005–007 were authoritative inputs. Their MCP protocol, exact caller identity, Desktop reload, and process-topology workloads were not repeated.

## Hypothesis

A globally installed plugin can expose one fixed `cteam(action)` tool, start cheaply, perform no rollout work until called, return `project_not_enabled` from per-call workspace metadata, and recognize a newly created `.cteam/` marker on the next call without restarting or refreshing the tool catalog.

## Procedure

1. Add a separate `context-activation-server` mode to the shared .NET 10 NativeAOT harness. Preserve the legacy `mcp-server` mode for Experiments 005–007.
2. Advertise one fixed `cteam` tool with the stable action names `status`, `mission`, `agents`, `usage`, and `open`. Implement only `status`; this experiment does not build the product API.
3. Parse workspace roots from actual Codex `_meta.x-codex-turn-metadata.workspaces` object keys. Check only `<workspace>/.cteam`; do not consult cwd, recency, project hints, or persisted rollouts.
4. Add deterministic xUnit v3 coverage for workspace parsing, inactive and enabled markers, unresolved/ambiguous callers, zero persisted reads, one-tool schema size, and a marker transition across two calls handled by the same MCP server.
5. Publish and stage the `win-x64` NativeAOT executable in the ignored local marketplace fixture. Run two bounded real Codex CLI transitions from the same ignored nested repository: once before its first commit and once after an initial commit.
6. Record raw MCP and Codex evidence under ignored `.cteam/experiment-008/`. Use the compiled app-server harness, without model inference, to measure the final installed C-Team skill catalog.
7. Keep the historical repository skills for earlier experiment reproduction, but omit the `skills/` directory from the activation-only installed package.

No production mission API, onboarding, shared core, broker, database, UI, lifecycle manager, installer, service, routing, or second HTTP/WebSocket API was implemented.

## Success criteria

- Codex sees one compact stable tool before any C-Team call.
- The inactive MCP does no rollout/session scan before its first call.
- A real inactive caller supplies enough workspace evidence to return `project_not_enabled` without reading persisted mission state.
- Creating `.cteam/` causes the next call on the same process to return enabled without another `tools/list`.
- Global C-Team skill/instruction footprint is measured separately from the MCP tool definition.
- MCP activation and project-guidance refresh are not conflated.

## Observed result

### Inactive startup and runtime work

Codex started the NativeAOT MCP eagerly. In both live runs, the server received `initialize`, `notifications/initialized`, and `tools/list` roughly 11 ms after the harness's runtime-start timestamp, about 10.7 seconds before the first explicit C-Team call. Startup working set was 11,800,576 and 11,116,544 bytes. The timing begins at managed runtime static initialization and is not full operating-system launch latency.

Before the first call the server performed protocol initialization and serialized its tool list only. It did not inspect caller metadata, check a marker, enumerate Codex sessions, or parse a rollout. Private evidence and deterministic tests report zero persisted mission reads.

**Inactive MCP startup: eager.**

**Inactive runtime work: dormant.**

### Stable facade and visible context

The live Codex client requested `tools/list` before any explicit C-Team call and received exactly one tool named `cteam`. Its compact serialized name, description, and input schema are 292 characters and 292 UTF-8 bytes. The action enum contains `status`, `mission`, `agents`, `usage`, and `open`; only `status` has experiment behavior.

The first packaging attempt removed the manifest's `skills` field but retained the historical `skills/` directory. Persisted context showed Codex had still auto-discovered those skills. The reusable C# stager now has an activation mode that omits the skills directory while leaving repository history intact. A fresh no-inference app-server `skills/list` against the final installed package returned zero C-Team skills, zero catalog characters, and zero catalog bytes.

No inspected protocol exposed additional plugin-interface text as agent instructions, so no size is claimed for that surface.

**Visible stable tool count: 1.**

**Serialized stable tool definition size: 292 bytes / 292 chars.**

### Project resolution and inactive result

The real initialize request identified `codex-mcp-client` 0.153.4 and protocol `2025-06-18`. It declared elicitation but not Roots. Each tool call supplied exact `thread_id` and `session_id`, but neither live run supplied a workspace map. The second run used the same repository after an initial commit, ruling out the unborn Git repository as the cause.

The facade correctly refused to use its plugin-cache cwd or to parse a persisted rollout merely to recover the caller cwd. Consequently both first calls returned `project_unresolved`, with zero workspace entries, `marker_checked: false`, and `persisted_mission_read: false`. The required inactive semantic result `project_not_enabled` was therefore not achieved end to end on this CLI context.

### Marker transition in one MCP lifetime

Both live runs created `.cteam/` between two calls. Within each run, the second call used the same PID and process-start timestamp, and no second `tools/list` appeared. Because caller workspace metadata remained absent, the second result also stayed `project_unresolved`; the live host could not recognize the marker.

The deterministic protocol test uses the real Experiment 005 workspace-map shape and proves that, when one workspace root is present, the same server returns `project_not_enabled`, observes a marker created between reads, and then returns `project_enabled` without restart. This isolates the failure to host-to-plugin project evidence rather than marker caching or dynamic catalog behavior, but it is not presented as a live success.

**Marker transition without MCP restart: no** for the end-to-end live Codex path.

### Project-guidance boundary and host comparison

The backend checks the marker on every `status` call and does not need a new session for MCP mechanics. Project instructions and skills belong to the agent context assembled for a session; a newly created marker or guidance file therefore needs a new Codex session to be reliably present from the start.

**New Codex session needed for project guidance: yes.**

CLI was tested directly twice. The final package was not hot-loaded into the already-running Desktop host because Experiment 006 already established that this Desktop version retains the previous plugin payload until restart. No duplicate paid Desktop workload was created. A post-restart Desktop check remains a retest, not an inferred success.

## Current status

**A4 — Insufficient/host-dependent.** The one-tool NativeAOT facade is compact and dormant, and its backend marker transition is deterministic. Current Codex CLI project evidence is not reliable enough across contexts: this independent repository supplied neither a workspace map nor Roots, so C-Team could not distinguish inactive from enabled without violating the rule against persisted rollout parsing for activation.

A1 must not be claimed until a real caller reliably supplies a project root, or Codex adds a supported repository-scoped activation/root signal. No dynamic catalog path was tested or preferred.

## Evidence references

- [`docs/evidence/pf4-context-activation.json`](../../docs/evidence/pf4-context-activation.json)
- [`experiments/CTeam.Experiments`](../CTeam.Experiments)
- [`tests/CTeam.Experiments.Tests`](../../tests/CTeam.Experiments.Tests)
- [`experiments/005-plugin-mcp-runtime`](../005-plugin-mcp-runtime)
- [`experiments/006-caller-mission-correlation`](../006-caller-mission-correlation)
- [`experiments/007-plugin-mcp-topology`](../007-plugin-mcp-topology)

## Known limitations

The live calls exercised the activation server before the final packaging-only removal of historical skills. The final NativeAOT binary was republished, directly exercised for `tools/list`, hash-matched into the plugin payload, and the final installed skill surface was queried separately without inference. A fresh Desktop process has not yet loaded that final package.

The Windows sandbox helper failed transiently while both bounded agents attempted marker creation through their preferred tools; each ultimately created the marker through a normal PowerShell command. The C-Team MCP recorded no process error, and the helper issue does not explain the missing workspace metadata because both C-Team calls in each run completed normally.

The test does not show that workspace metadata is always absent. Experiment 005 observed one workspace entry in another context. It shows that workspace metadata is not reliable enough to be the sole activation key on the tested host/version.

## Retest trigger

Retest when reliable repository-scoped plugin activation appears, MCP Roots or caller-workspace metadata changes, Codex reliably refreshes dynamic tool catalogs, plugin skill/instruction injection behavior changes, Desktop and CLI plugin discovery changes materially, or after Desktop restarts with the final package and can cheaply exercise a real inactive project.
