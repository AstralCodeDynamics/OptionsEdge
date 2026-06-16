import { useEffect } from 'react'
import api from '../services/api'
import { indicatorsApi } from '../services/api'
import { useAppStore } from '../store/appStore'
import { useSignalR } from './useSignalR'
import type { MarketSnapshot, MarketStatus } from '../types'

const HUB_URL = import.meta.env.VITE_HUB_URL as string

export function useMarketData() {
  const setSnapshot = useAppStore((s) => s.setSnapshot)
  const setMarketStatus = useAppStore((s) => s.setMarketStatus)
  const setIndicators = useAppStore((s) => s.setIndicators)
  const { connectionState } = useSignalR(HUB_URL)

  // Initial REST fetch for both symbols
  useEffect(() => {
    api
      .get<MarketSnapshot[]>('/market/snapshot')
      .then((res) => res.data.forEach((snap) => setSnapshot(snap)))
      .catch(() => {})

    api
      .get<MarketStatus>('/market/status')
      .then((res) => setMarketStatus(res.data))
      .catch(() => {})

    for (const symbol of ['NIFTY', 'BANKNIFTY']) {
      indicatorsApi.getIndicators(symbol)
        .then((data) => setIndicators(symbol, data))
        .catch(() => {})
    }
  }, [])

  return { connectionState }
}
