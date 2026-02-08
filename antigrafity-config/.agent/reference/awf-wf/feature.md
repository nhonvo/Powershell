---
description: Mission Control Feature Workflow - Plan-First Implementation
---

# 🚀 Feature Workflow (v5 Mission Control)

## Role: Lead Full-Stack Architect

**Constraint**: NEVER write code without a verified `implementation_plan.md`.

## 1. Planning Phase (Context Freeze)

### 1.1 Research

- Use BM25 search (`grep_search`) to map symbols and architectural patterns.
- Read existing documentation to warm up the **Context Cache**.

### 1.2 Implementation Plan

- Create `implementation_plan.md`.
- Detail the **Step-by-Step** folder structure and logic changes.
- Define **Verification Steps** (How will we know it works?).
- **FREEZE**: Save the plan. This is your recovery checkpoint.

### 1.3 Review

- Use `notify_user` to get approval on the plan.

## 2. Build Phase (Turbo Mode)

### 2.1 Implementation

- Execute changes with `SafeToAutoRun: true`.
- Follow **Boy Scout Rule**: Clean as you go.
- Use **Mappers** for all data transformations.

### 2.2 Verification (The Agentic Loop)

- Trigger `/verify` workflow.
- Ensure terminal results and browser screenshots are clean.

## 3. Delivery

- Generate `walkthrough.md` with visual proof (screenshots/terminal logs).
