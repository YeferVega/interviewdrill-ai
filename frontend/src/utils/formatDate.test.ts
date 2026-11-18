import { describe, expect, it } from 'vitest'
import { formatDate } from './formatDate'

describe('formatDate', () => {
  it('returns placeholder for empty values', () => {
    expect(formatDate(null)).toBe('—')
    expect(formatDate(undefined)).toBe('—')
    expect(formatDate('')).toBe('—')
  })

  it('returns invalid marker for malformed date', () => {
    expect(formatDate('not-a-date')).toBe('Fecha inválida')
    expect(formatDate('not-a-date', 'en')).toBe('Invalid date')
  })

  it('formats valid date with the requested locale', () => {
    const value = '2026-11-15T12:00:00Z'

    expect(formatDate(value)).toBe(new Date(value).toLocaleString('es-ES'))
    expect(formatDate(value, 'en')).toBe(new Date(value).toLocaleString('en-US'))
  })
})
