import axios from 'axios'
import type {
  MarketSnapshot, MarketStatus, Candle, IndicatorsResponse, OptionsChain, Signal, Position, Alert,
  ChatMessage, BacktestResult, BacktestHistoryResponse, StrategyLeg, PayoffResult,
  RegisterRequest, LoginRequest, ResetPasswordRequest, ChangePasswordRequest,
  AuthResponse, TwoFactorRequiredResponse, MeResponse, EnableTwoFactorResponse, VerifyTwoFactorSetupResponse,
  UsageStats, SignalHistoryResponse, SignalPreferenceRequest, SignalPreferenceResponse,
} from '../types'
import { useAppStore } from '../store/appStore'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

// Tokens are kept in module-level memory only — never localStorage/sessionStorage.
let _accessToken: string | null = null
let _refreshToken: string | null = null
let _accessTokenExpiryMs: number | null = null
let _refreshTimer: ReturnType<typeof setTimeout> | null = null

function clearRefreshTimer(): void {
  if (_refreshTimer) {
    clearTimeout(_refreshTimer)
    _refreshTimer = null
  }
}

function parseJwtPayload(token: string): { exp?: number; iat?: number } | null {
  const [, payload] = token.split('.')
  if (!payload) return null

  try {
    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/')
    const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=')
    return JSON.parse(atob(padded)) as { exp?: number; iat?: number }
  } catch {
    return null
  }
}

function tokenExpiryMs(access: string, accessTokenExpiry?: string): number | null {
  if (accessTokenExpiry) {
    const parsed = Date.parse(accessTokenExpiry)
    if (!Number.isNaN(parsed)) return parsed
  }

  const exp = parseJwtPayload(access)?.exp
  return exp ? exp * 1000 : null
}

function scheduleProactiveRefresh(): void {
  clearRefreshTimer()

  if (!_refreshToken || !_accessToken || !_accessTokenExpiryMs) return

  const now = Date.now()
  const payload = parseJwtPayload(_accessToken)
  const issuedAtMs = payload?.iat ? payload.iat * 1000 : now
  const lifetimeMs = Math.max(_accessTokenExpiryMs - issuedAtMs, 0)
  const fallbackRemainingMs = Math.max(_accessTokenExpiryMs - now, 0)
  const refreshAtMs = lifetimeMs > 0
    ? issuedAtMs + lifetimeMs * 0.8
    : now + fallbackRemainingMs * 0.8
  const delayMs = Math.max(0, refreshAtMs - now)

  _refreshTimer = setTimeout(() => {
    proactiveRefresh().catch(() => {})
  }, delayMs)
}

async function refreshTokens(refresh: string): Promise<AuthResponse> {
  const data = await authApi.refresh(refresh)
  setTokens(data.accessToken, data.refreshToken, data.accessTokenExpiry)
  return data
}

async function proactiveRefresh(): Promise<void> {
  if (_isRefreshing) return

  const refresh = getRefreshToken()
  if (!refresh) return

  _isRefreshing = true
  try {
    const data = await refreshTokens(refresh)
    _refreshQueue.forEach((p) => p.resolve(data.accessToken))
    _refreshQueue = []
  } catch (refreshError) {
    _refreshQueue.forEach((p) => p.reject(refreshError))
    _refreshQueue = []
    clearTokens()
    useAppStore.getState().logout()
    redirectToLogin()
  } finally {
    _isRefreshing = false
  }
}

export function setTokens(access: string, refresh: string, accessTokenExpiry?: string): void {
  _accessToken = access
  _refreshToken = refresh
  _accessTokenExpiryMs = tokenExpiryMs(access, accessTokenExpiry)
  scheduleProactiveRefresh()
}

export function clearTokens(): void {
  _accessToken = null
  _refreshToken = null
  _accessTokenExpiryMs = null
  clearRefreshTimer()
}

export function getAccessToken(): string | null {
  return _accessToken
}

export function getRefreshToken(): string | null {
  return _refreshToken
}

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  if (_accessToken) {
    config.headers.Authorization = `Bearer ${_accessToken}`
  }
  return config
})

let _isRefreshing = false
let _refreshQueue: Array<{
  resolve: (token: string) => void
  reject: (error: unknown) => void
}> = []

function redirectToLogin(): void {
  if (window.location.pathname !== '/login') {
    window.location.href = '/login'
  }
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const status = error.response?.status
    const originalRequest = error.config

    if (status === 401 && originalRequest && !originalRequest._retry) {
      const refresh = getRefreshToken()
      if (!refresh) {
        clearTokens()
        useAppStore.getState().logout()
        redirectToLogin()
        return Promise.reject(error)
      }

      if (_isRefreshing) {
        return new Promise((resolve, reject) => {
          _refreshQueue.push({
            resolve: (token: string) => {
              originalRequest._retry = true
              originalRequest.headers = originalRequest.headers ?? {}
              originalRequest.headers.Authorization = `Bearer ${token}`
              resolve(api(originalRequest))
            },
            reject,
          })
        })
      }

      originalRequest._retry = true
      _isRefreshing = true

      try {
        const data = await refreshTokens(refresh)

        _refreshQueue.forEach((p) => p.resolve(data.accessToken))
        _refreshQueue = []

        originalRequest.headers = originalRequest.headers ?? {}
        originalRequest.headers.Authorization = `Bearer ${data.accessToken}`
        return api(originalRequest)
      } catch (refreshError) {
        _refreshQueue.forEach((p) => p.reject(refreshError))
        _refreshQueue = []

        clearTokens()
        useAppStore.getState().logout()
        redirectToLogin()
        return Promise.reject(refreshError)
      } finally {
        _isRefreshing = false
      }
    }

    if (status === 429) {
      const retryAfter = error.response?.headers?.['retry-after']
      const message = retryAfter
        ? `You're sending requests too quickly. Try again in ${retryAfter}s.`
        : "You're sending requests too quickly. Please slow down and try again shortly."
      error.friendlyMessage = message
    }

    return Promise.reject(error)
  }
)

export const marketApi = {
  getSnapshots: () => api.get<MarketSnapshot[]>('/market/snapshot').then((r) => r.data),
  getSnapshot: (symbol: string) =>
    api.get<MarketSnapshot>(`/market/snapshot/${symbol}`).then((r) => r.data),
  getCandles: (symbol: string) =>
    api.get<Candle[]>(`/market/candles/${symbol}`).then((r) => r.data),
  getStatus: () => api.get<MarketStatus>('/market/status').then((r) => r.data),
}

export const indicatorsApi = {
  getIndicators: (symbol: string) =>
    api.get<IndicatorsResponse>(`/indicators/${symbol}`).then((r) => r.data),
}

export const optionsApi = {
  getChain: (symbol: string, expiry?: string) => {
    const params = expiry ? `?expiry=${expiry}` : ''
    return api.get<OptionsChain>(`/options/chain/${symbol}${params}`).then((r) => r.data)
  },
  getExpiries: (symbol: string) =>
    api.get<string[]>(`/options/expiries/${symbol}`).then((r) => r.data),
  getMaxPain: (symbol: string, expiry?: string) => {
    const params = expiry ? `?expiry=${expiry}` : ''
    return api.get<{ maxPain: number; spot: number; expiry: string }>(
      `/options/maxpain/${symbol}${params}`
    ).then((r) => r.data)
  },
  computePayoff: (legs: StrategyLeg[]) =>
    api.post<PayoffResult>('/options/payoff', {
      legs: legs.map((l) => ({
        symbol: l.symbol,
        strike: l.strike,
        optionType: l.optionType,
        action: l.action,
        lots: l.lots,
        premium: l.premium,
      })),
    }).then((r) => r.data),
}

export const signalsApi = {
  generate: (symbol: string) =>
    api.post<Signal>('/signals/generate', { symbol }).then((r) => r.data),
  getHistory: (params?: { page?: number; pageSize?: number }) => {
    const q = new URLSearchParams()
    q.set('page', String(params?.page ?? 1))
    q.set('pageSize', String(params?.pageSize ?? 20))
    return api.get<SignalHistoryResponse>(`/signals/history?${q}`).then((r) => r.data)
  },
  getById: (id: string) =>
    api.get<Signal>(`/signals/${id}`).then((r) => r.data),
}

export const signalPreferenceApi = {
  getPreferences: () =>
    api.get<SignalPreferenceResponse>('/signals/preferences').then((r) => r.data),
  savePreferences: (data: SignalPreferenceRequest) =>
    api.put<{ message: string }>('/signals/preferences', data).then((r) => r.data),
}

export const positionsApi = {
  getAll: () =>
    api.get<Position[]>('/positions').then((r) => r.data),
  create: (data: {
    symbol: string
    strike: number
    optionType: string
    expiry: string
    entryPrice: number
    quantity: number
    stopLoss: number
    target1: number
    target2?: number
    signalId?: string
  }) => api.post<Position>('/positions', data).then((r) => r.data),
  update: (id: string, data: {
    stopLoss?: number
    target1?: number
    target2?: number
    status?: string
    exitPrice?: number
    exitReason?: string
  }) => api.put<Position>(`/positions/${id}`, data).then((r) => r.data),
  close: (id: string) =>
    api.delete(`/positions/${id}`),
  getPnL: (id: string) =>
    api.get(`/positions/${id}/pnl`).then((r) => r.data),
}

export const backtestApi = {
  run: (data: {
    symbol: string
    strategy: string
    entryCondition: string
    exitCondition: string
    periodDays: number
    lots: number
    targetPoints?: number
    stopLossPoints?: number
    adxFilterEnabled?: boolean
  }) => api.post<BacktestResult>('/backtest/run', data).then((r) => r.data),
  getHistory: (params?: { page?: number; pageSize?: number }) => {
    const q = new URLSearchParams()
    q.set('page', String(params?.page ?? 1))
    q.set('pageSize', String(params?.pageSize ?? 8))
    return api.get<BacktestHistoryResponse>(`/backtest/history?${q}`).then((r) => r.data)
  },
}

export const alertsApi = {
  getAlerts: (params?: { unread?: boolean; limit?: number }) => {
    const q = new URLSearchParams()
    if (params?.unread != null) q.set('unread', String(params.unread))
    if (params?.limit != null) q.set('limit', String(params.limit))
    return api.get<Alert[]>(`/alerts?${q}`).then((r) => r.data)
  },
  markRead: (id: string) =>
    api.put<Alert>(`/alerts/${id}/read`).then((r) => r.data),
  markAllRead: () =>
    api.put('/alerts/read-all'),
}

export interface ChatStreamHandlers {
  onDelta: (text: string) => void
  onDone: (meta: { modelUsed?: string; inputTokens?: number; outputTokens?: number; costUsd?: number }) => void
  onError: (message: string) => void
}

// Raw SSE parsing: the backend streams `event: <type>\ndata: <json>\n\n` frames.
// axios doesn't support streaming response bodies, so we use fetch + ReadableStream here.
async function streamChatMessage(
  sessionId: string,
  message: string,
  handlers: ChatStreamHandlers,
  signal?: AbortSignal,
): Promise<void> {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' }
  if (_accessToken) headers.Authorization = `Bearer ${_accessToken}`

  const res = await fetch(`${API_BASE_URL}/chat/message`, {
    method: 'POST',
    headers,
    body: JSON.stringify({ sessionId, message }),
    signal,
  })

  if (!res.ok || !res.body) {
    const body = await res.json().catch(() => null)
    handlers.onError(body?.error ?? 'Failed to send message')
    return
  }

  const reader = res.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''

  while (true) {
    const { done, value } = await reader.read()
    if (done) break
    buffer += decoder.decode(value, { stream: true })

    // Split on double newline — each SSE event ends with \n\n.
    const events = buffer.split('\n\n')
    buffer = events.pop() ?? ''

    for (const event of events) {
      if (!event.trim()) continue

      const lines = event.split('\n')
      let type = ''
      let data = ''

      for (const rawLine of lines) {
        const line = rawLine.replace(/\r$/, '')
        if (line.startsWith('event:')) {
          type = line.slice('event:'.length).trim()
        } else if (line.startsWith('data:')) {
          data = line.slice('data:'.length).trim()
        }
      }

      if (!type || !data) continue

      try {
        const payload = JSON.parse(data) as {
          text?: string
          modelUsed?: string
          inputTokens?: number
          outputTokens?: number
          costUsd?: number
          error?: string
        }

        if (type === 'delta' && payload.text) {
          handlers.onDelta(payload.text)
        } else if (type === 'done') {
          handlers.onDone(payload)
        } else if (type === 'error') {
          handlers.onError(payload.error ?? 'Something went wrong')
          return
        }
      } catch {
        // Malformed JSON — skip this event.
      }
    }
  }
}

export interface GrowwStatus {
  enabled: boolean
  hasCredentials: boolean
  connected: boolean
  expiresAt?: string | null
  orderPlacementEnabled?: boolean
  error?: string | null
}

export interface GrowwCredentialsResult {
  success: boolean
  message: string
}

export interface PlaceOrderRequest {
  positionId?: string
  symbol: string
  strike: number
  optionType: string
  expiry: string
  quantity: number
  price: number
  orderType: string
  transactionType: string
}

export interface PlaceOrderResult {
  orderId: string
  status: string
  tradingSymbol: string
  quantity: number
}

export const aiApi = {
  getStatus: () =>
    api.get<{ hasKey: boolean; message: string }>('/ai/credentials/status').then((r) => r.data),
  saveKey: (apiKey: string) =>
    api.post<{ message: string }>('/ai/credentials', { apiKey }).then((r) => r.data),
  removeKey: () =>
    api.delete('/ai/credentials'),
}

export const growwApi = {
  getStatus: () => api.get<GrowwStatus>('/groww/status').then((r) => r.data),
  saveCredentials: (apiKey: string, apiSecret: string) =>
    api.post<GrowwCredentialsResult>('/groww/credentials', { apiKey, apiSecret }).then((r) => r.data),
  removeCredentials: () => api.delete('/groww/credentials').then((r) => r.data),
  placeOrder: (data: PlaceOrderRequest) =>
    api.post<PlaceOrderResult>('/orders/place', data).then((r) => r.data),
  cancelOrder: (orderId: string) =>
    api.post(`/orders/${orderId}/cancel`).then((r) => r.data),
}

export const chatApi = {
  newSession: () =>
    api.post<{ sessionId: string }>('/chat/new-session').then((r) => r.data),
  getHistory: (sessionId: string) =>
    api
      .get<{ sessionId: string; messages: Array<Omit<ChatMessage, 'userId' | 'sessionId'>> }>(
        '/chat/history',
        { params: { sessionId } },
      )
      .then((r) => r.data),
  streamMessage: streamChatMessage,
}

export const authApi = {
  register: (data: RegisterRequest) =>
    api.post<{ message: string; requiresEmailConfirmation: boolean }>('/auth/register', data).then((r) => r.data),

  confirmEmail: (userId: string, token: string) =>
    api.post<{ message: string }>('/auth/confirm-email', { userId, token }).then((r) => r.data),

  resendConfirmation: (email: string) =>
    api.post<{ message: string }>('/auth/resend-confirmation', { email }).then((r) => r.data),

  login: (data: LoginRequest) =>
    api.post<AuthResponse | TwoFactorRequiredResponse>('/auth/login', data).then((r) => r.data),

  twoFactor: (email: string, code: string) =>
    api.post<AuthResponse>('/auth/two-factor', { email, code }).then((r) => r.data),

  refresh: (refreshToken: string) =>
    api.post<AuthResponse>('/auth/refresh', { refreshToken }).then((r) => r.data),

  logout: (refreshToken: string) =>
    api.post('/auth/logout', { refreshToken }),

  forgotPassword: (email: string) =>
    api.post<{ message: string }>('/auth/forgot-password', { email }).then((r) => r.data),

  resetPassword: (data: ResetPasswordRequest) =>
    api.post<{ message: string }>('/auth/reset-password', data).then((r) => r.data),

  me: () =>
    api.get<MeResponse>('/auth/me').then((r) => r.data),

  enableTwoFactor: () =>
    api.post<EnableTwoFactorResponse>('/auth/enable-2fa').then((r) => r.data),

  verifyTwoFactorSetup: (code: string) =>
    api.post<VerifyTwoFactorSetupResponse>('/auth/verify-2fa-setup', { code }).then((r) => r.data),

  disableTwoFactor: (password: string) =>
    api.post('/auth/disable-2fa', { password }),

  changePassword: (data: ChangePasswordRequest) =>
    api.post('/auth/change-password', data),
}

export const usageApi = {
  getStats: () =>
    api.get<UsageStats>('/usage/stats').then((r) => r.data),
}

export default api
