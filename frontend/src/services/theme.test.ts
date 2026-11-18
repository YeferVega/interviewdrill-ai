import { describe, expect, it } from 'vitest'
import { getNextTheme, getThemeLabel } from './theme'

describe('theme utils', () => {
  it('cycles system -> light -> dark -> system', () => {
    expect(getNextTheme('system')).toBe('light')
    expect(getNextTheme('light')).toBe('dark')
    expect(getNextTheme('dark')).toBe('system')
  })

  it('returns Spanish labels', () => {
    expect(getThemeLabel('system')).toBe('Tema del sistema')
    expect(getThemeLabel('dark')).toBe('Tema oscuro')
  })
})
