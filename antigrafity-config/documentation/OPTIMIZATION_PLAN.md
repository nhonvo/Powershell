# Optimization Plan: Categorization, Data, and Analysis

## 1. Categorization System Optimization

### Current Architecture

- **Structure**: 3-Layer Waterfall (Strict Rules -> Merchant DB -> Smart Categorization).
- **Pros**: Clear priority system (User > System), rule usage tracking.
- **Cons**:
  - Merchant DB is hardcoded/static.
  - "Smart" model is a simple "Bag of Words" without context (amount, time).
  - `trainFromHistory` is O(N) in Node.js, causing timeout risks.

### Implementation Plan

#### A. Fix Training Performance

- **Goal**: Offload processing to MongoDB instead of iterating in Node.js.
- **Action**: Optimize `trainFromHistory` to use batched bulk operations or aggregation pipelines where possible.
- **Refactor**: Stop deleting the entire model on retrain. Use `upsert` with differential updates.

#### B. Enhance Context Awareness

- **Goal**: Differentiate ambiguity (e.g. "7-Eleven" could be Groceries vs Gas).
- **Action**: Add `amountRange` weights to the `SmartCategoryModel`.
  - Example: `< $10` -> Snacks, `> $30` -> Gas.

## 2. Import & Export Flow Optimization

### Import (Excel Seed)

- **Current Weakness**: Entire file loaded to RAM (V8 Heap limit risk). Fire-and-forget error handling.
- **Plan**:
  - Keep using `xlsx` for now (Simplicity > Perf for <5MB files).
  - **Critical**: Add detailed error reporting response, not just "success/fail". Return rows that failed.

### Export (Data Dump)

- **Current Weakness**: `findAll(userId)` fetches entire history. Crashes with large datasets.
- **Plan**:
  - Update `ExportService` to accept `ExportOptions` (`startDate`, `endDate`, `columns`).
  - Implement streaming response for the API route.

## 3. CRUD & Analytics Performance

### Repository Layer

- **Current Weakness**:
  - Regex search is slow (collscan).
  - Excessive `await connect()` calls.
  - Missing compound indexes for dashboard queries.
- **Plan**:
  - **Indexing**: Define `TransactionSchema.index({ userId: 1, transaction_date: -1, category: 1 })`.
  - **Search**: Move from `$regex` to `$text` search or optimize regex with anchors (`^prefix`).

### Action Items Checklist

- [ ] **Phase 1: Export Logic**
  - [ ] Modify `ExportService.exportTransactionsToExcel` to accept date range.
  - [ ] user-facing API endpoint to support query params.

- [ ] **Phase 2: Smart Categorization**
  - [ ] Refactor `trainFromHistory` to use batch processing (size 1000).
  - [ ] Add `weight` decay to old keywords to keep model fresh.

- [ ] **Phase 3: Database & Indexing**
  - [ ] Add compound indexes to Mongoose Schemas.
  - [ ] Verify `explain()` plans for dashboard queries.
