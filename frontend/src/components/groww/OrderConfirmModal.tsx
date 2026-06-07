import { useState } from 'react'
import type { Signal } from '../../types'

interface Props {
  open: boolean
  onClose: () => void
  signal: Signal | null
  onConfirm: (lots: number) => Promise<void>
  orderPlacementEnabled: boolean
}

const LOT_SIZE: Record<string, number> = { NIFTY: 75, BANKNIFTY: 35 }

export default function OrderConfirmModal({ open, onClose, signal, onConfirm, orderPlacementEnabled }: Props) {
  const [lots, setLots] = useState(1)
  const [placing, setPlacing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (!open || !signal) return null

  const lotSize = LOT_SIZE[signal.symbol] ?? 75
  const quantity = lots * lotSize
  const estimatedCost = signal.entryHigh * quantity

  const fmt = (v: number) =>
    v.toLocaleString('en-IN', { minimumFractionDigits: 1, maximumFractionDigits: 1 })

  const handleConfirm = async () => {
    setError(null)
    setPlacing(true)
    try {
      await onConfirm(lots)
      onClose()
    } catch (e: unknown) {
      const msg =
        (e as { response?: { data?: { error?: string } } })?.response?.data?.error
        ?? 'Failed to place order. Please try again.'
      setError(msg)
    } finally {
      setPlacing(false)
    }
  }

  return (
    <>
      <div className="fixed inset-0 bg-black/60 z-50" onClick={onClose} />

      <div className="fixed bottom-0 left-0 right-0 z-50 bg-gray-900 rounded-t-2xl p-5 max-h-[90vh] overflow-y-auto lg:inset-auto lg:top-1/2 lg:left-1/2 lg:-translate-x-1/2 lg:-translate-y-1/2 lg:w-full lg:max-w-md lg:rounded-2xl">
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-white font-semibold text-base">Confirm Order</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-white text-2xl leading-none">×</button>
        </div>

        <div className="bg-gray-800 rounded-lg px-4 py-3 mb-4">
          <p className="text-white font-semibold text-sm">
            {signal.symbol} {signal.strike} {signal.optionType}
          </p>
          <p className="text-gray-400 text-xs mt-0.5">
            Expiry {new Date(signal.expiry).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}
          </p>
        </div>

        <div className="space-y-3 mb-4">
          <div>
            <label className="block text-xs text-gray-400 mb-1">Lots</label>
            <div className="flex items-center gap-3">
              <button
                type="button"
                onClick={() => setLots((l) => Math.max(1, l - 1))}
                className="w-9 h-9 flex items-center justify-center bg-gray-800 hover:bg-gray-700 text-white rounded-lg text-lg"
              >−</button>
              <span className="text-white font-semibold text-base w-10 text-center">{lots}</span>
              <button
                type="button"
                onClick={() => setLots((l) => l + 1)}
                className="w-9 h-9 flex items-center justify-center bg-gray-800 hover:bg-gray-700 text-white rounded-lg text-lg"
              >+</button>
              <span className="text-xs text-gray-500">= {quantity} qty (lot size {lotSize})</span>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="bg-gray-800 rounded-lg p-2.5">
              <p className="text-[10px] text-gray-500 uppercase tracking-wide mb-0.5">Entry (LIMIT)</p>
              <p className="text-sm font-bold text-white">₹{fmt(signal.entryHigh)}</p>
            </div>
            <div className="bg-gray-800 rounded-lg p-2.5">
              <p className="text-[10px] text-gray-500 uppercase tracking-wide mb-0.5">Est. Cost</p>
              <p className="text-sm font-bold text-white">₹{estimatedCost.toLocaleString('en-IN', { maximumFractionDigits: 0 })}</p>
            </div>
            <div className="bg-gray-800 rounded-lg p-2.5">
              <p className="text-[10px] text-gray-500 uppercase tracking-wide mb-0.5">Stop Loss</p>
              <p className="text-sm font-bold text-red-400">₹{fmt(signal.stopLoss)}</p>
            </div>
            <div className="bg-gray-800 rounded-lg p-2.5">
              <p className="text-[10px] text-gray-500 uppercase tracking-wide mb-0.5">Target</p>
              <p className="text-sm font-bold text-emerald-400">₹{fmt(signal.target1)}</p>
            </div>
          </div>

          <p className="text-[11px] text-gray-500">
            Order will be placed as a {signal.optionType === 'CE' || signal.optionType === 'PE' ? 'BUY' : ''} LIMIT order, NRML product, valid for the day.
          </p>
        </div>

        {error && (
          <div className="bg-red-900/40 border border-red-700/50 rounded-lg px-3 py-2 mb-3">
            <p className="text-red-400 text-xs">{error}</p>
          </div>
        )}

        <div className="flex gap-3">
          <button
            onClick={onClose}
            className="flex-1 bg-gray-800 hover:bg-gray-700 text-white font-semibold rounded-xl py-3 transition-colors"
          >
            Cancel
          </button>
          {orderPlacementEnabled ? (
            <button
              onClick={handleConfirm}
              disabled={placing}
              className="flex-1 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white font-semibold rounded-xl py-3 transition-colors"
            >
              {placing ? 'Placing…' : 'Confirm Order'}
            </button>
          ) : (
            <button
              disabled
              className="flex-1 bg-gray-800 text-gray-500 font-semibold rounded-xl py-3 cursor-not-allowed"
            >
              Requires Production Server
            </button>
          )}
        </div>

        {!orderPlacementEnabled && (
          <p className="text-[11px] text-gray-500 mt-3">
            Live order placement is enabled only when the app is deployed on a server with a whitelisted static IP. You can still use all signals, analysis and alerts.
          </p>
        )}
      </div>
    </>
  )
}
