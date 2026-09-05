# Minimal plugin validation

Plugin: `c-team@personal`. Source manifest and skill are in the repository root; this plugin has no MCP/app/hook services.

The plugin-creator scaffold generated a local validation marketplace under `.cteam/plugin-market`, with its plugin source copied from the root. The marketplace manifest is `.cteam/plugin-market/.agents/plugins/marketplace.json`. This is an ignored development fixture, not a production distribution registry.

Verified with installed CLI 0.153.1:

1. `validate_plugin.py .` passed and `quick_validate.py skills/inspect-codex-run` passed. The bundled Python lacked PyYAML; a workspace-local dependency was used for validation.
2. `codex plugin marketplace add <repo>/.cteam/plugin-market --json` registered the explicit local marketplace named `personal` (no prior marketplace with that name was registered).
3. `codex plugin add c-team@personal --json` installed version 0.1.0 into the user's normal plugin cache.
4. Fresh app-server `skills/list` with `forceReload:true` returned enabled `c-team:inspect-codex-run`, scope user, pluginId `c-team@personal`, with a cache-backed SKILL.md path. Raw discovery evidence is `.cteam/recordings/plugin-discovery.jsonl`.
5. `read_marketplace_name.py` validated the marketplace identity; `update_plugin_cachebuster.py` generated `0.1.0+codex.20260904192306`. Reinstalling via the same `codex plugin add` command returned the new cache path.
6. A normal fresh Sol task explicitly invoked `$c-team:inspect-codex-run`. It read the refreshed cache's SKILL.md, executed `dotnet .cteam/final-build/CTeam.Spike.dll replay docs/evidence/example-run.jsonl` with exit code 0, and reported that replay succeeded with effective model unknown. Evidence: `.cteam/recordings/plugin-invocation.jsonl`.

The installed validation copy remains available locally. Deleting `.cteam/plugin-market` would remove its reinstall source; the durable plugin source remains the repository root. New tasks pick up refreshed skills; this test did not assume an already-running task would hot-reload them.

To reproduce packaging, use the current plugin-creator scaffold/validation scripts, copy the root manifest and skills into a local marketplace source, register that explicit marketplace, and install `c-team@<marketplace>`. Keep this separate from telemetry validation: a discovered skill does not prove Desktop live attachment.

## PF1 follow-up

On 2026-09-05, Codex CLI 0.153.4 installed the same marketplace fixture with `skills/pf1-native-companion/SKILL.md` and `bin/win-x64/cteam-pf1.exe`. The installed binary matched the published NativeAOT artifact, resolved relative to the cache-backed skill, and launched without PATH installation or elevation. Durable `%LOCALAPPDATA%` marker writes required separate approval for both tested commands, producing **PF1-C — Recurring approval**. Details and scope are in [experiment 004](../experiments/004-plugin-native-companion/README.md).
