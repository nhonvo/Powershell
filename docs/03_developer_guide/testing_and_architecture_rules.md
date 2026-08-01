# 🧪 Test Suite, Architecture Enforcement & Quality Assurance

> **Category**: Developer Guide  
> **Subsystem**: Quality Assurance & Reflection Testing  
> **Date**: 2026-07-31  
> **Author**: Antigravity AI Engineering Team  
> **Status**: Completed / Active  

---

## Executive Summary
This document specifies the XUnit test suite structure, reflection-based architecture rules, parity assertions, and execution instructions for validating `AgyTui` code quality before release.

## Table of Contents
- [1. Test Suite Structure](#1-test-suite-structure)
- [2. Reflection-Based Architecture Rules](#2-reflection-based-architecture-rules)
- [3. Profile Parity Assertions](#3-profile-parity-assertions)
- [4. Running Tests](#4-running-tests)
- [5. Cross References](#5-cross-references)

---

## 1. Test Suite Structure

The test project is located at `csapp/AgyTui.Tests/`:

```text
csapp/AgyTui.Tests/
├── Fixtures/                   # Service Container Fixtures (ServiceTestFixture.cs)
├── Integration/                # SQLite & JSON Storage Integration Tests
├── Mocks/                      # In-Memory SQLite Mock Connections & Repos
├── Parity/                     # PowerShell Profile <-> C# Parity Tests
└── Unit/                       # Unit Tests Mirroring Main Project Layers
    ├── Architecture/           # Layer Rule Enforcement & Hygiene Tests
    ├── Domain/                 # Domain Aggregate Invariant Tests
    ├── Infrastructure/         # Repositories, DI & Seeding Unit Tests
    └── UI/                     # Layout, Navigation & Screen View Tests
```

---

## 2. Reflection-Based Architecture Rules

`ArchitectureTests.cs` enforces Clean Architecture layer boundaries using .NET Reflection:

```csharp
[Fact]
public void Domain_Namespace_DoesNotReference_Infrastructure_Or_UI()
{
    var domainAssembly = typeof(AccountAggregate).Assembly;
    var domainTypes = domainAssembly.GetTypes()
        .Where(t => t.Namespace != null && t.Namespace.StartsWith("AgyTui.Domain"))
        .ToList();

    foreach (var type in domainTypes)
    {
        var invalidRefs = GetReferencedNamespaces(type)
            .Where(ns => ns.StartsWith("AgyTui.Infrastructure") || ns.StartsWith("AgyTui.UI"))
            .ToList();

        Assert.Empty(invalidRefs);
    }
}
```

---

## 3. Profile Parity Assertions

`ProfileAliasParityTests.cs` verifies that all key PowerShell aliases (`cc`, `ccd`, `cnav`, `reset-agy`, `purge-accounts`, `dotnet-info`) exist in both `Microsoft.PowerShell_profile.ps1` and `CommandRegistry.cs`.

---

## 4. Running Tests

Execute the full test suite from the terminal:

```powershell
dotnet test csapp/AgyTui.Tests/AgyTui.Tests.csproj -c Debug
```

---

## 5. Cross References
- [Clean Architecture Overview](../01_architecture/overview.md)
- [Production Release Publishing](release_publishing.md)

---

## 6. Persisted Architectural Invariants & Learned Standards

1. **Search Tree Filtering & Expansion Invariant**:
   - Match search terms strictly against node names (`w.Name`, `c.Name`), never against full absolute paths (`w.WorkspacePath`, `c.WorkspacePath`).
   - Root nodes matching the search query must remain **collapsed by default** unless at least one child node matches, suppressing non-matching sibling sub-modules.

2. **Strict Menu Priority & Verification Invariant**:
   - Menu priority order must be synchronized across both `categoryNames` in `MenuNodeBuilder.cs` and `orderedAliases` in `CommandRegistry.cs`.

3. **Zero Swallowed Exceptions Guardrail**:
   - Empty `catch { }` blocks are prohibited. Every exception must be captured via `LogHelper.LogError` or handled via `ExceptionMiddleware.Handle`.

4. **Unmapped C# API Registration Standard**:
   - Internal helper methods (`prune-workspaces`, `discover-workspaces`, `daily-note`, `orphan-notes`, `mastery-tree`) must be registered in `CommandRegistry.cs` and routed in `CommandRouter.cs`.

