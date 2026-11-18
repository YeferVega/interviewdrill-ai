import type { HubConnection } from '@microsoft/signalr'
import { create } from 'zustand'
import {
  getInterviewHistory,
  getStudyGuide,
  parseJobDescription,
  startInterview as startInterviewApi,
  submitAnswer as submitAnswerApi,
} from '../services/interviewApi'
import { getPersistentUserId } from '../services/anonymousUser'
import {
  getStoredInterviewLanguage,
  getStoredUiLanguage,
  setStoredInterviewLanguage,
  setStoredUiLanguage,
  type InterviewLanguage,
} from '../services/language'
import { ui, type UiLanguage } from '../i18n/strings'
import {
  createInterviewHubConnection,
  ensureJoined,
  leaveSession,
} from '../services/interviewHub'
import type {
  AppView,
  ChatMessage,
  EvaluationPayload,
  InterviewQuestion,
  InterviewState,
  JobProfile,
  WeightedSkill,
} from '../types/interview'
import { shouldAddInterviewerQuestion } from '../utils/interview'

interface InterviewActions extends InterviewState {
  setUiLanguage: (language: UiLanguage) => void
  setInterviewLanguage: (language: InterviewLanguage) => void
  parseJd: (payload: { file?: File | null; text?: string }) => Promise<void>
  updateRole: (role: string) => void
  updateSeniority: (seniority: JobProfile['seniority']) => void
  updateRequiredSkill: (index: number, value: Partial<WeightedSkill>) => void
  addRequiredSkill: () => void
  removeRequiredSkill: (index: number) => void
  updateNiceToHaveSkills: (value: string) => void
  updateResponsibility: (index: number, value: string) => void
  addResponsibility: () => void
  removeResponsibility: (index: number) => void
  startInterview: () => Promise<void>
  submitAnswer: (answerText: string) => Promise<void>
  reset: () => Promise<void>
  openView: (view: AppView) => void
  loadHistory: () => Promise<void>
  loadStudyGuide: (sessionId: string) => Promise<void>
}

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof Error && error.message) {
    return error.message
  }

  return fallback
}

function createMessage(role: ChatMessage['role'], text: string): ChatMessage {
  const id = typeof crypto !== 'undefined' && 'randomUUID' in crypto
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random()}`

  return {
    id,
    role,
    text,
    createdAt: new Date().toISOString(),
  }
}

export const useInterviewStore = create<InterviewActions>()((set, get) => {
  let hubConnection: HubConnection | null = null

  function applyProfile(patch: Partial<JobProfile>) {
    const current = get().profile

    if (!current) {
      return
    }

    set({ profile: { ...current, ...patch } })
  }

  function applyQuestion(question: InterviewQuestion) {
    set((state) => {
      if (!shouldAddInterviewerQuestion(state.messages, question)) {
        return {
          currentQuestion: question,
          status: state.busy ? state.status : 'asking',
        }
      }

      return {
        messages: [...state.messages, {
          id: question.id,
          role: 'interviewer',
          text: question.text,
          createdAt: new Date().toISOString(),
        }],
        currentQuestion: question,
        status: state.busy ? state.status : 'asking',
        error: '',
      }
    })
  }

  function applyFeedback(feedback: EvaluationPayload) {
    set({ feedback, error: '' })
  }

  function applyStudyGuide(markdown: string) {
    set({ studyGuideMarkdown: markdown })
  }

  function applyCompleted() {
    set({ status: 'completed', busy: false })
  }

  async function connectHub(): Promise<HubConnection> {
    if (hubConnection) {
      return hubConnection
    }

    hubConnection = await createInterviewHubConnection({
      onQuestion: applyQuestion,
      onFeedback: applyFeedback,
      onStudyGuide: applyStudyGuide,
      onCompleted: applyCompleted,
    })

    const connection = hubConnection

    connection.onreconnecting(() => set({ hubStatus: 'reconnecting' }))
    connection.onreconnected(async () => {
      set({ hubStatus: 'connected' })
      const sessionId = get().sessionId

      if (sessionId) {
        await ensureJoined(connection, sessionId)
      }
    })

    connection.onclose(() => set({ hubStatus: 'disconnected' }))

    set({ hubStatus: 'connecting' })
    return connection
  }

  return {
    userId: getPersistentUserId(),
    activeView: 'interview',
    uiLanguage: getStoredUiLanguage(),
    interviewLanguage: getStoredInterviewLanguage(),
    history: [],
    historyLoading: false,
    selectedHistorySessionId: null,
    historyGuideMarkdown: null,
    status: 'idle',
    profile: null,
    sessionId: '',
    messages: [],
    feedback: null,
    error: '',
    busy: false,
    hubStatus: 'idle',
    currentQuestion: null,
    studyGuideMarkdown: null,

    setUiLanguage: (language) => {
      setStoredUiLanguage(language)
      set({ uiLanguage: language })
    },

    setInterviewLanguage: (language) => {
      setStoredInterviewLanguage(language)
      set({ interviewLanguage: language })
    },

    parseJd: async (payload) => {
      if (!payload.file && !payload.text?.trim()) {
        set({ error: ui(get().uiLanguage).errorJdRequired })
        return
      }

      set({ status: 'parsing', busy: true, error: '' })

      try {
        const profile = await parseJobDescription(payload)
        set({ profile, status: 'review', busy: false })
      } catch (error) {
        set({ status: 'idle', busy: false, error: getErrorMessage(error, ui(get().uiLanguage).errorUnexpected) })
      }
    },

    updateRole: (role) => applyProfile({ role }),
    updateSeniority: (seniority) => applyProfile({ seniority }),
    updateNiceToHaveSkills: (value) => applyProfile({
      niceToHaveSkills: value.split(',').map((item) => item.trim()).filter(Boolean),
    }),
    updateRequiredSkill: (index, value) => {
      const profile = get().profile

      if (!profile) {
        return
      }

      const requiredSkills = profile.requiredSkills.map((skill, currentIndex) =>
        currentIndex === index ? { ...skill, ...value } : skill,
      )

      set({ profile: { ...profile, requiredSkills } })
    },
    addRequiredSkill: () => {
      const profile = get().profile

      if (!profile) {
        return
      }

      set({
        profile: {
          ...profile,
          requiredSkills: [...profile.requiredSkills, { name: '', weight: 0.6 }],
        },
      })
    },
    removeRequiredSkill: (index) => {
      const profile = get().profile

      if (!profile) {
        return
      }

      set({
        profile: {
          ...profile,
          requiredSkills: profile.requiredSkills.filter((_, currentIndex) => currentIndex !== index),
        },
      })
    },
    updateResponsibility: (index, value) => {
      const profile = get().profile

      if (!profile) {
        return
      }

      const responsibilities = profile.responsibilities.map((item, currentIndex) =>
        currentIndex === index ? value : item,
      )

      set({ profile: { ...profile, responsibilities } })
    },
    addResponsibility: () => {
      const profile = get().profile

      if (!profile) {
        return
      }

      set({
        profile: {
          ...profile,
          responsibilities: [...profile.responsibilities, ''],
        },
      })
    },
    removeResponsibility: (index) => {
      const profile = get().profile

      if (!profile) {
        return
      }

      set({
        profile: {
          ...profile,
          responsibilities: profile.responsibilities.filter((_, currentIndex) => currentIndex !== index),
        },
      })
    },

    startInterview: async () => {
      const { profile, userId, interviewLanguage, uiLanguage } = get()

      if (!profile) {
        set({ error: ui(uiLanguage).errorProfileFirst })
        return
      }

      set({ busy: true, status: 'starting', error: '' })

      try {
        const started = await startInterviewApi(userId, profile, interviewLanguage)
        set({ activeView: 'interview', sessionId: started.sessionId, feedback: null, studyGuideMarkdown: null })

        try {
          const connection = await connectHub()
          await ensureJoined(connection, started.sessionId)
          set({ hubStatus: 'connected' })
        } catch (error) {
          const strings = ui(get().uiLanguage)
          set({
            hubStatus: 'error',
            error: `${strings.errorSignalRPrefix} ${getErrorMessage(error, strings.errorUnexpected)} ${strings.errorSignalRSuffix}`,
          })
        }

        applyQuestion(started.question)
        set({ busy: false })
      } catch (error) {
        set({ status: 'review', busy: false, error: getErrorMessage(error, ui(uiLanguage).errorUnexpected) })
      }
    },

    submitAnswer: async (answerText) => {
      const { sessionId, userId, busy, status, interviewLanguage } = get()
      const answer = answerText.trim()

      if (!sessionId || busy || status === 'completed') {
        return
      }

      if (!answer) {
        set({ error: ui(get().uiLanguage).errorAnswerRequired })
        return
      }

      set((state) => ({
        messages: [...state.messages, createMessage('candidate', answer)],
        busy: true,
        status: 'answering',
        error: '',
      }))

      try {
        const result = await submitAnswerApi(sessionId, userId, answer, interviewLanguage)
        applyFeedback(result.feedback)

        if (result.nextQuestion) {
          applyQuestion(result.nextQuestion)
        }

        if (result.studyGuideMarkdown) {
          applyStudyGuide(result.studyGuideMarkdown)
        }

        if (result.interviewCompleted) {
          applyCompleted()
        } else {
          set({ busy: false })
        }
      } catch (error) {
        set({
          status: 'asking',
          busy: false,
          error: getErrorMessage(error, ui(get().uiLanguage).errorUnexpected),
        })
      }
    },

    openView: (view) => {
      set({ activeView: view, error: '' })

      if (view === 'history') {
        void get().loadHistory()
      }
    },

    loadHistory: async () => {
      set({ historyLoading: true, error: '' })

      try {
        const history = await getInterviewHistory(get().userId)
        set({ history, historyLoading: false })
      } catch (error) {
        set({ historyLoading: false, error: getErrorMessage(error, ui(get().uiLanguage).errorUnexpected) })
      }
    },

    loadStudyGuide: async (sessionId) => {
      set({
        selectedHistorySessionId: sessionId,
        historyGuideMarkdown: null,
        busy: true,
        error: '',
      })

      try {
        const markdown = await getStudyGuide(sessionId)
        set({ historyGuideMarkdown: markdown, busy: false })
      } catch (error) {
        set({ historyGuideMarkdown: null, busy: false, error: getErrorMessage(error, ui(get().uiLanguage).errorUnexpected) })
      }
    },

    reset: async () => {
      const { sessionId } = get()

      if (hubConnection && sessionId) {
        try {
          await leaveSession(hubConnection, sessionId)
        } catch {
          // best effort
        }
      }

      hubConnection = null
      set({
        status: 'idle',
        activeView: 'interview',
        profile: null,
        sessionId: '',
        messages: [],
        feedback: null,
        error: '',
        busy: false,
        hubStatus: 'idle',
        currentQuestion: null,
        studyGuideMarkdown: null,
        historyGuideMarkdown: null,
        selectedHistorySessionId: null,
      })
    },
  }
})
