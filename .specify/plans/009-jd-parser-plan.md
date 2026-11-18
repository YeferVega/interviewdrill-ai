# Technical Plan: JD Parser Agent (.NET 11)

## Architecture Overview
The JD Parser Agent converts a Job Description (PDF/DOCX/TXT or pasted text) into a structured `JobProfile`. Implemented in **C# / .NET 11** within `InterviewDrill.Infrastructure` and `InterviewDrill.Agents`. It depends on `InterviewDrill.Core` (contracts) and `InterviewDrill.Core.Security` (PII sanitizer).

```
┌─────────────────────────────────────────────────────────┐
│                   JD Parser Pipeline                    │
├─────────────────────────────────────────────────────────┤
│ 1. Document Upload (PDF/DOCX/TXT, máx. 2 MB)            │
│ 2. Text Extraction (PdfPig / DocumentFormat.OpenXml)    │
│ 3. PII Sanitization (RegexPiiSanitizer antes de LLM)    │
│ 4. LLM Call (structured output, temperature 0)          │
│ 5. rawTextHash Calculation (SHA-256)                    │
│ 6. JobProfile Output (JSON Schema válido)               │
│ 7. Fallback Heurístico (sin LLM)                        │
└─────────────────────────────────────────────────────────┘
```

## Components

### 1. IJdDocumentTextExtractor (`InterviewDrill.Core.Documents/IJdDocumentTextExtractor.cs`)
**Responsibility:** Interface for text extraction from documents.
- `ExtractText(file)` → `string`
- Implementations: `PdfPig` (PDF), `DocumentFormat.OpenXml` (DOCX), direct reading (TXT)

### 2. IJdParserAgent (`InterviewDrill.Core.Contracts/IAgents.cs`)
**Responsibility:** Interface of the JD parser agent.
- `ParseAsync(fileOrText, ct)` → `JobProfile`

### 3. LlmJdParserAgent (`InterviewDrill.Agents/LlmJdParserAgent.cs`)
**Responsibility:** Implementation with an LLM.
- Call to the LLM with strict JSON schema, temperature 0
- Extracts: role, seniority, requiredSkills[] with weight, niceToHaveSkills[], responsibilities[], rawTextHash
- PII sanitization before sending to the LLM (delegated to Spec 006)

### 4. HeuristicJdParserAgent (`InterviewDrill.Agents/HeuristicJdParserAgent.cs`)
**Responsibility:** Fallback without an LLM.
- Keyword-based extraction
- `profileSource: "heuristic"`

### 5. FallbackJdParserAgent (`InterviewDrill.Agents/FallbackJdParserAgent.cs`)
**Responsibility:** Wrapper that combines LLM + heuristic.

## Data Flow

```
Archivo JD (PDF/DOCX/TXT) ──> IJdDocumentTextExtractor ──> Texto plano
                                           │
                                           v
                                  RegexPiiSanitizer (PII antes de LLM)
                                           │
                                           v
                                  LlmJdParserAgent (LLM, temp 0, JSON schema)
                                           │
                                           v
                                  JobProfile (rol, seniority, skills, responsabilidades, rawTextHash)
                                           │
                                           v
                                  POST /api/jd/parse ──> API Response
```

## API Design (C#)

```csharp
// Data Contract
public record JobProfile(
    string Role,
    string Seniority, // enum: junior | mid | senior | lead | unknown
    IReadOnlyList<SkillWeight> RequiredSkills,
    IReadOnlyList<string> NiceToHaveSkills,
    IReadOnlyList<string> Responsibilities,
    string RawTextHash, // SHA-256 del texto normalizado
    string ProfileSource); // "llm" | "heuristic"

public record SkillWeight(string Name, double Weight); // 0-1

// Interface en Core
public interface IJdParserAgent
{
    Task<JobProfile> ParseAsync(string fileOrText, CancellationToken ct = default);
}

// Endpoint API
// POST /api/jd/parse → JobProfile
```

## Technology Choices

| Technology | Rationale |
|------------|-----------|
| **C# 15 / .NET 11** | Authoritative stack |
| **UglyToad.PdfPig** | PDF text extraction (safe, no vulnerabilities) |
| **DocumentFormat.OpenXml** | DOCX text extraction |
| **MarkItDown** | DISCARDED (dependencies with known vulnerabilities) |
| **RegexPiiSanitizer** | PII sanitization before the LLM (Spec 006) |
| **ILlmCompletionClient** | Pluggable LLM provider (Azure OpenAI, Bedrock, Ollama) |
| **xUnit** | Testing framework |

## File Structure (.NET)

```
backend/
├── src/
│   ├── InterviewDrill.Core.Documents/
│   │   ├── IJdDocumentTextExtractor.cs
│   │   └── JobProfile.cs
│   ├── InterviewDrill.Core.Security/
│   │   └── IPiiSanitizer.cs
│   ├── InterviewDrill.Core.Llm/
│   │   └── ILlmCompletionClient.cs
│   ├── InterviewDrill.Core.Contracts/
│   │   └── IAgents.cs (IJdParserAgent)
│   ├── InterviewDrill.Infrastructure.Documents/
│   │   ├── JdDocumentTextExtractor.cs
│   │   ├── PdfPigExtractor.cs
│   │   └── OpenXmlExtractor.cs
│   ├── InterviewDrill.Infrastructure.Security/
│   │   └── RegexPiiSanitizer.cs
│   ├── InterviewDrill.Agents/
│   │   ├── LlmJdParserAgent.cs
│   │   ├── HeuristicJdParserAgent.cs
│   │   └── FallbackJdParserAgent.cs
│   └── InterviewDrill.Api/
│       └── Controllers/JdParseController.cs (POST /api/jd/parse)
└── tests/
    └── InterviewDrill.Infrastructure.Tests/
        └── (JD parser tests)
```

## Implementation Status

| Task | Status | Evidence |
|------|--------|----------|
| PDF/DOCX/TXT text extraction | ✅ Complete | `IJdDocumentTextExtractor`, `JdDocumentTextExtractor`, PdfPig, DocumentFormat.OpenXml |
| IJdParserAgent with structured output | ✅ Complete | `LlmJdParserAgent` with `ILlmCompletionClient`; real provider pending |
| Heuristic fallback | ✅ Complete | `HeuristicJdParserAgent` and `FallbackJdParserAgent` |
| API endpoint /api/jd/parse | ✅ Complete | Implemented with integration tests |
| PII sanitization before the LLM | ~[~] Partial | Delegated to Spec 006 (RegexPiiSanitizer); not duplicated in JD Parser |
| Unit tests | ~[~] In progress | TXT/DOCX and endpoint covered; PDF fixture and golden dataset of 5 JDs pending |
| Stable rawTextHash | ✅ Complete | SHA-256 whitespace-insensitive |

## Testing Strategy

### Unit Tests
- **PDF/DOCX/TXT → text**: Per-format tests (TXT and DOCX covered; PDF fixture pending)
- **JobProfile schema**: Validation against a strict schema
- **rawTextHash**: Stable for the same content (whitespace-insensitive)
- **Heuristic fallback**: Works without an LLM
- **API endpoint**: POST /api/jd/parse returns a valid JobProfile

### Coverage Target
- > 85% coverage

### Golden Dataset
- 5 real JDs for validation (pending)

## Risks

| Risk | Mitigation |
|------|------------|
| PDF with vulnerabilities | Use PdfPig (reviewed), not MarkItDown |
| PII in the JD without sanitization | RegexPiiSanitizer before the LLM (Spec 006) |
| LLM unavailable | Heuristic fallback with profileSource: "heuristic" |
| Real LLM provider not configured | InvalidOperationException with clear instructions |

## Dependencies

- **PII Sanitizer (spec 006)**: RegexPiiSanitizer for prior sanitization
- **Persistence (spec 011)**: For storing the JobProfile
