import { useCallback, useEffect, useRef, useState } from 'react'
import { chatApi } from '../services/api'

const SESSION_STORAGE_KEY = 'oe_chat_session_id'

export interface DisplayMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
  modelUsed?: string
  inputTokens?: number
  outputTokens?: number
  costUsd?: number
  createdAt: string
  streaming?: boolean
}

function loadSessionId(): string | null {
  try {
    return localStorage.getItem(SESSION_STORAGE_KEY)
  } catch {
    return null
  }
}

function saveSessionId(id: string): void {
  try {
    localStorage.setItem(SESSION_STORAGE_KEY, id)
  } catch {
    // ignore storage failures (private browsing etc.)
  }
}

export function useAIChat() {
  const [sessionId, setSessionId] = useState<string | null>(null)
  const [messages, setMessages]   = useState<DisplayMessage[]>([])
  const [sending, setSending]     = useState(false)
  const [error, setError]         = useState<string | null>(null)
  const abortRef = useRef<AbortController | null>(null)

  useEffect(() => {
    const existing = loadSessionId()
    if (existing) {
      setSessionId(existing)
      chatApi
        .getHistory(existing)
        .then((h) =>
          setMessages(
            h.messages.map((m) => ({
              id: m.id,
              role: m.role,
              content: m.content,
              modelUsed: m.modelUsed,
              inputTokens: m.inputTokens,
              outputTokens: m.outputTokens,
              costUsd: m.costUsd,
              createdAt: m.createdAt,
            })),
          ),
        )
        .catch(() => {})
      return
    }

    chatApi
      .newSession()
      .then(({ sessionId: id }) => {
        setSessionId(id)
        saveSessionId(id)
      })
      .catch(() => setError('Failed to start chat session'))
  }, [])

  const startNewSession = useCallback(async () => {
    abortRef.current?.abort()
    setSending(false)
    setError(null)
    try {
      const { sessionId: id } = await chatApi.newSession()
      setSessionId(id)
      saveSessionId(id)
      setMessages([])
    } catch {
      setError('Failed to start a new chat session')
    }
  }, [])

  const sendMessage = useCallback(
    async (text: string) => {
      const trimmed = text.trim()
      if (!trimmed || sending || !sessionId) return

      setError(null)
      setSending(true)

      const userMsg: DisplayMessage = {
        id: crypto.randomUUID(),
        role: 'user',
        content: trimmed,
        createdAt: new Date().toISOString(),
      }
      const assistantId = crypto.randomUUID()
      const assistantMsg: DisplayMessage = {
        id: assistantId,
        role: 'assistant',
        content: '',
        createdAt: new Date().toISOString(),
        streaming: true,
      }
      setMessages((prev) => [...prev, userMsg, assistantMsg])

      const controller = new AbortController()
      abortRef.current = controller

      try {
        await chatApi.streamMessage(
          sessionId,
          trimmed,
          {
            onDelta: (delta) => {
              setMessages((prev) =>
                prev.map((m) => (m.id === assistantId ? { ...m, content: m.content + delta } : m)),
              )
            },
            onDone: (meta) => {
              setMessages((prev) =>
                prev.map((m) =>
                  m.id === assistantId
                    ? {
                        ...m,
                        streaming: false,
                        modelUsed: meta.modelUsed,
                        inputTokens: meta.inputTokens,
                        outputTokens: meta.outputTokens,
                        costUsd: meta.costUsd,
                      }
                    : m,
                ),
              )
            },
            onError: (message) => {
              setError(message)
              setMessages((prev) => prev.filter((m) => m.id !== assistantId))
            },
          },
          controller.signal,
        )
      } catch (e) {
        if ((e as { name?: string })?.name !== 'AbortError') {
          setError('Failed to reach the AI chat service')
          setMessages((prev) => prev.filter((m) => m.id !== assistantId))
        }
      } finally {
        setSending(false)
        abortRef.current = null
      }
    },
    [sessionId, sending],
  )

  useEffect(() => () => abortRef.current?.abort(), [])

  return { sessionId, messages, sending, error, sendMessage, startNewSession }
}
