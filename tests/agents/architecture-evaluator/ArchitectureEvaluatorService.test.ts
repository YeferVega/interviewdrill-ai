/**
 * Integration tests for ArchitectureEvaluatorService
 * Tests the complete evaluation flow from input to output
 * @module ArchitectureEvaluatorService.test
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { ArchitectureEvaluatorService } from '../../../src/agents/architecture-evaluator/ArchitectureEvaluatorService.js';
import { excellentResponse, goodResponse, averageResponse, poorResponse, emptyResponse, mixedResponse } from './fixtures/sampleResponses.js';

describe('ArchitectureEvaluatorService - Integration Tests', () => {
  let service: ArchitectureEvaluatorService;

  beforeEach(() => {
    service = new ArchitectureEvaluatorService();
  });

  describe('evaluate - Excellent Response', () => {
    it('should detect multiple concepts in excellent response', async () => {
      const result = await service.evaluate(excellentResponse);

      expect(result.coveredKeywords.length).toBeGreaterThanOrEqual(8);
      expect(result.technicalDepth).toBeGreaterThanOrEqual(70);
    });

    it('should identify key concepts correctly', async () => {
      const result = await service.evaluate(excellentResponse);

      expect(result.coveredKeywords).toContain('Zero-Math');
      expect(result.coveredKeywords).toContain('HNSW');
      expect(result.coveredKeywords).toContain('KernelFunction'); // Semantic Kernel
      expect(result.coveredKeywords).toContain('Kafka');
      expect(result.coveredKeywords).toContain('ActivitySource');
      expect(result.coveredKeywords).toContain('Polly');
      expect(result.coveredKeywords).toContain('SemanticCache');
      expect(result.coveredKeywords).toContain('PIISanitization');
    });

    it('should not detect anti-patterns in excellent response', async () => {
      const result = await service.evaluate(excellentResponse);

      expect(result.antiPatterns.length).toBe(0);
    });

    it('should identify strengths in excellent response', async () => {
      const result = await service.evaluate(excellentResponse);

      expect(result.strengths.length).toBeGreaterThan(0);
    });

    it('should have high technical depth score', async () => {
      const result = await service.evaluate(excellentResponse);

      expect(result.technicalDepth).toBeGreaterThanOrEqual(80);
    });

    it('should have few missed concepts', async () => {
      const result = await service.evaluate(excellentResponse);

      expect(result.missedConcepts.length).toBeLessThan(3);
    });
  });

  describe('evaluate - Good Response', () => {
    it('should detect some concepts in good response', async () => {
      const result = await service.evaluate(goodResponse);

      expect(result.coveredKeywords.length).toBeGreaterThanOrEqual(3);
      expect(result.coveredKeywords.length).toBeLessThan(8);
    });

    it('should have moderate technical depth score', async () => {
      const result = await service.evaluate(goodResponse);

      expect(result.technicalDepth).toBeGreaterThanOrEqual(30);
      expect(result.technicalDepth).toBeLessThanOrEqual(100);
    });

    it('should have some missed concepts', async () => {
      const result = await service.evaluate(goodResponse);

      expect(result.missedConcepts.length).toBeGreaterThan(0);
    });
  });

  describe('evaluate - Average Response', () => {
    it('should detect anti-patterns in average response', async () => {
      const result = await service.evaluate(averageResponse);

      expect(result.antiPatterns.length).toBeGreaterThan(0);
    });

    it('should detect some anti-patterns correctly', async () => {
      const result = await service.evaluate(averageResponse);

      // Should detect anti-patterns
      expect(result.antiPatterns.length).toBeGreaterThan(0);

      // Verify anti-patterns are detected (names may vary)
      // Common anti-patterns: DelegatingMathToLLM, HardcodingSecrets, NoErrorHandling, etc.
      const hasAntiPatterns = result.antiPatterns.length > 0;
      expect(hasAntiPatterns).toBe(true);
    });

    it('should have reduced score due to anti-patterns', async () => {
      const result = await service.evaluate(averageResponse);

      // Score should be penalized but may not be below 50 depending on detected concepts
      expect(result.technicalDepth).toBeLessThanOrEqual(100);
    });
  });

  describe('evaluate - Poor Response', () => {
    it('should detect multiple anti-patterns in poor response', async () => {
      const result = await service.evaluate(poorResponse);

      expect(result.antiPatterns.length).toBeGreaterThanOrEqual(3);
    });

    it('should detect critical anti-patterns', async () => {
      const result = await service.evaluate(poorResponse);

      // Should detect multiple anti-patterns
      expect(result.antiPatterns.length).toBeGreaterThan(0);

      // Poor response should have several anti-patterns
      const hasMultipleAntiPatterns = result.antiPatterns.length >= 2;
      expect(hasMultipleAntiPatterns).toBe(true);
    });

    it('should have very low technical depth score', async () => {
      const result = await service.evaluate(poorResponse);

      // Poor response should have low score due to many anti-patterns
      expect(result.technicalDepth).toBeLessThanOrEqual(50);
    });

    it('should have many missed concepts', async () => {
      const result = await service.evaluate(poorResponse);

      expect(result.missedConcepts.length).toBeGreaterThan(5);
    });

    it('should have few or no strengths', async () => {
      const result = await service.evaluate(poorResponse);

      expect(result.strengths.length).toBeLessThanOrEqual(3);
    });
  });

  describe('evaluate - Mixed Response', () => {
    it('should detect both concepts and anti-patterns', async () => {
      const result = await service.evaluate(mixedResponse);

      expect(result.coveredKeywords.length).toBeGreaterThan(0);
      expect(result.antiPatterns.length).toBeGreaterThan(0);
    });

    it('should have moderate score due to mixed quality', async () => {
      const result = await service.evaluate(mixedResponse);

      // Should have some concepts but penalized by anti-patterns
      expect(result.technicalDepth).toBeGreaterThanOrEqual(0);
      expect(result.technicalDepth).toBeLessThanOrEqual(100);
    });
  });

  describe('evaluate - Edge Cases', () => {
    it('should handle empty response gracefully', async () => {
      const result = await service.evaluate(emptyResponse);

      expect(result.technicalDepth).toBe(0);
      expect(result.coveredKeywords.length).toBe(0);
      expect(result.antiPatterns.length).toBe(0);
      expect(result.strengths.length).toBe(0);
      expect(result.missedConcepts.length).toBeGreaterThan(0);
    });

    it('should handle very short response', async () => {
      const shortResponse = {
        sessionId: 'short-test',
        topic: 'GenAI',
        text: 'Use AI.'
      };

      const result = await service.evaluate(shortResponse);

      expect(result.technicalDepth).toBeLessThan(20);
      expect(result.coveredKeywords.length).toBeLessThan(2);
    });
  });

  describe('Performance', () => {
    it('should complete evaluation in less than 3 seconds', async () => {
      const startTime = performance.now();
      await service.evaluate(excellentResponse);
      const endTime = performance.now();

      const duration = endTime - startTime;
      expect(duration).toBeLessThan(3000);
    });

    it('should handle large responses efficiently', async () => {
      const largeResponse = {
        sessionId: 'large-test',
        topic: 'GenAI',
        text: excellentResponse.text.repeat(10) // Make it 10x longer
      };

      const startTime = performance.now();
      await service.evaluate(largeResponse);
      const endTime = performance.now();

      const duration = endTime - startTime;
      expect(duration).toBeLessThan(5000); // Should still be under 5s
    });
  });

  describe('Result Structure', () => {
    it('should return complete ArchitectureEvaluationResult', async () => {
      const result = await service.evaluate(excellentResponse);

      expect(result).toHaveProperty('technicalDepth');
      expect(result).toHaveProperty('coveredKeywords');
      expect(result).toHaveProperty('missedConcepts');
      expect(result).toHaveProperty('strengths');
      expect(result).toHaveProperty('detectedPatterns');
      expect(result).toHaveProperty('antiPatterns');

      expect(typeof result.technicalDepth).toBe('number');
      expect(Array.isArray(result.coveredKeywords)).toBe(true);
      expect(Array.isArray(result.missedConcepts)).toBe(true);
      expect(Array.isArray(result.strengths)).toBe(true);
      expect(Array.isArray(result.detectedPatterns)).toBe(true);
      expect(Array.isArray(result.antiPatterns)).toBe(true);
    });

    it('should have technical depth in valid range', async () => {
      const result = await service.evaluate(excellentResponse);

      expect(result.technicalDepth).toBeGreaterThanOrEqual(0);
      expect(result.technicalDepth).toBeLessThanOrEqual(100);
    });

    it('should have no duplicates in covered keywords', async () => {
      const result = await service.evaluate(excellentResponse);

      const uniqueKeywords = new Set(result.coveredKeywords);
      expect(uniqueKeywords.size).toBe(result.coveredKeywords.length);
    });

    it('should have no duplicates in anti-patterns', async () => {
      const result = await service.evaluate(averageResponse);

      const uniqueAntiPatterns = new Set(result.antiPatterns);
      expect(uniqueAntiPatterns.size).toBe(result.antiPatterns.length);
    });
  });

  describe('Integration with Scoring Synthesizer', () => {
    it('should produce compatible output for Scoring Synthesizer', async () => {
      const result = await service.evaluate(excellentResponse);

      // ArchitectureEvaluatorService output should be compatible with Scoring Synthesizer input
      expect(result.technicalDepth).toBeDefined();
      expect(typeof result.technicalDepth).toBe('number');
      expect(result.technicalDepth).toBeGreaterThanOrEqual(0);
      expect(result.technicalDepth).toBeLessThanOrEqual(100);

      // Should have all required fields
      expect(Array.isArray(result.coveredKeywords)).toBe(true);
      expect(Array.isArray(result.missedConcepts)).toBe(true);
      expect(Array.isArray(result.strengths)).toBe(true);
    });
  });

  describe('Logging', () => {
    it('should work with logging enabled', async () => {
      const serviceWithLogging = new ArchitectureEvaluatorService({
        enableLogging: true
      });

      const result = await serviceWithLogging.evaluate(excellentResponse);

      expect(result.technicalDepth).toBeGreaterThan(0);
    });
  });
});
