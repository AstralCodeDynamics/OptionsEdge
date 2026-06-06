import { BarChart, Bar, XAxis, YAxis, Tooltip, Legend, ResponsiveContainer } from 'recharts'
import type { OptionsChainRow } from '../../types'

interface Props {
  rows: OptionsChainRow[]
  atm: number
}

function formatOi(value: number): string {
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M`
  if (value >= 1_000) return `${(value / 1_000).toFixed(0)}K`
  return String(value)
}

export function OIChart({ rows, atm }: Props) {
  const data = rows.map((row) => ({
    strike: row.strike,
    CE: row.ce.oi,
    PE: row.pe.oi,
    isAtm: row.isAtm,
  }))

  return (
    <div className="bg-gray-900 border border-gray-700 rounded-lg p-3">
      <h3 className="text-xs font-medium text-gray-400 uppercase tracking-wide mb-3">
        OI Distribution — ATM {atm}
      </h3>
      <ResponsiveContainer width="100%" height={200}>
        <BarChart data={data} margin={{ top: 4, right: 4, left: -8, bottom: 0 }}>
          <XAxis
            dataKey="strike"
            tick={{ fill: '#9ca3af', fontSize: 10 }}
            tickLine={false}
            axisLine={{ stroke: '#374151' }}
          />
          <YAxis
            tickFormatter={formatOi}
            tick={{ fill: '#9ca3af', fontSize: 10 }}
            tickLine={false}
            axisLine={false}
          />
          <Tooltip
            contentStyle={{ backgroundColor: '#111827', border: '1px solid #374151', borderRadius: 6 }}
            labelStyle={{ color: '#f9fafb', fontSize: 11 }}
            itemStyle={{ fontSize: 11 }}
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            formatter={(value: any) => [typeof value === 'number' ? formatOi(value) : String(value ?? ''), ''] as [string, string]}
          />
          <Legend
            wrapperStyle={{ fontSize: 11, paddingTop: 4 }}
            formatter={(val) => <span style={{ color: val === 'CE' ? '#ef4444' : '#22c55e' }}>{val}</span>}
          />
          <Bar dataKey="CE" fill="#ef4444" opacity={0.85} radius={[2, 2, 0, 0]} />
          <Bar dataKey="PE" fill="#22c55e" opacity={0.85} radius={[2, 2, 0, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
