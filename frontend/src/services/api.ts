import axios from 'axios'
import type { MarketSnapshot, MarketStatus, Candle, IndicatorsResponse, OptionsChain, Signal, Position, Alert, ChatMessage } from '../types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

let authToken: string | null = null

export function setAuthToken(token: string): void {
  authToken = token
}

export function clearAuthToken(): void {
  authToken = null
}

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  if (authToken) {
    config.headers.Authorization = `Bearer ${authToken}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      clearAuthToken()
      window.location.href = '/login'
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
}

export const signalsApi = {
  generate: (symbol: string) =>
    api.post<Signal>('/signals/generate', { symbol }).then((r) => r.data),
  getHistory: (symbol?: string, limit = 20) => {
    const params = new URLSearchParams()
    if (symbol) params.set('symbol', symbol)
    params.set('limit', String(limit))
    return api.get<Signal[]>(`/signals/history?${params}`).then((r) => r.data)
  },
  getById: (id: string) =>
    api.get<Signal>(`/signals/${id}`).then((r) => r.data),
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
  if (authToken) headers.Authorization = `Bearer ${authToken}`

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
  let eventType = ''

  while (true) {
    const { done, value } = await reader.read()
    if (done) break
    buffer += decoder.decode(value, { stream: true })

    let nlIdx = buffer.indexOf('\n')
    while (nlIdx >= 0) {
      const line = buffer.slice(0, nlIdx).replace(/\r$/, '')
      buffer = buffer.slice(nlIdx + 1)

      if (line.startsWith('event:')) {
        eventType = line.slice('event:'.length).trim()
      } else if (line.startsWith('data:')) {
        const json = line.slice('data:'.length).trim()
        if (json) {
          const payload = JSON.parse(json) as {
            text?: string
            modelUsed?: string
            inputTokens?: number
            outputTokens?: number
            costUsd?: number
            error?: string
          }
          if (eventType === 'delta' && payload.text) {
            handlers.onDelta(payload.text)
          } else if (eventType === 'done') {
            handlers.onDone(payload)
          } else if (eventType === 'error') {
            handlers.onError(payload.error ?? 'Something went wrong')
          }
        }
      } else if (line === '') {
        eventType = ''
      }

      nlIdx = buffer.indexOf('\n')
    }
  }
}

export interface GrowwStatus {
  enabled: boolean
  connected: boolean
  expiresAt?: string | null
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

export const growwApi = {
  connect: (totp: string) =>
    api.post<{ connected: boolean; expiresAt: string; importedPositions: number }>(
      '/groww/connect',
      { totp },
    ).then((r) => r.data),
  getStatus: () => api.get<GrowwStatus>('/groww/status').then((r) => r.data),
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

export default api
