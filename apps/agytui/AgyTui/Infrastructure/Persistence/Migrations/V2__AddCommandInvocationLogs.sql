CREATE TABLE IF NOT EXISTS command_invocation_logs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    alias TEXT NOT NULL,
    timestamp_utc TEXT NOT NULL,
    duration_ms REAL NOT NULL,
    success INTEGER NOT NULL,
    category TEXT,
    account_name TEXT
);
