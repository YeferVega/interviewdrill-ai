/**
 * AntiPatternDetector - Detecta antipatrones arquitectónicos
 * 
 * Analiza texto para identificar prácticas incorrectas o antipatrones
 * que indican falta de conocimiento o malas prácticas.
 * 
 * @module AntiPatternDetector
 */

import type { AntiPatternDefinition, DetectedAntiPattern } from './types.js';

/**
 * AntiPatternDetector - Detecta antipatrones en texto
 */
export class AntiPatternDetector {
  private antiPatterns: AntiPatternDefinition[];

  constructor() {
    this.antiPatterns = [];
    this.loadDefaultAntiPatterns();
  }

  /**
   * Carga los antipatrones por defecto
   */
  private loadDefaultAntiPatterns(): void {
    // Delegating math to LLMs
    this.addAntiPattern({
      name: 'DelegatingMathToLLM',
      indicators: [
        'use llm for calculations',
        'let gpt calculate',
        'ask llm to compute',
        'llm for math',
        'chatgpt for calculations',
        'ai for mathematical',
      ],
      patterns: [
        /use\s+(llm|gpt|ai|chatgpt)\s+(for|to)\s+(calculat|comput|math)/i,
        /let\s+(llm|gpt|ai)\s+(calculat|comput)/i,
        /ask\s+(llm|gpt|ai)\s+to\s+(calculat|comput)/i,
        /llm\s+(for|to)\s+math/i,
      ],
      severity: 'high',
      description: 'Delegar operaciones matemáticas a LLMs viola el principio Zero-Math',
      penalty: 20,
    });

    // Strong coupling to AI provider
    this.addAntiPattern({
      name: 'StrongCouplingToAIProvider',
      indicators: [
        'only use openai',
        'hardcoded to azure',
        'tied to specific provider',
        'no abstraction layer',
        'direct api calls',
        'no provider abstraction',
      ],
      patterns: [
        /only\s+use\s+(openai|azure|anthropic|google)/i,
        /hardcoded\s+to\s+(openai|azure|anthropic)/i,
        /tied\s+to\s+(specific\s+)?provider/i,
        /no\s+(abstraction|provider\s+abstraction)/i,
        /direct\s+api\s+calls\s+(to|without)/i,
      ],
      severity: 'medium',
      description: 'Acoplamiento fuerte a un proveedor específico de IA sin abstracción',
      penalty: 15,
    });

    // Synchronous blocking calls for heavy files
    this.addAntiPattern({
      name: 'SynchronousBlockingForHeavyFiles',
      indicators: [
        'read file synchronously',
        'blocking file operations',
        'sync file read',
        'wait for file',
        'blocking io',
      ],
      patterns: [
        /read\s+(file|document)\s+synchronous(ly)?/i,
        /blocking\s+(file|io)\s+(operations?|read)/i,
        /sync\s+(file|io)\s+read/i,
        /wait\s+(for|until)\s+file/i,
        /blocking\s+io\s+(for|when)\s+(file|large|heavy)/i,
      ],
      severity: 'medium',
      description: 'Operaciones síncronas bloqueantes para archivos pesados afectan performance',
      penalty: 10,
    });

    // No error handling
    this.addAntiPattern({
      name: 'NoErrorHandling',
      indicators: [
        'no need for error handling',
        'skip error handling',
        'assume it works',
        'no try catch',
        'ignore errors',
      ],
      patterns: [
        /no\s+(need\s+for|need)\s+error\s+handling/i,
        /skip\s+error\s+handling/i,
        /assume\s+(it\s+)?works?\s+(without|no)\s+error/i,
        /no\s+try[\s-]?catch/i,
        /ignore\s+errors?/i,
      ],
      severity: 'high',
      description: 'Falta de manejo de errores es crítico en producción',
      penalty: 15,
    });

    // No rate limiting handling
    this.addAntiPattern({
      name: 'NoRateLimitingHandling',
      indicators: [
        'ignore rate limits',
        'no throttling',
        'no retry logic',
        'assume no limits',
        'no 429 handling',
      ],
      patterns: [
        /ignore\s+rate\s+limits?/i,
        /no\s+(throttl|retry|rate\s+limit)/i,
        /assume\s+no\s+(rate\s+)?limits?/i,
        /no\s+(429|http\s+429)\s+handling/i,
      ],
      severity: 'medium',
      description: 'No manejar rate limiting causa fallos en producción',
      penalty: 10,
    });

    // Hardcoding secrets
    this.addAntiPattern({
      name: 'HardcodingSecrets',
      indicators: [
        'hardcode api key',
        'put key in code',
        'api key in config',
        'secret in source',
        'credentials in code',
      ],
      patterns: [
        /hardcode\s+(api\s+)?key/i,
        /put\s+(api\s+)?key\s+in\s+(code|config)/i,
        /api\s+key\s+in\s+(code|config|source)/i,
        /secret\s+in\s+source/i,
        /credentials?\s+in\s+code/i,
      ],
      severity: 'high',
      description: 'Hardcodear secretos es una vulnerabilidad de seguridad crítica',
      penalty: 25,
    });

    // No logging or monitoring
    this.addAntiPattern({
      name: 'NoLoggingOrMonitoring',
      indicators: [
        'no logging needed',
        'skip monitoring',
        'no observability',
        'don\'t need logs',
        'no metrics',
      ],
      patterns: [
        /no\s+(logging|logs?)\s+(needed|required)/i,
        /skip\s+(monitoring|observability)/i,
        /no\s+observability/i,
        /don'?t\s+need\s+logs?/i,
        /no\s+metrics?\s+(needed|required)/i,
      ],
      severity: 'medium',
      description: 'Falta de logging y monitoreo dificulta debugging en producción',
      penalty: 10,
    });

    // Ignoring PII protection
    this.addAntiPattern({
      name: 'IgnoringPIIProtection',
      indicators: [
        'no pii protection',
        'skip data masking',
        'no sanitization',
        'send raw data',
        'no need to mask',
      ],
      patterns: [
        /no\s+pii\s+protection/i,
        /skip\s+(data\s+)?mask(ing)?/i,
        /no\s+sanitiz(ation|ing)/i,
        /send\s+raw\s+data/i,
        /no\s+need\s+to\s+mask/i,
      ],
      severity: 'high',
      description: 'Ignorar protección de PII viola regulaciones de privacidad',
      penalty: 20,
    });
  }

  /**
   * Agrega un antipatrón a la lista
   */
  public addAntiPattern(antiPattern: AntiPatternDefinition): void {
    this.antiPatterns.push(antiPattern);
  }

  /**
   * Detecta todos los antipatrones en el texto
   * 
   * @param text - Texto a analizar
   * @returns Lista de antipatrones detectados con metadata
   */
  public detectAntiPatterns(text: string): DetectedAntiPattern[] {
    const normalizedText = text.toLowerCase();
    const detectedAntiPatterns: DetectedAntiPattern[] = [];

    for (const antiPattern of this.antiPatterns) {
      const detection = this.detectAntiPattern(normalizedText, antiPattern);
      if (detection) {
        detectedAntiPatterns.push(detection);
      }
    }

    return detectedAntiPatterns;
  }

  /**
   * Detecta un antipatrón específico
   */
  private detectAntiPattern(
    text: string,
    antiPattern: AntiPatternDefinition
  ): DetectedAntiPattern | null {
    // Try indicator matching
    const indicatorMatch = this.matchIndicators(text, antiPattern.indicators);
    
    // Try pattern matching
    const patternMatch = this.matchPatterns(text, antiPattern.patterns);

    // If either matched, return detected anti-pattern
    if (indicatorMatch || patternMatch) {
      const context = this.extractContext(text, antiPattern);

      return {
        name: antiPattern.name,
        severity: antiPattern.severity,
        context,
        penalty: antiPattern.penalty,
      };
    }

    return null;
  }

  /**
   * Busca indicadores en el texto
   */
  private matchIndicators(text: string, indicators: string[]): boolean {
    return indicators.some(indicator => text.includes(indicator.toLowerCase()));
  }

  /**
   * Busca patrones regex en el texto
   */
  private matchPatterns(text: string, patterns: RegExp[]): boolean {
    return patterns.some(pattern => pattern.test(text));
  }

  /**
   * Extrae contexto donde se encontró el antipatrón
   */
  private extractContext(text: string, antiPattern: AntiPatternDefinition): string {
    // Find first occurrence of any indicator
    for (const indicator of antiPattern.indicators) {
      const index = text.indexOf(indicator.toLowerCase());
      if (index !== -1) {
        const start = Math.max(0, index - 100);
        const end = Math.min(text.length, index + indicator.length + 100);
        return text.substring(start, end).trim();
      }
    }

    // If no indicator found, try patterns
    for (const pattern of antiPattern.patterns) {
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
   * Calcula la penalización total por antipatrones
   */
  public calculateTotalPenalty(detectedAntiPatterns: DetectedAntiPattern[]): number {
    return detectedAntiPatterns.reduce((total, ap) => total + ap.penalty, 0);
  }

  /**
   * Obtiene la lista de nombres de antipatrones
   */
  public getAntiPatternNames(detectedAntiPatterns: DetectedAntiPattern[]): string[] {
    return detectedAntiPatterns.map(ap => ap.name);
  }

  /**
   * Obtiene todos los antipatrones definidos
   */
  public getAllAntiPatterns(): AntiPatternDefinition[] {
    return [...this.antiPatterns];
  }
}
