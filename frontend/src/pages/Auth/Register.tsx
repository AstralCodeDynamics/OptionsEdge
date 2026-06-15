import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import AuthLayout, { AuthError, authInputCls, authButtonCls, authLinkCls, extractErrorMessage } from '../../components/common/AuthLayout'
import PasswordStrength, { isPasswordValid } from '../../components/common/PasswordStrength'

const emailRegex = /^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$/

export default function Register() {
  const navigate = useNavigate()
  const { register } = useAuth()

  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    if (!emailRegex.test(email.trim())) {
      setError('Please enter a valid email address.')
      return
    }
    if (password !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }
    if (!isPasswordValid(password)) {
      setError('Password does not meet the requirements below.')
      return
    }

    setSubmitting(true)
    try {
      const result = await register(email.trim(), password, displayName.trim())
      if (result.requiresEmailConfirmation) {
        navigate('/verify-email', { state: { email: email.trim() } })
      } else {
        navigate('/login', { state: { message: 'Account created. Please log in.' } })
      }
    } catch (e: unknown) {
      setError(extractErrorMessage(e, 'Registration failed. Please try again.'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <AuthLayout title="Create your account">
      <form onSubmit={handleSubmit} className="space-y-4">
        <AuthError message={error} />

        <div>
          <label className="block text-xs text-gray-400 mb-1">Display name</label>
          <input
            type="text"
            required
            autoComplete="name"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            className={authInputCls}
            placeholder="Your name"
          />
        </div>

        <div>
          <label className="block text-xs text-gray-400 mb-1">Email</label>
          <input
            type="email"
            required
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            onBlur={() => {
              if (email && !emailRegex.test(email.trim())) {
                setError('Please enter a valid email address.')
              }
            }}
            className={authInputCls}
            placeholder="you@example.com"
          />
        </div>

        <div className="space-y-2">
          <label className="block text-xs text-gray-400 mb-1">Password</label>
          <input
            type="password"
            required
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className={authInputCls}
            placeholder="••••••••"
          />
          <PasswordStrength password={password} />
        </div>

        <div>
          <label className="block text-xs text-gray-400 mb-1">Confirm password</label>
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
          {submitting ? 'Creating account…' : 'Create account'}
        </button>

        <div className="rounded-lg bg-blue-950/40 border border-blue-800/40 p-3 text-xs text-blue-300 space-y-1">
          <p className="font-medium">📧 Email verification required</p>
          <p>
            After registering, check your inbox for a confirmation email. If you don't see it within a few
            minutes, check your <strong>spam or junk folder</strong>.
          </p>
        </div>

        <p className="text-center text-sm text-gray-400">
          Already have an account?{' '}
          <Link to="/login" className={authLinkCls}>Log in</Link>
        </p>
      </form>
    </AuthLayout>
  )
}
