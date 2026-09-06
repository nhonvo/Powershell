-- V3__DomainDbStorage.sql
-- Relational persistence tables for Domain Aggregates (Workspaces & Spaced Repetition Decks)

CREATE TABLE IF NOT EXISTS workspaces (
    name TEXT PRIMARY KEY NOT NULL,
    workspace_path TEXT NOT NULL,
    associated_account TEXT DEFAULT 'default',
    tags_csv TEXT,
    alias TEXT,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS flashcard_decks (
    topic TEXT PRIMARY KEY NOT NULL,
    cards_count INTEGER DEFAULT 0,
    average_ease_factor REAL DEFAULT 2.5,
    last_reviewed_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS flashcards (
    id TEXT PRIMARY KEY NOT NULL,
    topic TEXT NOT NULL,
    front TEXT NOT NULL,
    back TEXT NOT NULL,
    ease_factor REAL DEFAULT 2.5,
    interval_days INTEGER DEFAULT 0,
    repetitions INTEGER DEFAULT 0,
    next_review TEXT,
    status TEXT DEFAULT 'new',
    FOREIGN KEY(topic) REFERENCES flashcard_decks(topic) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_workspaces_account ON workspaces(associated_account);
CREATE INDEX IF NOT EXISTS idx_flashcards_topic ON flashcards(topic);
CREATE INDEX IF NOT EXISTS idx_flashcards_next_review ON flashcards(next_review);
