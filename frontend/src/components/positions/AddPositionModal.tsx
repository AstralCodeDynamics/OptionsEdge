import { useState, useEffect } from 'react'
import { optionsApi } from '../../services/api'
import type { Position, Signal } from '../../types'

interface FormData {
  symbol: string
  strike: string
  optionType: 'CE' | 'PE'
  expiry: string
  entryPrice: string
  quantity: string
  stopLoss: string
  target1: string
  target2: string
}

interface Props {
  open: boolean
  onClose: () => void
  onSubmit: (data: {
    symbol: string
    strike: number
    optionType: string
    expiry: string
    entryPrice: number
    quantity: number
    stopLoss: number
    target1: number
    target2?: number
    signalId?: string
  }) => Promise<void>
  prefill?: Signal | Position | null
  mode?: 'add' | 'edit'
}

function validate(f: FormData, out: string[]): boolean {
  const entry = parseFloat(f.entryPrice)
  const sl    = parseFloat(f.stopLoss)
  const t1    = parseFloat(f.target1)

  if (!f.symbol) { out.push('Symbol is required'); return false }
  if (!f.strike || isNaN(parseInt(f.strike))) { out.push('Strike is required'); return false }
  if (!f.expiry) { out.push('Expiry is required'); return false }
  if (!f.entryPrice || entry <= 0) { out.push('Entry price must be positive'); return false }
  if (!f.quantity || parseInt(f.quantity) <= 0) { out.push('Quantity must be at least 1'); return false }
  if (!f.stopLoss || isNaN(sl)) { out.push('Stop loss is required'); return false }
  if (sl >= entry) { out.push('Stop loss must be below entry price'); return false }
  if (!f.target1 || isNaN(t1)) { out.push('Target 1 is required'); return false }
  if (t1 <= entry) { out.push('Target 1 must be above entry price'); return false }
  return true
}

const initialForm: FormData = {
  symbol: 'NIFTY',
  strike: '',
  optionType: 'CE',
  expiry: '',
  entryPrice: '',
  quantity: '1',
  stopLoss: '',
  target1: '',
  target2: '',
}

export default function AddPositionModal({ open, onClose, onSubmit, prefill, mode = 'add' }: Props) {
  const [form, setForm] = useState<FormData>({
    ...initialForm,
  })
  const [errors, setErrors]   = useState<string[]>([])
  const [loading, setLoading] = useState(false)
  const [expiries, setExpiries] = useState<string[]>([])
  const [expiriesLoading, setExpiriesLoading] = useState(false)
  const [expiriesError, setExpiriesError] = useState(false)
  const isEdit = mode === 'edit'

  useEffect(() => {
    if (!open) return
    if (!prefill) {
      setForm({ ...initialForm })
      setErrors([])
      return
    }

    const isPosition = 'entryPrice' in prefill
    setForm({
      symbol:     prefill.symbol,
      strike:     String(prefill.strike),
      optionType: prefill.optionType,
      expiry:     prefill.expiry,
      entryPrice: String(isPosition ? prefill.entryPrice : prefill.entryHigh),
      quantity:   String(isPosition ? prefill.quantity : 1),
      stopLoss:   String(prefill.stopLoss),
      target1:    String(prefill.target1),
      target2:    prefill.target2 != null ? String(prefill.target2) : '',
    })
    setErrors([])
  }, [open, prefill])

  useEffect(() => {
    if (!open) return

    let cancelled = false
    setExpiriesLoading(true)
    setExpiriesError(false)
    setExpiries([])

    optionsApi.getExpiries(form.symbol)
      .then((data) => {
        if (cancelled) return
        setExpiries(data)
        setForm((current) => ({
          ...current,
          expiry: data.includes(current.expiry) ? current.expiry : (data[0] ?? ''),
        }))
      })
      .catch(() => {
        if (cancelled) return
        setExpiriesError(true)
      })
      .finally(() => {
        if (!cancelled) setExpiriesLoading(false)
      })

    return () => { cancelled = true }
  }, [form.symbol, open])

  if (!open) return null

  const set = (key: keyof FormData) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setForm((f) => ({ ...f, [key]: e.target.value }))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const errs: string[] = []
    if (!validate(form, errs)) { setErrors(errs); return }
    setErrors([])
    setLoading(true)
    try {
      await onSubmit({
        symbol:     form.symbol,
        strike:     parseInt(form.strike),
        optionType: form.optionType,
        expiry:     form.expiry,
        entryPrice: parseFloat(form.entryPrice),
        quantity:   parseInt(form.quantity),
        stopLoss:   parseFloat(form.stopLoss),
        target1:    parseFloat(form.target1),
        target2:    form.target2 ? parseFloat(form.target2) : undefined,
        signalId:   !isEdit && prefill && 'entryHigh' in prefill ? prefill.id : undefined,
      })
      onClose()
    } catch {
      setErrors([isEdit ? 'Failed to update position. Please try again.' : 'Failed to add position. Please try again.'])
    } finally {
      setLoading(false)
    }
  }

  const inputCls = 'w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-emerald-500'
  const labelCls = 'block text-xs text-gray-400 mb-1'

  return (
    <>
      {/* Backdrop */}
      <div className="fixed inset-0 bg-black/60 z-50" onClick={onClose} />

      {/* Sheet — bottom on mobile, centered on desktop */}
      <div className="fixed bottom-0 left-0 right-0 z-50 bg-gray-900 rounded-t-2xl p-5 max-h-[90vh] overflow-y-auto lg:inset-auto lg:top-1/2 lg:left-1/2 lg:-translate-x-1/2 lg:-translate-y-1/2 lg:w-full lg:max-w-md lg:rounded-2xl">
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-white font-semibold text-base">{isEdit ? 'Edit Position' : 'Add Position'}</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-white text-2xl leading-none">×</button>
        </div>

        {prefill && !isEdit && (
          <div className="bg-gray-800 rounded-lg px-3 py-2 mb-4 text-xs text-emerald-400">
            Pre-filled from signal: {prefill.symbol} {prefill.strike} {prefill.optionType}
          </div>
        )}

        <form onSubmit={handleSubmit} className="flex flex-col gap-3">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className={labelCls}>Symbol</label>
              <select value={form.symbol} onChange={set('symbol')} className={inputCls}>
                <option>NIFTY</option>
                <option>BANKNIFTY</option>
              </select>
            </div>
            <div>
              <label className={labelCls}>Type</label>
              <select value={form.optionType} onChange={set('optionType')} className={inputCls}>
                <option value="CE">CE (Call)</option>
                <option value="PE">PE (Put)</option>
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className={labelCls}>Strike</label>
              <input type="number" value={form.strike} onChange={set('strike')} placeholder="24200" className={inputCls} />
            </div>
            <div>
              <label className={labelCls}>Expiry</label>
              <select
                value={form.expiry}
                onChange={set('expiry')}
                disabled={expiriesLoading || expiriesError || expiries.length === 0}
                className={inputCls}
              >
                {expiriesLoading && <option value="">Loading expiry dates…</option>}
                {expiriesError && <option value="">Couldn't load expiry dates</option>}
                {!expiriesLoading && !expiriesError && expiries.length === 0 && <option value="">No expiry dates available</option>}
                {isEdit && form.expiry && !expiries.includes(form.expiry) && (
                  <option value={form.expiry}>{form.expiry}</option>
                )}
                {expiries.map((d) => <option key={d} value={d}>{d}</option>)}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className={labelCls}>Entry Price (₹)</label>
              <input type="number" step="0.5" value={form.entryPrice} onChange={set('entryPrice')} placeholder="180" className={inputCls} />
            </div>
            <div>
              <label className={labelCls}>Quantity (lots)</label>
              <input type="number" min="1" value={form.quantity} onChange={set('quantity')} placeholder="1" className={inputCls} />
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div>
              <label className={labelCls}>Stop Loss (₹)</label>
              <input type="number" step="0.5" value={form.stopLoss} onChange={set('stopLoss')} placeholder="120" className={inputCls} />
            </div>
            <div>
              <label className={labelCls}>Target 1 (₹)</label>
              <input type="number" step="0.5" value={form.target1} onChange={set('target1')} placeholder="270" className={inputCls} />
            </div>
            <div>
              <label className={labelCls}>Target 2 (optional)</label>
              <input type="number" step="0.5" value={form.target2} onChange={set('target2')} placeholder="360" className={inputCls} />
            </div>
          </div>

          {errors.length > 0 && (
            <div className="bg-red-900/40 border border-red-700/50 rounded-lg px-3 py-2">
              {errors.map((e, i) => <p key={i} className="text-red-400 text-xs">{e}</p>)}
            </div>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 text-white font-semibold rounded-xl py-3 mt-1 transition-colors"
          >
            {loading ? (isEdit ? 'Saving…' : 'Adding…') : (isEdit ? 'Save Changes' : 'Add Position')}
          </button>
        </form>
      </div>
    </>
  )
}
