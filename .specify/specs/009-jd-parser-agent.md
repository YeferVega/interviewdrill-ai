# Specification 009: JD Parser Agent

## Overview
Agent that converts the Job Description uploaded by the user (PDF/DOCX/TXT or pasted text) into a structured `JobProfile` that feeds question generation and the study guide.

## Functional Requirements
1. Accept PDF, DOCX, TXT files and pasted plain text (max. 2 MB / ~30k tokens).
2. Extract text with secure libraries in the .NET backend: `UglyToad.PdfPig` for PDF, `DocumentFormat.OpenXml` for DOCX, and direct reading for TXT. MarkItDown is not used directly because the available packages drag dependencies with known vulnerabilities.
3. **Sanitize PII** from the text before calling the LLM (RegexPiiSanitizer: emails, phone numbers, contact names).
4. Call the LLM with **structured output** (strict JSON schema, temperature 0) to extract:
   - `role` (string)
   - `seniority` (enum: junior | mid | senior | lead | unknown)
   - `requiredSkills[]` (with a 0–1 weight inferred from emphasis in the JD)
   - `niceToHaveSkills[]`
   - `responsibilities[]`
5. Compute `rawTextHash` (SHA-256 of the normalized text) for question deduplication.
6. Allow the user to manually edit the `JobProfile` before starting.
7. Heuristic fallback (keyword-based extraction) when no LLM is available, marking `profileSource: "heuristic"`.

## Non-Functional Requirements
- Full parse < 10s P95 (file → JobProfile).
- Resilience: Polly retry with exponential backoff on HTTP 429.
- Never log the raw JD text without sanitizing it.

## Acceptance Criteria
- [~] PDF/DOCX/TXT → text with per-format tests (TXT and DOCX covered; PDF fixture test pending)
- [x] PII sanitized before inference (test with embedded email/phone)
- [x] JSON output validates against the `JobProfile` schema (strict)
- [x] `rawTextHash` stable for the same content (whitespace-insensitive)
- [x] Heuristic fallback works without an LLM
- [x] API endpoint for uploading/pasting a JD returns a `JobProfile`
- [~] Unit tests + golden dataset of 5 real JDs — functional tests and endpoint implemented; golden dataset of 5 JDs pending

## Data Contract
```json
{
  "role": "Senior Backend Engineer",
  "seniority": "senior",
  "requiredSkills": [{"name": "RAG", "weight": 0.9}, {"name": "Kafka", "weight": 0.8}],
  "niceToHaveSkills": ["Kubernetes"],
  "responsibilities": ["Design event-driven services", "Own production readiness"],
  "rawTextHash": "sha256:...",
  "profileSource": "llm"
}
```

## Out of Scope
- Candidate CV/resume parsing (phase 2).
- Company/values detection (phase 2).

## Dependencies
- PII Sanitizer (spec 006)
- Persistence (spec 011) for storing the JobProfile
