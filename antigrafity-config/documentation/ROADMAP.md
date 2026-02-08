# Product Roadmap: Finance Dashboard (V2 Active)

> **Context**: Focused on Advanced Scheduling, Task Management, and Architectural Governance.
> **Archive**: See `documentation/archive/ROADMAP_V1.md` for Phases 1-30.

## 📅 Phase 31: Advanced Scheduling (Calendar v2.0) - ✅ Completed

Transforming the calendar into a fully interactive financial command center.

- [x] **Multi-Source Aggregation**: Unified view of Bills, Subscriptions, Manual Events, and Goal Dates.
- [x] **Drag-and-Drop Operations**: Integrated FullCalendar (Phase 1).
- [x] **Advanced Views**: Integration of `FullCalendar` with visual "High Spend" indicators.
- [x] **Filtering**: Toggle visibility for Transactions, Bills, Tasks, and Goals.

## ✨ Phase 32: Dashboard UX Polish (v2.5) - ✅ Completed

Focused improvements on interaction design and admin panel usability.

- [x] **Grid Ruler Overlay**: Visual guides.
- [x] **Drop Zone Indicator**: Animated feedback.
- [x] **Syncing Indicator**: Pulse on save.
- [x] **Keyboard Navigation**: Escape to exit.
- [x] **Optimistic Updates**: Snappy UI.

## 🧹 Phase 33: Architecture Cleanup (v2.8) - ✅ Completed

Refactoring for maintainability.

- [x] **`useDashboardLayout` Hook**: Logic extraction.
- [x] **Component Separation**: Grid/Header/Widget split.
- [x] **Library Modernization**: `date-fns` adoption.

## 📅 Phase 34: Task Management (v2.9) - ✅ Completed

Integrated task manager linking financial entities.

- [x] **Kanban Board**: Drag-and-drop tasks.
- [x] **Calendar View**: Visual deadlines.
- [x] **Financial Linking**: Attach tasks to Bills/Goals.
- [x] **Glassmorphism UI**: Premium card design.
- [x] **Strict Data Mapping**: Establish `TaskMapper` (Completed).

## 🏛️ Phase 35: Governance & Standardization (v3.0) - ✅ Completed

Establishing the "Golden Standard" for codebase health.

- [x] **Mapping Central**: Eliminate `any`/`unknown` via strict Mapper pattern (All core entities: User, Goal, Bill, Budget, Asset, Debt, Task, Transaction).
- [x] **AI System Rules**: Formalize Agentic Flows in `.agent/rules/`.
- [x] **Documentation Sync**: Aligned `DOCUMENTATION.md` with runtime mapper pattern and strict typing rules.

## 🔐 Phase 36: Data Integrity & Standardization (v3.1) - ✅ Completed

Extending the "Golden Standard" to the entire domain.

- [x] **UserMapper**: Strict typing for Profile and Settings.
- [x] **GoalMapper**: Financial goals transformation.
- [x] **Categorization Engine**: Finalized `SmartCategory` and `CategorizationRule` mappers (Completed).
- [x] **Dev Experience**: Integrated VS Code snippets for standard Mapper/Repo/API patterns.
- [x] **Tech Debt Eradication**: Remove all legacy `any` casts in Repositories.

## 🧠 Phase 37: Intelligent Automation (v3.2) - ✅ Completed

Moving from "Passive Tracking" to "Proactive Wealth Management".

- [x] **Smart Anomaly Detection**: AI alerts for unusual spending spikes using Z-score analysis.
- [x] **Predictive Cash Flow**: Forecast balance based on recurring bills + historical spending trends.
- [x] **Natural Language Query V2**: Enhanced affordability querying with predictive context.
- [x] **Subscription Sentinel**: Auto-detect and flag recurring subscriptions and monthly burn.

## ⚡ Phase 38: Utility Management (EMN Integration) ✅

Integrating external utility providers (EMN.vn) for automated tracking.

- [x] **EmnService Expansion**: Full implementation of all 8 core API endpoints.
- [x] **Strict Typing**: Zod schemas and TypeScript interfaces for all EMN responses.
- [x] **Electric Dashboard**:
  - Customer Information Card.
  - Monthly Usage Visualization (Line/Bar charts).
  - Payment History Table.
  - Power Outage Schedule.
- [x] **Bill Estimation**: Integration with EVN calculation tool.

## 🤖 Phase 39: AI-Driven Insights & Automation - ✅ Completed

Leveraging data for proactive financial management.

- [x] **Anomaly Detection**: Flag unusual spending patterns or utility spikes. (Completed via Anomaly Scanner)
- [x] **Subscription Management**: Price change alerts, Renewal calendar view, Cancellation reminders, and Multi-currency support. (Completed via Subscription Sentinel v2)
- [x] **Cash Flow Forecasting**: Predict month-end balance based on recurring trends. (Completed via Forecast Service)
- [x] **Intelligent Tagging**: Auto-categorization using Gemini 2.0. (Completed via AI Categorize)

## 🎨 Future Enhancements (Backlog)

### User Experience

- [x] **Keyboard Shortcuts Panel**: [?] to view, [E] Edit, [Esc] Exit. (Completed)
- [x] **Undo/Redo System**: History stack for dashboard layout. (Completed)
- [x] **Widget Preview**: Hover tooltips with dimensions. (Completed)
- [x] **Responsive Preview**: Device toggle simulations. (Completed)

### Intelligence

- [x] **Smart Widget Suggestions**: AI recommendations based on user role. (Completed)
- [x] **Goal-Driven Layouts**: "I want to save money" -> Auto-generates Thrift Mode dashboard. (Completed)

## 🧩 Phase 40: Component Harmonization (The "Great Unification") - ✅ Completed

> **Objective**: Reduce technical debt by consolidating duplicate UI logic into a robust Design System based on Tailwind v4 and shadcn/ui.

**Status**: ✅ Completed

- [x] **Universal Infrastructure**:
  - [x] Created `UniversalCard` with slot-based architecture for all dashboard and system cards.
  - [x] Created `DataTableContainer` to unify all data-driven views (Transactions, Users, etc.).
  - [x] Created `DashboardChartContainer` for standardized chart presentation.
- [x] **Universal Feedback & State Management**:
  - [x] Created `StatusContainer`: A single component to handle Loading, Empty, and Error states across the app.
  - [x] Refactor `TableTransaction`, `ComparisonChart`, and `NetWorthHistoryChart` to use `StatusContainer`.
- [x] **Unified Popup & Confirmation System**:
  - [x] Created `ConfirmationModal` wrapping shadcn `AlertDialog` for all destructive and critical actions.
  - [x] Refactor `DeleteConfirmationDialog` and consolidated legacy custom modals.
- [x] **Typography & Design Tokens**:
  - [x] Created `Heading` component to enforce semantic hierarchy and typography standards.
  - [x] Centralized `THEME_COLORS` registry in `src/shared/constants/colors.ts` mapping to CSS variables.
  - [x] Eliminated hardcoded hex values in complex charts (`Area`, `Bar`, `Treemap`).
- [x] **Layout & Navigation Harmonization**:
  - [x] Unified `AppSidebar` and `AdminSidebar` into Generic navigation system.
  - [x] Standardized Header heights, breadcrumbs, and notification dropdowns.

## 🧠 Phase 41: Local Intelligence Infrastructure (The "Brain")

> **Objective**: Enable privacy-first, offline-capable AI running locally alongside the application.

**Status**: ✅ Completed

- [x] **Dockerized AI Stack**:
  - [x] Add `ollama` service to `docker-compose.yml` (GPU support optional).
  - [x] Add `chromadb` (or similar) for persistent vector storage.
- [x] **LLM Abstraction Layer**:
  - [x] Refactor `ai/*.service.ts` to use `ILLMProvider` interface.
  - [x] Implement `OllamaProvider` (Local) and `GeminiProvider` (Cloud) strategies.
  - [x] Create Environment switch `AI_PROVIDER=local|cloud`.

## 🔮 Phase 42: Personal Finance RAG (The "Oracle")

> **Objective**: "Talk to your data" - Turn raw transaction history into a queryable Knowledge Base.

**Status**: 🗓️ In Progress (Infrastructure Done)

- [x] **Vector Pipeline**:
  - [x] Batch job to generate embeddings for `Transaction.description` and `Category.keywords`.
  - [x] Real-time embedding for new transactions.
- [x] **Context Retrieval Engine**:
  - [x] Implement `FinanceContextService` hybrid search (Keyword + Vector).
  - [x] RAG Pipeline: Retrieve relevant history -> Inject into Prompt -> Answer User Query.
- [ ] **Smart Search**:
  - [ ] "Show me how much I spent on sushi last month" (Natural Language -> Vector -> Aggregation).

## 💰 Phase 43: Income Command Center

> **Objective**: deep-dive analytics for earnings, tax estimation, and salary growth visualization.

**Status**: ⏳ Planned

- [ ] **Income Dashboard (`/analytics/income`)**:
  - [ ] **Sankey Chart**: Visualize flow from Gross Salary -> Taxes/Deductions -> Net Pay -> Savings/Expenses.
  - [ ] **Growth Tracker**: YoY Salary comparison chart.
- [ ] **Tax Engine**:
  - [ ] Configurable Tax Brackets (Personal Income Tax).
  - [ ] Tax Deduction Simulator (Dependents, Insurance).
- [ ] **Paycheck Analyzer**:
  - [ ] Compare varying monthly income (freelance vs salary).

## 🎨 Phase 44: UI/UX Refactor & Design System V2

> **Objective**: Centralize styling logic and implement "delightful" motion defaults.
> **Reference**: [DESIGN_SYSTEM_V2.md](technical/DESIGN_SYSTEM_V2.md)

**Status**: 🗓️ In Progress (Aura Express V2 Infrastructure Ready)

- [x] **Semantic Design Tokens**:
  - [x] Extend `tailwind.config.ts` with semantic tiers (e.g., `bg-surface-primary`, `text-content-subtle`).
  - [x] Remove hardcoded hex values in features (In Progress).
- [ ] **Motion System**:
  - [ ] Create `MotionProvider` for global animation preferences.
  - [ ] Standardize transitions: `FadeIn`, `SlideUp`, `StaggerChildren`.
- [ ] **Navigation Refactor**:
  - [ ] Standardize `PageHeader` and `Breadcrumbs` implementation across all 30+ pages.
