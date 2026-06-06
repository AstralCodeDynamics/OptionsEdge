import { Outlet } from 'react-router-dom'
import Sidebar from './Sidebar'
import BottomNav from './BottomNav'
import Header from './Header'

export default function AppShell() {
  return (
    <div className="min-h-screen bg-gray-950 text-white flex">
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
