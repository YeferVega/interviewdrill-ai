# Architecture Evaluator Agent

## Overview

The Architecture Evaluator Agent analyzes technical responses from candidates, detecting key concepts, architectural patterns, and anti-patterns. It provides a technical depth score (0-100) based on the concepts covered and penalties applied for detected anti-patterns.

## Core Components

### 1. ArchitectureEvaluatorService

The main orchestrator that coordinates the complete evaluation process.

```typescript
import { ArchitectureEvaluatorService } from './src/index.js';

const evaluator = new ArchitectureEvaluatorService();

const result = await evaluator.evaluate({
  text: 'candidate response text here',
  topic: 'GenAI Architecture',
  sessionId: 'session-123'
});

console.log(result);
// {
//   technicalDepth: 85,
//   coveredKeywords: ['HNSW', 'Kafka', 'Zero-Math', ...],
//   detectedPatterns: ['HNSW', 'Semantic Kernel', ...],
//   antiPatterns: [],
//   missedConcepts: ['Polly', ...],
//   strengths: ['Strong understanding of vector search', ...]
// }
```

### 2. ConceptLibrary

Maintains the library of 12 key technical concepts to detect:

| Concept | Category | Weight | Description |
|---------|----------|--------|-------------|
| Zero-Math | principle | 0.9 | Don't delegate math to LLMs |
| KernelFunction | pattern | 0.85 | Semantic Kernel orchestration |
| HNSW | technology | 0.9 | Vector search algorithm |
| Kafka | technology | 0.8 | Event streaming |
| ActivitySource | technology | 0.85 | Distributed tracing |
| Polly | technology | 0.85 | Resilience patterns |
| RAG | pattern | 0.9 | Retrieval Augmented Generation |
| SemanticCache | pattern | 0.75 | LLM response caching |
| PIISanitization | principle | 0.9 | Data protection |
| RateLimiting | pattern | 0.8 | HTTP 429 handling |
| HumanInTheLoop | principle | 0.85 | Human oversight |
| RelevanceGuardrails | principle | 0.8 | Quality filtering |

### 3. PatternDetector

Detects concepts in text using keywords and regex patterns:

```typescript
import { PatternDetector } from './src/index.js';

const detector = new PatternDetector();
const concepts = detector.detectConcepts('Use HNSW for vector search');

// Returns: [{ name: 'HNSW', confidence: 0.95, weight: 0.9, ... }]
```

### 4. AntiPatternDetector

Identifies 8 common architectural anti-patterns:

| Anti-Pattern | Severity | Penalty | Description |
|-------------|----------|---------|-------------|
| DelegatingMathToLLM | high | 20 | Using LLMs for calculations |
| NoErrorHandling | high | 15 | Missing error handling |
| IgnoringPIIProtection | high | 20 | Not sanitizing PII data |
| StrongCouplingToProvider | medium | 10 | Hard dependency on single provider |
| HardcodingSecrets | high | 25 | API keys in code |
| SynchronousHeavyOperations | medium | 10 | Blocking I/O for heavy ops |
| NoLoggingOrMonitoring | low | 5 | Missing observability |
| IgnoringPerformanceConsiderations | low | 5 | Not addressing performance |

```typescript
import { AntiPatternDetector } from './src/index.js';

const detector = new AntiPatternDetector();
const antiPatterns = detector.detectAntiPatterns('Use GPT for calculations');

// Returns: [{ name: 'DelegatingMathToLLM', severity: 'high', penalty: 20, ... }]
```

### 5. StrengthAnalyzer

Extracts strengths from the response based on detected concepts and context:

```typescript
import { StrengthAnalyzer } from './src/index.js';

const analyzer = new StrengthAnalyzer({ minConfidence: 0.7 });
const strengths = analyzer.analyzeStrengths(detectedConcepts, responseText);

// Returns: [{ description: 'Strong understanding of vector search', ... }]
```

### 6. TechnicalDepthScorer

Calculates the final technical depth score (0-100):

```typescript
import { TechnicalDepthScorer } from './src/index.js';

const scorer = new TechnicalDepthScorer({
  maxScore: 100,
  minScore: 0,
  weightMultiplier: 100
});

const score = scorer.calculateScore(detectedConcepts, detectedAntiPatterns);
// Returns: 85

// With detailed breakdown:
const breakdown = scorer.calculateWithBreakdown(detectedConcepts, detectedAntiPatterns);
// {
//   score: 85,
//   baseScore: 95,
//   totalPenalty: 10,
//   conceptContributions: [...],
//   antiPatternPenalties: [...]
// }
```

## Scoring Formula

```
baseScore = sum(concept.weight × concept.confidence) × 100
totalPenalty = sum(antiPattern.penalty)
finalScore = clamp(baseScore - totalPenalty, 0, 100)
```

### Example Calculation

```
Concepts detected:
- HNSW (weight: 0.9, confidence: 0.95) → contribution: 0.855
- Kafka (weight: 0.8, confidence: 0.9) → contribution: 0.72
- Zero-Math (weight: 0.9, confidence: 1.0) → contribution: 0.9

Total weight sum: 2.475
Base score: 2.475 × 100 = 247.5 → capped at 100

Anti-patterns detected:
- NoErrorHandling (penalty: 15)
- IgnoringPIIProtection (penalty: 20)

Total penalty: 35

Final score: 100 - 35 = 65
```

## Integration with Scoring Synthesizer

The Architecture Evaluator output feeds directly into the Scoring Synthesizer:

```typescript
import { ArchitectureEvaluatorService, ScoringSynthesizerService } from './src/index.js';

const archEvaluator = new ArchitectureEvaluatorService();
const scoringSynth = new ScoringSynthesizerService();

// Evaluate architecture
const archResult = await archEvaluator.evaluate(candidateResponse);

// Evaluate security (not shown)
// const secResult = await secEvaluator.evaluate(candidateResponse);

// Synthesize scores
const finalResult = await scoringSynth.synthesize(
  archResult,
  secResult
);

// finalResult includes:
// - overallScore (weighted average)
// - verdict (PASS/NEEDS_PRACTICE/FAIL)
// - improvementAdvice
```

## Configuration Options

### ArchitectureEvaluatorService

```typescript
const evaluator = new ArchitectureEvaluatorService({
  enableLogging: true,           // Enable console logging
  minConfidenceThreshold: 0.5    // Minimum confidence to consider a concept detected
});
```

### StrengthAnalyzer

```typescript
const analyzer = new StrengthAnalyzer({
  minConfidence: 0.7,      // Min confidence for strength extraction
  minEvidenceLength: 10    // Min characters for evidence
});
```

### TechnicalDepthScorer

```typescript
const scorer = new TechnicalDepthScorer({
  maxScore: 100,           // Maximum possible score
  minScore: 0,             // Minimum possible score
  weightMultiplier: 100    // Multiplier for weight sum
});
```

## Performance

- **Target**: < 3 seconds per evaluation
- **Typical**: < 1 second for standard responses
- **Large responses**: < 5 seconds

## Testing

The agent includes comprehensive test coverage:

```bash
# Run all tests
npm test

# Run with coverage
npm run test:coverage

# Run specific test file
npx vitest tests/agents/architecture-evaluator/ArchitectureEvaluatorService.test.ts
```

**Test Coverage**: > 90% statements, > 85% branches

## Example Usage

### Basic Evaluation

```typescript
import { ArchitectureEvaluatorService } from './src/index.js';

const evaluator = new ArchitectureEvaluatorService();

const response = {
  text: `When designing a GenAI system, I would follow the Zero-Math principle 
         and use HNSW for vector search. I'd implement Semantic Kernel for 
         orchestration and Kafka for event streaming.`,
  topic: 'GenAI Architecture',
  sessionId: 'session-123'
};

const result = await evaluator.evaluate(response);

console.log(`Technical Depth: ${result.technicalDepth}/100`);
console.log(`Covered: ${result.coveredKeywords.join(', ')}`);
console.log(`Missed: ${result.missedConcepts.join(', ')}`);
console.log(`Strengths: ${result.strengths.join(', ')}`);
```

### Custom Concept Library

```typescript
import { ArchitectureEvaluatorService, ConceptLibrary } from './src/index.js';

const customLibrary = new ConceptLibrary();
customLibrary.addConcept({
  name: 'CustomPattern',
  category: 'pattern',
  weight: 0.8,
  keywords: ['custom', 'pattern'],
  description: 'Custom pattern description',
  patterns: [/custom\s+pattern/i]
});

const evaluator = new ArchitectureEvaluatorService({
  conceptLibrary: customLibrary
});
```

## Architecture

```
ArchitectureEvaluatorService (Orchestrator)
├── ConceptLibrary (12 concepts)
├── PatternDetector (detects concepts)
├── AntiPatternDetector (8 anti-patterns)
├── StrengthAnalyzer (extracts strengths)
└── TechnicalDepthScorer (calculates score)
```

## Future Enhancements

- Integration with LLM for semantic understanding
- Support for code snippet analysis
- Custom concept libraries per topic
- Historical trend analysis
- Confidence calibration
