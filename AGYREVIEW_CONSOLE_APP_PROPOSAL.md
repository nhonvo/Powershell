# 🔍 AGYREVIEW: Autonomous Multi-Repo Code Reviewer & Git Watcher Sentinel

> **Proposal & Architecture Blueprint**: Next-Gen Console Application in the Antigravity Developer Suite  
> **Application Name**: `agyreview`  
> **Status**: Design & Architectural Blueprint  
> **Date**: 2026-10-03  
> **Stack**: Go 1.22 (TUI + Daemon + Git Engine + Antigravity Plugin Integration)  

---

## 1. Executive Vision & Objectives

`agyreview` is designed as a dedicated, cross-repository code review and audit sentinel for the Antigravity Developer Suite. It extends the newly installed `code-review` plugin into an automated, multi-source audit orchestrator capable of reviewing:
1. **Local Workspaces** (e.g. `/home/user/projects/finance-dashboard`).
2. **Remote GitHub Repositories** (cloned into isolated shallow caches).
3. **Pull Request URLs** (fetching PR diffs, commits, and changed files).
4. **Active Git Branches & Changesets** (diffing `main...feature-branch`).

### Core Value Pillars
* **Universal Input Resolution**: Seamlessly accepts local paths, GitHub URLs, or PR links.
* **Autonomous Pipeline**: Drives the `code-review` plugin (`whole-codebase-review` and `security-audit` skills) headlessly or interactively.
* **Target-Embedded Deliverables**: Automatically writes clean, pure-Markdown reports into the reviewed repository (`<target>/doc/audit/<timestamp>_review.md`).
* **Daemon / Recurring Watcher**: Runs in the background, monitoring git branches and commits. When a new branch or commit is pushed, it can either **auto-audit** or **suggest review** to the operator.

```
                     ┌──────────────────────────────────────────────┐
                     │            User Inputs / Sources             │
                     │  • Local Path: /path/to/project              │
                     │  • GitHub Repo: github.com/owner/repo        │
                     │  • PR Link: github.com/owner/repo/pull/123   │
                     │  • Git Range: git diff origin/main...HEAD    │
                     └──────────────────────┬───────────────────────┘
                                            │
                                            ▼
                     ┌──────────────────────────────────────────────┐
                     │             AGYREVIEW Orchestrator           │
                     │  ┌────────────────────────────────────────┐  │
                     │  │ Target Resolver & Sandbox Workspace    │  │
                     │  ├────────────────────────────────────────┤  │
                     │  │ Git Watcher & Poller Daemon            │  │
                     │  ├────────────────────────────────────────┤  │
                     │  │ Heuristic Diff & AST Pre-Filter        │  │
                     │  ├────────────────────────────────────────┤  │
                     │  │ Antigravity Plugin Runner (agy --print)│  │
                     │  └────────────────────────────────────────┘  │
                     └──────────────────────┬───────────────────────┘
                                            │
                      ┌─────────────────────┴─────────────────────┐
                      ▼                                           ▼
         ┌───────────────────────────┐               ┌───────────────────────────┐
         │  Interactive TUI Cockpit  │               │  Target-Embedded Output   │
         │  • Live diff inspection   │               │  • <repo>/doc/audit/*.md  │
         │  • Severity filter (P0-P4)│               │  • Pure GFM Markdown      │
         │  • One-click approvals    │               │  • Clickable file links   │
         └───────────────────────────┘               └───────────────────────────┘
```

---

## 2. Core Feature Matrix

### 2.1 Universal Input Resolver

`agyreview` automatically classifies and resolves any user input:

| Input Pattern | Resolution Strategy | Example Command |
| :--- | :--- | :--- |
| **Local Path** | Verifies directory, checks git root, parses branch and dirty status. | `agyreview /home/user/finance-dashboard` |
| **GitHub Repo** | Shallow-clones (`--depth 1`) or updates existing repo in `~/.cache/agyreview/repos/<owner>_<repo>`. | `agyreview https://github.com/nhonvo/Powershell` |
| **GitHub PR Link** | Fetches PR metadata and diff via GitHub API or `gh pr diff`. Creates an ephemeral worktree diff. | `agyreview https://github.com/nhonvo/Powershell/pull/12` |
| **Branch / Commit Range** | Computes `git diff <base>...<head>` and limits review scope strictly to changed hunks. | `agyreview --diff origin/main...HEAD` |

---

### 2.2 Flexible Execution Modes

#### Mode A: Interactive TUI Cockpit (`agyreview`)
* Renders a multi-pane dashboard:
  - **Left Pane**: Monitored repositories and PR queue with status tags (`● IDLE`, `🔄 AUDITING`, `⚡ REVIEW NEEDED`, `✔ CLEAN`).
  - **Center Pane**: Live streaming review logs with real-time heuristic severity flags (`[P0 - CRITICAL]`, `[P1 - HIGH]`).
  - **Right Pane**: Finding summary with clickable links to offending files and suggested patches.
* Hotkeys:
  - `[Enter]`: Open full report in VS Code.
  - `[r]`: Trigger immediate re-review.
  - `[a]`: Add new local repo or GitHub URL to watch list.
  - `[p]`: Review specific PR link.
  - `[q]`: Exit.

#### Mode B: Headless CLI (`agyreview run <target>`)
* Scriptable one-shot execution suitable for CI/CD pipelines, pre-push git hooks, or quick terminal checks:
  ```bash
  # Review current local project
  agyreview run .

  # Review remote PR and exit with non-zero code if P0/P1 issues found
  agyreview run https://github.com/org/repo/pull/42 --strict-exit
  ```

#### Mode C: Watcher & Recurring Daemon (`agyreview watch` / `daemon`)
* Background service that polls monitored repositories on a configurable schedule (e.g. every 5 minutes):
  1. Checks `git fetch` for upstream branch changes or new commits.
  2. Detects newly created branches matching patterns (e.g. `feature/*`, `bugfix/*`).
  3. Evaluates change delta: if files changed > 0, triggers review policy.

---

### 2.3 Optional vs. Automatic Review Trigger Policy

Operators can configure trigger behaviors per repository in `~/.config/agyreview/config.json`:

```json
{
  "repositories": [
    {
      "name": "finance-dashboard",
      "path": "/home/truongnhon/projects/finance-dashboard",
      "watch": {
        "enabled": true,
        "pollInterval": "3m",
        "branches": ["main", "feature/*"],
        "triggerMode": "suggest",
        "notifyTelegram": true
      }
    },
    {
      "name": "Powershell",
      "path": "/mnt/c/Users/TruongNhon/Documents/Powershell",
      "watch": {
        "enabled": true,
        "pollInterval": "5m",
        "triggerMode": "auto",
        "autoFix": false
      }
    }
  ]
}
```

#### Trigger Modes:
* **`auto`**: Automatically executes the review immediately upon detecting a new commit or branch, writes the Markdown report to `<repo>/doc/audit/`, and optionally notifies via desktop or Telegram (`agybot`).
* **`suggest`**: Detects changes and emits an interactive prompt:
  ```text
  ⚡ [agyreview] New branch 'feature/bank-sync-v2' detected in finance-dashboard (3 new commits, +412 lines).
     Would you like to run code review now? [Y/n/details]: 
  ```
* **`manual`**: Only reviews when explicitly triggered by the user via CLI or TUI.

---

## 3. Pure Markdown Deliverable Architecture

Following the Antigravity markdown-only standard, all outputs generated by `agyreview` are pure GitHub-Flavored Markdown (`.md`):

### 3.1 Deliverable Storage Location
1. **Target-Embedded (Default)**:
   - `<target-repo>/doc/audit/REVIEW_<date>_<branch_or_pr>.md`
   - Keeps audit history version-controlled directly with the codebase.
2. **Central Reports Archive (Configurable)**:
   - `~/.local/share/agyreview/reports/<repo_slug>/<date>.md`

### 3.2 Report Structure
Each report follows the evidence-based reporting standard:
* **Executive Summary**: Total files audited, lines changed, severity distribution.
* **Critical Findings Table**:
  | Severity | File & Line | Summary | Proposed Action |
  | :--- | :--- | :--- | :--- |
  | `[P0]` | `[db.go:45](./internal/db/db.go#L45)` | SQL injection risk | Use parameterized query |
* **In-Depth Technical Analysis**: Quoting offending lines with full contextual explanations.
* **Verification Matrix**: Compiler checks (`go vet`, `make test`, `dotnet build`), static analysis results.
* **Dual Clickable Links**:
  - VS Code Clickable: `[REVIEW_20261003.md](./doc/audit/REVIEW_20261003.md)`
  - Windows UNC Path: `\\wsl.localhost\Ubuntu\...`

---

---

## 4. Standard Review Template, Barème Rubric & Result Schema (Deep Research)

To ensure consistency, objectivity, and enterprise-grade rigor, `agyreview` adopts an evidence-based evaluation barème and standardized result format synthesized from premier industry engineering standards:
* **Google Engineering Practices**: Small CLs, design intent, readability, tests, naming, and complexity limits.
* **ISO/IEC 25010 Quality Model**: Functional suitability, reliability, security, maintainability, and efficiency.
* **OWASP ASVS v4.0 & CWE/SANS Top 25**: Objective vulnerability scoring and risk prioritization.

### 4.1 The 100-Point Code Quality Barème (Evaluation Rubric)

Every repository or changeset starts with a baseline of **100 points**. Points are allocated across five core dimensions:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                   100-POINT CODE QUALITY EVALUATION BARÈME                  │
├──────────────────────────┬───────┬──────────────────────────────────────────┤
│ Category Dimension       │ Weight│ Focus Areas                              │
├──────────────────────────┼───────┼──────────────────────────────────────────┤
│ 1. Security & Isolation  │ 25%   │ Zero secrets, command injection, path    │
│                          │       │ traversal, multi-account credential leak │
├──────────────────────────┼───────┼──────────────────────────────────────────┤
│ 2. Architecture & Design │ 20%   │ Modularity, clean package boundaries,    │
│                          │       │ no circular dependencies, single resp.   │
├──────────────────────────┼───────┼──────────────────────────────────────────┤
│ 3. Concurrency & Resource│ 20%   │ Goroutine leaks, mutex re-entrancy, PTY  │
│    Reliability           │       │ file descriptors, clean OS termination   │
├──────────────────────────┼───────┼──────────────────────────────────────────┤
│ 4. Maintainability & Code│ 20%   │ Idiomatic Go/PowerShell, error wrapping, │
│    Quality               │       │ zero dead code, cognitive complexity <15 │
├──────────────────────────┼───────┼──────────────────────────────────────────┤
│ 5. Test Coverage & Docs  │ 15%   │ Comprehensive unit suites, reproducible  │
│                          │       │ builds, clear comments, up-to-date docs  │
└──────────────────────────┴───────┴──────────────────────────────────────────┘
```

#### Deductions by Severity Level:
* **`[P0 - CRITICAL]`**: **-25 points each** (Immediate Quality Gate failure; cannot merge).
* **`[P1 - HIGH]`**: **-10 points each** (Significant defect or resource leak).
* **`[P2 - MEDIUM]`**: **-3 points each** (Code smell, missing validation, or incomplete tests).
* **`[P3 - LOW]`**: **-1 point each** (Stylistic or naming inconsistency).
* **`[P4 - INFO]`**: **0 point deduction** (Architectural enhancement or modernization opportunity).

#### Quality Gate Classification:
* **Grade A (90–100 pts)**: `PASS / MERGE READY` — Exceptional code health. Production safe.
* **Grade B (75–89 pts)**: `PASS / MINOR RECOMMENDATIONS` — Merge acceptable with non-blocking suggestions.
* **Grade C (60–74 pts)**: `CHANGES REQUESTED` — Blocked on P2 remediations.
* **Grade F (<60 pts or any P0)**: `REJECTED / CRITICAL BLOCKERS` — Build or security gate failure.

---

### 4.2 Standardized Machine- & Human-Readable Result Schema

Every audit output begins with a YAML metadata header for automated CI/CD parsing, followed by GFM Markdown tables:

```markdown
---
schema_version: "1.0.0"
review_id: "rev-20261003-104800"
target_repository: "/home/truongnhon/projects/finance-dashboard"
commit_sha: "4b6ef83"
branch: "main"
quality_score: 88
quality_grade: "B"
verdict: "MERGE_ACCEPTABLE"
finding_counts:
  p0: 0
  p1: 0
  p2: 3
  p3: 1
  p4: 2
timestamp: "2026-10-03T10:48:00+07:00"
---

# Code Review & Quality Audit Dossier: finance-dashboard

## 1. Executive Quality Scorecard
* **Overall Score:** 88 / 100 (Grade B)
* **Quality Gate Verdict:** MERGE_ACCEPTABLE
...
```

---

## 5. Headless 3-Loop Review Engine & Multi-Agent Swarm Flow

To prevent superficial audits and ensure deep contextual accuracy, `agyreview` implements an automated **Headless 3-Loop Review Engine** and an optional **Multi-Agent Swarm Orchestrator**:

```mermaid
graph TD
    subgraph Headless 3-Loop Review Engine
        L1[Loop 1: Topological Reconnaissance<br/>AST, Linters, Static Analysis, Secrets] --> L2[Loop 2: Deep Context & Cross-Module Invariants<br/>Call Graphs, Concurrency, Mutex Re-entrancy, PTYs]
        L2 --> L3[Loop 3: Adversarial Validation & Consensus<br/>False-Positive Pruning, Patch Verification, Scoring]
    end
    
    subgraph Multi-Agent Swarm Flow (agyswarm Integration)
        L2 -.->|Dispatch Specialized Swarm| S1[Agent Alpha: Security & Secrets Sentry]
        L2 -.->|Dispatch Specialized Swarm| S2[Agent Beta: Concurrency & Leak Auditor]
        L2 -.->|Dispatch Specialized Swarm| S3[Agent Gamma: Architecture & Contracts]
        S1 & S2 & S3 --> S4[Agent Delta: Lead Arbiter & Synthesizer]
        S4 -.-> L3
    end
```

### 5.1 The 3-Loop Review Process
1. **Loop 1: Topological Reconnaissance & Threat Surface Mapping (Breadth)**:
   - Discovers repository layout, language stacks, entry points, and dependency graphs.
   - Executes static compilers and linters (`go vet`, `golangci-lint`, `dotnet build`, `psscriptanalyzer`).
   - Runs high-entropy regex scanners for credentials, API tokens, and private keys.
2. **Loop 2: Deep Context & Cross-Module Invariant Verification (Depth)**:
   - Traces execution paths and call graphs across packages and micro-apps.
   - Enforces concurrency invariants: goroutine lifecycle cancellation, channel closure, mutex re-entrancy, and PTY descriptor cleanup.
   - Audits error handling: verifies contextual error wrapping (`fmt.Errorf("...: %w", err)`) and edge case recovery.
3. **Loop 3: Adversarial Validation & Consensus Convergence (Consensus)**:
   - Adversarially re-checks findings to eliminate false positives.
   - Verifies that proposed remediation code patches do not introduce breaking regressions.
   - Computes the final 100-Point Barème score and generates the deliverable.

### 5.2 Multi-Agent Swarm Flow for Whole Projects
For large codebases, `agyreview` leverages `agyswarm` to spawn parallel child agent PTYs across accounts:
* **Agent Alpha (Security Sentry)**: Executes `security-audit` skill.
* **Agent Beta (Concurrency Auditor)**: Executes goroutine, mutex, and PTY lifecycle checks.
* **Agent Gamma (Architecture Arbiter)**: Evaluates modularity, clean code, and API contracts.
* **Agent Delta (Lead Arbiter)**: Aggregates findings on a shared Blackboard, deduplicates issues, and generates the final dossier.

---

## 6. Phase 6: Actionable Task Breakdown & Remediation Engine (`agyreview fix`)

Review reports must not be static dead-ends. Phase 6 introduces an automated bridge from **audit findings** to **code remediation**:

### 6.1 Task Breakdown Generator
* Parses findings into atomic, dependency-ordered work tasks.
* Automatically creates `<target-repo>/doc/audit/<timestamp>_REMEDIATION_PLAN.md`.
* Classifies each task into:
  - **Priority Queue**: `P0` (Blockers) ➔ `P1` (High) ➔ `P2` (Medium) ➔ `P3` (Low).
  - **T-Shirt Sizing**: S (1–2h), M (半日), L (1–2 days).
  - **Execution Path**: File, line range, and ready-to-apply diff.

### 6.2 Auto-Implementation Engine (`agyreview fix`)
Operators can trigger remediation directly from the CLI:
```bash
# Interactively review and apply patches one-by-one:
agyreview fix --interactive

# Automatically create an isolated git worktree, apply P0/P1 fixes, run tests, and commit:
agyreview fix --auto-p0-p1 --branch fix/audit-security-hardening
```

---

## 7. Phase 7: Product Roadmap & Strategic Evolution Skill (`product-roadmap-evolution`)

Phase 7 introduces the [`product-roadmap-evolution`](./.agents/plugins/code-review/skills/product-roadmap-evolution/SKILL.md) skill bundled inside the `code-review` plugin. It translates audit findings, technical debt, and architectural gaps into a forward-looking product roadmap.

### 7.1 The 3-Horizon Strategic Evolution Model
1. **Horizon 1 (Immediate / Sprint 1–2 / 0–30 Days)**:
   - Zero-defect hardening, resolving all P0/P1 blockers, secret rotation, and compiler warning liquidation.
2. **Horizon 2 (Mid-Term / Month 1–3 / 30–90 Days)**:
   - Modular decoupling, shared utility extraction, test rig expansion (>85% coverage), and memory leverage.
3. **Horizon 3 (Long-Term / Quarter 2–4 / 90–360 Days)**:
   - Autonomous multi-agent coordination, cross-platform ecosystem expansion, and self-healing CI/CD gates.

### 7.2 Technical Debt Retirement Tracking
Maps each roadmap milestone to its projected increase on the 100-Point Quality Barème:
* **Deliverable Generated**: `<target-repo>/PRODUCT_ROADMAP.md`.

---

## 8. Architectural Implementation Blueprint

### 8.1 Module Structure (`apps/agyreview`)

```text
apps/agyreview/
├── go.mod                     # Go 1.22.2, golang.org/x/term
├── go.sum
├── main.go                    # CLI router: cockpit, run, pr, watch, fix, roadmap
├── internal/
│   ├── model/
│   │   ├── target.go          # TargetRepo, PRInfo, ReviewScope, TriggerMode
│   │   ├── finding.go         # Finding, Severity (P0-P4), PatchDiff, BarèmeScore
│   │   ├── plan.go            # RemediationTask, ExecutionPlan, TaskPriority
│   │   └── config.go          # AppConfig, WatchConfig, NotificationConfig
│   ├── resolver/
│   │   ├── local.go           # Local directory path resolver & git state detector
│   │   ├── github.go          # GitHub clone/cache manager
│   │   └── pr.go              # GitHub PR parser & diff fetcher
│   ├── watcher/
│   │   ├── poller.go          # Background cron & git commit history poller
│   │   └── trigger.go         # Branch detection & "suggest review" prompt logic
│   ├── engine/
│   │   ├── runner.go          # Antigravity plugin driver (invokes code-review skills)
│   │   ├── loops.go           # Headless 3-Loop review orchestrator
│   │   ├── swarm.go           # agyswarm multi-agent dispatch coordinator
│   │   └── analyzer.go        # Fast AST & regex pre-filter (secrets, syntax errors)
│   ├── remediation/
│   │   ├── planner.go         # Task breakdown & remediation plan compiler
│   │   └── patcher.go         # Worktree creation, diff applicator & test runner
│   ├── roadmap/
│   │   └── generator.go       # 3-Horizon Product Roadmap & debt curve synthesizer
│   ├── markdown/
│   │   └── report.go          # Pure Markdown report generator with ANSI stripping
│   └── view/
│       ├── cockpit.go         # Alternate screen buffer TUI dashboard
│       └── prompt.go          # Interactive CLI prompt for suggested reviews
```

---

## 9. Comprehensive Implementation Roadmap & Milestones

| Phase | Milestone | Focus & Deliverables |
| :---: | :--- | :--- |
| **Phase 1** | Target Resolvers | Implement `local.go`, `github.go`, and `pr.go` to normalize any path, repo URL, or PR link into a ready-to-audit workspace. |
| **Phase 2** | Review Pipeline Engine | Connect `engine/runner.go` to `code-review` plugin (`whole-codebase-review` and `security-audit`). Integrate **100-Point Quality Barème**, **Headless 3-Loop Engine**, and **Multi-Agent Swarm Flow**. |
| **Phase 3** | Git Poller & Watcher | Background daemon polling commit histories, detecting new branches, and offering auto/suggest review triggers. |
| **Phase 4** | Interactive Cockpit TUI | Full-screen terminal cockpit with repo lists, live streaming reviews, findings inspector, and report viewers. |
| **Phase 5** | Makefile Integration | Add `agyreview` to root `Makefile` (`APPS`, `make review`), run unit tests, and cross-compile Windows `.exe`. |
| **Phase 6** | Task Breakdown & Remediation | Implement `remediation/planner.go` and `patcher.go` (`agyreview fix`) to break findings into actionable tickets and apply verified patches. |
| **Phase 7** | Product Roadmap Skill | Implement `roadmap/generator.go` and integrate `product-roadmap-evolution` skill to generate `<repo>/PRODUCT_ROADMAP.md`. |

---

## 10. Document Reference Links

* **VS Code Clickable (Recommended):**
  * Proposal Blueprint: [AGYREVIEW_CONSOLE_APP_PROPOSAL.md](./AGYREVIEW_CONSOLE_APP_PROPOSAL.md)
  * Code Review Plugin: [.agents/plugins/code-review/README.md](./.agents/plugins/code-review/README.md)
  * Product Roadmap Skill: [.agents/plugins/code-review/skills/product-roadmap-evolution/SKILL.md](./.agents/plugins/code-review/skills/product-roadmap-evolution/SKILL.md)
  * Whole Codebase Skill: [.agents/plugins/code-review/skills/whole-codebase-review/SKILL.md](./.agents/plugins/code-review/skills/whole-codebase-review/SKILL.md)
  * Security Audit Skill: [.agents/plugins/code-review/skills/security-audit/SKILL.md](./.agents/plugins/code-review/skills/security-audit/SKILL.md)
  * Full Codebase Audit: [BIG_FULL_CODEBASE_AUDIT_REPORT.md](./BIG_FULL_CODEBASE_AUDIT_REPORT.md)
* **Windows UNC Path:**
  * `\\wsl.localhost\Ubuntu\mnt\c\Users\TruongNhon\Documents\Powershell\AGYREVIEW_CONSOLE_APP_PROPOSAL.md`
  * `C:\Users\TruongNhon\Documents\Powershell\AGYREVIEW_CONSOLE_APP_PROPOSAL.md`

