import { useState, type FormEvent } from 'react'
import { useInterviewStore } from '../store/interviewStore'
import { ui } from '../i18n/strings'
import { InterviewLanguageField } from './InterviewLanguageField'

export function JdUploadPage() {
  const [file, setFile] = useState<File | null>(null)
  const [pastedText, setPastedText] = useState('')
  const parseJd = useInterviewStore((state) => state.parseJd)
  const status = useInterviewStore((state) => state.status)
  const error = useInterviewStore((state) => state.error)
  const strings = ui(useInterviewStore((state) => state.uiLanguage))

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    void parseJd({ file, text: pastedText })
  }

  return (
    <section className="page" aria-labelledby="jd-title">
      <header className="page-header">
        <p className="eyebrow">InterviewDrill AI</p>
        <h1 id="jd-title">{strings.jdTitle}</h1>
        <p>{strings.jdSubtitle}</p>
      </header>

      <form className="card jd-form" onSubmit={handleSubmit}>
        <InterviewLanguageField />

        <div className="field">
          <label htmlFor="jd-file">{strings.jdFileLabel}</label>
          <input
            id="jd-file"
            type="file"
            accept=".pdf,.docx,.txt"
            onChange={(event) => setFile(event.target.files?.[0] ?? null)}
          />
        </div>

        <div className="field">
          <label htmlFor="jd-text">{strings.jdPasteLabel}</label>
          <textarea
            id="jd-text"
            rows={10}
            value={pastedText}
            onChange={(event) => setPastedText(event.target.value)}
            placeholder={strings.jdPlaceholder}
          />
        </div>

        <button className="primary-button" type="submit" disabled={status === 'parsing'}>
          {status === 'parsing' ? strings.jdAnalyzing : strings.jdAnalyze}
        </button>

        {error ? <p className="error" role="alert">{error}</p> : null}
      </form>
    </section>
  )
}
