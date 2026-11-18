import { useInterviewStore } from '../store/interviewStore'
import { ui } from '../i18n/strings'
import type { InterviewLanguage } from '../services/language'

/**
 * Radio group that chooses the language the backend should use for interview questions,
 * judge feedback and the study guide (spec 021). Shown on the JD upload page and the
 * profile review page so the user can decide before or right before starting.
 */
export function InterviewLanguageField() {
  const interviewLanguage = useInterviewStore((state) => state.interviewLanguage)
  const setInterviewLanguage = useInterviewStore((state) => state.setInterviewLanguage)
  const strings = ui(useInterviewStore((state) => state.uiLanguage))

  const choose = (value: InterviewLanguage) => setInterviewLanguage(value)

  return (
    <fieldset className="field language-field">
      <legend>{strings.interviewLangLabel}</legend>
      <div className="language-options">
        <label className="language-option">
          <input
            type="radio"
            name="interview-language"
            value="es"
            checked={interviewLanguage === 'es'}
            onChange={() => choose('es')}
          />
          {strings.interviewLangEs}
        </label>
        <label className="language-option">
          <input
            type="radio"
            name="interview-language"
            value="en"
            checked={interviewLanguage === 'en'}
            onChange={() => choose('en')}
          />
          {strings.interviewLangEn}
        </label>
      </div>
    </fieldset>
  )
}
