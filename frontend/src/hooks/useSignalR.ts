import { useEffect, useRef, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import { getAccessToken, indicatorsApi } from '../services/api'
import { useAppStore } from '../store/appStore'
import type { Alert, MacdIndicator, RsiIndicator, Signal } from '../types'

export type ConnectionState = 'connecting' | 'connected' | 'reconnecting' | 'disconnected'

// Exponential backoff: 0, 1s, 2s, 4s, 8s, 16s, 30s, 30s, ...
const RETRY_DELAYS = [0, 1000, 2000, 4000, 8000, 16000, 30000]
const SYMBOLS = ['NIFTY', 'BANKNIFTY'] as const
const STOP_DELAY_MS = 1000

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

let sharedConnection: signalR.HubConnection | null = null
let sharedConnectionUrl: string | null = null
let sharedStartPromise: Promise<void> | null = null
let sharedSubscriberCount = 0
let sharedStopTimer: ReturnType<typeof setTimeout> | null = null
let sharedConnectionState: ConnectionState = 'disconnected'

const stateListeners = new Set<(state: ConnectionState) => void>()

function emitConnectionState(state: ConnectionState) {
  sharedConnectionState = state
  stateListeners.forEach((listener) => listener(state))
}

function clearSharedStopTimer() {
  if (sharedStopTimer) {
    clearTimeout(sharedStopTimer)
    sharedStopTimer = null
  }
}

function createConnection(url: string) {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(url, {
      skipNegotiation: false,
      accessTokenFactory: () => getAccessToken() ?? '',
    })
    .withAutomaticReconnect(RETRY_DELAYS)
    .configureLogging(signalR.LogLevel.Warning)
    .build()

  connection.onreconnecting(() => emitConnectionState('reconnecting'))
  connection.onreconnected(() => {
    emitConnectionState('connected')
    subscribeToSymbols(connection)
  })
  connection.onclose(() => emitConnectionState('disconnected'))

  connection.on('priceupdate', (event: PriceUpdateEvent) => {
    const store = useAppStore.getState()
    if (store.marketDataConnected !== true) return
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

  connection.on('marketstatus', (event: MarketStatusEvent) => {
    useAppStore.getState().setMarketStatus(event)
  })

  connection.on('indicatorupdate', (event: IndicatorUpdateEvent) => {
    if (useAppStore.getState().marketDataConnected !== true) return
    indicatorsApi.getIndicators(event.symbol)
      .then((response) => {
        const store = useAppStore.getState()
        store.setMarketDataConnected(response.isGrowwConnected)
        if (response.isGrowwConnected && response.data) {
          store.setIndicators(event.symbol, response.data)
        }
      })
      .catch(() => {})
  })

  connection.on('newsignal', (signal: Signal) => {
    useAppStore.getState().prependSignal(signal)
  })

  connection.on('newalert', (alert: Alert) => {
    useAppStore.getState().addAlert(alert)
  })

  connection.on('autosignalgenerated', (signal: Signal) => {
    useAppStore.getState().addSignal(signal)

    if ('Notification' in window && Notification.permission === 'granted') {
      new Notification('OptionsEdge Signal', {
        body: `${signal.symbol} ${signal.signalType} - ${signal.strike} ${signal.optionType} - Confidence ${signal.confidence}%`,
        icon: '/favicon.ico',
      })
    }
  })

  return connection
}

function getSharedConnection(url: string) {
  if (sharedConnection && sharedConnectionUrl === url) {
    return sharedConnection
  }

  if (sharedConnection) {
    const oldConnection = sharedConnection
    oldConnection.stop().catch(() => {})
  }

  sharedConnection = createConnection(url)
  sharedConnectionUrl = url
  return sharedConnection
}

function startSharedConnection(connection: signalR.HubConnection) {
  if (connection.state === signalR.HubConnectionState.Connected) {
    emitConnectionState('connected')
    return
  }

  if (
    connection.state === signalR.HubConnectionState.Connecting ||
    connection.state === signalR.HubConnectionState.Reconnecting ||
    sharedStartPromise
  ) {
    return
  }

  emitConnectionState('connecting')
  sharedStartPromise = connection
    .start()
    .then(() => {
      emitConnectionState('connected')
      subscribeToSymbols(connection)
    })
    .catch(() => emitConnectionState('disconnected'))
    .finally(() => {
      sharedStartPromise = null
    })
}

function scheduleSharedConnectionStop(connection: signalR.HubConnection) {
  clearSharedStopTimer()
  sharedStopTimer = setTimeout(() => {
    if (sharedSubscriberCount > 0 || sharedConnection !== connection) return

    const stopAfterStart = () => {
      if (sharedSubscriberCount > 0 || sharedConnection !== connection) return

      sharedConnection = null
      sharedConnectionUrl = null
      emitConnectionState('disconnected')
      connection.stop().catch(() => {})
    }

    if (sharedStartPromise) {
      sharedStartPromise.finally(stopAfterStart)
      return
    }

    stopAfterStart()
  }, STOP_DELAY_MS)
}

export function useSignalR(url: string) {
  const [connectionState, setConnectionState] = useState<ConnectionState>(sharedConnectionState)
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    stateListeners.add(setConnectionState)
    setConnectionState(sharedConnectionState)

    if (!getAccessToken()) {
      connectionRef.current = null
      emitConnectionState('disconnected')
      return () => {
        stateListeners.delete(setConnectionState)
      }
    }

    sharedSubscriberCount += 1
    clearSharedStopTimer()
    const connection = getSharedConnection(url)
    connectionRef.current = connection
    startSharedConnection(connection)

    return () => {
      sharedSubscriberCount = Math.max(0, sharedSubscriberCount - 1)
      stateListeners.delete(setConnectionState)
      connectionRef.current = null
      scheduleSharedConnectionStop(connection)
    }
  }, [url])

  return { connectionState, connectionRef }
}
