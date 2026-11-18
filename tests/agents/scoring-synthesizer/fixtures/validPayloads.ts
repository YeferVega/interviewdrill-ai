/**
 * Test fixtures for SchemaValidator tests
 */

import type { EvaluationPayload } from '../types.js';

/**
 * Valid EvaluationPayload examples
 */
export const validPayloads: EvaluationPayload[] = [
  {
    overallScore: 85,
    technicalDepth: 90,
    productionReadiness: 80,
    verdict: 'PASS',
    coveredKeywords: ['HNSW', 'Kafka', 'Zero-Math'],
    missedConcepts: ['Polly'],
    strengths: ['Strong understanding of vector search', 'Good event streaming knowledge'],
    improvementAdvice: 'Review resilience patterns like Polly for circuit breaking and retry logic.',
  },
  {
    overallScore: 65,
    technicalDepth: 70,
    productionReadiness: 60,
    verdict: 'NEEDS_PRACTICE',
    coveredKeywords: ['Semantic Kernel'],
    missedConcepts: ['HNSW', 'ActivitySource', 'PII sanitization'],
    strengths: ['Basic understanding of Semantic Kernel'],
    improvementAdvice: 'Study vector search algorithms and distributed tracing with ActivitySource.',
  },
  {
    overallScore: 45,
    technicalDepth: 50,
    productionReadiness: 40,
    verdict: 'FAIL',
    coveredKeywords: [],
    missedConcepts: ['Zero-Math', 'HNSW', 'Kafka', 'Polly', 'ActivitySource'],
    strengths: [],
    improvementAdvice: 'Focus on fundamental concepts: Zero-Math principle, vector search, and observability.',
  },
  {
    overallScore: 100,
    technicalDepth: 100,
    productionReadiness: 100,
    verdict: 'PASS',
    coveredKeywords: [
      'Zero-Math',
      'KernelFunction',
      'HNSW',
      'Kafka',
      'ActivitySource',
      'Polly',
      'PII sanitization',
    ],
    missedConcepts: [],
    strengths: [
      'Excellent understanding of all concepts',
      'Comprehensive production considerations',
    ],
    improvementAdvice: 'Excellent performance! Consider mentoring others.',
  },
  {
    overallScore: 0,
    technicalDepth: 0,
    productionReadiness: 0,
    verdict: 'FAIL',
    coveredKeywords: [],
    missedConcepts: ['All core concepts'],
    strengths: [],
    improvementAdvice: 'Start with fundamentals and build up knowledge systematically.',
  },
  {
    overallScore: 60,
    technicalDepth: 60,
    productionReadiness: 60,
    verdict: 'NEEDS_PRACTICE',
    coveredKeywords: ['Basic patterns'],
    missedConcepts: ['Advanced patterns'],
    strengths: ['Some understanding'],
    improvementAdvice: 'Deepen understanding of advanced patterns.',
  },
  {
    overallScore: 80,
    technicalDepth: 80,
    productionReadiness: 80,
    verdict: 'PASS',
    coveredKeywords: ['Good patterns'],
    missedConcepts: [],
    strengths: ['Strong performance'],
    improvementAdvice: 'Continue practicing to maintain excellence.',
  },
];

/**
 * Invalid payload examples with expected error descriptions
 */
export const invalidPayloads = [
  {
    name: 'Missing required field: overallScore',
    payload: {
      technicalDepth: 80,
      productionReadiness: 70,
      verdict: 'PASS',
      coveredKeywords: [],
      missedConcepts: [],
      strengths: [],
      improvementAdvice: 'Test',
    },
    expectedError: 'overallScore',
  },
  {
    name: 'Missing required field: verdict',
    payload: {
      overallScore: 80,
      technicalDepth: 80,
      productionReadiness: 80,
      coveredKeywords: [],
      missedConcepts: [],
      strengths: [],
      improvementAdvice: 'Test',
    },
    expectedError: 'verdict',
  },
  {
    name: 'overallScore out of range (negative)',
    payload: {
      overallScore: -10,
      technicalDepth: 80,
      productionReadiness: 80,
      verdict: 'PASS',
      coveredKeywords: [],
      missedConcepts: [],
      strengths: [],
      improvementAdvice: 'Test',
    },
    expectedError: 'overallScore',
  },
  {
    name: 'overallScore out of range (> 100)',
    payload: {
      overallScore: 150,
      technicalDepth: 80,
      productionReadiness: 80,
      verdict: 'PASS',
      coveredKeywords: [],
      missedConcepts: [],
      strengths: [],
      improvementAdvice: 'Test',
    },
    expectedError: 'overallScore',
  },
  {
    name: 'Invalid verdict value',
    payload: {
      overallScore: 80,
      technicalDepth: 80,
      productionReadiness: 80,
      verdict: 'INVALID_VERDICT',
      coveredKeywords: [],
      missedConcepts: [],
      strengths: [],
      improvementAdvice: 'Test',
    },
    expectedError: 'verdict',
  },
  {
    name: 'Wrong type: overallScore as string',
    payload: {
      overallScore: '80',
      technicalDepth: 80,
      productionReadiness: 80,
      verdict: 'PASS',
      coveredKeywords: [],
      missedConcepts: [],
      strengths: [],
      improvementAdvice: 'Test',
    },
    expectedError: 'overallScore',
  },
  {
    name: 'Wrong type: coveredKeywords as string instead of array',
    payload: {
      overallScore: 80,
      technicalDepth: 80,
      productionReadiness: 80,
      verdict: 'PASS',
      coveredKeywords: 'not-an-array',
      missedConcepts: [],
      strengths: [],
      improvementAdvice: 'Test',
    },
    expectedError: 'coveredKeywords',
  },
  {
    name: 'Wrong type: array items as numbers instead of strings',
    payload: {
      overallScore: 80,
      technicalDepth: 80,
      productionReadiness: 80,
      verdict: 'PASS',
      coveredKeywords: [1, 2, 3],
      missedConcepts: [],
      strengths: [],
      improvementAdvice: 'Test',
    },
    expectedError: 'coveredKeywords',
  },
  {
    name: 'Wrong type: improvementAdvice as number',
    payload: {
      overallScore: 80,
      technicalDepth: 80,
      productionReadiness: 80,
      verdict: 'PASS',
      coveredKeywords: [],
      missedConcepts: [],
      strengths: [],
      improvementAdvice: 123,
    },
    expectedError: 'improvementAdvice',
  },
  {
    name: 'Null payload',
    payload: null,
    expectedError: 'type',
  },
  {
    name: 'Undefined payload',
    payload: undefined,
    expectedError: 'type',
  },
  {
    name: 'Empty object',
    payload: {},
    expectedError: 'required',
  },
  {
    name: 'Array instead of object',
    payload: [],
    expectedError: 'type',
  },
];
