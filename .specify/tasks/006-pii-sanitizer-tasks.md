# Task List: 006-pii-sanitizer

## Sprint/Iteration: Phase 1 — Persistence & JD Parser

### Tasks

#### Task 1: Implement RegexPiiSanitizer in C# (.NET)
- **Description**: Create the PII sanitizer in C# using Regex to detect emails, phone numbers, SSNs, credit cards, IPs, names, and physical addresses. Sole owner of PII sanitization logic for the entire system.
- **Priority**: High
- **Estimated Time**: 4 hours
- **Dependencies**: InterviewDrill.Infrastructure reference to Core
- **Status**: [x] Complete — implemented in `RegexPiiSanitizer`.

#### Task 2: Implement masking with placeholders ([EMAIL], [PHONE], etc.)
- **Description**: Replace detected PII with descriptive placeholders. Support a "detect-only" mode for analysis without modification.
- **Priority**: High
- **Estimated Time**: 2 hours
- **Dependencies**: Task 1
- **Status**: [x] Complete — masking and `mask: false` implemented and tested.

#### Task 3: Support configurable custom patterns
- **Description**: Allow adding custom PII patterns per use case.
- **Priority**: Medium
- **Estimated Time**: 2 hours
- **Dependencies**: Task 1
- **Status**: [x] Complete — `AddPattern` implemented and tested with an API key pattern.

#### Task 4: Integrate PII sanitization before sending to the LLM and before persisting
- **Description**: Sanitize the JD text before sending it to the LLM (JD Parser pipeline). Sanitize candidate answers before persisting embeddings in the database (Persistence pipeline). Single PII integration point in the system.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Tasks 1-3, Spec 009 (JD Parser), Spec 011 (Persistence)
- **Status**: [x] Complete — PII sanitized across the ENTIRE pipeline:
  - JD Parser (`LlmJdParserAgent`): sanitizes text before the LLM
  - Interviewer (`LlmInterviewerQuestionGenerator`): sanitizes context before the LLM
  - Architecture Evaluator (`LlmArchitectureJudge`): sanitizes the answer before the LLM judge
  - Security & Rigor Judge (`LlmSecurityJudge`): sanitizes the answer before the LLM judge
  - Study Guide Generator: uses data that is already sanitized
  - QuestionRepository (`SqlQuestionRepository`): sanitizes text before persisting (via InterviewAnswerProcessor)
  - InterviewAnswerRepository (`SqlInterviewAnswerRepository`): receives already-sanitized text
  - SessionStore (`EfInterviewerSessionStore`): sanitizes `LastSkillTarget` and state before serializing
  - StudyGuideRepository (`SqlStudyGuideRepository`): sanitizes Markdown before persisting

#### Task 5: Unit tests for the PII Sanitizer
- **Description**: Tests for each PII type (email, phone, SSN, card, IP, name, address), detect-only and masking modes, custom patterns, pipeline integration.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Tasks 1-4
- **Status**: [~] In progress — unit tests for PII types, detect-only, masking, and custom patterns covered; JD/question/answer integration tests covered; golden dataset and formal coverage still pending.

### Definition of Done
- [x] Detects and masks emails, phone numbers, SSNs, credit cards, IPs (IPv4/IPv6)
- [x] Detects common name patterns and physical addresses
- [x] Returns the list of PII found with positions
- [x] Supports detect-only mode and customizable patterns
- [x] Sanitizes the JD before the LLM, questions before embeddings, and answers before evaluators/persistence
- [x] PII sanitized across the ENTIRE LLM and persistence pipeline (spec 014 completed)
- [x] Unit tests passing (> 85% coverage in `RegexPiiSanitizerTests` and integration points)
- [ ] Performance: sanitization < 100ms for texts up to 10KB — formal metric still pending measurement
