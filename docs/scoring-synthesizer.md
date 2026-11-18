# Scoring Synthesizer Agent - API Documentation

## Overview

The Scoring Synthesizer Agent is the central component that consolidates evaluations from the Architecture Evaluator and Security & Rigor Judge, calculates weighted scores, determines verdicts, and generates structured feedback.

## Quick Start

```typescript
import { ScoringSynthesizerService } from './src/index.js';

const service = new ScoringSynthesizerService();

const architectureEval = {
  technicalDepth: 85,
  coveredKeywords: ['HNSW', 'Kafka', 'Zero-Math'],
  missedConcepts: ['Polly'],
  strengths: ['Strong vector search knowledge'],
  detectedPatterns: ['HNSW'],
  antiPatterns: [],
};

const securityEval = {
  productionReadiness: 75,
  coveredKeywords: ['PII sanitization'],
  missedConcepts: ['Rate limiting'],
  strengths: ['Good data protection'],
  securityConcerns: [],
  resilienceScore: 80,
};

const result = await service.synthesize(architectureEval, securityEval);
console.log(result);
// {
//   overallScore: 81,
//   technicalDepth: 85,
//   productionReadiness: 75,
//   verdict: 'PASS',
//   coveredKeywords: ['HNSW', 'Kafka', 'Zero-Math', 'PII sanitization'],
//   missedConcepts: ['Polly', 'Rate limiting'],
//   strengths: ['Strong vector search knowledge', 'Good data protection'],
//   improvementAdvice: 'Focus on improving in these areas: ...'
// }
```

## API Reference

### ScoringSynthesizerService

Main orchestrator class that coordinates the entire scoring pipeline.

#### Constructor

```typescript
new ScoringSynthesizerService(config?: ScoringSynthesizerConfig)
```

**Configuration Options:**

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `scoreCalculator` | `ScoreCalculatorConfig` | `{ weights: { technical: 0.6, production: 0.4 } }` | Scoring weights and decimal precision |
| `verdictStrategy` | `IVerdictStrategy` | `DefaultVerdictStrategy(80, 60)` | Custom verdict thresholds |
| `feedbackConsolidator` | `FeedbackConsolidatorConfig` | Default templates | Custom advice templates |
| `auditLogger` | `AuditLoggerConfig` | `{ level: 'info' }` | Logging configuration |
| `enableDebugLogging` | `boolean` | `false` | Enable detailed debug logs |

#### Methods

##### `synthesize(architectureEval, securityEval): Promise<EvaluationPayload>`

Main method that processes evaluations and returns the final score.

**Parameters:**

- `architectureEval: ArchitectureEvaluation` - Evaluation from Architecture Evaluator
- `securityEval: SecurityEvaluation` - Evaluation from Security & Rigor Judge

**Returns:** `Promise<EvaluationPayload>`

**Throws:**
- `Error` if input scores are out of range (0-100)
- `Error` if output validation fails

##### `getComponents(): object`

Returns all internal components for testing or inspection.

---

### Data Types

#### ArchitectureEvaluation

```typescript
interface ArchitectureEvaluation {
  technicalDepth: number;       // 0-100
  coveredKeywords: string[];    // Concepts covered
  missedConcepts: string[];     // Important concepts missed
  strengths: string[];          // Identified strengths
  detectedPatterns: string[];   // Architecture patterns found
  antiPatterns: string[];       // Anti-patterns detected
}
```

#### SecurityEvaluation

```typescript
interface SecurityEvaluation {
  productionReadiness: number;  // 0-100
  coveredKeywords: string[];    // Security concepts covered
  missedConcepts: string[];     // Security concepts missed
  strengths: string[];          // Security strengths
  securityConcerns: string[];   // Concerns identified
  resilienceScore: number;      // 0-100
}
```

#### EvaluationPayload

```typescript
interface EvaluationPayload {
  overallScore: number;           // 0-100, weighted score
  technicalDepth: number;         // 0-100, input score
  productionReadiness: number;    // 0-100, input score
  verdict: 'PASS' | 'NEEDS_PRACTICE' | 'FAIL';
  coveredKeywords: string[];      // Merged, no duplicates
  missedConcepts: string[];       // Merged, no duplicates
  strengths: string[];            // Merged, no duplicates
  improvementAdvice: string;      // Actionable feedback
}
```

---

### Scoring Formula

```
overallScore = (technicalDepth × 0.6) + (productionReadiness × 0.4)
```

Weights are configurable via constructor.

### Verdict Thresholds

| Verdict | Score Range |
|---------|------------|
| PASS | ≥ 80 |
| NEEDS_PRACTICE | 60 - 79 |
| FAIL | < 60 |

---

## Individual Components

### SchemaValidator

Validates payloads against JSON Schema.

```typescript
import { SchemaValidator } from './src/index.js';

const validator = new SchemaValidator();
const result = validator.validate(payload); // throws SchemaValidationError if invalid
const isValid = validator.isValid(payload); // returns boolean
```

### ScoreCalculator

Calculates weighted overall score.

```typescript
import { ScoreCalculator } from './src/index.js';

const calculator = new ScoreCalculator({
  weights: { technical: 0.7, production: 0.3 },
  decimalPlaces: 2,
});

const score = calculator.calculate(80, 70); // 77
const breakdown = calculator.calculateWithBreakdown(80, 70);
// { overallScore: 77, technicalContribution: 56, productionContribution: 21, weights: {...} }
```

### VerdictDeterminer

Determines verdict based on score.

```typescript
import { VerdictDeterminer } from './src/index.js';

const determiner = new VerdictDeterminer();
const verdict = determiner.determine(75); // 'NEEDS_PRACTICE'
const details = determiner.determineWithDetails(75);
// { verdict: 'NEEDS_PRACTICE', score: 75, thresholds: {...}, distanceToNextVerdict: 5, nextVerdict: 'PASS' }
```

### FeedbackConsolidator

Merges feedback from both evaluators.

```typescript
import { FeedbackConsolidator } from './src/index.js';

const consolidator = new FeedbackConsolidator();
const feedback = consolidator.consolidate(archEval, secEval);
// { coveredKeywords: [...], missedConcepts: [...], strengths: [...], improvementAdvice: '...' }
```

### AuditLogger

Structured logging for audit trail.

```typescript
import { AuditLogger } from './src/index.js';

const logger = new AuditLogger({ level: 'info' });
logger.logEvaluationStart(sessionId, technicalDepth, productionReadiness);
logger.logScoreCalculation(sessionId, tech, prod, overall, weights);
logger.logVerdictDetermination(sessionId, overallScore, verdict);
logger.logEvaluation({ timestamp, sessionId, inputs, overallScore, verdict, weights });
```

---

## Error Handling

The service throws errors in these cases:

1. **Out of range scores**: `technicalDepth` or `productionReadiness` not in [0, 100]
2. **Invalid weights**: Custom weights don't sum to 1.0
3. **Schema validation**: Output payload doesn't match JSON Schema

```typescript
try {
  const result = await service.synthesize(archEval, secEval);
} catch (error) {
  console.error('Scoring failed:', error.message);
}
```

---

## Performance

- **Target**: < 2 seconds P95 latency
- **Typical**: < 100ms for standard evaluations
- **Throughput**: Supports concurrent evaluations

---

## JSON Schema

The output payload conforms to a strict JSON Schema:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "EvaluationPayload",
  "type": "object",
  "required": ["overallScore", "technicalDepth", "productionReadiness", "verdict", "coveredKeywords", "missedConcepts", "strengths", "improvementAdvice"],
  "properties": {
    "overallScore": { "type": "number", "minimum": 0, "maximum": 100 },
    "technicalDepth": { "type": "number", "minimum": 0, "maximum": 100 },
    "productionReadiness": { "type": "number", "minimum": 0, "maximum": 100 },
    "verdict": { "type": "string", "enum": ["PASS", "NEEDS_PRACTICE", "FAIL"] },
    "coveredKeywords": { "type": "array", "items": { "type": "string" } },
    "missedConcepts": { "type": "array", "items": { "type": "string" } },
    "strengths": { "type": "array", "items": { "type": "string" } },
    "improvementAdvice": { "type": "string" }
  },
  "additionalProperties": false
}
```
