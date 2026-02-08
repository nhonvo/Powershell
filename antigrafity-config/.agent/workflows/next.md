---
description: What to do next?
---

# /next (Finance Efficiency)

**Objective**: Determine current project state and suggest the optimal path.

## Workflow

1. **Analyze**:
   - Check `plans/` for active phases.
   - Check `git status` for modified files.
2. **Suggest**:
   - If plan is done: `/code phase-XX`.
   - If code is changed: `/verify`.
   - If verified: `/commit`.
   - If stuck: `/debug`.

## Tip

Run `python scripts/bm25_search.py "ROADMAP"` to see the big picture.
