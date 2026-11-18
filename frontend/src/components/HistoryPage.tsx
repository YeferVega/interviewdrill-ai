import { useEffect } from 'react'
import { StudyGuidePanel } from './StudyGuidePanel'
import { useInterviewStore } from '../store/interviewStore'
import { ui } from '../i18n/strings'
import { getVerdictClass, getVerdictLabel } from '../utils/interview'
import { formatDate } from '../utils/formatDate'

export function HistoryPage() {
  const history = useInterviewStore((state) => state.history)
  const loading = useInterviewStore((state) => state.historyLoading)
  const error = useInterviewStore((state) => state.error)
  const selected = useInterviewStore((state) => state.selectedHistorySessionId)
  const guide = useInterviewStore((state) => state.historyGuideMarkdown)
  const busy = useInterviewStore((state) => state.busy)
  const loadHistory = useInterviewStore((state) => state.loadHistory)
  const loadStudyGuide = useInterviewStore((state) => state.loadStudyGuide)
  const uiLanguage = useInterviewStore((state) => state.uiLanguage)
  const strings = ui(uiLanguage)

  useEffect(() => {
    void loadHistory()
  }, [loadHistory])

  return (
    <section className="page" aria-labelledby="history-title">
      <header className="page-header">
        <p className="eyebrow">{strings.historyEyebrow}</p>
        <h1 id="history-title">{strings.historyTitle}</h1>
        <p>{strings.historySubtitle}</p>
      </header>

      <div className="card">
        <button type="button" className="secondary-button" disabled={loading} onClick={() => void loadHistory()}>
          {loading ? strings.historyLoading : strings.historyRefresh}
        </button>

        {error ? <p className="error" role="alert">{error}</p> : null}

        <div className="history-grid" role="list" aria-label={strings.historySessionsAria}>
          {history.length === 0 && !loading ? <p>{strings.historyEmpty}</p> : null}

          {history.map((session) => (
            <article className="history-card" key={session.sessionId} role="listitem">
              <h2>{formatDate(session.startedAt, uiLanguage)}</h2>
              <p className={`verdict ${getVerdictClass(session.overallVerdict)}`}>
                {getVerdictLabel(session.overallVerdict, uiLanguage)} · {session.overallScore}
              </p>
              <p className="muted">{session.status}</p>

              <button
                type="button"
                disabled={busy || !session.studyGuideId}
                onClick={() => void loadStudyGuide(session.sessionId)}
              >
                {strings.historyViewGuide}
              </button>
            </article>
          ))}
        </div>

        {selected ? (
          <StudyGuidePanel
            markdown={guide ?? (busy ? '' : `${strings.historySelectSessionPrefix} ${selected}`)}
          />
        ) : null}
      </div>
    </section>
  )
}
