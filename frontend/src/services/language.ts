import type { UiLanguage } from '../i18n/strings'

/** Language in which the backend generates interview questions, feedback prose and the study guide. */
export type InterviewLanguage = 'es' | 'en'

const UI_STORAGE_KEY = 'interviewdrill:ui-language'
const INTERVIEW_STORAGE_KEY = 'interviewdrill:interview-language'

function readStored(key: string): UiLanguage | null {
  try {
    const value = localStorage.getItem(key)

    if (value === 'es' || value === 'en') {
      return value
    }
  } catch {
    // localStorage can be unavailable in private mode/tests.
  }

  return null
}

function persist(key: string, value: UiLanguage): void {
  try {
    localStorage.setItem(key, value)
  } catch {
    // ignore storage failures.
  }
}

/** Defaults to Spanish to preserve the historical UI language. */
export function getStoredUiLanguage(): UiLanguage {
  return readStored(UI_STORAGE_KEY) ?? 'es'
}

export function setStoredUiLanguage(language: UiLanguage): void {
  persist(UI_STORAGE_KEY, language)
}

/** Defaults to Spanish (the historical output language of the deterministic agents). */
export function getStoredInterviewLanguage(): InterviewLanguage {
  return readStored(INTERVIEW_STORAGE_KEY) ?? 'es'
}

export function setStoredInterviewLanguage(language: InterviewLanguage): void {
  persist(INTERVIEW_STORAGE_KEY, language)
}
