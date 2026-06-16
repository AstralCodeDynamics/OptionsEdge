import { useEffect, useState } from 'react'
import { signalsApi } from '../../services/api'
import type { SignalHistoryItem } from '../../types'

const PAGE_SIZE = 20

const TYPE_CONFIG: Record<string, { label: string; cls: string }> = {
  ENTRY: { label: 'ENTRY', cls: 'bg-green-900/60 text-green-300 border border-green-700' },
  HOLD: { label: 'HOLD', cls: 'bg-yellow-900/60 text-yellow-300 border border-yellow-700' },
  WATCH: { label: 'WATCH', cls: 'bg-blue-900/60 text-blue-300 border border-blue-700' },
  EXIT: { label: 'EXIT', cls: 'bg-red-900/60 text-red-300 border border-red-700' },
}

const fmtPrice = (value: number) =>
  value.toLocaleString('en-IN', { minimumFractionDigits: 1, maximumFractionDigits: 1 })

const fmtDateTime = (value: string) =>
  new Date(value).toLocaleString('en-IN', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  })

const fmtTime = (value: string) =>
  new Date(value).toLocaleTimeString('en-IN', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  })

const fmtExpiry = (value: string) =>
  new Date(value).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' })

function statusFor(signal: SignalHistoryItem) {
  return new Date(signal.validUntil).getTime() >= Date.now() ? 'Active' : 'Expired'
}

function modelLabel(model: string) {
  if (model.toLowerCase().includes('haiku')) return 'Haiku'
  if (model.toLowerCase().includes('sonnet')) return 'Sonnet'
  return model
}

function Box({ label, value, sub }: { label: string; value: string; sub?: string }) {
  return (
    <div className="min-w-0 flex-1 rounded-lg bg-gray-800 p-2 text-center">
      <p className="mb-0.5 text-[10px] uppercase tracking-wide text-gray-500">{label}</p>
      <p className="truncate text-sm font-bold leading-tight text-white">{value}</p>
      {sub && <p className="mt-0.5 truncate text-[10px] text-gray-400">{sub}</p>}
    </div>
  )
}

function SignalHistoryCard({ signal }: { signal: SignalHistoryItem }) {
  const cfg = TYPE_CONFIG[signal.signalType] ?? TYPE_CONFIG.WATCH
  const status = statusFor(signal)
  const isExpired = status === 'Expired'
  const targetSub = signal.target2 != null ? `T2: ${fmtPrice(signal.target2)}` : undefined
  const rationale = signal.rationale?.filter(Boolean) ?? []

  return (
    <div className={`space-y-3 rounded-xl border bg-gray-900 p-4 ${isExpired ? 'border-gray-800 opacity-70' : 'border-gray-700'}`}>
      <div className="flex items-start justify-between gap-2">
        <div className="flex min-w-0 flex-wrap items-center gap-2">
          <span className={`rounded px-2 py-0.5 text-xs font-bold ${cfg.cls}`}>{cfg.label}</span>
          <span className={`rounded border px-2 py-0.5 text-xs font-bold ${isExpired ? 'border-red-700 bg-red-900/40 text-red-300' : 'border-emerald-700 bg-emerald-900/40 text-emerald-300'}`}>
            {status}
          </span>
          <span className="text-sm font-semibold text-white">
            {signal.symbol} {signal.strike.toLocaleString('en-IN')} {signal.optionType}
          </span>
          <span className="text-xs text-gray-500">{fmtExpiry(signal.expiry)}</span>
        </div>
        <div className="flex-shrink-0 text-right">
          <p className="text-lg font-bold text-emerald-400">{signal.confidence}%</p>
          <p className="text-[10px] text-gray-500">confidence</p>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-2">
        <Box label="Entry" value={`${fmtPrice(signal.entryLow)}-${fmtPrice(signal.entryHigh)}`} />
        <Box label="Target" value={fmtPrice(signal.target1)} sub={targetSub} />
        <Box label="Stop Loss" value={fmtPrice(signal.stopLoss)} />
      </div>

      {rationale.length > 0 && (
        <ul className="space-y-0.5">
          {rationale.map((item, index) => (
            <li key={`${signal.id}-${index}`} className="flex items-start gap-1.5 text-xs text-gray-400">
              <span className="mt-0.5 text-emerald-500">-</span>
              <span>{item}</span>
            </li>
          ))}
        </ul>
      )}

      <div className="flex flex-wrap items-center gap-2 border-t border-gray-800 pt-1 text-[10px]">
        <span className="rounded bg-gray-800 px-1.5 py-0.5 text-gray-300">
          R:R {signal.riskReward.toFixed(1)}x
        </span>
        <span className="rounded bg-gray-800 px-1.5 py-0.5 text-gray-300">
          {modelLabel(signal.modelUsed)}
        </span>
        <span className="rounded bg-gray-800 px-1.5 py-0.5 text-gray-300">
          ${signal.costUsd.toFixed(4)}
        </span>
        <span className="rounded bg-gray-800 px-1.5 py-0.5 text-gray-300">
          {fmtTime(signal.createdAt)}
        </span>
        <span className={`rounded px-1.5 py-0.5 ${isExpired ? 'bg-red-900/40 text-red-400' : 'bg-gray-800 text-gray-300'}`}>
          {status}
        </span>
        <span className="text-gray-600">{fmtDateTime(signal.createdAt)}</span>
      </div>
    </div>
  )
}

export default function SignalHistory() {
  const [signals, setSignals] = useState<SignalHistoryItem[]>([])
  const [page, setPage] = useState(1)
  const [totalItems, setTotalItems] = useState(0)
  const [totalPages, setTotalPages] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  function loadHistory(nextPage = page) {
    setLoading(true)
    setError(null)
    signalsApi.getHistory({ page: nextPage, pageSize: PAGE_SIZE })
      .then((res) => {
        const total = res.totalItems ?? res.total ?? res.items.length
        setSignals(res.items)
        setPage(res.page)
        setTotalItems(total)
        setTotalPages(res.totalPages ?? Math.max(1, Math.ceil(total / res.pageSize)))
      })
      .catch(() => {
        setError('Could not load signal history.')
      })
      .finally(() => {
        setLoading(false)
      })
  }

  useEffect(() => {
    loadHistory(1)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const start = totalItems === 0 ? 0 : (page - 1) * PAGE_SIZE + 1
  const end = Math.min(page * PAGE_SIZE, totalItems)

  return (
    <div className="space-y-4 pb-20">
      <div className="flex flex-wrap items-end justify-between gap-2">
        <div>
          <h1 className="text-xl font-semibold text-white">Signal History</h1>
          <p className="mt-1 text-xs text-gray-500">
            Latest AI signals, newest first.
          </p>
        </div>
        <p className="text-[11px] text-gray-600">{totalItems} saved</p>
      </div>

      {loading ? (
        <div className="space-y-3">
          <div className="h-40 animate-pulse rounded-xl border border-gray-800 bg-gray-900" />
          <div className="h-40 animate-pulse rounded-xl border border-gray-800 bg-gray-900" />
        </div>
      ) : error ? (
        <p className="rounded-xl border border-red-800 bg-red-900/20 px-3 py-6 text-center text-xs text-red-400">
          {error}
        </p>
      ) : signals.length > 0 ? (
        <>
          <div className="space-y-3">
            {signals.map((signal) => (
              <SignalHistoryCard key={signal.id} signal={signal} />
            ))}
          </div>

          <div className="flex items-center justify-between pt-2 text-xs text-gray-500">
            <span>
              {start}-{end} of {totalItems}
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => loadHistory(page - 1)}
                disabled={page <= 1}
                className="rounded border border-gray-800 bg-gray-900 px-3 py-1.5 hover:bg-gray-800 disabled:cursor-not-allowed disabled:opacity-40"
              >
                Prev
              </button>
              <button
                onClick={() => loadHistory(page + 1)}
                disabled={page >= totalPages}
                className="rounded border border-gray-800 bg-gray-900 px-3 py-1.5 hover:bg-gray-800 disabled:cursor-not-allowed disabled:opacity-40"
              >
                Next
              </button>
            </div>
          </div>
        </>
      ) : (
        <p className="rounded-xl border border-gray-800 bg-gray-900 px-3 py-6 text-center text-xs text-gray-600">
          No saved signals yet.
        </p>
      )}
    </div>
  )
}
