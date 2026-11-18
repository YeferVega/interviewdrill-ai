/**
 * Concept Library - Biblioteca de conceptos técnicos clave
 * 
 * Mantiene la definición de conceptos importantes que deben detectarse
 * en las respuestas de los candidatos.
 * 
 * @module ConceptLibrary
 */

import type { ConceptDefinition } from './types.js';

/**
 * ConceptLibrary - Gestiona la biblioteca de conceptos técnicos
 */
export class ConceptLibrary {
  private concepts: Map<string, ConceptDefinition>;

  constructor() {
    this.concepts = new Map();
    this.loadDefaultConcepts();
  }

  /**
   * Carga los conceptos por defecto del dominio GenAI/Architecture
   */
  private loadDefaultConcepts(): void {
    // Zero-Math Principle
    this.addConcept({
      name: 'Zero-Math',
      keywords: ['zero-math', 'zero math', 'no math', 'don\'t delegate math', 'never delegate math', 'llm not for math', 'mathematical operations'],
      patterns: [
        /zero[\s-]?math/i,
        /don'?t\s+(delegate|use)\s+(math|calculations?)\s+(to|for)\s+llm/i,
        /never\s+delegate\s+math/i,
        /llm\s+(shouldn'?t|mustn'?t|not)\s+(do|handle|perform)\s+math/i,
      ],
      weight: 0.9,
      description: 'Principio de no delegar operaciones matemáticas a LLMs',
      examples: [
        'Never delegate mathematical operations to LLMs',
        'Use Zero-Math principle for calculations',
        'LLMs should not perform math operations',
      ],
      category: 'principle',
    });

    // KernelFunction (Semantic Kernel)
    this.addConcept({
      name: 'KernelFunction',
      keywords: ['kernelfunction', 'kernel function', 'semantic kernel', 'sk', 'orchestration', 'ai orchestration'],
      patterns: [
        /kernel[\s-]?function/i,
        /semantic\s+kernel/i,
        /\bsk\b.*orchestrat/i,
        /kernel.*orchestrat/i,
      ],
      weight: 0.85,
      description: 'Patrón de orquestación de Semantic Kernel',
      examples: [
        'Use KernelFunction for AI orchestration',
        'Semantic Kernel provides KernelFunction abstraction',
      ],
      category: 'pattern',
    });

    // HNSW (Vector Search)
    this.addConcept({
      name: 'HNSW',
      keywords: ['hnsw', 'hierarchical navigable small world', 'vector search', 'vector similarity', 'approximate nearest neighbor', 'ann'],
      patterns: [
        /\bhnsw\b/i,
        /hierarchical\s+navigable\s+small\s+world/i,
        /vector\s+(search|similarity)/i,
        /approximate\s+nearest\s+neighbor/i,
        /\bann\b.*search/i,
      ],
      weight: 0.9,
      description: 'Algoritmo HNSW para búsqueda vectorial eficiente',
      examples: [
        'Use HNSW for efficient vector similarity search',
        'HNSW provides O(log n) search complexity',
      ],
      category: 'technology',
    });

    // Kafka (Event Streaming)
    this.addConcept({
      name: 'Kafka',
      keywords: ['kafka', 'event streaming', 'message broker', 'event bus', 'pub/sub', 'publish subscribe', 'async messaging'],
      patterns: [
        /\bkafka\b/i,
        /event\s+stream(ing)?/i,
        /message\s+broker/i,
        /event\s+bus/i,
        /pub[\s-]?sub/i,
        /publish[\s-]?subscribe/i,
      ],
      weight: 0.8,
      description: 'Kafka para streaming de eventos y comunicación asíncrona',
      examples: [
        'Use Kafka for event streaming between services',
        'Kafka provides reliable message delivery',
      ],
      category: 'technology',
    });

    // ActivitySource (Distributed Tracing)
    this.addConcept({
      name: 'ActivitySource',
      keywords: ['activitysource', 'activity source', 'distributed tracing', 'opentelemetry', 'otel', 'tracing', 'observability'],
      patterns: [
        /activity[\s-]?source/i,
        /distributed\s+trac(ing|e)/i,
        /opentelemetry/i,
        /\botel\b/i,
        /tracing.*observability/i,
      ],
      weight: 0.85,
      description: 'ActivitySource para distributed tracing y observabilidad',
      examples: [
        'Use ActivitySource for distributed tracing',
        'ActivitySource integrates with OpenTelemetry',
      ],
      category: 'technology',
    });

    // Polly (Resilience Patterns)
    this.addConcept({
      name: 'Polly',
      keywords: ['polly', 'resilience', 'retry', 'circuit breaker', 'timeout', 'bulkhead', 'fallback', 'rate limiter'],
      patterns: [
        /\bpolly\b/i,
        /resilience\s+(pattern|library)/i,
        /circuit\s+breaker/i,
        /retry\s+(policy|logic|mechanism)/i,
        /bulkhead\s+pattern/i,
      ],
      weight: 0.85,
      description: 'Polly para patrones de resiliencia: retry, circuit breaker, timeout',
      examples: [
        'Use Polly for retry and circuit breaker patterns',
        'Polly provides resilience strategies',
      ],
      category: 'technology',
    });

    // RAG (Retrieval Augmented Generation)
    this.addConcept({
      name: 'RAG',
      keywords: ['rag', 'retrieval augmented generation', 'retrieval-augmented', 'knowledge base', 'vector database', 'embedding'],
      patterns: [
        /\brag\b/i,
        /retrieval[\s-]?augmented\s+generation/i,
        /vector\s+(database|db|store)/i,
        /knowledge\s+base.*retrieval/i,
      ],
      weight: 0.9,
      description: 'Retrieval Augmented Generation para contexto en LLMs',
      examples: [
        'Use RAG to provide context to LLMs',
        'RAG combines retrieval with generation',
      ],
      category: 'pattern',
    });

    // Semantic Cache
    this.addConcept({
      name: 'SemanticCache',
      keywords: ['semantic cache', 'semantic-cache', 'llm cache', 'response cache', 'cache semantic'],
      patterns: [
        /semantic[\s-]?cache/i,
        /llm\s+cache/i,
        /cache.*semantic/i,
        /response\s+cache.*llm/i,
      ],
      weight: 0.75,
      description: 'Caché semántico para reducir costos de LLM',
      examples: [
        'Implement semantic cache to reduce LLM costs',
        'Semantic cache stores similar query responses',
      ],
      category: 'pattern',
    });

    // PII Sanitization
    this.addConcept({
      name: 'PIISanitization',
      keywords: ['pii', 'pii sanitization', 'data masking', 'personally identifiable', 'sensitive data', 'regexpii', 'regex pii sanitizer'],
      patterns: [
        /\bpii\b.*sanitiz/i,
        /pii[\s-]?sanitiz/i,
        /data\s+mask(ing)?/i,
        /personally\s+identifiable/i,
        /sensitive\s+data.*protect/i,
        /regex[\s-]?pii/i,
      ],
      weight: 0.9,
      description: 'Sanitización de PII antes de inferencia o vectorización',
      examples: [
        'Use RegexPiiSanitizer for PII protection',
        'Sanitize PII before sending to LLM',
      ],
      category: 'principle',
    });

    // Rate Limiting
    this.addConcept({
      name: 'RateLimiting',
      keywords: ['rate limit', 'rate-limit', 'rate limiting', 'http 429', 'too many requests', 'throttling', 'backoff'],
      patterns: [
        /rate[\s-]?limit(ing)?/i,
        /http\s+429/i,
        /too\s+many\s+requests/i,
        /throttl(ing|e)/i,
        /exponential\s+backoff/i,
      ],
      weight: 0.8,
      description: 'Manejo de rate limiting y HTTP 429',
      examples: [
        'Handle HTTP 429 with exponential backoff',
        'Implement rate limiting handling',
      ],
      category: 'pattern',
    });

    // Human-in-the-Loop
    this.addConcept({
      name: 'HumanInTheLoop',
      keywords: ['human-in-the-loop', 'human in the loop', 'hitl', 'human intervention', 'manual review', 'human oversight'],
      patterns: [
        /human[\s-]?in[\s-]?the[\s-]?loop/i,
        /\bhitl\b/i,
        /human\s+(intervention|oversight|review)/i,
        /manual\s+review.*critical/i,
      ],
      weight: 0.85,
      description: 'Mecanismos de intervención humana para decisiones críticas',
      examples: [
        'Implement human-in-the-loop for critical decisions',
        'Use HITL for sensitive operations',
      ],
      category: 'principle',
    });

    // Relevance Guardrails
    this.addConcept({
      name: 'RelevanceGuardrails',
      keywords: ['relevance score', 'relevance guardrail', 'relevance threshold', 'confidence score', 'guardrail'],
      patterns: [
        /relevance\s+(score|guardrail|threshold)/i,
        /confidence\s+(score|threshold)/i,
        /relevance.*<\s*0\.78/i,
        /guardrail.*relevance/i,
      ],
      weight: 0.8,
      description: 'Guardrails de relevancia para filtrar respuestas de baja calidad',
      examples: [
        'Use RelevanceScore < 0.78 as threshold',
        'Implement relevance guardrails',
      ],
      category: 'principle',
    });
  }

  /**
   * Agrega un concepto a la biblioteca
   */
  public addConcept(concept: ConceptDefinition): void {
    this.concepts.set(concept.name, concept);
  }

  /**
   * Obtiene un concepto por nombre
   */
  public getConcept(name: string): ConceptDefinition | undefined {
    return this.concepts.get(name);
  }

  /**
   * Obtiene todos los conceptos
   */
  public getAllConcepts(): ConceptDefinition[] {
    return Array.from(this.concepts.values());
  }

  /**
   * Obtiene conceptos por categoría
   */
  public getConceptsByCategory(category: ConceptDefinition['category']): ConceptDefinition[] {
    return this.getAllConcepts().filter(c => c.category === category);
  }

  /**
   * Obtiene el número total de conceptos
   */
  public getConceptCount(): number {
    return this.concepts.size;
  }

  /**
   * Verifica si existe un concepto
   */
  public hasConcept(name: string): boolean {
    return this.concepts.has(name);
  }
}
