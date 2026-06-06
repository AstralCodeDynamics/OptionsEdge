import { BrowserRouter, Routes, Route } from 'react-router-dom'
import AppShell from './components/layout/AppShell'
import Dashboard from './pages/Dashboard'
import Positions from './pages/Positions'
import Chain from './pages/Chain'
import Backtest from './pages/Backtest'
import Chat from './pages/Chat'
import Login from './pages/Auth'

export default function App() {
  return (
    <BrowserRouter>
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
    </BrowserRouter>
  )
}
