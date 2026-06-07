import { useEffect, useState } from 'react'
import { useAppStore } from '../../store/appStore'
import { growwApi } from '../../services/api'
import ConnectGrowwModal from '../groww/ConnectGrowwModal'

export default function Header() {
  const toggleSidebar = useAppStore((s) => s.toggleSidebar)
  const marketStatus = useAppStore((s) => s.marketStatus)
  const growwStatus = useAppStore((s) => s.growwStatus)
  const setGrowwStatus = useAppStore((s) => s.setGrowwStatus)
  const [growwModalOpen, setGrowwModalOpen] = useState(false)

  useEffect(() => {
    growwApi.getStatus().then(setGrowwStatus).catch(() => {})
  }, [setGrowwStatus])

  return (
    <header className="h-12 bg-gray-900 border-b border-gray-800 flex items-center px-4 gap-3 lg:hidden">
      <button
        onClick={toggleSidebar}
        className="text-gray-400 hover:text-white w-8 h-8 flex items-center justify-center rounded"
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
            growwStatus.connected
              ? 'bg-emerald-500/20 text-emerald-400 hover:bg-emerald-500/30'
              : 'bg-gray-700 text-gray-400 hover:bg-gray-600',
          ].join(' ')}
        >
          {growwStatus.connected ? 'Groww Connected' : 'Connect Groww'}
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

      <ConnectGrowwModal
        open={growwModalOpen}
        onClose={() => setGrowwModalOpen(false)}
        status={growwStatus}
        onConnected={setGrowwStatus}
      />
    </header>
  )
}
