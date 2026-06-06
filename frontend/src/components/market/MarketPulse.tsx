import { useAppStore } from '../../store/appStore'

function FlowBar({ value, max = 2000 }: { value: number; max?: number }) {
  const pct = Math.min(Math.abs(value) / max, 1) * 100
  const isPositive = value >= 0
  return (
    <div className="h-1.5 bg-gray-800 rounded-full overflow-hidden mt-1">
      <div
        className={`h-full rounded-full transition-all duration-500 ${isPositive ? 'bg-emerald-500' : 'bg-red-500'}`}
        style={{ width: `${pct}%` }}
      />
    </div>
  )
}

function PcrGauge({ pcr }: { pcr: number }) {
  // PCR > 1 → bearish (more puts), PCR < 1 → bullish (more calls)
  const color = pcr > 1.2 ? 'text-red-400' : pcr < 0.9 ? 'text-emerald-400' : 'text-yellow-400'
  const label = pcr > 1.2 ? 'Bearish' : pcr < 0.9 ? 'Bullish' : 'Neutral'
  return (
    <span className={`text-xs font-medium ${color}`}>{label}</span>
  )
}

export default function MarketPulse() {
  // Use NIFTY snapshot for market-wide indicators (VIX, PCR, flows)
  const snapshot = useAppStore((s) => s.snapshots['NIFTY'])

  if (!snapshot) {
    return (
      <div className="grid grid-cols-2 lg:grid-cols-3 gap-3">
        {[1, 2, 3].map((i) => (
          <div key={i} className="bg-gray-900 border border-gray-800 rounded-xl p-4 animate-pulse h-20" />
        ))}
      </div>
    )
  }

  const { vix, pcr, fiiFlow, diiFlow } = snapshot

  const cards = [
    {
      label: 'India VIX',
      value: vix.toFixed(2),
      sub: vix > 16 ? 'High volatility' : vix < 12 ? 'Low volatility' : 'Moderate',
      valueColor: vix > 16 ? 'text-red-400' : vix < 12 ? 'text-emerald-400' : 'text-yellow-400',
    },
    {
      label: 'PCR',
      value: pcr.toFixed(2),
      sub: <PcrGauge pcr={pcr} />,
      valueColor: 'text-white',
    },
    {
      label: 'FII / DII Flow',
      value: null,
      sub: null,
      custom: (
        <div className="flex flex-col gap-1 mt-1">
          <div className="flex justify-between items-center">
            <span className="text-xs text-gray-500">FII</span>
            <span className={`text-sm font-semibold tabular-nums ${fiiFlow >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>
              {fiiFlow >= 0 ? '+' : ''}{fiiFlow.toFixed(0)} Cr
            </span>
          </div>
          <FlowBar value={fiiFlow} />
          <div className="flex justify-between items-center mt-1">
            <span className="text-xs text-gray-500">DII</span>
            <span className={`text-sm font-semibold tabular-nums ${diiFlow >= 0 ? 'text-emerald-400' : 'text-red-400'}`}>
              {diiFlow >= 0 ? '+' : ''}{diiFlow.toFixed(0)} Cr
            </span>
          </div>
          <FlowBar value={diiFlow} />
        </div>
      ),
    },
  ]

  return (
    <div className="grid grid-cols-2 lg:grid-cols-3 gap-3">
      {cards.map((card) => (
        <div key={card.label} className="bg-gray-900 border border-gray-800 rounded-xl p-4">
          <span className="text-xs font-medium text-gray-500 uppercase tracking-wider">{card.label}</span>
          {card.custom ?? (
            <>
              <div className={`text-xl font-bold mt-1 ${card.valueColor}`}>{card.value}</div>
              <div className="text-xs mt-0.5">{card.sub}</div>
            </>
          )}
        </div>
      ))}
    </div>
  )
}
