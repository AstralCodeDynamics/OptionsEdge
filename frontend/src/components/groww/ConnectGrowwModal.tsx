import { useEffect, useState } from 'react'
import { growwApi, type GrowwStatus } from '../../services/api'

interface Props {
  open: boolean
  onClose: () => void
  status: GrowwStatus | null
  onConnected: (status: GrowwStatus) => void
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

export default function ConnectGrowwModal({ open, onClose, status, onConnected }: Props) {
  const [totp, setTotp] = useState('')
  const [connecting, setConnecting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [imported, setImported] = useState<number | null>(null)
  const countdown = useCountdown(status?.expiresAt)

  useEffect(() => {
    if (open) { setTotp(''); setError(null); setImported(null) }
  }, [open])

  if (!open) return null

  const handleConnect = async (e: React.FormEvent) => {
    e.preventDefault()
    if (totp.trim().length !== 6) { setError('Enter the 6-digit code from your authenticator app'); return }

    setError(null)
    setConnecting(true)
    try {
      const res = await growwApi.connect(totp.trim())
      setImported(res.importedPositions)
      const newStatus = await growwApi.getStatus()
      onConnected(newStatus)
    } catch (e: unknown) {
      const msg =
        (e as { response?: { data?: { error?: string } } })?.response?.data?.error
        ?? 'Failed to connect to Groww. Check your TOTP and try again.'
      setError(msg)
    } finally {
      setConnecting(false)
    }
  }

  const inputCls = 'w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2.5 text-white text-center text-lg tracking-[0.4em] font-mono focus:outline-none focus:border-emerald-500'

  return (
    <>
      <div className="fixed inset-0 bg-black/60 z-50" onClick={onClose} />

      <div className="fixed bottom-0 left-0 right-0 z-50 bg-gray-900 rounded-t-2xl p-5 max-h-[90vh] overflow-y-auto lg:inset-auto lg:top-1/2 lg:left-1/2 lg:-translate-x-1/2 lg:-translate-y-1/2 lg:w-full lg:max-w-md lg:rounded-2xl">
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-white font-semibold text-base">Connect to Groww</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-white text-2xl leading-none">×</button>
        </div>

        {status?.connected ? (
          <div className="space-y-4">
            <div className="bg-emerald-900/30 border border-emerald-700/40 rounded-lg px-4 py-3">
              <p className="text-emerald-400 font-semibold text-sm">✓ Connected to Groww</p>
              <p className="text-gray-400 text-xs mt-1">Token expires in {countdown || '—'} (resets daily at 6:00 AM IST)</p>
            </div>
            {imported !== null && (
              <p className="text-xs text-gray-400">Imported {imported} open position{imported === 1 ? '' : 's'} from your Groww portfolio.</p>
            )}
            <button
              onClick={onClose}
              className="w-full bg-gray-800 hover:bg-gray-700 text-white font-semibold rounded-xl py-3 transition-colors"
            >
              Done
            </button>
          </div>
        ) : (
          <form onSubmit={handleConnect} className="space-y-4">
            <div className="bg-gray-800/60 rounded-lg px-4 py-3 space-y-1.5">
              <p className="text-xs text-gray-300 font-semibold">How to connect</p>
              <ol className="text-xs text-gray-400 space-y-1 list-decimal list-inside">
                <li>Open your authenticator app (e.g. Google Authenticator)</li>
                <li>Find the 6-digit code generated for your Groww account</li>
                <li>Enter it below before it refreshes (codes rotate every 30s)</li>
              </ol>
              <p className="text-[11px] text-gray-500 pt-1">
                The connection is valid until 6:00 AM IST — you'll need to reconnect daily.
              </p>
            </div>

            <div>
              <label className="block text-xs text-gray-400 mb-1">6-digit TOTP code</label>
              <input
                type="text"
                inputMode="numeric"
                maxLength={6}
                value={totp}
                onChange={(e) => setTotp(e.target.value.replace(/\D/g, ''))}
                placeholder="000000"
                className={inputCls}
                autoFocus
              />
            </div>

            {error && (
              <div className="bg-red-900/40 border border-red-700/50 rounded-lg px-3 py-2">
                <p className="text-red-400 text-xs">{error}</p>
              </div>
            )}

            <button
              type="submit"
              disabled={connecting}
              className="w-full bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white font-semibold rounded-xl py-3 transition-colors"
            >
              {connecting ? 'Connecting…' : 'Connect'}
            </button>
          </form>
        )}
      </div>
    </>
  )
}
