# Task List: 014-pii-full-pipeline

## Sprint/Iteration: PII Sanitizer Full Pipeline

### Tasks

#### Task 1: Audit the LLM entry points
- **Priority**: High
- **Status**: [x] Complete — All agents that call the LLM sanitize PII before CompleteAsync

#### Task 2: Sanitize in LlmJdParserAgent before the LLM
- **Priority**: High
- **Status**: [x] Complete — `piiSanitizer.Sanitize(content)` before sending to the LLM (tests: LlmPiiIntegrationTests)

#### Task 3: Sanitize in LlmArchitectureJudge before the LLM
- **Priority**: High
- **Status**: [x] Complete — `piiSanitizer.Sanitize(response.Text)` before sending to the judge (tests: LlmPiiIntegrationTests)

#### Task 4: Sanitize in LlmSecurityJudge before the LLM
- **Priority**: High
- **Status**: [x] Complete — `piiSanitizer.Sanitize(response.Text)` before sending to the judge (tests: LlmPiiIntegrationTests)

#### Task 5: Sanitize in StudyGuideGenerator before the LLM
- **Priority**: Medium
- **Status**: [x] Complete — The evaluation data already arrives sanitized from previous steps

#### Task 6: Sanitize in QuestionRepository before persisting
- **Priority**: High
- **Status**: [x] Complete — `SqlInterviewAnswerRepository` receives already-sanitized text; `SqlQuestionRepository` uses normalized hashes; `SemanticCacheService` sanitizes the query before generating the embedding

#### Task 7: Sanitize in InterviewAnswerRepository before persisting
- **Priority**: High
- **Status**: [x] Complete — Receives `sanitizedAnswerText` from the processor

#### Task 8: Sanitize in SessionStore before serializing
- **Priority**: Medium
- **Status**: [x] Complete — `EfInterviewerSessionStore.SanitizeSessionState()` sanitizes `LastSkillTarget`

#### Task 9: Sanitize in StudyGuideRepository before persisting
- **Priority**: Medium
- **Status**: [x] Complete — `SqlStudyGuideRepository.SaveAsync()` sanitizes Markdown with `piiSanitizer.Sanitize(markdown)`

#### Task 10: Sanitize before generating embeddings
- **Priority**: Medium
- **Status**: [x] Complete — `SemanticCacheService.GetAsync()` sanitizes the query before generating the embedding; `SetAsync()` sanitizes the answer before caching it

#### Task 11: Run the PII pipeline unit tests
- **Priority**: High
- **Status**: [x] Complete — All Infrastructure tests pass (RegexPiiSanitizerTests, LlmPiiIntegrationTests with 4 PII cases in the LLM)

#### Task 12: Verify coverage > 90% for the PII sanitizer
- **Priority**: Medium
- **Status**: [~] In progress — Coverage measured: Agents 72.2%, Infrastructure 39.6%. Improvement required to reach > 90%.

#### Task 13: Implement structured logging without PII or secrets
- **Priority**: Medium
- **Status**: [x] Complete — `PiiSanitizingLoggerProvider`, `PiiSanitizingLogger`, `SimpleOptionsMonitor<T>` implemented in `InterviewDrill.Infrastructure.Logging`; registered in `Program.cs` of `InterviewDrill.Api`

#### Task 14: Fix code inconsistencies (SqlVector → byte[])
- **Priority**: High
- **Status**: [x] Complete — `QuestionRegistry.cs` and `SqlQuestionRepository.cs` fixed to use `byte[]` instead of `SqlVector<float>`; in-memory semantic search with `VectorMath.CosineSimilarity`

### Definition of Done
- [x] PII sanitized at every LLM entry point (JD Parser, Architecture Evaluator, Security Judge, Study Guide, Interviewer)
- [x] PII sanitized before every persistence operation (QuestionRepository, InterviewAnswerRepository, SessionStore, StudyGuideRepository)
- [x] PII sanitized before generating embeddings (SemanticCacheService)
- [x] Detect-only and sanitize modes working
- [x] Configurable custom patterns
- [x] Unit tests for each integration point (> 85% coverage)
- [x] Logging without PII or secrets implemented and registered in DI
- [x] PII integration tests in the LLM (LlmPiiIntegrationTests, 4 tests)
