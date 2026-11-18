# Implementation Record — 006 PII Sanitizer Task 4 Partial Integration

## Task Reference
- `.specify/tasks/006-pii-sanitizer-tasks.md`
- **Task 4**: Integrate PII sanitization before sending to the LLM and before persisting embeddings.

## Changes Made
- `backend/src/InterviewDrill.Infrastructure/Persistence/QuestionRegistry.cs`:
  - added `IPiiSanitizer` dependency.
  - `WasAlreadyAskedAsync` sanitizes question text before exact/semantic duplicate checks.
  - `RegisterAsync` sanitizes question text before:
    - normalized hash calculation
    - embedding generation
    - persisting `QuestionEntity.Text`
- `backend/tests/InterviewDrill.Infrastructure.Tests/Persistence/QuestionRegistryTests.cs`:
  - added test proving raw PII is not persisted and is not sent to embedding generator.
  - updated all registry tests to use the new PII-aware constructor.

## Testing
```text
dotnet build backend/InterviewDrill.slnx
Build succeeded.
0 Error(s)

dotnet test backend/InterviewDrill.slnx
InterviewDrill.Core.Tests:              32 passed
InterviewDrill.Agents.Tests:             2 passed
InterviewDrill.Infrastructure.Tests:    41 passed
InterviewDrill.Api.Tests:                4 passed
Total:                                  79/79 passed
```

## Notes
- JD Parser already sanitized JD before LLM calls.
- Question persistence now sanitizes before embeddings/database persistence.
- Candidate answer persistence is not implemented yet. When Phase 3 integrates answer persistence, `IAnswerRepository` or answer service must call the same `IPiiSanitizer` before persisting answer text/embeddings.
- Two NuGet warnings remain from the installed .NET 11 RC SDK (`Microsoft.AspNetCore.WebUtilities` high severity). This is environment/runtime security, not caused by code.

## Review Checklist
- [x] Code follows project standards
- [x] All tests pass
- [x] No compile errors
- [ ] NuGet security warnings resolved by patched SDK/runtime or stable .NET upgrade
- [x] Documentation updated
- [ ] Performance impact assessed
