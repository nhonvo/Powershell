CREATE TABLE IF NOT EXISTS app_config (
    section_name TEXT PRIMARY KEY,
    json_data TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS accounts (
    account_name TEXT PRIMARY KEY,
    email TEXT,
    is_active INTEGER NOT NULL DEFAULT 0,
    quota_status TEXT DEFAULT 'OK',
    last_used TEXT,
    usage_count INTEGER DEFAULT 0,
    request_history_json TEXT DEFAULT '[]',
    metadata_json TEXT DEFAULT '{}',
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS system_state (
    state_key TEXT PRIMARY KEY,
    state_value TEXT,
    updated_at TEXT NOT NULL
);
