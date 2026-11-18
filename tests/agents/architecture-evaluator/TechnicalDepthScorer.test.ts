/**
 * Unit tests for TechnicalDepthScorer
 * @module TechnicalDepthScorer.test
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { TechnicalDepthScorer } from '../../../src/agents/architecture-evaluator/TechnicalDepthScorer.js';
import { DetectedConcept, DetectedAntiPattern } from '../../../src/agents/architecture-evaluator/types.js';

describe('TechnicalDepthScorer', () => {
  let scorer: TechnicalDepthScorer;

  beforeEach(() => {
    scorer = new TechnicalDepthScorer();
  });

  describe('calculateScore', () => {
    it('should return 0 for no concepts and no anti-patterns', () => {
      const score = scorer.calculateScore([], []);
      expect(score).toBe(0);
    });

    it('should calculate score based on concept weights', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', weight: 0.9, confidence: 1.0, category: 'technology', context: 'HNSW' },
        { name: 'Kafka', weight: 0.8, confidence: 1.0, category: 'technology', context: 'Kafka' }
      ];

      const score = scorer.calculateScore(concepts, []);
      
      // Weight sum = 0.9 + 0.8 = 1.7
      // Base score = 1.7 * 100 = 170 (capped at 100)
      expect(score).toBe(100);
    });

    it('should apply penalties from anti-patterns', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', weight: 0.9, confidence: 1.0, category: 'technology', context: 'HNSW' }
      ];

      const antiPatterns: DetectedAntiPattern[] = [
        { name: 'DelegatingMathToLLM', severity: 'high', penalty: 20, context: 'using LLM for math' }
      ];

      const score = scorer.calculateScore(concepts, antiPatterns);
      
      // Base score = 0.9 * 100 = 90
      // Penalty = 20
      // Final = 90 - 20 = 70
      expect(score).toBe(70);
    });

    it('should clamp score to minimum 0', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', weight: 0.3, confidence: 1.0, category: 'technology', context: 'HNSW' }
      ];

      const antiPatterns: DetectedAntiPattern[] = [
        { name: 'HardcodingSecrets', severity: 'high', penalty: 25, context: 'hardcoding keys' },
        { name: 'NoErrorHandling', severity: 'high', penalty: 15, context: 'no error handling' },
        { name: 'DelegatingMathToLLM', severity: 'high', penalty: 20, context: 'LLM for math' }
      ];

      const score = scorer.calculateScore(concepts, antiPatterns);
      
      // Base score = 0.3 * 100 = 30
      // Penalty = 25 + 15 + 20 = 60
      // Final = 30 - 60 = -30 (clamped to 0)
      expect(score).toBe(0);
    });

    it('should clamp score to maximum 100', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', weight: 0.9, confidence: 1.0, category: 'technology', context: 'HNSW' },
        { name: 'Kafka', weight: 0.8, confidence: 1.0, category: 'technology', context: 'Kafka' },
        { name: 'Polly', weight: 0.85, confidence: 1.0, category: 'technology', context: 'Polly' }
      ];

      const score = scorer.calculateScore(concepts, []);
      
      // Weight sum = 0.9 + 0.8 + 0.85 = 2.55
      // Base score = 2.55 * 100 = 255 (capped at 100)
      expect(score).toBe(100);
    });

    it('should factor in confidence when calculating contribution', () => {
      const highConfidence: DetectedConcept[] = [
        { name: 'HNSW', weight: 0.9, confidence: 1.0, category: 'technology', context: 'HNSW' }
      ];

      const lowConfidence: DetectedConcept[] = [
        { name: 'HNSW', weight: 0.9, confidence: 0.5, category: 'technology', context: 'HNSW' }
      ];

      const highScore = scorer.calculateScore(highConfidence, []);
      const lowScore = scorer.calculateScore(lowConfidence, []);

      // High confidence: 0.9 * 1.0 * 100 = 90
      // Low confidence: 0.9 * 0.5 * 100 = 45
      expect(highScore).toBeGreaterThan(lowScore);
    });

    it('should handle multiple anti-patterns', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', weight: 0.9, confidence: 1.0, category: 'technology', context: 'HNSW' }
      ];

      const antiPatterns: DetectedAntiPattern[] = [
        { name: 'DelegatingMathToLLM', severity: 'high', penalty: 20, context: 'LLM for math' },
        { name: 'HardcodingSecrets', severity: 'high', penalty: 25, context: 'hardcoding keys' },
        { name: 'NoErrorHandling', severity: 'high', penalty: 15, context: 'no error handling' }
      ];

      const score = scorer.calculateScore(concepts, antiPatterns);
      
      // Base score = 0.9 * 100 = 90
      // Penalty = 20 + 25 + 15 = 60
      // Final = 90 - 60 = 30
      expect(score).toBe(30);
    });

    it('should handle concepts with zero weight', () => {
      const concepts: DetectedConcept[] = [
        { name: 'Test', weight: 0, confidence: 1.0, category: 'technology', context: 'test' }
      ];

      const score = scorer.calculateScore(concepts, []);
      expect(score).toBe(0);
    });
  });

  describe('calculateWithBreakdown', () => {
    it('should provide detailed breakdown', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', weight: 0.9, confidence: 0.95, category: 'technology', context: 'HNSW' },
        { name: 'Kafka', weight: 0.8, confidence: 0.9, category: 'technology', context: 'Kafka' }
      ];

      const antiPatterns: DetectedAntiPattern[] = [
        { name: 'NoErrorHandling', severity: 'high', penalty: 15, context: 'no error handling' }
      ];

      const breakdown = scorer.calculateWithBreakdown(concepts, antiPatterns);

      expect(breakdown.score).toBeDefined();
      expect(breakdown.baseScore).toBeDefined();
      expect(breakdown.totalPenalty).toBe(15);
      expect(breakdown.weightSum).toBeGreaterThan(0);
      expect(breakdown.conceptCount).toBe(2);
      expect(breakdown.antiPatternCount).toBe(1);
      expect(breakdown.conceptContributions.length).toBe(2);
      expect(breakdown.antiPatternPenalties.length).toBe(1);
    });

    it('should calculate correct base score', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', weight: 0.9, confidence: 1.0, category: 'technology', context: 'HNSW' }
      ];

      const breakdown = scorer.calculateWithBreakdown(concepts, []);

      // Weight sum = 0.9 * 1.0 = 0.9
      // Base score = 0.9 * 100 = 90
      expect(breakdown.weightSum).toBeCloseTo(0.9, 2);
      expect(breakdown.baseScore).toBe(90);
    });

    it('should track all concept contributions', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', weight: 0.9, confidence: 0.95, category: 'technology', context: 'HNSW' },
        { name: 'Kafka', weight: 0.8, confidence: 0.9, category: 'technology', context: 'Kafka' }
      ];

      const breakdown = scorer.calculateWithBreakdown(concepts, []);

      expect(breakdown.conceptContributions).toHaveLength(2);
      expect(breakdown.conceptContributions[0].conceptName).toBe('HNSW');
      expect(breakdown.conceptContributions[1].conceptName).toBe('Kafka');
    });

    it('should track all anti-pattern penalties', () => {
      const antiPatterns: DetectedAntiPattern[] = [
        { name: 'DelegatingMathToLLM', severity: 'high', penalty: 20, context: 'LLM for math' },
        { name: 'HardcodingSecrets', severity: 'high', penalty: 25, context: 'hardcoding' }
      ];

      const breakdown = scorer.calculateWithBreakdown([], antiPatterns);

      expect(breakdown.antiPatternPenalties).toHaveLength(2);
      expect(breakdown.totalPenalty).toBe(45);
    });
  });

  describe('configuration', () => {
    it('should use custom configuration', () => {
      const customScorer = new TechnicalDepthScorer({
        maxScore: 50,
        minScore: 10,
        weightMultiplier: 50
      });

      const concepts: DetectedConcept[] = [
        { name: 'HNSW', weight: 0.9, confidence: 1.0, category: 'technology', context: 'HNSW' }
      ];

      const breakdown = customScorer.calculateWithBreakdown(concepts, []);

      // With multiplier 50: 0.9 * 50 = 45
      expect(breakdown.baseScore).toBe(45);
      
      const config = customScorer.getConfig();
      expect(config.maxScore).toBe(50);
      expect(config.minScore).toBe(10);
      expect(config.weightMultiplier).toBe(50);
    });

    it('should clamp to custom min/max scores', () => {
      const customScorer = new TechnicalDepthScorer({
        maxScore: 80,
        minScore: 20
      });

      const highConcepts: DetectedConcept[] = [
        { name: 'HNSW', weight: 0.9, confidence: 1.0, category: 'technology', context: 'HNSW' },
        { name: 'Kafka', weight: 0.8, confidence: 1.0, category: 'technology', context: 'Kafka' }
      ];

      const lowConcepts: DetectedConcept[] = [
        { name: 'Test', weight: 0.1, confidence: 1.0, category: 'technology', context: 'test' }
      ];

      const highAntiPatterns: DetectedAntiPattern[] = [
        { name: 'HardcodingSecrets', severity: 'high', penalty: 50, context: 'hardcoding' }
      ];

      const highScore = customScorer.calculateScore(highConcepts, []);
      const lowScore = customScorer.calculateScore(lowConcepts, highAntiPatterns);

      expect(highScore).toBe(80); // Capped at maxScore
      expect(lowScore).toBe(20); // Clamped at minScore
    });
  });
});
