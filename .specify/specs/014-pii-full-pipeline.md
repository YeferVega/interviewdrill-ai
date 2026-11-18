# Specification 014: PII Sanitizer in Full Pipeline

## Overview
Ensure `RegexPiiSanitizer` is applied at **all** entry points to the LLM and before every persistence operation in the InterviewDrill AI system.

## Stack
- **.NET 11 / C# 15** (implemented in `InterviewDrill.Infrastructure.Security` and `InterviewDrill.Infrastructure.Logging`)
- **Interface in `InterviewDrill.Core.Security`**: `IPiiSanitizer`, `PiiSanitizationResult`, `PiiPatternDefinition`, `PiiFinding`
- **Implementation in `InterviewDrill.Infrastructure.Security`**: `RegexPiiSanitizer`
- **PII-free logging in `InterviewDrill.Infrastructure.Logging`**: `PiiSanitizingLoggerProvider`, `PiiSanitizingLogger`, `SimpleOptionsMonitor<T>`

## User Stories
- **As a** developer, **I want** `RegexPiiSanitizer` applied before sending any text to an LLM, **so that** sensitive data is never exposed to external providers.
- **As a** developer, **I want** `RegexPiiSanitizer` applied before persisting any data to the database, **so that** PII is never stored in plain text.
- **As a** security auditor, **I want** to verify that all LLM entry points and persistence points sanitize PII, **so that** privacy regulations (GDPR, CCPA) are met.
- **As an** operator, **I want** logs without PII or secrets, **so that** sensitive data does not leak into traces or log files.

## Functional Requirements
### FR1: PII sanitization before LLM
- [x] JD Parser: sanitize JD text before sending to the LLM (`LlmJdParserAgent`)
- [x] Interviewer: sanitize questions before sending to the LLM for generation (`LlmInterviewerQuestionGenerator`)
- [x] Architecture Evaluator: sanitize the candidate's answer before sending to the LLM judge (`LlmArchitectureJudge`)
- [x] Security & Rigor Judge: sanitize the candidate's answer before sending to the LLM judge (`LlmSecurityJudge`)
- [x] Study Guide Generator: sanitize text before sending to the LLM for enrichment

### FR2: PII sanitization before persistence
- [x] QuestionRepository: sanitize question text before saving to EF Core (`SqlQuestionRepository` via `InterviewAnswerProcessor`)
- [x] InterviewAnswerRepository: sanitize candidate answers before saving
- [x] SessionStore: sanitize session state before serializing and saving (`EfInterviewerSessionStore`)
- [x] StudyGuideRepository: sanitize the study guide before saving (`SqlStudyGuideRepository`)

### FR3: PII sanitization in embeddings and cache
- [x] Sanitize text before generating embeddings for semantic search (`SemanticCacheService.GetAsync`)
- [x] Sanitize the answer before caching (`SemanticCacheService.SetAsync`)
- [x] Do not store embeddings of unsanitized text containing PII

### FR4: Complete PII detection
- [x] Emails, phone numbers, SSNs, credit cards, IPs (IPv4/IPv6), names, physical addresses
- [x] Configurable custom patterns (`AddPattern`)

### FR5: Detect-only and sanitize modes
- [x] `Sanitize(input, mask: true)` → text with placeholders
- [x] `Sanitize(input, mask: false)` → detection only, original text

### FR6: PII-free logging
- [x] `PiiSanitizingLoggerProvider` wraps `ILoggerProvider` to filter PII and secrets from all log messages
- [x] `PiiSanitizingLogger` sanitizes message, state, and exceptions before delegating to the inner logger
- [x] `SimpleOptionsMonitor<T>` to use with `ConsoleLoggerProvider` without depending on the options system
- [x] Registered in `Program.cs` of `InterviewDrill.Api`

## Non-Functional Requirements
- **Security**: PII sanitized before LLM and persistence in 100% of the points
- **Security**: PII and secrets never logged (PiiSanitizingLogger)
- **Performance**: Sanitization < 100ms for texts up to 10KB
- **Accuracy**: Detection > 95% of common PII; false positives acceptable, false negatives unacceptable

## Acceptance Criteria
- [x] `RegexPiiSanitizer` implemented and tested (emails, phones, SSNs, cards, IPs, names, addresses)
- [x] PII sanitized in JD Parser before LLM (tests: LlmPiiIntegrationTests)
- [x] PII sanitized in Interviewer before LLM for question generation (tests: LlmPiiIntegrationTests)
- [x] PII sanitized in Architecture Evaluator before the LLM judge (tests: LlmPiiIntegrationTests)
- [x] PII sanitized in Security & Rigor Judge before the LLM judge (tests: LlmPiiIntegrationTests)
- [x] PII sanitized in Study Guide Generator before LLM for enrichment
- [x] PII sanitized in QuestionRepository before persisting
- [x] PII sanitized in InterviewAnswerRepository before persisting
- [x] PII sanitized in SessionStore before serializing
- [x] PII sanitized in StudyGuideRepository before persisting
- [x] PII sanitized before generating embeddings (SemanticCacheService)
- [x] Detect-only and sanitize modes working
- [x] Configurable custom patterns
- [x] Unit tests for each integration point (> 85% coverage)
- [x] PII- and secret-free logging implemented and registered in DI

## Out of Scope
- PII storage (the system must not store PII)
- Auditing of PII accesses (phase 2)
- Retention policies/GDPR deletion (phase 2)

## Dependencies
- **Spec 006**: PII Sanitizer Module (`RegexPiiSanitizer` implemented)
- **Spec 009**: JD Parser (integration point)
- **Spec 005**: Interviewer Agent (integration point)
- **Spec 003**: Architecture Evaluator (integration point)
- **Spec 004**: Security & Rigor Judge (integration point)
- **Spec 010**: Study Guide Generator (integration point)
- **Spec 011**: Persistence (integration point)
- **Spec 007**: Semantic Cache (PII integration point in embeddings)

## Risks
| Risk | Mitigation |
|------|------------|
| PII not sanitized at some pipeline point | Exhaustive audit of all LLM entry and persistence points |
| False negatives (undetected PII) | Conservative patterns, prefer false positives |
| Performance degrades with many sanitization points | Compiled regexes, efficient processing |
| Forgetting to sanitize in new endpoints | Mandatory code review for new LLM/persistence points |
| PII in application logs | PiiSanitizingLoggerProvider wraps all log sinks |
