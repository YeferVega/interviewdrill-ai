/**
 * ScoreCalculator - Calculates weighted overall score from component scores
 * 
 * Implements the formula: overallScore = (TechnicalDepth * weightTechnical) + (ProductionReadiness * weightProduction)
 * Default weights: TechnicalDepth (0.6), ProductionReadiness (0.4)
 * 
 * @module ScoreCalculator
 */

import { clamp, roundToDecimalPlaces, validateScoreRange, validateWeightsSum } from '../../utils/mathUtils.js';
import type { ScoringWeights } from './types.js';

/**
 * Configuration options for ScoreCalculator
 */
export interface ScoreCalculatorConfig {
  /** Weights for scoring components (must sum to 1.0) */
  weights?: ScoringWeights;
  
  /** Number of decimal places for rounding (default: 2) */
  decimalPlaces?: number;
}

/**
 * ScoreCalculator class for computing weighted overall scores
 */
export class ScoreCalculator {
  private readonly weights: ScoringWeights;
  private readonly decimalPlaces: number;

  /**
   * Creates a new ScoreCalculator
   * 
   * @param config - Configuration options
   */
  constructor(config: ScoreCalculatorConfig = {}) {
    this.weights = config.weights || {
      technical: 0.6,
      production: 0.4,
    };

    this.decimalPlaces = config.decimalPlaces ?? 2;

    // Validate that weights sum to 1.0
    validateWeightsSum([this.weights.technical, this.weights.production]);
  }

  /**
   * Calculates the overall weighted score
   * 
   * Formula: overallScore = (technicalDepth * 0.6) + (productionReadiness * 0.4)
   * 
   * @param technicalDepth - Technical depth score (0-100)
   * @param productionReadiness - Production readiness score (0-100)
   * @returns Weighted overall score (0-100), rounded to configured decimal places
   * @throws Error if inputs are out of range
   */
  public calculate(technicalDepth: number, productionReadiness: number): number {
    // Validate input ranges
    validateScoreRange(technicalDepth, 'technicalDepth');
    validateScoreRange(productionReadiness, 'productionReadiness');

    // Calculate weighted score
    const rawScore =
      technicalDepth * this.weights.technical +
      productionReadiness * this.weights.production;

    // Clamp to valid range (should already be in range, but safety check)
    const clampedScore = clamp(rawScore, 0, 100);

    // Round to configured decimal places
    return roundToDecimalPlaces(clampedScore, this.decimalPlaces);
  }

  /**
   * Calculates the overall score and returns detailed breakdown
   * 
   * @param technicalDepth - Technical depth score (0-100)
   * @param productionReadiness - Production readiness score (0-100)
   * @returns Object with overall score and component breakdown
   */
  public calculateWithBreakdown(
    technicalDepth: number,
    productionReadiness: number
  ): {
    overallScore: number;
    technicalContribution: number;
    productionContribution: number;
    weights: ScoringWeights;
  } {
    // Validate input ranges
    validateScoreRange(technicalDepth, 'technicalDepth');
    validateScoreRange(productionReadiness, 'productionReadiness');

    const technicalContribution = technicalDepth * this.weights.technical;
    const productionContribution = productionReadiness * this.weights.production;
    const rawScore = technicalContribution + productionContribution;
    const clampedScore = clamp(rawScore, 0, 100);
    const overallScore = roundToDecimalPlaces(clampedScore, this.decimalPlaces);

    return {
      overallScore,
      technicalContribution: roundToDecimalPlaces(technicalContribution, this.decimalPlaces),
      productionContribution: roundToDecimalPlaces(productionContribution, this.decimalPlaces),
      weights: { ...this.weights },
    };
  }

  /**
   * Returns the current weights configuration
   */
  public getWeights(): ScoringWeights {
    return { ...this.weights };
  }

  /**
   * Returns the configured decimal places
   */
  public getDecimalPlaces(): number {
    return this.decimalPlaces;
  }
}
