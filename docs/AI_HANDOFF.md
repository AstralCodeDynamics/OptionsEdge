# AI Handoff

This file is the shared working memory for Codex and Claude AI. Read it before non-trivial changes and update it after changes that affect behavior, architecture, API contracts, data flow, or important debugging context.

## Agent Roles (as of June 2026)

- **Coding agent**: Codex — implements all backend and frontend changes, runs builds, commits
- **Reviewer/Consultant**: Claude AI (claude.ai) — reviews Codex's commits, writes implementation instructions, diagnoses issues, does NOT commit code directly
- **No Claude Code**: Claude Code has been retired from this project due to token cost. All implementation tasks go to Codex only.

## Update Protocol

- Add newest notes at the top of `Change Log`.
- Keep entries concise but specific: files changed, behavior changed, tests run, and known caveats.
- Do not record secrets, tokens, connection strings, or private account data.
- If a change is experimental or incomplete, mark it clearly.

## Current Focus

Backtest and Groww integration for NIFTY/BANKNIFTY options strategy testing.

Important caveat: Groww historical candles are real index candles, but historical option premiums are still estimated in the backtest engine with Black-Scholes. The backtest validates signal timing against market candles, not real historical option LTP replay.

## Change Log

### 2026-06-23 - Codex: AI artefact workflow structure

Files changed:

- `docs/ai/README.md`
- `docs/ai/00-COWORKER-PROJECT-MEMORY.md`
- `docs/ai/01-PROJECT-CONTEXT.md`
- `docs/ai/02-ARCHITECTURE-CONTEXT.md`
- `docs/ai/03-FUNCTIONAL-FLOWS.md`
- `docs/ai/04-CODING-STANDARDS.md`
- `docs/ai/05-AI-USAGE-RULES.md`
- `docs/ai/06-DATA-SAFETY-RULES.md`
- `docs/ai/07-CURRENT-STATE.md`
- `docs/ai/08-DECISIONS-LOG.md`
- `docs/ai/09-KNOWN-ISSUES.md`
- `docs/ai/10-REVIEW-CHECKLIST.md`
- `docs/ai/11-CODEX-INSTRUCTION-TEMPLATE.md`
- `docs/ai/12-COWORKER-REVIEW-TEMPLATE.md`
- `docs/ai/tasks/README.md`
- `docs/ai/tasks/pending/.gitkeep`
- `docs/ai/tasks/approved/.gitkeep`
- `docs/ai/tasks/in-progress/.gitkeep`
- `docs/ai/tasks/review/.gitkeep`
- `docs/ai/tasks/completed/.gitkeep`
- `docs/ai/reviews/pending/.gitkeep`
- `docs/ai/reviews/passed/.gitkeep`
- `docs/ai/reviews/failed/.gitkeep`
- `docs/ai/handoffs/README.md`
- `docs/ai/handoffs/latest.md`
- `CLAUDE.md`
- `docs/AI_HANDOFF.md`

Documentation:

- Added structured AI artefact workflow so Claude Coworker / Claude AI acts as Architect, Consultant, and Reviewer while Codex remains coding agent.
- Documented task-file-first workflow, Codex handoff requirement, review checklist use, and Groww data safety rules.
- No app behavior changed. No backend source, frontend source, migrations, or deployment workflow changed.

Validation:

- `git diff --check` — passed.

Claude Code active files: none. Codex active files: none.

### 2026-06-23 - Codex: Dashboard refresh countdown labels

Files changed:

- `frontend/src/hooks/useMarketData.ts`
- `frontend/src/components/market/MarketStatusBanner.tsx`
- `frontend/src/components/charts/PriceChart.tsx`
- `frontend/src/pages/Dashboard/index.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- Dashboard now polls Groww-backed market snapshots, status, and indicators every 30 seconds via REST, keeping the live Groww cache refreshed instead of relying only on SignalR.
- Added visible countdown labels so users can see when Live Index, Technical Indicators, Pivot Levels, and Market Pulse will refresh next.
- Price chart now checks candles every 30 seconds and shows its own countdown label.
- AI Signals section is labeled `Manual refresh` because signal generation remains user-triggered, not automatic.

Validation:

- `npm run build` from `frontend/` — passed (`tsc -b` and Vite), zero errors.

Claude Code active files: none. Codex active files: none.

### 2026-06-23 - Codex: Full position edit fields

Files changed:

- `backend/src/OptionsEdge.API/Features/Positions/Models.cs`
- `backend/src/OptionsEdge.API/Features/Positions/PositionEndpoints.cs`
- `frontend/src/components/positions/AddPositionModal.tsx`
- `frontend/src/components/positions/PositionCard.tsx`
- `frontend/src/pages/Positions/index.tsx`
- `frontend/src/services/api.ts`
- `docs/AI_HANDOFF.md`

Behavior:

- Position edit now supports user-editable Symbol, Type, Strike, Expiry, Entry Price, Quantity, Stop Loss, Target 1, and Target 2.
- Backend `UpdatePositionRequest` accepts optional contract/entry/quantity fields and persists valid provided values.
- Frontend edit modal title is now `Edit Position`; previously locked fields are no longer disabled and use normal input styling.
- `positionsApi.update()` and Positions edit submit now send all editable form fields.
- Position card action label changed from `Edit SL/Target` to `Edit Position`.

Validation:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — passed, 0 warnings.
- `npm run build` from `frontend/` — passed (`tsc -b` and Vite), zero errors.

Claude Code active files: none. Codex active files: none.

### 2026-06-23 - Codex: Critical Groww cache-miss fix, no silent mock fallback

Files changed:

- `backend/src/OptionsEdge.API/Infrastructure/Groww/GrowwMarketDataService.cs`
- `backend/src/OptionsEdge.API/Infrastructure/MockData/IMarketDataService.cs`
- `backend/src/OptionsEdge.API/Infrastructure/MockData/MockMarketDataService.cs`
- `backend/src/OptionsEdge.API/Features/Market/Models.cs`
- `backend/src/OptionsEdge.API/Features/Market/MarketEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Market/MarketService.cs`
- `backend/src/OptionsEdge.API/Features/Indicators/IndicatorEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Indicators/IndicatorService.cs`
- `backend/src/OptionsEdge.API/Features/Options/OptionsEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Options/OptionsService.cs`
- `backend/src/OptionsEdge.API/Features/Positions/Models.cs`
- `backend/src/OptionsEdge.API/Features/Positions/PositionEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Signals/AISignalService.cs`
- `backend/src/OptionsEdge.API/Features/Chat/ChatService.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Background/MarketDataWorker.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Background/PositionMonitorWorker.cs`
- `backend/tests/OptionsEdge.API.Tests/GrowwCacheMissTests.cs`
- `frontend/src/types/index.ts`
- `frontend/src/store/appStore.ts`
- `frontend/src/hooks/useMarketData.ts`
- `frontend/src/hooks/useSignalR.ts`
- `frontend/src/pages/Dashboard/index.tsx`
- `frontend/src/pages/Chain/index.tsx`
- `frontend/src/pages/Backtest/index.tsx`

Behavior:

- Fixed critical root cause behind wrong ATM, wrong option LTPs, false target hits, and alert spam: `GrowwMarketDataService.GetSnapshot()` no longer falls back to `MockMarketDataService` when Groww cache expires.
- `MockMarketDataService` is ONLY used when `Groww:Enabled=false` globally. It is NEVER a silent fallback for stale Groww cache. If Groww cache is empty, callers receive `null` or empty candles and must handle that explicitly.
- `GrowwGatedResponse<T>` now includes `IsDataFresh`. Groww-backed endpoints return `IsGrowwConnected=true, IsDataFresh=false, Data=null` when user is connected but live cache/data is unavailable.
- Options chain/max-pain endpoints refresh the user's Groww spot cache before building derived chain data; if refresh fails or cache remains empty, no chain is built.
- Indicators, AI signals, Chat market context, position P&L, and risk checks handle missing live data without using mock prices.
- `PositionMonitorWorker` skips spot history updates and alert evaluation when snapshot or option LTP is unavailable; logs warning instead of evaluating SL/target/IV/adverse-move alerts from missing data.
- Frontend store tracks `marketDataFresh`, clears cached snapshots/indicators on stale data, ignores SignalR market updates until REST confirms connected + fresh data, and shows amber degraded banners on Dashboard/Chain instead of rendering stale/mocked values.

Validation:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — passed, 0 warnings.
- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj` — passed, 52 tests.
- `npm run build` from `frontend/` — passed (`tsc -b` and Vite), zero errors.
- `git diff --check` — passed.

Claude Code active files: none. Codex active files: none.

### 2026-06-20 - Codex: Urgent stacked DANGER alert escape hatch

Files changed:

- `frontend/src/components/positions/AlertBanner.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- Each unread DANGER row now has an accessible dismiss button. Dismiss updates Zustand immediately and calls `alertsApi.markRead(id)`, matching existing Warning/Info behavior; alert remains in Notification History as read.
- When two or more DANGER alerts are pinned, banner shows a sticky count header with an always-reachable `Mark all read` action. It optimistically calls store `markAllRead()` and persists through `alertsApi.markAllRead()`.
- Fixed banner remains `z-50` and attention-grabbing, but is capped at `max-h-[40vh]` with internal scrolling and overscroll containment. Large alert volumes can no longer expand banner toward full-screen height.
- Confirmed `appStore.markRead` itself is local-only; backend persistence remains intentionally paired in `AlertBanner.dismiss`, avoiding a circular store/API dependency.

Validation:

- `npm run build` — passed (`tsc -b` and Vite), zero errors.
- `git diff --check` — passed.
- Static verification confirmed individual and bulk controls, backend calls, sticky bulk header, `40vh` cap, and internal scrolling.
- Interactive 5+ alert smoke test unavailable because the in-app browser surface was unavailable; no production account was accessed.

Claude Code active files: none. Codex active files: none.

### 2026-06-20 - Claude Code: Close remaining Groww data leak — ChatService, AISignalService, GrowwDataBlocked

Files changed:

- `backend/src/OptionsEdge.API/Features/Chat/ChatService.cs` — `GrowwCredentialService` injected; `BuildSystemPromptAsync` checks per-user Groww connection
- `backend/src/OptionsEdge.API/Features/Signals/AISignalService.cs` — `GrowwCredentialService` injected; blocks signal generation when no user Groww connection
- `frontend/src/components/groww/GrowwDataBlocked.tsx` — replaced dead `/settings/security` link with sidebar instruction text
- `docs/AI_HANDOFF.md`

**Gaps closed:**

`ChatService.BuildSystemPromptAsync` read `marketData.GetSnapshot()` / `indicatorService.GetIndicators()` from shared Groww singleton cache without checking user's own connection. Fix: when `Groww:Enabled = true` and user has no credentials, market context block in the system prompt is replaced with `"NOT AVAILABLE — user has not connected a Groww account."` Chat still answers general questions and references user's own positions — AI knows it lacks live data. `Groww:Enabled = false` (dev/mock): check skipped, behavior unchanged.

`AISignalService.GenerateEntrySignalAsync` read `marketData.GetSnapshot()` from the same shared cache before the AI key check. Fix: Groww check placed immediately after market-hours gate, same hard-block pattern as "no API key":
```
"No Groww account connected. Connect your Groww account in Settings to generate live signals."
```

**GrowwDataBlocked link fix:** Codex's implementation linked to `/settings/security` which has no Groww UI. The `GrowwStatusModal` is local state in Sidebar/Header — not globally accessible. Replaced the button with instruction text pointing to the sidebar Groww button.

**Backtest: intentionally NOT gated.** Reads historical public index candles — same for all users, no per-user account data, no privacy risk. Manu can add the gate for strict consistency if desired.

Tests: `dotnet build` 0 warnings. `dotnet test` 49 passed. `npm run build` 0 errors.

Claude Code active files: none. Codex active files: none.

### 2026-06-20 - Codex: Frontend Groww market-data gate and after-hours position badge

Files changed:

- `frontend/src/types/index.ts`
- `frontend/src/services/api.ts`
- `frontend/src/store/appStore.ts`
- `frontend/src/hooks/useMarketData.ts`
- `frontend/src/hooks/useSignalR.ts`
- `frontend/src/components/groww/GrowwDataBlocked.tsx` (new)
- `frontend/src/pages/Dashboard/index.tsx`
- `frontend/src/pages/Chain/index.tsx`
- `frontend/src/pages/Backtest/index.tsx`
- `frontend/src/components/positions/PositionCard.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- Added frontend `GrowwGatedResponse<T>` and updated all six gated API methods to match backend `{ isGrowwConnected, data }` responses.
- Dashboard now waits for access confirmation and renders no snapshots, candles, indicators, pivots, Market Pulse, charts, or signal controls when disconnected. Replaced the old decorative/dismissible Groww banner with a centered blocked state linking to `/settings/security`.
- Chain unwraps the gated response, clears prior chain data before each request, disables symbol/expiry controls until access is confirmed, and renders no metrics, OI chart, chain table, or Strategy Builder when disconnected.
- Backtest trade-chart candle fetch now checks the wrapper and shows the same blocked state instead of rendering candle data when disconnected.
- Store tracks authoritative market-data access. A false gate or disconnected per-user Groww status clears cached snapshots/indicators; logout also clears them. Reconnection resets access for a fresh fetch.
- SignalR price and indicator updates are discarded until REST access is confirmed. Indicator refresh responses also re-check the gate before writing to store.
- Added required `isAfterHoursEntry` to frontend `Position` type. Position cards now show a persistent neutral `Entered after hours` badge beside status when true.

Validation:

- `npm run build` — passed (`tsc -b` and Vite), zero errors.
- `git diff --check` — passed.
- Source audit found all direct `marketApi`, `indicatorsApi`, and gated `optionsApi` consumers and confirmed wrapper checks at each caller.
- Interactive disconnected-account/weekend-position smoke test unavailable because the in-app browser surface was unavailable. Backend migration remains pending per the preceding Claude entry until PostgreSQL is reachable.

Claude Code active files: none. Codex active files: none.

### 2026-06-20 - Claude Code: Per-user Groww gating on all market data endpoints; IsAfterHoursEntry on positions

Files changed:

- `backend/src/OptionsEdge.API/Features/Market/Models.cs` — added `GrowwGatedResponse<T>` record
- `backend/src/OptionsEdge.API/Features/Market/MarketEndpoints.cs` — `/snapshot`, `/snapshot/{symbol}`, `/candles/{symbol}` now gated
- `backend/src/OptionsEdge.API/Features/Indicators/IndicatorEndpoints.cs` — `/indicators/{symbol}` now gated
- `backend/src/OptionsEdge.API/Features/Options/OptionsEndpoints.cs` — `/chain/{symbol}`, `/maxpain/{symbol}` now gated
- `backend/src/OptionsEdge.API/Domain/Entities/Position.cs` — added `IsAfterHoursEntry` property
- `backend/src/OptionsEdge.API/Features/Positions/Models.cs` — added `IsAfterHoursEntry` to `PositionResponse`
- `backend/src/OptionsEdge.API/Features/Positions/PositionEndpoints.cs` — sets `IsAfterHoursEntry` on create, returns in response
- `backend/src/OptionsEdge.API/Infrastructure/Data/Migrations/20260620063027_AddAfterHoursEntryFlag.cs` (new)
- `docs/AI_HANDOFF.md`

**Bug fixed (confirmed Manu):** Any user without their own Groww connection was silently receiving another user's live market data from the shared `GrowwMarketDataService` singleton cache. Now blocked per-user.

**Per-user Groww gating — PATTERN (all future Groww-backed endpoints must follow this):**

Every endpoint that reads from `IMarketDataService` / `GrowwMarketDataService` / `OptionsService` (singleton shared cache) MUST gate per-user when `Groww:Enabled = true`:

```csharp
if (config.GetValue<bool>("Groww:Enabled"))
{
    var userId = ctx.GetUserId(config);
    if (!await credentialSvc.HasCredentialsAsync(userId, ct))
        return Results.Ok(new GrowwGatedResponse<T>(false, false, null));
}
return Results.Ok(new GrowwGatedResponse<T>(true, data is not null, data));
```

`GrowwCredentialService` is registered Scoped — inject directly in endpoint lambdas.

**Response shape change — BREAKING for Codex (frontend must update all callers):**

All gated endpoints now return `GrowwGatedResponse<T> { isGrowwConnected: bool, isDataFresh: bool, data: T? }` instead of raw data. When `Groww:Enabled = false` (dev/mock mode), `isGrowwConnected` and `isDataFresh` are always `true` (mock data always available).

Endpoints changed (before → after):

| Endpoint | Before | After |
|---|---|---|
| `GET /api/v1/market/snapshot` | `MarketSnapshotResponse[]` | `{ isGrowwConnected, isDataFresh, data: MarketSnapshotResponse[] \| null }` |
| `GET /api/v1/market/snapshot/{symbol}` | `MarketSnapshotResponse` | `{ isGrowwConnected, isDataFresh, data: MarketSnapshotResponse \| null }` |
| `GET /api/v1/market/candles/{symbol}` | `CandleResponse[]` | `{ isGrowwConnected, isDataFresh, data: CandleResponse[] \| null }` |
| `GET /api/v1/indicators/{symbol}` | `IndicatorsResponse` | `{ isGrowwConnected, isDataFresh, data: IndicatorsResponse \| null }` |
| `GET /api/v1/options/chain/{symbol}` | `OptionsChainResponse` | `{ isGrowwConnected, isDataFresh, data: OptionsChainResponse \| null }` |
| `GET /api/v1/options/maxpain/{symbol}` | `MaxPainResponse` | `{ isGrowwConnected, isDataFresh, data: MaxPainResponse \| null }` |

**NOT gated** (computed/non-Groww): `GET /market/status`, `GET /options/expiries/{symbol}`, `POST /options/payoff`.

**Codex frontend must:**
1. Update types for all 6 gated endpoints to `GrowwGatedResponse<T>` wrappers.
2. All callers check `isGrowwConnected` and `isDataFresh` before using `.data`.
3. When `isGrowwConnected = false`: show a prominent "Connect your Groww account in Settings to see live market data" message and disable market-data-dependent actions. No partial display, no anonymized fallback.
4. When `isDataFresh = false`: show amber degraded state and do not render live market values, options rows, or alerts from missing data.

**IsAfterHoursEntry — position flag:**

- New `bool` column on `Positions` table (defaultValue: false for existing rows).
- Set to `!MarketHoursHelper.IsMarketOpen()` at create time.
- Persisted: frontend shows a permanent badge/notice on after-hours positions — not just a one-time toast.
- Migration: `20260620063027_AddAfterHoursEntryFlag`. Manu must run `dotnet ef database update` when DB is available.

**Codex frontend must:**
1. Add `isAfterHoursEntry: boolean` to the `PositionResponse` type.
2. On position cards where `isAfterHoursEntry = true`, show a visible label (e.g., yellow badge "After-hours entry") so Manu can identify those positions at a glance.

Tests:

- `dotnet build` — 0 warnings, 0 errors.
- `dotnet test` — 49 passed.
- Migration generated cleanly; DB update pending (ManusMac.local:5432 unreachable from this session — apply with `dotnet ef database update` from the dev machine).

Caveats:

- `OptionsChainResponse` data from `OptionsService.GetChain()` may still reflect a previous connected user's cached chain until the singleton TTL expires (30s during market hours, 5min otherwise). The gate prevents non-connected users from *seeing* another user's data, but the singleton cache itself remains shared — this is inherent to the singleton design and acceptable given the TTL.
- `GET /market/status` intentionally NOT gated — open/close status is generic, not user-specific data.

Claude Code active files: none. Codex active files: all 6 gated frontend callers + `PositionResponse` type + position card UI.

### 2026-06-20 - Codex: Position edit prefill, API-backed expiries, and status tabs

Files changed:

- `frontend/src/components/positions/AddPositionModal.tsx`
- `frontend/src/pages/Positions/index.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- Edit SL/Target now passes the selected `Position` into `AddPositionModal`. Position-specific field mapping uses `entryPrice` and `quantity`; signal prefill still uses `entryHigh` and one lot.
- Added explicit add/edit modal mode. Edit mode labels the dialog and submit action appropriately, prefills every field, and visibly disables Symbol, Type, Strike, Expiry, Entry Price, and Quantity. Only Stop Loss, Target 1, and Target 2 remain editable.
- Deleted the frontend Thursday-based `EXPIRY_OPTIONS` generator. Modal now reuses `optionsApi.getExpiries(symbol)`, fetches whenever opened or Symbol changes, selects the first backend expiry for new positions, and shows disabled loading/error/empty states rather than guessing dates.
- Positions now use Active/Closed pill tabs matching the Chain symbol-toggle style, with live counts and per-tab empty states.
- Closed positions preserve the backend newest-first order and paginate client-side at 20 items using the existing history Prev/Next pattern. Active positions remain unpaginated.

Validation:

- `npm run build` — passed (`tsc -b` and Vite), zero errors.
- `git diff --check` — passed.
- Static search confirms no `EXPIRY_OPTIONS`, `getDay() === 4`, or edit `prefill={null}` remains in `frontend/src`.
- `npm run lint` could not run because the repository uses ESLint 10 but has no `eslint.config.js`/`.mjs`/`.cjs`; this is pre-existing project configuration, not a lint finding.
- Interactive browser smoke test unavailable because the in-app browser surface was unavailable. Local backend/API smoke test also unavailable because startup timed out connecting to PostgreSQL at `ManusMac.local:5432`.

Claude Code active files: none. Codex active files: none.

### 2026-06-19 - Codex: Comprehensive end-user functional guide

Files changed:

- `docs/USER_GUIDE.md` (new)
- `docs/OptionsEdge_User_Guide.pdf` (new, generated from the Markdown guide)
- `docs/AI_HANDOFF.md`

Documentation:

- Added a non-technical, source-verified user guide covering account onboarding, navigation, Anthropic and Groww setup, Dashboard, indicators, AI signals and history, positions and all seven alert conditions, Notification History, Options Chain, Strategy Builder, Backtest, AI Chat, Security, usage, auto signals, troubleshooting, glossary, and daily safety checklist.
- Expanded guide for first-time traders with foundational explanations of indexes, options, CE/PE, strike, expiry, premium, lots, ATM/ITM/OTM, orders versus positions, P&L, trade plans, and risk management.
- Added purpose, value, beginner examples, and a 30-second explanation to each trading module, plus a connected end-to-end trade journey and presenter guide for explaining/demoing OptionsEdge to others.
- Clearly separates live, simulated, cached, and estimated data and documents current limits such as market-hours gating, pagination sizes, rate limits, alert cadence/deduplication, backtest retention, and expiry rules.
- Calls out important current UI/data-model caveats: manual position picker still offers Thursday expiries, Edit SL/Target opens an unprefilled general form, Strategy Builder `Track This` loses BUY/SELL direction, local position close does not close Groww trades, live order tracking uses requested signal price rather than confirmed fill, and historical option premiums remain estimated.
- Generated a polished 50-page A4 PDF with a cover, linked contents, print-friendly typography, running headers, footers, and page numbers. `USER_GUIDE.md` remains the maintained source for future revisions and PDF regeneration.

Validation:

- Cross-checked guide against current frontend routes/pages/components and backend endpoints/services/workers.
- Verified the generated PDF contains 50 pages and extractable text, then visually inspected its cover, contents, representative beginner/interior pages, and final checklist page for clipping and layout issues.
- `git diff --check` passed.

Claude Code active files: none. Codex active files: none.

### 2026-06-19 - Claude Code: AI signal strike guardrail + real premium data in prompt (Anju 18/06/26 report)

Files changed:

- `backend/src/OptionsEdge.API/Features/Signals/AISignalService.cs`
- `backend/src/OptionsEdge.API/InternalsVisibleTo.cs` (new — exposes `internal` helpers to test project)
- `backend/tests/OptionsEdge.API.Tests/AISignalStrikeTests.cs` (new — 8 tests)
- `docs/AI_HANDOFF.md`

Bug (confirmed real trader report, Anju, 2026-06-18):

AI-generated signal picked 23600 CE (deep ITM, real premium ~400-500) but gave entry 110-130, which only makes sense for a near-ATM strike. Root cause: prompt gave only the ATM strike value — no actual per-strike premium data, so AI had to guess entry prices. No post-parse validation existed to catch a wildly wrong strike.

Three-fix solution:

**Fix A — Hard guardrail (`IsStrikeWithinBounds`):**
After parsing the AI's JSON output, `GenerateEntrySignalAsync` now calls `IsStrikeWithinBounds(aiOutput.Strike, snapshot.Ltp, symbol)`. If the strike is more than 3 steps from ATM (±150 NIFTY, ±300 BANKNIFTY), signal is rejected with a `"Please try generating again"` error instead of being saved and shown. Logged at `LogWarning` with strike, ATM, and steps count. Threshold: 3 steps is a judgment call — **Manu should raise/lower this if legitimate strategy calls get blocked.**

**Fix B — Real per-strike premiums in prompt (`BuildNearbyStrikesTable`):**
Before calling Claude, `GenerateEntrySignalAsync` now calls `optionsService.GetChain(symbol, expiries[0])` and builds a compact ATM ±5 strike table with CE/PE premium (Ltp) and OI. Injected into `BuildSignalPrompt` as `nearbyStrikesTable`. Fails gracefully: any exception during chain fetch logs `LogWarning` and falls back to ATM-only context (old behaviour). System prompt updated to require: `"entryLow/entryHigh MUST be consistent with the live CE/PE Premium shown in the nearby-strikes table above"`.

**Fix C — OI-weighted strike preference in system prompt:**
System prompt now instructs: prefer ATM or 1-2 nearest strikes, but weight toward the highest-OI nearby strike — high-OI strikes tend to see faster premium movement on directional confirmation. Hard backstop remains Fix A; Fix C only guides the AI within the allowed bounds.

**Threshold note for future signal-quality issues:**
Per Anju's report: always check real trader feedback before assuming "it's just AI being AI". Check the 3-step guardrail first if a signal is rejected — may need widening for deliberate ITM/OTM strategies. Document in this file when threshold is changed.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — 0 warnings, 0 errors.
- `dotnet test` — 49 passed (8 new: `IsStrikeWithinBounds` edge cases for NIFTY/BANKNIFTY, `BuildNearbyStrikesTable` table format/window/OI formatting/empty-graceful-fallback).

Caveats:

- Chain fetch in `GenerateEntrySignalAsync` calls `optionsService.GetChain()` which internally calls `marketData.GetSnapshot()` — minor duplicate snapshot read vs the one already fetched earlier in the method. Not worth optimizing for one call.
- `InternalsVisibleTo.cs` added so test project can call `internal static` helpers directly without making them `public`.

Claude Code active files: none. Codex active files: none.

### 2026-06-19 - Claude Code: Alert dedup now DB-backed — survives process restarts

Files changed:

- `backend/src/OptionsEdge.API/Infrastructure/Data/AppDbContext.cs` — added composite index `(PositionId, AlertType, CreatedAt)` on `Alert` entity
- `backend/src/OptionsEdge.API/Infrastructure/Data/Migrations/20260619125616_AddAlertDedupIndex.cs` (new) — drops old `IX_Alerts_PositionId` single-column FK index, creates `IX_Alerts_PositionId_AlertType_CreatedAt`
- `backend/src/OptionsEdge.API/Infrastructure/Background/PositionMonitorWorker.cs` — `ProcessPositionAsync` now accepts `AppDbContext db`; DB query runs after `IMemoryCache` fast-path miss, before setting the cache entry
- `backend/tests/OptionsEdge.API.Tests/AlertDedupTests.cs` (new — 4 tests)
- `docs/AI_HANDOFF.md`

Behavior:

- **Root cause fixed**: `PositionMonitorWorker` alert dedup was `IMemoryCache`-only (process memory). Any process restart (deploy, App Service recycle, crash-recovery) wiped the cache, letting the same alert re-fire if the triggering market condition persisted.
- **Fix**: before setting the in-memory dedup key, `ProcessPositionAsync` now runs:
  ```csharp
  bool recentDuplicate = await db.Alerts
      .Where(a => a.PositionId == position.Id
               && a.AlertType == trigger.AlertType
               && a.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-15))
      .AnyAsync(ct);
  if (recentDuplicate) continue;
  ```
- **Layered dedup**: `IMemoryCache` check runs first (fast, no DB hit for alerts already seen in this process lifetime). DB query runs only on cache miss (first tick after restart, or after the 15-min TTL expires). DB is authoritative; memory is a fast pre-filter.
- **Index**: `IX_Alerts_PositionId_AlertType_CreatedAt` covers the three WHERE columns exactly — sub-millisecond lookup for any realistic Alerts table size.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — 0 warnings, 0 errors.
- `dotnet test` — 41 passed (4 new: within-window duplicate detected, older-than-window allows re-fire, no prior alert allows fire, different AlertType not suppressed).
- Migration `AddAlertDedupIndex` applied to dev DB cleanly.

Caveats:

- Each alert check that gets past the memory cache now incurs one DB query. For a typical 5–20 active positions, this is negligible; the composite index keeps it fast.
- `DateTimeOffset.UtcNow.AddMinutes(-15)` inside EF LINQ: EF Core evaluates this as a client-side constant before building the SQL, so it translates correctly to a parameterized query.

Claude Code active files: none. Codex active files: none.

### 2026-06-17 - Claude Code: Weekly consistency check job — DB-persisted findings, email with Markdown attachment

Files changed:

- `backend/src/OptionsEdge.API/Domain/Entities/ConsistencyCheckRun.cs` (new)
- `backend/src/OptionsEdge.API/Domain/Entities/ConsistencyFinding.cs` (new)
- `backend/src/OptionsEdge.API/Infrastructure/Data/AppDbContext.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Data/Migrations/AddConsistencyCheckTables` (new)
- `backend/src/OptionsEdge.API/Common/Constants/AppConstants.cs` — added `IndicatorThresholds` nested class
- `backend/src/OptionsEdge.API/Common/Constants/DocumentedThresholds.json` (new — Manu-maintained reference)
- `backend/src/OptionsEdge.API/Common/Options/LotSizeOptions.cs` — added `LastReviewedUtc?: string`
- `backend/src/OptionsEdge.API/Features/Indicators/IndicatorService.cs` — uses `AppConstants.IndicatorThresholds.*` constants
- `backend/src/OptionsEdge.API/Infrastructure/Email/IEmailService.cs` — added `SendWeeklyConsistencyReportAsync`
- `backend/src/OptionsEdge.API/Infrastructure/Email/EmailService.cs` — Markdown file attachment via `BodyBuilder.Attachments.AddAsync`
- `backend/src/OptionsEdge.API/Infrastructure/Email/DevEmailService.cs` — logs Markdown to console
- `backend/src/OptionsEdge.API/Infrastructure/Background/ConsistencyReportMarkdownBuilder.cs` (new)
- `backend/src/OptionsEdge.API/Infrastructure/Background/WeeklyConsistencyCheckWorker.cs` (new)
- `backend/src/OptionsEdge.API/Program.cs` — registered `WeeklyConsistencyCheckWorker`
- `backend/src/OptionsEdge.API/appsettings.json` — added `Ops:AlertEmail`, `ExpiryRules:LastReviewedUtc`, `LotSizes:LastReviewedUtc`
- `backend/src/OptionsEdge.API/appsettings.Development.json` — same keys with dev values
- `backend/tests/OptionsEdge.API.Tests/WeeklyConsistencyCheckTests.cs` (new — 4 tests)
- `docs/AI_HANDOFF.md`

Behavior:

- `WeeklyConsistencyCheckWorker` (BackgroundService, weekly cadence) runs 4 checks and persists every run to `ConsistencyCheckRuns` / `ConsistencyFindings` tables BEFORE attempting email. Email failure never loses run data. `EmailSent` flag set true only after successful send.
  1. **Groww symbol resolution** (automatic): downloads `growwapi-assets.groww.in/instruments/instrument.csv`, verifies INDIAVIX/NIFTY/BANKNIFTY present.
  2. **Lot size staleness** (90-day reminder): reads `LotSizes:LastReviewedUtc` config. NEEDS_REVIEW if > 90 days or unset. **Manu must manually update this date after each NSE lot-size verification.**
  3. **Expiry rule staleness** (90-day reminder): reads `ExpiryRules:LastReviewedUtc`. Same pattern. **This is a reminder, not live NSE API verification.**
  4. **Indicator threshold drift** (automatic): compares `AppConstants.IndicatorThresholds.*` constants against `Common/Constants/DocumentedThresholds.json`. Flags drift as NEEDS_REVIEW.
- `ConsistencyReportMarkdownBuilder.Build()`: NEEDS_REVIEW/CHECK_FAILED findings listed first; ends with "paste to Claude Code/Codex" instruction.
- Email: short HTML summary body, full `.md` report as attachment. Subject: `"— {N} item(s) need review"` or `"— All clear"`.
- Email target: `config["Ops:AlertEmail"]` only. Dev default: `manumohankvr@gmail.com`.

**Manu must wire before first production deploy:**

- GitHub Actions secret `OPS_ALERT_EMAIL` → inject as `Ops__AlertEmail` in `deploy-optionsedge.yml` appsettings generation (same pattern as SMTP secrets). Do NOT touch workflow YAML in Claude Code/Codex tasks.
- Set `LotSizes:LastReviewedUtc` and `ExpiryRules:LastReviewedUtc` in production secrets after manually verifying current NSE rules.

DB queries (no admin UI needed):

```sql
SELECT * FROM "ConsistencyCheckRuns" ORDER BY "RunAtUtc" DESC LIMIT 5;
SELECT * FROM "ConsistencyFindings" WHERE "ConsistencyCheckRunId" = '<run_id>';
```

Tests:

- `dotnet build` — 0 warnings, 0 errors.
- `dotnet test` — 37 passed (3 new: markdown shape, priority ordering, temp file creation).
- Migration `AddConsistencyCheckTables` applied to dev DB cleanly.

Claude Code active files: none. Codex active files: none.

### 2026-06-17 - Codex: Notification History page and paginated alerts frontend support

Files changed:

- `frontend/src/services/api.ts`
- `frontend/src/types/index.ts`
- `frontend/src/hooks/useAlerts.ts`
- `frontend/src/pages/NotificationHistory/index.tsx`
- `frontend/src/App.tsx`
- `frontend/src/components/layout/Sidebar.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- Frontend `alertsApi.getAlerts` now matches the backend paginated response shape: `{ items, page, pageSize, total }`.
- `useAlerts` now unwraps `res.items` for both the initial banner/store load and the disconnected SignalR polling fallback; it no longer passes a bare array assumption through the app.
- `AlertBanner` was verified as store-only for alert rendering and did not need direct API changes.
- Added `/notifications` route with `NotificationHistory`, matching the Signal History dark-card layout and Prev/Next pagination pattern.
- Notification History supports All / Unread only filtering, page size 20, severity badges, read/unread state, optional position labels from the current positions store, and `Mark all read`.
- Sidebar now includes a Notifications nav item near Positions, with an unread-count badge sourced from the existing alert store.

Tests:

- `rg -n "getAlerts\\(|limit:" frontend/src --glob "*.ts" --glob "*.tsx"` shows only paginated `getAlerts` calls; no old `limit` call sites remain.
- `npm run build` in `frontend/` passed.

Caveats:

- Real live alert trigger / read-state reload verification was not completed in this turn.
- Local Vite server started successfully, but in-app Browser smoke test was blocked because the `iab` browser surface was unavailable.

Claude Code active files: none. Codex active files: none.

### 2026-06-17 - Claude Code: BREAKING — GET /api/v1/alerts paginated (page/pageSize/total), bare-array response removed

Files changed:

- `backend/src/OptionsEdge.API/Features/Positions/PositionEndpoints.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- `GET /api/v1/alerts` now accepts `?page=1&pageSize=20&unread=true|false` query params.
- `page` clamped ≥ 1; `pageSize` clamped 1–100.
- Response shape changed from a **bare array** to a wrapped object:
  ```json
  { "items": [...], "page": 1, "pageSize": 20, "total": 47 }
  ```
- Returns descending `CreatedAt` order, same as before.
- `?limit=` param removed; use `pageSize` instead.

**⚠ BREAKING — CODEX MUST UPDATE ALL THREE CALLERS TOGETHER:**

| Caller | File | What to change |
|---|---|---|
| `useAlerts` hook | `frontend/src/hooks/useAlerts.ts` | Read `response.data.items` not `response.data` for REST poll |
| `AlertBanner` component | `frontend/src/components/alerts/AlertBanner.tsx` (or similar path) | Same — consumes `useAlerts`, verify it handles the new shape |
| `NotificationHistory` page | `frontend/src/pages/NotificationHistory/index.tsx` (new, Codex builds this) | Use `items`, `page`, `pageSize`, `total` for pagination UI |

Do NOT update only the new history page and leave `useAlerts`/`AlertBanner` on the old bare-array shape — the banner will throw on deploy.

Pattern mirrors `GET /api/v1/signals/history` (`SignalHistoryResponse`): same field names, same clamp logic.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — 0 warnings, 0 errors.

Claude Code active files: none. Codex active files: `useAlerts.ts`, `AlertBanner.tsx`, new `NotificationHistory` page (all three must ship together).

### 2026-06-17 - Codex: useAlerts now shares the single MarketHub SignalR connection

Files changed:

- `frontend/src/hooks/useSignalR.ts`
- `frontend/src/hooks/useAlerts.ts`
- `docs/AI_HANDOFF.md`

Behavior:

- `useAlerts` no longer creates its own `HubConnectionBuilder` connection to MarketHub.
- `useSignalR` now registers the `newalert` client handler on the shared MarketHub connection alongside `priceupdate`, `marketstatus`, `indicatorupdate`, `newsignal`, and `autosignalgenerated`.
- `useAlerts` reuses `useSignalR(hubUrl)`, invokes `SubscribeToAlerts` when the shared connection reaches `connected`, and keeps the 30-second REST polling fallback only while SignalR is disconnected.
- This prevents the old second alert-only connection from receiving server broadcasts like `MarketStatus` without handlers, which caused production console warnings: `No client method with the name 'marketstatus' found`.

Tests:

- `grep -rn "HubConnectionBuilder" frontend/src --include="*.ts" --include="*.tsx"` now returns only `frontend/src/hooks/useSignalR.ts`.
- `npm run build` in `frontend/` passed.

Notes:

- Future alert changes must keep alerts on the shared `useSignalR` connection. Do not reintroduce a second `HubConnectionBuilder` for the same MarketHub URL.

Claude Code active files: none. Codex active files: none.

### 2026-06-16 - Codex: Suppressed fake fatal startup logs during EF design-time host aborts

Files changed:

- `backend/src/OptionsEdge.API/Program.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- Startup now catches `HostAbortedException` separately and does not log it as fatal.
- This removes false `FTL` noise during EF Core design-time operations such as:
  - `dotnet ef migrations bundle`
  - generated `efbundle` execution
- Real unexpected startup exceptions still go through `Log.Fatal(...)`.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — 0 warnings, 0 errors.
- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj --no-build` — 33 passed.

Notes:

- Pipeline migration behavior was not broken before; only fatal log classification was wrong.

Claude Code active files: none. Codex active files: none.

### 2026-06-16 - Codex: Backend log timestamps and cleanup schedule forced to IST

Files changed:

- `backend/src/OptionsEdge.API/Common/Time/IndiaTime.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Logging/IstTimestampEnricher.cs`
- `backend/src/OptionsEdge.API/Program.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Logging/LogFileMaintenanceService.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Logging/LogFileCleanupWorker.cs`
- `backend/tests/OptionsEdge.API.Tests/LogFileMaintenanceServiceTests.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- Serilog console/file output now writes `IstTimestamp` in `Asia/Kolkata`, independent of Ubuntu server local timezone.
- Log cleanup scheduling now calculates next run from UTC -> IST and executes at IST midnight, not server-local midnight.
- Added shared `IndiaTime` helper for deterministic IST conversion inside app code.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — 0 warnings, 0 errors.
- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj --no-build` — 33 passed.

Notes:

- Log event text now shows IST timestamps directly for easier production issue analysis.
- Serilog file rolling still uses sink day boundaries; visible timestamps are IST, cleanup schedule is IST.

Claude Code active files: none. Codex active files: none.

### 2026-06-16 - Codex: Serilog backend logging, global exception handling, daily log cleanup

Files changed:

- `backend/src/OptionsEdge.API/OptionsEdge.API.csproj`
- `backend/src/OptionsEdge.API/Program.cs`
- `backend/src/OptionsEdge.API/appsettings.json`
- `backend/src/OptionsEdge.API/Common/Options/LogFileOptions.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Logging/LogFilePathResolver.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Logging/LogFileMaintenanceService.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Logging/LogFileCleanupWorker.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Middleware/GlobalExceptionMiddleware.cs`
- `backend/tests/OptionsEdge.API.Tests/LogFileMaintenanceServiceTests.cs`
- `.github/workflows/deploy-optionsedge.yml`
- `docs/AI_HANDOFF.md`

Behavior:

- Backend now uses Serilog for structured logging with:
  - console sink
  - daily rolling file sink
  - request logging via `UseSerilogRequestLogging`
  - log enrichment for host, scheme, path, traceId, userId, remote IP
- Added config-driven `LogFiles` options:
  - `Directory`
  - `FileNamePrefix`
  - `RetentionDays`
  - `CleanupTimeLocal`
- Added global exception middleware that:
  - logs unhandled exceptions with request context and traceId
  - returns RFC 7807 `ProblemDetails`
  - gracefully handles bad requests / argument errors as `400`
  - treats client-aborted requests separately
- Added `LogFileCleanupWorker`, running once on startup and then daily at local `00:00:00`, deleting log files older than 7 days.

Deployment notes:

- Production workflow now writes `Serilog` + `LogFiles` settings into generated `appsettings.Production.json`.
- Production log path is `${API_DEPLOY_PATH}/logs`.
- Backend deploy `rsync` now excludes `logs/`; otherwise release deploys would delete accumulated runtime log files.
- Workflow explicitly creates `${API_DEPLOY_PATH}/logs` before service restart.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — 0 warnings, 0 errors.
- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj --no-build` — 33 passed.

Notes:

- Current retention policy uses file last-write time, not filename parsing.
- Cleanup schedule uses server-local time. On Ubuntu production, daily cleanup runs at server local midnight unless config changes.

Claude Code active files: none. Codex active files: none.

### 2026-06-16 - Codex: Production deploy workflow now writes lot sizes into appsettings.Production.json

Files changed:

- `.github/workflows/deploy-optionsedge.yml`
- `docs/AI_HANDOFF.md`

Behavior:

- GitHub Actions deploy workflow now includes:
  - `LotSizes.NIFTY = 65`
  - `LotSizes.BANKNIFTY = 30`
- This keeps production `appsettings.Production.json` aligned with backend config-driven lot sizes, so deployed pipeline output does not silently fall back to stale pre-Jan-2026 values.

Tests:

- Not run. YAML-only change.

Notes:

- Future NSE/SEBI lot-size revisions must update all config-generation surfaces together: `appsettings.json`, local `appsettings.Development.json`, and `.github/workflows/deploy-optionsedge.yml`.

Claude Code active files: none. Codex active files: none.

### 2026-06-16 - Codex: CRITICAL lot-size fix — NIFTY/BANKNIFTY now config-driven, not hardcoded

Files changed:

- `backend/src/OptionsEdge.API/Common/Constants/AppConstants.cs`
- `backend/src/OptionsEdge.API/Common/Options/LotSizeOptions.cs`
- `backend/src/OptionsEdge.API/Features/Config/ConfigEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Positions/PositionService.cs`
- `backend/src/OptionsEdge.API/Features/Groww/GrowwOrderService.cs`
- `backend/src/OptionsEdge.API/Features/Options/OptionsService.cs`
- `backend/src/OptionsEdge.API/Features/Backtest/BacktestService.cs`
- `backend/src/OptionsEdge.API/Program.cs`
- `backend/src/OptionsEdge.API/appsettings.json`
- `backend/src/OptionsEdge.API/appsettings.Development.json`
- `frontend/src/hooks/useLotSizes.ts`
- `frontend/src/services/api.ts`
- `frontend/src/types/index.ts`
- `frontend/src/components/groww/OrderConfirmModal.tsx`
- `frontend/src/components/strategy/StrategyBuilder.tsx`
- `backend/tests/OptionsEdge.API.Tests/TestOptionsMonitor.cs`
- `backend/tests/OptionsEdge.API.Tests/PositionAlertConditionTests.cs`
- `backend/tests/OptionsEdge.API.Tests/PositionLotSizeTests.cs`
- `backend/tests/OptionsEdge.API.Tests/LotSizeConfigurationReloadTests.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- Removed stale compile-time lot-size constants (`NIFTY 75`, `BANKNIFTY 35`) from `AppConstants`. Lot sizes now live in `appsettings.json` / `appsettings.Development.json` under `LotSizes` with current values `NIFTY: 65`, `BANKNIFTY: 30`.
- Added typed `LotSizeOptions` and wired `builder.Services.Configure<LotSizeOptions>(...)` in `Program.cs`.
- `PositionService`, `GrowwOrderService`, `OptionsService`, and `BacktestService` now read lot sizes from `IOptionsMonitor<LotSizeOptions>`, so P&L, order quantity, payoff quantity, Groww import lot parsing, and backtests all use same runtime config source.
- Added `GET /api/v1/config/lot-sizes`, returning uppercase keys (`NIFTY`, `BANKNIFTY`) for frontend consumers.
- Frontend removed hardcoded lot-size maps. `OrderConfirmModal` and `StrategyBuilder` now use shared `useLotSizes` hook backed by `/api/v1/config/lot-sizes`, with cached fetch and loading/error states instead of local constants.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — 0 warnings, 0 errors.
- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj --no-restore` — 31 passed.
- `npm run build` in `frontend/` passed.

Notes:

- `LotSizeConfigurationReloadTests` writes a temporary `appsettings.json`, changes `NIFTY` from `65` to `75`, waits for `IOptionsMonitor` reload, and verifies same `PositionService` instance changes P&L from `650` to `750` without code changes or service recreation.
- Future NSE/SEBI lot-size revisions should change config only; do not reintroduce hardcoded lot-size maps in backend or frontend.

Claude Code active files: none. Codex active files: none.

### 2026-06-16 - Codex: SignalR dev StrictMode negotiation stop fixed with shared connection

Files changed:

- `frontend/src/hooks/useSignalR.ts`
- `docs/AI_HANDOFF.md`

Behavior:

- `useSignalR` now uses a module-level shared MarketHub connection with subscriber counting instead of creating/stopping a new HubConnection per hook mount.
- This prevents React dev StrictMode from doing mount → `connection.start()` → cleanup → `connection.stop()` while SignalR is still negotiating, which produced `Failed to start the connection: Error: The connection was stopped during negotiation.`
- Shared connection stop is delayed by 1s and waits for any pending start promise before stopping, so quick remounts reuse the same connection.
- MarketHub handlers remain registered once before first start, so `marketstatus`/`newsignal` messages have handlers on the live connection.

Tests:

- `npm run build` in `frontend/` passed.

Notes:

- If `No client method with the name 'marketstatus' found` still appears after browser hard refresh, suspect stale Vite bundle/HMR connection. Full page reload should clear old connection.

Claude Code active files: none. Codex active files: none.

### 2026-06-16 - Codex: Console auth/SignalR errors triage and fixes

Files changed:

- `frontend/src/hooks/useSignalR.ts`
- `backend/src/OptionsEdge.API/appsettings.json`
- `docs/AI_HANDOFF.md`

Behavior:

- `useSignalR` now refuses to start a MarketHub connection when there is no in-memory access token. This avoids negotiating with an empty bearer token during the auth bootstrap/logout window, which produced `connection was stopped during negotiation`.
- MarketHub client handlers are now registered with lowercase method names (`marketstatus`, `newsignal`, etc.), matching the method names shown by the SignalR client warning. This should silence `No client method with the name 'marketstatus' found` when messages arrive.
- SignalR cleanup now marks the connection as stopped, clears `connectionRef`, and ignores late async state updates from a connection that was already disposed.
- Backend CORS allowed origins now includes both `http://localhost:5173` and `https://localhost:5173`, so local HTTPS Vite can be used with the new `Secure` refresh cookie.

Console diagnosis:

- `POST /api/v1/auth/refresh` returning 401 means the browser did not send a valid `refresh_token` cookie. This is expected for old sessions created before the HttpOnly-cookie migration; user must log in once to receive the cookie.
- If 401 continues after a fresh login, check browser cookie storage and scheme. The backend intentionally sets `Secure` + `SameSite=Strict`; with frontend on `http://localhost:5173` and API on `https://localhost:5001`, strict secure-cookie behavior can block cookie use in local dev. Use a same-scheme HTTPS frontend or revisit dev-only cookie policy deliberately.

Tests:

- `npm run build` in `frontend/` passed.

Claude Code active files: none. Codex active files: none.

### 2026-06-16 - Codex: Frontend uses HttpOnly refresh cookie and silent session bootstrap

Files changed:

- `frontend/src/services/api.ts`
- `frontend/src/hooks/useAuth.ts`
- `frontend/src/types/index.ts`
- `docs/AI_HANDOFF.md`

Behavior:

- Axios client now uses `withCredentials: true`, so browser sends/receives the backend `refresh_token` HttpOnly cookie automatically.
- Removed frontend JS refresh-token storage and API contract usage:
  - removed `_refreshToken`
  - removed `getRefreshToken`
  - `setTokens` now takes only `accessToken` + optional `accessTokenExpiry`
  - `AuthResponse` no longer has `refreshToken`
  - `authApi.refresh()` sends no body
  - `authApi.logout()` sends no body
- Proactive 80%-lifetime refresh and reactive 401 retry now call cookie-backed `authApi.refresh()` with no JS-readable refresh token.
- App-load auth bootstrap now attempts `authApi.refresh()` before `/auth/me` when no access token exists in memory. This fixes hard reload losing the JS access token while the HttpOnly refresh cookie still exists.
- `/auth/refresh` 401 responses are excluded from the normal 401 retry loop to avoid recursive refresh attempts when the cookie is missing/expired.

Tests:

- `npm run build` in `frontend/` passed.

Caveats:

- Tried to run the local HTTPS API for the requested hard-reload manual test, but `dotnet run --launch-profile https` did not reach the listening state after ~90s and was stopped. Browser Cmd+R verification was not completed in this turn.
- This frontend must deploy together with the backend cookie-auth change from the previous entry.

Claude Code active files: none. Codex active files: none.

### 2026-06-16 - Codex acting for Claude Code: Refresh token moved to HttpOnly cookie

Files changed:

- `backend/src/OptionsEdge.API/Features/Auth/AuthEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Auth/Models.cs`
- `docs/AI_HANDOFF.md`

BREAKING auth contract change:

- Refresh tokens are no longer returned in `AuthResponse` JSON. `AuthResponse` now contains access token data only (`AccessToken`, `AccessTokenExpiry`, user metadata, `TwoFactorEnabled`).
- `POST /api/v1/auth/login` and `POST /api/v1/auth/two-factor` now set `refresh_token` as an HttpOnly cookie instead of exposing it to frontend JS.
- `POST /api/v1/auth/refresh` no longer accepts `RefreshRequest` body. It reads `refresh_token` from `ctx.Request.Cookies`, revokes it, issues a new access token, rotates the refresh token row, and sets a new refresh cookie.
- `POST /api/v1/auth/logout` no longer accepts `LogoutRequest` body. It reads `refresh_token` from the cookie, revokes it for the current user when present, and clears the cookie.
- `RefreshRequest` and `LogoutRequest` DTOs were removed.

Cookie settings:

- Name: `refresh_token`
- `HttpOnly = true`
- `Secure = true`
- `SameSite = Strict`
- `Path = "/api/v1/auth"`
- `Expires` matches the actual stored `RefreshToken.ExpiresAt`, which is generated from `Jwt:RefreshTokenDays` (currently 7 days in appsettings).

Deployment notes:

- Must deploy together with matching frontend changes. Frontend must stop expecting `refreshToken` in `AuthResponse`, must call `/auth/refresh` and `/auth/logout` without body tokens, and must enable credentials on auth/API requests (`withCredentials: true`) so the cookie is sent.
- `Program.cs` CORS already has `.AllowCredentials()` with configured allowed origins, so backend CORS is ready for credentialed cookie requests.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — 0 warnings, 0 errors.

Caveats:

- Secure cookies require HTTPS in real browser use; local HTTP dev may need HTTPS backend/frontend or environment-specific handling.
- Current frontend is expected to break until the matching cookie-based auth client change lands.

Claude Code active files: none. Codex active files: none.

### 2026-06-16 - Codex: Proactive token refresh and SignalR handler consolidation

Files changed:

- `frontend/src/services/api.ts`
- `frontend/src/hooks/useAuth.ts`
- `frontend/src/hooks/useSignalR.ts`
- `frontend/src/hooks/useMarketData.ts`
- `docs/AI_HANDOFF.md`

Behavior:

- Frontend tokens now schedule a proactive refresh when `setTokens` runs. The timer uses `accessTokenExpiry` (falling back to JWT `exp`) and refreshes at ~80% of the token lifetime, so normal API calls and SignalR negotiation should not see stale access tokens.
- Reactive 401 refresh remains as fallback; successful refreshes now reschedule the proactive timer with the new expiry.
- `clearTokens` clears the proactive timer and stored expiry.
- `useAuth` now passes `accessTokenExpiry` from login and two-factor responses into `setTokens`.
- `useSignalR` now owns all MarketHub handlers (`PriceUpdate`, `MarketStatus`, `IndicatorUpdate`, `NewSignal`, `AutoSignalGenerated`) and registers them before `connection.start()`.
- `useSignalR` also subscribes to `NIFTY` and `BANKNIFTY` groups immediately after initial connect and after automatic reconnect.
- `useMarketData` no longer registers handlers on the shared connection; it only performs initial REST fetches and returns the connection state.

Tests:

- `npm run build` in `frontend/` passed.

Caveats:

- The noisy backend warnings `No client method with the name 'marketstatus'` and `No client method with the name 'newsignal'` were treated as race symptoms. They should stop once this frontend is deployed because handlers are attached before start/reconnect, but this was not verified against a live running app in this turn.

Claude Code active files: none. Codex active files: none.

### 2026-06-16 - Claude Code: JWT token lifetime 15→60min, ClockSkew 30s→5min (fixes auth/me 401 + SignalR negotiation loop)

Files changed:

- `backend/src/OptionsEdge.API/appsettings.json`
- `backend/src/OptionsEdge.API/appsettings.Development.json`
- `backend/src/OptionsEdge.API/Program.cs`
- `docs/AI_HANDOFF.md`

Investigation findings:

- `OnMessageReceived` / `JwtBearerEvents` in `Program.cs` was already correctly configured — SignalR query-string token extraction was NOT missing. This was NOT the cause.
- `AccessTokenMinutes` = 15 in both prod and dev configs — this IS the cause. 15-minute tokens expire during an active session. The frontend refreshes reactively (on 401), not proactively. Gap between token expiry and successful refresh = window where every authenticated request 401s, AND SignalR's `accessTokenFactory` returns the same expired token on reconnect → "connection stopped during negotiation" loops at 15-20s intervals (matching SignalR's reconnect backoff).
- `ClockSkew = TimeSpan.FromSeconds(30)` was an unusually tight value (ASP.NET default is 5 min). A server with any NTP drift could reject still-valid tokens early.

Changes:

- `AccessTokenMinutes`: 15 → 60. Users now have a full hour before needing a refresh — covers a normal trading session without hitting the expiry loop.
- `ClockSkew`: `TimeSpan.FromSeconds(30)` → `TimeSpan.FromMinutes(5)`. Standard default; protects against server/client clock drift.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — 0 warnings, 0 errors.

Caveats:

- 60-minute access token is still short enough to limit blast radius if a token is leaked — refresh tokens (7-day) are the real long-lived credential; access tokens are used for API calls and expire.
- Frontend should ideally implement proactive refresh (e.g., refresh when token has < 5 min remaining) as a long-term fix to eliminate any reactive-refresh gaps entirely.

Claude Code active files: none.

### 2026-06-16 - Claude Code: ValidUntil always-expired fix — prompt date injection, safety net, unified response

Files changed:

- `backend/src/OptionsEdge.API/Features/Signals/AISignalService.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- **Root cause**: `SignalSystemPrompt` was a `const string` with a hardcoded example `validUntil` date (`2026-06-06T15:30:00+05:30` — a past date). The AI copied that stale date as its output. Additionally, the user prompt contained no current IST date, so the AI had no anchor for "today". Result: every generated signal was born already expired.
- **Prompt fix**: `SignalSystemPrompt` → `BuildSignalSystemPrompt(DateTime istNow)` method. The example JSON now uses `{istNow:yyyy-MM-dd}T15:30:00+05:30` (today's IST date at runtime). The rule text also explicitly states: `use TODAY's date (YYYY-MM-DD) at 15:30:00+05:30 — do NOT use any other date`.
- **User prompt fix**: `BuildSignalPrompt` now accepts `DateTime istNow` and injects `Current date and time (IST): YYYY-MM-DD HH:mm:ss IST` as the first line — gives the AI an unambiguous anchor for date-relative calculations.
- **Safety net**: `parsedValidUntil` hoisted to a local variable (was inline in entity initializer). If the parsed/normalized value is `<= now`, logs `LogWarning` and overrides to `now.AddHours(4)`. Belt-and-suspenders regardless of AI behavior.
- **Unified response**: Live `SignalResponse.ValidUntil` now uses `parsedValidUntil.ToString("O")` instead of raw `aiOutput.ValidUntil`. Dashboard live card and Signal History page now always agree on expiry, both normalized to UTC ISO 8601.
- **IstZone static field** added to `AISignalService` (same pattern as `OptionsService`).

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — 0 warnings, 0 errors.

Caveats:

- The `$$"""..."""` interpolated raw string literal requires C# 11+; project already targets .NET 10, so this is fine.

Claude Code active files: none.

### 2026-06-16 - Codex: Signal History padding aligned with Chain

Files changed:

- `frontend/src/pages/SignalHistory/index.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- Signal History root wrapper now matches Chain page spacing exactly: `p-4 space-y-4 max-w-6xl mx-auto`.
- Header now uses Chain-style `flex flex-wrap items-center gap-3` layout and `text-lg font-bold` title sizing.
- Empty, error, and loading card states now use `p-4`, matching the SignalHistory card padding and app card spacing.
- Responsive side padding now matches Chain on mobile and desktop.

Tests:

- `npm run build` in `frontend/` passed.

Caveats:

- Visual change only; no API or data-flow changes.

Claude Code active files: none. Codex active files: none.

### 2026-06-16 - Claude Code: CRITICAL — DateTimeOffset UTC normalization (signal save root cause)

Files changed:

- `backend/src/OptionsEdge.API/Features/Signals/AISignalService.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Data/AppDbContext.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- **Root cause confirmed**: `AISignalService` built `Signal.ValidUntil` from `DateTimeOffset.TryParse(aiOutput.ValidUntil)`. Claude returns `ValidUntil` as an IST-offset string (e.g. `"2026-06-16T15:30:00+05:30"`); `TryParse` preserves the `+05:30` offset verbatim. Npgsql rejects any non-UTC `DateTimeOffset` on write to `timestamp with time zone` with `System.ArgumentException: Cannot write DateTimeOffset with Offset=05:30:00 to PostgreSQL type 'timestamp with time zone', only offset 0 (UTC) is supported`. This was the silent failure causing every signal to be shown to user but never saved to DB.
- **Point fix**: `AISignalService.cs:157` — `? vu : now.AddHours(4)` → `? vu.ToUniversalTime() : now.AddHours(4)`. Instant unchanged; only the offset representation normalized to UTC before Npgsql writes it.
- **Audit**: `grep -rn "DateTimeOffset.TryParse|DateTimeOffset.Parse"` found only one occurrence in the entire backend (the one above). No other call sites.
- **Defense-in-depth**: `AppDbContext.OnModelCreating` now iterates all entity properties after existing entity configuration and attaches a `ValueConverter<DateTimeOffset, DateTimeOffset>` (and nullable variant) that calls `.ToUniversalTime()` on every write. Any future `DateTimeOffset` written through EF Core is automatically UTC-normalized, even if the call site forgets.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — 0 warnings, 0 errors.

Caveats:

- The global converter in `AppDbContext` does not affect values read back from the DB (the `v => v` identity on the read side) — `DateTimeOffset` values read from Postgres come back as UTC already (Npgsql behavior), so no change on reads.
- The global converter is applied after all entity-specific configuration, so it does not conflict with any `HasDefaultValueSql("now()")` columns (those are DB-side defaults, not written through the converter).

Claude Code active files: none.

### 2026-06-16 - Codex: Signal history dark-card redesign

Files changed:

- `frontend/src/pages/SignalHistory/index.tsx`
- `frontend/src/types/index.ts`
- `docs/AI_HANDOFF.md`

Behavior:

- Replaced Signal History table with Dashboard AI Signals-style dark cards.
- Each history item now shows signal type badge, Active/Expired badge, contract header, confidence, 3-column Entry/Target/Stop Loss price grid, optional rationale bullets when `rationale` is present, and footer tags for R:R, model, cost, created time, and status.
- Pagination kept in the Backtest saved-run style (`Prev`/`Next`, bordered dark buttons, `start-end of total` count).
- Added optional `rationale?: string[]` to `SignalHistoryItem` so the UI can render rationale if the endpoint includes it later. Current backend still omits rationale by design.

Tests:

- `npm run build` in `frontend/` passed.

Caveats:

- Current `GET /api/v1/signals/history` response does not include rationale, so rationale bullets stay hidden until backend adds that field.

Claude Code active files: none. Codex active files: none.

### 2026-06-16 - Claude Code: VIX symbol corrected, day change reads Groww fields, signal save logs ERROR

Files changed:

- `backend/src/OptionsEdge.API/Features/Groww/GrowwUserApiClient.cs`
- `backend/src/OptionsEdge.API/Features/Signals/AISignalService.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- `GetVixAsync`: confirmed correct trading_symbol from Groww instruments CSV (`growwapi-assets.groww.in/instruments/instrument.csv`) is `INDIAVIX` (no space, no encoding). Previous `INDIA%20VIX` always returned GA001 400. Collapsed two-try fallback structure to single attempt with `INDIAVIX`. Comment records the instrument CSV source.
- `GetSpotSnapshotAsync`: `change` now prefers `day_change` field from Groww response, falls back to computed `ltp - prevClose`. `changePct` prefers `day_change_perc`, falls back to computed. Fixes dashboard showing `0.00 (+0.00%)` when Groww reports change directly.
- `AISignalService.GenerateEntrySignalAsync`: signal-save `catch` upgraded from `LogWarning` (no detail) to `LogError` with `userId`, `symbol`, `strike`, `expiry` — makes silent save failures visible in production logs with enough context to diagnose root cause.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — 0 warnings, 0 errors.

Caveats:

- `day_change_perc` field name unverified against a live Groww quote response — if still 0 after deploy, check actual response JSON via the new warning log and adjust field name.

Codex active files: none.

### 2026-06-16 - Codex: Signal history table page

Files changed:

- `frontend/src/App.tsx`
- `frontend/src/pages/Dashboard/index.tsx`
- `frontend/src/pages/SignalHistory/index.tsx` (new)
- `frontend/src/services/api.ts`
- `frontend/src/types/index.ts`
- `docs/AI_HANDOFF.md`

Behavior:

- Added `/signals/history` route with a paged signal history table.
- Dashboard AI Signals `Full Analysis` action now opens the signal history page.
- `signalsApi.getHistory` now calls `GET /api/v1/signals/history?page=1&pageSize=20` and returns the paged response.
- Added `SignalHistoryItem`/`SignalHistoryResponse` frontend types. Response handling supports current backend fields (`totalItems`, `totalPages`) and the shorthand `total` shape if returned later.
- Signal history table mirrors the Backtest trade-log table shell/classes: dark bordered rounded panel, compact text table, horizontal scroll, row hover, and Prev/Next pagination controls.
- Columns shown: Date/Time, Symbol, Type, Strike, Confidence, Entry, Target, SL, Status. Status is derived client-side from `validUntil` (`Active` when still valid, otherwise `Expired`).

Tests:

- `npm run build` in `frontend/` passed.

Caveats:

- No symbol filter on the page because the new backend history endpoint removed `?symbol`; data is shown newest-first as sorted by backend.

Claude Code active files: none. Codex active files: none.

### 2026-06-16 - Claude Code: Paginated signal history endpoint

Files changed:

- `backend/src/OptionsEdge.API/Features/Signals/Models.cs`
- `backend/src/OptionsEdge.API/Features/Signals/SignalEndpoints.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- `GET /api/v1/signals/history` replaced non-paginated `?symbol&limit` version with `?page=1&pageSize=20` (page clamped ≥ 1, pageSize clamped 1–50).
- Returns `SignalHistoryResponse { Items, Page, PageSize, TotalItems, TotalPages }` — same pattern as `BacktestHistoryResponse`.
- `SignalHistoryItem` is a lighter projection than `SignalResponse`: omits `Rationale`, `InputTokens`, `OutputTokens` — reduces payload for history list.
- Dates materialized in memory (EF Core can't translate `DateOnly.ToString()`); `Expiry` as `yyyy-MM-dd`, `CreatedAt`/`ValidUntil` as ISO 8601 `"O"` format.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — 0 warnings, 0 errors.

Caveats:

- Old `?symbol` filter param removed; Codex signal history page should filter client-side or add a new `?symbol=` query param server-side if needed.

Codex active files: signal history page (pending — build `GET /api/v1/signals/history?page=1&pageSize=20`, render `SignalHistoryResponse`, per-page nav).

### 2026-06-16 - Claude Code: VIX logging, full-chain PCR/MaxPain, expiry includes today

Files changed:

- `backend/src/OptionsEdge.API/Features/Groww/GrowwUserApiClient.cs`
- `backend/src/OptionsEdge.API/Features/Options/OptionsService.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- `GetVixAsync`: silent `catch { return 0m; }` replaced with proper `LogWarning` on both exception and zero-result paths. First attempt uses `INDIA%20VIX`; if that returns 0 or throws, retries with `INDIAVIX` (no space). Raw Groww response body logged on zero-result so production logs show the actual error.
- `OptionsService.GetChain`: display loop widened from ATM ±5 (11 strikes) to ATM ±10 (21 strikes). PCR now computed from the FULL Groww chain (`fullTotalCeOi`/`fullTotalPeOi` summed over all `growwChain` rows before the display loop) — falls back to the displayed-subset totals when Groww chain unavailable. MaxPain also computed from the full Groww chain via new `ComputeMaxPainFromGrowwChain(IReadOnlyList<GrowwOptionChainRow>)` — same algorithm as `ComputeMaxPain(rows)` but iterates Groww OI directly; falls back to displayed rows when chain unavailable.
- `OptionsService.GetExpiries` (NIFTY weekly block): loop now starts at `i = 0` (was `i = 1`). Today included when it is Tuesday AND `now.TimeOfDay < 15:30 IST`; past-close Tuesdays skip to next week.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` — 0 warnings, 0 errors.

Caveats:

- VIX fallback symbol `INDIAVIX` unverified against live Groww response — both attempts are now logged so the correct symbol can be confirmed from production logs.
- `GrowwSymbolHelper.cs` has no `GetNearestNiftyExpiry` method; the expiry fix lives entirely in `OptionsService.GetExpiries`.

Claude Code active files: none.

### 2026-06-15 - Codex: Auto Signal Preferences UI

Files changed:

- `frontend/src/components/common/Toggle.tsx` (new)
- `frontend/src/services/api.ts`
- `frontend/src/types/index.ts`
- `frontend/src/store/appStore.ts`
- `frontend/src/hooks/useSignalR.ts`
- `frontend/src/pages/Auth/SecuritySettings.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- Added reusable `Toggle` switch component.
- Added `SignalPreferenceResponse`/`SignalPreferenceRequest` types and `signalPreferenceApi` for `GET`/`PUT /signals/preferences`.
- Security Settings now shows `Auto Signal Preferences` after AI Connection and before Change Password, with per-index toggles, conditional time inputs, save action, server-normalized reload after save, and success/error state.
- App store now has `addSignal(signal)` for SignalR auto-signal inserts, deduping and capping the signal list at 20.
- `useSignalR` now handles `AutoSignalGenerated`, adds the signal to the store, and shows a browser notification when permission is already granted.

Tests:

- `npm run build` in `frontend/` passed.

Caveats:

- Frontend-only; backend endpoints and worker were implemented by Claude Code.
- UI does not request browser notification permission; it only shows notifications if permission is already `granted`.

Claude Code active files: none currently.

### 2026-06-15 - Claude Code: Auto-signal preferences (backend) — Codex frontend follow-up needed

Files changed:

- `backend/src/OptionsEdge.API/Domain/Entities/UserSignalPreference.cs` (new)
- `backend/src/OptionsEdge.API/Infrastructure/Data/AppDbContext.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Data/Migrations/20260615121505_AddUserSignalPreferences.cs` (new)
- `backend/src/OptionsEdge.API/Infrastructure/Data/Migrations/20260615121505_AddUserSignalPreferences.Designer.cs` (new)
- `backend/src/OptionsEdge.API/Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs`
- `backend/src/OptionsEdge.API/Features/Signals/UserSignalPreferenceService.cs` (new)
- `backend/src/OptionsEdge.API/Features/Signals/SignalEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Signals/Models.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Background/AutoSignalWorker.cs` (new)
- `backend/src/OptionsEdge.API/Program.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- New `UserSignalPreference` entity (`UserSignalPreferences` table, one row per user, unique `UserId`, FK to `AspNetUsers`): `NiftyAutoSignalEnabled`/`NiftyAutoSignalTimes` and `BankNiftyAutoSignalEnabled`/`BankNiftyAutoSignalTimes` (comma-separated IST `HH:mm`, default `"09:30,12:00,14:00"`), plus `CreatedAt`/`UpdatedAt`.
- New `UserSignalPreferenceService` (Scoped):
  - `GetOrCreateAsync(userId)` — lazily creates a default row.
  - `SaveAsync(userId, niftyEnabled, niftyTimes, bankNiftyEnabled, bankNiftyTimes)` — validates/normalizes both time strings via `ValidateTimes`.
  - `ValidateTimes`: parses comma-separated times, keeps only `09:15`-`15:25` inclusive, normalizes to zero-padded `HH:mm`, dedups, sorts, caps at 5, falls back to `"09:30"` if nothing valid remains.
  - `GetDueSignalsAsync()` — called every tick by `AutoSignalWorker`. Returns `(UserId, Symbol)` pairs whose schedule matches the current IST `HH:mm`, gated on `MarketHoursHelper.IsMarketOpen()`.
- New endpoints under `/api/v1/signals` (both `RequireAuthorization`):
  - `GET /api/v1/signals/preferences` → `SignalPreferenceResponse { niftyAutoSignalEnabled, niftyAutoSignalTimes, bankNiftyAutoSignalEnabled, bankNiftyAutoSignalTimes }` (camelCase via default minimal-API JSON options). Auto-creates the row on first call.
  - `PUT /api/v1/signals/preferences` — body `SignalPreferenceRequest` (same shape). Returns `{ "message": "Signal preferences saved." }`. Server-side validation/normalization always applies — client does not need to pre-validate.
- New `AutoSignalWorker` (`BackgroundService`, registered via `AddHostedService`, 60s tick): each tick calls `GetDueSignalsAsync()`, and for each due `(userId, symbol)` calls `AISignalService.GenerateEntrySignalAsync(symbol, userId, ct)`. On success, broadcasts the resulting `SignalResponse` to that user via `IHubContext<MarketHub>.Clients.User(userId).SendAsync("AutoSignalGenerated", signal, ct)`. Errors/no-API-key results are logged and skipped (no broadcast).

Verified end-to-end against the dev DB (server run + curl with a real JWT):

- `GET /api/v1/signals/preferences` (first call, no row yet) → defaults `{"niftyAutoSignalEnabled":false,"niftyAutoSignalTimes":"09:30,12:00,14:00","bankNiftyAutoSignalEnabled":false,"bankNiftyAutoSignalTimes":"09:30,12:00,14:00"}`.
- `PUT` with `niftyAutoSignalEnabled=true`, `niftyAutoSignalTimes="09:20, 25:99, 14:00, 09:20"` → saved as `"09:20,14:00"` (invalid `25:99` dropped, duplicate `09:20` deduped, sorted) — confirms `ValidateTimes`.
- Follow-up `GET` reflects the saved/normalized values.
- Log confirms `AutoSignalWorker started` on app boot.
- Reset the dev user's row back to all-defaults afterward to restore DB state to before testing.

Tests:

- `dotnet build` — 0 warnings, 0 errors.
- `dotnet test` — 27/27 passed.
- `dotnet ef migrations add AddUserSignalPreferences` + `dotnet ef database update` — applied cleanly to dev DB.

Caveats:

- `AutoSignalWorker` calls `AISignalService.GenerateEntrySignalAsync`, which requires the user to have a saved Anthropic key (see `UserAICredential`/2026-06-15 entry above). If missing, the per-user tick is logged as a warning and skipped — no broadcast, no crash.
- Frontend has NOT been built yet — see "Codex active files" below for the full spec (this was originally STEPS 5-7 of the task spec).

### Codex follow-up: Auto Signal Preferences UI (STEPS 5-7 of original spec)

API contract is live now (`GET`/`PUT /api/v1/signals/preferences`, see above) and the SignalR event `"AutoSignalGenerated"` is broadcast on `MarketHub` with a `SignalResponse` payload (camelCase JSON — same shape as `POST /api/v1/signals/generate` response). Needed frontend work:

1. `frontend/src/services/api.ts` — add:

   ```ts
   export const signalPreferenceApi = {
     getPreferences: () => api.get<SignalPreferenceResponse>('/signals/preferences').then(r => r.data),
     savePreferences: (data: SignalPreferenceRequest) => api.put('/signals/preferences', data).then(r => r.data),
   }
   ```

2. `frontend/src/types/index.ts` — add:

   ```ts
   export interface SignalPreferenceResponse {
     niftyAutoSignalEnabled: boolean
     niftyAutoSignalTimes: string
     bankNiftyAutoSignalEnabled: boolean
     bankNiftyAutoSignalTimes: string
   }
   export type SignalPreferenceRequest = SignalPreferenceResponse
   ```

3. `frontend/src/hooks/useSignalR.ts` — add a handler for `"AutoSignalGenerated"` (payload: `SignalResponse`) that calls a new `addAutoSignal` action on `appStore`.
4. `appStore` — new `autoSignals: Signal[]` slice + `addAutoSignal(signal)` that prepends and caps at 10.
5. `frontend/src/pages/Auth/SecuritySettings.tsx` — new "Auto Signal Preferences" `SectionCard`, placed after "AI Connection" and before "Change Password". Per-symbol (NIFTY 50 / BANK NIFTY) toggle + comma-separated `HH:mm` time-input (shown only when enabled), plus a "Save Preferences" button calling `signalPreferenceApi.savePreferences`. Backend already validates/normalizes times server-side (09:15-15:25, max 5, dedup+sort), so the UI can show the server's normalized response after save without its own validation. Note for the BANKNIFTY field copy: BANKNIFTY now has monthly expiry only (see 2026-06-15 expiry-fix entry).
6. `frontend/src/components/common/Toggle.tsx` — small reusable switch component (`{ checked, onChange }`) if one doesn't already exist in the codebase — check first.

Claude Code active files: none (backend done, awaiting Codex for the above).

### 2026-06-15 - Claude Code: NIFTY/BANKNIFTY expiry regulatory fix (Tuesday weekly, BANKNIFTY monthly-only)

Files changed:

- `backend/src/OptionsEdge.API/Infrastructure/Groww/GrowwSymbolHelper.cs`
- `backend/src/OptionsEdge.API/Features/Options/OptionsService.cs`
- `backend/src/OptionsEdge.API/Features/Signals/AISignalService.cs`
- `backend/src/OptionsEdge.API/Features/Backtest/BacktestService.cs`
- `docs/AI_HANDOFF.md`

Regulatory context (verified via web search):

- NSE moved NIFTY weekly options expiry from Thursday to **Tuesday**, effective Sep 1, 2025 (SEBI-mandated spread of weekly expiries across exchanges; first Tuesday expiry was Sep 2, 2025).
- BANKNIFTY weekly options were discontinued in **Nov 2024** (last expiry Nov 13, 2024) — only the monthly contract (last Tuesday of month) remains.

Behavior changes:

- `GrowwSymbolHelper`: renamed private `LastThursdayOfMonth` → public `LastTuesdayOfMonth` (Thursday → Tuesday). Used by `TryParseOptionSymbol` (Groww position import expiry approximation) and now shared by `OptionsService` and `BacktestService` instead of each holding a duplicate copy.
- `OptionsService.GetExpiries(symbol)`: NIFTY returns next 4 weekly Tuesdays + monthly last-Tuesdays (dedup, up to 6 total). BANKNIFTY returns monthly last-Tuesdays only (no weekly block), starting from the current month if not yet passed. This list feeds the AI signal prompt's "Available Expiries" and the options-chain/LTP fallback expiry.
- `AISignalService.SignalSystemPrompt`: added an explicit rule that NIFTY weekly options expire every Tuesday (NSE, since Sep 2025) and BANKNIFTY has monthly-only expiry (last Tuesday), and to pick from "Available Expiries".
- `BacktestService.OpenPosition`: synthetic contract expiry is now symbol-aware via new `GetContractExpiry`. NIFTY keeps the existing 7-day-out approximation (still valid — weekly cadence unchanged, just shifted weekday). BANKNIFTY now prices against the last Tuesday of the current/next month (its only real contract), instead of a nonexistent 7-day weekly contract.

Tests:

- `dotnet build` — 0 warnings, 0 errors.
- `dotnet test` — 27/27 passed.
- Manually traced `GetExpiries` for both symbols against 2026-06-15 (Mon) — NIFTY: 06-16/06-23/06-30/07-07 (weekly Tue) + 07-28/08-25 (monthly); BANKNIFTY: 06-30/07-28/08-25/09-29 (monthly only).

Caveats:

- `FormatOptionSymbol`/Groww trading-symbol format has no day component (`{SYM}{YY}{MMM}{STRIKE}{CE|PE}`), so the exact day-of-month is irrelevant to symbol generation — only `TryParseOptionSymbol`'s reverse-mapped `Expiry` date (used for position display/risk checks) is affected by the Tuesday change.
- No frontend strings reference "Thursday" — no Codex-side follow-up needed for this change.

Codex active files: none.

### 2026-06-15 - Claude Code: Fix ChatStreamChunk camelCase serialization

Files changed:

- `backend/src/OptionsEdge.API/Features/Chat/ChatEndpoints.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- `MapChatEndpoints` now serializes each `ChatStreamChunk` SSE payload with `JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }` instead of default options.
- SSE `data:` payloads now emit `{"type":"delta","text":"...",...}` (camelCase) instead of `{"Type":"delta","Text":"...",...}` (PascalCase), matching what `frontend/src/services/api.ts` already reads (`payload.text`, `payload.error`).
- Fixes blank chat display during streaming (frontend was reading `payload.text`, which was `undefined` under PascalCase output).

Tests:

- `dotnet build` — 0 warnings, 0 errors.
- `dotnet test` — 27/27 passed.
- `npm run build` in `frontend/` — passed, no errors.
- Verified `frontend/src/services/api.ts` already uses camelCase field access (`payload.text`, `payload.error`) — no frontend changes needed.

Caveats: none.

Claude Code active files: none.

### 2026-06-15 - Codex: SSE parser double-newline fix

Files changed:

- `frontend/src/services/api.ts`
- `docs/AI_HANDOFF.md`

Behavior:

- Chat SSE stream parsing now splits buffered text on `\n\n` and processes complete SSE events atomically.
- Parser keeps the trailing incomplete event in `buffer`, so multi-event chunks and split events no longer drop `delta` payloads.
- Malformed JSON events are skipped, and backend `error` events still render through the chat error-message path.

Tests:

- `npm run build` in `frontend/` passed.

Caveats:

- Frontend-only parser fix; backend stream shape unchanged.

Claude Code active files: none currently.

### 2026-06-15 - Codex: Chat markdown, scroll, and error stream display

Files changed:

- `frontend/package.json`
- `frontend/package-lock.json`
- `frontend/tailwind.config.ts`
- `frontend/src/pages/Chat/index.tsx`
- `frontend/src/hooks/useAIChat.ts`
- `frontend/src/services/api.ts`
- `docs/AI_HANDOFF.md`

Behavior:

- Chat assistant responses now render Markdown via `react-markdown` with Tailwind Typography prose styling.
- Assistant bubbles no longer use `whitespace-pre-wrap`; user bubbles still preserve whitespace.
- Chat auto-scroll now uses smooth scrolling only when a new message is added and instant scrolling while streaming updates arrive.
- Backend SSE `error` events now stop stream processing and render inside the assistant message as a red error bubble instead of removing the assistant message and showing only global error state.

Tests:

- `npm run build` in `frontend/` passed.

Caveats:

- Added frontend dependencies: `react-markdown` and `@tailwindcss/typography`.

Claude Code active files: none currently.

### 2026-06-15 - Codex: Per-user AI key frontend

Files changed:

- `frontend/src/services/api.ts`
- `frontend/src/pages/Auth/SecuritySettings.tsx`
- `frontend/src/pages/Dashboard/index.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- Added `aiApi` client methods for `/api/v1/ai/credentials/status`, `POST /ai/credentials`, and `DELETE /ai/credentials`.
- Security Settings now includes an `AI Connection (Anthropic)` card before Change Password, with status load on mount, key connect/verify, disconnect, and inline error/success states.
- Dashboard now detects the backend `"No AI API key configured"` signal-generation error and shows a dismissible yellow prompt linking to `Settings → AI Connection`.

Tests:

- `npm run build` in `frontend/` passed.

Caveats:

- Frontend-only; backend per-user AI key endpoints were already implemented by Claude Code.
- No inline Groww Integration section exists in `SecuritySettings.tsx`; the AI section was placed before Change Password as requested.

Claude Code active files: none currently.

### 2026-06-15 - Claude Code: Per-user Anthropic API key (backend)

Files changed:

- `backend/src/OptionsEdge.API/Domain/Entities/UserAICredential.cs` (new)
- `backend/src/OptionsEdge.API/Infrastructure/Data/AppDbContext.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Data/Migrations/20260615085707_AddUserAICredentials.cs` (new)
- `backend/src/OptionsEdge.API/Infrastructure/Data/Migrations/20260615085707_AddUserAICredentials.Designer.cs` (new)
- `backend/src/OptionsEdge.API/Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs`
- `backend/src/OptionsEdge.API/Features/AI/UserAICredentialService.cs` (new)
- `backend/src/OptionsEdge.API/Features/AI/AICredentialEndpoints.cs` (new)
- `backend/src/OptionsEdge.API/Features/AI/Models.cs` (new)
- `backend/src/OptionsEdge.API/Infrastructure/Claude/ClaudeApiClient.cs`
- `backend/src/OptionsEdge.API/Features/Signals/AISignalService.cs`
- `backend/src/OptionsEdge.API/Features/Chat/ChatService.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Data/DevDataSeeder.cs`
- `backend/src/OptionsEdge.API/Program.cs`
- `backend/src/OptionsEdge.API/appsettings.json`
- `backend/src/OptionsEdge.API/appsettings.Development.json`
- `docs/AI_HANDOFF.md`

Behavior:

- New `UserAICredential` entity (`UserAICredentials` table) stores each user's Anthropic API key, AES-encrypted via the existing `IEncryptionService`, one row per user (unique `UserId`, same shape as `GrowwCredential`).
- New `UserAICredentialService` (Scoped): `SaveAsync` (validates `sk-ant-` prefix, upserts, encrypts), `GetApiKeyAsync`, `HasKeyAsync`, `RemoveAsync` (soft delete via `IsActive=false`).
- New endpoints under `/api/v1/ai` (all `RequireAuthorization`):
  - `POST /api/v1/ai/credentials` — body `{ "apiKey": "sk-ant-..." }`. Rejects non-`sk-ant-` keys with `400`. Verifies the key with a 1-token test call to `https://api.anthropic.com/v1/messages`; `401` from Anthropic → `422 Unprocessable Entity`. Otherwise saves and returns `200`.
  - `DELETE /api/v1/ai/credentials` — deactivates the current user's key, `204`.
  - `GET /api/v1/ai/credentials/status` — `{ hasKey, message }`.
- `ClaudeApiClient.CompleteAsync`/`StreamAsync` (both overloads) now take `apiKey` as the first parameter; the `Claude:ApiKey` config value and the client's `IConfiguration` dependency are gone entirely.
- `AISignalService.GenerateEntrySignalAsync` fetches the calling user's key via `UserAICredentialService.GetApiKeyAsync` right before the Claude call (after the cache-hit check, so cached signals still work without a key); if missing, returns the existing `(SignalResponse, string? Error)` tuple with `Error = "No AI API key configured. Go to Settings → AI Connection to add your Anthropic key from console.anthropic.com"`. `RunPositionRiskCheckAsync` (currently unused/no callers) got the same check keyed on `position.UserId`, returning a `RiskCheckResponse` with `AlertType = "NO_API_KEY"`.
- `ChatService.StreamMessageAsync` checks the key first thing; if missing, yields a single `ChatStreamChunk("error", Error: "No AI API key configured...")` SSE chunk and returns — no DB writes, no Claude call.
- Config: removed `Claude:ApiKey` from both `appsettings.json` and `appsettings.Development.json`. Added `Dev:ClaudeApiKey` to `appsettings.Development.json` (still the same placeholder value, `sk-ant-YOUR-KEY`, not a real secret).
- `DevDataSeeder` restructured so the dev-user-creation `return` no longer skips later steps: after ensuring the dev user exists, it now also seeds `UserAICredentials` for `DevUserId` from `Dev:ClaudeApiKey` (only if not already present), so AI signals/chat work in dev without touching the (not-yet-built) Settings UI.
- As a side effect, generating this migration also resolved a pre-existing `has-pending-model-changes` drift noted in the 2026-06-08 migration fix entry: redundant single-column `IX_ChatMessages_UserId`/`IX_BacktestResults_UserId` indexes (superseded by the composite indexes added in `AddUserScopedHistoryIndexes`) are now dropped.

Verified end-to-end against the dev DB (server run + curl with a real JWT from `/api/v1/auth/login`):

- App startup logs `AI credentials saved for user 00000000-0000-0000-0000-000000000001` / `Dev user AI key seeded from Dev:ClaudeApiKey`.
- `GET /api/v1/ai/credentials/status` → `{"hasKey":true,...}` for dev user.
- `DELETE /api/v1/ai/credentials` → `204`, then status → `{"hasKey":false,...}`.
- `POST /api/v1/signals/generate` with no key → `400 { "error": "No AI API key configured..." }`.
- `POST /api/v1/chat/message` with no key → SSE `event: error` chunk with the same message, stream ends cleanly.
- `POST /api/v1/ai/credentials` with `{"apiKey":"not-a-key"}` → `400 { "error": "Invalid key format. Must start with sk-ant-" }`.
- Restarted the app afterward so `DevDataSeeder` re-seeded the dev key, restoring DB state to before testing.

Caveats:

- The dev placeholder key (`sk-ant-YOUR-KEY`) passes the `sk-ant-` prefix check and gets seeded/encrypted, but is not a real Anthropic key — real AI signal/chat calls in dev still need a real key set via `Dev:ClaudeApiKey` or the (future) Settings UI.
- `POST /api/v1/ai/credentials` makes a live call to `https://api.anthropic.com/v1/messages` to validate the key (costs ~1 token on a valid key).
- Frontend (Settings → AI Connection page, calling these three endpoints) is Codex's follow-up — not built here.

Tests:

- `dotnet build` — 0 warnings, 0 errors.
- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj` passed (27/27).
- `dotnet ef migrations add AddUserAICredentials` + `dotnet ef database update` — applied cleanly to dev DB.

Codex active files: none.

### 2026-06-15 - Codex: Groww status cache-first auth probe

Files changed:

- `backend/src/OptionsEdge.API/Features/Groww/GrowwEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Groww/GrowwUserApiClient.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- Confirmed with the requested grep that `Program.cs` and `DevDataSeeder.cs` contain no Groww auth calls (`GetOrRefreshToken`, `AuthenticateAsync`, `GrowwUserApiClient`, or `groww.`).
- `/api/v1/groww/status` now checks `IMemoryCache` for the existing per-user Groww token before calling `GetOrRefreshTokenAsync`, avoiding auth HTTP calls on repeated frontend status checks.
- `GrowwUserApiClient.TokenCacheKey(userId)` is now public so the endpoint can use the same cache key as the client (`groww:user_token:{userId}`).
- Groww status cancellation handling now has explicit `TaskCanceledException` and `OperationCanceledException` catches, both logging at debug and returning a non-fatal disconnected status.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` passed with zero warnings.

Caveats:

- `Program[0]` warning source is from `ILogger<Program>` injected into `GrowwEndpoints`, not from `Program.cs` startup code.
- Cache-first status response skips the one-time Groww portfolio import when the token is already cached; imports still run right after a fresh token is obtained through the status path.

Codex active files: none currently.

### 2026-06-15 - Codex: Groww status auth check timeout wording

Files changed:

- `backend/src/OptionsEdge.API/Features/Groww/GrowwEndpoints.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- Aligned the Groww auth probe with the requested dedicated timeout shape: `using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10))`.
- `OperationCanceledException` now logs at debug as `Groww startup check skipped — will authenticate on first user request`.
- The call still returns a non-fatal unauthenticated Groww status so the UI can retry later.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` passed with zero warnings.

Caveats:

- `Program.cs` still has no Groww HTTP/auth startup call; it only registers and maps Groww services/endpoints. The startup-triggered auth probe is `/api/v1/groww/status`, so the call-site fix remains in `GrowwEndpoints.cs`.

Codex active files: none currently.

### 2026-06-15 - Codex: VerifyEmail countdown + Groww status timeout

Files changed:

- `frontend/src/pages/Auth/VerifyEmail.tsx`
- `backend/src/OptionsEdge.API/Features/Groww/GrowwEndpoints.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- VerifyEmail confirmed countdown now starts from `6`, decrements to `0`, shows a check mark at completion, and navigates only after `next <= 0`.
- VerifyEmail countdown effect now uses `mountedRef`/`timerRef` guards to avoid duplicate timers under React Strict Mode and still clean up interval state.
- Groww status authentication check now uses a dedicated 10-second `CancellationTokenSource`; timeout logs at debug level and returns an unauthenticated Groww status for retry instead of warning as a generic failure.

Tests:

- `npm run build` in `frontend/` passed.
- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` passed with zero warnings.

Caveats:

- No Groww startup check exists in `Program.cs`; the auth probe that runs on app/dashboard startup is `/api/v1/groww/status`, so the timeout fix was applied in `GrowwEndpoints.cs`.

Codex active files: none currently.

### 2026-06-15 - Codex: VerifyEmail confirmed screen visibility fix

Files changed:

- `frontend/src/pages/Auth/VerifyEmail.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- VerifyEmail confirmed state now waits 500ms before starting the 5-second countdown so the success screen renders visibly before redirect timing begins.
- Countdown interval is stored in a `useRef` and cleared both on unmount and when the user clicks `Go to Login now`.
- Confirmed screen now uses a larger success icon, stronger success copy, and prominent countdown number.

Tests:

- `npm run build` in `frontend/` passed.

Caveats:

- Frontend-only UX fix; backend auth behavior unchanged.

Codex active files: none currently.

### 2026-06-15 - Claude Code: Graceful Groww credential handling (silent skip, monitor only connected users, dashboard prompt)

Files changed:

- `backend/src/OptionsEdge.API/Infrastructure/Groww/GrowwMarketDataService.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Background/PositionMonitorWorker.cs`
- `frontend/src/pages/Dashboard/index.tsx` (Claude edit, normally Codex-owned — flagged per explicit user request, no conflict with "Codex active files")
- `docs/AI_HANDOFF.md`

Behavior:

- `GrowwMarketDataService.RefreshForUserAsync` now checks `GrowwCredentialService.HasCredentialsAsync` before calling `GetSpotSnapshotAsync`; if the user has no active Groww credentials it logs at `LogDebug` (not `LogWarning`) and returns early, leaving cached/mock data in place. This was previously an exception-driven `LogWarning` on every refresh for users without Groww.
- `PositionMonitorWorker.TickAsync` now queries `GrowwCredentials` for users with `IsActive` credentials and only monitors `Positions` belonging to those users (`connectedUserIds.Contains(p.UserId)`); ticks with zero matching positions return early.
- Dashboard now polls `growwApi.getStatus()` once on mount and shows a dismissible yellow banner ("Connect Groww for live market data" → Settings → Security) when `enabled && !connected`. Dismissal is remembered for the session via `sessionStorage` (`growwPromptDismissed`).

Caveats:

- **Behavior change**: users with active positions but no Groww credentials are no longer monitored by `PositionMonitorWorker` at all (no SL/target/risk alerts), even though `OptionsService.GetOptionLtp` would still produce a Black-Scholes-estimated LTP for them. Previously all active positions were monitored regardless of Groww connection. If risk alerting should remain available to non-Groww users on estimated prices, this query needs revisiting.
- Dashboard's Groww-prompt fetch is independent of the existing `growwStatus` polling in `Header`/`Sidebar` (which already populates the same `GrowwStatus` via the app store) — duplicate `/groww/status` call on Dashboard mount.

Tests:

- `dotnet build src/OptionsEdge.API/OptionsEdge.API.csproj` zero warnings.
- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj` passed (27/27).
- `npm run build` in `frontend/` passed.

### 2026-06-15 - Codex: Auth email blur + countdown timer fixes

Files changed:

- `frontend/src/pages/Auth/Register.tsx`
- `frontend/src/pages/Auth/VerifyEmail.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- Register email blur validation now clears the stale email error when the entered email becomes valid.
- VerifyEmail countdown interval now uses a mutable `timerId` captured by the interval callback and cleanup, avoiding stale closure issues.

Tests:

- `npm run build` in `frontend/` passed.

Caveats:

- Frontend-only fix; backend auth behavior unchanged.

Codex active files: none currently.

### 2026-06-15 - Codex: Auth verification UX improvements

Files changed:

- `frontend/src/pages/Auth/Register.tsx`
- `frontend/src/pages/Auth/VerifyEmail.tsx`
- `frontend/src/pages/Auth/Login.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- Register now validates email with a stricter client-side regex on blur and before API submit.
- Register shows an email verification note with a spam/junk folder tip below the submit button.
- VerifyEmail confirmed state now starts a 5-second countdown and redirects to `/login` with a success message.
- VerifyEmail idle state now explains that the confirmation email was sent, that the link expires in 24 hours, and suggests checking spam/junk.
- Login now treats `403` as an unconfirmed-email state without a red generic error, showing a yellow verification warning with resend action and inline resend status.

Tests:

- `npm run build` in `frontend/` passed.

Caveats:

- Frontend-only UX change; backend auth behavior unchanged.

Codex active files: none currently.

### 2026-06-15 - Claude Code: Alert dedup cache survives failed SaveAlertAsync

Files changed:

- `backend/src/OptionsEdge.API/Infrastructure/Background/PositionMonitorWorker.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- `ProcessPositionAsync`'s alert loop already set the dedup cache key (`cache.Set`) before the async `SaveAlertAsync`/`BroadcastAlertAsync` calls, so a same-tick double-fire wasn't possible. Added the missing failure path: `SaveAlertAsync`/`BroadcastAlertAsync` now wrapped in try/catch — on exception, `cache.Remove(dedupKey)` releases the lock so the alert retries next tick instead of being silently swallowed by the 15-min dedup TTL.

Tests:

- `dotnet build src/OptionsEdge.API/OptionsEdge.API.csproj` zero warnings.

### 2026-06-15 - Claude Code: GrowwSymbolHelper uses correct Groww FNO symbol format

Files changed:

- `backend/src/OptionsEdge.API/Infrastructure/Groww/GrowwSymbolHelper.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- `FormatOptionSymbol` now builds `{UNDERLYING}{YY}{MMM}{STRIKE}{CE|PE}` (e.g. `BANKNIFTY26JUN51800CE`, `NIFTY26JUN24200CE`) — 3-letter month name, **no day component**. Previous format (`NIFTY26M1224200CE`, single-letter futures month code + day) was rejected by Groww with `400` on all FNO live-data quote/order calls.
- `TryParseOptionSymbol`'s regex updated to match the new `YYMMM` format (`JAN`-`DEC`). Since the symbol no longer carries a day, the reversed expiry date is approximated as the **last Thursday of the parsed month/year** (standard NSE monthly expiry) via new `LastThursdayOfMonth`.
- This fixes `GrowwOrderService` (order placement + portfolio position import) and `PositionMonitorWorker.GetCurrentLtpAsync` (per-position live LTP), both of which call `FormatOptionSymbol`/`TryParseOptionSymbol`.

Caveats:

- If a position's actual expiry isn't the last Thursday of its month (e.g. exchange holiday shifts it, or future weekly expiries on non-Thursday days), `TryParseOptionSymbol`'s reconstructed `Expiry` will be off by a few days. `FormatOptionSymbol` (the hot path for live LTP/order calls) is unaffected since Groww's symbol doesn't need the day anyway.

Tests:

- `dotnet build src/OptionsEdge.API/OptionsEdge.API.csproj` zero warnings.
- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj` passed (27/27).

### 2026-06-15 - Claude Code: PositionMonitorWorker fetches real Groww LTP per position

Files changed:

- `backend/src/OptionsEdge.API/Infrastructure/Background/PositionMonitorWorker.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- `ProcessPositionAsync` now takes the tick's `IServiceScope` and resolves `GrowwUserApiClient`/`GrowwCredentialService` from it (both scoped, reused across all positions in the tick — no per-position scope).
- New `GetCurrentLtpAsync`: when `Groww:Enabled` and the position's user has saved Groww credentials, calls `GrowwUserApiClient.GetOptionLtpAsync` directly for the position's trading symbol (via `GrowwSymbolHelper.FormatOptionSymbol`) and uses that LTP for P&L/alert checks.
- Falls back to `OptionsService.GetOptionLtp` (chain cache / Black-Scholes) when Groww is disabled, the user has no credentials, the Groww call throws, or the Groww quote is `<= 0`.
- Rate-limit pacing: `TickAsync` pauses 1.1s after every 10 positions processed (only when `Groww:Enabled`), keeping per-tick Groww calls under ~10 req/sec.

Caveats:

- Each position with Groww credentials makes its own live quote call every tick (60s) — for many active positions across many users this is more Groww traffic than the old shared-chain-cache approach; the 1.1s/10-position pacing only protects a single user's rate limit per tick, not across concurrent users.
- A Groww quote of exactly `0` (e.g., illiquid strike, no LTP yet) is treated the same as "fetch failed" and falls back to chain cache/BS.

Tests:

- `dotnet build src/OptionsEdge.API/OptionsEdge.API.csproj` zero warnings.

### 2026-06-15 - Codex: Position LTP source badge + auto-refresh

Files changed:

- `frontend/src/components/positions/PositionCard.tsx`
- `frontend/src/pages/Positions/index.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- Position cards now show an LTP source badge from the market snapshot store: green `LIVE LTP` when `dataSource === "groww_live"`, otherwise grey `EST. LTP`.
- Positions page refreshes the full position list on mount and every 30 seconds while market status is open or unknown, so card LTP/P&L can update from latest backend values.
- Positions header now shows the last successful refresh timestamp.

Tests:

- `npm run build` in `frontend/` passed.

Caveats:

- Polling uses existing `positionsApi.getAll()`; there is no `positionsApi.getPositions()` method in the current frontend API client.
- When market status is explicitly closed, the 30-second interval is paused after the initial refresh.

Codex active files: none currently.

### 2026-06-15 - Claude Code: Real Groww LTP for GetOptionLtp, real OI change/volume in chain

Files changed:

- `backend/src/OptionsEdge.API/Infrastructure/Groww/GrowwModels.cs`
- `backend/src/OptionsEdge.API/Features/Groww/GrowwUserApiClient.cs`
- `backend/src/OptionsEdge.API/Features/Options/OptionsService.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- `GrowwOptionLeg` gained `OiChange` (decimal), parsed from `oi_day_change` ?? `change_in_open_interest`. `Volume` parsing now tries `day_volume` first, then `volume`.
- `OptionsService.GetOptionLtp` (used by Position P&L, SL/target alerts, and `PositionMonitorWorker`) now checks the cached Groww chain (`GrowwChainCacheKey`) first for the strike/leg's real `Ltp`; falls back to the existing Black-Scholes estimate if the strike isn't in the cached chain or the cached LTP is `0`/missing.
- `OptionsService.GetChain` now overlays real `OiChange`/`Volume` per leg from the cached Groww chain when non-zero (`OiChange != 0`, `Volume > 0`); otherwise keeps the synthetic `Random()`-based estimate.

Caveats:

- Groww chain cache is populated only after `/api/v1/options/chain/{symbol}` has been called at least once for that symbol/expiry (`CacheGrowwChain`); until then `GetOptionLtp` and `GetChain` use fully synthetic values.
- `OiChange` of exactly `0` (legitimately flat OI) and `Volume` of exactly `0` (no trades yet) are indistinguishable from "Groww didn't report this field" and fall back to the synthetic estimate in those cases.

Tests:

- `dotnet build src/OptionsEdge.API/OptionsEdge.API.csproj` zero warnings.

### 2026-06-15 - Claude Code: Live VIX, real option chain OI/IV, FII/DII N/A when unavailable

Files changed:

- `backend/src/OptionsEdge.API/Features/Groww/GrowwUserApiClient.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Groww/GrowwMarketDataService.cs`
- `backend/src/OptionsEdge.API/Features/Market/Models.cs`
- `backend/src/OptionsEdge.API/Features/Options/OptionsEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Options/OptionsService.cs`
- `frontend/src/components/market/MarketPulse.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- `GrowwUserApiClient.GetVixAsync` fetches India VIX (`NSE/CASH/INDIA VIX` quote); failures return `0` (non-critical).
- `GrowwMarketDataService.RefreshForUserAsync` now patches the live `Vix` into the cached snapshot: fetched once for NIFTY, then reused from the cached NIFTY snapshot for BANKNIFTY (no extra Groww call per refresh).
- `MarketSnapshotResponse.FiiFlow`/`DiiFlow` documented as `0` (unavailable) whenever `DataSource` is `groww_live` — Groww has no FII/DII flow data.
- Frontend `MarketPulse` shows `N/A` for FII/DII when `dataSource === 'groww_live'` and the value is `0`, plus a small "FII/DII data not available via Groww API" note.
- `/api/v1/options/chain/{symbol}` now triggers a Groww option-chain refresh (when `Groww:Enabled`) for the resolved expiry, caching the result via `OptionsService.CacheGrowwChain` (30s market-hours / 5min off-hours TTL, same pattern as snapshot/candle caches).
- `OptionsService.GetChain` overlays real OI/IV/LTP from the cached Groww chain per strike (CE/PE independently); strikes/legs Groww didn't return keep the Black-Scholes-simulated values. PCR is computed from the (now real, when available) total OI.
- Greeks (Delta/Gamma/Theta/Vega) remain Black-Scholes-computed in all cases — Groww greeks aren't used.

Caveats:

- If a Groww chain fetch fails on a request, `GetChain` falls back to whatever was cached from the last successful fetch (or fully synthetic if nothing cached yet) — capped by the same TTL as the snapshot cache.
- Groww's `implied_volatility` is assumed to be a percentage (same scale as the synthetic `iv * 100`); not verified against a live response.
- BANKNIFTY VIX reuse depends on NIFTY having refreshed first in the same cycle — true for `/api/v1/market/snapshot` (refreshes both, NIFTY first) but a BANKNIFTY-only refresh with no NIFTY cache yet falls back to `0`.

Tests:

- `dotnet build src/OptionsEdge.API/OptionsEdge.API.csproj` zero warnings.
- `npm run build` in `frontend/` passed.
- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj` passed (27/27).

Codex active files: none currently.

### 2026-06-15 - Codex: ADX filter + backtest diagnostics

Files changed:

- `backend/src/OptionsEdge.API/Features/Backtest/Models.cs`
- `backend/src/OptionsEdge.API/Features/Backtest/BacktestService.cs`
- `frontend/src/pages/Backtest/index.tsx`
- `frontend/src/services/api.ts`
- `frontend/src/types/index.ts`
- `docs/AI_HANDOFF.md`

Behavior:

- Backtest run requests now accept `adxFilterEnabled` (default `true`) and persist `ADXFilter` state in saved run parameters.
- Entry candidates now pass an ADX(14) + EMA20 guard before trade entry when the filter is enabled: ADX must be at least `20`, bullish entries must close above EMA20, and bearish entries must close below EMA20.
- ADX/EMA trend values are computed from intraday candles with Skender and cached by IST trading day for simulation lookup.
- Backtest responses now include `diagnosticSummary` with candidate signals, ADX/EMA filtered signals, entered trades, target hits, SL hits, expiry exits, and theta exits.
- Backtest UI now includes an `ADX Filter` checkbox and shows a `Signal Diagnostics` row under result stats.

Tests:

- `dotnet build backend/src/OptionsEdge.API/OptionsEdge.API.csproj` passed with zero warnings.
- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj` passed (27/27).
- `npm run build` in `frontend/` passed.

Caveats:

- Plain `dotnet build` from repo root fails with `MSB1003` because the root has no `.sln` or project file; backend project build was used.

Claude Code active files: none currently.

### 2026-06-15 - Claude Code: Migration Designer.cs fix

Files changed:

- `backend/src/OptionsEdge.API/Infrastructure/Data/Migrations/20260608195500_AddUserScopedHistoryIndexes.cs` (regen)
- `backend/src/OptionsEdge.API/Infrastructure/Data/Migrations/20260608195500_AddUserScopedHistoryIndexes.Designer.cs` (new)
- `CLAUDE.md` (dual-agent protocol added)

Behavior:

- Migration now has correct Designer.cs.
- Future migrations will work without error.

Caveat:

- `dotnet ef migrations remove --no-build` (EF tools `10.0.3` vs runtime `10.0.8`) reverted `AppDbContextModelSnapshot.cs` to an empty model instead of the prior migration's state. Did not run `migrations add` after that — restored the snapshot via `git checkout` instead.
- `AppDbContextModelSnapshot.cs` was already correct (it already included the two new indexes; the only thing missing was the Designer.cs). Hand-built the Designer.cs by combining the prior migration's Designer header (renamed) with the current snapshot's `BuildModel` body as `BuildTargetModel` — the two are textually identical for the latest migration.
- Removed the stray `[DbContext]`/`[Migration]` attributes that had been manually added to the main migration `.cs` file (those belong only on `Designer.cs`; duplicate attribute was a build error).
- `dotnet ef migrations has-pending-model-changes` still reports pending changes, but this is a pre-existing tool/runtime-version artifact (confirmed identical on the untouched pre-fix tree too, via `git stash`), not caused by this fix.
- AppDbContextModelSnapshot.cs unchanged by this fix.

Tests:

- `dotnet build` zero warnings.
- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj` passed (27/27).
- `dotnet ef migrations list` shows `20260608195500_AddUserScopedHistoryIndexes (Pending)`.
- `dotnet ef migrations script` for this migration generates exactly the two expected `CREATE INDEX` statements.

Codex active files: none currently.

### 2026-06-08 - User-scoped history indexes

Files changed:

- `backend/src/OptionsEdge.API/Infrastructure/Data/AppDbContext.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Data/Migrations/20260608195500_AddUserScopedHistoryIndexes.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs`
- `docs/AI_HANDOFF.md`

Behavior:

- Added a composite index on `BacktestResults(UserId, CreatedAt DESC)` for paged per-user saved backtest history.
- Added a composite index on `ChatMessages(UserId, SessionId, CreatedAt)` for per-user chat session history.
- These are performance/scoping-support indexes only; no missing `UserId` columns were found on user-owned entities.

Caveat:

- `dotnet ef migrations add AddUserScopedHistoryIndexes` built successfully but stalled after the EF tool warning (`10.0.3` tools vs `10.0.8` runtime), so the migration class and snapshot were added manually in standard EF migration format.

Validation:

- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj` passed.

### 2026-06-08 - User-data security audit and SignalR scoping fixes

Files changed:

- `backend/src/OptionsEdge.API/Infrastructure/SignalR/MarketHub.cs`
- `backend/src/OptionsEdge.API/Features/Signals/AISignalService.cs`
- `backend/src/OptionsEdge.API/Features/Auth/AuthEndpoints.cs`
- `frontend/src/hooks/useSignalR.ts`
- `frontend/src/hooks/useAlerts.ts`
- `docs/AI_HANDOFF.md`

Behavior:

- Audited Minimal API route auth and user-owned data filtering across auth, positions, alerts, signals, chat, Groww, backtest, usage, billing, market/options/indicators, and SignalR.
- Confirmed user-owned REST data is filtered by the current JWT user for positions, alerts, backtest history, signal history/by-id, chat history, usage stats, Groww credentials, and imported Groww positions.
- Fixed SignalR alert subscription so clients can no longer choose an arbitrary `userId` alert group. The hub now derives the group from `ClaimTypes.NameIdentifier` and rejects mismatched IDs.
- Changed AI signal live broadcast from symbol group to `Clients.User(userId)` so a generated user-scoped signal is not broadcast to other authenticated users watching the same symbol.
- Updated frontend SignalR connections to pass the in-memory JWT via `accessTokenFactory`.
- Updated alert SignalR subscription to call `SubscribeToAlerts` without a dev/user id.
- Narrowed `/api/v1/auth/logout` so it only revokes refresh tokens owned by the currently authenticated user.

Security notes:

- Deliberately public endpoints remain `/health`, development OpenAPI, and pre-login auth flows: register, confirm email, resend confirmation, login, two-factor login completion, refresh, forgot password, reset password.
- Market, indicator, and options endpoints are authenticated but return shared market/reference data rather than user-owned private data.
- Groww order cancel relies on the current user's Groww token at the broker API boundary; there is no local Groww order ownership table yet.

Validation:

- `rg "Map(Get|Post|Put|Delete)|MapHub|RequireAuthorization|AllowAnonymous" backend/src/OptionsEdge.API -n`
- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj` passed.
- `npm run build` in `frontend/` passed.

### 2026-06-08 - Trade chart popup from Backtest trade log

Files changed:

- `frontend/src/pages/Backtest/index.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- Backtest Trade Log rows are now clickable.
- Clicking a trade opens a modal with a focused chart around the trade window.
- The modal fetches candles through `marketApi.getCandles(result.symbol)`.
- The chart marks Entry and Exit with vertical markers and dots.
- The modal summarizes entry premium, exit premium, SL, Target 1, P&L, exit reason, and position side.
- Position side is currently derived from strategy: `IronCondor` is shown as short premium; existing debit/long strategies are shown as long premium.

Caveat:

- The popup chart shows the underlying index candles (`NIFTY`/`BANKNIFTY`), while entry/exit premiums are simulated option/net strategy values from the backtest. It does not show historical option LTP candles.

Validation:

- `npm run build` in `frontend/` passed.
- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj` passed.

### 2026-06-08 - Custom target/SL points for backtest

Files changed:

- `backend/src/OptionsEdge.API/Features/Backtest/Models.cs`
- `backend/src/OptionsEdge.API/Features/Backtest/BacktestService.cs`
- `frontend/src/services/api.ts`
- `frontend/src/types/index.ts`
- `frontend/src/pages/Backtest/index.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- `BacktestRunRequest` now accepts optional `targetPoints` and `stopLossPoints`.
- The Backtest page now includes `Target pts` and `SL pts` numeric inputs, defaulting to `30` and `15`.
- When custom points are provided, exits use fixed option premium/net strategy premium points:
  - SL exits at `entry premium - stopLossPoints`.
  - Target 1 exits at `entry premium + targetPoints`.
  - Target 2 exits at `entry premium + (targetPoints * 2)`.
- Saved run parameters and responses include `targetPoints` and `stopLossPoints`.
- Result metadata and saved run rows show the selected target/SL points.
- If these values are omitted by older clients, the previous percentage-based behavior remains as fallback.

Validation:

- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj` passed.
- `npm run build` in `frontend/` passed.

### 2026-06-08 - Backtest SL/target premium points shown

Files changed:

- `backend/src/OptionsEdge.API/Features/Backtest/Models.cs`
- `backend/src/OptionsEdge.API/Features/Backtest/BacktestService.cs`
- `frontend/src/types/index.ts`
- `frontend/src/pages/Backtest/index.tsx`
- `docs/AI_HANDOFF.md`

Behavior:

- `BacktestTradeLogEntry` now includes `stopLossPrice`, `target1Price`, and `target2Price`.
- These values are option premium/net strategy premium points, not NIFTY/BANKNIFTY index points.
- Current rule remains premium-based: SL is `-35%`, Target 1 is `+70%`, and Target 2 is `+120%` from each trade entry premium/risk capital.
- Backtest Trade Log now shows `SL pts`, `T1 pts`, and `T2 pts`.
- Older saved runs that do not have these fields show `—` in the UI.

Validation:

- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj` passed.
- `npm run build` in `frontend/` passed.

### 2026-06-08 - API authentication audit and route lockdown

Files changed:

- `backend/src/OptionsEdge.API/Program.cs`
- `backend/src/OptionsEdge.API/Features/Auth/AuthEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Billing/BillingEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Chat/ChatEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Groww/GrowwEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Indicators/IndicatorEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Market/MarketEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Options/OptionsEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Positions/PositionEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Signals/SignalEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Usage/UsageEndpoints.cs`
- `docs/AI_HANDOFF.md`

Backend behavior:

- Added `RequireAuthorization()` to market, indicators, options, signals, positions, alerts, chat, Groww, orders, usage, billing, and the market SignalR hub.
- Added `RequireAuthorization()` to protected auth operations: logout, enable 2FA, verify 2FA setup, disable 2FA, `/auth/me`, and change password.
- Fixed `GET /api/v1/signals/{id}` to filter by current `UserId`; previously it fetched by id only.
- Deliberately public endpoints remain: `/health`, development OpenAPI, and auth bootstrap/recovery endpoints needed before login (`register`, `confirm-email`, `resend-confirmation`, `login`, `two-factor`, `refresh`, `forgot-password`, `reset-password`).

Validation:

- Endpoint audit was done with `rg "Map(Get|Post|Put|Delete|Group|Hub)|RequireAuthorization|AllowAnonymous" backend/src/OptionsEdge.API -n`.
- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj` passed.

### 2026-06-08 - Backtest saved-run pagination and retention cleanup

Files changed:

- `backend/src/OptionsEdge.API/Features/Backtest/BacktestEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Backtest/BacktestService.cs`
- `backend/src/OptionsEdge.API/Features/Backtest/Models.cs`
- `backend/src/OptionsEdge.API/appsettings.json`
- `backend/src/OptionsEdge.API/appsettings.Development.json`
- `frontend/src/services/api.ts`
- `frontend/src/types/index.ts`
- `frontend/src/pages/Backtest/index.tsx`
- `docs/AI_HANDOFF.md`

Backend behavior:

- `/api/v1/backtest/history` now supports `page` and `pageSize` query params.
- History response is now a paged envelope with `items`, `page`, `pageSize`, `totalItems`, `totalPages`, and `retentionDays`.
- Backtest history cleanup now runs before saving a new backtest and before listing history.
- Retention is configurable with `Backtest:HistoryRetentionDays`.
- Default retention is `30` days and is clamped between `1` and `365` days.
- Expired rows are deleted per user using EF Core `ExecuteDeleteAsync`.

Frontend behavior:

- Saved Runs now shows only `8` rows per page.
- Saved Runs includes Prev/Next pagination controls.
- Header shows total saved runs and configured retention days.
- After running a new backtest, history reloads page `1` so the newest run is visible.

Validation:

- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj` passed.
- `npm run build` in `frontend/` passed.
- `dotnet test` still emits an existing MSBuild copy warning from nested `bin/Debug/net10.0/...` output paths, but tests pass.

### 2026-06-08 - Intraday backtest, Pivot + EMA20 strategy, Groww fixes

Files changed:

- `backend/src/OptionsEdge.API/Features/Groww/GrowwUserApiClient.cs`
- `backend/src/OptionsEdge.API/Infrastructure/Groww/GrowwMarketDataService.cs`
- `backend/src/OptionsEdge.API/Features/Backtest/BacktestEndpoints.cs`
- `backend/src/OptionsEdge.API/Features/Backtest/BacktestService.cs`
- `backend/src/OptionsEdge.API/Features/Backtest/Models.cs`
- `frontend/src/pages/Backtest/index.tsx`
- `frontend/src/types/index.ts`

Backend behavior:

- Groww historical candles are fetched in chunks to avoid Groww 400 interval-duration errors.
- Groww candle parsing now skips malformed/null OHLC rows and treats missing/null volume as `0`.
- Groww positions endpoint changed to `/v1/positions/user?segment=FNO`.
- Backtest endpoints require authorization.
- When `Groww:Enabled` is true, `/api/v1/backtest/run` refreshes Groww candles before running the backtest.
- Backtest response includes `dataSource`, `candleCount`, and `tradingDays`.
- Backtest now simulates on 15-minute intraday candles instead of daily closes.
- Daily pivots are still computed from the previous trading day and applied to intraday bars.
- Added entry conditions:
  - `PivotEma20Bullish`
  - `PivotEma20Bearish`
- `SupertrendBullish` and `SupertrendBearish` now represent current trend state, not only trend-flip bars.
- SL/target checks now use intraday high/low and lot-size adjusted risk.
- Risk thresholds currently used:
  - SL: `-35%` of position risk
  - Target 1: `+70%` of position risk
  - Target 2: `+120%` of position risk

Frontend behavior:

- Backtest page shows Groww/mock data source, candle count, and trading days.
- Backtest page exposes `Pivot + EMA20 Bullish` and `Pivot + EMA20 Bearish`.
- Labels are more human-readable: options strategy vs signal strategy.
- Results show `Target Hits` and `SL Hits`.
- Trade log handles both `pnL` and `pnl` JSON casing for compatibility.
- Default backtest period is `90` days.

Validation:

- `dotnet test backend/tests/OptionsEdge.API.Tests/OptionsEdge.API.Tests.csproj` passed.
- `npm run build` in `frontend/` passed.

Observed result from UI after changes:

- `NIFTY + Long Call + Pivot + EMA20 Bullish + Target 1 Hit + 90 days`
- Groww historical candles: `1,450 candles`, `58 trading days`
- Result shown: `18` trades, `4` target hits, `13` SL hits, net P&L around `-₹39,521`
- Interpretation: the engine is producing trades, but this strategy is not profitable enough yet. It needs stricter filters.

Suggested next improvements:

- Add trend-strength filter such as ADX to avoid low-momentum entries.
- For bullish entries, require price above EMA20 and EMA50.
- Avoid long-call entries too close to R1 resistance.
- Consider a pivot/EMA20 breakout-and-retest entry instead of entering on any reclaim/bounce.
- Add strategy diagnostics to response, such as candidate signals, skipped signals, entry timestamps, and exit distribution.
