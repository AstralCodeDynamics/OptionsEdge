import type { ReactNode } from 'react'

interface Props {
  title: string
  subtitle?: string
  children: ReactNode
}

export default function AuthLayout({ title, subtitle, children }: Props) {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-950 px-4 py-10">
      <div className="w-full max-w-sm bg-gray-900 border border-gray-800 rounded-2xl p-6 sm:p-8">
        <div className="text-center mb-6">
          <span className="text-2xl font-bold text-emerald-400">OptionsEdge</span>
          <h1 className="text-lg font-semibold text-white mt-3">{title}</h1>
          {subtitle && <p className="text-sm text-gray-400 mt-1">{subtitle}</p>}
        </div>
        {children}
      </div>
    </div>
  )
}

export function AuthError({ message }: { message: string | null }) {
  if (!message) return null
  return (
    <div className="bg-red-900/30 border border-red-700/40 rounded-lg px-4 py-2.5 text-sm text-red-400">
      {message}
    </div>
  )
}

export function AuthSuccess({ message }: { message: string | null }) {
  if (!message) return null
  return (
    <div className="bg-emerald-900/30 border border-emerald-700/40 rounded-lg px-4 py-2.5 text-sm text-emerald-400">
      {message}
    </div>
  )
}

export const authInputCls =
  'w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2.5 min-h-[44px] text-white text-sm focus:outline-none focus:border-emerald-500'

export const authButtonCls =
  'w-full bg-emerald-500 hover:bg-emerald-600 disabled:bg-emerald-500/40 disabled:cursor-not-allowed text-gray-950 font-semibold rounded-xl py-3 min-h-[44px] transition-colors'

export const authLinkCls = 'text-emerald-400 hover:text-emerald-300 transition-colors'

export function extractErrorMessage(e: unknown, fallback: string): string {
  const resp = (e as { response?: { data?: { error?: string }; status?: number } })?.response
  return resp?.data?.error ?? fallback
}
