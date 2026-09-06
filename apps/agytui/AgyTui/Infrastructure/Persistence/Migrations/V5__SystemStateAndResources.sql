-- V5__SystemStateAndResources.sql
-- System state, resources index, and skills metadata tables for full SQLite DB persistence

CREATE TABLE IF NOT EXISTS system_state (
    state_key TEXT PRIMARY KEY NOT NULL,
    state_value TEXT,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS resources (
    id TEXT PRIMARY KEY NOT NULL,
    title TEXT NOT NULL,
    topic TEXT NOT NULL,
    file_path TEXT NOT NULL,
    content_hash TEXT,
    tags_csv TEXT,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS skills (
    skill_name TEXT PRIMARY KEY NOT NULL,
    display_name TEXT NOT NULL,
    skill_path TEXT NOT NULL,
    is_builtin INTEGER NOT NULL DEFAULT 0,
    updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_resources_topic ON resources(topic);
CREATE INDEX IF NOT EXISTS idx_skills_builtin ON skills(is_builtin);
