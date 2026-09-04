# C-Team — Model Strategy

## Principle

C-Team must treat Codex models as a **dynamic capability discovered from the current Codex installation/account**, not as a fixed product enum.

Sol, Terra, and Luna are the initial controlled dogfooding policy. They are not an architectural assumption and must not be hard-coded into C-Team's domain model, persistence model, protocol adapters, UI contracts, or analytics.

Codex may expose additional or legacy models such as GPT-5.5, GPT-5.4, GPT-5.4 mini, GPT-5.3-Codex-Spark, or future models. Availability may depend on account, authentication mode, rollout, API-key usage, product surface, and time.

## Initial controlled policy

For the first spike, keep the current agent configuration intentionally stable:

```text
Hannibal   → Sol    → planner / thinker
Murdock    → Sol    → challenger / lateral thinker
Face       → Luna   → explorer / investigator
B.A.       → Terra  → implementer
Reviewer   → Sol    → independent reviewer
```

This gives the spike a reproducible baseline while exercising multiple models.

Do not switch these agents to other models merely because alternatives are available. Model comparisons come **after** telemetry correctness is proven.

## Required model concepts

C-Team should be able to represent, where Codex exposes them:

- model identifier;
- display name;
- availability;
- supported reasoning-effort values;
- configured/requested model;
- effective model actually used by an agent/turn;
- service tier;
- model context window;
- capabilities relevant to multi-agent execution;
- usage/rate-limit bucket or quota identity, when observable;
- whether a model is inherited, overridden, rerouted, or falls back to another model.

Never infer the effective model solely from an agent TOML file.

## CQ11 — Model catalog and quota identity

The observability spike must answer this additional critical question:

> Can C-Team enumerate the models currently available to the signed-in Codex user, identify the effective model used by every agent, and determine whether different models consume different usage/rate-limit pools?

Investigate and document:

1. How the app-server exposes the current model catalog.
2. Whether the catalog reflects the actual signed-in account and product surface.
3. Which stable identifiers should be persisted/displayed.
4. How supported reasoning-effort values and model capabilities are exposed.
5. Whether effective model identity is available per thread, turn, or upstream response.
6. Whether model inheritance/overrides can be observed rather than inferred.
7. Whether separate quota/rate-limit pools can be associated with a model or model family.
8. Whether GPT-5.3-Codex-Spark's separate limits, when available to the account, are observable through app-server/account telemetry.
9. What remains unavailable and would require estimation or a compatibility source.

### CQ11 acceptance criterion

The spike must produce one of these outcomes with evidence:

- **Full:** model catalog, effective model, and quota identity are all observable.
- **Partial:** catalog/effective model are observable but quota identity is incomplete.
- **Minimal:** only configured/requested models can be determined reliably.

The result must be recorded in `docs/spike-findings.md` using the same Question / Finding / Evidence / Confidence / Impact / Remaining uncertainty structure as the other critical questions.

## Future comparison experiments

Only after the core spike can measure models and usage correctly should C-Team run controlled comparisons.

Interesting candidates include:

- Face on Luna vs GPT-5.3-Codex-Spark for bounded repository exploration;
- B.A. on Terra vs Spark for tiny, deterministic edits;
- B.A. on Terra vs GPT-5.5/Sol for harder implementation;
- Hannibal or Murdock on Sol vs other reasoning-capable models for complex architecture;
- Reviewer models compared by defect discovery, false positives, latency, and usage.

The point is not to declare one model globally best. C-Team should eventually help answer:

> Which model is the best fit for this role and task class, given quality, latency, usage, cache reuse, and available quota?

## Metrics for future routing analysis

Potential dimensions:

```text
quality / reviewer outcome
latency
input/output/reasoning tokens
cache ratio
retry/failure rate
tool activity
parallelism
quota consumption
model availability
```

Examples of useful derived findings:

```text
Face / Luna
  median duration        6.8 s
  median tokens          31k
  successful discovery   94%

Face / Spark
  median duration        1.5 s
  median tokens          26k
  successful discovery   91%
  quota bucket           separate (if observable)
```

or:

```text
B.A. / Terra
  first-review pass      91%
  median tokens          284k

B.A. / Sol
  first-review pass      96%
  median tokens          611k
```

These are examples of the analysis C-Team should enable, not expected values.

## Product rule

Prefer the cheapest/fastest model that can complete a task reliably, but keep model selection a policy layered on top of observed capabilities.

C-Team's core must remain model-agnostic.