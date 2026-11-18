import { describe, expect, it } from 'vitest'
import {
  getProgressLabel,
  getQuestionCount,
  getTargetQuestionCount,
  getVerdictClass,
  getVerdictLabel,
  shouldAddInterviewerQuestion,
} from './interview'
import type { ChatMessage, InterviewQuestion, JobProfile } from '../types/interview'

const profile: JobProfile = {
  role: 'Senior Backend Engineer',
  seniority: 'senior',
  requiredSkills: [
    { name: 'RAG', weight: 0.9 },
    { name: 'Kafka', weight: 0.8 },
  ],
  niceToHaveSkills: [],
  responsibilities: ['Own production services'],
  rawTextHash: 'sha256:test',
  profileSource: 'heuristic',
}

const question: InterviewQuestion = {
  id: 'question-1',
  text: '¿Cómo diseñas RAG?',
  skillTarget: 'RAG',
  difficulty: 'hard',
  type: 'situational',
}

describe('interview utils', () => {
  it('does not add duplicate interviewer questions', () => {
    const messages: ChatMessage[] = [{
      id: question.id,
      role: 'interviewer',
      text: question.text,
      createdAt: new Date().toISOString(),
    }]

    expect(shouldAddInterviewerQuestion(messages, question)).toBe(false)
    expect(shouldAddInterviewerQuestion([], question)).toBe(true)
  })

  it('maps verdict classes and labels', () => {
    expect(getVerdictClass('pass')).toBe('verdict-pass')
    expect(getVerdictClass('needsPractice')).toBe('verdict-practice')
    expect(getVerdictClass('fail')).toBe('verdict-fail')
    expect(getVerdictLabel('needsPractice')).toBe('NECESITA PRÁCTICA')
  })

  it('counts interviewer messages and progress', () => {
    const messages: ChatMessage[] = [
      { id: '1', role: 'interviewer', text: 'Q1', createdAt: '' },
      { id: '2', role: 'candidate', text: 'A1', createdAt: '' },
      { id: '3', role: 'interviewer', text: 'Q2', createdAt: '' },
    ]

    expect(getQuestionCount(messages)).toBe(2)
    expect(getProgressLabel(messages, profile)).toContain('2 /')
    expect(getTargetQuestionCount(profile)).toBeGreaterThanOrEqual(5)
    expect(getTargetQuestionCount(profile)).toBeLessThanOrEqual(8)
  })
})
