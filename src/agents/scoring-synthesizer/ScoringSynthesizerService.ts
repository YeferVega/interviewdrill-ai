/**
 * ScoringSynthesizerService - Main orchestrator for the scoring pipeline
 * 
 * Coordinates validation, calculation, verdict determination, and feedback consolidation.
 * Implements the complete scoring pipeline with error handling and audit logging.
 * 
 * @module ScoringSynthesizerService
 */

import { SchemaValidator, SchemaValidationError } from './SchemaValidator.js';
import { ScoreCalculator, type ScoreCalculatorConfig } from './ScoreCalculator.js';
import { VerdictDeterminer, type IVerdictStrategy } from './VerdictDeterminer.js';
import { FeedbackConsolidator, type FeedbackConsolidatorConfig } from './FeedbackConsolidator.js';
import { AuditLogger, type AuditLoggerConfig } from './AuditLogger.js';
import type {
  IScoringSynthesizer,
  ArchitectureEvaluation,
  SecurityEvaluation,
  EvaluationPayload,
} from './types.js';

/**
 * Configuration for ScoringSynthesizerService
 */
export interface ScoringSynthesizerConfig {
  /** Score calculator configuration */
  scoreCalculator?: ScoreCalculatorConfig;
  
  /** Verdict strategy (default: DefaultVerdictStrategy) */
  verdictStrategy?: IVerdictStrategy;
  
  /** Feedback consolidator configuration */
  feedbackConsolidator?: FeedbackConsolidatorConfig;
  
  /** Audit logger configuration */
  auditLogger?: AuditLoggerConfig;
  
  /** Enable detailed logging (default: false) */
  enableDebugLogging?: boolean;
}

/**
 * ScoringSynthesizerService - Main orchestrator for scoring pipeline
 */
export class ScoringSynthesizerService implements IScoringSynthesizer {
  private readonly schemaValidator: SchemaValidator;
  private readonly scoreCalculator: ScoreCalculator;
  private readonly verdictDeterminer: VerdictDeterminer;
  private readonly feedbackConsolidator: FeedbackConsolidator;
  private readonly auditLogger: AuditLogger;
  private readonly enableDebugLogging: boolean;

  /**
   * Creates a new ScoringSynthesizerService
   * 
   * @param config - Configuration options
   */
  constructor(config: ScoringSynthesizerConfig = {}) {
    this.schemaValidator = new SchemaValidator();
    this.scoreCalculator = new ScoreCalculator(config.scoreCalculator);
    this.verdictDeterminer = new VerdictDeterminer(config.verdictStrategy);
    this.feedbackConsolidator = new FeedbackConsolidator(config.feedbackConsolidator);
    this.auditLogger = new AuditLogger(config.auditLogger);
    this.enableDebugLogging = config.enableDebugLogging || false;
  }

  /**
   * Synthesizes evaluations from both evaluators into a final EvaluationPayload
   * 
   * Pipeline:
   * 1. Validate input payloads
   * 2. Calculate overall score
   * 3. Determine verdict
   * 4. Consolidate feedback
   * 5. Log audit trail
   * 6. Return complete EvaluationPayload
   * 
   * @param architectureEval - Evaluation from Architecture Evaluator
   * @param securityEval - Evaluation from Security & Rigor Judge
   * @returns Complete EvaluationPayload with scoring and feedback
   * @throws Error if validation fails or synthesis encounters an error
   */
  async synthesize(
    architectureEval: ArchitectureEvaluation,
    securityEval: SecurityEvaluation
  ): Promise<EvaluationPayload> {
    const sessionId = this.generateSessionId();
    const warnings: string[] = [];

    try {
      // Log start
      this.auditLogger.logEvaluationStart(
        sessionId,
        architectureEval.technicalDepth,
        securityEval.productionReadiness
      );

      // Step 1: Calculate overall score
      const overallScore = this.scoreCalculator.calculate(
        architectureEval.technicalDepth,
        securityEval.productionReadiness
      );

      if (this.enableDebugLogging) {
        this.auditLogger.logScoreCalculation(
          sessionId,
          architectureEval.technicalDepth,
          securityEval.productionReadiness,
          overallScore,
          this.scoreCalculator.getWeights()
        );
      }

      // Step 2: Determine verdict
      const verdict = this.verdictDeterminer.determine(overallScore);
      this.auditLogger.logVerdictDetermination(sessionId, overallScore, verdict);

      // Step 3: Consolidate feedback
      const feedback = this.feedbackConsolidator.consolidate(
        architectureEval,
        securityEval
      );

      // Step 4: Build EvaluationPayload
      const evaluationPayload: EvaluationPayload = {
        overallScore,
        technicalDepth: architectureEval.technicalDepth,
        productionReadiness: securityEval.productionReadiness,
        verdict,
        coveredKeywords: feedback.coveredKeywords,
        missedConcepts: feedback.missedConcepts,
        strengths: feedback.strengths,
        improvementAdvice: feedback.improvementAdvice,
      };

      // Step 5: Validate output payload
      this.schemaValidator.validate(evaluationPayload);

      // Step 6: Log completion
      this.auditLogger.logEvaluation({
        timestamp: new Date().toISOString(),
        sessionId,
        inputs: {
          technicalDepth: architectureEval.technicalDepth,
          productionReadiness: securityEval.productionReadiness,
        },
        overallScore,
        verdict,
        weights: this.scoreCalculator.getWeights(),
        ...(warnings.length > 0 ? { warnings } : {}),
      });

      return evaluationPayload;
    } catch (error) {
      // Log error
      if (error instanceof Error) {
        this.auditLogger.logError(sessionId, error, {
          architectureEval,
          securityEval,
        });
      }

      // Re-throw with additional context
      if (error instanceof SchemaValidationError) {
        throw new Error(
          `Output validation failed: ${error.message}`,
          { cause: error }
        );
      }

      throw error;
    }
  }

  /**
   * Generates a unique session ID
   */
  private generateSessionId(): string {
    return `session_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;
  }

  /**
   * Returns the components for testing or inspection
   */
  public getComponents() {
    return {
      schemaValidator: this.schemaValidator,
      scoreCalculator: this.scoreCalculator,
      verdictDeterminer: this.verdictDeterminer,
      feedbackConsolidator: this.feedbackConsolidator,
      auditLogger: this.auditLogger,
    };
  }
}
