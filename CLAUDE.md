# OptionsEdge — Codex Context

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

## Agent Protocol

### Coding Agent: Codex (OpenAI)
Codex handles ALL implementation tasks:
- Backend services, infrastructure, migrations
- Backend endpoints and tests
- Frontend pages, hooks, types, components
- AI prompts and security layer

### Review & Consultation: Claude AI (Anthropic)
Claude AI (via claude.ai) acts as architect and reviewer — NOT a coding agent. Claude:
- Reviews code after Codex commits
- Designs features before Codex implements
- Writes precise Codex instructions
- Diagnoses live production issues
- Does NOT commit code directly

### Workflow
1. Manu describes feature/bug to Claude AI
2. Claude AI designs the solution and writes a precise Codex instruction
3. Codex implements and commits
4. Claude AI reviews the committed code
5. Manu deploys after review passes

### Shared Memory: docs/AI_HANDOFF.md
- Codex reads AI_HANDOFF.md BEFORE every task
- Codex updates AI_HANDOFF.md AFTER every task
- Claude AI reads AI_HANDOFF.md when reviewing
- Never touch files listed as "active" in handoff
