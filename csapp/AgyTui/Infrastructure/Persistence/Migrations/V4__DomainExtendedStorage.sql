-- V4__DomainExtendedStorage.sql
-- Extended relational tables for Themes, AI Agent Invocation Logs, and System Registries

CREATE TABLE IF NOT EXISTS themes (
    theme_name TEXT PRIMARY KEY NOT NULL,
    display_name TEXT NOT NULL,
    accent_color TEXT,
    colors_json TEXT,
    is_active INTEGER NOT NULL DEFAULT 0,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ai_invocation_logs (
    id TEXT PRIMARY KEY NOT NULL,
    alias TEXT NOT NULL,
    timestamp_utc TEXT NOT NULL,
    duration_ms INTEGER NOT NULL,
    success INTEGER NOT NULL,
    active_account TEXT DEFAULT 'default',
    provider_mode TEXT DEFAULT 'auto',
    created_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_themes_active ON themes(is_active);
CREATE INDEX IF NOT EXISTS idx_ai_logs_account ON ai_invocation_logs(active_account);
CREATE INDEX IF NOT EXISTS idx_ai_logs_timestamp ON ai_invocation_logs(timestamp_utc);
