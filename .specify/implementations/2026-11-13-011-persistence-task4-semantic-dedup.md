# Implementation Record — 011 Persistence & Anti-Repetition Task 4

## Task Reference
- `.specify/tasks/011-persistence-antirepetition-tasks.md`
- **Task 4**: Implement semantic dedup with embeddings and a 0.92 threshold to reject paraphrases of questions already asked.

## Changes Made
- `backend/src/InterviewDrill.Core/Contracts/IEmbeddingGenerator.cs`: added provider-agnostic embedding abstraction for Azure OpenAI, OpenAI, Bedrock, Ollama or fake tests.
- `backend/src/InterviewDrill.Core/Vectors/VectorMath.cs`: added cosine similarity and semantic duplicate calculation in C# (Zero-Math principle). Initial namespace `Math` conflicted with `System.Math` and was renamed to `Vectors`.
- `backend/src/InterviewDrill.Infrastructure/Persistence/IQuestionRepository.cs`: added semantic duplicate existence method accepting `float[] embedding` and configurable threshold defaulting to 0.92.
- `backend/src/InterviewDrill.Infrastructure/Persistence/SqlQuestionRepository.cs`: implemented SQL Server/Azure SQL semantic duplicate check using `EF.Functions.VectorDistance("cosine", ...)`, converting similarity threshold 0.92 to maximum cosine distance `1 - 0.92 = 0.08`.
- `backend/src/InterviewDrill.Infrastructure/Persistence/QuestionRegistry.cs`: integrated embedding generator and semantic check into registry flow:
  - exact duplicate check first
  - generate embedding only if exact miss
  - semantic duplicate check against previous user+JD questions
  - persist embedding with question when registering
- Tests:
  - added `VectorMathTests` for identical, orthogonal, threshold boundary, strict > 0.92 and mismatched length behavior.
  - updated `QuestionRegistryTests` fake repository/generator to validate semantic duplicate rejection and post-exact semantic check.

## Testing
```text
dotnet build backend/InterviewDrill.slnx
Build succeeded.
0 Warning(s)
0 Error(s)

dotnet test backend/InterviewDrill.slnx
InterviewDrill.Core.Tests:              32 passed
InterviewDrill.Agents.Tests:             2 passed
InterviewDrill.Infrastructure.Tests:     8 passed
Total:                                  42/42 passed
```

## Notes
- Real embedding provider remains undecided. The abstraction lets Task 4 logic ship before choosing Azure OpenAI/OpenAI/Bedrock/Ollama.
- Exact dedup runs before semantic dedup to save embedding cost.
- `>=` was deliberately not used; the spec says "similarity > 0.92", so `VectorMath.IsSemanticDuplicate` uses strict greater-than.
- SQL Server/Azure SQL semantic query uses experimental vector APIs already suppressed narrowly in the model configuration.
- End-to-end paraphrase rejection requires real DB and embedding provider; marked task as in progress.

## Review Checklist
- [x] Code follows project standards
- [x] All unit tests pass
- [x] No linting/build errors
- [x] Documentation updated
- [x] Task/status records updated
- [ ] Performance impact assessed with real Azure SQL / AWS SQL Server and provider embeddings
