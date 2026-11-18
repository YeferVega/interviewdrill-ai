/**
 * TypeScript interfaces and types for the Scoring Synthesizer Agent
 * 
 * @module types
 */

/**
 * Evaluation payload from the Architecture Evaluator
 * Assesses the technical depth of candidate's response
 */
export interface ArchitectureEvaluation {
  /** Technical depth score from 0-100 */
  technicalDepth: number;
  
  /** Key technical concepts/patterns that were covered */
  coveredKeywords: string[];
  
  /** Important concepts that were missed */
  missedConcepts: string[];
  
  /** Strengths identified in the response */
  strengths: string[];
  
  /** Architectural patterns detected (e.g., Zero-Math, HNSW, Kafka) */
  detectedPatterns: string[];
  
  /** Anti-patterns detected (e.g., delegating math to LLMs) */
  antiPatterns: string[];
}

/**
 * Evaluation payload from the Security & Rigor Judge
 * Assesses production readiness and security considerations
 */
export interface SecurityEvaluation {
  /** Production readiness score from 0-100 */
  productionReadiness: number;
  
  /** Security/resilience concepts that were covered */
  coveredKeywords: string[];
  
  /** Important security/resilience concepts that were missed */
  missedConcepts: string[];
  
  /** Strengths identified in production considerations */
  strengths: string[];
  
  /** Security concerns or vulnerabilities identified */
  securityConcerns: string[];
  
  /** Resilience and fault tolerance score from 0-100 */
  resilienceScore: number;
}

/**
 * Final evaluation payload returned to the candidate
 * Must conform to JSON Schema validation
 */
export interface EvaluationPayload {
  /** Overall weighted score from 0-100 */
  overallScore: number;
  
  /** Technical depth component score from 0-100 */
  technicalDepth: number;
  
  /** Production readiness component score from 0-100 */
  productionReadiness: number;
  
  /** Final verdict based on overall score */
  verdict: 'PASS' | 'NEEDS_PRACTICE' | 'FAIL';
  
  /** All technical concepts that were covered (merged, no duplicates) */
  coveredKeywords: string[];
  
  /** All important concepts that were missed (merged, no duplicates) */
  missedConcepts: string[];
  
  /** All strengths identified (merged, no duplicates) */
  strengths: string[];
  
  /** Actionable advice for improvement based on missed concepts */
  improvementAdvice: string;
}

/**
 * Service interface for the Scoring Synthesizer
 */
export interface IScoringSynthesizer {
  /**
   * Synthesizes evaluations from Architecture Evaluator and Security Judge
   * into a final EvaluationPayload with weighted scoring and verdict
   * 
   * @param architectureEval - Evaluation from Architecture Evaluator
   * @param securityEval - Evaluation from Security & Rigor Judge
   * @returns Complete EvaluationPayload with scoring and feedback
   * @throws Error if validation fails or synthesis encounters an error
   */
  synthesize(
    architectureEval: ArchitectureEvaluation,
    securityEval: SecurityEvaluation
  ): Promise<EvaluationPayload>;
}

/**
 * Verdict type definition
 */
export type Verdict = 'PASS' | 'NEEDS_PRACTICE' | 'FAIL';

/**
 * Scoring weights configuration
 */
export interface ScoringWeights {
  /** Weight for technical depth (default: 0.6) */
  technical: number;
  
  /** Weight for production readiness (default: 0.4) */
  production: number;
}

/**
 * Audit log entry for scoring decisions
 */
export interface AuditLogEntry {
  /** Timestamp of the evaluation */
  timestamp: string;
  
  /** Unique session identifier */
  sessionId: string;
  
  /** Input scores */
  inputs: {
    technicalDepth: number;
    productionReadiness: number;
  };
  
  /** Calculated overall score */
  overallScore: number;
  
  /** Determined verdict */
  verdict: Verdict;
  
  /** Weights used for calculation */
  weights: ScoringWeights;
  
  /** Any errors or warnings encountered */
  warnings?: string[];
}
