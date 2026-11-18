# Task List: 008-frontend-ui

## Sprint/Iteration: Phase 2 — Interview Loop (and Phase 4 for the study guide)

### Tasks

#### Task 1: Scaffold base React 19 + Vite + TypeScript + Zustand
- **description**: Set up the frontend project with React 19, Vite, TypeScript, Zustand, and CSS design tokens. Tailwind was discarded for the first cut to reduce dependencies and allow dark/light/system via CSS variables.
- **Priority**: High
- **Estimated Time**: 1 hour
- **Dependencies**: None
- **Status**: [x] Complete

#### Task 2: Install and configure @microsoft/signalr
- **description**: Add the SignalR client for real-time communication with the .NET backend (SignalR hub).
- **Priority**: High
- **Estimated Time**: 1 hour
- **Dependencies**: Task 1
- **Status**: [x] Complete — client in `services/interviewHub.ts` with automatic reconnection.

#### Task 3: Create the home screen with JD upload (PDF/DOCX/TXT)
- **description**: Component to upload a Job Description file or paste text, with size validation (max. 2 MB).
- **Priority**: High
- **Estimated Time**: 4 hours
- **Dependencies**: Task 2, backend JD Parser endpoint (spec 009)
- **Status**: [x] Complete — `JdUploadPage.tsx`.

#### Task 4: Create the JobProfile review and edit screen
- **description**: Let the user review and edit the extracted skills, seniority, and responsibilities before starting the interview.
- **Priority**: Medium
- **Estimated Time**: 3 hours
- **Dependencies**: Task 3
- **Status**: [x] Complete — `ProfileReviewPage.tsx`.

#### Task 5: Create the chat interface for questions and answers (SignalR)
- **description**: Real-time chat where the backend sends questions and the user sends answers. Loading and error states.
- **Priority**: High
- **Estimated Time**: 5 hours
- **Dependencies**: Task 4, SignalR hub in backend (spec 005)
- **Status**: [x] Complete — `InterviewPage.tsx` + `interviewStore.ts`.

#### Task 6: Display structured feedback (EvaluationPayload)
- **description**: Show after each answer: overall score, TechnicalDepth, ProductionReadiness, verdict (PASS/NEEDS_PRACTICE/FAIL), coveredKeywords, missedConcepts, strengths, improvementAdvice.
- **Priority**: High
- **Estimated Time**: 4 hours
- **Dependencies**: Task 5
- **Status**: [x] Complete — `FeedbackPanel.tsx`.

#### Task 7: Interview progress indicator
- **description**: Progress bar for 8–10 questions, current difficulty indicator (easy/medium/hard/expert).
- **Priority**: Medium
- **Estimated Time**: 2 hours
- **Dependencies**: Task 5
- **Status**: [x] Complete — progress and current difficulty displayed.

#### Task 8: Session question-and-answer history
- **description**: Scrollable list of all interactions in the current interview.
- **Priority**: Medium
- **Estimated Time**: 2 hours
- **Dependencies**: Task 5
- **Status**: [x] Complete — scrollable messages.

#### Task 9: Generate and display the detailed study guide
- **description**: At the end of the interview, show the study guide in Markdown with prioritized topics, explanations, resources, and exercises. Export to PDF.
- **Priority**: High
- **Estimated Time**: 4 hours
- **Dependencies**: Task 6, backend Study Guide Generator (spec 010)
- **Status**: [x] Complete — guide visible in UI, Markdown download and PDF export via print-to-PDF.

#### Task 10: Dark/light mode and responsive design
- **description**: Dark/light theme toggle. Responsive design for mobile, tablet, and desktop. WCAG 2.1 AA accessibility.
- **Priority**: Medium
- **Estimated Time**: 3 hours
- **Dependencies**: Tasks 3-9
- **Status**: [~] In progress — system/light/dark toggle and responsive layout implemented; formal WCAG 2.1 AA audit still pending.

#### Task 11: Tests for the main components (Vitest)
- **description**: Tests for the chat components, feedback display, study guide, JD upload.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Tasks 3-9
- **Status**: [~] In progress — tests for utilities, feedback, guide, and date format; chat/JD upload/store tests still pending.

### Definition of Done
- [x] Home screen with JD upload working
- [x] Real-time chat with SignalR
- [x] Structured feedback (EvaluationPayload) visible after each answer
- [x] Study guide generated and exportable to PDF (Markdown + print-to-PDF; server-side endpoint with QuestPDF)
- [~] Responsive and accessible design (WCAG 2.1 AA) — responsive and labels/aria-live added; formal WCAG audit still pending
- [x] Component tests passing
- [x] Successful frontend build
