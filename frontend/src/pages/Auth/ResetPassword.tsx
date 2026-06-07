import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { authApi } from '../../services/api'
import AuthLayout, { AuthError, authInputCls, authButtonCls, authLinkCls, extractErrorMessage } from '../../components/common/AuthLayout'
import PasswordStrength, { isPasswordValid } from '../../components/common/PasswordStrength'

export default function ResetPassword() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const email = searchParams.get('email') ?? ''
  const token = searchParams.get('token') ?? ''

  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const missingParams = !email || !token

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

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
      await authApi.resetPassword({ email, token, newPassword, confirmPassword })
      navigate('/login', { state: { message: 'Password reset. Log in now.' } })
    } catch (e: unknown) {
      setError(extractErrorMessage(e, 'Could not reset password. The link may have expired.'))
    } finally {
      setSubmitting(false)
    }
  }

  if (missingParams) {
    return (
      <AuthLayout title="Invalid reset link">
        <div className="space-y-4">
          <AuthError message="This password reset link is missing required information. Please request a new one." />
          <Link to="/forgot-password" className={`block text-center ${authButtonCls}`}>Request new link</Link>
        </div>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout title="Choose a new password" subtitle={email}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <AuthError message={error} />

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
          {submitting ? 'Resetting…' : 'Reset password'}
        </button>

        <p className="text-center text-sm text-gray-400">
          <Link to="/login" className={authLinkCls}>Back to login</Link>
        </p>
      </form>
    </AuthLayout>
  )
}
