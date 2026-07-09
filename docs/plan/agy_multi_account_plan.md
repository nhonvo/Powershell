# Antigravity Multi-Account (Agy) Integration & Quota Plan

This document outlines the plan to improve the Antigravity Multi-Account (`agy`/`multigravity`) management flow. It standardizes account/session switching, implements a local request-history tracker to generate real quota metrics, establishes the exact visual Models & Quota layout, and refines the CLI user experience.

---

## 1. Refined Multi-Account Switching Flow

To make switching accounts and running sandboxed sessions user-friendly, we will replace the complex `agy-account` wrappers with a unified, short command-line tool `acc` (alias for `Invoke-AccountSession`).

### Command Usage Design:

| Command | Action | Description |
| :--- | :--- | :--- |
| `acc` | TUI Switcher | Opens the interactive `TerminalMenu` listing all accounts and their statuses. Selecting one switches to it. |
| `acc <name>` | Direct Switch | Immediately sets `<name>` as the active account (updates environment variables and credential tokens). |
| `acc -temp <name> { <scriptblock> }` | Sandboxed Command | Temporarily switches to `<name>`, executes the code block, and restores the original account on completion (with `try/finally` protection). |
| `acc -status` / `acc -s` | Quota & Status | Prints the active account's current weekly and 5-hour quota percentages and counts. |

---

## 2. Models & Quota Layout Design

We will fix the quota display to use the following exact layout format. It will dynamically calculate the progress bars and refresh timestamps for the chosen account based on local request logs (instead of displaying the same hardcoded placeholders for every account).

```text
└ Models & Quota

  Account: fptvttnhon2026@gmail.com

GEMINI MODELS
  Models within this group: Gemini Flash, Gemini Pro

  Weekly Limit
    [████████████████████████████████████████░░░░░░░░░░] 80.24%
    80% remaining · Refreshes in 142h 20m

  Five Hour Limit
    [███████████████████████████████████████████░░░░░░░] 85.26%
    85% remaining · Refreshes in 3h 51m


CLAUDE AND GPT MODELS
  Models within this group: Claude Opus, Claude Sonnet, GPT-OSS

  Weekly Limit
    [██████████████████████████████████████████████████] 100.00%
    Quota available

  Five Hour Limit
    [██████████████████████████████████████████████████] 100.00%
    Quota available
```

---

## 3. Local Request-History Tracker (Real Metrics Per Account)

Since the Gemini API does not provide an endpoint to fetch remaining quota directly, we will calculate the metrics by tracking local request histories.

We will update each account's `account_metadata.json` to include a rolling array of request timestamps:
```json
{
    "LastUsed": "2026-07-07T00:20:00+07:00",
    "UsageCount": 11,
    "QuotaStatus": "OK",
    "RequestHistory": [
        "2026-07-06T10:15:30.000Z",
        "2026-07-06T12:45:00.000Z",
        "2026-07-07T00:20:00.000Z"
    ]
}
```

#### Dynamic Quota Calculation Logic:
Every time an AI helper or `multigravity` CLI runs, we append `(Get-Date).ToUniversalTime().ToString('o')` to `RequestHistory`. When rendering the table:
1.  **Prune Old Data:** Filter out timestamps older than 7 days from the history array.
2.  **5-Hour Quota:** Count requests where timestamp is within the last 5 hours.
    $$\text{Remaining 5H Quota} = \max\left(0, 100 - \left(\frac{\text{Requests in last 5 hours}}{\text{5H Limit}}\right) \times 100\right)$$
3.  **Weekly Quota:** Count requests in the last 7 days.
    $$\text{Remaining Weekly Quota} = \max\left(0, 100 - \left(\frac{\text{Requests in last 7 days}}{\text{Weekly Limit}}\right) \times 100\right)$$
4.  **Limits Config:** Define typical standard tier limits (e.g., `5H Limit = 50 requests`, `Weekly Limit = 1000 requests`).

This ensures that the Account Usage Summary table shows **real, unique, and dynamic** usage metrics for each account.

---

## 4. Weekly Usage Metrics

We will extend the TUI details screen to show a weekly distribution report.

### Visual Representation of Weekly Metrics:
When viewing the account details or running `agy-account show`, we will draw a daily request chart for the past week:
```text
  Weekly Request Distribution (Last 7 Days)
  ===========================================
  Mon [████░░░░░░] 4 requests
  Tue [████████░░] 8 requests
  Wed [██░░░░░░░░] 2 requests
  Thu [░░░░░░░░░░] 0 requests
  Fri [░░░░░░░░░░] 0 requests
  Sat [░░░░░░░░░░] 0 requests
  Sun [██████████] 10 requests
  -------------------------------------------
  Total Weekly Requests: 24 / 1000 limit
```

This tracking data is parsed directly from the updated `RequestHistory` array.

---

## 5. Advanced Multi-Account Enhancements (v2.1)

### A. Workspace-Contextual Auto-Switching
To prevent executing scripts under the wrong developer credentials, the account switcher will bind to workspace navigation events:
*   **Projects.ps1 Config Schema:** Add an optional `"AssociatedAccount"` entry to the workspaces configuration array:
    ```powershell
    $Global:ProfileWorkspaces = @(
        @{ Name = "Powershell"; Path = "C:\Users\TruongNhon\Documents\Powershell"; AssociatedAccount = "fptvttnhon2026@gmail.com" },
        @{ Name = "finance-dashboard"; Path = "C:\Users\TruongNhon\Documents\finance-dashboard"; AssociatedAccount = "personal@gmail.com" }
    )
    ```
*   **Navigation Trigger Hook:** Inside the profile `prompt` function or customized `cd` wrapper:
    ```powershell
    $matchedProject = $Global:ProfileWorkspaces | Where-Object { $pwd.Path -like "$($_.Path)*" }
    if ($null -ne $matchedProject -and $matchedProject.AssociatedAccount -ne $Global:ActiveAccount) {
        Write-Host "[Auto-Switch] Changing credentials to: $($matchedProject.AssociatedAccount)" -ForegroundColor Cyan
        Invoke-AccountSession -Name $matchedProject.AssociatedAccount -Silent
    }
    ```

### B. Offline Mode Fallback
To ensure that slow networking does not block terminal startup:
*   **Diagnostics Check:** We execute a quick double-check:
    ```powershell
    $hasNetwork = [System.Net.NetworkInformation.NetworkInterface]::GetIsNetworkAvailable()
    $online = $hasNetwork -and (Test-Connection -ComputerName 8.8.8.8 -Count 1 -TimeoutMilliSeconds 200 -ErrorAction SilentlyContinue)
    ```
*   If `$online` is `$false`, the profile skips calling external endpoints, reads credential variables directly from `$env:GEMINI_HOME/active_account.txt` and cached values, and prefixes the shell prompt with a red `[Offline]` tag.

---

## 6. Verification Plan

### Manual Verification
1.  **Switching Flow Test:** Run `acc` without arguments, select an account, and verify it switches. Test `acc my-account` to switch directly.
2.  **Sandboxed Execution Test:** Run `acc -temp other-account { agy test }` and verify the CLI is called under `other-account`'s credentials, and Window session restores `default` upon completion.
3.  **Quota Verification:** Run a few mock requests under an account, check `acc -status`, and verify that the usage count increases and the remaining quota percentage decreases dynamically *only* for that account.
4.  **Weekly Graph Verification:** Verify that the weekly bar chart displays the daily request counts correctly matching the timestamps in `account_metadata.json`.

---

## 7. Tasks
- [x] Replace complex `agy-account` wrappers with a unified `acc` alias/command.
- [x] Implement interactive TUI switch menu in `acc` (when run without arguments).
- [x] Implement direct account switching: `acc <name>`.
- [x] Implement sandboxed command execution: `acc -temp <name> { <scriptblock> }` using `try/finally` protection.
- [x] Implement quota status command: `acc -status` / `acc -s`.
- [x] Update `account_metadata.json` structure to include a rolling array of `RequestHistory` timestamps.
- [x] Implement dynamic quota calculations for 5-hour limit and weekly limit based on `RequestHistory`.
- [x] Prune `RequestHistory` timestamps older than 7 days.
- [x] Implement daily request count bar chart for the past week in the TUI details screen.
- [x] Implement workspace-contextual auto-switching by mapping projects to accounts in `Projects.ps1`.
- [x] Implement network diagnostics check and offline fallback loading mode.
