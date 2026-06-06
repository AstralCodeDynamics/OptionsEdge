import { useAppStore } from '../../store/appStore'

export default function Header() {
  const toggleSidebar = useAppStore((s) => s.toggleSidebar)
  const marketStatus = useAppStore((s) => s.marketStatus)

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
    </header>
  )
}
