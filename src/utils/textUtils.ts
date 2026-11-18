/**
 * Text utilities for text processing and analysis
 * @module textUtils
 */

/**
 * Normalizes text by converting to lowercase, trimming whitespace,
 * and normalizing internal whitespace to single spaces.
 * @param text - The text to normalize
 * @returns Normalized text
 */
export function normalizeText(text: string): string {
  return text
    .toLowerCase()
    .trim()
    .replace(/\s+/g, ' ');
}

/**
 * Removes special characters from text, keeping only alphanumeric
 * characters and basic punctuation (.,!?;:).
 * @param text - The text to clean
 * @returns Cleaned text
 */
export function removeSpecialChars(text: string): string {
  return text.replace(/[^a-zA-Z0-9\s.,!?;:]/g, '');
}

/**
 * Splits text into sentences based on punctuation marks.
 * Handles common abbreviations and decimal numbers.
 * @param text - The text to split
 * @returns Array of sentences
 */
export function extractSentences(text: string): string[] {
  // Split on sentence-ending punctuation followed by space or end
  const sentences = text
    .replace(/([.!?])\s+(?=[A-Z])/g, '$1|SPLIT|')
    .split('|SPLIT|')
    .map(s => s.trim())
    .filter(s => s.length > 0);
  
  return sentences;
}

/**
 * Finds and extracts context around a keyword in text.
 * Returns approximately 100 characters before and after the keyword.
 * @param text - The full text to search
 * @param keyword - The keyword to find context for
 * @param contextLength - Number of characters to extract on each side (default: 100)
 * @returns Context string or empty string if keyword not found
 */
export function findKeywordContext(
  text: string,
  keyword: string,
  contextLength: number = 100
): string {
  const lowerText = text.toLowerCase();
  const lowerKeyword = keyword.toLowerCase();
  const index = lowerText.indexOf(lowerKeyword);
  
  if (index === -1) {
    return '';
  }
  
  const start = Math.max(0, index - contextLength);
  const end = Math.min(text.length, index + keyword.length + contextLength);
  
  let context = text.substring(start, end).trim();
  
  // Add ellipsis if we're not at the start/end of the text
  if (start > 0) {
    context = '...' + context;
  }
  if (end < text.length) {
    context = context + '...';
  }
  
  return context;
}

/**
 * Counts the number of words in a text string.
 * @param text - The text to count words in
 * @returns Number of words
 */
export function countWords(text: string): number {
  const trimmed = text.trim();
  if (trimmed.length === 0) {
    return 0;
  }
  return trimmed.split(/\s+/).length;
}

/**
 * Extracts the first N characters of text, breaking at word boundaries.
 * @param text - The text to truncate
 * @param maxLength - Maximum length of the result
 * @returns Truncated text
 */
export function truncateText(text: string, maxLength: number): string {
  if (text.length <= maxLength) {
    return text;
  }
  
  const truncated = text.substring(0, maxLength);
  const lastSpace = truncated.lastIndexOf(' ');
  
  if (lastSpace > maxLength * 0.8) {
    return truncated.substring(0, lastSpace) + '...';
  }
  
  return truncated + '...';
}
