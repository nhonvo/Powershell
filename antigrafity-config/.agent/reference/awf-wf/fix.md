---
description: Mission Control Fix Workflow - Self-Fixing Protocol
---

# 🔧 Fix Workflow (Agentic Recovery)

## 1. Diagnosis

1. Read the exact error code from the terminal.
2. Use `Oracle` skill to perform a root cause analysis.
3. Check for recent changes in `implementation_plan.md`.

## 2. Recovery Cycle (Turbo-All)

1. Apply the fix.
2. Run `dotnet build`.
3. If build fails, **DO NOT** ask for help. Analyze the new error and fix again.
4. If build succeeds, run `/verify`.

## 3. The 3-Strike Rule

- You have **3 attempts** to self-fix a logic or build error.
- If the 4th attempt is needed, stop and use `notify_user` with a detailed report of what was tried.

## 4. Prevention

- Every fix MUST include a new validation check or a Zod schema update to prevent recurrence.
