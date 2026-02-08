---
description: Semantic Git staging and release
---

// turbo-all

# /commit (Finance Efficiency)

**Objective**: Quality-gate code and release to the repository.

## Workflow

1. **Quality Check**: Run `npm run lint` or `dotnet build`.
2. **Analysis**: Check `git status` for modified files.
3. **Staging**: `git add .`
4. **Release**: `git commit -m "<type>(scope): <desc>"` (Closes: #TaskID)

**Next**: /save-brain
