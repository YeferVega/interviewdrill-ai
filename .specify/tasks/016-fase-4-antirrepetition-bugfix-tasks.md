# Task List: 016-fase-4-antirrepetition-bugfix

## Sprint/Iteration: Phase 4 — Anti-Repetition Bug Fix and Coverage

### Tasks

#### Task 1: Detect the anti-repetition bug across sessions
- **Description**: Extend the E2E smoke test to run a second interview with the same user+JD and compare the set of questions asked. The verification revealed that the second session repeated questions from the first (e.g. the Kafka question with P95 latency).
- **Priority**: High
- **Estimated Time**: 1 hour
- **Dependencies**: Existing E2E smoke test
- **Status**: [x] Complete — `scripts/e2e-smoke.ps1` refactored with `Invoke-InterviewRun`; the initial run failed with overlap, confirming the bug.

#### Task 2: Fix the lifecycle of InMemoryQuestionRegistry
- **Description**: `InMemoryQuestionRegistry` was registered as `AddScoped`, so it was recreated on each HTTP request and lost the question history (the `ConcurrentDictionary` was not shared). Changed to `AddSingleton` so the registry of asked questions persists across requests and process sessions. Verified that the class is already thread-safe (`ConcurrentDictionary`).
- **Priority**: High
- **Estimated Time**: 0.5 hours
- **Dependencies**: Task 1
- **Status**: [x] Complete — `Program.cs` line in the in-memory block: `AddSingleton<IQuestionRegistry, InMemoryQuestionRegistry>()`.

#### Task 3: Verify the fix end-to-end
- **Description**: Rebuild, run the E2E with anti-repetition again, and confirm that the second session does not overlap questions from the first. Confirm that Agents.Tests and Api.Tests do not regress with the change to Singleton.
- **Priority**: High
- **Estimated Time**: 0.5 hours
- **Dependencies**: Task 2
- **Status**: [x] Complete — E2E: "session 2 asked 8 distinct questions, 0 overlap with session 1". Agents 42/42, Api 12/12.

#### Task 4: Expand VectorMath coverage (Core)
- **Description**: Cover the missing edge cases in `InterviewDrill.Core/Vectors/VectorMath.cs`: empty vectors (throws `ArgumentException`), zero-norm vector (returns 0), and opposite vectors (similarity −1).
- **Priority**: Medium
- **Estimated Time**: 0.5 hours
- **Dependencies**: None
- **Status**: [x] Complete — +3 tests in `VectorMathTests.cs`; Core goes from 32 to 35 tests.

#### Task 5: Update tracking documentation
- **Description**: Reflect the anti-repetition bugfix and the Core expansion in `IMPLEMENTATION_STATUS.md` (Phase 4 section and Known Issues), in the spec 011 tasks, and in this task list.
- **Priority**: Medium
- **Estimated Time**: 0.5 hours
- **Dependencies**: Tasks 1-4
- **Status**: [x] Complete

#### Task 6: Retry running Infrastructure.Tests (environment limitation)
- **Description**: Clean bin/obj, rebuild, and unblock the DLLs, then try to run Infrastructure.Tests to validate the 2 model tests updated to `varbinary(max)`.
- **Priority**: Low
- **Estimated Time**: 0.5 hours
- **Dependencies**: Tasks 2-3
- **Status**: [ ] Not started — Not executable in this environment: Windows AppLocker/WDAC blocks the assembly (0x800711C7). The code is correct and anti-repetition was verified via E2E. Still to be run by the user in a regular PowerShell session (outside the sandbox).

### Definition of Done
- [x] Anti-repetition bug detected and documented
- [x] `InMemoryQuestionRegistry` is now a Singleton and persists across sessions
- [x] E2E verifies anti-repetition between two sessions of the same user+JD (0 overlap)
- [x] Core covers the VectorMath edge cases (35/35)
- [x] Agents (42/42) and Api (12/12) with no regression from the lifecycle change
- [x] Build 0 errors
- [x] `IMPLEMENTATION_STATUS.md`, tasks 011 and 016 updated
