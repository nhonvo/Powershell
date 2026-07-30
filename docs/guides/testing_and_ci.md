# Developer Guide: Multi-Layered Testing, CI/CD & Knowledge Base Sync

## 1. Overview
This guide describes the testing strategy, continuous integration pipeline, and Obsidian dataset synchronization engine for the **PowerShell Control Center (`AgyTui`)** codebase.

---

## 2. Test Suite Architecture

The test project `csapp/AgyTui.Tests` contains multiple test layers:

| Layer | Path | Description |
| :--- | :--- | :--- |
| **Unit Tests** | `Unit/` | Fast, isolated unit tests using `ServiceTestFixture` and in-memory mocks (`InMemoryAgyAccountRepository`, `FakeSqliteDatabase`). |
| **Integration Tests** | `Integration/` | Persistence tests verifying `SqliteMigrationEngine` DDL migrations and environment isolation (`agytui.dev.db` vs `agytui.db`). |
| **Parity Tests** | `Parity/` | `ProfileAliasParityTests.cs` verifying that every alias in `CommandRegistry.cs` is correctly registered and mapped in `CommandRouter.cs`. |
| **Benchmark Tests** | `Unit/Core/Services/PathResolutionBenchmarkTests.cs` | Performance tests asserting sub-millisecond execution for `IAppPathManager` thread-safe path resolution caching. |

### Running Tests Locally
To run all tests:
```powershell
dotnet test csapp/AgyTui.Tests/AgyTui.Tests.csproj -c Debug
```

---

## 3. Environment Isolation Rules

- `ENVIRONMENT=Development` (or `DOTNET_ENVIRONMENT=Development`): SQLite database resolves to `agytui.dev.db`.
- `ENVIRONMENT=Production`: SQLite database resolves to `agytui.db`.

---

## 4. Fresh Computer Onboarding

To onboard a fresh machine:
```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\script\Install-AgyEnvironment.ps1
```
This script automatically:
1. Audits and installs .NET 9 SDK via `winget` if missing.
2. Provisions `~/.gemini/` home directories (`logs/`, `history/`, `data/`).
3. Links `$PROFILE` to `Microsoft.PowerShell_profile.ps1`.
4. Compiles `AgyTui.csproj`.
5. Executes initial SQLite schema migrations.

---

## 5. Knowledge Base & Obsidian Dataset Sync Engine

The `refresh` command (`ObsidianClient.cs` / `ResourceExtractor.cs`) rescans the Obsidian Vault directory and synchronizes datasets to `learn/`:
- Flashcard Decks (`.json`)
- Quiz Question Decks
- Cheat Sheets & STAR Answer Templates

To run dataset sync manually from the CLI:
```powershell
AgyTui refresh
```
