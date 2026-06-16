import { useEffect, useRef, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import { getAccessToken, indicatorsApi } from '../services/api'
import { useAppStore } from '../store/appStore'
import type { MacdIndicator, RsiIndicator, Signal } from '../types'

export type ConnectionState = 'connecting' | 'connected' | 'reconnecting' | 'disconnected'

// Exponential backoff: 0, 1s, 2s, 4s, 8s, 16s, 30s, 30s, ...
const RETRY_DELAYS = [0, 1000, 2000, 4000, 8000, 16000, 30000]
const SYMBOLS = ['NIFTY', 'BANKNIFTY'] as const

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

interface IndicatorUpdateEvent {
  symbol: string
  rsi: RsiIndicator
  macd: MacdIndicator
  supertrendSignal: string
  timestamp: string
}

function subscribeToSymbols(connection: signalR.HubConnection) {
  for (const symbol of SYMBOLS) {
    connection.invoke('SubscribeToSymbol', symbol).catch(() => {})
  }
}

export function useSignalR(url: string) {
  const [connectionState, setConnectionState] = useState<ConnectionState>('disconnected')
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url, {
        skipNegotiation: false,
        accessTokenFactory: () => getAccessToken() ?? '',
      })
      .withAutomaticReconnect(RETRY_DELAYS)
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.onreconnecting(() => setConnectionState('reconnecting'))
    connection.onreconnected(() => {
      setConnectionState('connected')
      subscribeToSymbols(connection)
    })
    connection.onclose(() => setConnectionState('disconnected'))

    connection.on('PriceUpdate', (event: PriceUpdateEvent) => {
      const store = useAppStore.getState()
      const existing = store.snapshots[event.symbol]

      if (existing) {
        store.setSnapshot({
          ...existing,
          ltp: event.ltp,
          change: event.change,
          changePct: event.changePct,
          timestamp: event.timestamp,
        })
      }
    })

    connection.on('MarketStatus', (event: MarketStatusEvent) => {
      useAppStore.getState().setMarketStatus(event)
    })

    connection.on('IndicatorUpdate', (event: IndicatorUpdateEvent) => {
      indicatorsApi.getIndicators(event.symbol)
        .then((data) => useAppStore.getState().setIndicators(event.symbol, data))
        .catch(() => {})
    })

    connection.on('NewSignal', (signal: Signal) => {
      useAppStore.getState().prependSignal(signal)
    })

    connection.on('AutoSignalGenerated', (signal: Signal) => {
      useAppStore.getState().addSignal(signal)

      if ('Notification' in window && Notification.permission === 'granted') {
        new Notification('OptionsEdge Signal', {
          body: `${signal.symbol} ${signal.signalType} - ${signal.strike} ${signal.optionType} - Confidence ${signal.confidence}%`,
          icon: '/favicon.ico',
        })
      }
    })

    connectionRef.current = connection
    setConnectionState('connecting')

    connection
      .start()
      .then(() => {
        setConnectionState('connected')
        subscribeToSymbols(connection)
      })
      .catch(() => setConnectionState('disconnected'))

    return () => {
      connection.stop()
    }
  }, [url])

  return { connectionState, connectionRef }
}
