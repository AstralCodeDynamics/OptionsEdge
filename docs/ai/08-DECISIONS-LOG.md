# Decisions Log

Record architecture and workflow decisions newest first.

Do not record secrets or private account data.

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

Decision: when Groww is enabled, stale or missing live data blocks
market-data-dependent UI, risk checks, and actionable AI signals.

Reason:
- Silent mock fallback caused wrong ATM, option LTP, targets, and alerts.
- Trading assistant must prefer blocked/degraded state over false confidence.

Impact:
- Consumers must respect `isGrowwConnected` and `isDataFresh`.
- Mock data remains dev-only when `Groww:Enabled=false`.
