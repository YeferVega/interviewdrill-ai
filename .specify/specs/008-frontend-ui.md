# Specification: Frontend UI - Interview Interface

## Overview
The React user interface for the technical interview simulator that lets candidates upload a Job Description, interact with the Interviewer Agent in real time, view questions, submit answers, receive structured feedback (EvaluationPayload), and generate a detailed study guide at the end.

## Stack
- **React 19 + TypeScript + Vite**
- **Zustand**: Centralized state management
- **@microsoft/signalr**: SignalR client for real-time communication with the .NET backend
- **CSS design tokens + variables**: Styling (Tailwind was discarded to reduce dependencies in the first cut).

## Requirements

### Functional Requirements
1. Start screen with Job Description upload (PDF/DOCX/TXT) or pasted text
2. Manual review and editing of the extracted JobProfile (skills, seniority, responsibilities)
3. Chat interface for real-time questions and answers (SignalR)
4. Visualization of structured feedback (score, verdict, advice) after each answer
5. Interview progress indicator (8–10 questions)
6. History of the session's questions and answers
7. Visualization of strengths and areas for improvement
8. Generation and visualization of a detailed study guide at the end
9. Support for dark/light mode
10. Responsive design (mobile, tablet, desktop)
11. Accessibility (WCAG 2.1 AA)
12. Loading states and error handling

### Non-Functional Requirements
1. **Performance**: First Contentful Paint < 1.5s
2. **Responsiveness**: UI responds in < 100ms
3. **Real-time**: Feedback per answer P95 < 5s (includes LLM)
4. **Accessibility**: Screen reader support, keyboard navigation
5. **Browser support**: Chrome, Firefox, Safari, Edge (last 2 versions)

## User Stories
- as a **candidate**, I want to upload a Job Description and practice with questions specific to that position
- as a **candidate**, I want a clear and intuitive interface so I can focus on the questions
- as a **candidate**, I want to see my progress and immediate feedback to understand my performance
- as a **candidate**, I want the questions to be new when I retake the interview (anti-repetition)
- as a **candidate**, I want a detailed study guide at the end so I know what to study
- as a **user**, I want the interface to be accessible and to work on all my devices

## Acceptance Criteria
- [x] Start screen with JD upload and pasted text
- [x] Review and editing of the JobProfile before starting
- [x] Functional chat interface with SignalR (real time)
- [x] Answer input with validation
- [x] Display of the structured EvaluationPayload (score, verdict, advice)
- [x] Score visualization with a progress bar (TechnicalDepth and ProductionReadiness)
- [x] Verdict display with colors (green/yellow/red)
- [x] Listing of coveredKeywords and missedConcepts
- [~] Strengths display (shown in feedback; additional visual listing pending)
- [x] ImprovementAdvice visible
- [x] Scrollable session history
- [x] Interview history view with sessions, verdicts, and per-session access to the guide
- [x] Generation and display of a detailed study guide (Markdown/PDF via print)
- [x] Dark/light mode toggle (system/light/dark)
- [x] Responsive on mobile (< 768px)
- [x] Responsive on tablet (768px - 1024px)
- [x] Responsive on desktop (> 1024px)
- [x] Full keyboard navigation via native controls
- [x] Screen reader labels and aria-live on feedback/status
- [~] Loading spinners during evaluations (currently status labels and disabled buttons)
- [ ] Error boundaries with clear messages
- [~] Tests for the main components (Vitest) — initial tests covered; chat/store pending
- [ ] Accessibility audit passes

## Out of Scope
- Enterprise/SSO user authentication (MVP uses an anonymous userId)
- Voice/video interviews, avatar
- Coding mode with execution sandbox
- Multi-language (Spanish first; English later)

## Dependencies
- **React 19**: Main framework
- **TypeScript**: Type safety
- **Vite**: Build tool
- **CSS design tokens + variables**: Styling (Tailwind was discarded to reduce dependencies in the first cut).
- **Zustand**: State management
- **@microsoft/signalr**: Client for real-time communication with the .NET backend

## Risks
1. **Risk**: Session state complexity
   - **Mitigation**: Zustand for centralized state management

2. **Risk**: Accessibility hard to implement
   - **Mitigation**: Use accessible components from the start

3. **Risk**: Performance on mobile devices
   - **Mitigation**: Code splitting, lazy loading

4. **Risk**: Intermittent SignalR connection
   - **Mitigation**: Automatic reconnection, message buffering
