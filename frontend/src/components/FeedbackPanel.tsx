import type { EvaluationPayload } from '../types/interview'
import { useInterviewStore } from '../store/interviewStore'
import { ui } from '../i18n/strings'
import { getScoreBarWidth, getVerdictClass, getVerdictLabel } from '../utils/interview'

interface FeedbackPanelProps {
  feedback: EvaluationPayload | null
}

export function FeedbackPanel({ feedback }: FeedbackPanelProps) {
  const uiLanguage = useInterviewStore((state) => state.uiLanguage)

  if (!feedback) {
    return null
  }

  const strings = ui(uiLanguage)

  return (
    <aside className="feedback-panel" aria-live="polite">
      <div className={`verdict ${getVerdictClass(feedback.verdict)}`}>
        {getVerdictLabel(feedback.verdict, uiLanguage)}
      </div>

      <div className="scores">
        <div className="score">
          <span>{strings.scoreOverall}</span>
          <strong>{feedback.overallScore}</strong>
          <div className="bar" aria-hidden="true"><div style={{ width: getScoreBarWidth(feedback.overallScore) }} /></div>
        </div>

        <div className="score">
          <span>{strings.scoreTechnicalDepth}</span>
          <strong>{feedback.technicalDepth}</strong>
          <div className="bar" aria-hidden="true"><div style={{ width: getScoreBarWidth(feedback.technicalDepth) }} /></div>
        </div>

        <div className="score">
          <span>{strings.scoreProduction}</span>
          <strong>{feedback.productionReadiness}</strong>
          <div className="bar" aria-hidden="true"><div style={{ width: getScoreBarWidth(feedback.productionReadiness) }} /></div>
        </div>
      </div>

      <div className="chip-group">
        {feedback.coveredKeywords.map((keyword) => <span key={keyword} className="chip good">{keyword}</span>)}
        {feedback.missedConcepts.map((concept) => <span key={concept} className="chip warn">{concept}</span>)}
      </div>

      <p className="advice">{feedback.improvementAdvice}</p>
    </aside>
  )
}
