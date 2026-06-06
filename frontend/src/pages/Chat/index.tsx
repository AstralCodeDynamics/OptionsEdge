import { useState, useEffect, useRef } from 'react'
import { signalsApi } from '../../services/api'
import { useAppStore } from '../../store/appStore'
import { SignalCard } from '../../components/signals/SignalCard'
import type { Signal } from '../../types'

const SYMBOLS = ['NIFTY', 'BANKNIFTY'] as const
type Symbol = (typeof SYMBOLS)[number]

const COST_SONNET = '$0.015'
const COST_HAIKU  = '$0.00035'

export default function AIAnalysis() {
  const snapshots     = useAppStore((s) => s.snapshots)
  const storeSignals  = useAppStore((s) => s.signals)
  const prependSignal = useAppStore((s) => s.prependSignal)
  const setSignals    = useAppStore((s) => s.setSignals)

  const [symbol, setSymbol]   = useState<Symbol>('NIFTY')
  const [loading, setLoading] = useState(false)
  const [elapsed, setElapsed] = useState(0)
  const [error, setError]     = useState<string | null>(null)
  const timerRef              = useRef<ReturnType<typeof setInterval> | null>(null)

  const snapshot = snapshots[symbol]

  useEffect(() => {
    signalsApi.getHistory(undefined, 20)
      .then(setSignals)
      .catch(() => {})
  }, [])

  function startTimer() {
    setElapsed(0)
    timerRef.current = setInterval(() => setElapsed((e) => e + 1), 1000)
  }
  function stopTimer() {
    if (timerRef.current) { clearInterval(timerRef.current); timerRef.current = null }
  }

  async function handleGenerate() {
    setError(null)
    setLoading(true)
    startTimer()
    try {
      const sig = await signalsApi.generate(symbol)
      prependSignal(sig)
    } catch (e: unknown) {
      const msg =
        (e as { response?: { data?: { error?: string } } })?.response?.data?.error
        ?? 'Failed to generate signal. Check your API key and market hours.'
      setError(msg)
    } finally {
      stopTimer()
      setLoading(false)
    }
  }

  const displaySignals: Signal[] = storeSignals.filter((s) => s.symbol === symbol)

  return (
    <div className="p-4 space-y-5 max-w-2xl mx-auto">
      <div>
        <h1 className="text-lg font-bold text-white">AI Signal Engine</h1>
        <p className="text-xs text-gray-400 mt-0.5">
          Claude-powered options signals. Deep analysis via Sonnet.
        </p>
      </div>

      {/* Symbol selector + live price */}
      <div className="flex items-center gap-2 flex-wrap">
        {SYMBOLS.map((sym) => (
          <button
            key={sym}
            onClick={() => setSymbol(sym)}
            className={`px-4 py-1.5 rounded-full text-xs font-semibold transition-colors ${
              symbol === sym
                ? 'bg-blue-600 text-white'
                : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
            }`}
          >
            {sym}
          </button>
        ))}
        {snapshot && (
          <span className="ml-auto text-xs text-gray-400">
            {snapshot.ltp.toLocaleString('en-IN', { maximumFractionDigits: 2 })}
            <span className={`ml-1 ${snapshot.changePct >= 0 ? 'text-green-400' : 'text-red-400'}`}>
              {snapshot.changePct >= 0 ? '+' : ''}{snapshot.changePct.toFixed(2)}%
            </span>
          </span>
        )}
      </div>

      {/* Generate button */}
      <div className="bg-gray-900 border border-gray-700 rounded-xl p-4 space-y-3">
        <button
          onClick={handleGenerate}
          disabled={loading}
          className="w-full flex flex-col items-center gap-1 px-4 py-3 bg-emerald-700 hover:bg-emerald-600 disabled:bg-gray-700 disabled:cursor-not-allowed text-white rounded-lg transition-colors"
        >
          <span className="text-sm font-semibold">
            {loading ? `Analyzing… ${elapsed}s` : `Generate ${symbol} Signal`}
          </span>
          <span className="text-[10px] text-emerald-200/70">
            claude-sonnet · estimated {COST_SONNET}
          </span>
        </button>
        <div className="flex justify-between text-[10px] text-gray-600">
          <span>Haiku {COST_HAIKU} · Sonnet {COST_SONNET}</span>
          <span>10 AI calls / hr</span>
        </div>
      </div>

      {/* Error */}
      {error && (
        <div className="bg-red-900/30 border border-red-700 rounded-lg px-4 py-3 text-sm text-red-300">
          {error}
        </div>
      )}

      {/* Latest signal */}
      {displaySignals.length > 0 && (
        <div className="space-y-3">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
            Latest Signal
          </h2>
          <SignalCard signal={displaySignals[0]} />
        </div>
      )}

      {/* History */}
      {displaySignals.length > 1 && (
        <div className="space-y-3">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
            Signal History
          </h2>
          {displaySignals.slice(1).map((sig) => (
            <SignalCard key={sig.id} signal={sig} />
          ))}
        </div>
      )}

      {displaySignals.length === 0 && !loading && !error && (
        <p className="text-center py-10 text-gray-600 text-sm">
          No {symbol} signals yet. Hit "Generate" to get an AI trade idea.
        </p>
      )}

      {/* Usage note */}
      <div className="text-[10px] text-gray-700 text-center pb-2">
        AI calls cost real money. Haiku ≈ {COST_HAIKU} · Sonnet ≈ {COST_SONNET} per call.
      </div>
    </div>
  )
}
