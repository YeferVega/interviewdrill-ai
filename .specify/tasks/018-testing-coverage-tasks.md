# Task List: 018-testing-coverage

## Sprint/Iteration: Phase 5 — Coverage Hardening (Agents + Infrastructure)

### Tasks

#### Task 1: Coverage baseline per class
- **Description**: Run `dotnet test` for Agents and Infrastructure with `--collect:"XPlat Code Coverage"` and list classes with `line-rate < 0.7` to prioritize them.
- **Priority**: High
- **Estimated Time**: 0.5 hours
- **Dependencies**: None
- **Status**: [x] Complete — Baseline captured: Agents 72.2%, Infrastructure 39.6%. Gaps identified (InMemory* 0%, Sql* 0%, logging/Bedrock/HashEmbedding 0%, AnchoredRubricMath 0.64).

#### Task 2: Tests for in-memory repos and generators (Agents)
- **Description**: Cover `InMemoryQuestionRegistry` (dedup + normalization + isolation per user/jd), `InMemoryInterviewAnswerRepository`, `InMemoryStudyGuideRepository`, `InMemorySessionHistoryRepository`, `HeuristicInterviewerQuestionGenerator` (all templates), and `FallbackInterviewerQuestionGenerator` (success / fallback on `LlmUnavailableException` and `InterviewQuestionParsingException` / propagation of fatal errors).
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Task 1
- **Status**: [x] Complete — `InMemoryAndHeuristicGeneratorsTests.cs` created and green.

#### Task 3: Tests for AnchoredRubricMath (Agents)
- **Description**: `Score1To5` (range/NaN/Infinity), `Normalize` (linear mapping 1→0,5→100), `WeightedNormalized` (empty, weighted, clamp), `CombineDeterministicAndJudge` (weights 0/0.45/1 and clamping).
- **Priority**: High
- **Estimated Time**: 1 hour
- **Dependencies**: Task 1
- **Status**: [x] Complete — `AnchoredRubricMathTests.cs` created and green.

#### Task 4: Tests for SQL repos with EF InMemory (Infrastructure)
- **Description**: Add `Microsoft.EntityFrameworkCore.InMemory` (test only). Cover `SqlQuestionRepository` (hash + in-memory semantic dedup over `byte[]`), `SqlInterviewAnswerRepository` (serialization, per-session scope, invalid payload), `SqlStudyGuideRepository` (sanitizes PII + update preserves id), `SqlSessionRepository` (GetState stats + SetCompleted), `EfInterviewerSessionStore` (materializes the User→JD→Session aggregate, sanitizes PII, load/MarkCompleted).
- **Priority**: High
- **Estimated Time**: 4 hours
- **Dependencies**: Tasks 1, and the EF InMemory package
- **Status**: [x] Complete — `SqlRepositoriesTests.cs`, `SqlAnswerAndGuideRepositoriesTests.cs`, `SqlSessionRepositoryTests.cs`, and `InterviewDrillDbContextFactoryTests.cs` created and green.

#### Task 5: Tests for LLM edges and PII logging (Infrastructure)
- **Description**: `HashEmbeddingGenerator` (dimension, norm=1, determinism, null, cancellation), `UnavailableLlmCompletionClient`, `BedrockLlmClient` (empty ModelId and SigV4 NotImplemented), `PiiSanitizingLoggerProvider/Logger` (sanitizes email/phone/IP, BeginScope delegation, IsEnabled, disabled level), `SimpleOptionsMonitor<T>`.
- **Priority**: Medium
- **Estimated Time**: 2 hours
- **Dependencies**: Task 1
- **Status**: [x] Complete — `HashEmbeddingAndUnavailableLlmTests.cs` and `PiiSanitizingLoggerTests.cs` created and green.

#### Task 6: Extend JdDocumentTextExtractor (branches)
- **Description**: null content, uppercase extension, invalid PDF → `JdDocumentException`, empty TXT.
- **Priority**: Low
- **Estimated Time**: 0.5 hours
- **Dependencies**: Task 1
- **Status**: [x] Complete — cases added to `JdDocumentTextExtractorTests.cs`.

#### Task 7: Coverage filter to measure logic (exclude generated)
- **Description**: Create `backend/coverage.runsettings` with `Exclude` for migrations and `ExcludeByAttribute` for generated attributes, so the metric ignores EF-generated code and primary-constructor artifacts.
- **Priority**: Medium
- **Estimated Time**: 0.5 hours
- **Dependencies**: Tasks 2-6
- **Status**: [x] Complete — file created; Agents ~90.8%, Infrastructure ~71.6% (logic).

#### Task 8: Final verification with no regression
- **Description**: Full suite + E2E in both modes (in-memory and `-UseDatabase`) + build.
- **Priority**: High
- **Estimated Time**: 0.5 hours
- **Dependencies**: Tasks 2-7
- **Status**: [x] Complete — **238/238** (Core 35, Agents 81, Infrastructure 110, Api 12), E2E OK anti-repetition, build 0 errors.

### Definition of Done
- [x] Gaps detected by the baseline and covered with targeted tests
- [x] In-memory repos and generators (Agents) at 100% of their main paths
- [x] AnchoredRubricMath covered (rubrics + combination)
- [x] SQL repos + EfInterviewerSessionStore covered with EF InMemory (no network/SQL Server)
- [x] LLM edges + PII logging covered
- [x] Coverage filter applied; honest logic metric ≥ ~90% Agents / ~71% Infra
- [x] Suite 238/238, E2E in two modes, build 0 errors
- [x] No changes to production code in this spec
