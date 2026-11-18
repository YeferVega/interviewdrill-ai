# Technical Plan: Interviewer Agent (.NET 11)

## Architecture Overview
The Interviewer Agent is the technical host of the interview. Implemented in **C# / .NET 11** within `InterviewDrill.Agents`. It generates questions dynamically from the `JobProfile`, adjusts difficulty according to performance and guarantees anti-repetition.

```
┌─────────────────────────────────────────────────────────┐
│              Interviewer Agent Pipeline                 │
├─────────────────────────────────────────────────────────┤
│ 1. Session Management & History Tracking                │
│ 2. Topic Selection from JobProfile                      │
│ 3. Difficulty Assessment (C# DifficultyController)      │
│ 4. Question Generation (desde JobProfile, no banco estático)│
│ 5. Anti-repetición (exacta + semántica > 0.92)          │
│ 6. Response Delivery (SignalR + REST)                   │
└─────────────────────────────────────────────────────────┘
```

## Components

### 1. InterviewerAgent (`InterviewDrill.Agents/InterviewerAgent.cs`)
**Responsibility:** Main orchestrator of the interview.
- Generates questions from `JobProfile.requiredSkills` + `responsibilities`
- Adjusts difficulty according to the `EvaluationPayload` from the Scoring Synthesizer
- Anti-repetition: verifies against `IQuestionRegistry` (max. 3 regeneration attempts)

### 2. DifficultyController (`InterviewDrill.Agents/DifficultyController.cs`)
**Responsibility:** Difficulty adaptation logic in C# (Zero-Math).
- `score >= 80` (PASS) → raise one level
- `60 <= score < 80` (NEEDS_PRACTICE) → keep level
- `score < 60` (FAIL) → lower one level and run `probe_deeper`
- 2 consecutive FAILs on the same skill → switch skills

### 3. InterviewSkillPlanner (`InterviewDrill.Agents/InterviewSkillPlanner.cs`)
**Responsibility:** Skill coverage planning.
- Covers ≥ 90% of `requiredSkills` in 8-10 questions
- Prioritizes skills by weight and lowest asked count

### 4. FallbackInterviewerQuestionGenerator (`InterviewDrill.Agents/FallbackInterviewerQuestionGenerator.cs`)
**Responsibility:** Minimal emergency question bank without an LLM.
- Demo/offline mode, not the main mode

## Data Flow

```
JobProfile ──> InterviewSkillPlanner ──> Pregunta candidata
                                           │
                                           v
                                  QuestionRegistry (dedup check)
                                           │
                                  ¿Colisión? ──> Regenerar (máx. 3 intentos)
                                           │
                                           v
                                  SignalR / REST ──> Candidato
                                           │
                                           v
                                  Candidato responde ──> Evaluators (paralelo)
                                           │
                                           v
                                  Scoring Synthesizer ──> EvaluationPayload
                                           │
                                           v
                                  DifficultyController ──> Siguiente pregunta
```

## API Design (C#)

```csharp
// Interface en Core
public interface IInterviewerAgent
{
    Task<InterviewQuestion> GenerateQuestionAsync(JobProfile profile, InterviewSession session, CancellationToken ct = default);
    Task<InterviewResponse> ProcessAnswerAsync(string sessionId, string answerText, CancellationToken ct = default);
}

// Difficulty Controller
public sealed class DifficultyController
{
    public DifficultyLevel Adjust(double score, DifficultyLevel current, int consecutiveFails);
}
```

## Technology Choices

| Technology | Rationale |
|------------|-----------|
| **C# 15 / .NET 11** | Authoritative stack, Zero-Math for difficulty and coverage |
| **SignalR** | Real-time communication with the React frontend |
| **ILlmCompletionClient** | Dynamic question generation (pluggable provider) |
| **IQuestionRegistry** | Persistent anti-repetition per user+JD |
| **xUnit** | Testing framework |

## File Structure (.NET)

```
backend/
├── src/
│   └── InterviewDrill.Agents/
│       ├── InterviewerAgent.cs
│       ├── DifficultyController.cs
│       ├── InterviewSkillPlanner.cs
│       ├── FallbackInterviewerQuestionGenerator.cs
│       └── InterviewHub.cs (SignalR, en Api)
└── tests/
    └── InterviewDrill.Agents.Tests/
        └── (tests para interviewer, difficulty, anti-repetición)
```

## Implementation Status

| Task | Status | Evidence |
|------|--------|----------|
| IInterviewerAgent implemented | ✅ Complete | `InterviewerAgent` in `InterviewDrill.Agents` |
| Difficulty Controller | ✅ Complete | `DifficultyController` with tests (4 score cases) |
| Exact + semantic anti-repetition | ~[~] In progress | `IQuestionRegistry` + regeneration max. 3 attempts; real DB pending |
| Coverage ≥ 90% requiredSkills | ~[~] In progress | `InterviewSkillPlanner` prioritizes skills; formal metric guardrail pending |
| Follow-up questions (probe_deeper) | ✅ Complete | FAIL keeps the skill and generates `QuestionType.FollowUp` |
| SignalR hub | ✅ Complete | `InterviewHub`, per-session groups, REST endpoints |
| Heuristic fallback without LLM | ✅ Complete | `HeuristicInterviewerQuestionGenerator` + `FallbackInterviewerQuestionGenerator` |
| Unit tests | ~[~] In progress | Unit/offline e2e green; formal coverage ≥90% pending |

## Testing Strategy

### Unit Tests
- Generation from JobProfile (2 different JDs → different questions)
- Difficulty adaptation (4 cases: PASS, NEEDS_PRACTICE, FAIL, skill change)
- Anti-repetition (exact and semantic > 0.92)
- Skill coverage

### Coverage Target
- > 80% critical paths (formal metric pending)

## Risks

| Risk | Mitigation |
|------|------------|
| Overly hard questions demotivate | Difficulty Controller with gradual descent |
| LLM generates a repeated question | Two-layer dedup + bounded regeneration (max. 3 attempts) |
| Context window overflow | History summarization, only the last N interactions to the LLM |
| No LLM available | Minimal emergency heuristic bank per skill |
