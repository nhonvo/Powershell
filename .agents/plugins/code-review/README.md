# Code Review & Quality Sentinel Pro Plugin

A comprehensive plugin for Antigravity providing automated whole-codebase reviews, security audits, concurrency checks, and pure Markdown reports.

## Features

- **Whole Codebase Review (`whole-codebase-review`)**: Multi-pass audit covering architecture, compilation, static analysis, concurrency, and reliability.
- **Security Audit (`security-audit`)**: Secret scanning, shell injection defense, path traversal verification, and process isolation.
- **Always-on Review Rules (`rules/AGENTS.md`)**: Enforces standard severity classifications (`[P0]`, `[P1]`, `[P2]`, `[P3]`), workspace-relative file links, and pure Markdown outputs.

## Usage

Trigger review workflows using natural language:
- *"Review the whole codebase and produce an audit report"*
- *"Run a security and concurrency audit on the Go micro-apps"*
- *"Audit the repository for leaked secrets and resource leaks"*
