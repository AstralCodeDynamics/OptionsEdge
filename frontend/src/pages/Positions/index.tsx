import { useCallback, useEffect, useState } from 'react'
import { useAppStore } from '../../store/appStore'
import { positionsApi, alertsApi } from '../../services/api'
import PositionCard from '../../components/positions/PositionCard'
import AddPositionModal from '../../components/positions/AddPositionModal'
import { PositionCardSkeleton } from '../../components/common/Skeleton'
import type { Position, Alert } from '../../types'

const CLOSED_PAGE_SIZE = 20

export default function Positions() {
  const positions    = useAppStore((s) => s.positions)
  const setPositions = useAppStore((s) => s.setPositions)
  const upsertPosition = useAppStore((s) => s.upsertPosition)
  const removePosition = useAppStore((s) => s.removePosition)
  const alerts       = useAppStore((s) => s.alerts)
  const marketStatus = useAppStore((s) => s.marketStatus)

  const [modalOpen, setModalOpen]   = useState(false)
  const [editTarget, setEditTarget] = useState<Position | null>(null)
  const [loading, setLoading]       = useState(true)
  const [error, setError]           = useState<string | null>(null)
  const [confirmCloseId, setConfirmCloseId] = useState<string | null>(null)
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null)
  const [activeTab, setActiveTab] = useState<'active' | 'closed'>('active')
  const [closedPage, setClosedPage] = useState(1)

  const refreshPositions = useCallback(
    async (showError = false) => {
      try {
        const data = await positionsApi.getAll()
        setPositions(data)
        setLastUpdated(new Date())
        if (showError) setError(null)
      } catch {
        if (showError) setError('Failed to load positions')
      } finally {
        setLoading(false)
      }
    },
    [setPositions],
  )

  useEffect(() => {
    refreshPositions(true)

    if (marketStatus?.isOpen === false) return undefined

    const id = setInterval(() => {
      refreshPositions()
    }, 30_000)

    return () => clearInterval(id)
  }, [marketStatus?.isOpen, refreshPositions])

  const alertsByPosition = alerts.reduce<Record<string, Alert[]>>((acc, a) => {
    const id = a.positionId
    ;(acc[id] ??= []).push(a)
    return acc
  }, {})

  const activePositions = positions.filter((p) => p.status === 'active')
  const closedPositions = positions.filter((p) => p.status !== 'active')
  const closedTotalPages = Math.max(1, Math.ceil(closedPositions.length / CLOSED_PAGE_SIZE))
  const pagedClosedPositions = closedPositions.slice(
    (closedPage - 1) * CLOSED_PAGE_SIZE,
    closedPage * CLOSED_PAGE_SIZE,
  )

  useEffect(() => {
    setClosedPage((current) => Math.min(current, closedTotalPages))
  }, [closedTotalPages])

  const totalPnL = activePositions.reduce((sum, p) => sum + (p.pnl ?? 0), 0)
  const lastUpdatedText = lastUpdated
    ? lastUpdated.toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
    : '—'

  const handleAdd = async (data: Parameters<typeof positionsApi.create>[0]) => {
    const created = await positionsApi.create(data)
    upsertPosition(created)
  }

  const handleClose = (id: string) => {
    setConfirmCloseId(id)
  }

  const handleConfirmClose = async (id: string) => {
    setConfirmCloseId(null)
    await positionsApi.close(id)
    removePosition(id)
  }

  const handleEditSubmit = async (data: Parameters<typeof positionsApi.create>[0]) => {
    if (!editTarget) return
    const updated = await positionsApi.update(editTarget.id, {
      symbol:     data.symbol,
      strike:     data.strike,
      optionType: data.optionType,
      expiry:     data.expiry,
      entryPrice: data.entryPrice,
      quantity:   data.quantity,
      stopLoss: data.stopLoss,
      target1:  data.target1,
      target2:  data.target2,
    })
    upsertPosition(updated)
    setEditTarget(null)
  }

  const handleMarkAllRead = () => {
    alertsApi.markAllRead().catch(() => {})
    useAppStore.getState().markAllRead()
  }

  if (loading) {
    return (
      <div className="p-4 max-w-5xl mx-auto space-y-3">
        <div className="flex items-center justify-between mb-1">
          <h1 className="text-xl font-semibold text-white">Positions</h1>
        </div>
        {Array.from({ length: 3 }).map((_, i) => <PositionCardSkeleton key={i} />)}
      </div>
    )
  }

  return (
    <div className="p-4 max-w-5xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <div>
          <h1 className="text-xl font-semibold text-white">Positions</h1>
          <p className="mt-0.5 text-xs text-gray-500">
            Last updated {lastUpdatedText}
          </p>
        </div>
        <button
          onClick={() => setModalOpen(true)}
          className="bg-emerald-600 hover:bg-emerald-500 text-white text-sm font-medium rounded-lg px-4 py-2 min-h-[44px] transition-colors"
        >
          + Add Position
        </button>
      </div>

      {/* Summary bar */}
      {activePositions.length > 0 && (
        <div className="grid grid-cols-3 gap-3 mb-5">
          <div className="bg-gray-900 border border-gray-800 rounded-xl p-3 text-center">
            <div className="text-xs text-gray-500 mb-1">Active</div>
            <div className="text-white font-semibold">{activePositions.length}</div>
          </div>
          <div className="bg-gray-900 border border-gray-800 rounded-xl p-3 text-center">
            <div className="text-xs text-gray-500 mb-1">Total P&amp;L</div>
            <div className={`font-semibold ${totalPnL >= 0 ? 'text-green-400' : 'text-red-400'}`}>
              {totalPnL >= 0 ? '+' : ''}₹{totalPnL.toLocaleString('en-IN', { maximumFractionDigits: 0 })}
            </div>
          </div>
          <div className="bg-gray-900 border border-gray-800 rounded-xl p-3 text-center">
            <div className="text-xs text-gray-500 mb-1">Unread Alerts</div>
            <div className="text-white font-semibold">
              {alerts.filter((a) => !a.isRead).length}
              {alerts.some((a) => !a.isRead) && (
                <button
                  onClick={handleMarkAllRead}
                  className="text-gray-500 hover:text-gray-300 text-xs ml-2"
                >
                  clear
                </button>
              )}
            </div>
          </div>
        </div>
      )}

      {error && (
        <div className="bg-red-900/30 border border-red-700/50 rounded-lg px-4 py-3 text-red-400 text-sm mb-4">
          {error}
        </div>
      )}

      {positions.length > 0 && (
        <div className="flex gap-2 mb-4" role="tablist" aria-label="Position status">
          {(['active', 'closed'] as const).map((tab) => (
            <button
              key={tab}
              type="button"
              role="tab"
              aria-selected={activeTab === tab}
              onClick={() => {
                setActiveTab(tab)
                if (tab === 'closed') setClosedPage(1)
              }}
              className={`px-3 py-1 rounded-full text-xs font-semibold transition-colors ${
                activeTab === tab
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
              }`}
            >
              {tab === 'active' ? `Active (${activePositions.length})` : `Closed (${closedPositions.length})`}
            </button>
          ))}
        </div>
      )}

      {activeTab === 'active' && activePositions.length > 0 && (
        <section>
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
            {activePositions.map((p) => (
              <PositionCard
                key={p.id}
                position={p}
                alerts={alertsByPosition[p.id]}
                onEdit={(pos) => setEditTarget(pos)}
                onClose={handleClose}
                confirmingClose={confirmCloseId === p.id}
                onConfirmClose={handleConfirmClose}
                onCancelClose={() => setConfirmCloseId(null)}
              />
            ))}
          </div>
        </section>
      )}

      {activeTab === 'closed' && closedPositions.length > 0 && (
        <section>
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
            {pagedClosedPositions.map((p) => (
              <PositionCard
                key={p.id}
                position={p}
                alerts={alertsByPosition[p.id]}
                onEdit={() => {}}
                onClose={() => {}}
              />
            ))}
          </div>
          <div className="flex items-center justify-between pt-4 text-xs text-gray-500">
            <span>
              {(closedPage - 1) * CLOSED_PAGE_SIZE + 1}-{Math.min(closedPage * CLOSED_PAGE_SIZE, closedPositions.length)} of {closedPositions.length}
            </span>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => setClosedPage((page) => page - 1)}
                disabled={closedPage <= 1}
                className="rounded border border-gray-800 bg-gray-900 px-3 py-1.5 hover:bg-gray-800 disabled:cursor-not-allowed disabled:opacity-40"
              >
                Prev
              </button>
              <button
                type="button"
                onClick={() => setClosedPage((page) => page + 1)}
                disabled={closedPage >= closedTotalPages}
                className="rounded border border-gray-800 bg-gray-900 px-3 py-1.5 hover:bg-gray-800 disabled:cursor-not-allowed disabled:opacity-40"
              >
                Next
              </button>
            </div>
          </div>
        </section>
      )}

      {positions.length > 0 && activeTab === 'active' && activePositions.length === 0 && (
        <p className="rounded-xl border border-gray-800 bg-gray-900 p-4 text-center text-xs text-gray-600">
          No active positions.
        </p>
      )}

      {positions.length > 0 && activeTab === 'closed' && closedPositions.length === 0 && (
        <p className="rounded-xl border border-gray-800 bg-gray-900 p-4 text-center text-xs text-gray-600">
          No closed positions.
        </p>
      )}

      {/* Empty state */}
      {positions.length === 0 && !loading && (
        <div className="flex flex-col items-center justify-center py-16 gap-3">
          <p className="text-gray-400 text-sm">No positions yet.</p>
          <button
            onClick={() => setModalOpen(true)}
            className="bg-emerald-600 hover:bg-emerald-500 text-white text-sm rounded-lg px-4 py-2 min-h-[44px] transition-colors"
          >
            Add your first position
          </button>
        </div>
      )}

      {/* Add modal */}
      <AddPositionModal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        onSubmit={handleAdd}
      />

      {/* Edit modal */}
      <AddPositionModal
        open={editTarget !== null}
        onClose={() => setEditTarget(null)}
        onSubmit={handleEditSubmit}
        prefill={editTarget}
        mode="edit"
      />
    </div>
  )
}
