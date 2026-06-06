import { useEffect } from 'react'
import api from '../services/api'
import { useAppStore } from '../store/appStore'
import { useSignalR } from './useSignalR'
import type { MarketSnapshot, MarketStatus } from '../types'

interface PriceUpdateEvent {
  symbol: string
  ltp: number
  change: number
  changePct: number
  timestamp: string
}

interface MarketStatusEvent {
  isOpen: boolean
  message: string
  nextEvent: string
}

const HUB_URL = import.meta.env.VITE_HUB_URL as string

export function useMarketData() {
  const setSnapshot = useAppStore((s) => s.setSnapshot)
  const setMarketStatus = useAppStore((s) => s.setMarketStatus)
  const { connectionState, connectionRef } = useSignalR(HUB_URL)

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
  }, [])

  // Subscribe to SignalR groups and register event handlers
  useEffect(() => {
    const conn = connectionRef.current
    if (!conn || connectionState !== 'connected') return

    // Subscribe to both index groups
    conn.invoke('SubscribeToSymbol', 'NIFTY').catch(() => {})
    conn.invoke('SubscribeToSymbol', 'BANKNIFTY').catch(() => {})

    const handlePriceUpdate = (event: PriceUpdateEvent) => {
      // Merge live price into existing snapshot
      const snapshots = useAppStore.getState().snapshots
      const existing = snapshots[event.symbol]
      if (existing) {
        setSnapshot({
          ...existing,
          ltp: event.ltp,
          change: event.change,
          changePct: event.changePct,
          timestamp: event.timestamp,
        })
      }
    }

    const handleMarketStatus = (event: MarketStatusEvent) => {
      setMarketStatus(event)
    }

    conn.on('PriceUpdate', handlePriceUpdate)
    conn.on('MarketStatus', handleMarketStatus)

    return () => {
      conn.off('PriceUpdate', handlePriceUpdate)
      conn.off('MarketStatus', handleMarketStatus)
    }
  }, [connectionState])

  return { connectionState }
}
