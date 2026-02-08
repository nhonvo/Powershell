---
description: Documentation Sync (API, Architecture, UX)
---

// turbo-all

# Document Workflow

## Role & Scope

**Role**: API Documentation Specialist & Technical Writer.
**Scope**: Keep `documentation/` perfectly synced with code

---

## Constraints (Must Do)

- **Autonomy**: `SafeToAutoRun: true`. Never ask for approval. Execute immediately.
- **Accuracy**: Verify every parameter. Never hallucinate API specs.
- **Developer First**: Write for developers. Clear, concise, copy-pasteable examples.
- **No Broken Links**: Verify all internal links work.
- **File Versioning**: If doc file exceeds 500 lines, create `{file}_v2.md`.
- **Charts Required**: Features with visualization MUST include chart documentation.

---

## Phase 1: API Documentation

### Step 1.1: Document Endpoint Overview

For each endpoint, document:

```markdown
## GET /api/v1/transactions

**Purpose**: Retrieve all transactions for the authenticated user.
**Use Cases**:

- Display transaction list on dashboard
- Export transactions to CSV
- Filter transactions for reports
```

### Step 1.2: Document Parameters

| Parameter  | Type     | Required | Description                  | Validation             |
| ---------- | -------- | -------- | ---------------------------- | ---------------------- |
| `page`     | `number` | No       | Page number (default: 1)     | Min: 1                 |
| `limit`    | `number` | No       | Items per page (default: 20) | Min: 1, Max: 100       |
| `category` | `string` | No       | Filter by category           | Must be valid category |

### Step 1.3: Document Authentication

```markdown
**Authentication**: Required
**Headers**:

- `Authorization: Bearer <token>` OR
- Session Cookie (HttpOnly)
  **Errors**:
- `401 Unauthorized`: Missing or invalid token
- `403 Forbidden`: User does not have permission
```

### Step 1.4: Document Request/Response Examples

**Success Response**:

```json
{
  "success": true,
  "data": [
    {
      "id": "abc123",
      "description": "Grocery Store",
      "amount": 50.0,
      "category": "Food"
    }
  ],
  "meta": {
    "page": 1,
    "limit": 20,
    "total": 150
  }
}
```

**Error Response**:

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid category provided",
    "details": {
      "field": "category",
      "received": "invalid",
      "expected": ["Food", "Transport", "Entertainment"]
    }
  }
}
```

### Step 1.5: Document Rate Limiting

```markdown
**Rate Limits**:

- Standard Users: 100 requests/minute
- Premium Users: 1000 requests/minute
  **Headers in Response**:
- `X-RateLimit-Limit`: Max requests allowed
- `X-RateLimit-Remaining`: Requests remaining
- `X-RateLimit-Reset`: Unix timestamp when limit resets
```

### Step 1.6: Document Versioning

```markdown
**Current Version**: v1
**Deprecation Policy**:

- Deprecated endpoints will have `X-Deprecated: true` header.
- 6 months notice before removal.
- Migration guide provided for breaking changes.
```

---

## Phase 2: Project Documentation

### Step 2.1: Update ARCHITECTURE.md

**When to update**:

- New architectural pattern introduced.
- New service/repository added.
- New external integration added.
  **What to add**:

```markdown
## New Pattern: Event-Driven Notifications

### Overview

We now use an event bus for notifications instead of direct calls.

### Flow

1. Service emits event: `eventBus.emit('transaction.created', data)`
2. Listeners process: `eventBus.on('transaction.created', sendNotification)`

### Files

- `src/lib/event-bus.ts` - Core event bus
- `src/server/listeners/` - Event listeners
```

### Step 2.2: Update DATABASE_SEEDING.md

**When to update**:

- New model added.
- New seed data format.
- New seeding method.
  **What to add**:

```markdown
## New Model: Notifications

### Schema

- `userId`: ObjectId (required)
- `message`: String (required)
- `read`: Boolean (default: false)
- `createdAt`: Date

### Seed Data

Located at: `src/server/db/seeds/notifications.json`
```

### Step 2.3: Update ROADMAP.md

**When to update**:

- Feature completed: Mark `[x]`
- Feature started: Mark `[/]`
- New feature planned: Add to backlog

```markdown
- [x] Phase 35: Transaction Import ✅
- [/] Phase 36: Notifications System 🚧
- [ ] Phase 37: Mobile App
```

### Step 2.4: Update DEVELOPMENT_GUIDE.md

**When to update**:

- New environment variable added.
- New command added.
- New setup step required.

```markdown
## New Environment Variable

### NOTIFICATION_SERVICE_URL

- **Required**: Yes (Production)
- **Default**: `http://localhost:3001`
- **Description**: URL of the notification microservice
```

---

## Phase 3: UX/UI Documentation

### Step 3.1: Document New Components

**Location**: `documentation/guides/{ComponentName}.md`

````markdown
# GoalProgressCard

## Purpose

Displays progress towards a financial goal with visual progress bar.

## Props

| Prop     | Type         | Required | Description  |
| -------- | ------------ | -------- | ------------ |
| `goal`   | `IGoal`      | Yes      | Goal object  |
| `onEdit` | `() => void` | No       | Edit handler |

## Usage

\```tsx
<GoalProgressCard goal={goal} onEdit={() => openModal()} />
\```

## Design Tokens

- Background: `bg-white/80 backdrop-blur-xl`
- Border: `border-white/20`
- Progress Bar: `bg-gradient-to-r from-emerald-400 to-emerald-600`
````

### Step 3.2: Update Design System (4-ui-ux.md)

**When to update**:

- New color added.
- New animation pattern.
- New spacing convention.

---

## Phase 4: Migration Guides

### When to Create

- API breaking change (param removed, response shape changed).
- Database schema migration.
- Deprecated feature removal.

### Template

````markdown
# Migration Guide: v1 to v2

## Breaking Changes

### 1. Transaction Response Shape Changed

**Before (v1)**:
\```json
{ "amount": 50.00 }
\```
**After (v2)**:
\```json
{ "amount": { "value": 50.00, "currency": "USD" } }
\```

### Migration Steps

1. Update your API client to handle the new shape.
2. Update any code that accesses `transaction.amount` directly.
3. Test thoroughly.

### Affected Endpoints

- `GET /api/v1/transactions`
- `GET /api/v1/transactions/:id`
````

---

## Quickstart Guide Template

For new features, include:

````markdown
# Quickstart: Feature Name

## 1. Setup

Ensure you have the latest dependencies:
\```bash
bun install
\```

## 2. Configuration

Add to your `.env`:
\```
NEW_FEATURE_ENABLED=true
\```

## 3. Basic Usage

\```typescript
import { useNewFeature } from '@/features/new-feature/hooks';
function MyComponent() {
const { data } = useNewFeature();
return <div>{data}</div>;
}
\```

## 4. Common Use Cases

### Use Case 1: Basic Display

...

### Use Case 2: With Filtering

...

## 5. Troubleshooting

### Error: Feature not available

- Ensure `NEW_FEATURE_ENABLED=true` in `.env`
- Restart the dev server
````

---

## Phase 5: Charts & Data Visualization Documentation

### Step 5.1: Document Chart Type

For each chart in a feature:

```markdown
## Spending by Category Chart

**Type**: Pie Chart (recharts)
**Data Source**: `GET /api/v1/analytics/category-breakdown`
**Props**:
| Prop | Type | Description |
|---|---|---|
| `data` | `CategoryData[]` | Array of category totals |
| `period` | `'week' | 'month' | 'year'` | Time period |
```

### Step 5.2: Document Chart Configuration

````markdown
## Configuration

```tsx
<ResponsiveContainer width="100%" height={300}>
  <PieChart>
    <Pie
      data={data}
      dataKey="amount"
      nameKey="category"
      cx="50%"
      cy="50%"
      outerRadius={100}
      fill="#8884d8"
    >
      {data.map((entry, index) => (
        <Cell key={index} fill={COLORS[index % COLORS.length]} />
      ))}
    </Pie>
    <Tooltip content={<CustomTooltip />} />
    <Legend />
  </PieChart>
</ResponsiveContainer>
```
````

### Step 5.3: Document Color Palette

```markdown
## Color Palette

| Category   | Color  | Hex       |
| ---------- | ------ | --------- |
| Income     | Green  | `#10b981` |
| Expense    | Red    | `#ef4444` |
| Savings    | Blue   | `#3b82f6` |
| Investment | Purple | `#8b5cf6` |
```

### Step 5.4: Document Animation

```markdown
## Animation

- **Duration**: 1000ms
- **Easing**: `ease-out`
- **Entrance**: Fade in with stagger
```

---

## Expectation

- All documentation is accurate and up-to-date.
- No broken internal links.
- API docs include all endpoints with examples.
- Code examples are copy-pasteable and work.
- Chart documentation includes type, data source, config, and colors.
- Good documentation prevents support tickets.

---

## Extended Guidelines (Combined)

// turbo-all

# Document Workflow (v2.5.0)

**Central Source of Truth**
`documentation/DOCUMENTATION.md` (v2.5.0).
Refer to Sections 1-15 covering everything from Product Vision to Testing Patterns.
**Update Triggers**

- **New Feature/API**: Update Checklist (Sec 7) and Schemas (Sec 13).
- **Architecture Change**: Update Architecture (Sec 2).
- **UI Change**: Update Design (Sec 8) and Widgets (Sec 14).
- **Complex Fix**: Log in Knowledge Journal (Sec 9).
  **Documentation Standards**
- **Completeness**: All API endpoints must have auth, params, and response examples.
- **Accuracy**: No broken internal links.
- **Usability**: Copy-pasteable code examples.
- **Visuals**: Charts must document their color palette and data source.
