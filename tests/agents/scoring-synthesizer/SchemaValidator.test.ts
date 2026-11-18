/**
 * SchemaValidator Unit Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { SchemaValidator, SchemaValidationError } from '../../../src/agents/scoring-synthesizer/SchemaValidator.ts';
import { validPayloads, invalidPayloads } from './fixtures/validPayloads.ts';

describe('SchemaValidator', () => {
  let validator: SchemaValidator;

  beforeEach(() => {
    validator = new SchemaValidator();
  });

  describe('validate()', () => {
    describe('valid payloads', () => {
      validPayloads.forEach((payload, index) => {
        it(`should accept valid payload #${index + 1} (score: ${payload.overallScore}, verdict: ${payload.verdict})`, () => {
          const result = validator.validate(payload);
          expect(result).toEqual(payload);
          expect(result.overallScore).toBe(payload.overallScore);
          expect(result.verdict).toBe(payload.verdict);
        });
      });

      it('should accept payload with empty arrays', () => {
        const emptyArrayPayload = {
          overallScore: 50,
          technicalDepth: 50,
          productionReadiness: 50,
          verdict: 'NEEDS_PRACTICE' as const,
          coveredKeywords: [],
          missedConcepts: [],
          strengths: [],
          improvementAdvice: 'Test advice',
        };
        const result = validator.validate(emptyArrayPayload);
        expect(result.coveredKeywords).toEqual([]);
        expect(result.missedConcepts).toEqual([]);
        expect(result.strengths).toEqual([]);
      });

      it('should accept payload with boundary score values', () => {
        const boundaryPayload = {
          overallScore: 0,
          technicalDepth: 0,
          productionReadiness: 0,
          verdict: 'FAIL' as const,
          coveredKeywords: [],
          missedConcepts: [],
          strengths: [],
          improvementAdvice: 'Start from basics',
        };
        expect(() => validator.validate(boundaryPayload)).not.toThrow();
      });

      it('should accept payload with maximum score values', () => {
        const maxPayload = {
          overallScore: 100,
          technicalDepth: 100,
          productionReadiness: 100,
          verdict: 'PASS' as const,
          coveredKeywords: ['All concepts'],
          missedConcepts: [],
          strengths: ['Perfect understanding'],
          improvementAdvice: 'Excellent!',
        };
        expect(() => validator.validate(maxPayload)).not.toThrow();
      });
    });

    describe('invalid payloads', () => {
      invalidPayloads.forEach(({ name, payload, expectedError }) => {
        it(`should reject: ${name}`, () => {
          expect(() => validator.validate(payload)).toThrow(SchemaValidationError);
          
          try {
            validator.validate(payload);
          } catch (error) {
            expect(error).toBeInstanceOf(SchemaValidationError);
            if (error instanceof SchemaValidationError) {
              expect(error.errors.length).toBeGreaterThan(0);
              const formattedErrors = error.getFormattedErrors();
              expect(formattedErrors.toLowerCase()).toContain(expectedError.toLowerCase());
            }
          }
        });
      });

      it('should provide descriptive error messages for missing required fields', () => {
        const invalidPayload = {
          technicalDepth: 80,
          productionReadiness: 70,
          verdict: 'PASS',
          coveredKeywords: [],
          missedConcepts: [],
          strengths: [],
          improvementAdvice: 'Test',
        };

        try {
          validator.validate(invalidPayload);
          expect.fail('Should have thrown SchemaValidationError');
        } catch (error) {
          expect(error).toBeInstanceOf(SchemaValidationError);
          if (error instanceof SchemaValidationError) {
            const formattedErrors = error.getFormattedErrors();
            expect(formattedErrors).toContain('overallScore');
            expect(formattedErrors).toContain('required');
          }
        }
      });

      it('should provide descriptive error messages for out-of-range values', () => {
        const invalidPayload = {
          overallScore: 150,
          technicalDepth: 80,
          productionReadiness: 70,
          verdict: 'PASS',
          coveredKeywords: [],
          missedConcepts: [],
          strengths: [],
          improvementAdvice: 'Test',
        };

        try {
          validator.validate(invalidPayload);
          expect.fail('Should have thrown SchemaValidationError');
        } catch (error) {
          expect(error).toBeInstanceOf(SchemaValidationError);
          if (error instanceof SchemaValidationError) {
            expect(error.errors.some(e => e.keyword === 'maximum')).toBe(true);
          }
        }
      });

      it('should provide descriptive error messages for invalid enum values', () => {
        const invalidPayload = {
          overallScore: 80,
          technicalDepth: 80,
          productionReadiness: 70,
          verdict: 'INVALID',
          coveredKeywords: [],
          missedConcepts: [],
          strengths: [],
          improvementAdvice: 'Test',
        };

        try {
          validator.validate(invalidPayload);
          expect.fail('Should have thrown SchemaValidationError');
        } catch (error) {
          expect(error).toBeInstanceOf(SchemaValidationError);
          if (error instanceof SchemaValidationError) {
            expect(error.errors.some(e => e.keyword === 'enum')).toBe(true);
          }
        }
      });

      it('should provide descriptive error messages for wrong types', () => {
        const invalidPayload = {
          overallScore: '80', // string instead of number
          technicalDepth: 80,
          productionReadiness: 70,
          verdict: 'PASS',
          coveredKeywords: [],
          missedConcepts: [],
          strengths: [],
          improvementAdvice: 'Test',
        };

        try {
          validator.validate(invalidPayload);
          expect.fail('Should have thrown SchemaValidationError');
        } catch (error) {
          expect(error).toBeInstanceOf(SchemaValidationError);
          if (error instanceof SchemaValidationError) {
            expect(error.errors.some(e => e.keyword === 'type')).toBe(true);
          }
        }
      });
    });

    describe('edge cases', () => {
      it('should handle decimal scores correctly', () => {
        const decimalPayload = {
          overallScore: 75.5,
          technicalDepth: 80.25,
          productionReadiness: 70.75,
          verdict: 'NEEDS_PRACTICE' as const,
          coveredKeywords: ['Test'],
          missedConcepts: [],
          strengths: [],
          improvementAdvice: 'Test',
        };
        expect(() => validator.validate(decimalPayload)).not.toThrow();
      });

      it('should handle large arrays', () => {
        const largePayload = {
          overallScore: 80,
          technicalDepth: 80,
          productionReadiness: 80,
          verdict: 'PASS' as const,
          coveredKeywords: Array.from({ length: 100 }, (_, i) => `keyword-${i}`),
          missedConcepts: Array.from({ length: 50 }, (_, i) => `concept-${i}`),
          strengths: Array.from({ length: 20 }, (_, i) => `strength-${i}`),
          improvementAdvice: 'Test advice',
        };
        expect(() => validator.validate(largePayload)).not.toThrow();
      });

      it('should handle empty strings in arrays', () => {
        const payloadWithEmptyStrings = {
          overallScore: 80,
          technicalDepth: 80,
          productionReadiness: 80,
          verdict: 'PASS' as const,
          coveredKeywords: [''],
          missedConcepts: [''],
          strengths: [''],
          improvementAdvice: 'Test',
        };
        expect(() => validator.validate(payloadWithEmptyStrings)).not.toThrow();
      });

      it('should reject additional properties', () => {
        const payloadWithExtra = {
          overallScore: 80,
          technicalDepth: 80,
          productionReadiness: 80,
          verdict: 'PASS',
          coveredKeywords: [],
          missedConcepts: [],
          strengths: [],
          improvementAdvice: 'Test',
          extraField: 'should not be here',
        };
        expect(() => validator.validate(payloadWithExtra)).toThrow(SchemaValidationError);
      });
    });
  });

  describe('isValid()', () => {
    it('should return true for valid payloads', () => {
      validPayloads.forEach((payload) => {
        expect(validator.isValid(payload)).toBe(true);
      });
    });

    it('should return false for invalid payloads', () => {
      invalidPayloads.forEach(({ payload }) => {
        expect(validator.isValid(payload)).toBe(false);
      });
    });

    it('should not throw errors for invalid payloads', () => {
      const invalidPayload = {
        overallScore: -10,
        technicalDepth: 80,
        productionReadiness: 70,
        verdict: 'PASS',
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        improvementAdvice: 'Test',
      };
      expect(() => validator.isValid(invalidPayload)).not.toThrow();
      expect(validator.isValid(invalidPayload)).toBe(false);
    });
  });

  describe('validateWithErrors()', () => {
    it('should return isValid=true and empty errors for valid payloads', () => {
      const validPayload = validPayloads[0];
      const result = validator.validateWithErrors(validPayload);
      expect(result.isValid).toBe(true);
      expect(result.errors).toEqual([]);
    });

    it('should return isValid=false and errors for invalid payloads', () => {
      const invalidPayload = {
        overallScore: -10,
        technicalDepth: 80,
        productionReadiness: 70,
        verdict: 'PASS',
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        improvementAdvice: 'Test',
      };
      const result = validator.validateWithErrors(invalidPayload);
      expect(result.isValid).toBe(false);
      expect(result.errors.length).toBeGreaterThan(0);
    });

    it('should return all errors for multiple validation failures', () => {
      const invalidPayload = {
        overallScore: -10,
        technicalDepth: 150,
        productionReadiness: 70,
        verdict: 'INVALID',
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        improvementAdvice: 'Test',
      };
      const result = validator.validateWithErrors(invalidPayload);
      expect(result.isValid).toBe(false);
      expect(result.errors.length).toBeGreaterThan(1);
    });
  });

  describe('SchemaValidationError', () => {
    it('should format errors correctly', () => {
      const invalidPayload = {
        overallScore: -10,
        technicalDepth: 80,
        productionReadiness: 70,
        verdict: 'PASS',
        coveredKeywords: [],
        missedConcepts: [],
        strengths: [],
        improvementAdvice: 'Test',
      };

      try {
        validator.validate(invalidPayload);
        expect.fail('Should have thrown SchemaValidationError');
      } catch (error) {
        expect(error).toBeInstanceOf(SchemaValidationError);
        if (error instanceof SchemaValidationError) {
          const formattedErrors = error.getFormattedErrors();
          expect(formattedErrors).toContain('overallScore');
          expect(formattedErrors).toContain('>=');
          expect(error.payload).toEqual(invalidPayload);
        }
      }
    });

    it('should include error name and message', () => {
      const error = new SchemaValidationError('Test error', [], {});
      expect(error.name).toBe('SchemaValidationError');
      expect(error.message).toBe('Test error');
    });
  });
});
