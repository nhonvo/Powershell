# Detailed Plan - Step 9: Core Layer Refactoring, Domain Migration & Service Abstraction

## 1. Executive Summary
This document defines the architectural refactoring plan for `csapp/AgyTui/Core`. The goal is to eliminate static registry access, migrate data contracts and domain models into their respective `Domain/` bounded contexts (`AccountContext`, `WorkspaceContext`, `LearnContext`), introduce interface abstractions for `WorkspaceRegistry`, `ResourceRegistry`, and `Config`, prune unused/dead logic, and update PowerShell profile aliases (`Microsoft.PowerShell_profile.ps1`).

---

## 2. Core Components Audit & Target Architecture

| Current Component | Current Location | Target Location & Pattern | Interface & DI Strategy |
| :--- | :--- | :--- | :--- |
| `AccountMetadata` | `Core/Models/AccountMetadata.cs` | `Domain/AccountContext/AccountMetadata.cs` | Data contract for `AccountAggregate` |
| `LearningModels` | `Core/Models/LearningModels.cs` | `Domain/LearnContext/LearningModels.cs` | Models for quizzes, flashcards, STAR answers |
| `WorkspaceEntry` & `Link` | `Core/Registries/WorkspaceRegistry.cs` | `Domain/WorkspaceContext/WorkspaceModels.cs` | Models for workspace aggregates |
| `WorkspaceRegistry` | `Core/Registries/WorkspaceRegistry.cs` | `Infrastructure/Services/WorkspaceRegistry.cs` | `IWorkspaceRegistry` registered as `Singleton` |
| `ResourceRegistry` | `Core/Registries/ResourceRegistry.cs` | `Infrastructure/Services/ResourceRegistry.cs` | `IResourceRegistry` registered as `Singleton` |
| `Config` | `Core/Models/Config.cs` | `Infrastructure/Configuration/ConfigService.cs` | `IConfigService` registered as `Singleton` |
| `EnvironmentProvider` | `Core/Configuration/EnvironmentProvider.cs` | `Core/Configuration/EnvironmentProvider.cs` | Static environment resolution helper |
| `AppPathManager` | `Core/Services/AppPathManager.cs` | `Core/Services/AppPathManager.cs` | `IAppPathManager` registered as `Singleton` |

---

## 3. Detailed Refactoring Tasks

### 3.1 Domain Model Migration
1. Move `AccountMetadata` to `AgyTui.Domain.AccountContext`.
2. Move `LearningModels` (`QuizQuestion`, `StarAnswer`, `Flashcard`, `CheatSheet`) to `AgyTui.Domain.LearnContext`.
3. Extract `WorkspaceEntry` and `WorkspaceLink` from `WorkspaceRegistry.cs` into `AgyTui.Domain.WorkspaceContext`.

### 3.2 Workspace & Resource Registry Service Abstraction
1. Create `IWorkspaceRegistry.cs` interface declaring:
   - `WorkspaceEntry[] GetWorkspaces()`
   - `WorkspaceAggregate[] GetWorkspaceAggregates()`
   - `int SyncAllProjects(string? customBaseDir = null)`
   - `void SaveWorkspaces(WorkspaceEntry[] entries)`
   - `WorkspaceEntry[] FindByQuery(string query, bool asRegex = false)`
   - `WorkspaceEntry[] GetByAccount(string accountName)`
   - `string GetGitBranch(string dirPath)`
   - `string HandleWorkspaceAction(WorkspaceEntry selected, int actionIdx)`
2. Convert `WorkspaceRegistry` from static utility to injectable `WorkspaceRegistry : IWorkspaceRegistry` registered in `Bootstrapper.cs`.
3. Create `IResourceRegistry.cs` interface declaring:
   - `ResourceCategory[] GetCategories()`
   - `ResourceCategory? GetCategory(string key)`
   - `void RefreshCategories()`
4. Convert `ResourceRegistry` to injectable `ResourceRegistry : IResourceRegistry` registered in `Bootstrapper.cs`.

### 3.3 Configuration Service Abstraction (`IConfigService`)
1. Create `IConfigService.cs` declaring:
   - `ConfigModel Current { get; }`
   - `void Save()`
   - `void Reload()`
2. Register `IConfigService` as a `Singleton` in `Bootstrapper.cs`.

### 3.4 Dead Code & Unused Logic Pruning
1. Remove deprecated inline fallbacks and redundant static getters in `CommandRegistry.cs`.
2. Clean up dead methods in `WorkspaceRegistry.cs` and `ResourceRegistry.cs`.

### 3.5 PowerShell Profile & Menu Parity Alignment
1. Verify `$aliases` in `Microsoft.PowerShell_profile.ps1` maps `SystemHelper` -> `SystemConsoleView`, `AccountHelper` -> `AgyAccountStore`, `StudyHelper` -> `LearnRouter`, `SshHelper` -> `SshConsoleView`.
2. Assert 100% parity across `CommandRegistry` entries and profile function wrappers via `ProfileAliasParityTests.cs`.

---

## 4. Implementation Checklist

- [x] Fix `SystemHelper` accelerator mapping in `Microsoft.PowerShell_profile.ps1`.
- [x] Move `AccountMetadata.cs` to `Domain/AccountContext/`.
- [x] Move `LearningModels.cs` to `Domain/LearnContext/`.
- [x] Extract `WorkspaceModels.cs` into `Domain/WorkspaceContext/`.
- [x] Create `IWorkspaceRegistry.cs` and `WorkspaceRegistry.cs` service.
- [x] Create `IResourceRegistry.cs` and `ResourceRegistry.cs` service.
- [x] Create `IConfigService.cs` and `ConfigService.cs`.
- [x] Register new services in `Bootstrapper.cs` and `ServiceTestFixture.cs`.
- [x] Prune unused logic in `Core/`.
- [x] Verify test suite passes with `dotnet test csapp/AgyTui.Tests/AgyTui.Tests.csproj -c Debug`.
