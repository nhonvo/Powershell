# EMN Integration (Electric Utility)

## Overview

This feature provides integration with **EVN (Vietnam Electricity)** to track detailed electricity usage, bills, payment history, and outage schedules.

## Architecture

### 1. Data Source

- **Official Portal**: `https://emn.vn`
- **Authentication**: Custom headless browser automation (Puppeteer) to bypass CSRF/Anti-bot protection.
- **Session Management**: Authenticated cookies are extracted and stored in MongoDB (`emn_sessions` collection) for subsequent API calls.

### 2. Services

#### `EmnService` (Singleton)

- **Role**: Core orchestrator.
- **Responsibilities**:
  - Managing user sessions (login/logout).
  - Proxying API requests to EMN endpoints.
  - Normalizing date formats (`YYYY-MM-DD` <-> `DD/MM/YYYY`).
  - Merging session cookies.

#### `EmnBrowserService`

- **Role**: Authentication specialist.
- **Library**: `puppeteer-core`.
- **Responsibilities**:
  - Launching headless Chrome.
  - Automating the login form submission.
  - Extracting `__RequestVerificationToken` and session cookies.

#### 3. Data Caching & Synchronization (New)

To address EMN portal instability and slow response times, we implemented a "Cache First, Sync Second" strategy.

#### 3.1. EmnData Model

- **Collection**: `emndatas`
- **Schema**:
  - `userId`: ObjectId
  - `type`: 'usage' | 'payment' | 'bill'
  - `period`: String (YYYY-MM or YYYY for aggregation)
  - `data`: Mixed (Raw JSON from API)

# 3. Data Flow & Synchronization

## 3.1. Hybrid Caching Strategy (Cache-First)

To ensure system stability and performance, we implement a **"Cache First, Sync Second"** strategy:

1.  **Read**: Always fetch data from the local MongoDB `EmnData` collection first.
    - **Fast**: Zero latency for UI rendering.
    - **Reliable**: Works even if the external utility provider is down.
2.  **Sync (Write)**: Triggered manually via "Sync Data" button or scheduled cron job.
    - **Crawling**: Connects to external provider using Puppeteer.
    - **Upsert**: Updates local cache with latest bills, payments, and usage details.

## 3.2. Data Models

We store cached data in the `EmnData` model with a flexible payload structure:

- **`userId`**: Owner of the data.
- **`customerCode`**: Utility customer ID (e.g., PE...).
- **`type`**: `'usage' | 'bill' | 'payment'`.
- **`period`**: Time identifier (e.g., `2024-01`).
- **`data`**: The raw or normalized JSON payload from the provider.
- **`lastSync`**: Timestamp of the last successful fetch.

### 3.3. API Endpoints

- **`POST /api/v1/emn/sync`**: Triggers the crawling process (3 years history).
- **`GET /api/v1/emn/usage`**: Returns cached usage data (fallback to sync if empty).
- **`GET /api/v1/emn/payment`**: Returns cached payment history.
- **`GET /api/v1/emn/bills`**: Returns cached bills.

### 3.4. Seeding & Data Backup

To support development and data migration:

1. **Crawl & Export**:

   ```bash
   bun scripts/crawl-emn.ts
   ```

   - Crawls **3 years** of payment history and **6 months** of detailed usage.
   - Exports data to `seed/emn-data.json`.

2. **Import/Seed**:

   ```bash
   bun scripts/seed-emn.ts
   ```

   - Reads `seed/emn-data.json`.
   - Upserts data for the **current primary user**.
   - Also available via **Admin > Database Seeding > Seed EMN Data**.

## 4. Implementation Details

## 4.1. Service Architecture

The `EmnService` singleton manages the integration:

- **`syncData(userId)`**: The core orchestrator.
  1. Logs in via `EmnBrowserService`.
  2. Iterates through last 3 years for payments.
  3. Iterates through last 6 months for daily usage.
  4. Saves each record to `EmnDataRepository`.
- **`getUsage(userId, ...)`**: Retrieves data from repository only.

## 4.2. Repository Pattern

`EmnDataRepository` handles all DB interactions, ensuring strict typing and cleaner queries:

- `upsert(...)`: Handles idempotent updates based on period/type.
- `findMany(...)`: optimized batch retrieval.

### 4. Components

- **QuickSelect**: Standardized date range picker (Daily, Monthly, Yearly).
- **EmnUsageChart**: Recharts-based visualization of daily/cumulative usage.
- **EmnPaymentsTable**: Historical payment records.
- **EmnOutageList**: Scheduled maintenance notification.

## Key APIs

| Endpoint                 | Method | Description                    |
| :----------------------- | :----- | :----------------------------- |
| `/api/v1/emn/auth/login` | `POST` | Triggers Puppeteer login flow. |
| `/api/v1/emn/usage`      | `GET`  | Daily usage details.           |
| `/api/v1/emn/payments`   | `GET`  | Payment history.               |
| `/api/v1/emn/bills`      | `GET`  | Monthly bill list.             |

## Resilience Patterns

- **Browser Automation**: Used only for login to ensure 100% success rate on authentication.
- **Cookie Persistence**: MongoDB storage prevents needing to re-login on every request.
- **User-Agent Spoofing**: Mimics real Chrome browser to avoid WAF blocking.
