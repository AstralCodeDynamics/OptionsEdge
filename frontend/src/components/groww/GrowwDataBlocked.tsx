import { Link } from 'react-router-dom'

export default function GrowwDataBlocked() {
  return (
    <div className="flex min-h-64 flex-col items-center justify-center gap-4 rounded-xl border border-gray-800 bg-gray-900 p-6 text-center">
      <p className="text-sm font-medium text-gray-200">
        Connect your Groww account to see live market data
      </p>
      <Link
        to="/settings/security"
        className="rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-emerald-500"
      >
        Connect Groww
      </Link>
    </div>
  )
}
