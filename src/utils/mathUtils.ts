/**
 * Utility functions for mathematical operations
 * 
 * @module mathUtils
 */

/**
 * Clamps a number to be within a specified range
 * 
 * @param value - The value to clamp
 * @param min - Minimum allowed value
 * @param max - Maximum allowed value
 * @returns The clamped value
 */
export function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}

/**
 * Rounds a number to a specified number of decimal places
 * 
 * @param value - The value to round
 * @param decimalPlaces - Number of decimal places (default: 2)
 * @returns The rounded value
 */
export function roundToDecimalPlaces(value: number, decimalPlaces: number = 2): number {
  const multiplier = Math.pow(10, decimalPlaces);
  return Math.round(value * multiplier) / multiplier;
}

/**
 * Validates that a score is within the valid range (0-100)
 * 
 * @param score - The score to validate
 * @param fieldName - Name of the field for error messages
 * @throws Error if score is out of range
 */
export function validateScoreRange(score: number, fieldName: string): void {
  if (score < 0 || score > 100) {
    throw new Error(
      `${fieldName} must be between 0 and 100, got: ${score}`
    );
  }
}

/**
 * Validates that weights sum to 1.0 (within floating point tolerance)
 * 
 * @param weights - Array of weight values
 * @throws Error if weights don't sum to 1.0
 */
export function validateWeightsSum(weights: number[]): void {
  const sum = weights.reduce((a, b) => a + b, 0);
  const tolerance = 0.0001;
  
  if (Math.abs(sum - 1.0) > tolerance) {
    throw new Error(
      `Weights must sum to 1.0, got: ${sum} (weights: [${weights.join(', ')}])`
    );
  }
}
