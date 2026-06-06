import { useState, useEffect } from 'react'
import { optionsApi } from '../../services/api'
import { useAppStore } from '../../store/appStore'
import { ChainTable } from '../../components/chain/ChainTable'
import { OIChart } from '../../components/charts/OIChart'
import type { OptionsChain } from '../../types'

const SYMBOLS = ['NIFTY', 'BANKNIFTY'] as const
type Symbol = (typeof SYMBOLS)[number]

export default function Chain() {
  const snapshots = useAppStore((s) => s.snapshots)

  const [symbol, setSymbol] = useState<Symbol>('NIFTY')
  const [expiries, setExpiries] = useState<string[]>([])
  const [selectedExpiry, setSelectedExpiry] = useState<string>('')
  const [chain, setChain] = useState<OptionsChain | null>(null)
  const [loading, setLoading] = useState(false)

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
    setLoading(true)
    optionsApi.getChain(symbol, selectedExpiry)
      .then((data) => { setChain(data); setLoading(false) })
      .catch(() => setLoading(false))
  }, [symbol, selectedExpiry])

  const spot = snapshots[symbol]?.ltp ?? chain?.spot ?? 0
  const atm = chain ? Math.round(spot / (symbol === 'NIFTY' ? 50 : 100)) * (symbol === 'NIFTY' ? 50 : 100) : 0

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
              className={`px-3 py-1 rounded-full text-xs font-semibold transition-colors ${
                symbol === sym
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
              }`}
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
            className="bg-gray-800 border border-gray-600 text-white text-xs rounded px-2 py-1"
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
              PCR: <span className={`font-semibold ${chain.pcr > 1 ? 'text-green-400' : 'text-red-400'}`}>
                {chain.pcr.toFixed(2)}
              </span>
            </span>
            <span className="text-gray-400">
              MaxPain: <span className="text-blue-400 font-semibold">{chain.maxPain}</span>
            </span>
          </div>
        )}
      </div>

      {loading && (
        <div className="bg-gray-900 border border-gray-800 rounded-xl p-6 animate-pulse h-40" />
      )}

      {!loading && chain && (
        <>
          {/* OI chart */}
          <OIChart rows={chain.rows} atm={atm} />

          {/* Chain table */}
          <ChainTable rows={chain.rows} spot={spot} maxPain={chain.maxPain} />
        </>
      )}
    </div>
  )
}
