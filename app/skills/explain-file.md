---
name: explain-file
description: Ask local AI to explain the active file structure and design
trigger:
  - explain
  - review
steps:
  - primitive: ask
    args:
      question: Explain the structure, design patterns, and public API of this file.
---

# Explain Active File

This skill sends the currently opened file to the AI assistant to summarize its structure, key methods, and potential refactoring opportunities.
