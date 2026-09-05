# Experiment 005 — Plugin MCP runtime

## Purpose

Determine whether Codex can automatically launch the plugin-bundled .NET 10 `win-x64` NativeAOT executable as a stdio MCP server, use it without recurring approval, read persisted C-Team mission state, and keep simultaneous project/session contexts safe.

## Original environment

Executed 2026-09-05 on Windows 10.0.26220 with .NET SDK 10.0.400, Codex CLI 0.153.4, and the local `c-team@personal` plugin. The final live read used plugin `0.1.0+codex.20260905214403`; bounded-scan, protocol-error, schema and redaction refinements were hash-verified as `0.1.0+codex.20260905220449` without another model invocation. Experiments 001–004 were treated as established; no earlier observability workload was repeated.

## Hypothesis

Declaring `./bin/win-x64/cteam.exe mcp-server` in the plugin's `.mcp.json` will let Codex own the companion lifecycle and invoke structured tools without the recurring approval seen when Experiment 004 shell-launched the companion to write `%LOCALAPPDATA%`.

## Procedure

1. Extend the shared C# experiment harness with a newline-delimited JSON-RPC stdio server, MCP initialize/tool handling, allowlisted private evidence, and a bounded persisted-rollout probe.
2. Publish one self-contained NativeAOT executable, stage it as both the historical PF1 filename and `bin/win-x64/cteam.exe`, install the local plugin, and verify the installed hash.
3. Capture the real Codex initialize request. Request `roots/list` only if the client declares `roots`.
4. Invoke a harmless ping, runtime/environment inspection, supported plugin-data check, and one sanitized current-mission read through the installed server.
5. Launch two bounded Codex CLI contexts simultaneously from the repository and its existing nested fixture. Hold both pings for ten seconds and record process overlap, per-call metadata, cwd, project signals, errors, and approval behavior.
6. Keep raw commands, identifiers, paths, and JSONL under ignored `.cteam/experiment-005/`; preserve only allowlisted facts here and in `docs/evidence/pf2-mcp-runtime.json`.

No test launched an app-server directly or repeated the paid workloads from Experiments 001–004. Each Codex CLI invocation answered MR1–MR8 protocol, approval, read-access, restart, or concurrency questions.

## Success criteria

- Codex automatically starts the bundled executable as stdio MCP without a wrapper, PATH installation, Windows elevation, or per-tool approval.
- Initialize, tool discovery, structured results, and a bounded persisted-state read work through the installed plugin.
- Actual Roots capability and plugin-owned environment/storage support are recorded without assumptions.
- Two simultaneous contexts reveal process count, overlap, caller/project signals, isolation, and cross-project limitations.

## Observed result

### MCP handshake

Codex automatically launched the relative executable from the versioned plugin cache. The real client was `codex-mcp-client`, titled `Codex`, version 0.153.4, requesting MCP protocol `2025-06-18`. Its initialize capabilities contained `elicitation` with `form` and `url`; it did not declare `roots`. The server therefore did not request `roots/list` and returned protocol `2025-06-18` plus a tools capability.

Initialize carried no project or thread identity. The server cwd was the versioned plugin root and `AppContext.BaseDirectory` was its bundled `bin/win-x64` directory, confirming that cwd does not identify the active project.

### Launch and approval behavior

The final installed `cteam.exe` was the 2,736,640-byte self-contained NativeAOT payload staged by the repository harness; its published and installed SHA-256 hashes matched. It required no wrapper, PATH registration, managed .NET runtime, Windows elevation, or shell launch.

The first successful plugin launch, a later plugin refresh/restart, the current-mission read, and both simultaneous contexts exposed and ran MCP tools without a Codex approval prompt. The installed-plugin command itself was an explicit development action; it is separate from recurring runtime/tool approval.

### Environment and durable storage

Codex 0.153.4 supplied neither `PLUGIN_ROOT`, `PLUGIN_DATA`, nor an observed equivalent to the MCP process. Only the two explicitly inherited `CTEAM_*` experiment variables appeared in the allowlisted environment capture. The plugin-data tool consequently returned `available: false` and performed no write. No `%LOCALAPPDATA%` fallback was used, so Experiment 004's PF1-C restriction was not disguised as storage success.

### Tools and read access

`cteam_ping` returned structured content with a process id and experiment context label. The future UI-facing names `cteam_get_current_mission`, `cteam_get_agent_tree`, and `cteam_get_usage` advertise an experimental JSON output schema; no Apps SDK UI or second HTTP/WebSocket API was built.

The installed `cteam_probe_current_mission` read the Desktop-owned active rollout without approval and returned one high-confidence project-hint candidate with a sanitized mission key, `running` status, nine observed agents, configured/turn-context model `gpt-5.6-sol`, high effort, and cumulative usage. This field does not claim upstream execution identity. The tool did not return prompts, commands, source, account data, or raw identifiers.

The first probe implementation failed on Desktop's open writer handle because `File.ReadLines` did not share writes. The final implementation opens rollouts with `FileShare.ReadWrite | FileShare.Delete` and tolerates an incomplete appended JSON line, matching the already-established Experiment 003 source behavior. It checks a fixed 31-day session-directory window, encounters at most 64 rollout files, and reads fixed file-length snapshots capped at 64 MiB per file and 256 MiB total. It exposes `scanned_files`/`scan_truncated` and treats a truncated project-hint scan as ambiguous. Deterministic tests cover the open writer, partial line and scan bound.

### Tool-call caller signals

Although initialize had no Roots support, every `tools/call` request carried Codex-specific `_meta.x-codex-turn-metadata`. Its `session_id` and `thread_id` both exactly matched the invoking context's `thread.started` id. It also carried the plugin id and a workspace map when Codex had one. This is useful exact caller identity, but it is an extension on individual calls rather than standard MCP initialization context.

The experimental server intentionally did not turn that metadata into production routing. The read tool accepted an explicit `mission_id` or `project_hint`; project-hint selection reports ambiguity rather than silently choosing when multiple rollouts share a cwd.

### Multi-project/process result

Two separate, simultaneous Codex CLI client processes each started one distinct `cteam.exe` child about 6 ms apart. Their evidence intervals overlapped for at least 19.7 seconds; each child survived for at least 19.7/22.0 seconds respectively, used the same plugin-cache cwd, and exited with its owning Codex client. No shared process, file lock, protocol collision, or cross-context result was observed. This does not determine how multiple conversations inside one long-lived Desktop host are mapped to MCP processes.

Both calls had exact, distinct session/thread ids. The top-level repository context included one workspace entry. The nested fixture context included no workspace entry, and the client declared no Roots capability. The concurrent contexts were deliberately ephemeral, so they did not supply two persisted rollout targets; the concurrent probes also preceded the final open-writer fix. Therefore isolation and caller identity are proven, while metadata-only cross-project persisted-state attribution is not.

## Current status

**PF2-B — Viable with bounded host/context limitation.** Plugin-managed stdio transport, NativeAOT launch, structured tools, persisted-state reads, restarts, concurrency, and approval-free normal operation work. A supported plugin-owned durable path was not supplied, and project-to-rollout resolution still needs an explicit mission/project parameter or a small adapter that consumes per-call Codex metadata.

**M2 — multiple instances work but caller/project identity is ambiguous.** One independent MCP child per tested Codex client and exact caller thread ids are available, but project metadata was incomplete in one simultaneous context and cross-project persisted attribution was not proven.

## Evidence references

- [`docs/evidence/pf2-mcp-runtime.json`](../../docs/evidence/pf2-mcp-runtime.json)
- [`.mcp.json`](../../.mcp.json)
- [`experiments/CTeam.Experiments`](../CTeam.Experiments)
- [`tests/CTeam.Experiments.Tests`](../../tests/CTeam.Experiments.Tests)
- [`docs/near-live-observation.md`](../../docs/near-live-observation.md)

## Known limitations

This version-scoped result used Codex CLI 0.153.4 and a local personal-marketplace plugin on Windows. The two-context lifecycle test used ephemeral CLI sessions to avoid creating synthetic persisted missions. It therefore did not prove automatic metadata-only mapping for two concurrently persisted Desktop tasks. Process-stop messages were not delivered before the host closed stdio, so lifetimes are lower bounds derived from first/last server evidence plus confirmation that both PIDs were gone after their clients exited. Plugin-owned durable storage remains unavailable in the observed environment.

The three UI-facing tools share a minimal experimental mission-snapshot shape. They establish MCP transport and schema direction only; they are not the production C-Team backend. The bounded recent-file scan can omit a mission outside its 31-day/64-file window and reports truncation when the file cap or byte budget is reached; production indexing remains outside this spike.

## Retest trigger

Retest when Codex changes MCP protocol/client capabilities, declares Roots, changes `_meta.x-codex-turn-metadata`, supplies a supported plugin-owned durable path, changes plugin cache/lifecycle/approval behavior, or changes persisted rollout location/sharing. The immediate bounded follow-up for stronger classification is two persisted Desktop contexts, including a nested workspace, resolved from MCP metadata alone without an explicit project hint.
