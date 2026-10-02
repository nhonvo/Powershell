---
name: whole-codebase-review
description: Comprehensive multi-pass audit of an entire repository or codebase covering architecture, security, concurrency, static analysis, and code quality. Use when asked to review the whole codebase, conduct a repo-wide audit, or assess software quality across all modules.
---

# Whole Codebase Review & Audit Protocol

This skill guides the agent through an exhaustive, multi-pass review of the entire workspace codebase.

## 6-Phase Review Procedure

### Phase 1: Workspace Topology & Module Discovery
1. Map all projects, micro-applications, shared libraries, and configuration files.
2. Identify language stacks:
   - Go (`go.mod`, package structure, CLI binaries)
   - PowerShell (`.ps1`, `.psm1`, profile configurations)
   - C# / .NET (`.csproj`, `.sln`)
   - Shell & Docker (`Dockerfile`, scripts, makefiles)
3. Check dependencies and module versions.

### Phase 2: Compiler & Static Linter Verification
1. Run master build checks (`make build` or individual module builds).
2. Execute existing unit test suites (`make test` or `go test ./...`).
3. Run static analyzers (`go vet`, `psscriptanalyzer`, `dotnet build /warnaserror` if applicable).
4. Record all compilation warnings, deprecation notices, and test failures.

### Phase 3: Concurrency, Deadlock & Resource Leak Audit
Inspect all concurrent components:
1. **Goroutines**: Are all background goroutines bounded by contexts or cancel channels?
2. **Mutex Usage**: Check for re-entrancy bugs (e.g. calling an RLock-acquiring method while holding Lock).
3. **PTY & OS Descriptors**: Verify that child processes, PTY masters (`ptmx`), and file handles are explicitly closed on termination and error branches.
4. **Channel Operations**: Check for potential blocking channel reads/writes with no consumers.

### Phase 4: Security & Credentials Audit
1. Search for hardcoded secrets, API keys, tokens, or private credentials.
2. Inspect external process execution (`exec.Command`, `Start-Process`) for unsanitized command injection.
3. Verify file permissions and path traversal vulnerabilities (e.g. `filepath.Clean`, `os.UserHomeDir`).
4. Audit environment isolation across accounts (e.g. `GEMINI_CLI_HOME` segregation).

### Phase 5: Code Quality & Idiomatic Standards
1. Error handling: Are errors checked immediately and wrapped with context (`fmt.Errorf("...: %w", err)`)?
2. Magic numbers & hardcoded constants: Are configuration values and timeouts extracted into constants or config files?
3. Code duplication: Are shared data models and utilities reused properly across micro-apps?

### Phase 6: Synthesis & Deliverable Generation
1. Synthesize all findings into a structured, executive-grade Markdown report:
   `./FULL_CODEBASE_AUDIT_REPORT.md` (or `./doc/audit/<timestamp>_full_audit.md`).
2. Categorize all issues using standard severity ratings (`[P0 - CRITICAL]`, `[P1 - HIGH]`, `[P2 - MEDIUM]`, `[P3 - LOW]`).
3. Provide clickable workspace-relative links (`[filename.go:line](./path/filename.go#L10)`) for every finding.
4. Provide concrete diffs or replacement code blocks for fixes.
