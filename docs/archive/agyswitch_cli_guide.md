# 🛸 AGYSWITCH CLI (Go Engine v1.3.0) — Complete Feature & Migration Guide

> **Official Guide**: How to set up, use, and transition to the new `agyswitch` CLI tool for Google Antigravity (`agy`).

---

## 📋 Table of Contents
1. [Overview](#1-overview)
2. [Key Feature Breakdown](#2-key-feature-breakdown)
3. [Interactive TUI Features & Controls](#3-interactive-tui-features--controls)
4. [Step-by-Step Guide: Switching to the New `agyswitch` Tool](#4-step-by-step-guide-switching-to-the-new-agyswitch-tool)
5. [CLI Command Reference](#5-cli-command-reference)
6. [Daily Workflows & Examples](#6-daily-workflows--examples)
7. [Troubleshooting & Resetting Accounts](#7-troubleshooting--resetting-accounts)

---

## 1. Overview

`agyswitch` (Go Engine v1.3.0) is a high-performance, cross-platform context manager for Google Antigravity (`agy`). It enables seamless multi-account management, instant token context switching, account credential resetting, and quota-aware auto-launching across **Windows (PowerShell)** and **Linux/WSL (Ubuntu Zsh)**.

### Why Switch to `agyswitch`?
- ⚡ **Ultra-Fast Execution**: Sub-10ms startup time built in Go (replaces heavy wrappers).
- 🔐 **Isolated Account Vaults**: Every account maintains an independent context directory (`~/.gemini_<account>`), preventing token leakage between work, personal, and secondary Google accounts.
- 🎯 **Quota-Aware Selection**: Auto-detects logged-in accounts and selects accounts with active quota to prevent `agy` rate-limit blocks.
- 🧹 **Clean Reset Capabilities**: Dedicated reset command to wipe stale tokens without deleting stored account profiles.
- 🎨 **Modern Interactive TUI**: Terminal UI with real-time status badges, search filtering, and single-keystroke hotkeys.

---

## 2. Key Feature Breakdown

| Feature | Description | Command / Trigger |
| :--- | :--- | :--- |
| **Multi-Account Isolation** | Maintains separate OAuth tokens, `settings.json`, and `google_accounts.json` per account context. | `~/.gemini_<account_name>` |
| **Instant Account Switching** | Swaps active account marker, primary `~/.gemini` context, and Windows Credential Manager DPAPI vault. | `agyswitch switch <account>` |
| **Direct Agy Launching** | Swaps context and immediately launches `agy` CLI with clean environment arguments. | `agyswitch launch <account>` |
| **Quota-Aware Auto-Launch** | Evaluates active tokens and automatically launches `agy` under the best quota account. | `agyswitch launch-quota` / Hotkey `A` |
| **Account Reset** | Clears cached OAuth tokens (`keyring_token.txt`, `antigravity-oauth-token`) and keyring credentials for re-auth. | `agyswitch reset <account>` / Hotkey `X` |
| **Account Listing** | Lists all registered accounts with login status badges, token signatures, and quota health. | `agyswitch list` |
| **Interactive TUI v1.3.0** | Full-screen interactive terminal interface with live status monitoring and keyboard navigation. | `agyswitch` or `agyswitch status` |

---

## 3. Interactive TUI Features & Controls

When you run `agyswitch` with no arguments, the interactive TUI screen opens:

```text
🛸 AGYSWITCH - Dedicated Antigravity Multi-Account Vault (Go Engine v1.3.0)
──────────────────────────────────────────────────────────────────────────────────
 Active Account: vothuongtruongnhon2002

 ● 1. vothuongtruongnhon2002 (vothuongtruongnhon2002@gmail.com) (✔ Logged In · Key: ya29..) [✔ Quota OK]
   2. fptvttnhon2020         (fptvttnhon2020@gmail.com  ) (✔ Logged In · Key: ya29..) [✔ Quota OK]
   3. fptvttnhon2026         (fptvttnhon2026@gmail.com  ) (✘ Logged Out) [✘ Logged Out]
   4. nhontruongvo           (nhontruongvo@gmail.com    ) (✘ Logged Out) [✘ Logged Out]
──────────────────────────────────────────────────────────────────────────────────
 Navigation: [↑/↓/j/k] Move  │  [Enter] Switch  │  [L] Launch  │  [A] Auto Quota  │  [X] Reset  │  [/] Search  │  [Q] Quit
```

### Keyboard Shortcuts Reference

| Hotkey | Action | Description |
| :---: | :--- | :--- |
| `↑` / `↓` or `j` / `k` | **Navigate** | Move highlight selection up or down through the account list. |
| `1` – `9` | **Quick Jump** | Directly jump to account by index number. |
| `Enter` | **Switch Account** | Set highlighted account as active and exit TUI. |
| `L` | **Launch Agy** | Switch active context to highlighted account and launch `agy`. |
| `A` | **Auto Quota Select & Launch** | Auto-detect best logged-in account with healthy quota and launch `agy`. |
| `X` | **Reset Credentials** | Prompt & clear OAuth tokens for highlighted account for re-login. |
| `/` | **Filter / Search** | Type query to filter account list by handle or email. Press `Enter` to confirm. |
| `Q` or `Esc` | **Quit** | Exit TUI without changing active account. |

---

## 4. Step-by-Step Guide: Switching to the New `agyswitch` Tool

Follow these steps to transition your workspace and shell environment to the new `agyswitch-go` tool.

### Step 1: Verify Binary Build & Installation

#### On Linux / WSL (Ubuntu)
Compile and install the Go binary to `~/.local/bin/agyswitch`:
```bash
cd /home/truongnhon/projects/powershell-profile/agyswitch-go
go build -o ~/.local/bin/agyswitch main.go
chmod +x ~/.local/bin/agyswitch
```

#### On Windows (PowerShell)
Compile `agyswitch.exe` and place it in your User profile binary folder (`~/.local/bin` or repository root):
```powershell
cd C:\Users\TruongNhon\projects\powershell-profile\agyswitch-go
go build -o ..\agyswitch.exe main.go
```

---

### Step 2: Configure Shell Profile Aliases

Ensure your shell profile routes `agyswitch` and wrapper aliases directly to the compiled Go binary.

#### WSL Ubuntu Zsh (`~/.zshrc` or `profile_wsl.zsh`)
Add or update the following alias definitions:
```zsh
# Agyswitch Go Engine Route
export PATH="$HOME/.local/bin:$PATH"

alias agyswitch="$HOME/.local/bin/agyswitch"
alias agys="$HOME/.local/bin/agyswitch"
alias agy-quota="$HOME/.local/bin/agyswitch launch-quota"
```

#### Windows PowerShell (`$PROFILE`)
Add or update the following function aliases in your `$PROFILE`:
```powershell
$AGYSWITCH_BIN = "$HOME\projects\powershell-profile\agyswitch.exe"

function agyswitch { & $AGYSWITCH_BIN @args }
Set-Alias -Name agys -Value agyswitch -Option AllScope
```

---

### Step 3: Seed & Verify Accounts

Run `agyswitch list` to inspect registered account contexts:
```bash
agyswitch list
```

If you need to seed account contexts from your Google accounts list, run:
```bash
# On WSL
bash /home/truongnhon/projects/powershell-profile/seed-wsl-accounts.sh
```

---

### Step 4: Login to Accounts

To log into any secondary account:
1. Switch context to target account:
   ```bash
   agyswitch switch fptvttnhon2020
   ```
2. Run `agy login` to authenticate with Google:
   ```bash
   agy login
   ```
3. OAuth tokens will automatically be stored securely inside `~/.gemini_fptvttnhon2020`.

---

## 5. CLI Command Reference

`agyswitch` provides both non-interactive CLI commands and the interactive TUI.

```bash
# 1. Open Interactive TUI
agyswitch

# 2. View Status & Account List
agyswitch status
agyswitch list

# 3. Switch Active Context
agyswitch switch <accountName>
# Example: agyswitch switch fptvttnhon2020

# 4. Launch Agy CLI under specific account
agyswitch launch <accountName> [agy-flags]
# Example: agyswitch launch fptvttnhon2020

# 5. Reset Credentials for an account
agyswitch reset <accountName>
# Example: agyswitch reset fptvttnhon2026

# 6. Auto-select Best Quota Account & Output Info
agyswitch quota

# 7. Auto-select Best Quota Account & Launch Agy CLI
agyswitch launch-quota
agyswitch auto-launch
```

---

## 6. Daily Workflows & Examples

### Workflow A: Interactive Account Selection
1. Open TUI by typing `agyswitch` or `agys`.
2. Use arrow keys to browse accounts.
3. Press `L` to launch `agy` directly under the highlighted account context.

### Workflow B: One-Command Auto Quota Launch
When you want to start coding immediately without caring which account has quota available:
```bash
agyswitch launch-quota
```
`agyswitch` checks all logged-in account tokens, picks the active account (or first logged-in account with healthy quota), and starts `agy`.

### Workflow C: Resetting a Logged-Out or Stale Account
If `agy` throws authentication errors on account `nhontruongvo`:
```bash
# 1. Reset stored OAuth credentials
agyswitch reset nhontruongvo

# 2. Switch to account and re-authenticate
agyswitch switch nhontruongvo
agy login
```

---

## 7. Troubleshooting & Resetting Accounts

### Issue 1: `agy` uses old account credentials after switching
- **Cause**: Shell environment variable `GEMINI_HOME` might be overridden in current subshell.
- **Fix**: Run `agyswitch switch <acc>` directly, or check that `~/.gemini/active_account.txt` matches your desired account name.

### Issue 2: Account status shows `(✘ Logged Out)`
- **Cause**: No valid `keyring_token.txt` or `antigravity-oauth-token` was found in `~/.gemini_<account>`.
- **Fix**: Switch to account (`agyswitch switch <acc>`) and run `agy login`.

### Issue 3: Want to wipe all account tokens and start fresh
- **Fix**: Run `reset-wsl-accounts.sh` script or reset individual accounts using `agyswitch reset <acc>`.

---
*Docs generated for `agyswitch-go` Engine v1.3.0*.
