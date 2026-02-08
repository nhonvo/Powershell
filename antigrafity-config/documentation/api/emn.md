# EMN.vn Integration API Documentation

## Overview

The EMN integration allows the dashboard to fetch real-time and historical electricity data from EMN.vn (EVN). It uses a hybrid authentication model:

1. **Automated Login**: Uses username/password to generate a session server-side.
2. **Manual Session**: Allows injecting cookie strings directly if automated login is blocked.

All sessions are persisted in the `EmnSession` MongoDB collection, mapped to the user's ID.

---

## Authentication Endpoints

### POST /api/v1/emn/auth/login

**Purpose**: Authenticate with EMN.vn using credentials.
**Request Body**:

```json
{
  "username": "customer_id",
  "password": "emn_password"
}
```

**Response**:

```json
{
  "success": true,
  "message": "Login successful",
  "logs": ["Step 1: Fetching login page...", "..."]
}
```

### POST /api/v1/emn/auth/session

**Purpose**: Manually inject EMN session cookies.
**Request Body**:

```json
{
  "cookieString": ".AspNetCore.Antiforgery...; .AspNetCore.Cookies=...",
  "username": "customer_id"
}
```

---

## Data Endpoints

### GET /api/v1/emn/profile

**Purpose**: Fetch customer profile information.
**Requires**: Active EMN session for the user.
**Response**: `IEmnCustomerInfo`

```json
{
  "success": true,
  "data": {
    "maKhachHang": "PA123...",
    "tenKhachHang": "NGUYEN VAN A",
    "maTram": "T123..."
  }
}
```

### GET /api/v1/emn/usage

**Purpose**: Fetch day-by-day consumption details.
**Query Params**:

- `maKhachHang`: string
- `ngayBatDau`: YYYY-MM-DD
- `ngayKetThuc`: YYYY-MM-DD
  **Response**: `IEmnUsageDetail[]`

### GET /api/v1/emn/bills

**Purpose**: List recent bills.

### GET /api/v1/emn/payments

**Purpose**: List payment history.
**Query Params**:

- `maKhachHang`: string
- `thangBatDau`: YYYY-MM-DD
- `thangKetThuc`: YYYY-MM-DD

### GET /api/v1/emn/outages

**Purpose**: Fetch upcoming power outage schedule.
**Query Params**:

- `maTram`: string
- `ngayBatDau`: YYYY-MM-DD
- `ngayKetThuc`: YYYY-MM-DD

---

## Utilities

### POST /api/v1/emn/calculate

**Purpose**: Calculate estimated bill based on EVN pricing.
**Request Body**: `IEmnCalculationParams`

### GET /api/v1/emn/calculate

**Purpose**: Fetch metadata for calculation form (Voltage levels, groups, etc.).

---

## Database Schema: EmnSession

- `userId`: ObjectId (Indexed, link to User)
- `cookies`: string (Securely stored session)
- `username`: string
- `lastSync`: Date
- `expiresAt`: Date
