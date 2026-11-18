/**
 * FeedbackConsolidator - Merges feedback from both evaluators
 * 
 * Consolidates coveredKeywords, missedConcepts, and strengths from
 * Architecture Evaluator and Security & Rigor Judge without duplicates.
 * Generates actionable improvement advice based on missed concepts.
 * 
 * @module FeedbackConsolidator
 */

import { mergeArraysUnique, filterEmptyStrings } from '../../utils/arrayUtils.js';
import type { ArchitectureEvaluation, SecurityEvaluation } from './types.js';

/**
 * Consolidated feedback result
 */
export interface ConsolidatedFeedback {
  /** Merged covered keywords (no duplicates) */
  coveredKeywords: string[];
  
  /** Merged missed concepts (no duplicates) */
  missedConcepts: string[];
  
  /** Merged strengths (no duplicates) */
  strengths: string[];
  
  /** Actionable improvement advice */
  improvementAdvice: string;
}

/**
 * Configuration for FeedbackConsolidator
 */
export interface FeedbackConsolidatorConfig {
  /** Custom advice templates (optional) */
  adviceTemplates?: Map<string, string>;
  
  /** Maximum advice length (default: 500) */
  maxAdviceLength?: number;
}

/**
 * FeedbackConsolidator class for merging evaluation feedback
 */
export class FeedbackConsolidator {
  private readonly adviceTemplates: Map<string, string>;
  private readonly maxAdviceLength: number;

  /**
   * Creates a new FeedbackConsolidator
   * 
   * @param config - Configuration options
   */
  constructor(config: FeedbackConsolidatorConfig = {}) {
    this.adviceTemplates = config.adviceTemplates || this.getDefaultAdviceTemplates();
    this.maxAdviceLength = config.maxAdviceLength || 500;
  }

  /**
   * Consolidates feedback from both evaluators
   * 
   * @param architectureEval - Evaluation from Architecture Evaluator
   * @param securityEval - Evaluation from Security & Rigor Judge
   * @returns Consolidated feedback with merged arrays and generated advice
   */
  public consolidate(
    architectureEval: ArchitectureEvaluation,
    securityEval: SecurityEvaluation
  ): ConsolidatedFeedback {
    // Merge covered keywords without duplicates
    const coveredKeywords = mergeArraysUnique(
      filterEmptyStrings(architectureEval.coveredKeywords),
      filterEmptyStrings(securityEval.coveredKeywords)
    );

    // Merge missed concepts without duplicates
    const missedConcepts = mergeArraysUnique(
      filterEmptyStrings(architectureEval.missedConcepts),
      filterEmptyStrings(securityEval.missedConcepts)
    );

    // Merge strengths without duplicates
    const strengths = mergeArraysUnique(
      filterEmptyStrings(architectureEval.strengths),
      filterEmptyStrings(securityEval.strengths)
    );

    // Generate improvement advice based on missed concepts
    const improvementAdvice = this.generateAdvice(missedConcepts);

    return {
      coveredKeywords,
      missedConcepts,
      strengths,
      improvementAdvice,
    };
  }

  /**
   * Generates actionable improvement advice based on missed concepts
   * 
   * @param missedConcepts - List of missed concepts
   * @returns Actionable advice string
   */
  private generateAdvice(missedConcepts: string[]): string {
    if (missedConcepts.length === 0) {
      return 'Excellent performance! Consider mentoring others or exploring advanced topics.';
    }

    const adviceParts: string[] = [];

    // Map each missed concept to specific advice
    for (const concept of missedConcepts) {
      const normalizedConcept = concept.toLowerCase().trim();
      const advice = this.getAdviceForConcept(normalizedConcept);
      if (advice && !adviceParts.includes(advice)) {
        adviceParts.push(advice);
      }
    }

    // Build final advice
    let finalAdvice = 'Focus on improving in these areas: ';
    
    if (adviceParts.length > 0) {
      finalAdvice += adviceParts.join(' ');
    } else {
      finalAdvice += `Study the following concepts: ${missedConcepts.join(', ')}.`;
    }

    // Truncate if too long
    if (finalAdvice.length > this.maxAdviceLength) {
      finalAdvice = finalAdvice.substring(0, this.maxAdviceLength - 3) + '...';
    }

    return finalAdvice;
  }

  /**
   * Gets specific advice for a concept
   * 
   * @param concept - Normalized concept name (lowercase, trimmed)
   * @returns Advice string or null if no template found
   */
  private getAdviceForConcept(concept: string): string | null {
    // Check exact match first
    if (this.adviceTemplates.has(concept)) {
      return this.adviceTemplates.get(concept)!;
    }

    // Check partial matches
    for (const [key, value] of this.adviceTemplates.entries()) {
      if (concept.includes(key) || key.includes(concept)) {
        return value;
      }
    }

    return null;
  }

  /**
   * Returns default advice templates for common concepts
   */
  private getDefaultAdviceTemplates(): Map<string, string> {
    return new Map([
      ['zero-math', 'Review the Zero-Math principle: never delegate mathematical operations to LLMs.'],
      ['kernelFunction', 'Study Semantic Kernel KernelFunction patterns for AI orchestration.'],
      ['hnsw', 'Learn about HNSW algorithm for efficient vector similarity search.'],
      ['kafka', 'Understand Kafka for event streaming and async communication.'],
      ['activitysource', 'Implement distributed tracing with ActivitySource for observability.'],
      ['polly', 'Study Polly library for resilience patterns: retry, circuit breaker, timeout.'],
      ['pii', 'Implement PII sanitization with RegexPiiSanitizer before LLM inference.'],
      ['sanitization', 'Ensure data sanitization before vectorization or inference.'],
      ['guardrails', 'Implement guardrails for relevance scoring and intervention mechanisms.'],
      ['rate limit', 'Handle rate limiting (HTTP 429) with exponential backoff.'],
      ['semantic cache', 'Implement semantic cache to reduce LLM costs for repeated patterns.'],
      ['latency', 'Optimize for low latency: target P95 < 2s for evaluation responses.'],
      ['human-in-the-loop', 'Implement human-in-the-loop mechanisms for critical decisions.'],
    ]);
  }
}
