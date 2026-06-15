import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import { authApi } from '../../services/api'
import AuthLayout, { AuthError, AuthSuccess, authInputCls, authButtonCls, authLinkCls, extractErrorMessage } from '../../components/common/AuthLayout'

interface LocationState {
  from?: { pathname: string }
  message?: string
}

export default function Login() {
  const navigate = useNavigate()
  const location = useLocation()
  const { login } = useAuth()
  const state = location.state as LocationState | null

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [unconfirmed, setUnconfirmed] = useState(false)
  const [lockedUntil, setLockedUntil] = useState<string | null>(null)
  const [resendStatus, setResendStatus] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setUnconfirmed(false)
    setLockedUntil(null)
    setResendStatus(null)
    setSubmitting(true)

    try {
      const result = await login(email.trim(), password)
      if (result.requiresTwoFactor) {
        navigate('/two-factor', { state: { email: result.email } })
        return
      }
      navigate(state?.from?.pathname ?? '/', { replace: true })
    } catch (e: unknown) {
      const status = (e as { response?: { status?: number } })?.response?.status
      if (status === 403) {
        setUnconfirmed(true)
      } else if (status === 423) {
        setLockedUntil('Account locked due to too many failed attempts. Try again later.')
      } else {
        setError(extractErrorMessage(e, 'Invalid email or password.'))
      }
    } finally {
      setSubmitting(false)
    }
  }

  const handleResend = async () => {
    setResendStatus(null)
    try {
      const res = await authApi.resendConfirmation(email.trim())
      setResendStatus(res.message)
    } catch {
      setResendStatus('Could not resend confirmation email. Try again shortly.')
    }
  }

  return (
    <AuthLayout title="Log in to your account">
      <form onSubmit={handleSubmit} className="space-y-4">
        {state?.message && <AuthSuccess message={state.message} />}
        <AuthError message={lockedUntil} />
        <AuthError message={error} />

        {unconfirmed && (
          <div className="rounded-lg bg-yellow-950/40 border border-yellow-700/40 p-3 text-xs text-yellow-300 space-y-2">
            <p className="font-medium">
              ⚠️ Email verification required
            </p>
            <p>Please check your inbox and click the confirmation link before logging in.</p>
            <p>Don't see it? Check your spam folder, or:</p>
            <button
              type="button"
              onClick={handleResend}
              className="underline text-yellow-400 hover:text-yellow-300"
            >
              Resend confirmation email
            </button>
            {resendStatus && (
              <p className="text-emerald-400">{resendStatus}</p>
            )}
          </div>
        )}

        <div>
          <label className="block text-xs text-gray-400 mb-1">Email</label>
          <input
            type="email"
            required
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className={authInputCls}
            placeholder="you@example.com"
          />
        </div>

        <div>
          <div className="flex items-center justify-between mb-1">
            <label className="block text-xs text-gray-400">Password</label>
            <Link to="/forgot-password" className={`text-xs ${authLinkCls}`}>Forgot password?</Link>
          </div>
          <input
            type="password"
            required
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className={authInputCls}
            placeholder="••••••••"
          />
        </div>

        <button type="submit" disabled={submitting} className={authButtonCls}>
          {submitting ? 'Logging in…' : 'Log in'}
        </button>

        <p className="text-center text-sm text-gray-400">
          Don't have an account?{' '}
          <Link to="/register" className={authLinkCls}>Create account</Link>
        </p>
      </form>
    </AuthLayout>
  )
}
