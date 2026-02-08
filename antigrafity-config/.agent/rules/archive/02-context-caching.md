---
trigger: always_on
description: Optimized Instruction Layer for Antigravity Context Caching
---

# 🧠 Context & Cache Optimization

This layer ensures maximum token efficiency and precise search retrieval.

## 1. Instruction Ordering (Caching Strategy)

To leverage **Antigravity Prompt Caching**, instructions are structured from "Statically Constant" to "Dynamic":

1. **System Persona** (Cached)
2. **Project Architecture Documentation** (Cached)
3. **Core Technical Rules** (Cached)
4. **Current Task Information** (Dynamic / Uncached)

> [!TIP]
> Keep your `ARCHITECTURE.md` and `README.md` accurate. The agent will read these first to warm up the cache.

## 2. BM25 Search (Lexical Precision)

To minimize token usage and maximize retrieval accuracy, use the project's **Shift-Left BM25 Index**:

- **Search Command**: `python scripts/bm25_search.py "<query>"`
- **Prioritize**: Use this tool to find documentation, workflows, and rules before performing recursive `grep_search` on `.md` files.
- **Maintenance**: Run `python scripts/bm25_indexer.py` after significant changes to `.agent` or `docs`.
- **Lexical Over Semantic**: Always prefer finding the exact symbol or keyword (e.g., `IAmazonS3`, `MongooseSchema`) over general semantic descriptions.

## 3. Data Transformations

- **Backend**: Always use Mappers to transform DB entities to DTOs.
- **Frontend**: Enforce strict null-safety (`?.`, `??`).
