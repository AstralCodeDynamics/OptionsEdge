# TASK-0002: AI Architecture Planning — Target AI Design for OptionsEdge

## Goal

Produce architecture documentation that defines the target AI subsystem design
for OptionsEdge. Output is Markdown documents only. No app code changes.

## Role

Claude Coworker / Claude AI is the author of this planning task.

Codex is out of scope for this task. If Manu directs Codex to this task, Codex
must produce only Markdown documentation artefacts — no backend source, no
frontend source, no migrations, no deployment workflow changes.

## Status

Completed: 2026-06-25
Output:
- docs/ai/architecture/AI-ARCHITECTURE-PLAN.md

## Read First

- `CLAUDE.md`
- `docs/AI_HANDOFF.md`
- `docs/ai/README.md`
- `docs/ai/01-PROJECT-CONTEXT.md`
- `docs/ai/02-ARCHITECTURE-CONTEXT.md`
- `docs/ai/05-AI-USAGE-RULES.md`
- `docs/ai/06-DATA-SAFETY-RULES.md`
- `docs/ai/07-CURRENT-STATE.md`
- `docs/ai/08-DECISIONS-LOG.md`

## Context

OptionsEdge already uses the Claude API in two features:

- `AISignalService` — entry signal generation with strike guardrail, real
  premium injection, OI-weighted strike preference.
- `ChatService` — market-context-aware AI chat, per-user Groww gate.

Both features have cost controls and rate limits but they are implemented ad hoc.
There is no unified AI architecture layer that governs model routing, prompt
versioning, response caching, audit logging, token budget management, or
graceful degradation.

This task designs that target layer as a documented architecture plan before any
implementation begins.

## Scope

Allowed output:

- One or more new Markdown files under `docs/ai/` or a new subdirectory such
  as `docs/ai/architecture/`.
- Updates to existing `docs/ai` artefacts if the new architecture decisions
  require it (e.g., `08-DECISIONS-LOG.md`, `07-CURRENT-STATE.md`).
- Update to `docs/AI_HANDOFF.md` after task completes.

## Out Of Scope

- No changes to `backend/` source.
- No changes to `frontend/` source.
- No new or modified EF Core migrations.
- No changes to `.github/workflows/`.
- No changes to `CLAUDE.md`, `SPEC.md`, `ARCHITECTURE.md`, or `TASKS.md`
  unless those files need a pointer added to the new architecture doc.
- No new NuGet packages, npm packages, or config secrets.

## Requirements

The architecture documentation must cover all sections below. Each section must
be grounded in the current codebase patterns and the data safety rules in
`docs/ai/06-DATA-SAFETY-RULES.md`.

### 1. AI Subsystem Overview

- Diagram or structured description of how the AI subsystem sits inside the
  broader OptionsEdge backend.
- List current AI entry points: `AISignalService`, `ChatService`. Identify any
  others if found during analysis.
- Describe the boundary between AI infrastructure (shared) and AI features
  (per-feature callers).

### 2. Model Routing Strategy

- Define which model tier handles which use case.
- `claude-haiku-4-5`: quick signals, automated risk checks, lightweight market
  summaries.
- `claude-sonnet-4-6`: user-triggered deep analysis, complex chat with full
  market context.
- Document rules for selecting a model at call time (not hardcoded per service
  — configurable).
- Define how model names are configured (appsettings, not constants scattered
  across services).

### 3. Prompt Versioning

- Define how prompts are versioned and stored.
- Current state: prompts are inline strings in service classes.
- Target state: named, versioned prompt templates loadable by feature and
  version string.
- Specify where prompt templates live (embedded resource, config file, or
  dedicated folder).
- Define how prompt version is recorded in audit log so a logged signal can be
  traced back to exact prompt used.

### 4. Response Caching

- Document current caching approach: market snapshot hash, 5-min TTL.
- Define target caching contract: cache key structure, TTL rules per use case,
  invalidation triggers.
- Specify that cache must be user-scoped or hash-based — never serve another
  user's cached AI response.
- Define behavior when cache hit: skip AI call, return cached response with
  `fromCache: true` flag in response metadata.

### 5. Token Budget Management

- Current limits: max 1000 tokens per response, 10 AI calls per user per hour.
- Document where these limits live and how they are enforced.
- Define target: centralized `AIBudgetOptions` config section with per-feature
  overrides.
- Specify token estimation approach for input prompt (character count heuristic
  or tiktoken-equivalent).
- Define behavior on budget exceeded: return structured error, log at Warning,
  do not call AI.

### 6. Audit Logging

- Every AI call must produce an audit record before discarding context.
- Minimum audit fields: `UserId`, `Feature` (signal/chat/risk), `Model`,
  `PromptVersion`, `InputTokensEstimated`, `OutputTokensActual`, `CostUsd`,
  `CacheHit`, `DurationMs`, `IsDataFresh`, `IsGrowwConnected`, `Timestamp`.
- Define whether audit goes to DB table, structured log, or both.
- Specify that audit log must never contain prompt text that includes live
  option prices, position details, or user-identifying market data.
- Define retention policy for audit records.

### 7. User Isolation

- Confirm current isolation gaps (if any) beyond the Groww per-user gate
  already in place.
- Define rules: AI context built per request, never shared across users, cache
  key always includes user scope or is market-snapshot-hash-only (global but
  safe).
- Confirm ChatService per-user Groww check is sufficient or needs strengthening.

### 8. Graceful Degradation

- Define what each AI feature returns when the AI API is unavailable or returns
  an error.
- Signal: return structured error to frontend, do not show partial/invalid
  signal.
- Chat: return user-friendly message, preserve conversation history.
- Risk check: skip AI enrichment, log Warning, continue with rule-based alert
  only.
- Document retry policy: max retries, backoff, circuit breaker consideration.

### 9. Safety Disclaimers

- Document required disclaimer injection rules.
- Every AI signal response must include a disclaimer that this is not financial
  advice and user must verify before trading.
- Chat responses must include the disclaimer when market data or signals are
  discussed.
- Disclaimer text must be configurable, not hardcoded.

### 10. Implementation Roadmap (High Level)

- Phase 1: Centralize model config and budget options in appsettings.
- Phase 2: Extract prompt templates to versioned files.
- Phase 3: Add AI audit log table and structured logging.
- Phase 4: Harden response caching with user-scope and cache metadata.
- Phase 5: Add circuit breaker and graceful degradation handling.
- For each phase, state estimated scope (small/medium/large) and whether it is
  a Codex implementation task or a further Claude Coworker design task.

## Data Safety

- No secrets, tokens, API keys, connection strings, Groww credentials, or
  private account data in any documentation produced.
- No sample prompts containing real user positions, real option prices, or
  real account identifiers.
- Audit log design must explicitly exclude sensitive market context from log
  body.

## Validation

Documentation-only task. Required checks:

- `git diff --check` — passes on produced Markdown files.
- All section headings from Requirements above are present in produced output.
- No backend source, frontend source, migration, or deployment workflow file is
  modified.
- Handoff updated in `docs/AI_HANDOFF.md` and `docs/ai/handoffs/latest.md`.

## Deliverable

- New file: `docs/ai/architecture/AI-ARCHITECTURE.md` (primary output).
- Optional supporting files under `docs/ai/architecture/` if sections are large
  enough to warrant splitting.
- Updated `docs/ai/08-DECISIONS-LOG.md` with a decision entry for the AI
  architecture design.
- Updated `docs/AI_HANDOFF.md` change log entry.
- Updated `docs/ai/handoffs/latest.md`.
