/**
 * InterviewDrill AI - Scoring Synthesizer & Architecture Evaluator
 * 
 * Central components for the InterviewDrill AI system:
 * - Scoring Synthesizer: Consolidates evaluations and calculates weighted scores
 * - Architecture Evaluator: Analyzes technical responses and detects patterns/anti-patterns
 * 
 * @module interviewdrill-ai
 */

// Main service
export { ScoringSynthesizerService } from './agents/scoring-synthesizer/ScoringSynthesizerService.js';
export type { ScoringSynthesizerConfig } from './agents/scoring-synthesizer/ScoringSynthesizerService.js';

// Components
export { SchemaValidator, SchemaValidationError } from './agents/scoring-synthesizer/SchemaValidator.js';
export { ScoreCalculator } from './agents/scoring-synthesizer/ScoreCalculator.js';
export type { ScoreCalculatorConfig } from './agents/scoring-synthesizer/ScoreCalculator.js';
export { VerdictDeterminer, DefaultVerdictStrategy } from './agents/scoring-synthesizer/VerdictDeterminer.js';
export type { IVerdictStrategy } from './agents/scoring-synthesizer/VerdictDeterminer.js';
export { FeedbackConsolidator } from './agents/scoring-synthesizer/FeedbackConsolidator.js';
export type { ConsolidatedFeedback, FeedbackConsolidatorConfig } from './agents/scoring-synthesizer/FeedbackConsolidator.js';
export { AuditLogger } from './agents/scoring-synthesizer/AuditLogger.js';
export type { AuditLoggerConfig } from './agents/scoring-synthesizer/AuditLogger.js';

// Types
export type {
  IScoringSynthesizer,
  ArchitectureEvaluation,
  SecurityEvaluation,
  EvaluationPayload,
  Verdict,
  ScoringWeights,
  AuditLogEntry,
} from './agents/scoring-synthesizer/types.js';

// Utilities
export * from './utils/mathUtils.js';
export * from './utils/arrayUtils.js';
export * from './utils/textUtils.js';

// Architecture Evaluator
export { ArchitectureEvaluatorService } from './agents/architecture-evaluator/ArchitectureEvaluatorService.js';
export type { ArchitectureEvaluatorServiceConfig } from './agents/architecture-evaluator/ArchitectureEvaluatorService.js';
export { ConceptLibrary } from './agents/architecture-evaluator/ConceptLibrary.js';
export { PatternDetector } from './agents/architecture-evaluator/PatternDetector.js';
export { AntiPatternDetector } from './agents/architecture-evaluator/AntiPatternDetector.js';
export { StrengthAnalyzer } from './agents/architecture-evaluator/StrengthAnalyzer.js';
export type { StrengthAnalyzerConfig } from './agents/architecture-evaluator/StrengthAnalyzer.js';
export { TechnicalDepthScorer } from './agents/architecture-evaluator/TechnicalDepthScorer.js';
export type { TechnicalDepthScorerConfig, ScoreBreakdown } from './agents/architecture-evaluator/TechnicalDepthScorer.js';

// Architecture Evaluator types
export type {
  IArchitectureEvaluator,
  CandidateResponse,
  ArchitectureEvaluationResult,
  ConceptDefinition,
  AntiPatternDefinition,
  DetectedConcept,
  DetectedAntiPattern,
  IdentifiedStrength,
} from './agents/architecture-evaluator/types.js';
