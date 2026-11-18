import { describe, expect, it } from 'vitest'
import { ui } from './strings'

describe('ui dictionaries', () => {
  it('exposes exactly the same keys for es and en', () => {
    const es = ui('es') as unknown as Record<string, string>
    const en = ui('en') as unknown as Record<string, string>

    const esKeys = Object.keys(es).sort()
    const enKeys = Object.keys(en).sort()

    expect(esKeys).toEqual(enKeys)
    expect(esKeys.length).toBeGreaterThan(50)
  })

  it('has no empty values', () => {
    for (const lang of ['es', 'en'] as const) {
      const dictionary = ui(lang) as unknown as Record<string, string>

      for (const [key, value] of Object.entries(dictionary)) {
        expect(value, `${lang}.${key}`).not.toHaveLength(0)
      }
    }
  })

  it('falls back to Spanish for unknown languages', () => {
    expect(ui(undefined)).toEqual(ui('es'))
  })

  it('localizes verdict labels differently per language', () => {
    expect(ui('es').verdictNeedsPractice).toBe('NECESITA PRÁCTICA')
    expect(ui('en').verdictNeedsPractice).toBe('NEEDS PRACTICE')
  })
})
