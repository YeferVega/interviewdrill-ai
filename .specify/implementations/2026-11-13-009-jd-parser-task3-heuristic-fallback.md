# Implementation Record — 009 JD Parser Task 3

## Task Reference
- `.specify/tasks/009-jd-parser-agent-tasks.md`
- **Task 3**: Implement heuristic fallback without LLM and mark `ProfileSource = "heuristic"`.

## Changes Made
- `backend/src/InterviewDrill.Core/Llm/LlmUnavailableException.cs`: added controlled exception for unavailable LLM provider.
- `backend/src/InterviewDrill.Infrastructure/JdTextHashing.cs`: extracted shared raw JD hash calculation used by both LLM and heuristic parsers.
- `backend/src/InterviewDrill.Infrastructure/HeuristicJdParserAgent.cs`: added keyword-based Job Description parser for role, seniority, required skills, nice-to-have skills and responsibilities.
- `backend/src/InterviewDrill.Infrastructure/FallbackJdParserAgent.cs`: added composite parser that falls back to heuristic parser when LLM is unavailable or produces invalid parseable JSON.
- `backend/tests/InterviewDrill.Infrastructure.Tests/Agents/HeuristicJdParserAgentTests.cs`: added fallback parser and composite parser tests.

## Behavior Implemented
- Heuristic parser sanitizes PII before extracting fields.
- Computes the same `RawTextHash` as the LLM parser.
- Sets `ProfileSource = "heuristic"`.
- Recognizes common skills:
  - RAG
  - SemanticKernel
  - HNSW
  - Kafka
  - Observability
  - Polly
  - React
  - TypeScript
  - EF Core
  - PostgreSQL
  - SQL Server
  - Azure
  - AWS
  - CI/CD
  - CloudFoundry
  - Kubernetes
  - Docker
  - gRPC
- Separates required skills from nice-to-have using contextual line markers.
- `FallbackJdParserAgent` catches:
  - `LlmUnavailableException`
  - `JdParsingException`

## Testing
```text
dotnet build backend/InterviewDrill.slnx
Build succeeded.
0 Warning(s)
0 Error(s)

dotnet test backend/InterviewDrill.slnx
InterviewDrill.Core.Tests:              32 passed
InterviewDrill.Agents.Tests:             2 passed
InterviewDrill.Infrastructure.Tests:    40 passed
Total:                                  74/74 passed
```

## Notes
- Heuristic mode is intentionally a demo/offline fallback, not the primary parser quality path.
- Skill weights are approximate; LLM structured parsing remains the preferred path when provider is available.
- A future real LLM adapter can be wrapped in `FallbackJdParserAgent(heuristicParser)` for offline resilience.

## Review Checklist
- [x] Code follows project standards
- [x] All tests pass
- [x] No linting/build errors
- [x] Documentation updated
- [x] Task status updated
- [ ] Performance impact assessed
