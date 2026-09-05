# C-Team — Plugin MCP Runtime Spike

## Goal

Validate whether the C-Team plugin can use its bundled NativeAOT `cteam.exe` as a stdio MCP server and whether that architecture can support project-aware, multi-session C-Team usage without recurring approval or a second custom local API.

This is a bounded compatibility spike. Do not build production SQLite, Apps SDK UI, routing analytics, installers, services, or lifecycle infrastructure.

## Context

PF1 established that a plugin can bundle and launch a NativeAOT executable in place, but durable writes to `%LOCALAPPDATA%` caused recurring approval when the executable was launched as a shell command.

The current hypothesis is that `cteam.exe` should instead be the plugin's declared stdio MCP server. Codex should own process lifecycle and communicate with it over MCP.

Target shape:

```text
Codex / ChatGPT
      │
      │ MCP
      ▼
cteam.exe
NativeAOT stdio MCP
      │
      ├─ reads persisted Codex telemetry
      ├─ exposes C-Team tools/resources
      └─ uses plugin-owned durable data only if the host provides it safely
```

## Important MCP Roots question

MCP defines a client `roots` capability. When a client declares it during initialization, an MCP server may send `roots/list` to obtain workspace/project URIs.

However, a current Codex issue reports that plugin MCP clients did not declare `roots` in Codex CLI 0.146.0, leaving plugin MCP servers without a project/workspace signal. Do not assume this is still true on the currently installed Codex version.

This spike must test the actual current handshake.

### MR1 — Initialization capabilities

Capture the real MCP `initialize` request received by C-Team and record:

- client name/version;
- protocol version;
- declared client capabilities;
- whether `roots` is present;
- any initialization metadata that identifies thread/session/workspace/project;
- relevant environment variables injected into the MCP process.

If the client declares `roots`, issue `roots/list` from the server and record the returned roots.

If it does not, record that as evidence; do not invent a project-root mechanism.

## MR2 — Bundled stdio MCP launch

Package the compiled NativeAOT experiment executable under the plugin, for example:

```text
bin/win-x64/cteam.exe
```

Declare it as a stdio MCP server using the current supported plugin MCP configuration.

Prove:

- Codex resolves the executable relative to the installed plugin root;
- no PATH modification is needed;
- no PowerShell/Python wrapper is required by C-Team itself;
- no Windows elevation is required;
- MCP initialize succeeds;
- `tools/list` succeeds;
- a harmless `cteam_ping` tool can be called;
- a second read-only tool can return basic process/plugin metadata.

## MR3 — Plugin-owned durable data

Inspect the actual environment/configuration supplied to the MCP server for plugin-owned paths such as `PLUGIN_ROOT`, `PLUGIN_DATA`, or current equivalents.

If a documented/supported writable plugin data directory is provided, test one tiny marker write there.

Prove whether:

- the data path is stable across tool calls;
- data survives MCP process restart;
- data survives plugin refresh/version change;
- no recurring approval is triggered for the write;
- concurrent C-Team MCP instances can safely see the same marker where intended.

Do not fall back to `%LOCALAPPDATA%` merely to make the test pass. If no supported writable plugin-owned path exists, record that fact.

## MR4 — Read access to persisted Codex state

From the MCP server process, perform one bounded read-only probe against the persisted Codex state already used by experiments 002/003.

Do not launch a synthetic Codex workload.

Determine whether the MCP process can:

- discover relevant persisted Codex state;
- read a known current/recent mission;
- reconstruct a minimal mission snapshot;
- do so without approval.

Return a sanitized result through one MCP tool such as:

```text
cteam_probe_current_mission
```

The tool should avoid returning prompts, command output, account data, or source code unless necessary for the experiment.

## MR5 — UI/backend contract direction

Do not build the Apps SDK UI.

Instead, validate that the MCP server can expose the kind of data the future UI needs through structured MCP tools/resources.

At minimum define experimental schemas for:

```text
cteam_get_current_mission
cteam_get_agent_tree
cteam_get_usage
```

Implement only what is necessary to prove the transport/data shape. A ping plus one real read-only mission tool is sufficient if that answers the question.

The default architecture assumption is:

> The UI and the model should consume the same C-Team MCP backend. Do not add a second localhost HTTP/WebSocket API unless a later Apps SDK experiment proves it necessary.

## MR6 — Multiple projects / sessions

Test two simultaneous Codex project/session contexts with the C-Team plugin enabled.

The experiment should determine:

- how many `cteam.exe` MCP processes are started;
- whether they are per session, per conversation, per project, or shared;
- whether their PIDs/lifetimes overlap;
- whether each process receives any distinct project/thread/root signal;
- whether each process can identify the correct mission for its caller;
- whether shared plugin data causes unsafe cross-project ambiguity;
- whether concurrent processes can coexist without file-lock or protocol problems.

Do not assume process cwd represents the project. Record cwd, but treat it as evidence only.

If opening two real projects is required, use two existing small repositories/tasks and avoid generating substantial model work. The test is lifecycle/context discovery, not model quality.

## MR7 — Mission identity fallback

If MCP initialization/roots does not provide a reliable project or thread identity, evaluate the cheapest fallback strategy without building production UX.

Candidate signals include:

- explicit tool parameter supplied by the caller;
- MCP roots if they become available;
- persisted thread/session identifiers already present in Codex state;
- cwd/project hints passed in tool arguments;
- latest-active mission heuristic with explicit ambiguity reporting.

Classify each fallback as:

```text
certain
high-confidence
ambiguous
```

Do not silently bind an MCP process to a project based on an unreliable heuristic.

## MR8 — Approval behavior

Record approval behavior separately for:

1. MCP server startup;
2. MCP handshake/tool enumeration;
3. read-only persisted-state access;
4. supported plugin-data write, if available;
5. repeated tool calls;
6. process restart;
7. second project/session instance.

The desired normal path is zero recurring approval.

## Implementation requirements

Use the existing compiled experiment harness.

New reusable experiment code must be C#/.NET 10 and compile in the repository.

Prefer:

```text
experiments/CTeam.Experiments/
tests/CTeam.Experiments.Tests/
experiments/005-plugin-mcp-runtime/
```

Do not introduce a production MCP package if a tiny standards-compliant implementation is enough for the spike. If using an MCP library materially reduces risk, keep it NativeAOT-compatible and justify it in the experiment README.

Build/publish output belongs under ignored `artifacts/`.

Raw transcripts/logs remain under ignored `.cteam/experiment-005/`.

Commit only sanitized evidence.

## Quota guardrail

This spike should consume very little model quota.

- Do not rerun experiments 001–004.
- Do not create multi-agent synthetic workloads.
- Do not use Murdock/Reviewer merely for process compliance.
- Prefer local executable/MCP tests over inference.
- Any live Codex invocation must answer a specific MR question.
- Stop immediately when MR1–MR8 have enough evidence.

## PF2 classification

At the end classify the plugin-MCP architecture exactly one of:

### PF2-A — Viable primary runtime

Bundled stdio MCP starts automatically, tools work, required read access works, project/session context can be resolved adequately, and normal repeated operation needs no recurring approval.

### PF2-B — Viable with bounded host/context limitation

MCP runtime works without recurring approval, but current project/session identity requires an explicit parameter or other bounded workaround acceptable for MVP.

### PF2-C — Transport works but recurring approval/runtime restriction remains

The stdio MCP architecture works technically, but normal read/write/tool operation still causes recurring approval or another UX-blocking restriction.

### PF2-D — Plugin stdio MCP runtime is not viable

The bundled executable cannot function as the required plugin MCP backend on the tested version.

## Multi-project sub-result

Record one separate result:

```text
M1 — safely isolated per-session/project instances
M2 — multiple instances work but caller/project identity is ambiguous
M3 — shared/single process semantics are observed and safe
M4 — multi-project behavior is unsuitable
```

Do not force the architecture to prefer single-process or multi-process behavior; observe what Codex actually does.

## Deliverables

Add/update:

```text
experiments/005-plugin-mcp-runtime/README.md
docs/evidence/pf2-mcp-runtime.json
EXPERIMENTS.md
```

Add C# experiment/test code as necessary.

The experiment README must include:

```text
Purpose
Environment
Protocol handshake evidence
Roots/project-context result
Plugin-root/data-path result
Approval behavior
Persisted-state read result
Multi-project/process result
PF2 classification
M1/M2/M3/M4 classification
Known limitations
Retest trigger
```

## Decision gate

Stop after PF2 and the multi-project classification.

Do not proceed into production C-Team MCP implementation, SQLite, Apps SDK UI, or lifecycle design automatically.
