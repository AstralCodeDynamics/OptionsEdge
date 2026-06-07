import { useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { useAppStore } from '../store/appStore'
import { alertsApi } from '../services/api'
import type { Alert } from '../types'

const DEV_USER_ID = import.meta.env.VITE_DEV_USER_ID
  ?? '00000000-0000-0000-0000-000000000001'

export function useAlerts(hubUrl: string) {
  const addAlert  = useAppStore((s) => s.addAlert)
  const setAlerts = useAppStore((s) => s.setAlerts)
  const connRef   = useRef<signalR.HubConnection | null>(null)
  const timerRef  = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    // Initial load
    alertsApi.getAlerts({ unread: false, limit: 50 }).then(setAlerts).catch(() => {})

    // SignalR
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { skipNegotiation: false })
      .withAutomaticReconnect([0, 1000, 2000, 4000, 8000, 16000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.on('NewAlert', (alert: Alert) => addAlert(alert))

    connRef.current = connection

    connection
      .start()
      .then(() => connection.invoke('SubscribeToAlerts', DEV_USER_ID).catch(() => {}))
      .catch(() => {})

    // Polling fallback when SignalR is disconnected
    timerRef.current = setInterval(() => {
      if (connection.state !== signalR.HubConnectionState.Connected) {
        alertsApi
          .getAlerts({ unread: true, limit: 20 })
          .then((list) => list.forEach((a) => addAlert(a)))
          .catch(() => {})
      }
    }, 30_000)

    return () => {
      connection.stop()
      if (timerRef.current) clearInterval(timerRef.current)
    }
  }, [hubUrl, addAlert, setAlerts])
}
