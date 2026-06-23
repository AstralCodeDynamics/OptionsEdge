# Current State

Last updated: 2026-06-23.

## Focus

Backtest and Groww integration for NIFTY/BANKNIFTY options strategy testing.

## Current Safety Posture

- Groww cache-miss silent mock fallback has been fixed.
- Groww-gated responses include `isGrowwConnected` and `isDataFresh`.
- Frontend clears or blocks live market display when Groww is disconnected or stale.
- Dashboard refreshes Groww-backed market snapshots, status, indicators, and candles every 30 seconds.
- AI Signals remain manual refresh/user-triggered.

## Current Caveats

- Historical option premiums in backtest are still estimated with Black-Scholes.
- Backtest validates signal timing against market candles, not real historical option LTP replay.
- `GET /market/status`, expiries, and payoff endpoints are intentionally not Groww-gated when they do not expose user-specific live data.

## Workflow State

`docs/ai` now holds structured AI artefacts for Claude Coworker and Codex. `docs/AI_HANDOFF.md` remains chronological change log.
