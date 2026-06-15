# AI Handoff

This file is the shared working memory for Claude Code and Codex. Read it before non-trivial changes and update it after changes that affect behavior, architecture, API contracts, data flow, or important debugging context.

## Update Protocol

- Add newest notes at the top of `Change Log`.
- Keep entries concise but specific: files changed, behavior changed, tests run, and known caveats.
- Do not record secrets, tokens, connection strings, or private account data.
- If a change is experimental or incomplete, mark it clearly.

## Current Focus

Backtest and Groww integration for NIFTY/BANKNIFTY options strategy testing.

Important caveat: Groww historical candles are real index candles, but historical option premiums are still estimated in the backtest engine with Black-Scholes. The backtest validates signal timing against market candles, not real historical option LTP replay.

## Change Log

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
