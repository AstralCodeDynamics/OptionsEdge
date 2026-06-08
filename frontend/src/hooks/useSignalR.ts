import { useEffect, useRef, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import { getAccessToken } from '../services/api'

export type ConnectionState = 'connecting' | 'connected' | 'reconnecting' | 'disconnected'

// Exponential backoff: 0, 1s, 2s, 4s, 8s, 16s, 30s, 30s, ...
const RETRY_DELAYS = [0, 1000, 2000, 4000, 8000, 16000, 30000]

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
    connection.onreconnected(() => setConnectionState('connected'))
    connection.onclose(() => setConnectionState('disconnected'))

    connectionRef.current = connection
    setConnectionState('connecting')

    connection
      .start()
      .then(() => setConnectionState('connected'))
      .catch(() => setConnectionState('disconnected'))

    return () => {
      connection.stop()
    }
  }, [url])

  return { connectionState, connectionRef }
}
