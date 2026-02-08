---
description: Design feature + Freeze context
---

// turbo-all

# /plan (Finance Efficiency)

**Objective**: Plan a feature using Aura Express & .NET Finance patterns while minimizing token drift.

## Workflow

// turbo-all

1. **Auto-Scan**: IMMEDIATELY run `python scripts/bm25_search.py "<feature_topic>"` using the topic from the user request.
2. **Retrieve Context**:
   - Check `documentation/technical/` for categorization or design rules.
   - Use search results to inform the plan.
3. **Propose MVP**:
   - Define data model (TS).
   - Define UI components (Aura Express).
   - Propose 3-5 implementation phases.
4. **Freeze Specs**: Create `plans/[date]-[feature]/plan.md`.

**Next**: /code phase-01
