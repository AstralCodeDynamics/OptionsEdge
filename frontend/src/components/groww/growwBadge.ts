import type { GrowwStatus } from '../../services/api'

// Status badge shown in Header/Sidebar: Connect (gray) / Connected (green) / Reconnecting (yellow) / Failed (red).
export function growwBadgeLabel(status: GrowwStatus): string {
  if (!status.hasCredentials) return 'Connect Groww'
  if (status.connected) return 'Groww Connected'
  return status.error ? 'Groww Connection Failed' : 'Groww Reconnecting…'
}

export function growwBadgeClass(status: GrowwStatus): string {
  if (!status.hasCredentials) return 'bg-gray-500/20 text-gray-400 hover:bg-gray-500/30'
  if (status.connected) return 'bg-emerald-500/20 text-emerald-400 hover:bg-emerald-500/30'
  if (status.error) return 'bg-red-500/20 text-red-400 hover:bg-red-500/30'
  return 'bg-amber-500/20 text-amber-400 hover:bg-amber-500/30'
}
