import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAppStore } from '../../store/appStore'
import { growwApi } from '../../services/api'
import { useAuth } from '../../hooks/useAuth'
import GrowwStatusModal from '../groww/GrowwStatusModal'
import { growwBadgeClass, growwBadgeLabel } from '../groww/growwBadge'

export default function Header() {
  const toggleSidebar = useAppStore((s) => s.toggleSidebar)
  const marketStatus = useAppStore((s) => s.marketStatus)
  const growwStatus = useAppStore((s) => s.growwStatus)
  const setGrowwStatus = useAppStore((s) => s.setGrowwStatus)
  const user = useAppStore((s) => s.user)
  const { logout } = useAuth()
  const [growwModalOpen, setGrowwModalOpen] = useState(false)
  const [userMenuOpen, setUserMenuOpen] = useState(false)

  useEffect(() => {
    const poll = () => growwApi.getStatus().then(setGrowwStatus).catch(() => {})
    poll()
    const id = setInterval(poll, 5 * 60_000)
    return () => clearInterval(id)
  }, [setGrowwStatus])

  return (
    <header className="h-12 bg-gray-900 border-b border-gray-800 flex items-center px-4 gap-3 lg:hidden">
      <button
        onClick={toggleSidebar}
        className="text-gray-400 hover:text-white min-w-[44px] min-h-[44px] -ml-2 flex items-center justify-center rounded"
        aria-label="Toggle menu"
      >
        ☰
      </button>
      <span className="text-sm font-bold text-emerald-400 flex-1">OptionsEdge</span>
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
      {marketStatus && (
        <span
          className={[
            'text-xs px-2 py-0.5 rounded-full font-medium',
            marketStatus.isOpen ? 'bg-emerald-500/20 text-emerald-400' : 'bg-gray-700 text-gray-400',
          ].join(' ')}
        >
          {marketStatus.isOpen ? 'Open' : 'Closed'}
        </span>
      )}

      {user && (
        <div className="relative">
          <button
            onClick={() => setUserMenuOpen((v) => !v)}
            className="min-w-[44px] min-h-[44px] flex items-center justify-center rounded-full bg-emerald-500/20 text-emerald-400 text-sm font-semibold"
            aria-label="Account menu"
          >
            {user.displayName.charAt(0).toUpperCase()}
          </button>

          {userMenuOpen && (
            <>
              <div className="fixed inset-0 z-30" onClick={() => setUserMenuOpen(false)} />
              <div className="absolute right-0 top-10 z-40 w-56 bg-gray-900 border border-gray-800 rounded-xl shadow-lg py-2">
                <div className="px-4 py-2 border-b border-gray-800">
                  <p className="text-sm font-medium text-white truncate">{user.displayName}</p>
                  <p className="text-xs text-gray-500 truncate">{user.email}</p>
                </div>
                <Link
                  to="/settings/security"
                  onClick={() => setUserMenuOpen(false)}
                  className="block px-4 py-2.5 min-h-[44px] flex items-center text-sm text-gray-300 hover:bg-gray-800 hover:text-white transition-colors"
                >
                  Security settings
                </Link>
                <button
                  onClick={() => { setUserMenuOpen(false); logout() }}
                  className="w-full text-left px-4 py-2.5 min-h-[44px] flex items-center text-sm text-gray-300 hover:bg-gray-800 hover:text-white transition-colors"
                >
                  Log out
                </button>
              </div>
            </>
          )}
        </div>
      )}

      <GrowwStatusModal
        open={growwModalOpen}
        onClose={() => setGrowwModalOpen(false)}
        status={growwStatus}
        onStatusChange={setGrowwStatus}
      />
    </header>
  )
}
