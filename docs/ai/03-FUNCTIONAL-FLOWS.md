# Functional Flows

## Dashboard Flow

1. Frontend checks market status and Groww-gated market data.
2. If user is not Groww-connected, market-data-dependent UI stays blocked.
3. If data is not fresh, UI shows degraded state and avoids live values.
4. Fresh data can update snapshots, indicators, pivots, market pulse, and chart.
5. AI signal generation stays user-triggered.

## Signal Flow

1. User requests signal for NIFTY or BANKNIFTY.
2. Backend checks market hours, rate limit, Groww user connection, and fresh
   market data.
3. Cached signal may be returned when market snapshot hash matches valid TTL.
4. AI response must be parsed and validated before save/display.
5. Signal includes exact contract, entry range, stop loss, targets, confidence,
   rationale, and validity.

No actionable AI signal may be generated from stale, missing, or silently mocked
Groww data.

## Position Flow

1. User manually enters position after trading on Groww.
2. App tracks position P&L and risk from current option/market data.
3. Position monitor checks alert conditions during market hours.
4. Alerts dedupe by position, alert type, and time window.
5. DANGER alerts must remain visible until user marks read.

## Options Chain Flow

1. User selects symbol and expiry.
2. Backend refreshes or reads Groww-backed spot/chain inputs.
3. If Groww is disabled globally, mock mode may serve dev data.
4. If Groww is enabled but user lacks credentials or data is stale, return
   gated response with no usable chain data.

## Backtest Flow

Backtest uses historical candles and currently estimates historical option
premiums.

It validates timing against market candles, not true historical option LTP
replay.

## Review Flow

Claude Coworker reviews Codex output against task file, architecture, safety
rules, tests, and `10-REVIEW-CHECKLIST.md`.
