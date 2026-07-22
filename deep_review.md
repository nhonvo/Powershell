# Deep Review — PowerShell Profile & AgyTuiApp (2026-07-22)

Full-project review covering `Microsoft.PowerShell_profile.ps1`, `psapp/scripts/*.ps1`, `psapp/Tests/**`, and the entire `csapp/AgyTuiApp` C# TUI app (services, domain, infra, views/renderers).

## 🎉 Implementation & Verification Summary (Post-Fix Status)

- **`dotnet build` (Dev Mode - `TreatWarningsAsErrors=true`)**: **0 Warnings, 0 Errors** (VERIFIED)
- **`dotnet test` (.NET Unit Tests)**: **26/26 Passed, 0 Failed** (VERIFIED)
- **`pwsh -File psapp/Tests/run_tests.ps1` (Pester Test Suite)**: **27/27 Passed, 0 Failed** (VERIFIED)
- **Tracked `profile.config.json` Isolation**: Fully verified — running unit tests no longer mutates `profile.config.json` on disk.

### Task Completion Status:
- **P0 Critical Tasks (T1–T13)**: **[DONE] (VERIFIED)**
- **P1 Warning & Refactoring Tasks (T14–T32)**: **[DONE] (VERIFIED)**
- **UI/UX Focused Tasks (T33–T41)**: **[DONE] (VERIFIED)**
- **Services/Domain Tasks (T42–T52)**: **[DONE] (VERIFIED)**

---

## 🔴 Critical / Must-Fix

### PowerShell profile & config
1. **Test suite mutates the tracked production config on every run.** `ConfigServiceTests.cs` calls the real `Config.Save()` against the real `ConfigPath` (`Config.GetProfileRepoRoot()`, `Services/Domain/Config.cs:110-137`) instead of a redirected/temp path. Every `dotnet test` silently rewrites `csapp/profile.config.json` (flips `Ai.EnableOllama`/`Ai.EnableAgy`, normalizes line endings) — this is exactly the diff sitting in `git status` right now. **Fix:** give `Config` a test-overridable path (env var or constructor param) and point `ConfigServiceTests`/`ConfigTests` at a temp file.
2. **The entire nested `profile.config.json` schema is ignored by the PowerShell profile.** The profile reads flat props (`$config.PoshThemesPath`, `$config.AiMode`, `$config.ProjectsBaseDir`, etc.), but the real config file (and the C# `ConfigService`) uses nested `Ui.*`/`Ai.*`/`Project.*`/`System.*`. Every `$config.<Flat>` access is `$null`, so **every setting in the tracked config file is silently ignored** in favor of hardcoded PS-side fallbacks — e.g. `Project.BaseDir: "C:\\Users\\sshuser\\project"` is configured but never read. `Microsoft.PowerShell_profile.ps1:37-123`. **Fix:** update the PS reader to walk the nested schema, or retire it in favor of shelling out to the C# `ConfigService` (one schema, one owner).
3. **`Invoke-MasterLearningSuite` has a broken path that breaks ~19 aliases.** `Microsoft.PowerShell_profile.ps1:2027,2037` build the csproj/dll path via `Join-Path $root "AgyTuiApp\..."`, omitting the `csapp\` segment every other path in the file uses. Every alias routed through it — `learn`, `flashcard`, `vocab`, `kana`, `kanji`, `jlpt`, `grammar`, `algo`, `complexity`, `problems`, `snippets`, `sheets`, `quiz`, `interview`, `star`, `mock`, `obsidian`, `refresh`, `vault-open` — fails outright ("project file does not exist"). This is the entire "Learn & Study" feature area from `menu_map.md`, dead. **Fix:** reuse `$Global:AgyTuiAppProject` (already correct, line 146) instead of re-deriving the path.
4. **Alias/function name collisions silently break two commands.** `Set-Alias go -Value Reload-Profile` (line 1349) permanently shadows `function go { Invoke-ControlCenter "go" $args }` (line 1844) since PowerShell resolves Alias before Function — the control-center `go` shortcut is unreachable. Same pattern: `Set-Alias gcmt -Value Invoke-GitCommit` (line 1419) is overwritten by `Set-Alias gcmt -Value Invoke-GitCommitWizard` (line 1925) — `Invoke-GitCommit` is unreachable via its documented alias. The codebase already fixed this exact bug class once for `gclone`/`Clone-Project` (comment at lines 1894-1898) but reintroduced it twice. **Fix:** rename one side of each collision; add a cheap CI grep that fails if a name is registered as both alias target and function.
5. **The Ollama/Agy feature-flag gate can be bypassed.** `Program.cs:356,362` hardcode two alias allow-lists to enforce `EnableAiOllama`/`EnableAgy`, but `ollama-status`, `ollama-models`, `ollama-pull`, `ollama-start`, `ollama-logs`, `ollama-benchmark` (all marked `RequiresAiOllama: true` in `CommandRegistry.cs:240-251`) aren't in the list — invoking them directly bypasses the disabled flag entirely, contradicting the explicit "don't let either surface bypass the flag independently" guardrail in `docs/refactor_plan.md` §6. **Fix:** gate by looking up `CommandEntry.RequiresAiOllama/RequiresAgy` instead of hand-maintained lists.

### Security
6. **Silent plaintext fallback on crypto failure for OAuth tokens.** `Services/Domain/TokenVault.cs:19-22,34-37` — `Protect`/`Unprotect` swallow all `CryptographicException`s and return the input unchanged. If DPAPI fails, `AgyAccountCore.BackupActiveToken` (`AccountHelper.cs:635-650`) writes an **unencrypted** OAuth token to `keyring_token.txt` while believing it encrypted it, with no warning anywhere. `DecryptToken` (`AccountHelper.cs:680-684`) then misdiagnoses this as "decrypt failed" rather than "this is already plaintext." **Fix:** throw/log on crypto failure; never persist plaintext silently where ciphertext was expected.
7. **Unauthenticated SSH-key-planting listener.** `SystemHelper.cs:475-514` (`SshHelper.StartKeyReceiver`) opens `TcpListener(IPAddress.Any, port)` with no auth for 2 minutes — any LAN host that connects and sends an `ssh-...`-shaped line gets appended to `authorized_keys`. The sibling `StartMobileSshKeyReceiver` (line 658) requires a one-time token; this legacy path doesn't. **Fix:** require the same token scheme, or remove the unauthenticated path.
8. **SQL injection / dot-command bypass in the SQLite browser.** `Services/DatabaseHelper.cs:140` embeds free-text user SQL unescaped into a hand-quoted `sqlite3.exe` command line; a query with `"` shifts argv boundaries. Worse: the write-detection safety net (`DatabaseHelper.cs:114`, regex for `insert|update|delete|drop|create|alter|replace`) doesn't recognize sqlite3 dot-commands — a query starting with `.shell calc.exe` or `.load foo.dll` skips the backup-before-write guard and can execute arbitrary code. **Fix:** use `ArgumentList`, and either disable dot-commands (`-cmd`/`-no-shell` style flags) or block queries starting with `.`.
9. **Arbitrary file read via path traversal, shipped to the AI backend.** `Services/Domain/IdeCommandRegistry.cs:24-26` — `Path.Combine(ctx.RootPath, a[0])` has no containment check; `a[0]` can be `..\..\..` or an absolute path (which makes `Path.Combine` discard `RootPath` entirely). The subsequent `edit`/`ask` commands (lines 34-44) then read and send that file's contents to the AI provider — an arbitrary-file-read path disguised as a sandboxed file browser. **Fix:** resolve to a full path and verify it's still under `RootPath` before use.
10. **Command-injection-shaped `Invoke-Expression` in `compress_video.ps1`.** `psapp/scripts/compress_video.ps1:85-101` builds `$ffmpegArgs` as a proper array, then manually backtick-quotes filenames, joins into a string, and runs it via `Invoke-Expression` instead of `& ffmpeg @ffmpegArgs`. A filename containing backticks/`$(...)`/`;` gets re-parsed and executed by PowerShell. **Fix:** call `& ffmpeg @ffmpegArgs` directly; drop the manual quoting.
11. **NuGet API key exposed on the process command line.** `Services/DotNetHelper.cs:96-98` (`PublishPackage`) passes `--api-key {apiKey}` as a literal CLI argument, visible to any other local process/user via Task Manager/WMI while running. **Fix:** pass via `NUGET_API_KEY` env var through `RunInteractive`.

### Destructive tests running against live state
12. **`Remove-BinObj cleans bin and obj folders` (`Profile-All.Tests.ps1:71-73`) calls the real recursive bin/obj-deleting helper with no sandboxing** — if the test runner's CWD is anywhere near the repo root, this can delete real build output (including the app's own `bin`/`obj`) mid test-run, no `-WhatIf`, no guard.
13. **`Invoke-GitUndo discards uncommitted changes` invokes a real soft-reset against the live repo**, and only avoided mutating history because of an unrelated non-interactive-terminal guard that happened to skip the confirmation. The test's own `git` mock (`Profile-All.Tests.ps1:12`) is a no-op here because the real call goes through compiled `[AgyTui.GitHelper]`, not the PowerShell `git` function — false confidence that this is safely intercepted.
14. **`New-Features.Tests.ps1`'s `AgySecretVault` test writes/removes a key in what is very likely the user's real, persistent secret store**, not an isolated vault.
15. **2 of 4 PowerShell test files fail outright** (`AI-Tools.Tests.ps1`, `New-Features.Tests.ps1`) — both hardcode `C:\Users\TruongNhon\Documents\Powershell\...` (wrong username, and missing the `csapp\` segment for the DLL path), which doesn't exist on this machine. `Profile-All.Tests.ps1:20-34` shows the correct `$PSScriptRoot`-relative pattern to copy.

**Fix for 12–15:** run destructive-adjacent tests in a disposable temp directory/repo, point secret-vault tests at an isolated path, and copy the `$PSScriptRoot`-relative path pattern into the two broken files.

---

## 🟡 Warnings / Code Smells

### PowerShell profile & scripts
- Two independent, divergent "is AgyTuiApp built, else rebuild" implementations (`Load-AgyTuiDll`, lines 147-181, vs. `Invoke-MasterLearningSuite`'s inline check, lines 2037-2041) — the latter is the buggy one (Critical #3) and has no AppDomain short-circuit, which is why "AgyTuiApp binary not built. Building now..." printed 3× in one test run.
- ~150 lines of dead PS-only `class SystemHelper` (lines 1047-1217) that the file's own region comment says was superseded by `AgyTui.SystemHelper` — only `Clear-ShellHistory` still uses the old path; the dead `GetPublicIP` even uses the obsolete `System.Net.WebClient` with no timeout.
- ~16 dead methods in PS-only `class GitHelper` (lines 441-684) and a dead `DockerHelper.GetContainers` (lines 821-824) — real wrappers all call the compiled `[AgyTui.*Helper]` equivalents instead; `Get-DockerContainers` even reimplements the dead method's logic inline rather than calling it.
- 6 near-identical AI wrapper functions (`Invoke-Codex-By-Ollama`, `Invoke-Claude-By-Ollama`, `Invoke-OpenClaw-By-Ollama`, `Invoke-Clawdbot-By-Ollama`, `Invoke-Hermes-By-Ollama`, `Invoke-HermesDesktop-By-Ollama`, lines 1555-1602) — identical shape, no try/catch, already flagged in `docs/refactor_plan.md` and still unaddressed.
- `Write-AgyStartupCheckpoint` does unconditional blocking disk I/O 4× on every shell startup (lines 131-421) despite its own comment calling it "temporary diagnostic" — not gated behind any debug flag.
- `build_dev.ps1`/`publish_release.ps1` rename locked binaries to `*.old_<rand>` before rebuilding but never delete the renamed file afterward — indefinite disk-space leak.
- `docs/refactor_plan.md` itself contains stale claims (e.g. §16 says `Start-AgyManager`/`Start-AgyProxy`/`Test-AgyAiGate` are "called but never defined" — false, they're defined at lines 184-212) — treat it as a lead list to spot-check, not ground truth.
- `docs/menu_map.md` has drifted from the actual `MenuNode` tree: Docker/AWS tools were folded into `[Workspace & Dev]`, `[Theme & Settings]` was split into `[Appearance & Layout]`/`[Help & Docs]`, and a new `[Obsidian & Resources]` category exists in code but not in the doc.

### C# Services/Domain
- `Config.Save()` (`Services/Domain/Config.cs:168-207`) only regex-patches `Ui.Mode`/`Ai.Mode`/`Ui.Density`; every other field (`Ai.EnableOllama/EnableAgy`, `Project.*`, `System.*`) is never persisted — in-memory mutations of those silently revert on restart.
- Two divergent quota-webhook implementations: `QuotaTracker.TriggerLowQuotaWebhook` (`QuotaTracker.cs:34-55`) is 100% dead (zero call sites, also `async void` with I/O outside its own try block) while `AccountHelper.cs:534-554` has its own separately-wired version with a different payload shape.
- Two divergent DPAPI-encryption implementations: `TokenVault` and `AccountHelper.cs:141-260`'s `AgySecretVault` duplicate the same logic independently, plus `AgySecretVault` hardcodes `C:\Users\Public\.gemini\secrets.json` instead of respecting `AgySourceHome`.
- Unanchored substring filter (`AccountHelper.cs:338`, `Regex.IsMatch(name, "backup|copy|temp")`) hides any account whose name merely *contains* those substrings (e.g. `"Copywriter"`) from every menu, with no way to select it.
- Raw-string `ProcessRunner` argument construction (no `ArgumentList`) at multiple sites with untrusted input: `DatabaseHelper.cs:140` (free-text SQL), `DotNetHelper.cs:106` (migration name), `GitHelper.cs:105` (commit message, incomplete quote escaping), `AwsHelper.cs:20` (POSIX quotes on Windows argv, silently swallowed stderr), `AiHelper.cs:111,127,265` (raw string-built JSON and model names).
- `StudyHelper.cs`: 7-day windows computed as `AddDays(-7)` with `>=` actually span 8 days (lines 699, 889); `GetCurrentStreak` (lines 738-749) awards a bogus "grace day" streak if a log entry has a future/clock-skewed date; `LoadJson`/`SaveJson` (377-403) swallow all exceptions indistinguishably (corrupted vs. missing vs. failed-save all look the same to the user); several `NullReferenceException` risks on `System.Text.Json`-deserialized records whose non-nullability isn't actually enforced (lines 455, 460, 579, 964, 1020, 667, 682, 1056-1057, 1083) — masked by the `CS8625` suppression; unescaped user text (`DailyGoals.Show`, line 790; weekly chart label, line 707) breaks Spectre markup rendering if it contains `[`.
- `Services/Infra/TtlCache.GetOrCompute` (lines 16-25) isn't atomic — concurrent callers on a cache miss both run the (I/O-bound) factory.
- `HttpClientProvider`'s shared client has no `Timeout` set (defaults to 100s); `ResourceDiscovery.cs:274-278` bypasses the shared client with its own `new HttpClient()` anyway (SSRF/DoS-adjacent: fetches arbitrary user-added URLs with no allow-list or size cap).
- `Process` objects from `GetProcessById`/`GetProcesses()` are never disposed (`SystemHelper.cs:71,134,154,548`) — `GetProcesses()` in particular leaks one handle per process on the machine.
- Hardcoded machine-specific path `C:\Users\sshuser\project` baked into `WorkspaceRegistry.cs:74` (shared source, not a per-user config).

### C# Views/Renderers
- `ScreenChrome`'s standardized color-token set (`AccentColor`/`SuccessColor`/etc., §1 of the refactor plan) has **zero call sites outside its own declaration** — every screen still hardcodes `Color.Green`/`Color.Red`/etc. directly; the infrastructure was built but never adopted.
- `Program.RunCommand` still does its own bespoke clear+print+footer instead of routing through `ScreenChrome` — the "one command-output contract" from the plan isn't implemented.
- Four independent "pick one from a list" implementations now exist (`SpectreMenu.Show`, `SpectreMenu.ShowWithEscape`, `FlatTreeRenderer.RenderSubPageSelection`, and `ThreePaneRenderer`'s inline branches) where the plan calls for collapsing the original two into one.
- `OllamaStatusWidget.Render()` (`StatusWidgets.cs:249-252`) blocks the UI input loop on a synchronous `.Result` HTTP call on cache miss; the sibling `PublicIpWidget` four classes above does this correctly (fire-and-forget + placeholder).
- `GitNexus.cs:30` has a Spectre markup bug (`[bold]...[]` instead of `[/]`) that would throw at runtime — currently masked because `ShowLiveDashboard`/`ShowCommitGraph` are dead code, never called from anywhere.
- `MenuRendererBase.GetActiveChildren` re-reads and re-parses the config JSON from disk on every keystroke (no caching), despite `TtlCache<,>` already existing and being used by a sibling cache in the same class.
- `Icons.GetCategoryIcon`/`GetCategoryHotkey` triplicate the same category if-chain 3× instead of one lookup table; both also carry a dead unreachable branch left over from a category that was later split.
- `FileExplorer.GetFileIcon` is a stale private duplicate of `Icons.GetFileIcon`, missing several extensions and ignoring the Nerd-Font/UTF-8 fallback logic.
- Two real `CommandRegistry` aliases (`f` / "Open Explorer", and `no-auto-commit`/`autocommit`) are unreachable from the menu tree — nothing asserts that every registry alias is reachable, the same "silently desyncs" failure mode the registry itself was built to eliminate, just moved up one layer.
- Testability bug confirming the console-pollution symptom seen during `dotnet test`: `ScreenChromeTests.cs:16-21` calls the real `ScreenChrome.RenderBanner(..., forceClear: true)`, which writes raw ANSI escape codes straight to `Console`/`AnsiConsole` with no injectable output target — the test only asserts "doesn't throw," providing no real coverage while corrupting CI logs. `FlatTreeRendererTests`'s one test also doesn't call into `FlatTreeRenderer` at all — it hand-copies the clamping arithmetic into the test body.

---

## 🟢 Enhancement Opportunities (concrete, scoped)

**PowerShell**
- Fix `Invoke-MasterLearningSuite`'s path bug and the `go`/`gcmt` collisions (Critical #3/#4) — both are one-line fixes with outsized impact.
- Copy `Profile-All.Tests.ps1`'s `$PSScriptRoot`-relative path pattern into `AI-Tools.Tests.ps1` and `New-Features.Tests.ps1`.
- Replace `compress_video.ps1`'s `Invoke-Expression` with `& ffmpeg @ffmpegArgs`.
- Consolidate the 6 near-identical AI wrapper functions into one parameterized helper with consistent try/catch.
- Delete the `*.old_<rand>` backup files in `build_dev.ps1`/`publish_release.ps1` once a rebuild succeeds.
- Gate `Write-AgyStartupCheckpoint`'s 4 unconditional disk writes behind an `$env:AGY_STARTUP_DEBUG` flag.
- Delete the dead PS-only `SystemHelper`/`GitHelper`/`DockerHelper` classes now that the C# equivalents are the only ones actually called.

**C# Services/Domain**
- Make `TokenVault.Protect`/`Unprotect` log/throw on crypto failure instead of silently returning plaintext; add `TokenVaultTests` (currently zero coverage on the encryption boundary for OAuth tokens).
- Redirect `ConfigServiceTests`/`ConfigTests` to a temp `ConfigPath` so the suite stops mutating the tracked config file.
- Anchor the account-name exclusion regex (`AccountHelper.cs:338`) so it only matches whole segments, not substrings.
- Migrate the highest-risk raw-string `ProcessRunner` call sites (SQL, migration name, commit message, AWS query, Ollama model name/JSON) to `ArgumentList`-based calls.
- Fix the 7-vs-8-day window and the negative-`diff` grace-day bug in `StudyHelper.cs`; escape user-text render sites with `.EscapeMarkup()` to match the rest of the file.
- Add path-containment validation before `IdeCommandRegistry.cs` assigns `CurrentFile`.
- Require the mobile-flow's one-time-token scheme in the legacy `SshHelper.StartKeyReceiver`, or remove it.

**C# Views/Renderers**
- Route `Program.RunCommand`'s AI/Ollama/Agy gating through `CommandEntry.RequiresAiOllama`/`RequiresAgy` lookups instead of the two hardcoded alias lists.
- Wrap the config-enabled checks in `GetActiveChildren` with the existing `TtlCache<,>` abstraction to stop re-parsing JSON on every keystroke.
- Make `OllamaStatusWidget.Render()` follow `PublicIpWidget`'s fire-and-forget + placeholder pattern instead of blocking on `.Result`.
- Fix the `[bold]...[]` markup typo in `GitNexus.cs:30`, and either wire `ShowLiveDashboard`/delete the dead code.
- Add an empty-pool guard to `KanaQuiz.Run` matching `KanjiLookup.Run`'s existing pattern (currently records a fabricated 100% score on an empty session).
- Give `ScreenChrome` an injectable output seam so `ScreenChromeTests` can assert on rendered content instead of "doesn't throw," and stop leaking ANSI codes into CI logs.

---

## 🔵 Future Enhancement Ideas (larger / architectural)

- **One config system.** Retire either the PS-side flat-schema reader or the C#-side `ConfigService`, so there's a single owned schema instead of two readers that have already drifted apart (Critical #2).
- **One "shell out safely" abstraction.** Mark the raw-string `ProcessRunner.Run`/`RunCapture` overloads `[Obsolete]` and force new call sites onto an `ArgumentList`-only path (the safe pattern already exists and is used correctly elsewhere in `AiHelper.cs`).
- **Actually adopt the `ScreenChrome` color-token set and consolidate the four "pick from a list" implementations** into the single `MenuNode` leaf-list primitive `docs/refactor_plan.md` §1.3 already specifies — the plan and the infrastructure both exist; only the migration is missing.
- **Deterministic time for `StudyHelper`.** Introduce a `TimeProvider`/clock abstraction (the pattern already exists in `AccountHelper.cs`) so streak/window logic becomes testable instead of bound to the real system clock — likely why the 7-vs-8-day and grace-day bugs went unnoticed this long.
- **Upgrade Pester to 5.x and add PSScriptAnalyzer to CI.** The bundled Pester 3.4.0 has no `-CodeCoverage` and dated mocking; PSScriptAnalyzer's built-in rules (`PSAvoidUsingInvokeExpression`, `PSUseDeclaredVarsMoreThanAssignments`) would have caught several findings above mechanically.
- **Extract testable seams from `FlatTreeRenderer.Run()`/`ThreePaneRenderer.Run()`** (currently 600–1000+ line monolithic methods) — pure static functions for visible-row flattening, selection clamping, and search filtering, so tests exercise production code instead of re-derived copies.
- **Add a CI assertion that every `CommandRegistry` alias is reachable from `MenuNodeBuilder.BuildTree()`**, mirroring the existing `AssertSwitchCases` check, to catch orphaned aliases like `f`/`no-auto-commit` automatically.
- **Reconcile `docs/menu_map.md` with the real menu tree**, and treat `docs/refactor_plan.md`'s "resolved" claims as unverified until spot-checked (several are stale).

---

## Test Coverage Gaps (confirmed by reading test files, not assumed)

**Zero coverage:** `TokenVault`, `AgyKeyringHelper`, `AgySecretVault`, most of `AccountHelper.cs`/`AgyAccountCore`, all of `AiHelper.cs` (`AgyAiCore`, `OllamaHelper`, `AntigravityDeckHelper`), `ProcessRunner`, `AwsHelper`, `DockerHelper`, `GitHelper`, `DotNetHelper`, `SystemHelper`/`SshHelper`, `GuidedLearnFlow`, `ObsidianGraph`/`ObsidianStudySync`, `SkillService`, `DatabaseHelper`, `AiLearningGenerator`, `IdeCommandRegistry`, `ProjectScaffolder`, `WorkspaceRegistry`, `EditorResolver`, `HttpClientProvider`, `ThemeHelper`, and most of `StudyHelper.cs` (`FlashcardEngine`, `DailyGoals`, `StudyStreak`, `DueReview`, `ProgressDashboard`, `WordOfDay`) — notably including the exact methods where the streak/window bugs above live.

**PowerShell side, zero Pester coverage:** `Invoke-ControlCenter` and its ~14 passthrough shortcuts, the entire Master Learning Suite router (whose path bug would've been caught immediately by any test invoking it), the full Docker/AWS/Git command surface beyond one smoke test each, `ProfileEnvironment.InitializeSession()`, `AgyAccountManager`'s actual switching behavior (only existence is checked), and all of `psapp/scripts/*.ps1` (`cs-minify.ps1`/`cs-deminify.ps1` in particular are pure deterministic text transforms — cheap, high-value round-trip tests would be easy to add).

---

## 🛠️ Task Breakdown (Agent-Ready)

Each task below is scoped to be picked up independently by a coding agent with no other context beyond this file — it states the file(s), the defect, the fix, and how to verify it. Ordered by priority. `Depends on` notes cross-task ordering where it matters; everything else can be parallelized.

### P0 — Critical (security / broken features)

- **T1. Stop tests from mutating the tracked `profile.config.json`.**
 Files: `csapp/AgyTuiApp/Services/Domain/Config.cs`, `csapp/AgyTuiApp.Tests/Unit/ConfigServiceTests.cs`, `ConfigTests.cs`.
 Fix: give `Config`/`ConfigService` a test-overridable config path (env var or constructor/static-init param); update both test files to point at a temp file instead of the real repo path.
 Verify: run `dotnet test` twice in a row; `git status` on `csapp/profile.config.json` must stay clean.

- **T2. Make the PowerShell profile read the real (nested) config schema.**
 Files: `Microsoft.PowerShell_profile.ps1:37-123`.
 Fix: update every `$config.<Flat>` read (`PoshThemesPath`, `AiMode`, `VerboseStartup`, `StartupLogFile`, `ProjectsBaseDir`, `AgySourceHome`, `GlobalBinDir`) to walk the nested shape (`$config.System.PoshThemesPath`, `$config.Ai.Mode`, `$config.Project.BaseDir`, etc.) matching `csapp/profile.config.json` and the C# `ConfigService`.
 Verify: set a non-default value in `Project.BaseDir` in `profile.config.json`, reload the profile, confirm the workspace picker actually uses it instead of the hardcoded fallback.

- **T3. Fix the broken path in `Invoke-MasterLearningSuite`.**
 Files: `Microsoft.PowerShell_profile.ps1:2027,2037`.
 Fix: replace the re-derived `Join-Path $root "AgyTuiApp\..."` paths with `$Global:AgyTuiAppProject` (already correct, line 146) / its `dist`/`bin` DLL equivalent.
 Verify: run `learn`, `flashcard`, and `quiz` from a fresh shell; none should print "project file does not exist."

- **T4. Fix the `go` and `gcmt` alias/function collisions.**
 Files: `Microsoft.PowerShell_profile.ps1:1349,1419,1844,1925`.
 Fix: rename the control-center passthrough (e.g. `function go` → `function Invoke-GoTo` with alias `goto`, following the `gclone`/`Clone-Project` precedent at lines 1894-1898) and remove or rename the dead `Set-Alias gcmt -Value Invoke-GitCommit` at line 1419 so it doesn't shadow the wizard.
 Verify: `Get-Alias go`, `Get-Alias gcmt` resolve to the intended target; `(Get-Command go).CommandType` is what you expect.

- **T5. Close the Ollama/Agy feature-flag bypass in `Program.cs`.**
 Files: `csapp/AgyTuiApp/Program.cs:356,362`, `csapp/AgyTuiApp/CommandRegistry.cs:240-251`.
 Fix: replace the two hardcoded alias allow-lists with a lookup against `CommandRegistry.All`'s `RequiresAiOllama`/`RequiresAgy` flags for the invoked alias.
 Verify: with `Ai.EnableOllama: false` in config, `AgyTuiApp.exe ollama-status` must refuse to run (same message as `claude-ollama` does today).

- **T6. Fix `TokenVault`'s silent plaintext fallback.**
 Files: `csapp/AgyTuiApp/Services/Domain/TokenVault.cs:19-22,34-37`, `Services/AccountHelper.cs:635-650,680-684`.
 Fix: on `CryptographicException`, throw (or return a distinct failure sentinel) instead of returning the input unchanged; update `BackupActiveToken`/`DecryptToken` to handle that failure explicitly rather than comparing `res == encrypted`.
 Verify: add a unit test that forces DPAPI to fail (e.g. mismatched protection scope) and asserts no plaintext is written to `keyring_token.txt`.

- **T7. Require authentication on the legacy SSH key receiver.**
 Files: `csapp/AgyTuiApp/Services/SystemHelper.cs:475-514` (`SshHelper.StartKeyReceiver`), compare `StartMobileSshKeyReceiver` at line 658.
 Fix: either delete `StartKeyReceiver` if unused, or require the same one-time-token handshake `StartMobileSshKeyReceiver` already implements before appending to `authorized_keys`.
 Verify: connecting to the listener without presenting a valid token must not add a key.

- **T8. Fix SQL injection / dot-command bypass in the SQLite browser.**
 Files: `csapp/AgyTuiApp/Services/DatabaseHelper.cs:114,140`.
 Fix: switch the `sqlite3.exe` invocation to an `ArgumentList`-based call (no hand-quoted string); reject or strip queries starting with `.` (dot-commands) before they reach the write-detection regex, or launch `sqlite3` with dot-commands disabled.
 Verify: a query like `select 1; -- "` and a query starting with `.shell` are both rejected/neutralized rather than executed.

- **T9. Fix path traversal in `IdeCommandRegistry`.**
 Files: `csapp/AgyTuiApp/Services/Domain/IdeCommandRegistry.cs:24-26,34-44`.
 Fix: resolve `Path.Combine(ctx.RootPath, a[0])` via `Path.GetFullPath` and verify the result still starts with `Path.GetFullPath(ctx.RootPath)` before assigning `ctx.CurrentFile`; reject otherwise.
 Verify: `open ..\..\..\Windows\System32\drivers\etc\hosts` (or an absolute path) is rejected instead of loaded.

- **T10. Replace `Invoke-Expression` with direct invocation in `compress_video.ps1`.**
 Files: `psapp/scripts/compress_video.ps1:85-101`.
 Fix: call `& ffmpeg @ffmpegArgs` directly; delete the manual backtick-quoting and the `Invoke-Expression $cmdLine` string-building path.
 Verify: compress a file whose name contains a space and one containing a backtick; both must complete without re-parsing errors.

- **T11. Stop putting the NuGet API key on the process command line.**
 Files: `csapp/AgyTuiApp/Services/DotNetHelper.cs:96-98`.
 Fix: pass the key via the `NUGET_API_KEY` environment variable through `RunInteractive`'s `env` param instead of `--api-key {apiKey}` in the argument string.
 Verify: run `PublishPackage` and confirm (e.g. via Process Explorer or `Get-Process ... -ea silentlycontinue | select CommandLine` while it runs) the key no longer appears in the visible command line.

- **T12. Sandbox destructive PS tests.**
 Files: `psapp/Tests/Unit/Profile-All.Tests.ps1` (`Remove-BinObj cleans bin and obj folders` ~line 71-73, `Invoke-GitUndo discards uncommitted changes` ~line 87-89), `psapp/Tests/Unit/New-Features.Tests.ps1` (`AgySecretVault` test lines 6-13).
 Fix: `Push-Location` into a freshly created temp directory (with its own throwaway git repo for the git test) before each of these `It` blocks and `Pop-Location`/cleanup after; point the secret-vault test at an isolated vault path (env var override) instead of the real store.
 Verify: run the suite from inside the real repo root and confirm no real `bin`/`obj` folders, git history, or secret-store entries are touched (diff `git status` before/after, confirm real secret store file unchanged).

- **T13. Fix the two PS test files with hardcoded wrong paths.**
 Files: `psapp/Tests/Unit/AI-Tools.Tests.ps1:6,14`, `psapp/Tests/Unit/New-Features.Tests.ps1:3`. Reference pattern: `psapp/Tests/Unit/Profile-All.Tests.ps1:20-34`.
 Fix: replace the hardcoded `C:\Users\TruongNhon\Documents\Powershell\...` paths with the `$PSScriptRoot`-relative resolution already used correctly in `Profile-All.Tests.ps1` (including the `csapp\` segment for the DLL path).
 Verify: `pwsh -File psapp/Tests/run_tests.ps1` reports 20/20 passed (currently 18/20).

### P1 — Warnings / code smells

- **T14. Delete the dead PS-only `SystemHelper`/`GitHelper`/`DockerHelper` classes.**
 Files: `Microsoft.PowerShell_profile.ps1:441-684` (`GitHelper`, keep only what's still called: `GenerateAiCommitMessage`, `MergeSquash`, `StashSnapshot`, `Gcmt`), `:1047-1217` (`SystemHelper`, keep only `ClearHistory`), `DockerHelper.GetContainers` (~lines 821-824).
 Fix: remove unused methods; confirm no remaining call sites via `Select-String` before deleting each.
 Verify: `pwsh -File psapp/Tests/run_tests.ps1` still passes; profile still loads with no errors.

- **T15. Consolidate the 6 near-identical AI wrapper functions.**
 Files: `Microsoft.PowerShell_profile.ps1:1555-1602`.
 Fix: replace `Invoke-Codex-By-Ollama`, `Invoke-Claude-By-Ollama`, `Invoke-OpenClaw-By-Ollama`, `Invoke-Clawdbot-By-Ollama`, `Invoke-Hermes-By-Ollama`, `Invoke-HermesDesktop-By-Ollama` with one parameterized `Invoke-AiTool($MethodName, $FallbackCmd, $args)` helper, wrapped in try/catch; keep the original function names as thin one-line callers so existing aliases keep working.
 Verify: `claude`, `codex`, `openclaw`, `hermes` aliases still resolve and behave the same; a forced Ollama error now prints a friendly message instead of a raw stack trace.

- **T16. Stop leaking `*.old_<rand>` backup binaries.**
 Files: `psapp/scripts/build_dev.ps1:15-19`, `psapp/scripts/publish_release.ps1:29-32`.
 Fix: delete the renamed old file in a `finally` block once the new build/publish succeeds.
 Verify: run each script twice; no accumulating `*.old_*` files remain in the output directory.

- **T17. Gate `Write-AgyStartupCheckpoint`'s disk I/O behind a debug flag.**
 Files: `Microsoft.PowerShell_profile.ps1:131-140` (and call sites at 140,214,379,421).
 Fix: no-op the function unless `$env:AGY_STARTUP_DEBUG` (or `System.VerboseStartup` from config, once T2 lands) is set.
 Verify: time shell startup before/after; checkpoint file is not written by default.

- **T18. Consolidate the duplicate quota-webhook implementations.**
 Files: `csapp/AgyTuiApp/Services/Domain/QuotaTracker.cs:34-55`, `csapp/AgyTuiApp/Services/AccountHelper.cs:534-554`.
 Fix: delete `QuotaTracker.TriggerLowQuotaWebhook` (confirmed zero call sites) or make `AccountHelper`'s version call into it; fix the `async void` + pre-try-block file I/O either way.
 Verify: existing quota tests (`QuotaMetricsTests.cs`) still pass; no remaining duplicate payload-building code.

- **T19. Consolidate the duplicate DPAPI-encryption implementations.**
 Files: `csapp/AgyTuiApp/Services/Domain/TokenVault.cs`, `csapp/AgyTuiApp/Services/AccountHelper.cs:141-260` (`AgySecretVault`).
 Fix: have `AgySecretVault` call `TokenVault.Protect`/`Unprotect` instead of reimplementing DPAPI logic; make its storage path respect `Config.Current.AgySourceHome` instead of the hardcoded `C:\Users\Public\.gemini\secrets.json`.
 Depends on: T6 (fix `TokenVault` first, then point `AgySecretVault` at the fixed version).
 Verify: `Invoke-SecretVault -Action set/get/list/remove` still works end-to-end; secrets land under the configured `AgySourceHome`.

- **T20. Anchor the account-name exclusion regex.**
 Files: `csapp/AgyTuiApp/Services/AccountHelper.cs:338`.
 Fix: change `Regex.IsMatch(name, "backup|copy|temp", RegexOptions.IgnoreCase)` to match whole name segments only (e.g. `^(backup|copy|temp)([_-]|$)` or an exact-match/prefix check appropriate to the actual naming convention), not arbitrary substrings.
 Verify: an account named `"Copywriter"` or `"template-project"` now appears in `GetAccounts()`.

- **T21. Migrate remaining raw-string `ProcessRunner` call sites to `ArgumentList`.**
 Files: `csapp/AgyTuiApp/Services/DotNetHelper.cs:106` (migration name), `Services/GitHelper.cs:105` (commit message), `Services/AwsHelper.cs:20` (JMESPath query), `Services/AiHelper.cs:111,127,265` (JSON body, model name).
 Fix: switch each to the `ArgumentList`/array-based `ProcessRunner` overload (add one if it doesn't exist yet) instead of string interpolation into a single `args` string; for `AiHelper.cs:111,127` build the JSON via `JsonSerializer.Serialize` instead of string interpolation.
 Depends on: T8 uses the same `ArgumentList` pattern — do it once, reuse everywhere.
 Verify: a migration name or commit message containing `"` no longer breaks argument parsing.

- **T22. Fix the 7-vs-8-day window and grace-day bugs in `StudyHelper.cs`.**
 Files: `csapp/AgyTuiApp/Services/StudyHelper.cs:699-700,889-890` (window), `:738-749` (`GetCurrentStreak`).
 Fix: change `DateTime.Today.AddDays(-7)` + `>=` to `AddDays(-6)` for an inclusive 7-day window (or keep `-7` and use `>` — pick one and make both call sites consistent); add a guard in `GetCurrentStreak` that rejects/ignores log entries with a future date instead of letting a negative `diff` satisfy the grace-day check.
 Verify: add unit tests seeding exactly 7 and 8 days of log entries and asserting the reported day-count; add a test seeding a future-dated entry and assert it doesn't fabricate a streak.

- **T23. Escape user-supplied text in `StudyHelper.cs` Spectre renders.**
 Files: `csapp/AgyTuiApp/Services/StudyHelper.cs:707,790`.
 Fix: apply `.EscapeMarkup()` to `t.Topic`/`t.Activity` in `DailyGoals.Show` and to the bar-chart label in `ShowWeeklyChart`, matching the pattern already used elsewhere in the same file (lines 487, 1006, 1042).
 Verify: a goal/topic containing `[red]` or `[[` renders literally instead of breaking/altering console styling.

- **T24. Make `Config.Save()` persist all fields, not just three.**
 Files: `csapp/AgyTuiApp/Services/Domain/Config.cs:168-207`.
 Depends on: T1 (fix test isolation first so this change can be safely tested against a temp file).
 Fix: extend the regex-line-patch logic to cover `Ai.EnableOllama`/`Ai.EnableAgy`, `Project.*`, `System.*`, or replace the line-patching approach with a comment-preserving JSON round-trip (e.g. via `JsonNode`) that updates arbitrary fields.
 Verify: mutate each field in-memory, call `Save()`, reload, confirm the value survived — for every field, not just `Ui.Mode`/`Ai.Mode`/`Ui.Density`.

- **T25. Dispose leaked `Process` handles.**
 Files: `csapp/AgyTuiApp/Services/SystemHelper.cs:71,134,154,548`.
 Fix: wrap `GetProcessById`/`GetProcessesByName`/`GetProcesses()` results in `using`/`try-finally` and call `.Dispose()` after use.
 Verify: run `Invoke-SystemMonitor`/`Stop-ProcessFriendly` in a loop and confirm handle count (Task Manager "Handles" column for the AgyTuiApp process) doesn't grow unbounded.

- **T26. Stop `OllamaStatusWidget` from blocking the UI thread.**
 Files: `csapp/AgyTuiApp/Views/Components/StatusWidgets.cs:249-252` (compare `PublicIpWidget` pattern at lines 62-93).
 Fix: return a `"Checking..."` placeholder immediately on cache miss and refresh the cached value via `Task.Run`, instead of calling `.Result` synchronously inside `Render()`.
 Verify: with the Ollama daemon stopped/unreachable, opening the AI Agent menu no longer stalls keyboard input for the HTTP timeout.

- **T27. Fix the `GitNexus.cs` markup typo and resolve its dead code.**
 Files: `csapp/AgyTuiApp/Views/Screens/Git/GitNexus.cs:24-60` (`ShowLiveDashboard`, `ShowCommitGraph`), line 30 specifically.
 Fix: change `[bold]{col.EscapeMarkup()}[]` to `[bold]{col.EscapeMarkup()}[/]`; then either wire `ShowLiveDashboard`/`ShowCommitGraph` into an actual command (e.g. under `nexus`) or delete them if `RepoGraph.Show` fully supersedes them.
 Verify: if wired in, invoking it renders without a markup exception; if deleted, `dotnet build` still succeeds with no new dead-code warnings.

- **T28. Add an empty-pool guard to `KanaQuiz.Run`.**
 Files: `csapp/AgyTuiApp/Views/Screens/Quizzes/KanaQuiz.cs` (`Run`, ~lines 38-45), compare `KanjiLookup.Run` (~lines 135-139).
 Fix: if the due/pool set (`Hiragana`/`Katakana`) is empty, show a `SpectrePanel.Warning` and return early instead of completing a 0-question session and recording a fabricated 100% score.
 Verify: clear/empty the kana pool and confirm the quiz warns instead of logging a fake perfect session.

- **T29. Cache the config-enabled checks used by `GetActiveChildren`.**
 Files: `csapp/AgyTuiApp/Views/Renderers/MenuRendererBase.cs:12-42`, `csapp/AgyTuiApp/Services/AiHelper.cs:458-475` (`IsAiOllamaEnabled`/`IsAgyEnabled`).
 Fix: wrap both checks in a short-TTL `TtlCache<Unit,bool>` (or a single cached bool invalidated on config reload) instead of re-reading/re-parsing the config file on every call — currently invoked on every arrow-key press and every search keystroke in both renderers.
 Verify: add a quick benchmark/log-count showing `File.ReadAllText` on the config path no longer fires once per keystroke.

- **T30. Give `ScreenChrome` a testable output seam.**
 Files: `csapp/AgyTuiApp/Views/Components/ScreenChrome.cs:114-182` (`RenderBanner`, `forceClear` path at line 139), `csapp/AgyTuiApp.Tests/Unit/ScreenChromeTests.cs`.
 Fix: accept an `IAnsiConsole` (Spectre supports this) or `TextWriter` parameter instead of writing to `Console`/`AnsiConsole` statics directly; update `ScreenChromeTests` to inject a test console and assert on actual rendered content instead of only "does not throw."
 Verify: `dotnet test` no longer prints raw ANSI/breadcrumb text to the console during the `ScreenChromeTests` run; the tests assert real content.

- **T31. Deduplicate `Icons` category lookup and `FileExplorer`'s stale icon duplicate.**
 Files: `csapp/AgyTuiApp/Views/Components/Icons.cs:85-148` (`GetCategoryIcon`/`GetCategoryHotkey`), `csapp/AgyTuiApp/Views/Screens/Ide/FileExplorer.cs:53-64` (`GetFileIcon`).
 Fix: replace the triplicated if-chains in `Icons.cs` with one lookup table keyed by category holding all icon variants (Nerd Font / UTF-8 emoji / ASCII); delete `FileExplorer.GetFileIcon` and call `Icons.GetFileIcon` instead. Also remove the dead unreachable `"theme"`/`"setting"` branch left over from the category split.
 Verify: file explorer icons match the rest of the app in both Nerd-Font and plain-ASCII terminal modes; adding a new category only requires editing one table.

- **T32. Add a build-time reachability check for `CommandRegistry` aliases.**
 Files: `csapp/AgyTuiApp/CommandRegistry.cs` (near `AssertSwitchCases`, lines 388-408), `csapp/AgyTuiApp/Views/MenuNode.cs` (`MenuNodeBuilder.BuildTree`).
 Fix: add an `AssertAllAliasesReachable()` check (mirroring `AssertSwitchCases`) that walks the built `MenuNode` tree and fails if any `CommandRegistry.All` alias (currently `f`, `no-auto-commit`/`autocommit`) isn't present as a leaf; call it alongside `AssertSwitchCases()` at startup/in tests.
 Verify: the new assertion currently fails (proving it catches the known gap); either add `f`/`no-auto-commit` to the menu tree or remove them from the registry to make it pass.

---

## 🎛️ UI/UX Focused Review — Render Flow, Reuse, Hotkeys, Scroll, Mobile Mode, Search

Follow-up deep dive specifically on `FlatTreeRenderer.cs`, `ThreePaneRenderer.cs`, `ScreenChrome.cs`, `MenuRendererBase.cs`, and `SpectreWidgets.cs` (`SpectreMenu`, `SpectrePager`), cross-referenced against the mobile-mode wiring in `ThemeHelper.cs`/`Program.cs`/`Config.cs`. This confirms and sharpens several items from the section above and adds new, narrower findings about interaction consistency.

### The core finding: six independent "scrollable list" implementations

The app has one recurring UI primitive — *a vertically scrollable, keyboard-navigable list with a viewport window* — implemented **six separate times**, each with different viewport math, different paging behavior, and a different search paradigm:

| # | Where | Viewport algorithm | PageUp/PageDown | Search |
|---|---|---|---|---|
| 1 | `FlatTreeRenderer` main tree (`FlatTreeRenderer.cs:685-699`) | dynamic `maxRows` from `Console.WindowHeight`, minimal-scroll (only scrolls when selection leaves view) | fixed ±10 rows, **not** relative to `maxRows` | live incremental filter (`/` prefix, updates every keystroke) |
| 2 | `FlatTreeRenderer` theme sub-picker (`FlatTreeRenderer.cs:897-903`) | hardcoded `maxRows = 12`, same minimal-scroll math | none (no PageUp/Down handling for this branch beyond the generic details-mode ±10 at lines 309-314) | live incremental filter via `_detailsSearchBuffer` |
| 3 | `FlatTreeRenderer` proj/workspace sub-picker (`FlatTreeRenderer.cs:972-980`) | `isMobile ? 5 : clamp(3,6, termH-18)`, same minimal-scroll math, **only place in the app with a mobile-aware row count** | generic ±10 (lines 309-314) | live incremental filter via `_detailsSearchBuffer` |
| 4 | `SpectreMenu.ShowWithEscape` (`SpectreWidgets.cs:95-174`) | `pageSize` from `WindowHeight-8` clamped 5-15, **center-selection-in-viewport** math (different formula from 1-3) | full `pageSize` (correctly relative to viewport — unlike #1) | none — no filter at all |
| 5 | `SpectrePager.Show` (`SpectreWidgets.cs:177-276`) | `pageSize = WindowHeight-8` (unclamped), non-overlapping full-page jumps | full `pageSize` | **modal** vi-style: blocks on `Console.ReadLine()`, jumps to next match, no live filtering |
| 6 | `SpectreMenu.Show`/`BuildPrompt` via Spectre's native `SelectionPrompt` (`SpectreWidgets.cs:83-93`) | Spectre's own built-in pager (`PageSize`, `MoreChoicesText`) | Spectre's own (unknown to the rest of the app) | Spectre's own built-in `SearchEnabled` type-ahead — different visual style/hint text than 1-3 |

None of these six shares code with any other, so a fix to one (e.g. the `FlatTreeRenderer` scroll-flash fix from a recent commit, or a future "smooth scroll" improvement) doesn't propagate to the rest, and the user gets a different paging feel and a different search paradigm depending on which specific screen they're on — despite `docs/refactor_plan.md` explicitly calling for "one sub-page list contract."

**Enhancement:** extract one `Views/Components/ScrollableListView` (or similar) with a single viewport-computation function (`ComputeViewport(total, selected, maxVisible) -> (topRow, endRow)`), a single "N items above/below" indicator renderer (currently copy-pasted verbatim at `FlatTreeRenderer.cs:701-704`, `794-797`, `905-908`, `920-923`, `982-985`, `1053-1056`), viewport-relative PageUp/Down everywhere, and one pluggable search mode (live-filter, the pattern used in 3 of the 6 places today, is the best UX and should become the only one). Route `SpectreMenu`, `SpectrePager`, and all of `FlatTreeRenderer`'s sub-pickers through it. `ThreePaneRenderer` should get the same component once it needs list rendering, instead of inventing a 7th variant.

### Render / re-render flow

- `ScreenChrome.RenderFrame`/`WriteSmooth` (`ScreenChrome.cs:55-99`) is a good, already-shared abstraction (cursor-home + `\x1b[K` per line instead of full clear, `forceClear` only on structural changes) — this is the "butter-smooth rendering" fix from the recent commit history, and it's correctly reused by both `RenderTree` and `RenderSubPageSelection`. No new finding here beyond confirming it works as intended.
- **`ScreenChrome.RenderBanner`'s static footer hint line is renderer-mode-specific and wrong for `flat-tree`.** `ScreenChrome.cs:179`: `"[[Tab/→]] Navigate Panes | [[←/Esc]] Go Back | [[Enter]] Select & Run"` describes `ThreePaneRenderer`'s Tab-between-panes model. `FlatTreeRenderer` calls this same method (`FlatTreeRenderer.cs:187`) but has no panes at all — a flat-tree user sees a hint for a navigation model that doesn't apply to the screen they're looking at, while `FlatTreeRenderer`'s *own* accurate hotkey bar (`FlatTreeRenderer.cs:829`) is a second, separately-hardcoded, and separately wrong (see below) footer. **Fix:** make the footer hint line a parameter of `RenderBanner`/`RenderFrame`, supplied per-renderer (or per-screen, once `IMenuRenderer` differentiates), instead of one hardcoded three-pane-flavored string shared by both.
- **`RenderBanner`'s short-banner mode (`winHeight < 45`, lines 144-158) is entirely height-driven and has no width-based truncation.** On a narrow (mobile SSH) but tall terminal, the full 6-line ASCII-art banner (lines 160-165) still renders, and the breadcrumb/title line (`Control Center v3.0 | Account: ... | Time: ...`) is built as one long unwrapped markup string with no `winWidth`-based shortening — it will wrap unpredictably in a 40-column terminal. Contrast with `FlatTreeRenderer.RenderTree`, which does compute `winWidth` and clamp panel width (`FlatTreeRenderer.cs:800-802`). **Fix:** thread the same `winWidth` check into `RenderBanner` and drop to a one-line/no-ASCII-art banner below some width threshold, the same way it already drops to a shorter banner below a height threshold.

### Hotkeys & interaction consistency

- **The hotkey footer bar advertises shortcuts the C# TUI doesn't handle.** `FlatTreeRenderer.cs:829` hardcodes `cg`/`cdk`/`cnav`/`cai`/`csys`/`cnet`/`cssh` and `[Ctrl+Space]`/`[Ctrl+Shift+C]`/`[Ctrl+Shift+B]`/`[Ctrl+Shift+T]`/`[F7]` — these are PowerShell profile aliases and PSReadLine chords that only work at the *shell prompt*, not inside the running `AgyTuiApp` process reading raw `Console.ReadKey`. A user who presses `Ctrl+Shift+B` while inside the flat-tree menu (because the menu itself told them to) gets nothing. This is on every single frame regardless of context, contradicting the plan's explicit "context-sensitive, not a static legend" requirement. **Fix:** replace with a hint bar built from what's actually bound in `FlatTreeRenderer.Run()`'s own key switch (`↑/↓ j/k`, `/` search, `Enter` expand/run, `Esc/q` back, plus whatever's contextually valid for the highlighted row — e.g. `[A]dd/[D]elete/L[o]gout` only when an account row is selected), not PowerShell-level bindings.
- **Typed characters are silently swallowed with zero effect in the `agyswitch` and `learn`/`session`/`weak` sub-pickers.** `FlatTreeRenderer.cs:536-542`: the `default:` case appends any printable, non-`a/d/o` character to `_detailsSearchBuffer` for *every* `detailsMode`, including `agyswitch` and `learn`/`session`/`weak`. But the `itemsCount` computation (lines 270-290) and the `RenderSubPageSelection` bodies for those two modes (lines 848-870, 928-939) **never read `_detailsSearchBuffer` at all** — only `theme` and `proj` actually filter by it. Net effect: a user in the account-switcher who types e.g. `"g"` to try to jump to an account starting with "g" sees nothing happen, with no feedback that the keystroke did anything (it silently mutates a buffer nobody renders or checks). This is a real, user-visible dead-input bug, not just an inconsistency. **Fix:** either wire live-filter into `agyswitch` (filter the account list by name) and `learn`/`session`/`weak` (filter the topic list) the same way `theme`/`proj` already do, or stop capturing keystrokes into `_detailsSearchBuffer` for modes that don't use it.
- **`PageUp`/`PageDown` jump a fixed 10 rows instead of one viewport height.** `FlatTreeRenderer.cs:226,232,563,560` (search mode and normal-mode tree) hardcode `± 10`. On a maximized terminal where `maxRows` (line 688) might be 30+, PageDown undershoots badly; on a small terminal where `maxRows` might be 3-5 (the `Math.Max(3, ...)` floor at line 688), PageDown overshoots past the visible window every time. Compare `SpectreMenu.ShowWithEscape` (`SpectreWidgets.cs:155,159`), which correctly pages by the actual computed `pageSize`. **Fix:** page by `maxRows` (recomputed each frame, same as the render pass) instead of the constant `10`.
- **The expanded live-widget row (`VisibleRowType.Widget`) has no keybinding of its own.** If the user arrows down so the *selected* row is the inline live panel itself (e.g. the expanded `/disk` widget block, not its parent command row), `Enter`/`RightArrow` (`FlatTreeRenderer.cs:577-636`) and `LeftArrow` (`FlatTreeRenderer.cs:638-655`) both switch only on `row.Type == VisibleRowType.Command`, so none of the branches match a `Widget` row — every key except the global nav keys is a no-op while the cursor sits on that row, including the collapse action, which only works if the user first moves the cursor back up to the parent command row. **Fix:** either skip `Widget` rows entirely during Up/Down navigation (treat them as display-only, not a stop the cursor can land on), or make `Enter`/`Left` on a `Widget` row collapse it directly (look up the row's index-1 parent to get the alias).
- **`LeftArrow` only collapses; it never moves focus to the parent when already collapsed.** `FlatTreeRenderer.cs:638-655`: pressing Left on an already-collapsed category/group, or on a plain command row, does nothing. Many tree UIs (VS Code's explorer, `claude-cli`'s own `/` menu that this feature is modeled on) move the cursor *up to the parent* in that case. **Enhancement:** when Left has nothing to collapse, move `selectionIndex` to the nearest preceding row with a smaller `Indent`.

### Mobile mode: three disconnected signals

There are three independent "are we in mobile/narrow context" signals in the codebase, and they don't talk to each other:

1. **`ThemeHelper.IsMobileModeActive()`** (`ThemeHelper.cs:53-56`) — reads `enable_mobile` from the oh-my-posh theme config; drives only the *shell prompt* (stacked ASCII prompt vs. rich Unicode prompt). Toggled by the PowerShell `mobile`/`mobile-setup` aliases (`Microsoft.PowerShell_profile.ps1:1334-1341`).
2. **`Config.Current.Density` ("compact"/"comfortable")** (`Services/Domain/Config.cs:13,52`) — drives only `FlatTreeRenderer`'s description-hiding behavior (`FlatTreeRenderer.cs:677,762`: hides the `· description` suffix on non-selected rows when compact). Toggled by the `density` command (`Program.cs:813-818`) — and *also* by `mobile-setup` (`Program.cs:760-763`), which is the **only** place that flips both signals together.
3. **An ad-hoc third check, local to one screen:** `FlatTreeRenderer.cs:953`: `bool isMobile = winWidth < 90 || activeTheme.EndsWith("-mobile", StringComparison.OrdinalIgnoreCase);` — computed independently inside the `proj` sub-picker only, re-deriving "is the active theme mobile" by string-matching the theme name directly instead of calling `ThemeHelper.IsMobileModeActive()`, and additionally treating any narrow terminal (<90 cols) as mobile regardless of the theme setting.

Consequences:
- Running the standalone PowerShell `mobile` alias (prompt-only) does **not** set `Ui.Density` to `compact`, so the TUI still renders in full "comfortable" mode — a mobile SSH user following the documented `mobile` command doesn't get a denser TUI unless they separately remember `mobile-setup` (or `density`) exists. `menu_map.md`/`docs/refactor_plan.md` don't document this gap.
- The width-based auto-mobile heuristic (`winWidth < 90`) exists in exactly **one** of ~6 renderable screens (the `proj` picker). Every other screen — the main tree, the theme picker, the account switcher, the topic picker, and all of `ThreePaneRenderer` — ignores terminal width entirely for layout purposes (only `RenderTree`'s panel *width* clamps to `winWidth`, per above; nothing switches from a wide table to a stacked-card layout the way the `proj` picker does).
- `ThreePaneRenderer` has **zero** references to `Density`, mobile, or window width/height anywhere in the file — a user on a narrow/mobile terminal who has `Ui.Mode: "three-pane"` gets no adaptation at all, not even the `Density`-driven description-hiding that flat-tree gets.

**Enhancement (concrete):** add `Config.IsMobileContext()` (`ThemeHelper.IsMobileModeActive() || Density == "compact" || Console.WindowWidth < <threshold>`) as the *one* place this is decided, and have the `proj` picker's `isMobile` local variable, `FlatTreeRenderer`'s compact-description logic, and any future width-aware layout switches all call it — so `mobile` (prompt), `density` (TUI), and raw terminal width converge on one signal instead of three.

**Future enhancement:** since `mobile-setup` already treats "mobile" as a single combined concept (prompt theme + TUI density), consider extending that combined toggle to also force a stacked/card layout (like the `proj` picker already has) across *every* list screen — theme picker, account switcher, topic picker — when active, rather than leaving `proj` as the only screen that actually reflows for narrow terminals.

### Filter/search: three different paradigms, one dead path

- **Live incremental filter** (type-ahead, updates the visible list every keystroke, no Enter needed to see results) — used by `FlatTreeRenderer`'s main tree search and its `theme`/`proj` sub-pickers. This is the best of the three and matches the `claude-cli`-style UX the flat-tree mode is modeled on.
- **Modal vi-style search** (`/` or `f`, blocks on `Console.ReadLine()`, jumps to the next match, no live filtering, `SpectreWidgets.cs:251-263`) — used only by `SpectrePager.Show` (the generic text pager used for help docs / long output).
- **Spectre's native `SelectionPrompt.SearchEnabled`** (`SpectreWidgets.cs:90`) — its own built-in type-ahead with different visuals (`[dim cyan](Move ↑/↓ or j/k to reveal more items)[/]`-style hints) than the custom-built ones above; used wherever `SpectreMenu.Show(..., searchEnabled: true)` is called.
- **Dead/no-op capture** — `agyswitch` and `learn`/`session`/`weak` (see Hotkeys section above) capture keystrokes into a buffer that's never read.

**Enhancement:** standardize on the live-incremental-filter paradigm everywhere a list is shown (it's already the majority pattern and the best UX), retire the modal `ReadLine()`-based search in `SpectrePager`, and either implement or remove the dead capture in `agyswitch`/`learn`/`session`/`weak`. Once the `ScrollableListView` component proposed above exists, this becomes a single implementation choice instead of four.

### Additional task breakdown (continuing from T32)

- **T33. Extract a shared `ScrollableListView` component and delete the six duplicate viewport/scroll implementations.**
 Files: new `csapp/AgyTuiApp/Views/Components/ScrollableListView.cs`; callers in `FlatTreeRenderer.cs` (lines 685-699, 897-903, 972-980), `SpectreWidgets.cs` (`ShowWithEscape` 95-174, `SpectrePager.Show` 177-276, `BuildPrompt` 83-93).
 Fix: one `ComputeViewport(total, selected, maxVisible)` helper, one "items above/below" indicator renderer, one viewport-relative PageUp/Down, one pluggable live-filter search mode; migrate all six call sites onto it.
 Verify: paging behavior (`PageUp`/`PageDown`) and scroll-indicator text are identical across the main tree, theme picker, proj picker, and `SpectreMenu`/`SpectrePager` for the same list size/terminal size.
 Depends on: none, but do this before T22-style future list-UX work to avoid a 7th duplicate.

- **T34. Fix `PageUp`/`PageDown` to page by the actual viewport height, not a fixed 10.**
 Files: `csapp/AgyTuiApp/Views/FlatTreeRenderer.cs:226,232,309-314,560,563`.
 Fix: recompute (or cache from the last render pass) `maxRows` and use it as the page step instead of the literal `10`.
 Verify: on a maximized terminal, PageDown jumps close to a full screen of rows; on a minimized terminal, it never overshoots past the last visible row by more than one page.

- **T35. Make the `flat-tree` footer hotkey bar reflect only hotkeys the C# TUI itself handles.**
 Files: `csapp/AgyTuiApp/Views/FlatTreeRenderer.cs:829`.
 Fix: replace the hardcoded PowerShell-alias/PSReadLine-chord text with the actual in-app bindings (`↑/↓ j/k` move, `/` search, `Enter`/`→` expand or run, `←` collapse, `Esc/q` back, plus row-specific hints like `[A]dd [D]elete L[o]gout` only while an account row is focused).
 Verify: every hotkey listed in the footer produces a visible effect when pressed from that exact screen state.

- **T36. Make `ScreenChrome.RenderBanner`'s footer hint renderer-specific.**
 Files: `csapp/AgyTuiApp/Views/Components/ScreenChrome.cs:179`, call sites in `FlatTreeRenderer.cs:187` and `ThreePaneRenderer.cs:54`.
 Fix: add a `footerHint` parameter to `RenderBanner` (default to the existing three-pane text for `ThreePaneRenderer`, pass a flat-tree-appropriate one from `FlatTreeRenderer`).
 Verify: launching in `flat-tree` mode no longer shows "Tab/→ Navigate Panes".

- **T37. Wire live-filter search into the `agyswitch` and `learn`/`session`/`weak` sub-pickers, or stop capturing dead keystrokes.**
 Files: `csapp/AgyTuiApp/Views/FlatTreeRenderer.cs:270-290` (`itemsCount`), `:536-542` (capture), `:848-870`,`:928-939` (render).
 Fix: filter `AgyAccountCore.GetAccounts()`/the fixed topic array by `_detailsSearchBuffer` the same way `theme`/`proj` already filter their lists, and update `itemsCount` accordingly; if filtering these lists isn't wanted, remove the capture in the `default:` case for these two modes instead so typed characters don't silently vanish.
 Verify: typing in the account switcher either visibly filters the list, or is a documented no-op with no keystroke silently consumed into an invisible buffer.

- **T38. Add width-awareness to `ScreenChrome.RenderBanner`'s short-banner threshold.**
 Files: `csapp/AgyTuiApp/Views/Components/ScreenChrome.cs:114-158`.
 Fix: compute `winWidth` (already done elsewhere in the same method) and drop to the short banner (or a new even-narrower variant) below a width threshold (e.g. 60 cols), not just below a height threshold.
 Verify: a 40-column-wide, 40-row-tall terminal gets the compact banner instead of the full 6-line ASCII-art one.

- **T39. Unify the three mobile/density signals behind one `Config.IsMobileContext()` check.**
 Files: `csapp/AgyTuiApp/Services/Domain/Config.cs`, `csapp/AgyTuiApp/ThemeHelper.cs:53-56`, `csapp/AgyTuiApp/Views/FlatTreeRenderer.cs:953,677`.
 Fix: add `Config.IsMobileContext()` combining `ThemeHelper.IsMobileModeActive()`, `Density == "compact"`, and a window-width check; replace `FlatTreeRenderer.cs:953`'s local `isMobile` computation and the `isCompact` check at line 677 with calls to it.
 Depends on: none; pairs well with T24 (`Config.Save()` persisting all fields) since `IsMobileContext()` reads config state that today only partially round-trips.
 Verify: toggling the PowerShell `mobile` alias alone now also affects TUI layout decisions that call `IsMobileContext()`, without needing a separate `density` toggle.

- **T40. Give `ThreePaneRenderer` the same `Density`/mobile awareness `FlatTreeRenderer` has.**
 Files: `csapp/AgyTuiApp/Views/ThreePaneRenderer.cs`.
 Depends on: T39.
 Fix: apply `Config.IsMobileContext()`/`Density` to at least the description-hiding behavior `FlatTreeRenderer` already has, so three-pane users aren't completely unaffected by the mobile/density toggles that `mobile-setup` explicitly claims to control ("Toggles compact prompt and high-density TUI layout" — `CommandRegistry.cs:369` — currently false for three-pane users).
 Verify: with `Ui.Mode: "three-pane"` and `Density: "compact"`, list rows visibly get denser, matching what already happens in flat-tree mode.

- **T41. Retire `SpectrePager`'s modal `ReadLine()`-based search in favor of live-filter.**
 Files: `csapp/AgyTuiApp/Views/Components/SpectreWidgets.cs:251-263`.
 Depends on: T33 (do this as part of migrating `SpectrePager` onto the shared component).
 Fix: replace the blocking `Console.ReadLine()` search-and-jump with the same incremental filter-as-you-type pattern used in `FlatTreeRenderer`.
 Verify: pressing `/` in a long help/pager view starts filtering visible lines immediately per keystroke, with no blocking prompt.


  
 ## 🗂️ Services/Domain Deep Dive — `WorkspaceRegistry.cs` & siblings
  
 Focused line-by-line review of `csapp/AgyTuiApp/Services/Domain/*.cs`, with `WorkspaceRegistry.cs` as the primary target. Cross-checked against every real call site (`Grep`-verified, not assumed) and the existing test suite.
  
 ### `WorkspaceRegistry.cs`
  
 1. **Hardcoded, foreign-machine fallback search path.** `WorkspaceRegistry.cs:74` unconditionally adds `@"C:\Users\sshuser\project"` to the auto-discovery search bases regardless of `Config.Current.ProjectsBaseDir`. It's dead weight on any machine that isn't the original `sshuser` box (harmless today only because `Directory.Exists` short-circuits it) and shouldn't be baked into shared source at all.
 2. **Workspace-registry storage location doesn't respect the configurable home override.** `ConfigFile` (lines 20-22) is hardcoded to `%USERPROFILE%\.gemini\antigravity\priority_workspaces.json`, ignoring `System.AgySourceHome`/`Config.Current.AgySourceHome` — the same override that `AccountRepository`/`AgyAccountCore` (accounts, tokens) *do* respect. A user who's redirected `AgySourceHome` elsewhere (e.g. a portable/multi-profile setup) still gets their workspace list written to the default `%USERPROFILE%` location, splitting state across two roots.
 3. **Free-text workspace search is run through `Regex.IsMatch` on the raw, unescaped user query.** `FindByQuery` (`WorkspaceRegistry.cs:123-137`, consumed by `ProfileNavigator.Navigate`) treats whatever the user types as a regex pattern. A perfectly ordinary folder name search like `"app (v2)"` or `"my.project"` either throws (caught by the outer `try`/`catch`, line 132-135) and silently returns **zero results**, or subtly mismatches because `.`/`+`/`*` are regex metacharacters — the user sees "no workspace matched" for a workspace that's actually right there. **Fix:** default to `Contains(query, StringComparison.OrdinalIgnoreCase)`; only fall back to `Regex.IsMatch` if the caller explicitly opts into pattern search.
 4. **One bad subdirectory aborts discovery for every sibling under the same base.** `AutoDiscoverWorkspaces`'s `try`/`catch` (lines 82-99) wraps `Directory.GetDirectories(baseDir)` *and* the entire `foreach` over its results. If checking one `dir` (e.g. `Directory.GetFiles(dir, "*.csproj")` on a junction/symlink with a denied ACL) throws, the exception is caught at the *outer* level and the loop for that whole `baseDir` stops — any workspace that would have been found in a later sibling directory is silently lost, not just the problematic one.
 5. **No cap on auto-discovered workspace count.** Scanning `Documents`/`Desktop` (lines 76-77) — plausible dumping grounds for many unrelated folders, several of which may contain a stray `.git` — can return an unbounded number of entries with no size limit at discovery time.
 6. **`GetGitBranch` doesn't understand git worktrees/submodules.** `WorkspaceRegistry.cs:141-158` assumes `.git` is always a directory containing `HEAD`. For a worktree or submodule, `.git` is a *file* containing `gitdir: <path-elsewhere>` — `File.Exists(Path.Combine(dirPath, ".git", "HEAD"))` is false in that case, and the function silently returns `""` (no branch shown) for an otherwise perfectly valid workspace.
 7. **`GetByAccount` has no null-safe fallback.** `WorkspaceRegistry.cs:139` does an exact `string.Equals(w.AssociatedAccount, accountName, ...)`. Any workspace entry with `AssociatedAccount == null` (hand-edited JSON, or an older schema) is permanently invisible to `GetByAccount("default")`-style lookups, with no "treat null as default" fallback.
 8. **No caching, on a hot path.** `GetWorkspaces()` does `File.Exists` + `File.ReadAllText` + `JsonSerializer.Deserialize` on **every call**, and it's invoked once per render frame while `FlatTreeRenderer`'s `proj` sub-picker is open (its `itemsCount` computation re-derives the full list on every keystroke — same shape as the already-flagged `GetActiveChildren` hot-loop issue, T29). `TtlCache<,>` is the established pattern elsewhere in this same layer (`AccountHelper`, `AiHelper`, `ObsidianHelper`) but isn't used here at all.
 9. **Two different entry points to "act on a selected workspace" offer two different action sets.** `ProfileNavigator.Navigate` (`WorkspaceRegistry.cs:203-210`) offers 5 actions including "Launch New Terminal Session (wt / pwsh)"; `FlatTreeRenderer`'s inline `proj` sub-picker (`FlatTreeRenderer.cs:403-409`) offers only 4, missing that option. Same underlying feature, reached via the CLI `proj`/`p` alias vs. the TUI menu node, behaves differently depending on which door the user walked through — the same "move logic into one shared View, don't reimplement per entry point" issue already flagged for list-pickers (see T33), here spanning the Domain/View boundary.
 10. **`SaveWorkspaces` failures are swallowed to an on-screen panel with no retry** (`WorkspaceRegistry.cs:105-121`) — acceptable for a low-stakes cache file, but worth noting alongside the config-save findings elsewhere in this doc (T24) as part of the same "silent persistence failure" pattern.
  
 ### Sibling files in `Services/Domain/`
  
 - **`Config.cs`: `AutoDetectDensity()` overrides an explicit user preference, and is very likely why a prior commit "fixed" tests by changing their expectations instead of the bug.** `Config.cs:224-235` unconditionally forces `Ui.Mode = "flat-tree"` and `Ui.Density = "compact"` whenever `Console.WindowWidth < 70`, run from the static constructor (`Config.cs:104-108`) on **every** load — including every unit test process, whose console width is whatever the test host happens to report. Commit `a5f583b` ("update ConfigServiceTests to use flat-tree so unit tests do not revert default layout") reads as working around this exact behavior by changing the tests' expected value rather than scoping the auto-detect to only apply when the user hasn't set an explicit preference. As-is, any user who has deliberately set `Ui.Mode: "three-pane"` in `profile.config.json` loses that preference the moment their terminal is narrower than 70 columns — silently, with no log/notice.
 - `Config.cs:182`'s section-tracking regex in `Save()` matches a `"SpacedRepetition"` key that doesn't exist anywhere in `ConfigData` — vestigial dead branch from a config section that was apparently never added (or already removed).
 - **`AccountRepository.cs`: `GetAccounts()` is dead code that only the test suite exercises.** Grep across the whole `csapp` tree confirms `AccountRepository.GetAccounts()` (`AccountRepository.cs:32-36`, unfiltered raw directory listing) has exactly one caller: `AccountServiceTests.GetAccounts_ReturnsAccountsList`. Every real code path — `Program.cs`, `FlatTreeRenderer.cs`, `ThreePaneRenderer.cs`, `StatusWidgets.cs`, all of `AccountHelper.cs` — calls `AgyAccountCore.GetAccounts()` (`Services/AccountHelper.cs:318`) instead, an entirely separate reimplementation that additionally applies the backup/copy/temp substring filter already flagged as buggy in T20. **This means the passing `GetAccounts_ReturnsAccountsList` test provides zero real coverage** — it validates an abandoned duplicate, not the logic the shipped app actually runs. (`AccountRepository.GetActiveAccount()`/`SetActiveAccount()`, by contrast, *are* the live implementation — called from `AccountHelper.cs:346,745` — so this one file is half load-bearing, half dead-but-tested.)
 - **`AiLearningGenerator.cs`: stdout/stderr redirection deadlock risk + unvalidated AI output written straight to a data file.** `ExecuteCliGenerator` (`AiLearningGenerator.cs:84-119`) redirects both `StandardOutput` and `StandardError` but only drains `StandardOutput` (`.ReadToEnd()`, line 104) before `WaitForExit()` — if the child CLI (`agy`/`claude.cmd`) writes enough to stderr to fill its OS pipe buffer while nothing reads it, the process can hang indefinitely with no timeout and no way to cancel from the TUI. Separately, whatever the CLI prints to stdout is written verbatim to a `.json` deck/quiz file (line 110) with no validation that it's actually valid JSON, despite the prompt (line 49) merely *asking* for "clean JSON format" — any chatty response (a markdown code fence, a leading "Here's your JSON:") produces a corrupt data file whose failure only surfaces later as a swallowed exception in `StudyHelper.LoadJson`.
 - **`ProjectScaffolder.cs`: unsanitized project name reused as both a shell argument and a directory name, plus a working-directory mismatch for the React template.** `ProjectScaffolder.cs:18,30,34` — `name` comes straight from `AnsiConsole.Ask<string>` with no filtering (contrast `AiLearningGenerator.GetTargetFilePath`, which does strip invalid filename characters from similar free text); a `"` in the name breaks the quoted `-o "..."` argument passed to `dotnet new` (same raw-string `ProcessRunner` injection class as T21). Separately, the `"react (Vite)"` branch (lines 27-30) creates `outputDir\name` as an empty directory but then runs `npm create vite@latest {name}` with no working-directory override — Vite actually scaffolds into `<process-CWD>\{name}`, not `outputDir\name`, so specifying a non-default output directory for a React project silently produces an empty folder where the user asked and the real project elsewhere.
 - **`IdeCommandRegistry.cs` confirms the concrete exploit path for the path-traversal bug (T9).** `open` (`IdeCommandRegistry.cs:24-26`) is the injection point; `ask` (lines 37-44) is the exfiltration path — it reads the traversed file (truncated at 8000 chars) and forwards its contents to `AgyAiCore.AskAi`, i.e. off-machine to whichever AI provider is configured. `edit` (line 35) separately passes `ctx.CurrentFile` as a minimally-quoted raw string to `ProcessRunner.Run`, so a crafted filename reachable via the same traversal bug could also break argument quoting there.
  
 ### Task breakdown (continuing from T41)
  
 - **T42. Make `WorkspaceRegistry.FindByQuery` do substring matching by default instead of raw regex.**
   Files: `csapp/AgyTuiApp/Services/Domain/WorkspaceRegistry.cs:123-137`.
   Fix: change the match to `w.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || w.WorkspacePath.Contains(query, StringComparison.OrdinalIgnoreCase)`; if pattern search is still wanted, add it as an explicit opt-in (e.g. a `bool asRegex` parameter) rather than the default.
   Verify: searching for a workspace named `"my.app (v2)"` by typing that exact string returns a match instead of throwing/returning empty.
  
 - **T43. Route `WorkspaceRegistry`'s config file through `AgySourceHome`, and drop the hardcoded `sshuser` path.**
   Files: `csapp/AgyTuiApp/Services/Domain/WorkspaceRegistry.cs:20-22,74`.
   Fix: base `ConfigFile` on `AccountRepository.AgySourceHome` (or `Config.Current.AgySourceHome`) the same way account/token storage already does; remove the `C:\Users\sshuser\project` literal, relying solely on `Config.Current.ProjectsBaseDir` plus the generic `userProfile`-relative fallbacks.
   Verify: setting `System.AgySourceHome` in config relocates `priority_workspaces.json` along with accounts/tokens, not just the latter.
  
 - **T44. Isolate per-directory exceptions in `AutoDiscoverWorkspaces` so one bad subdirectory doesn't kill its siblings.**
   Files: `csapp/AgyTuiApp/Services/Domain/WorkspaceRegistry.cs:79-100`.
   Fix: move the `try`/`catch` inside the `foreach (var dir in subDirs)` loop (around the per-`dir` checks only), keeping the outer `Directory.GetDirectories(baseDir)` call's own guard separate.
   Verify: a search base containing one inaccessible/junction subdirectory alongside several valid project folders still discovers the valid ones.
  
 - **T45. Cache `WorkspaceRegistry.GetWorkspaces()` with the existing `TtlCache<,>` pattern.**
   Files: `csapp/AgyTuiApp/Services/Domain/WorkspaceRegistry.cs:24-44`, compare `Services/Infra/TtlCache.cs` usage in `AccountHelper.cs`/`AiHelper.cs`.
   Depends on: pairs with T29 (same anti-pattern, same fix shape).
   Fix: wrap the file-read + deserialize in a short-TTL cache, invalidated by `SaveWorkspaces`.
   Verify: opening the `proj` picker and typing a search query no longer re-reads/re-parses `priority_workspaces.json` on every keystroke.
  
 - **T46. Add a null-safe fallback to `WorkspaceRegistry.GetByAccount`.**
   Files: `csapp/AgyTuiApp/Services/Domain/WorkspaceRegistry.cs:139`.
   Fix: treat `null`/empty `AssociatedAccount` as belonging to `"default"` (or to whichever account is currently active) instead of excluding it from every per-account filter.
   Verify: a hand-added workspace entry with no `AssociatedAccount` field shows up under `GetByAccount("default")`.
  
 - **T47. Unify the `proj` workspace action menu between `ProfileNavigator.Navigate` and `FlatTreeRenderer`'s inline picker.**
   Files: `csapp/AgyTuiApp/Services/Domain/WorkspaceRegistry.cs:203-232` (`ProfileNavigator`), `csapp/AgyTuiApp/Views/FlatTreeRenderer.cs:403-431`.
   Fix: extract the shared action list (cd / launch terminal / open IDE / open Explorer / git diff) and its dispatch into one method both call, instead of two independently-maintained action arrays.
   Verify: the "Launch New Terminal Session" action is available identically whether reached via the `proj`/`p` CLI alias or the TUI's `[Workspace & Dev]` menu node.
  
 - **T48. Scope `Config.AutoDetectDensity()` to first-run only, and stop it from clobbering an explicit user `Ui.Mode`.**
   Files: `csapp/AgyTuiApp/Services/Domain/Config.cs:104-108,224-235`.
   Depends on: pairs with T1 (test isolation) and T24 (full-field `Save()`) — fixing this makes both easier to verify correctly.
   Fix: only apply the width-based auto-detect when no config file exists yet (i.e. `Current` is still the compiled-in default), not on every `Load()` regardless of a saved preference; also delete the dead `"SpacedRepetition"` branch in `Save()` (`Config.cs:182`).
   Verify: a config file with an explicit `Ui.Mode: "three-pane"` stays `"three-pane"` after `Load()` even when `Console.WindowWidth < 70`; `dotnet test` run from a narrow terminal no longer needs tests to special-case the auto-override.
  
 - **T49. Resolve the dead-vs-live `GetAccounts()` duplication between `AccountRepository` and `AccountHelper`.**
   Files: `csapp/AgyTuiApp/Services/Domain/AccountRepository.cs:32-36`, `csapp/AgyTuiApp/Services/AccountHelper.cs:318` (`AgyAccountCore.GetAccounts`), `csapp/AgyTuiApp.Tests/Unit/AccountServiceTests.cs:16-21`.
   Depends on: pairs with T20 (fixing the substring-filter bug should happen on whichever implementation survives).
   Fix: either delete `AccountRepository.GetAccounts()` and repoint `AccountServiceTests` at `AgyAccountCore.GetAccounts()`, or make `AgyAccountCore.GetAccounts()` call `AccountRepository.GetAccounts()` plus its filter, so there's one implementation and the existing test actually exercises production behavior.
   Verify: `GetAccounts_ReturnsAccountsList` fails if the real filtering logic (the one users actually hit) is broken — today it can't, since it never calls that code.
  
 - **T50. Fix the stdout/stderr deadlock risk in `AiLearningGenerator.ExecuteCliGenerator`, and validate output before writing.**
   Files: `csapp/AgyTuiApp/Services/Domain/AiLearningGenerator.cs:84-119`.
   Fix: drain `StandardOutput` and `StandardError` concurrently (e.g. `Task.WhenAll` of two `ReadToEndAsync()` calls, or async event handlers) before/while waiting for exit, with a timeout; attempt `JsonDocument.Parse` on the captured output before writing it to the target `.json` file, falling back to a clear error panel instead of a silently-corrupt deck file.
   Verify: a generator CLI that writes a large amount to stderr no longer hangs the TUI; a CLI response wrapped in markdown fences is rejected with a clear message instead of producing a file that only fails later inside `StudyHelper`.
  
 - **T51. Sanitize `ProjectScaffolder`'s project name and fix the Vite working-directory mismatch.**
   Files: `csapp/AgyTuiApp/Services/Domain/ProjectScaffolder.cs:18,27-35`.
   Depends on: shares the `ArgumentList`-migration pattern from T21.
   Fix: strip invalid filename characters from `name` the same way `AiLearningGenerator.GetTargetFilePath` already does; pass `outputDir` as the working directory to the `npm create vite` invocation (or `cd` into `Path.Combine(outputDir, name)`'s parent before running it) so the scaffolded project actually lands where the empty directory was created.
   Verify: scaffolding a React project into a non-default output directory produces the Vite project files inside that directory, not an empty folder plus a stray project elsewhere.
  
 - **T52. Add git-worktree/submodule support to `WorkspaceRegistry.GetGitBranch`.**
   Files: `csapp/AgyTuiApp/Services/Domain/WorkspaceRegistry.cs:141-158`.
   Fix: if `.git` is a file rather than a directory, read its `gitdir: <path>` line and resolve `HEAD` from that path instead of assuming `.git` is always a directory.
   Verify: a workspace checked out as a git worktree shows its actual branch name instead of a blank branch column.
  
