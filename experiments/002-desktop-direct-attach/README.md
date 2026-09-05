# Experiment 002 — Desktop direct attach

## Purpose

Determine whether C-Team can subscribe directly to the app-server session already owned by ChatGPT Desktop.

## Original environment

Measured 2026-09-04 on Windows 10.0.26220 with ChatGPT Desktop 26.901.4073.0 and Codex 0.153.1.

## Hypothesis

A supported shared endpoint, daemon or attach API lets another local client observe the active Desktop task without resuming or taking ownership of it.

## Procedure

The original spike inspected the Desktop/Codex process relationship and documented transports, looked for a supported shared endpoint, tested Windows daemon availability, and initialized an independent stdio app-server. The second server listed its own loaded threads and read the active Desktop thread from durable storage without resuming it. This archive did not repeat those operations.

## Success criteria

A second client must discover a supported Desktop endpoint and receive a causally new notification from the Desktop-owned task without starting, resuming or taking ownership of that task.

## Observed result

Desktop launched its app-server over private stdio with no listener override. No documented shared listener or subscription endpoint was found, and the app-server daemon lifecycle reported that it was supported only on Unix. A second stdio server could read the Desktop thread from common durable storage, but its loaded-thread list stayed independent and empty. Durable read access therefore did not establish a live subscription.

The failed approach is preserved because a future shared listener or subscription API could change the architecture decision.

## Current status

**Blocked.** CQ10 outcome C, persisted-state observation, was the strongest supported option on the tested installation.

## Evidence references

- [`docs/desktop-observation.md`](../../docs/desktop-observation.md)
- [`docs/spike-findings.md`](../../docs/spike-findings.md)
- [`docs/evidence/experiment-index.json`](../../docs/evidence/experiment-index.json)

## Known limitations

Internal named pipes were deliberately not reverse engineered. The result applies to this Windows/Desktop/Codex version and does not rule out an attach path on another platform or future release.

## Retest trigger

Retest if Codex or Desktop exposes a shared listener, a Desktop thread-subscription API, a Windows daemon lifecycle, or a documented attach/discovery API.
