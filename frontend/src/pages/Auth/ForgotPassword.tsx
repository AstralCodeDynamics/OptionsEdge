import { useState } from 'react'
import { Link } from 'react-router-dom'
import { authApi } from '../../services/api'
import AuthLayout, { AuthSuccess, authInputCls, authButtonCls, authLinkCls } from '../../components/common/AuthLayout'

export default function ForgotPassword() {
  const [email, setEmail] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [submitted, setSubmitted] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    try {
      await authApi.forgotPassword(email.trim())
    } finally {
      setSubmitting(false)
      setSubmitted(true)
    }
  }

  return (
    <AuthLayout title="Reset your password" subtitle="We'll email you a link to reset your password.">
      {submitted ? (
        <div className="space-y-4">
          <AuthSuccess message="If an account exists for that email, a password reset link has been sent." />
          <p className="text-center text-sm text-gray-400">
            <Link to="/login" className={authLinkCls}>Back to login</Link>
          </p>
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="space-y-4">
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

          <button type="submit" disabled={submitting} className={authButtonCls}>
            {submitting ? 'Sending…' : 'Send reset link'}
          </button>

          <p className="text-center text-sm text-gray-400">
            <Link to="/login" className={authLinkCls}>Back to login</Link>
          </p>
        </form>
      )}
    </AuthLayout>
  )
}
