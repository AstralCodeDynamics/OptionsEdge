# AI Task Queue

Task files are contract between Claude Coworker, Codex, and Manu.

## Status Folders

- `pending/` - drafted by Claude Coworker, not approved.
- `approved/` - approved by Manu, ready for Codex.
- `in-progress/` - Codex actively working.
- `review/` - Codex done, awaiting Claude Coworker review.
- `completed/` - reviewed and accepted.

## Rules

- Every non-trivial Codex task starts from task file.
- Task file must state goal, scope, out-of-scope, requirements, validation, deliverable.
- Docs-only tasks must explicitly say no backend/frontend/migration/deployment changes.
- Groww-related tasks must cite `docs/ai/06-DATA-SAFETY-RULES.md`.
- Completed task must update `docs/ai/handoffs/latest.md`.
