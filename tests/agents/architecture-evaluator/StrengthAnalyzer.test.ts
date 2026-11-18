/**
 * Unit tests for StrengthAnalyzer
 * @module StrengthAnalyzer.test
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { StrengthAnalyzer } from '../../../src/agents/architecture-evaluator/StrengthAnalyzer.js';
import { DetectedConcept, IdentifiedStrength } from '../../../src/agents/architecture-evaluator/types.js';

describe('StrengthAnalyzer', () => {
  let analyzer: StrengthAnalyzer;

  beforeEach(() => {
    analyzer = new StrengthAnalyzer();
  });

  describe('analyzeStrengths', () => {
    it('should return empty array for empty concepts list', () => {
      const result = analyzer.analyzeStrengths([], 'Some text here');
      expect(result).toEqual([]);
    });

    it('should not identify strengths for low confidence concepts', () => {
      const lowConfidenceConcepts: DetectedConcept[] = [
        { name: 'HNSW', confidence: 0.5, weight: 0.9, weight: 0.8, category: 'technology', context: 'using HNSW' },
        { name: 'Kafka', confidence: 0.3, weight: 0.8, weight: 0.8, category: 'technology', context: 'using Kafka' }
      ];

      const result = analyzer.analyzeStrengths(lowConfidenceConcepts, 'Some text with HNSW and Kafka');
      
      // No concepts meet the 0.7 threshold
      expect(result.length).toBe(0);
    });

    it('should identify strengths for high confidence concepts', () => {
      const highConfidenceConcepts: DetectedConcept[] = [
        { name: 'HNSW', confidence: 0.9, weight: 0.9, weight: 0.8, category: 'technology', context: 'using HNSW for vector search' },
        { name: 'Kafka', confidence: 0.8, weight: 0.8, weight: 0.8, category: 'technology', context: 'using Kafka for streaming' }
      ];

      const text = 'I would use HNSW for vector search and Kafka for event streaming';
      const result = analyzer.analyzeStrengths(highConfidenceConcepts, text);

      expect(result.length).toBeGreaterThan(0);
      expect(result.some(s => s.description.includes('HNSW'))).toBe(true);
    });

    it('should extract evidence from text containing the concept', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', confidence: 0.9, weight: 0.9, weight: 0.8, category: 'technology', context: 'HNSW is efficient' }
      ];

      const text = 'The weather is nice. I would use HNSW for vector search. The food was good.';
      const result = analyzer.analyzeStrengths(concepts, text);

      expect(result.length).toBeGreaterThan(0);
      const strength = result.find(s => s.relatedConcept === 'HNSW');
      expect(strength).toBeDefined();
      expect(strength?.evidence).toContain('HNSW');
    });

    it('should group concepts by category', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', confidence: 0.9, weight: 0.8, category: 'technology', context: 'HNSW' },
        { name: 'Kafka', confidence: 0.8, weight: 0.8, category: 'technology', context: 'Kafka' },
        { name: 'Zero-Math', confidence: 0.85, category: 'principle', context: 'Zero-Math' }
      ];

      const text = 'Using HNSW and Kafka for the system. Following Zero-Math principle.';
      const result = analyzer.analyzeStrengths(concepts, text);

      // Should have category-level strengths
      expect(result.length).toBeGreaterThan(0);
    });

    it('should identify comprehensive response for many concepts', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', confidence: 0.9, weight: 0.8, category: 'technology', context: 'HNSW' },
        { name: 'Kafka', confidence: 0.85, weight: 0.8, category: 'technology', context: 'Kafka' },
        { name: 'Polly', confidence: 0.8, weight: 0.8, category: 'technology', context: 'Polly' },
        { name: 'Semantic Kernel', confidence: 0.9, category: 'pattern', context: 'Semantic Kernel' }
      ];

      const text = 'I would use HNSW for search, Kafka for streaming, Polly for resilience, and Semantic Kernel for orchestration. This comprehensive approach covers all aspects.';
      const result = analyzer.analyzeStrengths(concepts, text);

      // Should have comprehensive strength
      const comprehensive = result.find(s => s.description.toLowerCase().includes('comprehensive'));
      expect(comprehensive).toBeDefined();
    });

    it('should use custom confidence threshold', () => {
      const customAnalyzer = new StrengthAnalyzer({ minConfidence: 0.5 });
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', confidence: 0.6, weight: 0.8, category: 'technology', context: 'HNSW' }
      ];

      const text = 'Using HNSW for vector search';
      const result = customAnalyzer.analyzeStrengths(concepts, text);

      // Should identify strength with lower threshold
      expect(result.length).toBeGreaterThan(0);
    });

    it('should handle text with no sentences gracefully', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', confidence: 0.9, weight: 0.8, category: 'technology', context: 'HNSW' }
      ];

      const text = '';
      const result = analyzer.analyzeStrengths(concepts, text);

      // Should handle gracefully (no evidence)
      expect(Array.isArray(result)).toBe(true);
    });

    it('should extract individual strengths for very high confidence concepts', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', confidence: 0.95, weight: 0.8, category: 'technology', context: 'HNSW is great' }
      ];

      const text = 'HNSW provides efficient vector search with O(log n) complexity';
      const result = analyzer.analyzeStrengths(concepts, text);

      // Should have individual strength for 0.9+ confidence
      const individual = result.find(s => s.description.includes('Strong understanding'));
      expect(individual).toBeDefined();
    });
  });

  describe('edge cases', () => {
    it('should handle very long text', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', confidence: 0.9, weight: 0.8, category: 'technology', context: 'HNSW' }
      ];

      const longText = 'A'.repeat(10000) + ' HNSW is used here. ' + 'B'.repeat(10000);
      const result = analyzer.analyzeStrengths(concepts, longText);

      expect(result.length).toBeGreaterThan(0);
    });

    it('should handle text with special characters', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', confidence: 0.9, weight: 0.8, category: 'technology', context: 'HNSW' }
      ];

      const text = 'Using HNSW! With special chars @#$%^&*() and punctuation.';
      const result = analyzer.analyzeStrengths(concepts, text);

      expect(result.length).toBeGreaterThan(0);
    });

    it('should handle duplicate concepts', () => {
      const concepts: DetectedConcept[] = [
        { name: 'HNSW', confidence: 0.9, weight: 0.8, category: 'technology', context: 'HNSW' },
        { name: 'HNSW', confidence: 0.85, weight: 0.8, category: 'technology', context: 'HNSW again' }
      ];

      const text = 'Using HNSW for search';
      const result = analyzer.analyzeStrengths(concepts, text);

      // Should handle duplicates gracefully
      expect(Array.isArray(result)).toBe(true);
    });
  });
});
