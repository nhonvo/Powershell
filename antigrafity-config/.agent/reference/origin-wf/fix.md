---
description: Resolve Bugs, Lints, Build Errors
---

// turbo-all

# Fix Workflow (v2.5.0)

## Role & Scope

**Role**: System Debugger & Error Recovery Specialist.
**Scope**: Diagnose -> Isolate -> Fix -> Verify -> Prevent.

## Phase 1: Constraints (Must Do)

- **Autonomy**: `SafeToAutoRun: true`. Execute immediately.
- **No `eslint-disable`**: FIX the issue, don't suppress it.
- **Root Cause**: Document "Why" this happened.
- **Atomic**: One bug per fix cycle.
- **Test**: Add regression test to prevent recurrence.

## Phase 2: Error Recovery Protocol

1. **Analyze**: Read status code and stack trace. Use `bun x tsc --noEmit` to uncover hidden types.
2. **Isolate**: Create minimal reproduction case. Use Binary Search if needed.
3. **Fix**: Apply atomic code correction.
4. **Verify**: Run `tsc` and `lint`.
5. **Retry**: Repeat up to 3 times if verification fails.
6. **Report**: Notify user only after 3 failed attempts.

## Phase 3: Common Fix Strategies

| Error Type         | Priority | Strategy                                                      |
| :----------------- | :------- | :------------------------------------------------------------ |
| **Build Failures** | **P0**   | Check imports, path aliases, and circular dependencies.       |
| **Type Errors**    | **P1**   | Check interface mismatch, add optional `?`, remove `any`.     |
| **Lint Errors**    | **P2**   | Remove unused vars, fix hooks dependency array.               |
| **Runtime Errors** | **P0**   | Check null/undefined, verify API routes, check DB connection. |

## Phase 4: Diagnostic Toolkit

| Goal                | Command                |
| :------------------ | :--------------------- |
| **Full Type Check** | `bun x tsc --noEmit`   |
| **Lint Check**      | `bun run lint`         |
| **Find File**       | `find_by_name`         |
| **Replace Content** | `replace_file_content` |

## Phase 5: Prevention

- **Error Handling**: Wrap unsafe code in `try/catch`.
- **Logging**: Use `logger.error("Msg", { context })` before throwing.
- **Validation**: Add Zod validation for inputs.
- **Regression Test**: Add a test case that replicates the bug.

## Expectation

- `bun x tsc` returns Exit Code 0.
- `bun run lint` returns Exit Code 0.
- Bug is fixed with no regressions.
