import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import type { InterviewQuestion, EvaluationPayload } from '../types/interview'

export interface InterviewHubHandlers {
  onQuestion: (question: InterviewQuestion) => void
  onFeedback: (feedback: EvaluationPayload) => void
  onStudyGuide: (markdown: string) => void
  onCompleted: (sessionId: string) => void
}

export async function createInterviewHubConnection(handlers: InterviewHubHandlers): Promise<HubConnection> {
  const connection = new HubConnectionBuilder()
    .withUrl('/hubs/interview', {
      transport: undefined,
      skipNegotiation: true,
    })
    .configureLogging(LogLevel.Warning)
    .withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
    .build()

  connection.on('QuestionReceived', handlers.onQuestion)
  connection.on('FeedbackReceived', handlers.onFeedback)
  connection.on('StudyGuideReady', handlers.onStudyGuide)
  connection.on('InterviewCompleted', handlers.onCompleted)

  return connection
}

export async function ensureJoined(connection: HubConnection, sessionId: string): Promise<void> {
  if (connection.state !== HubConnectionState.Connected) {
    await connection.start()
  }

  await connection.invoke('JoinSessionAsync', sessionId)
}

export async function leaveSession(connection: HubConnection, sessionId: string): Promise<void> {
  if (connection.state === HubConnectionState.Connected) {
    await connection.invoke('LeaveSessionAsync', sessionId)
  }
}
