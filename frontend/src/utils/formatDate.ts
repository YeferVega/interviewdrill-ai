import { ui, type UiLanguage } from '../i18n/strings'

export function formatDate(
  value: string | null | undefined,
  lang: UiLanguage = 'es',
): string {
  if (!value) {
    return '—'
  }

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return ui(lang).invalidDate
  }

  return date.toLocaleString(lang === 'en' ? 'en-US' : 'es-ES')
}
