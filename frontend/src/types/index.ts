export interface MarketSnapshot {
  symbol: string
  ltp: number
  open: number
  high: number
  low: number
  close: number
  change: number
  changePct: number
  vix: number
  pcr: number
  fiiFlow: number
  diiFlow: number
  timestamp: string
  dataSource: 'groww_live' | 'mock'
}

export interface Candle {
  time: number
  open: number
  high: number
  low: number
  close: number
  volume: number
}

export interface MarketStatus {
  isOpen: boolean
  message: string
  nextEvent: string
}

export interface IndicatorsResponse {
  symbol: string
  rsi: RsiIndicator
  macd: MacdIndicator
  bollingerBands: BollingerBandsIndicator
  adx: AdxIndicator
  ema: EmaIndicator
  supertrend: SupertrendIndicator
  pivots: PivotLevels
  timestamp: string
}

export interface RsiIndicator {
  value: number
  signal: 'Overbought' | 'Oversold' | 'Neutral'
}

export interface MacdIndicator {
  value: number
  signal: number
  histogram: number
  isBullishCross: boolean
}

export interface BollingerBandsIndicator {
  upper: number
  middle: number
  lower: number
  isSqueeze: boolean
}

export interface AdxIndicator {
  value: number
  strength: 'Weak' | 'Moderate' | 'Strong'
}

export interface EmaIndicator {
  ema9: number
  ema20: number
  ema50: number
  ema200: number
  priceAboveEma20: boolean
  priceAboveEma50: boolean
}

export interface SupertrendIndicator {
  value: number
  isBullish: boolean
}

export interface PivotLevels {
  r3: number
  r2: number
  r1: number
  pivot: number
  s1: number
  s2: number
  s3: number
}

export interface OptionsChain {
  symbol: string
  expiry: string
  spot: number
  pcr: number
  maxPain: number
  rows: OptionsChainRow[]
}

export interface OptionsChainRow {
  strike: number
  isAtm: boolean
  ce: OptionLeg
  pe: OptionLeg
}

export interface OptionLeg {
  ltp: number
  oi: number
  oiChange: number
  volume: number
  iv: number
  delta: number
  gamma: number
  theta: number
  vega: number
}

export interface StrategyLeg {
  symbol: string
  strike: number
  optionType: 'CE' | 'PE'
  action: 'BUY' | 'SELL'
  lots: number
  premium: number
  delta?: number
  gamma?: number
  theta?: number
  vega?: number
}

export interface PayoffPoint {
  price: number
  pnl: number
}

export interface PayoffResult {
  payoffCurve: PayoffPoint[]
  maxProfit: number | null
  maxProfitUnlimited: boolean
  maxLoss: number | null
  maxLossUnlimited: boolean
  breakevens: number[]
}

export interface Signal {
  id: string
  symbol: string
  signalType: 'ENTRY' | 'EXIT' | 'HOLD' | 'WATCH'
  optionType: 'CE' | 'PE'
  strike: number
  expiry: string
  entryLow: number
  entryHigh: number
  stopLoss: number
  target1: number
  target2?: number
  confidence: number
  riskReward: number
  rationale: string[]
  modelUsed: string
  inputTokens: number
  outputTokens: number
  costUsd: number
  validUntil: string
  createdAt: string
  fromCache?: boolean
}

export interface Position {
  id: string
  userId: string
  symbol: string
  strike: number
  optionType: 'CE' | 'PE'
  expiry: string
  entryPrice: number
  quantity: number
  stopLoss: number
  target1: number
  target2?: number
  signalId?: string
  status: 'active' | 'closed' | 'expired'
  exitPrice?: number
  exitReason?: string
  closedAt?: string
  createdAt: string
  currentLtp?: number
  pnl?: number
  pnlPct?: number
}

export interface Alert {
  id: string
  userId: string
  positionId: string
  severity: 'Info' | 'Warning' | 'Danger'
  alertType: string
  message: string
  isRead: boolean
  createdAt: string
}

export interface ChatMessage {
  id: string
  userId: string
  sessionId: string
  role: 'user' | 'assistant'
  content: string
  modelUsed?: string
  inputTokens?: number
  outputTokens?: number
  costUsd?: number
  createdAt: string
}

export interface BacktestTradeLogEntry {
  entryDate: string
  exitDate: string
  contract: string
  entryPrice: number
  exitPrice: number
  pnL: number
  exitReason: string
}

export interface BacktestResult {
  id: string
  symbol: string
  strategy: string
  entryCondition: string
  exitCondition: string
  periodDays: number
  lots: number
  winRate: number
  totalTrades: number
  netPnl: number
  maxDrawdown: number
  sharpeRatio: number
  profitFactor: number
  avgWin: number
  avgLoss: number
  tradeLog: BacktestTradeLogEntry[]
  createdAt: string
}

export interface AuthUser {
  id: string
  email: string
  displayName: string
  subscriptionPlan: string
  twoFactorEnabled: boolean
  emailConfirmed: boolean
}

export interface RegisterRequest {
  email: string
  password: string
  displayName: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface ResetPasswordRequest {
  email: string
  token: string
  newPassword: string
  confirmPassword: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  accessTokenExpiry: string
  userId: string
  displayName: string
  email: string
  subscriptionPlan: string
  twoFactorEnabled: boolean
}

export interface TwoFactorRequiredResponse {
  twoFactorRequired: boolean
  email: string
}

export interface UsageStats {
  callsToday: number
  callsLimit: number
  costToday: number
  walletBalance: number
}

export interface MeResponse {
  id: string
  email: string
  displayName: string
  subscriptionPlan: string
  walletBalance: number
  emailConfirmed: boolean
  twoFactorEnabled: boolean
  createdAt: string
}

export interface EnableTwoFactorResponse {
  sharedKey: string
  authenticatorUri: string
}

export interface VerifyTwoFactorSetupResponse {
  enabled: boolean
  recoveryCodes: string[]
}
