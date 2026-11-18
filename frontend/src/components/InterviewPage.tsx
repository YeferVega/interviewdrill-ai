import { useState, type FormEvent } from 'react'
import { FeedbackPanel } from './FeedbackPanel'
import { StudyGuidePanel } from './StudyGuidePanel'
import { useInterviewStore } from '../store/interviewStore'
import { ui } from '../i18n/strings'
import { getProgressLabel } from '../utils/interview'
import { formatDifficulty } from '../utils/difficulty'

export function InterviewPage() {
  const messages = useInterviewStore((state) => state.messages)
  const profile = useInterviewStore((state) => state.profile)
  const feedback = useInterviewStore((state) => state.feedback)
  const studyGuideMarkdown = useInterviewStore((state) => state.studyGuideMarkdown)
  const busy = useInterviewStore((state) => state.busy)
  const status = useInterviewStore((state) => state.status)
  const hubStatus = useInterviewStore((state) => state.hubStatus)
  const error = useInterviewStore((state) => state.error)
  const currentQuestion = useInterviewStore((state) => state.currentQuestion)
  const submitAnswer = useInterviewStore((state) => state.submitAnswer)
  const reset = useInterviewStore((state) => state.reset)
  const uiLanguage = useInterviewStore((state) => state.uiLanguage)
  const strings = ui(uiLanguage)
  const [answer, setAnswer] = useState('')

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    void submitAnswer(answer)
    setAnswer('')
  }

  return (
    <section className="interview-layout" aria-label={strings.interviewLayoutAria}>
      <div className="chat-panel">
        <header className="chat-header">
          <div>
            <h1>{strings.interviewTitle}</h1>
            <p>
              {getProgressLabel(messages, profile, uiLanguage)} ·{' '}
              {strings.difficultyPrefix}: {formatDifficulty(currentQuestion?.difficulty, uiLanguage)}
            </p>
          </div>
          <div className={`hub-status ${hubStatus}`} aria-live="polite">
            {hubStatus === 'connected' ? strings.signalrConnected : hubStatus === 'error' ? strings.restMode : hubStatus}
          </div>
        </header>

        <ol className="messages" aria-label={strings.messagesAria}>
          {messages.map((message) => (
            <li key={message.id} className={`message ${message.role}`}>
              <span className="speaker">{message.role === 'interviewer' ? strings.speakerInterviewer : strings.speakerYou}</span>
              <p>{message.text}</p>
            </li>
          ))}
        </ol>

        {status === 'completed' ? (
          <div className="completed-card">
            <h2>{strings.completedTitle}</h2>
            <p>{strings.completedBody}</p>
            <button className="primary-button" type="button" onClick={() => void reset()}>{strings.practiceAnother}</button>
          </div>
        ) : (
          <form className="answer-form" onSubmit={handleSubmit}>
            <label htmlFor="answer">{strings.answerLabel}</label>
            <textarea
              id="answer"
              rows={6}
              value={answer}
              onChange={(event) => setAnswer(event.target.value)}
              disabled={busy || !profile}
              placeholder={strings.answerPlaceholder}
            />
            <button type="submit" className="primary-button" disabled={busy}>
              {busy ? strings.evaluating : strings.sendAnswer}
            </button>
          </form>
        )}

        {error ? <p className="error" role="alert">{error}</p> : null}
      </div>

      <div className="right-column">
        <FeedbackPanel feedback={feedback} />
        <StudyGuidePanel markdown={studyGuideMarkdown} />
      </div>
    </section>
  )
}
