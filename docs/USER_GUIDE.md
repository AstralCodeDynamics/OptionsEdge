# OptionsEdge User Guide

**Guide version:** 19 June 2026  
**Audience:** First-time traders, active traders, product owners, trainers, and OptionsEdge users  
**Application:** `https://optionsedge.astralcode.in`

**Guide goal:** Explain both trading meaning and application behavior, so a beginner can learn each concept and a non-trader can confidently demonstrate the product.

## Contents

- [Account Registration and Login](#5-account-registration-and-login)
- [First-Time Setup](#6-recommended-first-time-setup)
- [Beginner Trading Foundations](#beginner-trading-foundations)
- [Dashboard](#7-dashboard-module)
- [AI Signals](#8-ai-signals-module)
- [Signal History](#9-signal-history)
- [Positions](#10-positions-module)
- [Risk Monitoring and Alerts](#11-position-risk-monitoring-and-alerts)
- [Notification History](#12-notification-history)
- [Options Chain](#13-options-chain-module)
- [Strategy Builder](#14-strategy-builder-module)
- [Backtest](#15-backtest-module)
- [AI Chat](#16-ai-chat-module)
- [Security Settings](#17-security-settings-module)
- [Presenter Guide](#presenter-guide-explain-optionsedge-to-others)
- [Troubleshooting](#19-troubleshooting)
- [Current Limitations](#20-current-limitations-and-features-not-yet-available)
- [Glossary](#21-quick-glossary)

## 1. What OptionsEdge Does

OptionsEdge is a trading-assistance system for NIFTY and BANKNIFTY options. It brings market data, technical indicators, AI-generated trade ideas, position tracking, risk alerts, strategy analysis, and backtesting into one place.

OptionsEdge helps you answer practical questions such as:

- Is the market trending or moving sideways?
- Is NIFTY or BANKNIFTY near an important support or resistance level?
- Which option contract, entry range, stop loss, and target does the AI suggest?
- Is an open position getting dangerously close to its stop loss?
- How would a multi-leg strategy behave at expiry?
- How did a strategy perform on historical market candles?
- What does the AI think about my current positions?

OptionsEdge is an assistant, not a promise of profit. AI confidence, technical indicators, options-chain values, payoff calculations, and backtests can all be wrong or incomplete. Always verify the contract, expiry, premium, quantity, available margin, and current market price before trading.

> **Important:** OptionsEdge is for educational and decision-support purposes. It is not financial advice. A signal is not an instruction to trade. You remain responsible for every order and every risk decision.

## 2. How the System Works

OptionsEdge has four main layers from a user's point of view:

1. **Market understanding:** Dashboard, chart, indicators, pivot levels, VIX, PCR, and options chain.
2. **Decision support:** AI signals, AI Chat, Strategy Builder, and Backtest.
3. **Trade tracking:** Positions, P&L, stop-loss and target progress, and position alerts.
4. **Account and connections:** Login security, Anthropic AI key, Groww connection, automatic signal schedules, and usage information.

### 2.1 Live data versus estimated data

Look for the data-source label throughout the app:

- **LIVE / LIVE LTP:** Groww-backed market data is available.
- **SIMULATED:** Groww integration is disabled and the system is using simulated market data.
- **Loading:** Groww is enabled, but a live response has not yet arrived.
- **EST. LTP:** Option premium is estimated, usually with an options-pricing model, rather than confirmed from a current broker quote.

Live underlying-index data does not guarantee that every option value on screen is live. Option-chain values can be a mix of Groww data and estimates when Groww omits a strike or field. Greeks are always model-calculated.

### 2.2 Market hours

Market-aware features use Indian Standard Time and treat normal market hours as **9:15 AM to 3:30 PM, Monday to Friday**.

- Manual AI signal generation is blocked outside these hours.
- AI Chat is blocked outside these hours.
- Automatic signals run only during market hours.
- Position risk monitoring runs only during market hours.
- Dashboard and historical pages remain available outside market hours.
- Backtesting can be used outside market hours.

The system checks weekdays, but it does not contain a complete exchange-holiday calendar. On an exchange holiday that falls on a weekday, the app may initially treat the time as market hours even when NSE is closed.

## 3. Before You Start

For the full experience, prepare:

- A working email address.
- A strong OptionsEdge password.
- An authenticator app if you want two-factor authentication.
- Your own Anthropic API key for AI signals and AI Chat.
- Optional Groww Trade API TOTP credentials for live data, broker position import, and risk monitoring.

Live order placement may still be unavailable even when Groww is connected. It requires OptionsEdge to run from a production server whose static IP is whitelisted in Groww.

## 4. Navigation

### 4.1 Desktop navigation

Desktop sidebar contains:

- **Dashboard** — live market overview and AI signal generation.
- **Positions** — tracked positions and P&L.
- **Notifications** — full alert history.
- **Chain** — options chain and Strategy Builder.
- **Backtest** — historical strategy simulation.
- **AI Chat** — context-aware market conversation.
- **Security** — usage, 2FA, AI key, auto signals, password, and account details.

User name, email, and **Log out** appear at the bottom. When Groww integration is enabled, a Groww status badge appears near the OptionsEdge logo. Select that badge to connect, inspect, or disconnect Groww.

### 4.2 Mobile navigation

Bottom navigation contains Home, Positions, Chain, Backtest, and Chat. Use the menu button in the top-left corner for Notifications, Security, and other sidebar destinations.

The red badge over **Positions** represents unread alert count. Full notification history is available through the menu.

### 4.3 Market and connection status

The mobile header can show:

- Groww status.
- Market Open or Market Closed.
- User account menu.

The Dashboard also shows connection status:

- **Market Open / Market Closed** — exchange-hours status.
- **Closes in / Opens in** — next market event estimate.
- **Reconnecting** — live update channel is reconnecting.
- **Offline** — real-time updates are disconnected. Normal API pages may still work.

## 5. Account Registration and Login

### 5.1 Create an account

1. Open OptionsEdge and select **Create account**.
2. Enter your display name.
3. Enter a valid email address.
4. Create a password that meets every rule:
   - At least 8 characters.
   - At least one uppercase letter.
   - At least one lowercase letter.
   - At least one digit.
   - At least one special character.
5. Enter the same password again.
6. Select **Create account**.
7. Open the confirmation email and select its verification link.
8. If no email arrives, check spam or junk, then use **Resend email**.

The verification link is presented to users as valid for 24 hours. If it fails or expires, request another confirmation email.

### 5.2 Log in

1. Enter your email and password.
2. Select **Log in**.
3. If 2FA is enabled, enter the six-digit authenticator code.
4. You can use one saved recovery code instead if the authenticator is unavailable.

After five failed password attempts, the account can be locked for 15 minutes. Wait, then try again carefully.

### 5.3 Session behavior

OptionsEdge keeps the short-lived access token only in browser memory. A secure, unreadable refresh cookie keeps the session alive and restores it after a normal page reload. The access token lasts about 60 minutes and is refreshed automatically. The refresh session lasts up to seven days unless revoked, expired, or cleared.

If the session cannot be refreshed, the app returns to Login. Logging out revokes the current refresh session and clears the browser cookie.

### 5.4 Forgot password

1. Select **Forgot password?** on Login.
2. Enter your email.
3. Select **Send reset link**.
4. Check inbox and spam folders.
5. Open the reset link.
6. Enter and confirm a new password that meets all password rules.
7. Select **Reset password** and log in again.

For privacy, the app gives the same response whether or not an account exists for an entered email.

## 6. Recommended First-Time Setup

Complete these steps after your first login.

### 6.1 Add your Anthropic AI key

**What is this key?** Anthropic is external company providing Claude AI model. API key identifies user's Anthropic account when OptionsEdge requests analysis. It is similar to a private service password and must never be shared publicly.

**Why is it required?** OptionsEdge does not use one common paid AI account for every user. Each user connects own key, sees own Anthropic charges, and can disconnect access.

1. Open **Security**.
2. Find **AI Connection (Anthropic)**.
3. Get an API key from `console.anthropic.com`.
4. Paste a key beginning with `sk-ant-`.
5. Select **Connect AI Key**.
6. Wait for **AI Key Connected**.

OptionsEdge validates the key with a very small test request before saving it. The key is encrypted when stored. AI usage is billed directly by Anthropic; OptionsEdge does not store Anthropic payment details.

Disconnecting the key disables new AI signals and AI Chat. Existing saved history remains visible.

### 6.2 Connect Groww, if enabled

**What is broker connection?** Groww is broker where real orders and holdings exist. Connecting Trade API credentials lets OptionsEdge request live market information, inspect broker positions for import, and—only when separately enabled—submit an order after confirmation.

**Why use it?** Without broker connection, OptionsEdge may use simulated index data and estimated option premiums. That is useful for learning but not enough for reliable live risk monitoring. Groww connection improves data and lets system associate analysis with real trading environment.

Groww connection is opened from the **Groww status badge** in the desktop sidebar or mobile header.

1. Sign in at `groww.in/trade-api/api-keys`.
2. Generate a Trade API key using TOTP authentication.
3. Copy both values while Groww displays them:
   - TOTP Token, also called API Key.
   - TOTP Secret, also called API Secret.
4. Open the Groww badge in OptionsEdge.
5. Paste both values.
6. Select **Connect Groww**.
7. Confirm the status changes to **Groww Connected**.

Credentials are tested before being saved and are encrypted at rest. Groww authentication tokens are renewed automatically, normally around the daily 6:00 AM IST boundary.

On the first successful authentication after a token refresh, OptionsEdge attempts to import open Groww F&O option positions that are not already tracked.

If the server administrator has disabled Groww, the modal shows that integration is unavailable. Simulated or estimated values may still let you explore most screens.

## Beginner Trading Foundations

Read this section before using trading modules. It supplies the basic language needed to understand and explain OptionsEdge.

### What are NIFTY and BANKNIFTY?

NIFTY and BANKNIFTY are market indexes. An index is a calculated number representing a group of shares rather than one company.

- **NIFTY 50** represents 50 major companies listed on NSE.
- **BANKNIFTY** represents a group of major banking shares.

When someone says “NIFTY is at 24,000,” it means the calculated index value is near 24,000 points. You cannot buy one unit of the index in the same way you buy one company share. Traders use products such as futures and options to trade its movement.

### What is an option?

An option is a time-limited contract whose value depends mainly on an underlying asset such as NIFTY or BANKNIFTY.

- **Call option or CE:** generally gains value when the underlying rises, all else equal.
- **Put option or PE:** generally gains value when the underlying falls, all else equal.

“All else equal” matters. Option price also changes because of time remaining, volatility, strike distance, demand, supply, and liquidity. NIFTY can move in the expected direction while an option still performs poorly if the move is too small or too late.

### What makes one option contract different from another?

An option contract is identified by four main details:

1. **Underlying:** NIFTY or BANKNIFTY.
2. **Strike:** reference level, such as 24,000.
3. **Type:** CE or PE.
4. **Expiry:** date when the contract ends.

Example: **NIFTY 24000 CE, 23 June** is different from NIFTY 24100 CE, NIFTY 24000 PE, and NIFTY 24000 CE with another expiry.

Always verify all four details. Choosing the right direction but the wrong expiry or strike means buying a different trade.

### What is option premium?

Premium is the market price of one option unit. If premium is ₹100 and lot size is 65, one lot costs approximately:

`₹100 × 65 = ₹6,500`

This excludes brokerage, exchange charges, taxes, and slippage. OptionsEdge often displays premium as Entry, LTP, Stop Loss, and Target values.

### What is lot size?

Index options trade in fixed bundles called lots. Quantity shown as “1 lot” does not mean one option unit.

At this guide's date:

- One NIFTY lot uses 65 units.
- One BANKNIFTY lot uses 30 units.

Lot sizes can change through exchange rules. Confirm current quantity in Groww before ordering.

### What are ATM, ITM, and OTM?

These terms describe strike location relative to current index price.

- **ATM, At the Money:** strike nearest current index price.
- **ITM, In the Money:** option already has intrinsic value.
- **OTM, Out of the Money:** option has no intrinsic value at current spot.

If NIFTY is at 24,020, 24,000 is approximately ATM. For calls, lower strikes are more ITM and higher strikes more OTM. For puts, higher strikes are more ITM and lower strikes more OTM.

ATM options are commonly used because they often balance responsiveness and trading activity. OTM options look cheaper but can lose their full premium quickly.

### What is expiry and why does it matter?

Every option has an expiry. After expiry, the contract no longer trades. As expiry approaches, time value usually reduces faster. This is called time decay.

Current OptionsEdge rules use:

- NIFTY weekly expiry on Tuesday.
- BANKNIFTY monthly expiry on the last Tuesday of the month.

Never assume an expiry from memory. Exchange holidays and regulatory changes can shift dates. Verify with Groww or NSE.

### What is a trade plan?

A trade plan defines what must happen before entry and what you will do afterward.

- **Entry:** price or range where trade may be opened.
- **Stop Loss:** planned exit when trade moves against you.
- **Target 1:** first profit objective.
- **Target 2:** optional second objective.
- **Quantity:** number of lots, which controls rupee risk.
- **Validity:** how long setup remains meaningful.

Example:

- Buy NIFTY 24000 CE between ₹100 and ₹110.
- Stop Loss ₹70.
- Target 1 ₹150.
- One lot.

If entry occurs at ₹110, premium risk to stop is ₹40 per unit. With lot size 65, planned rupee risk is approximately ₹2,600 before costs and slippage.

### What is the difference between an order and a position?

- **Order:** instruction sent to broker, such as “buy one lot at ₹110.”
- **Position:** exposure that exists after an order is filled.

An unfilled order is not a position. A partially filled order creates a smaller position than requested. OptionsEdge's local tracker is a record of a position; it is not proof that Groww executed an order.

### What is P&L?

P&L means profit and loss.

For a bought option, simplified P&L is:

`(current premium − entry premium) × lots × lot size`

Example: entry ₹100, current premium ₹120, one NIFTY lot of 65:

`(₹120 − ₹100) × 65 = ₹1,300 profit`

If current premium is ₹80, same calculation gives ₹1,300 loss. Charges and slippage reduce actual result.

### What is risk management?

Risk management decides how much can be lost before placing a trade. Good analysis without risk control can still destroy capital.

Core questions:

- How much money can I lose on this trade?
- Where is the idea proven wrong?
- How many lots fit that maximum loss?
- Is expected reward worth that risk?
- Can I exit quickly if market moves sharply?

OptionsEdge supports these decisions with stop-loss values, targets, P&L, progress bars, alerts, and payoff diagrams. It cannot guarantee execution at your chosen exit price.

### How all trading modules connect

OptionsEdge modules represent different stages of one trading process:

1. **Dashboard — understand market:** User checks whether data is live, market is open, trend is bullish or bearish, volatility is high or low, and price is near support or resistance.
2. **Options Chain — choose contract:** User compares strikes, CE versus PE, premium, OI, IV, and expiry.
3. **AI Signal — create trade plan:** System proposes contract, entry range, stop loss, targets, reasons, and validity.
4. **Groww — execute actual trade:** User verifies real quote, quantity, margin, and places broker order. A position exists only after fill.
5. **Positions — record and follow trade:** User records actual entry, lots, stop, and targets. App calculates local P&L.
6. **Risk Alerts — react to change:** System warns when tracked position approaches stop, reaches target, or faces another monitored risk.
7. **Notification and Signal History — review:** User studies what system said and when alerts occurred.
8. **Backtest and Strategy Builder — improve:** User tests rule ideas and understands payoff before another trade.
9. **AI Chat — explain and compare:** User asks questions about current market, open positions, or strategy choices.

### One complete beginner trade journey

Assume NIFTY is near 24,020 during market hours.

1. Dashboard shows NIFTY rising, price above EMA20, moderate ADX, and spot below R1.
2. User opens Chain and sees 24,000 as ATM.
3. AI Signal suggests NIFTY 24000 CE with entry ₹100–₹110, SL ₹70, T1 ₹150, valid until 3:30 PM.
4. User opens Groww and confirms correct expiry, live premium ₹105, acceptable spread, and lot size.
5. User places one-lot order. It fills at ₹106, not ₹105.
6. User records Position with entry ₹106, one lot, SL ₹70, and T1 ₹150.
7. Premium rises to ₹130. Position module shows approximate P&L:

   `(₹130 − ₹106) × 65 = ₹1,560`

8. Later premium falls. At a value close to stop, OptionsEdge sends SL Approaching warning.
9. If user exits in Groww at ₹74, actual trade is closed. User then closes local Position record.
10. After market, user reviews Notification History and Signal History to understand timeline.

This example shows an important distinction: OptionsEdge supports analysis and tracking, while Groww performs actual execution.

### Signal, position, and alert are different things

- **Signal:** idea for a possible trade that may never be taken.
- **Order:** request sent to broker.
- **Position:** exposure created after order fills.
- **Alert:** message about a tracked position or risk condition.

A signal does not automatically become a position. A position does not automatically close because an alert appeared.

### A simple explanation of OptionsEdge

Use this when someone asks what product does:

> “OptionsEdge watches NIFTY and BANKNIFTY, explains market conditions, creates AI-assisted trade plans, helps record open option trades, warns when risk conditions occur, and lets users study strategies before risking money. It supports decisions; user still verifies and executes trades.”

## 7. Dashboard Module

**What is a trading dashboard?** A trading dashboard is one screen combining current market price, recent price movement, volatility, technical indicators, and important levels. It is similar to a car dashboard: each gauge answers one question, but safe decisions come from reading them together.

**Why use it?** Opening several websites and charts can make a beginner react to isolated numbers. Dashboard gives one structured starting point: Is market open? Is data live? Is price rising or falling? Is trend strong? Is volatility high? Is price near support or resistance? Only after answering these should user consider a trade.

**How OptionsEdge helps.** It brings NIFTY and BANKNIFTY into same view, marks LIVE versus SIMULATED data, plots candles, computes indicators, displays pivot levels, and provides AI signal entry point. It reduces manual calculation; it does not replace interpretation.

**Beginner example.** Suppose NIFTY is green, but RSI is already overbought, price is near R1 resistance, and VIX is high. Green index card alone may look bullish. Full Dashboard says move may be extended and risky to chase. Best response may be waiting for confirmation rather than immediately buying CE.

**Explain it in 30 seconds:**

> “Dashboard is market overview. It shows where NIFTY and BANKNIFTY are, how they moved, whether data is live, what trend and momentum indicators say, and where support or resistance may be. It helps user understand context before asking AI for a trade.”

Dashboard is the main market workspace.

### 7.1 NIFTY and BANKNIFTY cards

Each index card shows:

- Current index value.
- Point change and percentage change.
- Day open, high, and low.
- India VIX value.
- Data source: LIVE, SIMULATED, or Loading.

Green values indicate a positive move; red values indicate a negative move. This is only direction, not a buy or sell recommendation.

### 7.2 Select an index

Use **NIFTY** or **BANKNIFTY** buttons to change the active analysis. The price chart, indicators, pivots, generated signal, and signal button follow this selection.

### 7.3 Price chart

The chart displays historical index candles. Use it to inspect direction, range, volatility, and interaction with moving averages. It is an underlying-index chart, not an option-premium chart.

When live Groww data is available, candles are refreshed from Groww. Otherwise, the app can show simulated candles.

### 7.4 Technical Indicators

**What is technical analysis?** Technical analysis studies price, momentum, trend, volatility, and volume patterns. An indicator is a formula applied to historical candles. It summarises one aspect of market; it does not know future.

**Why use several indicators?** RSI may say market is overbought while ADX says trend is strong. That can mean price is stretched but still trending. One indicator alone can mislead. Dashboard groups indicators so user can look for agreement, disagreement, and context.

**Simple reading order for beginners:**

1. Determine direction with price, EMA, and SuperTrend.
2. Check trend strength with ADX.
3. Check momentum stretch with RSI.
4. Check momentum change with MACD.
5. Check volatility compression with Bollinger Bands.
6. Compare price with Pivot, R1, and S1.

OptionsEdge calculates indicators from index candles.

### RSI (14)

RSI estimates recent momentum on a 0–100 scale.

- **Oversold:** 30 or below. Selling may be stretched, but price can continue falling.
- **Neutral:** between 30 and 70.
- **Overbought:** 70 or above. Buying may be stretched, but price can continue rising.

### MACD (12, 26, 9)

MACD compares fast and slow momentum.

- MACD above its signal line is displayed as bullish.
- MACD below its signal line is displayed as bearish.
- **CROSS** means a fresh bullish crossover was detected.
- Histogram shows the gap between MACD and its signal line.

### Bollinger Bands (20, 2)

Upper, middle, and lower bands describe recent price range and volatility.

- **NORMAL:** no narrow-band squeeze detected.
- **BREAKOUT:** the app detected a narrow-band squeeze. This means expansion may follow; it does not predict direction.

### ADX (14)

ADX describes trend strength, not direction.

- Below 20: Weak.
- 20 through 40: Moderate.
- Above 40: Strong.

A strong ADX can describe either a strong rise or a strong fall.

### EMA Cloud

The app shows EMA 9, 20, 50, and 200.

- Price above EMA20 and EMA50 is labelled BUY.
- Price below both is labelled SELL.
- Mixed positioning is labelled neutral.

Treat these labels as indicator summaries, not complete trade decisions.

### SuperTrend (10, 3)

- **BUY / Uptrend:** SuperTrend is bullish.
- **SELL / Downtrend:** SuperTrend is bearish.

### 7.5 Pivot Levels

Pivot levels are calculated from the most recent completed trading day.

- **R1, R2, R3:** possible resistance zones.
- **Pivot:** central reference level.
- **S1, S2, S3:** possible support zones.
- **SPOT:** current index position within the ladder.

Price can cross any level. Use pivots together with momentum, trend, and risk controls.

### 7.6 Market Pulse

### India VIX

The app labels:

- Below 12: Low volatility.
- 12 through 16: Moderate volatility.
- Above 16: High volatility.

Higher VIX often means larger expected moves and more expensive options.

### PCR

PCR is put open interest divided by call open interest. The current UI labels:

- Below 0.9: Bullish.
- 0.9 through 1.2: Neutral.
- Above 1.2: Bearish.

PCR interpretation depends on context. High put OI can also represent support or hedging, so never use this label alone.

### FII and DII flow

Groww does not provide FII/DII cash-market flow through this integration. When live Groww data is active, these values display **N/A**. Simulated mode may show sample flow values; do not treat those as real institutional data.

## 8. AI Signals Module

**What is a trading signal?** A trading signal is a structured trade idea created from market conditions. It normally states direction, exact contract, entry range, stop loss, targets, confidence, and time validity. Signal is a proposal, not a guaranteed outcome or automatic command.

**Why use it?** Beginners often know they feel bullish or bearish but do not know which strike to select, how much premium to pay, where to exit a losing trade, or when idea becomes stale. A structured signal forces these decisions to be written before entry.

**How OptionsEdge helps.** It sends current market snapshot, indicators, available expiries, and nearby option premiums to AI. Returned idea is checked for extreme strike selection and shown as a consistent card. This gives user a starting trade plan instead of a vague “market may rise” opinion.

**Beginner example.** “NIFTY looks bullish” is not actionable. “Watch NIFTY 24000 CE, enter ₹100–₹110, Stop Loss ₹70, Target ₹150, valid until market close” is actionable because user can compare live Groww price with every condition. User can reject it when live premium is ₹145 because entry opportunity has already passed.

**Why signal may say WATCH instead of ENTRY.** AI can identify direction without seeing enough confirmation. WATCH means conditions are interesting but immediate trade may have poor timing. Doing nothing is a valid trading decision.

**Explain it in 30 seconds:**

> “AI Signals converts market data into a complete trade idea: contract, entry, stop, target, confidence, reasons, and expiry time. User checks it against live Groww data and decides whether to act. Signal never guarantees profit.”

### 8.1 Generate a signal

1. Connect an Anthropic AI key.
2. Wait until market hours.
3. Select NIFTY or BANKNIFTY on Dashboard.
4. Review current chart and indicators.
5. Select **Generate NIFTY Signal** or **Generate BANKNIFTY Signal**.
6. Wait while the elapsed-seconds counter runs.
7. Read the full signal before deciding whether to trade.

The app uses Claude Sonnet for signal generation. A normal request uses current spot, VIX, PCR, indicators, available expiries, and nearby option premiums. A visible cost estimate is approximate; actual token cost is shown on the returned card.

### 8.2 Signal types

- **ENTRY:** possible new trade setup.
- **WATCH:** setup is not ready; monitor conditions.
- **HOLD:** maintain an existing directional view or wait.
- **EXIT:** close or avoid the setup.

The type comes from AI output. Apply your own judgement before acting.

### 8.3 Read a signal card

Each card includes:

- Contract: symbol, strike, CE or PE, and expiry.
- Entry range: preferred option-premium buying range.
- Target 1 and optional Target 2.
- Stop loss.
- Confidence percentage.
- Risk-to-reward ratio.
- Technical rationale.
- Model and token cost.
- Creation time and validity countdown.

### Confidence

Confidence is the AI's assessment, usually constrained to 55–85%. It is not a measured probability of profit.

### Risk-to-reward

R:R compares expected reward with risk between entry and stop. Example: 1.8x means the planned reward is about 1.8 times the planned risk. Slippage, gaps, and changing premiums can make actual results different.

### Validity

Signals are intended for the current trading session and normally expire near 3:30 PM IST. Do not use an expired card as a fresh entry without generating and validating a new setup.

### Cached signal

A **cached** badge means the market state was similar to a recent request and OptionsEdge reused a result for up to five minutes. This avoids unnecessary AI cost. Confirm that live price and entry range still make sense.

### 8.4 Signal safeguards

The backend applies important checks:

- AI-selected strikes are limited to three strike intervals from ATM: ±150 points for NIFTY and ±300 points for BANKNIFTY.
- Entry prices are prompted using nearby option-chain premiums when available.
- NIFTY expiry choices follow Tuesday weekly expiries.
- BANKNIFTY uses monthly expiry on the last Tuesday.
- A signal that arrives with an already-expired time is corrected to a future fallback time.

If strike validation fails, the app asks you to generate again instead of showing the questionable signal.

### 8.5 Signal availability and limits

- Signals require a connected Anthropic key unless a matching cached result is available.
- Signals are blocked outside 9:15 AM–3:30 PM IST.
- Free users can make up to 10 AI calls within the enforced hourly usage window.
- Pro users are configured for up to 50 calls in that window.
- Signal generation and chat share the AI call counter.

### 8.6 Place an order from a signal

**Place Order** appears only when Groww is connected. The order still cannot be submitted unless production order placement is enabled.

1. Select **Place Order** on a valid signal.
2. Set number of lots.
3. Review contract, expiry, total quantity, entry price, estimated cost, stop loss, and target.
4. Select **Confirm Order**.

The Dashboard sends a **BUY LIMIT, NRML, DAY** order at the signal's upper entry price. After Groww accepts the request, OptionsEdge creates a local tracked position using that same signal price and risk levels.

Important differences can exist between requested price, broker execution price, and local tracker entry price. Confirm the actual fill in Groww. The current UI does not provide an order-book or cancellation screen.

## 9. Signal History

**What is Signal History?** Signal History is a journal of AI trade ideas previously saved for current user. A journal preserves what system suggested at that time instead of relying on memory after market moved.

**Why use it?** Without history, people remember winning ideas and forget losing or expired ones. History helps answer: How many signals were ENTRY versus WATCH? Were entry prices realistic? Did signals arrive too late? Did user follow expired ideas? It supports honest review.

**How OptionsEdge helps.** It stores signal contract, risk levels, confidence, cost, model, creation time, and validity. Newest ideas appear first. Active and Expired labels make it harder to mistake an old setup for a current one.

**Beginner example.** If today's NIFTY card resembles yesterday's signal, do not reuse yesterday's entry and stop. Open history only to review old decision, then generate or analyse current setup using current prices.

**Explain it in 30 seconds:**

> “Signal History is audit trail of AI ideas. It helps review what was suggested, when it was valid, what risk levels were given, and whether user is looking at an old or current setup.”

Open **Full Analysis** from Dashboard or navigate to `/signals/history`.

History shows saved signals newest first, including:

- Signal type and Active/Expired state.
- Contract and expiry.
- Confidence.
- Entry, target, optional Target 2, and stop loss.
- Risk-to-reward, model, cost, and timestamps.

History loads 20 signals per page. **Prev** and **Next** remain disabled when total saved signals fit on one page. The history endpoint intentionally uses a lighter record and currently does not include rationale text, even though the live Dashboard card does.

A signal can appear on Dashboard but fail to appear in history if database saving failed after AI generation. The visible signal is still not proof that it was persisted.

## 10. Positions Module

**What is a position?** A position is active market exposure created after a broker order is filled. If user buys one lot of NIFTY 24000 CE and order executes, user now holds a long call position. Its value changes until it is sold, expires, or settles.

**Why track a position?** Before entry, trader focuses on opportunity. After entry, job changes to risk management. User must know actual entry premium, current premium, rupee P&L, stop loss, target, expiry, and quantity. Memory is not reliable when prices move quickly.

**How OptionsEdge helps.** Positions module keeps trade plan beside current or estimated premium. It calculates P&L using lot size, shows progress toward stop and target, attaches risk alerts, allows risk-level edits, and separates active from closed records.

**What Position module does not do.** A local Position is not broker truth. Adding one does not buy an option. Editing its stop does not place a Groww stop order. Closing it does not sell contract. Groww remains source of truth for actual holdings, fills, available quantity, and realised P&L.

**Beginner example.** User buys one NIFTY lot at ₹100 with Stop Loss ₹70 and Target ₹150. Position module can show current premium ₹120, estimated profit ₹1,300, progress toward target, and warning if premium later falls near ₹70. It cannot guarantee exit at ₹70 during a fast fall.

**Why correct entry price matters.** If actual Groww fill is ₹104 but local position says ₹100, every P&L value and percentage will be wrong. Use actual broker average price whenever possible.

**Explain it in 30 seconds:**

> “Position means trade already held after order execution. Positions module records contract, quantity, entry, stop, target, current premium, and P&L so user can manage risk after entering. It tracks; it does not execute broker exits.”

Positions tracks option trades and calculates local P&L. It does not replace the Groww portfolio or contract note.

### 10.1 Add a position manually

1. Open **Positions**.
2. Select **+ Add Position**.
3. Choose NIFTY or BANKNIFTY.
4. Choose CE or PE.
5. Enter strike.
6. Select expiry.
7. Enter option entry premium.
8. Enter quantity in lots.
9. Enter stop loss below entry premium.
10. Enter Target 1 above entry premium.
11. Optionally enter Target 2.
12. Select **Add Position**.

Validation requires:

- Symbol must be NIFTY or BANKNIFTY.
- Type must be CE or PE.
- Entry price and lots must be positive.
- Stop loss must be below entry price.
- Target 1 must be above entry price.

The tracker currently models long option positions. It does not store a BUY/SELL direction for manually tracked positions.

> **Expiry warning:** The current manual Add Position expiry picker still generates upcoming Thursdays. Current system expiry rules use Tuesday for NIFTY weekly contracts and the last Tuesday for BANKNIFTY monthly contracts. Verify the real Groww/NSE expiry before adding a manual position.

### 10.2 Groww-imported positions

After successful Groww authentication, OptionsEdge can import open F&O option positions not already tracked.

For an imported position:

- Lots are calculated from broker quantity and configured lot size.
- Entry price uses Groww average price.
- Default stop loss is 65% of entry premium.
- Default Target 1 is 150% of entry premium.

Review and edit these default risk levels immediately. Import does not preserve short-position direction in the local Position model, so local P&L and alerts are designed primarily for bought options.

### 10.3 Position summary

Top summary shows:

- Number of active positions.
- Total P&L across active positions.
- Unread alerts.

Positions refresh every 30 seconds while the market is open. Outside market hours, automatic page polling stops.

### 10.4 Read a position card

Each card shows:

- Contract and expiry.
- Active, Closed, or Expired status.
- Number of lots.
- Current P&L in rupees and percentage.
- Entry premium.
- Current or estimated LTP.
- Stop loss and Target 1.
- Stop-loss distance bar.
- Target progress bar.
- Up to three recent alerts.

Configured lot sizes at this guide's date are NIFTY 65 and BANKNIFTY 30. P&L is calculated as:

`(current option LTP − entry premium) × lots × lot size`

The **LIVE LTP / EST. LTP** badge is derived from the index snapshot's source. Option LTP itself can still fall back to cached chain data or a pricing estimate. Confirm important decisions against Groww.

### 10.5 Edit risk levels

1. Select **Edit SL/Target**.
2. Change Stop Loss, Target 1, or optional Target 2.
3. Complete any other fields the form marks as required.
4. Save.

The card updates local monitoring rules. This does not place or modify a stop-loss order in Groww.

> **Current UI caveat:** Edit SL/Target opens the general Add Position form and does not prefill the selected position. The backend applies only the Stop Loss, Target 1, and Target 2 values from this edit submission. Recheck the position card after saving.

### 10.6 Close a tracked position

1. Select **Close Position**.
2. Confirm **Yes**.

This closes the position only inside OptionsEdge and marks the reason as manual. It does **not** sell the option, close a Groww position, or cancel an order. Close the real trade in Groww first, then close its tracker entry.

The closed item is removed from the current in-memory list immediately. Reload Positions to see it again under the Closed section.

## 11. Position Risk Monitoring and Alerts

**What is risk monitoring?** Risk monitoring repeatedly compares an open position with predefined danger and profit conditions. It answers “Has something happened that requires attention?” while user is not continuously watching every price.

**Why use it?** Option premiums can change quickly because of index movement, volatility, and time decay. A trade that looked safe 15 minutes ago can approach its stop before user checks phone again. Monitoring reduces chance of silent deterioration.

**How OptionsEdge helps.** About once per minute during market hours, backend checks active Groww-connected positions for stop proximity, stop hit, targets, volatility spike, adverse index move, and premium erosion. It saves alert, broadcasts it to user, and avoids repeating same alert continuously.

**Why alerts are not broker protection.** Alert is a message, not an executed stop-loss order. Network delay, stale quotes, browser restrictions, server downtime, or market gaps can prevent timely notice. Broker-side risk controls and personal monitoring remain necessary.

**Beginner example.** User buys CE at ₹100 with SL ₹70. At ₹76, system may send SL Approaching warning. At ₹69, it may send Danger. User must still open Groww and execute exit; OptionsEdge warning alone does not close position.

**Explain it in 30 seconds:**

> “Risk Monitoring watches active tracked trades during market hours and warns about stop loss, targets, sharp opposite moves, volatility, and premium decay. It is an early-warning system, not an automatic exit system.”

### 11.1 Monitoring prerequisites

Automatic risk alerts are generated only when all these conditions are true:

- Position status is Active.
- User has active Groww credentials.
- Current time is within market hours.
- Backend worker is running.

Monitoring checks positions about once per minute. It prefers a direct Groww option quote. If unavailable, it can fall back to cached chain data or an estimated premium.

Without Groww connection, a position can still appear with estimated P&L, but backend risk-alert monitoring intentionally skips it.

### 11.2 Alert conditions

### Stop Loss Approaching — Warning

Triggered when option LTP is above the stop but within 10% of the stop-loss value.

Example: SL is ₹100 and LTP is between ₹100 and ₹110.

### Stop Loss Hit — Danger

Triggered when LTP is at or below stop loss. Message instructs immediate exit review.

### Target 1 Hit — Info

Triggered when LTP reaches Target 1. Message suggests booking partial profit.

### Target 2 Hit — Info

Triggered when LTP reaches optional Target 2. Message notes full target reached.

### IV Spike — Warning

Triggered when VIX, used as a broad volatility proxy, changes by more than 20% over roughly 30 minutes.

### Adverse Index Move — Danger

Triggered when the underlying moves more than 0.5% against the option direction over roughly 15 minutes:

- CE: index falls more than 0.5%.
- PE: index rises more than 0.5%.

### Theta Decay — Warning

Triggered when current premium is more than 50% below entry premium. This is a simple premium-erosion measure, not isolated mathematical theta.

### 11.3 Duplicate alert control

The same alert type for the same position is suppressed for 15 minutes. This suppression survives server restarts through database checking.

If the condition remains dangerous after 15 minutes, the alert can fire again.

### 11.4 Alert presentation

- **Danger:** pulsing red banner fixed at the top, audible beep, and browser notification when permission is granted. It has no close button.
- **Warning:** orange banner. It can be dismissed manually and auto-marks read after 30 seconds.
- **Info:** green toast near the bottom-right. It can be dismissed and auto-marks read after five seconds.

To clear a Danger banner, use **Mark all read** in Notification History or **clear** beside unread alerts on Positions.

Browser notifications depend on browser permission and browser support. OptionsEdge requests permission when a Danger alert first arrives. This is not a true mobile push service: if the site is fully closed, no service worker guarantees delivery.

## 12. Notification History

**What is Notification History?** It is permanent list of saved risk alerts. Screen banners are temporary; history lets user return later and see what happened, when, and for which position.

**Why use it?** A warning can disappear automatically, a browser notification can be missed, or several alerts can arrive together. History prevents important events from depending on memory or one temporary popup.

**How OptionsEdge helps.** It sorts alerts newest first, labels severity, links them to positions when possible, records read state, and provides All versus Unread views.

**Beginner example.** User notices P&L loss after lunch but did not see morning warning. Notification History can show SL Approaching at 10:45 AM and Adverse Move at 10:52 AM. This helps review both system behavior and user's response.

**Explain it in 30 seconds:**

> “Notification History is alert journal. It keeps risk messages after banners disappear, so user can review danger, warning, and target events and clear unread state.”

Open **Notifications** to review saved risk alerts newest first.

Each card shows:

- Danger, Warning, or Info severity.
- Alert type.
- Message.
- Related position when available.
- Read or Unread state.
- Timestamp.

Controls:

- **All** — all saved alerts.
- **Unread only** — only unread alerts.
- **Mark all read** — clears unread state.
- **Prev / Next** — moves through 20 alerts per page.

“Position unavailable” means the page could not match the alert to a position currently loaded in the client. The alert itself remains valid history.

## 13. Options Chain Module

**What is an options chain?** Options chain is a table of available call and put contracts for one underlying and one expiry. Each row is a strike. Call information appears on one side and put information on other side.

**Why use it?** Saying “buy NIFTY call” is incomplete because many NIFTY calls exist. Chain helps compare strikes by premium, open interest, implied volatility, and sensitivity. It also shows where current spot sits relative to ATM.

**How OptionsEdge helps.** It automatically chooses nearby strike window, highlights ATM and MaxPain, visualises OI, calculates PCR, displays CE and PE data side by side, and sends selected values into Strategy Builder.

**Beginner example.** NIFTY spot is 24,020. Chain may mark 24,000 as ATM. User can compare 24,000 CE with 24,100 CE. 24,100 CE may be cheaper, but because it is farther OTM it needs more upward movement and has greater risk of expiring worthless.

**How OI should be interpreted.** High OI means many open contracts, not that price must reverse there. OI can come from buyers, sellers, hedges, spreads, or old positions. Use it as context, not certainty.

**Explain it in 30 seconds:**

> “Options Chain is catalogue of contracts for one expiry. It compares calls and puts across strikes using price, OI, IV, and Greeks, helping user choose exact contract rather than only market direction.”

Open **Chain** for option-strike analysis and strategy construction.

### 13.1 Choose symbol and expiry

1. Select NIFTY or BANKNIFTY.
2. Select an expiry from the dropdown.
3. Wait for chain, OI chart, and Strategy Builder to refresh.

Expiry rules used by the chain:

- NIFTY: next four weekly Tuesdays, plus monthly Tuesday expiries, up to six unique dates.
- BANKNIFTY: monthly expiries only, on the last Tuesday of each month.
- If today is an expiry Tuesday, it remains available until 3:30 PM IST.

### 13.2 Chain summary

- **Spot:** current underlying index price.
- **PCR:** total put OI divided by total call OI.
- **MaxPain:** strike where calculated aggregate option-writer payout is lowest at expiry.

When a full Groww chain is available, PCR and MaxPain use the full chain, not only visible rows.

### 13.3 OI chart

The OI chart compares call and put open interest around ATM. Large bars can indicate areas where many contracts are open, but they do not prove future support or resistance.

### 13.4 Chain table

The table displays 21 strikes: ATM plus ten strikes on each side.

### Terms

- **CE:** call option.
- **PE:** put option.
- **Strike:** contract exercise level.
- **ATM:** strike nearest current spot.
- **MP:** MaxPain strike.
- **LTP:** latest or estimated option premium.
- **OI:** open contracts.
- **IV:** implied volatility percentage.
- **Delta:** estimated option-price sensitivity to a one-point underlying move.
- **Gamma:** rate at which Delta changes.
- **Theta:** estimated daily time-decay effect.
- **Vega:** estimated sensitivity to a one-percentage-point IV change.

The visible table currently focuses on OI, IV, Delta, and LTP. OI change, volume, Gamma, Theta, and Vega are available in backend chain data and feed strategy calculations, but are not all displayed as columns.

Groww can supply live LTP, OI, and IV. Missing values fall back to model-generated estimates. Greeks always use Black-Scholes calculations with a 6.5% risk-free-rate assumption.

## 14. Strategy Builder Module

**What is an options strategy?** An options strategy combines one or more option positions to create a particular risk and reward shape. Each individual BUY or SELL contract is called a leg.

**Why combine legs?** One option may have unlimited upside but high time-decay cost. Adding another leg can reduce cost, cap loss, cap profit, or create a range-based view. More legs can reduce one risk while introducing complexity, margin needs, and execution risk.

**How OptionsEdge helps.** Strategy Builder lets user choose each leg, then immediately see expiry payoff, maximum profit, maximum loss, breakevens, and combined Greeks. It converts a list of contracts into a visual “what happens if index expires here?” picture.

**Beginner example: Bull Call Spread.** Buy 24,000 CE and sell 24,100 CE. Bought call benefits from rise; sold higher call helps pay some cost but limits maximum profit above 24,100. Builder's payoff diagram makes that trade-off visible.

**Why payoff is not a prediction.** Diagram does not say where market will go. It only says approximate result at different expiry prices, assuming entered premiums and excluding costs. User still needs a market view and entry timing.

**Explain it in 30 seconds:**

> “Strategy Builder combines option legs and draws profit/loss shape at expiry. It helps user understand maximum gain, maximum loss, breakeven, and sensitivity before placing a complex trade.”

Strategy Builder appears below the options chain.

### 14.1 Build a strategy

1. Start with the default ATM CE BUY leg.
2. Choose CE or PE.
3. Choose strike.
4. Choose BUY or SELL.
5. Set lots.
6. Review or edit premium.
7. Select **+ Add Leg** for more legs.
8. Remove unwanted legs with ×. At least one leg must remain.

Changing strike or option type reloads premium and Greeks from the current chain row. Editing premium manually lets you test a different fill price.

The backend supports up to ten payoff legs.

### 14.2 Payoff diagram and summary

Payoff updates shortly after each edit and represents expiry payoff.

- **Max Profit:** highest calculated payoff, or Unlimited.
- **Max Loss:** lowest calculated payoff, or Unlimited.
- **Breakeven:** underlying expiry prices where payoff crosses zero.
- **Net Greeks:** combined Delta, Gamma, Theta, and Vega after BUY/SELL direction, lots, and lot size.

This diagram excludes brokerage, taxes, slippage, margin changes, early exercise considerations, and pre-expiry volatility changes.

### 14.3 Analyze with AI

Select **Analyze with AI** to open AI Chat with a prepared message containing all legs, premiums, payoff limits, and breakevens. Review the prepared text, then send it.

### 14.4 Track This

Select **Track This** to create one local Position entry for each leg. Default local risk levels are:

- Stop loss: 50% of entered premium.
- Target 1: 150% of entered premium.

> **Important limitation:** Local Position records do not store BUY/SELL direction. A SELL leg from Strategy Builder is created as a normal long-style tracked option. Resulting P&L and alerts are not reliable for short legs or the combined multi-leg strategy. Use the payoff diagram for structure analysis and Groww for actual position truth.

## 15. Backtest Module

**What is backtesting?** Backtesting applies fixed trading rules to historical data to see how those rules would have behaved in past market conditions. It is a simulation, not a live trade and not a forecast.

**Why use it?** A strategy can sound logical but fail repeatedly. Backtesting forces it through many past candles and measures trade count, wins, losses, drawdown, and consistency. It can reveal that a rule is too rare, too risky, or too dependent on one market phase.

**How OptionsEdge helps.** User selects index, option structure, entry rule, exit rule, period, risk points, lots, and optional ADX filter. System creates simulated trades, calculates statistics, shows cumulative P&L, exposes trade log, and saves runs for comparison.

**Beginner example.** Instead of assuming “RSI Oversold + Long Call must work,” run 90-day test. Result may show 18 trades, 45% win rate, negative P&L, and large drawdown. This does not prove rule will always fail, but it warns that simple idea needs more filters or different risk settings.

**What a good backtest should answer.** Did rule trade often enough? Were results driven by one big win? How bad was losing streak? Did small parameter change destroy result? Does logic make trading sense? OptionsEdge provides evidence, but user must ask these questions.

**Common beginner mistake: overfitting.** Trying many combinations until one past result looks excellent can create strategy fitted to history rather than future. Prefer simple rules that remain reasonable across periods and conditions.

**Explain it in 30 seconds:**

> “Backtest is historical practice run. It applies selected rules to old market candles and measures what would have happened. It helps reject weak ideas before real money, but estimated option prices mean results are not guaranteed.”

Backtest estimates how a selected strategy and rule set would have behaved on historical 15-minute index candles.

### 15.1 Configure a run

Choose:

- **Symbol:** NIFTY or BANKNIFTY.
- **Options Strategy:** contract structure.
- **Lots:** position size.
- **Signal Strategy:** condition that opens a trade.
- **Exit Condition:** additional exit preference.
- **Period:** 5–90 days.
- **Target pts:** default 30 premium points.
- **SL pts:** default 15 premium points.
- **ADX Filter:** enabled by default.

Select **Run Backtest** and wait for the elapsed timer.

Every successful run is saved automatically.

### 15.2 Options strategies

- **Long Call:** buy ATM CE; bullish, premium risk limited to amount paid.
- **Long Put:** buy ATM PE; bearish, premium risk limited to amount paid.
- **Bull Call Spread:** buy ATM CE and sell CE two strikes higher.
- **Bear Put Spread:** buy ATM PE and sell PE two strikes lower.
- **Straddle:** buy ATM CE and ATM PE; expects a large move in either direction.
- **Strangle:** buy OTM CE and OTM PE two strikes away; cheaper but needs a larger move.
- **Iron Condor:** sell OTM CE and PE, with farther protective options; expects range-bound movement.

### 15.3 Entry conditions

- Pivot + EMA20 Bullish.
- Pivot + EMA20 Bearish.
- RSI Oversold below 30.
- RSI Overbought above 70.
- MACD Bullish Cross.
- MACD Bearish Cross.
- SuperTrend Bullish.
- SuperTrend Bearish.
- Breakout Above R1.
- Breakdown Below S1.

Only one trade is open at a time in a simulation.

### 15.4 ADX Filter

When enabled:

- ADX must be at least 20.
- Bullish entries require price above EMA20.
- Bearish entries require price below EMA20.
- Neutral structures can pass when ADX requirement is met.

Filtered signals appear in Signal Diagnostics.

### 15.5 Exit behavior

Stop-loss risk control is always active. Target handling is also always active:

- If **Target 2 Hit** is selected, Target 2 is used.
- For other selections, Target 1 remains active.
- Theta Decay 50% and Expiry Minus 1 Day add those specific exit checks.
- Any open trade at the end of the data period is closed as End of Period.

When Target and SL point values are supplied by the UI, the engine uses those premium-point distances. Older saved runs without custom values may use percentage fallbacks: SL 35%, Target 1 70%, Target 2 120% of calculated risk capital.

### 15.6 Understand results

- **Win Rate:** percentage of trades with positive P&L.
- **Net P&L:** sum of simulated trade P&L.
- **Max Drawdown:** largest decline from a previous cumulative P&L peak.
- **Sharpe Ratio:** average return divided by return variability per trade; it is not annualised.
- **Profit Factor:** gross profit divided by gross loss.
- **Total Trades:** number of completed simulated trades.
- **Target Hits / SL Hits:** distribution of major exit outcomes.

High win rate can still lose money when losses are larger than wins. Evaluate Net P&L, drawdown, profit factor, trade count, and individual trades together.

### 15.7 Signal Diagnostics

- **Candidate Signals:** entry conditions detected before filtering.
- **Filtered Out:** candidates rejected by ADX/EMA filter.
- **Trades Entered:** accepted candidates.
- **Target Hits / SL Hits:** outcome counts.

If no trades appear, try a longer period, another entry condition, or temporarily disable ADX Filter for comparison.

### 15.8 Trade Log and chart popup

Trade Log shows entry/exit date, simulated contract, entry/exit premium, stop, targets, P&L, and exit reason. Ten trades appear per page.

Select a trade row to open a chart around its entry and exit. The chart shows the **underlying NIFTY or BANKNIFTY index candles**. Entry and exit premium figures remain simulated option or net-strategy values and do not share the chart's price scale.

### 15.9 Saved Runs

- Every run is saved.
- Select a saved run to reopen its results.
- Eight saved runs appear per page.
- Default retention is 30 days.
- Old runs are deleted automatically when history is loaded or a new run is saved.

### 15.10 Critical backtest limitation

When Groww is connected, historical **index candles are real Groww data**, but historical option premiums are still estimated using Black-Scholes with assumed volatility. The backtest validates signal timing against index movement. It does not replay actual historical option-chain premiums, bid/ask spreads, liquidity, slippage, brokerage, or taxes.

Use backtests to compare ideas, not to forecast guaranteed returns.

## 16. AI Chat Module

**What is AI Chat in a trading system?** It is conversational analysis tool. Instead of reading every number alone, user can ask questions in normal language and receive explanation based on current market summary and tracked positions.

**Why use it?** Beginners often see RSI, VIX, PCR, pivots, and positions but struggle to combine them into one view. Chat can explain relationships, compare alternatives, identify missing risk questions, and translate technical data into plain language.

**How OptionsEdge helps.** It gives Claude selected live market context, indicator summaries, and active tracked positions. This makes answers more relevant than a generic chatbot that knows nothing about user's screen.

**Beginner example.** Ask: “I hold NIFTY 24000 CE at ₹100, current premium ₹78, SL ₹70. Explain what I should check before deciding.” Useful answer should discuss remaining risk, distance to stop, index trend, expiry, volatility, and position size—not only say BUY or SELL.

**Best way to ask.** Include objective, contract, entry, quantity, stop, expiry, and concern. Ask for scenarios and risks. “Explain what would invalidate this trade” is safer than “Tell me guaranteed winning trade.”

**Explain it in 30 seconds:**

> “AI Chat is context-aware trading explainer. It combines current market summary and tracked positions, then answers questions in plain language. It helps reasoning; user still verifies numbers and owns decision.”

AI Chat is a trading conversation powered by Claude Sonnet.

### 16.1 What Chat knows

For each request, the backend can include:

- Current market status.
- Current index values and selected indicator summaries.
- Your active tracked positions.
- Up to ten recent messages from the current chat session.

This lets the AI answer questions such as:

- “Analyze Market.”
- “Check My Positions.”
- “Best Strategy Today.”
- “Tomorrow's Outlook.”
- “What should I watch before entering this strategy?”

### 16.2 Send a message

1. Open **AI Chat**.
2. Type a question or select a quick action.
3. Press Enter or select **Send**.
4. Use Shift+Enter for a new line.
5. Read the streamed response.

Each completed assistant message shows model, token count, and estimated USD cost.

Chat requires an Anthropic key and is available only during market hours. It shares the AI rate limit with signal generation.

### 16.3 New Chat and history

Select **New chat** to create an empty session. Messages are saved in the backend by session, but the current UI does not show a list of old sessions. A full app reload can start a new session because the active session identifier is held only in memory. There is no visible way to browse abandoned sessions.

### 16.4 Chat safety

AI Chat can misunderstand data, use stale context, or give an overconfident answer. It cannot see your real Groww order book unless positions were imported or added to OptionsEdge. It should never be treated as authority to ignore a stop loss or increase risk.

## 17. Security Settings Module

**What is this module?** Security Settings controls account protection and services that make trading features work: AI key, automatic signal schedule, usage visibility, password, and 2FA.

**Why it matters to trading workflow.** AI modules cannot work without user's Anthropic key. Automatic signals can consume AI calls at scheduled times. Weak account security can expose personal trading context and connected credentials. This page is operational control centre, not only password page.

**Explain it in 30 seconds:**

> “Security Settings protects account and configures AI behavior. User connects own AI key, controls scheduled signals, watches cost and limits, enables 2FA, and changes password.”

### 17.1 AI Usage

Usage cards show:

- **Calls today:** current stored AI-call counter and plan limit.
- **Cost today:** AI usage cost recorded since midnight UTC.
- **Wallet balance:** informational balance for future billing.

Current enforcement uses an hourly reset window even though the UI label says “Calls today.” Free plan limit is 10; Pro plan limit is 50. Wallet top-up and Razorpay payments are not yet available.

### 17.2 Enable two-factor authentication

1. Select **Enable 2FA**.
2. Scan the QR code with an authenticator app, or enter the manual key.
3. Enter the six-digit code.
4. Select **Verify and enable**.
5. Copy and securely store all ten recovery codes.
6. Select **Done**.

Each recovery code can be used once. OptionsEdge does not provide a recovery-code download or regeneration screen in the current UI.

### 17.3 Disable two-factor authentication

1. Select **Disable 2FA**.
2. Enter current password.
3. Confirm.

Disabling resets the authenticator key. Old authenticator codes stop working.

### 17.4 Auto Signal Preferences

**What is an automatic signal?** It is same AI signal analysis triggered by schedule instead of user pressing Generate. Scheduling creates regular review points, such as after market open, around noon, and before late-session movement.

**Why use it?** User may forget to open Dashboard at planned times. Automatic signals can create a consistent routine. More signals do not mean more trades; user should still reject weak, late, cached, or unsuitable ideas.

Automatic signals can be enabled independently for NIFTY and BANKNIFTY.

1. Connect an Anthropic API key.
2. Open **Security → Auto Signal Preferences**.
3. Enable NIFTY, BANKNIFTY, or both.
4. Enter comma-separated IST times, for example `09:30,12:00,14:00`.
5. Select **Save Preferences**.
6. Review the normalized times returned by the server.

Rules:

- Valid range is 09:15 through 15:25 IST.
- Maximum five times per index.
- Invalid times are removed.
- Duplicates are removed.
- Times are sorted and formatted as `HH:mm`.
- If every entered value is invalid, time becomes `09:30`.
- Worker checks schedules about once per minute.

Automatic generation happens on the server even if the Dashboard is not open. Newly generated results are saved in Signal History. A five-minute cache hit can be delivered without creating a new history row for that scheduled event. Real-time on-screen or browser notification requires an active live connection. The app only sends an auto-signal browser notification if permission was already granted.

### 17.5 Change password

Enter current password, new password, and confirmation. New password must meet all strength rules. Select **Change password**.

Changing password does not visibly sign out every other refresh session. If compromise is suspected, also disconnect sensitive integrations and contact the administrator.

### 17.6 Account Info

Shows email, display name, membership date, and current plan. Current UI does not provide profile editing or account deletion.

## Presenter Guide: Explain OptionsEdge to Others

This section is for product owner, trainer, or user who needs to explain system without being an expert trader.

### One-sentence explanation

> “OptionsEdge is NIFTY and BANKNIFTY decision-support system that combines market analysis, AI trade plans, position tracking, risk alerts, strategy payoff, and historical testing while leaving final verification and trade execution with user.”

### Two-minute explanation

Use this order:

1. **Start with problem:** Options traders must watch price, indicators, option premiums, risk levels, and open positions at same time.
2. **Explain Dashboard:** It organises current market context and tells user whether values are live or estimated.
3. **Explain AI Signals:** It converts context into structured idea with exact contract, entry, stop, target, and validity.
4. **Explain Groww boundary:** Groww is broker and source of actual execution. OptionsEdge is assistant and tracker.
5. **Explain Positions and Alerts:** Once trade exists, app watches defined risk conditions and warns user; it does not execute exit.
6. **Explain Chain and Strategy Builder:** These help compare contracts and understand multi-leg payoff.
7. **Explain Backtest:** It tests rule ideas on historical index candles, using estimated option premiums.
8. **Finish with safety:** User remains responsible for verification, quantity, order, and exit.

### Five-minute demonstration script

1. Open Dashboard and point to **Market Open/Closed** and **LIVE/SIMULATED** labels.
2. Show NIFTY card: “This is underlying index, not option premium.”
3. Point to chart, EMA/SuperTrend direction, ADX strength, RSI momentum, VIX volatility, and Pivot/R1/S1 levels.
4. Open AI Signal card: explain contract, entry range, stop, target, confidence, reasons, and validity.
5. State: “This signal is idea. No trade exists yet.”
6. Open Chain: show ATM row, CE/PE sides, premium, OI, IV, and MaxPain.
7. Open Strategy Builder: add second leg and show payoff shape changes.
8. Open Positions: explain one filled trade, P&L calculation, stop and target progress.
9. Open Notifications: explain Danger, Warning, and Info records.
10. Open Backtest: show that rules can be tested before real money, then state option premiums are estimated.
11. Open AI Chat: explain it can translate market and position context into normal language.
12. End: “Groww remains source of truth and user makes every final trade decision.”

### Common questions and safe answers

**Does OptionsEdge trade automatically?**  
Normally no. It analyses and tracks. A confirmed signal order can be submitted only when Groww integration and production order placement are enabled. Risk alerts never auto-exit.

**Are all prices live?**  
No. Read labels. Underlying data can be live while option LTP or chain fields still use cached or estimated values. Verify in Groww.

**Does high AI confidence mean likely profit?**  
No. Confidence is model judgement within its prompt, not audited probability of profit.

**Why does user need Anthropic key?**  
Claude performs AI signal and chat analysis. User's key authorises those calls and user pays Anthropic directly.

**Why connect Groww?**  
It improves access to live data, supports position import and monitoring, and can enable confirmed orders in eligible deployment.

**What happens when stop loss is hit?**  
OptionsEdge sends Danger alert. User must still exit real position in Groww unless separate broker-side stop order already exists.

**Why use Backtest if premium is estimated?**  
It is useful for comparing entry timing, rule frequency, direction, drawdown, and relative strategy behavior. It is not accurate historical option-P&L replay.

**Can beginner follow every AI signal?**  
No. Beginner should first verify live price, expiry, liquidity, lot exposure, maximum loss, and whether signal is still valid. Skipping trade is acceptable.

**What is most important feature?**  
Risk discipline. Finding entries is useful, but managing quantity, stop loss, and exit protects capital.

## 18. Common End-to-End Workflows

### 18.1 Generate and manually trade a signal

1. Connect Anthropic key.
2. Connect Groww if live data is desired.
3. Confirm Dashboard says Market Open and inspect data-source labels.
4. Select index.
5. Review chart, trend indicators, pivots, VIX, and PCR.
6. Generate signal.
7. Confirm strike, expiry, option premium, entry range, stop, targets, confidence, and validity.
8. Compare current Groww option LTP with signal entry range.
9. Place trade manually in Groww, or use Place Order only when enabled and reviewed.
10. Add or verify position in Position Tracker.
11. Correct entry price to actual fill if needed.
12. Watch Groww and OptionsEdge; do not rely on alerts as the only exit method.
13. Close broker trade first, then close local tracker entry.

### 18.2 Use automatic signals

1. Connect AI key.
2. Configure up to five valid market-hour times.
3. Save preferences.
4. Grant browser notification permission if desired.
5. Check Signal History after each scheduled time.
6. Treat every automatic signal as a fresh idea that still needs price and risk verification.

### 18.3 Monitor a manually entered trade

1. Connect Groww.
2. Add position with accurate contract, entry, lots, stop, and targets.
3. Verify card shows expected premium and P&L.
4. Edit risk levels if trade plan changes.
5. Keep Groww available for actual execution.
6. Review Notification History if an alert disappears from screen.

### 18.4 Test a strategy before using it

1. Open Chain and choose symbol/expiry.
2. Build legs and review payoff.
3. Send structure to AI Chat for qualitative analysis.
4. Open Backtest and choose closest built-in strategy.
5. Run several entry conditions with same period and risk settings.
6. Compare drawdown, profit factor, trade count, and trade log—not just Net P&L.
7. Remember that option premiums are estimated.

## 19. Troubleshooting

### “Market is closed” when generating a signal or using Chat

These AI features work only from 9:15 AM through 3:30 PM IST on weekdays. Backtest or inspect saved history outside those hours.

### “No AI API key configured”

Open Security → AI Connection and connect a valid Anthropic key beginning with `sk-ant-`.

### “Invalid API key”

Copy a current key from Anthropic Console. Confirm no spaces were added. The backend tests the key before saving it.

### AI rate limit reached

Signals and Chat share the limit. Wait until the hourly window resets. Repeated requests in a similar market state may return a cached signal without a new AI call.

### Groww badge shows connection failed

- Recopy TOTP Token and TOTP Secret.
- Confirm the secret is valid Base32.
- Make sure the API key is active in Groww.
- Retry after a temporary authentication timeout.
- Contact administrator if Groww integration is disabled server-side.

### Place Order says “Requires Production Server”

Groww is connected, but order placement is disabled because server static IP is not enabled or whitelisted. Trade manually in Groww.

### Position shows EST. LTP

Current option quote was not confirmed from live chain data. Open Chain for the same symbol and expiry to refresh chain data, then verify directly in Groww.

### No position alerts arrive

Check all of these:

- Groww is connected.
- Position is Active.
- Market is open.
- Browser has network access.
- Alert condition has actually triggered.
- Same alert was not fired within the last 15 minutes.
- Review Notification History in case a banner auto-dismissed.

### Signal history Prev/Next is disabled

History uses 20 signals per page. If count is 20 or fewer, one page is correct and both directions cannot move beyond it.

### Notification history Prev/Next is disabled

Notification History also uses 20 records per page. Filtered Unread count can reduce it to one page.

### Backtest fails to fetch candles

When Groww is enabled, Backtest requires successful Groww historical-candle retrieval. Reconnect Groww and retry. In simulated mode, ensure enough historical candles exist for indicator warm-up.

### Backtest has no trades

Try longer period, another entry rule, or disabling ADX Filter for comparison. Some strict combinations legitimately produce no entries.

### Chat history disappeared after reload

Current UI does not list old chat sessions. Hard reload can start a new session even though old messages remain stored in backend.

### Browser notifications do not appear

Check site notification permission in browser settings. Keep OptionsEdge open or active in browser. Current app does not provide guaranteed push delivery when fully closed.

## 20. Current Limitations and Features Not Yet Available

The following are important current-state limitations:

- No guaranteed profit or trade accuracy.
- No native iOS or Android app.
- No guaranteed background push when browser is closed.
- No WhatsApp position alerts.
- No user-facing email position alerts.
- No Razorpay payment flow or usable wallet top-up.
- No order book or order-cancel UI.
- Closing a local Position does not close a Groww trade.
- Editing local SL/targets does not create or modify broker orders.
- Manual position expiry picker still uses Thursday dates and must be independently verified.
- Edit SL/Target currently opens an unprefilled general position form.
- Position tracker lacks BUY/SELL direction, so short-option and multi-leg tracking is unreliable.
- Signal History omits live signal rationale.
- AI Chat does not expose an old-session browser.
- Groww does not provide FII/DII flow through current integration.
- Greeks and missing chain values are model estimates.
- Backtest option premiums are estimates, even when index candles are real.
- Exchange holidays are not fully modelled.
- Current system supports only NIFTY and BANKNIFTY.

## 21. Quick Glossary

- **ATM:** strike nearest current underlying price.
- **ITM:** option with intrinsic value.
- **OTM:** option without intrinsic value at current spot.
- **CE:** call option; generally benefits from a rise when bought.
- **PE:** put option; generally benefits from a fall when bought.
- **LTP:** latest traded price, or an estimate where labelled.
- **OI:** number of open option contracts.
- **IV:** implied volatility; market-implied uncertainty embedded in option premium.
- **VIX:** broad expected-volatility measure for Indian equities.
- **PCR:** put OI divided by call OI.
- **MaxPain:** calculated strike with lowest aggregate writer payout at expiry.
- **SL:** stop loss.
- **T1 / T2:** first and second targets.
- **R:R:** planned reward divided by planned risk.
- **EMA:** moving average weighted toward recent prices.
- **RSI:** momentum oscillator.
- **MACD:** momentum and trend indicator based on moving averages.
- **ADX:** trend-strength indicator.
- **SuperTrend:** ATR-based directional trend indicator.
- **Theta:** estimated option value lost with time, all else equal.
- **Delta:** estimated option-price response to underlying movement.
- **Gamma:** estimated rate of Delta change.
- **Vega:** estimated response to IV change.
- **NRML:** Groww carry-forward derivatives product type used by current order flow.

## 22. Daily Safety Checklist

Before any trade:

- Confirm Market Open status.
- Confirm LIVE versus SIMULATED or ESTIMATED labels.
- Verify option contract and real expiry in Groww.
- Verify current premium is inside or near planned entry range.
- Check lot size and total rupee exposure.
- Set maximum loss before entry.
- Confirm stop and targets are realistic for current volatility.
- Check signal validity and age.
- Review liquidity and bid/ask spread in Groww.
- Avoid acting on one indicator alone.

After entry:

- Confirm broker fill and average price.
- Add or correct tracked position.
- Keep broker stop/exit plan independent of app alerts.
- Treat Danger alerts urgently, but verify broker price.
- Close the real broker trade before closing local tracker.

After market:

- Review Signal History and Notification History.
- Check whether tracked positions match Groww.
- Remove or close stale local positions.
- Use Backtest for comparison, not hindsight certainty.
- Review Anthropic usage and cost.
