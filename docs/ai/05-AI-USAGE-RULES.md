# AI Usage Rules

## Roles

- Codex implements code and docs.
- Claude Coworker / Claude AI designs, consults, reviews, and writes task contracts.

## Model Strategy

- Use cheaper quick model tier for frequent signals, risk checks, and
  lightweight market status work.
- Use deeper model tier only for user-triggered deep analysis.
- Keep AI responses bounded by configured max token limits.
- Cache AI responses aggressively when market snapshot hash is unchanged.

## AI Safety

- No AI actionable signal from stale or missing market data.
- No AI actionable signal from mock data when Groww is enabled.
- No AI market context for user who lacks per-user Groww credentials when Groww is enabled.
- Do not send secrets, tokens, connection strings, Groww credentials, or private account data to AI.
- Log usage/cost metadata without private secrets.

## Prompt/Response Rules

- Prefer structured JSON outputs for signals and risk checks.
- Validate parsed AI output before saving or displaying.
- Reject impossible or unsafe contract/strike/price combinations.
- Keep rationale short and user-actionable.
- Preserve "not financial advice" posture; user trades manually.
