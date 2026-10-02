# AGYSWITCH Sessions View Enhancement Plan

## Overview
In **AGYSWITCH** (Go Engine v2.0), Tab `[4] 📊 Sessions & Cost` allows users to explore, monitor costs, inspect step trajectories, and continue past agent sessions.

Currently, when grouped by project, all sessions of a project (e.g. 64 sessions for `finance-dashboard`) are flattened sequentially across paginated pages. This causes the first project to monopolize the first 10 pages, burying other projects and making navigation cumbersome.

This document details the architecture and implementation for:
1. **Per-Project 5-Item View & Interactive Expansion**: By default, each project displays its top 5 most recent sessions plus an interactive expandable row (`▶ [... X more sessions · Press Enter to expand]`). Pressing Enter or Space toggles expand/collapse.
2. **Global Real-Time Search (`/`)**: Interactive search filter bar matching session titles, conversation IDs, project names, and workspace directories with live filtering.
3. **Streamlined Selection & Instant Resume**: Intuitive keyboard navigation (`Enter`/`C` to resume, `Space` to toggle, `E` to expand/collapse all, `W` to open in VS Code, `V` for trajectory log, `D` for deletion).
4. **Resilient Terminal UX**: Full responsiveness across terminal sizes (compact < 80 cols and wide >= 80 cols) with zero flicker.

---

## 1. Architecture & Component Changes

```
┌────────────────────────────────────────────────────────────────────────┐
│                        AGYSWITCH Tab 4: Sessions                       │
├────────────────────────────────────────────────────────────────────────┤
│ [🔍 Search: "sync" ] (Live typing / Esc to clear)                     │
│                                                                        │
│ 📁 finance-dashboard (64 sessions · $10.1895) [5 shown · 59 more]      │
│  ▶  1. Untitled Conversation                          0 st · $0.0000   │
│     2. Untitled Conversation                          0 st · $0.0000   │
│     3. Untitled Conversation                          0 st · $0.0000   │
│     4. Untitled Conversation                          0 st · $0.0000   │
│     5. Production Bank Sync Troubleshooting        4910 st · $2.4550   │
│     ▶  [... 59 more sessions for finance-dashboard · Enter to expand]  │
│                                                                        │
│ 📁 agygit (12 sessions · $1.4200) [5 shown · 7 more]                   │
│     6. Git Status Refactor                           45 st · $0.0225   │
│     ...                                                                │
└────────────────────────────────────────────────────────────────────────┘
```

### 1.1 Data Model Extensions (`internal/view/app.go` & `internal/service/sessions/`)
- **`SessionViewItem`**:
  ```go
  type SessionViewItem struct {
      IsExpandToggle bool
      ProjectName    string
      Session        model.SessionInfo
      HiddenCount    int
      TotalInProject int
      IsExpanded     bool
  }
  ```
- **`App` State Additions**:
  - `SessionSearchQuery string`: Current text filter.
  - `isSearching bool`: Active typing mode flag.
  - `expandedProjects map[string]bool`: Track expanded state per project.

### 1.2 Core Service Filtering (`internal/service/sessions/sessions.go`)
- `FilterSessions(sessions []model.SessionInfo, query string) []model.SessionInfo`:
  - Case-insensitive substring matching across:
    - `s.Title`
    - `s.ConversationID`
    - `s.ProjectName`
    - `s.WorkspaceDir`
  - Returns original list if `query == ""`.

### 1.3 Interactive Controls & Keybindings
| Key | Action | Context |
| :--- | :--- | :--- |
| `/` | Open Search bar & start typing query | Tab 4 (Sessions) |
| `Enter` | **On Session**: Resume with `agy --conversation <cid>`<br>**On Expand Toggle**: Expand/Collapse project<br>**In Search Mode**: Confirm query & exit typing mode | Tab 4 |
| `Space` | Toggle expand/collapse of current project | Tab 4 |
| `E` / `e` | Expand All / Collapse All projects | Tab 4 |
| `W` / `w` | Open workspace in VS Code (`code <dir>`) | Tab 4 |
| `V` / `v` | View trajectory step log modal | Tab 4 |
| `D` / `d` | Delete session | Tab 4 |
| `Esc` | **In Search Mode**: Cancel search typing<br>**With Query**: Clear query & show all<br>**Without Query**: Exit AGYSWITCH | Global / Tab 4 |
| `Backspace` | Erase search character | In Search Mode |
| `Ctrl+U` | Clear entire search query | In Search Mode |
| `↑` / `↓` (`j`/`k`) | Navigate items | Tab 4 |
| `n` / `p` | Page Down / Page Up | Tab 4 |

---

## 2. Implementation Steps

1. **`internal/service/sessions/sessions.go`**:
   - Implement `FilterSessions` helper function.
   - Add unit tests in `sessions_test.go`.
2. **`internal/view/app.go`**:
   - Add `SessionSearchQuery`, `isSearching`, and `expandedProjects` fields to `App`.
   - Implement `getVisibleSessionItems()` which applies:
     - Project filter (`SessionFilterProject`)
     - Text search (`FilterSessions`)
     - Sorting (`SessionSortMode`)
     - Grouping (`GroupSessionsByProjectSorted`)
     - Top 5 per project clamping + expandable/collapsible toggle items.
   - Update `renderSessionsTab()` to draw search input, project headers with expand status, top 5 sessions, toggle rows, and dynamic pagination.
   - Update event loop in `Run()` to handle `/`, `Enter`, `Space`, `E`, `W`, `Esc`, `Backspace`, `Ctrl+U`, and arrow navigation.
   - Update Help palette (`?` / `h`) and footer status strings.
3. **Verification & Tests**:
   - Add unit tests in `internal/view/app_test.go` and `internal/service/sessions/sessions_test.go`.
   - Run `go test ./...` in `apps/agyswitch`.
   - Validate live rendering behavior.

---
Dual Reference Links:
- VS Code Clickable: [SESSIONS_FEATURE_ENHANCEMENT_PLAN.md](./SESSIONS_FEATURE_ENHANCEMENT_PLAN.md)
- Windows Path: `C:\Users\TruongNhon\Documents\Powershell\SESSIONS_FEATURE_ENHANCEMENT_PLAN.md`
