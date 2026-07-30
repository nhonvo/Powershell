# 🚀 Automated Fresh Machine Setup Guide

> **Category**: User Guide  
> **Subsystem**: Machine Onboarding & Setup  
> **Date**: 2026-07-31  
> **Author**: Antigravity AI Engineering Team  
> **Status**: Active / Approved  

---

## Executive Summary
This document provides instructions for setting up **PowerShell Control Center (`AgyTui`)** on a fresh Windows machine using the automated onboarding script [psapp/scripts/Install-AgyEnvironment.ps1](file:///C:/Users/TruongNhon/Documents/Powershell/psapp/scripts/Install-AgyEnvironment.ps1).

## Table of Contents
- [1. Prerequisites Audit](#1-prerequisites-audit)
- [2. One-Command Automated Onboarding](#2-one-command-automated-onboarding)
- [3. Profile Integration Verification](#3-profile-integration-verification)
- [4. Verification & Launching](#4-verification--launching)
- [5. Cross References](#5-cross-references)

---

## 1. Prerequisites Audit

Before running setup, ensure:
- **Operating System**: Windows 10/11 (or Windows Server 2022+).
- **PowerShell**: PowerShell 7.0+ (`pwsh`) recommended.
- **Package Manager**: Windows Package Manager (`winget`) available (installed automatically by Windows).

---

## 2. One-Command Automated Onboarding

Open a PowerShell terminal and run:

```powershell
. "$HOME\Documents\Powershell\psapp\scripts\Install-AgyEnvironment.ps1"
```

### What the Onboarding Script Does:
1. **SDK Audit**: Checks for `.NET 9 SDK`. If missing, installs `Microsoft.DotNet.SDK.9` silently via `winget`.
2. **Profile Linking**: Integrates `Microsoft.PowerShell_profile.ps1` into your `$PROFILE`.
3. **App Data Initialization**: Prepares `%APPDATA%\AgyTui` directory structure.
4. **Binary Compilation**: Builds `csapp/AgyTui/AgyTui.csproj` in Release mode.
5. **Database Initialization**: Triggers automatic SQLite schema migrations (V1-V6) and populates default seed data.

---

## 3. Profile Integration Verification

Verify that your `$PROFILE` contains the dot-source link:

```powershell
Get-Content $PROFILE | Select-String "Microsoft.PowerShell_profile.ps1"
```

Reload your profile:
```powershell
. $PROFILE
```

---

## 4. Verification & Launching

Launch the production environment:
```powershell
cc
```

Launch the isolated development sandbox:
```powershell
ccd
```

---

## 5. Cross References
- [PowerShell Profile Commands](file:///C:/Users/TruongNhon/Documents/Powershell/docs/02_user_guide/powershell_profile_shortcuts.md)
- [Spectre.Console TUI Screen Catalog](file:///C:/Users/TruongNhon/Documents/Powershell/docs/02_user_guide/tui_screen_catalog.md)
