/**
 * Integration Tests for ScoringSynthesizerService
 * 
 * Tests the complete scoring pipeline end-to-end
 */

import { describe, it, expect } from 'vitest';
import { ScoringSynthesizerService } from '../../src/agents/scoring-synthesizer/ScoringSynthesizerService.ts';
import type { ArchitectureEvaluation, SecurityEvaluation } from '../../src/agents/scoring-synthesizer/types.ts';

describe('ScoringSynthesizerService - Integration Tests', () => {
  describe('Happy Path', () => {
    it('should synthesize a PASS evaluation correctly', async () => {
      const service = new ScoringSynthesizerService();

      const archEval: ArchitectureEvaluation = {
        technicalDepth: 90,
        coveredKeywords: ['HNSW', 'Kafka', 'Zero-Math'],
        missedConcepts: [],
        strengths: ['Strong understanding of vector search'],
        detectedPatterns: ['HNSW'],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 85,
        coveredKeywords: ['PII sanitization', 'Rate limiting'],
        missedConcepts: [],
        strengths: ['Good security practices'],
        securityConcerns: [],
        resilienceScore: 90,
      };

      const result = await service.synthesize(archEval, secEval);

      // Score: (90 * 0.6) + (85 * 0.4) = 54 + 34 = 88
      expect(result.overallScore).toBe(88);
      expect(result.technicalDepth).toBe(90);
      expect(result.productionReadiness).toBe(85);
      expect(result.verdict).toBe('PASS');
      expect(result.coveredKeywords).toContain('HNSW');
      expect(result.coveredKeywords).toContain('PII sanitization');
      expect(result.missedConcepts).toEqual([]);
      expect(result.strengths.length).toBeGreaterThan(0);
      expect(result.improvementAdvice).toContain('Excellent performance');
    });

    it('should synthesize a NEEDS_PRACTICE evaluation correctly', async () => {
      const service = new ScoringSynthesizerService();

      const archEval: ArchitectureEvaluation = {
        technicalDepth: 70,
        coveredKeywords: ['Semantic Kernel'],
        missedConcepts: ['HNSW', 'Zero-Math'],
        strengths: ['Basic understanding'],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 60,
        coveredKeywords: ['Rate limiting'],
        missedConcepts: ['PII sanitization'],
        strengths: ['Some security awareness'],
        securityConcerns: [],
        resilienceScore: 65,
      };

      const result = await service.synthesize(archEval, secEval);

      // Score: (70 * 0.6) + (60 * 0.4) = 42 + 24 = 66
      expect(result.overallScore).toBe(66);
      expect(result.verdict).toBe('NEEDS_PRACTICE');
      expect(result.missedConcepts).toContain('HNSW');
      expect(result.missedConcepts).toContain('PII sanitization');
      expect(result.improvementAdvice).toContain('Focus on improving');
    });

    it('should synthesize a FAIL evaluation correctly', async () => {
      const service = new ScoringSynthesizerService();

      const archEval: ArchitectureEvaluation = {
        technicalDepth: 40,
        coveredKeywords: [],
        missedConcepts: ['Zero-Math', 'HNSW', 'Kafka'],
        strengths: [],
        detectedPatterns: [],
        antiPatterns: ['Delegating math to LLM'],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 30,
        coveredKeywords: [],
        missedConcepts: ['PII sanitization', 'Rate limiting'],
        strengths: [],
        securityConcerns: ['No PII protection'],
        resilienceScore: 25,
      };

      const result = await service.synthesize(archEval, secEval);

      // Score: (40 * 0.6) + (30 * 0.4) = 24 + 12 = 36
      expect(result.overallScore).toBe(36);
      expect(result.verdict).toBe('FAIL');
      expect(result.missedConcepts.length).toBeGreaterThan(0);
    });
  });

  describe('Boundary Conditions', () => {
    it('should return PASS at exactly 80', async () => {
      const service = new ScoringSynthesizerService();

      const archEval: ArchitectureEvaluation = {
        technicalDepth: 80,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 80,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        securityConcerns: [],
        resilienceScore: 80,
      };

      const result = await service.synthesize(archEval, secEval);
      // Score: (80 * 0.6) + (80 * 0.4) = 48 + 32 = 80
      expect(result.overallScore).toBe(80);
      expect(result.verdict).toBe('PASS');
    });

    it('should return NEEDS_PRACTICE at exactly 60', async () => {
      const service = new ScoringSynthesizerService();

      const archEval: ArchitectureEvaluation = {
        technicalDepth: 60,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 60,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        securityConcerns: [],
        resilienceScore: 60,
      };

      const result = await service.synthesize(archEval, secEval);
      // Score: (60 * 0.6) + (60 * 0.4) = 36 + 24 = 60
      expect(result.overallScore).toBe(60);
      expect(result.verdict).toBe('NEEDS_PRACTICE');
    });

    it('should return FAIL below 60', async () => {
      const service = new ScoringSynthesizerService();

      const archEval: ArchitectureEvaluation = {
        technicalDepth: 50,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 50,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        securityConcerns: [],
        resilienceScore: 50,
      };

      const result = await service.synthesize(archEval, secEval);
      // Score: (50 * 0.6) + (50 * 0.4) = 30 + 20 = 50
      expect(result.overallScore).toBe(50);
      expect(result.verdict).toBe('FAIL');
    });
  });

  describe('Feedback Consolidation', () => {
    it('should merge keywords without duplicates', async () => {
      const service = new ScoringSynthesizerService();

      const archEval: ArchitectureEvaluation = {
        technicalDepth: 80,
        coveredKeywords: ['HNSW', 'Kafka', 'Polly'],
        missedConcepts: [],
        strengths: [],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 80,
        coveredKeywords: ['Kafka', 'PII sanitization'],
        missedConcepts: [],
        strengths: [],
        securityConcerns: [],
        resilienceScore: 80,
      };

      const result = await service.synthesize(archEval, secEval);

      expect(result.coveredKeywords).toContain('HNSW');
      expect(result.coveredKeywords).toContain('Kafka');
      expect(result.coveredKeywords).toContain('Polly');
      expect(result.coveredKeywords).toContain('PII sanitization');
      // Kafka should appear only once
      expect(result.coveredKeywords.filter(k => k === 'Kafka').length).toBe(1);
    });

    it('should generate advice based on missed concepts', async () => {
      const service = new ScoringSynthesizerService();

      const archEval: ArchitectureEvaluation = {
        technicalDepth: 60,
        coveredKeywords: [],
        missedConcepts: ['Zero-Math', 'HNSW'],
        strengths: [],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 50,
        coveredKeywords: [],
        missedConcepts: ['PII sanitization'],
        strengths: [],
        securityConcerns: [],
        resilienceScore: 45,
      };

      const result = await service.synthesize(archEval, secEval);

      expect(result.improvementAdvice).toContain('Zero-Math');
      expect(result.improvementAdvice).toContain('HNSW');
      expect(result.improvementAdvice).toContain('sanitization');
    });
  });

  describe('Error Handling', () => {
    it('should throw error for invalid technicalDepth', async () => {
      const service = new ScoringSynthesizerService();

      const archEval: ArchitectureEvaluation = {
        technicalDepth: -10,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 50,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        securityConcerns: [],
        resilienceScore: 50,
      };

      await expect(service.synthesize(archEval, secEval)).rejects.toThrow();
    });

    it('should throw error for invalid productionReadiness', async () => {
      const service = new ScoringSynthesizerService();

      const archEval: ArchitectureEvaluation = {
        technicalDepth: 50,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 150,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        securityConcerns: [],
        resilienceScore: 50,
      };

      await expect(service.synthesize(archEval, secEval)).rejects.toThrow();
    });
  });

  describe('Custom Configuration', () => {
    it('should work with custom scoring weights', async () => {
      const service = new ScoringSynthesizerService({
        scoreCalculator: {
          weights: { technical: 0.7, production: 0.3 },
        },
      });

      const archEval: ArchitectureEvaluation = {
        technicalDepth: 80,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 70,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        securityConcerns: [],
        resilienceScore: 70,
      };

      const result = await service.synthesize(archEval, secEval);
      // Score: (80 * 0.7) + (70 * 0.3) = 56 + 21 = 77
      expect(result.overallScore).toBe(77);
    });

    it('should work with debug logging enabled', async () => {
      const service = new ScoringSynthesizerService({
        enableDebugLogging: true,
      });

      const archEval: ArchitectureEvaluation = {
        technicalDepth: 80,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 80,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        securityConcerns: [],
        resilienceScore: 80,
      };

      // Should not throw
      const result = await service.synthesize(archEval, secEval);
      expect(result.verdict).toBe('PASS');
    });
  });

  describe('Performance', () => {
    it('should complete synthesis within 2 seconds', async () => {
      const service = new ScoringSynthesizerService();

      const archEval: ArchitectureEvaluation = {
        technicalDepth: 85,
        coveredKeywords: ['HNSW', 'Kafka', 'Zero-Math'],
        missedConcepts: ['Polly'],
        strengths: ['Strong patterns'],
        detectedPatterns: ['HNSW'],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 75,
        coveredKeywords: ['PII sanitization'],
        missedConcepts: ['Rate limiting'],
        strengths: ['Good security'],
        securityConcerns: [],
        resilienceScore: 80,
      };

      const start = performance.now();
      await service.synthesize(archEval, secEval);
      const elapsed = performance.now() - start;

      expect(elapsed).toBeLessThan(2000); // < 2s
    });
  });
});
