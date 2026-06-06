import { useEffect, useState } from 'react'
import { useAppStore } from '../../store/appStore'
import { positionsApi, alertsApi } from '../../services/api'
import PositionCard from '../../components/positions/PositionCard'
import AddPositionModal from '../../components/positions/AddPositionModal'
import type { Position, Alert } from '../../types'

export default function Positions() {
  const positions    = useAppStore((s) => s.positions)
  const setPositions = useAppStore((s) => s.setPositions)
  const upsertPosition = useAppStore((s) => s.upsertPosition)
  const removePosition = useAppStore((s) => s.removePosition)
  const alerts       = useAppStore((s) => s.alerts)

  const [modalOpen, setModalOpen]   = useState(false)
  const [editTarget, setEditTarget] = useState<Position | null>(null)
  const [loading, setLoading]       = useState(true)
  const [error, setError]           = useState<string | null>(null)

  useEffect(() => {
    positionsApi
      .getAll()
      .then(setPositions)
      .catch(() => setError('Failed to load positions'))
      .finally(() => setLoading(false))
  }, [setPositions])

  const alertsByPosition = alerts.reduce<Record<string, Alert[]>>((acc, a) => {
    const id = a.positionId
    ;(acc[id] ??= []).push(a)
    return acc
  }, {})

  const activePositions = positions.filter((p) => p.status === 'active')
  const closedPositions = positions.filter((p) => p.status !== 'active')

  const totalPnL = activePositions.reduce((sum, p) => sum + (p.pnl ?? 0), 0)

  const handleAdd = async (data: Parameters<typeof positionsApi.create>[0]) => {
    const created = await positionsApi.create(data)
    upsertPosition(created)
  }

  const handleClose = async (id: string) => {
    if (!confirm('Close this position?')) return
    await positionsApi.close(id)
    removePosition(id)
  }

  const handleEditSubmit = async (data: Parameters<typeof positionsApi.create>[0]) => {
    if (!editTarget) return
    const updated = await positionsApi.update(editTarget.id, {
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
      <div className="p-4 flex items-center justify-center min-h-[200px]">
        <div className="text-gray-400 text-sm">Loading positions…</div>
      </div>
    )
  }

  return (
    <div className="p-4 max-w-5xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-xl font-semibold text-white">Positions</h1>
        <button
          onClick={() => setModalOpen(true)}
          className="bg-emerald-600 hover:bg-emerald-500 text-white text-sm font-medium rounded-lg px-4 py-2 transition-colors"
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

      {/* Active positions grid */}
      {activePositions.length > 0 && (
        <section className="mb-6">
          <h2 className="text-sm font-medium text-gray-400 uppercase tracking-wide mb-3">Active</h2>
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
            {activePositions.map((p) => (
              <PositionCard
                key={p.id}
                position={p}
                alerts={alertsByPosition[p.id]}
                onEdit={(pos) => setEditTarget(pos)}
                onClose={handleClose}
              />
            ))}
          </div>
        </section>
      )}

      {/* Closed positions */}
      {closedPositions.length > 0 && (
        <section>
          <h2 className="text-sm font-medium text-gray-400 uppercase tracking-wide mb-3">Closed</h2>
          <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
            {closedPositions.map((p) => (
              <PositionCard
                key={p.id}
                position={p}
                alerts={alertsByPosition[p.id]}
                onEdit={() => {}}
                onClose={() => {}}
              />
            ))}
          </div>
        </section>
      )}

      {/* Empty state */}
      {positions.length === 0 && !loading && (
        <div className="flex flex-col items-center justify-center py-16 gap-3">
          <p className="text-gray-400 text-sm">No positions yet.</p>
          <button
            onClick={() => setModalOpen(true)}
            className="bg-emerald-600 hover:bg-emerald-500 text-white text-sm rounded-lg px-4 py-2 transition-colors"
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

      {/* Edit SL/Target modal */}
      <AddPositionModal
        open={editTarget !== null}
        onClose={() => setEditTarget(null)}
        onSubmit={handleEditSubmit}
        prefill={null}
      />
    </div>
  )
}
