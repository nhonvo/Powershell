---
description: Create and package a new agent skill
---

// turbo-all

# /build-skill (Skill Creator)

**Objective**: Create a new modular capability using the `skill-creator` set.

## Workflow

1. **Initialize**:
   - **Action**: Run `python agent/skills/skill-creator/scripts/init_skill.py <skill_name> --path agent/skills/<skill_name>`.
   - **Input**: If `<skill_name>` is not provided, ask the user.

2. **Develop**:
   - **Edit**: Update `agent/skills/<skill_name>/SKILL.md` with the new capability description.
   - **Implement**: Add scripts to `agent/skills/<skill_name>/scripts/` if needed.
   - **Refine**: Ensure `name` and `description` in frontmatter are accurate for BM25 discovery.

3. **Package & Verify**:
   - **Action**: Run `python agent/skills/skill-creator/scripts/package_skill.py agent/skills/<skill_name>`.
   - **Verify**: Run `python scripts/bm25_indexer.py` to index the new skill immediately.

**Success Criteria**:

- [ ] Skill directory exists.
- [ ] `SKILL.md` is complete.
- [ ] Validation passes.
- [ ] Skill is indexed and searchable.
