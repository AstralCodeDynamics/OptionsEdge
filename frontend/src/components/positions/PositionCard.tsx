import type { Position, Alert } from '../../types'

interface Props {
  position: Position
  alerts?: Alert[]
  onEdit: (position: Position) => void
  onClose: (id: string) => void
  confirmingClose?: boolean
  onConfirmClose?: (id: string) => void
  onCancelClose?: () => void
}

function PnLColor(value: number | undefined): string {
  if (value == null) return 'text-gray-400'
  return value >= 0 ? 'text-green-400' : 'text-red-400'
}

function SlProgressBar({ ltp, entry, sl }: { ltp?: number; entry: number; sl: number }) {
  const pct =
    ltp == null || entry <= sl
      ? 0
      : Math.min(100, Math.max(0, ((entry - ltp) / (entry - sl)) * 100))

  const color =
    pct < 33 ? 'bg-green-500' : pct < 66 ? 'bg-yellow-500' : 'bg-red-500'

  return (
    <div>
      <div className="flex justify-between text-xs text-gray-500 mb-0.5">
        <span>SL Distance</span>
        <span className={pct >= 66 ? 'text-red-400' : ''}>{pct.toFixed(0)}% used</span>
      </div>
      <div className="h-1.5 bg-gray-700 rounded-full overflow-hidden">
        <div className={`h-full ${color} rounded-full transition-all`} style={{ width: `${pct}%` }} />
      </div>
    </div>
  )
}

function TargetProgressBar({ ltp, entry, target }: { ltp?: number; entry: number; target: number }) {
  const pct =
    ltp == null || target <= entry
      ? 0
      : Math.min(100, Math.max(0, ((ltp - entry) / (target - entry)) * 100))

  return (
    <div>
      <div className="flex justify-between text-xs text-gray-500 mb-0.5">
        <span>Target T1</span>
        <span className={pct >= 80 ? 'text-green-400' : ''}>{pct.toFixed(0)}%</span>
      </div>
      <div className="h-1.5 bg-gray-700 rounded-full overflow-hidden">
        <div
          className="h-full bg-emerald-500 rounded-full transition-all"
          style={{ width: `${pct}%` }}
        />
      </div>
    </div>
  )
}

const SEVERITY_CLS: Record<string, string> = {
  Danger:  'text-red-400',
  Warning: 'text-orange-400',
  Info:    'text-green-400',
}

export default function PositionCard({
  position: p,
  alerts = [],
  onEdit,
  onClose,
  confirmingClose = false,
  onConfirmClose,
  onCancelClose,
}: Props) {
  const lastAlerts = alerts.slice(0, 3)
  const statusColor =
    p.status === 'active' ? 'text-green-400' : p.status === 'closed' ? 'text-gray-400' : 'text-yellow-400'

  return (
    <div className="bg-gray-900 border border-gray-800 rounded-xl p-4 flex flex-col gap-3">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-2 flex-wrap">
            <span className="font-semibold text-white text-sm">{p.symbol}</span>
            <span className="text-gray-300 text-sm">
              {p.strike} {p.optionType}
            </span>
            <span className="text-gray-500 text-xs">{p.expiry}</span>
          </div>
          <div className="flex items-center gap-2 mt-0.5">
            <span className={`text-xs ${statusColor} font-medium uppercase`}>{p.status}</span>
            <span className="text-gray-600 text-xs">·</span>
            <span className="text-gray-500 text-xs">{p.quantity} lot{p.quantity !== 1 ? 's' : ''}</span>
          </div>
        </div>
        <div className="text-right">
          <div className={`text-lg font-bold ${PnLColor(p.pnl)}`}>
            {p.pnl != null ? `₹${p.pnl >= 0 ? '+' : ''}${p.pnl.toLocaleString('en-IN', { maximumFractionDigits: 0 })}` : '—'}
          </div>
          {p.pnlPct != null && (
            <div className={`text-xs ${PnLColor(p.pnlPct)}`}>
              {p.pnlPct >= 0 ? '+' : ''}
              {p.pnlPct.toFixed(2)}%
            </div>
          )}
        </div>
      </div>

      {/* Price row */}
      <div className="grid grid-cols-3 gap-2 text-center text-xs">
        <div className="bg-gray-800 rounded-lg p-2">
          <div className="text-gray-500 mb-0.5">Entry</div>
          <div className="text-white font-medium">₹{p.entryPrice}</div>
        </div>
        <div className="bg-gray-800 rounded-lg p-2">
          <div className="text-gray-500 mb-0.5">LTP</div>
          <div className={`font-medium ${PnLColor(p.pnl)}`}>
            {p.currentLtp != null ? `₹${p.currentLtp}` : '—'}
          </div>
        </div>
        <div className="bg-gray-800 rounded-lg p-2">
          <div className="text-gray-500 mb-0.5">SL / T1</div>
          <div className="text-red-400 font-medium text-[11px]">₹{p.stopLoss}</div>
          <div className="text-green-400 font-medium text-[11px]">₹{p.target1}</div>
        </div>
      </div>

      {/* SL & target progress */}
      {p.status === 'active' && (
        <div className="flex flex-col gap-2">
          <SlProgressBar ltp={p.currentLtp} entry={p.entryPrice} sl={p.stopLoss} />
          <TargetProgressBar ltp={p.currentLtp} entry={p.entryPrice} target={p.target1} />
        </div>
      )}

      {/* Last alerts */}
      {lastAlerts.length > 0 && (
        <div className="flex flex-col gap-1">
          {lastAlerts.map((a) => (
            <div key={a.id} className={`text-xs ${SEVERITY_CLS[a.severity] ?? 'text-gray-400'}`}>
              • {a.message}
            </div>
          ))}
        </div>
      )}

      {/* Actions */}
      {p.status === 'active' && (
        confirmingClose ? (
          <div className="flex items-center gap-2 mt-1">
            <span className="flex-1 text-xs text-gray-300">Confirm close?</span>
            <button
              onClick={() => onConfirmClose?.(p.id)}
              className="bg-red-600 hover:bg-red-500 text-white text-xs font-medium rounded-lg px-4 py-2 transition-colors"
            >
              Yes
            </button>
            <button
              onClick={() => onCancelClose?.()}
              className="bg-gray-800 hover:bg-gray-700 text-gray-300 text-xs font-medium rounded-lg px-4 py-2 transition-colors"
            >
              Cancel
            </button>
          </div>
        ) : (
          <div className="flex gap-2 mt-1">
            <button
              onClick={() => onEdit(p)}
              className="flex-1 bg-gray-800 hover:bg-gray-700 text-gray-300 text-xs font-medium rounded-lg py-2 transition-colors"
            >
              Edit SL/Target
            </button>
            <button
              onClick={() => onClose(p.id)}
              className="flex-1 bg-red-900/40 hover:bg-red-900/70 text-red-300 text-xs font-medium rounded-lg py-2 transition-colors"
            >
              Close Position
            </button>
          </div>
        )
      )}
    </div>
  )
}
