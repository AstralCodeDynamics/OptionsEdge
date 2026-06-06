# OptionsEdge — Architecture Document

## 1. System Architecture

```
Browser / Mobile PWA
        |
React Frontend (Vite + TypeScript + TailwindCSS)
        | HTTPS REST + SignalR WebSocket
.NET Core 10 Minimal API
        |
   +-----------+------------------+------------------+
   |           |                  |                  |
Service    SignalR Hub      Background Workers   Middleware
Layer      /hubs/market     MarketDataWorker     RateLimiting
           |                PositionMonitor      ErrorHandling
           |                                     JWT Auth
   +-------+--------+
   |                |
EF Core +       IMemoryCache
Npgsql              |
   |           Claude API
PostgreSQL    (Anthropic)
```

---

## 2. Monorepo Structure

```
OptionsEdge/
├── CLAUDE.md                              (Claude Code context - keep lean)
├── SPEC.md
├── ARCHITECTURE.md
├── TASKS.md
├── .gitignore
├── README.md
│
├── backend/
│   ├── OptionsEdge.sln
│   └── src/
│       └── OptionsEdge.API/
│           ├── Program.cs
│           ├── appsettings.json
│           ├── appsettings.Development.json  (gitignored - has secrets)
│           │
│           ├── Features/
│           │   ├── Market/
│           │   │   ├── MarketEndpoints.cs
│           │   │   ├── MarketService.cs
│           │   │   └── Models.cs
│           │   ├── Options/
│           │   │   ├── OptionsEndpoints.cs
│           │   │   ├── OptionsService.cs
│           │   │   └── Models.cs
│           │   ├── Indicators/
│           │   │   ├── IndicatorEndpoints.cs
│           │   │   ├── IndicatorService.cs
│           │   │   └── Models.cs
│           │   ├── Signals/
│           │   │   ├── SignalEndpoints.cs
│           │   │   ├── AISignalService.cs
│           │   │   ├── SignalCacheService.cs
│           │   │   └── Models.cs
│           │   ├── Positions/
│           │   │   ├── PositionEndpoints.cs
│           │   │   ├── PositionService.cs
│           │   │   ├── AlertService.cs
│           │   │   └── Models.cs
│           │   ├── Backtest/
│           │   │   ├── BacktestEndpoints.cs
│           │   │   ├── BacktestService.cs
│           │   │   └── Models.cs
│           │   ├── Chat/
│           │   │   ├── ChatEndpoints.cs
│           │   │   ├── ChatService.cs
│           │   │   └── Models.cs
│           │   └── Auth/
│           │       ├── AuthEndpoints.cs
│           │       ├── AuthService.cs
│           │       ├── JwtService.cs
│           │       └── Models.cs
│           │
│           ├── Infrastructure/
│           │   ├── Data/
│           │   │   ├── AppDbContext.cs
│           │   │   └── Migrations/
│           │   ├── MockData/
│           │   │   └── MockMarketDataService.cs
│           │   ├── Background/
│           │   │   ├── MarketDataWorker.cs      (refreshes every 30s)
│           │   │   ├── PositionMonitorWorker.cs (checks alerts every 60s)
│           │   │   └── MarketHoursHelper.cs
│           │   ├── SignalR/
│           │   │   └── MarketHub.cs
│           │   └── Claude/
│           │       └── ClaudeApiClient.cs
│           │
│           ├── Domain/
│           │   ├── Entities/
│           │   │   ├── User.cs
│           │   │   ├── Position.cs
│           │   │   ├── Signal.cs
│           │   │   ├── Alert.cs
│           │   │   ├── ChatMessage.cs
│           │   │   ├── AIUsageLog.cs
│           │   │   └── BacktestResult.cs
│           │   └── Enums/
│           │       ├── AlertSeverity.cs
│           │       ├── SignalType.cs
│           │       └── OptionType.cs
│           │
│           └── Common/
│               ├── Extensions/
│               │   └── ServiceCollectionExtensions.cs
│               ├── Middleware/
│               │   ├── RateLimitingMiddleware.cs
│               │   └── ErrorHandlingMiddleware.cs
│               └── Constants/
│                   └── AppConstants.cs
│
└── frontend/
    ├── package.json
    ├── vite.config.ts
    ├── tailwind.config.ts
    ├── tsconfig.json
    └── src/
        ├── main.tsx
        ├── App.tsx
        ├── pages/
        │   ├── Dashboard/
        │   ├── Positions/
        │   ├── Chain/
        │   ├── Backtest/
        │   ├── Chat/
        │   └── Auth/
        ├── components/
        │   ├── layout/     (AppShell, Sidebar, BottomNav, Header)
        │   ├── market/     (IndexCard, MarketPulse, PivotLevels)
        │   ├── charts/     (PriceChart, OIChart, PayoffDiagram)
        │   ├── indicators/ (IndicatorPanel)
        │   ├── signals/    (SignalCard, SignalBadge)
        │   ├── positions/  (PositionCard, AddPositionModal, AlertBanner)
        │   ├── chain/      (ChainTable, ChainRow)
        │   └── common/     (LoadingSpinner, ErrorBoundary, PushNotification)
        ├── hooks/
        │   ├── useSignalR.ts
        │   ├── useMarketData.ts
        │   ├── usePositions.ts
        │   ├── useAlerts.ts
        │   └── useAIChat.ts
        ├── services/
        │   └── api.ts
        ├── store/
        │   └── appStore.ts
        └── types/
            └── index.ts
```

---

## 3. Database Schema (PostgreSQL)

### users
| Column | Type | Notes |
|---|---|---|
| id | UUID PK | gen_random_uuid() |
| email | VARCHAR(255) UNIQUE | |
| password_hash | VARCHAR(255) | bcrypt |
| display_name | VARCHAR(100) | |
| subscription_plan | VARCHAR(20) | free/starter/pro/elite |
| wallet_balance | DECIMAL(10,4) | for Phase 2 billing |
| ai_calls_today | INT | reset daily |
| ai_calls_reset_at | TIMESTAMPTZ | |
| is_active | BOOLEAN | |
| created_at | TIMESTAMPTZ | |

### positions
| Column | Type | Notes |
|---|---|---|
| id | UUID PK | |
| user_id | UUID FK | |
| symbol | VARCHAR(20) | NIFTY/BANKNIFTY |
| strike | INT | |
| option_type | VARCHAR(2) | CE/PE |
| expiry | DATE | |
| entry_price | DECIMAL(10,2) | |
| quantity | INT | lots |
| stop_loss | DECIMAL(10,2) | |
| target1 | DECIMAL(10,2) | |
| target2 | DECIMAL(10,2) | nullable |
| signal_id | UUID | nullable, linked signal |
| status | VARCHAR(20) | active/closed/expired |
| exit_price | DECIMAL(10,2) | nullable |
| exit_reason | VARCHAR(50) | sl_hit/target1/manual |
| closed_at | TIMESTAMPTZ | nullable |
| created_at | TIMESTAMPTZ | |

### signals
| Column | Type | Notes |
|---|---|---|
| id | UUID PK | |
| user_id | UUID FK | |
| symbol | VARCHAR(20) | |
| signal_type | VARCHAR(20) | ENTRY/EXIT/HOLD/WATCH |
| option_type | VARCHAR(2) | CE/PE |
| strike | INT | |
| expiry | DATE | |
| entry_low | DECIMAL(10,2) | |
| entry_high | DECIMAL(10,2) | |
| stop_loss | DECIMAL(10,2) | |
| target1 | DECIMAL(10,2) | |
| target2 | DECIMAL(10,2) | |
| confidence | INT | 0-100 |
| risk_reward | DECIMAL(5,2) | |
| rationale | TEXT | |
| market_snapshot | JSONB | full snapshot at signal time |
| model_used | VARCHAR(50) | haiku/sonnet |
| input_tokens | INT | |
| output_tokens | INT | |
| cost_usd | DECIMAL(10,6) | |
| valid_until | TIMESTAMPTZ | |
| created_at | TIMESTAMPTZ | |

### alerts
| Column | Type | Notes |
|---|---|---|
| id | UUID PK | |
| user_id | UUID FK | |
| position_id | UUID FK | |
| severity | VARCHAR(10) | WARNING/DANGER/INFO |
| alert_type | VARCHAR(50) | SL_APPROACHING/SL_HIT/TARGET_HIT/IV_SPIKE |
| message | TEXT | |
| is_read | BOOLEAN | |
| created_at | TIMESTAMPTZ | |

### chat_messages
| Column | Type | Notes |
|---|---|---|
| id | UUID PK | |
| user_id | UUID FK | |
| session_id | UUID | group messages by session |
| role | VARCHAR(10) | user/assistant |
| content | TEXT | |
| model_used | VARCHAR(50) | |
| input_tokens | INT | |
| output_tokens | INT | |
| cost_usd | DECIMAL(10,6) | |
| created_at | TIMESTAMPTZ | |

### ai_usage_logs
| Column | Type | Notes |
|---|---|---|
| id | UUID PK | |
| user_id | UUID FK | |
| feature | VARCHAR(50) | signal/chat/backtest_analysis |
| model_used | VARCHAR(50) | |
| input_tokens | INT | |
| output_tokens | INT | |
| cost_usd | DECIMAL(10,6) | |
| wallet_before | DECIMAL(10,4) | Phase 2 |
| wallet_after | DECIMAL(10,4) | Phase 2 |
| created_at | TIMESTAMPTZ | |

### backtest_results
| Column | Type | Notes |
|---|---|---|
| id | UUID PK | |
| user_id | UUID FK | |
| strategy | VARCHAR(50) | |
| parameters | JSONB | |
| win_rate | DECIMAL(5,2) | |
| total_trades | INT | |
| net_pnl | DECIMAL(12,2) | |
| max_drawdown | DECIMAL(12,2) | |
| sharpe_ratio | DECIMAL(5,2) | |
| profit_factor | DECIMAL(5,2) | |
| trade_log | JSONB | array of individual trades |
| created_at | TIMESTAMPTZ | |

---

## 4. API Endpoints (v1)

```
GET  /api/v1/market/snapshot
GET  /api/v1/market/candles/{symbol}
GET  /api/v1/market/status

GET  /api/v1/options/chain/{symbol}?expiry=YYYY-MM-DD
GET  /api/v1/options/expiries/{symbol}
GET  /api/v1/options/maxpain/{symbol}?expiry=YYYY-MM-DD

GET  /api/v1/indicators/{symbol}

POST /api/v1/signals/generate
GET  /api/v1/signals/history
GET  /api/v1/signals/{id}

GET    /api/v1/positions
POST   /api/v1/positions
PUT    /api/v1/positions/{id}
DELETE /api/v1/positions/{id}
GET    /api/v1/positions/{id}/pnl

GET  /api/v1/alerts
PUT  /api/v1/alerts/{id}/read
PUT  /api/v1/alerts/read-all

POST /api/v1/chat/message
GET  /api/v1/chat/history
POST /api/v1/chat/new-session

POST /api/v1/backtest/run
GET  /api/v1/backtest/history

POST /api/v1/auth/register
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
```

---

## 5. SignalR Hub — MarketHub

```
Hub URL: /hubs/market

Groups:
  "NIFTY"            → all subscribers to NIFTY data
  "BANKNIFTY"        → all subscribers to BANKNIFTY data
  "alerts:{userId}"  → user-specific position alerts

Server → Client events:
  PriceUpdate        → { symbol, ltp, change, changePct, timestamp }
  IndicatorUpdate    → { symbol, rsi, macdSignal, trend, supertrend }
  PositionAlert      → { positionId, severity, message, alertType }
  NewSignal          → SignalResponse
  MarketStatus       → { isOpen, message, nextEvent }
```

---

## 6. AI Prompt Strategy & Cost Control

### Model Selection
- Haiku (claude-haiku-4-5): background position risk checks, quick status, OI alerts
- Sonnet (claude-sonnet-4-6): deep entry signal analysis, user-triggered chat

### Caching Strategy
Cache key = SHA256( symbol + RSI_rounded + MACD_signal + PCR_rounded + spot_bucket )
spot_bucket = Math.Round(spot / 50) * 50  (rounds to nearest 50 points)
TTL = 5 minutes

### Cost Controls
1. Cache hit -> return cached, no API call
2. Outside market hours -> no AI calls at all
3. User rate limit: 10 calls/hour (429 if exceeded)
4. max_tokens: 800 for Haiku calls, 1000 for Sonnet calls
5. System prompt designed to output JSON (structured, no prose fluff)

### Per-Call Cost Estimates
- Haiku risk check: ~500 tokens total -> $0.00035
- Sonnet deep signal: ~1200 tokens total -> $0.015
- Daily cost (personal use): ~$0.10-0.30

---

## 7. Responsive Design Breakpoints

```css
/* Mobile first */
Default:    320px-767px   → single column, bottom nav
md:         768px-1199px  → two column, side nav collapsible
lg:         1200px+       → three column, full dashboard
```

Touch targets: minimum 44x44px
Charts: always 100% width, height responsive to container
Bottom nav (mobile): Home, Positions, Chain, Backtest, Chat

---

## 8. Local Dev Setup (Mac)

```bash
# One-time setup
brew install postgresql@16
brew services start postgresql@16
createdb optionsedge_dev

# Install .NET 10 SDK from https://dotnet.microsoft.com

# Backend
cd backend
dotnet restore
dotnet ef database update --project src/OptionsEdge.API
dotnet run --project src/OptionsEdge.API

# Frontend (separate terminal)
cd frontend
npm install
npm run dev

# Ports
# API:      https://localhost:5001
# Frontend: http://localhost:5173
# SignalR:  https://localhost:5001/hubs/market
```

### Required: appsettings.Development.json (gitignored)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=optionsedge_dev;Username=postgres"
  },
  "Claude": {
    "ApiKey": "sk-ant-YOUR-KEY",
    "HaikuModel": "claude-haiku-4-5-20251001",
    "SonnetModel": "claude-sonnet-4-6"
  },
  "Jwt": {
    "Secret": "min-32-char-local-dev-secret-here",
    "Issuer": "OptionsEdge",
    "Audience": "OptionsEdge",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  }
}
```

---

## 9. Future: E2E Hosting

- Backend: Docker container
- Frontend: Static build via Nginx
- DB: PostgreSQL on E2E managed or self-hosted
- SSL: Let's Encrypt
- Secrets: E2E environment variables
- CI/CD: GitHub Actions
