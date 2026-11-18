/**
 * VerdictDeterminer Unit Tests
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { 
  VerdictDeterminer, 
  DefaultVerdictStrategy 
} from '../../../src/agents/scoring-synthesizer/VerdictDeterminer.ts';

describe('VerdictDeterminer', () => {
  describe('DefaultVerdictStrategy', () => {
    describe('constructor', () => {
      it('should create with default thresholds (80, 60)', () => {
        const strategy = new DefaultVerdictStrategy();
        const thresholds = strategy.getThresholds();
        expect(thresholds.pass).toBe(80);
        expect(thresholds.needsPractice).toBe(60);
      });

      it('should create with custom thresholds', () => {
        const strategy = new DefaultVerdictStrategy(75, 50);
        const thresholds = strategy.getThresholds();
        expect(thresholds.pass).toBe(75);
        expect(thresholds.needsPractice).toBe(50);
      });

      it('should throw error if passThreshold <= needsPracticeThreshold', () => {
        expect(() => {
          new DefaultVerdictStrategy(60, 80);
        }).toThrow(/passThreshold .* must be greater than needsPracticeThreshold/);
      });

      it('should throw error if thresholds are equal', () => {
        expect(() => {
          new DefaultVerdictStrategy(70, 70);
        }).toThrow(/passThreshold .* must be greater than needsPracticeThreshold/);
      });
    });

    describe('determine()', () => {
      let strategy: DefaultVerdictStrategy;

      beforeEach(() => {
        strategy = new DefaultVerdictStrategy();
      });

      it('should return PASS for score >= 80', () => {
        expect(strategy.determine(80)).toBe('PASS');
        expect(strategy.determine(85)).toBe('PASS');
        expect(strategy.determine(100)).toBe('PASS');
      });

      it('should return NEEDS_PRACTICE for 60 <= score < 80', () => {
        expect(strategy.determine(60)).toBe('NEEDS_PRACTICE');
        expect(strategy.determine(70)).toBe('NEEDS_PRACTICE');
        expect(strategy.determine(79)).toBe('NEEDS_PRACTICE');
        expect(strategy.determine(79.99)).toBe('NEEDS_PRACTICE');
      });

      it('should return FAIL for score < 60', () => {
        expect(strategy.determine(0)).toBe('FAIL');
        expect(strategy.determine(30)).toBe('FAIL');
        expect(strategy.determine(59)).toBe('FAIL');
        expect(strategy.determine(59.99)).toBe('FAIL');
      });

      describe('boundary conditions', () => {
        it('should return PASS at exactly 80', () => {
          expect(strategy.determine(80)).toBe('PASS');
        });

        it('should return NEEDS_PRACTICE at exactly 60', () => {
          expect(strategy.determine(60)).toBe('NEEDS_PRACTICE');
        });

        it('should return FAIL at exactly 0', () => {
          expect(strategy.determine(0)).toBe('FAIL');
        });

        it('should return PASS at exactly 100', () => {
          expect(strategy.determine(100)).toBe('PASS');
        });

        it('should handle decimal scores correctly', () => {
          expect(strategy.determine(79.5)).toBe('NEEDS_PRACTICE');
          expect(strategy.determine(80.5)).toBe('PASS');
          expect(strategy.determine(59.5)).toBe('FAIL');
          expect(strategy.determine(60.5)).toBe('NEEDS_PRACTICE');
        });
      });

      describe('all verdict thresholds', () => {
        const testCases = [
          { score: 0, expected: 'FAIL' as const },
          { score: 30, expected: 'FAIL' as const },
          { score: 59, expected: 'FAIL' as const },
          { score: 60, expected: 'NEEDS_PRACTICE' as const },
          { score: 70, expected: 'NEEDS_PRACTICE' as const },
          { score: 79, expected: 'NEEDS_PRACTICE' as const },
          { score: 80, expected: 'PASS' as const },
          { score: 90, expected: 'PASS' as const },
          { score: 100, expected: 'PASS' as const },
        ];

        testCases.forEach(({ score, expected }) => {
          it(`should return ${expected} for score ${score}`, () => {
            expect(strategy.determine(score)).toBe(expected);
          });
        });
      });
    });
  });

  describe('VerdictDeterminer', () => {
    describe('constructor', () => {
      it('should create with default strategy', () => {
        const determiner = new VerdictDeterminer();
        expect(determiner.determine(80)).toBe('PASS');
      });

      it('should create with custom strategy', () => {
        const customStrategy = new DefaultVerdictStrategy(75, 50);
        const determiner = new VerdictDeterminer(customStrategy);
        expect(determiner.determine(75)).toBe('PASS');
        expect(determiner.determine(50)).toBe('NEEDS_PRACTICE');
        expect(determiner.determine(49)).toBe('FAIL');
      });
    });

    describe('determine()', () => {
      let determiner: VerdictDeterminer;

      beforeEach(() => {
        determiner = new VerdictDeterminer();
      });

      it('should return correct verdict for all ranges', () => {
        expect(determiner.determine(85)).toBe('PASS');
        expect(determiner.determine(70)).toBe('NEEDS_PRACTICE');
        expect(determiner.determine(45)).toBe('FAIL');
      });
    });

    describe('determineWithDetails()', () => {
      let determiner: VerdictDeterminer;

      beforeEach(() => {
        determiner = new VerdictDeterminer();
      });

      it('should return detailed information for FAIL verdict', () => {
        const result = determiner.determineWithDetails(50);
        expect(result.verdict).toBe('FAIL');
        expect(result.score).toBe(50);
        expect(result.thresholds).toEqual({ pass: 80, needsPractice: 60 });
        expect(result.distanceToNextVerdict).toBe(10); // 60 - 50 = 10
        expect(result.nextVerdict).toBe('NEEDS_PRACTICE');
      });

      it('should return detailed information for NEEDS_PRACTICE verdict', () => {
        const result = determiner.determineWithDetails(70);
        expect(result.verdict).toBe('NEEDS_PRACTICE');
        expect(result.score).toBe(70);
        expect(result.thresholds).toEqual({ pass: 80, needsPractice: 60 });
        expect(result.distanceToNextVerdict).toBe(10); // 80 - 70 = 10
        expect(result.nextVerdict).toBe('PASS');
      });

      it('should return detailed information for PASS verdict', () => {
        const result = determiner.determineWithDetails(90);
        expect(result.verdict).toBe('PASS');
        expect(result.score).toBe(90);
        expect(result.thresholds).toEqual({ pass: 80, needsPractice: 60 });
        expect(result.distanceToNextVerdict).toBe(0);
        expect(result.nextVerdict).toBeNull();
      });

      it('should calculate distance correctly at boundaries', () => {
        const result59 = determiner.determineWithDetails(59);
        expect(result59.distanceToNextVerdict).toBe(1); // 60 - 59 = 1

        const result79 = determiner.determineWithDetails(79);
        expect(result79.distanceToNextVerdict).toBe(1); // 80 - 79 = 1
      });
    });
  });
});
