# 🖥️ Spectre.Console TUI Screen Catalog

> **Category**: User Guide  
> **Subsystem**: User Interface Views & Layouts  
> **Date**: 2026-07-31  
> **Author**: Antigravity AI Engineering Team  
> **Status**: Active / Approved  

---

## Executive Summary
This document provides a visual and operational catalog of all interactive Spectre.Console terminal screens, navigation layouts, widgets, and keybindings in `AgyTui`.

## Table of Contents
- [1. TUI Architecture & Layout Renderers](#1-tui-architecture--layout-renderers)
- [2. Interactive Navigation Components](#2-interactive-navigation-components)
- [3. Domain Screen Views Catalog](#3-domain-screen-views-catalog)
- [4. Global Hotkeys & Keybindings](#4-global-hotkeys--keybindings)
- [5. Cross References](#5-cross-references)

---

## 1. TUI Architecture & Layout Renderers

`AgyTui` renders rich ANSI terminal interfaces using Spectre.Console:
- **`ThreePaneRenderer.cs`**: 3-pane responsive layout featuring a Left Navigation Tree, Main Content View, and Right Status/Help Panel.
- **`FlatTreeRenderer.cs`**: Hierarchical tree layout for menu nodes (`MenuNode.cs`).
- **`ScreenChrome.cs`**: Outer border chrome with header bar, active account indicator, and hotkey guide footer.

---

## 2. Interactive Navigation Components

- **Command Palette (`Ctrl+P` / `CommandPalette.cs`)**: Fuzzy-search window allowing instant execution of any command or screen transition across the entire system.
- **ScrollableListView (`ScrollableListView.cs`)**: Interactive keyboard-navigable list component supporting pagination and selection highlighting.

---

## 3. Domain Screen Views Catalog

| Screen View Class | Location | Purpose & Capabilities |
| :--- | :--- | :--- |
| `StudyConsoleView` | `UI/Screens/Learn/` | Spaced repetition study dashboard, SuperMemo 2 flashcards, and quiz console. |
| `GitNexus` | `UI/Screens/Git/` | Interactive Git repository visualizer, branch tree, commit history, and stash manager. |
| `CodeViewer` | `UI/Screens/Ide/` | Syntax-highlighted file syntax code viewer. |
| `GitDiffViewer` | `UI/Screens/Ide/` | Interactive side-by-side / inline Git diff viewer. |
| `SystemConsoleView` | `UI/Screens/SysNet/` | System resource monitoring (CPU, RAM, Disk, SQLite DB stats). |

---

## 4. Global Hotkeys & Keybindings

- **`Ctrl+P`**: Toggle Command Palette
- **`Tab` / `Shift+Tab`**: Switch focus between TUI panes
- **`Up` / `Down`**: Navigate menu tree and lists
- **`Enter`**: Execute selected command / open view
- **`Esc`**: Go back to parent menu / close modal

---

## 5. Cross References
- [PowerShell Profile Shortcuts](file:///C:/Users/TruongNhon/Documents/Powershell/docs/02_user_guide/powershell_profile_shortcuts.md)
- [DDD Bounded Contexts](file:///C:/Users/TruongNhon/Documents/Powershell/docs/01_architecture/ddd_bounded_contexts.md)
