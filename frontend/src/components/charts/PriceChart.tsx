import { useEffect, useRef } from 'react'
import {
  createChart,
  CandlestickSeries,
  HistogramSeries,
  LineSeries,
  type IChartApi,
  type ISeriesApi,
  type CandlestickData,
  type HistogramData,
  type LineData,
  type Time,
} from 'lightweight-charts'
import type { Candle, EmaIndicator } from '../../types'

interface Props {
  candles: Candle[]
  ema?: EmaIndicator
  symbol: string
}

export function PriceChart({ candles, ema, symbol }: Props) {
  const containerRef = useRef<HTMLDivElement>(null)
  const chartRef = useRef<IChartApi | null>(null)
  const candleSeriesRef = useRef<ISeriesApi<'Candlestick'> | null>(null)
  const volumeSeriesRef = useRef<ISeriesApi<'Histogram'> | null>(null)
  const ema20Ref = useRef<ISeriesApi<'Line'> | null>(null)
  const ema50Ref = useRef<ISeriesApi<'Line'> | null>(null)

  // Create chart once on mount
  useEffect(() => {
    if (!containerRef.current) return

    const chart = createChart(containerRef.current, {
      layout: {
        background: { color: '#111827' },
        textColor: '#9ca3af',
      },
      grid: {
        vertLines: { color: '#1f2937' },
        horzLines: { color: '#1f2937' },
      },
      crosshair: { mode: 1 },
      rightPriceScale: { borderColor: '#374151' },
      timeScale: { borderColor: '#374151', timeVisible: true },
    })

    candleSeriesRef.current = chart.addSeries(CandlestickSeries, {
      upColor: '#22c55e',
      downColor: '#ef4444',
      borderUpColor: '#22c55e',
      borderDownColor: '#ef4444',
      wickUpColor: '#22c55e',
      wickDownColor: '#ef4444',
    })

    volumeSeriesRef.current = chart.addSeries(HistogramSeries, {
      priceFormat: { type: 'volume' },
      priceScaleId: 'volume',
    })
    chart.priceScale('volume').applyOptions({ scaleMargins: { top: 0.8, bottom: 0 } })

    ema20Ref.current = chart.addSeries(LineSeries, {
      color: '#f59e0b',
      lineWidth: 1,
      priceLineVisible: false,
      lastValueVisible: false,
    })

    ema50Ref.current = chart.addSeries(LineSeries, {
      color: '#8b5cf6',
      lineWidth: 1,
      priceLineVisible: false,
      lastValueVisible: false,
    })

    chartRef.current = chart

    const ro = new ResizeObserver(() => {
      if (containerRef.current) {
        chart.applyOptions({
          width: containerRef.current.clientWidth,
        })
      }
    })
    ro.observe(containerRef.current)

    return () => {
      ro.disconnect()
      chart.remove()
      chartRef.current = null
    }
  }, [])

  // Update data when candles change
  useEffect(() => {
    if (!candleSeriesRef.current || !volumeSeriesRef.current) return
    if (candles.length === 0) return

    const candleData: CandlestickData[] = candles.map((c) => ({
      time: c.time as Time,
      open: c.open,
      high: c.high,
      low: c.low,
      close: c.close,
    }))

    const volData: HistogramData[] = candles.map((c) => ({
      time: c.time as Time,
      value: c.volume,
      color: c.close >= c.open ? '#22c55e33' : '#ef444433',
    }))

    candleSeriesRef.current.setData(candleData)
    volumeSeriesRef.current.setData(volData)
    chartRef.current?.timeScale().fitContent()
  }, [candles])

  // Update EMA lines when ema values change (we draw flat reference lines from last bar)
  useEffect(() => {
    if (!ema20Ref.current || !ema50Ref.current || !ema || candles.length === 0) return

    // Build EMA line as single point at last candle (lightweight-charts needs full series for proper lines)
    // We use a constant line from earliest available point
    const times = candles.map((c) => c.time as Time)
    const ema20Line: LineData[] = times.map((t) => ({ time: t, value: ema.ema20 }))
    const ema50Line: LineData[] = times.map((t) => ({ time: t, value: ema.ema50 }))

    ema20Ref.current.setData(ema20Line)
    ema50Ref.current.setData(ema50Line)
  }, [ema, candles])

  return (
    <div className="bg-gray-900 border border-gray-700 rounded-lg overflow-hidden">
      <div className="flex items-center justify-between px-3 py-2 border-b border-gray-700">
        <span className="text-xs font-semibold text-white">{symbol} · 15min</span>
        <div className="flex items-center gap-3 text-xs">
          <span className="flex items-center gap-1">
            <span className="inline-block w-3 h-0.5 bg-amber-400" /> EMA20
          </span>
          <span className="flex items-center gap-1">
            <span className="inline-block w-3 h-0.5 bg-violet-500" /> EMA50
          </span>
        </div>
      </div>
      <div
        ref={containerRef}
        className="w-full"
        style={{ height: typeof window !== 'undefined' && window.innerWidth < 768 ? 240 : 320 }}
      />
    </div>
  )
}
