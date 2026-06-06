import type { IndicatorsResponse } from '../../types'

interface Props {
  indicators: IndicatorsResponse
}

type BadgeVariant = 'buy' | 'sell' | 'neutral'

function Badge({ variant, label }: { variant: BadgeVariant; label: string }) {
  const cls =
    variant === 'buy'
      ? 'bg-green-900/60 text-green-300 border border-green-700'
      : variant === 'sell'
      ? 'bg-red-900/60 text-red-300 border border-red-700'
      : 'bg-gray-800 text-gray-400 border border-gray-600'
  return (
    <span className={`inline-block px-2 py-0.5 rounded text-xs font-semibold ${cls}`}>
      {label}
    </span>
  )
}

function Card({ title, badge, children }: { title: string; badge?: React.ReactNode; children: React.ReactNode }) {
  return (
    <div className="bg-gray-900 border border-gray-700 rounded-lg p-3">
      <div className="flex items-center justify-between mb-2">
        <span className="text-xs font-medium text-gray-400 uppercase tracking-wide">{title}</span>
        {badge}
      </div>
      {children}
    </div>
  )
}

export function IndicatorPanel({ indicators }: Props) {
  const { rsi, macd, bollingerBands, adx, ema, supertrend } = indicators

  const rsiBadge: BadgeVariant =
    rsi.signal === 'Overbought' ? 'sell' : rsi.signal === 'Oversold' ? 'buy' : 'neutral'

  const macdBadge: BadgeVariant = macd.isBullishCross
    ? 'buy'
    : macd.value > macd.signal
    ? 'buy'
    : macd.value < macd.signal
    ? 'sell'
    : 'neutral'

  const bbBadge: BadgeVariant = bollingerBands.isSqueeze ? 'neutral' : 'neutral'

  const adxBadge: BadgeVariant =
    adx.strength === 'Strong' ? 'buy' : adx.strength === 'Moderate' ? 'neutral' : 'neutral'

  const emaBadge: BadgeVariant =
    ema.priceAboveEma20 && ema.priceAboveEma50
      ? 'buy'
      : !ema.priceAboveEma20 && !ema.priceAboveEma50
      ? 'sell'
      : 'neutral'

  const stBadge: BadgeVariant = supertrend.isBullish ? 'buy' : 'sell'

  return (
    <div className="grid grid-cols-2 md:grid-cols-3 gap-2">
      <Card title="RSI(14)" badge={<Badge variant={rsiBadge} label={rsi.signal} />}>
        <p className="text-xl font-bold text-white">{rsi.value.toFixed(1)}</p>
        <div className="mt-1 h-1.5 rounded bg-gray-700 overflow-hidden">
          <div
            className={`h-full rounded ${rsi.value >= 70 ? 'bg-red-500' : rsi.value <= 30 ? 'bg-green-500' : 'bg-blue-500'}`}
            style={{ width: `${Math.min(rsi.value, 100)}%` }}
          />
        </div>
      </Card>

      <Card
        title="MACD(12,26,9)"
        badge={<Badge variant={macdBadge} label={macd.isBullishCross ? 'CROSS' : macdBadge.toUpperCase()} />}
      >
        <p className="text-sm font-semibold text-white">
          {macd.value > 0 ? '+' : ''}{macd.value.toFixed(2)}
        </p>
        <p className="text-xs text-gray-400 mt-0.5">
          Hist: <span className={macd.histogram > 0 ? 'text-green-400' : 'text-red-400'}>
            {macd.histogram > 0 ? '+' : ''}{macd.histogram.toFixed(2)}
          </span>
        </p>
      </Card>

      <Card
        title="Bollinger Bands"
        badge={<Badge variant={bbBadge} label={bollingerBands.isSqueeze ? 'SQUEEZE' : 'NORMAL'} />}
      >
        <p className="text-xs text-gray-300">
          U: <span className="text-white font-medium">{bollingerBands.upper.toFixed(0)}</span>
        </p>
        <p className="text-xs text-gray-300">
          M: <span className="text-white font-medium">{bollingerBands.middle.toFixed(0)}</span>
        </p>
        <p className="text-xs text-gray-300">
          L: <span className="text-white font-medium">{bollingerBands.lower.toFixed(0)}</span>
        </p>
      </Card>

      <Card title="ADX(14)" badge={<Badge variant={adxBadge} label={adx.strength.toUpperCase()} />}>
        <p className="text-xl font-bold text-white">{adx.value.toFixed(1)}</p>
        <p className="text-xs text-gray-400 mt-0.5">Trend Strength</p>
      </Card>

      <Card title="EMA Cloud" badge={<Badge variant={emaBadge} label={emaBadge.toUpperCase()} />}>
        <div className="space-y-0.5">
          {([9, 20, 50, 200] as const).map((p) => {
            const val = p === 9 ? ema.ema9 : p === 20 ? ema.ema20 : p === 50 ? ema.ema50 : ema.ema200
            return (
              <p key={p} className="text-xs text-gray-300">
                EMA{p}: <span className="text-white font-medium">{val.toFixed(0)}</span>
              </p>
            )
          })}
        </div>
      </Card>

      <Card title="SuperTrend" badge={<Badge variant={stBadge} label={supertrend.isBullish ? 'BUY' : 'SELL'} />}>
        <p className="text-xl font-bold text-white">{supertrend.value.toFixed(0)}</p>
        <p className={`text-xs mt-0.5 ${supertrend.isBullish ? 'text-green-400' : 'text-red-400'}`}>
          {supertrend.isBullish ? '▲ Uptrend' : '▼ Downtrend'}
        </p>
      </Card>
    </div>
  )
}
