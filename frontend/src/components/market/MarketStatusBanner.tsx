import { useEffect, useState } from 'react'
import { useAppStore } from '../../store/appStore'
import type { ConnectionState } from '../../hooks/useSignalR'

interface Props {
  connectionState: ConnectionState
}

export default function MarketStatusBanner({ connectionState }: Props) {
  const marketStatus = useAppStore((s) => s.marketStatus)
  const [, forceRender] = useState(0)

  // Tick every 30s to refresh the "next event" countdown display
  useEffect(() => {
    const id = setInterval(() => forceRender((n) => n + 1), 30_000)
    return () => clearInterval(id)
  }, [])

  const isConnected = connectionState === 'connected'
  const isReconnecting = connectionState === 'reconnecting'

  if (!isConnected && !marketStatus) {
    return (
      <div className="flex items-center gap-2 px-4 py-2 bg-yellow-500/10 border border-yellow-500/20 rounded-lg text-yellow-400 text-sm">
        <span className="animate-pulse">●</span>
        <span>Connecting to live data…</span>
      </div>
    )
  }

  const isOpen = marketStatus?.isOpen ?? false

  return (
    <div className="flex flex-wrap items-center gap-3">
      {/* Market open/closed pill */}
      <div
        className={[
          'flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-semibold',
          isOpen
            ? 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/30'
            : 'bg-gray-800 text-gray-400 border border-gray-700',
        ].join(' ')}
      >
        <span className={isOpen ? 'animate-pulse' : ''}>●</span>
        {isOpen ? 'Market Open' : 'Market Closed'}
      </div>

      {/* Next event */}
      {marketStatus?.nextEvent && (
        <span className="text-xs text-gray-500">{marketStatus.nextEvent}</span>
      )}

      {/* SignalR status */}
      {isReconnecting && (
        <span className="text-xs text-yellow-400 animate-pulse">Reconnecting…</span>
      )}
      {!isConnected && !isReconnecting && (
        <span className="text-xs text-gray-600">● Offline</span>
      )}
    </div>
  )
}
