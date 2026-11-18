import { useEffect, useState } from 'react'
import {
  applyTheme,
  getNextTheme,
  getStoredTheme,
  type Theme,
} from '../services/theme'
import { useInterviewStore } from '../store/interviewStore'
import { ui } from '../i18n/strings'

function getThemeLabel(theme: Theme, lang: 'es' | 'en'): string {
  const strings = ui(lang)

  switch (theme) {
    case 'light':
      return strings.themeLight
    case 'dark':
      return strings.themeDark
    case 'system':
      return strings.themeSystem
  }
}

export function ThemeToggle() {
  const [theme, setTheme] = useState<Theme>(getStoredTheme)
  const uiLanguage = useInterviewStore((state) => state.uiLanguage)

  useEffect(() => {
    applyTheme(theme)
  }, [theme])

  return (
    <button
      type="button"
      className="theme-toggle"
      onClick={() => setTheme((current) => getNextTheme(current))}
    >
      {getThemeLabel(theme, uiLanguage)}
    </button>
  )
}
