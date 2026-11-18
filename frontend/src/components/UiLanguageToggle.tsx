import { useInterviewStore } from '../store/interviewStore'
import { ui } from '../i18n/strings'

/** Header button that switches the interface language between Español and English (spec 021). */
export function UiLanguageToggle() {
  const uiLanguage = useInterviewStore((state) => state.uiLanguage)
  const setUiLanguage = useInterviewStore((state) => state.setUiLanguage)

  return (
    <button
      type="button"
      className="lang-toggle"
      aria-label={ui(uiLanguage).uiLanguageToggleAria}
      onClick={() => setUiLanguage(uiLanguage === 'es' ? 'en' : 'es')}
    >
      {uiLanguage === 'es' ? 'ES' : 'EN'}
    </button>
  )
}
