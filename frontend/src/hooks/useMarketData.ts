import { useEffect } from 'react'
import { indicatorsApi, marketApi } from '../services/api'
import { useAppStore } from '../store/appStore'
import { useSignalR } from './useSignalR'

const HUB_URL = import.meta.env.VITE_HUB_URL as string

export function useMarketData() {
  const setSnapshot = useAppStore((s) => s.setSnapshot)
  const setMarketStatus = useAppStore((s) => s.setMarketStatus)
  const setIndicators = useAppStore((s) => s.setIndicators)
  const marketDataConnected = useAppStore((s) => s.marketDataConnected)
  const setMarketDataConnected = useAppStore((s) => s.setMarketDataConnected)
  const growwConnected = useAppStore((s) => s.growwStatus?.connected)
  const { connectionState } = useSignalR(HUB_URL)

  // Initial REST fetch for both symbols
  useEffect(() => {
    marketApi.getSnapshots()
      .then((response) => {
        setMarketDataConnected(response.isGrowwConnected)
        if (response.isGrowwConnected) {
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
          if (response.isGrowwConnected && response.data) {
            setIndicators(symbol, response.data)
          }
        })
        .catch(() => {})
    }
  }, [growwConnected, setIndicators, setMarketDataConnected, setMarketStatus, setSnapshot])

  return { connectionState, isGrowwConnected: marketDataConnected }
}
