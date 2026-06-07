import { useEffect, useState } from 'react'
import { Link, useLocation, useSearchParams } from 'react-router-dom'
import { authApi } from '../../services/api'
import AuthLayout, { AuthError, AuthSuccess, authButtonCls, authLinkCls, extractErrorMessage } from '../../components/common/AuthLayout'

interface LocationState {
  email?: string
}

type ConfirmState = 'idle' | 'confirming' | 'confirmed' | 'failed'

export default function VerifyEmail() {
  const location = useLocation()
  const [searchParams] = useSearchParams()
  const state = location.state as LocationState | null
  const email = state?.email

  const userId = searchParams.get('userId')
  const token = searchParams.get('token')

  const [confirmState, setConfirmState] = useState<ConfirmState>('idle')
  const [confirmError, setConfirmError] = useState<string | null>(null)
  const [resendStatus, setResendStatus] = useState<string | null>(null)
  const [resending, setResending] = useState(false)

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
      <AuthLayout title="Email confirmed!">
        <div className="space-y-4">
          <AuthSuccess message="Your email has been confirmed. You can now log in." />
          <Link to="/login" className={`block text-center ${authButtonCls}`}>Log in now</Link>
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
        <p className="text-sm text-gray-400 text-center">
          Click the link in the email to confirm your account, then log in.
        </p>
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
