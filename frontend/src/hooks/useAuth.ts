import { useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAppStore } from '../store/appStore'
import { authApi, setTokens, clearTokens, getAccessToken } from '../services/api'
import type { AuthResponse, AuthUser, MeResponse, TwoFactorRequiredResponse } from '../types'

function toAuthUser(me: MeResponse): AuthUser {
  return {
    id: me.id,
    email: me.email,
    displayName: me.displayName,
    subscriptionPlan: me.subscriptionPlan,
    twoFactorEnabled: me.twoFactorEnabled,
    emailConfirmed: me.emailConfirmed,
  }
}

function isTwoFactorRequired(
  result: AuthResponse | TwoFactorRequiredResponse,
): result is TwoFactorRequiredResponse {
  return (result as TwoFactorRequiredResponse).twoFactorRequired === true
}

export function useAuth() {
  const navigate = useNavigate()
  const setUser = useAppStore((s) => s.setUser)
  const setIsAuthenticated = useAppStore((s) => s.setIsAuthenticated)
  const setAuthLoading = useAppStore((s) => s.setAuthLoading)
  const storeLogout = useAppStore((s) => s.logout)

  const checkAuth = useCallback(async () => {
    try {
      if (!getAccessToken()) {
        const data = await authApi.refresh()
        setTokens(data.accessToken, data.accessTokenExpiry)
      }

      const me = await authApi.me()
      setUser(toAuthUser(me))
      setIsAuthenticated(true)
    } catch {
      clearTokens()
      setUser(null)
      setIsAuthenticated(false)
    } finally {
      setAuthLoading(false)
    }
  }, [setUser, setIsAuthenticated, setAuthLoading])

  const login = useCallback(
    async (email: string, password: string) => {
      const result = await authApi.login({ email, password })

      if (isTwoFactorRequired(result)) {
        return { requiresTwoFactor: true as const, email: result.email }
      }

      setTokens(result.accessToken, result.accessTokenExpiry)
      setUser({
        id: result.userId,
        email: result.email,
        displayName: result.displayName,
        subscriptionPlan: result.subscriptionPlan,
        twoFactorEnabled: result.twoFactorEnabled,
        emailConfirmed: true,
      })
      setIsAuthenticated(true)

      return { requiresTwoFactor: false as const, email: result.email }
    },
    [setUser, setIsAuthenticated],
  )

  const completeTwoFactor = useCallback(
    async (email: string, code: string) => {
      const result = await authApi.twoFactor(email, code)
      setTokens(result.accessToken, result.accessTokenExpiry)
      setUser({
        id: result.userId,
        email: result.email,
        displayName: result.displayName,
        subscriptionPlan: result.subscriptionPlan,
        twoFactorEnabled: result.twoFactorEnabled,
        emailConfirmed: true,
      })
      setIsAuthenticated(true)

      return result
    },
    [setUser, setIsAuthenticated],
  )

  const logout = useCallback(async () => {
    await authApi.logout().catch(() => {})
    clearTokens()
    storeLogout()
    navigate('/login')
  }, [storeLogout, navigate])

  const register = useCallback(async (email: string, password: string, displayName: string) => {
    const result = await authApi.register({ email, password, displayName })
    return { requiresEmailConfirmation: result.requiresEmailConfirmation, message: result.message }
  }, [])

  return { checkAuth, login, completeTwoFactor, logout, register }
}
