---
name: master-dotnet-skill
description: Comprehensive framework for building, refactoring, and optimizing .NET applications using Clean Architecture and SOLID principles. Use for all .NET/C# related tasks.
---

# Master .NET Skill

## Overview

This skill provides a standardized approach to .NET development, enforcing the **Polyglot Software Architect** persona. It prioritizes "Working & Maintainable" solutions, robust security, and cloud-native patterns.

## Core Principles

### 1. Engineering Mindset

- **Pragmatic Perfectionist**: Value specific solutions over theoretical purity.
- **YAGNI**: Do not over-abstract until necessary.
- **Boy Scout Rule**: Always leave code cleaner than you found it.

### 2. Architecture Standards

- **Clean Architecture**:
  - **Core/Domain**: Entities, Enums, Interfaces (No external dependencies).
  - **Application**: DTOs, Interfaces, Logic/Handlers.
  - **Infrastructure**: DB Context, External APIs.
  - **Presentation**: API Controllers, UI.
- **CQRS**: Prefer splitting Command (Write) and Query (Read) responsibilities where complex.
- **Dependency Injection**: Use built-in .NET DI. Register services with appropriate lifecycles (Scoped, Singleton, Transient).

### 3. Coding Standards

- **Async/Await**: Mandatory for I/O.
- **EF Core**:
  - Use `AsNoTracking()` for read-only operations.
  - Use `ProjectTo<Dto>()` or Manual Mapping to avoid fetching unnecessary columns.
- **Security**:
  - OWASP Top 10 enforced.
  - Parameterized queries (EF Core does this by default).
  - Anti-Forgery tokens for MVC/Razor.

## Workflow

### Step 1: Analysis & Design

Before writing code:

1. Identify the layer (Domain, App, Infra, Presentation).
2. Define the Entity or DTO.
3. Plan the interface/contract.

### Step 2: Implementation

1. **Domain**: Create/Update Entities.
2. **Infrastructure**: Update DbContext/Migrations.
3. **Application**: Implement logic/handlers using defined interfaces.
4. **Presentation**: Expose via API/Controller.

### Step 3: Refactoring & Cleanup

- Remove unused usings.
- Fix formatting.
- Ensure no "magic strings" or hardcoded secrets.

## Common Tasks

### 1. Creating a New Service

- Define `I{Name}Service` in Application layer.
- Implement `{Name}Service` in Infrastructure/Application layer.
- Register in `Program.cs` / `DependencyInjection.cs`.

### 2. Database Operations

- Always use Migrations for schema changes.
- Seeding data should be idempotent.

### 3. Error Handling

- Use Global Exception Handling Middleware.
- Return standardized API responses (e.g., `ProblemDetails`).
