import { useEffect, useState } from 'react'

export function useLocalStorageState<T>(key: string, fallback: T) {
  const [value, setValue] = useState<T>(() => {
    if (typeof window === 'undefined') {
      return fallback
    }

    const rawValue = window.localStorage.getItem(key)
    if (rawValue === null) {
      return fallback
    }

    try {
      return JSON.parse(rawValue) as T
    } catch {
      return fallback
    }
  })

  useEffect(() => {
    window.localStorage.setItem(key, JSON.stringify(value))
  }, [key, value])

  return [value, setValue] as const
}
