# Code Review & Quality Standards (Always-On Rule)

When reviewing code, auditing architectures, or inspecting pull requests, you MUST enforce the following review protocols:

## 1. Severity Rating Taxonomy

Every identified finding must be categorized strictly using these severity levels:

* **[P0 - CRITICAL]**: Security vulnerabilities (arbitrary command execution, credential leakage, path traversal), fatal race conditions or deadlocks that freeze the application, data loss bugs, or broken builds.
* **[P1 - HIGH]**: Resource leaks (unclosed PTYs, leaked goroutines, unclosed file descriptors), unhandled error cases causing runtime panics, incorrect business logic, or quota-blocking failures.
* **[P2 - MEDIUM]**: Code smells, inefficient algorithms, missing input validation, lack of timeouts on network/IPC calls, or incomplete unit test coverage.
* **[P3 - LOW]**: Stylistic inconsistencies, non-idiomatic naming conventions, redundant type assertions, or missing documentation comments.
* **[P4 - INFO / SUGGESTION]**: Modernization opportunities, micro-optimizations, or architectural enhancements for future scalability.

## 2. Evidence-Based Reporting Standard

For every finding reported:
1. Provide the exact file path and line numbers using **workspace-relative links** (e.g. `[manager.go:205](./apps/agyswarm/internal/engine/manager.go#L205)`).
2. Quote the offending code snippet.
3. Explain the failure mechanism (e.g. why a deadlock, leak, or exploit occurs).
4. Provide a concrete, ready-to-apply code fix or refactoring patch.

## 3. Pure Markdown Deliverable Standard

* All code review summaries and audit reports must be saved directly into the workspace root or `./doc/` as clean GitHub Flavored Markdown (`.md`).
* Strip ANSI escape sequences from logs and command outputs.
* Provide dual link references for VS Code and Windows local paths.
