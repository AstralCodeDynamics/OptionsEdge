import { useEffect, useRef, useState } from 'react'
import {
  ResponsiveContainer, LineChart, Line, XAxis, YAxis, Tooltip, CartesianGrid, ReferenceLine, ReferenceDot,
} from 'recharts'
import { backtestApi, marketApi } from '../../services/api'
import type { BacktestResult, BacktestTradeLogEntry, Candle } from '../../types'

const SYMBOLS = ['NIFTY', 'BANKNIFTY']
const STRATEGIES = ['LongCall', 'LongPut', 'BullCallSpread', 'BearPutSpread', 'Straddle', 'Strangle', 'IronCondor']
const ENTRY_CONDITIONS = [
  'PivotEma20Bullish', 'PivotEma20Bearish',
  'RSI_Oversold', 'RSI_Overbought', 'MACD_Bullish_Cross', 'MACD_Bearish_Cross',
  'SupertrendBullish', 'SupertrendBearish', 'PriceBreakoutAboveR1', 'PriceBreakdownBelowS1',
]
const EXIT_CONDITIONS = ['Target1Hit', 'Target2Hit', 'SLHit', 'ThetaDecay50Pct', 'ExpiryMinus1Day']
const LABELS: Record<string, string> = {
  LongCall: 'Long Call',
  LongPut: 'Long Put',
  BullCallSpread: 'Bull Call Spread',
  BearPutSpread: 'Bear Put Spread',
  IronCondor: 'Iron Condor',
  PivotEma20Bullish: 'Pivot + EMA20 Bullish',
  PivotEma20Bearish: 'Pivot + EMA20 Bearish',
  RSI_Oversold: 'RSI Oversold',
  RSI_Overbought: 'RSI Overbought',
  MACD_Bullish_Cross: 'MACD Bullish Cross',
  MACD_Bearish_Cross: 'MACD Bearish Cross',
  PriceBreakoutAboveR1: 'Breakout Above R1',
  PriceBreakdownBelowS1: 'Breakdown Below S1',
  Target1Hit: 'Target 1 Hit',
  Target2Hit: 'Target 2 Hit',
  SLHit: 'Stop Loss Hit',
  ThetaDecay50Pct: 'Theta Decay 50%',
  ExpiryMinus1Day: 'Expiry Minus 1 Day',
}

const growwEnabled = import.meta.env.VITE_GROWW_ENABLED === 'true'
const SUBTITLE = growwEnabled
  ? 'Backtest using 90 days of real NSE historical data from Groww'
  : 'Simulate options strategies over 90 days of historical data'

const PAGE_SIZE = 10
const HISTORY_PAGE_SIZE = 8

const fmtMoney = (v: number) =>
  v.toLocaleString('en-IN', { maximumFractionDigits: 0 })
const fmtPoints = (v?: number | null) =>
  v == null ? '—' : v.toFixed(2)

const tradePnl = (trade: { pnL?: number; pnl?: number }) => trade.pnL ?? trade.pnl ?? 0
const labelFor = (value: string) => LABELS[value] ?? value
const parseTradeTime = (value: string) => Math.floor(new Date(`${value.replace(' ', 'T')}:00Z`).getTime() / 1000)
const formatChartTime = (seconds: number) =>
  new Date(seconds * 1000).toLocaleString('en-IN', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  })
const positionSideFor = (strategy: string) => strategy === 'IronCondor' ? 'Short premium' : 'Long premium'

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

function TradeChartModal({
  result,
  trade,
  onClose,
}: {
  result: BacktestResult
  trade: BacktestTradeLogEntry
  onClose: () => void
}) {
  const [candles, setCandles] = useState<Candle[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)
    marketApi.getCandles(result.symbol)
      .then((rows) => {
        if (!cancelled) setCandles(rows)
      })
      .catch(() => {
        if (!cancelled) setError('Could not load candles for this trade.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => { cancelled = true }
  }, [result.symbol])

  const entryTs = parseTradeTime(trade.entryDate)
  const exitTs = parseTradeTime(trade.exitDate)
  const from = entryTs - 2 * 24 * 60 * 60
  const to = exitTs + 2 * 24 * 60 * 60
  const chartRows = candles
    .filter((c) => c.time >= from && c.time <= to)
    .map((c) => ({
      time: c.time,
      close: c.close,
      label: formatChartTime(c.time),
    }))

  const nearest = (target: number) =>
    chartRows.reduce<(typeof chartRows)[number] | null>((best, row) => {
      if (!best) return row
      return Math.abs(row.time - target) < Math.abs(best.time - target) ? row : best
    }, null)

  const entryPoint = nearest(entryTs)
  const exitPoint = nearest(exitTs)
  const side = positionSideFor(result.strategy)

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4">
      <div className="w-full max-w-5xl max-h-[90vh] overflow-y-auto rounded-xl border border-gray-700 bg-gray-950 shadow-2xl">
        <div className="flex flex-wrap items-start justify-between gap-3 border-b border-gray-800 px-4 py-3">
          <div>
            <p className="text-sm font-semibold text-white">
              {result.symbol} · {labelFor(result.strategy)} · {side}
            </p>
            <p className="mt-1 text-xs text-gray-500">
              Entry {trade.entryDate} · Exit {trade.exitDate} · {labelFor(trade.exitReason)}
            </p>
          </div>
          <button
            onClick={onClose}
            className="rounded bg-gray-800 px-3 py-1.5 text-xs font-medium text-gray-200 hover:bg-gray-700"
          >
            Close
          </button>
        </div>

        <div className="grid grid-cols-2 gap-3 px-4 py-3 text-xs sm:grid-cols-5">
          <div>
            <p className="text-gray-500">Entry premium</p>
            <p className="font-semibold text-white">{trade.entryPrice.toFixed(2)}</p>
          </div>
          <div>
            <p className="text-gray-500">Exit premium</p>
            <p className="font-semibold text-white">{trade.exitPrice.toFixed(2)}</p>
          </div>
          <div>
            <p className="text-gray-500">SL</p>
            <p className="font-semibold text-red-300">{fmtPoints(trade.stopLossPrice)}</p>
          </div>
          <div>
            <p className="text-gray-500">Target 1</p>
            <p className="font-semibold text-emerald-300">{fmtPoints(trade.target1Price)}</p>
          </div>
          <div>
            <p className="text-gray-500">P&L</p>
            <p className={tradePnl(trade) >= 0 ? 'font-semibold text-emerald-300' : 'font-semibold text-red-300'}>
              {tradePnl(trade) >= 0 ? '+' : ''}₹{fmtMoney(tradePnl(trade))}
            </p>
          </div>
        </div>

        <div className="px-4 pb-4">
          <div className="h-[340px] rounded-lg border border-gray-800 bg-gray-900 p-3">
            {loading ? (
              <div className="flex h-full items-center justify-center text-xs text-gray-500">Loading chart…</div>
            ) : error ? (
              <div className="flex h-full items-center justify-center text-xs text-red-400">{error}</div>
            ) : chartRows.length > 0 ? (
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={chartRows} margin={{ top: 8, right: 12, left: -8, bottom: 8 }}>
                  <CartesianGrid stroke="#1f2937" strokeDasharray="3 3" />
                  <XAxis
                    dataKey="time"
                    type="number"
                    domain={['dataMin', 'dataMax']}
                    tickFormatter={(v) => formatChartTime(Number(v))}
                    tick={{ fill: '#9ca3af', fontSize: 10 }}
                    tickLine={false}
                    axisLine={{ stroke: '#374151' }}
                  />
                  <YAxis
                    domain={['auto', 'auto']}
                    tick={{ fill: '#9ca3af', fontSize: 10 }}
                    tickLine={false}
                    axisLine={false}
                    tickFormatter={(v) => fmtMoney(Number(v))}
                  />
                  <Tooltip
                    contentStyle={{ backgroundColor: '#111827', border: '1px solid #374151', borderRadius: 6 }}
                    labelStyle={{ color: '#f9fafb', fontSize: 11 }}
                    itemStyle={{ fontSize: 11 }}
                    formatter={(value: unknown) => [fmtMoney(Number(value)), `${result.symbol} close`]}
                    labelFormatter={(label) => formatChartTime(Number(label))}
                  />
                  <ReferenceLine x={entryPoint?.time} stroke="#22c55e" strokeDasharray="4 4" label={{ value: 'Entry', fill: '#22c55e', fontSize: 11 }} />
                  <ReferenceLine x={exitPoint?.time} stroke="#f97316" strokeDasharray="4 4" label={{ value: 'Exit', fill: '#f97316', fontSize: 11 }} />
                  <Line type="monotone" dataKey="close" stroke="#38bdf8" strokeWidth={2} dot={false} />
                  {entryPoint && <ReferenceDot x={entryPoint.time} y={entryPoint.close} r={5} fill="#22c55e" stroke="#052e16" />}
                  {exitPoint && <ReferenceDot x={exitPoint.time} y={exitPoint.close} r={5} fill="#f97316" stroke="#431407" />}
                </LineChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex h-full items-center justify-center text-xs text-gray-500">
                No candles found around this trade window.
              </div>
            )}
          </div>
          <p className="mt-2 text-[11px] text-gray-600">
            Chart shows {result.symbol} index candles around the trade. Entry/exit premiums are simulated option prices from the backtest.
          </p>
        </div>
      </div>
    </div>
  )
}

function ResultPanel({ result }: { result: BacktestResult }) {
  const [page, setPage] = useState(0)
  const [selectedTrade, setSelectedTrade] = useState<BacktestTradeLogEntry | null>(null)
  const targetHits = result.tradeLog.filter((t) => t.exitReason.startsWith('Target')).length
  const slHits = result.tradeLog.filter((t) => t.exitReason === 'SLHit').length
  const sourceLabel = result.dataSource === 'groww'
    ? 'Groww historical candles'
    : result.dataSource === 'mock'
      ? 'Mock historical candles'
      : 'Historical candles'

  const chartData = (() => {
    let cumulative = 0
    return result.tradeLog.map((t, i) => {
      cumulative += tradePnl(t)
      return { trade: i + 1, cumulative: Math.round(cumulative) }
    })
  })()

  const totalPages = Math.max(1, Math.ceil(result.tradeLog.length / PAGE_SIZE))
  const pageStart = page * PAGE_SIZE
  const pageRows = result.tradeLog.slice(pageStart, pageStart + PAGE_SIZE)

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2 text-xs text-gray-400">
        <span className={[
          'px-2 py-1 rounded border font-medium',
          result.dataSource === 'groww'
            ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-300'
            : 'bg-gray-800 border-gray-700 text-gray-300',
        ].join(' ')}>
          {sourceLabel}
        </span>
        <span>{result.candleCount.toLocaleString('en-IN')} candles</span>
        <span>·</span>
        <span>{result.tradingDays.toLocaleString('en-IN')} trading days</span>
        {result.targetPoints != null && result.stopLossPoints != null && (
          <>
            <span>·</span>
            <span>T {result.targetPoints.toFixed(0)} pts / SL {result.stopLossPoints.toFixed(0)} pts</span>
          </>
        )}
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-4 lg:grid-cols-8 gap-3">
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
        <StatCard label="Target Hits" value={String(targetHits)} accent="pos" />
        <StatCard label="SL Hits" value={String(slHits)} accent={slHits > 0 ? 'neg' : undefined} />
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
          <p className="text-xs text-gray-600 text-center py-8">
            No entries matched this rule set during the selected period.
          </p>
        )}
      </div>

      <div className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
        <div className="px-3 pt-3 pb-2">
          <h3 className="text-xs font-medium text-gray-400 uppercase tracking-wide">
            Trade Log
          </h3>
          <p className="mt-1 text-[11px] text-gray-600">
            SL/T1/T2 are option premium points: SL -35%, T1 +70%, T2 +120% from each trade entry.
          </p>
        </div>
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
                    <th className="text-right font-medium px-3 py-2">SL pts</th>
                    <th className="text-right font-medium px-3 py-2">T1 pts</th>
                    <th className="text-right font-medium px-3 py-2">T2 pts</th>
                    <th className="text-right font-medium px-3 py-2">Exit ₹</th>
                    <th className="text-right font-medium px-3 py-2">P&amp;L</th>
                    <th className="text-left font-medium px-3 py-2">Reason</th>
                  </tr>
                </thead>
                <tbody>
                  {pageRows.map((t, i) => (
                    <tr
                      key={pageStart + i}
                      onClick={() => setSelectedTrade(t)}
                      className="cursor-pointer border-t border-gray-800/60 hover:bg-gray-800/40"
                    >
                      <td className="px-3 py-2 text-gray-400 whitespace-nowrap">{t.entryDate}</td>
                      <td className="px-3 py-2 text-gray-400 whitespace-nowrap">{t.exitDate}</td>
                      <td className="px-3 py-2 text-gray-300 whitespace-nowrap">{t.contract}</td>
                      <td className="px-3 py-2 text-right text-gray-300">{t.entryPrice.toFixed(2)}</td>
                      <td className="px-3 py-2 text-right text-red-300">{fmtPoints(t.stopLossPrice)}</td>
                      <td className="px-3 py-2 text-right text-emerald-300">{fmtPoints(t.target1Price)}</td>
                      <td className="px-3 py-2 text-right text-emerald-300">{fmtPoints(t.target2Price)}</td>
                      <td className="px-3 py-2 text-right text-gray-300">{t.exitPrice.toFixed(2)}</td>
                      <td className={`px-3 py-2 text-right font-semibold ${tradePnl(t) >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>
                        {tradePnl(t) >= 0 ? '+' : ''}{fmtMoney(tradePnl(t))}
                      </td>
                      <td className="px-3 py-2 text-gray-400 whitespace-nowrap">{labelFor(t.exitReason)}</td>
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
          <p className="text-xs text-gray-600 text-center py-6">
            Try a different entry condition or a longer period to generate trades.
          </p>
        )}
      </div>
      {selectedTrade && (
        <TradeChartModal
          result={result}
          trade={selectedTrade}
          onClose={() => setSelectedTrade(null)}
        />
      )}
    </div>
  )
}

export default function Backtest() {
  const [symbol, setSymbol] = useState(SYMBOLS[0])
  const [strategy, setStrategy] = useState(STRATEGIES[0])
  const [entryCondition, setEntryCondition] = useState(ENTRY_CONDITIONS[0])
  const [exitCondition, setExitCondition] = useState(EXIT_CONDITIONS[0])
  const [periodDays, setPeriodDays] = useState(90)
  const [lots, setLots] = useState(1)
  const [targetPoints, setTargetPoints] = useState(30)
  const [stopLossPoints, setStopLossPoints] = useState(15)

  const [running, setRunning] = useState(false)
  const [elapsed, setElapsed] = useState(0)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<BacktestResult | null>(null)

  const [history, setHistory] = useState<BacktestResult[]>([])
  const [historyLoading, setHistoryLoading] = useState(true)
  const [historyPage, setHistoryPage] = useState(1)
  const [historyTotalPages, setHistoryTotalPages] = useState(1)
  const [historyTotalItems, setHistoryTotalItems] = useState(0)
  const [historyRetentionDays, setHistoryRetentionDays] = useState(30)
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  function loadHistory(page = historyPage) {
    setHistoryLoading(true)
    backtestApi.getHistory({ page, pageSize: HISTORY_PAGE_SIZE })
      .then((res) => {
        setHistory(res.items)
        setHistoryPage(res.page)
        setHistoryTotalPages(res.totalPages)
        setHistoryTotalItems(res.totalItems)
        setHistoryRetentionDays(res.retentionDays)
      })
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
      const res = await backtestApi.run({
        symbol,
        strategy,
        entryCondition,
        exitCondition,
        periodDays,
        lots,
        targetPoints,
        stopLossPoints,
      })
      setResult(res)
      setSelectedId(res.id)
      loadHistory(1)
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
            <label className="block text-xs text-gray-400 mb-1">Options Strategy</label>
            <select
              value={strategy}
              onChange={(e) => setStrategy(e.target.value)}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white"
            >
              {STRATEGIES.map((s) => <option key={s} value={s}>{labelFor(s)}</option>)}
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
            <label className="block text-xs text-gray-400 mb-1">Signal Strategy</label>
            <select
              value={entryCondition}
              onChange={(e) => setEntryCondition(e.target.value)}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white"
            >
              {ENTRY_CONDITIONS.map((c) => <option key={c} value={c}>{labelFor(c)}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1">Exit Condition</label>
            <select
              value={exitCondition}
              onChange={(e) => setExitCondition(e.target.value)}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white"
            >
              {EXIT_CONDITIONS.map((c) => <option key={c} value={c}>{labelFor(c)}</option>)}
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
          <div>
            <label className="block text-xs text-gray-400 mb-1">Target pts</label>
            <input
              type="number"
              min={1}
              step={1}
              value={targetPoints}
              onChange={(e) => setTargetPoints(Math.max(1, Number(e.target.value) || 1))}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm text-white"
            />
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1">SL pts</label>
            <input
              type="number"
              min={1}
              step={1}
              value={stopLossPoints}
              onChange={(e) => setStopLossPoints(Math.max(1, Number(e.target.value) || 1))}
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
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
            Saved Runs
          </h2>
          <p className="text-[11px] text-gray-600">
            {historyTotalItems} saved · kept {historyRetentionDays} days
          </p>
        </div>
        {historyLoading ? (
          <div className="bg-gray-900 border border-gray-800 rounded-xl p-6 animate-pulse h-16" />
        ) : history.length > 0 ? (
          <>
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
                  <span className="text-white font-semibold">{h.symbol} · {labelFor(h.strategy)}</span>
                <span className="text-gray-500">{labelFor(h.entryCondition)} → {labelFor(h.exitCondition)}</span>
                <span className="text-gray-500">{h.periodDays}d · {h.lots} lot{h.lots > 1 ? 's' : ''}</span>
                {h.targetPoints != null && h.stopLossPoints != null && (
                  <span className="text-gray-500">T {h.targetPoints.toFixed(0)} · SL {h.stopLossPoints.toFixed(0)}</span>
                )}
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
            <div className="flex items-center justify-between pt-2 text-xs text-gray-500">
              <span>
                Page {historyPage} of {historyTotalPages}
              </span>
              <div className="flex gap-2">
                <button
                  onClick={() => loadHistory(historyPage - 1)}
                  disabled={historyPage <= 1}
                  className="px-3 py-1.5 rounded bg-gray-900 border border-gray-800 hover:bg-gray-800 disabled:opacity-40 disabled:cursor-not-allowed"
                >
                  Prev
                </button>
                <button
                  onClick={() => loadHistory(historyPage + 1)}
                  disabled={historyPage >= historyTotalPages}
                  className="px-3 py-1.5 rounded bg-gray-900 border border-gray-800 hover:bg-gray-800 disabled:opacity-40 disabled:cursor-not-allowed"
                >
                  Next
                </button>
              </div>
            </div>
          </>
        ) : (
          <p className="text-xs text-gray-600 text-center py-3">No saved backtests yet — run one above</p>
        )}
      </div>
    </div>
  )
}
