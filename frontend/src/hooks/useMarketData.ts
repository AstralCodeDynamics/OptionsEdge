import { useCallback, useEffect, useRef, useState } from 'react'
import { indicatorsApi, marketApi } from '../services/api'
import { useAppStore } from '../store/appStore'
import { useSignalR } from './useSignalR'

const HUB_URL = import.meta.env.VITE_HUB_URL as string
const MARKET_REFRESH_SECONDS = 30

export function useMarketData() {
  const [refreshInSeconds, setRefreshInSeconds] = useState(MARKET_REFRESH_SECONDS)
  const lastRefreshAtRef = useRef(Date.now())
  const setSnapshot = useAppStore((s) => s.setSnapshot)
  const setMarketStatus = useAppStore((s) => s.setMarketStatus)
  const setIndicators = useAppStore((s) => s.setIndicators)
  const marketDataConnected = useAppStore((s) => s.marketDataConnected)
  const marketDataFresh = useAppStore((s) => s.marketDataFresh)
  const setMarketDataConnected = useAppStore((s) => s.setMarketDataConnected)
  const setMarketDataFresh = useAppStore((s) => s.setMarketDataFresh)
  const growwConnected = useAppStore((s) => s.growwStatus?.connected)
  const { connectionState } = useSignalR(HUB_URL)

  const refreshMarketData = useCallback(() => {
    lastRefreshAtRef.current = Date.now()
    setRefreshInSeconds(MARKET_REFRESH_SECONDS)

    marketApi.getSnapshots()
      .then((response) => {
        setMarketDataConnected(response.isGrowwConnected)
        setMarketDataFresh(response.isDataFresh)
        if (response.isGrowwConnected && response.isDataFresh) {
          response.data?.forEach((snapshot) => setSnapshot(snapshot))
        }
      })
      .catch(() => {})

    marketApi.getStatus()
      .then(setMarketStatus)
      .catch(() => {})

    for (const symbol of ['NIFTY', 'BANKNIFTY']) {
      indicatorsApi.getIndicators(symbol)
        .then((response) => {
          setMarketDataConnected(response.isGrowwConnected)
          setMarketDataFresh(response.isDataFresh)
          if (response.isGrowwConnected && response.isDataFresh && response.data) {
            setIndicators(symbol, response.data)
          }
        })
        .catch(() => {})
    }
  }, [setIndicators, setMarketDataConnected, setMarketDataFresh, setMarketStatus, setSnapshot])

  useEffect(() => {
    refreshMarketData()

    const refreshId = setInterval(refreshMarketData, MARKET_REFRESH_SECONDS * 1000)
    const countdownId = setInterval(() => {
      const elapsedSeconds = Math.floor((Date.now() - lastRefreshAtRef.current) / 1000)
      setRefreshInSeconds(Math.max(MARKET_REFRESH_SECONDS - elapsedSeconds, 0))
    }, 1000)

    return () => {
      clearInterval(refreshId)
      clearInterval(countdownId)
    }
  }, [growwConnected, refreshMarketData])

  return {
    connectionState,
    isGrowwConnected: marketDataConnected,
    isDataFresh: marketDataFresh,
    refreshInSeconds,
    refreshIntervalSeconds: MARKET_REFRESH_SECONDS,
  }
}
