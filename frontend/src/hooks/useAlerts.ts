import { useEffect } from 'react'
import { useSignalR } from './useSignalR'
import { useAppStore } from '../store/appStore'
import { alertsApi } from '../services/api'

export function useAlerts(hubUrl: string) {
  const setAlerts = useAppStore((s) => s.setAlerts)
  const { connectionState, connectionRef } = useSignalR(hubUrl)

  useEffect(() => {
    alertsApi.getAlerts({ unread: false, page: 1, pageSize: 50 })
      .then((res) => setAlerts(res.items))
      .catch(() => {})
  }, [setAlerts])

  useEffect(() => {
    if (connectionState !== 'connected') return

    connectionRef.current?.invoke('SubscribeToAlerts').catch(() => {})
  }, [connectionState, connectionRef])

  // Polling fallback when SignalR is disconnected.
  useEffect(() => {
    const timer = setInterval(() => {
      if (connectionState !== 'connected') {
        alertsApi
          .getAlerts({ unread: true, page: 1, pageSize: 20 })
          .then((res) => res.items.forEach((alert) => useAppStore.getState().addAlert(alert)))
          .catch(() => {})
      }
    }, 30_000)

    return () => {
      clearInterval(timer)
    }
  }, [connectionState])
}
