---
trigger: always_on
---

## Feature 1: Drag & Drop Dashboard

### Architecture

- **Library**: `react-grid-layout` for widget positioning.
- **State**: Layout stored in `User.settings.dashboardLayout`.
- **Persistence**: Auto-save on drop via `useMutation`.

### Rules

- **Widget Registry**: All widgets registered in `WidgetRegistry.tsx`.
- **Responsive**: Use `lg/md/sm` breakpoints for layout.
- **Snap**: Grid snaps to 12-columnlayout.
- **Collision**: Prevent widget overlap.

### Implementation

```typescript
// Save layout on change
const handleLayoutChange = useCallback((layout: Layout[]) => {
  saveLayoutMutation.mutate(layout);
}, []);
```

## Feature 2: Import/Export Flow

### Import Flow (Excel -> DB)

1. **Upload**: User selects `.xlsx` file.
2. **Parse**: `ExcelImportService.parseAndFilter()`.
3. **Validate**: Zod schema validation.
4. **Dedupe**: Check `transaction_code` for duplicates.
5. **Categorize**: Run AI categorization pipeline.
6. **Store**: Save via `TransactionRepository`.

### Export Flow (DB -> Excel)

1. **Query**: Fetch all user data.
2. **Transform**: Map to export format.
3. **Generate**: `xlsx.write()` to buffer.
4. **Download**: Trigger browser download.

### Rules

- **File Size**: Max 10MB upload.
- **Format Detection**: Support bank format (row 8) and system backup (row 0).
- **Backup Includes**: ALL collections (Transactions, Goals, Budgets, Bills, Tags, Rules).

## Feature 3: Notification System

### Architecture

- **Model**: `Notification` (userId, type, message, read, createdAt).
- **Types**: `info`, `warning`, `success`, `error`.
- **Delivery**: Real-time via polling or WebSocket (future).

### Triggers

| Event                | Notification                             |
| -------------------- | ---------------------------------------- |
| Budget threshold 80% | "You've used 80% of your Food budget"    |
| Bill due in 3 days   | "Rent is due in 3 days"                  |
| Goal milestone       | "You're 50% closer to Emergency Fund!"   |
| Import complete      | "Successfully imported 150 transactions" |

### Rules

- **Mark as Read**: On click or bulk action.
- **Clear Old**: Auto-delete after 30 days.
- **Badge Count**: Show unread count in header.

## Feature 4: Task/Goal System

### Architecture

- **Model**: `Goal` (name, targetAmount, currentAmount, deadline, priority).
- **Progress**: `(currentAmount / targetAmount) * 100`.
- **Linking**: Transactions can be linked to Goals.

### Rules

- **Auto-Link**: Transactions matching keywords link automatically.
- **Progress Update**: Recalculate on transaction create/update.
- **Deadline Alert**: Notify when deadline approaches.

## Feature 5: Charts & Visualization

### Library

- **Primary**: `recharts` (Composable, React-native).
- **Animation**: `duration={1000}`, `easing="ease-out"`.

### Patterns

- **Responsive**: Always wrap in `<ResponsiveContainer>`.
- **Tooltip**: Custom styled with Glassmorphism.
- **Colors**: Use semantic palette from Tailwind config.

### Implementation

```tsx
<ResponsiveContainer width="100%" height={300}>
  <BarChart data={data}>
    <XAxis dataKey="month" />
    <YAxis />
    <Tooltip content={<CustomTooltip />} />
    <Bar dataKey="income" fill="var(--chart-income)" />
    <Bar dataKey="expense" fill="var(--chart-expense)" />
  </BarChart>
</ResponsiveContainer>
```

## Feature 6: AI Categorization

### Pipeline

1. **Exact Match**: Check `CategorizationRule` (Regex/Keyword).
2. **Fuzzy Match**: Search `SmartCategory` keywords.
3. **AI Fallback**: Call Gemini API for unknown merchants.

### Rules

- **Learning**: User corrections create new `CategorizationRule`.
- **Confidence**: Show confidence score for AI-categorized items.
- **Batch**: Apply rules retroactively on rule creation.

## Feature 7: Recurring Transaction Detection

### Architecture

- **Service**: `RecurringDetectionService` in `src/server/services/ai/`.
- **Model**: `RecurringTransaction` (userId, description, amount, frequency, nextDate).
- **Detection**: Runs on transaction import via event listener.

### Detection Logic

```typescript
// Pattern detection algorithm
function detectRecurring(transactions: ITransaction[]): RecurringPattern[] {
  // Group by similar descriptions (fuzzy match)
  // Check for regular intervals (weekly, monthly, yearly)
  // Require at least 3 occurrences to confirm
  return patterns.filter((p) => p.occurrences >= 3 && p.confidence > 0.8);
}
```

### Rules

- **Minimum Occurrences**: 3 to suggest as recurring.
- **Confidence Threshold**: 80% pattern match.
- **User Confirmation**: Prompt user to confirm before creating rule.
- **Auto-Update**: Update next expected date after each match.

### UI Components

- `RecurringList.tsx`: Display detected patterns.
- `RecurringConfirmModal.tsx`: Confirm/reject suggestions.

## Feature 8: Subscription Tracker

### Architecture

- **Model**: `Subscription` (userId, name, amount, frequency, category, startDate, status).
- **Detection**: Auto-detect from recurring transactions matching known services.
- **Integration**: Link to `RecurringTransaction` for updates.

### Known Services Detection

```typescript
const KNOWN_SUBSCRIPTIONS = [
  { pattern: /netflix/i, name: "Netflix", category: "Entertainment" },
  { pattern: /spotify/i, name: "Spotify", category: "Entertainment" },
  { pattern: /amazon prime/i, name: "Amazon Prime", category: "Shopping" },
  { pattern: /gym|fitness/i, name: "Gym Membership", category: "Health" },
];
```

### Dashboard Widget

- **Total Monthly Cost**: Sum of all active subscriptions.
- **Upcoming Renewals**: Next 7 days.
- **Annual Projection**: 12-month cost forecast.

### Rules

- **Status**: `active`, `paused`, `cancelled`.
- **Reminder**: Notify before renewal date.
- **Cancellation Tracking**: Mark cancelled, keep history.

## Feature 9: Spending Anomaly Detection

### Architecture

- **Service**: `AnomalyDetectionService` in `src/server/services/ai/`.
- **Algorithm**: Z-Score detection (> 2 standard deviations from mean).
- **Trigger**: Runs daily via cron job or on transaction import.

### Detection Algorithm

```typescript
function detectAnomalies(
  transactions: ITransaction[],
  history: ITransaction[]
): Anomaly[] {
  const categoryStats = calculateCategoryStats(history); // mean, stdDev
  return transactions
    .filter((tx) => {
      const stats = categoryStats[tx.category];
      if (!stats) return false;
      const zScore = (tx.amount - stats.mean) / stats.stdDev;
      return Math.abs(zScore) > 2; // Flag if > 2 std deviations
    })
    .map((tx) => ({
      transaction: tx,
      severity: zScore > 3 ? "high" : "medium",
      message: `Unusual ${tx.category} expense: ${tx.amount} (avg: ${stats.mean})`,
    }));
}
```

### Notification Types

| Severity | Example                                        |
| -------- | ---------------------------------------------- |
| High     | "Unusual Food expense: $500 (avg: $50)"        |
| Medium   | "Higher than usual Transport: $100 (avg: $40)" |

### Rules

- **History Window**: 90 days for baseline.
- **Minimum Data**: Require 10+ transactions per category for detection.
- **User Dismissal**: Allow user to mark as "expected" to suppress.

## Feature 10: Multi-Currency Support

### Architecture

- **Model Extension**: Add `currency` field to `Transaction`, `Goal`, `Budget`.
- **Exchange Service**: `ExchangeRateService` with daily rates cache.
- **User Preference**: Default currency in `User.settings.currency`.

### Implementation

```typescript
// Transaction with currency
interface ITransaction {
  amount: number;
  currency: string; // ISO 4217: 'USD', 'VND', 'EUR'
  amountInBaseCurrency?: number; // Converted for aggregation
}
// Exchange rate service
class ExchangeRateService {
  async convert(amount: number, from: string, to: string): Promise<number> {
    const rate = await this.getRate(from, to);
    return amount * rate;
  }
  async getRate(from: string, to: string): Promise<number> {
    // Check cache first, then fetch from API
    const cached = await rateCache.get(`${from}_${to}`);
    if (cached) return cached;
    const rate = await fetchExternalAPI(from, to);
    await rateCache.set(`${from}_${to}`, rate, TTL_24H);
    return rate;
  }
}
```

### Display Rules

- **Show Original**: Always show amount in original currency.
- **Show Converted**: Show converted amount in parentheses if different.
- **Aggregations**: Sum in base currency for reports/charts.

## Feature 11: Budget Management

### Architecture

- **Model**: `Budget` (userId, category, limit, period, spent, alertThreshold).
- **Periods**: `weekly`, `monthly`, `yearly`.
- **Tracking**: Auto-update `spent` on transaction create/update.

### Calculation

```typescript
async function updateBudgetSpent(
  userId: string,
  category: string,
  period: string
) {
  const { startDate, endDate } = getPeriodDates(period);
  const spent = await transactionRepository.sumByCategory(
    userId,
    category,
    startDate,
    endDate
  );
  await budgetRepository.updateSpent(userId, category, spent);
}
```

### Alert Triggers

| Threshold | Action                       |
| --------- | ---------------------------- |
| 50%       | Info notification (optional) |
| 80%       | Warning notification         |
| 100%      | Alert: "Budget exceeded!"    |

### Dashboard Widget

- **Progress Bar**: Visual % used.
- **Color**: Green (< 50%), Yellow (50-80%), Red (> 80%).

## Feature 12: Bill Reminders

### Architecture

- **Model**: `Bill` (userId, name, amount, dueDate, frequency, category, isPaid, isAutoPay).
- **Scheduler**: Cron job checks daily for upcoming bills.
- **Notification**: Create notification X days before due.

### Reminder Schedule

```typescript
const REMINDER_DAYS = [7, 3, 1, 0]; // Days before due date
async function checkUpcomingBills() {
  const today = new Date();
  for (const days of REMINDER_DAYS) {
    const targetDate = addDays(today, days);
    const bills = await billRepository.findByDueDate(targetDate);
    for (const bill of bills) {
      await notificationService.create({
        userId: bill.userId,
        type: days === 0 ? "warning" : "info",
        message: `${bill.name} is due ${
          days === 0 ? "today" : `in ${days} days`
        }: $${bill.amount}`,
      });
    }
  }
}
```

### Auto-Mark Paid

- **Transaction Match**: If transaction matches bill description + amount ± 10%, mark as paid.
- **Manual Override**: User can always mark manually.

## Feature 13: Debt Tracker (Snowball/Avalanche)

### Architecture

- **Model**: `Debt` (userId, name, balance, interestRate, minimumPayment, dueDate).
- **Strategies**: Snowball (lowest balance first), Avalanche (highest interest first).
- **Projection**: Calculate payoff timeline.

### Payoff Calculator

```typescript
function calculatePayoff(
  debts: IDebt[],
  strategy: "snowball" | "avalanche",
  extraPayment: number
) {
  const sorted =
    strategy === "snowball"
      ? debts.sort((a, b) => a.balance - b.balance)
      : debts.sort((a, b) => b.interestRate - a.interestRate);
  // Simulate month-by-month payoff
  let timeline = [];
  let remaining = [...sorted];
  let month = 0;
  while (remaining.some((d) => d.balance > 0)) {
    month++;
    // Apply minimum payments + extra to first debt
    // Track interest accrued
    // Remove paid debts
  }
  return { totalMonths: month, interestSaved: calculateSavings() };
}
```

### Dashboard Widget

- **Debt-Free Date**: Projected date.
- **Interest Saved**: Compared to minimum payments only.
- **Progress Bar**: Total debt reduction.

## Feature 14: Asset Tracking

### Architecture

- **Model**: `Asset` (userId, name, type, currentValue, purchasePrice, purchaseDate).
- **Types**: `cash`, `investment`, `property`, `vehicle`, `other`.
- **Valuation**: Manual update or API integration (stocks).

### Net Worth Calculation

```typescript
function calculateNetWorth(userId: string) {
  const assets = await assetRepository.findAll(userId);
  const debts = await debtRepository.findAll(userId);
    const totalAssets = assets.reduce((sum, a) => sum
```
