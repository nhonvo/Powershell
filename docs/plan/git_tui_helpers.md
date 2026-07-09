# Specification: Interactive Git TUI Helpers

This document details the specifications for the Git branch selector (`co`) and the Conventional Commit generator (`gcmt`) helper tools.

---

## 1. Interactive Git Checkout (`co`)

### A. Concept
Bypass manual typing of branch names by displaying a searchable local branch menu.

### B. Interactive Flow (Human Mode)
1.  User runs `co` without arguments.
2.  Script executes `git branch` and parses local branches.
3.  Calculates the current active branch (prefixed with `*`).
4.  Renders the list using `TerminalMenu.ps1`:
    ```text
    Select Git Branch to Checkout
    =============================
      * main (current)
        feat/terminal-ide
        fix/quota-limits
        dev
    ```
5.  If user selects a branch (e.g. `feat/terminal-ide`), the helper runs `git checkout feat/terminal-ide`.
6.  If the selection is cancelled, it restores the prompt.

### C. Token-Saving Flow (AI Agent Mode)
1.  If `$Global:AiMode -eq $true`:
    - Skip the menu and print the list of branches as flat text:
      ```text
      main
      feat/terminal-ide
      fix/quota-limits
      dev
      ```
    - Bypasses blocking prompt so the AI can select one by running `co <branch-name>` directly.

---

## 2. Conventional Commit Generator (`gcmt`)

### A. Concept
Guides developers to write standardised commit logs conforming to the Conventional Commits specification.

### B. Wizard Structure (Human Mode)
When run as `gcmt` without arguments, it launches a step-by-step picker:

```mermaid
flowchart TD
    Start[Run gcmt] --> SelectType[1. Select Type\nfeat, fix, docs, refactor, chore]
    SelectType --> InputScope[2. Input Scope\noptional e.g. 'core', 'nav']
    InputScope --> InputDesc[3. Input Description\nshort commit summary]
    InputDesc --> VerifyCommit{Review Commit Message\ngit commit -m 'type(scope): desc'}
    VerifyCommit -->|Approve| Commit[Run: git commit -m ...]
    VerifyCommit -->|Cancel| Exit[Abort Commit]
```

### C. Execution Layouts
*   **Step 1: Select Type**
    ```text
      Select Commit Type:
      > [1] feat     (New feature)
        [2] fix      (Bug fix)
        [3] docs     (Documentation changes)
        [4] refactor (Code restructuring without behavior changes)
        [5] chore    (Maintenance/dependencies)
    ```
*   **Step 2: Input Scope**
    ```text
      Enter commit scope (optional, press Enter to skip): core
    ```
*   **Step 3: Description**
    ```text
      Enter commit description: add token-saving dual mode profile
    ```
*   **Confirm:**
    ```text
      Generated commit: feat(core): add token-saving dual mode profile
      Confirm commit? (Y/N): y
    ```

### D. Staged Files & AI Assist (v2.1)
To ensure commits are context-aware and quick to write:
1.  **Stage Verification:** Before running the wizard, the helper executes `git diff --cached --name-only`. If this returns empty, it displays an alert: `No changes staged for commit. Please run git add before committing.` and exits.
2.  **AI-Generated Commit Suggestion:** On the commit description prompt, the user can press `[Tab]` to query the local Ollama backend. The helper feeds the output of `git diff --cached` with a system instruction to generate a one-line concise conventional description, which pre-populates the input field.

### E. Token-Saving Flow (AI Agent Mode)
*   If `$Global:AiMode -eq $true` and `gcmt` is called without parameters, it prints a list of supported types and syntax help:
    ```text
    Usage: gcmt <message>
    Supported Types: feat, fix, docs, style, refactor, test, chore
    ```
    This prevents the AI from getting stuck in interactive question prompts.

---

## 3. Tasks
- [x] Implement git branch checkout selector `co` with `TerminalMenu` in Human Mode.
- [x] Implement flat branch list fallback for `co` in AI Agent Mode.
- [x] Implement `gcmt` wizard with commit type selection, scope input, and description input.
- [x] Implement non-interactive helper messages for `gcmt` in AI Agent Mode.
- [x] Verify branch switches and Conventional Commit generation.
- [x] Implement stage validation check in `gcmt` to block empty commits.
- [x] Implement `[Tab]` handler to invoke local Ollama backend for AI commit message generation based on cached diffs (implemented via `ai` option in interactive picker).
