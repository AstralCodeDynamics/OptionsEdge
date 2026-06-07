# OptionsEdge

NIFTY/BANKNIFTY options trading assistant — your expert trading partner.

## Stack
- Backend: .NET Core 10, C#, PostgreSQL, SignalR
- Frontend: React 18, TypeScript, TailwindCSS, TradingView Charts
- AI: Claude API (Haiku + Sonnet)

## Local Setup

### Prerequisites
- .NET 10 SDK
- Node.js 20+
- PostgreSQL 16 (`brew install postgresql@16`)

### 1. Database
```bash
brew services start postgresql@16
createdb optionsedge_dev
```

### 2. Backend secrets
Create `backend/src/OptionsEdge.API/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=optionsedge_dev;Username=postgres"
  },
  "Claude": {
    "ApiKey": "sk-ant-YOUR-KEY-HERE",
    "HaikuModel": "claude-haiku-4-5-20251001",
    "SonnetModel": "claude-sonnet-4-6"
  },
  "Jwt": {
    "Secret": "your-local-dev-secret-minimum-32-characters",
    "Issuer": "OptionsEdge",
    "Audience": "OptionsEdge"
  }
}
```

### 3. Run backend
```bash
cd backend
dotnet restore
dotnet ef database update --project src/OptionsEdge.API
dotnet run --project src/OptionsEdge.API --launch-profile https
# API:     https://localhost:5001
# SignalR: https://localhost:5001/hubs/market
```
The frontend talks to the API over HTTPS (see `.env.development` below), so always
launch with the `https` profile — the plain `http` profile only binds port 5000 and
the frontend's API calls will fail with `ERR_CONNECTION_REFUSED`.

In `Development`, a seed user is created automatically on first run:
- Email: `dev@optionsedge.local`
- Password: `DevPass123!`

### 4. Run frontend
```bash
cd frontend
cp .env.example .env.development   # defaults match the steps above; edit if your ports differ
npm install
npm run dev
# App: http://localhost:5173
```

### 5. Run backend tests
```bash
cd backend
dotnet test OptionsEdge.slnx
```

## Disclaimer
For educational purposes. Not financial advice. Always do your own research before trading.
