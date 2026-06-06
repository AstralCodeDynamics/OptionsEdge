import { NavLink } from 'react-router-dom'
import { useAppStore } from '../../store/appStore'

const navItems = [
  { to: '/', label: 'Home', icon: '▦' },
  { to: '/positions', label: 'Positions', icon: '◈' },
  { to: '/chain', label: 'Chain', icon: '⊞' },
  { to: '/backtest', label: 'Backtest', icon: '◷' },
  { to: '/chat', label: 'Chat', icon: '◉' },
]

export default function BottomNav() {
  const unreadCount = useAppStore((s) => s.unreadCount)

  return (
    <nav className="fixed bottom-0 left-0 right-0 h-16 bg-gray-900 border-t border-gray-800 flex items-center lg:hidden z-10">
      {navItems.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          end={item.to === '/'}
          className={({ isActive }) =>
            [
              'flex-1 flex flex-col items-center justify-center gap-0.5 py-2 text-xs min-h-[44px] transition-colors relative',
              isActive ? 'text-emerald-400' : 'text-gray-500',
            ].join(' ')
          }
        >
          <span className="text-lg leading-none">{item.icon}</span>
          <span>{item.label}</span>
          {item.to === '/positions' && unreadCount > 0 && (
            <span className="absolute top-1 right-1/4 w-4 h-4 bg-red-500 rounded-full text-white text-[10px] flex items-center justify-center">
              {unreadCount > 9 ? '9+' : unreadCount}
            </span>
          )}
        </NavLink>
      ))}
    </nav>
  )
}
