# AGYSWITCH Go Engine TUI Enhancement Plan & Shell Integration

> **Document Status**: Active Blueprint  
> **Target Package**: `agyswitch-go`  
> **Integrations**: Windows PowerShell (`Microsoft.PowerShell_profile.ps1`) & WSL Zsh/Bash (`linux/posh-profile.zsh`)

---

## 1. Overview & Core Directives

The `agyswitch-go` engine is the single source of truth for **100% credential-isolated multi-account management** across Google Antigravity / Gemini accounts.

### Core Architecture Rule
Executing any account shortcut (`agysw`, `agyswitch`, `agyx`, `agy-account`) **solely triggers and opens the Go `agyswitch` binary**. All legacy routing through C# Control Center (`cc`) or shell script logic is completely unhooked.

---

## 2. Interactive TUI Blueprint (Bullet List UI Features)

When launched without positional arguments (`agysw`), `agyswitch` opens an interactive, lightweight Terminal User Interface (TUI).

- **Header Banner**:
  - Compact ANSI art badge showing engine version (`AGYSWITCH v1.2.0 (Go)`).
  - Active account status summary header line.

- **Interactive Account Selector**:
  - `↑ / ↓` or `j / k` navigation across registered accounts (`1-5`).
  - Quick-jump hotkeys: Pressing `1` to `5` instantly highlights that account.
  - Active indicator (`● Active`) highlighted in vibrant Green.
  - Inactive accounts indicated with bullet space (` `).

- **Token Health & Credential Badges**:
  - **Logged In Badge**: `✔ Logged In · Key: ya29..<middle_sig>` (Vibrant Green & Cyan).
  - **Logged Out Badge**: `✘ Logged Out` (Yellow / Gray).
  - **Isolated Storage Path**: Shows mapped folder (`~/.gemini_<account>`).

- **Interactive Action Footer**:
  - `[Enter]` Switch active context to highlighted account.
  - `[L]` Switch active context AND launch `agy` CLI immediately.
  - `[R]` Refresh token status & check quota keyring validity.
  - `[Q / Esc]` Clean exit back to shell.

- **Non-Interactive CLI Mode (Fast Execution)**:
  - `agysw status` — Render instantaneous terminal summary table.
  - `agysw <account_name>` — Switch context directly without opening TUI.
  - `agysw switch <account_name>` — Explicit switch command.

---

## 3. Flow of Use

```mermaid
flowchart TD
    A["User types 'agysw' in Shell (PowerShell / WSL Zsh)"] --> B{"Positional Arguments?"}
    
    B -- "No Args ('agysw')" --> C["Launch agyswitch Interactive TUI Engine"]
    B -- "With Target ('agysw fpt2020')" --> D["Direct Account Switch Execution"]
    
    C --> E["Display Account List & Token Health"]
    E --> F{"User Key Action"}
    
    F -- "Press [Enter]" --> G["Switch Context (~/.gemini Mirror)"]
    F -- "Press [L]" --> H["Switch Context + Strip IDE Env + Exec 'agy'"]
    F -- "Press [Q]" --> I["Exit Cleanly"]
    
    D --> G
    G --> J["Update Active Keyring & Re-inject cmdkey.exe"]
    J --> K["Print Context Success Message"]
```

### Detailed Workflow Steps

1. **Trigger Phase**:
   - User types `agysw` in PowerShell or WSL terminal.
   - Shell alias routes directly to binary (`~/.local/bin/agyswitch` or `agyswitch.exe`).

2. **Isolation & Verification Phase**:
   - `agyswitch` reads active directory `~/.gemini/active_account.txt`.
   - Vault checks each registered account folder (`.gemini_fptvttnhon2020`, `.gemini_nhontruongvo`, etc.).
   - Extracts unique middle token signature (`GetShortSignature`).

3. **Interactive Selection / Launch Phase**:
   - User highlights desired account and hits `[Enter]` or `[L]`.
   - Pre-switch backup saves active context state to prevent token cross-contamination.
   - Target account credentials are 1-to-1 mirrored to active `~/.gemini` context.
   - OS keyring (`cmdkey.exe`) is updated for host transparency.
   - If `[L]` was selected, launcher strips IDE environment variables (`GEMINI_CLI_IDE_AUTH_TOKEN`, `GEMINI_CLI_IDE_SERVER_PORT`, `ANTIGRAVITY_IDE_SERVER_PORT`) and executes `agy` CLI seamlessly.

---

## 4. Shell Integration (.ps1 & .sh / .zsh)

### Windows PowerShell (`Microsoft.PowerShell_profile.ps1`)

```powershell
# Directly route all account management shortcuts to agyswitch Go binary
function Invoke-AgyAccount {
    param(
        [Parameter(Position=0)][string]$SubCommand,
        [Parameter(Position=1)][string]$TargetAccount
    )
    if (Get-Command agyswitch -ErrorAction SilentlyContinue) {
        if (-not $SubCommand) { & agyswitch; return }
        switch ($SubCommand.ToLowerInvariant()) {
            "use"    { if ($TargetAccount) { & agyswitch switch $TargetAccount } else { & agyswitch } }
            "list"   { & agyswitch status }
            "ls"     { & agyswitch status }
            "status" { & agyswitch status }
            default  { if ($TargetAccount) { & agyswitch $SubCommand $TargetAccount } else { & agyswitch $SubCommand } }
        }
        return
    }
    if (Get-Command wsl -ErrorAction SilentlyContinue) {
        $wslCmd = "agyswitch"
        if ($SubCommand) { $wslCmd += " $SubCommand" }
        if ($TargetAccount) { $wslCmd += " $TargetAccount" }
        wsl bash -c "$wslCmd"
        return
    }
}

Set-Alias -Name agyswitch -Value Invoke-AgyAccount -Force
Set-Alias -Name agysw -Value Invoke-AgyAccount -Force
Set-Alias -Name agyx -Value Invoke-AgyAccount -Force
Set-Alias -Name agy-account -Value Invoke-AgyAccount -Force
```

### WSL Zsh / Bash (`linux/posh-profile.zsh`)

```zsh
# Primary Go binary symlinked in user PATH (~/.local/bin/agyswitch)
alias agy-account="agyswitch"
alias agyswitch="agyswitch"
alias agysw="agyswitch"
alias agyx="agyswitch"
alias agy="agyswitch"
```

---

## 5. Next Implementation Tasks

- [x] Complete Go engine codebase (`vault`, `store`, `launcher`, `main`).
- [x] Unhook legacy C# Control Center (`cc`) account switching code.
- [x] Maintain 100% test coverage across C# and Go test suites.
- [ ] Implement raw terminal TUI keyboard handler (`termbox-go` or `bubbletea`) in `agyswitch-go/ui` for non-zero terminal interactions.
