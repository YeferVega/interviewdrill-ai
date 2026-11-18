/**
 * PatternDetector - Detecta patrones y conceptos técnicos en texto
 * 
 * Analiza texto de respuestas de candidatos para identificar conceptos
 * técnicos clave usando keywords y regex patterns.
 * 
 * @module PatternDetector
 */

import type { ConceptDefinition, DetectedConcept } from './types.js';
import { ConceptLibrary } from './ConceptLibrary.js';

/**
 * PatternDetector - Detecta conceptos técnicos en texto
 */
export class PatternDetector {
  private conceptLibrary: ConceptLibrary;

  constructor(conceptLibrary?: ConceptLibrary) {
    this.conceptLibrary = conceptLibrary || new ConceptLibrary();
  }

  /**
   * Detecta todos los conceptos en el texto
   * 
   * @param text - Texto a analizar
   * @returns Lista de conceptos detectados con metadata
   */
  public detectConcepts(text: string): DetectedConcept[] {
    const normalizedText = this.normalizeText(text);
    const detectedConcepts: DetectedConcept[] = [];

    for (const concept of this.conceptLibrary.getAllConcepts()) {
      const detection = this.detectConcept(normalizedText, concept);
      if (detection) {
        detectedConcepts.push(detection);
      }
    }

    return detectedConcepts;
  }

  /**
   * Detecta un concepto específico en el texto
   */
  private detectConcept(text: string, concept: ConceptDefinition): DetectedConcept | null {
    // Try keyword matching first (faster)
    const keywordMatch = this.matchKeywords(text, concept.keywords);
    
    // Try pattern matching (more accurate)
    const patternMatch = this.matchPatterns(text, concept.patterns);

    // If either matched, return detected concept
    if (keywordMatch || patternMatch) {
      const confidence = this.calculateConfidence(text, concept, keywordMatch, patternMatch);
      const context = this.extractContext(text, concept);

      return {
        name: concept.name,
        confidence,
        weight: concept.weight,
        context,
        category: concept.category,
      };
    }

    return null;
  }

  /**
   * Busca keywords en el texto
   */
  private matchKeywords(text: string, keywords: string[]): boolean {
    const lowerText = text.toLowerCase();
    return keywords.some(keyword => {
      const lowerKeyword = keyword.toLowerCase();
      return lowerText.includes(lowerKeyword);
    });
  }

  /**
   * Busca patrones regex en el texto
   */
  private matchPatterns(text: string, patterns: RegExp[]): boolean {
    return patterns.some(pattern => pattern.test(text));
  }

  /**
   * Calcula el nivel de confianza de la detección
   */
  private calculateConfidence(
    text: string,
    concept: ConceptDefinition,
    keywordMatch: boolean,
    patternMatch: boolean
  ): number {
    let confidence = 0.0;

    // Base confidence from concept weight
    confidence += concept.weight * 0.3;

    // Boost if keyword matched
    if (keywordMatch) {
      confidence += 0.3;
    }

    // Boost if pattern matched (more reliable)
    if (patternMatch) {
      confidence += 0.4;
    }

    // Boost if multiple indicators found
    const keywordMatches = concept.keywords.filter(k => 
      text.toLowerCase().includes(k.toLowerCase())
    ).length;
    
    if (keywordMatches > 1) {
      confidence += Math.min(keywordMatches * 0.05, 0.2);
    }

    // Cap at 1.0
    return Math.min(confidence, 1.0);
  }

  /**
   * Extrae contexto donde se encontró el concepto
   */
  private extractContext(text: string, concept: ConceptDefinition): string {
    // Find first occurrence of any keyword or pattern
    const lowerText = text.toLowerCase();
    
    for (const keyword of concept.keywords) {
      const index = lowerText.indexOf(keyword.toLowerCase());
      if (index !== -1) {
        // Extract surrounding context (100 chars before and after)
        const start = Math.max(0, index - 100);
        const end = Math.min(text.length, index + keyword.length + 100);
        return text.substring(start, end).trim();
      }
    }

    // If no keyword found, try patterns
    for (const pattern of concept.patterns) {
      const match = text.match(pattern);
      if (match && match.index !== undefined) {
        const start = Math.max(0, match.index - 100);
        const end = Math.min(text.length, match.index + match[0].length + 100);
        return text.substring(start, end).trim();
      }
    }

    return '';
  }

  /**
   * Normaliza el texto para mejor matching
   */
  private normalizeText(text: string): string {
    return text
      .replace(/\s+/g, ' ') // Normalize whitespace
      .replace(/[^\w\s\-.,;:()]/g, '') // Remove special chars except common punctuation
      .trim();
  }

  /**
   * Obtiene la lista de conceptos cubiertos (nombres)
   */
  public getCoveredKeywords(detectedConcepts: DetectedConcept[]): string[] {
    return detectedConcepts.map(c => c.name);
  }

  /**
   * Calcula qué conceptos importantes faltan
   */
  public getMissedConcepts(detectedConcepts: DetectedConcept[]): string[] {
    const detectedNames = new Set(detectedConcepts.map(c => c.name));
    const allConcepts = this.conceptLibrary.getAllConcepts();
    
    return allConcepts
      .filter(c => !detectedNames.has(c.name) && c.weight >= 0.8) // Only high-weight concepts
      .map(c => c.name);
  }

  /**
   * Obtiene la biblioteca de conceptos
   */
  public getConceptLibrary(): ConceptLibrary {
    return this.conceptLibrary;
  }
}
