# Known Issues

## Current

- Backtest historical option premiums are estimated with Black-Scholes, not
  replayed from true historical option LTP.
- Some legacy docs still mention earlier mock-data-only phase language.
  Current Groww safety rules in `docs/AI_HANDOFF.md` and
  `docs/ai/06-DATA-SAFETY-RULES.md` take precedence for live data work.
- Browser/in-app smoke tests may be unavailable in some agent sessions.
  Record this in handoff when it happens.

## Review Watchlist

- Any endpoint reading shared Groww-backed cache without per-user credential gate.
- Any frontend caller reading gated `data` without checking `isGrowwConnected` and `isDataFresh`.
- Any AI prompt that can receive stale, missing, mocked, or cross-user live data.
- Any position alert generated when snapshot or option LTP is unavailable.
- Any task that changes behavior without task file or handoff.
