import { create } from 'zustand'
import type { MarketSnapshot, Position, Alert, AuthUser, MarketStatus, IndicatorsResponse, Signal } from '../types'
import type { GrowwStatus } from '../services/api'

interface MarketSlice {
  snapshots: Record<string, MarketSnapshot>
  marketStatus: MarketStatus | null
  indicators: Record<string, IndicatorsResponse>
  setSnapshot: (snapshot: MarketSnapshot) => void
  setMarketStatus: (status: MarketStatus) => void
  setIndicators: (symbol: string, indicators: IndicatorsResponse) => void
}

interface SignalsSlice {
  signals: Signal[]
  setSignals: (signals: Signal[]) => void
  addSignal: (signal: Signal) => void
  prependSignal: (signal: Signal) => void
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

interface AuthSlice {
  user: AuthUser | null
  isAuthenticated: boolean
  isAuthLoading: boolean
  setUser: (user: AuthUser | null) => void
  setIsAuthenticated: (value: boolean) => void
  setAuthLoading: (value: boolean) => void
  logout: () => void
}

interface UiSlice {
  sidebarOpen: boolean
  toggleSidebar: () => void
  setSidebarOpen: (open: boolean) => void
}

interface GrowwSlice {
  growwStatus: GrowwStatus | null
  setGrowwStatus: (status: GrowwStatus | null) => void
}

type AppStore = MarketSlice & SignalsSlice & PositionsSlice & AlertsSlice & AuthSlice & UiSlice & GrowwSlice

export const useAppStore = create<AppStore>((set) => ({
  // Market
  snapshots: {},
  marketStatus: null,
  indicators: {},
  setSnapshot: (snapshot) =>
    set((s) => ({ snapshots: { ...s.snapshots, [snapshot.symbol]: snapshot } })),
  setMarketStatus: (marketStatus) => set({ marketStatus }),
  setIndicators: (symbol, indicators) =>
    set((s) => ({ indicators: { ...s.indicators, [symbol]: indicators } })),

  // Signals
  signals: [],
  setSignals: (signals) => set({ signals }),
  addSignal: (signal) =>
    set((s) => ({ signals: [signal, ...s.signals.filter((x) => x.id !== signal.id)].slice(0, 20) })),
  prependSignal: (signal) =>
    set((s) => ({ signals: [signal, ...s.signals.filter((x) => x.id !== signal.id)] })),

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

  // Auth
  user: null,
  isAuthenticated: false,
  isAuthLoading: true,
  setUser: (user) => set({ user }),
  setIsAuthenticated: (isAuthenticated) => set({ isAuthenticated }),
  setAuthLoading: (isAuthLoading) => set({ isAuthLoading }),
  logout: () => set({ user: null, isAuthenticated: false }),

  // UI
  sidebarOpen: false,
  toggleSidebar: () => set((s) => ({ sidebarOpen: !s.sidebarOpen })),
  setSidebarOpen: (sidebarOpen) => set({ sidebarOpen }),

  // Groww
  growwStatus: null,
  setGrowwStatus: (growwStatus) => set({ growwStatus }),
}))
