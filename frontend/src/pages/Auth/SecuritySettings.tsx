import { useEffect, useState } from 'react'
import { QRCodeSVG } from 'qrcode.react'
import { useAppStore } from '../../store/appStore'
import { aiApi, authApi } from '../../services/api'
import { AuthError, AuthSuccess, authInputCls, authButtonCls, extractErrorMessage } from '../../components/common/AuthLayout'
import PasswordStrength, { isPasswordValid } from '../../components/common/PasswordStrength'
import UsageDashboard from '../../components/usage/UsageDashboard'
import type { EnableTwoFactorResponse } from '../../types'

function SectionCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="bg-gray-900 border border-gray-800 rounded-2xl p-5 sm:p-6 space-y-4">
      <h2 className="text-base font-semibold text-white">{title}</h2>
      {children}
    </div>
  )
}

function TwoFactorSection() {
  const user = useAppStore((s) => s.user)
  const setUser = useAppStore((s) => s.setUser)

  const [setup, setSetup] = useState<EnableTwoFactorResponse | null>(null)
  const [setupCode, setSetupCode] = useState('')
  const [recoveryCodes, setRecoveryCodes] = useState<string[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [copied, setCopied] = useState(false)

  const [disablePassword, setDisablePassword] = useState('')
  const [disabling, setDisabling] = useState(false)
  const [showDisableForm, setShowDisableForm] = useState(false)

  const handleEnable = async () => {
    setError(null)
    setLoading(true)
    try {
      const res = await authApi.enableTwoFactor()
      setSetup(res)
    } catch (e: unknown) {
      setError(extractErrorMessage(e, 'Could not start 2FA setup. Try again.'))
    } finally {
      setLoading(false)
    }
  }

  const handleVerifySetup = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      const res = await authApi.verifyTwoFactorSetup(setupCode.trim())
      setRecoveryCodes(res.recoveryCodes)
      if (user) setUser({ ...user, twoFactorEnabled: true })
    } catch (e: unknown) {
      setError(extractErrorMessage(e, 'Invalid verification code.'))
    } finally {
      setLoading(false)
    }
  }

  const handleCopyRecoveryCodes = async () => {
    if (!recoveryCodes) return
    await navigator.clipboard.writeText(recoveryCodes.join('\n'))
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  const handleDone = () => {
    setSetup(null)
    setSetupCode('')
    setRecoveryCodes(null)
  }

  const handleDisable = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setDisabling(true)
    try {
      await authApi.disableTwoFactor(disablePassword)
      if (user) setUser({ ...user, twoFactorEnabled: false })
      setShowDisableForm(false)
      setDisablePassword('')
    } catch (e: unknown) {
      setError(extractErrorMessage(e, 'Could not disable 2FA.'))
    } finally {
      setDisabling(false)
    }
  }

  if (user?.twoFactorEnabled && !recoveryCodes) {
    return (
      <SectionCard title="Two-Factor Authentication">
        <div className="flex items-center gap-2">
          <span className="bg-emerald-500/20 text-emerald-400 text-xs font-medium px-2.5 py-1 rounded-full">
            ● 2FA is active
          </span>
        </div>
        <AuthError message={error} />

        {!showDisableForm ? (
          <button
            onClick={() => setShowDisableForm(true)}
            className="text-sm text-red-400 hover:text-red-300 transition-colors"
          >
            Disable 2FA
          </button>
        ) : (
          <form onSubmit={handleDisable} className="space-y-3">
            <div>
              <label className="block text-xs text-gray-400 mb-1">Confirm your password to disable 2FA</label>
              <input
                type="password"
                required
                value={disablePassword}
                onChange={(e) => setDisablePassword(e.target.value)}
                className={authInputCls}
                placeholder="••••••••"
              />
            </div>
            <div className="flex gap-2">
              <button type="submit" disabled={disabling} className="flex-1 bg-red-500 hover:bg-red-600 disabled:bg-red-500/40 text-white font-semibold rounded-xl py-2.5 min-h-[44px] transition-colors">
                {disabling ? 'Disabling…' : 'Disable 2FA'}
              </button>
              <button
                type="button"
                onClick={() => { setShowDisableForm(false); setDisablePassword(''); setError(null) }}
                className="flex-1 bg-gray-800 hover:bg-gray-700 text-white font-semibold rounded-xl py-2.5 min-h-[44px] transition-colors"
              >
                Cancel
              </button>
            </div>
          </form>
        )}
      </SectionCard>
    )
  }

  if (recoveryCodes) {
    return (
      <SectionCard title="Two-Factor Authentication">
        <AuthSuccess message="2FA enabled! Save these recovery codes somewhere safe — each can be used once if you lose access to your authenticator app." />
        <div className="bg-gray-800 rounded-lg p-3 grid grid-cols-2 gap-2 font-mono text-sm text-gray-200">
          {recoveryCodes.map((c) => <span key={c}>{c}</span>)}
        </div>
        <div className="flex gap-2">
          <button onClick={handleCopyRecoveryCodes} className="flex-1 bg-gray-800 hover:bg-gray-700 text-white font-semibold rounded-xl py-2.5 min-h-[44px] transition-colors">
            {copied ? 'Copied!' : 'Copy all'}
          </button>
          <button onClick={handleDone} className="flex-1 bg-emerald-500 hover:bg-emerald-600 text-gray-950 font-semibold rounded-xl py-2.5 min-h-[44px] transition-colors">
            Done
          </button>
        </div>
      </SectionCard>
    )
  }

  if (setup) {
    return (
      <SectionCard title="Two-Factor Authentication">
        <p className="text-sm text-gray-400">Scan this QR code with your authenticator app, or enter the key manually.</p>
        <div className="flex justify-center bg-white rounded-xl p-4">
          <QRCodeSVG value={setup.authenticatorUri} size={180} />
        </div>
        <div className="bg-gray-800 rounded-lg px-3 py-2.5">
          <p className="text-xs text-gray-400 mb-1">Manual entry key</p>
          <p className="font-mono text-sm text-gray-200 break-all">{setup.sharedKey}</p>
        </div>
        <form onSubmit={handleVerifySetup} className="space-y-3">
          <AuthError message={error} />
          <div>
            <label className="block text-xs text-gray-400 mb-1">Enter the 6-digit code to confirm</label>
            <input
              type="text"
              inputMode="numeric"
              maxLength={6}
              required
              value={setupCode}
              onChange={(e) => setSetupCode(e.target.value.replace(/\D/g, ''))}
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2.5 min-h-[44px] text-white text-center text-lg tracking-[0.4em] font-mono focus:outline-none focus:border-emerald-500"
              placeholder="000000"
            />
          </div>
          <button type="submit" disabled={loading || setupCode.length !== 6} className={authButtonCls}>
            {loading ? 'Verifying…' : 'Verify and enable'}
          </button>
        </form>
      </SectionCard>
    )
  }

  return (
    <SectionCard title="Two-Factor Authentication">
      <p className="text-sm text-gray-400">
        Protect your account with an extra layer of security. Once enabled, you'll need a code from your authenticator app to log in.
      </p>
      <AuthError message={error} />
      <button onClick={handleEnable} disabled={loading} className={authButtonCls}>
        {loading ? 'Starting…' : 'Enable 2FA'}
      </button>
    </SectionCard>
  )
}

function AIConnectionSection() {
  const [aiStatus, setAiStatus] = useState<{ hasKey: boolean; message: string } | null>(null)
  const [aiKey, setAiKey] = useState('')
  const [savingAiKey, setSavingAiKey] = useState(false)
  const [aiKeyError, setAiKeyError] = useState<string | null>(null)
  const [aiKeySuccess, setAiKeySuccess] = useState<string | null>(null)

  useEffect(() => {
    aiApi.getStatus().then(setAiStatus).catch(() => {})
  }, [])

  const handleSaveAiKey = async () => {
    setAiKeyError(null)
    setAiKeySuccess(null)
    setSavingAiKey(true)
    try {
      const res = await aiApi.saveKey(aiKey.trim())
      setAiKeySuccess(res.message)
      setAiKey('')
      const status = await aiApi.getStatus()
      setAiStatus(status)
    } catch (e: unknown) {
      const status = (e as { response?: { status?: number } })?.response?.status
      if (status === 422) {
        setAiKeyError('Invalid API key. Check your key at console.anthropic.com')
      } else {
        setAiKeyError(extractErrorMessage(e, 'Failed to save API key.'))
      }
    } finally {
      setSavingAiKey(false)
    }
  }

  const handleRemoveAiKey = async () => {
    setAiKeyError(null)
    setAiKeySuccess(null)
    try {
      await aiApi.removeKey()
      setAiStatus({ hasKey: false, message: 'No API key set.' })
    } catch {
      setAiKeyError('Failed to disconnect AI key.')
    }
  }

  return (
    <SectionCard title="AI Connection (Anthropic)">
      {aiStatus?.hasKey ? (
        <div className="space-y-3">
          <div className="flex items-center justify-between p-3 rounded-lg bg-emerald-950/40 border border-emerald-800/40">
            <div className="flex items-center gap-2">
              <span className="h-2 w-2 rounded-full bg-emerald-400 animate-pulse" />
              <span className="text-sm text-emerald-400 font-medium">AI Key Connected</span>
            </div>
            <button
              onClick={handleRemoveAiKey}
              className="text-xs text-red-400 hover:text-red-300 underline"
            >
              Disconnect
            </button>
          </div>
          <AuthError message={aiKeyError} />
          <AuthSuccess message={aiKeySuccess} />
        </div>
      ) : (
        <div className="space-y-3">
          <p className="text-xs text-gray-400">
            Connect your own Anthropic API key to enable AI signals and chat. You are billed directly by Anthropic - we never store payment details.
          </p>
          <p className="text-xs text-gray-500">
            Get your free key at{' '}
            <a
              href="https://console.anthropic.com"
              target="_blank"
              rel="noopener noreferrer"
              className="text-teal-400 underline hover:text-teal-300"
            >
              console.anthropic.com
            </a>
          </p>
          <input
            type="password"
            value={aiKey}
            onChange={(e) => setAiKey(e.target.value)}
            placeholder="sk-ant-api03-..."
            className={authInputCls}
          />
          <button
            onClick={handleSaveAiKey}
            disabled={savingAiKey || !aiKey.startsWith('sk-ant-')}
            className={authButtonCls}
          >
            {savingAiKey ? 'Verifying key…' : 'Connect AI Key'}
          </button>
          <AuthError message={aiKeyError} />
          <AuthSuccess message={aiKeySuccess} />
        </div>
      )}
    </SectionCard>
  )
}

function ChangePasswordSection() {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSuccess(null)

    if (newPassword !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }
    if (!isPasswordValid(newPassword)) {
      setError('Password does not meet the requirements below.')
      return
    }

    setSubmitting(true)
    try {
      await authApi.changePassword({ currentPassword, newPassword, confirmPassword })
      setSuccess('Password changed successfully.')
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
    } catch (e: unknown) {
      setError(extractErrorMessage(e, 'Could not change password.'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <SectionCard title="Change Password">
      <form onSubmit={handleSubmit} className="space-y-4">
        <AuthError message={error} />
        <AuthSuccess message={success} />

        <div>
          <label className="block text-xs text-gray-400 mb-1">Current password</label>
          <input
            type="password"
            required
            autoComplete="current-password"
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            className={authInputCls}
            placeholder="••••••••"
          />
        </div>

        <div className="space-y-2">
          <label className="block text-xs text-gray-400 mb-1">New password</label>
          <input
            type="password"
            required
            autoComplete="new-password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            className={authInputCls}
            placeholder="••••••••"
          />
          <PasswordStrength password={newPassword} />
        </div>

        <div>
          <label className="block text-xs text-gray-400 mb-1">Confirm new password</label>
          <input
            type="password"
            required
            autoComplete="new-password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            className={authInputCls}
            placeholder="••••••••"
          />
        </div>

        <button type="submit" disabled={submitting} className={authButtonCls}>
          {submitting ? 'Changing…' : 'Change password'}
        </button>
      </form>
    </SectionCard>
  )
}

function AccountInfoSection() {
  const user = useAppStore((s) => s.user)
  const [createdAt, setCreatedAt] = useState<string | null>(null)

  useEffect(() => {
    authApi.me().then((me) => setCreatedAt(me.createdAt)).catch(() => {})
  }, [])

  if (!user) return null

  const memberSince = createdAt
    ? new Date(createdAt).toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' })
    : '—'

  const rows: Array<[string, string]> = [
    ['Email', user.email],
    ['Display name', user.displayName],
    ['Member since', memberSince],
    ['Plan', user.subscriptionPlan],
  ]

  return (
    <SectionCard title="Account Info">
      <dl className="space-y-2.5">
        {rows.map(([label, value]) => (
          <div key={label} className="flex items-center justify-between text-sm">
            <dt className="text-gray-400">{label}</dt>
            <dd className="text-white font-medium">{value}</dd>
          </div>
        ))}
      </dl>
    </SectionCard>
  )
}

export default function SecuritySettings() {
  return (
    <div className="max-w-2xl mx-auto px-4 py-6 space-y-5">
      <h1 className="text-xl font-bold text-white">Security Settings</h1>
      <UsageDashboard />
      <TwoFactorSection />
      <AIConnectionSection />
      <ChangePasswordSection />
      <AccountInfoSection />
    </div>
  )
}
