# Implementation Record — 009 JD Parser Task 1

## Task Reference
- `.specify/tasks/009-jd-parser-agent-tasks.md`
- **Task 1**: Integrate text extraction for JDs (PDF/DOCX/TXT).

## Changes Made
- `backend/src/InterviewDrill.Core/Documents/IJdDocumentTextExtractor.cs`: added provider-agnostic text extraction contract.
- `backend/src/InterviewDrill.Core/Documents/JdDocumentExceptions.cs`: added controlled exceptions for oversize and unsupported format.
- `backend/src/InterviewDrill.Infrastructure/Documents/JdDocumentTextExtractor.cs`: implemented extraction for PDF, DOCX and TXT with 2 MB size guard and text normalization.
- `backend/src/InterviewDrill.Infrastructure/InterviewDrill.Infrastructure.csproj`:
  - removed risky MarkItDown packages discovered during evaluation.
  - added `UglyToad.PdfPig 1.7.0-custom-5` for PDF text extraction.
  - added `DocumentFormat.OpenXml 3.5.1` for DOCX text extraction.
- `backend/tests/InterviewDrill.Infrastructure.Tests/Documents/JdDocumentTextExtractorTests.cs`: added TXT, DOCX, unsupported extension and size-limit tests.

## Testing
```text
dotnet build backend/InterviewDrill.slnx
Build succeeded.
0 Warning(s)
0 Error(s)

dotnet test backend/InterviewDrill.slnx
InterviewDrill.Core.Tests:              32 passed
InterviewDrill.Agents.Tests:             2 passed
InterviewDrill.Infrastructure.Tests:    31 passed
Total:                                  65/65 passed
```

## Notes
- Original spec mentioned MarkItDown. During implementation, both available .NET MarkItDown-style packages pulled dependencies with known security warnings (`Microsoft.AspNetCore.WebUtilities` high vulnerability and `OpenMcdf` moderate vulnerabilities). This conflicts with the project security principle, so the implementation uses narrower libraries instead.
- PDF library is currently prerelease `1.7.0-custom-5`; this should be monitored for stable replacement when available.
- TXT is normalized to LF and trimmed.
- PII sanitization is intentionally not applied in this task; that belongs to 006 PII pipeline integration.
- Unsupported files and oversize files fail before parsing.

## Review Checklist
- [x] Code follows project standards
- [x] All unit tests pass
- [x] No linting/build errors
- [x] Documentation updated
- [x] Security deviation from MarkItDown documented
- [ ] Performance impact assessed for large PDF/DOCX uploads
