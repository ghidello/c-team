---
name: inspect-codex-run
description: Inspect or replay a local C-Team Codex observability spike recording. Use when the user asks to inspect a C-Team run or verify its replay evidence.
---

# Inspect a C-Team run

Use the local C-Team console spike in the user's C-Team repository. Locate `src/CTeam.Spike/CTeam.Spike.csproj` in that repository and read its README for the current commands.

For an existing recording run:

```powershell
dotnet run --project src/CTeam.Spike -- replay <recording.jsonl>
```

If the user only wants a demonstration, use the sanitized example identified by README. Raw `.cteam/recordings/` files are development-only and may contain private session data. Keep them local.

Report hierarchy, available usage, model evidence and any missing fields. Configured model names do not establish actual model execution. Distinguish live observation from replay and persisted snapshots.

This plugin directs the console spike; it does not supply a production MCP service or UI. Stop at the spike decision gate.
