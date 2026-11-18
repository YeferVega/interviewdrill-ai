/**
 * Calculates technical depth score based on detected concepts and anti-patterns
 * @module TechnicalDepthScorer
 */

import { DetectedConcept, DetectedAntiPattern } from './types.js';

/**
 * Configuration for TechnicalDepthScorer
 */
export interface TechnicalDepthScorerConfig {
  /** Maximum possible score */
  maxScore?: number;
  /** Minimum possible score (after penalties) */
  minScore?: number;
  /** Multiplier for converting weight sum to score points */
  weightMultiplier?: number;
}

/**
 * Result of score calculation with detailed breakdown
 */
export interface ScoreBreakdown {
  /** Final technical depth score (0-100) */
  score: number;
  /** Base score from concept coverage before penalties */
  baseScore: number;
  /** Total penalty from anti-patterns */
  totalPenalty: number;
  /** Sum of concept weights */
  weightSum: number;
  /** Number of concepts detected */
  conceptCount: number;
  /** Number of anti-patterns detected */
  antiPatternCount: number;
  /** Details of each concept's contribution */
  conceptContributions: Array<{
    conceptName: string;
    weight: number;
    confidence: number;
    contribution: number;
  }>;
  /** Details of each anti-pattern's penalty */
  antiPatternPenalties: Array<{
    antiPatternName: string;
    severity: string;
    penalty: number;
  }>;
}

/**
 * Calculates technical depth score from detected concepts and anti-patterns
 */
export class TechnicalDepthScorer {
  private readonly maxScore: number;
  private readonly minScore: number;
  private readonly weightMultiplier: number;

  /**
   * Creates a new TechnicalDepthScorer instance
   * @param config - Configuration options
   */
  constructor(config: TechnicalDepthScorerConfig = {}) {
    this.maxScore = config.maxScore ?? 100;
    this.minScore = config.minScore ?? 0;
    this.weightMultiplier = config.weightMultiplier ?? 100;
  }

  /**
   * Calculates the technical depth score with full breakdown
   * @param detectedConcepts - Array of detected concepts with weights and confidence
   * @param detectedAntiPatterns - Array of detected anti-patterns with penalties
   * @returns Score breakdown with detailed calculation information
   */
  public calculateWithBreakdown(
    detectedConcepts: DetectedConcept[],
    detectedAntiPatterns: DetectedAntiPattern[]
  ): ScoreBreakdown {
    // Calculate base score from concepts
    const conceptContributions = detectedConcepts.map(concept => {
      // Contribution = weight * confidence (penalized by low confidence)
      const contribution = concept.weight * concept.confidence;
      return {
        conceptName: concept.name,
        weight: concept.weight,
        confidence: concept.confidence,
        contribution
      };
    });

    const weightSum = conceptContributions.reduce(
      (sum, c) => sum + c.contribution, 0
    );

    // Convert weight sum to base score
    // Weight sum is typically 0-N where N is number of concepts
    // We normalize to 0-100 scale
    const baseScore = Math.min(weightSum * this.weightMultiplier, this.maxScore);

    // Calculate total penalty from anti-patterns
    const antiPatternPenalties = detectedAntiPatterns.map(ap => ({
      antiPatternName: ap.name,
      severity: ap.severity,
      penalty: ap.penalty
    }));

    const totalPenalty = detectedAntiPatterns.reduce(
      (sum, ap) => sum + ap.penalty, 0
    );

    // Calculate final score
    const rawScore = baseScore - totalPenalty;
    const score = this.clampScore(rawScore);

    return {
      score,
      baseScore: Math.round(baseScore * 100) / 100,
      totalPenalty,
      weightSum: Math.round(weightSum * 1000) / 1000,
      conceptCount: detectedConcepts.length,
      antiPatternCount: detectedAntiPatterns.length,
      conceptContributions,
      antiPatternPenalties
    };
  }

  /**
   * Calculates the technical depth score (simplified version)
   * @param detectedConcepts - Array of detected concepts
   * @param detectedAntiPatterns - Array of detected anti-patterns
   * @returns Technical depth score (0-100)
   */
  public calculateScore(
    detectedConcepts: DetectedConcept[],
    detectedAntiPatterns: DetectedAntiPattern[]
  ): number {
    const breakdown = this.calculateWithBreakdown(detectedConcepts, detectedAntiPatterns);
    return Math.round(breakdown.score);
  }

  /**
   * Clamps score to valid range [minScore, maxScore]
   */
  private clampScore(score: number): number {
    return Math.max(this.minScore, Math.min(this.maxScore, score));
  }

  /**
   * Gets the configuration values
   */
  public getConfig(): Required<TechnicalDepthScorerConfig> {
    return {
      maxScore: this.maxScore,
      minScore: this.minScore,
      weightMultiplier: this.weightMultiplier
    };
  }
}
