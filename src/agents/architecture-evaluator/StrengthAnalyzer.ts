/**
 * Analyzes candidate responses to identify strengths based on detected concepts
 * @module StrengthAnalyzer
 */

import { DetectedConcept, IdentifiedStrength } from './types.js';
import { extractSentences, countWords } from '../../utils/textUtils.js';

/**
 * Configuration for StrengthAnalyzer
 */
export interface StrengthAnalyzerConfig {
  /** Minimum confidence threshold to consider a concept a strength (0.0 - 1.0) */
  minConfidence?: number;
  /** Minimum word count for evidence extraction */
  minEvidenceLength?: number;
}

/**
 * Analyzes detected concepts to identify and describe strengths in candidate responses
 */
export class StrengthAnalyzer {
  private readonly minConfidence: number;
  private readonly minEvidenceLength: number;

  /**
   * Creates a new StrengthAnalyzer instance
   * @param config - Configuration options
   */
  constructor(config: StrengthAnalyzerConfig = {}) {
    this.minConfidence = config.minConfidence ?? 0.7;
    this.minEvidenceLength = config.minEvidenceLength ?? 10;
  }

  /**
   * Analyzes detected concepts and original text to identify strengths
   * @param detectedConcepts - Array of concepts detected in the response
   * @param originalText - The original candidate response text
   * @returns Array of identified strengths with descriptions and evidence
   */
  public analyzeStrengths(
    detectedConcepts: DetectedConcept[],
    originalText: string
  ): IdentifiedStrength[] {
    if (detectedConcepts.length === 0) {
      return [];
    }

    const strengths: IdentifiedStrength[] = [];
    const sentences = extractSentences(originalText);

    // Filter concepts by confidence threshold
    const highConfidenceConcepts = detectedConcepts.filter(
      concept => concept.confidence >= this.minConfidence
    );

    // Group concepts by category for better strength descriptions
    const conceptsByCategory = this.groupByCategory(highConfidenceConcepts);

    // Generate strengths for each category with multiple concepts
    for (const [category, concepts] of Object.entries(conceptsByCategory)) {
      if (concepts.length > 0) {
        const strength = this.createCategoryStrength(
          category,
          concepts,
          sentences
        );
        if (strength) {
          strengths.push(strength);
        }
      }
    }

    // Generate individual strengths for very high confidence concepts
    const veryHighConfidenceConcepts = highConfidenceConcepts.filter(
      c => c.confidence >= 0.9
    );

    for (const concept of veryHighConfidenceConcepts) {
      const strength = this.createIndividualStrength(concept, sentences);
      if (strength) {
        strengths.push(strength);
      }
    }

    // Check for comprehensive response (many concepts detected)
    if (highConfidenceConcepts.length >= 3) {
      const comprehensiveStrength = this.createComprehensiveStrength(
        highConfidenceConcepts.length,
        sentences
      );
      if (comprehensiveStrength) {
        strengths.push(comprehensiveStrength);
      }
    }

    return strengths;
  }

  /**
   * Groups detected concepts by their category
   */
  private groupByCategory(
    concepts: DetectedConcept[]
  ): Record<string, DetectedConcept[]> {
    const grouped: Record<string, DetectedConcept[]> = {};

    for (const concept of concepts) {
      if (!grouped[concept.category]) {
        grouped[concept.category] = [];
      }
      grouped[concept.category].push(concept);
    }

    return grouped;
  }

  /**
   * Creates a strength description for a category of related concepts
   */
  private createCategoryStrength(
    category: string,
    concepts: DetectedConcept[],
    sentences: string[]
  ): IdentifiedStrength | null {
    const categoryDescriptions: Record<string, string> = {
      architecture: 'Demonstrates strong understanding of architectural principles and patterns',
      pattern: 'Shows knowledge of proven design patterns and best practices',
      technology: 'Exhibits familiarity with modern technologies and frameworks',
      principle: 'Understands and applies important software development principles'
    };

    const baseDescription = categoryDescriptions[category] || 
      `Shows knowledge in ${category} concepts`;

    const conceptNames = concepts.map(c => c.name).join(', ');
    const description = `${baseDescription} (${conceptNames})`;

    // Find evidence from sentences
    const evidence = this.findEvidenceForConcepts(concepts, sentences);

    if (!evidence || evidence.length < this.minEvidenceLength) {
      return null;
    }

    return {
      description,
      relatedConcept: concepts[0]?.name,
      evidence
    };
  }

  /**
   * Creates a strength description for an individual high-confidence concept
   */
  private createIndividualStrength(
    concept: DetectedConcept,
    sentences: string[]
  ): IdentifiedStrength | null {
    const description = `Strong understanding of ${concept.name}`;
    const evidence = this.findEvidenceForConcept(concept, sentences);

    if (!evidence || evidence.length < this.minEvidenceLength) {
      return null;
    }

    return {
      description,
      relatedConcept: concept.name,
      evidence
    };
  }

  /**
   * Creates a strength description for comprehensive responses
   */
  private createComprehensiveStrength(
    conceptCount: number,
    sentences: string[]
  ): IdentifiedStrength | null {
    const description = `Comprehensive response covering ${conceptCount} key concepts`;
    
    // Use the first substantial sentence as evidence
    const evidence = sentences.find(s => countWords(s) >= 10) || sentences[0];

    if (!evidence || evidence.length < this.minEvidenceLength) {
      return null;
    }

    return {
      description,
      evidence
    };
  }

  /**
   * Finds evidence in sentences that mentions any of the given concepts
   */
  private findEvidenceForConcepts(
    concepts: DetectedConcept[],
    sentences: string[]
  ): string {
    for (const sentence of sentences) {
      const lowerSentence = sentence.toLowerCase();
      
      // Check if sentence mentions any of the concepts
      for (const concept of concepts) {
        if (lowerSentence.includes(concept.name.toLowerCase())) {
          return sentence.trim();
        }
      }
    }

    // Fallback: use the longest sentence as evidence
    return sentences.reduce((longest, current) => 
      current.length > longest.length ? current : longest, '');
  }

  /**
   * Finds evidence in sentences that mentions a specific concept
   */
  private findEvidenceForConcept(
    concept: DetectedConcept,
    sentences: string[]
  ): string {
    const conceptLower = concept.name.toLowerCase();
    
    for (const sentence of sentences) {
      if (sentence.toLowerCase().includes(conceptLower)) {
        return sentence.trim();
      }
    }

    // Fallback: use the longest sentence as evidence
    return sentences.reduce((longest, current) => 
      current.length > longest.length ? current : longest, '');
  }
}
