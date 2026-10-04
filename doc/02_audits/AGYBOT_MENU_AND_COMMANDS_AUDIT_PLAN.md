# AGYBOT Command & Menu Audit & Refactoring Plan

## 1. Executive Summary

This document provides a comprehensive audit of all existing Telegram slash commands, inline keyboards, callback handlers, and menu options in `agybot`. It establishes a streamlined, modular refactoring plan to eliminate redundancies, standardize command routing, and enhance remote mobile usability.

---

## 2. Complete Inventory of Existing Commands & Handlers

### 2.1 Current Registered Slash Commands (27 Commands)

| Command Alias | Description / Action | Target Handler |
|---|---|---|
| `/start`, `/menu`, `/plan` | Open Main Interactive Control Panel | `handlePlanDashboard` |
| `/help` | Complete Bot Reference Guide | `handleHelp` |
| `/model`, `/models` | Model Picker Grid / Set Model | `handleModels` / `handleSetModel` |
| `/effort` | Reasoning Effort Selector (High/Med/Low) | `handleEffort` |
| `/quota`, `/account` | View Accounts & Gemini/Claude Quotas | `handleAccount` |
| `/switch` | Switch Active Google Account (`agyswitch`) | `handleSwitch` |
| `/thinking`, `/thought` | Toggle Reasoning Stream ($0 Free) | `handleToggleThinking` |
| `/skills`, `/plugins` | List Active Skills & Rules | `handleSkills` |
| `/health`, `/diag`, `/diagnose` | Run 12-App System Health Diagnostics | `handleHealthCheck` |
| `/status` | Host CPU, RAM, Containers & Session Stats | `handleStatus` |
| `/proj`, `/projects` | List & Pick Registered `agyproj` Workspaces | `handleProjects` |
| `/cd`, `/workspace` | Switch Active Workspace Path | `handleCd` |
| `/ls` | List Directory Files & Folders | `handleLs` |
| `/view`, `/cat` | View Text File Snippet | `handleCat` |
| `/getfile`, `/download` | Upload File from PC to Phone Attachment | `handleGetFile` |
| `/newproj`, `/createproj` | Scaffold New Codebase & Register `agyproj` | `handleNewProj` |
| `/rebuild`, `/build` | Rebuild All 12 Micro-Apps (`build-apps.ps1`) | `handleRebuild` |
| `/replan` | Replan Technical Debt & Product Roadmap | `handleReplan` |
| `/swarm` | Launch Multi-Agent Parallel Swarm Workmates | `handleSwarm` |
| `/review` | Run `agyreview` Code & Security Audit | `handleReview` |
| `/research` | Perform Deep Architectural Research | `handleResearch` |
| `/session`, `/sessions` | Active Session Info / Recent Brain Sessions | `handleCurrentSession` / `handleSessions` |
| `/resume` | Resume Previous Session ID | `handleResume` |
| `/reset`, `/new` | Reset Current Chat Session State | `delete(userSessions, id)` |
| `/config` | View Bot Configuration Settings | `handleConfig` |
| `/pin`, `/auth`, `/unlock` | Security Unlock Authentication | `authMgr.SetAwaitingPin` |
| `/lock` | Lock Security Controller | `authMgr.Lock` |

### 2.2 Direct Chat Shortcuts

- `-c`, `0c`, `/c`, `/continue`: Continue active prompt session.
- Document / Photo attachment: Download and save uploaded file directly into active workspace.

### 2.3 Current Inline Keyboard Buttons (Main Keyboard)

```
┌──────────────────────────────────────┬──────────────────────────────────────┐
│  📋 Plan & Controller                │  🔄 Continue (-c)                    │
├──────────────────────────────────────┼──────────────────────────────────────┤
│  🧠 Model: <active_model>            │  ⚡ Effort: <active_effort>          │
├──────────────────────────────────────┼──────────────────────────────────────┤
│  📊 Quotas (agyswitch)               │  💭 Thinking: ON/OFF                 │
├──────────────────────────────────────┼──────────────────────────────────────┤
│  📁 Projects                         │  🛠️ Rebuild Apps                     │
├──────────────────────────────────────┼──────────────────────────────────────┤
│  🐝 Swarm Task                       │  👁️ Code Review                      │
├──────────────────────────────────────┼──────────────────────────────────────┤
│  📂 Files                            │  🖥️ Host Status                      │
└──────────────────────────────────────┴──────────────────────────────────────┘
```

---

## 3. Findings & Architectural Gaps

1. **Monolithic Handler File (`handler.go`)**:
   - `handler.go` currently spans **1,200+ lines** of code containing command routing, workspace operations, system status, file downloads, build execution, and callback queries in a single file.
2. **Command Alias Duplication**:
   - Multiple redundant slash commands map to identical logic (`/plan`, `/menu`, `/start`; `/model`, `/models`; `/rebuild`, `/build`; `/getfile`, `/download`).
3. **Inconsistent Callback Prefixes**:
   - Inline keyboard callbacks use mixed prefixes (`btn_`, `cd_proj_`, `set_model_`, `set_effort_`).

---

## 4. Refactoring Plan & Modular Architecture

### 4.1 Categorized Command Groups

We refactor the bot into **5 Domain Modules**:

```
                                  ┌───────────────────────────┐
                                  │      bot.Router (cmd)     │
                                  └─────────────┬─────────────┘
                                                │
       ┌───────────────────┬────────────────────┼────────────────────┬───────────────────┐
       │                   │                    │                    │                   │
┌──────▼──────┐     ┌──────▼──────┐      ┌──────▼──────┐      ┌──────▼──────┐     ┌──────▼──────┐
│ 🕹️ Control  │     │ 🧠 AI Model │      │ 📁 Workspace│      │ 🛠️ Automation│     │ 💾 Session  │
│   Module    │     │   & Quota   │      │   & Files   │      │   & Swarm   │     │  Management │
│(control.go) │     │  (model.go) │      │ (workspace.go)     │  (agent.go) │     │(session.go) │
└─────────────┘     └─────────────┘      └─────────────┘      └─────────────┘     └─────────────┘
```

#### Group 1: 🕹️ System Control & Core Navigation (`control.go`)
- `/plan` (Primary dashboard)
- `/status` (Host hardware & container status)
- `/health` (12-App system health check)
- `/help` (Command reference)
- `/lock`, `/unlock` (Security pin authentication)

#### Group 2: 🧠 AI Engine, Models & Quotas (`model.go`)
- `/model [name]` (Interactive model picker grid)
- `/effort [high|med|low]` (Reasoning effort)
- `/thinking [on|off]` (Reasoning stream toggle)
- `/quota` (Account quotas via `agyswitch`)
- `/switch [account]` (Account switcher)
- `/skills` (Loaded skills & plugins)

#### Group 3: 📁 Workspaces & File Management (`workspace.go`)
- `/proj` (Registered `agyproj` list & picker)
- `/cd [path]` (Workspace directory navigator)
- `/ls [path]` (File/folder directory browser)
- `/getfile [path]` (Download host file to phone)
- `/cat [file]` (View file content preview)
- `/newproj [name]` (Scaffold & register project)

#### Group 4: 🛠️ Agent Automation & Multi-Agent (`agent.go`)
- `-c`, `0c`, `/continue` (Session continuation)
- `/rebuild` (Rebuild all 12 suite micro-apps)
- `/replan` (Tech debt audit & roadmap replan)
- `/swarm [task]` (Multi-agent parallel workforce)
- `/review [target]` (Security & quality code audit)
- `/research [topic]` (Deep architectural analysis)

#### Group 5: 💾 Session & Memory Management (`session.go`)
- `/sessions` (Recent brain sessions list)
- `/resume [id]` (Resume previous session)
- `/session` (Active session state)
- `/reset` (Reset active conversation context)

---

## 5. Refactoring Execution Roadmap

1. **Step 1: Standardize Callback Prefixes**:
   - Prefix all callback query data using standard domain routing:
     - `nav:plan`, `nav:models`, `nav:effort`, `nav:quota`, `nav:projects`, `nav:files`, `nav:health`, `nav:status`, `nav:sessions`, `nav:help`.
     - `act:set_model:<id>`, `act:set_effort:<level>`, `act:cd:<path>`.
2. **Step 2: Modularize Codebase**:
   - Extract domain handlers into clean files in `apps/agybot/internal/bot/`:
     - `router.go` - Central update & command dispatcher.
     - `control.go` - Status, health, help, auth.
     - `model.go` - Models, effort, thinking, quotas, skills.
     - `workspace.go` - Workspaces, files, getfile, newproj.
     - `agent.go` - Prompt execution, continuation, rebuild, replan, swarm, review.
3. **Step 3: Verification & Rebuild**:
   - Run unit tests (`go test ./...`).
   - Recompile binaries (`agybot.exe` & `agybot`).
   - Verify live daemon execution.
