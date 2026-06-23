# Latest Codex Handoff

## TASK-0001-CLEANUP-FIX: Proper Markdown Physical Line Normalization

Date: 2026-06-23

## Summary

Previous cleanup did not fully normalize physical raw-line formatting.

Reformatted `docs/ai` Markdown files for readable raw GitHub view, Git diffs, and AI review tools.

Preserved content meaning, Groww safety rules, Codex/Coworker role separation, and task/review/handoff workflow instructions.

## Files Created Or Updated

- `docs/ai/README.md`
- `docs/ai/00-COWORKER-PROJECT-MEMORY.md`
- `docs/ai/01-PROJECT-CONTEXT.md`
- `docs/ai/02-ARCHITECTURE-CONTEXT.md`
- `docs/ai/03-FUNCTIONAL-FLOWS.md`
- `docs/ai/04-CODING-STANDARDS.md`
- `docs/ai/05-AI-USAGE-RULES.md`
- `docs/ai/06-DATA-SAFETY-RULES.md`
- `docs/ai/07-CURRENT-STATE.md`
- `docs/ai/08-DECISIONS-LOG.md`
- `docs/ai/09-KNOWN-ISSUES.md`
- `docs/ai/10-REVIEW-CHECKLIST.md`
- `docs/ai/11-CODEX-INSTRUCTION-TEMPLATE.md`
- `docs/ai/12-COWORKER-REVIEW-TEMPLATE.md`
- `docs/ai/tasks/README.md`
- `docs/ai/handoffs/README.md`
- `docs/ai/handoffs/latest.md`
- `docs/AI_HANDOFF.md`

## Behavior

No app behavior changed. Documentation formatting only.

No backend source, frontend source, migrations, or deployment workflow changed.

## Validation

`git diff --check` passed.

## Review

Claude Coworker should review with `docs/ai/10-REVIEW-CHECKLIST.md`.
