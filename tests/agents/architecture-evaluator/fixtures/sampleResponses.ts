/**
 * Test fixtures with sample candidate responses for architecture evaluation
 * @module sampleResponses
 */

import { CandidateResponse } from '../../../src/agents/architecture-evaluator/types.js';

/**
 * Excellent response covering multiple concepts correctly
 */
export const excellentResponse: CandidateResponse = {
  sessionId: 'test-session-1',
  topic: 'GenAI Architecture',
  text: `When designing a GenAI system, I would follow the Zero-Math principle to ensure 
  we never delegate mathematical operations to LLMs. For vector search, I'd implement 
  HNSW algorithm which provides O(log n) search complexity and scales well. The system 
  should use Semantic Kernel with KernelFunction for AI orchestration, providing a clean 
  abstraction layer. For event streaming between services, Apache Kafka is ideal as it 
  handles high throughput and provides reliability. We need distributed tracing with 
  ActivitySource and OpenTelemetry for observability. Polly library should be used for 
  resilience patterns like retry and circuit breaker. I'd implement a SemanticCache to 
  reduce LLM costs for similar queries. Before sending data to LLMs, we must use 
  RegexPiiSanitizer for data masking and PII sanitization. We should handle rate limiting 
  with exponential backoff when we get HTTP 429 responses. For critical decisions, 
  implement human-in-the-loop mechanisms. Finally, use relevance guardrails with a 
  RelevanceScore threshold of 0.78 to filter low-quality responses.`
};

/**
 * Good response covering some concepts with minor gaps
 */
export const goodResponse: CandidateResponse = {
  sessionId: 'test-session-2',
  topic: 'GenAI Architecture',
  text: `For GenAI architecture, I would use HNSW for vector similarity search since it's 
  efficient. I'd implement Semantic Kernel for orchestration. We need to handle rate 
  limiting properly with retry logic. I should mention that we need to sanitize PII 
  before sending to LLMs. Using Kafka for event streaming would be good for 
  communication between services.`
};

/**
 * Average response with some concepts but also anti-patterns
 */
export const averageResponse: CandidateResponse = {
  sessionId: 'test-session-3',
  topic: 'GenAI Architecture',
  text: `I would build the system using OpenAI directly without abstraction. For 
  calculations, we can let the LLM handle mathematical operations. We need vector 
  search, maybe HNSW or brute force. I think we can skip error handling for the 
  initial version and add it later. For storage, we'll use a simple database. We 
  should probably hardcode the API keys in the config file for simplicity.`
};

/**
 * Poor response with many anti-patterns and few concepts
 */
export const poorResponse: CandidateResponse = {
  sessionId: 'test-session-4',
  topic: 'GenAI Architecture',
  text: `I would just use GPT-4 for everything including calculations. We can hardcode 
  the API key in the source code. No need for error handling initially, we can assume 
  it works. We don't need rate limiting or monitoring. Just send raw data directly 
  to the LLM without any sanitization. We can use synchronous blocking file reads 
  for processing large documents.`
};

/**
 * Response focused on RAG pattern
 */
export const ragFocusedResponse: CandidateResponse = {
  sessionId: 'test-session-5',
  topic: 'RAG Implementation',
  text: `For RAG implementation, I would use Retrieval Augmented Generation pattern. 
  The system needs a vector database to store embeddings. We use HNSW for efficient 
  vector similarity search. Before vectorizing documents, we must sanitize PII data. 
  The semantic cache can help reduce LLM costs for repeated queries. We need to 
  implement relevance guardrails to ensure response quality.`
};

/**
 * Response focused on resilience and production readiness
 */
export const resilienceFocusedResponse: CandidateResponse = {
  sessionId: 'test-session-6',
  topic: 'Production Systems',
  text: `For production systems, resilience is critical. I'd use Polly library for 
  implementing retry policies and circuit breaker patterns. We need to handle HTTP 429 
  rate limiting with exponential backoff. Distributed tracing with ActivitySource and 
  OpenTelemetry provides observability. We should implement semantic cache to improve 
  performance and reduce costs. The Zero-Math principle ensures we don't delegate 
  mathematical operations to LLMs.`
};

/**
 * Empty response
 */
export const emptyResponse: CandidateResponse = {
  sessionId: 'test-session-7',
  topic: 'GenAI Architecture',
  text: ''
};

/**
 * Very short response
 */
export const veryShortResponse: CandidateResponse = {
  sessionId: 'test-session-8',
  topic: 'GenAI Architecture',
  text: 'I would use AI.'
};

/**
 * Response with irrelevant content
 */
export const irrelevantResponse: CandidateResponse = {
  sessionId: 'test-session-9',
  topic: 'GenAI Architecture',
  text: `The weather today is sunny and warm. I like pizza and hamburgers. My favorite 
  color is blue. I went to the store yesterday and bought some groceries. The movie 
  was really good, I enjoyed the plot and the acting.`
};

/**
 * Response with mixed quality - some good concepts, some anti-patterns
 */
export const mixedResponse: CandidateResponse = {
  sessionId: 'test-session-10',
  topic: 'GenAI Architecture',
  text: `I would use HNSW for vector search and Kafka for event streaming. However, 
  we can let the LLM handle calculations for simplicity. We should use Semantic Kernel 
  but we can skip monitoring for now. I'd hardcode the API keys for the MVP and add 
  security later. We need distributed tracing with ActivitySource for observability.`
};

/**
 * Array of all sample responses for iteration in tests
 */
export const allSampleResponses: CandidateResponse[] = [
  excellentResponse,
  goodResponse,
  averageResponse,
  poorResponse,
  ragFocusedResponse,
  resilienceFocusedResponse,
  emptyResponse,
  veryShortResponse,
  irrelevantResponse,
  mixedResponse
];
