# Decisions Log

Record architecture and workflow decisions newest first.

Do not record secrets or private account data.

## 2026-06-23 - AI Architecture Plan (TASK-0002)

Decision: design a unified AI infrastructure layer for OptionsEdge before any implementation begins.

Output: `docs/ai/architecture/AI-ARCHITECTURE-PLAN.md`.

Reason:

- `AISignalService` and `ChatService` each implement cost control, rate limiting, model selection, and audit logging independently with duplicated logic.
- No prompt versioning — prompts are inline strings with no traceability from audit record to exact prompt used.
- No centralized budget guard — rate limit logic duplicated across two feature services.
- Audit log missing fields: `PromptVersion`, `CacheHit`, `DurationMs`, `IsDataFresh`, `IsGrowwConnected`.
- No graceful degradation contract defined for AI API failures.
- No disclaimer injection contract.

Impact:

- Five-phase implementation roadmap defined (centralize config → prompt registry → audit hardening → cache/budget → resilience).
- All phases are Codex implementation tasks; each requires a separate approved task file before coding begins.
- No app code changed by this planning task.
- Safety constraints from `06-DATA-SAFETY-RULES.md` incorporated into all phases.

## 2026-06-23 - Structured AI Artefact Workflow

Decision: create `docs/ai` as shared memory and task contract layer.

Reason:
- Claude Coworker needs durable architecture/review context.
- Codex needs low-token, task-scoped instructions.
- Markdown artefacts make task status, handoffs, and review outcomes explicit.

Impact:
- Every non-trivial task starts from a task file.
- Every completed Codex task creates or updates a handoff.
- Every review uses review checklist.

## 2026-06-23 - Groww Freshness Is Hard Safety Gate

Decision: when Groww is enabled, stale or missing live data blocks market-data-dependent UI, risk checks, and actionable AI signals.

Reason:
- Silent mock fallback caused wrong ATM, option LTP, targets, and alerts.
- Trading assistant must prefer blocked/degraded state over false confidence.

Impact:
- Consumers must respect `isGrowwConnected` and `isDataFresh`.
- Mock data remains dev-only when `Groww:Enabled=false`.
