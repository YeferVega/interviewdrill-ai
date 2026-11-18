/**
 * VerdictDeterminer - Determines verdict based on overall score
 * 
 * Verdict logic:
 * - PASS: score >= 80
 * - NEEDS_PRACTICE: 60 <= score < 80
 * - FAIL: score < 60
 * 
 * Uses strategy pattern for extensibility
 * 
 * @module VerdictDeterminer
 */

import type { Verdict } from './types.js';

/**
 * Strategy interface for verdict determination
 */
export interface IVerdictStrategy {
  determine(score: number): Verdict;
}

/**
 * Default verdict strategy using standard thresholds
 */
export class DefaultVerdictStrategy implements IVerdictStrategy {
  private readonly passThreshold: number;
  private readonly needsPracticeThreshold: number;

  /**
   * Creates a new DefaultVerdictStrategy
   * 
   * @param passThreshold - Score threshold for PASS (default: 80)
   * @param needsPracticeThreshold - Score threshold for NEEDS_PRACTICE (default: 60)
   */
  constructor(passThreshold: number = 80, needsPracticeThreshold: number = 60) {
    if (passThreshold <= needsPracticeThreshold) {
      throw new Error(
        `passThreshold (${passThreshold}) must be greater than needsPracticeThreshold (${needsPracticeThreshold})`
      );
    }

    this.passThreshold = passThreshold;
    this.needsPracticeThreshold = needsPracticeThreshold;
  }

  /**
   * Determines verdict based on score
   * 
   * @param score - Overall score (0-100)
   * @returns Verdict: PASS, NEEDS_PRACTICE, or FAIL
   */
  public determine(score: number): Verdict {
    if (score >= this.passThreshold) {
      return 'PASS';
    } else if (score >= this.needsPracticeThreshold) {
      return 'NEEDS_PRACTICE';
    } else {
      return 'FAIL';
    }
  }

  /**
   * Returns the thresholds used by this strategy
   */
  public getThresholds(): { pass: number; needsPractice: number } {
    return {
      pass: this.passThreshold,
      needsPractice: this.needsPracticeThreshold,
    };
  }
}

/**
 * VerdictDeterminer class for determining evaluation verdicts
 */
export class VerdictDeterminer {
  private readonly strategy: IVerdictStrategy;

  /**
   * Creates a new VerdictDeterminer
   * 
   * @param strategy - Verdict strategy to use (default: DefaultVerdictStrategy)
   */
  constructor(strategy: IVerdictStrategy = new DefaultVerdictStrategy()) {
    this.strategy = strategy;
  }

  /**
   * Determines the verdict based on overall score
   * 
   * @param overallScore - The overall score (0-100)
   * @returns The determined verdict
   */
  public determine(overallScore: number): Verdict {
    return this.strategy.determine(overallScore);
  }

  /**
   * Determines verdict and returns detailed information
   * 
   * @param overallScore - The overall score (0-100)
   * @returns Object with verdict and threshold information
   */
  public determineWithDetails(overallScore: number): {
    verdict: Verdict;
    score: number;
    thresholds: { pass: number; needsPractice: number };
    distanceToNextVerdict: number;
    nextVerdict: Verdict | null;
  } {
    const verdict = this.strategy.determine(overallScore);
    
    let thresholds = { pass: 80, needsPractice: 60 };
    if (this.strategy instanceof DefaultVerdictStrategy) {
      thresholds = this.strategy.getThresholds();
    }

    let distanceToNextVerdict = 0;
    let nextVerdict: Verdict | null = null;

    if (verdict === 'FAIL') {
      distanceToNextVerdict = thresholds.needsPractice - overallScore;
      nextVerdict = 'NEEDS_PRACTICE';
    } else if (verdict === 'NEEDS_PRACTICE') {
      distanceToNextVerdict = thresholds.pass - overallScore;
      nextVerdict = 'PASS';
    } else {
      distanceToNextVerdict = 0;
      nextVerdict = null; // Already at highest verdict
    }

    return {
      verdict,
      score: overallScore,
      thresholds,
      distanceToNextVerdict,
      nextVerdict,
    };
  }
}
