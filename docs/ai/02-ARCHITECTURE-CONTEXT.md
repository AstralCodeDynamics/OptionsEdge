# Architecture Context

## System Shape

Frontend React app calls .NET Minimal APIs over HTTPS and listens to SignalR `/hubs/market`. Backend uses feature folders, services, background workers, EF Core, PostgreSQL, and market data infrastructure.

## Backend Rules

- Keep feature-folder structure under `backend/src/OptionsEdge.API/Features`.
- Register services through feature or infrastructure DI extension patterns already in repo.
- Use `ProblemDetails` for API errors where applicable.
- Use `ILogger` for logging.
- Use EF Core migrations for schema changes.
- Never hardcode secrets.
- Preserve `/api/v1/...` endpoint versioning.

## Frontend Rules

- React components in PascalCase.
- Hooks use `use` prefix.
- Keep mobile-first layout.
- No `console.log` in production code.
- Respect existing Zustand store, API service, hooks, and component patterns.

## Data Architecture Rules

- Groww-backed live data is shared infrastructure and must be gated per user before display or AI use.
- `GrowwGatedResponse<T>` shape is current safety contract for gated endpoints: `isGrowwConnected`, `isDataFresh`, `data`.
- Mock data is valid only when `Groww:Enabled=false`.
- Options, indicators, AI signals, chat market context, position P&L, and risk checks must not derive actionable output from stale or missing live data.

## Change Discipline

- Small task, small diff.
- No unrelated refactors.
- No backend/frontend source changes for documentation-only tasks.
- Update docs and handoffs when contracts, flows, or known risks change.
