export default function GrowwDataBlocked() {
  return (
    <div className="flex min-h-64 flex-col items-center justify-center gap-4 rounded-xl border border-gray-800 bg-gray-900 p-6 text-center">
      <p className="text-sm font-medium text-gray-200">
        Connect your Groww account to see live market data
      </p>
      <p className="text-xs text-gray-400">
        Use the <span className="font-semibold text-emerald-400">Groww</span> button in the sidebar to connect.
      </p>
    </div>
  )
}
