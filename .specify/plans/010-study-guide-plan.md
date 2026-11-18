# Technical Plan: Study Guide Generator Agent (.NET 11)

## Architecture Overview
The Study Guide Generator consolidates all the evaluations of the session and produces a detailed, prioritized study guide. Implemented in **C# / .NET 11** within `InterviewDrill.Agents`.

```
┌─────────────────────────────────────────────────────────┐
│            Study Guide Generator Pipeline               │
├─────────────────────────────────────────────────────────┤
│ 1. Recibir historial: EvaluationPayload[] + JobProfile  │
│ 2. Calcular prioridad en C# (Zero-Math)                 │
│ 3. Agrupar conceptos fallados por tema                  │
│ 4. LLM enriquecimiento (temp 0.3, structured output)    │
│ 5. Generar Markdown con máxima 7 temas                  │
│ 6. Exportar a PDF (QuestPDF o print-to-PDF)             │
│ 7. Persistir en historial de sesiones                   │
└─────────────────────────────────────────────────────────┘
```

## Components

### 1. IStudyGuideGenerator (`InterviewDrill.Core.Contracts/IAgents.cs`)
**Responsibility:** Interface of the study guide generator.

### 2. StudyGuideGenerator (`InterviewDrill.Agents/StudyGuideGenerator.cs`)
**Responsibility:** Concrete implementation.
- Computes priority in C#: `priority(skill) = failureFrequency × jdWeight × (1 - avgScore/100)`
- Groups failed concepts by topic (max. 7 topics)
- Generates Markdown with explanation, relevance, resources, exercise, time
- Includes a "What you already master" section (strengths)

## Data Flow

```
EvaluationPayload[] (por pregunta) + JobProfile ──> StudyGuideGenerator
                                           │
                                           v
                                  Calcular prioridad en C# (Zero-Math)
                                           │
                                           v
                                  Agrupar por tema (máx. 7)
                                           │
                                           v
                                  LLM enriquecimiento (temp 0.3)
                                           │
                                           v
                                  Markdown output
                                           │
                                           v
                                  Export PDF + Persistir
```

## API Design (C#)

```csharp
// Interface en Core
public interface IStudyGuideGenerator
{
    Task<StudyGuide> GenerateAsync(
        IReadOnlyList<EvaluationPayload> evaluations,
        JobProfile jobProfile,
        CancellationToken ct = default);
}

public record StudyGuide(
    string Markdown,
    IReadOnlyList<StudyTopic> Topics,
    string GeneratedAt);

public record StudyTopic(
    string Name,
    double Priority,
    string Explanation,
    string RelevanceToRole,
    IReadOnlyList<string> Resources,
    string Exercise,
    string SuccessCriteria,
    TimeSpan EstimatedTime);
```

## Technology Choices

| Technology | Rationale |
|------------|-----------|
| **C# 15 / .NET 11** | Authoritative stack, Zero-Math for priority |
| **QuestPDF** | Server-side PDF export (formal work pending) |
| **Markdown** | Guide output format |
| **xUnit** | Testing framework |

## File Structure (.NET)

```
backend/
├── src/
│   └── InterviewDrill.Agents/
│       └── StudyGuideGenerator.cs
└── tests/
    └── InterviewDrill.Agents.Tests/
        └── (Study Guide tests - golden dataset pendiente)
```

## Implementation Status

| Task | Status | Evidence |
|------|--------|----------|
| IStudyGuideGenerator implemented | ✅ Complete | `StudyGuideGenerator` in `InterviewDrill.Agents` |
| Priority computed in C# (Zero-Math) | ✅ Complete | Formula: failureFrequency × jdWeight × (1 - avgScore/100) |
| Topics ordered by descending impact | ✅ Complete | Implemented |
| Each topic: explanation, relevance, resources, exercise, time | ~[~] Partial | Deterministic explanations/resources/exercises complete; LLM enrichment (temp 0.3) pending |
| "What you already master" section | ✅ Complete | Consolidated strengths |
| Markdown + PDF export | ~[~] Partial | Markdown download + print-to-PDF UI; formal server-side PDF with QuestPDF pending |
| Tests with golden dataset | ~[~] In progress | Prioritization tests, 7-topic limit; formal golden dataset pending |

## Testing Strategy

### Unit Tests
- **Priority Calculation**: Formula in C#, boundary conditions
- **Topic Grouping**: Grouping by keywords, limit of 7 topics
- **Markdown Generation**: Correct structure, strengths section

### Golden Dataset
- 3 simulated sessions → expected guides (formal work pending)

### Coverage Target
- > 85% coverage

## Risks

| Risk | Mitigation |
|------|------------|
| Guide too overwhelming | Maximum 7 prioritized topics |
| PII in the guide | History already comes sanitized; there is no PII in the guide |
| LLM enrichment unavailable | Deterministic explanations/resources/exercises as fallback |

## Dependencies

- **Scoring Synthesizer (spec 002)**: Provides EvaluationPayload[]
- **JD Parser (spec 009)**: Provides skill weights
- **Persistence (spec 011)**: Stores the guide
