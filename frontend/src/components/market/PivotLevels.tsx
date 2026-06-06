import type { PivotLevels as PivotLevelsType } from '../../types'

interface Props {
  pivots: PivotLevelsType
  spot: number
}

interface Level {
  label: string
  value: number
  isSpot?: boolean
  isPivot?: boolean
}

export function PivotLevels({ pivots, spot }: Props) {
  const levels: Level[] = [
    { label: 'R3', value: pivots.r3 },
    { label: 'R2', value: pivots.r2 },
    { label: 'R1', value: pivots.r1 },
    { label: 'Pivot', value: pivots.pivot, isPivot: true },
    { label: 'S1', value: pivots.s1 },
    { label: 'S2', value: pivots.s2 },
    { label: 'S3', value: pivots.s3 },
  ]

  const spotPosition = levels.findIndex((l) => !l.isSpot && spot > l.value)
  const levelsWithSpot: (Level | { label: 'SPOT'; value: number; isSpot: true })[] = [
    ...levels.slice(0, Math.max(0, spotPosition)),
    { label: 'SPOT', value: spot, isSpot: true },
    ...levels.slice(spotPosition < 0 ? 0 : spotPosition),
  ]

  return (
    <div className="bg-gray-900 border border-gray-700 rounded-lg p-3">
      <h3 className="text-xs font-medium text-gray-400 uppercase tracking-wide mb-3">
        Pivot Levels
      </h3>
      <div className="space-y-1">
        {levelsWithSpot.map((level, idx) => {
          if ('isSpot' in level && level.isSpot) {
            return (
              <div key={`spot-${idx}`} className="flex items-center gap-2">
                <div className="w-1 h-0.5 bg-yellow-400 flex-shrink-0" />
                <span className="text-xs font-bold text-yellow-400 w-10">SPOT</span>
                <div className="flex-1 h-px bg-yellow-400/50" />
                <span className="text-xs font-bold text-yellow-400">
                  {level.value.toLocaleString('en-IN', { maximumFractionDigits: 2 })}
                </span>
              </div>
            )
          }
          const l = level as Level
          const isResistance = l.label.startsWith('R')
          const isSupport = l.label.startsWith('S')
          const labelCls = l.isPivot
            ? 'text-blue-400'
            : isResistance
            ? 'text-red-400'
            : isSupport
            ? 'text-green-400'
            : 'text-gray-300'
          const barCls = l.isPivot
            ? 'bg-blue-500'
            : isResistance
            ? 'bg-red-500'
            : isSupport
            ? 'bg-green-500'
            : 'bg-gray-500'

          return (
            <div key={l.label} className="flex items-center gap-2">
              <div className={`w-1 h-4 rounded-sm ${barCls} flex-shrink-0`} />
              <span className={`text-xs font-semibold ${labelCls} w-10`}>{l.label}</span>
              <span className="text-xs text-gray-300 flex-1 text-right">
                {l.value.toLocaleString('en-IN', { maximumFractionDigits: 2 })}
              </span>
            </div>
          )
        })}
      </div>
    </div>
  )
}
