# TASK-0003: Centralize AI Configuration — Phase 1

**Status:** Approved
**Author:** Claude Coworker
**Date:** 2026-06-25
**Phase:** AI Architecture Plan — Phase 1 (§10, AI-ARCHITECTURE-PLAN.md)

---

## Goal

Replace scattered `config["Claude:SonnetModel"]` / `config["Claude:HaikuModel"]` reads with a single strongly-typed `AIOptions` binding. Add `AI:Models`, `AI:Budget`, and `AI:Disclaimers` sections to `appsettings.json`. Register options in DI. **No behavior change.**

---

## Role

Codex is coding agent. Implement only this task.

Claude Coworker / Claude AI is Architect, Consultant, and Reviewer.

---

## Read First

- `CLAUDE.md`
- `docs/AI_HANDOFF.md`
- `docs/ai/README.md`
- `docs/ai/01-PROJECT-CONTEXT.md`
- `docs/ai/02-ARCHITECTURE-CONTEXT.md`
- `docs/ai/04-CODING-STANDARDS.md`
- `docs/ai/06-DATA-SAFETY-RULES.md`
- `docs/ai/architecture/AI-ARCHITECTURE-PLAN.md` (Section 2 — Model Routing Strategy, Section 5 — Token Budget Management, Section 10 — Phase 1)

---

## Context

Three service classes and one endpoint currently read AI model names directly from `IConfiguration` using raw string keys:

| File | Line | Config Key Read |
|---|---|---|
| `Features/Signals/AISignalService.cs` | 126 | `config["Claude:SonnetModel"]` — signal model |
| `Features/Signals/AISignalService.cs` | 303 | `config["Claude:HaikuModel"]` — risk check model |
| `Features/Chat/ChatService.cs` | 80 | `config["Claude:SonnetModel"]` — chat model |
| `Features/AI/AICredentialEndpoints.cs` | 33 | `config["Claude:HaikuModel"]` — API key validation test call |

`AppConstants.AiTokenLimits` and `AppConstants.RateLimits` hold token and rate limit values as hardcoded class constants. No strongly-typed options class exists. No `AI:` section exists in `appsettings.json`.

The existing `Claude:` section in `appsettings.json` also contains `Claude:BypassMarketHours`, which is read by `AISignalService`, `ChatService`, and `PositionMonitorWorker`. That key is **not in scope** for this task.

---

## Scope

- Create `backend/src/OptionsEdge.API/Common/Configuration/AIOptions.cs` with `AIOptions`, `AIModelOptions`, `AIFeatureOptions`, `AIBudgetOptions`, `AIBudgetFeatureOptions`, `AIRateLimitOptions`, `AIDisclaimerOptions` classes.
- Add `AI:` config section to `backend/src/OptionsEdge.API/appsettings.json`.
- Register `Configure<AIOptions>` in `Program.cs` (see Requirements §3).
- Inject `IOptions<AIOptions>` into `AISignalService` and `ChatService` as an **additional** constructor parameter alongside existing `IConfiguration config`. Replace the four model-name reads with options-based resolution. Keep `IConfiguration` in both constructors — it is still used for `Claude:BypassMarketHours`.
- Inject `IOptions<AIOptions>` into the POST `/api/v1/ai/credentials` lambda and replace the `config["Claude:HaikuModel"]` read at line 33.
- Add a fallback-only comment to `AppConstants.Models`.

---

## Out Of Scope

- No `ModelRouter`, `PromptRegistry`, `AIBudgetGuard`, or `IAIOrchestrator` — those are Phase 2+.
- Do **not** remove `Claude:BypassMarketHours` config key or its `IConfiguration` reads in any file.
- Do **not** remove the `Claude:` section from `appsettings.json` — it must remain intact.
- Do **not** modify `PositionMonitorWorker.cs` — it only reads `BypassMarketHours`, not model names.
- No EF Core migrations.
- No frontend changes.
- No changes to existing API contracts, signal logic, risk check behavior, or streaming chat logic.
- No changes to `AppConstants.AiTokenLimits` or `AppConstants.RateLimits` constants — they remain. Only model name reads are migrated in Phase 1.

---

## Requirements

### 1. Create `AIOptions.cs`

**Path:** `backend/src/OptionsEdge.API/Common/Configuration/AIOptions.cs`

```csharp
using OptionsEdge.API.Common.Constants;

namespace OptionsEdge.API.Common.Configuration;

public class AIOptions
{
    public AIModelOptions Models { get; init; } = new();
    public Dictionary<string, AIFeatureOptions> Features { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public AIBudgetOptions Budget { get; init; } = new();
    public AIDisclaimerOptions Disclaimers { get; init; } = new();
}

public class AIModelOptions
{
    // Fallback defaults reference AppConstants so model strings are never duplicated.
    public string Quick   { get; init; } = AppConstants.Models.Haiku;
    public string Deep    { get; init; } = AppConstants.Models.Sonnet;
    public string Default { get; init; } = AppConstants.Models.Haiku;
}

public class AIFeatureOptions
{
    /// <summary>Tier key: "Quick", "Deep", or "Default". Resolved by ResolveModel helper.</summary>
    public string ModelTier { get; init; } = "Default";
}

public class AIBudgetOptions
{
    public int DefaultMaxOutputTokens { get; init; } = 1000;
    public Dictionary<string, AIBudgetFeatureOptions> Features { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, AIRateLimitOptions> RateLimits { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public class AIBudgetFeatureOptions
{
    public int MaxOutputTokens { get; init; } = 1000;
}

public class AIRateLimitOptions
{
    public int CallsPerHour { get; init; } = 10;
}

public class AIDisclaimerOptions
{
    public string SignalDisclaimer  { get; init; } = string.Empty;
    public string ChatDisclaimer    { get; init; } = string.Empty;
    public List<string> InjectOnFeatures { get; init; } = [];
}
```

`AIModelOptions` defaults reference `AppConstants.Models.*` — do not hardcode model strings a second time.

---

### 2. Add `AI:` Section to `appsettings.json`

Add the following section to `backend/src/OptionsEdge.API/appsettings.json`. Insert it **after** the existing `Claude:` block. Do not remove or alter any existing keys.

```json
"AI": {
  "Models": {
    "Quick":   "claude-haiku-4-5-20251001",
    "Deep":    "claude-sonnet-4-6-20250514",
    "Default": "claude-haiku-4-5-20251001"
  },
  "Features": {
    "Signal":    { "ModelTier": "Deep"  },
    "RiskCheck": { "ModelTier": "Quick" },
    "Chat":      { "ModelTier": "Deep"  }
  },
  "Budget": {
    "DefaultMaxOutputTokens": 1000,
    "Features": {
      "Signal":    { "MaxOutputTokens": 1000 },
      "RiskCheck": { "MaxOutputTokens": 500  },
      "Chat":      { "MaxOutputTokens": 1500 }
    },
    "RateLimits": {
      "Free":  { "CallsPerHour": 5  },
      "Basic": { "CallsPerHour": 10 },
      "Pro":   { "CallsPerHour": 50 }
    }
  },
  "Disclaimers": {
    "SignalDisclaimer": "This signal is for informational purposes only and does not constitute financial advice. Always verify before trading.",
    "ChatDisclaimer":   "AI analysis is for informational purposes only. Not financial advice. Trade manually and apply your own judgment.",
    "InjectOnFeatures": [ "Signal", "Chat" ]
  }
}
```

> **Note on model string:** `AI:Models:Deep` uses `claude-sonnet-4-6-20250514` (fully versioned). `AppConstants.Models.Sonnet` holds `claude-sonnet-4-6` (non-versioned alias). Both are valid Anthropic API values; the config value wins at runtime because `AIModelOptions.Deep` is populated from config. Do not change `AppConstants.Models.Sonnet`.

> **Important — preserve currently effective model values:** Because this task has a strict no-behavior-change requirement, `AI:Models` values must preserve the model IDs already in use at runtime.
> - Check `appsettings.json` for `Claude:SonnetModel` and `Claude:HaikuModel`. If those keys are present, copy their exact values into `AI:Models:Deep` and `AI:Models:Quick` respectively. Do not substitute a different model ID.
> - The JSON block above shows the intended target values. If the existing `Claude:SonnetModel` / `Claude:HaikuModel` values differ from those, the existing values win.
> - If there is a mismatch between `Claude:SonnetModel` / `Claude:HaikuModel` in `appsettings.json` and the corresponding constants in `AppConstants.Models`, preserve the currently effective runtime behavior (config wins over constants) and document the mismatch in the handoff under a `Model ID Mismatch` heading.

---

### 3. Register Options in DI — `Program.cs`

Add the following line in `Program.cs` **before** `builder.Services.AddAIServices()`, grouped with the other service registrations:

```csharp
builder.Services.Configure<AIOptions>(builder.Configuration.GetSection("AI"));
```

Add the required `using` statement at the top of `Program.cs`:

```csharp
using OptionsEdge.API.Common.Configuration;
```

Do **not** change the signature of `AddAIServices`. The existing pattern for `AddXxxServices` extension methods takes only `IServiceCollection` — keep it consistent.

---

### 4. Update `AISignalService`

**File:** `backend/src/OptionsEdge.API/Features/Signals/AISignalService.cs`

Add to usings:
```csharp
using Microsoft.Extensions.Options;
using OptionsEdge.API.Common.Configuration;
```

Add `IOptions<AIOptions> aiOptions` as an additional constructor parameter (after existing `IConfiguration config`):

```csharp
public class AISignalService(
    ClaudeApiClient claude,
    SignalCacheService cache,
    IndicatorService indicatorService,
    OptionsService optionsService,
    IMarketDataService marketData,
    IHubContext<MarketHub> hub,
    AppDbContext db,
    IConfiguration config,
    IOptions<AIOptions> aiOptions,      // ← add
    UserAICredentialService aiCredentials,
    GrowwCredentialService growwCredentials,
    ILogger<AISignalService> logger)
```

Replace the model resolution at line 126 (signal):

```csharp
// Before:
var model = config["Claude:SonnetModel"] ?? AppConstants.Models.Sonnet;

// After:
var model = ResolveModel(aiOptions.Value, "Signal");
```

Replace the model resolution at line 303 (risk check):

```csharp
// Before:
var model = config["Claude:HaikuModel"] ?? AppConstants.Models.Haiku;

// After:
var model = ResolveModel(aiOptions.Value, "RiskCheck");
```

Add this private static helper method in `AISignalService`:

```csharp
private static string ResolveModel(AIOptions ai, string feature)
{
    var tier = ai.Features.TryGetValue(feature, out var f) ? f.ModelTier : "Default";
    return tier switch
    {
        "Quick"   => ai.Models.Quick,
        "Deep"    => ai.Models.Deep,
        _         => ai.Models.Default
    };
}
```

---

### 5. Update `ChatService`

**File:** `backend/src/OptionsEdge.API/Features/Chat/ChatService.cs`

Add to usings:
```csharp
using Microsoft.Extensions.Options;
using OptionsEdge.API.Common.Configuration;
```

Add `IOptions<AIOptions> aiOptions` as an additional constructor parameter (after existing `IConfiguration config`):

```csharp
public class ChatService(
    ClaudeApiClient claude,
    IndicatorService indicatorService,
    IMarketDataService marketData,
    AppDbContext db,
    IConfiguration config,
    IOptions<AIOptions> aiOptions,      // ← add
    UserAICredentialService aiCredentials,
    GrowwCredentialService growwCredentials,
    ILogger<ChatService> logger)
```

Replace the model resolution at line 80 (chat):

```csharp
// Before:
var model = config["Claude:SonnetModel"] ?? AppConstants.Models.Sonnet;

// After:
var model = ResolveModel(aiOptions.Value, "Chat");
```

Add the same private static helper in `ChatService` (duplicate the method — do not introduce a shared static utility class in Phase 1):

```csharp
private static string ResolveModel(AIOptions ai, string feature)
{
    var tier = ai.Features.TryGetValue(feature, out var f) ? f.ModelTier : "Default";
    return tier switch
    {
        "Quick"   => ai.Models.Quick,
        "Deep"    => ai.Models.Deep,
        _         => ai.Models.Default
    };
}
```

---

### 6. Update `AICredentialEndpoints`

**File:** `backend/src/OptionsEdge.API/Features/AI/AICredentialEndpoints.cs`

Add to usings:
```csharp
using Microsoft.Extensions.Options;
using OptionsEdge.API.Common.Configuration;
```

In the POST `/api/v1/ai/credentials` lambda, add `IOptions<AIOptions> aiOptions` as a parameter (alongside the existing `IConfiguration config`):

```csharp
group.MapPost("/credentials", async (
    SaveAIKeyRequest req,
    UserAICredentialService aiCredentialService,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    IOptions<AIOptions> aiOptions,      // ← add
    HttpContext ctx,
    CancellationToken ct) =>
```

Replace the model string at line 33:

```csharp
// Before:
model = config["Claude:HaikuModel"] ?? "claude-haiku-4-5-20251001",

// After:
model = aiOptions.Value.Models.Quick,
```

The DELETE and GET endpoints do not read model names — leave them unchanged.

---

### 7. Update `AppConstants.Models` Comment

**File:** `backend/src/OptionsEdge.API/Common/Constants/AppConstants.cs`

Update the `Models` nested class to add a comment:

```csharp
public static class Models
{
    // Fallback defaults only — runtime model selection reads from AI:Models config via AIOptions.
    // Do not read these constants directly in service classes; use IOptions<AIOptions> instead.
    public const string Haiku = "claude-haiku-4-5-20251001";
    public const string Sonnet = "claude-sonnet-4-6";
}
```

---

## Data Safety

- No secrets, tokens, connection strings, Groww credentials, or private account data in any added or modified file.
- `AIOptions.cs` contains no sensitive values — model names, token limits, and disclaimer text only.
- `Claude:BypassMarketHours` must remain functional after this change. Do not touch its reads in `AISignalService`, `ChatService`, or `PositionMonitorWorker`.
- This task is config consolidation only — no Groww gating, freshness checks, signal output, or risk assessment logic changes.

---

## Validation

Run from `backend/`:

```bash
dotnet build src/OptionsEdge.API/OptionsEdge.API.csproj
```

```bash
dotnet test tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj
```

Confirm each of the following:

1. Build completes with **zero errors** and **zero new warnings** introduced by this change.
2. All existing tests pass (or, if tests cannot run due to missing infrastructure such as DB, record the reason explicitly in the handoff).
3. The following grep returns **no results** (confirms all raw config reads for model names are gone):

```bash
grep -rn 'config\["Claude:SonnetModel"\]\|config\["Claude:HaikuModel"\]' src/OptionsEdge.API/
```

4. The following grep returns results in `AISignalService.cs`, `ChatService.cs`, and `AICredentialEndpoints.cs` (confirms `IOptions<AIOptions>` is injected):

```bash
grep -rn 'IOptions<AIOptions>' src/OptionsEdge.API/
```

5. The following grep confirms `Claude:BypassMarketHours` reads are still present:

```bash
grep -rn 'BypassMarketHours' src/OptionsEdge.API/
```

---

## Deliverable

Return in `docs/ai/handoffs/latest.md`:

- List of all files changed (exact paths).
- Output of all four grep checks above.
- Build result (clean / errors).
- Test result, or reason tests were skipped.
- Confirmation that `Claude:BypassMarketHours` reads remain untouched.
- Any deviations from this task file, with justification.

Update `docs/AI_HANDOFF.md` to reflect Phase 1 complete.

---

*End of TASK-0003*
