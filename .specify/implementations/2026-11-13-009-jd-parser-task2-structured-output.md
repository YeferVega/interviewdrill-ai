# Implementation Record — 009 JD Parser Task 2

## Task Reference
- `.specify/tasks/009-jd-parser-agent-tasks.md`
- **Task 2**: Implement `IJdParserAgent` with structured output (LLM, temperature 0), delegating PII sanitization to Spec 006.

## Changes Made
- `backend/src/InterviewDrill.Core/Llm/ILlmCompletionClient.cs`: added provider-agnostic LLM completion request contract with temperature and optional JSON schema.
- `backend/src/InterviewDrill.Core/Contracts/JdParsingException.cs`: added domain-specific parsing exception.
- `backend/src/InterviewDrill.Infrastructure/JdParserAgent.cs`: implemented `LlmJdParserAgent`.
- `backend/tests/InterviewDrill.Infrastructure.Tests/Agents/LlmJdParserAgentTests.cs`: added tests for structured profile, stable rawText hash, PII masking before LLM call, temperature 0 enforcement, invalid JSON and empty JD rejection.

## Behavior Implemented
- Parses sanitized Job Description text into `JobProfile`:
  - `Role`
  - `Seniority`
  - `RequiredSkills` with weights clamped to 0-1
  - `NiceToHaveSkills`
  - `Responsibilities`
  - `RawTextHash`
  - `ProfileSource = "llm"`
- Computes SHA-256 hash of normalized original JD text before sending sanitized text to the LLM.
- Sends sanitized JD text to LLM with JSON-only structured-output prompt.
- Treats the JD as data and instructs the model not to follow embedded instructions.
- Rejects empty JD, invalid JSON or missing JSON object.

## Testing
```text
dotnet build backend/InterviewDrill.slnx
Build succeeded.
0 Warning(s)
0 Error(s)

dotnet test backend/InterviewDrill.slnx
InterviewDrill.Core.Tests:              32 passed
InterviewDrill.Agents.Tests:             2 passed
InterviewDrill.Infrastructure.Tests:    36 passed
Total:                                  70/70 passed
```

## Notes
- Production LLM provider remains configurable via `ILlmCompletionClient`.
- Fake LLM is used only in tests; API wiring should inject Azure OpenAI/OpenAI/Bedrock/Ollama adapter later.
- PII masking is validated with the real `RegexPiiSanitizer` inside the JD parser tests.

## Review Checklist
- [x] Code follows project standards
- [x] All tests pass
- [x] No linting/build errors
- [x] Documentation updated
- [x] Task status updated
- [ ] Performance impact assessed with real provider latency
