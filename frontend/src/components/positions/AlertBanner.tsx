import { useEffect, useRef } from 'react'
import { useAppStore } from '../../store/appStore'
import { alertsApi } from '../../services/api'
import { requestPermission, sendNotification } from '../../utils/pushNotification'

function playBeep() {
  try {
    const ctx  = new AudioContext()
    const osc  = ctx.createOscillator()
    const gain = ctx.createGain()
    osc.connect(gain)
    gain.connect(ctx.destination)
    osc.frequency.value = 880
    gain.gain.setValueAtTime(0.3, ctx.currentTime)
    gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.5)
    osc.start()
    osc.stop(ctx.currentTime + 0.5)
  } catch {
    // silently ignore if AudioContext unavailable
  }
}

export default function AlertBanner() {
  const alerts   = useAppStore((s) => s.alerts)
  const markRead = useAppStore((s) => s.markRead)

  const beepedRef  = useRef<Set<string>>(new Set())
  const warnTimers = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map())
  const infoTimers = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map())

  const danger   = alerts.filter((a) => a.severity === 'Danger'  && !a.isRead)
  const warnings = alerts.filter((a) => a.severity === 'Warning' && !a.isRead)
  const infos    = alerts.filter((a) => a.severity === 'Info'    && !a.isRead)

  // Beep + push on new DANGER alerts
  useEffect(() => {
    for (const a of danger) {
      if (!beepedRef.current.has(a.id)) {
        beepedRef.current.add(a.id)
        playBeep()
        requestPermission().then((ok) => {
          if (ok) sendNotification('⚠️ DANGER Alert', a.message)
        })
      }
    }
  }, [danger])

  // Auto-dismiss warnings after 30 s
  useEffect(() => {
    for (const a of warnings) {
      if (!warnTimers.current.has(a.id)) {
        const t = setTimeout(() => {
          dismiss(a.id)
          warnTimers.current.delete(a.id)
        }, 30_000)
        warnTimers.current.set(a.id, t)
      }
    }
    return () => {
      warnTimers.current.forEach((t) => clearTimeout(t))
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [warnings.map((a) => a.id).join(',')])

  // Auto-dismiss infos after 5 s
  useEffect(() => {
    for (const a of infos) {
      if (!infoTimers.current.has(a.id)) {
        const t = setTimeout(() => {
          dismiss(a.id)
          infoTimers.current.delete(a.id)
        }, 5_000)
        infoTimers.current.set(a.id, t)
      }
    }
    return () => {
      infoTimers.current.forEach((t) => clearTimeout(t))
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [infos.map((a) => a.id).join(',')])

  const dismiss = (id: string) => {
    markRead(id)
    alertsApi.markRead(id).catch(() => {})
  }

  if (!danger.length && !warnings.length && !infos.length) return null

  return (
    <>
      {/* DANGER — fixed top, no dismiss, pulsing red */}
      {danger.length > 0 && (
        <div className="fixed top-0 left-0 right-0 z-50 animate-pulse">
          {danger.map((a) => (
            <div key={a.id} className="flex items-center gap-3 bg-red-600 px-4 py-3">
              <span className="text-white font-bold text-sm shrink-0">⚠ DANGER</span>
              <span className="text-white text-sm">{a.message}</span>
            </div>
          ))}
        </div>
      )}

      {/* WARNING — slide-in below danger, manual or 30 s dismiss */}
      {warnings.length > 0 && (
        <div
          className="fixed left-0 right-0 z-40"
          style={{ top: danger.length > 0 ? `${danger.length * 44}px` : 0 }}
        >
          {warnings.map((a) => (
            <div key={a.id} className="flex items-center justify-between bg-orange-500 px-4 py-2">
              <div className="flex items-center gap-2">
                <span className="text-white font-semibold text-sm">WARNING</span>
                <span className="text-white text-sm">{a.message}</span>
              </div>
              <button
                onClick={() => dismiss(a.id)}
                className="text-white opacity-75 hover:opacity-100 ml-4 text-xl leading-none"
              >
                ×
              </button>
            </div>
          ))}
        </div>
      )}

      {/* INFO — bottom-right toast, auto 5 s */}
      {infos.length > 0 && (
        <div className="fixed bottom-20 right-4 z-40 flex flex-col gap-2 lg:bottom-4">
          {infos.map((a) => (
            <div
              key={a.id}
              className="flex items-center gap-3 bg-green-700 text-white rounded-lg px-4 py-3 shadow-xl max-w-xs"
            >
              <span className="text-sm flex-1">✓ {a.message}</span>
              <button
                onClick={() => dismiss(a.id)}
                className="opacity-75 hover:opacity-100 text-xl leading-none"
              >
                ×
              </button>
            </div>
          ))}
        </div>
      )}
    </>
  )
}
