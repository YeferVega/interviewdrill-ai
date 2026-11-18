/**
 * ScoreCalculator Unit Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { ScoreCalculator } from '../../../src/agents/scoring-synthesizer/ScoreCalculator.ts';

describe('ScoreCalculator', () => {
  describe('constructor', () => {
    it('should create with default weights (0.6, 0.4)', () => {
      const calculator = new ScoreCalculator();
      const weights = calculator.getWeights();
      expect(weights.technical).toBe(0.6);
      expect(weights.production).toBe(0.4);
    });

    it('should create with custom weights', () => {
      const calculator = new ScoreCalculator({
        weights: { technical: 0.7, production: 0.3 },
      });
      const weights = calculator.getWeights();
      expect(weights.technical).toBe(0.7);
      expect(weights.production).toBe(0.3);
    });

    it('should create with custom decimal places', () => {
      const calculator = new ScoreCalculator({ decimalPlaces: 3 });
      expect(calculator.getDecimalPlaces()).toBe(3);
    });

    it('should throw error if weights do not sum to 1.0', () => {
      expect(() => {
        new ScoreCalculator({ weights: { technical: 0.5, production: 0.3 } });
      }).toThrow(/Weights must sum to 1.0/);
    });

    it('should throw error if weights sum to more than 1.0', () => {
      expect(() => {
        new ScoreCalculator({ weights: { technical: 0.7, production: 0.5 } });
      }).toThrow(/Weights must sum to 1.0/);
    });

    it('should accept weights that sum to 1.0 within floating point tolerance', () => {
      expect(() => {
        new ScoreCalculator({ weights: { technical: 0.333333, production: 0.666667 } });
      }).not.toThrow();
    });
  });

  describe('calculate()', () => {
    let calculator: ScoreCalculator;

    beforeEach(() => {
      calculator = new ScoreCalculator();
    });

    it('should calculate overall score with default weights', () => {
      const result = calculator.calculate(80, 70);
      // (80 * 0.6) + (70 * 0.4) = 48 + 28 = 76
      expect(result).toBe(76);
    });

    it('should calculate with custom weights', () => {
      const customCalculator = new ScoreCalculator({
        weights: { technical: 0.7, production: 0.3 },
      });
      const result = customCalculator.calculate(80, 70);
      // (80 * 0.7) + (70 * 0.3) = 56 + 21 = 77
      expect(result).toBe(77);
    });

    describe('boundary conditions', () => {
      it('should handle (0, 0)', () => {
        const result = calculator.calculate(0, 0);
        expect(result).toBe(0);
      });

      it('should handle (100, 100)', () => {
        const result = calculator.calculate(100, 100);
        expect(result).toBe(100);
      });

      it('should handle (50, 50)', () => {
        const result = calculator.calculate(50, 50);
        // (50 * 0.6) + (50 * 0.4) = 30 + 20 = 50
        expect(result).toBe(50);
      });

      it('should handle (0, 100)', () => {
        const result = calculator.calculate(0, 100);
        // (0 * 0.6) + (100 * 0.4) = 0 + 40 = 40
        expect(result).toBe(40);
      });

      it('should handle (100, 0)', () => {
        const result = calculator.calculate(100, 0);
        // (100 * 0.6) + (0 * 0.4) = 60 + 0 = 60
        expect(result).toBe(60);
      });
    });

    describe('verdict threshold scenarios', () => {
      it('should calculate score exactly at FAIL/NEEDS_PRACTICE boundary (60)', () => {
        // To get 60: (technicalDepth * 0.6) + (productionReadiness * 0.4) = 60
        // Let's use (60, 60): (60 * 0.6) + (60 * 0.4) = 36 + 24 = 60
        const result = calculator.calculate(60, 60);
        expect(result).toBe(60);
      });

      it('should calculate score exactly at NEEDS_PRACTICE/PASS boundary (80)', () => {
        // To get 80: (technicalDepth * 0.6) + (productionReadiness * 0.4) = 80
        // Let's use (80, 80): (80 * 0.6) + (80 * 0.4) = 48 + 32 = 80
        const result = calculator.calculate(80, 80);
        expect(result).toBe(80);
      });

      it('should calculate score just below FAIL threshold (59)', () => {
        // To get 59: use (58, 60): (58 * 0.6) + (60 * 0.4) = 34.8 + 24 = 58.8
        const result = calculator.calculate(58, 60);
        expect(result).toBe(58.8);
      });

      it('should calculate score just above FAIL threshold (61)', () => {
        // To get 61: use (62, 60): (62 * 0.6) + (60 * 0.4) = 37.2 + 24 = 61.2
        const result = calculator.calculate(62, 60);
        expect(result).toBe(61.2);
      });

      it('should calculate score just below PASS threshold (79)', () => {
        // To get 79: use (78, 80): (78 * 0.6) + (80 * 0.4) = 46.8 + 32 = 78.8
        const result = calculator.calculate(78, 80);
        expect(result).toBe(78.8);
      });

      it('should calculate score just above PASS threshold (81)', () => {
        // To get 81: use (82, 80): (82 * 0.6) + (80 * 0.4) = 49.2 + 32 = 81.2
        const result = calculator.calculate(82, 80);
        expect(result).toBe(81.2);
      });
    });

    describe('decimal inputs and rounding', () => {
      it('should handle decimal inputs correctly', () => {
        const result = calculator.calculate(75.5, 80.25);
        // (75.5 * 0.6) + (80.25 * 0.4) = 45.3 + 32.1 = 77.4
        expect(result).toBe(77.4);
      });

      it('should round to 2 decimal places by default', () => {
        const result = calculator.calculate(75.555, 80.333);
        // Should be rounded to 2 decimal places
        const decimalPlaces = result.toString().split('.')[1];
        expect(decimalPlaces ? decimalPlaces.length : 0).toBeLessThanOrEqual(2);
      });

      it('should respect custom decimal places', () => {
        const customCalculator = new ScoreCalculator({ decimalPlaces: 3 });
        const result = customCalculator.calculate(75.555, 80.333);
        const decimalPlaces = result.toString().split('.')[1];
        expect(decimalPlaces ? decimalPlaces.length : 0).toBeLessThanOrEqual(3);
      });
    });

    describe('error handling', () => {
      it('should throw error for negative technicalDepth', () => {
        expect(() => calculator.calculate(-10, 50)).toThrow(/technicalDepth must be between 0 and 100/);
      });

      it('should throw error for negative productionReadiness', () => {
        expect(() => calculator.calculate(50, -10)).toThrow(/productionReadiness must be between 0 and 100/);
      });

      it('should throw error for technicalDepth > 100', () => {
        expect(() => calculator.calculate(150, 50)).toThrow(/technicalDepth must be between 0 and 100/);
      });

      it('should throw error for productionReadiness > 100', () => {
        expect(() => calculator.calculate(50, 150)).toThrow(/productionReadiness must be between 0 and 100/);
      });

      it('should throw error for both scores out of range', () => {
        expect(() => calculator.calculate(-10, 150)).toThrow(/technicalDepth must be between 0 and 100/);
      });
    });
  });

  describe('calculateWithBreakdown()', () => {
    let calculator: ScoreCalculator;

    beforeEach(() => {
      calculator = new ScoreCalculator();
    });

    it('should return detailed breakdown', () => {
      const result = calculator.calculateWithBreakdown(80, 70);
      expect(result.overallScore).toBe(76);
      expect(result.technicalContribution).toBe(48);
      expect(result.productionContribution).toBe(28);
      expect(result.weights).toEqual({ technical: 0.6, production: 0.4 });
    });

    it('should show correct contributions with custom weights', () => {
      const customCalculator = new ScoreCalculator({
        weights: { technical: 0.7, production: 0.3 },
      });
      const result = customCalculator.calculateWithBreakdown(80, 70);
      expect(result.technicalContribution).toBe(56);
      expect(result.productionContribution).toBe(21);
      expect(result.weights).toEqual({ technical: 0.7, production: 0.3 });
    });

    it('should validate input ranges', () => {
      expect(() => calculator.calculateWithBreakdown(-10, 50)).toThrow();
      expect(() => calculator.calculateWithBreakdown(50, -10)).toThrow();
      expect(() => calculator.calculateWithBreakdown(150, 50)).toThrow();
      expect(() => calculator.calculateWithBreakdown(50, 150)).toThrow();
    });
  });
});
