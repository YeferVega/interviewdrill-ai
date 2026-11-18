# Technical Plan: PII Sanitizer in Full Pipeline (.NET 11)

## Architecture Overview
Wire `RegexPiiSanitizer` into all entry points to the LLM and before all persistence in the InterviewDrill AI system.

```
┌─────────────────────────────────────────────────────────┐
│              PII Sanitizer Full Pipeline                 │
├─────────────────────────────────────────────────────────┤
│  Antes de LLM:                                          │
│  ├── JD Parser: Texto del JD antes de LlmJdParserAgent  │
│  ├── Interviewer: Preguntas antes de LLM generation     │
│  ├── Architecture Evaluator: Respuesta antes de juez    │
│  ├── Security & Rigor Judge: Respuesta antes de juez    │
│  └── Study Guide: Texto antes de LLM enriquecimiento    │
│                                                         │
│  Antes de Persistencia:                                 │
│  ├── QuestionRepository: Texto de preguntas             │
│  ├── InterviewAnswerRepository: Respuestas del candidato│
│  ├── SessionStore: Estado de sesión                     │
│  └── StudyGuideRepository: Guía de estudio              │
│                                                         │
│  Antes de Embeddings:                                   │
│  └── Texto antes de generar embeddings semánticos       │
└─────────────────────────────────────────────────────────┘
```

## Components

### 1. RegexPiiSanitizer (`InterviewDrill.Infrastructure.Security/RegexPiiSanitizer.cs`)
**Responsibility:** Implementation of `IPiiSanitizer`. Already implemented, it needs to be wired at every point.

### 2. IPiiSanitizer (`InterviewDrill.Core.Security/IPiiSanitizer.cs`)
**Responsibility:** Interface of the sanitizer. Already defined.

## Integration Points

| Point | File | Action |
|-------|---------|--------|
| JD Parser → LLM | `InterviewDrill.Agents/LlmJdParserAgent.cs` | Sanitize text before `CompleteAsync` |
| Interviewer → LLM | `InterviewDrill.Agents/InterviewerAgent.cs` | Sanitize questions before LLM generation |
| Architecture Evaluator → LLM | `InterviewDrill.Agents/LlmArchitectureJudge.cs` | Sanitize the candidate's answer before `CompleteAsync` |
| Security Judge → LLM | `InterviewDrill.Agents/LlmSecurityJudge.cs` | Sanitize the candidate's answer before `CompleteAsync` |
| Study Guide → LLM | `InterviewDrill.Agents/StudyGuideGenerator.cs` | Sanitize text before LLM enrichment |
| QuestionRepository → DB | `InterviewDrill.Infrastructure.Persistence/Repositories/SqlQuestionRepository.cs` | Sanitize text before saving |
| InterviewAnswerRepository → DB | `InterviewDrill.Infrastructure.Persistence/Repositories/SqlInterviewAnswerRepository.cs` | Sanitize the answer before saving |
| SessionStore → Serialization | `InterviewDrill.Infrastructure.Persistence/EfInterviewerSessionStore.cs` | Sanitize `InterviewerStateJson` before saving |
| StudyGuideRepository → DB | `InterviewDrill.Infrastructure.Persistence/Repositories/SqlStudyGuideRepository.cs` | Sanitize the Markdown before saving |
| Embeddings → Vector store | `InterviewDrill.Infrastructure.Caching/` | Sanitize text before generating embeddings |

## Technology Choices

| Technology | Rationale |
|------------|-----------|
| **C# 15 / .NET 11** | Authoritative stack |
| **RegexPiiSanitizer** | Sanitizer already implemented in Infrastructure.Security |
| **IPiiSanitizer** | Interface in Core.Security |
| **xUnit** | Testing framework |

## File Structure (.NET)

```
backend/
├── src/
│   ├── InterviewDrill.Core.Security/
│   │   ├── IPiiSanitizer.cs
│   │   └── PiiSanitizationResult.cs
│   └── InterviewDrill.Infrastructure.Security/
│       └── RegexPiiSanitizer.cs
└── tests/
    └── InterviewDrill.Agents.Tests/
        └── (PII pipeline integration tests)
```

## Implementation Status

| Integration point | Status | Evidence |
|---------------------|--------|-----------|
| JD Parser before the LLM | ~[~] Partial | Delegated to Spec 006; verify the real implementation |
| Interviewer before the LLM | ❌ Not started | Not clear whether it sanitizes |
| Architecture Evaluator before the LLM | ~[~] Partial | `LlmArchitectureJudge` receives `IPiiSanitizer`; verify usage |
| Security Judge before the LLM | ~[~] Partial | `LlmSecurityJudge` receives `IPiiSanitizer`; verify usage |
| Study Guide before the LLM | ❌ Not started | Not clear whether it sanitizes |
| QuestionRepository before the DB | ~[~] Partial | Mentioned in spec 011; verify the implementation |
| InterviewAnswerRepository before the DB | ~[~] Partial | Mentioned in spec 011; verify the implementation |
| SessionStore before serializing | ~[~] Partial | Mentioned in spec 011; verify the implementation |
| StudyGuideRepository before the DB | ❌ Not started | Not clear whether it sanitizes |
| Before embeddings | ❌ Not started | Not clear whether it sanitizes |

## Testing Strategy

### Unit Tests
- Tests for each pipeline integration point
- Verify that PII is sanitized before sending to the LLM
- Verify that PII is sanitized before persisting
- Tests for detect-only and sanitize modes

### Coverage Target
- > 90% coverage for the PII sanitizer

## Risks

| Risk | Mitigation |
|------|------------|
| PII not sanitized at some point | Exhaustive audit of all entry points to the LLM and persistence |
| False negatives | Conservative patterns, prefer false positives |
| Performance | Compiled regexes, efficient sanitization |
| New endpoints without sanitization | Mandatory code review |
