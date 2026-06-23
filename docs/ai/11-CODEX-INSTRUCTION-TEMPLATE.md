# Codex Instruction Template

Use this template for every non-trivial Codex task.

```md
# TASK-XXXX: <title>

## Goal
<one clear outcome>

## Role
Codex is coding agent. Implement only this task.
Claude Coworker / Claude AI is Architect, Consultant, and Reviewer.

## Read First
- CLAUDE.md
- docs/AI_HANDOFF.md
- docs/ai/README.md
- docs/ai/01-PROJECT-CONTEXT.md
- docs/ai/02-ARCHITECTURE-CONTEXT.md
- docs/ai/06-DATA-SAFETY-RULES.md
- <task-specific files>

## Scope
- <allowed change>

## Out Of Scope
- <explicit exclusions>

## Requirements
- <behavior/doc/code requirement>

## Data Safety
- No secrets, tokens, connection strings, Groww credentials, or private account data.
- If Groww data is touched, preserve per-user gating and freshness checks.
- No AI actionable signal from stale or missing data.

## Validation
- <commands/checks>

## Deliverable
- Files changed.
- Behavior changed.
- Validation results.
- Handoff updated.
```

For docs-only tasks, state that backend source, frontend source, migrations, and
deployment workflow are out of scope.
