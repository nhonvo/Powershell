-- V6__ComprehensiveLearningSeeding.sql
-- Comprehensive schema tables for Interview Questions, Vocab, and Quiz Banks

CREATE TABLE IF NOT EXISTS quiz_questions (
    id TEXT PRIMARY KEY NOT NULL,
    category TEXT NOT NULL,
    type TEXT,
    difficulty TEXT,
    question TEXT NOT NULL,
    format TEXT,
    hints_json TEXT,
    companies_json TEXT,
    tags_json TEXT,
    updated_at TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_quiz_questions_category ON quiz_questions(category);
