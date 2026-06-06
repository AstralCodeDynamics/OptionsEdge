# OptionsEdge — Product Specification

## 1. Product Vision

OptionsEdge is a personal NIFTY/BANKNIFTY options trading assistant that acts as an **expert trading partner**. It actively monitors the market, generates AI-powered signals with exact entry/exit/stop-loss levels, watches your open positions in real time, and alerts you immediately when risk conditions change. The user trades manually on Groww based on the signals.

**Core Promise:** The app never goes silent. It watches the market while you work and speaks up when it matters.

---

## 2. User Personas

### Phase 1 — Personal (Current)
- Single user (you), trading NIFTY/BANKNIFTY weekly options on Groww
- Uses Mac, wants mobile alerts too
- Wants expert-level signals without manually watching charts all day

### Phase 2 — Multi-user SaaS (Future)
- Other retail traders on subscription plans
- Per-user AI call billing tracked and deducted from wallet
- Hosted on E2E Networks

---

## 3. Core Features

### 3.1 Live Market Dashboard
- Real-time NIFTY and BANKNIFTY spot price (mock data Phase 1, NSE public feed Phase 2)
- India VIX, PCR (Put-Call Ratio), Max Pain
- FII/DII flow data
- Advance/Decline ratio
- Auto-refreshes every 30 seconds during market hours (9:15 AM – 3:30 PM IST)
- Outside market hours: shows last close data with clear "Market Closed" indicator

### 3.2 Technical Indicators Engine
Computed server-side using Skender.Stock.Indicators on 15-min OHLCV candle data:
- RSI (14) with overbought/oversold zones
- MACD (12,26,9) with signal line crossover detection
- Bollinger Bands (20,2) with squeeze detection
- ADX (14) for trend strength
- EMA 9, 20, 50, 200
- Supertrend (10,3)
- Pivot Points (Classic, Camarilla) — daily S1/S2/S3, R1/R2/R3
- Volume analysis — spike detection

### 3.3 Options Chain Viewer
- Full CE/PE chain for selected expiry (weekly + monthly)
- Columns: OI, Change in OI, Volume, IV, LTP, Delta, Gamma, Theta, Vega
- Color-coded: ITM (highlighted), ATM (bold), OTM (normal)
- OI bar charts alongside strike prices for visual wall identification
- Max Pain calculation displayed prominently
- PCR per strike and overall
- Expiry selector: current weekly, next weekly, monthly
- Auto-highlights strikes with unusual OI buildup (potential support/resistance)

### 3.4 AI Signal Engine (Expert Partner)
The core intelligence. Uses Claude API with a carefully engineered system prompt.

#### Signal Types
**Entry Signals**
- Contract: exact strike + expiry (e.g., NIFTY 24200 CE 12-Jun-2025)
- Entry price range (e.g., Rs.78-85)
- Stop loss price
- Target 1 and Target 2
- Risk:Reward ratio
- Confidence score (%)
- Rationale (bullet points, max 5)
- Signal validity window

**Position Monitoring Signals (CRITICAL FEATURE)**
- When user marks a trade as "Active":
  - App continuously monitors price vs stop loss
  - Monitors IV change (sudden spike = danger)
  - Monitors index movement vs trade direction
  - Danger condition triggers -> immediate alert
  - Alert types: WARNING (approaching SL), DANGER (exit now), INFO (target near)

**Market Condition Signals**
- Gap-up/gap-down open alerts at 9:15 AM
- Trend reversal detection mid-session
- Unusual OI buildup at a strike (whale activity)
- VIX spike alert

#### AI Model Strategy (Cost Optimised)
- Haiku (cheap, fast): position risk checks every 5 min, quick market status, OI alerts
- Sonnet (smarter): deep analysis when user explicitly requests, new entry signals
- Response caching: same market snapshot hash -> return cached signal (5-min TTL)
- No AI calls outside market hours

### 3.5 Active Position Tracker
User manually enters trades placed on Groww:
- Symbol, Strike, CE/PE, Expiry, Buy price, Quantity (lots)
- Stop loss and Target pre-filled from signal

App tracks in real time:
- Current LTP vs entry -> current P&L
- Distance to stop loss (% and Rs.)
- Distance to target
- Theta decay impact per day
- IV change impact

**Alert Triggers (immediate push notification + in-app banner):**
- Price within 10% of SL -> WARNING
- Price hits SL -> DANGER: EXIT NOW
- Price reaches Target 1 -> INFO: Book partial profits
- IV spikes >20% suddenly -> WARNING: Premium inflating
- Index moves sharply opposite to trade -> DANGER: Check position
- Theta decay >50% of remaining premium -> WARNING

### 3.6 Backtesting Engine
- Strategies: Long Call, Long Put, Bull Call Spread, Bear Put Spread, Straddle, Strangle, Iron Condor
- Entry conditions: RSI crossover, MACD signal, Breakout, Supertrend flip
- Exit conditions: SL hit, Target hit, Theta decay %, Time-based
- Period: 1 month, 3 months, 6 months, 1 year
- Results: Win rate, Total P&L, Max drawdown, Sharpe ratio, Profit factor, Avg win/loss

### 3.7 Strategy Builder
- Visual options strategy constructor with payoff diagram
- Add multiple legs (Buy/Sell CE/PE at various strikes)
- Breakeven points, max profit, max loss shown live
- One-click: send strategy to AI for analysis
- One-click: add all legs to position tracker

### 3.8 Notifications & Alerts
- In-app banners (critical always visible)
- Browser push notifications (background)
- Future: WhatsApp via Twilio, Email via SendGrid
- Alert history log with timestamps

### 3.9 AI Chat (Expert Partner)
- Persistent chat with Claude as trading expert
- Context-aware: knows open positions + current market snapshot
- Conversation history per session
- Quick-action buttons: "Analyze Now", "Check My Positions", "Market Outlook"

### 3.10 User Account & Billing (Phase 2)
- JWT auth (email + password, future: Google OAuth)
- Wallet balance system
- Per-AI-call cost deduction -> AIUsageLogs
- Plans: Free (10 calls/day) / Starter Rs.299/mo / Pro Rs.799/mo / Elite Rs.1999/mo
- Razorpay payment integration
- Usage dashboard

---

## 4. Non-Functional Requirements

### Performance
- Dashboard loads < 2 seconds
- AI signals return < 5s (Haiku), < 10s (Sonnet)
- SignalR price updates latency < 500ms
- API P95 < 300ms (non-AI endpoints)

### Responsiveness
- Mobile (320-767px): single column, bottom nav, swipeable panels
- Tablet (768-1199px): two-column, collapsible side nav
- Desktop (1200px+): three-column dashboard, full features
- All charts resize on orientation change
- Touch targets minimum 44px

### Reliability
- Graceful degradation: AI down -> show cached signal with timestamp
- Market data failover: stale indicator shown if data is old
- SignalR auto-reconnect on disconnect

### Security
- JWT: 15-min access token, 7-day refresh token
- API keys in environment variables only, never in code
- CORS: known origins only
- Rate limiting on all endpoints
- FluentValidation on all API inputs
- HTTPS only in production

### Cost Efficiency
- AI cache: 5-min TTL keyed on market snapshot hash
- Haiku for all background/automated checks
- Sonnet only for user-triggered deep analysis
- Batch all indicator calculations per refresh cycle
- PostgreSQL connection pooling via Npgsql
- Frontend: lazy-load charts, code-split by route

---

## 5. Market Hours Behaviour

| Time (IST) | Behaviour |
|---|---|
| 9:00-9:15 AM | Pre-market: show previous close, prepare gap analysis |
| 9:15 AM | Market open alert, gap-up/down signal generated |
| 9:15 AM-3:30 PM | Full live mode: all signals, monitoring, alerts active |
| 3:30 PM | Market close summary: day's best signals, P&L summary |
| After 3:30 PM | Offline mode: EOD data, backtesting allowed, no AI market calls |

---

## 6. Out of Scope (Phase 1)
- Actual order placement (no broker API)
- Real NSE data feed (mock data only)
- Native mobile app (PWA instead)
- WhatsApp/Email notifications
- Multi-user accounts
- Payment processing

---

## 7. Success Criteria
1. Correctly identifies trend direction before a significant NIFTY move
2. Alerts user to exit bad position before SL is hit
3. Signal entry/target/SL is specific enough to act on immediately
4. Usable on phone during the day to check market at a glance
5. Saves time vs manually watching charts all day
