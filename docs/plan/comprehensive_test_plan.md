# Comprehensive Testing Plan & Verification Matrix

This document outlines the systematic verification strategy for the Enhanced PowerShell Profile. It covers both legacy (old) commands and newly introduced tools, detailing their automated test availability, target scenarios, and manual validation steps.

---

## 1. Testing Strategy

### A. Automated Testing (CI/CD & Local Pester)
- **Framework:** Pester 3.4.0 (Windows PowerShell 5.1 native).
- **Execution Command:** `powershell -ExecutionPolicy Bypass -File .\Tests\run_tests.ps1`
- **Verification Focus:** AST syntax verification, mock interceptors for external CLI executables (git, docker, dotnet, aws), and output string assertions.

### B. Manual Verification (Interactive & AI Mode Simulation)
- **Human Mode:** Interactive TUI menu loops, keyboard listeners, in-place rendering redraws, and color formats.
- **AI Mode Simulation:** Executes functions under `$env:AI_MODE = 'true'` to ensure zero interactive blocks, silent boots, and plain text formatted output.

---

## 2. Test Verification Matrix (Old vs. New Features)

| Feature / Command | Status | Automated Test | Test File & Context | Verification Goal / Manual Scenario |
| :--- | :--- | :--- | :--- | :--- |
| **1. Navigation (`..` / `...`)** | Old | **Yes** | `Profile-All.Tests.ps1` / *Navigation* | Verifies directory context goes up one level (`..`) or two levels (`...`). |
| **2. Disk Space (`df`)** | Old | **Yes** | `Profile-All.Tests.ps1` / *System Helpers* | Prints formatted table of local drive space percentages. |
| **3. Public IP (`myip`)** | Old | **Yes** | `Profile-All.Tests.ps1` / *System Helpers* | Resolves external IPv4 address. |
| **4. Process Killer (`killname`)** | Old | **Yes** | `Profile-All.Tests.ps1` / *System Helpers* | Closes target processes by friendly name. |
| **5. SSH Connection Info (`sshinfo`)** | Old | **Yes** | `Profile-All.Tests.ps1` / *System Helpers* | Outputs active port 22 connection list and quick guide. |
| **6. Clean Build (`clean-proj`)** | Old | **Yes** | `Profile-All.Tests.ps1` / *DotNet Cmdlets* | Recursively deletes `bin` and `obj` subfolders. |
| **7. DotNet Build (`build-proj`)** | Old | **Yes** | `Profile-All.Tests.ps1` / *DotNet Cmdlets* | Triggers native compiler toolchain. |
| **8. Git Status (`gs`)** | Old | **Yes** | `Profile-All.Tests.ps1` / *Git Cmdlets* | Retrieves current working branch file summaries. |
| **9. Undo Commit (`gundo`)** | Old | **Yes** | `Profile-All.Tests.ps1` / *Git Cmdlets* | Resets head to previous commit keeping local modifications. |
| **10. Docker List (`dps`)** | Old | **Yes** | `Profile-All.Tests.ps1` / *Docker Helpers* | Queries active local container tables. |
| **11. AWS S3 List (`s3ls`)** | Old | **Yes** | `Profile-All.Tests.ps1` / *AWS Commands* | List bucket metadata from active profile context. |
| **12. Multi-Account Manager (`acc`)** | Old / Enhanced | **Yes** | `Profile-All.Tests.ps1` / *Multi-Account Manager* | Verify directory switching, offline fallbacks, and usage logs. |
| **13. Theme Selection (`theme` / `mobile`)** | Old | **Yes** | `Profile-All.Tests.ps1` / *Theme Switcher* | Interactively triggers Oh My Posh style selectors. |
| **14. SSH Auth Key Receiver** | Old | **Yes** | `Profile-All.Tests.ps1` / *Mobile SSH Key Authorizer* | Sets up local TCP listener socket for key transfers. |
| **15. Secrets Vault (`sec`)** | New | **Yes** | `New-Features.Tests.ps1` / *AgySecretVault* | Encrypts/decrypts API tokens using DPAPI serialization. |
| **16. Port Inspector (`killport`)** | New | **Yes** | `New-Features.Tests.ps1` / *KillPort* | Identifies and terminates processes by listening port numbers. |
| **17. Project Scaffolder (`new-project`)** | New | **No** | *Manual verification only* | Creates target directory layout matching templates. |
| **18. Branch Selector (`co`)** | New | **No** | *Manual verification only* | Interactively checks out Git branch. |
| **19. Commit Wizard (`gcmt`)** | New | **No** | *Manual verification only* | Builds conventional commits using local Ollama. |
| **20. AI Command Explainer (`ask-ai` / `??`)** | New | **No** | *Manual verification only* | Interactively resolves last console error. |
| **21. Docker TUI (`dkcl`)** | New | **No** | *Manual verification only* | Manages Compose groupings and container states. |
| **22. Resource Utilization (`sysmon`)** | New | **No** | *Manual verification only* | Live bars (Human) / Raw percentages (AI) metrics. |
| **23. SQLite Inspector (`db-tui`)** | New | **No** | *Manual verification only* | TUI previewing table rows and column constraints. |
| **24. Multi-Log Streamer (`logstream`)** | New | **No** | *Manual verification only* | Keyword colored regex logs tailer. |

---

## 3. Detailed Test Scenarios & Step-by-Step Manual Runs

### Scenario 1: Shell Profile Initialization
*   **Run Context:** Profile boot checks.
*   **Human Mode Verification:**
    1. Open a clean PowerShell window.
    2. Confirm standard greeting, active account name, disk spaces, and phone control guides print successfully.
*   **AI Mode Verification:**
    1. Run `$env:AI_MODE = 'true'; powershell`
    2. Confirm the shell boots instantly in less than 200ms with zero console output.

### Scenario 2: Secrets Vault Management (`sec`)
*   **Run Context:** Encrypted credential vaults.
*   **Human Mode Verification:**
    1. Run `sec set github_token gho_test123` -> Verify "saved and encrypted successfully" prints.
    2. Run `sec list` -> Verify `github_token` is listed in key catalog.
    3. Run `sec get github_token` -> Verify it prints `gho_test123`.
    4. Run `sec rm github_token` -> Verify deletion message is printed.

### Scenario 3: Project Scaffold Bootstrapping (`new-project`)
*   **Run Context:** Template generation.
*   **Human Mode Verification:**
    1. Run `new-project` -> Interactive picker loads.
    2. Select template `react-vite`, enter name `temp-scaffold`, and select port `3000`.
    3. Verify that directory `temp-scaffold` is generated on disk and registered inside `Projects.ps1`.
    4. Clean up: `[Projects]::UnregisterProject("temp-scaffold")`; remove directory.
*   **AI Mode Verification:**
    1. Run `new-project -Template react-vite -Name temp-scaffold-ai -Port 3001`
    2. Verify it exits immediately with no interactive prompt wait states.

### Scenario 4: Conventional Commit AI pre-fills (`gcmt`)
*   **Run Context:** Commit descriptions.
*   **Human Mode Verification:**
    1. Initialize dummy git repo, stage a change, and run `gcmt`.
    2. Choose commit type `feat`, input scope `testing`, select option `ai` for description.
    3. Verify Ollama compiles the diff and returns conventional message summary.

### Scenario 5: Diagnostics & Streamers (`sysmon` / `dkcl` / `db-tui` / `logstream`)
*   **sysmon:**
    - Human: Run `sysmon` -> Verify live terminal refresh rates.
    - AI: Run `sysmon` -> Verify return value is tab-separated percentages (`CPU%, RAM%, Disk%`).
*   **dkcl:**
    - Human: Run `dkcl` -> TUI dashboard renders Compose container project trees.
    - AI: Run `dkcl` -> Returns simple `docker ps -a` table format.
*   **db-tui:**
    - Run `db-tui <path-to-sqlite-db>` -> Verify it lists metadata tables and outputs top data rows.
*   **logstream:**
    - Run `logstream app.log` -> Verify color rules (errors in Red, warnings in Yellow).

---

## 4. Regression Prevention Checklist
- Run AST parser syntax verification: `powershell -File .\Tests\run_tests.ps1`
- Verify that no emojis are in the helper class files (prevents encoding compiler failures in PS 5.1).
- Confirm splatting parameters are handled as direct array values instead of `@` inside class static method scopes.
- Confirm loop variables never overwrite global variables (e.g. rename `$pid` to `$owningPid` to avoid collision with `$PID`).
