import {
  AreaChart, Area, XAxis, YAxis, Tooltip, CartesianGrid, ReferenceLine, ResponsiveContainer,
} from 'recharts'
import type { PayoffPoint } from '../../types'

interface Props {
  curve: PayoffPoint[]
  spot?: number
}

const fmtMoney = (v: number) => v.toLocaleString('en-IN', { maximumFractionDigits: 0 })

export function PayoffDiagram({ curve, spot }: Props) {
  const data = curve.map((p) => ({
    price: p.price,
    profit: p.pnl >= 0 ? p.pnl : 0,
    loss: p.pnl < 0 ? p.pnl : 0,
  }))

  return (
    <div className="bg-gray-900 border border-gray-700 rounded-lg p-3">
      <h3 className="text-xs font-medium text-gray-400 uppercase tracking-wide mb-3">
        Payoff at Expiry
      </h3>
      <ResponsiveContainer width="100%" height={240}>
        <AreaChart data={data} margin={{ top: 4, right: 8, left: -8, bottom: 0 }}>
          <CartesianGrid stroke="#1f2937" strokeDasharray="3 3" />
          <XAxis
            dataKey="price"
            type="number"
            domain={['dataMin', 'dataMax']}
            tickFormatter={(v) => fmtMoney(Number(v))}
            tick={{ fill: '#9ca3af', fontSize: 10 }}
            tickLine={false}
            axisLine={{ stroke: '#374151' }}
          />
          <YAxis
            tickFormatter={(v) => `₹${fmtMoney(Number(v))}`}
            tick={{ fill: '#9ca3af', fontSize: 10 }}
            tickLine={false}
            axisLine={false}
          />
          <Tooltip
            contentStyle={{ backgroundColor: '#111827', border: '1px solid #374151', borderRadius: 6 }}
            labelStyle={{ color: '#f9fafb', fontSize: 11 }}
            itemStyle={{ fontSize: 11 }}
            formatter={(value: unknown, name: unknown) => {
              const num = Number(value)
              if (num === 0) return null
              return [`₹${fmtMoney(num)}`, name === 'profit' ? 'P&L (profit)' : 'P&L (loss)'] as [string, string]
            }}
            labelFormatter={(label) => `Spot ₹${fmtMoney(Number(label))}`}
          />
          <ReferenceLine y={0} stroke="#6b7280" strokeDasharray="2 2" />
          {spot != null && (
            <ReferenceLine x={spot} stroke="#3b82f6" strokeDasharray="3 3" label={{ value: 'Spot', fill: '#60a5fa', fontSize: 10, position: 'top' }} />
          )}
          <Area type="monotone" dataKey="profit" stroke="#22c55e" fill="#22c55e" fillOpacity={0.25} strokeWidth={1.5} />
          <Area type="monotone" dataKey="loss" stroke="#ef4444" fill="#ef4444" fillOpacity={0.25} strokeWidth={1.5} />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  )
}
