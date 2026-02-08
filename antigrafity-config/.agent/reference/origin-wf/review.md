---
description: Security Audit & Code Review
---

// turbo-all

# Review Workflow (v2.5.0)

## Role & Scope

**Role**: Security Auditor (OWASP) & Ruthless Code Reviewer.
**Scope**: Review every line for Security, Quality, and Architecture.

## Phase 1: Automated Gate (The "Holy Trinity")

Code must pass this sequence before manual review:

1. **Types**: `bun x tsc --noEmit`
2. **Lint**: `bun run lint`
3. **Tests**: `bun run test`

## Phase 2: Architecture Review

| Layer       | Requirement                                                              |
| :---------- | :----------------------------------------------------------------------- |
| **Route**   | Validates Auth (`getAuthenticatedContext`). Calls Service. No DB access. |
| **Service** | Business Logic. Calls Repository. Returns Domain Object.                 |
| **Repo**    | DB Access. Calls `connect()`. Uses Mapper.                               |
| **Mapper**  | Transforms `ObjectId` <-> `string`.                                      |

## Phase 3: Security Audit (OWASP Top 10)

| Check              | What to Look For              | Fix                                 |
| :----------------- | :---------------------------- | :---------------------------------- |
| **Injection**      | User input in DB query.       | Use Zod validation + Sanitization.  |
| **Auth**           | Missing `userId` check.       | Always filter queries by `userId`.  |
| **Sensitive Data** | Stack traces in API response. | Map 500 errors to generic messages. |
| **XSS**            | `dangerouslySetInnerHTML`.    | Use `DOMPurify` or remove.          |
| **Secrets**        | API keys in code.             | Move to `.env`.                     |

## Phase 4: Code Quality Criteria

- **Naming**: `PascalCase` components, `camelCase` functions.
- **Types**: Zero `any` usage. Strict interfaces.
- **Imports**: Absolute paths (`@/lib/...`). No circular deps.
- **Performance**: No `await` in loops (use `Promise.all`). Pagination used.

## Phase 5: Approval Matrix

- **CRITICAL (Block)**: Security vulnerabilies, `any` types, broken build. -> **Fix Immediately**.
- **High (Comment)**: Architecture violations, missing tests. -> **Request Changes**.
- **Low (Note)**: Style nits, variable names. -> **Approve with Comments**.

## Expectation

- Codebase is healthy.
- Zero critical vulnerabilities.
- All checks pass.
