# AgyTuiApp Test Suite Guide

## Directory Layout
* `Unit/`: Fast, pure in-memory unit tests (no process spawning, no network, no long delays).
* `Integration/`: Integration tests touching external process runners, resource format parsers, or file system IO.

## Naming Conventions
* **Test Class**: `<SubjectUnderTest>Tests` (e.g., `SpacedRepetitionTests`, `ConfigTests`)
* **Test Method**: `MethodUnderTest_Scenario_ExpectedOutcome` (e.g., `UpdateCard_QualityZero_ResetsIntervalAndRepetitions`, `Save_UiModeChanged_DoesNotMutateAiMode`)

## Execution
Run all tests:
```bash
dotnet test AgyTuiApp.Tests/AgyTuiApp.Tests.csproj
```
