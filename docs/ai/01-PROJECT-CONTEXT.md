# Project Context

## Product

OptionsEdge is a personal NIFTY/BANKNIFTY options trading assistant.

It monitors market data, generates AI signals, tracks manually entered
positions, and alerts when risk changes.

User trades manually on Groww.

OptionsEdge is not a broker and must not silently place trades.

## Stack

- Backend: .NET Core 10, C#, Minimal APIs, SignalR, EF Core, PostgreSQL.
- Frontend: React 18, TypeScript, Vite, TailwindCSS, TradingView Lightweight Charts.
- Indicators: Skender.Stock.Indicators.
- Auth: JWT.
- AI: Claude API per current code/docs, with quick and deep model tiers.

## AI Roles

- Codex is coding agent.
- Claude Coworker / Claude AI is Architect, Consultant, and Reviewer.
- Manu approves direction and deploys after review passes.

## Sources Of Truth

- `CLAUDE.md` - lean root context and role split.
- `docs/AI_HANDOFF.md` - chronological handoff/change log.
- `SPEC.md` - product requirements.
- `ARCHITECTURE.md` - system structure and contracts.
- `TASKS.md` - legacy phase list.
- `docs/ai` - structured AI artefact workflow.

## Constraints

- No secrets in repo or artefacts.
- No raw SQL schema changes; use EF Core migrations.
- All endpoints versioned under `/api/v1/...`.
- Frontend stays TypeScript strict.
- Backend follows Microsoft C# conventions.
- Responsive first: mobile, tablet, desktop.
