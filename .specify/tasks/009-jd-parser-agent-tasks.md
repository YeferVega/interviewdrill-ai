# Task List: 009-jd-parser-agent

## Sprint/Iteration: Phase 1 — Persistence & JD Parser

### Tasks

#### Task 1: Integrate text extraction for JDs (PDF/DOCX/TXT)
- **Description**: Set up secure JD extraction in InterviewDrill.Infrastructure to convert uploaded files into plain text. Max. 2 MB. MarkItDown was abandoned because of dependencies with known vulnerabilities; PdfPig is used for PDF and DocumentFormat.OpenXml for DOCX.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: InterviewDrill.Infrastructure reference to Core
- **Status**: [x] Complete

#### Task 2: Implement IJdParserAgent with structured output (LLM, temp 0)
- **Description**: Call the LLM with a strict JSON schema to extract role, seniority, requiredSkills[] with weight 0-1, niceToHaveSkills[], responsibilities[], rawTextHash. Temperature 0 for determinism. PII sanitization of the text is delegated to Spec 006 (RegexPiiSanitizer).
- **Priority**: High
- **Estimated Time**: 4 hours
- **Dependencies**: Task 1, Core contract IJdParserAgent, Spec 006 (RegexPiiSanitizer for prior sanitization)
- **Status**: [x] Complete — implemented with `ILlmCompletionClient`; real provider still pending.

#### Task 3: Implement heuristic fallback (without LLM)
- **description**: Keyword-based extraction when no LLM is available. Mark profileSource: "heuristic".
- **Priority**: Medium
- **Estimated Time**: 2 hours
- **Dependencies**: Task 2
- **Status**: [x] Complete — `HeuristicJdParserAgent` and `FallbackJdParserAgent` implemented.

#### Task 4: API endpoint to upload a JD and get the JobProfile
- **description**: Create an endpoint in InterviewDrill.Api that accepts a file or text and returns a valid JobProfile.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Task 2, InterviewDrill.Api
- **Status**: [x] Complete — `POST /api/jd/parse` implemented with integration tests.

#### Task 5: Unit tests for the JD Parser
- **description**: Tests per format (PDF, DOCX, TXT), JobProfile schema validation, stable rawTextHash, heuristic fallback. Golden dataset of 5 real JDs.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Tasks 1-4
- **Status**: [~] In progress — unit/integration tests, TXT/DOCX, and endpoint covered; PDF fixture and the golden dataset of 5 JDs still pending.

### Definition of Done
- [x] PDF/DOCX/TXT → text with safe extractors (PdfPig for PDF, DocumentFormat.OpenXml for DOCX; TXT/DOCX tested)
- [x] JSON output validates against the JobProfile schema (strict)
- [x] Stable rawTextHash for the same content (whitespace-insensitive)
- [x] Heuristic fallback works without an LLM
- [x] PII sanitization delegated to Spec 006 (not duplicated here)
- [~] Unit tests passing (> 85% coverage) — unit/integration tests and endpoint covered; PDF fixture and the golden dataset of 5 JDs still pending
