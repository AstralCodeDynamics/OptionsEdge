import { useCallback, useEffect, useMemo, useState } from 'react'
import { alertsApi, positionsApi } from '../../services/api'
import { useAppStore } from '../../store/appStore'
import type { Alert, Position } from '../../types'

const PAGE_SIZE = 20

const SEVERITY_CONFIG: Record<Alert['severity'], { label: string; cls: string; dot: string }> = {
  Danger: {
    label: 'DANGER',
    cls: 'border-red-700 bg-red-900/50 text-red-300',
    dot: 'bg-red-500',
  },
  Warning: {
    label: 'WARNING',
    cls: 'border-orange-700 bg-orange-900/50 text-orange-300',
    dot: 'bg-orange-500',
  },
  Info: {
    label: 'INFO',
    cls: 'border-green-700 bg-green-900/50 text-green-300',
    dot: 'bg-green-500',
  },
}

const fmtDateTime = (value: string) =>
  new Date(value).toLocaleString('en-IN', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  })

function positionLabel(position?: Position) {
  if (!position) return null
  return `${position.symbol} ${position.strike.toLocaleString('en-IN')} ${position.optionType}`
}

function NotificationCard({
  alert,
  position,
}: {
  alert: Alert
  position?: Position
}) {
  const severity = SEVERITY_CONFIG[alert.severity]
  const label = positionLabel(position)

  return (
    <div className={`space-y-3 rounded-xl border bg-gray-900 p-4 ${alert.isRead ? 'border-gray-800 opacity-70' : 'border-gray-700'}`}>
      <div className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 flex-wrap items-center gap-2">
          <span className={`rounded border px-2 py-0.5 text-xs font-bold ${severity.cls}`}>
            {severity.label}
          </span>
          <span className="rounded border border-gray-700 bg-gray-800 px-2 py-0.5 text-xs font-bold text-gray-300">
            {alert.alertType}
          </span>
          {!alert.isRead && (
            <span className={`h-2 w-2 rounded-full ${severity.dot}`} aria-label="Unread" />
          )}
        </div>
        <span className="flex-shrink-0 text-xs text-gray-500">{fmtDateTime(alert.createdAt)}</span>
      </div>

      <p className="text-sm leading-6 text-gray-100">{alert.message}</p>

      <div className="flex flex-wrap items-center gap-2 border-t border-gray-800 pt-1 text-[10px]">
        {label ? (
          <span className="rounded bg-gray-800 px-1.5 py-0.5 text-gray-300">
            {label}
          </span>
        ) : (
          <span className="rounded bg-gray-800 px-1.5 py-0.5 text-gray-500">
            Position unavailable
          </span>
        )}
        <span className={`rounded px-1.5 py-0.5 ${alert.isRead ? 'bg-gray-800 text-gray-500' : 'bg-emerald-900/40 text-emerald-300'}`}>
          {alert.isRead ? 'Read' : 'Unread'}
        </span>
      </div>
    </div>
  )
}

export default function NotificationHistory() {
  const [alerts, setAlerts] = useState<Alert[]>([])
  const [page, setPage] = useState(1)
  const [totalItems, setTotalItems] = useState(0)
  const [totalPages, setTotalPages] = useState(1)
  const [unreadOnly, setUnreadOnly] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const positions = useAppStore((s) => s.positions)
  const setPositions = useAppStore((s) => s.setPositions)

  const positionsById = useMemo(
    () => new Map(positions.map((position) => [position.id, position])),
    [positions],
  )

  const loadHistory = useCallback((nextPage: number, nextUnreadOnly: boolean) => {
    setLoading(true)
    setError(null)
    alertsApi.getAlerts({
      unread: nextUnreadOnly ? true : false,
      page: nextPage,
      pageSize: PAGE_SIZE,
    })
      .then((res) => {
        setAlerts(res.items)
        setPage(res.page)
        setTotalItems(res.total)
        setTotalPages(Math.max(1, Math.ceil(res.total / res.pageSize)))
      })
      .catch(() => {
        setError('Could not load notification history.')
      })
      .finally(() => {
        setLoading(false)
      })
  }, [])

  useEffect(() => {
    loadHistory(1, unreadOnly)
  }, [loadHistory, unreadOnly])

  useEffect(() => {
    if (positions.length > 0) return
    positionsApi.getAll().then(setPositions).catch(() => {})
  }, [positions.length, setPositions])

  const handleToggleUnreadOnly = (nextUnreadOnly: boolean) => {
    setUnreadOnly(nextUnreadOnly)
    setPage(1)
  }

  const handleMarkAllRead = () => {
    alertsApi.markAllRead()
      .then(() => {
        useAppStore.getState().markAllRead()
        loadHistory(1, unreadOnly)
      })
      .catch(() => {})
  }

  const start = totalItems === 0 ? 0 : (page - 1) * PAGE_SIZE + 1
  const end = Math.min(page * PAGE_SIZE, totalItems)

  return (
    <div className="p-4 space-y-4 max-w-6xl mx-auto">
      <div className="flex flex-wrap items-center gap-3">
        <div>
          <h1 className="text-lg font-bold text-white">Notification History</h1>
          <p className="mt-0.5 text-xs text-gray-500">
            Alerts from position risk monitoring, newest first.
          </p>
        </div>
        <div className="ml-auto flex flex-wrap items-center gap-2">
          <div className="flex rounded border border-gray-800 bg-gray-900 p-0.5 text-xs">
            <button
              onClick={() => handleToggleUnreadOnly(false)}
              className={`rounded px-3 py-1.5 ${!unreadOnly ? 'bg-emerald-600 text-white' : 'text-gray-400 hover:text-white'}`}
            >
              All
            </button>
            <button
              onClick={() => handleToggleUnreadOnly(true)}
              className={`rounded px-3 py-1.5 ${unreadOnly ? 'bg-emerald-600 text-white' : 'text-gray-400 hover:text-white'}`}
            >
              Unread only
            </button>
          </div>
          <button
            onClick={handleMarkAllRead}
            disabled={loading}
            className="rounded border border-gray-800 bg-gray-900 px-3 py-1.5 text-xs text-gray-300 hover:bg-gray-800 disabled:cursor-not-allowed disabled:opacity-40"
          >
            Mark all read
          </button>
        </div>
      </div>

      {loading ? (
        <div className="space-y-3">
          <div className="h-32 animate-pulse rounded-xl border border-gray-800 bg-gray-900 p-4" />
          <div className="h-32 animate-pulse rounded-xl border border-gray-800 bg-gray-900 p-4" />
        </div>
      ) : error ? (
        <p className="rounded-xl border border-red-800 bg-red-900/20 p-4 text-center text-xs text-red-400">
          {error}
        </p>
      ) : alerts.length > 0 ? (
        <>
          <div className="space-y-3">
            {alerts.map((alert) => (
              <NotificationCard
                key={alert.id}
                alert={alert}
                position={positionsById.get(alert.positionId)}
              />
            ))}
          </div>

          <div className="flex items-center justify-between pt-2 text-xs text-gray-500">
            <span>
              {start}-{end} of {totalItems}
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => loadHistory(page - 1, unreadOnly)}
                disabled={page <= 1}
                className="rounded border border-gray-800 bg-gray-900 px-3 py-1.5 hover:bg-gray-800 disabled:cursor-not-allowed disabled:opacity-40"
              >
                Prev
              </button>
              <button
                onClick={() => loadHistory(page + 1, unreadOnly)}
                disabled={page >= totalPages}
                className="rounded border border-gray-800 bg-gray-900 px-3 py-1.5 hover:bg-gray-800 disabled:cursor-not-allowed disabled:opacity-40"
              >
                Next
              </button>
            </div>
          </div>
        </>
      ) : (
        <p className="rounded-xl border border-gray-800 bg-gray-900 p-4 text-center text-xs text-gray-600">
          {unreadOnly ? 'No unread notifications.' : 'No notifications yet.'}
        </p>
      )}
    </div>
  )
}
