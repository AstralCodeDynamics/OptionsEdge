import { useEffect, useState } from 'react'
import { useAppStore } from '../../store/appStore'
import { usageApi } from '../../services/api'
import { AuthError, extractErrorMessage } from '../common/AuthLayout'
import type { UsageStats } from '../../types'

export default function UsageDashboard() {
  const user = useAppStore((s) => s.user)
  const [stats, setStats] = useState<UsageStats | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    usageApi.getStats()
      .then(setStats)
      .catch((e: unknown) => setError(extractErrorMessage(e, 'Could not load usage stats.')))
  }, [])

  const limitReached = stats !== null && stats.callsToday >= stats.callsLimit

  return (
    <div className="bg-gray-900 border border-gray-800 rounded-2xl p-5 sm:p-6 space-y-4">
      <h2 className="text-base font-semibold text-white">AI Usage</h2>
      <AuthError message={error} />

      {!stats && !error && <p className="text-sm text-gray-400">Loading…</p>}

      {stats && (
        <>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div className="bg-gray-800 rounded-xl px-4 py-3">
              <p className="text-xs text-gray-400">Calls today</p>
              <p className="text-lg font-semibold text-white">{stats.callsToday} / {stats.callsLimit}</p>
            </div>
            <div className="bg-gray-800 rounded-xl px-4 py-3">
              <p className="text-xs text-gray-400">Cost today</p>
              <p className="text-lg font-semibold text-white">${stats.costToday.toFixed(4)}</p>
            </div>
            <div className="bg-gray-800 rounded-xl px-4 py-3">
              <p className="text-xs text-gray-400">Wallet balance</p>
              <p className="text-lg font-semibold text-white">
                ₹{stats.walletBalance.toLocaleString('en-IN', { maximumFractionDigits: 2 })}
              </p>
            </div>
          </div>

          {limitReached && user?.subscriptionPlan === 'free' && (
            <p className="text-sm text-amber-400 bg-amber-500/10 border border-amber-500/20 rounded-lg px-3 py-2.5">
              You've used {stats.callsToday}/{stats.callsLimit} free calls today. Upgrade for more.
            </p>
          )}
        </>
      )}
    </div>
  )
}
