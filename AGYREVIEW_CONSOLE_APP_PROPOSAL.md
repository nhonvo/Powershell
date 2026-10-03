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

## 4. Architectural Implementation Blueprint

### 4.1 Module Structure (`apps/agyreview`)

```text
apps/agyreview/
├── go.mod                     # Go 1.22.2, golang.org/x/term
├── go.sum
├── main.go                    # CLI router: cockpit, run, pr, watch, daemon, config
├── internal/
│   ├── model/
│   │   ├── target.go          # TargetRepo, PRInfo, ReviewScope, TriggerMode
│   │   ├── finding.go         # Finding, Severity (P0-P4), PatchDiff
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
│   │   └── analyzer.go        # Fast AST & regex pre-filter (secrets, syntax errors)
│   ├── markdown/
│   │   └── report.go          # Pure Markdown report generator with ANSI stripping
│   └── view/
│       ├── cockpit.go         # Alternate screen buffer TUI dashboard
│       └── prompt.go          # Interactive CLI prompt for suggested reviews
```

---

## 5. Implementation Roadmap & Milestones

| Phase | Milestone | Deliverable |
| :---: | :--- | :--- |
| **Phase 1** | Target Resolvers | Implement `local.go`, `github.go`, and `pr.go` to normalize any path, repo URL, or PR link into a ready-to-audit workspace. |
| **Phase 2** | Review Pipeline Engine | Connect `engine/runner.go` to the installed `code-review` plugin (`whole-codebase-review` and `security-audit`) and compile pure Markdown outputs. |
| **Phase 3** | Git Poller & Watcher | Implement background daemon polling git commit histories and detecting new branches with auto/suggest triggers. |
| **Phase 4** | Interactive Cockpit TUI | Build full-screen terminal cockpit with repo lists, live streaming reviews, and report viewers. |
| **Phase 5** | Makefile Integration | Add `agyreview` to root `Makefile` (`APPS`, `make review`), run unit tests, and cross-compile Windows `.exe`. |

---

## 6. Document Reference Links

* **VS Code Clickable (Recommended):**
  * Proposal Blueprint: [AGYREVIEW_CONSOLE_APP_PROPOSAL.md](./AGYREVIEW_CONSOLE_APP_PROPOSAL.md)
  * Code Review Plugin: [.agents/plugins/code-review/README.md](./.agents/plugins/code-review/README.md)
  * Full Codebase Audit: [BIG_FULL_CODEBASE_AUDIT_REPORT.md](./BIG_FULL_CODEBASE_AUDIT_REPORT.md)
* **Windows UNC Path:**
  * `\\wsl.localhost\Ubuntu\mnt\c\Users\TruongNhon\Documents\Powershell\AGYREVIEW_CONSOLE_APP_PROPOSAL.md`
  * `C:\Users\TruongNhon\Documents\Powershell\AGYREVIEW_CONSOLE_APP_PROPOSAL.md`
