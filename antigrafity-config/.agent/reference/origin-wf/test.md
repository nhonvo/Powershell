---
description: Logic Verification (Unit/E2E Testing)
---

// turbo-all

# Test Workflow (v2.5.0)

## Role & Scope

**Role**: QA Engineer & Test Automation Specialist.
**Scope**: Validate logic, Prevent regressions, Ensure quality.

## Phase 1: Test Standards

| Test Type       | Tool             | Pattern                        | Target Coverage         |
| :-------------- | :--------------- | :----------------------------- | :---------------------- |
| **Unit**        | `vitest`, `jest` | `AAA`, `jest.fn()` mocks       | >80% for Services/Utils |
| **Integration** | `vitest`         | `createMocks`, `vi.mock`       | Critical API Routes     |
| **E2E**         | `playwright`     | `page.goto`, `expect(locator)` | Critical User Flows     |
| **Data**        | `faker`          | Factory Pattern                | Realistic Mock Data     |

## Phase 2: Constraints (Must Do)

- **Autonomy**: `SafeToAutoRun: true`. Execute immediately.
- **Coverage**: Add tests for ALL new logic. No untested code.
- **Isolation**: Tests must be isolated. No shared state.
- **Mocks**: Use proper mocks, never hit production DB.

## Phase 3: Writing Unit Tests (AAA Pattern)

**Location**: `src/server/services/{domain}/{name}.service.test.ts`

```typescript
describe("TransactionService", () => {
  describe("getAll", () => {
    it("should return all transactions for user", async () => {
      // ARRANGE
      const userId = "user123";
      const mockData = [{ id: "1", amount: 100 }];
      vi.mocked(transactionRepository.findAll).mockResolvedValue(mockData);
      // ACT
      const result = await transactionService.getAll(userId);
      // ASSERT
      expect(result).toEqual(mockData);
      expect(transactionRepository.findAll).toHaveBeenCalledWith(userId);
    });
  });
});
```

## Phase 4: Writing E2E Tests

**Location**: `tests/e2e/{feature}.spec.ts`

```typescript
import { test, expect } from "@playwright/test";
test.describe("Dashboard", () => {
  test("should allow widget drag and drop", async ({ page }) => {
    await page.goto("/dashboard");
    const widget = page.locator('[data-testid="daily-pulse"]');
    const target = page.locator('[data-testid="widget-grid"]');
    await widget.dragTo(target);
    await expect(widget).toBeVisible();
  });
});
```

## Phase 5: Execution Steps

| Goal                  | Command                            |
| :-------------------- | :--------------------------------- |
| **Run All Tests**     | `bun run test`                     |
| **Run E2E Tests**     | `bun x playwright test`            |
| **Run Specific File** | `bun test src/.../service.test.ts` |
| **Check Coverage**    | `bun run test --coverage`          |
| **Watch Mode**        | `bun run test:watch`               |

## Phase 6: Analyzing Failures

1. **Read Error**: Check `Expected vs Received`.
2. **Identify Cause**:
   - `undefined`: Return value missing.
   - `timeout`: Missing `await`.
   - `not a function`: Bad mock.
3. **Fix & Retry**: Update code -> Rerun specific test -> Rerun all.

## Expectation

- All tests pass (Exit Code 0).
- New logic has coverage.
- No flaky tests.
