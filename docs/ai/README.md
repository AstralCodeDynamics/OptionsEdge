# AI Artefact Workflow

This folder is shared memory and task contract for OptionsEdge AI work.

## Roles

- Codex is the coding agent.
  Codex implements approved tasks, runs validation, updates handoffs, and
  never changes app behavior outside the task file.
- Claude Coworker / Claude AI is Architect, Consultant, and Reviewer.
  Claude designs changes, asks architecture questions, writes precise task
  files, and reviews Codex output.
- Markdown artefacts are the source of truth between Claude, Codex, and Manu.

## Required Flow

1. Claude Coworker creates or updates a task file under `docs/ai/tasks/pending/`.
2. Manu approves the task by moving it to `docs/ai/tasks/approved/`.
3. Codex reads `CLAUDE.md`, `docs/AI_HANDOFF.md`, this workflow, and the
   approved task file before coding.
4. Codex moves active task context to `docs/ai/tasks/in-progress/` when needed
   and implements only that task.
5. Codex creates or updates `docs/ai/handoffs/latest.md` after every completed task.
6. Claude Coworker reviews using `docs/ai/10-REVIEW-CHECKLIST.md`.
7. `PASS` and `PASS WITH NOTES` reviews go under
   `docs/ai/reviews/passed/`; `FAIL` reviews go under
   `docs/ai/reviews/failed/`.
8. Completed task artefact moves to `docs/ai/tasks/completed/`.

Every non-trivial task must start from a task file.

Every review must use the review checklist.

## Index

- `00-COWORKER-PROJECT-MEMORY.md` - durable Claude Coworker memory.
- `01-PROJECT-CONTEXT.md` - product, stack, roles, and constraints.
- `02-ARCHITECTURE-CONTEXT.md` - architecture rules Codex must respect.
- `03-FUNCTIONAL-FLOWS.md` - user and system flows.
- `04-CODING-STANDARDS.md` - implementation standards.
- `05-AI-USAGE-RULES.md` - AI model, caching, rate limit, and prompt rules.
- `06-DATA-SAFETY-RULES.md` - Groww and market-data safety rules.
- `07-CURRENT-STATE.md` - current build state and active caveats.
- `08-DECISIONS-LOG.md` - architecture decision history.
- `09-KNOWN-ISSUES.md` - bugs, risks, and follow-ups.
- `10-REVIEW-CHECKLIST.md` - required review checklist.
- `11-CODEX-INSTRUCTION-TEMPLATE.md` - task prompt template for Codex.
- `12-COWORKER-REVIEW-TEMPLATE.md` - review output template for Claude.
- `tasks/` - task queue by status.
- `reviews/` - review queue and outcomes.
- `handoffs/` - Codex completion handoffs.

## Safety

Do not include secrets, tokens, connection strings, Groww credentials, private
account data, or private trading account details in any artefact.
