/**
 * Main orchestrator service for architecture evaluation
 * Coordinates all evaluation components to produce complete assessment
 * @module ArchitectureEvaluatorService
 */

import { IArchitectureEvaluator, CandidateResponse, ArchitectureEvaluationResult } from './types.js';
import { ConceptLibrary } from './ConceptLibrary.js';
import { PatternDetector } from './PatternDetector.js';
import { AntiPatternDetector } from './AntiPatternDetector.js';
import { StrengthAnalyzer } from './StrengthAnalyzer.js';
import { TechnicalDepthScorer } from './TechnicalDepthScorer.js';

/**
 * Configuration for ArchitectureEvaluatorService
 */
export interface ArchitectureEvaluatorServiceConfig {
  /** Custom concept library (optional) */
  conceptLibrary?: ConceptLibrary;
  /** Minimum confidence for strength identification (default: 0.7) */
  minStrengthConfidence?: number;
  /** Enable detailed logging (default: false) */
  enableLogging?: boolean;
}

/**
 * Main service that orchestrates architecture evaluation
 */
export class ArchitectureEvaluatorService implements IArchitectureEvaluator {
  private readonly conceptLibrary: ConceptLibrary;
  private readonly patternDetector: PatternDetector;
  private readonly antiPatternDetector: AntiPatternDetector;
  private readonly strengthAnalyzer: StrengthAnalyzer;
  private readonly technicalDepthScorer: TechnicalDepthScorer;
  private readonly enableLogging: boolean;

  /**
   * Creates a new ArchitectureEvaluatorService instance
   * @param config - Configuration options
   */
  constructor(config: ArchitectureEvaluatorServiceConfig = {}) {
    this.conceptLibrary = config.conceptLibrary ?? new ConceptLibrary();
    this.patternDetector = new PatternDetector(this.conceptLibrary);
    this.antiPatternDetector = new AntiPatternDetector();
    this.strengthAnalyzer = new StrengthAnalyzer({
      minConfidence: config.minStrengthConfidence ?? 0.7
    });
    this.technicalDepthScorer = new TechnicalDepthScorer();
    this.enableLogging = config.enableLogging ?? false;
  }

  /**
   * Evaluates a candidate's response and returns comprehensive assessment
   * @param response - The candidate's response to evaluate
   * @returns Complete architecture evaluation result
   */
  public async evaluate(response: CandidateResponse): Promise<ArchitectureEvaluationResult> {
    try {
      this.log(`Starting evaluation for session: ${response.sessionId}`);
      this.log(`Topic: ${response.topic}`);
      this.log(`Response length: ${response.text.length} characters`);

      // Step 1: Detect concepts using PatternDetector
      this.log('Step 1: Detecting concepts...');
      const detectedConcepts = this.patternDetector.detectConcepts(response.text);
      this.log(`Found ${detectedConcepts.length} concepts`);

      // Step 2: Detect anti-patterns using AntiPatternDetector
      this.log('Step 2: Detecting anti-patterns...');
      const detectedAntiPatterns = this.antiPatternDetector.detectAntiPatterns(response.text);
      this.log(`Found ${detectedAntiPatterns.length} anti-patterns`);

      // Step 3: Analyze strengths using StrengthAnalyzer
      this.log('Step 3: Analyzing strengths...');
      const identifiedStrengths = this.strengthAnalyzer.analyzeStrengths(
        detectedConcepts,
        response.text
      );
      this.log(`Identified ${identifiedStrengths.length} strengths`);

      // Step 4: Calculate technical depth score using TechnicalDepthScorer
      this.log('Step 4: Calculating technical depth score...');
      const scoreBreakdown = this.technicalDepthScorer.calculateWithBreakdown(
        detectedConcepts,
        detectedAntiPatterns
      );
      this.log(`Score: ${scoreBreakdown.score} (base: ${scoreBreakdown.baseScore}, penalty: ${scoreBreakdown.totalPenalty})`);

      // Step 5: Build the result
      this.log('Step 5: Building evaluation result...');
      const result = this.buildResult(
        detectedConcepts,
        detectedAntiPatterns,
        identifiedStrengths,
        scoreBreakdown.score
      );

      this.log('Evaluation complete');
      return result;
    } catch (error) {
      this.log(`Error during evaluation: ${error}`);
      throw new Error(`Failed to evaluate response: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  /**
   * Builds the final evaluation result from component outputs
   */
  private buildResult(
    detectedConcepts: ReturnType<PatternDetector['detectConcepts']>,
    detectedAntiPatterns: ReturnType<AntiPatternDetector['detectAntiPatterns']>,
    identifiedStrengths: ReturnType<StrengthAnalyzer['analyzeStrengths']>,
    technicalDepthScore: number
  ): ArchitectureEvaluationResult {
    // Extract covered keywords (concept names)
    const coveredKeywords = this.patternDetector.getCoveredKeywords(detectedConcepts);

    // Extract detected patterns (concept names that are patterns)
    const detectedPatterns = detectedConcepts
      .filter(c => c.category === 'pattern' || c.category === 'technology')
      .map(c => c.name);

    // Extract anti-pattern names
    const antiPatterns = this.antiPatternDetector.getAntiPatternNames(detectedAntiPatterns);

    // Calculate missed concepts
    const missedConcepts = this.patternDetector.getMissedConcepts(detectedConcepts);

    // Extract strength descriptions
    const strengths = identifiedStrengths.map(s => s.description);

    return {
      technicalDepth: Math.round(technicalDepthScore),
      coveredKeywords,
      detectedPatterns,
      antiPatterns,
      missedConcepts,
      strengths
    };
  }

  /**
   * Logs a message if logging is enabled
   */
  private log(message: string): void {
    if (this.enableLogging) {
      console.log(`[ArchitectureEvaluator] ${message}`);
    }
  }

  /**
   * Gets the internal components for testing/inspection
   */
  public getComponents() {
    return {
      conceptLibrary: this.conceptLibrary,
      patternDetector: this.patternDetector,
      antiPatternDetector: this.antiPatternDetector,
      strengthAnalyzer: this.strengthAnalyzer,
      technicalDepthScorer: this.technicalDepthScorer
    };
  }
}
