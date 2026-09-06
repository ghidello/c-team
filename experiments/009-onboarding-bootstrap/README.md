# Experiment 009 — C-Team project bootstrap and onboarding

## Purpose

Determine the smallest safe and obvious way to initialize C-Team in an existing repository, compare agent, npm, .NET, and bundled-command entry points, and establish one canonical generated footprint without publishing packages or changing the user's global plugin configuration.

## Original environment

Executed 2026-09-06 on Windows 10.0.26220 with .NET SDK 10.0.400, `dnx`/`dotnet tool exec`, bundled Node 24.19.0, NVM-managed Node 24.14.0 with npm/npx 11.9.0, and pnpm 11.19.0. The Codex process PATH exposed its bundled Node without npm/npx; CMD could invoke the NVM-managed `%NVM_SYMLINK%\npx.cmd` directly. No public npm or NuGet package was published, no C-Team plugin was installed or changed, and no Codex inference was needed for the fixture runs.

Experiment 008B's D1 result was authoritative: after `.cteam/` appears, the existing MCP can recognize project activation immediately. This experiment did not repeat that paid workload.

## Reproduction procedure

Build and verify the shared implementation from the repository root:

```text
dotnet test CTeam.Spike.sln -c Release --no-restore
dotnet publish experiments/CTeam.Experiments/CTeam.Experiments.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=true --no-restore -o artifacts/experiments/009-onboarding-bootstrap/win-x64
dotnet pack experiments/009-onboarding-bootstrap/dotnet/CTeam.Init.Experiment.csproj -c Release --no-restore -o <local-feed>
```

Run the NuGet tool from that local feed with `dnx CTeam.Init.Experiment@0.0.0-experiment init --target <fixture> --source <local-feed> --configfile <local-only-config> --no-http-cache`. Redirect `DOTNET_CLI_HOME`, `NUGET_PACKAGES`, `NUGET_HTTP_CACHE_PATH`, `APPDATA`, and `LOCALAPPDATA` to ignored scratch; disable first-use certificate generation and telemetry.

Stage the npm fixture from `npm/package.json`, `npm/bin/cteam-init.mjs`, and the published executable at `native/win-x64/cteam.exe`. Use the compiled `pack-npm-bootstrap` harness command to create a local `.tgz`, inspect its three `package/` entries, and invoke the staged launcher with Node. For each surface, run a fresh initialization and a repeat, then compare both generated files against `tests/fixtures/onboarding/fresh` byte-for-byte.

## Hypothesis

A tiny agent skill should be the primary experience when C-Team is already installed. It can obtain explicit authorization and invoke the plugin-bundled native initializer. Local npm and NuGet packages can expose equivalent manual commands from the same payload or source. A direct `cteam init` command is useful internally but should not become the primary user path while the plugin executable has no stable PATH location.

## Canonical footprint

The initializer creates or merges exactly two committed files:

```text
.
|-- .cteam/
|   `-- config.json
`-- AGENTS.md
```

`.cteam/config.json` is UTF-8 without BOM and contains only:

```json
{
  "schemaVersion": 1
}
```

The root `AGENTS.md` receives one marker-delimited C-Team section. Existing content outside the markers is preserved. The canonical golden files live under [`tests/fixtures/onboarding/fresh`](../../tests/fixtures/onboarding/fresh), and the C# tests compare their complete bytes.

The default footprint deliberately excludes `.agents/plugins/marketplace.json`. Project initialization and user-level plugin installation are separate operations. An existing repository marketplace file is preserved byte-for-byte.

## Canonical initializer behavior

The shared .NET 10 initializer is available through:

```text
cteam init --target <existing-directory> [--dry-run]
```

It returns deterministic JSON containing `status`, `changedFiles`, `plannedFiles`, `nextSteps`, and an optional `detail`. It:

- initializes a fresh repository;
- appends or refreshes one managed guidance block while preserving existing `AGENTS.md` content;
- repairs a partial configuration or guidance footprint;
- reports `already_initialized` without rewriting files on repeat;
- reports schema version 0 as `upgrade_required`, including every file the eventual upgrade would affect, without applying or discarding unknown configuration;
- rejects malformed, future-schema, duplicate-marker, misaligned-marker, and descendant reparse-point cases before writing;
- restores prior files and removes a newly created empty `.cteam` marker if a later write fails;
- constructs only the two lexically in-target paths and rejects pre-existing descendant reparse points.

The output always states that plugin installation is separate and recommends a fresh Codex session when the new project guidance should be loaded from the beginning.

## Entry-point comparison

| Entry point | Actual experiment | Payload | Result | Friction and maintenance |
| --- | --- | ---: | --- | --- |
| Agent/plugin skill | A plugin-shaped fixture applied the skill's relative-path convention and invoked the bundled NativeAOT initializer directly. | skill plus existing native payload | Canonical fresh bytes | Leading in-agent candidate; real installed-skill discovery, approval, and agent execution remain untested. |
| npm/npx | A zero-dependency, three-file package carries a 691-byte Node launcher and the `win-x64` native initializer. The compiled harness created the local tarball; CMD invoked it through NVM-managed npx with `--offline`. | 1,445,241-byte `.tgz`; 1,093 bytes committed package source | First run `initialized`; repeat `already_initialized`; canonical bytes | The host process did not inherit NVM's npm/npx path, so the experiment used `%NVM_SYMLINK%\npx.cmd` explicitly. Carrying native payloads preserves one implementation but requires per-platform packages. |
| .NET `dnx` | A local-only `PackAsTool` package compiles the canonical C# source directly. `dnx` consumed only the local feed and used workspace-local caches. First and second runs returned `initialized` and `already_initialized`. | 26,886-byte `.nupkg`; zero external package dependencies | Canonical fresh bytes; repeat idempotent | Clean one-shot command with no global tool/PATH install, but requires the .NET 10 SDK and writes ordinary package-cache data. |
| Bundled `cteam init` | The 3,189,248-byte `win-x64` NativeAOT experiment executable ran directly and initialized the fixture. | already present in plugin package | Canonical fresh bytes | Exact implementation and no extra runtime, but Experiment 005 found no supported stable PATH or plugin-root environment for a developer terminal. Exposing it directly would reintroduce path or installer work. |

The npm package intentionally remained `private` and local for the experiment. Its package source contains no dependencies. The executable payload accounts for almost all of its archive size.

The .NET 10 SDK's documented `dnx`/`dotnet tool exec` behavior is one-shot package execution without permanent installation. npm documents `npx` as the npm-exec path for running local or remote package commands. These platform semantics were checked against [Microsoft's `dotnet tool exec` documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-exec) and [npm's `npm exec` documentation](https://docs.npmjs.com/cli/v11/commands/npm-exec/).

## Required scenario results

Fresh, existing-AGENTS, existing-marketplace, repeated, partial, dry-run, schema-upgrade, malformed/future-schema, failed-write rollback, and reparse-point cases passed deterministic tests. The sanitized tree, representative AGENTS diff, and aggregate scenario results are under [`fixtures`](fixtures).

Four separately invoked fresh targets—the staged skill payload, npm payload, local `dnx` package, and direct NativeAOT command—matched the same two golden files byte-for-byte. No initializer created or modified a file outside its selected target.

The successful `dnx` run redirected its CLI home, NuGet packages, HTTP cache, APPDATA, and LOCALAPPDATA into ignored experiment scratch and used a local feed with no HTTP source. An earlier discarded isolation attempt omitted the certificate-generation guard; before the initializer ran, the .NET CLI reported its first-use ASP.NET development-certificate action and then failed on sandboxed user NuGet configuration access. The controlled rerun disabled certificate generation and telemetry and completed locally. This is package-host setup behavior, not initializer behavior, and is retained as a reproduction warning.

The npm/pnpm dry-run reported the intended three package files. Sandbox policy prevented pnpm and Windows tar from creating the `.tgz`, so the compiled C# harness created the equivalent npm archive and a deterministic tar-layout test verifies its `package/` prefix and entries. A follow-up invoked that local archive through CMD and NVM-managed npx 11.9.0 with `--offline`. Its cache stayed under ignored experiment scratch; the first and repeat results matched the golden fixture.

## Current status

**O4 — More evidence needed.** The canonical initializer, direct native path, local `dnx` package, and exact offline npx transport all produced equivalent deterministic files. The agent-first route remains the leading product hypothesis, but this run did not install, discover, and execute the skill in a real agent session. That missing observation is material to choosing the agent route over the package alternatives.

The recommended product boundary is:

```text
initialize project
    → create/merge .cteam/config.json and AGENTS.md only
    → current MCP activation changes immediately
    → fresh agent session loads guidance from the start

install or enable plugin for user
    → separate explicit operation
```

## Evidence references

- [`docs/evidence/pf5-onboarding-bootstrap.json`](../../docs/evidence/pf5-onboarding-bootstrap.json)
- [`tests/fixtures/onboarding`](../../tests/fixtures/onboarding)
- [`experiments/CTeam.Experiments`](../CTeam.Experiments)
- [`tests/CTeam.Experiments.Tests`](../../tests/CTeam.Experiments.Tests)
- [`experiments/008b-context-activation-db`](../008b-context-activation-db)

## Known limitations

The experiment did not publish packages, install the skill globally, or use a paid model call to exercise its prose. It validates the skill's path and command mechanics in an isolated plugin-shaped fixture. The skill validator bundled with this Codex installation could not start because its Python environment lacked PyYAML; the skill's frontmatter and structure were inspected directly.

The initializer rechecks planned files immediately before each write and refuses to overwrite a changed file. Together with reparse-point preflight, this reduces ordinary concurrent-edit risk, but it is not a security boundary against a hostile process swapping a directory junction between a check and an OS write. A production-grade adversarial guarantee would require handle-based no-follow filesystem operations. This experiment makes no such guarantee.

Only `win-x64` NativeAOT packaging was tested. Other npm payloads would need platform selection. The package sizes contain the broader shared experiment harness and should not be treated as optimized production initializer sizes.

Repository marketplace advertisement remains optional and was intentionally not designed into the canonical initializer. A future onboarding mission may add an explicit flag if actual users need it.

## Retest trigger

Retest when Codex adds repository-scoped plugin installation or activation, npm or .NET one-shot execution changes materially, C-Team's project schema changes, the plugin exposes a stable supported executable path, or cross-runtime Claude/Copilot onboarding establishes a better common convention.
