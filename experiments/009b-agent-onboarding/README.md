# Experiment 009B — Real agent-first onboarding validation

## Purpose

Validate the installed C-Team onboarding skill in one real Codex Desktop task without reopening Experiment 008B activation mechanics or Experiment 009 initializer/package work.

## Environment and preconditions

Executed 2026-09-07 with Codex CLI 0.153.4 and ChatGPT Desktop. One fresh projectless Codex task used configured model `gpt-5.6-sol` with high reasoning in a disposable directory that initially had no `.cteam/` marker.

The local C-Team marketplace source was updated through the plugin cachebuster/reinstall workflow to `0.1.0+codex.20260906220726`. The package contained the Experiment 009 canonical NativeAOT initializer and one onboarding skill. Published, marketplace-source, and installed executable hashes matched. The plugin and skill validator scripts could not start because their bundled Python environment lacked PyYAML; real installation and app-server discovery provided the decisive ingestion checks.

Raw task ids, paths, prompts, command lines, configuration snapshots, and protocol recordings remain under ignored `.cteam/experiment-009b/`.

## Procedure

1. Add the narrowly scoped `initialize-c-team` skill and current NativeAOT initializer to the existing local plugin source, update its cachebuster, and reinstall `c-team@personal`.
2. Record the installed skill size and a post-install configuration baseline.
3. Create one fresh Desktop task in a disposable projectless directory with the natural prompt `Initialize C-Team in this project`.
4. Wait for the concrete pre-mutation explanation and approve once.
5. Inspect the actual task trace to determine which installed skill and executable path Codex used.
6. Compare `.cteam/config.json` and `AGENTS.md` with Experiment 009's golden files; inspect repository marketplace and user plugin configuration state.
7. Call the C-Team status surface in the same task/MCP lifetime.
8. Send the natural initialization request again and verify no rewrite.
9. Run Experiment 001's existing no-inference `skills/list(forceReload:true)` harness in a separate fresh app-server process to measure the installed catalog surface.

No named agents, npx/dnx reruns, architectural work, or synthetic telemetry tasks were used.

## Results

### Installed catalog footprint

The no-inference app-server harness discovered exactly one C-Team skill through ordinary `skills/list`:

```text
c-team:initialize-c-team
```

The name-plus-description catalog measurement was 129 characters and 129 UTF-8 bytes. The installed `SKILL.md` was 1,450 bytes and its discovery description was 104 characters/bytes. This remains a narrow global surface.

### Desktop discovery and package refresh

The fresh Desktop task was created after the new plugin version was installed, but it inherited C-Team package `0.1.0+codex.20260906191756` from the already-running Desktop host. That older package exposed neither the onboarding skill nor the `init` command. The task therefore did not select the new skill through its ordinary loaded catalog.

After the old bundled command failed, the task searched installed C-Team cache versions, found the new package, read its `SKILL.md` directly, and followed it. This proves that the skill text and bundled path work, but manual cache discovery is not equivalent to normal installed-skill selection. A separately started app-server process saw the new skill immediately, isolating the failure to Desktop's running plugin snapshot rather than plugin ingestion.

### Approval and mutation

Before mutation, the task identified the exact disposable root, explained that it would create `.cteam/config.json` and create or merge the managed C-Team section in root `AGENTS.md`, and stated that plugin installation, marketplace metadata, global settings, and outside-project writes were excluded. It then stopped and requested approval. One approval was sent.

After approval, the task invoked the new installed package's bundled `bin/win-x64/cteam.exe init --target <exact-root>`. It did not recreate the file generation logic in prose or shell commands. The initializer returned `initialized`; both resulting files matched Experiment 009's canonical golden files byte-for-byte.

No repository marketplace file was created. The local marketplace manifest hash was unchanged. The user's Codex config prefix matched the post-install baseline exactly; the only 107-byte append was the host-created trust entry for the disposable projectless task. The initializer made no user-global plugin configuration change.

### Activation and guidance boundary

The same Desktop task remained attached to the old C-Team MCP generation, whose catalog exposed legacy mission tools rather than Experiment 008B's one-tool activation facade. Its live call returned the legacy mission-not-found result, so `project_enabled` was not proven in the same MCP lifetime.

The task's final response consequently recommended a fresh task for both new `AGENTS.md` guidance and the current plugin backend. That does not satisfy the desired clean distinction: guidance may require a fresh task, while Experiment 008B established that marker activation does not require an MCP restart once the correct backend is loaded.

### Repeat request

The natural initialization request was sent again in the same task. The bundled initializer returned `already_initialized`; the file hashes were unchanged and repository marketplace metadata remained absent.

## Decision

**O4 — More evidence needed.** The skill's approval language, canonical invocation, output, and repeat safety worked, and a new standalone app-server discovered its small catalog entry. The tested Desktop host did not refresh its plugin snapshot for a task created after reinstall, so ordinary installed-skill selection and same-process activation were not proven. Manual cache discovery cannot be promoted to agent-first success.

The minimum retest is one fresh Desktop application lifecycle after plugin installation, followed by the same natural request in a new disposable project. Do not repeat package or activation architecture experiments. Foundational onboarding validation is not yet complete, and production/MVP implementation planning is not unblocked by this run.

Experiment 010 remains optional and unrelated to this result.

## Evidence

- [`docs/evidence/pf5b-agent-onboarding.json`](../../docs/evidence/pf5b-agent-onboarding.json)
- [`experiments/009-onboarding-bootstrap`](../009-onboarding-bootstrap)
- [`experiments/008b-context-activation-db`](../008b-context-activation-db)

## Retest triggers

Retest after a full Desktop restart with the already installed package, or when Codex plugin skill/catalog hot-reload behavior changes. Retest if the C-Team initializer schema or skill discovery mechanism changes materially.
