import type { OptionsChainRow } from '../../types'

interface Props {
  rows: OptionsChainRow[]
  spot: number
  maxPain: number
}

function OiBar({ value, max }: { value: number; max: number }) {
  const pct = max > 0 ? Math.min((value / max) * 100, 100) : 0
  return (
    <div className="w-10 h-1.5 bg-gray-700 rounded overflow-hidden">
      <div className="h-full bg-blue-500 rounded" style={{ width: `${pct}%` }} />
    </div>
  )
}

function fmt(n: number) {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`
  if (n >= 1_000) return `${(n / 1_000).toFixed(0)}K`
  return String(n)
}

export function ChainTable({ rows, spot, maxPain }: Props) {
  const maxOi = Math.max(...rows.flatMap((r) => [r.ce.oi, r.pe.oi]))

  return (
    <div className="bg-gray-900 border border-gray-700 rounded-lg overflow-hidden">
      <div className="overflow-x-auto">
        <table className="w-full text-xs">
          <thead className="sticky top-0 bg-gray-800 z-10">
            <tr>
              <th colSpan={5} className="py-2 px-2 text-red-400 text-center font-semibold border-b border-gray-700">
                CALL (CE)
              </th>
              <th className="py-2 px-3 text-center font-bold text-white border-b border-l border-r border-gray-700">
                Strike
              </th>
              <th colSpan={5} className="py-2 px-2 text-green-400 text-center font-semibold border-b border-gray-700">
                PUT (PE)
              </th>
            </tr>
            <tr className="text-gray-400 border-b border-gray-700">
              <th className="py-1.5 px-2 text-right">OI</th>
              <th className="py-1.5 px-1 text-right">Bar</th>
              <th className="py-1.5 px-2 text-right">IV%</th>
              <th className="py-1.5 px-2 text-right">Δ</th>
              <th className="py-1.5 px-2 text-right font-semibold text-red-300">LTP</th>
              <th className="py-1.5 px-3 text-center font-bold text-white border-l border-r border-gray-700">
                —
              </th>
              <th className="py-1.5 px-2 text-left font-semibold text-green-300">LTP</th>
              <th className="py-1.5 px-2 text-left">Δ</th>
              <th className="py-1.5 px-2 text-left">IV%</th>
              <th className="py-1.5 px-1 text-left">Bar</th>
              <th className="py-1.5 px-2 text-left">OI</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => {
              const isAtm = row.isAtm
              const isMaxPain = row.strike === maxPain
              const rowBg = isAtm
                ? 'bg-yellow-900/30'
                : isMaxPain
                ? 'bg-blue-900/20'
                : 'hover:bg-gray-800/40'
              const ceItm = row.strike < spot
              const peItm = row.strike > spot

              return (
                <tr key={row.strike} className={`border-b border-gray-800 ${rowBg} transition-colors`}>
                  {/* CE side */}
                  <td className={`py-1.5 px-2 text-right ${ceItm ? 'text-gray-300' : 'text-gray-500'}`}>
                    {fmt(row.ce.oi)}
                  </td>
                  <td className="py-1.5 px-1">
                    <OiBar value={row.ce.oi} max={maxOi} />
                  </td>
                  <td className={`py-1.5 px-2 text-right ${ceItm ? 'text-gray-300' : 'text-gray-500'}`}>
                    {row.ce.iv.toFixed(1)}
                  </td>
                  <td className={`py-1.5 px-2 text-right ${ceItm ? 'text-gray-300' : 'text-gray-500'}`}>
                    {row.ce.delta.toFixed(2)}
                  </td>
                  <td className={`py-1.5 px-2 text-right font-medium ${ceItm ? 'text-red-300' : 'text-red-500'}`}>
                    {row.ce.ltp.toFixed(1)}
                  </td>

                  {/* Strike */}
                  <td className={`py-1.5 px-3 text-center font-bold border-l border-r border-gray-700 ${
                    isAtm ? 'text-yellow-400' : isMaxPain ? 'text-blue-400' : 'text-white'
                  }`}>
                    {row.strike}
                    {isAtm && <span className="ml-1 text-yellow-400/70 text-[10px]">ATM</span>}
                    {isMaxPain && !isAtm && <span className="ml-1 text-blue-400/70 text-[10px]">MP</span>}
                  </td>

                  {/* PE side */}
                  <td className={`py-1.5 px-2 text-left font-medium ${peItm ? 'text-green-300' : 'text-green-500'}`}>
                    {row.pe.ltp.toFixed(1)}
                  </td>
                  <td className={`py-1.5 px-2 text-left ${peItm ? 'text-gray-300' : 'text-gray-500'}`}>
                    {row.pe.delta.toFixed(2)}
                  </td>
                  <td className={`py-1.5 px-2 text-left ${peItm ? 'text-gray-300' : 'text-gray-500'}`}>
                    {row.pe.iv.toFixed(1)}
                  </td>
                  <td className="py-1.5 px-1">
                    <OiBar value={row.pe.oi} max={maxOi} />
                  </td>
                  <td className={`py-1.5 px-2 text-left ${peItm ? 'text-gray-300' : 'text-gray-500'}`}>
                    {fmt(row.pe.oi)}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}
