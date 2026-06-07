import { Outlet } from 'react-router-dom'
import Sidebar from './Sidebar'
import BottomNav from './BottomNav'
import Header from './Header'
import AlertBanner from '../positions/AlertBanner'
import { useAlerts } from '../../hooks/useAlerts'

const HUB_URL = import.meta.env.VITE_HUB_URL

export default function AppShell() {
  useAlerts(HUB_URL)

  return (
    <div className="min-h-screen bg-gray-950 text-white flex">
      <AlertBanner />
      <Sidebar />
      <div className="flex-1 flex flex-col min-w-0">
        <Header />
        <main className="flex-1 overflow-auto pb-16 lg:pb-0">
          <Outlet />
        </main>
      </div>
      <BottomNav />
    </div>
  )
}
