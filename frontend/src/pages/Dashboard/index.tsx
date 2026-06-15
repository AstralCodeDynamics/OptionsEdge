import { useState, useEffect, useRef } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMarketData } from '../../hooks/useMarketData'
import { useAppStore } from '../../store/appStore'
import { marketApi, signalsApi, growwApi, positionsApi } from '../../services/api'
import IndexCard from '../../components/market/IndexCard'
import MarketStatusBanner from '../../components/market/MarketStatusBanner'
import MarketPulse from '../../components/market/MarketPulse'
import { IndicatorPanel } from '../../components/indicators/IndicatorPanel'
import { PivotLevels } from '../../components/market/PivotLevels'
import { PriceChart } from '../../components/charts/PriceChart'
import { SignalCard } from '../../components/signals/SignalCard'
import OrderConfirmModal from '../../components/groww/OrderConfirmModal'
import { IndexCardSkeleton, IndicatorPanelSkeleton } from '../../components/common/Skeleton'
import type { Candle, Signal } from '../../types'

const SYMBOLS = ['NIFTY', 'BANKNIFTY'] as const
type Symbol = (typeof SYMBOLS)[number]

export default function Dashboard() {
  const { connectionState } = useMarketData()
  const snapshots     = useAppStore((s) => s.snapshots)
  const indicators    = useAppStore((s) => s.indicators)
  const storeSignals  = useAppStore((s) => s.signals)
  const prependSignal = useAppStore((s) => s.prependSignal)
  const growwStatus   = useAppStore((s) => s.growwStatus)
  const upsertPosition = useAppStore((s) => s.upsertPosition)
  const navigate      = useNavigate()

  const [activeSymbol, setActiveSymbol] = useState<Symbol>('NIFTY')
  const [candles, setCandles]           = useState<Candle[]>([])
  const [signalLoading, setSignalLoading] = useState(false)
  const [signalElapsed, setSignalElapsed] = useState(0)
  const [signalError, setSignalError]   = useState<string | null>(null)
  const [orderSignal, setOrderSignal]    = useState<Signal | null>(null)
  const [growwPrompt, setGrowwPrompt]    = useState(false)
  const [aiKeyPrompt, setAiKeyPrompt]    = useState(false)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    marketApi.getCandles(activeSymbol)
      .then(setCandles)
      .catch(() => {})
  }, [activeSymbol])

  useEffect(() => {
    if (sessionStorage.getItem('growwPromptDismissed')) return
    growwApi.getStatus()
      .then((s) => { if (s.enabled && !s.connected) setGrowwPrompt(true) })
      .catch(() => {})
  }, [])

  function dismissGrowwPrompt() {
    setGrowwPrompt(false)
    sessionStorage.setItem('growwPromptDismissed', '1')
  }

  function startTimer() {
    setSignalElapsed(0)
    timerRef.current = setInterval(() => setSignalElapsed((e) => e + 1), 1000)
  }
  function stopTimer() {
    if (timerRef.current) { clearInterval(timerRef.current); timerRef.current = null }
  }

  async function handleGenerateSignal() {
    setSignalError(null)
    setSignalLoading(true)
    startTimer()
    try {
      const sig = await signalsApi.generate(activeSymbol)
      setAiKeyPrompt(false)
      prependSignal(sig)
    } catch (e: unknown) {
      const msg =
        (e as { response?: { data?: { error?: string } } })?.response?.data?.error
        ?? 'Signal generation failed. Check API key and market hours.'
      if (msg.includes('No AI API key configured')) setAiKeyPrompt(true)
      setSignalError(msg)
    } finally {
      stopTimer()
      setSignalLoading(false)
    }
  }

  async function handleConfirmOrder(lots: number) {
    if (!orderSignal) return
    const signal = orderSignal

    await growwApi.placeOrder({
      symbol: signal.symbol,
      strike: signal.strike,
      optionType: signal.optionType,
      expiry: signal.expiry,
      quantity: lots,
      price: signal.entryHigh,
      orderType: 'LIMIT',
      transactionType: 'BUY',
    })

    // Auto-create the position in the tracker so it shows up alongside Groww-imported ones
    const created = await positionsApi.create({
      symbol: signal.symbol,
      strike: signal.strike,
      optionType: signal.optionType,
      expiry: signal.expiry,
      entryPrice: signal.entryHigh,
      quantity: lots,
      stopLoss: signal.stopLoss,
      target1: signal.target1,
      target2: signal.target2,
      signalId: signal.id,
    })
    upsertPosition(created)
  }

  const nifty = snapshots['NIFTY']
  const bankNifty = snapshots['BANKNIFTY']
  const activeSnapshot = snapshots[activeSymbol]
  const activeIndicators = indicators[activeSymbol]

  return (
    <div className="p-4 space-y-5 max-w-5xl mx-auto">
      {growwPrompt && (
        <div className="rounded-lg bg-yellow-950/40 border border-yellow-700/40 p-3 flex items-start justify-between gap-3">
          <div className="text-xs text-yellow-300">
            <p className="font-semibold mb-1">🔗 Connect Groww for live market data</p>
            <p>
              Go to <strong>Settings → Security → Groww Integration</strong> to connect your account
              and get real-time prices and alerts.
            </p>
          </div>
          <button
            onClick={dismissGrowwPrompt}
            className="text-gray-500 hover:text-gray-300 text-lg leading-none flex-shrink-0"
            aria-label="Dismiss"
          >
            ×
          </button>
        </div>
      )}

      {aiKeyPrompt && (
        <div className="rounded-lg bg-yellow-950/40 border border-yellow-700/40 p-3 flex items-start justify-between gap-3">
          <div className="text-xs text-yellow-300">
            <p className="font-semibold mb-1">🤖 AI key required</p>
            <p>
              Go to{' '}
              <Link to="/settings/security" className="underline text-yellow-400 hover:text-yellow-300">
                Settings → AI Connection
              </Link>
              {' '}to add your Anthropic API key and enable AI signals.
            </p>
          </div>
          <button
            onClick={() => setAiKeyPrompt(false)}
            className="text-gray-500 hover:text-gray-300 text-lg leading-none flex-shrink-0"
            aria-label="Dismiss"
          >
            ×
          </button>
        </div>
      )}

      <MarketStatusBanner connectionState={connectionState} />

      {/* Index cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {nifty ? <IndexCard snapshot={nifty} /> : <IndexCardSkeleton />}
        {bankNifty ? <IndexCard snapshot={bankNifty} /> : <IndexCardSkeleton />}
      </div>

      {/* Symbol selector */}
      <div className="flex gap-2">
        {SYMBOLS.map((sym) => (
          <button
            key={sym}
            onClick={() => setActiveSymbol(sym)}
            className={`px-4 py-1.5 rounded-full text-xs font-semibold transition-colors ${
              activeSymbol === sym
                ? 'bg-blue-600 text-white'
                : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
            }`}
          >
            {sym}
          </button>
        ))}
      </div>

      {/* Price chart */}
      {candles.length > 0 && (
        <PriceChart
          candles={candles}
          ema={activeIndicators?.ema}
          symbol={activeSymbol}
        />
      )}

      {/* Indicators + Pivots */}
      {activeIndicators ? (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <div className="lg:col-span-2">
            <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-2">
              Technical Indicators
            </h2>
            <IndicatorPanel indicators={activeIndicators} />
          </div>
          <div>
            <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-2">
              Pivot Levels
            </h2>
            <PivotLevels
              pivots={activeIndicators.pivots}
              spot={activeSnapshot?.ltp ?? 0}
            />
          </div>
        </div>
      ) : (
        <IndicatorPanelSkeleton />
      )}

      {/* Market pulse */}
      <div>
        <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">
          Market Pulse
        </h2>
        <MarketPulse />
      </div>

      {/* Quick signal section */}
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
            AI Signals
          </h2>
          <button
            onClick={() => navigate('/chat')}
            className="text-[10px] text-blue-400 hover:text-blue-300 transition-colors"
          >
            Full Analysis →
          </button>
        </div>

        <div className="flex gap-2">
          <button
            onClick={handleGenerateSignal}
            disabled={signalLoading}
            className="flex-1 flex flex-col items-center py-2.5 px-3 bg-emerald-800 hover:bg-emerald-700 disabled:bg-gray-800 disabled:cursor-not-allowed rounded-lg transition-colors"
          >
            <span className="text-xs font-semibold text-white">
              {signalLoading ? `Analyzing… ${signalElapsed}s` : `Generate ${activeSymbol} Signal`}
            </span>
            <span className="text-[10px] text-emerald-300/60 mt-0.5">Sonnet · ~$0.015</span>
          </button>
        </div>

        {signalError && (
          <p className="text-xs text-red-400 bg-red-900/20 border border-red-800 rounded-lg px-3 py-2">
            {signalError}
          </p>
        )}

        {storeSignals.filter((s) => s.symbol === activeSymbol).slice(0, 1).map((sig) => (
          <SignalCard
            key={sig.id}
            signal={sig}
            growwConnected={growwStatus?.connected}
            onPlaceOrder={setOrderSignal}
          />
        ))}

        {storeSignals.filter((s) => s.symbol === activeSymbol).length === 0 && !signalLoading && (
          <p className="text-xs text-gray-600 text-center py-3">
            No signals yet for {activeSymbol}
          </p>
        )}
      </div>

      <OrderConfirmModal
        open={orderSignal !== null}
        onClose={() => setOrderSignal(null)}
        signal={orderSignal}
        onConfirm={handleConfirmOrder}
        orderPlacementEnabled={growwStatus?.orderPlacementEnabled ?? false}
      />
    </div>
  )
}
