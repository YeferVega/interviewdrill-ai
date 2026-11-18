import type { UiLanguage } from '../i18n/strings'
import type { InterviewLanguage } from '../services/language'

export type Seniority = 'unknown' | 'junior' | 'mid' | 'senior' | 'lead'
export type Difficulty = 'easy' | 'medium' | 'hard' | 'expert'
export type QuestionType = 'conceptual' | 'situational' | 'failureScenario' | 'followUp'
export type Verdict = 'fail' | 'needsPractice' | 'pass'
export type HubConnectionStatus = 'idle' | 'connecting' | 'connected' | 'reconnecting' | 'disconnected' | 'error'
export type InterviewRecordStatus = 'InProgress' | 'Completed' | 'Aborted'
export type AppView = 'interview' | 'history'

export interface InterviewSessionSummary {
  sessionId: string
  startedAt: string
  endedAt?: string | null
  status: InterviewRecordStatus
  overallScore: number
  overallVerdict: Verdict
  studyGuideId?: string | null
  studyGuideCreatedAt?: string | null
}

export interface WeightedSkill {
  name: string
  weight: number
}

export interface JobProfile {
  role: string
  seniority: Seniority
  requiredSkills: WeightedSkill[]
  niceToHaveSkills: string[]
  responsibilities: string[]
  rawTextHash: string
  profileSource: string
}

export interface InterviewQuestion {
  id: string
  text: string
  skillTarget: string
  difficulty: Difficulty
  type: QuestionType
}

export interface EvaluationPayload {
  overallScore: number
  technicalDepth: number
  productionReadiness: number
  verdict: Verdict
  coveredKeywords: string[]
  missedConcepts: string[]
  strengths: string[]
  improvementAdvice: string
}

export interface InterviewStartResponse {
  sessionId: string
  question: InterviewQuestion
}

export interface SubmitAnswerRequest {
  userId: string
  answerText: string
}

export interface InterviewAnswerResponse {
  feedback: EvaluationPayload
  nextQuestion: InterviewQuestion | null
  interviewCompleted: boolean
  studyGuideMarkdown?: string | null
}

export type MessageRole = 'interviewer' | 'candidate'

export interface ChatMessage {
  id: string
  role: MessageRole
  text: string
  createdAt: string
}

export type InterviewStatus =
  | 'idle'
  | 'parsing'
  | 'review'
  | 'starting'
  | 'asking'
  | 'answering'
  | 'completed'
  | 'error'

export interface InterviewState {
  userId: string
  activeView: AppView
  uiLanguage: UiLanguage
  interviewLanguage: InterviewLanguage
  history: InterviewSessionSummary[]
  historyLoading: boolean
  selectedHistorySessionId: string | null
  historyGuideMarkdown: string | null
  status: InterviewStatus
  profile: JobProfile | null
  sessionId: string
  messages: ChatMessage[]
  feedback: EvaluationPayload | null
  error: string
  busy: boolean
  hubStatus: HubConnectionStatus
  currentQuestion?: InterviewQuestion | null
  studyGuideMarkdown?: string | null
}
