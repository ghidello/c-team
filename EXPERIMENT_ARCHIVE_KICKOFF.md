# Codex kickoff — experiment archive + PF1

Use **Sol with High reasoning** for the primary session.

Read these files before making changes:

- `PROJECT.md`
- `PRODUCTION_REQUIREMENTS.md`
- `AGENTS.md`
- `EXPERIMENTS.md`
- `EXPERIMENT_ARCHIVE_PLAN.md`
- `docs/spike-findings.md`
- `docs/desktop-observation.md`
- `docs/near-live-observation.md`

Then execute only the experiment-archive/PF1 task in `EXPERIMENT_ARCHIVE_PLAN.md`.

Important constraints:

1. **Do not rerun the expensive observability spikes.** Existing committed findings/evidence are authoritative for archiving unless you uncover a contradiction.
2. Audit the ignored `.cteam/` directory and preserve only unique, sanitized evidence or reusable fixtures. Do not commit transient build/review/work directories.
3. Do not delete the user's local `.cteam/` content; produce a keep/discard audit instead.
4. New reusable experiment probes and tests must be C#/.NET 10 and compiled in the repository. Avoid creating new PowerShell/Python test infrastructure unless an external shell behavior itself is what is being preserved historically.
5. Establish a shared `experiments/CTeam.Experiments` console harness and `tests/CTeam.Experiments.Tests` using xUnit v3, keeping it separate from future production code.
6. Archive experiments 001–003 from existing results, including failed approaches and explicit retest triggers.
7. Execute only the bounded remaining portion of PF1: validate whether a local installed C-Team plugin can carry and launch a bundled `win-x64` NativeAOT companion in place without PATH installation or elevation, and determine first/subsequent approval behavior.
8. The PF1 executable must be harmless: marker/base-directory output, optional marker file in an explicitly selected per-user scratch location, exit 0. It must not inspect Codex data.
9. Classify PF1 as A, B, C, or D exactly as defined in the plan. Do not turn a mocked approval test into a success claim.
10. Prefer local deterministic work over model delegation. Use Face/B.A. only when useful; do not invoke Murdock/Reviewer merely for process compliance.
11. Keep production constraints intact: no Windows Service, no administrator requirement for normal use, no Python/PowerShell runtime dependency, NativeAOT companion target.
12. Stop at the plan's stop condition. Do not implement SQLite, production MCP, Apps SDK UI, lifecycle/auto-start, installer, or routing.

The goal is to leave C-Team with a clean compatibility laboratory we can cheaply retest when Codex changes, not to build more product.