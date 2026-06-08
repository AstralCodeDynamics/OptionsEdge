import { useEffect, useState } from 'react'
import { growwApi } from '../../services/api'
import type { GrowwStatus } from '../../services/api'
import { AuthError, AuthSuccess, authInputCls, authButtonCls, extractErrorMessage } from '../common/AuthLayout'

interface Props {
  open: boolean
  onClose: () => void
  status: GrowwStatus | null
  onStatusChange: (status: GrowwStatus) => void
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

export default function GrowwStatusModal({ open, onClose, status, onStatusChange }: Props) {
  const countdown = useCountdown(status?.expiresAt)

  const [apiKey, setApiKey] = useState('')
  const [apiSecret, setApiSecret] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [removing, setRemoving] = useState(false)
  const [confirmDisconnect, setConfirmDisconnect] = useState(false)

  useEffect(() => {
    if (open) {
      setError(null)
      setSuccess(null)
      setApiKey('')
      setApiSecret('')
      setConfirmDisconnect(false)
    }
  }, [open])

  if (!open) return null

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSuccess(null)
    setSaving(true)
    try {
      const res = await growwApi.saveCredentials(apiKey.trim(), apiSecret.trim())
      setSuccess(res.message)
      setApiKey('')
      setApiSecret('')
      const fresh = await growwApi.getStatus()
      onStatusChange(fresh)
    } catch (e: unknown) {
      setError(extractErrorMessage(e, 'Could not connect to Groww. Check your TOTP Token and Secret.'))
    } finally {
      setSaving(false)
    }
  }

  const handleDisconnect = async () => {
    setError(null)
    setSuccess(null)
    setRemoving(true)
    try {
      await growwApi.removeCredentials()
      setConfirmDisconnect(false)
      const fresh = await growwApi.getStatus()
      onStatusChange(fresh)
    } catch (e: unknown) {
      setError(extractErrorMessage(e, 'Could not disconnect Groww. Try again.'))
    } finally {
      setRemoving(false)
    }
  }

  return (
    <>
      <div className="fixed inset-0 bg-black/60 z-50" onClick={onClose} />

      <div className="fixed bottom-0 left-0 right-0 z-50 bg-gray-900 rounded-t-2xl p-5 max-h-[90vh] overflow-y-auto lg:inset-auto lg:top-1/2 lg:left-1/2 lg:-translate-x-1/2 lg:-translate-y-1/2 lg:w-full lg:max-w-md lg:rounded-2xl">
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-white font-semibold text-base">Connect Your Groww Account</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-white text-2xl leading-none">×</button>
        </div>

        {!status?.enabled ? (
          <div className="bg-gray-800/60 border border-gray-700/50 rounded-lg px-4 py-3">
            <p className="text-gray-300 text-sm font-semibold">Groww integration is not enabled</p>
            <p className="text-gray-400 text-xs mt-1">
              Ask the administrator to enable Groww in the server configuration.
            </p>
          </div>
        ) : status.connected ? (
          <div className="space-y-4">
            <div className="bg-emerald-900/30 border border-emerald-700/40 rounded-lg px-4 py-3">
              <p className="text-emerald-400 font-semibold text-sm">✓ Groww Connected</p>
              <p className="text-gray-400 text-xs mt-1">
                Credentials saved — auto-reconnects daily at 6:00 AM IST.
              </p>
              <p className="text-gray-400 text-xs mt-1">Token expires in {countdown || '—'}</p>
            </div>

            <AuthError message={error} />

            {!confirmDisconnect ? (
              <button
                onClick={() => setConfirmDisconnect(true)}
                className="text-sm text-red-400 hover:text-red-300 transition-colors"
              >
                Disconnect Groww
              </button>
            ) : (
              <div className="bg-red-900/20 border border-red-700/30 rounded-lg px-4 py-3 space-y-3">
                <p className="text-gray-300 text-sm">
                  This removes your saved credentials. You can reconnect any time.
                </p>
                <div className="flex gap-2">
                  <button
                    onClick={handleDisconnect}
                    disabled={removing}
                    className="flex-1 bg-red-500/20 hover:bg-red-500/30 disabled:opacity-50 text-red-400 font-semibold rounded-lg py-2.5 text-sm transition-colors"
                  >
                    {removing ? 'Disconnecting…' : 'Yes, disconnect'}
                  </button>
                  <button
                    onClick={() => setConfirmDisconnect(false)}
                    className="flex-1 bg-gray-800 hover:bg-gray-700 text-white font-semibold rounded-lg py-2.5 text-sm transition-colors"
                  >
                    Cancel
                  </button>
                </div>
              </div>
            )}
          </div>
        ) : (
          <div className="space-y-4">
            {status.hasCredentials && (
              <div className="bg-red-900/30 border border-red-700/40 rounded-lg px-4 py-3">
                <p className="text-red-400 font-semibold text-sm">✗ Groww Connection Failed</p>
                {status.error && <p className="text-gray-400 text-xs mt-1 break-words">{status.error}</p>}
                <p className="text-gray-400 text-xs mt-2">
                  Re-enter your TOTP Token and Secret below to reconnect.
                </p>
              </div>
            )}

            <div className="bg-gray-800/60 border border-gray-700/50 rounded-lg px-4 py-3 space-y-1.5">
              <p className="text-gray-300 text-xs font-semibold">How to get your TOTP credentials</p>
              <ol className="text-gray-400 text-xs list-decimal list-inside space-y-1">
                <li>Go to <span className="text-gray-300">groww.in/trade-api/api-keys</span> and sign in</li>
                <li>Generate a new API key — choose the TOTP authentication type</li>
                <li>Copy the <span className="text-gray-300">TOTP Token</span> and <span className="text-gray-300">TOTP Secret</span> shown</li>
              </ol>
              <p className="text-amber-400 text-xs mt-1">
                ⚠ Groww shows these only once — copy them somewhere safe before closing that page.
              </p>
            </div>

            <form onSubmit={handleSave} className="space-y-3">
              <div>
                <label className="block text-xs text-gray-400 mb-1">TOTP Token</label>
                <input
                  type="password"
                  required
                  value={apiKey}
                  onChange={(e) => setApiKey(e.target.value)}
                  className={authInputCls}
                  placeholder="Paste your TOTP Token from Groww"
                  autoComplete="off"
                />
                <p className="text-gray-500 text-xs mt-1">From groww.in/trade-api/api-keys — labeled "API Key" / "TOTP Token"</p>
              </div>

              <div>
                <label className="block text-xs text-gray-400 mb-1">TOTP Secret</label>
                <input
                  type="password"
                  required
                  value={apiSecret}
                  onChange={(e) => setApiSecret(e.target.value)}
                  className={authInputCls}
                  placeholder="Paste your TOTP Secret from Groww"
                  autoComplete="off"
                />
                <p className="text-gray-500 text-xs mt-1">From the same page — labeled "API Secret" / "TOTP Secret"</p>
              </div>

              <AuthError message={error} />
              <AuthSuccess message={success} />

              <button type="submit" disabled={saving} className={authButtonCls}>
                {saving ? 'Connecting…' : 'Connect Groww'}
              </button>
            </form>

            <p className="text-gray-500 text-xs">
              🔒 Your credentials are encrypted and stored securely. They are never shared with anyone.
            </p>
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
