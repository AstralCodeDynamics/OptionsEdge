# Review Checklist

Claude Coworker / Claude AI must use this checklist for every review.

## Task Contract

- Task file exists for non-trivial work.
- Codex changed only approved scope.
- Handoff created or updated.
- `docs/AI_HANDOFF.md` updated when behavior, architecture, API contract, data
  flow, or important debugging context changed.

## Architecture

- Existing feature-folder and API patterns respected.
- `/api/v1/...` contract preserved unless task explicitly changes it.
- EF Core migrations used for schema changes.
- No unrelated refactor or deployment workflow change.

## Data Safety

- No secrets, tokens, connection strings, or private account data added.
- Groww-enabled paths have no silent mock fallback.
- Per-user Groww credential gating is preserved.
- Callers respect `isGrowwConnected` and `isDataFresh`.
- No AI actionable signal from stale, missing, mocked, or cross-user data.

## Backend

- Auth/user boundaries preserved.
- `ILogger` used for logging.
- Errors follow project conventions.
- Tests cover changed behavior when risk is non-trivial.

## Frontend

- TypeScript types match API contracts.
- Mobile-first UI still works.
- Market-data-dependent UI blocks or degrades safely.
- No production `console.log`.

## Validation

- Required tests/builds/checks ran.
- Docs-only tasks ran `git diff --check`.
- Any skipped validation has clear reason and residual risk.

## Outcome

Review result must be one of:

- PASS
- PASS WITH NOTES
- FAIL

Store `PASS` and `PASS WITH NOTES` reviews under `docs/ai/reviews/passed/`.

Store `FAIL` reviews under `docs/ai/reviews/failed/`.
