# Latest Handoff

## TASK-0002: AI Architecture Plan

Date: 2026-06-23

Author: Claude Coworker

## Summary

Produced target AI architecture documentation for the OptionsEdge AI subsystem.

No application source code was changed. Documentation only.

## Files Created Or Updated

- `docs/ai/architecture/AI-ARCHITECTURE-PLAN.md` (new — primary deliverable)
- `docs/ai/08-DECISIONS-LOG.md` (decision entry added)
- `docs/AI_HANDOFF.md` (change log entry added)
- `docs/ai/handoffs/latest.md` (this file)

## Behavior

No app behavior changed.

No backend source, frontend source, migrations, or deployment workflow changed.

## Key Findings From Source Review

- AI entry points confirmed: `AISignalService` (signal generation + risk check) and `ChatService` (streaming chat).
- `AISignalService` uses Sonnet for signals and Haiku for risk checks — correct tier split.
- Both services read model names from `IConfiguration` directly — no routing abstraction.
- Rate limit logic is duplicated in both `AISignalService.GenerateEntrySignalAsync` and `ChatService.ValidateAsync`.
- Prompts are inline strings — no versioning, no traceability from audit record to prompt.
- `AIUsageLog` entity exists but is missing fields: `PromptVersion`, `CacheHit`, `DurationMs`, `IsDataFresh`, `IsGrowwConnected`.
- No disclaimer injection in signal or chat responses.
- No circuit breaker or formal retry policy on `ClaudeApiClient`.

## Architecture Plan Sections Delivered

1. AI Subsystem Overview (current and target shapes)
2. Model Routing Strategy
3. Prompt Versioning
4. Response Caching
5. Token Budget Management
6. Audit Logging
7. User Isolation
8. Graceful Degradation
9. Safety Disclaimers
10. Implementation Roadmap (5 phases)

## Roadmap Summary

| Phase | Scope | Size | Owner |
|---|---|---|---|
| 1 | Centralize AI config in appsettings | Small | Codex |
| 2 | Extract prompts to versioned embedded resources | Medium | Codex |
| 3 | Harden audit log (new fields, centralized writer, retention) | Medium | Codex |
| 4 | Cache hardening + centralized budget guard | Medium | Codex |
| 5 | Circuit breaker and resilience pipeline | Large | Claude Coworker design + Codex |

Each phase requires its own approved task file before Codex begins coding.

## Validation

- `git status` confirms only new docs files; no backend or frontend source touched.
- All 10 section headings from TASK-0002 requirements present in output.
- No secrets, tokens, credentials, or private data in any artefact produced.

## Next Step

Manu to review `docs/ai/architecture/AI-ARCHITECTURE-PLAN.md`.

If approved, create Phase 1 task file under `docs/ai/tasks/pending/` targeting config centralization.
