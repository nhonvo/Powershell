---
description: React Optimization & Code Modernization
---

// turbo-all

# Refactor Workflow (v2.5.0)

## Role & Scope

**Role**: React Expert & Performance Engineer.
**Scope**: Modernize legacy code, Optimize performance, Eliminate tech debt.

## Phase 1: Principles & Targets

| Principle     | Implementation                             |
| :------------ | :----------------------------------------- |
| **Boy Scout** | Clean as you go. Fix formatting/imports.   |
| **SOLID**     | Apply patterns but prioritize readability. |
| **DRY**       | Extract duplicate logic to utilities.      |
| **YAGNI**     | Don't over-abstract.                       |

**Common Targets**:

- `any` types -> Strict Interfaces.
- Class Components -> Functional Components + Hooks.
- Large Components (>200 lines) -> Split into sub-components.
- Unnecessary Re-renders -> `useMemo`, `useCallback`, `React.memo`.

## Phase 2: React Modernization

### Step 2.1: Class to Function

```typescript
// BEFORE: Class
class MyComp extends Component {
  state = { count: 0 };
  render() { ... }
}
// AFTER: Function
function MyComp() {
  const [count, setCount] = useState(0);
  return ( ... );
}
```

### Step 2.2: Performance Tuning

| Problem                   | Solution                                                  |
| :------------------------ | :-------------------------------------------------------- |
| **Expensive Calculation** | Wrap in `useMemo(() => calc(a,b), [a,b])`.                |
| **Function Prop**         | Wrap in `useCallback(fn, [])` to prevent child re-render. |
| **Heavy Component**       | Use `React.lazy` + `Suspense`.                            |
| **Pure Component**        | Wrap in `React.memo`.                                     |

## Phase 3: Structural Refactoring

- **Repositories**: Extract direct DB calls from Services to `src/server/repositories`.
- **Mappers**: Extract data transformation to `src/server/mappers`.
- **Utils**: Move pure functions to `src/lib/utils`.

## Phase 4: Verification

1. **Baseline**: Run `bun x tsc --noEmit` BEFORE starting.
2. **Edit**: Apply atomic refactor.
3. **Verify**: Run `tsc` and `lint` AFTER editing.
4. **Test**: Ensure no behavior change (same input -> same output).

# Quick Reference Workflow (v2.5.0)

**Essential Environment**
Ensure `.env` contains: `MONGODB_URI`, `JWT_SECRET`, and `GEMINI_API_KEY`.
**File Architecture**

- **API**: `src/app/api/v1/`
- **Logic**: `src/server/` (`services`, `repositories`, `mappers`, `db`)
- **UI**: `src/features/` and `src/components/`
  **Developer Command Palette**
- **Dev**: `bun run dev`
- **Verify**: `bun x tsc --noEmit` & `bun run lint`
- **Build**: `bun run build`
- **Test**: `bun run test`
  **Design Tokens**
- **Colors**: Indigo (Brand), Emerald (Income), Rose (Expense).
- **Style**: Glassmorphism + Dark Mode required.
  **3-Tier Flow**
  `Route → Service → Repository → DB` (Always use Mappers).

## Expectation

- Zero `any` types.
- Codebase follows strict FSD architecture.
- Performance improvements documented.
- Exit Code 0 for all verifications.
