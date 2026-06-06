import { useEffect, useRef } from 'react'
import {
  createChart,
  CandlestickSeries,
  HistogramSeries,
  type IChartApi,
  type ISeriesApi,
  type CandlestickData,
  type HistogramData,
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

    chartRef.current = chart

    const ro = new ResizeObserver(() => {
      if (containerRef.current) {
        chart.applyOptions({ width: containerRef.current.clientWidth })
      }
    })
    ro.observe(containerRef.current)

    return () => {
      ro.disconnect()
      chart.remove()
      chartRef.current = null
    }
  }, [])

  // Update candle + volume data
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

  const fmtPrice = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 0 })

  return (
    <div className="bg-gray-900 border border-gray-700 rounded-lg overflow-hidden">
      <div className="flex flex-wrap items-center justify-between px-3 py-2 border-b border-gray-700 gap-2">
        <span className="text-xs font-semibold text-white">{symbol} · 15min</span>
        {ema && (
          <div className="flex items-center gap-3 text-xs">
            <span>
              <span className="text-amber-400 font-medium">EMA20:</span>{' '}
              <span className="text-gray-200">{fmtPrice(ema.ema20)}</span>
            </span>
            <span>
              <span className="text-violet-400 font-medium">EMA50:</span>{' '}
              <span className="text-gray-200">{fmtPrice(ema.ema50)}</span>
            </span>
            <span>
              <span className="text-blue-400 font-medium">EMA200:</span>{' '}
              <span className="text-gray-200">{fmtPrice(ema.ema200)}</span>
            </span>
          </div>
        )}
      </div>
      <div
        ref={containerRef}
        className="w-full"
        style={{ height: typeof window !== 'undefined' && window.innerWidth < 768 ? 240 : 320 }}
      />
    </div>
  )
}
