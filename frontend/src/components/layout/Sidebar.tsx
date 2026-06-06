import { NavLink } from 'react-router-dom'
import { useAppStore } from '../../store/appStore'

const navItems = [
  { to: '/', label: 'Dashboard', icon: '▦' },
  { to: '/positions', label: 'Positions', icon: '◈' },
  { to: '/chain', label: 'Chain', icon: '⊞' },
  { to: '/backtest', label: 'Backtest', icon: '◷' },
  { to: '/chat', label: 'AI Chat', icon: '◉' },
]

export default function Sidebar() {
  const sidebarOpen = useAppStore((s) => s.sidebarOpen)
  const setSidebarOpen = useAppStore((s) => s.setSidebarOpen)

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
        <div className="px-4 py-5 border-b border-gray-800">
          <span className="text-lg font-bold text-emerald-400">OptionsEdge</span>
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
      </aside>
    </>
  )
}
