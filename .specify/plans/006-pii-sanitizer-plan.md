# Technical Plan: PII Sanitizer Module (.NET 11)

## Architecture Overview
The PII Sanitizer is a critical security module that detects and sanitizes personally identifiable information (PII) before sending it to LLMs or vectorization systems. Implemented in **C# / .NET 11** within `InterviewDrill.Infrastructure`.

```
┌─────────────────────────────────────────────────────────┐
│               PII Sanitizer Pipeline                    │
├─────────────────────────────────────────────────────────┤
│ 1. Input Text Normalization                             │
│ 2. Pattern Detection (regex compilados)                 │
│ 3. PII Classification & Position Tracking               │
│ 4. Masking & Replacement ([EMAIL], [PHONE], etc.)       │
│ 5. Output Generation (sanitized + findings)             │
└─────────────────────────────────────────────────────────┘
```

## Components

### 1. RegexPiiSanitizer (`InterviewDrill.Infrastructure.Security/RegexPiiSanitizer.cs`)
**Responsibility:** Implements `IPiiSanitizer`. Detects and masks PII with compiled regexes.
- Emails, phone numbers, SSNs, credit cards, IPs (IPv4/IPv6), names, physical addresses
- Detect-only and sanitize modes
- Configurable custom patterns

### 2. IPiiSanitizer (`InterviewDrill.Core.Security/IPiiSanitizer.cs`)
**Responsibility:** Sanitizer interface in Core.
- `Sanitize(string? input, bool mask = true)` → `PiiSanitizationResult`
- `AddPattern(PiiPatternDefinition definition)`

## Data Flow

```
Input Text ──> RegexPiiSanitizer.Sanitize()
                    │
                    v
          PiiSanitizationResult (Texto sanitizado + Findings)
                    │
         ┌──────────┴──────────┐
         v                     v
   Antes de LLM           Antes de persistir
   (JD Parser, Interviewer)  (EF Core, QuestionRegistry, Answers)
```

## API Design (C#)

```csharp
// Interface en Core
public interface IPiiSanitizer
{
    PiiSanitizationResult Sanitize(string? input, bool mask = true);
    void AddPattern(PiiPatternDefinition definition);
}

// Implementation en Infrastructure
public sealed class RegexPiiSanitizer : IPiiSanitizer
{
    public PiiSanitizationResult Sanitize(string? input, bool mask = true);
    public void AddPattern(PiiPatternDefinition definition);
}
```

## Technology Choices

| Technology | Rationale |
|------------|-----------|
| **C# 15 / .NET 11** | Authoritative stack, compiled regexes for performance |
| **System.Text.RegularExpressions** | Native pattern matching, compiled options |
| **xUnit** | Testing framework |

## File Structure (.NET)

```
backend/
├── src/
│   ├── InterviewDrill.Core.Security/
│   │   ├── IPiiSanitizer.cs
│   │   ├── PiiPatternDefinition.cs
│   │   ├── PiiFinding.cs
│   │   └── PiiSanitizationResult.cs
│   └── InterviewDrill.Infrastructure.Security/
│       └── RegexPiiSanitizer.cs
└── tests/
    └── InterviewDrill.Infrastructure.Tests/
        └── (PII sanitizer tests)
```

## Implementation Status

| Task | Status | Evidence |
|------|--------|----------|
| RegexPiiSanitizer implemented | ✅ Complete | `RegexPiiSanitizer` in `InterviewDrill.Infrastructure.Security` |
| Masking with placeholders | ✅ Complete | `mask: true/false`, descriptive placeholders |
| Custom patterns | ✅ Complete | `AddPattern` implemented and tested |
| Integrate before LLM and persistence | ~[~] Partial | JD Parser, QuestionRegistry, InterviewAnswerProcessor sanitize; candidate answers pending |
| Unit tests | ~[~] In progress | PII types, detect-only, masking, custom patterns covered; formal coverage pending |
| Performance (< 100ms for 10KB) | ❌ Not started | No performance tests |

## Testing Strategy

### Unit Tests
- **EmailDetector**: Multiple email formats
- **PhoneDetector**: International and national formats
- **SSNDetector**: xxx-xx-xxxx pattern
- **CreditCardDetector**: Visa, MasterCard, Amex with Luhn validation
- **IPDetector**: IPv4 and IPv6
- **NameDetector**: Common name patterns
- **AddressDetector**: Physical addresses

### Coverage Target
- > 90% coverage

### Performance Target
- < 100ms for texts up to 10KB

## Risks

| Risk | Mitigation |
|------|------------|
| False negatives (PII not detected) | Conservative patterns, prefer false positives |
| Performance degrades with long texts | Chunked processing if necessary |
| Outdated patterns | Keep the pattern library up to date |
