# 🕹️ AGYSWITCH CLI & TUI — User Guide

> **Category**: User Guide  
> **Subsystem**: Command Center & Interactive Terminal Dashboard  
> **Status**: Production / Active  

---

## 1. Fast Start

Launch the interactive Terminal UI:
```bash
agyswitch
# or short alias:
agysw
```

Launch Antigravity CLI directly under a specific isolated account context:
```bash
agyswitch acc1
# or with subcommands/prompts:
agyswitch acc1 "write a python script"
```

---

## 2. Interactive TUI Keybindings

When running `agyswitch`, the 3-pane interactive dashboard appears:

| Hotkey | Action |
| :--- | :--- |
| `[↑]` / `[k]` | Move cursor up |
| `[↓]` / `[j]` | Move cursor down |
| `[Tab]` | Switch focus between **Accounts**, **Global Skills**, **Workspace Rules**, and **Sessions** panels |
| `[Enter]` | Switch active account context and launch `agy` |
| `[A]` | **Smart Auto-Launch**: Auto-picks the account with highest remaining quota and launches `agy` |
| `[Q]` | Refresh quota summaries for all accounts from Google CloudCode |
| `[S]` | **Sync Skills**: Propagate global skills from `~/.gemini/skills` to all isolated accounts |
| `[R]` | Open account Reset modal (`--soft`, `--auth`, `--hard`) |
| `[Esc]` / `[q]` | Exit TUI |

---

## 3. CLI Command Reference

`agyswitch` supports a full headless CLI suite for scripting and automation:

### Status & Account Listing
```bash
agyswitch status      # Display account table, active status, login state, and quotas
agyswitch ls          # Alias for status
```

### Quota Probing
```bash
agyswitch quota             # Probe quota for current active account
agyswitch quota all         # Probe quotas for all logged-in accounts
agyswitch quota <accName>   # Probe quota for specific account
```

### Account Management
```bash
agyswitch add <name>        # Create a new account directory context (~/.gemini_<name>)
agyswitch login <name>      # Launch isolated 'agy login' flow for target account
agyswitch rename <old> <new># Rename account context directory and metadata
agyswitch delete <name>     # Safely delete account directory and metadata
```

### Template Initialization & Seeding
```bash
agyswitch init              # Provision ~/.gemini_template canonical defaults
agyswitch seed [name]       # Mirror template to account directory (defaults to active account)
```

### Context Resetting
```bash
agyswitch reset <name>              # Standard reset (clears logs/history)
agyswitch reset <name> --soft       # Soft reset (clears logs, cache, temporary databases)
agyswitch reset <name> --auth       # Auth reset (clears keyring tokens and OAuth state)
agyswitch reset <name> --hard       # Hard wipe (nukes context and re-seeds from template)
```

### Smart Launching
```bash
agyswitch auto-launch       # Automatically select account with highest quota and launch agy
agyswitch launch-quota      # Alias for auto-launch
```

### Remote Mobile / SSH Sidecar
```bash
agyswitch serve             # Start HTTP sidecar on port 8080
agyswitch serve 9000        # Start on custom port (e.g. 9000)
```
