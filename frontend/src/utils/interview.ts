import type { ChatMessage, InterviewQuestion, JobProfile, Verdict } from '../types/interview'
import { ui, type UiLanguage } from '../i18n/strings'

export function shouldAddInterviewerQuestion(messages: ChatMessage[], question: InterviewQuestion): boolean {
  return !messages.some((message) => message.id === question.id)
}

export function getVerdictClass(verdict: Verdict): string {
  switch (verdict) {
    case 'pass':
      return 'verdict-pass'
    case 'needsPractice':
      return 'verdict-practice'
    case 'fail':
      return 'verdict-fail'
  }
}

export function getVerdictLabel(verdict: Verdict, lang: UiLanguage = 'es'): string {
  const strings = ui(lang)

  switch (verdict) {
    case 'pass':
      return strings.verdictPass
    case 'needsPractice':
      return strings.verdictNeedsPractice
    case 'fail':
      return strings.verdictFail
  }
}

export function getQuestionCount(messages: ChatMessage[]): number {
  return messages.filter((message) => message.role === 'interviewer').length
}

export function getTargetQuestionCount(profile: JobProfile | null): number {
  if (!profile) {
    return 8
  }

  const weightedSignals = profile.requiredSkills.length
    + Math.floor(profile.responsibilities.length / 2)
    + profile.requiredSkills.filter((skill) => skill.weight >= 0.85).length

  return Math.min(8, Math.max(5, weightedSignals))
}

export function getProgressLabel(
  messages: ChatMessage[],
  profile: JobProfile | null,
  lang: UiLanguage = 'es',
): string {
  const current = getQuestionCount(messages)
  const target = getTargetQuestionCount(profile)
  return `${ui(lang).progressPrefix}: ${Math.min(current, target)} / ${target}`
}

export function getScoreBarWidth(score: number): string {
  return `${Math.max(0, Math.min(100, score))}%`
}
