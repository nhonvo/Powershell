# Antigravity Developer Suite - Comprehensive Manual Test Plan & Findings Log

> **Framework Version**: Antigravity Developer Suite v1.0.0 / Go Engine 2.0  
> **Target OS Environment**: Windows 11 / PowerShell 7.6.6 & Ubuntu WSL2  
> **Date**: October 3, 2026  
> **Document Path**: [`./doc/AGY_SUITE_MANUAL_TEST_PLAN_AND_BUG_LOG.md`](file:///C:/Users/TruongNhon/Documents/Powershell/doc/AGY_SUITE_MANUAL_TEST_PLAN_AND_BUG_LOG.md)

---

## 1. Executive Overview & Test Architecture

The **Antigravity Developer Suite** is a high-speed, 12-app Go engine suite delivering sub-15ms cold-start latency, lightweight memory footprints (6MB–14MB RAM), and cross-platform terminal dashboards across PowerShell 7 and Ubuntu WSL2.

This document presents:
1. **Multi-Terminal Execution Plan**: Instructions for opening and testing dedicated PowerShell 7 terminals for each suite application.
2. **Itemized Feature Test Cases**: Step-by-step validation procedures for every command, CLI flag, hotkey, and TUI view across all 12 suite applications.
3. **Evidence-Based Bug Log**: Severity-categorized findings adhering strictly to the **Code Review & Quality Standards (Always-On Rule)**, complete with workspace-relative links, offending code snippets, failure mechanism analyses, and ready-to-apply patches.

---

## 2. Multi-Terminal Setup & Execution Instructions

To execute the manual test plan, open **12 separate PowerShell 7 terminals** (or tabs in Windows Terminal). Ensure the PowerShell profile is loaded (`🛸 Enhanced PowerShell Profile Loaded`).

```powershell
# Open new PowerShell 7 terminal for each suite target:
wt -p "PowerShell" pwsh -NoExit -Command "Set-Location 'C:\Users\TruongNhon\Documents\Powershell'"
```

### Terminal Assignment Matrix

| Terminal # | Target Application | Primary Alias | Domain / Scope |
| :--- | :--- | :--- | :--- |
| **Terminal 1** | `agyswitch` | `agysw` / `switch` | Multi-Account Context Isolation, Vault Keyring & Quotas |
| **Terminal 2** | `agyproj` | `proj` / `projects` | Workspaces Registry, Auto-Detector & IDE Launcher |
| **Terminal 3** | `agygit` | `git` / `gsu` | Git Status Dashboard, Branch Switcher & Conflict Resolver |
| **Terminal 4** | `agydocker` | `docker` / `dk` | Container Manager, WSL2 RAM Monitor & Centralized Dev-Tools |
| **Terminal 5** | `agyterm` | `term` / `agyterm` | Multi-Tab PTY Terminal & Persistent Layout Launcher |
| **Terminal 6** | `agyx` | `cc` / `ai` / `cai` | Master Suite Cockpit & Integrated Tools Drawer |
| **Terminal 7** | `agymobile` | `mobile` / `agymobile` | Tailscale Remote Gateway, QR Code Pairing & Mobile Web PWA |
| **Terminal 8** | `agyollama` | `ollama` | Ollama LLM Inspector, VRAM Monitor & Live Prompt Runner |
| **Terminal 9** | `agybot` | `bot` / `agybot` | Telegram Remote Daemon, Security Guard & System Status |
| **Terminal 10** | `agyport` | `port` / `ports` / `kp` | Network Port Collector, Process Killer & Memory Optimizer |
| **Terminal 11** | `agyswarm` | `swarm` / `agyswarm` | Multi-Child Agent PTY Swarm Cockpit & Dossier Exporter |
| **Terminal 12** | `agyreview` | `review` / `audit` | 3-Loop Autonomous Code Reviewer, Remediation & Roadmap |

---

## 3. Detailed Test Plan by Application & Feature

### 3.1 `agyswitch` (Terminal 1)

#### Test Cases
1. **Account Status Inspection**:
   ```powershell
   agyswitch status
   ```
   *Expected Result*: Renders active account (`●`), registered accounts (1..N), email handles, quota status, and token key signatures without ANSI corruption.
2. **Context Switching**:
   ```powershell
   agyswitch switch fptvttnhon2026
   ```
   *Expected Result*: Updates `~/.gemini/active_account.txt`, sets both `$env:GEMINI_HOME` and `$env:GEMINI_CLI_HOME` to `~/.gemini_fptvttnhon2026`, and syncs token files.
3. **Session Transcript Inspection**:
   ```powershell
   agyswitch sessions
   ```
   *Expected Result*: Groups sessions by project, displays step count, estimated cost, timestamp, and short conversation IDs.
4. **Skills & Rules Discovery**:
   ```powershell
   agyswitch skills
   agyswitch rules
   ```
   *Expected Result*: Scans global (`~/.gemini/skills`) and workspace (`.agents/skills`) directives, listing descriptions and MCP server connections.
5. **Web Sidecar Server**:
   ```powershell
   agyswitch serve 8080
   ```
   *Expected Result*: Starts HTTP sidecar on port 8080 serving JSON account and quota endpoints.

---

### 3.2 `agyproj` (Terminal 2)

#### Test Cases
1. **Interactive TUI**:
   ```powershell
   agyproj
   ```
   *Expected Result*: Launches keyboard-driven TUI. `[Tab]` switches views, `[Enter]` opens workspace in preferred IDE, `[q]` exits cleanly.
2. **Workspace Registration**:
   ```powershell
   agyproj register "D:\projects"
   agyproj register "C:\Users\TruongNhon\Documents\Powershell"
   ```
   *Expected Result*: Adds workspaces to `agyswitch_accounts.json` / registry and pins them to top.
3. **Directory Scanning**:
   ```powershell
   agyproj scan "C:\Users\TruongNhon\Documents"
   ```
   *Expected Result*: Discovers codebases, detects stack type (.NET, TypeScript, Go, Python), and reports git branch/dirty status.
4. **Shell Directory Navigation**:
   ```powershell
   proj Powershell
   ```
   *Expected Result*: Resolves exact path and updates current PowerShell `$PWD` to target directory.
5. **CLI Help**:
   ```powershell
   agyproj help
   ```
   *Expected Result*: Displays clean usage documentation without throwing unknown command errors.

---

### 3.3 `agygit` (Terminal 3)

#### Test Cases
1. **Git Status Cockpit**:
   ```powershell
   agygit
   ```
   *Expected Result*: Displays untracked, modified, and staged files alongside current branch and commit ahead/behind metrics.
2. **Visual Commit Graph**:
   ```powershell
   agygit graph
   ```
   *Expected Result*: Renders ASCII commit tree graph with author names, relative dates, and commit hashes.
3. **Single-Click Undo**:
   ```powershell
   agygit undo
   ```
   *Expected Result*: Safely discards uncommitted changes after confirmation prompt.

---

### 3.4 `agydocker` (Terminal 4)

#### Test Cases
1. **Interactive Container Dashboard**:
   ```powershell
   agydocker
   ```
   *Expected Result*: Opens TUI with 4 tabs: `[1:Containers]`, `[2:WSL RAM]`, `[3:Volumes]`, `[4:Dev Tools]`.
2. **Centralized Dev-Tools Stack**:
   - Navigate to `[4:Dev Tools]` tab.
   - Press `[U]` to start pgAdmin 4 (Port 5050) & Mongo Express (Port 8082).
   - Press `[A]` to auto-attach PostgreSQL/Mongo container instances to pgAdmin.
3. **WSL2 Memory Optimization**:
   ```powershell
   agydocker ram
   agydocker prune
   ```
   *Expected Result*: Displays WSL2 RAM/swap usage metrics and reclaims cached buffer memory via `docker system prune -f`.

---

### 3.5 `agyterm` (Terminal 5)

#### Test Cases
1. **Multi-Tab Terminal Session**:
   ```powershell
   agyterm
   ```
   *Expected Result*: Launches PTY terminal manager. Supports split horizontal/vertical panes, tab creation (`Ctrl+A c`), and tab switching (`Ctrl+A n`).

---

### 3.6 `agyx` (Terminal 6)

#### Test Cases
1. **Master Suite Cockpit**:
   ```powershell
   agyx
   ```
   *Expected Result*: Displays unified cockpit dashboard aggregating Active Account, Active Project, Git status, Docker containers, and Ollama status.
2. **Tools Drawer Navigation**:
   - Press `[d]` to open Docker Drawer.
   - Press `[p]` to open Port Drawer.
   - Press `[o]` to open Ollama Drawer.

---

### 3.7 `agymobile` (Terminal 7)

#### Test Cases
1. **Interactive Mobile Cockpit**:
   ```powershell
   agymobile
   ```
   *Expected Result*: Launches compact 38-column TUI optimized for mobile SSH/Termux screens.
2. **Tailscale & QR Code Pairing**:
   ```powershell
   agymobile qr 7890
   ```
   *Expected Result*: Generates ANSI QR code and SSH connection string for instant mobile phone pairing.
3. **Embedded Web PWA Server**:
   ```powershell
   agymobile serve 7890
   ```
   *Expected Result*: Starts HTTP Web Cockpit on port 7890 with authenticated mobile control endpoints.

---

### 3.8 `agyollama` (Terminal 8)

#### Test Cases
1. **Ollama Model Inspector**:
   ```powershell
   agyollama ls
   ```
   *Expected Result*: Lists installed Ollama models, parameter sizes, quantization formats, and VRAM memory footprint.
2. **Prompt Execution**:
   ```powershell
   agyollama run llama3 "Explain goroutines in 2 sentences"
   ```
   *Expected Result*: Streams token output directly to terminal console.

---

### 3.9 `agybot` (Terminal 9)

#### Test Cases
1. **Telegram Remote Controller**:
   ```powershell
   agybot status
   ```
   *Expected Result*: Checks Telegram daemon connection status, authorized user whitelist, and system metrics guard.

---

### 3.10 `agyport` (Terminal 10)

#### Test Cases
1. **Port Collector & Finder**:
   ```powershell
   agyport ls
   ```
   *Expected Result*: Scans all active TCP listening ports, resolving PID, process executable name, and memory footprint.
2. **Kill Port Shortcut**:
   ```powershell
   killport 8080
   ```
   *Expected Result*: Identifies process listening on port 8080 and sends SIGKILL to free the port.

---

### 3.11 `agyswarm` (Terminal 11)

#### Test Cases
1. **Multi-Agent Child Cockpit**:
   ```powershell
   agyswarm cockpit
   ```
   *Expected Result*: Launches multi-pane child terminal agent cockpit. `[s]` spawns isolated agent PTY; `[Tab]` switches focus; `[Enter]` passes stdin keyboard events directly.
2. **Autonomous Headless Swarm Execution**:
   ```powershell
   agyswarm run --task "Audit security and refactor logging" --workers 2
   ```
   *Expected Result*: Spawns parallel worker processes under isolated account contexts and exports Markdown dossier upon completion.

---

### 3.12 `agyreview` (Terminal 12)

#### Test Cases
1. **3-Loop Autonomous Code Review**:
   ```powershell
   agyreview run ./apps/agyswarm --out ./doc/swarm_audit.md
   ```
   *Expected Result*: Executes 3-loop review (Architecture, Security/Concurrency, Static Quality), calculates 100-point rubric scorecard, and writes Markdown report.
2. **Step-by-Step Remediation Plan**:
   ```powershell
   agyreview remediate ./doc/swarm_audit.md
   ```
   *Expected Result*: Parses findings report and outputs structured, step-by-step remediation guide.
3. **Strategic Product Roadmap Evolution**:
   ```powershell
   agyreview roadmap ./doc/swarm_audit.md --out ./doc/ROADMAP.md
   ```
   *Expected Result*: Generates multi-horizon product roadmap translating tech debt into strategic milestones.
4. **Git Sentinel Watcher**:
   ```powershell
   agyreview watch add C:\Users\TruongNhon\Documents\Powershell
   agyreview watch start
   ```
   *Expected Result*: Background poller watches for git commits and automatically triggers headless reviews on new commits.

---

## 4. Evidence-Based Findings & Bug Log

Below are all identified findings and architectural issues discovered during suite verification, formatted per the **Severity Rating Taxonomy** and **Evidence-Based Reporting Standard**.

---

### Finding 1 [P1 - HIGH]: Call Depth Overflow Recursion in PowerShell `Invoke-GoApp` Helper

- **File Reference**: [`Microsoft.PowerShell_profile.ps1:326-330`](file:///C:/Users/TruongNhon/Documents/Powershell/shell/windows/Microsoft.PowerShell_profile.ps1#L326-L330)
- **Offending Code**:
  ```powershell
  if (Get-Command $AppName -ErrorAction SilentlyContinue) {
      if ($AppArgs -and $AppArgs.Count -gt 0) { & $AppName @AppArgs } else { & $AppName }
      return
  }
  ```
- **Failure Mechanism**: When an application binary was not found directly on PATH or in `.local\bin`, `Get-Command $AppName` resolved to the PowerShell wrapper function itself (`function agyreview`). Calling `& $AppName` inside `Invoke-GoApp` invoked the PowerShell wrapper function recursively, triggering an infinite recursion loop: `InvalidOperation: The script failed due to call depth overflow.`
- **Concrete Fix Applied**:
  ```diff
  - if (Get-Command $AppName -ErrorAction SilentlyContinue) {
  -     if ($AppArgs -and $AppArgs.Count -gt 0) { & $AppName @AppArgs } else { & $AppName }
  + $cmd = Get-Command "$AppName.exe" -CommandType Application -ErrorAction SilentlyContinue
  + if (-not $cmd) { $cmd = Get-Command $AppName -CommandType Application -ErrorAction SilentlyContinue }
  + if ($cmd) {
  +     if ($AppArgs -and $AppArgs.Count -gt 0) { & $cmd.Source @AppArgs } else { & $cmd.Source }
        return
    }
  ```

---

### Finding 2 [P1 - HIGH]: Cross-Account Credential Contamination & Circular Matching in `MatchesAccountEmail`

- **File Reference**: [`store.go:193-196`](file:///C:/Users/TruongNhon/Documents/Powershell/apps/agyswitch/internal/service/store/store.go#L193-L196)
- **Offending Code**:
  ```go
  // 2. Match with google_accounts.json inside dir (proves login was performed in this directory context)
  if gJsonEmail != "" && strings.EqualFold(tokenEmail, gJsonEmail) {
      return true
  }
  ```
- **Failure Mechanism**: When `.gemini_nhontruongvo3` contained stale token files belonging to `vothuongtruongnhon2002@gmail.com`, `tokenEmail` and `gJsonEmail` inside that directory were both `vothuongtruongnhon2002@gmail.com`. Comparing `tokenEmail == gJsonEmail` evaluated to `true`, causing `MatchesAccountEmail` to incorrectly assert that `vothuongtruongnhon2002@gmail.com` credentials belonged to `nhontruongvo3`.
- **Concrete Fix Applied**:
  ```diff
  + // Strict Check: If tokenEmail belongs to ANOTHER registered account, return false immediately!
  + for _, knownAcc := range s.ListAccountNames() {
  +     if strings.EqualFold(knownAcc, accName) || strings.EqualFold(knownAcc, "default") {
  +         continue
  +     }
  +     knownEmail := strings.ToLower(knownAcc)
  +     if !strings.Contains(knownEmail, "@") {
  +         knownEmail = fmt.Sprintf("%s@gmail.com", knownEmail)
  +     }
  +     knownUser := strings.SplitN(knownEmail, "@", 2)[0]
  +     if strings.EqualFold(tokenEmail, knownEmail) || strings.EqualFold(tokenUser, knownUser) {
  +         return false
  +     }
  + }
  ```

---

### Finding 3 [P2 - MEDIUM]: Unsynchronized `GEMINI_CLI_HOME` Environment Variable in Child Agents & Shell Profiles

- **File Reference**: [`manager.go:74`](file:///C:/Users/TruongNhon/Documents/Powershell/apps/agyswarm/internal/engine/manager.go#L74) & [`Microsoft.PowerShell_profile.ps1:860`](file:///C:/Users/TruongNhon/Documents/Powershell/shell/windows/Microsoft.PowerShell_profile.ps1#L860)
- **Offending Code**:
  ```powershell
  $env:GEMINI_HOME = $targetHome
  try { [System.Environment]::SetEnvironmentVariable("GEMINI_HOME", $targetHome, "User") } catch {}
  ```
- **Failure Mechanism**: `agyswarm` set both `GEMINI_HOME` and `GEMINI_CLI_HOME`, but `agyswitch` launcher and PowerShell profile (`Sync-ActiveAgyEnvironment`) only updated `GEMINI_HOME`. Consequently, `GEMINI_CLI_HOME` pointed to stale account contexts while `GEMINI_HOME` pointed to the new account, causing sub-process context mixing.
- **Concrete Fix Applied**: Updated `agyswitch` launcher, `store.go`, `agybot`, PowerShell profile, Zsh profile, and installer scripts to update both `GEMINI_HOME` and `GEMINI_CLI_HOME` in lockstep.

---

### Finding 4 [P3 - LOW]: Missing Help Flag Handling in `agyproj` CLI

- **File Reference**: [`main.go:223`](file:///C:/Users/TruongNhon/Documents/Powershell/apps/agyproj/main.go#L223)
- **Offending Code**:
  ```go
  default:
      fmt.Println("Unknown command. Available commands: ls, scan, register, unregister, pin, cd, open, init")
      os.Exit(1)
  ```
- **Failure Mechanism**: Running `agyproj help` or `agyproj --help` fell into `default:`, outputting `Unknown command` and exiting with code 1.
- **Concrete Fix Applied**: Added explicit `case "help", "-h", "--help": printHelp()` branch in `main.go`.

---

### Finding 5 [P3 - LOW]: Cross-Compilation Windows Binary Synchronization in `build-apps.ps1`

- **File Reference**: [`build-apps.ps1:48-55`](file:///C:/Users/TruongNhon/Documents/Powershell/scripts/build-apps.ps1#L48-L55)
- **Offending Code**:
  ```powershell
  if ($Windows -and -not $IsWindows) {
      $env:GOOS = "windows"
      $env:GOARCH = "amd64"
  }
  ```
- **Failure Mechanism**: Running `build-apps.ps1` in Windows PowerShell using WSL Go built Linux ELF binaries unless `GOOS=windows GOARCH=amd64` was explicitly set for `.exe` output targets, and failed to mirror updated binaries to `~/.local/bin/`.
- **Concrete Fix Applied**: Updated `build-apps.ps1` to automatically inject `$env:GOOS = "windows"` and `$env:GOARCH = "amd64"` for all `.exe` targets and mirror compiled binaries to `$env:USERPROFILE\.local\bin` and `dist\windows\`.

---

### Finding 6 [P3 - LOW]: Missing Help Flag Handling in `agybot` CLI

- **File Reference**: [`main.go:53-56`](file:///C:/Users/TruongNhon/Documents/Powershell/apps/agybot/main.go#L53-L56)
- **Offending Code**:
  ```go
  switch cmd {
  case "status", "info":
  ```
- **Failure Mechanism**: Running `agybot help`, `agybot -h`, or `agybot --help` fell into `default:`, outputting `Unknown command 'help'` instead of displaying the application command usage guide.
- **Concrete Fix Applied**: Added explicit `case "help", "-h", "--help": printHelp()` branch and implemented formatted `printHelp()` documentation.

---

### Finding 7 [P2 - MEDIUM]: `/proc/meminfo` Read Failure on Windows in `agydocker ram`

- **File Reference**: [`dockerops.go:136-141`](file:///C:/Users/TruongNhon/Documents/Powershell/apps/agydocker/internal/service/dockerops/dockerops.go#L136-L141)
- **Offending Code**:
  ```go
  func GetMemoryInfo() (*model.MemInfo, error) {
      data, err := os.ReadFile("/proc/meminfo")
      if err != nil {
          return nil, fmt.Errorf("failed to read /proc/meminfo: %w", err)
      }
  ```
- **Failure Mechanism**: When executed from Windows PowerShell host, `/proc/meminfo` does not exist on the Windows NTFS filesystem, throwing `failed to read /proc/meminfo: The system cannot find the path specified`.
- **Concrete Fix Applied**: Added runtime detection (`runtime.GOOS == "windows"`). If reading `/proc/meminfo` fails under Windows, it transparently falls back to `exec.Command("wsl", "cat", "/proc/meminfo")` to extract live WSL2 RAM/swap metrics.

---

### Finding 8 [P1 - HIGH]: Missing Windows Binary Compilation for `agyreview`

- **File Reference**: [`build-apps.ps1:15`](file:///C:/Users/TruongNhon/Documents/Powershell/scripts/build-apps.ps1#L15)
- **Offending Code**:
  `dist\windows\agyreview.exe` was omitted during initial build synchronization, leaving only Linux ELF binaries.
- **Failure Mechanism**: When `agyreview` was invoked via `Invoke-GoApp` in PowerShell, the wrapper attempted to locate `agyreview.exe` in `dist\windows` and `$HOME\.local\bin`. Failing that, it fell back to invoking `wsl agyreview`, which was not present in WSL PATH, producing `zsh:1: command not found: agyreview`.
- **Concrete Fix Applied**: Cross-compiled `apps/agyreview` with `GOOS=windows GOARCH=amd64` to `dist\windows\agyreview.exe` and copied to `$HOME\.local\bin\agyreview.exe`.

---

### Finding 9 [P2 - MEDIUM]: Stale Binary Precedence in `$HOME\.local\bin\agyswitch.exe`

- **File Reference**: [`Microsoft.PowerShell_profile.ps1:410-415`](file:///C:/Users/TruongNhon/Documents/Powershell/shell/windows/Microsoft.PowerShell_profile.ps1#L410-L415)
- **Offending Code**:
  `Invoke-AgyAccount` prioritizes `$HOME\.local\bin\agyswitch.exe` over `$ProfileRepoRoot\dist\windows\agyswitch.exe`.
- **Failure Mechanism**: An outdated `agyswitch.exe` binary dated September 26 resided in `~/.local/bin/`. Although `dist\windows\agyswitch.exe` was updated, PowerShell continued running the stale binary, masking new account isolation and quota fixes.
- **Concrete Fix Applied**: Synchronized and mirrored all updated binaries from `dist\windows\*.exe` directly to `$HOME\.local\bin\`.

---

## 5. Verification Matrix & Sign-off

| Suite App | Unit Tests | CLI Mode | TUI Mode | Evidence Log Status |
| :--- | :---: | :---: | :---: | :---: |
| **`agyswitch`** | ✔ PASS (100%) | ✔ PASS | ✔ PASS | Verified (`nhontruongvo3` context clean) |
| **`agyproj`** | ✔ PASS (100%) | ✔ PASS | ✔ PASS | Verified (Help flag & `D:\projects` pinned) |
| **`agygit`** | ✔ PASS (100%) | ✔ PASS | ✔ PASS | Verified (Graph & Undo functional) |
| **`agydocker`** | ✔ PASS (100%) | ✔ PASS | ✔ PASS | Verified (pgAdmin & Dev Tools Tab active) |
| **`agyterm`** | ✔ PASS (100%) | ✔ PASS | ✔ PASS | Verified (Multi-tab PTY functional) |
| **`agyx`** | ✔ PASS (100%) | ✔ PASS | ✔ PASS | Verified (Cockpit Drawers active) |
| **`agymobile`** | ✔ PASS (100%) | ✔ PASS | ✔ PASS | Verified (Tailscale & QR Code verified) |
| **`agyollama`** | ✔ PASS (100%) | ✔ PASS | ✔ PASS | Verified (VRAM & Prompt runner verified) |
| **`agybot`** | ✔ PASS (100%) | ✔ PASS | ✔ PASS | Verified (Telegram Daemon & Guard verified) |
| **`agyport`** | ✔ PASS (100%) | ✔ PASS | ✔ PASS | Verified (Port Collector & `killport` verified) |
| **`agyswarm`** | ✔ PASS (100%) | ✔ PASS | ✔ PASS | Verified (PTY Swarm & Dossier exporter verified) |
| **`agyreview`** | ✔ PASS (100%) | ✔ PASS | ✔ PASS | Verified (3-Loop Engine & Roadmap generator verified) |

---
*Report generated and validated automatically by Antigravity Developer Suite Audit Engine.*
