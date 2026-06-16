import { useEffect, useState } from 'react'
import { signalsApi } from '../../services/api'
import type { SignalHistoryItem } from '../../types'

const PAGE_SIZE = 20

const fmtPrice = (value: number) => value.toFixed(2)

const fmtDateTime = (value: string) =>
  new Date(value).toLocaleString('en-IN', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  })

function statusFor(signal: SignalHistoryItem) {
  return new Date(signal.validUntil).getTime() >= Date.now() ? 'Active' : 'Expired'
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
      <div>
        <h1 className="text-xl font-semibold text-white">Signal History</h1>
        <p className="mt-1 text-xs text-gray-500">
          Latest AI signals, newest first.
        </p>
      </div>

      <div className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
        <div className="flex flex-wrap items-center justify-between gap-2 px-3 pt-3 pb-2">
          <div>
            <h2 className="text-xs font-medium text-gray-400 uppercase tracking-wide">
              Signals
            </h2>
            <p className="mt-1 text-[11px] text-gray-600">
              Entry and risk levels are option premium points from each generated signal.
            </p>
          </div>
          <p className="text-[11px] text-gray-600">{totalItems} saved</p>
        </div>

        {loading ? (
          <div className="h-28 animate-pulse border-t border-gray-800 bg-gray-900" />
        ) : error ? (
          <p className="border-t border-gray-800 px-3 py-6 text-center text-xs text-red-400">
            {error}
          </p>
        ) : signals.length > 0 ? (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-xs">
                <thead>
                  <tr className="text-gray-500 border-t border-gray-800">
                    <th className="text-left font-medium px-3 py-2">Date/Time</th>
                    <th className="text-left font-medium px-3 py-2">Symbol</th>
                    <th className="text-left font-medium px-3 py-2">Type</th>
                    <th className="text-right font-medium px-3 py-2">Strike</th>
                    <th className="text-right font-medium px-3 py-2">Confidence</th>
                    <th className="text-right font-medium px-3 py-2">Entry</th>
                    <th className="text-right font-medium px-3 py-2">Target</th>
                    <th className="text-right font-medium px-3 py-2">SL</th>
                    <th className="text-left font-medium px-3 py-2">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {signals.map((signal) => {
                    const status = statusFor(signal)

                    return (
                      <tr
                        key={signal.id}
                        className="border-t border-gray-800/60 hover:bg-gray-800/40"
                      >
                        <td className="px-3 py-2 text-gray-400 whitespace-nowrap">
                          {fmtDateTime(signal.createdAt)}
                        </td>
                        <td className="px-3 py-2 text-gray-300 whitespace-nowrap">
                          {signal.symbol}
                        </td>
                        <td className="px-3 py-2 whitespace-nowrap">
                          <span className="font-semibold text-white">{signal.signalType}</span>
                          <span className={signal.optionType === 'CE' ? 'ml-1 text-emerald-300' : 'ml-1 text-red-300'}>
                            {signal.optionType}
                          </span>
                        </td>
                        <td className="px-3 py-2 text-right text-gray-300">
                          {signal.strike.toLocaleString('en-IN')}
                        </td>
                        <td className="px-3 py-2 text-right font-semibold text-emerald-400">
                          {signal.confidence}%
                        </td>
                        <td className="px-3 py-2 text-right text-gray-300 whitespace-nowrap">
                          {fmtPrice(signal.entryLow)}-{fmtPrice(signal.entryHigh)}
                        </td>
                        <td className="px-3 py-2 text-right text-emerald-300 whitespace-nowrap">
                          {fmtPrice(signal.target1)}
                          {signal.target2 != null ? ` / ${fmtPrice(signal.target2)}` : ''}
                        </td>
                        <td className="px-3 py-2 text-right text-red-300">
                          {fmtPrice(signal.stopLoss)}
                        </td>
                        <td className="px-3 py-2 whitespace-nowrap">
                          <span className={[
                            'rounded border px-2 py-0.5 text-[11px] font-medium',
                            status === 'Active'
                              ? 'border-emerald-500/30 bg-emerald-500/10 text-emerald-300'
                              : 'border-gray-700 bg-gray-800 text-gray-400',
                          ].join(' ')}>
                            {status}
                          </span>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
            <div className="flex items-center justify-between px-3 py-2.5 border-t border-gray-800 text-xs text-gray-400">
              <span>
                {start}-{end} of {totalItems}
              </span>
              <div className="flex gap-2">
                <button
                  onClick={() => loadHistory(page - 1)}
                  disabled={page <= 1}
                  className="px-2.5 py-1 rounded bg-gray-800 hover:bg-gray-700 disabled:opacity-40 disabled:cursor-not-allowed"
                >
                  Prev
                </button>
                <button
                  onClick={() => loadHistory(page + 1)}
                  disabled={page >= totalPages}
                  className="px-2.5 py-1 rounded bg-gray-800 hover:bg-gray-700 disabled:opacity-40 disabled:cursor-not-allowed"
                >
                  Next
                </button>
              </div>
            </div>
          </>
        ) : (
          <p className="border-t border-gray-800 px-3 py-6 text-center text-xs text-gray-600">
            No saved signals yet.
          </p>
        )}
      </div>
    </div>
  )
}
