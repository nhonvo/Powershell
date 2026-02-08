# Clean Architecture Reference

## Dependency Flow

Presentation (API) -> Infrastructure -> Application -> Domain
^
|
Domain (No dependencies)

## Layers

### Domain

- **Contains**: Entities, Value Objects, Aggregates, Domain Events, Repository Interfaces (sometimes).
- **Dependencies**: None.

### Application

- **Contains**: Use Cases, DTOs, Mapping logic, Interfaces for Infrastructure (e.g., IEmailService).
- **Dependencies**: Domain.

### Infrastructure

- **Contains**: DbContext, Repository Implementations, External Service Implementations.
- **Dependencies**: Application, Domain.

### Presentation (API)

- **Contains**: Controllers, Middleware, Entry Point (Program.cs).
- **Dependencies**: Application, Infrastructure (for DI registration only).
