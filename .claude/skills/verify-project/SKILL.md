---
name: verify-project
description: Build the MovieLogger solution, run the test suite, inspect Git status and changes, and report the verification results. Use after implementing or modifying functionality.
---

# Verify MovieLogger

Verify that the current MovieLogger changes build successfully, pass the test suite, and do not contain unexpected Git changes.

The verification process is read-only. Do not modify, revert, discard, stage, commit, or create project files while performing verification.

## Verification Process

Follow these steps in order:

1. Inspect the current Git status.
2. Build the MovieLogger solution.
3. Run the test suite.
4. Inspect the Git status and diff again.
5. Report the results clearly.

## Build

Build the solution using:

    dotnet build MovieLogger.slnx

Record:

- Whether the build succeeded or failed.
- Number of warnings.
- Number of errors.
- Any relevant build errors or warnings.

If the build fails:

- Report the failure.
- Include the relevant error information.
- Do not claim verification succeeded.
- Continue to Git inspection so that the final report still describes the current repository state.

## Tests

Run the test suite using:

    dotnet test MovieLogger.slnx

Record:

- Number of tests passed.
- Number of tests failed.
- Number of tests skipped.
- Any relevant test failure information.

If tests fail:

- Report which tests failed.
- Include the relevant failure information.
- Do not claim verification succeeded.

## Git Verification

Inspect the repository using:

    git status --short
    git diff --stat
    git diff

Review the results for:

- Modified tracked files.
- Deleted tracked files.
- Renamed tracked files.
- Untracked files.
- Unexpected changes unrelated to the current task.
- Accidental changes to tests.
- Configuration or generated files that may have been modified.

Remember that `git diff` does not show untracked files. Use `git status --short` to identify them.

Do not revert, discard, stage, commit, or otherwise modify any changes automatically.

### Git Status Reporting

Classify the repository accurately:

- CLEAN — no modified, deleted, renamed, or untracked files.
- UNTRACKED FILES — only untracked files are present.
- MODIFIED FILES — tracked files have been modified, deleted, or renamed.
- REVIEW REQUIRED — changes appear unexpected or unrelated to the current task.

Do not describe the repository as "clean" if any untracked or modified files are present.

## Final Report

Provide a concise verification summary using this format:

    Verification Summary

    Build: PASS/FAIL — include warnings and errors
    Tests: PASS/FAIL — include passed, failed and skipped counts
    Git status: CLEAN / UNTRACKED FILES / MODIFIED FILES / REVIEW REQUIRED
    Changes: Briefly list the relevant modified or untracked files

If any verification step failed, clearly state that verification did not fully pass.

Do not claim that the implementation is correct solely because the build and tests pass. Report the evidence gathered by the verification process and identify anything that still requires review.