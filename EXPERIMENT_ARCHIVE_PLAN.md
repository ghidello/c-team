# C-Team — Experiment archive and PF1 plan

## Goal

Turn the existing spike work into a durable, low-cost compatibility laboratory and complete the smallest remaining deployment-feasibility question: whether the installed C-Team plugin can carry and launch a bundled NativeAOT companion executable in-place.

This task is primarily repository housekeeping, deterministic C# test work, and one tiny plugin launch experiment. It must not repeat the expensive observability campaigns.

## Cost guardrail

Treat all existing spike conclusions and committed sanitized evidence as established unless migration reveals a contradiction.

Do **not**:

- rerun the first multi-agent observability campaign;
- rerun model-routing comparisons;
- create synthetic subagent fan-out;
- use Astra to manufacture telemetry;
- regenerate evidence that is already safely committed;
- keep a Codex session running merely to populate an experiment folder.

Use **Sol High** as the primary session for coordination. Delegate only when the task is materially cheaper/clearer that way. Most of this work should be local file inspection and deterministic implementation.

## A1 — Create the durable experiment structure

Create:

```text
experiments/
  CTeam.Experiments/
  001-app-server-observability/
  002-desktop-direct-attach/
  003-persisted-near-live/
  004-plugin-native-companion/

tests/
  CTeam.Experiments.Tests/
  fixtures/
```

Do not move production/spike code gratuitously. The goal is a clean experiment boundary, not a repository rewrite.

### Shared C# harness

`experiments/CTeam.Experiments` should be a .NET 10 console project that can host reusable deterministic probes.

Preferred command shape:

```text
dotnet run --project experiments/CTeam.Experiments -- <experiment-command>
```

The exact command names are up to the implementation, but `plugin-native-companion` (or equivalent) should cover experiment 004.

`tests/CTeam.Experiments.Tests` should use xUnit v3 and compile with the rest of the solution/test set.

New tests/probes worth preserving should be C# by default.

## A2 — Archive experiments 001–003 without rerunning them

Create one `README.md` per experiment using the contract in `EXPERIMENTS.md`.

Use existing committed sources:

- `docs/spike-findings.md`
- `docs/codex-protocol.md`
- `docs/desktop-observation.md`
- `docs/near-live-observation.md`
- `docs/evidence/*`
- existing source/tests that reproduce deterministic behavior

Reference existing sanitized evidence rather than duplicating large files unless a fixture must be colocated for deterministic tests.

### Experiment 001

Capture the app-server observability result:

- owned app-server structured events;
- hierarchy/model/token/lifecycle evidence;
- deterministic replay;
- model catalog/quota discovery;
- version/platform scope;
- retest trigger for material app-server changes.

### Experiment 002

Capture the blocked direct-attach path:

- Desktop private stdio result;
- second app-server can read durable state but is not a subscriber;
- Windows daemon/shared endpoint limitations observed at the time;
- exact retest triggers: shared listener, Desktop subscription API, Windows daemon, supported attach/discovery API.

This failed approach is especially important to preserve because it may become viable later.

### Experiment 003

Capture persisted near-live observation:

- D1 recommendation;
- watcher + length/prefix reconciliation requirement;
- persisted-record observation latency measurements;
- mission-selection ambiguity;
- child rollout hydration follow-up;
- token update cadence distinction;
- retest trigger for rollout/persistence changes.

## A3 — Audit `.cteam/` and preserve only unique evidence

Inspect the current ignored `.cteam/` contents.

Classify each top-level item as one of:

```text
TRANSIENT
UNIQUE_EVIDENCE
REUSABLE_FIXTURE
HISTORICAL_REPRODUCTION
UNKNOWN
```

Examples of likely transient material include temporary build/publish/review work directories. Do not commit those directories.

Promote only material that adds information not already represented by committed evidence or tests.

### Promotion destinations

- sanitized factual evidence → `docs/evidence/`
- deterministic test input → `tests/fixtures/`
- experiment procedure/result → experiment `README.md`
- historical script whose exact method matters → experiment-local `historical/` with a note explaining why it is retained

Never commit credentials, raw prompts, source contents from private repositories, account identifiers, unsanitized commands/output, or raw rollout data unless it is already explicitly sanitized and approved for the repo.

Produce `docs/cteam-scratch-audit.md` containing a concise table of what was found and what should be kept/deleted locally. Do not delete the user's local `.cteam/` contents as part of this task unless explicitly asked; identify disposable items instead.

## A4 — Compile experiment outputs inside the repository artifact tree

Configure experiment build/publish output so preserved experiment work does not create random scratch build directories under `.cteam/`.

Prefer the SDK's repository-level artifacts support (`ArtifactsPath` or an equivalent simple configuration) if it fits the existing solution without disrupting normal development.

Target conceptual layout:

```text
artifacts/
  bin/
  obj/
  publish/
  experiments/
```

Ensure `artifacts/` is ignored.

Do not over-engineer build customization just to achieve the exact folder names.

## A5 — Experiment 004: plugin-bundled NativeAOT companion

### Already proven

Do not repeat unless needed as a prerequisite:

- .NET 10 win-x64 NativeAOT publish succeeds on the configured machine;
- the produced standalone EXE runs when copied by itself;
- no managed runtime payload is required.

### Remaining hypothesis

Determine whether an installed C-Team plugin can contain and launch its bundled native companion **in place**, as the current user, without relying on:

- PATH installation;
- administrator rights;
- a Windows Service;
- Python or PowerShell at runtime;
- a separate MSI/setup program;
- recurring sandbox approval for ordinary invocation.

### Preferred plugin layout

Use a development fixture similar to:

```text
<plugin-root>/
  .codex-plugin/
    plugin.json
  skills/
  bin/
    win-x64/
      cteam-pf1.exe
```

Use the current supported local plugin development/install mechanism. Do not invent undocumented deployment semantics if the platform cannot do this.

### Tiny companion behavior

The PF1 companion must remain harmless and bounded. It should do only enough to establish launch context, such as:

- print a stable marker;
- print its executable/base directory;
- optionally write a small marker file to an explicitly chosen per-user writable C-Team scratch location;
- exit 0.

It must not read Codex conversations or perform observability work for this experiment.

### Questions to answer

1. Is the EXE included in the installed plugin/cache payload?
2. Can plugin/skill configuration discover its installed root without hard-coded machine paths?
3. Can the executable be launched using a path relative to the plugin installation/root?
4. Does launch work without adding anything to PATH?
5. Does it run as the current desktop user?
6. Does it require elevation?
7. Is approval required the first time?
8. Is approval required again on subsequent normal launches?
9. Where can the companion write durable per-user state safely without modifying its installed plugin payload?
10. Does plugin refresh/update replace the binary cleanly enough for a future update story?
11. Is there an obvious multi-platform binary layout that does not complicate Windows-first delivery?

### PF1 classification

Classify the observed deployment path as exactly one:

- **PF1-A — Transparent:** bundled and launched in-place with no recurring approval/elevation/setup.
- **PF1-B — One-time consent/setup:** normal use is clean after a bounded one-time user action.
- **PF1-C — Recurring approval:** technically works but recurring sandbox/approval makes it unsuitable for the intended runtime.
- **PF1-D — Unsupported:** the plugin cannot reasonably launch the bundled native companion using supported mechanisms.

Document platform/version scope and evidence. Do not generalize beyond the tested Codex/Desktop/plugin version.

## A6 — Preserve future retestability

Every experiment README must include a concrete `Retest trigger` section.

The top-level `EXPERIMENTS.md` is the compatibility matrix and must remain concise enough to scan whenever Codex is updated.

Do not add an automatic periodic retest system now.

## Tests

No paid model inference should be needed for deterministic tests.

At minimum cover applicable C# harness behavior such as:

- experiment metadata/registry validation if introduced;
- relative companion path resolution;
- process invocation and exit-code capture;
- writable-state path selection logic;
- packaging-layout validation using a fake local plugin tree;
- NativeAOT project remains publishable (this may be a documented/manual build gate rather than a unit test if CI environment lacks native toolchain prerequisites).

Do not test Codex plugin approval behavior by mocking it and then claim PF1 success; that part requires one real bounded plugin invocation.

## Deliverables

Expected repository changes include:

```text
EXPERIMENTS.md
EXPERIMENT_ARCHIVE_PLAN.md
experiments/CTeam.Experiments/...
experiments/001-app-server-observability/README.md
experiments/002-desktop-direct-attach/README.md
experiments/003-persisted-near-live/README.md
experiments/004-plugin-native-companion/README.md
tests/CTeam.Experiments.Tests/...
docs/cteam-scratch-audit.md
```

Update solution/build files and `.gitignore` only as required.

Update `EXPERIMENTS.md` with the final PF1 classification.

## Stop condition

Stop after:

1. the existing experiments are durably archived;
2. `.cteam/` has a documented keep/discard audit;
3. reusable experiment code/tests are C# and compile in-repo;
4. PF1 is classified A/B/C/D with evidence.

Do **not** proceed into production SQLite, MCP, Apps SDK UI, companion lifecycle, auto-start, installer, or routing implementation.