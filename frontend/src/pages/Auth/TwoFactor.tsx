import { useEffect, useRef, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import AuthLayout, { AuthError, authButtonCls, authLinkCls, extractErrorMessage } from '../../components/common/AuthLayout'

interface LocationState {
  email?: string
}

export default function TwoFactor() {
  const navigate = useNavigate()
  const location = useLocation()
  const { completeTwoFactor } = useAuth()
  const state = location.state as LocationState | null
  const email = state?.email

  const [code, setCode] = useState('')
  const [recoveryMode, setRecoveryMode] = useState(false)
  const [recoveryCode, setRecoveryCode] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (!email) {
      navigate('/login', { replace: true })
    }
  }, [email, navigate])

  useEffect(() => {
    inputRef.current?.focus()
  }, [recoveryMode])

  const submit = async (value: string) => {
    if (!email || submitting) return
    setError(null)
    setSubmitting(true)
    try {
      await completeTwoFactor(email, value)
      navigate('/', { replace: true })
    } catch (e: unknown) {
      setError(extractErrorMessage(e, 'Invalid verification code.'))
      setCode('')
      setRecoveryCode('')
    } finally {
      setSubmitting(false)
    }
  }

  const handleCodeChange = (value: string) => {
    const digits = value.replace(/\D/g, '').slice(0, 6)
    setCode(digits)
    if (digits.length === 6) {
      submit(digits)
    }
  }

  const handleRecoverySubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (recoveryCode.trim()) submit(recoveryCode.trim())
  }

  if (!email) return null

  return (
    <AuthLayout title="Two-factor verification" subtitle={`Verifying ${email}`}>
      <div className="space-y-4">
        <AuthError message={error} />

        {!recoveryMode ? (
          <>
            <p className="text-sm text-gray-400 text-center">
              Enter the 6-digit code from your authenticator app.
            </p>
            <input
              ref={inputRef}
              type="text"
              inputMode="numeric"
              maxLength={6}
              autoFocus
              value={code}
              onChange={(e) => handleCodeChange(e.target.value)}
              disabled={submitting}
              placeholder="000000"
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-3 min-h-[44px] text-white text-center text-2xl tracking-[0.5em] font-mono focus:outline-none focus:border-emerald-500 disabled:opacity-50"
            />
            {submitting && <p className="text-center text-xs text-gray-500">Verifying…</p>}
            <button
              type="button"
              onClick={() => { setRecoveryMode(true); setError(null); setCode('') }}
              className={`block w-full text-center text-sm ${authLinkCls}`}
            >
              Use a recovery code instead
            </button>
          </>
        ) : (
          <form onSubmit={handleRecoverySubmit} className="space-y-4">
            <p className="text-sm text-gray-400 text-center">
              Enter one of your recovery codes.
            </p>
            <input
              ref={inputRef}
              type="text"
              autoFocus
              value={recoveryCode}
              onChange={(e) => setRecoveryCode(e.target.value)}
              disabled={submitting}
              placeholder="Recovery code"
              className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2.5 min-h-[44px] text-white text-center font-mono focus:outline-none focus:border-emerald-500 disabled:opacity-50"
            />
            <button type="submit" disabled={submitting || !recoveryCode.trim()} className={authButtonCls}>
              {submitting ? 'Verifying…' : 'Verify'}
            </button>
            <button
              type="button"
              onClick={() => { setRecoveryMode(false); setError(null); setRecoveryCode('') }}
              className={`block w-full text-center text-sm ${authLinkCls}`}
            >
              Use authenticator app instead
            </button>
          </form>
        )}

        <p className="text-center text-sm text-gray-400">
          <Link to="/login" className={authLinkCls}>Back to login</Link>
        </p>
      </div>
    </AuthLayout>
  )
}
