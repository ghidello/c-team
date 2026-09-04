# C-Team spike findings and decision gate

Date: 2026-09-04. Environment: Windows, Desktop 26.901.4073.0, Codex 0.153.1, .NET 10. Evidence is scoped to this installed build and signed-in account.

**Recommend exactly one decision-gate choice: Architecture B — Persisted-state observer.** Desktop is the central workflow; the installed Desktop server has private stdio ownership. A separate server can discover candidate Desktop tasks and read their persisted state, but cannot establish a live subscription to the active Desktop task. The owned-runtime console remains a laboratory reference, not a proposed second production execution surface.

**CQ11: Minimal**, conservatively. Catalog discovery and a separately named Spark quota bucket exceed the literal wording of that label, but the required per-agent effective execution model is unproven. Therefore the definitions of Partial and Full are not satisfied.

The spike is at the decision gate, with meaningful negative results. It is **not an all-green technical acceptance**: the runtime exposed no structured plan events, and sandbox-enforced writes prevented the fixture's successful implementation. Those gaps were not replaced with synthetic success or permission bypasses. No production subsystem was implemented.

## Evidence inventory

- [Experiment index](evidence/experiment-index.json): recording filenames, SHA-256 hashes, method/item counts, outcomes, and RPC errors. Private originals remain in ignored `.cteam/recordings/`.
- [Full observed model catalog](evidence/model-catalog.json), [sanitized quota identities](evidence/quota-buckets.json).
- [Sanitized real-run excerpt](evidence/example-run.jsonl), [transformation provenance](evidence/example-run.provenance.json). This excerpt derives from `run-3b`, which detected four agents but failed its fixture edit.
- [Replay checks](evidence/replay-checks.json): independent-process final-state hashes.
- [Protocol notes](codex-protocol.md), [Desktop evidence](desktop-observation.md), [plugin validation](plugin-validation.md), [experiment plan and Murdock decisions](experiment-plan.md).

## CQ1 — Agent hierarchy

**Question:** Can hierarchy and identities be reconstructed without assistant prose?

**Finding:** Yes, combining structured live activity with metadata reads. Initial v2 streams emitted `thread/started` for the root only. Parent-owned `subAgentActivity(kind:started)` identifies the child; child status/turn/usage events follow. `thread/read(includeTurns:false)` supplies role, nickname, session id, parent id and configured model. `kind:interacted` must never reparent the target: a child can interact with its parent.

**Evidence:** `run-3b` captured a root plus Face, B.A., and Reviewer; metadata reads resolved Luna, Terra, and Sol configurations respectively. The sanitized excerpt preserves these links. Root/child persisted metadata also carries `source.subagent.thread_spawn`, including depth and parent. Regression tests cover reverse interaction and metadata enrichment.

**Confidence:** High for these runs.

**Impact on C-Team architecture:** Use source-specific identity enrichment; do not wait exclusively for child `thread/started`, infer roles from nicknames, or confuse task path with role.

**Remaining uncertainty:** Reparenting/fork semantics and ordering under other runtime versions need compatibility coverage. Detached reviews have a separate review relation, not necessarily a spawn parent.

## CQ2 — Effective model

**Question:** Which model actually executed each agent?

**Finding:** Requested model and server-configured/pre-inference model are observable. Actual upstream execution identity is not established. `Thread.model` and `Thread.reasoningEffort` explicitly describe configuration in the installed schema. `turn_context.model` precedes inference. No observed `turn`/usage/item response confirmed the executing model; no reroute was observed. The mapper retains reroute target/source/turn/time as routing evidence and leaves EffectiveModel null.

**Evidence:** `run-3b` metadata resolves configured Sol/Luna/Terra/Sol; the hosting Desktop task's turn context is Astra, differing from the planned Hannibal/Sol policy. Native detached review also reported configured Astra despite the Sol caller. `architecture-control` recorded Murdock/Sol and a default-role child with inherited Sol configuration, without a model override.

**Confidence:** High for configuration; effective execution remains unknown.

**Impact on C-Team architecture:** Retain generic model strings and sourced observations with thread/turn scope. Keep configuration, requested intent, pre-inference selection and confirmed execution distinct.

**Remaining uncertainty:** Upstream fallback/rerouting, actual service tier, and post-response model identity. Service tier may be null; null is not a default-tier assertion.

## CQ3 — Per-agent usage

**Question:** Are counters accurate per thread, including cache and reasoning?

**Finding:** `thread/tokenUsage/updated` exposes cumulative `total`, latest `last`, all six token breakdown fields, and context window. Replace total snapshots; do not sum them. Missing fields stay null. Cached input is not extra input, and reasoning output is not extra output.

**Evidence:** `run-3b` final totalTokens: root 126396; Face 59585; B.A. 64787; Reviewer 35932. In each of these four threads, summing the distinct observed last snapshots equals its final cumulative total; the first total equals first last. There were 7/3/3/2 usage samples respectively. Tests cover cumulative replacement and missing fields. Runtime context-window values are separate from the model catalog.

**Confidence:** High for recorded counters, not for billing attribution.

**Impact on C-Team architecture:** Store latest cumulative snapshots and preserve reported totals independently. Never infer monetary cost or account quota consumption from a thread's token count.

**Remaining uncertainty:** Counter reset/compaction and inherited-history behavior outside these controls; `last` must not be called whole-turn usage when multiple updates occur within one turn.

## CQ4 — Lifecycle and timing

**Question:** Can lifecycle and wall duration be measured?

**Finding:** Thread runtime state and turn outcome are distinct. `turn/started` and `turn/completed` carry start/completion timestamps and durationMs; the observer preserves protocol duration, with recorded receive-time fallback labeled when necessary. Exact turn IDs avoid treating an earlier completed turn as the current turn's completion. Thread flags can indicate waiting.

**Evidence:** `run-3b` root/Face/B.A./Reviewer durations were approximately 51.4/14.2/10.7/9.1 seconds. `run-4` contains a failed test command, but its Codex turns completed normally: completed inference is not successful fixture work. Failed/interrupted terminal values are understood by the schema/mapper; interruption was not induced on the Desktop task.

**Confidence:** High for successful turn boundaries and command failures; lower for unexercised interruption/wait cases.

**Impact on C-Team architecture:** Display runtime status, last turn outcome, and mission result separately. Do not use thread creation age as execution duration.

**Remaining uncertainty:** No live interrupted-turn or approval-wait acceptance experiment was performed; unsupported server requests receive explicit errors rather than an interactive approval flow.

## CQ5 — Structured plan

**Question:** Does the runtime emit live structured plan progress?

**Finding:** The protocol defines `turn/plan/updated` with steps/status/explanation, and the mapper supports it, but the tested native v2 model/tool surface did not expose the planning tool. Repeated requests yielded prose and explicit reports of unavailable `update_plan`, with zero structured plan notifications.

**Evidence:** Method counts in `run-1`, `run-2b`, `run-3b`, and `run-4`. No synthetic event is represented as a live capture.

**Confidence:** High for absence in the controlled surface; not a universal Codex limitation.

**Impact on C-Team architecture:** Show plan as unavailable rather than parsing prose or inventing steps. Technical acceptance item 11 remains unmet for this environment.

**Remaining uncertainty:** Whether another supported runtime configuration exposes the native plan tool. Changing model families or building a replacement planner is outside the spike.

## CQ6 — Tool activity

**Question:** What can be measured from structured activity?

**Finding:** Command status, exit code and duration; MCP server/tool identity and failures; file-change attempts/status; and collaboration activity are structured. Items are keyed by identity. Some pre-execution failures appear only in diagnostics/tool output. Test classification remains a command-level heuristic.

**Evidence:** `run-2b` has successful reads and a failed file-change item. `run-4` ran Test.ps1 and received the expected nonzero exit for the deliberately broken Add, then Set-Content received access denied. `run-1` contains failed MCP calls and Windows helper setup diagnostics. `plugin-invocation` contains a successful replay command (exit 0).

**Confidence:** High for emitted items; not all failed attempts produce the same event variants.

**Impact on C-Team architecture:** Retain structured status and duration without parsing stdout for primary metrics. A completed turn may contain failed tools.

**Remaining uncertainty:** Approval semantics are schema-only here; the spike rejects unsupported server requests, it does not implement steering or approval UI.

## CQ7 — Diff metadata

**Question:** Can changed files and added/removed lines be calculated?

**Finding:** The protocol provides turn diff updates and per-item file-change metadata. The mapper counts unified-diff hunks, preserves file paths/kinds, and never retains diff source in domain state. It replaces per-turn diff snapshots and uses completed file-change metadata when a turn diff is absent. Failed writes are attempts, not successful changed files.

**Evidence:** Real failed file-change items were captured without turn diff notifications. Tests cover completed file-change fallback, deletion metadata, valid hunks and header-like code inside a hunk. No successful fixture write or live `turn/diff/updated` was obtained; live line-count correctness is not claimed.

**Confidence:** Medium: schema plus focused tests, with live failure metadata.

**Impact on C-Team architecture:** Keep this version-sensitive aggregation at the protocol boundary; retain raw diagnostic streams only locally.

**Remaining uncertainty:** Binary content, complex rename/quoted-path cases, and patch formats beyond unified diff need broader evidence before production.

## CQ8 — Review agents

**Question:** How do custom and native reviews appear?

**Finding:** Custom Reviewer/Sol is an ordinary role-bearing child. Native detached review is available only with legacy history in this installation; it returns `reviewThreadId` and review-mode items. The domain records ReviewOfThreadId separately from parentage.

**Evidence:** `run-1` detached review failed with `paginated threads do not support detached review`. `run-2` proved legacy history requires experimental capability. `run-2b` started/completed a native review, but its cwd was the server process's parent repository, and its configured model was Astra. Therefore it reviewed spike changes rather than the tiny fixture. The harness now launches its server in the supplied fixture cwd; no claim is made that the native-review correction was revalidated with another native review.

**Confidence:** High for observed representation/restrictions; limited for the corrected native-review target behavior.

**Impact on C-Team architecture:** Treat review relationship and execution configuration as observed facts; never equate native review with the custom reviewer policy.

**Remaining uncertainty:** The default native review model and inherited cwd behavior need explicit verification before using review as an isolated product workflow.

## CQ9 — Replay

**Question:** Can the recorded stream reproduce final state without Codex running?

**Finding:** Yes. The recorder precedes parsing, serializes ordering, records raw stdout and outgoing messages, and supplies its exact timestamp to the same mapper used by replay. Final snapshots are exported after stopping/draining the owned process. Unknown methods remain in raw recordings and become counts in state.

**Evidence:** `run-4` live/replay JSON SHA-256 both `8D662AE3BFC95667B9ADD2F983A62E1EEA86BBF84BBC515C1E2384419ED97303`. Additional controls and sanitized replay checks are listed in replay-checks.json. Tests exercise string-ID correlation and raw-string recorder replay. Reviewer independently reproduced run-4 equivalence.

**Confidence:** High for complete captured streams.

**Impact on C-Team architecture:** Keep one mapping path and a defined final recording boundary. The test result does not by itself prove every derived metric correct; raw-field checks supplement it.

**Remaining uncertainty:** A truncated/corrupt recording envelope fails replay; this spike is not a recovery/archive system. Malformed protocol lines inside valid envelopes remain recorded and are ignored consistently.

## CQ10 — Existing Desktop-owned session

**Question:** Can C-Team observe the session already owned by Desktop?

**Finding:** CQ10 outcome **C — persisted-state observation**. No supported live attachment was found for this Windows Desktop installation. This is distinct from decision-gate Architecture B.

**Evidence:** Desktop launched a private child app-server with default stdio; Windows daemon lifecycle returned Unix-only. A separate server's loaded list remained empty before/after reading the active Desktop task, which returned notLoaded. A cwd/source-filtered thread/list found two Desktop candidates including this mission. Scoped persisted files updated while work continued. See desktop-observation.md for commands and raw-recording references.

**Confidence:** High for current process topology and persisted access; not a cross-platform/future-version claim.

**Impact on C-Team architecture:** Select the persisted observer direction to preserve the user's Desktop workflow. Owned-runtime telemetry cannot be presented as passive Desktop observation.

**Remaining uncertainty:** Automatic selection of the active task among candidates, flush latency/completeness, history-mode changes, and the stability of rollout storage. No production polling/SQLite history layer was built.

## CQ11 — Model catalog and quota identity

**Question:** Can catalog, effective execution and quota identity be joined?

**Finding:** **Minimal** under the required taxonomy, with the following richer capability result.

| Dimension | Observed result |
| --- | --- |
| Catalog | Complete paginated result with includeHidden=true: 10 entries; identifiers/display names/efforts/modalities/service tiers/multi-agent version |
| Requested/configured selection | Observable for root and hydrated children; pre-inference turn context available in persisted state |
| Inheritance | Default child control inherited Sol configuration; explicit custom roles retained their configured policy |
| Actual upstream execution model | Not established; no observed post-response attestation |
| Quota catalog | `codex` and `codex_bengalfox`; latter explicitly labeled GPT-5.3-Codex-Spark |
| Per-agent execution-to-quota join | Not established; concurrent account activity invalidates attribution from percentage changes |
| Context window | Usage-notification source, not model/list in this build |

**Evidence:** Complete catalog and allowlisted quota artifacts; capabilities/architecture-control recordings; installed schema explicitly excluding execution semantics from Thread.model. No Spark or alternate-model comparison was run.

**Confidence:** High for visible catalog/configuration/named bucket; no claim for missing execution attribution.

**Impact on C-Team architecture:** Use dynamic strings/capabilities, preserve provenance and unknowns, and defer automatic routing and quota analytics. Full is excluded; Partial also requires effective-model evidence not obtained here.

**Remaining uncertainty:** Hidden entries are not proven executable entitlements; API-key-only availability, upstream fallback and per-execution quota identity require further supported telemetry.

## Final acceptance and recommendation

The working console launches/initializes Codex, executes controlled multi-agent tasks, reconstructs/enriches hierarchy, captures usage/timing/activity, records/replays streams, and supports a locally installed skill. Release build and standard xUnit v3 execution are verified. The model/effort gaps are explicitly recorded as permitted by the brief.

Structured live plan capture is unmet. Successful file modification and live diff aggregation remain unproven because both patch and shell writes were denied under the tested sandbox. Native review availability was proven with the target/configuration caveats above. The spike therefore answers the architecture questions without claiming all technical acceptance conditions passed.

Choose **Architecture B — Persisted-state observer** for any separately authorized next phase. Its trade-off is reduced real-time fidelity and dependence on version-sensitive stored history, in exchange for observing the user's existing Desktop workflow. Do not promise automatic active-task selection or exact effective-model/quota attribution. Architecture A lacks an attachment mechanism here; C would change runtime ownership; D would add a second product execution surface not justified by the current goal.

Stop here. No production implementation or model-comparison experiment is authorized by this decision.

## Follow-up decision: Desktop near-live observation

The quota-sensitive persisted-state follow-up is complete. [Measured near-live evidence](near-live-observation.md) supports **D1 — Hybrid, persisted-first**: persisted record-to-observer latency was 1.404 ms median and 6.668 ms p95 across 131 independent-probe samples, with deterministic restart/reconciliation and zero live parse failures. Token updates were cumulative and attributable but arrived stepwise, with a 13.885-second median interval.

Use filesystem notifications plus periodic length/prefix reconciliation. Do not rely on file modification timestamps, which remained unchanged while the rollout grew. Explicit thread selection is certain; cwd-only selection remains ambiguous. Automatic child-file hydration and live root failure/completion timing remain follow-up gaps. The optional PF1 NativeAOT probe stopped because the host lacks the Windows native linker; installing that prerequisite is the next bounded packaging experiment.
