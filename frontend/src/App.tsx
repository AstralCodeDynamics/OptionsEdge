import { lazy, Suspense } from 'react'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import AppShell from './components/layout/AppShell'

const Dashboard = lazy(() => import('./pages/Dashboard'))
const Positions = lazy(() => import('./pages/Positions'))
const Chain = lazy(() => import('./pages/Chain'))
const Backtest = lazy(() => import('./pages/Backtest'))
const Chat = lazy(() => import('./pages/Chat'))
const Login = lazy(() => import('./pages/Auth'))

const PageFallback = <div className="p-8 text-gray-400">Loading...</div>

export default function App() {
  return (
    <BrowserRouter>
      <Suspense fallback={PageFallback}>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route element={<AppShell />}>
            <Route path="/" element={<Dashboard />} />
            <Route path="/positions" element={<Positions />} />
            <Route path="/chain" element={<Chain />} />
            <Route path="/backtest" element={<Backtest />} />
            <Route path="/chat" element={<Chat />} />
          </Route>
        </Routes>
      </Suspense>
    </BrowserRouter>
  )
}
