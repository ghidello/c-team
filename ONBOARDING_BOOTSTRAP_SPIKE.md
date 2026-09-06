# Experiment 009 — C-Team project bootstrap and onboarding

## Purpose

Design and validate the simplest way for a developer or coding agent to initialize C-Team in a repository that does not yet contain `.cteam/`.

This experiment should compare an agent-driven bootstrap path with small developer-facing bootstrap commands, including JavaScript/npm and .NET-friendly options, without committing the product to one ecosystem prematurely.

## Product goal

A project should be able to move from:

```text
C-Team not initialized
```

to:

```text
.cteam/ present
project policy/guidance present
C-Team plugin discoverable/usable
```

with one obvious action and no service, installer wizard, PATH surgery, or hand-edited plugin cache files.

The bootstrap must be idempotent and safe to run in an existing repository.

## Candidate entry points

Evaluate at least these concepts:

### O1 — Agent/plugin skill bootstrap

A tiny globally available C-Team onboarding skill detects `project_not_enabled`, explains what initialization will create, asks the user for approval, and then creates the C-Team project environment.

This is the most natural path when the user is already inside Codex/another coding agent.

### O2 — npm/npx bootstrap

Example shape only:

```text
npx c-team init
```

or a scoped package if the unscoped name is unavailable/undesirable.

The package should be intentionally tiny and primarily bootstrap project files. It must not make Node/npm a C-Team runtime dependency.

### O3 — .NET bootstrap

Evaluate a .NET-native equivalent suitable for developers without Node, for example:

```text
dnx CTeam.Init
```

or another current .NET package/tool mechanism that can execute without requiring a permanent global install.

Prefer a one-shot package/application experience over asking users to install a global dotnet tool merely to create project files.

### O4 — bundled `cteam` command

If the plugin/runtime binary is already locally accessible in a supported way, evaluate whether:

```text
cteam init
```

can be exposed without reintroducing PATH/installer complexity. This is secondary; do not compromise plugin-contained deployment just to obtain a CLI command.

## Canonical initializer

All entry points must call or reproduce one canonical initialization model. Do not let npm, .NET, and the agent skill generate subtly different project layouts.

Preferred architecture:

```text
canonical C-Team init specification/template
              │
      ┌───────┼────────┐
      ▼       ▼        ▼
 agent skill  npx    .NET bootstrap
```

Where practical, generated files should be deterministic so different entry points can be fixture-compared byte-for-byte.

## Initial project footprint to test

Keep it minimal. Candidate shape:

```text
.cteam/
  config/policy marker

optional project guidance
  AGENTS.md addition or dedicated referenced file

optional repository marketplace
  .agents/plugins/marketplace.json
```

Do not decide that every initialized project must contain every item. The experiment should determine the smallest useful committed footprint.

The `.cteam/` directory itself is the preferred activation marker unless Experiment 008 disproves that design.

## Required behaviors

Test:

- fresh repository initialization;
- repository that already has `AGENTS.md`;
- repository that already has `.agents/plugins/marketplace.json`;
- repeated initialization (idempotence);
- partially initialized project;
- upgrade/schema-version path at least as a dry-run design;
- no network requirement after package/bootstrap payload is already available, where feasible;
- no modification outside the target repository unless the user explicitly requests plugin installation;
- clear separation between **initialize project** and **install/enable plugin for user**.

## User experience

Desired agent flow:

```text
C-Team status → project_not_enabled
        ↓
"Initialize C-Team in this repository?"
        ↓ user agrees
create/merge project files
        ↓
C-Team backend recognizes .cteam immediately
        ↓
if guidance/skills changed:
  recommend starting a fresh Codex session
```

Desired terminal flow should be equivalently simple:

```text
npx c-team init
```

or:

```text
<.NET one-shot equivalent> init
```

and should print exactly what changed plus any next step such as installing/enabling the plugin or starting a fresh agent session.

## Context and installation principle

Initialization and global plugin installation are separate operations.

A repository may advertise C-Team through its marketplace metadata, but it must not silently mutate a user's global plugin state.

Likewise, a globally installed C-Team plugin should remain dormant until `.cteam/` exists or the user explicitly asks to initialize the project.

## Classification

Finish with a bootstrap recommendation:

- **O1 — Agent-first + portable bootstrap packages**: skill is primary UX; npx and .NET one-shot commands are equivalent manual entry points.
- **O2 — Package-first**: one package ecosystem provides a clearly superior bootstrap and other paths should wrap/defer to it.
- **O3 — Bundled runtime init**: the plugin/runtime can expose `cteam init` cleanly without installation friction.
- **O4 — More evidence needed**.

The expected preference is O1, but the experiment must compare actual friction, generated output, packaging size and maintenance burden.

## Evidence

Publish:

```text
experiments/009-onboarding-bootstrap/README.md
docs/evidence/pf5-onboarding-bootstrap.json
```

Include generated fixture trees/diffs rather than real user/project paths.

## Implementation constraints

- Bootstrap implementation may use Node or .NET as the subject of the experiment, but neither becomes a required production runtime for `cteam.exe`.
- Prefer tiny packages with no unnecessary dependency tree.
- Do not publish public npm/NuGet packages merely to run the experiment unless explicitly approved.
- Do not alter the user's global Codex/plugin configuration during fixture tests.
- Keep initializer logic deterministic and testable.

## Retest triggers

- Codex adds repository-scoped plugin installation/activation;
- npm/.NET one-shot execution mechanisms change materially;
- C-Team project schema changes;
- cross-runtime Claude/Copilot onboarding establishes a better common convention.

## Stop condition

Stop after selecting O1/O2/O3/O4 and documenting the canonical generated project footprint. Do not build a polished installer or publish packages as part of this spike.