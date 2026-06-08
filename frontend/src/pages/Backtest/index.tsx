import { useEffect, useRef, useState } from 'react'
import {
  ResponsiveContainer, LineChart, Line, XAxis, YAxis, Tooltip, CartesianGrid,
} from 'recharts'
import { backtestApi } from '../../services/api'
import type { BacktestResult } from '../../types'

const SYMBOLS = ['NIFTY', 'BANKNIFTY']
const STRATEGIES = ['LongCall', 'LongPut', 'BullCallSpread', 'BearPutSpread', 'Straddle', 'Strangle', 'IronCondor']
const ENTRY_CONDITIONS = [
  'RSI_Oversold', 'RSI_Overbought', 'MACD_Bullish_Cross', 'MACD_Bearish_Cross',
  'SupertrendBullish', 'SupertrendBearish', 'PriceBreakoutAboveR1', 'PriceBreakdownBelowS1',
]
const EXIT_CONDITIONS = ['SLHit', 'Target1Hit', 'Target2Hit', 'ThetaDecay50Pct', 'ExpiryMinus1Day']

const growwEnabled = import.meta.env.VITE_GROWW_ENABLED === 'true'
const SUBTITLE = growwEnabled
  ? 'Backtest using 90 days of real NSE historical data from Groww'
  : 'Simulate options strategies over 90 days of historical data'

const PAGE_SIZE = 10

const fmtMoney = (v: number) =>
  v.toLocaleString('en-IN', { maximumFractionDigits: 0 })

function StatCard({ label, value, accent }: { label: string; value: string; accent?: 'pos' | 'neg' }) {
  return (
    <div className="bg-gray-900 border border-gray-800 rounded-xl p-3">
      <p className="text-[10px] text-gray-500 uppercase tracking-wide mb-1">{label}</p>
      <p className={[
        'text-base font-bold',
        accent === 'pos' ? 'text-emerald-400' : accent === 'neg' ? 'text-red-400' : 'text-white',
      ].join(' ')}>
        {value}
      </p>
    </div>
  )
}

function ResultPanel({ result }: { result: BacktestResult }) {
  const [page, setPage] = useState(0)

  const chartData = (() => {
    let cumulative = 0
    return result.tradeLog.map((t, i) => {
      cumulative += t.pnL
      return { trade: i + 1, cumulative: Math.round(cumulative) }
    })
  })()

  const totalPages = Math.max(1, Math.ceil(result.tradeLog.length / PAGE_SIZE))
  const pageStart = page * PAGE_SIZE
  const pageRows = result.tradeLog.slice(pageStart, pageStart + PAGE_SIZE)

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
        <StatCard label="Win Rate" value={`${result.winRate.toFixed(1)}%`} />
        <StatCard
          label="Net P&L"
          value={`₹${fmtMoney(result.netPnl)}`}
          accent={result.netPnl >= 0 ? 'pos' : 'neg'}
        />
        <StatCard label="Max Drawdown" value={`₹${fmtMoney(result.maxDrawdown)}`} accent="neg" />
        <StatCard label="Sharpe Ratio" value={result.sharpeRatio.toFixed(2)} />
        <StatCard label="Profit Factor" value={result.profitFactor.toFixed(2)} />
        <StatCard label="Total Trades" value={String(result.totalTrades)} />
      </div>

      <div className="bg-gray-900 border border-gray-800 rounded-xl p-3">
        <h3 className="text-xs font-medium text-gray-400 uppercase tracking-wide mb-3">
          Cumulative P&amp;L
        </h3>
        {chartData.length > 0 ? (
          <ResponsiveContainer width="100%" height={220}>
            <LineChart data={chartData} margin={{ top: 4, right: 8, left: -8, bottom: 0 }}>
              <CartesianGrid stroke="#1f2937" strokeDasharray="3 3" />
              <XAxis
                dataKey="trade"
                tick={{ fill: '#9ca3af', fontSize: 10 }}
                tickLine={false}
                axisLine={{ stroke: '#374151' }}
                label={{ value: 'Trade #', position: 'insideBottom', offset: -2, fill: '#6b7280', fontSize: 10 }}
              />
              <YAxis
                tickFormatter={(v) => `₹${fmtMoney(Number(v))}`}
                tick={{ fill: '#9ca3af', fontSize: 10 }}
                tickLine={false}
                axisLine={false}
              />
              <Tooltip
                contentStyle={{ backgroundColor: '#111827', border: '1px solid #374151', borderRadius: 6 }}
                labelStyle={{ color: '#f9fafb', fontSize: 11 }}
                itemStyle={{ fontSize: 11 }}
                formatter={(value: unknown) => [`₹${fmtMoney(Number(value))}`, 'Cumulative P&L']}
                labelFormatter={(label) => `Trade #${label}`}
              />
              <Line type="monotone" dataKey="cumulative" stroke="#22c55e" strokeWidth={2} dot={false} />
            </LineChart>
          </ResponsiveContainer>
        ) : (
          <p className="text-xs text-gray-600 text-center py-8">No trades were generated for this run</p>
        )}
      </div>

      <div className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
        <h3 className="text-xs font-medium text-gray-400 uppercase tracking-wide px-3 pt-3 pb-2">
          Trade Log
        </h3>
        {pageRows.length > 0 ? (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-xs">
                <thead>
                  <tr className="text-gray-500 border-t border-gray-800">
                    <th className="text-left font-medium px-3 py-2">Entry</th>
                    <th className="text-left font-medium px-3 py-2">Exit</th>
                    <th className="text-left font-medium px-3 py-2">Contract</th>
                    <th className="text-right font-medium px-3 py-2">Entry ₹</th>
                    <th className="text-right font-medium px-3 py-2">Exit ₹</th>
                    <th className="text-right font-medium px-3 py-2">P&amp;L</th>
                    <th className="text-left font-medium px-3 py-2">Reason</th>
                  </tr>
                </thead>
                <tbody>
                  {pageRows.map((t, i) => (
                    <tr key={pageStart + i} className="border-t border-gray-800/60">
                      <td className="px-3 py-2 text-gray-400 whitespace-nowrap">{t.entryDate}</td>
                      <td className="px-3 py-2 text-gray-400 whitespace-nowrap">{t.exitDate}</td>
                      <td className="px-3 py-2 text-gray-300 whitespace-nowrap">{t.contract}</td>
                      <td className="px-3 py-2 text-right text-gray-300">{t.entryPrice.toFixed(2)}</td>
                      <td className="px-3 py-2 text-right text-gray-300">{t.exitPrice.toFixed(2)}</td>
                      <td className={`px-3 py-2 text-right font-semibold ${t.pnL >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>
                        {t.pnL >= 0 ? '+' : ''}{fmtMoney(t.pnL)}
                      </td>
                      <td className="px-3 py-2 text-gray-400 whitespace-nowrap">{t.exitReason}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="flex items-center justify-between px-3 py-2.5 border-t border-gray-800 text-xs text-gray-400">
              <span>
                {pageStart + 1}–{Math.min(pageStart + PAGE_SIZE, result.tradeLog.length)} of {result.tradeLog.length}
              </span>
              <div className="flex gap-2">
                <button
                  onClick={() => setPage((p) => Math.max(0, p - 1))}
                  disabled={page === 0}
                  className="px-2.5 py-1 rounded bg-gray-800 hover:bg-gray-700 disabled:opacity-40 disabled:cursor-not-allowed"
                >
                  Prev
                </button>
                <button
                  onClick={() => setPage((p) => Math.min(totalPages - 1, p + 1))}
                  disabled={page >= totalPages - 1}
                  className="px-2.5 py-1 rounded bg-gray-800 hover:bg-gray-700 disabled:opacity-40 disabled:cursor-not-allowed"
                >
                  Next
                </button>
              </div>
            </div>
          </>
        ) : (
          <p className="text-xs text-gray-600 text-center py-6">No trades were generated for this run</p>
        )}
      </div>
    </div>
  )
}

export default function Backtest() {
  const [symbol, setSymbol] = useState(SYMBOLS[0])
  const [strategy, setStrategy] = useState(STRATEGIES[0])
  const [entryCondition, setEntryCondition] = useState(ENTRY_CONDITIONS[0])
  const [exitCondition, setExitCondition] = useState(EXIT_CONDITIONS[0])
  const [periodDays, setPeriodDays] = useState(30)
  const [lots, setLots] = useState(1)

  const [running, setRunning] = useState(false)
  const [elapsed, setElapsed] = useState(0)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<BacktestResult | null>(null)

  const [history, setHistory] = useState<BacktestResult[]>([])
  const [historyLoading, setHistoryLoading] = useState(true)
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  function loadHistory() {
    setHistoryLoading(true)
    backtestApi.getHistory()
      .then(setHistory)
      .catch(() => {})
      .finally(() => setHistoryLoading(false))
  }

  useEffect(() => { loadHistory() }, [])

  function startTimer() {
    setElapsed(0)
    timerRef.current = setInterval(() => setElapsed((e) => e + 1), 1000)
  }
  function stopTimer() {
    if (timerRef.current) { clearInterval(timerRef.current); timerRef.current = null }
  }

  async function handleRun() {
    setError(null)
    setRunning(true)
    startTimer()
    try {
      const res = await backtestApi.run({ symbol, strategy, entryCondition, exitCondition, periodDays, lots })
      setResult(res)
      setSelectedId(res.id)
      loadHistory()
    } catch (e: unknown) {
      const msg =
        (e as { response?: { data?: { error?: string } } })?.response?.data?.error
        ?? 'Backtest run failed. Please try again.'
      setError(msg)
    } finally {
      stopTimer()
      setRunning(false)
    }
  }

  function handleSelectHistory(item: BacktestResult) {
    setResult(item)
    setSelectedId(item.id)
  }

  return (
    <div className="p-4 space-y-5 max-w-5xl mx-auto">
      <div>
        <h1 className="text-xl font-semibold text-white">Backtest</h1>
        <p className="text-gray-400 text-sm mt-0.5">
          {SUBTITLE}
        </p>
      </div>

      {/* Configuration form */}
      <div className="bg-gray-900 border border-gray-800 rounded-xl p-4 space-y-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          <div>
            <label className="block text-xs text-gray-400 mb-1">Symbol</label>
            <select
              value={symbol}
              onChange={(e) => setSymbol(e.target.value)}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white"
            >
              {SYMBOLS.map((s) => <option key={s} value={s}>{s}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1">Strategy</label>
            <select
              value={strategy}
              onChange={(e) => setStrategy(e.target.value)}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white"
            >
              {STRATEGIES.map((s) => <option key={s} value={s}>{s}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1">Lots</label>
            <input
              type="number"
              min={1}
              value={lots}
              onChange={(e) => setLots(Math.max(1, Number(e.target.value) || 1))}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white"
            />
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1">Entry Condition</label>
            <select
              value={entryCondition}
              onChange={(e) => setEntryCondition(e.target.value)}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white"
            >
              {ENTRY_CONDITIONS.map((c) => <option key={c} value={c}>{c}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1">Exit Condition</label>
            <select
              value={exitCondition}
              onChange={(e) => setExitCondition(e.target.value)}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white"
            >
              {EXIT_CONDITIONS.map((c) => <option key={c} value={c}>{c}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1">Period (days)</label>
            <input
              type="number"
              min={5}
              max={90}
              value={periodDays}
              onChange={(e) => setPeriodDays(Math.min(90, Math.max(5, Number(e.target.value) || 5)))}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white"
            />
          </div>
        </div>

        <button
          onClick={handleRun}
          disabled={running}
          className="w-full sm:w-auto flex flex-col items-center justify-center py-2.5 px-6 min-h-[44px] bg-emerald-800 hover:bg-emerald-700 disabled:bg-gray-800 disabled:cursor-not-allowed rounded-lg transition-colors"
        >
          <span className="text-xs font-semibold text-white">
            {running ? `Running… ${elapsed}s` : 'Run Backtest'}
          </span>
        </button>

        {error && (
          <p className="text-xs text-red-400 bg-red-900/20 border border-red-800 rounded-lg px-3 py-2">
            {error}
          </p>
        )}
      </div>

      {/* Result */}
      {result && <ResultPanel result={result} />}

      {/* History / compare — every run is auto-saved, so this list doubles as the compare view */}
      <div className="space-y-2">
        <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
          Saved Runs
        </h2>
        {historyLoading ? (
          <div className="bg-gray-900 border border-gray-800 rounded-xl p-6 animate-pulse h-16" />
        ) : history.length > 0 ? (
          <div className="space-y-1.5">
            {history.map((h) => (
              <button
                key={h.id}
                onClick={() => handleSelectHistory(h)}
                className={[
                  'w-full text-left flex flex-wrap items-center gap-x-4 gap-y-1 px-3 py-2.5 rounded-lg border transition-colors text-xs',
                  selectedId === h.id
                    ? 'bg-emerald-400/10 border-emerald-400/40'
                    : 'bg-gray-900 border-gray-800 hover:bg-gray-800',
                ].join(' ')}
              >
                <span className="text-white font-semibold">{h.symbol} · {h.strategy}</span>
                <span className="text-gray-500">{h.entryCondition} → {h.exitCondition}</span>
                <span className="text-gray-500">{h.periodDays}d · {h.lots} lot{h.lots > 1 ? 's' : ''}</span>
                <span className={h.netPnl >= 0 ? 'text-emerald-400 font-semibold' : 'text-red-400 font-semibold'}>
                  ₹{fmtMoney(h.netPnl)}
                </span>
                <span className="text-gray-500">Win {h.winRate.toFixed(1)}% · {h.totalTrades} trades</span>
                <span className="text-gray-600 ml-auto">
                  {new Date(h.createdAt).toLocaleString('en-IN', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}
                </span>
              </button>
            ))}
          </div>
        ) : (
          <p className="text-xs text-gray-600 text-center py-3">No saved backtests yet — run one above</p>
        )}
      </div>
    </div>
  )
}
