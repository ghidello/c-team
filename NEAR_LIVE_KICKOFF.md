# Near-live Desktop spike kickoff

Use this as the first prompt for the next Codex mission.

---

Implement the Desktop near-live observation spike described in `NEAR_LIVE_SPIKE.md`.

Read `PROJECT.md`, `SPIKE.md`, `MODELS.md`, `AGENTS.md`, `PRODUCTION_REQUIREMENTS.md`, and the existing findings under `docs/` before making changes.

Use **Sol with High reasoning** for the primary session. Treat this mission as quota-sensitive.

Treat the first observability-spike findings as established. Do not repeat expensive Codex experiments merely to reconfirm them.

The goal is only to determine whether persisted ChatGPT Desktop Codex state is responsive and reliable enough for a near-live C-Team experience.

Prefer:

- local filesystem observation;
- existing recordings;
- deterministic tests;
- self-observation of this mission;
- bounded implementation;
- the cheapest experiment that answers each remaining question.

Delegation rules for this mission:

- use Face only for bounded read-only investigation that materially reduces primary-context growth;
- use B.A. for a bounded implementation chunk when that is more efficient than keeping the work in the primary context;
- do not invoke Murdock unless a genuinely consequential architectural surprise appears;
- do not invoke Reviewer merely for process compliance; use it only if the implementation becomes consequential enough to justify the extra quota;
- do not create subagents solely to populate C-Team telemetry.

Before performing any additional Codex inference solely to generate telemetry, identify the unanswered acceptance criterion that requires it. If NL5 needs child-agent evidence and no natural child activity exists, use one small Face probe only.

Keep the primary Sol context from growing unnecessarily. Delegate bounded work when it reduces overall context cost, but avoid fan-out that duplicates context without clear value.

Also perform the narrow **PF1 native companion packaging feasibility** experiment from `PRODUCTION_REQUIREMENTS.md` only if it can be answered cheaply with a tiny hello-world NativeAOT executable and without distracting from the near-live measurements. If PF1 requires significant unrelated work, document it as the immediate next experiment instead of expanding this spike.

Do not implement SQLite, production MCP, React/Apps SDK UI, analytics, steering, automatic routing, an installer, a Windows Service, or production lifecycle management.

Record private measurements under ignored `.cteam/` paths and commit only sanitized/allowlisted evidence.

Stop at the decision gate in `NEAR_LIVE_SPIKE.md` with measured latency/fidelity results and a D1, D2, or D3 recommendation. Do not continue into production implementation unless explicitly asked.

---
