# Technical Plan 018: Testing Coverage Hardening

## Objective
Raise Agents to ≥ 90% and cover Infrastructure with honest coverage of the logic, without touching runtime.

## Approach
1. **Per-class baseline**: `dotnet test <proyecto> --collect:"XPlat Code Coverage"` and parse the `coverage.cobertura.xml` to list classes with `line-rate < 0.7`.
2. **Tests targeted at real gaps**, prioritizing:
   - Agents: `InMemory*` repos (0%), heuristic/fallback generators (0%), `AnchoredRubricMath` (~0.64).
   - Infrastructure: SQL repos (0%) via **EF InMemory**, `HashEmbedding`/`Unavailable`/`Bedrock` edge cases (0%), PII logging (0%), docx extractor branches.
3. **Coverage filter** `backend/coverage.runsettings`: `Exclude` of `*.Migrations*` + `ExcludeByAttribute` of generated code → metric for logic only.
4. **Verification**: full suite + E2E (both modes) + coverage re-measurement.

## Decisions
- **EF InMemory only in tests**: adds `Microsoft.EntityFrameworkCore.InMemory` (Test project only). It does not alter `InterviewDrill.Infrastructure`.
- **Do not change production**: this spec is net-test; the `varbinary`/dedup-in-memory SQL logic was already ported in Phase 3/4.
- **Coverlet artifacts**: a class primary constructor with async state machines can report a low class figure even when the methods are at 100%; it is documented and not "dressed up".

## Work order
1. baseline → 2. Agents tests → 3. Infra tests → 4. runsettings → 5. full suite + E2E → 6. measure → 7. document.

## Outputs (new artifacts)
- `backend/tests/InterviewDrill.Agents.Tests/InMemoryAndHeuristicGeneratorsTests.cs`
- `backend/tests/InterviewDrill.Agents.Tests/AnchoredRubricMathTests.cs`
- `backend/tests/InterviewDrill.Infrastructure.Tests/Llm/HashEmbeddingAndUnavailableLlmTests.cs`
- `backend/tests/InterviewDrill.Infrastructure.Tests/Logging/PiiSanitizingLoggerTests.cs`
- `backend/tests/InterviewDrill.Infrastructure.Tests/Persistence/SqlRepositoriesTests.cs`
- `backend/tests/InterviewDrill.Infrastructure.Tests/Persistence/SqlAnswerAndGuideRepositoriesTests.cs`
- `backend/tests/InterviewDrill.Infrastructure.Tests/Persistence/SqlSessionRepositoryTests.cs`
- `backend/tests/InterviewDrill.Infrastructure.Tests/Persistence/InterviewDrillDbContextFactoryTests.cs`
- `backend/tests/InterviewDrill.Infrastructure.Tests/Documents/JdDocumentTextExtractorTests.cs` (+ cases)
- `backend/coverage.runsettings`

## Risks
- **InMemory ≠ real SQL**: mitigated with the `-UseDatabase` E2E (Phase 4).
- **PDF fixture absent**: left explicit in Out of Scope.
