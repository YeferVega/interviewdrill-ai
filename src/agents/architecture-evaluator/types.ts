/**
 * TypeScript interfaces for Architecture Evaluator Agent
 * 
 * @module types
 */

/**
 * Candidate response to be evaluated
 */
export interface CandidateResponse {
  /** Text of the candidate's response */
  text: string;
  
  /** Topic of the interview */
  topic: string;
  
  /** Unique session identifier */
  sessionId: string;
}

/**
 * Definition of a technical concept to detect
 */
export interface ConceptDefinition {
  /** Name of the concept */
  name: string;
  
  /** Keywords that indicate this concept */
  keywords: string[];
  
  /** Regex patterns for detection */
  patterns: RegExp[];
  
  /** Importance weight (0.0 - 1.0) */
  weight: number;
  
  /** Description of the concept */
  description: string;
  
  /** Example usages */
  examples: string[];
  
  /** Category of the concept */
  category: 'architecture' | 'pattern' | 'technology' | 'principle';
}

/**
 * Definition of an anti-pattern to detect
 */
export interface AntiPatternDefinition {
  /** Name of the anti-pattern */
  name: string;
  
  /** Indicators that suggest this anti-pattern */
  indicators: string[];
  
  /** Regex patterns for detection */
  patterns: RegExp[];
  
  /** Severity level */
  severity: 'low' | 'medium' | 'high';
  
  /** Description of why this is an anti-pattern */
  description: string;
  
  /** Penalty to apply to score */
  penalty: number;
}

/**
 * Detected concept with metadata
 */
export interface DetectedConcept {
  /** Name of the concept */
  name: string;
  
  /** Confidence level (0.0 - 1.0) */
  confidence: number;
  
  /** Importance weight from ConceptDefinition (0.0 - 1.0) */
  weight: number;
  
  /** Context where it was found */
  context: string;
  
  /** Category of the concept */
  category: ConceptDefinition['category'];
}

/**
 * Detected anti-pattern with metadata
 */
export interface DetectedAntiPattern {
  /** Name of the anti-pattern */
  name: string;
  
  /** Severity level */
  severity: AntiPatternDefinition['severity'];
  
  /** Context where it was found */
  context: string;
  
  /** Penalty applied */
  penalty: number;
}

/**
 * Identified strength in the response
 */
export interface IdentifiedStrength {
  /** Description of the strength */
  description: string;
  
  /** Related concept */
  relatedConcept?: string;
  
  /** Evidence from text */
  evidence: string;
}

/**
 * Service interface for Architecture Evaluator
 */
export interface IArchitectureEvaluator {
  /**
   * Evaluates a candidate's response
   * 
   * @param response - The candidate's response to evaluate
   * @returns Architecture evaluation with scoring and feedback
   */
  evaluate(response: CandidateResponse): Promise<ArchitectureEvaluationResult>;
}

/**
 * Result of architecture evaluation
 */
export interface ArchitectureEvaluationResult {
  /** Technical depth score (0-100) */
  technicalDepth: number;
  
  /** Keywords that were covered */
  coveredKeywords: string[];
  
  /** Important concepts that were missed */
  missedConcepts: string[];
  
  /** Strengths identified in the response */
  strengths: string[];
  
  /** Architectural patterns detected */
  detectedPatterns: string[];
  
  /** Anti-patterns detected */
  antiPatterns: string[];
  
  /** Detected concepts with metadata (optional for compatibility) */
  concepts?: DetectedConcept[];
  
  /** Detected anti-patterns with metadata (optional for compatibility) */
  antiPatternsDetailed?: DetectedAntiPattern[];
  
  /** Identified strengths with metadata (optional for compatibility) */
  strengthsDetailed?: IdentifiedStrength[];
}
