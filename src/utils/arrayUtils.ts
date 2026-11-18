/**
 * Utility functions for array operations
 * 
 * @module arrayUtils
 */

/**
 * Merges multiple arrays into one, removing duplicates
 * Uses Set for efficient deduplication
 * 
 * @param arrays - Arrays to merge
 * @returns Merged array with unique values
 */
export function mergeArraysUnique<T>(...arrays: T[][]): T[] {
  const merged = new Set<T>();
  
  for (const array of arrays) {
    for (const item of array) {
      merged.add(item);
    }
  }
  
  return Array.from(merged);
}

/**
 * Removes duplicates from an array
 * 
 * @param array - Array to deduplicate
 * @returns New array with unique values
 */
export function removeDuplicates<T>(array: T[]): T[] {
  return Array.from(new Set(array));
}

/**
 * Checks if an array is empty or undefined
 * 
 * @param array - Array to check
 * @returns true if array is empty, undefined, or null
 */
export function isNullOrEmpty<T>(array: T[] | undefined | null): boolean {
  return !array || array.length === 0;
}

/**
 * Safely gets array length, returns 0 for undefined/null
 * 
 * @param array - Array to get length of
 * @returns Length of array or 0
 */
export function safeLength<T>(array: T[] | undefined | null): number {
  return array ? array.length : 0;
}

/**
 * Filters out empty strings and null/undefined values from an array
 * 
 * @param array - Array to filter
 * @returns Filtered array with only non-empty strings
 */
export function filterEmptyStrings(array: string[]): string[] {
  return array.filter(item => item !== null && item !== undefined && item.trim() !== '');
}

/**
 * Creates a frequency map of items in an array
 * 
 * @param array - Array to analyze
 * @returns Map of item frequencies
 */
export function getFrequencyMap<T>(array: T[]): Map<T, number> {
  const frequency = new Map<T, number>();
  
  for (const item of array) {
    frequency.set(item, (frequency.get(item) || 0) + 1);
  }
  
  return frequency;
}
