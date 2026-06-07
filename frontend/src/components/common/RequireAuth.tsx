import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAppStore } from '../../store/appStore'

export function AuthLoadingScreen() {
  return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-4 bg-gray-950">
      <span className="text-2xl font-bold text-emerald-400">OptionsEdge</span>
      <div className="w-8 h-8 border-2 border-gray-700 border-t-emerald-400 rounded-full animate-spin" />
    </div>
  )
}

export function RequireAuth({ children }: { children: ReactNode }) {
  const isAuthenticated = useAppStore((s) => s.isAuthenticated)
  const isAuthLoading = useAppStore((s) => s.isAuthLoading)
  const location = useLocation()

  if (isAuthLoading) {
    return <AuthLoadingScreen />
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  return <>{children}</>
}
