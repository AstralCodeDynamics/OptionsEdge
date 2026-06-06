import { useMarketData } from '../../hooks/useMarketData'
import { useAppStore } from '../../store/appStore'
import IndexCard from '../../components/market/IndexCard'
import MarketStatusBanner from '../../components/market/MarketStatusBanner'
import MarketPulse from '../../components/market/MarketPulse'

export default function Dashboard() {
  const { connectionState } = useMarketData()
  const snapshots = useAppStore((s) => s.snapshots)

  const nifty = snapshots['NIFTY']
  const bankNifty = snapshots['BANKNIFTY']

  return (
    <div className="p-4 space-y-5 max-w-5xl mx-auto">
      {/* Status bar */}
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
