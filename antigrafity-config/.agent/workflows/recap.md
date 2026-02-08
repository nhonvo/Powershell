---
description: Restore project context and session state
---

# /recap (Finance Efficiency)

**Objective**: Quickly reload where we left off.

## Workflow

1. **Load State**: Read `.brain/session.json` and `.brain/handover.md`.
2. **Scan Code**: Run `git status` to see uncommitted work.
3. **Scan Design**: Check `plans/` for the latest active phase.
4. **Summary**: Remind the User of the current Feature, Task, and Blockers.
5. **Guidance**: Run `/next` to suggest the start point.
