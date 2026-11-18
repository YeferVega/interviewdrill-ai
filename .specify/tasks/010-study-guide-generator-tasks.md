# Task List: 010-study-guide-generator

## Sprint/Iteration: Phase 4 — Study Guide & History

### Tasks

#### Task 1: Implement IStudyGuideGenerator in InterviewDrill.Agents
- **Description**: Concrete class that receives the session's EvaluationPayload[] + JobProfile and generates the study guide in Markdown.
- **Priority**: High
- **Estimated Time**: 4 hours
- **Dependencies**: Spec 002 (Scoring Synthesizer), Spec 009 (JobProfile weights), Spec 011 (Persistence)
- **Status**: [x] Complete — `StudyGuideGenerator` implemented.

#### Task 2: Compute priority in C# (Zero-Math)
- **Description**: Formula in code: priority(skill) = failureFrequency × jdWeight × (1 - avgScore/100). Never delegate it to the LLM.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Task 1, Core scoring utilities
- **Status**: [x] Complete — calculation in `StudyGuideGenerator`.

#### Task 3: Group failed concepts by topic
- **Description**: Group missedConcepts and antiPatterns by topic (e.g. "Resilience" groups Polly + rate limiting + circuit breaker).
- **Priority**: Medium
- **Estimated Time**: 2 hours
- **Dependencies**: Task 2
- **Status**: [x] Complete — thematic grouping by keywords.

#### Task 4: Generate per-topic explanation with the LLM (temp 0.3)
- **Description**: For each prioritized topic (max. 7), the LLM generates: a 2-4 paragraph explanation, relevance to the JD role, 2-3 resources, 1 exercise with success criteria, time estimate.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Task 3, structured output LLM
- **Status**: [~] In progress — deterministic explanations/resources/exercises complete; LLM enrichment and temp 0.3 still pending.

#### Task 5: Include the "What you already master" section (strengths)
- **Description**: Consolidate strengths from all of the session's evaluations and show a strengths section.
- **Priority**: Medium
- **Estimated Time**: 1 hour
- **Dependencies**: Task 1
- **Status**: [x] Complete — consolidated strengths section.

#### Task 6: Export Markdown + PDF
- **Description**: Export the study guide to Markdown and PDF. Persist it in the session history.
- **Priority**: Medium
- **Estimated Time**: 3 hours
- **Dependencies**: Task 4, Task 5
- **Status**: [x] Complete — guide persisted, Markdown download, print-to-PDF, and server-side PDF endpoint with QuestPDF.

#### Task 7: Tests with golden dataset (3 simulated sessions)
- **description**: Tests that validate the generated guide matches the expected guides for 3 sample sessions.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Tasks 1-6
- **Status**: [~] In progress — tests for prioritization, the 7-topic limit, and no missed concepts; formal golden dataset still pending.

### Definition of Done
- [x] Priority computed in C# with tests (not delegated to the LLM)
- [x] Topics ordered by descending impact
- [x] Each topic includes: explanation, relevance to the JD, resources, exercise, time
- [x] Strengths section present
- [~] Markdown + PDF export working (Markdown + print-to-PDF; server-side endpoint with QuestPDF implemented)
- [~] Unit tests passing (> 85% coverage) — prioritization and 7-topic limit tested; formal golden dataset still pending
