import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { FeedbackPanel } from './FeedbackPanel'
import type { EvaluationPayload } from '../types/interview'

const feedback: EvaluationPayload = {
  overallScore: 82,
  technicalDepth: 85,
  productionReadiness: 78,
  verdict: 'pass',
  coveredKeywords: ['RAG', 'Polly'],
  missedConcepts: ['Semantic Cache'],
  strengths: ['Concrete'],
  improvementAdvice: 'Agrega métricas de producción.',
}

describe('FeedbackPanel', () => {
  it('renders verdict, scores, keywords and advice', () => {
    render(<FeedbackPanel feedback={feedback} />)

    expect(screen.getByText('PASS')).toBeInTheDocument()
    expect(screen.getByText('82')).toBeInTheDocument()
    expect(screen.getByText('RAG')).toBeInTheDocument()
    expect(screen.getByText('Semantic Cache')).toBeInTheDocument()
    expect(screen.getByText('Agrega métricas de producción.')).toBeInTheDocument()
  })

  it('renders nothing when feedback is absent', () => {
    render(<FeedbackPanel feedback={null} />)

    expect(screen.queryByText('PASS')).not.toBeInTheDocument()
  })
})
