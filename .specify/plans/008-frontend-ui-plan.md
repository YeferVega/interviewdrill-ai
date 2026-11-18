# Technical Plan: Frontend UI - Interview Interface (React 19)

## Architecture Overview
The user interface follows a **Component-Based SPA** pattern with **React 19**, TypeScript, Vite, Zustand and SignalR client. Implemented in `frontend/`.

```
┌─────────────────────────────────────────────────────────┐
│              Frontend Architecture                      │
├─────────────────────────────────────────────────────────┤
│ Presentation Layer (React Components)                   │
│ - JdUploadPage (subida de JD)                           │
│ - ProfileReviewPage (edición de JobProfile)             │
│ - InterviewPage (chat en tiempo real)                   │
│ - FeedbackPanel (visualización EvaluationPayload)       │
│ - SessionHistory (historial Q&A)                        │
│ - StudyGuidePage (guía de estudio)                      │
│ - HistoryPage (historial de sesiones)                   │
├─────────────────────────────────────────────────────────┤
│ State Management Layer (Zustand)                        │
│ - interviewStore (estado de entrevista)                 │
│ - uiStore (tema, preferencias)                          │
├─────────────────────────────────────────────────────────┤
│ Data Layer (SignalR + REST)                             │
│ - interviewHub.ts (cliente SignalR)                     │
│ - API clients (REST endpoints)                          │
├─────────────────────────────────────────────────────────┤
│ Styling Layer (CSS Variables + Design Tokens)           │
│ - Dark/Light/System theme                               │
│ - Responsive design (mobile, tablet, desktop)           │
└─────────────────────────────────────────────────────────┘
```

## Components

### 1. JdUploadPage (`frontend/src/pages/JdUploadPage.tsx`)
**Responsibility:** Landing screen with JD upload (PDF/DOCX/TXT) or pasted text. Size validation (max. 2 MB).

### 2. ProfileReviewPage (`frontend/src/pages/ProfileReviewPage.tsx`)
**Responsibility:** Review and manual editing of the extracted JobProfile (skills, seniority, responsibilities).

### 3. InterviewPage (`frontend/src/pages/InterviewPage.tsx`)
**Responsibility:** Real-time chat with SignalR. Backend sends questions, user sends answers. Loading and error states.

### 4. FeedbackPanel (`frontend/src/components/FeedbackPanel.tsx`)
**Responsibility:** Display of structured feedback after each answer.
- Overall score, TechnicalDepth, ProductionReadiness
- Verdict (PASS/NEEDS_PRACTICE/FAIL) with colors
- coveredKeywords, missedConcepts, strengths, improvementAdvice

### 5. StudyGuidePage (`frontend/src/pages/StudyGuidePage.tsx`)
**Responsibility:** Study guide in Markdown with prioritized topics. Markdown download and PDF export via print-to-PDF.

### 6. HistoryPage (`frontend/src/pages/HistoryPage.tsx`)
**Responsibility:** Session history with verdicts and per-session guide access.

## Data Flow

```
Usuario sube JD ──> POST /api/jd/parse ──> JobProfile
JobProfile ──> Revisión/edición ──> POST /api/interview/sessions ──> SessionId + Primera pregunta
Pregunta ──> SignalR ──> Usuario responde ──> POST /api/interview/sessions/{id}/answers
Respuesta ──> Evaluators (paralelo) ──> Scoring Synthesizer ──> EvaluationPayload
EvaluationPayload ──> SignalR ──> FeedbackPanel
Fin de entrevista ──> Study Guide Generator ──> Markdown + PDF
```

## API Design (TypeScript)

```typescript
// SignalR Hub Client
interface InterviewHubClient {
  ReceiveQuestion: (question: InterviewQuestion) => void;
  ReceiveFeedback: (feedback: EvaluationPayload) => void;
  ReceiveStudyGuide: (guide: string) => void;
}

// Zustand Store
interface InterviewState {
  sessionId: string | null;
  currentQuestion: InterviewQuestion | null;
  feedback: EvaluationPayload | null;
  history: QuestionResponse[];
  isLoading: boolean;
  startSession: (profile: JobProfile) => Promise<void>;
  submitResponse: (answerText: string) => Promise<void>;
}
```

## Technology Choices

| Technology | Rationale |
|------------|-----------|
| **React 19** | Main framework (updated from React 18 in the original plan) |
| **TypeScript** | Type safety, better DX |
| **Vite** | Fast build tool, HMR |
| **Zustand** | Lightweight state management (no React Query, no Tailwind) |
| **@microsoft/signalr** | SignalR client for real-time communication with the .NET backend |
| **CSS Variables** | Styling with design tokens (Tailwind was discarded) |
| **Vitest** | Testing framework |

## File Structure

```
frontend/
├── src/
│   ├── pages/
│   │   ├── JdUploadPage.tsx
│   │   ├── ProfileReviewPage.tsx
│   │   ├── InterviewPage.tsx
│   │   ├── StudyGuidePage.tsx
│   │   └── HistoryPage.tsx
│   ├── components/
│   │   ├── FeedbackPanel.tsx
│   │   └── ...
│   ├── services/
│   │   ├── interviewHub.ts
│   │   └── api.ts
│   ├── stores/
│   │   ├── interviewStore.ts
│   │   └── uiStore.ts
│   └── App.tsx
├── tests/
│   └── (Vitest component tests)
├── index.html
├── package.json
├── tsconfig.json
├── vite.config.ts
└── postcss.config.css
```

## Implementation Status

| Task | Status | Evidence |
|------|--------|----------|
| Scaffold React 19 + Vite + TS + Zustand | ✅ Complete | `frontend/` build OK |
| SignalR client | ✅ Complete | `services/interviewHub.ts` with automatic reconnection |
| Landing screen with JD upload | ✅ Complete | `JdUploadPage.tsx` |
| JobProfile review and editing | ✅ Complete | `ProfileReviewPage.tsx` |
| Chat interface with SignalR | ✅ Complete | `InterviewPage.tsx` + `interviewStore.ts` |
| Structured feedback (EvaluationPayload) | ✅ Complete | `FeedbackPanel.tsx` |
| Progress indicator | ✅ Complete | Progress and current difficulty shown |
| Question and answer history | ✅ Complete | Scrollable messages |
| Detailed study guide | ✅ Complete | Markdown download + print-to-PDF |
| Dark/light mode and responsive | ~[~] In progress | Toggle implemented; WCAG 2.1 AA audit pending |
| Component tests (Vitest) | ~[~] In progress | Initial tests covered; chat/store pending |
| Error boundaries | ❌ Not started | Not implemented |
| Accessibility audit | ❌ Not started | Pending |

## Testing Strategy

### Component Tests (Vitest)
- **JdUploadPage**: File upload, size validation
- **ProfileReviewPage**: Editing of skills, seniority
- **InterviewPage**: Question display, answer submission
- **FeedbackPanel**: Score display, verdicts, colors
- **StudyGuidePage**: Guide display, Markdown download

### Coverage Target
- Initial tests covered; chat and store pending

### Performance Target
- First Contentful Paint < 1.5s
- UI responds in < 100ms
- Feedback per answer P95 < 5s

## Risks

| Risk | Mitigation |
|------|------------|
| Session state complexity | Zustand for centralized state management |
| Accessibility hard to implement | Use accessible components from the start |
| Performance on mobile devices | Code splitting, lazy loading |
| Intermittent SignalR connection | Automatic reconnection, message buffering |
