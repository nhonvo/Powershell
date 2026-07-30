# ⚡ PowerShell Profile Commands & Command Center Aliases

> **Category**: User Guide  
> **Subsystem**: Shell Integration & Shortcuts  
> **Date**: 2026-07-31  
> **Author**: Antigravity AI Engineering Team  
> **Status**: Completed / Active  

---

## Executive Summary
This document specifies all PowerShell profile functions, aliases, and terminal integration shortcuts exported by `Microsoft.PowerShell_profile.ps1`.

## Table of Contents
- [1. Environment Launch Triggers](#1-environment-launch-triggers)
- [2. Navigation & Workspace Shortcuts](#2-navigation--workspace-shortcuts)
- [3. Account & System Utilities](#3-account--system-utilities)
- [4. Complete Alias Catalog](#4-complete-alias-catalog)
- [5. Cross References](#5-cross-references)

---

## 1. Environment Launch Triggers

| Command | Function | Target Environment | SQLite Database | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `cc` | `Invoke-ControlCenter` | Production (`Production`) | `agytui.db` | Launches production Spectre.Console TUI dashboard. |
| `ccd` | `Invoke-ControlCenterDev` | Development (`Development`) | `agytui.dev.db` | Launches isolated dev sandbox with debug build. |

---

## 2. Navigation & Workspace Shortcuts

- **`proj` / `cnav`**: Launches the interactive Workspace Navigator to switch between project directories.
- **`ai` / `cai` / `claude`**: Routes prompt directly to the Multi-Agent router.
- **`rterm` / `open-term`**: Launches a new Windows Terminal (`wt.exe`) tab positioned in the current directory.

---

## 3. Account & System Utilities

- **`reset-agy`**: Resets account data and purges custom account context directories.
- **`purge-accounts`**: Deletes all non-default account profiles and revokes cached tokens.
- **`dotnet-info`**: Displays system .NET SDK and runtime build information.

---

## 4. Complete Alias Catalog

```powershell
Set-Alias -Name ai -Value Invoke-MultiAgent -Force
Set-Alias -Name cai -Value Invoke-MultiAgent -Force
Set-Alias -Name claude -Value Invoke-MultiAgent -Force
Set-Alias -Name cc -Value Invoke-ControlCenter -Force
Set-Alias -Name ccd -Value Invoke-ControlCenterDev -Force
Set-Alias -Name cnav -Value Invoke-ControlCenterNavigator -Force
Set-Alias -Name reset-agy -Value Reset-AgyAccountData -Force
Set-Alias -Name purge-accounts -Value Purge-AgyAccounts -Force
Set-Alias -Name dotnet-info -Value Show-DotNetInfo -Force
Set-Alias -Name proj -Value Invoke-WorkspaceNavigator -Force
```

---

## 5. Cross References
- [Automated Onboarding Guide](onboarding_and_setup.md)
- [Dual Environment Workflow](../03_developer_guide/dual_environment_workflow.md)
