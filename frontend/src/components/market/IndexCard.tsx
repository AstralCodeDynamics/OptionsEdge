import type { MarketSnapshot } from '../../types'

interface Props {
  snapshot: MarketSnapshot
}

export default function IndexCard({ snapshot }: Props) {
  const isUp = snapshot.change >= 0
  const changeColor = isUp ? 'text-emerald-400' : 'text-red-400'
  const changeBg = isUp ? 'bg-emerald-400/10' : 'bg-red-400/10'

  return (
    <div className="bg-gray-900 border border-gray-800 rounded-xl p-4 flex flex-col gap-2">
      <div className="flex items-center justify-between">
        <span className="text-xs font-semibold text-gray-400 tracking-wider uppercase">
          {snapshot.symbol}
        </span>
        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${changeBg} ${changeColor}`}>
          VIX {snapshot.vix.toFixed(2)}
        </span>
      </div>

      <div className="flex items-end gap-2">
        <span className="text-2xl lg:text-3xl font-bold text-white tabular-nums">
          {snapshot.ltp.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
        </span>
      </div>

      <div className={`flex items-center gap-2 ${changeColor}`}>
        <span className="text-sm font-semibold tabular-nums">
          {isUp ? '+' : ''}{snapshot.change.toFixed(2)}
        </span>
        <span className="text-sm font-medium tabular-nums">
          ({isUp ? '+' : ''}{snapshot.changePct.toFixed(2)}%)
        </span>
        <span className="text-lg leading-none">{isUp ? '▲' : '▼'}</span>
      </div>

      <div className="flex gap-4 mt-1 text-xs text-gray-500">
        <span>H <span className="text-gray-300">{snapshot.high.toLocaleString('en-IN')}</span></span>
        <span>L <span className="text-gray-300">{snapshot.low.toLocaleString('en-IN')}</span></span>
        <span>O <span className="text-gray-300">{snapshot.open.toLocaleString('en-IN')}</span></span>
      </div>
    </div>
  )
}
