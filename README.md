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
dotnet run --project src/OptionsEdge.API
# API: https://localhost:5001
```

### 4. Run frontend
```bash
cd frontend
npm install
npm run dev
# App: http://localhost:5173
```

## Disclaimer
For educational purposes. Not financial advice. Always do your own research before trading.
