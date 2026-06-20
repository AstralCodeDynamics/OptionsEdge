import { useState, useEffect } from 'react'
import { optionsApi } from '../../services/api'
import { useAppStore } from '../../store/appStore'
import { ChainTable } from '../../components/chain/ChainTable'
import { OIChart } from '../../components/charts/OIChart'
import { StrategyBuilder } from '../../components/strategy/StrategyBuilder'
import { ChainTableSkeleton } from '../../components/common/Skeleton'
import GrowwDataBlocked from '../../components/groww/GrowwDataBlocked'
import type { OptionsChain } from '../../types'

const SYMBOLS = ['NIFTY', 'BANKNIFTY'] as const
type Symbol = (typeof SYMBOLS)[number]

export default function Chain() {
  const growwStatus = useAppStore((s) => s.growwStatus)
  const marketDataConnected = useAppStore((s) => s.marketDataConnected)
  const setMarketDataConnected = useAppStore((s) => s.setMarketDataConnected)
  const [symbol, setSymbol] = useState<Symbol>('NIFTY')
  const [expiries, setExpiries] = useState<string[]>([])
  const [selectedExpiry, setSelectedExpiry] = useState<string>('')
  const [chain, setChain] = useState<OptionsChain | null>(null)
  const [loading, setLoading] = useState(false)
  const [isGrowwConnected, setIsGrowwConnected] = useState<boolean | null>(null)

  // Fetch expiries when symbol changes
  useEffect(() => {
    optionsApi.getExpiries(symbol)
      .then((data) => {
        setExpiries(data)
        setSelectedExpiry(data[0] ?? '')
      })
      .catch(() => {})
  }, [symbol])

  // Fetch chain when symbol or expiry changes
  useEffect(() => {
    if (!selectedExpiry) return
    let cancelled = false
    setLoading(true)
    setChain(null)
    optionsApi.getChain(symbol, selectedExpiry)
      .then((response) => {
        if (cancelled) return
        setIsGrowwConnected(response.isGrowwConnected)
        setMarketDataConnected(response.isGrowwConnected)
        setChain(response.isGrowwConnected ? response.data : null)
      })
      .catch(() => {
        if (!cancelled) setChain(null)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => { cancelled = true }
  }, [growwStatus?.connected, selectedExpiry, setMarketDataConnected, symbol])

  const spot = chain?.spot ?? 0
  const atm = chain ? Math.round(spot / (symbol === 'NIFTY' ? 50 : 100)) * (symbol === 'NIFTY' ? 50 : 100) : 0
  const isBlocked = isGrowwConnected === false
    || marketDataConnected === false
    || (growwStatus?.enabled === true && !growwStatus.connected)
  const controlsEnabled = isGrowwConnected === true && !isBlocked

  return (
    <div className="p-4 space-y-4 max-w-6xl mx-auto">
      <div className="flex flex-wrap items-center gap-3">
        <h1 className="text-lg font-bold text-white">Options Chain</h1>

        {/* Symbol selector */}
        <div className="flex gap-2">
          {SYMBOLS.map((sym) => (
            <button
              key={sym}
              onClick={() => setSymbol(sym)}
              disabled={!controlsEnabled}
              className={`px-3 py-1 rounded-full text-xs font-semibold transition-colors ${
                symbol === sym
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
              } disabled:cursor-not-allowed disabled:opacity-40`}
            >
              {sym}
            </button>
          ))}
        </div>

        {/* Expiry selector */}
        {expiries.length > 0 && (
          <select
            value={selectedExpiry}
            onChange={(e) => setSelectedExpiry(e.target.value)}
            disabled={!controlsEnabled}
            className="bg-gray-800 border border-gray-600 text-white text-xs rounded px-2 py-1 disabled:cursor-not-allowed disabled:opacity-40"
          >
            {expiries.map((exp) => (
              <option key={exp} value={exp}>{exp}</option>
            ))}
          </select>
        )}

        {/* Key metrics */}
        {chain && (
          <div className="flex gap-4 ml-auto text-xs">
            <span className="text-gray-400">
              Spot: <span className="text-white font-semibold">
                {chain.spot.toLocaleString('en-IN', { maximumFractionDigits: 2 })}
              </span>
            </span>
            <span className="text-gray-400">
              PCR: <span className={`font-semibold ${chain.pcr > 1.2 ? 'text-red-400' : chain.pcr < 0.9 ? 'text-green-400' : 'text-yellow-400'}`}>
                {chain.pcr.toFixed(2)}
              </span>
            </span>
            <span className="text-gray-400">
              MaxPain: <span className="text-blue-400 font-semibold">{chain.maxPain}</span>
            </span>
          </div>
        )}
      </div>

      {loading && !isBlocked && <ChainTableSkeleton />}

      {isBlocked && <GrowwDataBlocked />}

      {!loading && controlsEnabled && chain && (
        <>
          {/* OI chart */}
          <OIChart rows={chain.rows} atm={atm} />

          {/* Chain table */}
          <ChainTable rows={chain.rows} spot={spot} maxPain={chain.maxPain} />

          {/* Strategy builder */}
          <StrategyBuilder chain={chain} symbol={symbol} />
        </>
      )}
    </div>
  )
}
