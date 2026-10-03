---
name: product-roadmap-evolution
description: Strategic product roadmap and technical debt evolution generator. Translates code review findings, architectural gaps, and tech debt into an actionable, multi-horizon product roadmap. Use when asked to formulate a product roadmap, prioritize tech debt retirement, plan architecture modernization, or define next-step product milestones following a code review.
---

# Product Roadmap & Strategic Evolution Protocol

This skill guides the agent in transforming audit findings, code quality assessments, and architectural evaluations into a structured, executive-grade **Product Roadmap** and **Technical Debt Retirement Plan**.

---

## 1. 3-Horizon Strategic Framework

Every product roadmap must structure engineering and product initiatives across three distinct planning horizons:

### Horizon 1: Foundation Hardening & Defect Liquidation (Immediate: Sprint 1–2 / 0–30 Days)
* **Objective**: Zero-defect security posture, build stability, and elimination of critical blockers.
* **Focus Areas**:
  - Resolving all `[P0 - CRITICAL]` and `[P1 - HIGH]` findings from the code review.
  - Secret eradication and credential rotation.
  - Concurrency deadlocks, goroutine leaks, and file descriptor cleanups.
  - Compiler warnings, broken tests, and CI/CD pipeline reliability.

### Horizon 2: Architecture Modernization & Modular Scaling (Mid-Term: Month 1–3 / 30–90 Days)
* **Objective**: Structural health, reduced cognitive load, test automation, and performance leverage.
* **Focus Areas**:
  - Modular decoupling: breaking monolithic services into specialized packages or micro-apps.
  - Introducing shared abstractions (e.g. unified ANSI strippers, standardized HTTP clients).
  - Expanding unit and integration test coverage to target >85%.
  - Memory optimization, caching layers (TTL cache eviction), and query performance.

### Horizon 3: Strategic Capabilities & Ecosystem Expansion (Long-Term: Quarter 2–4 / 90–360 Days)
* **Objective**: Competitive differentiation, cross-platform capabilities, and autonomous workflows.
* **Focus Areas**:
  - Autonomous multi-agent coordination (e.g. `agyswarm` integration).
  - Cross-platform extensions (macOS/Linux/Windows native binaries).
  - Headless remote control gateways (e.g. Telegram `agybot`, Web PWAs).
  - AI-assisted developer experience, self-healing services, and automated CI/CD gating.

---

## 2. Technical Debt Retirement Curve (Barème Scoring Impact)

When generating the roadmap, map each milestone to its projected impact on the Code Quality Barème (100-Point Quality Index):

| Milestone | Target Horizon | Debt Category | Current Score | Projected Score | Quality Gate Impact |
| :--- | :---: | :--- | :---: | :---: | :--- |
| **M1: Security & Concurrency Lockdown** | H1 | Security & Stability | 72 (Grade C) | 88 (Grade B) | Unblocks Production Deployment |
| **M2: Modular Decoupling & Test Rig** | H2 | Maintainability & Quality | 88 (Grade B) | 94 (Grade A) | Reduces Onboarding Friction |
| **M3: Autonomous Swarm Sentinel** | H3 | Scalability & AI Ecosystem | 94 (Grade A) | 98 (Grade A+) | Enterprise Grade Maturity |

---

## 3. Roadmap Deliverable Generation Protocol

When activated, synthesize findings into a clean GitHub-Flavored Markdown report:
`./PRODUCT_ROADMAP.md` (or `<target-repo>/doc/PRODUCT_ROADMAP.md`).

### Deliverable Schema:
1. **Executive Roadmap Summary**: Current technical debt valuation, overall code health score, and strategic North Star.
2. **Horizon 1, 2, 3 Initiative Breakdown**: Each milestone must specify:
   - **Theme & Value Proposition**
   - **Associated Code Review Findings** (linked with workspace-relative links: `[db.go:45](./internal/db/db.go#L45)`)
   - **T-Shirt Size / Story Points** (S / M / L / XL)
   - **Success Criteria & KPIs** (e.g. 0 P0/P1 bugs, <15ms cold start latency, >85% test coverage)
3. **Dependency Graph & Execution Sequence** (rendered in Mermaid flowchart).
4. **Immediate Next Steps**: Top 3 tactical actions for the upcoming sprint.
