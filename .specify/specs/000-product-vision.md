# Specification 000: Product Vision — InterviewDrill AI

## Overview
Web platform that simulates personalized technical interviews based on a Job Description (JD). The user uploads the JD for the position they are aiming at, the system conducts an adaptive interview with specialized agents, gives immediate feedback per response and, at the end, generates a detailed study guide. If the user retakes the interview for the same JD, they receive completely new questions.

## Stack (authoritative)
- **Backend:** .NET 11 / C# 15, ASP.NET Core, SignalR
- **Agents:** Microsoft Agent Framework / Semantic Kernel
- **Persistence:** EF Core (SQL Server/Azure SQL with native vector support) + Redis
- **Frontend:** React 19 + TypeScript + Vite + Zustand + SignalR client

## User Flow (end-to-end)
1. **Upload JD** → user uploads PDF/DOCX/TXT or pastes text.
2. **Parse** → JD Parser Agent produces `JobProfile` (role, seniority, skills, responsibilities).
3. **Confirm** → user reviews/edits the detected skills.
4. **Interview** → Interviewer Agent generates questions from the `JobProfile`; adaptive difficulty; immediate feedback after each response (EvaluationPayload).
5. **Study Guide** → on closing, Study Guide Generator emits a prioritized guide.
6. **Retake** → same position, 100% new questions (exact + semantic persistent deduplication).

## Actors
- **Candidate**: practices for a specific position.
- **System**: orchestrates agents, persists history, guarantees no-repetition.

## Success Metrics
- Feedback per response P95 < 5s (includes LLM).
- 0 repeated questions (similarity > 0.92) for the same user+JD.
- Study guide generated in < 10s on close.
- JD skills coverage ≥ 90% per 8–10 question interview.

## Scope of this foundation (MVP)
- Complete flow with 1 user (no multi-tenant auth in MVP; anonymous persistent `userId` per browser).
- Interview in text mode (voice is out of MVP scope).
- 8 questions per session, difficulty easy→expert.

## Out of Scope (MVP)
- Voice/video interviews, avatar.
- Coding mode with execution sandbox.
- Multi-language (Spanish first; English later).
- Enterprise/SSO authentication.

## Dependencies
- All specs 001–011.
