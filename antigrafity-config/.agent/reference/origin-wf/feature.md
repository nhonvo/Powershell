---
description: End-to-End Feature Implementation
---

// turbo-all

# Feature Workflow

## Role & Scope

**Role**: Full-Stack Architect.
**Scope**: Plan -> Build -> Polish complete features from scratch.

## Constraints (Must Do)

- **Autonomy**: `SafeToAutoRun: true`. Never ask for approval. Execute immediately.
- **No `globals.css`**: Create local CSS modules or use `components/ui`. Never modify `src/app/globals.css`.
- **No `any`**: Use strict interfaces from `src/shared/types`. If interface doesn't exist, create it.
- **FSD Architecture**: Strictly follow Feature-Sliced Design layers. Logic in `hooks/` or `services/`, UI in `components/`.
- **Mappers**: Transform all DB Documents via `src/server/mappers`. Never leak Mongoose types to frontend.
- **No Placeholders**: Never use `// ... rest of code`. Always output complete, functional code blocks.
- **Null Safety**: ALWAYS handle `null`, `undefined`, `0`, empty arrays. Use optional chaining (`?.`) and nullish coalescing (`??`).
- **Seeding Required**: Every new feature MUST include demo seed data in `src/server/db/seeds/` or `seed-data.json`.
- **File Splitting**: If a component exceeds 200 lines, split into sub-components. If a service grows too large, create v2 file.
- **Large Files**: If documentation file exceeds 500 lines, create `{file}_v2.md`.

## Phase 0: Reuse Strategy (MANDATORY)

1. **Audit Existing Components**:
   - `src/components/ui` (Atoms)
   - `src/components/common` (Molecules like Cards, Filters)
   - `src/components/layout` (Page wrappers)
2. **Audit Existing Hooks**:
   - `src/hooks` and `src/lib` (Utils)
3. **Strict Reuse Rules**:
   - **Wrappers**: ALWAYS use `PageLayout` for top-level pages.
   - **Filters**: ALWAYS use `QuickSelect` for date ranges.
   - **Cards**: ALWAYS use `ComponentCard` or standard `Card` + `Glass` variants.
   - **Charts**: ALWAYS wrap in `DashboardChartContainer`.
   ## Phase 1: Planning (Understand the Problem)
   ### Step 1.1: Analyze Requirements
   - Read the user's request carefully.
   - Identify the **core feature** being requested.

- List all **edge cases** that need to be handled (e.g., empty state, error state, loading state).
- Identify any **breaking changes** that require user review.

### Step 1.2: Design Database Schema (If Needed)

- Determine if a new Mongoose Model is required.
- Define the schema fields with types (`string`, `number`, `Date`, `Types.ObjectId`).
- Identify indexes for query performance.
- Example:
  ```typescript
  // src/server/db/models/{Entity}.ts
  const EntitySchema = new Schema({
    userId: { type: Schema.Types.ObjectId, required: true, index: true },
    name: { type: String, required: true },
    createdAt: { type: Date, default: Date.now },
  });
  ```
  ### Step 1.3: Plan API Endpoints
  - Define the RESTful routes:
  - `GET /api/v1/{domain}/{entity}` - List all.
  - `GET /api/v1/{domain}/{entity}/[id]` - Get one.
  - `POST /api/v1/{domain}/{entity}` - Create.
  - `PUT /api/v1/{domain}/{entity}/[id]` - Update.
  - `DELETE /api/v1/{domain}/{entity}/[id]` - Delete.
  - Define request/response shapes using Zod schemas.
  ### Step 1.4: Sketch Component Hierarchy
  - Define the UI component tree:
    ```
    features/{domain}/components/
    ├── {Entity}Page.tsx  (Main container)
    ├── {Entity}List.tsx  (List view)
    ├── {Entity}Card.tsx  (Individual item)
    ├── {Entity}Modal.tsx (Create/Edit form)
    └── {Entity}Empty.tsx (Empty state)
    ```
  ````
    ### Step 1.5: Create Artifact (Optional for Complex Tasks)
  - Create `task.md` with checklist:
    ```markdown
  - [ ] Create Model
  - [ ] Create Mapper
  - [ ] Create Repository
  - [ ] Create Service
  - [ ] Create API Route
  - [ ] Create React Hook
  - [ ] Create UI Components
  - [ ] Verify Types & Lint
  ````
  ## Phase 2: Backend Implementation (Bottom-Up)
  ### Step 2.1: Create Mongoose Model
  - **File**: `src/server/db/models/{Entity}.ts`
  - **Requirements**:
  - Use `Types.ObjectId` for IDs.
  - Apply `toJSON` plugin for clean serialization.
  - Add proper indexes.
- **Example**:
  ```typescript
  import mongoose, { Schema, Document, Types } from 'mongoose';
  import toJSON from '../plugins/toJSON';
    export interface I{Entity} {
    _id?: Types.ObjectId;
    userId: Types.ObjectId;
    name: string;
  }
    const {Entity}Schema = new Schema({
    userId: { type: Schema.Types.ObjectId, required: true, index: true },
    name: { type: String, required: true }
  }, { timestamps: true });
    {Entity}Schema.plugin(toJSON);
    export default mongoose.models.{Entity} || mongoose.model('{Entity}', {Entity}Schema);
  ```
  ### Step 2.2: Create Mapper
  - **File**: `src/server/mappers/{Entity}Mapper.ts`
  - **Purpose**: Transform DB Document <-> Domain Object.
- **Methods**:
  - `toDomain(doc)`: Convert Mongoose Doc to shared interface.
  - `toPersistence(data)`: Convert shared interface to DB input.
- **Example**:
  ```typescript
  export class {Entity}Mapper {
    toDomain(doc: I{Entity}Document): I{Entity} {
return {
  id: doc._id.toString(),
  userId: doc.userId.toString(),
  name: doc.name
};
    }
  }
  export const {entity}Mapper = new {Entity}Mapper();
  ```
  ### Step 2.3: Create Repository
  - **File**: `src/server/repositories/{entity}.repository.ts`
  - **Pattern**: Singleton. Must call `await connect()` in every method.
- **Example**:
  ```typescript
  import { connect } from '@/server/db/client';
  import {Entity}Model from '@/server/db/models/{Entity}';
  import { {entity}Mapper } from '@/server/mappers/{Entity}Mapper';
    class {Entity}Repository {
    async findAll(userId: string) {
await connect();
const docs = await {Entity}Model.find({ userId }).lean();
return {entity}Mapper.toDomainArray(docs);
    }
  }
    export const {entity}Repository = new {Entity}Repository();
  ```
  ### Step 2.4: Create Service
  - **File**: `src/server/services/{domain}/{entity}.service.ts`
  - **Purpose**: Business logic, validation, orchestration.
- **Example**:
  ```typescript
  import { {entity}Repository } from '@/server/repositories/{entity}.repository';
    class {Entity}Service {
    async getAll(userId: string) {
return {entity}Repository.findAll(userId);
    }
  }
    export const {entity}Service = new {Entity}Service();
  ```
  ### Step 2.5: Create API Route
  - **File**: `src/app/api/v1/{domain}/{entity}/route.ts`
  - **Requirements**:
  - Use `getAuthenticatedContext(req)` for auth.
  - Use Zod for request body validation.
  - Use `jsonSuccess()` and `jsonError()` for responses.
- **Example**:
  ```typescript
  import { NextRequest } from 'next/server';
  import { getAuthenticatedContext } from '@/lib/api-context';
  import { {entity}Service } from '@/server/services/{domain}/{entity}.service';
  import { jsonSuccess, jsonError } from '@/lib/api-response';
    export async function GET(req: NextRequest) {
    try {
const { userId } = await getAuthenticatedContext(req);
const data = await {entity}Service.getAll(userId);
return jsonSuccess(data);
    } catch (error) {
return jsonError('Failed to fetch', 500);
    }
  }
  ```
  ## Phase 3: Frontend Implementation (Top-Down)
  ### Step 3.1: Create React Query Hook
  - **File**: `src/features/{domain}/hooks/use{Entity}.ts`
  - **Pattern**: Use `useQuery` for reads, `useMutation` for writes.
- **Example**:
  ```typescript
  import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
  import { fetcher } from '@/lib/fetcher';
    export function use{Entity}s() {
    return useQuery({
queryKey: ['{entity}s'],
queryFn: () => fetcher('/api/v1/{domain}/{entity}')
    });
  }
  ```
  ### Step 3.2: Create UI Components
  - **File**: `src/features/{domain}/components/{Entity}Page.tsx`
  - **Design Requirements**:
  - **Glassmorphism**: `bg-white/80 backdrop-blur-xl border-white/20`.
  - **Motion**: `whileHover={{ scale: 1.02 }}` via `framer-motion`.
  - **Loading State**: Use Skeleton components.
  - **Error State**: Show error message with retry button.
  - **Empty State**: Dashed border, icon, CTA button.
- **Modules**:
  - `PageLayout`: Consistent header, breadcrumbs, actions.
  - `QuickSelect`: Unified date filtering.
  - `Emn*(Components)`: Follow the modular pattern from Electric Utility feature.
- **Example**:
  ```tsx
  'use client';
  import { QuickSelect } from '@/features/analytics/components/filters/QuickSelect';
  import PageLayout from '@/components/layout/PageLayout';
    export function {Entity}Page() {
    return (
<PageLayout
  pageTitle="{Entity} Manager"
  action={<QuickSelect ... />}
>
  ...
</PageLayout>
    );
  }
  ```
  ### Step 3.3: Handle Loading, Error, Empty States
  - **Loading**: Use `Skeleton` component from `shadcn/ui`.
  - **Error**: Show message + `Retry` button.
- **Empty**: Dashed border, centered icon, "Add First" CTA.

### Step 3.4: Ensure Mobile Responsiveness

- Use `lg:` breakpoint as primary.
- Grid: `grid-cols-1 md:grid-cols-2 lg:grid-cols-3`.
- Sidebar: Collapse to Drawer on mobile.

### Step 3.5: Resilience & Polish (The EMN Standard)

1. **Strict Overflow Protection**: Wrap ALL tables/grids in `<div className="overflow-x-auto min-w-0">`.
2. **Graceful Failures**:
   - If API key is missing -> Show Mock Data or "Demo Mode" badge.
   - If data is empty -> Show `EmptyState` component, never blank space.
3. **Skeleton Loading**:
   - Match specific component height (e.g., `h-[400px]`).
   - Use `animate-pulse` classes for text lines.
4. **Formatting**:

   - Dates: uses `date-fns` with `format(date, 'dd MMM yyyy')`.
   - Currency: use `Intl.NumberFormat` for currency.

   ## Phase 4: Polish & Verify

   ### Step 4.1: Run Type Check

   ```bash
   bun x tsc --noEmit
   ```

   ```

   ```

- If errors exist, FIX THEM immediately.

### Step 4.2: Run Lint

```bash
bun run lint
```

- If errors exist, FIX THEM immediately.

### Step 4.3: Optimize Performance

- Wrap heavy list items in `React.memo`.
- Use `useCallback` for inline handlers passed as props.
- Use `useMemo` for expensive calculations.

### Step 4.4: Add Accessibility

- Add `aria-label` to interactive elements.
- Ensure keyboard navigation works (Tab, Enter, Escape).
- Test with screen reader if possible.

### Step 4.5: Update Documentation

- Update `ROADMAP.md` (mark as completed).
- Update `ARCHITECTURE.md` if new patterns introduced.

## Naming Conventions

| Entity  | Pattern    | Example    |
| ------------------- | ---------------- | ---------------------- |
| Files (Components)  | `PascalCase.tsx` | `GoalCard.tsx`   |
| Files (Utils/Hooks) | `camelCase.ts`   | `useGoals.ts`    |
| Interfaces    | `I{Entity}`| `IGoal`    |
| API Routes    | `kebab-case`     | `/api/v1/goal-tracker` |

## Phase 5: Null Safety & Edge Cases

### Step 5.1: Handle Null/Undefined

```typescript
// BAD: Will crash if data is undefined
const total = data.reduce((sum, item) => sum + item.amount, 0);
// GOOD: Handle undefined
const total = data?.reduce((sum, item) => sum + item.amount, 0) ?? 0;
```

### Step 5.2: Handle Empty Arrays

```typescript
// BAD: Shows nothing
{
  data.map((item) => <Card key={item.id} />);
}
// GOOD: Show empty state
{
  data?.length > 0 ? (
    data.map((item) => <Card key={item.id} />)
  ) : (
    <EmptyState message="No items yet" />
  );
}
```

### Step 5.3: Handle Zero Values

```typescript
// BAD: 0 is falsy, shows fallback incorrectly
const amount = data.amount || "N/A";
// GOOD: Use nullish coalescing
const amount = data.amount ?? "N/A";
```

## Phase 6: Seeding Demo Data

### Step 6.1: Add to seed-data.json

Every new feature MUST include demo data in `src/server/db/seed-data.json`:

```json
{
  "{entities}": [
    {
"userId": "{{demoUserId}}",
"name": "Sample Entity",
"amount": 1000,
"createdAt": "2026-01-01T00:00:00Z"
    }
  ]
}
```

### Step 6.2: Update SeedService

Add new entity to `seedFromData()` method in `seed.service.ts`.