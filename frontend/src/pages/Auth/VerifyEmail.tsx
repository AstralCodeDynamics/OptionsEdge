import { useEffect, useRef, useState } from 'react'
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom'
import { authApi } from '../../services/api'
import AuthLayout, { AuthError, AuthSuccess, authButtonCls, authLinkCls, extractErrorMessage } from '../../components/common/AuthLayout'

interface LocationState {
  email?: string
}

type ConfirmState = 'idle' | 'confirming' | 'confirmed' | 'failed'

export default function VerifyEmail() {
  const location = useLocation()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const state = location.state as LocationState | null
  const email = state?.email

  const userId = searchParams.get('userId')
  const token = searchParams.get('token')

  const [confirmState, setConfirmState] = useState<ConfirmState>('idle')
  const [confirmError, setConfirmError] = useState<string | null>(null)
  const [resendStatus, setResendStatus] = useState<string | null>(null)
  const [resending, setResending] = useState(false)
  const [countdown, setCountdown] = useState(5)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    if (!userId || !token) return
    setConfirmState('confirming')
    authApi.confirmEmail(userId, token)
      .then(() => setConfirmState('confirmed'))
      .catch((e) => {
        setConfirmError(extractErrorMessage(e, 'Invalid or expired confirmation link.'))
        setConfirmState('failed')
      })
  }, [userId, token])

  useEffect(() => {
    if (confirmState !== 'confirmed') return

    const startDelay = setTimeout(() => {
      timerRef.current = setInterval(() => {
        setCountdown((prev) => {
          if (prev <= 1) {
            if (timerRef.current) clearInterval(timerRef.current)
            navigate('/login', {
              state: { message: 'Email confirmed! You can now log in.' },
            })
            return 0
          }

          return prev - 1
        })
      }, 1000)
    }, 500)

    return () => {
      clearTimeout(startDelay)
      if (timerRef.current) clearInterval(timerRef.current)
    }
  }, [confirmState, navigate])

  const handleResend = async () => {
    if (!email) return
    setResending(true)
    setResendStatus(null)
    try {
      const res = await authApi.resendConfirmation(email)
      setResendStatus(res.message)
    } catch {
      setResendStatus('Could not resend confirmation email. Try again shortly.')
    } finally {
      setResending(false)
    }
  }

  if (confirmState === 'confirming') {
    return (
      <AuthLayout title="Confirming your email…">
        <p className="text-sm text-gray-400 text-center">Please wait a moment.</p>
      </AuthLayout>
    )
  }

  if (confirmState === 'confirmed') {
    return (
      <AuthLayout title="Email Verified!">
        <div className="space-y-6 text-center">
          <div className="flex justify-center">
            <div className="w-20 h-20 rounded-full bg-emerald-500/20 border-2 border-emerald-500/50 flex items-center justify-center text-4xl">
              ✅
            </div>
          </div>

          <div className="space-y-1">
            <p className="text-white font-semibold text-base">
              Your email has been confirmed!
            </p>
            <p className="text-gray-400 text-sm">
              Welcome to OptionsEdge. You can now log in to your account.
            </p>
          </div>

          <div className="flex flex-col items-center gap-1">
            <span className="text-4xl font-bold text-emerald-400 tabular-nums">
              {countdown}
            </span>
            <span className="text-xs text-gray-500">
              Redirecting to login automatically…
            </span>
          </div>

          <Link
            to="/login"
            onClick={() => {
              if (timerRef.current) clearInterval(timerRef.current)
            }}
            className={`block ${authButtonCls}`}
          >
            Go to Login now
          </Link>
        </div>
      </AuthLayout>
    )
  }

  if (confirmState === 'failed') {
    return (
      <AuthLayout title="Confirmation failed">
        <div className="space-y-4">
          <AuthError message={confirmError} />
          {email && (
            <button onClick={handleResend} disabled={resending} className={authButtonCls}>
              {resending ? 'Sending…' : 'Resend confirmation email'}
            </button>
          )}
          {resendStatus && <AuthSuccess message={resendStatus} />}
          <p className="text-center text-sm text-gray-400">
            <Link to="/login" className={authLinkCls}>Back to login</Link>
          </p>
        </div>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout title="Check your email" subtitle={email ? `We sent a confirmation link to ${email}` : 'We sent you a confirmation link.'}>
      <div className="space-y-4">
        <div className="rounded-lg bg-emerald-950/40 border border-emerald-800/40 p-3 text-xs text-emerald-300 space-y-1">
          <p className="font-medium">
            📧 Confirmation email sent
          </p>
          <p>Click the link in the email to verify your account. The link expires in 24 hours.</p>
          <p className="text-yellow-400">
            ⚠️ Don't see it? Check your <strong>spam or junk folder</strong>.
          </p>
        </div>
        {email && (
          <button onClick={handleResend} disabled={resending} className={authButtonCls}>
            {resending ? 'Sending…' : 'Resend email'}
          </button>
        )}
        {resendStatus && <AuthSuccess message={resendStatus} />}
        <p className="text-center text-sm text-gray-400">
          <Link to="/login" className={authLinkCls}>Back to login</Link>
        </p>
      </div>
    </AuthLayout>
  )
}
