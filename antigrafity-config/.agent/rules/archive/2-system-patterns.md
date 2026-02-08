---
trigger: model_decision
description: Defines mandatory Security Protocols (OWASP), UI/Design System (Tailwind/Shadcn), AI Integration logic, and Database Seeding strategies.
---

# System Patterns: Security, UI/UX, AI, Seeding

## Authentication & Authorization

- **Mechanism**: Custom JWTs stored in strict `HttpOnly`, `Secure`, `SameSite` cookies.
- **Validation**: Verify tokens at Proxy/Middleware. In API routes, use:

```typescript
const { userId, role } = await getAuthenticatedContext(req);
```

- **RBAC**:
  - **User**: Can only access own data.
  - **Admin**: Can access all data + system settings.

## Multi-Tenancy (CRITICAL)

**Ironclad Rule**: `userId` is the only Source of Truth.

- **Isolation**: Every DB operation (Find, Update, Delete) **MUST** filter by `userId`.
- **Constraint**: Repositories must accept `userId` as a top-level parameter.

  ```typescript
  // ✅ GOOD
  async findAll(userId: string) { return Model.find({ userId }); }

  // ❌ BAD
  async findAll() { return Model.find({}); }
  ```

## Data Validation & Sanitization

- **Input**: Validate ALL Body/Query params using **Zod** schemas (`src/shared/schemas`).
- **Sanitization**: Strip unknown fields to prevent NoSQL Injection.
- **Output**: Never leak internals. Use `jsonSuccess(data)` or `jsonError(msg)`.

## Security Constraints

| Threat      | Defense                                          |
| :---------- | :----------------------------------------------- |
| **CSRF**    | Token validation on all mutating requests.       |
| **XSS**     | HttpOnly cookies + React escaping.               |
| **Leakage** | Mask 500 errors. No stack traces in response.    |
| **Logging** | Log failed auth attempts (401/403) with context. |

## API Response Standard

```typescript
type ApiResponse<T> =
  | { success: true; data: T; error?: never }
  | { success: false; data?: never; error: { code: string; message: string } };
```

---

# UI/UX & Design System

## Design Language: Premium Glassmorphism

- **Surface**: `bg-white/80` (Light) / `bg-gray-900/80` (Dark) + `backdrop-blur-xl`.
- **Border**: `border-white/20` (Light) / `border-gray-800/50` (Dark).
- **Shadow**: `shadow-2xl` for floating elements, `shadow-sm` for cards.
- **Feedback**: Interactive elements must provide haptic-like visual feedback.

## Motion & Animation (Mandatory)

We use `framer-motion` for all interactions.

- **Hover**: `whileHover={{ scale: 1.02 }}` (Cards) or `y: -2`.
- **Tap**: `whileTap={{ scale: 0.98 }}`.
- **Entrance**: Use `AnimatePresence` + Staggered Fade In (no jarring flashes).
- **Charts**: `recharts` with `duration={1000}` and `easing="ease-out"`.

## Typography & Components

- **Stack**: Tailwind CSS v4 + `shadcn/ui`.
- **Text**:
  - **Headers**: Bold, `tracking-tight`, Font 'Executive' (Outfit).
  - **Data**: Monospace for financial figures.
- **Mandatory Standard Components**:
  - **Wrappers**: `PageLayout` (Layout, Header, Breadcrumbs).
  - **Filtering**: `QuickSelect` (Date ranges).
  - **Presentation**: `ComponentCard` (Standard headers/content).
  - **Visualization**: `DashboardChartContainer` (Charts with tabs).
- **Empty States**: Never show blank space. Use dashed borders, icons, and CTAs.
- **Loading**: Use Shimmer Skeletons, never generic spinners for main content.

## Responsive Strategy (Desktop First)

- **Breakpoints**: High priority on `lg` and `xl` (Dashboard is data-heavy).
- **Adaptation**:
  - Sidebar -> Drawer on Mobile (`sm`, `md`).
  - Grids (`grid-cols-3`) -> Stack (`grid-cols-1`).

## Theming

- **Dark Mode**: Mandatory support via `dark:` class variants.
- **Semantic Colors**:
  - **Primary**: Indigo (`brand`)
  - **Success**: Emerald (`income`)
  - **Success**: Emerald (`income`)
  - **Destructive**: Rose (`expense`)

## Dashboard Layout Standards (12-Column System)

The standard dashboard page layout (proven in EMN feature) uses a 12-column grid to maximize screen real estate on large displays.

### Grid Ratios

- **Sidebar (Info/Nav)**: `col-span-12 xl:col-span-4 2xl:col-span-3`
- **Main (Charts/Tables)**: `col-span-12 xl:col-span-8 2xl:col-span-9`

### Overflow Protection

**MANDATORY**: All Tables and Lists must be wrapped to prevent layout blowout.

```tsx
<div className="overflow-x-auto min-w-0">
  <TableComponent />
</div>
```

### Component Spacing

- **Container**: `space-y-6 md:space-y-8`
- **Grid Gap**: `gap-6 md:gap-8` (Consistent rhythm)

---

# AI System & Agentic Flows

## Architecture Layers

The AI system (`src/server/services/ai/`) uses a **Grounded RAG** approach to ensure accuracy and privacy.

| Layer                 | Component              | Logic/Model                    | Usage                              |
| :-------------------- | :--------------------- | :----------------------------- | :--------------------------------- |
| **1. Categorization** | `CategorizationEngine` | Rule -> Fuzzy -> Gemini Flash  | Auto-categorize transactions.      |
| **2. Forecasting**    | `ForecastService`      | Linear Regression / Rule-based | Cash flow checking, debt snowball. |
| **3. Agentic Chat**   | `AgentService`         | Gemini 1.5 Pro                 | Natural Language Interface.        |

## Categorization Logic

1. **Tier 1 (Exact)**: Matches user-defined regex rules (100% confidence).
2. **Tier 2 (Fuzzy)**: Matches keyword similarity (~80% confidence).
3. **Tier 3 (AI)**: Sends sanitized description (No PII) to LLM.
4. **Feedback Loop**: User corrections allow system to learn (updates Tier 1 rules).

## Generative UI Pattern

The Chatbot does NOT render HTML/Markdown. It returns JSON instructions for the Frontend.

**Flow**:

1. User: "Show my spending in pie chart."
2. Agent: Calls `generate_visualization` tool.
3. Response: `{ type: 'pie', data: [...] }`.
4. Frontend (`AIVisualization`): Renders interactive Rechart component.

## Tool Suite & Privacy

- **Isolation**: Tool execution is strictly scoped to `userId`.
- **Policy**: Financial data is used for **grounding only** (context window). NEVER used for model training.

**Available Tools**:

- `generate_visualization`: Create charts.
- `search_transactions`: Semantic search.
- `system_navigation`: Route client to pages.

---

# Database Seeding & Data Flows

## Centralized Seed Service

All seeding operations MUST go through `SeedService` (Singleton) at `src/server/services/data/seed.service.ts`. Never manipulate DB directly.

## Seeding Strategies

| Method            | Payload                       | Use Case                                                    |
| :---------------- | :---------------------------- | :---------------------------------------------------------- |
| **JSON Seeding**  | `{ action: "seedFromJSON" }`  | System Init, CI/CD. Source: `seed-data.json`.               |
| **Excel Seeding** | `{ action: "seedFromExcel" }` | Migration. Supports Bank Format (Row 8+) or Backup (Row 0). |
| **Demo Data**     | `{ action: "seedDemo" }`      | Generates 6 months of realistic data + Daily Pulse.         |
| **Widgets**       | `{ action: "seedWidgets" }`   | Restores default dashboard layouts.                         |

## Smart Data Features

- **Daily Pulse**: Demo data MUST include at least one transaction on the _current day_ to ensure the dashboard looks alive.
- **Auto-Init**: New users receive empty `dashboardLayout`. Frontend detects this and applies default template.
- **Localization**: Hints `locale` (en/vi) to generate culturally relevant categories/amounts.

## Supported Models (11 Total)

Users, Transactions, Smart Categories, Rules, Budgets, Goals, Tags, Recurring, Bills, Assets, Debts.

## Access Control

- **Endpoint**: `POST /api/v1/admin/seed`
- **Admin**: Can seed any user.
- **User**: Can only reset their own "Demo Data" sandbox.
- **Constraint**: Always filter operations by `targetUserId`.

---

# Business Flows & Logic

## Data Digestion Flow (Import -> Compute -> Store)

1. **Ingest**: User uploads Excel (Bank or Backup format).
2. **Parse & Validate**: Zod validation. Check for duplicates via `transaction_code`.
3. **Compute**:
   - **Categorize**: Apply Rules -> NLP -> Fallback.
   - **Link**: Attach transactions to Goals/Budgets.
4. **Store**: Save via Repository -> MongoDB.
5. **Refresh**: Invalidate Query Cache -> Update Dashboard.

## Currency Handling

- **Storage**: Store as **absolute numbers** (e.g., `150000`).
- **Computation**: Perform all math on raw values.
- **Display**: use `Intl.NumberFormat` via `CurrencyContext` only at the last mile (UI).

## Data Integrity (3-Tier Mapping)

Enforce strict separation: `Persistence → Mapper → Domain`.

- **Persistence**: Mongoose Document (`_id: ObjectId`).
- **Domain**: Pure Interface (`_id: string`, no Mongoose methods).
- **DTO**: Serialized JSON for API.
- **Rule**: Never bypass the Mapper layer.

## Smart Categories (Learning Loop)

1. **Learn**: User manually updates a category on a transaction.
2. **Persist**: System asks to create a `CategorizationRule` (Regex/Keyword).
3. **Apply**: Retroactively apply this rule to unverified transactions.

## System Resilience

- **Null Safety**: Always handle empty arrays/nulls. Use optional chaining `?.`.
- **Dates**: Use ISO 8601 for transport. Use `date-fns` for display.
- **Transactions**: Use MongoDB Sessions/Transactions for multi-document writes (seeding/import).

---
