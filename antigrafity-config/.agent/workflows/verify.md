---
description: The Agentic Loop - Build, Verify, Document, and Index
---

// turbo-all

# ✅ Verification Workflow: "Closing the Loop"

This workflow is the final quality gate. It ensures code works, docs are updated, and the project "Brain" (BM25) is refreshed.

## Phase 1: Technical Integrity

1. **Build**: Run `npm run build` or `dotnet build`.
2. **Local Run**: Start the application locally.
3. **Browser Agent**:
   - Navigate to the new/modified feature.
   - Verify UI matches "Aura Express" specs.
   - Take screenshot for proof.
4. **Auto-Fix**: If build fails, automatically trigger `/debug` (Up to 3 times).

## Phase 2: Knowledge Synchronization

1. **Doc Update**: Update the following if impacted:
   - `documentation/api.yaml` (API changes)
   - `documentation/design-specs.md` (UI changes)
   - `documentation/ROADMAP.md` (Mark phase as complete)
2. **Lexical Indexing**:
   - **MANDATORY**: Run `python scripts/bm25_indexer.py` (Auto-run this immediately).
   - **Verification**: Run `python scripts/bm25_search.py "<feature>"` to ensure the new knowledge is discoverable.

## Phase 3: Final Handover

1. **Save Brain**: Run `/save-brain` to capture decisions and session progress.
2. **Commit**: Trigger `/commit` to push changes with semantic precision.

---

## 📈 Success Criteria

- [x] 0 Build Errors.
- [x] Browser verification successful.
- [x] `documentation/` folder is perfectly synced.
- [x] BM25 Index updated with newest chunks.
