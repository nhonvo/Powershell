---
description: Advanced workflow for validating, staging, and committing changes with semantic precision and roadmap integration.
---

# Advanced Git Commit Workflow

## Role & Scope

**Role**: Release Manager / DevOps Engineer.
**Scope**: Perform quality gates, stage changes, synthesize semantic commit messages, and ensure roadmap alignment

---

## Constraints (Must Do)

- **Semantic Model**: Strictly follow [Conventional Commits](https://www.conventionalcommits.org/).
- `feat`: New feature (corresponds to a Phase/Task)
- `fix`: Bug fix (includes Lint/Type fixes)
- `refactor`: Code change that neither fixes a bug nor adds a feature (e.g., Clean Arch, strict types)
- `perf`: Performance optimization
- `style`: Cosmetic changes, formatting (no logic change)
- `docs`: Documentation updates
- `chore`: Build tasks, package updates, housekeeping
- **The "Holy Trinity" Gate**: Before committing, verify `bun run lint` and `bun run build` (which includes `tsc` check) unless it's a documentation-only change.
- **Roadmap Linking**: If the change completes a Task or Phase, the commit message body should reference it.
- **Autonomy**: `SafeToAutoRun: true` for pure Git commands.

---

## Workflow Steps

### Phase 1: Pre-Commit Quality Gate

Ensure the code is in a "Ship-it" state. If this fails, the agent must fix the issues before proceeding.
// turbo

```bash
bun run lint
bun run build
```

### Phase 2: Contextual Analysis

Identify what was actually achieved.

1. **Check Status**:
   // turbo

```bash
git status
```

1. **Analyze Diffs**: Run `git diff` on critical files to understand the "Why" and "How" of the changes.
2. **Roadmap Alignment**: Check `documentation/roadmap/ROADMAP.md` or current `task.md` to see which Phase/Task this work belongs to.

### Phase 3: Intelligent Staging

Stage changes thoughtfully.

1. **Stage All**: If the entire work set is ready.
   // turbo

```bash
git add .
```

1. **Partial Staging**: Use `git add <file>` if only specific changes should be committed.

### Phase 4: Semantic Message Synthesis

Generate a commit message that provides value to future maintainers.

- **Header**: `<type>(<scope>): <short description>` (Max 50 chars).
- **Body** (Optional but recommended for `feat`/`refactor`): Explain the problem solved and the architectural impact.
- **Footer**: Reference tasks (e.g., `Closes #36` or `Phase: Data Integrity`).
  **Examples**:
- `feat(asset): implement AssetMapper for strict type safety`
- `refactor(repo): replace .lean() with typed Mongoose documents`
- `fix(types): resolve hydration mismatch in UserDropdown`

### Phase 5: Atomic Commit

Execute the commit.
// turbo

```bash
git commit -m "<header>" -m "<body>"
```

### Phase 6: Sync & Verification

Finalize the cycle.

1. **Push**:
   // turbo

```bash
git push
```

1. **Verify**:
   // turbo

```bash
git log -1 --oneline
```

### Phase 7: Post-Commit Bookkeeping (Conditional)

If a Phase or Task was completed:

1. Update `documentation/roadmap/ROADMAP.md` (mark as done).
1. Record learnings in `documentation/logs/KNOWLEDGE_LOG.md`.

### Rationale

- **Architecture & Integrity**: By integrating the "Holy Trinity" (Lint, Types, Build) into the commit workflow, we prevent "breaking the build" and ensure only high-quality code reaches the repository.
- **Traceability**: Linking commits to the `ROADMAP.md` ensures that every change has a designated purpose and that project progress is automatically trackable.
- **Semantic Precision**: Using Conventional Commits facilitates automated changelog generation and clearer versioning.
