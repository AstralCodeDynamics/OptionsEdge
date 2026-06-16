import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useLotSizes } from '../../hooks/useLotSizes'
import { optionsApi, positionsApi } from '../../services/api'
import { useAppStore } from '../../store/appStore'
import { PayoffDiagram } from '../charts/PayoffDiagram'
import type { OptionsChain, PayoffResult, StrategyLeg } from '../../types'

interface Props {
  chain: OptionsChain
  symbol: string
}

const fmtMoney = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 0 })

let legSeq = 0
const nextLegId = () => ++legSeq

interface BuilderLeg extends StrategyLeg {
  id: number
}

function lookupChainLeg(chain: OptionsChain, strike: number, optionType: 'CE' | 'PE') {
  const row = chain.rows.find((r) => r.strike === strike)
  return row ? (optionType === 'CE' ? row.ce : row.pe) : null
}

function makeLeg(chain: OptionsChain, symbol: string): BuilderLeg {
  const row = chain.rows.find((r) => r.isAtm) ?? chain.rows[Math.floor(chain.rows.length / 2)]
  const ce = row?.ce ?? null
  return {
    id: nextLegId(),
    symbol,
    strike: row?.strike ?? 0,
    optionType: 'CE',
    action: 'BUY',
    lots: 1,
    premium: ce ? Math.round(ce.ltp * 100) / 100 : 0,
    delta: ce?.delta,
    gamma: ce?.gamma,
    theta: ce?.theta,
    vega: ce?.vega,
  }
}

export function StrategyBuilder({ chain, symbol }: Props) {
  const navigate = useNavigate()
  const upsertPosition = useAppStore((s) => s.upsertPosition)
  const { getLotSize, loading: lotSizesLoading, error: lotSizesError } = useLotSizes()
  const lotSize = getLotSize(symbol) ?? 0

  const [legs, setLegs] = useState<BuilderLeg[]>(() => [makeLeg(chain, symbol)])
  const [payoff, setPayoff] = useState<PayoffResult | null>(null)
  const [loadingPayoff, setLoadingPayoff] = useState(false)
  const [tracking, setTracking] = useState(false)
  const [trackError, setTrackError] = useState<string | null>(null)
  const [trackedOk, setTrackedOk] = useState(false)

  const strikes = useMemo(() => chain.rows.map((r) => r.strike), [chain.rows])

  // Debounced live payoff recompute whenever the leg set changes
  useEffect(() => {
    if (legs.length === 0) {
      setPayoff(null)
      return
    }
    let cancelled = false
    setLoadingPayoff(true)
    const handle = setTimeout(() => {
      optionsApi.computePayoff(legs)
        .then((res) => { if (!cancelled) setPayoff(res) })
        .catch(() => { if (!cancelled) setPayoff(null) })
        .finally(() => { if (!cancelled) setLoadingPayoff(false) })
    }, 300)
    return () => { cancelled = true; clearTimeout(handle) }
  }, [legs])

  function addLeg() {
    setLegs((ls) => [...ls, makeLeg(chain, symbol)])
    setTrackedOk(false)
  }

  function removeLeg(id: number) {
    setLegs((ls) => ls.filter((l) => l.id !== id))
    setTrackedOk(false)
  }

  function updateLeg(id: number, patch: Partial<StrategyLeg>) {
    setLegs((ls) => ls.map((l) => {
      if (l.id !== id) return l
      const next = { ...l, ...patch }
      if (patch.strike !== undefined || patch.optionType !== undefined) {
        const found = lookupChainLeg(chain, next.strike, next.optionType)
        if (found) {
          next.premium = Math.round(found.ltp * 100) / 100
          next.delta = found.delta
          next.gamma = found.gamma
          next.theta = found.theta
          next.vega = found.vega
        }
      }
      return next
    }))
    setTrackedOk(false)
  }

  // Net Greeks weighted by direction (BUY = +1, SELL = -1) and contract size
  const netGreeks = useMemo(() => legs.reduce((acc, l) => {
    const qty = (l.action === 'BUY' ? 1 : -1) * l.lots * lotSize
    acc.delta += (l.delta ?? 0) * qty
    acc.gamma += (l.gamma ?? 0) * qty
    acc.theta += (l.theta ?? 0) * qty
    acc.vega += (l.vega ?? 0) * qty
    return acc
  }, { delta: 0, gamma: 0, theta: 0, vega: 0 }), [legs, lotSize])

  function handleAnalyzeWithAI() {
    const legsDescription = legs
      .map((l) => `${l.action} ${l.lots} lot(s) ${symbol} ${l.strike} ${l.optionType} @ ₹${l.premium}`)
      .join('; ')
    const payoffSummary = payoff
      ? ` Max profit: ${payoff.maxProfitUnlimited ? 'unlimited' : `₹${fmtMoney(payoff.maxProfit ?? 0)}`}, ` +
        `max loss: ${payoff.maxLossUnlimited ? 'unlimited' : `₹${fmtMoney(payoff.maxLoss ?? 0)}`}, ` +
        `breakeven(s): ${payoff.breakevens.length > 0 ? payoff.breakevens.map(fmtMoney).join(', ') : 'none'}.`
      : ''
    const prefillMessage =
      `Can you review this options strategy on ${symbol}? Legs: ${legsDescription}.${payoffSummary} ` +
      `What's your view on the risk/reward, and when should I look to adjust or exit it?`
    navigate('/chat', { state: { prefillMessage } })
  }

  async function handleTrackThis() {
    setTrackError(null)
    setTrackedOk(false)
    setTracking(true)
    try {
      for (const leg of legs) {
        const created = await positionsApi.create({
          symbol: leg.symbol,
          strike: leg.strike,
          optionType: leg.optionType,
          expiry: chain.expiry,
          entryPrice: leg.premium,
          quantity: leg.lots,
          stopLoss: Math.round(leg.premium * 0.5 * 100) / 100,
          target1: Math.round(leg.premium * 1.5 * 100) / 100,
        })
        upsertPosition(created)
      }
      setTrackedOk(true)
    } catch {
      setTrackError('Failed to add one or more legs to the Position Tracker. Please try again.')
    } finally {
      setTracking(false)
    }
  }

  return (
    <div className="bg-gray-900 border border-gray-800 rounded-xl p-4 space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-2">
        <h2 className="text-sm font-semibold text-white">Strategy Builder</h2>
        <span className="text-[11px] text-gray-500">
          {lotSize > 0 ? `Lot size ${lotSize}` : lotSizesLoading ? 'Loading lot size...' : 'Lot size unavailable'}
        </span>
        <button
          onClick={addLeg}
          className="text-xs px-3 py-1.5 rounded-lg bg-gray-800 hover:bg-gray-700 text-gray-200 font-medium transition-colors"
        >
          + Add Leg
        </button>
      </div>

      <div className="space-y-2">
        {legs.map((leg) => (
          <div key={leg.id} className="flex flex-wrap items-center gap-2 bg-gray-800/60 border border-gray-700 rounded-lg p-2.5">
            <div className="flex rounded-lg overflow-hidden border border-gray-700">
              {(['CE', 'PE'] as const).map((t) => (
                <button
                  key={t}
                  onClick={() => updateLeg(leg.id, { optionType: t })}
                  className={`px-2.5 py-1 text-xs font-semibold transition-colors ${
                    leg.optionType === t ? 'bg-blue-600 text-white' : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
                  }`}
                >
                  {t}
                </button>
              ))}
            </div>

            <select
              value={leg.strike}
              onChange={(e) => updateLeg(leg.id, { strike: Number(e.target.value) })}
              className="bg-gray-800 border border-gray-700 rounded-lg px-2 py-1 text-xs text-white"
            >
              {strikes.map((s) => <option key={s} value={s}>{s}</option>)}
            </select>

            <div className="flex rounded-lg overflow-hidden border border-gray-700">
              {(['BUY', 'SELL'] as const).map((a) => (
                <button
                  key={a}
                  onClick={() => updateLeg(leg.id, { action: a })}
                  className={`px-2.5 py-1 text-xs font-semibold transition-colors ${
                    leg.action === a
                      ? a === 'BUY' ? 'bg-emerald-600 text-white' : 'bg-red-600 text-white'
                      : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
                  }`}
                >
                  {a}
                </button>
              ))}
            </div>

            <label className="flex items-center gap-1 text-[10px] text-gray-500">
              Lots
              <input
                type="number"
                min={1}
                value={leg.lots}
                onChange={(e) => updateLeg(leg.id, { lots: Math.max(1, Number(e.target.value) || 1) })}
                className="w-14 bg-gray-800 border border-gray-700 rounded-lg px-2 py-1 text-xs text-white"
              />
            </label>

            <label className="flex items-center gap-1 text-[10px] text-gray-500">
              Premium ₹
              <input
                type="number"
                min={0}
                step="0.05"
                value={leg.premium}
                onChange={(e) => updateLeg(leg.id, { premium: Math.max(0, Number(e.target.value) || 0) })}
                className="w-20 bg-gray-800 border border-gray-700 rounded-lg px-2 py-1 text-xs text-white"
              />
            </label>

            <button
              onClick={() => removeLeg(leg.id)}
              disabled={legs.length === 1}
              title="Remove leg"
              className="ml-auto text-gray-500 hover:text-red-400 disabled:opacity-30 disabled:cursor-not-allowed text-lg leading-none px-1"
            >
              ×
            </button>
          </div>
        ))}
      </div>

      {loadingPayoff && !payoff && (
        <div className="bg-gray-900 border border-gray-700 rounded-lg p-6 animate-pulse h-60" />
      )}
      {payoff && <PayoffDiagram curve={payoff.payoffCurve} spot={chain.spot} />}

      {payoff && (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <div className="bg-gray-800/60 border border-gray-700 rounded-lg p-2.5">
            <p className="text-[10px] text-gray-500 uppercase tracking-wide mb-0.5">Max Profit</p>
            <p className="text-sm font-bold text-emerald-400">
              {payoff.maxProfitUnlimited ? 'Unlimited' : `₹${fmtMoney(payoff.maxProfit ?? 0)}`}
            </p>
          </div>
          <div className="bg-gray-800/60 border border-gray-700 rounded-lg p-2.5">
            <p className="text-[10px] text-gray-500 uppercase tracking-wide mb-0.5">Max Loss</p>
            <p className="text-sm font-bold text-red-400">
              {payoff.maxLossUnlimited ? 'Unlimited' : `₹${fmtMoney(payoff.maxLoss ?? 0)}`}
            </p>
          </div>
          <div className="bg-gray-800/60 border border-gray-700 rounded-lg p-2.5 col-span-2 sm:col-span-1">
            <p className="text-[10px] text-gray-500 uppercase tracking-wide mb-0.5">Breakeven</p>
            <p className="text-sm font-bold text-white">
              {payoff.breakevens.length > 0 ? payoff.breakevens.map(fmtMoney).join(', ') : '—'}
            </p>
          </div>
          <div className="bg-gray-800/60 border border-gray-700 rounded-lg p-2.5">
            <p className="text-[10px] text-gray-500 uppercase tracking-wide mb-0.5">Net Greeks (Δ/Γ/Θ/V)</p>
            <p className="text-xs font-semibold text-gray-200">
              {lotSize > 0
                ? `${netGreeks.delta.toFixed(2)} / ${netGreeks.gamma.toFixed(4)} / ${netGreeks.theta.toFixed(2)} / ${netGreeks.vega.toFixed(2)}`
                : 'Loading...'}
            </p>
          </div>
        </div>
      )}

      {lotSizesError && (
        <p className="text-xs text-red-400 bg-red-900/20 border border-red-800 rounded-lg px-3 py-2">{lotSizesError}</p>
      )}

      <div className="flex flex-wrap items-center gap-3">
        <button
          onClick={handleAnalyzeWithAI}
          className="text-xs font-semibold px-4 py-2 min-h-[44px] rounded-lg bg-blue-600 hover:bg-blue-500 text-white transition-colors"
        >
          Analyze with AI
        </button>
        <button
          onClick={handleTrackThis}
          disabled={tracking}
          className="text-xs font-semibold px-4 py-2 min-h-[44px] rounded-lg bg-emerald-700 hover:bg-emerald-600 disabled:bg-gray-800 disabled:cursor-not-allowed text-white transition-colors"
        >
          {tracking ? 'Adding…' : 'Track This'}
        </button>
        {trackedOk && <span className="text-xs text-emerald-400">All legs added to Position Tracker</span>}
      </div>
      {trackError && (
        <p className="text-xs text-red-400 bg-red-900/20 border border-red-800 rounded-lg px-3 py-2">{trackError}</p>
      )}
    </div>
  )
}
