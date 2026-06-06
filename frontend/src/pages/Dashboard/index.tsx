import { useState } from 'react'
import { useMarketData } from '../../hooks/useMarketData'
import { useAppStore } from '../../store/appStore'
import { marketApi } from '../../services/api'
import IndexCard from '../../components/market/IndexCard'
import MarketStatusBanner from '../../components/market/MarketStatusBanner'
import MarketPulse from '../../components/market/MarketPulse'
import { IndicatorPanel } from '../../components/indicators/IndicatorPanel'
import { PivotLevels } from '../../components/market/PivotLevels'
import { PriceChart } from '../../components/charts/PriceChart'
import type { Candle } from '../../types'
import { useEffect } from 'react'

const SYMBOLS = ['NIFTY', 'BANKNIFTY'] as const
type Symbol = (typeof SYMBOLS)[number]

export default function Dashboard() {
  const { connectionState } = useMarketData()
  const snapshots = useAppStore((s) => s.snapshots)
  const indicators = useAppStore((s) => s.indicators)

  const [activeSymbol, setActiveSymbol] = useState<Symbol>('NIFTY')
  const [candles, setCandles] = useState<Candle[]>([])

  useEffect(() => {
    marketApi.getCandles(activeSymbol)
      .then(setCandles)
      .catch(() => {})
  }, [activeSymbol])

  const nifty = snapshots['NIFTY']
  const bankNifty = snapshots['BANKNIFTY']
  const activeSnapshot = snapshots[activeSymbol]
  const activeIndicators = indicators[activeSymbol]

  return (
    <div className="p-4 space-y-5 max-w-5xl mx-auto">
      <MarketStatusBanner connectionState={connectionState} />

      {/* Index cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {nifty ? (
          <IndexCard snapshot={nifty} />
        ) : (
          <div className="bg-gray-900 border border-gray-800 rounded-xl p-4 animate-pulse h-32" />
        )}
        {bankNifty ? (
          <IndexCard snapshot={bankNifty} />
        ) : (
          <div className="bg-gray-900 border border-gray-800 rounded-xl p-4 animate-pulse h-32" />
        )}
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
        <div className="bg-gray-900 border border-gray-800 rounded-xl p-6 animate-pulse h-40" />
      )}

      {/* Market pulse */}
      <div>
        <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3">
          Market Pulse
        </h2>
        <MarketPulse />
      </div>
    </div>
  )
}
