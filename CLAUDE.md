# OptionsEdge — Claude Code Context

## Project
NIFTY/BANKNIFTY options trading assistant. No broker API. Signals only — user trades manually on Groww. Active risk monitoring alerts user to exit dangerous positions.

## Stack
- Backend: .NET Core 10, C#, Minimal APIs, SignalR, PostgreSQL (Npgsql + EF Core)
- Frontend: React 18, TypeScript, Vite, TailwindCSS, TradingView Lightweight Charts
- AI: Claude API (claude-haiku-4-5 for quick signals, claude-sonnet-4-6 for deep analysis)
- Indicators: Skender.Stock.Indicators
- Auth: JWT (ASP.NET Core)
- Payments: Razorpay (future)

## Monorepo Structure
```
OptionsEdge/
  CLAUDE.md              ← this file (keep under 200 lines)
  SPEC.md
  ARCHITECTURE.md
  TASKS.md
  backend/               ← .NET Core 10 solution
  frontend/              ← React + Vite
  docs/
```

## Key Rules
- Read `docs/AI_HANDOFF.md` before non-trivial changes and update it after changes so Claude/Codex stay in sync.
- Responsive first: mobile → tablet → desktop
- Cache AI responses aggressively (market data unchanged = no new API call)
- All C# code follows Microsoft coding conventions
- All React code in TypeScript strict mode
- No console.log in production code — use ILogger (backend) / proper logging
- EF Core migrations only — no raw SQL schema changes
- All endpoints versioned: /api/v1/...
- Never hardcode secrets — use appsettings + environment variables
- PostgreSQL on localhost:5432 (dev), connection string in appsettings.Development.json

## Cost Controls
- Use claude-haiku-4-5 for quick/frequent calls (signals, risk checks)
- Use claude-sonnet-4-6 only for deep analysis (user-triggered)
- Cache AI responses per market snapshot hash (5-min TTL)
- Max 1000 tokens per AI response
- Rate limit: 10 AI calls per user per hour

## Coding Conventions
- Backend: Feature-folder structure under /Features
- DTOs suffix: Request/Response (e.g. AnalyzeRequest, SignalResponse)
- Services registered in DI via extension methods in each feature folder
- Frontend: components in PascalCase, hooks in camelCase with use prefix
- API errors: ProblemDetails RFC 7807 format

## Dual-Agent Protocol

- Second agent: Codex (OpenAI)
- Shared memory: docs/AI_HANDOFF.md
- Read AI_HANDOFF.md BEFORE every task
- Update AI_HANDOFF.md AFTER every task
- Never touch files listed as "Codex active" in handoff
- Own areas: Backend services, Infrastructure,
  Groww integration, DB migrations, AI prompts,
  Security layer
- Codex owns: Backend endpoints, Tests,
  Frontend pages, Frontend hooks, Frontend types,
  UI components
