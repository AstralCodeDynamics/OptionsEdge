import { useEffect, useRef, useState } from 'react'
import type { KeyboardEvent } from 'react'
import { useAppStore } from '../../store/appStore'
import { useAIChat } from '../../hooks/useAIChat'

const QUICK_ACTIONS = [
  'Analyze Market',
  'Check My Positions',
  'Best Strategy Today',
  "Tomorrow's Outlook",
]

function TypingIndicator() {
  return (
    <div className="flex items-center gap-1 px-1 py-1">
      <span className="w-1.5 h-1.5 rounded-full bg-gray-500 animate-bounce [animation-delay:-0.3s]" />
      <span className="w-1.5 h-1.5 rounded-full bg-gray-500 animate-bounce [animation-delay:-0.15s]" />
      <span className="w-1.5 h-1.5 rounded-full bg-gray-500 animate-bounce" />
    </div>
  )
}

function formatCost(costUsd?: number): string | null {
  if (costUsd == null) return null
  return `$${costUsd.toFixed(4)}`
}

export default function AIChat() {
  const snapshots    = useAppStore((s) => s.snapshots)
  const positions    = useAppStore((s) => s.positions)
  const marketStatus = useAppStore((s) => s.marketStatus)

  const { messages, sending, error, sendMessage, startNewSession } = useAIChat()
  const [input, setInput] = useState('')
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const nifty = snapshots.NIFTY
  const activeCount = positions.filter((p) => p.status === 'active').length

  const handleSend = () => {
    if (!input.trim() || sending) return
    sendMessage(input)
    setInput('')
  }

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  return (
    <div className="flex flex-col h-[calc(100vh-4rem)] lg:h-screen max-w-3xl mx-auto w-full">
      {/* Context bar */}
      <div className="flex items-center gap-2 flex-wrap px-4 py-3 border-b border-gray-800 text-xs text-gray-400">
        {nifty && (
          <span className="text-gray-200 font-medium">
            NIFTY {nifty.ltp.toLocaleString('en-IN', { maximumFractionDigits: 0 })}{' '}
            <span className={nifty.changePct >= 0 ? 'text-green-400' : 'text-red-400'}>
              {nifty.changePct >= 0 ? '▲' : '▼'} {Math.abs(nifty.changePct).toFixed(2)}%
            </span>
          </span>
        )}
        <span className="text-gray-600">|</span>
        <span>{activeCount} Active Position{activeCount !== 1 ? 's' : ''}</span>
        <span className="text-gray-600">|</span>
        <span className={marketStatus?.isOpen ? 'text-green-400' : 'text-gray-500'}>
          Market {marketStatus?.isOpen ? 'Open' : 'Closed'}
        </span>
        <button
          onClick={startNewSession}
          className="ml-auto text-gray-500 hover:text-gray-300 transition-colors"
        >
          New chat
        </button>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto px-4 py-4 space-y-3">
        {messages.length === 0 && (
          <div className="h-full flex flex-col items-center justify-center text-center gap-2 py-16">
            <p className="text-gray-300 text-sm font-medium">Your AI options trading partner</p>
            <p className="text-gray-500 text-xs max-w-sm">
              Ask about market conditions, your open positions, or what to do next. Powered by Claude Sonnet.
            </p>
          </div>
        )}

        {messages.map((m) => (
          <div key={m.id} className={`flex ${m.role === 'user' ? 'justify-end' : 'justify-start'}`}>
            <div
              className={`max-w-[85%] sm:max-w-[75%] flex flex-col ${
                m.role === 'user' ? 'items-end' : 'items-start'
              }`}
            >
              <div
                className={`rounded-2xl px-4 py-2.5 text-sm whitespace-pre-wrap break-words ${
                  m.role === 'user'
                    ? 'bg-blue-600 text-white rounded-br-sm'
                    : 'bg-gray-800 text-gray-100 rounded-bl-sm'
                }`}
              >
                {m.streaming && m.content === '' ? <TypingIndicator /> : m.content}
              </div>
              {m.role === 'assistant' && !m.streaming && m.costUsd != null && (
                <span className="text-[10px] text-gray-600 mt-1 px-1">
                  {m.modelUsed?.includes('sonnet') ? 'Sonnet' : m.modelUsed} ·{' '}
                  {(m.inputTokens ?? 0) + (m.outputTokens ?? 0)} tokens · {formatCost(m.costUsd)}
                </span>
              )}
            </div>
          </div>
        ))}
        <div ref={bottomRef} />
      </div>

      {error && (
        <div className="mx-4 mb-2 bg-red-900/30 border border-red-700/50 rounded-lg px-3 py-2 text-red-400 text-xs">
          {error}
        </div>
      )}

      {/* Quick actions */}
      <div className="px-4 pb-2 flex gap-2 overflow-x-auto">
        {QUICK_ACTIONS.map((label) => (
          <button
            key={label}
            onClick={() => !sending && sendMessage(label)}
            disabled={sending}
            className="shrink-0 bg-gray-800 hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed text-gray-300 text-xs font-medium rounded-full px-3 py-1.5 transition-colors"
          >
            {label}
          </button>
        ))}
      </div>

      {/* Input */}
      <div className="px-4 pb-4 pt-1 flex items-end gap-2 border-t border-gray-800">
        <textarea
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          rows={1}
          placeholder="Ask about the market, your positions, or strategy…"
          className="flex-1 resize-none bg-gray-800 text-gray-100 text-sm rounded-xl px-3 py-2.5 placeholder:text-gray-500 focus:outline-none focus:ring-1 focus:ring-blue-600 max-h-32"
        />
        <button
          onClick={handleSend}
          disabled={sending || !input.trim()}
          className="bg-blue-600 hover:bg-blue-500 disabled:bg-gray-700 disabled:cursor-not-allowed text-white text-sm font-medium rounded-xl px-4 py-2.5 transition-colors"
        >
          Send
        </button>
      </div>
    </div>
  )
}
