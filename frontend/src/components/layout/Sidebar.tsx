import { useEffect, useState } from 'react'
import { NavLink } from 'react-router-dom'
import { useAppStore } from '../../store/appStore'
import { growwApi } from '../../services/api'
import { useAuth } from '../../hooks/useAuth'
import GrowwStatusModal from '../groww/GrowwStatusModal'
import { growwBadgeClass, growwBadgeLabel } from '../groww/growwBadge'

const navItems = [
  { to: '/', label: 'Dashboard', icon: '▦' },
  { to: '/positions', label: 'Positions', icon: '◈' },
  { to: '/chain', label: 'Chain', icon: '⊞' },
  { to: '/backtest', label: 'Backtest', icon: '◷' },
  { to: '/chat', label: 'AI Chat', icon: '◉' },
  { to: '/settings/security', label: 'Security', icon: '⚙' },
]

export default function Sidebar() {
  const sidebarOpen = useAppStore((s) => s.sidebarOpen)
  const setSidebarOpen = useAppStore((s) => s.setSidebarOpen)
  const growwStatus = useAppStore((s) => s.growwStatus)
  const setGrowwStatus = useAppStore((s) => s.setGrowwStatus)
  const user = useAppStore((s) => s.user)
  const { logout } = useAuth()
  const [growwModalOpen, setGrowwModalOpen] = useState(false)

  useEffect(() => {
    const poll = () => growwApi.getStatus().then(setGrowwStatus).catch(() => {})
    poll()
    const id = setInterval(poll, 5 * 60_000)
    return () => clearInterval(id)
  }, [setGrowwStatus])

  return (
    <>
      {sidebarOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-20 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}
      <aside
        className={[
          'fixed top-0 left-0 h-full w-56 bg-gray-900 border-r border-gray-800 z-30 flex flex-col transition-transform duration-200',
          'lg:translate-x-0 lg:static lg:z-auto',
          sidebarOpen ? 'translate-x-0' : '-translate-x-full',
        ].join(' ')}
      >
        <div className="px-4 py-5 border-b border-gray-800 flex items-center justify-between gap-2">
          <span className="text-lg font-bold text-emerald-400">OptionsEdge</span>
          {growwStatus?.enabled && (
            <button
              onClick={() => setGrowwModalOpen(true)}
              className={[
                'text-xs px-2 py-0.5 rounded-full font-medium transition-colors',
                growwBadgeClass(growwStatus),
              ].join(' ')}
            >
              {growwBadgeLabel(growwStatus)}
            </button>
          )}
        </div>
        <nav className="flex-1 py-4">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) =>
                [
                  'flex items-center gap-3 px-4 py-3 text-sm font-medium transition-colors',
                  isActive
                    ? 'text-emerald-400 bg-emerald-400/10'
                    : 'text-gray-400 hover:text-white hover:bg-gray-800',
                ].join(' ')
              }
              onClick={() => setSidebarOpen(false)}
            >
              <span className="text-base">{item.icon}</span>
              {item.label}
            </NavLink>
          ))}
        </nav>

        {user && (
          <div className="border-t border-gray-800 p-4 space-y-3">
            <div className="min-w-0">
              <p className="text-sm font-medium text-white truncate">{user.displayName}</p>
              <p className="text-xs text-gray-500 truncate">{user.email}</p>
            </div>
            <button
              onClick={() => logout()}
              className="w-full text-left text-sm font-medium text-gray-400 hover:text-white hover:bg-gray-800 rounded-lg px-3 py-2.5 min-h-[44px] transition-colors"
            >
              Log out
            </button>
          </div>
        )}
      </aside>

      <GrowwStatusModal
        open={growwModalOpen}
        onClose={() => setGrowwModalOpen(false)}
        status={growwStatus}
        onStatusChange={setGrowwStatus}
      />
    </>
  )
}
