# Desktop observation experiment

Tested 2026-09-04 on Windows 10.0.26220, ChatGPT Desktop package `OpenAI.Codex_26.901.4073.0_x64__2p2nqsd0c76g0`, bundled `codex-cli 0.153.1`.

## Result

CQ10 outcome **C — persisted-state observation** is proven. Direct supported attachment to this Desktop-owned app-server is **not available through the mechanisms found in this installation**. This is a version/platform-scoped result, not a claim about every Codex deployment.

## Evidence ladder

1. `Get-CimInstance Win32_Process` outside the restricted task sandbox identified Desktop main PID 34916 and its child `codex.exe` PID 33292. Its relevant command line was `codex.exe -c features.code_mode_host=true app-server --analytics-default-enabled ...`. There was no `--listen` override. Installed app-server help identifies `stdio://` as the default.
2. `codex app-server --help` exposes stdio, Unix sockets, and WebSocket listener options, plus `daemon` and `proxy`. Their existence alone does not mean Desktop uses them. `codex app-server daemon version` returned `Error: codex app-server daemon lifecycle is only supported on Unix platforms`.
3. Scoped process/pipe discovery found no documented shared app-server endpoint for this Desktop server. Internal `codex-ipc` and browser/runner named pipes were not treated as app-server transports or reverse engineered.
4. A second independently launched stdio app-server initialized successfully using the signed-in user's home. `thread/loaded/list` returned an empty list. `thread/read` with the active Desktop root id and `includeTurns:true` succeeded, returned `source:vscode`, `model:gpt-6-astra`, one turn, and `status:{type:notLoaded}`. A second `thread/loaded/list` was still empty. No resume/start operation was sent for the Desktop task.
5. Raw request/response evidence is local in `.cteam/recordings/desktop-read.jsonl`. The task's rollout JSONL contains `session_meta`, `turn_context`, `token_usage_record`, tool records, and structured subagent source metadata. Scoped files were updated during the active mission, supporting incremental persisted observation, not only after-action reading.
6. A scoped `thread/list` using this repository's cwd, `sourceKinds:["vscode"]`, and `useStateDbOnly:true` found two candidate Desktop tasks, including this mission, without supplying its id. Both were `notLoaded` in the second server. Discovery is therefore possible; selecting the currently active Desktop task still requires a user/app hint or a separately validated freshness heuristic. Evidence: `.cteam/recordings/desktop-discovery.jsonl`.

The sandboxed launch initially failed with `Could not find home directory`; the same read-only probe with normal approved process access initialized immediately. That launch restriction is not evidence that the protocol lacks a feature.

## Interpretation and limits

The second server's loaded-thread state is independent of Desktop. Its successful read demonstrates common durable storage, not a second live subscriber. Direct attachment would require an identified shared endpoint and a causally new notification from the Desktop-owned task without resuming or starting it through C-Team. No such endpoint or event was established here.

Persisted `turn_context.model` gives the context configured for a turn, not an upstream response attestation. Inherited child history must be separated from child-owned turns. The host task's Astra context differs from the requested Sol dogfooding policy; the explicitly controlled fixtures request Sol and keep the custom-agent assignments unchanged.

Rollout paths are marked unstable by the generated schema. Flush latency, partial trailing lines, retention, history-mode changes, and inherited history require a compatibility adapter and tests before product use. This spike does not implement that production adapter or query/create a C-Team SQLite store.

Relevant public contract: [Codex App Server](https://learn.chatgpt.com/docs/app-server). Exact local schemas and experiment recordings take precedence for this installation.
