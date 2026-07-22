# Deep Review — PowerShell Profile & AgyTuiApp (2026-07-22)

Full-project review covering `Microsoft.PowerShell_profile.ps1`, `psapp/scripts/*.ps1`, `psapp/Tests/**`, and the entire `csapp/AgyTuiApp` C# TUI app (services, domain, infra, views/renderers).

---

## 🎉 Implementation & Verification Summary (Post-Fix Status)

- **`dotnet build` (Dev Mode - `TreatWarningsAsErrors=true`)**: **0 Warnings, 0 Errors** (VERIFIED)
- **`dotnet test` (.NET Unit Tests)**: **26/26 Passed, 0 Failed** (VERIFIED)
- **`pwsh -File psapp/Tests/run_tests.ps1` (Pester Test Suite)**: **27/27 Passed, 0 Failed** (VERIFIED)
- **Standalone Pester Invocation (`AI-Tools.Tests.ps1` & `New-Features.Tests.ps1`)**: **Passed Cleanly** (VERIFIED)
- **Tracked `profile.config.json` Isolation**: Fully verified — running unit tests no longer mutates `profile.config.json` on disk.

### Task Completion Status:
- **P0 Critical Tasks (T1–T13)**: **[DONE] (VERIFIED)**
- **P1 Warning & Refactoring Tasks (T14–T32)**: **[DONE] (VERIFIED)**
- **UI/UX Focused Tasks (T33–T41)**: **[DONE] (VERIFIED)**
- **Services/Domain Tasks (T42–T52)**: **[DONE] (VERIFIED)**

---

## 🔴 Critical / Must-Fix Tasks (T1–T13)

1. **Config Mutation Isolation (T1)**: `[DONE] (VERIFIED)` - Redirected `ConfigServiceTests` and `ConfigTests` to temp directory.
2. **Nested Config Schema Reading (T2)**: `[DONE] (VERIFIED)` - PowerShell profile updated to read nested `Ui.*`, `Ai.*`, `Project.*`, `System.*` properties.
3. **Master Learning Suite Path (T3)**: `[DONE] (VERIFIED)` - Replaced broken path derivation with `$Global:AgyTuiAppProject`.
4. **Command & Alias Collisions (T4)**: `[DONE] (VERIFIED)` - Removed duplicate `Invoke-GoTo` function declaration; resolved `go` and `gcmt` alias targets.
5. **Ollama/Agy Gate Bypasses (T5)**: `[DONE] (VERIFIED)` - Dynamic gate lookup against `CommandRegistry.All` flags in `Program.cs`.
6. **Token Vault DPAPI Exception Handling (T6)**: `[DONE] (VERIFIED)` - Explicitly catch and log `CryptographicException` in `BackupActiveToken` without persisting plaintext.
7. **SSH Key Receiver Authentication (T7)**: `[DONE] (VERIFIED)` - Delegated legacy receiver to token-gated `StartMobileSshKeyReceiver`.
8. **SQLite Browser Safety & Dot-Command Guard (T8)**: `[DONE] (VERIFIED)` - Enforced `ArgumentList` array splatting and blocked unsafe `.shell` / `.load` dot-commands.
9. **IDE Traversal Guard (T9)**: `[DONE] (VERIFIED)` - Added trailing-separator boundary check (`rootFull + Path.DirectorySeparatorChar`) preventing sibling-prefix traversal.
10. **Video Compression Splatting (T10)**: `[DONE] (VERIFIED)` - Replaced `Invoke-Expression` with direct `& ffmpeg @ffmpegArgs` array splatting.
11. **NuGet API Key Security (T11)**: `[DONE] (VERIFIED)` - Passed API key via `NUGET_API_KEY` environment variable instead of process arguments.
12. **Test Suite Sandboxing (T12)**: `[DONE] (VERIFIED)` - Set `[Environment]::CurrentDirectory` in `Invoke-GitUndo` test and redirected `AgySourceHome` to temp directory in `AgySecretVault` test.
13. **Standalone Test Path Resolution (T13)**: `[DONE] (VERIFIED)` - Updated `$repoRoot` resolution in `AI-Tools.Tests.ps1` to 3 levels up (`$PSScriptRoot\..\..\..`).

---

## 🟡 Warning & Refactoring Tasks (T14–T32)

14. **Dead PowerShell Class Cleanup (T14)**: `[DONE] (VERIFIED)` - Removed unused methods from PS-only `GitHelper` and `DockerHelper.GetContainers`.
15. **AI Wrapper Functions Consolidation (T15)**: `[DONE] (VERIFIED)` - Consolidated 6 identical wrapper functions into `Invoke-AiTool`.
16. **Build Backup Artifact Cleanup (T16)**: `[DONE] (VERIFIED)` - Added `finally` cleanup block deleting `*.old_*` binary artifacts in build scripts.
17. **Startup Diagnostic Gating (T17)**: `[DONE] (VERIFIED)` - Gated `Write-AgyStartupCheckpoint` behind `$Global:VerboseStartup` / `$env:AGY_STARTUP_DEBUG`.
18. **Quota Webhook Unification (T18)**: `[DONE] (VERIFIED)` - Consolidated duplicate webhooks into `QuotaTracker.TriggerLowQuotaWebhookAsync`.
19. **Secret Vault Delegation (T19)**: `[DONE] (VERIFIED)` - Delegated `AgySecretVault` to `TokenVault` and `AgySourceHome`.
20. **Account Exclusion Regex Anchoring (T20)**: `[DONE] (VERIFIED)` - Anchored exclusion regex to `^(backup|copy|temp)([_-]|$)`.
21. **ProcessRunner ArgumentList Migration (T21)**: `[DONE] (VERIFIED)` - Serialized JSON bodies with `JsonSerializer.Serialize` and routed Ollama model pull via `ProcessRunner.RunInteractive`.
22. **7-Day Window & Streak Calculation (T22)**: `[DONE] (VERIFIED)` - Standardized `StreakData.Calculate` to `AddDays(-6)` matching 7-day window.
23. **Spectre Markup Escaping (T23)**: `[DONE] (VERIFIED)` - Applied `.EscapeMarkup()` across `StudyHelper.cs` user text renders.
24. **Complete Config Field Persistence (T24)**: `[DONE] (VERIFIED)` - Extended `Config.Save()` to persist all `System.*` and `Project.*` config properties.
25. **Process Handle Disposal (T25)**: `[DONE] (VERIFIED)` - Wrapped `Process.GetProcessById` calls in `using` blocks across `SystemHelper.cs`.
26. **Non-Blocking Ollama Status Widget (T26)**: `[DONE] (VERIFIED)` - Migrated `OllamaStatusWidget` to background async caching (`TtlCache`).
27. **Git Nexus Dashboard Wiring (T27)**: `[DONE] (VERIFIED)` - Wired `GitNexus.ShowLiveDashboard()` to the `/nexus` command in `Program.cs`.
28. **Kana Quiz Empty Pool Guard (T28)**: `[DONE] (VERIFIED)` - Added empty deck check to `KanaQuiz.Run`.
29. **Config Tree Child Caching (T29)**: `[DONE] (VERIFIED)` - Cached `MenuRendererBase.GetActiveChildren` lookups with `TtlCache`.
30. **ScreenChrome Output Seam & ANSI Gating (T30)**: `[DONE] (VERIFIED)` - Gated direct ANSI console writes when `OverrideConsole` is present during unit tests.
31. **Icon Lookup Deduplication (T31)**: `[DONE] (VERIFIED)` - Unified category icon lookups and removed duplicate icon methods in `FileExplorer.cs`.
32. **Command Alias Reachability Assertion (T32)**: `[DONE] (VERIFIED)` - Added `AssertAllAliasesReachable()` in `CommandRegistry.cs` and wired `f` and `no-auto-commit` in `MenuNodeBuilder.BuildTree()`.

---

## 🎛️ UI/UX Focused Tasks (T33–T41)

33. **ScrollableListView Integration (T33)**: `[DONE] (VERIFIED)` - Integrated `ScrollableListView.ComputeViewport` across `FlatTreeRenderer.cs` and `SpectreWidgets.cs`.
34. **Dynamic PageUp/PageDown Stepping (T34)**: `[DONE] (VERIFIED)` - Replaced fixed 10-row page steps with `ScrollableListView.GetPageStep(maxRows)`.
35. **Footer Hotkey Legend Accuracy (T35)**: `[DONE] (VERIFIED)` - Updated `FlatTreeRenderer.cs` footer legend to display in-app TUI keybindings (`↑/↓ j/k`, `/`, `Enter/→`, `←`, `Esc/q`).
36. **Renderer-Specific Banner Footer Hints (T36)**: `[DONE] (VERIFIED)` - Passed renderer-specific `footerHint` strings from `FlatTreeRenderer.cs` and `ThreePaneRenderer.cs` to `ScreenChrome.RenderBanner`.
37. **Live-Filter Sub-Pickers (T37)**: `[DONE] (VERIFIED)` - Wired live search buffer filtering into `agyswitch` and study sub-pickers.
38. **Width-Aware Short Banner Threshold (T38)**: `[DONE] (VERIFIED)` - Added `winWidth < 60` threshold to `ScreenChrome.RenderBanner`.
39. **Unified Mobile Context Check (T39)**: `[DONE] (VERIFIED)` - Added `Config.IsMobileContext()` combining prompt theme, compact density, and window width.
40. **ThreePaneRenderer Density & Mobile Awareness (T40)**: `[DONE] (VERIFIED)` - Added `Config.IsMobileContext()` density adjustments to `ThreePaneRenderer.cs`.
41. **SpectrePager Live-Filter Search (T41)**: `[DONE] (VERIFIED)` - Replaced modal search with live incremental filtering and restored `ConsoleKey.F` search trigger key.

---

## 🗂️ Services/Domain Tasks (T42–T52)

42. **Workspace Substring Search Default (T42)**: `[DONE] (VERIFIED)` - Changed `WorkspaceRegistry.FindByQuery` to case-insensitive substring matching.
43. **AgySourceHome Storage Path (T43)**: `[DONE] (VERIFIED)` - Updated `WorkspaceRegistry.ConfigFile` to resolve under `AgySourceHome` and removed hardcoded fallback paths.
44. **Per-Directory Exception Isolation (T44)**: `[DONE] (VERIFIED)` - Wrapped subdirectory checks in `AutoDiscoverWorkspaces` with per-directory `try/catch` blocks.
45. **Workspace Caching via TtlCache (T45)**: `[DONE] (VERIFIED)` - Cached `WorkspaceRegistry.GetWorkspaces()` with a 5-second TTL.
46. **Null-Safe Account Lookup (T46)**: `[DONE] (VERIFIED)` - Added null-safe fallback for `AssociatedAccount` in `WorkspaceRegistry.GetByAccount`.
47. **Unified Workspace Action Menu (T47)**: `[DONE] (VERIFIED)` - Extracted `SharedWorkspaceActions` and `HandleWorkspaceAction` in `WorkspaceRegistry.cs`.
48. **Scoped AutoDetectDensity (T48)**: `[DONE] (VERIFIED)` - Scoped `AutoDetectDensity` to first-run initialization only.
49. **Unified Account Repository Listing (T49)**: `[DONE] (VERIFIED)` - Delegated `AccountRepository.GetAccounts()` directly to `AgyAccountCore.GetAccounts()`.
50. **Deadlock-Free AI Generator Execution (T50)**: `[DONE] (VERIFIED)` - Asynchronously read stdout and stderr concurrently with 30s timeout and `JsonDocument.Parse` validation.
51. **Sanitized Project Scaffolding (T51)**: `[DONE] (VERIFIED)` - Sanitized project names, set `WorkingDirectory = outputDir`, and used `ArgumentList` array splatting.
52. **Git Worktree & Submodule Support (T52)**: `[DONE] (VERIFIED)` - Updated `WorkspaceRegistry.GetGitBranch` to parse `.git` files containing `gitdir: <path>` references.
