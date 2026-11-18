import type {
  InterviewAnswerResponse,
  InterviewSessionSummary,
  InterviewStartResponse,
  JobProfile,
} from '../types/interview'
import type { InterviewLanguage } from './language'

export class ApiError extends Error {
  status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function parseResponse<T>(response: Response): Promise<T> {
  const text = await response.text()
  const data = text ? JSON.parse(text) as T | { error?: string } : null

  if (!response.ok) {
    const message = data && typeof data === 'object' && 'error' in data
      ? String(data.error ?? response.statusText)
      : response.statusText
    throw new ApiError(message, response.status)
  }

  return data as T
}

export async function getInterviewHistory(userId: string): Promise<InterviewSessionSummary[]> {
  const response = await fetch(`/api/interview/sessions?userId=${encodeURIComponent(userId)}`)
  return parseResponse<InterviewSessionSummary[]>(response)
}

export async function getStudyGuide(sessionId: string): Promise<string> {
  const response = await fetch(`/api/interview/sessions/${encodeURIComponent(sessionId)}/study-guide`)
  const data = await parseResponse<{ markdown: string }>(response)
  return data.markdown
}

export async function parseJobDescription(payload: { file?: File | null; text?: string }): Promise<JobProfile> {
  const form = new FormData()

  if (payload.file) {
    form.append('file', payload.file, payload.file.name)
  } else {
    form.append('text', payload.text ?? '')
  }

  const response = await fetch('/api/jd/parse', {
    method: 'POST',
    body: form,
  })

  return parseResponse<JobProfile>(response)
}

export async function startInterview(
  userId: string,
  profile: JobProfile,
  language?: InterviewLanguage,
): Promise<InterviewStartResponse> {
  const response = await fetch('/api/interview/sessions', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      userId,
      profile,
      language,
    }),
  })

  return parseResponse<InterviewStartResponse>(response)
}

export async function submitAnswer(
  sessionId: string,
  userId: string,
  answerText: string,
  language?: InterviewLanguage,
): Promise<InterviewAnswerResponse> {
  const response = await fetch(
    `/api/interview/sessions/${encodeURIComponent(sessionId)}/answers`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        userId,
        answerText,
        language,
      }),
    },
  )

  return parseResponse<InterviewAnswerResponse>(response)
}
