# Coding Standards

Codex is coding agent and must follow these standards on implementation tasks.

## General

- Read task file and current handoff before editing.
- Keep diff scoped to requested files and behavior.
- Preserve existing patterns before adding abstractions.
- Do not include secrets, tokens, connection strings, or private account data.
- Update `docs/AI_HANDOFF.md` and `docs/ai/handoffs/latest.md` after completed non-trivial task.

## Backend

- Follow Microsoft C# conventions.
- Use feature folders under `/Features`.
- DTO names end with `Request` or `Response`.
- Use `ILogger`, not console output.
- Use EF Core migrations for schema changes.
- Keep endpoints under `/api/v1/...`.
- Return RFC 7807 `ProblemDetails` for API errors where applicable.
- Validate inputs and preserve auth/user boundaries.

## Frontend

- TypeScript strict mode.
- Components in PascalCase.
- Hooks in camelCase with `use` prefix.
- Mobile-first layout.
- Use existing API client, store, hooks, and component conventions.
- No `console.log` in production code.
- Do not render market-data-dependent values unless Groww access and freshness are confirmed.

## Validation

- Run validation named in the task file.
- For docs-only tasks, `git diff --check` is enough unless non-doc files changed.
- If build/test cannot run, record why in handoff.
