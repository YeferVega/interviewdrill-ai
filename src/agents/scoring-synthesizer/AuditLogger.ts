/**
 * AuditLogger - Structured logging for scoring decisions
 * 
 * Uses pino for high-performance structured logging.
 * Logs all evaluation inputs, calculations, and verdicts for audit trail.
 * 
 * @module AuditLogger
 */

import pino, { type Logger } from 'pino';
import type { AuditLogEntry, Verdict, ScoringWeights } from './types.js';

/**
 * Configuration for AuditLogger
 */
export interface AuditLoggerConfig {
  /** Log level (default: 'info') */
  level?: 'debug' | 'info' | 'warn' | 'error';
  
  /** Output destination (default: stdout) */
  destination?: NodeJS.WritableStream;
  
  /** Enable pretty printing for development (default: false) */
  pretty?: boolean;
}

/**
 * AuditLogger class for structured audit logging
 */
export class AuditLogger {
  private readonly logger: Logger;
  private readonly level: string;

  /**
   * Creates a new AuditLogger
   * 
   * @param config - Configuration options
   */
  constructor(config: AuditLoggerConfig = {}) {
    this.level = config.level || 'info';
    
    const options: pino.LoggerOptions = {
      level: this.level,
      formatters: {
        level: (label) => ({ level: label }),
      },
      timestamp: pino.stdTimeFunctions.isoTime,
    };

    if (config.destination) {
      this.logger = pino(options, config.destination);
    } else {
      this.logger = pino(options);
    }
  }

  /**
   * Logs a complete evaluation audit entry
   * 
   * @param entry - Audit log entry with all evaluation details
   */
  public logEvaluation(entry: AuditLogEntry): void {
    this.logger.info(
      {
        event: 'evaluation_completed',
        sessionId: entry.sessionId,
        inputs: entry.inputs,
        overallScore: entry.overallScore,
        verdict: entry.verdict,
        weights: entry.weights,
        warnings: entry.warnings,
      },
      `Evaluation completed: ${entry.verdict} (${entry.overallScore.toFixed(2)})`
    );
  }

  /**
   * Logs the start of an evaluation
   * 
   * @param sessionId - Unique session identifier
   * @param technicalDepth - Input technical depth score
   * @param productionReadiness - Input production readiness score
   */
  public logEvaluationStart(
    sessionId: string,
    technicalDepth: number,
    productionReadiness: number
  ): void {
    this.logger.info(
      {
        event: 'evaluation_started',
        sessionId,
        technicalDepth,
        productionReadiness,
      },
      `Evaluation started for session: ${sessionId}`
    );
  }

  /**
   * Logs score calculation details
   * 
   * @param sessionId - Unique session identifier
   * @param technicalDepth - Technical depth score
   * @param productionReadiness - Production readiness score
   * @param overallScore - Calculated overall score
   * @param weights - Weights used for calculation
   */
  public logScoreCalculation(
    sessionId: string,
    technicalDepth: number,
    productionReadiness: number,
    overallScore: number,
    weights: ScoringWeights
  ): void {
    this.logger.debug(
      {
        event: 'score_calculated',
        sessionId,
        calculation: {
          formula: `(TechnicalDepth * ${weights.technical}) + (ProductionReadiness * ${weights.production})`,
          technicalDepth,
          productionReadiness,
          weights,
          result: overallScore,
        },
      },
      `Score calculated: ${overallScore.toFixed(2)}`
    );
  }

  /**
   * Logs verdict determination
   * 
   * @param sessionId - Unique session identifier
   * @param overallScore - Overall score used for verdict
   * @param verdict - Determined verdict
   */
  public logVerdictDetermination(
    sessionId: string,
    overallScore: number,
    verdict: Verdict
  ): void {
    this.logger.info(
      {
        event: 'verdict_determined',
        sessionId,
        overallScore,
        verdict,
      },
      `Verdict determined: ${verdict}`
    );
  }

  /**
   * Logs validation errors
   * 
   * @param sessionId - Unique session identifier
   * @param error - Error message or object
   * @param payload - Invalid payload that failed validation
   */
  public logValidationError(
    sessionId: string,
    error: string | Error,
    payload?: unknown
  ): void {
    const errorMessage = error instanceof Error ? error.message : error;
    
    this.logger.error(
      {
        event: 'validation_error',
        sessionId,
        error: errorMessage,
        payload,
      },
      `Validation error: ${errorMessage}`
    );
  }

  /**
   * Logs general errors
   * 
   * @param sessionId - Unique session identifier
   * @param error - Error object
   * @param context - Additional context
   */
  public logError(
    sessionId: string,
    error: Error,
    context?: Record<string, unknown>
  ): void {
    this.logger.error(
      {
        event: 'error',
        sessionId,
        error: {
          name: error.name,
          message: error.message,
          stack: error.stack,
        },
        context,
      },
      `Error during evaluation: ${error.message}`
    );
  }

  /**
   * Logs warnings
   * 
   * @param sessionId - Unique session identifier
   * @param message - Warning message
   * @param context - Additional context
   */
  public logWarning(
    sessionId: string,
    message: string,
    context?: Record<string, unknown>
  ): void {
    this.logger.warn(
      {
        event: 'warning',
        sessionId,
        message,
        context,
      },
      `Warning: ${message}`
    );
  }

  /**
   * Logs debug information
   * 
   * @param sessionId - Unique session identifier
   * @param message - Debug message
   * @param data - Additional data
   */
  public logDebug(
    sessionId: string,
    message: string,
    data?: Record<string, unknown>
  ): void {
    this.logger.debug(
      {
        event: 'debug',
        sessionId,
        data,
      },
      message
    );
  }

  /**
   * Returns the underlying pino logger for advanced usage
   */
  public getLogger(): Logger {
    return this.logger;
  }
}
