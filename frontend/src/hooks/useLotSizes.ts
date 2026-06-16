import { useEffect, useState } from 'react'
import { configApi } from '../services/api'
import type { LotSizeConfig } from '../types'

let cachedLotSizes: LotSizeConfig | null = null
let inflightLotSizes: Promise<LotSizeConfig> | null = null

async function loadLotSizes(): Promise<LotSizeConfig> {
  if (cachedLotSizes) return cachedLotSizes
  if (inflightLotSizes) return inflightLotSizes

  inflightLotSizes = configApi.getLotSizes()
    .then((lotSizes) => {
      cachedLotSizes = lotSizes
      return lotSizes
    })
    .finally(() => {
      inflightLotSizes = null
    })

  return inflightLotSizes
}

export function useLotSizes() {
  const [lotSizes, setLotSizes] = useState<LotSizeConfig | null>(cachedLotSizes)
  const [loading, setLoading] = useState(cachedLotSizes === null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (cachedLotSizes) {
      setLotSizes(cachedLotSizes)
      setLoading(false)
      return
    }

    let active = true
    setLoading(true)

    loadLotSizes()
      .then((nextLotSizes) => {
        if (!active) return
        setLotSizes(nextLotSizes)
        setError(null)
      })
      .catch(() => {
        if (!active) return
        setError('Failed to load current lot sizes.')
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => {
      active = false
    }
  }, [])

  const getLotSize = (symbol: string) =>
    lotSizes?.[symbol.toUpperCase() as keyof LotSizeConfig] ?? null

  return { lotSizes, loading, error, getLotSize }
}
