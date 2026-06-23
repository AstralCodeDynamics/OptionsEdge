# Data Safety Rules

These rules protect users from wrong trades, privacy leaks, and stale-data signals.

## Groww Rules

- No silent mock fallback when `Groww:Enabled=true`.
- Mock data is allowed only when `Groww:Enabled=false`.
- Gate Groww-backed market data per user.
- Respect `isGrowwConnected`.
- Respect `isDataFresh`.
- If user is not Groww-connected, do not show live market data.
- If data is not fresh, do not show live market values, options rows, position risk output, or AI market signals as current.
- No AI actionable signal from stale or missing data.
- No AI actionable signal from another user's cached Groww data.

## Endpoint Contract

Groww-backed endpoints should return:

```json
{
  "isGrowwConnected": true,
  "isDataFresh": true,
  "data": {}
}
```

When blocked or stale, `data` must be `null` or otherwise unusable by caller.

## Frontend Contract

- Check `isGrowwConnected` before reading `data`.
- Check `isDataFresh` before rendering live values or enabling market-data-dependent actions.
- Clear cached live data when gate fails or freshness fails.
- Ignore SignalR market updates until REST confirms connected and fresh data.

## Backend Contract

- Check per-user Groww credentials before reading shared Groww-backed caches.
- Never build options chain, indicators, position risk, chat market context, or AI signals from missing live snapshot.
- Position monitor must skip alert evaluation when required market data is unavailable.

## Artefact Hygiene

Never record secrets, tokens, connection strings, Groww credentials, private account data, or private trading account details.
