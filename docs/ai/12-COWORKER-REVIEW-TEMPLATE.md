# Coworker Review Template

Claude Coworker / Claude AI uses this template to review Codex work.

```md
# Review: TASK-XXXX <title>

## Result
PASS | PASS WITH NOTES | FAIL

## Scope Check
- Task file used:
- Files changed match scope:
- App behavior changed:
- Handoff updated:

## Findings
1. <severity> <file:line> <issue>

## Checklist
- Task contract:
- Architecture:
- Data safety:
- Backend:
- Frontend:
- Validation:

## Groww Safety
- No silent mock fallback when Groww enabled:
- Per-user credential gating preserved:
- `isGrowwConnected` respected:
- `isDataFresh` respected:
- No AI actionable signal from stale/missing data:

## Validation Reviewed
- <commands and outcomes>

## Required Fixes
- <only if FAIL>

## Notes
- <optional>
```

Reviews must reference `docs/ai/10-REVIEW-CHECKLIST.md`.
