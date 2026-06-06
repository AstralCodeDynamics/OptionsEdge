import axios from 'axios'
import type { MarketSnapshot, MarketStatus, Candle, IndicatorsResponse, OptionsChain, Signal, Position, Alert } from '../types'

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

export default api
