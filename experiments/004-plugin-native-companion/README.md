# Experiment 004 — Plugin-bundled NativeAOT companion

## Purpose

Determine whether a locally installed C-Team plugin can carry and launch a harmless bundled .NET 10 `win-x64` NativeAOT executable in place without PATH installation or Windows elevation, and measure first and subsequent Codex approval behavior.

## Original environment

Executed 2026-09-05 on Windows 10.0.26220 with .NET SDK 10.0.400, Codex CLI 0.153.4, the configured local `personal` marketplace, and C-Team plugin `0.1.0+codex.20260905202737`. A final source refinement was refreshed as `0.1.0+codex.20260905203506`. The earlier standalone NativeAOT prerequisite was established on Codex 0.153.1; it was rebuilt only because the executable was required for this plugin-package test.

## Hypothesis

The installed plugin cache preserves a `bin/win-x64/cteam-pf1.exe` payload, an installed skill can derive the plugin root from its own path, and the executable can write a marker in `%LOCALAPPDATA%/C-Team/experiments/004-plugin-native-companion` without recurring approval.

## Procedure

1. Publish `experiments/CTeam.Experiments` as a self-contained `win-x64` NativeAOT executable into ignored `artifacts/pf1/win-x64`.
2. Stage the manifest, skills and executable into the existing ignored local marketplace fixture and refresh its cachebuster with the supported plugin-development helper.
3. Install `c-team@personal`, verify the installed executable hash matches the published file, and verify `cteam-pf1` does not resolve through PATH.
4. Make one real bounded `codex exec --approve-for-me` invocation that uses the installed skill and launches the installed executable twice as separate commands, with marker names `first` and `second`.
5. Preserve the raw JSONL privately under ignored `.cteam/experiment-004/` and commit only allowlisted facts.

The executable prints a stable marker, base directory and user, optionally writes one selected marker file, and performs no Codex inspection. The Codex Windows command runner used PowerShell to start the native executable; the executable itself has no PowerShell, Python or managed-runtime dependency.

## Success criteria

- Installed payload contains the same native executable and the skill resolves it relative to the installed root.
- Launch requires neither PATH modification nor Windows elevation and runs as the current desktop user when approved.
- The selected per-user marker is written and the process exits 0.
- First/subsequent approval behavior is observed, not mocked.

## Observed result

The installed cache contained the 2,011,136-byte executable with the same SHA-256 hash as the published artifact. The installed skill resolved its own versioned cache root and launched `bin/win-x64/cteam-pf1.exe`; PATH lookup remained false.

On both first and subsequent commands, the executable itself started inside the sandbox and printed its marker/base directory. Its `%LOCALAPPDATA%` marker write was denied. Automatic approval review then allowed each command to run as the current desktop user, write its distinct marker and exit 0. Approval from the first command did not carry over to the second. No Windows elevation or installer was involved.

After the approval measurement, the probe was refined to report a denied marker write without throwing an unhandled exception. Plugin refresh installed that changed payload as `0.1.0+codex.20260905203506` in a new versioned cache directory; the final published and installed hashes matched, a no-marker launch exited 0, and the prior cache-version directory was no longer present. The approval experiment was not repeated because the write target and policy-sensitive command were unchanged. `bin/<runtime-identifier>/` is the straightforward multi-platform layout, but only `win-x64` was tested.

## Current status

**PF1-C — Recurring approval.** Bundling, installed-root discovery and in-place execution work, but durable per-user state outside the workspace required approval on every tested command. This is unsuitable for the intended unattended normal runtime on the tested Codex version.

## Evidence references

- [`docs/evidence/pf1-plugin-launch.json`](../../docs/evidence/pf1-plugin-launch.json)
- [`docs/near-live-observation.md`](../../docs/near-live-observation.md)
- [`skills/pf1-native-companion/SKILL.md`](../../skills/pf1-native-companion/SKILL.md)
- [`experiments/CTeam.Experiments`](../CTeam.Experiments)
- [`tests/CTeam.Experiments.Tests`](../../tests/CTeam.Experiments.Tests)

## Known limitations

The real approval test used Codex CLI 0.153.4 from a Desktop-managed workspace; it did not hot-reload the skill into the already-running Desktop task. The test used automatic approval review, not a human approval dialog. It tested two launches in one ephemeral Codex invocation and one per-user write location. It did not test lifecycle, auto-start, a long-running process, other runtime identifiers or production observability.

## Retest trigger

Retest when Codex changes plugin execution trust, sandbox writable roots, approval persistence, local plugin packaging/cache behavior, native-tool invocation, or support for declared bundled executables. Also retest on the first supported mechanism that grants a plugin-owned durable per-user directory without recurring approval.
