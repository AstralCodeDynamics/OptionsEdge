import { useEffect, useState } from 'react'
import type { GrowwStatus } from '../../services/api'

interface Props {
  open: boolean
  onClose: () => void
  status: GrowwStatus | null
}

function useCountdown(expiresAt?: string | null) {
  const [remaining, setRemaining] = useState('')

  useEffect(() => {
    if (!expiresAt) { setRemaining(''); return }
    function update() {
      const diff = new Date(expiresAt!).getTime() - Date.now()
      if (diff <= 0) { setRemaining('Expired'); return }
      const h = Math.floor(diff / 3_600_000)
      const m = Math.floor((diff % 3_600_000) / 60_000)
      setRemaining(`${h}h ${m}m`)
    }
    update()
    const id = setInterval(update, 60_000)
    return () => clearInterval(id)
  }, [expiresAt])

  return remaining
}

export default function GrowwStatusModal({ open, onClose, status }: Props) {
  const countdown = useCountdown(status?.expiresAt)

  if (!open) return null

  return (
    <>
      <div className="fixed inset-0 bg-black/60 z-50" onClick={onClose} />

      <div className="fixed bottom-0 left-0 right-0 z-50 bg-gray-900 rounded-t-2xl p-5 max-h-[90vh] overflow-y-auto lg:inset-auto lg:top-1/2 lg:left-1/2 lg:-translate-x-1/2 lg:-translate-y-1/2 lg:w-full lg:max-w-md lg:rounded-2xl">
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-white font-semibold text-base">Groww Connection</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-white text-2xl leading-none">×</button>
        </div>

        {!status?.enabled ? (
          <div className="bg-gray-800/60 border border-gray-700/50 rounded-lg px-4 py-3">
            <p className="text-gray-300 text-sm font-semibold">Groww is not enabled</p>
            <p className="text-gray-400 text-xs mt-1">
              Add your API credentials to the server configuration to enable live data.
            </p>
          </div>
        ) : status.connected ? (
          <div className="space-y-3">
            <div className="bg-emerald-900/30 border border-emerald-700/40 rounded-lg px-4 py-3">
              <p className="text-emerald-400 font-semibold text-sm">✓ Groww Connected</p>
              <p className="text-gray-400 text-xs mt-1">
                Live data active. Token refreshes automatically at 6:00 AM IST.
              </p>
              <p className="text-gray-400 text-xs mt-1">Token expires in {countdown || '—'}</p>
            </div>
          </div>
        ) : (
          <div className="space-y-3">
            <div className="bg-red-900/30 border border-red-700/40 rounded-lg px-4 py-3">
              <p className="text-red-400 font-semibold text-sm">✗ Groww Connection Failed</p>
              {status.error && <p className="text-gray-400 text-xs mt-1 break-words">{status.error}</p>}
              <p className="text-gray-400 text-xs mt-2">
                Check that your ApiKey and ApiSecret are correct in the server configuration.
              </p>
            </div>
          </div>
        )}

        <button
          onClick={onClose}
          className="w-full mt-4 bg-gray-800 hover:bg-gray-700 text-white font-semibold rounded-xl py-3 transition-colors"
        >
          Done
        </button>
      </div>
    </>
  )
}
