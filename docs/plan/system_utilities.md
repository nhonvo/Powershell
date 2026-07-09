# Specification: Port Inspector, History Searcher, & System Monitor

This document details the specifications for three system utility commands: `killport`, `Ctrl+R` (Fuzzy History Searcher), and `sysmon` (System Resource Monitor).

---

## 1. Port Inspector (`killport`)

### A. Concept
A command utility to identify processes blocking specific network ports and terminate them.

### B. Interactive Flow (Human Mode)
1.  User runs `killport 8080`.
2.  Script calls `Get-NetTCPConnection -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue`.
3.  Resolves process ID: `$pid = $connection.OwningProcess`.
4.  Fetches process name: `$proc = Get-Process -Id $pid`.
5.  Displays a stylized warning:
    ```text
    ⚠️ Port 8080 is blocked by: node.exe (PID: 14210)
    Do you want to terminate this process? (Y/N): 
    ```
6.  If `Y` is typed, calls `Stop-Process -Id 14210 -Force` and prints success.

### C. Token-Saving Flow (AI Agent Mode)
*   Bypasses the confirmation prompt.
*   Directly outputs key-value details so the agent can quickly inspect the block:
    ```text
    PORT:8080;PID:14210;PROCESS:node.exe;STATE:Listen
    ```

---

## 2. Fuzzy History Searcher (Fuzzy `Ctrl+R`)

### A. Concept
Binds `Ctrl+R` to a dynamic `TerminalMenu` search bar populated with past commands from PowerShell history.

### B. How it works
1.  Overwrites the PSReadLine key handler:
    ```powershell
    Set-PSReadLineKeyHandler -Key 'Ctrl+r' -ScriptBlock {
        [FuzzyHistorySearcher]::Open()
    }
    ```
2.  `[FuzzyHistorySearcher]::Open()` reads commands:
    *   Queries active session history: `Get-History`.
    *   Reads persistent PSReadLine history file (resolved via `(Get-PSReadLineOption).HistorySavePath`).
    *   Groups and selects unique commands, keeping the most recent.
3.  Loads command strings into `TerminalMenu.ps1` for real-time fuzzy filtering.
4.  Selecting a command inserts it directly into the active console prompt line.

### C. AI Agent Mode
*   AI agents do not interact with PSReadLine key handlers (they submit commands directly to the process stdin). Therefore, this handler is safely disabled when `$Global:AiMode -eq $true`.

---

## 3. Live System Resource Monitor (`sysmon`)

### A. Concept
A lightweight, live-updating resource dashboard.

### B. UI Design (Human Mode)
```text
  System Resource Monitor  [Refresh: 2s]
  =============================================
  CPU Load:  [████████░░░░░░░░░░░░░░░░] 40.2%
  RAM Load:  [██████████████░░░░░░░░░░] 60.5% (9.8 GB / 16 GB)
  Disk (C:): [██████████████████████░░] 88.0%
  
  Top Memory Consumers:
  1. msedge.exe      - 1.2 GB
  2. code.exe        - 850 MB
  3. pwsh.exe        - 150 MB
```

### C. Token-Saving Flow (AI Agent Mode)
When the AI agent runs `sysmon`, it wants to quickly inspect system resources without rendering blocking loops.
*   Runs exactly **once** (does not loop or refresh).
*   Outputs data in a single clean, structured block:
    ```text
    CPU: 40.20%
    RAM: 60.50% (9.80GB/16.00GB)
    Disk_C: 88.00%
    Top_Proc: msedge.exe(1.2GB), code.exe(850MB), pwsh.exe(150MB)
    ```
*   **Token Savings:** Reduces output from 600 characters (containing block shapes) to 120 characters of plain text, saving **75% of context window tokens**.

---

## 4. Tasks
- [x] Implement `killport` to resolve TCP listener process names and display termination prompts.
- [x] Implement AI Mode fallback for `killport` outputting raw key-value pairs.
- [x] Implement fuzzy history searcher by binding `Ctrl+R` to a `TerminalMenu` with history items.
- [x] Disable fuzzy history searcher when in AI Mode.
- [x] Build the live CPU/RAM/Disk utilization gauge UI in `sysmon` (Human Mode).
- [x] Implement snapshot plain text output for `sysmon` in AI Agent Mode.
- [x] Verify port termination, fuzzy search selection, and resource stats.
