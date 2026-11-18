# Implementation Record — 009 JD Parser Task 4

## Task Reference
- `.specify/tasks/009-jd-parser-agent-tasks.md`
- **Task 4**: API endpoint to upload a JD and get a `JobProfile`.

## Changes Made
- `backend/src/InterviewDrill.Api/Program.cs`: replaced weather template with API composition root for JD parsing:
  - `POST /api/jd/parse`
  - `GET /api/health`
  - DI registration for `IPiiSanitizer`, `IJdDocumentTextExtractor`, `ILlmCompletionClient` and `IJdParserAgent`
  - multipart form limit of 2 MB
- `backend/src/InterviewDrill.Infrastructure/Llm/UnavailableLlmCompletionClient.cs`: added deterministic unavailable LLM client so endpoint can operate with heuristic fallback until real provider is configured.
- `backend/tests/InterviewDrill.Api.Tests/...`: added ASP.NET Core integration test project using `WebApplicationFactory<Program>`.
- Tests cover JSON body, TXT multipart upload, unsupported file and empty body.

## API Behavior
- Accepts multipart form field `file` or form field `text`.
- Accepts raw pasted text body.
- Accepts JSON body with `{ "text": "..." }`.
- Returns `JobProfile` on success.
- Returns `400 Bad Request` for empty JD or unsupported file.
- Returns `413 Payload Too Large` when extractor detects a file over 2 MB.
- Currently returns heuristic profile because no real LLM provider is registered.

## Notes
- Security warning found: installed .NET 11 SDK is `11.0.100-rc.3.26522.148`, and test host pulls `Microsoft.AspNetCore.WebUtilities 11.0.0-rc.3.26522.148` with a known high severity vulnerability (`GHSA-x33c-w48q-qrgc`).
- Recommendation: keep .NET 11 target if user installs the stable/patched SDK, or temporarily move production to .NET 10 LTS if the vulnerability must be eliminated now.
- `UnavailableLlmCompletionClient` is intentional placeholder so endpoint and fallback behavior can be tested without secrets/API key.
- Next real provider should replace registration with Azure OpenAI/OpenAI/Bedrock/Ollama adapter.

## Review Checklist
- [x] Code follows project standards
- [x] All tests pass
- [x] No compile errors
- [x] Documentation updated
- [x] Endpoint integration tests added
- [ ] Security vulnerability resolved by SDK/patch or provider downgrade
