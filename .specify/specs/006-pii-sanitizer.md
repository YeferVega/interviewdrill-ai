# Specification: PII Sanitizer Module

## Overview
The PII Sanitizer is a critical security module that detects and sanitizes personally identifiable information (PII) in the data before sending it to LLMs or vectorization systems. It implements masking and cleansing of sensitive data.

## Requirements

### Functional Requirements
1. Detect emails in text
2. Detect phone numbers
3. Detect social security numbers (SSN)
4. Detect credit card numbers
5. Detect IP addresses
6. Detect full names (common patterns)
7. Detect physical addresses
8. Mask detected PII with placeholders ([EMAIL], [PHONE], etc.)
9. Support configuration of custom patterns
10. Provide a "detect-only" mode for analysis without modification
11. Return sanitized text and the list of PII found

### Non-Functional Requirements
1. **Performance**: Sanitization < 100ms for texts up to 10KB
2. **Accuracy**: Detection > 95% of common PII
3. **Safety**: False positives acceptable, false negatives unacceptable
4. **Configurability**: Customizable patterns per use case

## User Stories
- as a **system**, I want to sanitize data before sending it to LLMs to protect the user's privacy
- as a **security team**, I want to comply with privacy regulations (GDPR, CCPA)
- as a **developer**, I want a simple API to sanitize text at any point in the pipeline

## Acceptance Criteria
- [x] Detects and masks emails correctly
- [x] Detects and masks phone numbers in multiple formats
- [x] Detects and masks SSN
- [x] Detects and masks credit cards
- [x] Detects and masks IPs (IPv4 and IPv6)
- [x] Detects common name patterns
- [ ] Detects physical addresses
- [x] Replaces PII with descriptive placeholders
- [x] Returns the list of PII found with positions
- [x] Supports detect-only mode
- [x] Customizable patterns
- [x] Unit tests for each main PII type
- [ ] Performance tests
- [ ] Coverage > 90%

## Out of Scope
- Data validation (detection and masking only)
- PII storage
- Access auditing

## Stack
- **.NET 11 / C# 15** (implementation in `InterviewDrill.Infrastructure`)
- **Regex (.NET)** for pattern matching
- **Interfaces in `InterviewDrill.Core`** if exposed as a service

## Dependencies
- **Regex (.NET)**: For pattern matching
- **C#**: Type-safe implementation

## Risks
1. **Risk**: False negatives (undetected PII)
   - **Mitigation**: Use conservative patterns, prefer false positives

2. **Risk**: Performance degrades with very long texts
   - **Mitigation**: Chunk processing if necessary

3. **Risk**: Outdated patterns
   - **Mitigation**: Keep the pattern library updated
