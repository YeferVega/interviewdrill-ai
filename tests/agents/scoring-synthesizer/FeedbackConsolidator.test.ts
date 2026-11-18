/**
 * FeedbackConsolidator Unit Tests
 */

import { describe, it, expect } from 'vitest';
import { FeedbackConsolidator } from '../../../src/agents/scoring-synthesizer/FeedbackConsolidator.ts';
import type { ArchitectureEvaluation, SecurityEvaluation } from '../../../src/agents/scoring-synthesizer/types.ts';

describe('FeedbackConsolidator', () => {
  describe('consolidate()', () => {
    it('should merge coveredKeywords without duplicates', () => {
      const consolidator = new FeedbackConsolidator();
      
      const archEval: ArchitectureEvaluation = {
        technicalDepth: 80,
        coveredKeywords: ['HNSW', 'Kafka', 'Polly'],
        missedConcepts: [],
        strengths: [],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 70,
        coveredKeywords: ['Kafka', 'PII sanitization', 'Rate limiting'],
        missedConcepts: [],
        strengths: [],
        securityConcerns: [],
        resilienceScore: 75,
      };

      const result = consolidator.consolidate(archEval, secEval);
      
      expect(result.coveredKeywords).toContain('HNSW');
      expect(result.coveredKeywords).toContain('Kafka');
      expect(result.coveredKeywords).toContain('Polly');
      expect(result.coveredKeywords).toContain('PII sanitization');
      expect(result.coveredKeywords).toContain('Rate limiting');
      expect(result.coveredKeywords.filter(k => k === 'Kafka').length).toBe(1); // No duplicates
    });

    it('should merge missedConcepts without duplicates', () => {
      const consolidator = new FeedbackConsolidator();
      
      const archEval: ArchitectureEvaluation = {
        technicalDepth: 60,
        coveredKeywords: [],
        missedConcepts: ['Zero-Math', 'ActivitySource'],
        strengths: [],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 50,
        coveredKeywords: [],
        missedConcepts: ['ActivitySource', 'Polly'],
        strengths: [],
        securityConcerns: [],
        resilienceScore: 45,
      };

      const result = consolidator.consolidate(archEval, secEval);
      
      expect(result.missedConcepts).toContain('Zero-Math');
      expect(result.missedConcepts).toContain('ActivitySource');
      expect(result.missedConcepts).toContain('Polly');
      expect(result.missedConcepts.filter(c => c === 'ActivitySource').length).toBe(1);
    });

    it('should merge strengths without duplicates', () => {
      const consolidator = new FeedbackConsolidator();
      
      const archEval: ArchitectureEvaluation = {
        technicalDepth: 85,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: ['Strong understanding of patterns', 'Good architecture'],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 80,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: ['Good architecture', 'Security-conscious'],
        securityConcerns: [],
        resilienceScore: 85,
      };

      const result = consolidator.consolidate(archEval, secEval);
      
      expect(result.strengths).toContain('Strong understanding of patterns');
      expect(result.strengths).toContain('Good architecture');
      expect(result.strengths).toContain('Security-conscious');
      expect(result.strengths.filter(s => s === 'Good architecture').length).toBe(1);
    });

    it('should handle empty arrays gracefully', () => {
      const consolidator = new FeedbackConsolidator();
      
      const archEval: ArchitectureEvaluation = {
        technicalDepth: 50,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 40,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        securityConcerns: [],
        resilienceScore: 35,
      };

      const result = consolidator.consolidate(archEval, secEval);
      
      expect(result.coveredKeywords).toEqual([]);
      expect(result.missedConcepts).toEqual([]);
      expect(result.strengths).toEqual([]);
      expect(result.improvementAdvice).toContain('Excellent performance');
    });

    it('should filter out empty strings', () => {
      const consolidator = new FeedbackConsolidator();
      
      const archEval: ArchitectureEvaluation = {
        technicalDepth: 70,
        coveredKeywords: ['HNSW', '', 'Kafka', '   '],
        missedConcepts: ['Zero-Math', ''],
        strengths: ['Good', ''],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 65,
        coveredKeywords: ['Polly', ''],
        missedConcepts: ['ActivitySource', ''],
        strengths: ['Security', ''],
        securityConcerns: [],
        resilienceScore: 70,
      };

      const result = consolidator.consolidate(archEval, secEval);
      
      expect(result.coveredKeywords).not.toContain('');
      expect(result.coveredKeywords).not.toContain('   ');
      expect(result.missedConcepts).not.toContain('');
      expect(result.strengths).not.toContain('');
    });

    it('should generate advice based on missed concepts', () => {
      const consolidator = new FeedbackConsolidator();
      
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

      const result = consolidator.consolidate(archEval, secEval);
      
      expect(result.improvementAdvice).toContain('Zero-Math');
      expect(result.improvementAdvice).toContain('HNSW');
      expect(result.improvementAdvice).toContain('sanitization');
    });

    it('should return generic advice when no concepts are missed', () => {
      const consolidator = new FeedbackConsolidator();
      
      const archEval: ArchitectureEvaluation = {
        technicalDepth: 90,
        coveredKeywords: ['All concepts'],
        missedConcepts: [],
        strengths: ['Excellent'],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 85,
        coveredKeywords: ['Security'],
        missedConcepts: [],
        strengths: ['Strong'],
        securityConcerns: [],
        resilienceScore: 90,
      };

      const result = consolidator.consolidate(archEval, secEval);
      
      expect(result.improvementAdvice).toContain('Excellent performance');
    });

    it('should use custom advice templates when provided', () => {
      const customTemplates = new Map([
        ['custom-concept', 'Custom advice for this concept.'],
      ]);

      const consolidator = new FeedbackConsolidator({
        adviceTemplates: customTemplates,
      });
      
      const archEval: ArchitectureEvaluation = {
        technicalDepth: 60,
        coveredKeywords: [],
        missedConcepts: ['custom-concept'],
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
        resilienceScore: 45,
      };

      const result = consolidator.consolidate(archEval, secEval);
      
      expect(result.improvementAdvice).toContain('Custom advice for this concept');
    });

    it('should truncate advice if too long', () => {
      const consolidator = new FeedbackConsolidator({
        maxAdviceLength: 100,
      });
      
      const archEval: ArchitectureEvaluation = {
        technicalDepth: 50,
        coveredKeywords: [],
        missedConcepts: [
          'Zero-Math',
          'KernelFunction',
          'HNSW',
          'Kafka',
          'ActivitySource',
          'Polly',
          'PII sanitization',
          'Rate limiting',
          'Semantic cache',
          'Latency optimization',
          'Human-in-the-loop',
        ],
        strengths: [],
        detectedPatterns: [],
        antiPatterns: [],
      };

      const secEval: SecurityEvaluation = {
        productionReadiness: 40,
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        securityConcerns: [],
        resilienceScore: 35,
      };

      const result = consolidator.consolidate(archEval, secEval);
      
      expect(result.improvementAdvice.length).toBeLessThanOrEqual(100);
    });
  });
});
