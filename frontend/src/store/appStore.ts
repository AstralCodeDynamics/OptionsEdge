import { create } from 'zustand'
import type { MarketSnapshot, Position, Alert, User, MarketStatus } from '../types'

interface MarketSlice {
  snapshots: Record<string, MarketSnapshot>
  marketStatus: MarketStatus | null
  setSnapshot: (snapshot: MarketSnapshot) => void
  setMarketStatus: (status: MarketStatus) => void
}

interface PositionsSlice {
  positions: Position[]
  setPositions: (positions: Position[]) => void
  upsertPosition: (position: Position) => void
  removePosition: (id: string) => void
}

interface AlertsSlice {
  alerts: Alert[]
  unreadCount: number
  setAlerts: (alerts: Alert[]) => void
  addAlert: (alert: Alert) => void
  markRead: (id: string) => void
  markAllRead: () => void
}

interface UserSlice {
  user: User | null
  setUser: (user: User | null) => void
}

interface UiSlice {
  sidebarOpen: boolean
  toggleSidebar: () => void
  setSidebarOpen: (open: boolean) => void
}

type AppStore = MarketSlice & PositionsSlice & AlertsSlice & UserSlice & UiSlice

export const useAppStore = create<AppStore>((set) => ({
  // Market
  snapshots: {},
  marketStatus: null,
  setSnapshot: (snapshot) =>
    set((s) => ({ snapshots: { ...s.snapshots, [snapshot.symbol]: snapshot } })),
  setMarketStatus: (marketStatus) => set({ marketStatus }),

  // Positions
  positions: [],
  setPositions: (positions) => set({ positions }),
  upsertPosition: (position) =>
    set((s) => {
      const existing = s.positions.findIndex((p) => p.id === position.id)
      if (existing >= 0) {
        const updated = [...s.positions]
        updated[existing] = position
        return { positions: updated }
      }
      return { positions: [...s.positions, position] }
    }),
  removePosition: (id) =>
    set((s) => ({ positions: s.positions.filter((p) => p.id !== id) })),

  // Alerts
  alerts: [],
  unreadCount: 0,
  setAlerts: (alerts) =>
    set({ alerts, unreadCount: alerts.filter((a) => !a.isRead).length }),
  addAlert: (alert) =>
    set((s) => ({
      alerts: [alert, ...s.alerts],
      unreadCount: s.unreadCount + (alert.isRead ? 0 : 1),
    })),
  markRead: (id) =>
    set((s) => ({
      alerts: s.alerts.map((a) => (a.id === id ? { ...a, isRead: true } : a)),
      unreadCount: Math.max(0, s.unreadCount - 1),
    })),
  markAllRead: () =>
    set((s) => ({
      alerts: s.alerts.map((a) => ({ ...a, isRead: true })),
      unreadCount: 0,
    })),

  // User
  user: null,
  setUser: (user) => set({ user }),

  // UI
  sidebarOpen: false,
  toggleSidebar: () => set((s) => ({ sidebarOpen: !s.sidebarOpen })),
  setSidebarOpen: (sidebarOpen) => set({ sidebarOpen }),
}))
