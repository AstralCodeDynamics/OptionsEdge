import axios from 'axios'
import type { MarketSnapshot, MarketStatus, Candle, IndicatorsResponse, OptionsChain } from '../types'

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

export default api
