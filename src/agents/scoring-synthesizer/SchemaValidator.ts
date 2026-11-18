/**
 * SchemaValidator - Validates payloads against JSON Schema
 * 
 * Uses ajv library for fast, standards-compliant JSON Schema validation.
 * Provides descriptive error messages for debugging.
 * 
 * @module SchemaValidator
 */

import Ajv, { type ErrorObject, type ValidateFunction } from 'ajv';
import evaluationPayloadSchema from './schemas/EvaluationPayload.json' with { type: 'json' };
import type { EvaluationPayload } from './types.js';

/**
 * Custom error class for schema validation failures
 */
export class SchemaValidationError extends Error {
  public readonly errors: ErrorObject[];
  public readonly payload: unknown;

  constructor(message: string, errors: ErrorObject[], payload: unknown) {
    super(message);
    this.name = 'SchemaValidationError';
    this.errors = errors;
    this.payload = payload;
  }

  /**
   * Returns a formatted string with all validation errors
   */
  getFormattedErrors(): string {
    return this.errors
      .map((err) => {
        const path = err.instancePath || 'root';
        const message = err.message || 'Unknown error';
        const params = err.params ? JSON.stringify(err.params) : '';
        return `  - ${path}: ${message} ${params}`;
      })
      .join('\n');
  }
}

/**
 * SchemaValidator class for validating evaluation payloads
 */
export class SchemaValidator {
  private ajv: Ajv;
  private validateFn: ValidateFunction;

  constructor() {
    this.ajv = new Ajv({
      allErrors: true, // Report all errors, not just the first one
      removeAdditional: false, // Don't remove additional properties
      useDefaults: false, // Don't apply defaults
      coerceTypes: false, // Don't coerce types
    });

    this.validateFn = this.ajv.compile(evaluationPayloadSchema);
  }

  /**
   * Validates an EvaluationPayload against the JSON Schema
   * 
   * @param payload - The payload to validate
   * @returns The validated payload (type-safe)
   * @throws SchemaValidationError if validation fails
   */
  public validate(payload: unknown): EvaluationPayload {
    const valid = this.validateFn(payload);

    if (!valid) {
      const errors = this.validateFn.errors || [];
      const errorMessage = [
        'EvaluationPayload validation failed:',
        new SchemaValidationError('Validation failed', errors, payload).getFormattedErrors(),
      ].join('\n');

      throw new SchemaValidationError(errorMessage, errors, payload);
    }

    // At this point, payload is guaranteed to match EvaluationPayload schema
    return payload as EvaluationPayload;
  }

  /**
   * Checks if a payload is valid without throwing an error
   * 
   * @param payload - The payload to check
   * @returns true if valid, false otherwise
   */
  public isValid(payload: unknown): boolean {
    return this.validateFn(payload) as boolean;
  }

  /**
   * Validates and returns errors without throwing
   * 
   * @param payload - The payload to validate
   * @returns Object with isValid flag and errors array
   */
  public validateWithErrors(payload: unknown): {
    isValid: boolean;
    errors: ErrorObject[];
  } {
    const valid = this.validateFn(payload);
    return {
      isValid: valid as boolean,
      errors: valid ? [] : this.validateFn.errors || [],
    };
  }
}
