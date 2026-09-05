# 📋 AgyTui Feature Consolidation & Clean-Up Task Plan

## Overview
This task plan tracks the refactoring, consolidation, category reorganizations, and removals across `AgyTui`.

---

## 🎯 Phase 1: Clean-Up & Initial Removals
| # | Task | Description | Status |
|---|---|---|---|
| **1** | **Remove `/ask-ai`** | Removed Antigravity AI Agent chat command and references. | ✅ Completed |
| **2** | **Remove `/open-term`** | Consolidated terminal launching into `/proj`. | ✅ Completed |
| **3** | **Remove `/favorite` CLI toggle** | Favorites configured strictly via JSON (`profile.config.json`). | ✅ Completed |
| **4** | **Consolidate Workspace Navigation into `/proj`** | Integrated Cd, Terminal, WSL, IDE, Git Diff, Auto-Discover, Prune into `/proj`. | ✅ Completed |
| **5** | **Consolidate IDE under `/ide`** | Diff Viewer and Symbol Search integrated directly into `/ide`. | ✅ Completed |
| **6** | **Separate Git Nexus Group** | Dedicated `/git-nexus` group (`Git Nexus Graph & Stats`). | ✅ Completed |
| **7** | **Remove Cloud AI Agents** | Removed `claude`, `codex`, `claude-cloud`, `codex-cloud`. | ✅ Completed |
| **8** | **Remove Antigravity Deck & Manager** | Removed `deck-*`, `desk-*`, `mgr-*`, `agm-*`. | ✅ Completed |
| **9** | **Remove Study/Quiz Drill Suites** | Removed Japanese drills, English vocab, C# Quiz, DSA, Interview banks, Pomodoro. | ✅ Completed |

---

## 🎯 Phase 2: Category Reorganization & Local AI Agent Enhancement
| # | Task | Description | Status |
|---|---|---|---|
| **10** | **Combine Workspace Navigation & Dev Tools** | Merged `Workspace Navigation` and `Developer Tools & Scaffolding` into unified group `📂 Workspace & Developer Tools` (`/workspace-dev`). | ✅ Completed |
| **11** | **Move Obsidian to `[Workspace & Dev]`** | Relocated `Obsidian Vault & Resources` from dedicated category into `[Workspace & Dev]`. | ✅ Completed |
| **12** | **Move Appearance to `[System & Network]`** | Relocated `Appearance & Favorites` from dedicated category into `[System & Network]`. | ✅ Completed |
| **13** | **Consolidate Ollama & Local AI Agents** | In `[AI Agent & Ollama]`, the tree displays only `🦙 /ollama` (Ollama Center) and `🧠 /ai` (Local AI Agent Launcher). Sub-commands (`hermes`, `hermesd`, `openclaw`, `agy-cli`, `ai-history`, `ollama-*`) are hidden from the tree. | ✅ Completed |
| **14** | **Enhance `/ai` with Full Ollama Agent Support** | Upgraded `/ai` to dynamically detect all installed Ollama models and agents (Hermes3, OpenClaw, DeepSeek, Qwen, Llama, Phi, agy CLI) with interactive chat launcher and model parameter picker. | ✅ Completed |
| **15** | **Build & Test Verification** | Updated `CommandRegistry.cs`, `MenuNode.cs`, `CommandRouter.cs`, `IOllamaClient.cs`, `OllamaClient.cs`, and tests. Rebuilt Release (0 warnings/errors); all 304/304 tests passed. | ✅ Completed |

---
