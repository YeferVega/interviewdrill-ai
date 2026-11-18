export type Theme = 'light' | 'dark' | 'system'

const STORAGE_KEY = 'interviewdrill:theme'

export function getStoredTheme(): Theme {
  try {
    const value = localStorage.getItem(STORAGE_KEY)

    if (value === 'light' || value === 'dark' || value === 'system') {
      return value
    }
  } catch {
    // localStorage can be unavailable in private mode/tests.
  }

  return 'system'
}

export function applyTheme(theme: Theme): void {
  try {
    localStorage.setItem(STORAGE_KEY, theme)
  } catch {
    // ignore storage failures.
  }

  document.documentElement.dataset.theme = theme
  document.documentElement.style.colorScheme = theme === 'system'
    ? 'light dark'
    : theme
}

export function getNextTheme(current: Theme): Theme {
  if (current === 'system') {
    return 'light'
  }

  return current === 'light' ? 'dark' : 'system'
}

export function getThemeLabel(theme: Theme): string {
  switch (theme) {
    case 'light':
      return 'Tema claro'
    case 'dark':
      return 'Tema oscuro'
    case 'system':
      return 'Tema del sistema'
  }
}
