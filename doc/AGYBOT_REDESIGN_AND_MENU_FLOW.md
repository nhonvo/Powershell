# AGYBOT Telegram Controller — Full Architecture & Menu Flow Redesign Specification

## 1. Executive Summary & Vision

`agybot` is the Telegram Remote Controller for the **Antigravity AI Suite**. It provides autonomous remote control of developer PCs from mobile devices (Telegram), enabling full session continuation, project switching, code reviews, multi-agent swarms, file transfers, and model management.

This redesign document establishes the complete UI/UX flow, menu hierarchy, model picker inline keyboard, and error diagnostics system.

---

## 2. Updated Menu Hierarchy & Inline Keyboard Architecture

```
                                  ┌───────────────────────────┐
                                  │   /start | /menu | /plan  │
                                  └─────────────┬─────────────┘
                                                │
       ┌───────────────────┬────────────────────┼────────────────────┬───────────────────┐
       │                   │                    │                    │                   │
┌──────▼──────┐     ┌──────▼──────┐      ┌──────▼──────┐      ┌──────▼──────┐     ┌──────▼──────┐
│  🧠 Model   │     │ 📁 Projects │      │ 🔄 Continue │      │  🛠️ Build   │     │  📂 Files   │
│  Selection  │     │   Selector  │      │     (-c)    │      │  & Replan   │     │  & Transfer │
└──────┬──────┘     └──────┬──────┘      └─────────────┘      └──────┬──────┘     └──────┬──────┘
       │                   │                                         │                   │
       ├─ Gemini 3.7 Flash ├─ #1 Powershell                          ├─ /rebuild         ├─ /ls [path]
       ├─ Gemini 3.8 Pro   ├─ #2 BinhDinhFood                        ├─ /replan          ├─ /getfile
       ├─ Claude Sonnet    ├─ #3 InvestManagementSystem              ├─ /swarm           └─ Push Doc
       └─ Claude Opus      └─ #15 learning                           └─ /review
```

### Main Controller Keyboard Layout (`buildMainKeyboard`)

```
┌──────────────────────────────────────┬──────────────────────────────────────┐
│  🧠 Model: Gemini 3.7 Flash          │  👤 Account Quotas (agyswitch)       │
├──────────────────────────────────────┼──────────────────────────────────────┤
│  📁 Projects (15 Registered)         │  🔄 Continue Task (-c)               │
├──────────────────────────────────────┼──────────────────────────────────────┤
│  🛠️ Rebuild App                      │  📋 Replan & Audit                   │
├──────────────────────────────────────┼──────────────────────────────────────┤
│  🐝 Multi-Agent Swarm                │  👁️ Code Review                      │
├──────────────────────────────────────┼──────────────────────────────────────┤
│  📂 Files & Download                 │  💭 Reasoning: OFF ($0 Free)         │
├──────────────────────────────────────┼──────────────────────────────────────┤
│  🧠 Recent Sessions                  │  🩺 System & App Health              │
└──────────────────────────────────────┴──────────────────────────────────────┘
```

---

## 3. Detailed Component Specifications

### 3.1 Interactive Model Selector (`btn_models` / `/model`)
When tapping `🧠 Model` or sending `/model`, the bot displays an interactive inline keyboard of all supported models:

```
🧠 SELECT ACTIVE ANTIGRAVITY AI MODEL:

Current Active Model: Gemini 3.7 Flash (Default)

┌──────────────────────────────────────┬──────────────────────────────────────┐
│  ⭐ Gemini 3.7 Flash                 │     Gemini 3.8 Pro                   │
├──────────────────────────────────────┼──────────────────────────────────────┤
│     Claude 3.5 Sonnet                │     Claude 3.7 Sonnet (Thinking)     │
├──────────────────────────────────────┼──────────────────────────────────────┤
│     Claude Opus 4.6                  │     Ollama (Local DeepSeek)          │
└──────────────────────────────────────┴──────────────────────────────────────┘
```

- Tapping any model button instantly updates `userSession.ActiveModel`, persists the preference, and confirms with:
  `✅ Active AI model updated to: Gemini 3.8 Pro`.

### 3.2 Workspace & Drive Access (`/cd`, `/projects`)
- Automatically registers drive paths (`D:\learning`, `D:\projects`, etc.).
- Preserves security rules while permitting non-destructive file reads across all drive letters (`C:`, `D:`, `E:`).

### 3.3 File Transfer Engine (Hermes / OpenClaw Style)
- `/getfile <path>`: Generates a direct Telegram document download link/attachment.
- Photo & Document Attachment Handling: Automatically downloads files sent from phone into active workspace root.

### 3.4 Error Diagnostics & System Self-Healing (`/health`)
- Verifies health status of all 12 micro-apps:
  - `agybot`, `agyswitch`, `agyproj`, `agygit`, `agydocker`, `agyterm`, `agyx`, `agymobile`, `agyollama`, `agyport`, `agyswarm`, `agyreview`.
- If an application is down or missing, displays a **1-Tap Rebuild** button (`/rebuild`).

---

## 4. Implementation Roadmap

1. **Phase 1**: Update `types.go`, `client.go`, and `handler.go` with model callback queries (`select_model_<id>`).
2. **Phase 2**: Enhance `buildMainKeyboard` to show current active model & toggle state dynamically on buttons.
3. **Phase 3**: Recompile binaries for Windows (`agybot.exe`) and Linux (`agybot`).
4. **Phase 4**: Verification & end-to-end testing via Telegram.
