# Protocol evidence and reproduction

Installed executable: `%LOCALAPPDATA%\OpenAI\Codex\bin\1e3e57cdf0634c02\codex.exe`, version `codex-cli 0.153.1`.

Generate version-matched diagnostic schemas:

```powershell
codex app-server generate-ts --experimental --out .cteam/schema
codex app-server generate-json-schema --experimental --out .cteam/schema-json
```

Schema generation is marked experimental by the installed CLI. Runtime experimental opt-in is separate from generating an inclusive schema.

## Wire contract

Stdio transports newline-delimited JSON request, response, and notification envelopes. Initialize with `clientInfo` name `cteam`, title `C-Team`, version `spike`, then send `initialized`. The capability probe returned `Codex Desktop/0.153.1 ... (cteam; spike)` and the normal Codex home. The early discovery probe enabled `experimentalApi:true`; the initial .NET fixture did not.

| CQ | Local schema / messages | Semantics to preserve |
| --- | --- | --- |
| 1 | `Thread`, `SessionSource`, `SubAgentSource`, `ThreadItem.subAgentActivity` | Thread/session identity, explicit parent, role and nickname; task path is not role. Initial v2 run sent child activities without child `thread/started`. |
| 2 | `Thread.model`, `Thread.reasoningEffort`, `ThreadStartResponse`, `TurnStartParams`, `ModelReroutedNotification` | Thread fields explicitly describe configuration, not per-turn execution. Reroute target is routing evidence, not proof of completed execution. |
| 3 | `thread/tokenUsage/updated`, `ThreadTokenUsage`, `TokenUsageBreakdown` | `total` and `last` are separate snapshots. Preserve reported totals; cached input is included in input and reasoning output in output. Includes cache-write and model-context-window fields. |
| 4 | `thread/status/changed`, `turn/started`, `turn/completed`, `Turn` | Runtime thread status is distinct from latest turn outcome. Turn carries start/completion and `durationMs`. |
| 5 | `turn/plan/updated`, `TurnPlanUpdatedNotification` | Step text/status and optional explanation. A schema alone does not establish that this runtime emits it. |
| 6 | `item/started`, `item/completed`, command/file/MCP/dynamic/collaboration item variants | Deduplicate by item identity, preserve status/exit code/duration and file metadata. Failures before command execution may appear elsewhere. |
| 7 | `turn/diff/updated` | Aggregate unified-diff metadata per turn, replace cumulative updates; raw code only in development recordings. |
| 8 | `review/start`, `ReviewStartParams`, `enteredReviewMode`, `exitedReviewMode` | Detached review returns a review thread id; custom reviewer is a separate role-based child. |
| 9 | Recorder + shared mapper | Replay recorded order, including request correlation and observed timestamps; compare complete final snapshots. |
| 10 | `thread/read`, `thread/loaded/list`, local process transport | Stored read is not subscription; see desktop-observation.md. |
| 11 | `model/list`, `account/read`, `account/rateLimits/read` | Enumerate all pages with `includeHidden:true`; quota snapshots are account-wide and cannot establish per-agent attribution by timing. |

## Catalog and quota artifacts

`docs/evidence/model-catalog.json` preserves every catalog entry returned by the signed-in app-server, including capabilities and supported reasoning efforts. It is an observed catalog, not a guarantee every listed/hidden model will execute successfully. No inference tests of additional models were run.

`docs/evidence/quota-buckets.json` is an allowlisted derivative with account/reset-credit identifiers and usage percentages removed. The explicit Spark label proves an observable separate named bucket. No protocol join from every turn's execution to a quota bucket was established.

Model context windows are not part of this installation's `model/list` result. Runtime token-usage notifications are the execution-context-window source. Local `models_cache.json` has separate cached capability fields, which must not be attributed to the catalog response.

Public reference: [Codex App Server](https://learn.chatgpt.com/docs/app-server). The checked-in findings rely primarily on generated local types and recorded behavior.
