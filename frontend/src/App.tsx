import { lazy, Suspense, useEffect } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import AppShell from './components/layout/AppShell'
import { RequireAuth } from './components/common/RequireAuth'
import { useAuth } from './hooks/useAuth'

const Dashboard = lazy(() => import('./pages/Dashboard'))
const Positions = lazy(() => import('./pages/Positions'))
const Chain = lazy(() => import('./pages/Chain'))
const Backtest = lazy(() => import('./pages/Backtest'))
const Chat = lazy(() => import('./pages/Chat'))

const Login = lazy(() => import('./pages/Auth/Login'))
const Register = lazy(() => import('./pages/Auth/Register'))
const VerifyEmail = lazy(() => import('./pages/Auth/VerifyEmail'))
const TwoFactor = lazy(() => import('./pages/Auth/TwoFactor'))
const ForgotPassword = lazy(() => import('./pages/Auth/ForgotPassword'))
const ResetPassword = lazy(() => import('./pages/Auth/ResetPassword'))
const SecuritySettings = lazy(() => import('./pages/Auth/SecuritySettings'))

const PageFallback = <div className="p-8 text-gray-400">Loading...</div>

function AppRoutes() {
  const { checkAuth } = useAuth()

  useEffect(() => {
    checkAuth()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="/verify-email" element={<VerifyEmail />} />
      <Route path="/two-factor" element={<TwoFactor />} />
      <Route path="/forgot-password" element={<ForgotPassword />} />
      <Route path="/reset-password" element={<ResetPassword />} />

      <Route
        path="/"
        element={
          <RequireAuth>
            <AppShell />
          </RequireAuth>
        }
      >
        <Route index element={<Dashboard />} />
        <Route path="positions" element={<Positions />} />
        <Route path="chain" element={<Chain />} />
        <Route path="backtest" element={<Backtest />} />
        <Route path="chat" element={<Chat />} />
        <Route path="settings/security" element={<SecuritySettings />} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <Suspense fallback={PageFallback}>
        <AppRoutes />
      </Suspense>
    </BrowserRouter>
  )
}
