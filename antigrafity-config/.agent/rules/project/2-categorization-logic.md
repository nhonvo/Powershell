---
trigger: model_decision
description: Financial Architecture & Business Rules (Categorization Pipeline)
---

# 🏗️ Finance Architecture: Categorization Engine

All development on the transaction module must strictly follow the **4-Tier Classification Pipeline**.

## 1. The 4-Tier Pipeline (Waterfall Approach)

| Priority | Tier  | Name              | Technology                             |
| -------- | ----- | ----------------- | -------------------------------------- |
| **P0**   | **1** | **Deterministic** | DB-Driven Rules (JSON Snapshot)        |
| **P1**   | **2** | **Merchant DB**   | Static Pattern Database                |
| **P2**   | **3** | **Smart ML**      | Local Keyword Weighting (User History) |
| **P3**   | **4** | **Semantic**      | Vector Embeddings                      |

## 2. Implementation Rules

- **Rule Hierarchy**: Always sort rules by Priority before application.
- **Confidence Thresholds**:
  - Confidence > 0.95: Auto-Categorize.
  - Confidence < 0.90: Set `needs_review: true`.
- **Operators**: Support `contains`, `equals`, `regex`, `startsWith`, `endsWith`.
- **Performance**: Use `CACHE_TTL` for system rules. Never call DB in loops; use batch processing.

## 3. Data Integrity

- Descriptions must be tokenized (length > 2, minus stop words).
- Every confirmed manual transaction must increment the Smart ML keyword weights.
- Never hardcode categories in logic; use Constants from `@/shared/types/category`.
