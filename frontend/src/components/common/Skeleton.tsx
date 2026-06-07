function Bone({ className = '' }: { className?: string }) {
  return <div className={`animate-pulse bg-gray-800 rounded ${className}`} />
}

export function IndexCardSkeleton() {
  return (
    <div className="bg-gray-900 border border-gray-800 rounded-xl p-4 flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <Bone className="h-3 w-20" />
        <Bone className="h-5 w-16 rounded-full" />
      </div>
      <Bone className="h-8 w-32" />
      <Bone className="h-4 w-28" />
      <Bone className="h-3 w-40" />
    </div>
  )
}

export function IndicatorPanelSkeleton() {
  return (
    <div className="bg-gray-900 border border-gray-800 rounded-xl p-6 grid grid-cols-2 sm:grid-cols-3 gap-4">
      {Array.from({ length: 6 }).map((_, i) => (
        <div key={i} className="space-y-2">
          <Bone className="h-3 w-16" />
          <Bone className="h-5 w-20" />
        </div>
      ))}
    </div>
  )
}

export function ChainTableSkeleton() {
  return (
    <div className="bg-gray-900 border border-gray-800 rounded-xl p-4 space-y-2">
      <Bone className="h-4 w-full" />
      {Array.from({ length: 8 }).map((_, i) => (
        <Bone key={i} className="h-8 w-full" />
      ))}
    </div>
  )
}

export function PositionCardSkeleton() {
  return (
    <div className="bg-gray-900 border border-gray-800 rounded-xl p-4 flex flex-col gap-3">
      <div className="flex items-start justify-between">
        <div className="space-y-2">
          <Bone className="h-4 w-32" />
          <Bone className="h-3 w-24" />
        </div>
        <div className="flex flex-col items-end gap-2">
          <Bone className="h-5 w-20" />
          <Bone className="h-3 w-14" />
        </div>
      </div>
      <Bone className="h-3 w-full" />
      <Bone className="h-3 w-2/3" />
    </div>
  )
}
