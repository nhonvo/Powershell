---
description: Full flow
---

// turbo-all

# Agency Workflows Index

This directory contains the standardized workflows for the Agent. These workflows guide the agent through complex tasks with consistency, security, and performance in mind.

## Feature Workflow (`feature.md`)

**Role**: Full-Stack Architect.  
**Scope**: Plan, Build, and Polish complete features from scratch.

- **Phase 1: Planning** - Analyze requirements, design DB schema, and plan API/UI hierarchy.
- **Phase 2: Backend** - Create Mongoose Models, Mappers, Repositories, Services, and API Routes.
- **Phase 3: Frontend** - Implement React Query hooks and UI components (Glassmorphism, Motion).
- **Phase 4: Polish** - Run Type Checks, Lints, optimize performance, and ensure Accessibility.
- **Phase 5: Null Safety** - Rigorous handling of `null`, `undefined`, and empty states.
- **Phase 6: Seeding** - Add demo data to `src/server/db/seed-data.json`.
- **Phase 7: Organization** - Split large components and version large services.

## Fix Workflow (`fix.md`)

**Role**: System Debugger & Error Recovery Specialist.  
**Scope**: Resolve bugs, lints, and build errors through an "Analyze -> Fix -> Verify" loop.

- **Step 1: Analyze** - Inspect stack traces and run `tsc` to find root causes.
- **Step 2: Isolate** - Create minimal reproduction cases.
- **Step 3: Fix** - Apply atomic code corrections.
- **Step 4: Verify** - Run `tsc` and `lint` to ensure the fix is solid.
- **Step 5: Retry** - Automated retry loop (up to 3 times) before escalating.

## Refactor Workflow (`refactor.md`)

**Role**: React Expert & Performance Engineer.  
**Scope**: Modernize legacy code and optimize performance.

- **Step 1: Modernization** - Convert Class components to Functional components + Hooks.
- **Step 2: Perf Tuning** - Use `useMemo`, `useCallback`, and `React.memo` for optimization.
- **Step 3: Structural** - Extract logic to proper Repositories, Mappers, and Utilities.
- **Step 4: Verification** - Ensure no behavior changes while maintaining zero `any` types.

## Test Workflow (`test.md`)

**Role**: QA Engineer & Test Automation Specialist.  
**Scope**: Validate logic and prevent regressions.

- **Phase 1: Standards** - Unit (Vitest/Jest), Integration, and E2E (Playwright).
- **Phase 2: Constraints** - Ensure isolation, coverage (>80%), and proper mocking.
- **Phase 3: Unit Testing** - AAA Pattern (Arrange, Act, Assert).
- **Phase 4: E2E Testing** - Critical flow validation using Playwright.
- **Phase 5: Execution** - Run suite (`bun run test`) and analyze failures.

## Document Workflow (`document.md`)

**Role**: API Documentation Specialist & Technical Writer.  
**Scope**: Sync `documentation/` with code implementation.

- **Phase 1: API Documentation** - Document endpoints, parameters, authentication, and examples.
- **Phase 2: Project Docs** - Update `ARCHITECTURE.md`, `ROADMAP.md`, and `DEVELOPMENT_GUIDE.md`.
- **Phase 3: UI/UX Docs** - Document new components and design system tokens.
- **Phase 4: Migration Guides** - Create guides for breaking changes or schema migrations.
- **Phase 5: Chart Documentation** - Specific documentation for data visualizations.

## Review Workflow (`review.md`)

**Role**: Security Auditor & Ruthless Code Reviewer.  
**Scope**: Review lines for Security (OWASP), Quality, and Architecture.

- **Phase 1: Automated Gate** - Run the "Holy Trinity" (Types, Lint, Tests).
- **Phase 2: Architecture** - Verify strict layer separation (Route -> Service -> Repo -> DB).
- **Phase 3: Security Scan** - Check for Injections, Auth missing, XSS, and Secrets in code.
- **Phase 4: Quality Criteria** - Enforce naming conventions and absolute path imports.
- **Phase 5: Approval Matrix** - Block on Critical/Security issues, request changes for High/Arch issues.

## Commit Workflow (`commit.md`)

**Role**: Release Manager / DevOps Engineer.  
**Scope**: Perform quality gates, stage changes, and commit with roadmap integration.

- **Phase 1: Pre-Commit Gate** - Run the "Holy Trinity" (Types, Lint, Tests).
- **Phase 2: Contextual Analysis** - Analyze diffs and align with `ROADMAP.md`.
- **Phase 3: Intelligent Staging** - Stage changes selectively or globally.
- **Phase 4: Semantic Synthesis** - Generate high-quality Conventional Commits.
- **Phase 5: Atomic Commit & Push** - Execute the local commit and remote sync.
- **Phase 6: Bookkeeping** - Update `ROADMAP.md` and `KNOWLEDGE_LOG.md`.
