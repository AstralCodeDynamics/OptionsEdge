# OptionsEdge — Build Tasks for Claude Code

> Feed this file to Claude Code at the start of each phase.
> Always say: "Read CLAUDE.md, SPEC.md, ARCHITECTURE.md, then complete Phase X tasks."

---

## Phase 0 — Project Scaffold

### Backend
- [x] Create solution: `dotnet new sln -n OptionsEdge`
- [x] Create API project: `dotnet new webapi -n OptionsEdge.API --use-minimal-apis`
- [x] Add NuGet packages: Npgsql.EntityFrameworkCore.PostgreSQL, EF Core Design, Skender.Stock.Indicators, SignalR, JwtBearer, FluentValidation.AspNetCore, BCrypt.Net-Next
- [x] Create folder structure per ARCHITECTURE.md
- [x] Create all Domain/Entities (User, Position, Signal, Alert, ChatMessage, AIUsageLog, BacktestResult)
- [x] Create all Domain/Enums (AlertSeverity, SignalType, OptionType)
- [x] Create AppDbContext with all DbSets and proper EF Core configuration
- [x] Add AppConstants.cs (market hours 9:15-15:30 IST, rate limits, model names)
- [x] Add appsettings.json (structure only, no secrets)
- [x] Add appsettings.Development.json to .gitignore
- [x] Create initial EF Core migration
- [x] Verify: `dotnet build` passes with zero warnings

### Frontend
- [x] Scaffold: `npm create vite@latest frontend -- --template react-ts`
- [x] Install: tailwindcss @tailwindcss/vite, axios, zustand, @microsoft/signalr, lightweight-charts, react-router-dom, recharts
- [x] Configure Tailwind with custom breakpoints (mobile first: default < 768px < 1200px)
- [x] Create folder structure per ARCHITECTURE.md
- [x] Create all TypeScript interfaces in `src/types/index.ts` (MarketSnapshot, Signal, Position, Alert, ChatMessage, User, IndicatorsResponse, OptionsChain types)
- [x] Create `src/services/api.ts` with Axios instance pointing to localhost:5001
- [x] Create `src/store/appStore.ts` with Zustand (market, positions, alerts, user, ui slices)
- [x] Create `src/App.tsx` with React Router (all pages stubbed with placeholder content)
- [x] Create responsive AppShell: Sidebar on lg+, BottomNav on mobile
- [x] Verify: `npm run dev` runs without errors

---

## Phase 1 — Live Data Foundation

### Backend
- [x] Create `MockMarketDataService`:
  - Realistic NIFTY OHLCV 15-min candles, 90 days history, price range 23000-25000
  - BANKNIFTY candles, price range 50000-53000
  - Price simulation: random walk with mean reversion, realistic volatility
  - VIX: 11-18 range, PCR: 0.8-1.6 range, FII flow: random +/- 500-2000 Cr
- [x] Create `MarketDataWorker` (BackgroundService):
  - Every 30s during market hours only (use MarketHoursHelper)
  - Gets latest snapshot, broadcasts via SignalR
  - Outside hours: broadcasts MarketStatus "Closed"
- [x] Create `MarketHub` (SignalR Hub):
  - Groups: NIFTY, BANKNIFTY, alerts:{userId}
  - Server-to-client: PriceUpdate, MarketStatus
- [x] Create `MarketHoursHelper`: IST timezone, 9:15-15:30, Mon-Fri
- [x] Implement GET /api/v1/market/snapshot, /candles/{symbol}, /status
- [x] Wire CORS for http://localhost:5173
- [x] Verify SignalR connection from frontend works

### Frontend
- [x] Create `useSignalR.ts`: connects, handles reconnect with exponential backoff
- [x] Create `useMarketData.ts`: REST fetch on mount + SignalR updates merged
- [x] Create `IndexCard.tsx`: price, change, change%, IV — responsive sizing
- [x] Create `MarketStatusBanner.tsx`: Open/Closed/Pre-market, countdown to next event
- [x] Create `MarketPulse.tsx`: VIX, PCR gauge, FII/DII flow — 2-col mobile, 3-col desktop
- [x] Wire Dashboard page: all components showing live data
- [x] Verify real-time updates work in browser and at 375px mobile width

---

## Phase 2 — Indicators & Options Chain

### Backend
- [x] Create `IndicatorService` using Skender.Stock.Indicators:
  - RSI(14): value, signal enum (Overbought/Oversold/Neutral)
  - MACD(12,26,9): value, signal line, histogram, crossover bool
  - BollingerBands(20,2): upper, middle, lower, squeeze bool (bandwidth < threshold)
  - ADX(14): value, trend strength label (Weak <20, Moderate 20-40, Strong >40)
  - EMA: 9, 20, 50, 200 — values + price above/below bool
  - Supertrend(10,3): value, isBullish bool
  - Pivots (Classic): S3/S2/S1/Pivot/R1/R2/R3
- [x] Implement GET /api/v1/indicators/{symbol}
- [x] Create `OptionsService`:
  - Chain: 5 strikes below ATM + ATM + 5 above, CE and PE
  - OI realistic values with heavier OI at round numbers (24000, 24200 etc.)
  - IV via simplified formula, Greeks (Delta/Gamma/Theta/Vega) from Black-Scholes
  - PCR = sum(PE OI) / sum(CE OI)
  - Max Pain = strike where total option loss is minimum
- [x] Implement GET /api/v1/options/chain/{symbol}, /expiries/{symbol}, /maxpain/{symbol}
- [x] Add IndicatorUpdate to SignalR broadcast

### Frontend
- [x] Create `IndicatorPanel.tsx`:
  - 6 cards: RSI, MACD, BB, ADX, EMA, Supertrend
  - Signal badge: BUY (green), SELL (red), NEUTRAL (blue)
  - 2-col mobile, 3-col desktop grid
- [x] Create `PivotLevels.tsx`: vertical ladder R3→R2→R1→Spot→S1→S2→S3
- [x] Create `PriceChart.tsx` (TradingView Lightweight Charts):
  - Candlestick + volume bars + EMA20 + EMA50 lines
  - Full width, height: 240px mobile / 320px desktop
  - Handles resize correctly
- [x] Create `OIChart.tsx` (Recharts bar): CE vs PE OI by strike
- [x] Create `ChainTable.tsx`:
  - All columns, ATM highlighted, ITM colored
  - OI mini bar in cell
  - Sticky header, horizontal scroll on mobile
- [x] Wire all to live data from API + SignalR

---

## Phase 3 — AI Signal Engine

### Backend
- [x] Create `ClaudeApiClient`:
  - POST to https://api.anthropic.com/v1/messages
  - Configurable model, max_tokens from appsettings
  - Deserialises response, extracts text content
  - Retries twice on 5xx, throws on 4xx
  - Calculates and returns token counts
- [x] Create `SignalCacheService`:
  - Hash = SHA256(symbol + rsi_bucket + macd_signal + pcr_bucket + spot_bucket)
  - spot_bucket = round to nearest 50. rsi_bucket = round to nearest 5. pcr_bucket = round to nearest 0.1
  - IMemoryCache with 5-min sliding TTL
- [x] Create `AISignalService`:
  - GenerateEntrySignal(symbol): calls Sonnet, returns SignalResponse as JSON
  - RunPositionRiskCheck(position, snapshot): calls Haiku, returns severity + message
  - System prompt: persona as NIFTY options expert, instructs JSON output
  - Signal JSON schema: { signalType, symbol, strike, optionType, expiry, entryLow, entryHigh, stopLoss, target1, target2, confidence, riskReward, rationale[], validUntil }
  - Risk check JSON schema: { severity, alertType, message, recommendedAction }
  - Enforces: no calls outside market hours, respects rate limit, checks cache first
- [x] Implement POST /api/v1/signals/generate, GET /api/v1/signals/history, GET /api/v1/signals/{id}
- [x] Save signal to DB with full market_snapshot JSONB
- [x] Log AI usage to AIUsageLog table on every call
- [x] Broadcast NewSignal via SignalR

### Frontend
- [x] Create `SignalCard.tsx`:
  - Header: signal type badge (STRONG BUY/BUY/SELL/WATCH) + confidence %
  - Contract: symbol + strike + CE/PE + expiry
  - Three boxes: Entry | Target | Stop Loss
  - Footer: R:R badge, IV badge, expiry badge, validity countdown
  - "Add to Position" button
  - Full-width on mobile, card on desktop
- [x] Create Signals section on Dashboard and standalone AI Analysis page:
  - "Generate Signal" button (Sonnet) with cost estimate shown
  - Quick check buttons (Haiku): Trend Check, OI Check
  - Loading state showing elapsed seconds
  - Signal history below
- [x] Show AI usage info (model used, approximate cost per call)

---

## Phase 4 — Position Tracker & Alerts (MOST CRITICAL)

### Backend
- [ ] Implement full Position CRUD: GET/POST/PUT/DELETE /api/v1/positions
- [ ] Create `PositionService`:
  - CalculatePnL: (currentLtp - entryPrice) * qty * lotSize, also in %
  - CalculateDistanceToSL: % and Rs. from SL
  - CalculateDistanceToTarget: % and Rs. from T1 and T2
  - GetThetaDecayPercent: estimate daily theta eaten of original premium
  - CheckAlertConditions: returns List<AlertTrigger> for all conditions met
- [ ] Alert conditions (ALL must be checked):
  1. ltp <= sl * 1.10 AND not already alerted -> WARNING: "SL Approaching — ₹X away"
  2. ltp <= sl -> DANGER: "EXIT NOW — Stop Loss Hit on {contract}"
  3. ltp >= target1 AND not alerted -> INFO: "Target 1 Reached — Book partial profits"
  4. ltp >= target2 AND not alerted -> INFO: "Target 2 Reached — Full target hit!"
  5. ivChange > 20% in last 30 min -> WARNING: "IV Spike detected — Premium inflating"
  6. indexMoved > 0.5% against trade in 15 min -> DANGER: "Sharp adverse move — Review position"
  7. thetaDecayPercent > 50 -> WARNING: "50% premium eroded by time decay"
- [ ] Create `AlertService`: SaveAlert, BroadcastAlert via SignalR
- [ ] Create `PositionMonitorWorker` (BackgroundService):
  - Runs every 60s during market hours
  - For each active position: get LTP, check conditions, fire alerts
  - Deduplicates: don't re-fire same alert type for same position within 15 min
- [ ] Implement GET/PUT /api/v1/alerts endpoints
- [ ] Implement GET /api/v1/positions/{id}/pnl

### Frontend
- [ ] Create `AlertBanner.tsx`:
  - DANGER: red full-width banner pinned top, cannot dismiss, pulses
  - WARNING: orange slide-in from top, auto-dismiss after 30s or on tap
  - INFO: green toast bottom-right, auto-dismiss 5s
  - Sound: use Web Audio API for DANGER alert (beep)
  - Badge count on BottomNav bell icon
- [ ] Create `PositionCard.tsx`:
  - Header: symbol + strike + CE/PE + expiry + status
  - P&L: current Rs. and % — green if positive, red if negative (live update)
  - SL Bar: colored progress bar (green > yellow > red as LTP approaches SL)
  - Target Bar: progress toward target
  - Greeks row: Theta/day, current IV
  - Alerts section: last 3 alerts for this position
  - Actions: Edit SL/Target, Close Position
- [ ] Create `AddPositionModal.tsx`:
  - If opened from SignalCard: pre-fills all fields
  - Manual: symbol, strike, CE/PE, expiry, entry price, lots, SL, target
  - Bottom sheet on mobile, modal on desktop
  - Validates: SL < entry for CE buy, target > entry for CE buy etc.
- [ ] Create `useAlerts.ts`: SignalR alerts:{userId} group + polling fallback every 30s
- [ ] Create `pushNotification.ts`: requestPermission, sendNotification
  - Request permission on first DANGER alert
  - Fire browser push for DANGER and WARNING when app is in background
- [ ] Positions page: card grid (1-col mobile, 2-col tablet, 3-col desktop)
- [ ] Position summary bar: total P&L, # positions, margin used

---

## Phase 5 — AI Chat Expert Partner

### Backend
- [ ] Create `ChatService`:
  - BuildSystemPrompt(userId): fetches active positions + current market snapshot
  - System prompt includes: expert persona, market context, user's positions, instructions to be proactive
  - SendMessage(userId, sessionId, message): calls Sonnet, saves messages, logs usage
  - GetHistory(sessionId): returns ordered messages
- [ ] Implement POST /api/v1/chat/message (streaming SSE)
- [ ] Implement GET /api/v1/chat/history, POST /api/v1/chat/new-session

### Frontend
- [ ] Create `AIChat.tsx` page:
  - Context bar: "NIFTY 24,198 ▲ 0.36% | 2 Active Positions | Market Open"
  - Chat bubbles: user (right, accent color), assistant (left, muted bg)
  - Streaming: assistant message types character by character
  - Typing indicator: 3-dot animation while waiting
  - Quick buttons row: "Analyze Market", "Check My Positions", "Best Strategy Today", "Tomorrow's Outlook"
  - Input: textarea + send button, Enter to send, Shift+Enter for newline
  - Auto-scroll to latest message
  - Token cost shown below each assistant message
  - Full screen mobile, side panel or full page desktop
- [ ] Create `useAIChat.ts`: manages session, history, streaming parsing

---

## Phase 6 — Backtesting Engine

### Backend
- [ ] Create `BacktestService`:
  - Uses 90 days mock OHLCV history from MockMarketDataService
  - Strategies: LongCall, LongPut, BullCallSpread, BearPutSpread, Straddle, Strangle, IronCondor
  - Entry signals: RSI_Oversold(30), RSI_Overbought(70), MACD_Bullish_Cross, MACD_Bearish_Cross, SupertrendBullish, SupertrendBearish, PriceBreakoutAboveR1, PriceBreakdownBelowS1
  - Exit signals: SLHit, Target1Hit, Target2Hit, ThetaDecay50Pct, ExpiryMinus1Day
  - Compute: winRate, totalPnL, maxDrawdown, sharpeRatio, profitFactor, avgWin, avgLoss, tradeLog
  - Save to backtest_results table
- [ ] Implement POST /api/v1/backtest/run, GET /api/v1/backtest/history

### Frontend
- [ ] Create `Backtest.tsx` page:
  - Form: strategy, entry condition, exit condition, period, lots
  - "Run Backtest" button with progress indicator
  - Stats cards: Win Rate, Net P&L, Max Drawdown, Sharpe Ratio, Profit Factor, Total Trades
  - Cumulative P&L line chart (Recharts)
  - Trade log table: date, contract, entry, exit, P&L, reason — paginated
  - Save/compare backtest results
  - Responsive: cards stack on mobile

---

## Phase 7 — Strategy Builder

### Backend
- [ ] Add POST /api/v1/options/payoff:
  - Input: legs[] with { symbol, strike, optionType, action(buy/sell), lots, premium }
  - Output: payoffCurve[] (price points + net P&L), maxProfit, maxLoss, breakevens[]

### Frontend
- [ ] Create `StrategyBuilder` section (on Chain page or standalone):
  - "Add Leg" button → row with: CE/PE toggle, strike picker, Buy/Sell toggle, lots, premium (auto from chain)
  - Payoff diagram updates live as legs change (Recharts AreaChart, positive green, negative red)
  - Summary: Max Profit / Max Loss / Breakeven / Greeks
  - "Analyze with AI" → opens Chat with strategy pre-loaded as context
  - "Track This" → creates positions for all legs in Position Tracker

---

## Phase 8 — Auth & Polish

### Backend
- [ ] Create AuthService: register (bcrypt hash), login (verify + issue JWT), refresh
- [ ] Create JwtService: issue 15-min access + 7-day refresh tokens
- [ ] Protect all endpoints with [Authorize] except /auth/* and /health
- [ ] Add RateLimitingMiddleware: 10 AI calls per userId per hour, 429 response
- [ ] Add ErrorHandlingMiddleware: catch all unhandled exceptions, return ProblemDetails
- [ ] Add FluentValidation to all Request models
- [ ] Add GET /health endpoint

### Frontend
- [ ] Create Login.tsx page: email + password form, responsive
- [ ] Store JWT in memory (not localStorage) — use React context
- [ ] Axios interceptors: attach Bearer token, 401 → refresh, 429 → show limit message
- [ ] Add PWA: manifest.json (icon, name, theme), service worker for offline shell
- [ ] Add offline indicator: shows when SignalR is disconnected (yellow bar)
- [ ] Final responsive QA at 375px (iPhone SE), 768px (iPad), 1280px (MacBook)
- [ ] Loading skeletons on all data cards (not spinners)
- [ ] Error boundaries on all pages
- [ ] Keyboard navigation and ARIA labels on interactive elements

---

## Phase 9 — Multi-User Billing Prep

### Backend (future-proof now, activate later)
- [ ] Verify all DB writes include userId from JWT claim
- [ ] Verify AIUsageLog populated on every AI call
- [ ] Create GET /api/v1/usage/stats → { callsToday, callsLimit, costToday, walletBalance }
- [ ] Add plan-based limit check before every AI call
- [ ] Stub POST /api/v1/billing/topup (placeholder, no Razorpay yet)

### Frontend
- [ ] Create UsageDashboard component: calls today / limit, cost today, wallet balance
- [ ] Show on profile/settings page
- [ ] When daily limit hit: clear "You've used X/10 free calls today. Upgrade for more."

---

## Phase 10 — Final QA

- [ ] Test all 7 alert conditions manually (add mock position, simulate LTP changes)
- [ ] Test SignalR reconnection (disable/enable network)
- [ ] Test on iPhone Chrome at 375px
- [ ] Test on iPad at 768px
- [ ] Test on MacBook at 1280px and 1440px
- [ ] Test market hours logic (mock time as 9:14, 9:16, 15:29, 15:31, Saturday)
- [ ] Verify AI cache works (2 rapid calls → 1 API call, 1 cache hit)
- [ ] Verify rate limiter blocks at call 11 in same hour
- [ ] Write README.md with full local setup instructions
- [ ] Verify zero secrets in git with: `git log --all -p | grep -i "sk-ant\|password\|secret"`
- [ ] Final `dotnet build` zero warnings
- [ ] Final `npm run build` zero errors

---

## Claude Code Usage Instructions

### Starting a new phase:
"Read CLAUDE.md first. Then read ARCHITECTURE.md. Then complete all unchecked tasks in Phase X of TASKS.md. Follow all coding conventions in CLAUDE.md. After completing each task, mark it as done with [x]."

### After each phase:
- Run `dotnet build` — fix all warnings before moving on
- Run `npm run build` — fix all TypeScript errors
- Commit: `git commit -m "Phase X complete: [brief description]"`

### Keep sessions focused:
- One phase per Claude Code session ideally
- If a phase is large, split into backend session + frontend session
- Always start by saying "Read CLAUDE.md" to load context lean
