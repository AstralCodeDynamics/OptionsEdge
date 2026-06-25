# AI Architecture Plan — OptionsEdge

**Status:** Draft — awaiting Manu review  
**Author:** Claude Coworker  
**Date:** 2026-06-23  
**Task:** TASK-0002

---

## 1. AI Subsystem Overview

### Current Shape

```
Frontend (React)
    │
    ▼
.NET Minimal APIs  (/api/v1/signals, /api/v1/chat/*)
    │
    ├── AISignalService          ← entry signals + risk checks
    │       ├── ClaudeApiClient  ← HTTP wrapper (shared infra)
    │       ├── SignalCacheService
    │       ├── IndicatorService
    │       └── OptionsService
    │
    └── ChatService              ← streaming chat
            ├── ClaudeApiClient
            └── IndicatorService / market data
```

**AI entry points found in source:**

| Entry Point | Location | Model | Use Case |
|---|---|---|---|
| `AISignalService.GenerateEntrySignalAsync` | Features/Signals | Sonnet | Entry signal generation |
| `AISignalService.RunPositionRiskCheckAsync` | Features/Signals | Haiku | Position risk assessment |
| `ChatService.StreamMessageAsync` | Features/Chat | Sonnet | Streaming market-aware chat |

**Infrastructure:**

| Component | Location | Role |
|---|---|---|
| `ClaudeApiClient` | Infrastructure/Claude | HTTP calls to Anthropic API |
| `UserAICredentialService` | Features/AI | Per-user API key lookup |
| `AIUsageLog` (EF entity) | Domain/Entities | Basic usage audit |
| `AppConstants.Models` | Common/Constants | Model name strings |
| `AppConstants.AiTokenLimits` | Common/Constants | Per-call token caps |
| `AppConstants.RateLimits` | Common/Constants | Per-plan hourly call limits |

### Target Shape

```
Frontend (React)
    │
    ▼
.NET Minimal APIs
    │
    ├── AISignalService          ← calls IAIOrchestrator
    ├── ChatService              ← calls IAIOrchestrator
    └── [future: RiskWorker]     ← calls IAIOrchestrator
            │
            ▼
    ┌─────────────────────────────────────────────────────┐
    │  AI Infrastructure Layer (Infrastructure/AI/)       │
    │                                                     │
    │  IAIOrchestrator                                    │
    │      ├── ModelRouter        ← pick model by feature │
    │      ├── PromptRegistry     ← load versioned prompt │
    │      ├── AIResponseCache    ← check/set cache       │
    │      ├── AIBudgetGuard      ← enforce token limits  │
    │      ├── ClaudeApiClient    ← HTTP to Anthropic     │
    │      └── AIAuditLogger      ← write audit record    │
    └─────────────────────────────────────────────────────┘
```

**Boundary rule:** `AISignalService` and `ChatService` own business logic (context building, output validation, DB persistence, Groww gating). The AI infrastructure layer owns cross-cutting concerns: model selection, prompt loading, caching, budget, audit.

---

## 2. Model Routing Strategy

### Current State (gap)

Both services read model names from `IConfiguration` directly:

```csharp
// AISignalService — signal
var model = config["Claude:SonnetModel"] ?? AppConstants.Models.Sonnet;

// AISignalService — risk check
var model = config["Claude:HaikuModel"] ?? AppConstants.Models.Haiku;

// ChatService
var model = config["Claude:SonnetModel"] ?? AppConstants.Models.Sonnet;
```

Config keys are used inconsistently. No routing abstraction.

### Target: `ModelRouter`

Config section in `appsettings.json`:

```json
"AI": {
  "Models": {
    "Quick":  "claude-haiku-4-5-20251001",
    "Deep":   "claude-sonnet-4-6-20250514",
    "Default": "claude-haiku-4-5-20251001"
  },
  "Features": {
    "Signal":    { "ModelTier": "Deep"  },
    "RiskCheck": { "ModelTier": "Quick" },
    "Chat":      { "ModelTier": "Deep"  }
  }
}
```

`ModelRouter` resolves feature name → tier → model string. Services never read model names directly.

**Routing rules:**

| Feature | Tier | Rationale |
|---|---|---|
| Signal generation | Deep (Sonnet) | Multi-factor technical analysis, structured JSON, high accuracy requirement |
| Position risk check | Quick (Haiku) | Frequent background checks, simpler prompt, cost control |
| Chat | Deep (Sonnet) | Multi-turn context, nuanced reasoning, user-facing |
| Future: auto market summary | Quick (Haiku) | Scheduled, lightweight |

Model names are **never hardcoded in service classes**. Constants in `AppConstants.Models` remain as fallback defaults only.

---

## 3. Prompt Versioning

### Current State (gap)

Prompts are inline strings in service methods:

- `BuildSignalSystemPrompt(DateTime istNow)` — dynamic, IST date injected
- `BuildSignalPrompt(...)` — large interpolated string
- `BuildRiskCheckPrompt(...)` — short interpolated string
- `RiskCheckSystemPrompt` — const string
- `ChatPersona` — const string, embedded in `BuildSystemPromptAsync`

No versioning. No traceability from audit log to prompt used.

### Target: `PromptRegistry`

**Storage:** Embedded resources under `Infrastructure/AI/Prompts/`:

```
Infrastructure/AI/Prompts/
  signal-system/v1.txt
  signal-user/v1.txt
  risk-check-system/v1.txt
  risk-check-user/v1.txt
  chat-persona/v1.txt
```

Files are embedded at build time (`EmbeddedResource` in `.csproj`). Dynamic parts (market data, indicators, strike table) remain code-injected at call time using `string.Format` or `StringBuilder` substitution on the loaded template.

**Version contract:**

```
{feature}-{role}/{version}.txt
```

- `feature`: `signal`, `risk-check`, `chat`
- `role`: `system`, `user`
- `version`: `v1`, `v2`, ...

**`PromptRegistry` interface:**

```csharp
public interface IPromptRegistry
{
    // Load template; caller injects dynamic values after
    string Load(string feature, string role, string version = "latest");
    string CurrentVersion(string feature, string role);
}
```

**Audit record** stores `PromptVersion` as `"signal-system/v1"` so any logged signal is traceable to the exact prompt.

**Version bump rule:** Any change to prompt text = new version file. Old files stay. No in-place edits to existing version files.

---

## 4. Response Caching

### Current State

`SignalCacheService` exists and is used by `AISignalService`:

```csharp
var cacheKey = cache.BuildKey(
    key,                // symbol
    indicators.Rsi.Value,
    macdTrend,
    (double)snapshot.Pcr,
    snapshot.Ltp);
```

5-minute TTL (assumed from CLAUDE.md). Cache is in-process (`MemoryCache` or similar). No `fromCache` flag exposed in response until recently added (`cached with { FromCache = true }`).

### Target Cache Contract

**Cache key structure:**

```
ai:signal:{symbol}:{snapshotHash}
ai:risk:{positionId}:{snapshotHash}
```

`snapshotHash` = deterministic hash over fields that change signal output: `Ltp`, `Rsi`, `MacdTrend`, `Pcr`. Same market state = same hash = cache hit.

**No user ID in signal cache key** — signal is determined by market snapshot, not by user identity. Risk check cache key uses `positionId` (already user-scoped).

**User isolation rule:**

> Signal cache is market-snapshot-scoped (safe to share). Risk check cache is position-scoped (inherently user-isolated). Chat context is never cached — always rebuilt per request.

**TTL per use case:**

| Feature | TTL | Invalidation |
|---|---|---|
| Signal | 5 min | Market snapshot hash change |
| Risk check | 2 min | Position change or snapshot hash change |
| Chat | No cache | N/A |

**Cache metadata in response:**

```csharp
record SignalResponse(
    ...
    bool FromCache,
    string? CacheKey   // optional, for debug/audit only, never shown in UI
);
```

**Distributed cache:** Phase 4 target is Redis (`IDistributedCache`). Current in-process cache acceptable for single-instance dev. Redis keyed identically — no service logic changes on migration.

---

## 5. Token Budget Management

### Current State (gap)

Token limits defined as constants:

```csharp
// AppConstants.AiTokenLimits
SonnetMaxTokens = 1000
HaikuMaxTokens  = 500  (assumed)
```

Rate limit per user per plan exists in `AppConstants.RateLimits`. Enforcement is in both `AISignalService` and `ChatService` with duplicated logic.

### Target: `AIBudgetOptions` + `AIBudgetGuard`

**Config section:**

```json
"AI": {
  "Budget": {
    "DefaultMaxOutputTokens": 1000,
    "Features": {
      "Signal":    { "MaxOutputTokens": 1000 },
      "RiskCheck": { "MaxOutputTokens": 500  },
      "Chat":      { "MaxOutputTokens": 1500 }
    },
    "RateLimits": {
      "Free":    { "CallsPerHour": 5  },
      "Basic":   { "CallsPerHour": 10 },
      "Pro":     { "CallsPerHour": 30 }
    }
  }
}
```

**`AIBudgetGuard` responsibilities:**

1. Check hourly call count against plan limit before any AI call.
2. Estimate input token count (character count / 4 heuristic — sufficient for budget guard, not for billing).
3. Enforce `MaxOutputTokens` per feature when calling `ClaudeApiClient`.
4. On budget exceeded: return structured `AIBudgetExceededException`, log at `Warning`, never call AI.

**Rate limit logic moves out of feature services** into `AIBudgetGuard`. Feature services call `GuardAsync(userId, feature)` — throw or return error result. Removes duplication between `AISignalService` and `ChatService`.

---

## 6. Audit Logging

### Current State (gap)

`AIUsageLog` entity exists with fields: `UserId`, `Feature`, `ModelUsed`, `InputTokens`, `OutputTokens`, `CostUsd`, `CreatedAt`.

Missing fields: `PromptVersion`, `CacheHit`, `DurationMs`, `IsDataFresh`, `IsGrowwConnected`.

Audit is written by each feature service separately (not centralized). No audit on cache hits. No duration tracking.

### Target: Centralized Audit Record

**Minimum audit fields (target `AIUsageLog` schema extension):**

| Field | Type | Source |
|---|---|---|
| `UserId` | `Guid` | Auth context |
| `Feature` | `string` | Caller (`signal`, `risk-check`, `chat`) |
| `Model` | `string` | `ModelRouter` result |
| `PromptVersion` | `string` | `PromptRegistry` version used |
| `InputTokensEstimated` | `int` | Pre-call estimate |
| `OutputTokensActual` | `int` | API response |
| `CostUsd` | `decimal` | `ClaudeApiClient.CalculateCost` |
| `CacheHit` | `bool` | Whether cache served response |
| `DurationMs` | `int` | Wall-clock from guard to response |
| `IsDataFresh` | `bool` | From Groww gate at call time |
| `IsGrowwConnected` | `bool` | From Groww gate at call time |
| `Timestamp` | `DateTimeOffset` | UTC |

**Audit destination:** Both DB table and structured log.

- DB table: for billing, cost analysis, per-user dashboards, retention queries.
- Structured log (`ILogger`): for operational monitoring, alerting, Seq/Grafana.

**What must never appear in audit log body:**

- Live option prices or premium values
- User position entry/stop/target prices
- User-identifying trading context (position details, P&L)
- Groww credentials or tokens
- Prompt text (log `PromptVersion` string only, not prompt content)

**Retention:** 90-day hard delete via scheduled `IHostedService` or PostgreSQL partition. Configurable via `AI:Audit:RetentionDays`.

**Centralization:** `AIAuditLogger` writes both DB and structured log. Feature services pass an `AICallContext` (feature, prompt version, freshness flags) and receive back audit ID. Audit on cache hit is a fast DB write with `CacheHit=true`, `DurationMs=0`, no token fields.

---

## 7. User Isolation

### Current Assessment

| Isolation Point | Current State | Gap? |
|---|---|---|
| Signal cache | Market-snapshot hash key (no user ID) | Safe — market data is public |
| Risk check cache | (not cached today) | Safe when added if keyed by positionId |
| Chat context | Built per-request from user's own positions + Groww gate | Safe |
| AI API key | Per-user via `UserAICredentialService` | Safe |
| Groww credentials | Per-user via `GrowwCredentialService` | Safe |
| Chat market context | Groww gate checked in `BuildSystemPromptAsync` before reading shared cache | Safe |
| Audit log | `UserId` always written | Safe |

**Gap identified:** `ChatService.BuildSystemPromptAsync` reads shared market snapshot cache (`marketData.GetSnapshot(sym)`) only after checking `hasGroww`. This is correct. **No cross-user leakage found.**

**Rules to codify (prevent future regression):**

1. Signal cache key must never include user-specific data (signal is market-state output, not user output).
2. Risk check cache key must always include `positionId` (position is user-scoped).
3. Chat context must always go through Groww gate before reading `IMarketDataService`.
4. `IAIOrchestrator` must accept `userId` on every call and pass it to `AIAuditLogger`. Never call AI without attributable user context.
5. AI context construction (prompt building) must happen inside the per-request service call, never in a shared static or background-cached field.

---

## 8. Graceful Degradation

### Degradation Matrix

| Feature | Failure Condition | Degraded Response |
|---|---|---|
| Signal | Claude API error / timeout | `(null, "AI service temporarily unavailable.")` — do not show partial signal |
| Signal | Budget exceeded | `(null, "Rate limit reached. X calls/hour.")` — existing message |
| Signal | Missing market data | `(null, "Live market data temporarily unavailable.")` — existing |
| Signal | Strike sanity fail | `(null, "Strike failed sanity check.")` — existing |
| Risk check | Claude API error | `RiskCheckResponse("INFO", "UNKNOWN", "Unable to assess risk")` — existing |
| Risk check | Missing market snapshot | Skip AI enrichment, return rule-based alert only, log Warning |
| Chat | Claude API error | `ChatStreamChunk("error", Error: "AI service temporarily unavailable.")` |
| Chat | Budget exceeded | Stream error chunk, preserve conversation history |

### Retry Policy

**Recommended target (Phase 5 implementation):**

```
Max retries: 2
Retry delays: 500ms, 1500ms (exponential backoff)
Retry on: HTTP 429, HTTP 503, transient network errors
Do not retry on: HTTP 400 (bad request), parse errors, budget errors, safety blocks
```

**Circuit breaker consideration:**

- Threshold: 5 consecutive AI call failures within 60 seconds.
- Open state: return degraded response immediately, no API call.
- Half-open after 30 seconds.
- Use `Microsoft.Extensions.Resilience` (`AddResiliencePipeline`) — already available in .NET 8+.

No circuit breaker in Phase 1–4. Add in Phase 5 once basic infrastructure is in place.

---

## 9. Safety Disclaimers

### Current State (gap)

No consistent disclaimer injection. `ChatPersona` includes "you provide analysis, signals, and risk guidance only, never execute trades" but this is in the system prompt — not in the response to the user.

### Target: Configurable Disclaimer Injection

**Config section:**

```json
"AI": {
  "Disclaimers": {
    "SignalDisclaimer": "This signal is for informational purposes only and does not constitute financial advice. Always verify before trading. Past signals are not indicative of future results.",
    "ChatDisclaimer": "AI analysis is for informational purposes only. Not financial advice. Trade manually and apply your own judgment.",
    "InjectOnFeatures": [ "Signal", "Chat" ]
  }
}
```

**Injection rules:**

- Signal: always append disclaimer to `Rationale` or as a separate `Disclaimer` field in `SignalResponse`. Never buried inside AI-generated rationale text.
- Chat: append disclaimer when response contains market price references, signal recommendations, or risk guidance keywords. Detected client-side or via post-processing flag on response.
- Disclaimer text is never hardcoded in service classes — always loaded from config so it can be updated without code deploy.

---

## 10. Implementation Roadmap

### Phase 1 — Centralize Config (Small | Codex task)

**Scope:** No new infrastructure classes. Config consolidation only.

- Create `AI:Models`, `AI:Budget`, `AI:Disclaimers` sections in `appsettings.json`.
- Remove direct `config["Claude:SonnetModel"]` reads from service classes.
- Create `AIOptions`, `AIBudgetOptions`, `AIDisclaimerOptions` strongly-typed option classes.
- Register via `services.Configure<AIOptions>(config.GetSection("AI"))`.
- Update `AppConstants.Models` to point at defaults (kept as fallback only).
- **No behavior change.** Services read from options classes instead of raw config strings.
- Estimated: Small (1–2 hours Codex work).

### Phase 2 — Prompt Registry (Medium | Codex task)

**Scope:** Extract prompts to versioned embedded files. Zero behavioral change.

- Create `Infrastructure/AI/Prompts/` with `signal-system/v1.txt`, `risk-check-system/v1.txt`, `chat-persona/v1.txt`.
- Create `PromptRegistry` (implements `IPromptRegistry`). Loads embedded resources by feature/role/version. Caches loaded strings in `IMemoryCache` with no expiry (build-time static content).
- Update `AISignalService` and `ChatService` to load base prompts via `PromptRegistry`. Dynamic values still injected by service after load.
- `PromptRegistry.CurrentVersion` returns version string for audit.
- Register in DI in `Infrastructure/AI/AIInfrastructureExtensions.cs`.
- Estimated: Medium (2–4 hours Codex work).

### Phase 3 — AI Audit Log Hardening (Medium | Codex task)

**Scope:** Extend `AIUsageLog` entity with missing fields. Centralize audit write.

- EF Core migration: add `PromptVersion`, `CacheHit`, `DurationMs`, `IsDataFresh`, `IsGrowwConnected` to `AIUsageLogs` table.
- Create `AIAuditLogger` service. Accepts `AIAuditRecord` DTO. Writes to DB + `ILogger` structured log.
- Remove duplicated audit write from `AISignalService` and `ChatService`. Replace with `AIAuditLogger.LogAsync(...)`.
- Add `AI:Audit:RetentionDays` config. Implement `AuditRetentionWorker` (`BackgroundService`) running nightly.
- Estimated: Medium (3–5 hours Codex work + 1 migration).

### Phase 4 — Cache Hardening + Budget Guard (Medium | Codex task)

**Scope:** Harden cache contract and centralize rate limit enforcement.

- Standardize `SignalCacheService` key to `ai:signal:{symbol}:{snapshotHash}` format.
- Add Redis `IDistributedCache` fallback path (keep in-process as default, Redis as opt-in via config `AI:Cache:UseRedis`).
- Create `AIBudgetGuard`. Move rate limit check out of `AISignalService` and `ChatService`.
- `AIBudgetGuard.GuardAsync(userId, feature, ct)` — throws `AIBudgetExceededException` or returns guard result. Estimated input tokens included in result.
- Add `fromCache` flag to all AI response types that support caching.
- Estimated: Medium (3–5 hours Codex work).

### Phase 5 — Resilience + Circuit Breaker (Large | Codex task + Claude Coworker design)

**Scope:** Retry policy and circuit breaker around `ClaudeApiClient`.

- Claude Coworker designs specific resilience pipeline config first (separate design task).
- Add `Microsoft.Extensions.Resilience` pipeline to `ClaudeApiClient` HTTP client registration.
- Configure retry (2 retries, exponential backoff) and circuit breaker (5 failures / 60s).
- Add `AI:Resilience` config section (thresholds, timeouts configurable).
- Verify all degradation paths in §8 return correct fallback without throwing unhandled exceptions.
- Estimated: Large (design + 4–8 hours Codex work).

---

## Gaps Not Covered by This Plan

These are noted for future design tasks, not blocking Phase 1–5:

- **Auto-signal worker (`AutoSignalWorker`):** Uses `AISignalService` indirectly. When budget guard is added, auto-signal calls count against user's hourly limit — confirm this is the intended behavior with Manu.
- **Streaming audit for chat:** `ChatService.StreamMessageAsync` accumulates tokens across stream. Audit is written post-stream. Acceptable for now; streaming mid-call audit is a future enhancement.
- **AI for backtest:** Backtest currently uses no AI. If AI-assisted signal replay is added, it should route through the same `IAIOrchestrator` rather than a separate path.
- **Multi-model fallback:** If Sonnet is unavailable, fall back to Haiku? Not recommended for signal quality reasons — document this explicitly when Phase 5 is designed.

---

## Safety Constraints (Non-Negotiable)

These apply to all phases. Codex must not deviate from these in any implementation task.

1. No AI actionable signal from stale or missing Groww data.
2. No AI call when `isGrowwConnected=false` and `Groww:Enabled=true` (for signal and risk check features).
3. Audit log must never contain prompt text with live prices, position details, or user-identifying trading context.
4. No secrets, tokens, Groww credentials, or API keys in any artefact or log record.
5. Cache key must never serve one user's AI context to another user.
6. Disclaimer must appear in every signal response and in relevant chat responses.
7. Budget exceeded → return error, do not call AI, do not silently skip.

---

*End of AI-ARCHITECTURE-PLAN.md*
