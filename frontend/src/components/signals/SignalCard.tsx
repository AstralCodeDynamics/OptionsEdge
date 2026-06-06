import { useState, useEffect } from 'react'
import type { Signal } from '../../types'

interface Props {
  signal: Signal
  onAddPosition?: (signal: Signal) => void
}

const TYPE_CONFIG: Record<string, { label: string; cls: string }> = {
  ENTRY: { label: 'ENTRY',  cls: 'bg-green-900/60 text-green-300 border border-green-700' },
  HOLD:  { label: 'HOLD',   cls: 'bg-yellow-900/60 text-yellow-300 border border-yellow-700' },
  WATCH: { label: 'WATCH',  cls: 'bg-blue-900/60 text-blue-300 border border-blue-700' },
  EXIT:  { label: 'EXIT',   cls: 'bg-red-900/60 text-red-300 border border-red-700' },
}

function useCountdown(validUntil: string) {
  const [remaining, setRemaining] = useState('')

  useEffect(() => {
    function update() {
      const diff = new Date(validUntil).getTime() - Date.now()
      if (diff <= 0) { setRemaining('Expired'); return }
      const h = Math.floor(diff / 3_600_000)
      const m = Math.floor((diff % 3_600_000) / 60_000)
      const s = Math.floor((diff % 60_000) / 1_000)
      setRemaining(h > 0 ? `${h}h ${m}m` : `${m}m ${s}s`)
    }
    update()
    const id = setInterval(update, 1000)
    return () => clearInterval(id)
  }, [validUntil])

  return remaining
}

function Box({ label, value, sub }: { label: string; value: string; sub?: string }) {
  return (
    <div className="flex-1 bg-gray-800 rounded-lg p-2 text-center min-w-0">
      <p className="text-[10px] text-gray-500 uppercase tracking-wide mb-0.5">{label}</p>
      <p className="text-sm font-bold text-white leading-tight">{value}</p>
      {sub && <p className="text-[10px] text-gray-400 mt-0.5">{sub}</p>}
    </div>
  )
}

export function SignalCard({ signal, onAddPosition }: Props) {
  const cfg      = TYPE_CONFIG[signal.signalType] ?? TYPE_CONFIG.WATCH
  const countdown = useCountdown(signal.validUntil)
  const isExpired = new Date(signal.validUntil).getTime() < Date.now()

  const fmtPrice = (v: number) =>
    v.toLocaleString('en-IN', { minimumFractionDigits: 1, maximumFractionDigits: 1 })

  const expiryLabel = new Date(signal.expiry)
    .toLocaleDateString('en-IN', { day: '2-digit', month: 'short' })

  const createdLabel = new Date(signal.createdAt)
    .toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: false })

  return (
    <div className={`bg-gray-900 border rounded-xl p-4 space-y-3 ${isExpired ? 'border-gray-800 opacity-60' : 'border-gray-700'}`}>
      {/* Header */}
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-center gap-2 flex-wrap">
          <span className={`px-2 py-0.5 rounded text-xs font-bold ${cfg.cls}`}>{cfg.label}</span>
          <span className="text-white font-semibold text-sm">
            {signal.symbol} {signal.strike} {signal.optionType}
          </span>
          <span className="text-gray-500 text-xs">{expiryLabel}</span>
          {signal.fromCache && (
            <span className="text-[10px] text-gray-600 bg-gray-800 px-1.5 py-0.5 rounded">cached</span>
          )}
        </div>
        <div className="text-right flex-shrink-0">
          <p className="text-lg font-bold text-emerald-400">{signal.confidence}%</p>
          <p className="text-[10px] text-gray-500">confidence</p>
        </div>
      </div>

      {/* Price boxes */}
      <div className="flex gap-2">
        <Box
          label="Entry"
          value={`${fmtPrice(signal.entryLow)}–${fmtPrice(signal.entryHigh)}`}
        />
        <Box
          label="Target"
          value={fmtPrice(signal.target1)}
          sub={signal.target2 ? `T2: ${fmtPrice(signal.target2)}` : undefined}
        />
        <Box
          label="Stop Loss"
          value={fmtPrice(signal.stopLoss)}
        />
      </div>

      {/* Rationale */}
      {signal.rationale.length > 0 && (
        <ul className="space-y-0.5">
          {signal.rationale.map((r, i) => (
            <li key={i} className="flex items-start gap-1.5 text-xs text-gray-400">
              <span className="text-emerald-500 mt-0.5">•</span>
              <span>{r}</span>
            </li>
          ))}
        </ul>
      )}

      {/* Footer */}
      <div className="flex items-center justify-between gap-2 pt-1 border-t border-gray-800 flex-wrap">
        <div className="flex items-center gap-2 text-[10px] flex-wrap">
          <span className="bg-gray-800 text-gray-300 px-1.5 py-0.5 rounded">
            R:R {signal.riskReward.toFixed(1)}x
          </span>
          <span className="bg-gray-800 text-gray-300 px-1.5 py-0.5 rounded">
            {signal.modelUsed.includes('haiku') ? 'Haiku' : 'Sonnet'}
          </span>
          <span className="bg-gray-800 text-gray-300 px-1.5 py-0.5 rounded">
            ${signal.costUsd.toFixed(4)}
          </span>
          <span className={`px-1.5 py-0.5 rounded ${isExpired ? 'bg-red-900/40 text-red-400' : 'bg-gray-800 text-gray-300'}`}>
            {isExpired ? 'Expired' : countdown}
          </span>
          <span className="text-gray-600">{createdLabel}</span>
        </div>
        {onAddPosition && !isExpired && (
          <button
            onClick={() => onAddPosition(signal)}
            className="text-[11px] font-semibold px-3 py-1 bg-emerald-700 hover:bg-emerald-600 text-white rounded-lg transition-colors"
          >
            + Add Position
          </button>
        )}
      </div>
    </div>
  )
}
