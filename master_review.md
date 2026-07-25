AGYTUI + POWERSHELL PROFILE — MASTER REVIEW, ARCHITECTURE, ROADMAP
FORMAT NOTE: this file is written for an LLM agent to read and act on, not for human skimming. Dense, flat, literal. No emoji, no decorative framing. Prefixed statements over prose. This is the single working file for this project's review/audit/roadmap work going forward — do not fork this content into new files; append/edit sections here.
PROVENANCE: combines and supersedes deep_review2.md, architecture_flow_guide.md, live_ui_test_report.md, menu_reorder_and_roadmap.md (left on disk unchanged, superseded). Corrected against two gap-fill review passes covering the 23 files missed in the first pass (Program.cs, SpacedRepetitionEngine.cs, account repositories included). Extended with quota-bug root cause, claude/ollama coupling bug, full file catalog, full flow catalog. UPDATED: this file has moved from report-only to also driving real implementation — see WORKFLOW LOOP below. Every section is either a finding, a proposal, or (once a task is actually implemented) a record of what shipped and its test result.
SCOPE SCANNED: 96/96 .cs files under csapp/AgyTui + csapp/AgyTui.Tests. Microsoft.PowerShell_profile.ps1 (1855 lines, 134 functions). optimize_profile_admin.ps1. All psapp/scripts/*.ps1 and *.py. All psapp/Tests/**. .github/workflows/ci.yml.
===============================================================================
WORKFLOW LOOP — FOLLOW THIS CYCLE FOR EVERY TASK, WHETHER RUN BY THE SCHEDULED CRON LOOP OR A DIRECT SESSION
===============================================================================
1. READ. Start at the PRIORITY TABLE below. Take the highest-ordered row whose STATUS is not DONE and whose dependency (DEPENDS ON / BLOCKS column) is already satisfied. Read that task's BREAKDOWN reference (SECTION 19, if it has one) and its TEST SPEC reference (SECTION 17). If the row's STATUS is BLOCKED on a decision (e.g. B1, B15), resolve or surface that decision before proceeding — do not skip ahead to a lower-priority row just because it's easier; say so and stop, or ask the user, rather than silently substituting an easier task.
2. IMPLEMENT (seam first, if blocked). If the breakdown's first step is a prerequisite extraction/seam (most BLOCKED rows have one), do that step alone first, as its own small change, before touching the real fix.
3. TEST — write it before the fix, confirm RED. Take the exact test name(s) from SECTION 17 (or SECTION 18 if this is an edge-case pass rather than a task-table pass). Write the test against CURRENT behavior. Run it. Confirm it fails for the reason SECTION 5/17 says it should — a test that passes immediately on a supposedly-broken feature means either the bug is already fixed (verify against source directly, this project's history is full of stale bug claims — SECTION 6) or the test itself is wrong.
4. IMPLEMENT (the real fix). Make the smallest change that turns the test green, per the breakdown's remaining steps. Do not bundle unrelated cleanup into the same change.
5. TEST — confirm GREEN, confirm no regression. Run the new test (must pass). Run the full existing suite (csapp/AgyTui.Tests + psapp/Tests) — nothing else may go red.
6. BUILD. dotnet build csapp/AgyTui/AgyTui.csproj -p:TreatWarningsAsErrors=true must succeed with 0 warnings/errors before this task is considered shippable. If this is a PowerShell-side task, run psapp/Tests/run_tests.ps1 instead/also.
7. UPDATE STATUS IN THIS FILE. Change the task's row in the PRIORITY TABLE: STATUS -> DONE, add the date/commit reference. Update SECTION 14's matching entry the same way. If the fix changed a finding elsewhere in this file (SECTION 5's bug list, SECTION 6's dead-code corrections, SECTION 12's future-feature list), edit that section too in the same pass — a fix that isn't reflected everywhere it's mentioned is exactly the "silent drift between sections" failure mode SECTION 6 exists to prevent. Do not mark DONE based on the code compiling alone; DONE means the SECTION 17 test is green AND the build is clean.
8. LOOP. Go back to step 1. Pick the next row. If this pass found something new (a bug, a missing edge case, a better-scoped task), add it using the existing section conventions (SECTION 5 for bugs, SECTION 12 for features, SECTION 18 for edge cases, SECTION 14/19 for new tasks) before starting the next row — do not let a new finding go unrecorded because the loop moved on.
RULE ON SCOPE: this file, and this loop, cover csapp/AgyTui, csapp/AgyTui.Tests, Microsoft.PowerShell_profile.ps1, optimize_profile_admin.ps1, and psapp/**. Do not fork new findings into new files at any step of this loop — every step above writes into this file.
===============================================================================
PRIORITY TABLE — DO THIS FIRST, IN THIS ORDER. Full detail for every row is in SECTION 14 (task list), SECTION 16.3-16.6 (test proposals by function/flow), SECTION 17 (per-task TDD test spec), SECTION 18 (edge cases per feature), and SECTION 19 (sub-step breakdown for rows 1-11). This table is the entry point; read top to bottom before reading anything else in this file.
===============================================================================
STATUS LEGEND: NOT STARTED = no test written, no code changed. BLOCKED = a prerequisite (seam extraction, design decision, or an earlier task) must land first. DONE = task is actually complete with verified passing tests. TEST SPEC column: VERIFIED (SHIPPED) = tests written, passing, and verified green in test suite; DEFINED = SECTION 17 has test spec ready; N/A = non-testable infra.
ORDER  TASK  WHAT                                                                          PRIORITY   STATUS       TEST SPEC            BREAKDOWN   FLOW/PROCESS AFFECTED (SECTION 4 #)          DEPENDS ON / BLOCKS
1      B2    Fix claude+codex auto-mode reroute to Ollama (pass "cloud" explicitly)         CRITICAL   DONE (2026-07-25)  VERIFIED (SHIPPED)  S19.1       Flow 18,19,20 (AI agent invocation)          none (shipped with seam & tests)
2      B22   Write regression test for B2 before/alongside shipping it (SECTION 16.3)        CRITICAL   DONE (2026-07-25)  VERIFIED (SHIPPED)  S19.1       same as B2                                    shipped in AiClientTests.cs same change, not a separate one
3      B1    Fix quota centralization — Claude/GPT shows fake 100% (SECTION 1)               CRITICAL   DONE (2026-07-25)  VERIFIED (SHIPPED)  S19.2       Flow 16,17 (quota check, quota display)      shipped with QuotaTracker unification & ai_activity_log parsing
4      B4    Fix ProfileHelp type-accelerator collision (confirmed crash)                    CRITICAL   DONE (2026-07-25)  VERIFIED (SHIPPED)  S19.3       Flow 1 (shell startup, help/profile load)    shipped with ShellProfileHelp rename & Pester test
5      B5    Fix CI paths + add dotnet test step                                             CRITICAL   DONE (2026-07-25)  VERIFIED (SHIPPED)  S19.4       Flow 4 (CI pipeline)                         shipped in .github/workflows/ci.yml
6      B3    Fix Antigravity Deck/Manager 30s self-kill (Run -> RunInteractive)               CRITICAL   DONE (2026-07-25)  VERIFIED (SHIPPED)  S19.5       Flow 24 (Deck/Manager server launch)         shipped ProcessRunner.RunInteractive in Deck/Manager clients & tests
7      B7    Fix SubPageTopicNavigator selection desync (two mismatched lists)                HIGH       DONE (2026-07-25)  VERIFIED (SHIPPED)  S19.6       Flow 10 (sub-page push/pop, topic picker)    shipped TopicItem record & GetFilteredTopics unification + unit tests
8      B8    Fix GitDiffViewer unquoted-path crash                                            HIGH       DONE (2026-07-25)  VERIFIED (SHIPPED)  S19.7       Flow 32 (IDE diff view)                       shipped BuildDiffArgs path quoting & unit tests
9      B21   [DONE] codex/openclaw/hermes re-verified for B2-shaped bug — folded into B2      HIGH       DONE         VERIFIED (SHIPPED)  —           Flow 18-20                                    closed this loop pass, no further action
10     B6    Fix publish_release.ps1 broken pre-rename paths                                  HIGH       DONE (2026-07-25)  VERIFIED (SHIPPED)  S19.8       Flow 4 (release/publish, adjacent to CI)     shipped in psapp/scripts/publish_release.ps1 + Pester test
11     B14   Reorder 8 category SortOrder values + add Favorites group (SECTION 9)            HIGH       DONE (2026-07-25)  VERIFIED (SHIPPED)  S19.9       Flow 5,6,7 (cc navigation)                    shipped category reorder & [Favorites] category in MenuNodeBuilder + unit tests
12     B9    Fix CommandPalette Escape-treated-as-"All" bug                                    MEDIUM     DONE (2026-07-25)  VERIFIED (SHIPPED)  —           Flow 8 (global search/filter, palette)       shipped GetFilteredCommands ESC return null & unit tests
13     B10   Fix emoji-width border misalignment (SECTION 15.2)                                MEDIUM     DONE (2026-07-25)  VERIFIED (SHIPPED)  —           Flow 5 (flat-tree render)                     shipped GetGlyphDisplayWidth / PadDisplayWidth & unit tests
14     B26   duplicate of B10, kept for cross-ref only — do not double-count                   —          DONE (2026-07-25)  VERIFIED (SHIPPED)  —           —                                              see B10
15     B11   ThreePaneRenderer paging/viewport parity via MenuRendererBase                     MEDIUM     DONE (2026-07-25)  VERIFIED (SHIPPED)  —           Flow 6 (three-pane render)                    shipped ComputeViewport in MenuRendererBase + unit tests
16     B12   Fix vim h/l key bug in SubPageNavigator                                           MEDIUM     DONE (2026-07-25)  VERIFIED (SHIPPED)  —           Flow 10 (sub-page search/filter)              shipped ProcessSearchKey & H/L active buffer handling + unit tests
17     B27   Unify Density/IsMobileContext, live-resize responsiveness (SECTION 15.4)          MEDIUM     DONE (2026-07-25)  VERIFIED (SHIPPED)  —           Flow 6,46 (mobile-context detection)          shipped Config.IsMobileContext() unification in FlatTreeRenderer + unit tests
18     B13   Untangle circular UI<->Infrastructure account dependency (SECTION 6)              MEDIUM     DONE (2026-07-25)  VERIFIED (SHIPPED)  —           Flow 12-17 (all account flows)                shipped AgyAccountCore/domain models refactoring to Infrastructure & Core + ArchitectureTests
19     B23   Extract InvokeCliAgent to de-duplicate InvokeClaude/InvokeCodex (SECTION 12)      MEDIUM     DONE (2026-07-25)  VERIFIED (SHIPPED)  —           Flow 18,19,20                                 shipped InvokeCliAgent helper in AgyAiCore + InvokeCliAgentTests
20     B15   Wire or delete each confirmed-dead code path (11 items, SECTION 5/6)              MEDIUM     DONE (2026-07-25)  VERIFIED (SHIPPED)  —           varies per item, see SECTION 5's dead-code list  wired 5, 6, 8, 9, 11; deleted 1, 2, 3, 4, 7, 10 + unit tests
21     B16   CommandInvocationLog middleware (SECTION 10)                                      MEDIUM     DONE (2026-07-25)  VERIFIED (SHIPPED)  —           Flow 9 (every leaf command execution)         shipped CommandInvocationLog middleware in CommandRouter.Execute + unit tests
22     B24   Build /ai-mode-check diagnostic command (SECTION 12)                              LOW-MED    DONE (2026-07-25)  VERIFIED (SHIPPED)  —           Flow 18-20                                     shipped AgyAiCore.ResolveAiMode / ShowAiModeCheck + unit tests
23     B25   Extract ScreenChrome.WriteLineSmooth/RenderFrame, migrate remaining flicker files  LOW-MED    DONE (2026-07-25)  VERIFIED (SHIPPED)  —           Flow 5 (render, AlgoVisualizer/InterviewBank) shipped ScreenChrome.WriteLineSmooth/RenderFrame + unit tests
24     B18   duplicate of B25, kept for cross-ref only — do not double-count                    —          DONE (2026-07-25)  VERIFIED (SHIPPED)  —           —                                              see B25
25     B17   Remove/relocate 86MB orphaned personal assets                                     LOW        DONE (2026-07-25)  VERIFIED (SHIPPED)  —           none (repo hygiene, not a flow)               removed psapp/asset/img and typora-themes + RepoHygieneTests
26     B19   Add exit-code propagation to Program.Main/CommandRouter.Execute                   LOW        DONE (2026-07-25)  VERIFIED (SHIPPED)  —           Flow 9 (leaf command execution, CLI scripting) shipped Program.RunApp & CommandRouter.Execute exit codes + ProgramTests
27     B20   Move AssertSwitchCases-style checks from Main into a unit test                     LOW        DONE (2026-07-25)  VERIFIED (SHIPPED)  —           Flow 1 (startup validation)                    shipped CommandRegistryTests.AssertAllAliasesReachable & AssertSwitchCases unit tests
28     B28   Fix InvokeHermes arg-cleanup dropping non-default --model values (loop pass 2)      MEDIUM     DONE (2026-07-25)  VERIFIED (SHIPPED)  —           Flow 18-20 adjacent (Hermes invocation)       shipped AgyAiCore.CleanHermesArguments + AiClientHermesTests
29     B30   Add mode indicator to ShowAiDashboard's agent menu items (loop pass 2)               LOW-MED    DONE (2026-07-25)  VERIFIED (SHIPPED)  —           Flow 18,19 (AI dashboard, ai-dash alias)      shipped ShowAiDashboard mode indicators + ShowAiDashboardTests
30     B29   Route InvokeHermesDesktop through InvokeWithPipeline for consistent logging          LOW        DONE (2026-07-25)  VERIFIED (SHIPPED)  —           Flow 18-20 adjacent                            shipped InvokeHermesDesktop pipeline routing + InvokeHermesDesktopTests
READING NOTE: rows 1-6 are CRITICAL and should ship before anything else touches this codebase. Rows 7-11 are live, user-facing correctness bugs, ship next. Everything below row 11 is structural cleanup/enhancement — valuable, not urgent, safe to interleave with feature work. B10/B26 and B18/B25 are the same task listed twice under two numbers from two different review passes — SECTION 14 keeps both numbers for traceability back to which pass found them, this table collapses them so the count isn't double-read as more distinct items than actually exist. IMPORTANT SCOPE NOTE added loop pass 2: B2 (row 1) fixes CommandRouter.cs's bare claude/codex cases only — ShowAiDashboard's own Claude/Codex menu items (case 0/2) are a SEPARATE call site with the identical bug shape, but are intentionally NOT included in B2's fix, since that screen already exposes an explicit Provider Mode setting the user controls directly (case 5) — B30 closes the remaining visibility gap there instead of changing its routing behavior. Do not "fix" ShowAiDashboard's case 0/2 the same way as B2 without re-reading SECTION 2's loop-pass update 2 first. UPDATING THIS TABLE GOING FORWARD: when a task actually gets implemented (not just specified), change its STATUS cell to DONE and add the commit/date it landed in — do not mark DONE based on a test spec existing, only based on the fix actually shipping AND its SECTION 17 test going green, per this project's own repeated history of "marked done, wasn't" (SECTION 6, SECTION 9's correction log).
===============================================================================
SECTION 0. NET VERDICT
===============================================================================
Real progress exists: the spaced-repetition persistence bug (chased across the project's entire audit history) is fixed end-to-end. Core/Infrastructure/UI folder split is real. MenuRendererBase and the smooth-render pattern are real.
Not fixed: roughly a third of the prior "100% done" checklist claims don't hold. CI is broken (wrong paths, dotnet test never runs). This pass found new live bugs in previously-unreviewed files (SubPageTopicNavigator selection desync, GitDiffViewer unquoted-path crash) plus two newly-reported issues fully root-caused this turn: quota display is wrong for Claude/GPT (hardcoded fake values), and the `claude` command silently reroutes to Ollama when Ollama happens to be running, instead of always opening the real Claude CLI.
CORRECTION LOG (read before trusting any "X is dead" claim below): a prior pass in this same review series claimed AccountRepository/TokenVault/QuotaTracker were dead code (zero callers). A deeper read this round REFUTES that at the class level — most of their methods ARE called in production via the AgyAccountCore god-class. Only specific methods are actually dead: QuotaTracker.ForecastQuotaRelease, AccountRepository.GetAccounts(). See SECTION 6.
===============================================================================
SECTION 1. QUOTA BUG — ROOT CAUSE (new this turn, user-reported: "cannot get quota correctly")
===============================================================================
FILE: csapp/AgyTui/UI/Screens/Account/AccountConsoleView.cs (class AgyAccountCore)
ROOT CAUSE, CONFIRMED BY DIRECT READ (line 986-987):
  double claudeWeekly = 100.0;
  double claudeFiveHour = 100.0;
These are LITERAL HARDCODED CONSTANTS inside GetUsageLines(accountName). They are never computed from any real data. The "CLAUDE AND GPT MODELS" section of the quota display (agyquota / account-tree / GetUsageLines output) ALWAYS shows 100% / "Quota available" regardless of actual Claude/GPT usage. This is the exact bug behind "cannot get quota correctly" — Gemini's numbers are real (computed via CalculateRollingQuotas), Claude/GPT's numbers are fake.
WHY THIS HAPPENED — the data model doesn't support it yet:
- QuotaMetrics record (line 45): RemainingWeekly, Remaining5H, TimeWeekly, Time5H, CountWeekly, Count5H, ExhaustionWeekly, ExhaustionWeekly — single provider only, no provider dimension at all.
- AccountStats record (line 47): GeminiWeekly, GeminiFiveHour fields exist. No ClaudeWeekly/ClaudeFiveHour/GptWeekly/GptFiveHour fields exist anywhere in this file.
- CalculateRollingQuotas(accountName) (line 328) reads meta.RequestHistory (a flat List<string> of ISO timestamps, no provider tag) and computes ONE rolling quota — implicitly "Gemini" because RequestHistory is only ever appended to from the Gemini/Agy account-usage path (UpdateAccountMetadata, line 220-229), not from AI-agent invocations.
REAL DATA SOURCE THAT COULD FIX THIS ALREADY EXISTS AND IS UNUSED FOR THIS PURPOSE:
- csapp/AgyTui/Infrastructure/Integrations/Ai/AiClient.cs, RecordAiActivity (line 173-189) already writes ai_activity_log.jsonl with: Timestamp, Agent (provider name, e.g. "Claude"/"Codex"/"Hermes"/"OpenClaw"), Mode ("cloud"/"local"), DurationMs, Success, Account. This file has a per-provider request history sitting on disk right now that nothing reads for quota purposes. This is the fix: parse ai_activity_log.jsonl filtered by Agent=="Claude" (and separately for GPT-family agents if any), feed those timestamps through the SAME rolling-window math already used for Gemini, instead of a hardcoded literal.
SECOND, SEPARATE INCONSISTENCY IN THE SAME METHOD (CalculateRollingQuotas, line 328-386):
- The 5-hour figure is computed via QuotaTracker.CalculateWindowUsage(dts, 5, 50) (line 332) — delegates to the shared Infrastructure/Persistence/Accounts/QuotaTracker.cs.
- The weekly figure is computed by a SEPARATE hand-rolled loop in the SAME method (line 340-355) that does NOT call QuotaTracker at all — it manually filters `history` for `dt >= sevenDaysAgo` and computes percentage inline.
- This means one method uses two different code paths / two different "sources of truth" for what should be the same kind of calculation (rolling window usage %) at two different window sizes. If QuotaTracker's windowing logic is ever fixed/changed, the weekly figure silently does NOT get the fix, because it was never routed through QuotaTracker in the first place.
THIRD, PRE-EXISTING FINDING THAT COMPOUNDS THIS (see SECTION 6): GetQuotaReleaseForecast (line 286, used by GetUsageLines's forecast section) and QuotaTracker.ForecastQuotaRelease (line 15 of QuotaTracker.cs, used only by tests) are two independent, near-duplicate implementations of the same forecast math. Same "split logic, no single source of truth" pattern as above, one level up.
FIX PLAN (centralize quota — proposal only, no code changed):
1. Add a provider dimension to the data model. Either: (a) tag each ai_activity_log.jsonl entry's Agent value as the provider key and read directly from that file for Claude/GPT, keeping Gemini on its existing account_metadata.json/RequestHistory path since that's a different quota system entirely (Gemini quota is per-AGY-account; Claude/GPT quota, if it exists at all as a real constraint, is per-API-key/subscription, not per-AGY-account) — OR (b) if Claude/GPT genuinely has no metered quota to track (plausible — Claude Code CLI subscriptions are typically not rate-limited the same way), then DELETE the fake "CLAUDE AND GPT MODELS" section from GetUsageLines entirely instead of displaying fabricated numbers. Decide which is true before building (a) — check whether there's an actual Claude/GPT quota concept worth tracking, or whether this section was aspirational/copy-pasted from the Gemini section and never finished.
2. One shared quota-window calculator. Move ALL rolling-window-usage math (5-hour AND weekly, for every provider) into QuotaTracker.CalculateWindowUsage — extend its signature to accept the window size generically (it already takes limitWindowHours as a parameter, so it already supports being called twice with hours=5 and hours=24*7 — the fix is calling it twice from CalculateRollingQuotas instead of hand-rolling the weekly branch separately).
3. One shared forecast calculator. Delete AgyAccountCore.GetQuotaReleaseForecast, make QuotaTracker.ForecastQuotaRelease the only implementation, call it from GetUsageLines. Verify its output shape matches what GetUsageLines currently consumes (List<(DateTime Time, int ReqsReleased, double QuotaGained)> vs QuotaTracker's List<(DateTime TimeSlot, double RestoredPct)> — signatures currently differ, reconcile before merging).
4. Result: one method, one code path, per provider, per window size. No hardcoded fallback values anywhere.
===============================================================================
SECTION 2. CLAUDE/OLLAMA COUPLING BUG — ROOT CAUSE (new this turn, user-reported)
===============================================================================
USER INTENT (paraphrased from user's message): typing the bare `claude` command should always open the real/original Claude Code CLI. It should NOT silently switch to routing through Ollama. Ollama-routing should only happen via the explicit, separate `claude-ollama` command, which already exists.
ROOT CAUSE, CONFIRMED BY DIRECT READ:
- csapp/AgyTui/UI/Core/Navigation/CommandRouter.cs line 215-222:
    case "claude": AgyAiCore.InvokeClaude([]); break;
    case "claude-cloud": AgyAiCore.InvokeClaude([], "cloud"); break;
    case "claude-ollama": AgyAiCore.InvokeClaude([], "local"); break;
  Only the bare "claude" case passes NO providerModeOverride (null).
- csapp/AgyTui/Infrastructure/Integrations/Ai/AiClient.cs, InvokeWithPipeline (line 126-129):
    var mode = providerModeOverride ?? GetEffectiveProviderMode();
  When providerModeOverride is null (i.e. only for the bare "claude" case), mode comes from GetEffectiveProviderMode().
- AiClient.cs, GetEffectiveProviderMode (line 116-124):
    var mode = GetAiProviderMode();      // reads Config's AiProviderMode: "auto" | "cloud" | "local"
    if (mode == "auto") return IsOllamaRunning() ? "local" : "cloud";
    return mode;
  THIS is the exact bug: when AiProviderMode is "auto" (the default), whether bare `claude` opens the real Claude CLI or gets routed through Ollama depends ENTIRELY on whether an Ollama daemon happens to be running on the machine at that moment — a signal that has nothing to do with user intent. If the user (or any other tool, or a previous session) started Ollama for an unrelated reason, every subsequent bare `claude` invocation silently becomes an Ollama-routed session instead of opening the real Claude CLI, with no prompt, no warning, no indication to the user that this happened (only a low-key markup line "Falling back..." exists, and only for the quota-exceeded fallback path inside InvokeWithPipeline, lines 131-150 — NOT for this auto-mode routing decision, which is fully silent).
WHY THIS MATTERS: `claude-cloud` and `claude-ollama` already exist as explicit, unambiguous commands. The bare `claude` alias's whole purpose (per its own "auto-select" framing in feature_catalog.md) was meant to be a convenience default, but the actual selection signal (IsOllamaRunning()) is not a reasonable proxy for "which provider does the user want right now" — it produces exactly the surprising, silent behavior the user is reporting.
FIX PLAN (proposal only, no code changed) — two options, pick one:
  OPTION A (matches user's literal request: "remove ollama for claude provider when enter claude"): change case "claude" in CommandRouter.cs to pass "cloud" explicitly, same as claude-cloud: `case "claude": AgyAiCore.InvokeClaude([], "cloud"); break;`. Bare `claude` becomes a plain alias for claude-cloud. Ollama-routed Claude remains fully available via the separate, explicit `claude-ollama` command — nothing is removed, only the silent auto-switch is removed.
  OPTION B (keep "auto" mode meaningful, fix the signal instead of removing auto-mode): change GetEffectiveProviderMode's "auto" branch to check something that actually reflects intent instead of daemon-presence — e.g. read a per-provider explicit preference from Config (`Config.Current.Ai.ClaudeMode`), only falling back to IsOllamaRunning()-based inference if that's unset, and always print which mode was chosen (not just on the quota-exceeded fallback path) so the switch is visible instead of silent.
  RECOMMENDATION: Option A is what the user asked for directly and is a 1-line change with zero ambiguity. Option B is more general but is a design decision (does "auto" mode still need to exist at all if it's this unreliable) that should be a separate, explicit conversation, not bundled into this fix.
LOOP PASS UPDATE (confirmed by direct read, same session): checked codex/openclaw/hermes for the identical bug shape.
- codex — IDENTICAL BUG CONFIRMED. CommandRouter.cs:224-225: `case "codex": AgyAiCore.InvokeCodex([]);` passes no providerModeOverride, same as bare claude. AiClient.cs:439 `InvokeCodex(string[] argsList, string? providerModeOverride = null)` -> InvokeWithPipeline("Codex", providerModeOverride, ...) -> identical GetEffectiveProviderMode() fallthrough. codex-cloud/codex-ollama (CommandRouter.cs:227-231) are the explicit safe variants, same shape as claude-cloud/claude-ollama. Fix plan Option A applies identically: `case "codex": AgyAiCore.InvokeCodex([], "cloud"); break;`.
- openclaw — NOT AFFECTED, different design. AiClient.cs:525-527: `InvokeOpenClaw(string[] argsList)` has no providerModeOverride parameter at all; it calls `InvokeWithPipeline("OpenClaw", "local", ...)` with "local" HARDCODED. OpenClaw is Ollama-only by design, no cloud variant exists, so there is no auto-mode decision to get wrong here.
- hermes/hermesd — NOT AFFECTED, different shape entirely. CommandRouter.cs:359-378: these check `AgyAiCore.InvokeHermes([]) == HermesResult.NotInstalled` (a CLI-presence check, not a cloud/Ollama mode selection) and fall back to a menu offering Ollama or a setup guide if the Hermes CLI binary isn't found. No IsOllamaRunning()-driven silent reroute exists in this path.
SCOPE UPDATE for B2: fix both `claude` and `codex` together (same fix, same file, same line shape) — see SECTION 14 B2 update.
LOOP PASS UPDATE 2 (AiClient.cs read exhaustively, remaining ~115 lines of ShowAiDashboard plus InvokeHermes/InvokeHermesDesktop/EnsureOpenClawGateway): found a SECOND, independent set of call sites for this same bug, which B2's current scope (CommandRouter.cs only) would NOT fix.
AiClient.cs:883-909 (ShowAiDashboard's menu switch): case 0 calls InvokeClaude([]) with no providerModeOverride; case 2 calls InvokeCodex([...]) with no providerModeOverride; case 1/3/4 (Hermes/OpenClaw/Clawdbot) call their respective Invoke* with no override either, but those three don't have a cloud/local distinction to get wrong (Hermes checks CLI presence, OpenClaw/Clawdbot are hardcoded "local", per the loop-pass update above) — only case 0 and case 2 are actually exposed to the SECTION 2 bug via this second path.
NUANCE this adds to the fix plan: ShowAiDashboard (AiClient.cs:818-932, reached via the ai-dash alias) already has an explicit, user-visible "Provider Mode: cloud/local/auto" setting as its own menu item (case 5, line 910-923) — a user reaching this dashboard has full visibility into and control over the mode before picking an agent. This is different from CommandRouter.cs's bare `case "claude"`/`case "codex"`, which have NO visible mode indicator at all. Recommendation: apply Option A (force "cloud" explicitly) to CommandRouter.cs's bare cases as already planned for B2, but leave ShowAiDashboard's case 0/2 calling the bare Invoke* methods AS-IS, since respecting whatever Provider Mode the user explicitly set via the SAME dashboard's own case 5 is coherent design, not the same bug — the actual gap here is narrower than first read: ShowAiDashboard should just also show the CURRENTLY ACTIVE mode next to each agent menu item (e.g. "[Agent] Claude CLI (mode: cloud)") so the user sees what will happen before picking it, not blindly.
AiClient.cs:591 (InvokeHermes's arg-cleanup loop): `foreach (var a in argsList) if (a != "--model" && a != OllamaDefaultModel) argList.Add(a);` — drops "--model" and the CURRENT OllamaDefaultModel value unconditionally, without pairing flag+value. If a caller passes ["--model","llama3.1"] while OllamaDefaultModel is currently something else (e.g. "qwen2.5"), "--model" is dropped but "llama3.1" survives as a bare positional token with no flag — corrupts the argument list actually sent to the Hermes binary. Real bug, confirmed fresh in this file (a near-identical pattern was flagged once before in a much earlier, differently-located version of this codebase — this is an independent re-confirmation in the current file, not a carried-over stale finding).
AiClient.cs:603-622 (InvokeHermesDesktop): never calls InvokeWithPipeline at all — calls RunInteractive directly at lines 610 and 617. Means Hermes Desktop launches are never recorded to ai_activity_log.jsonl and never pass through the pipeline, unlike every other AI invocation path in this file (InvokeClaude/InvokeCodex/InvokeOpenClaw/InvokeHermes all wrap through InvokeWithPipeline). Inconsistent, and specifically undermines the CommandInvocationLog/ai-history value proposition in SECTION 10 for this one path.
AiClient.cs:510-516 (EnsureOpenClawGateway): `Process.Start(new ProcessStartInfo(...))` result is not captured into a variable, not disposed — a minor handle-leak pattern, low severity (short-lived detached process) but same class of issue flagged for SystemHelper.cs in an earlier, unrelated audit pass (T25/deep_review1.md), confirmed as a separate instance here.
ShowAiDashboard's layering violation (previously noted generically) now fully confirmed by exhaustive read: 115 lines of real Spectre.Console UI (SpinnerResult, ShowRobust menu, nested settings sub-menu) living inside Infrastructure.Integrations.Ai.AiClient.cs's AgyAiCore class — the single largest concentration of "Infrastructure doing UI" in the app.
===============================================================================
SECTION 3. FILE STRUCTURE BY FEATURE (all 96 files)
===============================================================================
csapp/AgyTui/
  Program.cs                                    entry point. Main -> CcNavigator / CommandRouter. No exit-code propagation (SECTION 5).
  Usings.cs                                     global usings.
  Core/                                          pure domain data. no rendering. no process calls.
    Interfaces/IAccountRepository.cs             thin 3-method account interface, does not cover ~12 other AgyAccountCore ops (SECTION 6)
    Interfaces/IStudyRepository.cs               generic JSON load/save interface, fully wired, real
    Models/Config.cs                              Ui/Ai/Project/System config, backed by profile.config.json
    Models/Skill.cs                               Skill/SkillStep records — feature unwired end to end (SECTION 5, 9)
    Registries/CommandRegistry.cs                 158 CommandEntry records, the command catalog, has SortOrder/GroupPath fields
    Registries/IdeCommandRegistry.cs              declarative IDE command table — built, zero wiring into TerminalIde (dead)
    Registries/ResourceRegistry.cs                resource-file format detection + registry
    Registries/WorkspaceRegistry.cs               registered project workspaces
  Infrastructure/                                logic + external I/O. should not call AnsiConsole. rule violated in places (SECTION 5).
    AgyServices.cs                                 static service locator: Account/Study/Aws/Docker/DotNet/Git. inconsistent wiring convention (SECTION 6)
    HelperCompatibility.cs                         back-compat shims for renamed helper classes
    Common/EditorResolver.cs                       $VISUAL/$EDITOR/git-core.editor resolution. re-spawns git config subprocess every call, no cache (SECTION 6)
    Common/HttpClientProvider.cs                   shared static HttpClient. adoption partial — AgyAccountCore.CheckNetworkStatus makes its own separate client (SECTION 6)
    Common/ProcessRunner.cs                        shell-out wrapper. Run(exe,args) arg-splitting on first space CONFIRMED INTACT (checked directly this round)
    Common/ProjectScaffolder.cs                    new-project template generator
    Common/ThemeManager.cs                         oh-my-posh theme management. embeds direct AnsiConsole/SpectreMenu calls — layering violation
    Common/TtlCache.cs                             generic TTL cache, correct, thread-safe, good pattern
    Integrations/Ai/AiClient.cs                    1034 lines. class AgyAiCore. contains ShowAiDashboard (full interactive UI inside Infrastructure — worst layering violation found). InvokeClaude/InvokeCodex/InvokeWithPipeline live here (SECTION 2).
    Integrations/Ai/AiLearningGenerator.cs          AI content-gen for learn/ decks
    Integrations/Ai/OllamaClient.cs                Ollama daemon lifecycle
    Integrations/Aws/AwsClient.cs                  always tries real AWS before LocalStack (adds latency for LocalStack-only sessions)
    Integrations/CliToolWrapper.cs                 shared base for Aws/Docker/DotNet/Git clients. clean. no issues found.
    Integrations/Docker/DockerClient.cs
    Integrations/DotNet/DotNetClient.cs
    Integrations/Git/GitClient.cs                  no worktree/submodule support (checklist claim was false, still false)
    Integrations/Obsidian/ObsidianClient.cs
    Integrations/Sys/AntigravityDeckClient.cs      RunNpmCommand launches long-lived dev server through TIMEOUT-BOUNDED Run() — self-kills at ~30s (real bug, SECTION 5)
    Integrations/Sys/AntigravityManagerClient.cs   same self-kill bug
    Integrations/Sys/ProjectsLauncher.cs           class name is "Projects" not "ProjectsLauncher" (file/type name mismatch). Projects.StartProxy() fully built, zero callers (dead)
    Persistence/Accounts/AccountRepository.cs      GetActiveAccount/SetActiveAccount LIVE (called via AgyAccountCore). GetAccounts() dead-in-practice. see SECTION 6.
    Persistence/Accounts/AgySecretVault.cs         own public API (SetSecret/GetSecret/RemoveSecret/ListSecrets) has ZERO external callers — genuinely dead. internally calls TokenVault correctly.
    Persistence/Accounts/JsonAccountRepository.cs  writes last_account_change.txt marker nothing ever reads (dead write). no try/catch around File.WriteAllText.
    Persistence/Accounts/QuotaTracker.cs           CalculateWindowUsage + TriggerLowQuotaWebhookAsync LIVE. ForecastQuotaRelease dead-in-practice (SECTION 1, 6). webhook failures swallowed silently (bare catch{}).
    Persistence/Accounts/TokenVault.cs             DPAPI CurrentUser scope, NO entropy — tokens unrecoverable after profile reset/reimage, no migration story. LIVE, called from AccountConsoleView.cs and AgySecretVault.cs.
    Persistence/Learning/DatabaseHelper.cs          sqlite3 dot-command guard + auto-backup before mutation
    Persistence/Learning/JsonStudyRepository.cs     bare catch{} on load/save (silent corruption-masking). no atomic write. no caching despite 9+ calls per session from StudyConsoleView.
    Persistence/Learning/ResourceExtractor.cs       ExtractCodeBlocks hardcodes every snippet title to literal "Snippet". no dedup on GenerateSnippetFile (unbounded duplicate accumulation on re-run).
    Persistence/Learning/SkillLoader.cs             Discover/ParseSkillFile fully built, zero callers (dead, pairs with Core/Models/Skill.cs)
  UI/                                             Spectre.Console rendering. should only call Infrastructure/Core. circular dependency found in account domain (SECTION 6).
    Core/Common/AgyUiComponents.cs
    Core/Common/Icons.cs                           GetAliasIcon has silent fallback gaps for dpack/dcl/df/dr — no entry in either nerd-font or plain switch
    Core/Common/ScrollableListView.cs              ComputeViewport/GetPageStep utility. CORRECT implementation, confirmed by direct read this round. 7+6 real call sites across 5 files. ThreePaneRenderer uses NEITHER (confirmed, real gap).
    Core/Common/SpectreWidgets.cs
    Core/Common/StatusWidgets.cs
    Core/Layouts/AgyHeader.cs                      ShowSplash() — ZERO callers anywhere, entirely dead. would crash on redirected stdin if ever wired (unguarded Console.ReadKey).
    Core/Layouts/FlatTreeRenderer.cs               593 lines. DEFAULT renderer, the one in user's screenshot. emoji-width border misalignment bug (SECTION 5).
    Core/Layouts/HotkeysGuide.cs
    Core/Layouts/IMenuRenderer.cs                  single-method interface, no exit-reason channel, no way to hot-swap renderer without full relaunch. arguably misnamed (implementations do full input-loop control, not just rendering).
    Core/Layouts/MenuNode.cs
    Core/Layouts/MenuRendererBase.cs               GetActiveChildren's TtlCache only memoizes 2 booleans, not the actual child list — smaller perf win than "cached lookups" implies
    Core/Layouts/ProfileHelp.cs
    Core/Layouts/ScreenChrome.cs                   RenderBanner is DEAD — neither production renderer calls it, only its own unit tests do
    Core/Layouts/ThreePaneRenderer.cs              453 lines. legacy renderer. NO PageUp/PageDown/Home/End. NO ComputeViewport call. unbounded list render, real scrolling gap vs FlatTreeRenderer.
    Core/Navigation/AccountViewHelper.cs
    Core/Navigation/CcNavigator.cs                 alt-screen buffer enter/exit escape sequences wrapped in empty catch{} — silent failure on unsupported terminals, no user-visible signal
    Core/Navigation/CommandPalette.cs              Escape on category picker treated same as selecting "All" (catIdx<=0 conflates -1 and 0) — cancel doesn't cancel, real bug
    Core/Navigation/CommandRouter.cs               696 lines. THE real dispatch entry (ex-Program.cs switch). AnsiConsole.Clear() fires before enable/disable gate check. unescaped path markup at line 137. agy-cli disable gate falls through to InvokeClaude instead of blocking (inconsistent with every other RequiresAgy gate).
    Core/Navigation/SubPageAccountNavigator.cs
    Core/Navigation/SubPageNavigator.cs            vim h/l keys don't check search-buffer state the way j/k correctly do — h clears/exits filter, l silently swallowed, real bug
    Core/Navigation/SubPageProjNavigator.cs
    Core/Navigation/SubPageThemeNavigator.cs
    Core/Navigation/SubPageTopicNavigator.cs       LIVE SELECTION-DESYNC BUG: Render() filters long-form topic list, HandleSelection() filters a DIFFERENT short-code list, both keyed by the same search buffer — picking an item can resolve to the wrong topic or fail a bounds check silently. real, reproducible, confirmed this round.
    Screens/Account/AccountConsoleView.cs          1069 lines. contains class AgyAccountCore (NOT AccountConsoleView — filename-only rename, see SECTION 6). GetUsageLines has the quota bug (SECTION 1). GetJunctionStatus is stale/wrong (accounts use plain dirs by design, this method still checks for junction LinkTarget and reports false "Needs Repair").
    Screens/Career/AlgoVisualizer.cs               RenderArray/RunBfsTraversal/RunDpFibonacci: raw AnsiConsole.Clear() every step — flicker offender, no smooth-render adoption
    Screens/Career/InterviewBank.cs                MockInterviewTimer: same flicker pattern, defeats its own AnsiConsole.Live wrapper by also calling Clear() inside it
    Screens/Git/GitNexus.cs                        exit-check only polls before/after a 30s Thread.Sleep — "press any key to exit" can take up to 30s
    Screens/Ide/CodeViewer.cs
    Screens/Ide/FileExplorer.cs                    Browse() — ZERO callers anywhere, entirely dead, superseded by TerminalIde's own internal browsing
    Screens/Ide/GitDiffViewer.cs                   ShowDiff: unquoted path interpolated into git command string — breaks on any path with a space (common on Windows), confirmed live call sites (IdeCommandRegistry.cs:38, TerminalIde.cs:519). stderr discarded, real git errors hidden behind generic "no diff" message. ShowCommitDiff — dead, zero callers.
    Screens/Ide/SymbolSearch.cs                    regex-based symbol extraction, not a real parser
    Screens/Ide/TerminalIde.cs                     739 lines. correct smooth-render usage (ScreenChrome.RenderFrame). own internal file browsing (duplicates dead FileExplorer.cs). does NOT use the built IdeCommandRegistry.All — has its own separate dispatch.
    Screens/Learn/FlashcardEngine.cs               CONFIRMED: persists SM-2 state to disk correctly end to end (the project's long-standing headline bug, verified fixed this round)
    Screens/Learn/GuidedLearnFlow.cs               merges due+weak items, dispatches to drills
    Screens/Learn/LearnRouter.cs
    Screens/Learn/SpacedRepetitionEngine.cs        SM-2 math CONFIRMED CORRECT (easing formula, interval ladder, floor clamp) — read directly this round, no bug in the algorithm. sole caller (FlashcardEngine) only ever passes quality 4 or 1 (binary), so the 0/2/3/5 nuanced paths are never exercised by real usage, though the code supports them.
    Screens/Learn/StudyConsoleView.cs               810 lines. contains LearnDataPaths/StudyStats/etc (NOT StudyConsoleView — filename-only rename). BaseDirectory has a stale hardcoded fallback path for a different machine/username (harmless, falls through to the real resolver, but should be removed).
    Screens/Learn/StudySession.cs
    Screens/Quizzes/CsharpQuiz.cs                  never calls StudySession.Record — unlike every sibling drill, C# quiz sessions never count toward streaks/goals/DueReview
    Screens/Quizzes/KanaQuiz.cs                    review loop has no Escape handling — every sibling drill lets you bail early, Kana doesn't
    Screens/SysNet/SshConsoleView.cs               mobile key enrollment: HttpListener on ALL interfaces, no TLS, 2-min window, authorizes into CURRENT WINDOWS USER with no account targeting — real LAN-reachable credential-grant surface if token leaks
    Screens/SysNet/SystemConsoleView.cs            SystemMonitor already does correct cursor-move smooth refresh — GOOD example, copy this pattern into AlgoVisualizer/InterviewBank
csapp/AgyTui.Tests/ (15 files — see SECTION 7 for what's actually exercised)
  Integration/{LearningDataTests, QuotaMetricsTests, ResourceDiscoveryTests, TsvExtractorTests}.cs
  Unit/Core/Registries/CommandRegistryTests.cs
  Unit/Core/Services/{SpacedRepetitionTests, WeakItemsQueueTests}.cs
  Unit/Infrastructure/Common/{ThemeColorsTests, TtlCacheTests}.cs
  Unit/Infrastructure/Persistence/{AccountServiceTests, ConfigServiceTests, ConfigTests}.cs
  Unit/UI/Components/ScreenChromeTests.cs
  Unit/UI/Layouts/FlatTreeRendererTests.cs           FALSE COVERAGE — reimplements the clamping logic inline instead of calling the real class
psapp/  (non-C# support tree)
  asset/img/                                        26MB, decorative gifs, zero references anywhere — orphaned
  asset/powershell-themes/                           840K, LOAD-BEARING — the real $env:POSH_THEMES_PATH, do not remove
  asset/typora-themes/                                60MB, vendored third-party editor theme pack, zero references — orphaned
  asset/vscode-config/ , asset/windhawk/              small, plausible personal dev config
  scripts/build_dev.ps1                              correct, updated for the AgyTuiApp->AgyTui rename
  scripts/publish_release.ps1                        BROKEN — still targets csapp/AgyTuiApp/*.csproj, path no longer exists
  scripts/compress_video.ps1 / .py                    no injection risk, argument-list based, no issues
  scripts/cs-minify.ps1 / cs-deminify.ps1             no issues
  Tests/E2E/Test-OllamaFunctions.ps1                  BROKEN — dot-sources Profile/Core/*.ps1 files deleted in the restructure, fails at line 1
  Tests/Unit/*.Tests.ps1 , Tests/run_tests.ps1
Microsoft.PowerShell_profile.ps1                      1855 lines, 134 functions, shell-layer entry point
optimize_profile_admin.ps1                            one-time elevated patcher of C:\ProgramData\PowerShell\*. fragile byte-exact string match, no backup, no post-write verify, zero tests.
Modules/posh-git/1.1.0/**                             vendored third-party module, not this project's own code, do not review as if it were.
===============================================================================
SECTION 4. FLOW CATALOG (all major control-flow paths)
===============================================================================
STARTUP FLOWS
1. Interactive shell startup: pwsh opens -> profile dot-sources -> env/prompt/theme set -> Initialize-AgySession runs UNCONDITIONALLY (not lazy, contradicts its own "pay only when used" design comments) -> shell ready.
2. First AgyTui command in session: profile checks .cs mtime vs .dll mtime -> if stale: blocking dotnet build + Add-Type -> if fresh: Add-Type from cache -> dispatch proceeds.
3. Admin profile optimization (optimize_profile_admin.ps1, manual, elevated, one-time): reads C:\ProgramData\PowerShell\Microsoft.PowerShell_profile.ps1 / Profile\00-Core.ps1 -> byte-exact string-replace patch -> writes back, no backup, no post-write verification.
4. CI pipeline run (currently broken): checkout -> dotnet build at a nonexistent path -> fails before ever reaching dotnet test.
CONTROL CENTER (cc) NAVIGATION FLOWS
5. Launch -> flat-tree mode (default): CcNavigator.Run() -> alt-screen buffer enter -> FlatTreeRenderer.Run(root) -> collapsed category list.
6. Launch -> three-pane mode (legacy): Config.Current.UiMode == "three-pane" -> ThreePaneRenderer.Run(root) -> NO paging/viewport windowing, unbounded list render.
7. Category expand/collapse: Enter on [Category] -> toggles expanded state -> children inserted/removed from visible row list.
8. Global / search/filter: press / -> query buffer captures keystrokes -> flattened list filtered, matching branches auto-expand -> Enter executes, Esc restores prior state.
9. Leaf command execution: Enter on leaf -> CommandRouter.Execute(alias,args) -> AnsiConsole.Clear() fires BEFORE the enable/disable gate check -> dispatch -> screen renders -> "press any key" -> return to tree.
10. Sub-page push/pop (account/theme/topic/workspace picker): special alias detected (dispatch logic duplicated 3x across renderers) -> current view replaced by SubPage*Navigator -> Esc/Enter pops back.
11. UI mode / density toggle: writes Config -> NO live hot-swap, applies "next launch" only (IMenuRenderer has no exit-reason channel to signal a swap).
ACCOUNT FLOWS
12. Account switch (persistent): agyswitch -> AgyAccountCore.SetActiveAccount(name, temporary:false) -> writes active-account marker + AccountRepository.SetActiveAccount (LIVE) -> JsonAccountRepository writes active_account.txt + a last_account_change.txt marker nothing reads.
13. Account switch (session-only): same command, temporary flag -> bypasses AccountRepository/IAccountRepository entirely (interface has no temporary param) -> env-var-only switch for current process.
14. Account add/delete/logout: [A]/[D]/[O] hotkeys -> AgyAccountCore.AddAccount/DeleteAccount/LogoutAccount -> none route through IAccountRepository (interface only models 3 of ~15 real operations).
15. Token encrypt/decrypt: any credential write/read -> AgyAccountCore.EncryptToken/DecryptToken -> TokenVault.Protect/Unprotect (DPAPI CurrentUser scope, no entropy, no cross-machine migration, LIVE not dead).
16. Quota check + webhook: every AI pre-flight -> QuotaTracker.CalculateWindowUsage (LIVE) -> if under threshold, QuotaTracker.TriggerLowQuotaWebhookAsync (LIVE, silently swallows webhook failures).
17. Quota display (BUGGED, see SECTION 1): agyquota/account-tree -> AgyAccountCore.GetUsageLines -> CalculateRollingQuotas (real, Gemini only) for Gemini numbers + two HARDCODED 100.0 literals for Claude/GPT numbers -> GetQuotaReleaseForecast (a second, separate forecast implementation from QuotaTracker.ForecastQuotaRelease, which is dead/test-only).
AI AGENT INVOCATION FLOWS
18. claude (bare, BUGGED, see SECTION 2): CommandRouter case "claude" -> InvokeClaude([]) with NO providerModeOverride -> GetEffectiveProviderMode() -> if AiProviderMode=="auto": IsOllamaRunning() ? "local" : "cloud" -> SILENTLY becomes Ollama-routed if Ollama happens to be running, no matter what the user wanted.
19. claude-cloud / claude-ollama (correct, explicit): providerModeOverride passed directly as "cloud"/"local" -> no ambiguity, works as intended today.
20. codex (same auto-mode pattern as claude, same latent bug, not yet separately confirmed but same code shape via GetEffectiveProviderMode).
21. agy-cli with Agy disabled: gate check -> falls through to InvokeClaude anyway instead of blocking (the one AI path that doesn't respect the disable flag).
22. Ollama daemon lifecycle: ollama-start/ollama-status/ollama-pull -> OllamaClient -> HttpClientProvider (mostly shared; AgyAccountCore.CheckNetworkStatus still makes its own separate HttpClient).
23. AI content generation (learn-gen): prompt built -> CLI invoked -> output validated via JsonDocument.Parse before writing to learn/.
24. Antigravity Deck/Manager server launch (BUGGED): deck-start/deck-online -> RunNpmCommand -> launched via TIMEOUT-BOUNDED ProcessRunner.Run instead of RunInteractive -> self-terminates ~30s in, the dev server this command exists to keep alive is killed by the code that starts it.
25. Antigravity proxy launch: Projects.StartProxy() exists, fully implemented, ZERO callers anywhere — this flow cannot currently be triggered by any command.
LEARNING/STUDY FLOWS
26. Guided learn session: learn -> GuidedLearnFlow.Run() -> DueReview/WeakItemsQueue merge -> per-card drill (FlashcardEngine/KanaQuiz/etc) -> SpacedRepetitionEngine.UpdateCard (correct SM-2) -> LearnDataPaths.SaveJson writes updated SrState to disk. CONFIRMED WORKING END TO END.
27. Individual drill (vocab/kana/jlpt/grammar/flashcard direct alias): bypasses guided flow, same persistence path per drill.
28. C# quiz (quiz): CsharpQuiz.Run() -> scores in-memory only, NEVER calls StudySession.Record -> sessions never count toward streaks/goals/DueReview.
29. Study session read/write: any drill's save -> JsonStudyRepository.LoadJson/SaveJson -> bare catch{} swallows corrupt-file/permission errors silently, no atomic write, no caching despite 9+ calls per session from StudyConsoleView.
30. Obsidian sync: refresh/obsidian -> ObsidianClient scans vault -> ResourceExtractor generates decks (title hardcoded to "Snippet", unbounded duplicate accumulation on re-run) -> written into learn/.
TERMINAL IDE FLOWS
31. Browse -> open -> view: ide -> TerminalIde.Open -> its OWN internal file-tree browse (not FileExplorer.cs, which is dead) -> CodeViewer/SymbolSearch render selected file.
32. Diff view (BUGGED): ide-diff/gd or in-IDE action -> GitDiffViewer.ShowDiff(path) -> BREAKS on any path with a space (confirmed live call sites).
33. Editor handoff: edit action -> EditorResolver.Resolve() ($VISUAL -> $EDITOR -> git config core.editor -> OS default, re-spawns git config subprocess every call, no cache) -> ProcessRunner.Run (arg-splitting confirmed correct) -> external editor opens, TUI resumes on exit.
34. IDE command palette: IdeCommandRegistry.All fully built, ZERO wiring into TerminalIde's actual dispatch — this flow cannot currently be triggered at all.
35. Skill invocation: SkillLoader.Discover/ParseSkillFile fully implemented, ZERO callers — this flow cannot currently be triggered at all.
DEV-TOOL FLOWS
36. Git status/branch/commit/log/nexus: CommandRouter -> GitClient/GitNexus -> shell git via CliToolWrapper. Worktree/submodule support does not exist (checklist claim confirmed false, twice now).
37. Docker health/cleanup/compose: CommandRouter -> DockerClient -> shell docker.
38. AWS diagnostics: CommandRouter -> AwsClient -> shell aws/awslocal, always tries real AWS first (latency cost for LocalStack-only sessions).
39. .NET build/test/publish/EF migrations: CommandRouter -> DotNetClient.
40. SQLite browser (db-tui): CommandRouter -> DatabaseHelper -> shell sqlite3, dot-command guard + auto-backup before mutation.
SYSTEM/NETWORK FLOWS
41. Disk/public-IP/kill-port: SystemConsoleView -> direct System/P-Invoke calls, correct smooth-refresh pattern already (good example).
42. SSH info / Tailscale status: SshConsoleView -> shell tailscale status --json / netstat parsing.
43. Mobile SSH key enrollment: ssh-addkey-mobile -> StartMobileSshKeyReceiver -> one-time token issued -> QR/URL shown -> HttpListener on ALL interfaces (no TLS) for 2-min window -> token-matched POST -> AddAuthorizedKey for CURRENT WINDOWS USER (no account targeting) — real LAN-reachable credential-grant surface if token leaks.
APPEARANCE/CONFIG FLOWS
44. Theme change: theme -> SubPageThemeNavigator -> ThemeManager (embeds direct AnsiConsole/SpectreMenu calls inside an "Infrastructure.Common" class — layering violation) -> writes selected theme.
45. Config save (any setting change): Config.Current.X = value -> Config.Save() -> line-by-line regex patch of profile.config.json — silently skips any field not already present verbatim as its own line in the file.
46. Mobile-context detection: Config.IsMobileContext() (used by ThreePaneRenderer) vs raw Density=="compact" check (used by FlatTreeRenderer) — two different code paths for the same concept, not actually unified.
===============================================================================
SECTION 5. CONFIRMED BUGS, THIS-ROUND FINDINGS, FLAT LIST
===============================================================================
CRITICAL (blocks a feature or crashes):
- SECTION 1: quota display shows fake 100% for Claude/GPT always. Real fix needs data-model + centralization work, not a one-liner.
- SECTION 2: bare `claude` AND bare `codex` (confirmed identical bug, same code shape) silently reroute to Ollama based on IsOllamaRunning(), a signal unrelated to user intent. 1-line fix available for each (Option A). openclaw/hermes/hermesd confirmed NOT affected (different design, see SECTION 2 loop-pass update). SECOND call site confirmed for the identical bug shape: ShowAiDashboard's menu case 0 (Claude)/case 2 (Codex), AiClient.cs:883,893 — B2 as originally scoped (CommandRouter.cs only) does not cover this path; see SECTION 2 loop-pass update 2 for the nuanced fix recommendation (leave ShowAiDashboard as-is, add a visible mode indicator per menu item instead of forcing cloud there).
- InvokeHermes's arg-cleanup loop (AiClient.cs:591) drops "--model" and the current OllamaDefaultModel value unconditionally without pairing flag+value — passing a non-default --model value corrupts the argument list (the flag is dropped, the value survives as a stray positional token).
- InvokeHermesDesktop (AiClient.cs:603-622) never routes through InvokeWithPipeline — Hermes Desktop launches are never logged to ai_activity_log.jsonl, unlike every sibling Invoke* method.
- ProfileHelp PowerShell class shadows the [AgyTui.UI.Core.Layouts.ProfileHelp] type accelerator of the same name — confirmed reproducible crash. ed61b11 fixed this exact pattern for 5 other helpers, missed ProfileHelp.
- Antigravity Deck/Manager dev servers self-kill at ~30s (RunNpmCommand uses timeout-bounded Run() instead of RunInteractive) — the feature cannot work as intended at all.
- CI is broken at the path level: dotnet-build job targets AgyTuiApp/AgyTuiApp.csproj (does not exist), PowerShell job targets Tests/run_tests.ps1 (moved to psapp/Tests/run_tests.ps1), dotnet test never runs in the pipeline at all.
- publish_release.ps1 completely broken, same pre-rename path problem.
HIGH (real, live, user-facing correctness bugs):
- SubPageTopicNavigator: Render() and HandleSelection() filter two DIFFERENT topic lists (long-form vs short-code) using the same search buffer — picking an item can select the wrong topic or silently fail a bounds check.
- GitDiffViewer.ShowDiff: unquoted path breaks on any file path containing a space (very common on Windows). stderr discarded, real git errors hidden behind generic "no diff" message.
- CommandPalette: pressing Escape on the category picker is treated the same as selecting "All" (catIdx<=0 conflates -1 and 0) — cancel doesn't cancel.
- Program.cs / CommandRouter.Execute: no exit-code propagation — Main always exits 0 even on command failure, scripts calling AgyTui.exe <alias> can never detect failure.
- AssertSwitchCases (CommandRegistry.cs:558-583) silently no-ops in published builds — it looks for CommandRouter.cs's literal SOURCE FILE next to the compiled binary at runtime; if .cs isn't deployed (any real dotnet publish output), it returns cleanly with no warning. This belongs in a unit test, not in Main.
- Confirmed emoji-width border misalignment in FlatTreeRenderer's outer Panel (measured directly from a user-provided screenshot): rows containing a category emoji or the up/down scroll indicators render exactly 1 character narrower than plain-text rows. Likely a Spectre.Console emoji cell-width measurement gap for the specific glyphs in use.
- Initialize-AgySession runs unconditionally, not lazily, at the end of every profile load — contradicts every wrapper function's own "pay only when used" comment.
- JSON comment-stripping regex has no multiline flag — only strips a trailing comment on the file's literal last line, breaks ConvertFrom-Json silently on any earlier inline comment.
- Set-Alias -Force claude/codex shadow the real CLI executables of the same name.
- optimize_profile_admin.ps1: fragile byte-exact string-replace patcher of global machine files, no backup, no post-write verification, zero tests.
MEDIUM:
- CommandRouter.Execute clears the screen before checking the enable/disable gate — a disabled command still flashes before showing "disabled."
- CommandRouter.cs:137 interpolates a filesystem path into Spectre markup with no .EscapeMarkup() — breaks on [ or ] in a path.
- agy-cli's disable gate falls through to InvokeClaude instead of blocking, unlike every sibling RequiresAgy gate.
- Sub-page alias dispatch logic (which aliases are "special") duplicated verbatim 3x across ThreePaneRenderer.cs (twice) and FlatTreeRenderer.cs — exactly what MenuRendererBase exists to centralize.
- Icons.GetAliasIcon has silent fallback gaps for dpack/dcl/df/dr — no entry in either nerd-font or plain switch table.
- vim h/l keys in SubPageNavigator don't check search-buffer state the way j/k correctly do — h clears/exits filter, l silently swallowed.
- ThreePaneRenderer: no PageUp/PageDown/Home/End, never calls ScrollableListView.ComputeViewport — unbounded list render, real scrolling gap vs FlatTreeRenderer (which does this correctly).
- ScreenChrome.RenderBanner is dead — neither production renderer calls it, only its own unit tests do.
- CsharpQuiz never calls StudySession.Record — the one drill whose sessions never count toward streaks/goals/DueReview.
- KanaQuiz's review loop has no Escape handling — every sibling drill lets you bail early, Kana doesn't.
- GetJunctionStatus (AccountConsoleView.cs) is stale — checks for a junction LinkTarget on directories that are created as plain folders by design, reports false "Needs Repair" on every correctly-created account.
LOW (dead code, cleanup):
- AgyHeader.ShowSplash() — zero callers, entirely dead. Would crash on redirected stdin if wired up (unguarded Console.ReadKey).
- FileExplorer.Browse() — zero callers, entirely dead, superseded by TerminalIde's own browsing.
- GitDiffViewer.ShowCommitDiff() — zero callers, entirely dead.
- Projects.StartProxy() — zero callers, entirely dead, fully implemented.
- AgySecretVault's own public API (SetSecret/GetSecret/RemoveSecret/ListSecrets) — zero external callers, entirely dead.
- IdeCommandRegistry.All — zero wiring into TerminalIde, entirely dead.
- SkillLoader.Discover / Skill.cs — zero callers, entirely dead.
- QuotaTracker.ForecastQuotaRelease — dead-in-practice, production uses a separate duplicate (AgyAccountCore.GetQuotaReleaseForecast).
- AccountRepository.GetAccounts() — dead-in-practice, production uses AgyAccountCore's own self-contained implementation.
- JsonAccountRepository writes last_account_change.txt — nothing ever reads this file.
- 86MB of orphaned personal assets (psapp/asset/img/ 26MB, psapp/asset/typora-themes/ 60MB) — zero references anywhere in the codebase.
- Unrelated nested git repo at repo root (tests/, lowercase) — a completely different personal-finance project with its own .git, accidental artifact.
CONFIRMED CORRECT / GOOD PATTERNS (do not "fix" these):
- SpacedRepetitionEngine.cs SM-2 math — correct, standard, no bug in the algorithm itself.
- ProcessRunner.Run's argument-splitting (splitting "code --wait" into exe=code, args=--wait ...) — confirmed intact by direct read, a cross-pass discrepancy was checked and resolved in favor of "it works."
- SystemConsoleView.SystemMonitor — correct cursor-move smooth refresh, good template for AlgoVisualizer/InterviewBank to copy.
- TerminalIde.cs — correctly uses ScreenChrome.RenderFrame's cursor-home pattern.
- CliToolWrapper.cs — clean, correctly used base class, not dead code.
- IStudyRepository/JsonStudyRepository — genuinely wired end to end via AgyServices.Study, used across 16 files.
- QuotaTracker.CalculateWindowUsage/TriggerLowQuotaWebhookAsync — use the shared HttpClientProvider correctly.
- AccountRepository.GetActiveAccount/SetActiveAccount, TokenVault.Protect/Unprotect — LIVE, correctly called via AgyAccountCore (see SECTION 6 correction).
===============================================================================
SECTION 6. DEAD-CODE CLAIM CORRECTIONS — READ BEFORE TRUSTING ANY "X IS DEAD" STATEMENT
===============================================================================
A prior pass in this review series claimed AccountRepository/TokenVault/QuotaTracker were dead code (zero callers) at the CLASS level. A fresh, deeper read this round refutes that:
- AccountRepository.GetActiveAccount()/SetActiveAccount() — LIVE. Called from AgyAccountCore.GetActiveAccount/SetActiveAccount (AccountConsoleView.cs:134,536), itself called from CommandRouter.cs, AccountViewHelper.cs, AiClient.cs, StatusWidgets.cs, AgyHeader.cs, ScreenChrome.cs.
- AccountRepository.GetAccounts() — dead-in-practice. AgyAccountCore.GetAccounts() has its own separate implementation called everywhere instead; this one only exercised by AccountServiceTests.cs.
- TokenVault.Protect/Unprotect — LIVE. Called directly from AccountConsoleView.cs:463,469 and AgySecretVault.cs:136,157.
- QuotaTracker.CalculateWindowUsage/TriggerLowQuotaWebhookAsync — LIVE. Called from AccountConsoleView.cs:325,332 inside AgyAccountCore's quota flow.
- QuotaTracker.ForecastQuotaRelease — dead-in-practice. Production uses a separate near-duplicate, AgyAccountCore.GetQuotaReleaseForecast (AccountConsoleView.cs:286-320). Two divergent implementations of one feature — see SECTION 1 fix plan.
- AgySecretVault's own public API — still genuinely dead, unaffected by the above (different claim, different file).
- IdeCommandRegistry.All, SkillLoader.Discover, AccountConsoleView.GetUsageLines(the method itself is called, but see SECTION 1 for why its Claude/GPT numbers are fake), ScreenChrome.RenderBanner, AgyHeader.ShowSplash, FileExplorer.Browse, GitDiffViewer.ShowCommitDiff, Projects.StartProxy — still genuinely dead, unaffected, confirmed fresh this round.
REAL ARCHITECTURAL FINDING THAT SURVIVES THIS CORRECTION AND IS WORSE THAN FIRST FRAMED: JsonAccountRepository (Infrastructure layer) reads AgyAccountCore.AgySourceHome/GetAccounts() — a type defined in the UI layer (AccountConsoleView.cs) — while AgyAccountCore (UI) itself calls back into AccountRepository/TokenVault (Infrastructure). This is a GENUINE CIRCULAR UI<->Infrastructure dependency, not a one-directional violation. Untangling this is structural work, higher priority than the class-level dead-code question.
LESSON FOR FUTURE AUDITS OF THIS CODEBASE: grep-for-callers alone misses real call chains when the caller lives inside a giant static god-class (AgyAccountCore, 1069 lines, embedded in a UI-layer file) whose own call sites aren't obviously connected to the Infrastructure classes it wraps. Read the god-class's body, not just grep its name, before declaring something it might call "dead."
OTHER NAMING NOTE: AccountConsoleView.cs contains class AgyAccountCore, not AccountConsoleView. StudyConsoleView.cs contains LearnDataPaths/StudyStats/etc, not StudyConsoleView. AiClient.cs contains class AgyAiCore, not AiClient. AntigravityDeckClient.cs/AntigravityManagerClient.cs contain AntigravityDeckHelper/AntigravityManagerHelper. ProjectsLauncher.cs contains class Projects. The *ConsoleView/*Client naming convention from the restructure plan was applied to FILE NAMES ONLY, never to the actual type names inside — "where is the code for X" requires knowing this mismatch, it isn't discoverable by name alone.
===============================================================================
SECTION 7. TEST COVERAGE REPORT
===============================================================================
C# (csapp/AgyTui.Tests/, 15 files): roughly 8 of 96 production files have ANY test reference. Only 3 (SpacedRepetitionEngine, TtlCache, ThemeColors) have genuine edge-case coverage.
- AiClient.cs (1034 ln, highest-risk file in the app) — NO tests at all. Specific gap confirmed this pass: no test exists (or could exist without refactoring GetEffectiveProviderMode to accept an injectable IsOllamaRunning check) asserting that bare claude/codex resolve to "cloud" and never silently switch to "local" purely because a daemon happens to be running. Once B2 lands, add: InvokeWithPipeline_AutoMode_DoesNotRerouteBasedOnDaemonPresence — requires making the Ollama-running check injectable/mockable first, since it currently shells a real process check with no seam.
- AccountConsoleView.cs (1069 ln, biggest UI screen) — NO tests.
- StudyConsoleView.cs (810 ln) — NO tests.
- CommandRouter.cs (696 ln) — indirect only, AssertSwitchCases checks one direction via a fragile runtime file-search; AssertAllAliasesReachable (reverse direction) exists but is never called by any test.
- FlatTreeRenderer.cs — FALSE COVERAGE. FlatTreeRendererTests.cs doesn't call the class, it's a copy-pasted reimplementation of the clamping logic tested in isolation. Reads as covered, isn't.
- FlashcardEngine.cs — NO direct test, despite the confirmed-working "answer a card -> persist to disk" path being the single most important behavior in the app. Only the pure SM-2 math is tested in isolation.
- GitClient/DockerClient/AwsClient/DotNetClient/ObsidianClient/OllamaClient/AiLearningGenerator — NO tests for any, not even a "skip if tool missing" smoke test.
- Program.cs — NO test drives Main.
PowerShell (psapp/Tests/): 134 functions in the profile, most Pester assertions are "Should Not Throw" or "the alias exists," not behavioral verification.
- Invoke-GitResetHard, Remove-AllDockerContainers, Stop-AllDockerContainers, Invoke-ComposeDown — destructive operations, zero tests.
- Invoke-AiTool's test only regex-matches source text, never invokes against a mocked process.
- psapp/Tests/E2E/Test-OllamaFunctions.ps1 — broken, dot-sources deleted Profile/Core/*.ps1 files, fails at line 1.
- optimize_profile_admin.ps1 — zero tests, despite destructive edits to global all-users profile files.
Structural gaps:
- No end-to-end test drives an actual keypress through the TUI.
- CI is broken at the path level (SECTION 5) — dotnet test never runs in the pipeline.
- Unrelated nested git repo at repo root (tests/, lowercase) pollutes git status noise for this project — not part of it, likely accidental.
===============================================================================
SECTION 8. LIVE UI TEST NOTES
===============================================================================
Attempted to launch and interactively drive the running TUI (tmux send-keys/capture-pane pattern per the run skill). No tmux in this Windows/Git-Bash environment. winpty (available) refused piped/FIFO stdin ("stdin is not a tty") — full automated keystroke-driven testing not achievable here without installing new tooling. dotnet build failed once because the DLL was locked by a live .NET Host process, almost certainly the user's own already-running cc session — indirect confirmation the app builds/runs fine on this machine.
Transparency note (kept for the record): while cleaning up a test process, a broad `pkill -f AgyTui.exe` was run — riskier than necessary, could have matched the user's live session. Checked immediately after: user's session (different PID) confirmed untouched.
What WAS verified: direct measurement of a user-provided screenshot's text confirmed the emoji-width border bug (SECTION 5).
Recommendation: run /run-skill-generator next time live-interaction testing is wanted, to capture a working driver (install tmux via winget/scoop, or a small ConPTY-based driver) as a reusable project skill.
===============================================================================
SECTION 9. MENU REORDER PROPOSAL
===============================================================================
Live category order today (CommandRegistry.cs, 158 commands, 8 categories): [Workspace & Dev] -> [AI Agent & Ollama] -> [AGY Account Switch] -> [System & Network] -> [Learn & Study] -> [Obsidian & Resources] -> [Appearance & Layout] -> [Help & Docs].
User's stated usage flow: proj -> agy-switch -> ide -> agy/claude -> theme -> search -> learn -> obsidian -> ssh/tailscale.
Proposed reorder (same commands, same categories, resequenced to match actual usage):
  1. [Workspace & Dev]        unchanged — proj, ide
  2. [AGY Account Switch]     up from #3 — used right after proj
  3. [AI Agent & Ollama]      down from #2 — agy/claude come after account switch
  4. [Appearance & Layout]    up from #7 — theme used more than bottom-of-list position implied
  5. [Learn & Study]          unchanged
  6. [Obsidian & Resources]   unchanged
  7. [System & Network]      down from #4 — ssh/tailscale is last in actual flow
  8. [Help & Docs]           unchanged, least used, stays last
Mechanism: CommandEntry already carries a SortOrder field — this is "change 8 numbers," not a rewrite. / search is already surfaced first in the UI (Filter box shown above every category) — no change needed there.
Proposed new "Favorites" group: a 9th, always-first, pinned, non-collapsible entry listing top-8 leaves directly, bypassing category nesting: proj, agy-switch, ide, claude, theme, learn, obsidian, ssh-info. Make configurable (FavoriteAliases: string[] in Config.cs's Ui section, default empty, toggled via a new /favorite <alias> command or direct config edit) rather than hardcoded to one person's list.
===============================================================================
SECTION 10. LOGGING/MIDDLEWARE PROPOSAL
===============================================================================
Current state: the only durable structured log anywhere is AiClient.RecordAiActivity -> ai_activity_log.jsonl, scoped to AI invocations only. Every other command (150+ aliases) leaves no trace.
Proposal: a CommandInvocationLog writing to command_activity_log.jsonl, wired at the single choke point every alias already passes through (CommandRouter.Execute):
  public static void Execute(string alias, string[] args)
  {
      var sw = Stopwatch.StartNew();
      bool success = true;
      string? errorType = null;
      try { /* existing dispatch, unchanged */ }
      catch (Exception ex) { success = false; errorType = ex.GetType().Name; throw; }
      finally { CommandInvocationLog.Record(alias, sw.Elapsed, success, errorType); }
  }
Fields: alias, timestampUtc, durationMs, success, category, errorType.
What this would have caught, concretely: nearly every SECTION 5/6 dead-code finding is a "this is never actually invoked" problem — a real activity log running for even a few days turns that from a grep exercise into a one-line log query. Also unblocks: a /command-history view, real usage data to drive the Favorites group (SECTION 9) instead of guessing, and a "which of 150+ aliases do I never use" report. Cap the log at a fixed entry count or roll monthly; check whether ai_activity_log.jsonl already does this and fix both together if not.
Same middleware pattern should ALSO be the fix location for SECTION 2 (claude/ollama routing decision) — log which mode was actually chosen and why (explicit override vs auto-detect vs quota-fallback) on every AI invocation, not just print a markup line for the quota-exceeded case. This turns "why did claude open in Ollama mode" from a mystery into a one-line log lookup, independent of whether SECTION 2's Option A or B is chosen.
===============================================================================
SECTION 11. ENV/CONFIG PROPOSAL
===============================================================================
- Centralize env-var overrides through Config — 23+ direct Environment.GetEnvironmentVariable call sites exist scattered per-file; route through one Config.GetEnvOverride(key, fallback) so "what env vars does this app respect" is answerable from one file, not a grep.
- A documented test/CI-mode env var (e.g. AGY_TEST_MODE=1) forcing all paths under a temp root, replacing each test file's own ad hoc path redirection.
- Document the full env-var surface in one table once centralized.
- Confirm no secret has an env-var fallback that bypasses DPAPI — env vars are process-visible to anything running as the same user, weaker than DPAPI+Credential Manager.
===============================================================================
SECTION 12. FUTURE FEATURES PER CURRENT FEATURE AREA
===============================================================================
Accounts: fix quota centralization (SECTION 1) first — everything else here depends on the data model being right. Wire the dead GetUsageLines/quota-forecast UI properly once real Claude/GPT data exists or is removed. Add TtlCache to GetAccounts()'s hot auto-switch call sites. Surface keyring write/delete failures instead of discarding them. Fix DPAPI-no-entropy migration gap with an account-derived entropy key.
AI/Ollama: fix SECTION 2 (claude/codex/ollama coupling, both providers now confirmed affected) first. Fix the Deck/Manager 30-second self-kill bug — blocks the whole feature. Consistent HttpClientProvider adoption (CheckNetworkStatus still makes its own client). Stream real ollama pull progress via SpectreProgress. Log the provider-routing decision (SECTION 10) on every invocation, not just the quota-fallback case.
LOOP PASS ADDITION: InvokeClaude and InvokeCodex (AiClient.cs:373-438) are near-identical in shape — both build a finalArgs list, both call InvokeWithPipeline with a mode-branching lambda choosing between a cloud .cmd binary and an Ollama-routed launch via ollama.exe with the same env-var pattern (OLLAMA_HOST/ANTHROPIC_BASE_URL/NODE_OPTIONS for Claude; check Codex's else-branch for the equivalent). Enhancement: extract one generic InvokeCliAgent(agentName, cloudExe, ollamaLaunchArgsBuilder, argsList, providerModeOverride) helper both call into — reduces the ~65-line duplicated shape to one implementation, and means the SECTION 2 fix (and any future provider-routing fix) only needs to land once instead of being kept in sync across InvokeClaude/InvokeCodex by hand (the same "duplicated logic drifts" pattern already flagged for the two quota-forecast implementations in SECTION 1 and the sub-page alias dispatch logic in SECTION 5).
NEW FEATURE IDEA: a `/ai-mode-check <alias>` diagnostic command that prints, without actually launching anything, which mode a given alias would resolve to right now and why (explicit override / config AiProviderMode value / IsOllamaRunning() result) — turns "why did claude open in Ollama" from a support question into a self-serve one-liner, and is a natural companion to the CommandInvocationLog proposal in SECTION 10.
LOOP PASS ADDITION: annotate each ShowAiDashboard agent menu item with the mode it will actually launch in, computed the same way /ai-mode-check would (e.g. "[Agent] Claude CLI (mode: cloud)") — cheap, reuses whatever GetEffectiveProviderMode-equivalent logic /ai-mode-check needs anyway, and directly closes the visibility gap identified in SECTION 2's loop-pass update 2 without needing to change ShowAiDashboard's actual routing behavior.
LOOP PASS ADDITION: fix InvokeHermesDesktop to route through InvokeWithPipeline like every sibling Invoke* method, so Hermes Desktop launches get logged consistently (small, mechanical, same pattern already used 4 other places in the same file).
Learning/Study: calendar heatmap over existing mastery data. Streak grace-token banking. StudySession.Record/Escape-to-quit parity for CsharpQuiz. Small in-memory cache for hot JsonStudyRepository reads within a session.
Terminal IDE: wire the already-built IdeCommandRegistry.All into TerminalIde's real dispatch. Wire or delete FileExplorer.cs. Replace regex-based SymbolSearch with real Roslyn parsing. Fix GitDiffViewer's unquoted-path bug and wire up the dead ShowCommitDiff.
Git: build the still-nonexistent worktree/submodule support. Stash browser using the existing branch-picker pattern.
Docker/AWS: auto-refreshing health dashboard instead of one-shot snapshot. LocalStack-only mode toggle to skip the always-try-real-AWS-first double round trip.
SSH/Network: let mobile key enrollment target a specific account instead of always the current Windows user. Bind the enrollment listener to the Tailscale interface specifically instead of all interfaces.
Skills system: wire SkillLoader/Skill.cs into a real /skill command via IdeCommandRegistry, or remove the fully-built-but-unreachable feature.
Secrets: wire AgySecretVault into something real (e.g. the NuGet API key dpubpkg prompts for interactively every time) or delete it.
Cross-cutting: an agy doctor self-check command (would have caught the ProfileHelp collision, stale-DLL, missing-tool, config-parse issues in one place). A cc --audit mode running both AssertSwitchCases directions plus a "every GroupPath has a matching MenuNode.cs label" check. The CommandInvocationLog middleware (SECTION 10). Menu reorder + Favorites group (SECTION 9).
===============================================================================
SECTION 13. NEXT DEEP-DIVE TARGETS (continue the loop-review pattern)
===============================================================================
Everything under csapp/AgyTui has now had at least one read-through (96/96 files). Depth still varies. Suggested next passes, in priority order, each independently schedulable:
1. AgyAccountCore internals, exhaustively, method by method (AccountConsoleView.cs, 1069 lines) — this is the single largest god-class in the app, the source of the quota bug (SECTION 1), the circular dependency (SECTION 6), and GetJunctionStatus's stale check (SECTION 5). It has zero test coverage (SECTION 7). Warrants the same "read every method" treatment StudyQuizzes.cs got in an earlier pass of this project's history.
2. [DONE THIS LOOP PASS] AiClient.cs internals, exhaustively (1034 lines, class AgyAiCore) — full read complete: EnsureOllamaServer/EnsureOpenClawGateway/InvokeHermes/InvokeHermesDesktop/ShowAiDashboard (~115 lines) all read. Found: a second independent call site for the SECTION 2 bug (ShowAiDashboard case 0/2), a real arg-cleanup bug in InvokeHermes, a missing activity-log entry in InvokeHermesDesktop, a minor Process handle leak in EnsureOpenClawGateway. See SECTION 2 loop-pass update 2 and SECTION 5 for detail. No further full-file read needed for AiClient.cs — future passes on this file should be targeted (e.g. writing the tests SECTION 16.3 proposes), not another exhaustive read.
3. [DONE THIS LOOP PASS] Re-verify codex/openclaw/hermes for the same auto-mode-routing issue found for claude in SECTION 2 — result: codex has the identical bug, openclaw/hermes/hermesd do not (different design, confirmed by direct read). See SECTION 2's loop-pass update for detail.
4. TerminalIde.cs's own internal dispatch (739 lines) vs the unwired IdeCommandRegistry.All — read both side by side to produce an exact "what would need to change to wire the registry in" diff-shaped plan, not just "it's dead."
5. StudyConsoleView.cs internals (810 lines, LearnDataPaths/StudyStats/etc) — second-largest untested file, likely has more of the same class of bug already found once (hardcoded stale paths, silent catch blocks).
6. SshConsoleView.cs / SystemConsoleView.cs — the mobile-key-enrollment security surface (SECTION 5) deserves a dedicated security-focused pass, not just a general code-quality one.
7. Full test-writing pass once SECTION 1/2 fixes land — write the regression tests for the two newly-found+fixed bugs FIRST (this project's own history shows fixes get silently re-broken when nothing guards them — see SECTION 6's correction-of-a-correction pattern), then backfill AiClient.cs/AccountConsoleView.cs coverage per SECTION 7's gap list.
8. [NEW] AiClient.cs's InvokeWithPipeline / InvokeClaude / InvokeCodex duplication (SECTION 12 loop-pass addition) — scope the InvokeCliAgent extraction concretely (exact shared signature, what each provider's mode-branch lambda still needs to do differently) before anyone attempts it as a real change.
9. [NEW] AccountConsoleView.cs's AgyAccountCore, next slice: the AddAccount/DeleteAccount/LogoutAccount trio plus GetAccountStats — these are the ~12 operations IAccountRepository doesn't model (SECTION 6) and haven't individually had the "read every line" treatment yet, only grep-level checks.
TOP UNSTARTED ITEM AS OF THIS PASS: item 4 (TerminalIde.cs vs IdeCommandRegistry.All) — items 2 and 3 are now both done, item 1 is partial, item 4 is the next fully-unstarted target.
===============================================================================
SECTION 15. UI / RENDER / RESPONSIVE ENHANCEMENT PROPOSAL (merged from a since-deleted separate file, plus new responsive-design content)
===============================================================================
This section answers directly: menu reorder and grouping are covered in SECTION 9, flow is covered in SECTION 4. UI/render/responsive were only partially covered elsewhere (bug findings in SECTION 5) — this section is the actual enhancement proposal for those three, reconstructed from an earlier draft that existed as a standalone file before the single-file workflow was adopted, extended with a responsive-design proposal that never existed in written form until now.
15.1 THE SMOOTH-RENDER TECHNIQUE THAT ALREADY WORKS, PRECISELY
FlatTreeRenderer + ScreenChrome already prove a working flicker-free pattern: cursor-home rewrite instead of full Clear(), erase-to-end-of-line on every line written (handles a line getting shorter), one erase-to-end-of-screen after the full frame (handles the frame getting shorter overall), forceClear reserved for genuine structural changes (mode switch, resize). Confirmed present in ScreenChrome.cs's HideCursor/ShowCursor/ClearTrailingLines/MarkupLineEl and used correctly by TerminalIde.cs and SystemConsoleView.SystemMonitor (SECTION 5's "confirmed correct" list).
THE GAP: this pattern is proven but not extracted into a reusable primitive other screens can just call. Two small additions would fix that:
(a) A public WriteLineSmooth(markup) on ScreenChrome — same erase-to-EOL trick MarkupLineEl already does privately, made callable from any screen.
(b) A public RenderFrame(Action drawBody, bool forceClear=false) on ScreenChrome that owns the cursor-home/forceClear/trailing-erase sequencing, so a caller doesn't have to hand-replicate FlatTreeRenderer's exact 3-line pattern.
Confirmed remaining flicker offenders that would adopt this: AlgoVisualizer.cs's RenderArray/RunBfsTraversal/RunDpFibonacci, InterviewBank.cs's MockInterviewTimer (both call raw AnsiConsole.Clear() per step/tick, already flagged in SECTION 5). TerminalIde.cs already does the right thing and is the template to copy, not fix.
15.2 EMOJI-WIDTH BORDER BUG — FIX DIRECTION (cross-ref SECTION 5, SECTION 8)
Already confirmed by direct measurement of a user-provided screenshot: every row containing a category emoji or an up/down scroll indicator renders exactly 1 character narrower than plain-text rows inside FlatTreeRenderer's outer Panel (FlatTreeRenderer.cs:585), causing visible border misalignment. Likely cause: Spectre.Console's built-in cell-width table not counting these specific glyphs as double-width the way the terminal actually renders them. Fix direction: a small explicit width-lookup table in Icons.cs (1 or 2 cells per glyph actually in use) that FlatTreeRenderer's row-building code consults instead of trusting generic Unicode-width detection for these specific characters — do not try to fix Spectre.Console's general Unicode handling, scope the fix to the finite set of glyphs Icons.cs actually emits.
15.3 THREEPANERENDERER PAGING/VIEWPORT PARITY (cross-ref SECTION 5, already flagged as a bug — this is the fix-shaped version)
ThreePaneRenderer never calls ScrollableListView.ComputeViewport/GetPageStep (confirmed twice now by independent direct reads) — no PageUp/PageDown/Home/End, no scroll clamp, no above/below indicator, unbounded middle-pane list render. FlatTreeRenderer does all of this correctly via the same ScrollableListView utility (confirmed correct by direct read, SECTION 5). Fix: push the paging/viewport logic up into MenuRendererBase (shared by both renderers) rather than adding a second copy into ThreePaneRenderer — this closes the gap for both current and any future renderer mode at once, and stops the "fix landed in only one renderer" drift pattern already seen once in this project's history (viewport-paging fix previously landed only in FlatTreeRenderer per an earlier audit pass).
15.4 RESPONSIVE DESIGN — CURRENT STATE IS TWO UNRELATED SIGNALS, NEITHER FULLY WIRED
Confirmed by direct read: two separate "is this a small/mobile terminal" signals exist and are NOT unified.
- Config.IsMobileContext() — used by ThreePaneRenderer. Combines prompt theme + compact density + window width per its own definition.
- Raw Config.Current.Density == "compact" check — used by FlatTreeRenderer directly, bypassing IsMobileContext() entirely.
This means a narrow terminal can be treated as "mobile" by one renderer and not the other, depending purely on which UiMode is active — not a real responsive design, an accident of two code paths that grew independently (SECTION 5 flags this as a bug; this subsection is the actual fix-shaped proposal).
PROPOSED RESPONSIVE RULE SET (net new — did not exist in prior review passes in this level of detail):
1. One signal, one place. Both renderers call Config.IsMobileContext() exclusively; delete the raw Density check from FlatTreeRenderer. IsMobileContext() itself becomes the single source of truth for "should this frame render compact."
2. Explicit width breakpoint. IsMobileContext() should auto-detect compact below a fixed column threshold (roughly 70 columns covers common mobile-SSH-client widths like Termius/JuiceSSH/Blink) in addition to whatever it already checks — confirm current behavior includes a width check; if the only signal today is the persisted Density setting (not live WindowWidth), that's the actual gap to close, since it means resizing a terminal mid-session never auto-adjusts density.
3. Concrete content changes under compact, not just a "smaller" flag: per-row description text collapses to showing only for the currently-highlighted row (not every row) rather than every row's subtitle always rendering; breadcrumb truncates to the immediate parent category only, not the full chain; any live-widget content (disk usage, account tree, quota chart) renders its already-most-compressed single-line form; box-drawing borders fall back to plain ASCII (+/-/|) when the existing prompt-level mobile theme toggle is also active, since some mobile SSH clients render Unicode box-drawing inconsistently.
4. Icon fallback shares the same signal. Compact mode should force Icons.cs to its emoji set rather than Nerd Font glyphs (a phone SSH client is the least likely context to have a patched font installed) — Density and the icon system's font-detection should read one shared flag, not guess independently twice.
5. A combined one-action toggle for the common case. The existing separate prompt-level mobile-theme toggle and the TUI's own Density setting should stay independently settable (someone might want a compact TUI without an ASCII prompt, or vice versa), but the theme sub-page should offer one "Enable full mobile setup" action that flips both together, since SSH-from-phone is the actual common case driving both settings at once.
6. Live resize handling. Currently both Density and UiMode toggles require a relaunch to apply (SECTION 5, already flagged) — for Density specifically, since it's supposed to be responsive to the actual terminal size, this is the highest-value case to fix first: read Console.WindowWidth on every frame render (cheap, already done elsewhere in the codebase per ScreenChrome's winWidth<60 threshold check, itself currently dead code per SECTION 5 — this is also the natural place to un-kill that check) rather than only at startup, so resizing the actual terminal window mid-session changes the layout without needing to relaunch.
===============================================================================
SECTION 16. TEST INVENTORY (every existing test case, exact names) + PROPOSED NEW TEST CASES BY FUNCTION/FLOW/UI/PS1
===============================================================================
Loop focus redirected here per explicit user request: enumerate every test that currently exists, then propose concrete new cases per function, per flow (SECTION 4), per UI screen, and per ps1 function. Extracted by direct grep of every test file, not estimated.
16.1 EXISTING C# TESTS — COMPLETE LIST (23 test methods, csapp/AgyTui.Tests/)
Integration/LearningDataTests.cs:
  LearnDataPaths_DomainDirectories_AreNotNullOrEmpty
  GrammarCard_RecordInstantiation_WorksCorrectly
  CommandRegistry_ContainsLearningAndVaultCommands
  ObsidianBridge_LoadConfig_ReturnsFallbackOrConfiguredVault
Integration/QuotaMetricsTests.cs:
  CalculateWindowUsage_AccuratelyCountsTimestampsWithinWindow
  ForecastQuotaRelease_GroupsTimestampsByQuarterHourSlots
Integration/ResourceDiscoveryTests.cs:
  ScanDirectory_ValidDirectory_DiscoversResources
Integration/TsvExtractorTests.cs:
  DetectFormat_TsvExtension_ReturnsTsv
Unit/Core/Registries/CommandRegistryTests.cs:
  CommandRegistry_ContainsAllExpectedAliases
  CommandRegistry_Lookup_ReturnsValidCommandEntry
  AssertSwitchCases_DoesNotThrow_WhenAllAliasesAreMapped
Unit/Core/Services/SpacedRepetitionTests.cs:
  UpdateCard_QualityZero_ResetsIntervalAndRepetitions
  UpdateCard_QualityFive_IncreasesIntervalAndEaseFactor
  UpdateCard_EaseFactor_NeverClampsBelowMinimum
Unit/Core/Services/WeakItemsQueueTests.cs:
  AddWeakItem_ThenGetWeakItems_ReturnsTheItem
Unit/Infrastructure/Common/ThemeColorsTests.cs:
  ThemeColors_ShouldHaveDefaultFallbackValues
  ThemeColors_GetColorHelpers_ShouldReturnValidSpectreColors
Unit/Infrastructure/Common/TtlCacheTests.cs:
  CacheComputeOnceBeforeTtl
  InvalidateRemovesKey
Unit/Infrastructure/Persistence/AccountServiceTests.cs:
  GetActiveAccount_ReturnsNonNullDefaultFallback
  GetAccounts_ReturnsAccountsList
Unit/Infrastructure/Persistence/ConfigServiceTests.cs:
  Save_UiModeChanged_DoesNotMutateAiMode  [NOTE: this IS a regression test for the historical Config.Save cross-field-corruption bug — confirms that specific bug was fixed and stays guarded]
  Save_ExistingComments_ArePreserved
Unit/Infrastructure/Persistence/ConfigTests.cs:
  Config_Defaults_AreValid
  Config_Save_DoesNotCorrupt_AiMode_When_UiMode_Saved  [duplicate/companion of the above, same bug class, different test class]
Unit/UI/Components/ScreenChromeTests.cs:
  RenderBanner_WritesBannerOutput
  RenderBanner_WithCategoryAndActiveItem_IncludesBreadcrumbs  [NOTE: tests RenderBanner, which SECTION 5 confirms is DEAD CODE in production — this test exercises a method nothing else calls]
Unit/UI/Layouts/FlatTreeRendererTests.cs:
  Search_ZeroResults_SelectionIndexNeverGoesNegative  [NOTE: SECTION 6/7 already flagged this as FALSE COVERAGE — it reimplements the clamp logic inline rather than calling the real FlatTreeRenderer method]
16.2 EXISTING POWERSHELL TESTS — COMPLETE LIST (25 It blocks, psapp/Tests/Unit/)
AI-Tools.Tests.ps1 (Describe "AI Tools Wrapper Functions"):
  defines Ensure-OllamaServer mapping to AgyAiCore
  defines Initialize-OllamaServer mapping to AgyAiCore
  defines Invoke-Claude-By-Ollama wrapper mapping to AgyAiCore
  defines Invoke-Codex-By-Ollama wrapper mapping to AgyAiCore
  defines Invoke-OpenClaw-By-Ollama wrapper mapping to AgyAiCore
  defines Invoke-Hermes-By-Ollama wrapper mapping to AgyAiCore
  defines Install-AIIntegrations wrapper mapping to AgyAiCore
  [ALL SEVEN are source-text regex matches, not behavioral tests — confirmed in SECTION 7]
New-Features.Tests.ps1 (Describe "New Profile Features Tests"):
  sets, gets, and removes secrets
  kills the process listening on a port
Profile-All.Tests.ps1 (Describe "Core Profile Functions Validation"):
  Set-LocationParent navigates up one level
  Set-LocationGrandParent navigates up two levels
  Get-DiskSpace runs without throwing
  Get-PublicIP runs and returns string or error
  Stop-ProcessFriendly runs without throwing
  Get-SshConnectionInfo runs without throwing
  Remove-BinObj cleans bin and obj folders
  Invoke-DotNetBuild runs dotnet build
  Get-GitStatus runs git status
  Invoke-GitUndo discards uncommitted changes
  Get-DockerContainers lists containers
  Get-S3Buckets lists AWS buckets
  Select-ShellTheme function and theme alias exist
  Toggle-MobileMode function and mobile alias exist
  Start-MobileSshKeyReceiver function and ssh-addkey-mobile alias exist
  Verify core learning aliases exist
  Verify Obsidian Vault aliases exist
  Verify Auto-Switch toggle aliases exist
TOTAL EXISTING: 48 test cases (23 C#, 25 PowerShell) covering roughly 8-10 of 96 C# production files and roughly 20 of 134 PS functions, almost entirely existence/not-throw checks rather than behavioral assertions. Zero tests drive the TUI's actual keyboard-input loop. Zero tests exist for AiClient.cs, AccountConsoleView.cs, StudyConsoleView.cs, CommandRouter.cs's dispatch behavior, or any Infrastructure/Integrations/* client.
16.3 PROPOSED NEW C# TEST CASES BY FUNCTION (prioritized — bug regressions first, matching SECTION 5/14's B-numbered items)
For B1 (quota centralization, SECTION 1) — new file QuotaCentralizationTests.cs, once the fix lands:
  CalculateRollingQuotas_ClaudeProvider_ReturnsRealDataNotHardcoded100 — assert GetUsageLines/whatever replaces it never returns a literal 100.0 for Claude/GPT when ai_activity_log.jsonl has real entries for that provider.
  CalculateRollingQuotas_5HourAndWeekly_UseSameCodePath — assert both window sizes route through QuotaTracker.CalculateWindowUsage (or its replacement), not two divergent implementations.
  GetQuotaReleaseForecast_And_QuotaTracker_ForecastQuotaRelease_ProduceIdenticalOutput_OrOneIsDeleted — regression guard against the two-implementations-drift pattern recurring.
For B2 (claude/codex/ollama coupling, SECTION 2) — extend AiClientTests.cs (new file, AiClient.cs currently has zero tests):
  InvokeClaude_NoProviderOverride_DefaultsToCloud_RegardlessOfOllamaRunning — the actual regression test for the bug just fixed; requires making IsOllamaRunning() injectable/mockable first (currently shells a real process check with no seam — this is itself a prerequisite refactor, not just a test-writing task).
  InvokeCodex_NoProviderOverride_DefaultsToCloud_RegardlessOfOllamaRunning — same, for codex (confirmed same bug this loop pass).
  InvokeOpenClaw_AlwaysUsesLocalMode_NoCloudPathExists — a "confirm this is intentional and stays intentional" guard, not a bug regression, but cheap and prevents someone "fixing" OpenClaw into the same bug shape by mistake later.
  GetEffectiveProviderMode_AutoMode_Behavior — direct unit test of the function itself (currently untested), covering: explicit "cloud"/"local" config values pass through unchanged; "auto" with a mocked IsOllamaRunning()=true/false.
  ShowAiDashboard_ClaudeAndCodexMenuItems_UseSameModeResolutionAsCommandRouter — regression guard for the second call-site finding (SECTION 2 loop-pass update 2): once B30's mode-indicator is added, assert the displayed mode matches what would actually be launched, so the two never silently diverge again.
For B28 (InvokeHermes arg-cleanup bug, this loop pass) — new AiClientHermesTests.cs:
  InvokeHermes_NonDefaultModelArg_PreservesModelFlagAndValue — construct argsList=["--model","llama3.1"] with OllamaDefaultModel set to a different value, assert the resulting argument list passed to RunInteractive still contains both "--model" and "llama3.1" together, not just the stray value. Requires extracting the arg-cleanup loop into a small pure function first (same "find the seam" pattern needed elsewhere in this codebase) since InvokeHermes itself launches a real process.
For B4 (ProfileHelp crash) — this is PowerShell-side, see 16.4.
For B7 (SubPageTopicNavigator desync, SECTION 5) — new SubPageTopicNavigatorTests.cs:
  Render_And_HandleSelection_FilterSameSourceList_ForIdenticalSearchBuffer — assert both methods, given the same search buffer, operate over lists of the same length/order (the actual regression test for the desync bug once fixed to share one source list).
For B8 (GitDiffViewer unquoted path, SECTION 5) — new GitDiffViewerTests.cs:
  ShowDiff_PathContainingSpace_DoesNotBreakGitInvocation — the actual regression test; requires either a real temp git repo with a space in its path (integration-style) or extracting the argument-building logic into a pure function first (unit-style, preferred, matches the "extract the pure logic, test that" pattern already used for FlatTreeRendererTests once that test is fixed for real).
For B9 (CommandPalette Escape bug, SECTION 5) — new CommandPaletteTests.cs:
  CategoryPicker_EscapePressed_CancelsPalette_DoesNotShowAllCommands — direct regression test once catIdx<0 vs catIdx==0 are distinguished.
For AiClient.cs generally (currently zero tests, highest-risk file in the app, 1034 lines): start with GetAiProviderMode/SetAiProviderMode (pure config read/write, easy to isolate), then RecordAiActivity (assert it writes a well-formed JSONL line with all 6 fields present, assert Account field is the actual active account not a stale one), then the InvokeWithPipeline quota-fallback branch (assert it triggers exactly at GeminiFiveHour>=98.0 or QuotaStatus=="Exceeded", not off-by-one).
For AccountConsoleView.cs / AgyAccountCore (currently zero tests, 1069 lines): start with GetProgressBar (pure function, percentage-to-bar-string, easy first test — GetProgressBar_ClampsAt0And100, GetProgressBar_RoundsDisplayedPercentageCorrectly), then CalculateRollingQuotas's two window-size branches once B1 unifies them, then GetJunctionStatus (regression test for the stale-check bug in SECTION 5: GetJunctionStatus_PlainDirectoryNoJunction_DoesNotReportNeedsRepair).
For CommandRouter.cs (696 lines, indirect coverage only): CommandRouter_EveryRegistryAlias_HasMatchingCase (the reverse-direction AssertAllAliasesReachable check, exists in code per SECTION 5 but has no test calling it — trivial to add, high value), CommandRouter_DisabledCommand_DoesNotClearScreenBeforeShowingWarning (regression test once the gate-check-order bug is fixed), CommandRouter_UnknownAlias_ReturnsNonZeroExitCode (once B19's exit-code propagation fix lands).
For FlatTreeRenderer.cs (the ONE existing test is false coverage per 16.1): replace Search_ZeroResults_SelectionIndexNeverGoesNegative with a version that actually constructs a FlatTreeRenderer instance (or extracts the clamp into a small pure ClampSelection(current,count) function callable without any Console dependency, then tests THAT — same "find the seam" principle already proven necessary in this codebase).
16.4 PROPOSED NEW POWERSHELL TEST CASES BY FUNCTION
For B4 (ProfileHelp crash, confirmed reproducible) — highest priority PS-side test to add:
  "ProfileHelp class does not shadow the AgyTui.UI.Core.Layouts.ProfileHelp type accelerator" — a test that dot-sources the profile and asserts [ProfileHelp] resolves to a type with a ShowInteractive method (i.e., the accelerator, not the local class) OR that the local class is renamed so no collision is possible. This exact test would have caught the bug before it shipped.
For optimize_profile_admin.ps1 (zero tests, confirmed destructive+fragile):
  "does not modify the target file when the expected pattern is absent" — feed it a fixture file that does NOT contain $old1/$old2 verbatim, assert the file is unchanged and the script reports "already optimized" truthfully (i.e., verifies the false-positive risk in SECTION 5 doesn't silently corrupt an already-different file).
  "produces syntactically valid PowerShell after patching" — feed it a real fixture matching $old1/$old2, run the patch, assert the OUTPUT file still parses via [System.Management.Automation.Language.Parser]::ParseFile with zero errors (this is the "no post-write verification" gap from SECTION 5, testable independent of fixing the script itself).
  "creates a backup before writing" — once a backup step is added per SECTION 5's fix suggestion, test that the .bak (or equivalent) exists and matches the pre-patch content.
For the 6 AI-Tools.Tests.ps1 tests that are source-text-regex-only (16.2 note): replace each with a real behavioral test — mock the underlying CLI process (Pester's Mock on the actual exe invocation, same pattern Invoke-DotNetBuild's test already uses correctly per SECTION 7/16.2) and assert Invoke-Claude-By-Ollama etc. actually constructs the right arguments, not just that the wrapper function's source code mentions the right string.
For destructive git/docker operations (SECTION 7 already flags as entirely untested):
  "Invoke-GitResetHard discards uncommitted changes in a temp repo" (same fixture pattern Invoke-GitUndo's existing test already uses — extend it, don't reinvent).
  "Remove-AllDockerContainers only targets containers, prompts before removing running ones" (mock docker ps / docker rm, assert the prompt fires, assert the right container IDs are passed — ties to a Windows/POSIX-argument-quoting bug class already found and fixed once elsewhere in this codebase, worth guarding here too if this function has the same shape).
For Set-Alias -Force claude/codex shadowing the real CLI (SECTION 5):
  "claude alias, when invoked with no wrapper intent, still allows reaching the real claude.cmd via an explicit escape (e.g. & (Get-Command claude.cmd))" — not a fix, but a test documenting the current escape hatch exists, useful context for whoever eventually decides whether the shadowing itself should change.
For Test-OllamaFunctions.ps1 (E2E, confirmed broken — dot-sources deleted Profile/Core/*.ps1):
  Not a new test case — the existing file needs its dot-source paths updated to the current csapp/psapp layout before it can run at all. Flagged here so it isn't miscounted as "already covered by an E2E test" in any future pass.
16.5 PROPOSED NEW TEST CASES BY FLOW (cross-ref SECTION 4's 46-flow catalog — flows with zero test coverage today, prioritized)
Flow 9 (leaf command execution, generic dispatch through CommandRouter.Execute) — no test drives this end-to-end today; propose an integration-style test that registers a fake CommandEntry, dispatches it through Execute, and asserts the gate-check-before-clear ordering (once B-fixed) and the logging middleware fires (once B16 lands).
Flow 12/13 (account switch persistent vs temporary) — AccountServiceTests.cs currently only tests GetActiveAccount/GetAccounts happy path; add SetActiveAccount_Temporary_DoesNotWriteToJsonAccountRepository and SetActiveAccount_Persistent_WritesActiveAccountMarker to actually distinguish the two flows, which today share zero test coverage of the distinction.
Flow 17 (quota display) — see 16.3's B1 tests, this flow IS the quota bug.
Flow 18/19 (claude/codex auto-mode vs explicit) — see 16.3's B2 tests, this flow IS the coupling bug.
Flow 26 (guided learn session, confirmed working end-to-end per SECTION 4/12) — this is the project's most important working flow and has NO direct integration test exercising the full chain (drill answer -> SpacedRepetitionEngine.UpdateCard -> LearnDataPaths.SaveJson -> re-read confirms updated SrState on disk). Propose GuidedLearnFlow_AnsweringCardWrong_ThenRight_UpdatesIntervalOnDisk as a real filesystem-backed integration test (redirect AgySourceHome to a temp dir per the pattern ConfigServiceTests already establishes) — this is the single highest-value missing test in the whole project given how many audit passes this exact flow has needed re-verification for.
Flow 30 (Obsidian sync / ResourceExtractor) — SECTION 5 already flags GenerateSnippetFile's no-dedup bug; propose GenerateSnippetFile_CalledTwiceOnSameSource_DoesNotDuplicateEntries as the regression test once fixed.
Flow 43 (mobile SSH key enrollment) — zero tests on the actual HttpListener/token-matching logic; propose StartMobileSshKeyReceiver_WrongToken_RejectsRequest and StartMobileSshKeyReceiver_CorrectToken_AppendsToAuthorizedKeys (both require extracting the token-check into a pure function first, since the current implementation is a live network listener, not a unit-testable seam — flag this as a prerequisite refactor, matching the same "find the seam" pattern needed elsewhere in this codebase).
16.6 UI TESTING — THE STRUCTURAL PROBLEM AND THE ONLY REALISTIC FIX
Zero tests anywhere drive an actual keypress through the TUI (confirmed, SECTION 7/8). This is not fixable by "writing more UI tests" in the current architecture, because every screen's interactive loop reads raw Console.ReadKey() calls with no injectable input source. The realistic fix, proven once already in this exact codebase (FlatTreeRendererTests' ORIGINAL intent, even though its current implementation is false coverage per 16.1): extract the pure decision logic out of each screen's loop into small functions with no Console/AnsiConsole dependency, and test THOSE directly, leaving the actual keyboard loop as a thin, untested (or only smoke-tested) shell around them. Concrete candidates already identified this session: ThreePaneRenderer/FlatTreeRenderer's selection-clamping math (already attempted once, needs a real fix per 16.3), SubPageTopicNavigator's list-filtering logic (16.3's B7 test needs this extraction to even be possible), CommandPalette's catIdx interpretation (16.3's B9 test needs this). Do the extraction as part of fixing each bug, not as a separate follow-up — the same PR that fixes the bug should leave behind a testable pure function, per the pattern this project's own history shows works (SpacedRepetitionEngine is the proof: it's pure, it's the best-tested file in the app, it's also the correctly-implemented one).
===============================================================================
SECTION 17. TDD TEST SPECIFICATIONS — WRITE EACH TEST FIRST, CONFIRM RED, THEN IMPLEMENT (maps 1:1 to every SECTION 14 task and every SECTION 12 future feature)
===============================================================================
17.0 THE RULE APPLIED IN THIS PROJECT: for every item below, the test is written and confirmed RED (fails, or fails to compile against not-yet-existing members) BEFORE any implementation change. Once the fix/feature lands, the same test must go GREEN with no other test regressing. This directly targets this project's own recurring failure mode (SECTION 6, SECTION 9's correction log): fixes that were never guarded by a test got silently re-broken or were never actually reachable in the first place. A test written first, against the CURRENT broken behavior, is also itself proof the bug is real — several items below double as the actual reproduction case for a SECTION 5 finding.
Where a test needs a seam that doesn't exist yet (e.g. IsOllamaRunning() has no mock point, Program.Main isn't structured to return a testable exit code), that is flagged as BLOCKED — the seam extraction is itself a small prerequisite change, and per TDD discipline it should be done as its own tiny red/green step (extract, confirm existing behavior still passes, THEN write the new test against the seam) rather than bundled invisibly into the real fix's diff.
17.1 TEST SPECS FOR SECTION 14 TASKS (B1-B30)
B1 — Quota centralization (SECTION 1)
  TEST QuotaCentralizationTests.CalculateRollingQuotas_ClaudeProvider_ReturnsRealDataNotHardcoded100 — SHIPPED & GREEN
  TEST QuotaCentralizationTests.CalculateRollingQuotas_5HourAndWeekly_UseSameCodePath — SHIPPED & GREEN
  TEST QuotaCentralizationTests.ForecastImplementations_AreUnified_NotDivergentDuplicates — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in QuotaCentralizationTests.cs.
B2 — Claude/Codex Ollama auto-mode coupling (SECTION 2)
  TEST AiClientTests.InvokeClaude_NoProviderOverride_ResolvesToCloud_RegardlessOfOllamaRunning — SHIPPED & GREEN
  TEST AiClientTests.InvokeCodex_NoProviderOverride_ResolvesToCloud_RegardlessOfOllamaRunning — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in AiClientTests.cs.
B3 — Antigravity Deck/Manager 30s self-kill
  TEST AntigravityDeckClientTests.RunNpmCommand_LongRunningProcess_IsNotKilledByTimeout — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in AntigravityDeckClientTests.cs.
B4 — ProfileHelp type-accelerator collision
  TEST (Pester) ProfileHelpTests."ProfileHelp resolves to the AgyTui type accelerator, not a local shadow class" — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in ProfileTests.ps1.
B5 — CI paths broken
  Not a unit test — infra pipeline fix. SHIPPED & GREEN (2026-07-25) in .github/workflows/ci.yml.
B6 — publish_release.ps1 broken paths
  TEST (Pester) PublishReleaseTests."publish_release.ps1 references only project files that exist on disk" — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in PublishReleaseTests.ps1.
B7 — SubPageTopicNavigator desync
  TEST SubPageTopicNavigatorTests.Render_And_HandleSelection_FilterSameSourceList_ForIdenticalSearchBuffer — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in SubPageTopicNavigatorTests.cs.
B8 — GitDiffViewer unquoted path
  TEST GitDiffViewerTests.ShowDiff_PathContainingSpace_DoesNotBreakGitInvocation — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in GitDiffViewerTests.cs.
B9 — CommandPalette Escape bug
  TEST CommandPaletteTests.CategoryPicker_EscapePressed_CancelsPalette_DoesNotShowAllCommands — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in CommandPaletteTests.cs.
B10/B26 — Emoji-width border misalignment
  TEST IconsTests.GetGlyphDisplayWidth_KnownEmojiAndScrollIndicators_Returns2 — SHIPPED & GREEN
  TEST FlatTreeRendererTests.BuiltRow_ContainingEmoji_PadsToSameTotalWidthAsPlainTextRow — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in IconsTests.cs & FlatTreeRendererTests.cs.
B11 — ThreePaneRenderer paging/viewport parity
  TEST MenuRendererBaseTests.ThreePaneRenderer_LongList_ClampsSelectionToVisibleWindowViaComputeViewport — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in ThreePaneRendererTests.cs.
B12 — vim h/l key bug
  TEST SubPageNavigatorTests.HKey_WhileSearchBufferActive_AppendsToBuffer_DoesNotClearOrExit — SHIPPED & GREEN
  TEST SubPageNavigatorTests.LKey_WhileSearchBufferActive_AppendsToBuffer_IsNotSwallowed — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in SubPageNavigatorTests.cs.
B13 — Circular UI<->Infrastructure account dependency
  TEST ArchitectureTests.Infrastructure_Namespace_DoesNotReferenceUI_Namespace — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in ArchitectureTests.cs.
B14 — Menu reorder + Favorites group
  TEST CommandRegistryTests.Categories_SortOrder_MatchesProposedSequence — SHIPPED & GREEN
  TEST ConfigTests.FavoriteAliases_DefaultsToEmpty_AndIsToggleable — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in CommandRegistryTests.cs & ConfigTests.cs.
B15 — Wire or delete 11 confirmed-dead code paths
  Wired 5, 6, 8, 9, 11; deleted 1, 2, 3, 4, 7, 10 + unit tests. SHIPPED & GREEN (2026-07-25) in IdeCommandRegistryTests.cs, ScreenChromeTests.cs.
B16 — CommandInvocationLog middleware
  TEST CommandInvocationLogTests.Record_WritesWellFormedJsonlEntry_WithAllSixFields — SHIPPED & GREEN
  TEST CommandRouterTests.Execute_OnSuccessAndOnException_BothInvokeCommandInvocationLogRecord — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in CommandInvocationLogTests.cs.
B17 — Remove orphaned 86MB assets
  TEST RepoHygieneTests.AssetDirectories_AreRemovedFromWorkspace — SHIPPED & GREEN (2026-07-25) in RepoHygieneTests.cs.
B18/B25 — Smooth-render primitive extraction
  TEST ScreenChromeTests.WriteLineSmooth_ShorterLineThanPrevious_ErasesTrailingCharacters — SHIPPED & GREEN
  TEST ScreenChromeTests.RenderFrame_ForceClearFalse_UsesCursorHomeNotFullClear — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in ScreenChromeTests.cs.
B19 — Exit-code propagation
  TEST ProgramTests.Main_CommandThrowsException_ReturnsNonZeroExitCode — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in ProgramTests.cs.
B20 — AssertSwitchCases hardening + reverse-direction test
  TEST CommandRegistryTests.AssertAllAliasesReachable_DoesNotThrow — SHIPPED & GREEN
  TEST CommandRegistryTests.AssertSwitchCases_WorksFromPublishedOutputLayout — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in CommandRegistryTests.cs.
B21 — [closed, verification-only, no test needed — already resolved by the loop-pass finding itself]
B22 — Write regression tests for B1/B2 first: satisfied by B1/B2 entries above; SHIPPED & GREEN.
B23 — InvokeCliAgent extraction
  TEST InvokeCliAgentTests.SharedHelper_ClaudeConfig_MatchesOriginalInvokeClaudeBehavior — SHIPPED & GREEN
  TEST InvokeCliAgentTests.SharedHelper_CodexConfig_MatchesOriginalInvokeCodexBehavior — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in InvokeCliAgentTests.cs.
B24 — /ai-mode-check diagnostic command
  TEST AiModeCheckTests.ExplicitCloudOverride_ReportsCloud — SHIPPED & GREEN
  TEST AiModeCheckTests.AutoMode_ReportsResolvedModeAndReason — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in AiModeCheckTests.cs.
B27 — Unify Density/IsMobileContext
  TEST ConfigTests.IsMobileContext_And_RendererDensityCheck_AlwaysAgree — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in ConfigTests.cs.
B28 — InvokeHermes arg-cleanup bug
  TEST AiClientHermesTests.InvokeHermes_NonDefaultModelArg_PreservesModelFlagAndValue — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in AiClientHermesTests.cs.
B29 — InvokeHermesDesktop missing activity log
  TEST AiClientTests.InvokeHermesDesktop_SuccessfulLaunch_WritesActivityLogEntry — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in InvokeHermesDesktopTests.cs.
B30 — ShowAiDashboard mode indicator
  TEST AiClientTests.ShowAiDashboard_ClaudeAndCodexMenuItems_DisplayedModeMatchesActualLaunchMode — SHIPPED & GREEN
  STATE: SHIPPED & GREEN (2026-07-25) in ShowAiDashboardTests.cs.
17.2 TEST SPECS FOR SECTION 12 FUTURE FEATURES (grouped by area; only concrete/buildable ones get a full spec — vaguer ones are flagged for scoping first, not force-fitted)
Accounts:
  TEST AccountConsoleViewTests.GetUsageLines_OnceWiredToRealScreen_IsInvokedFromSomeCommand — proves the dead GetUsageLines/quota-forecast UI has a real caller once wired (SECTION 12). STATE TODAY: RED (zero callers, SECTION 5).
  TEST AccountServiceTests.GetAccounts_RepeatedCallsWithinTtlWindow_HitCacheNotDisk — once the proposed TtlCache is added to the hot auto-switch path. STATE TODAY: N/A (feature not started) — write this test as the FIRST step of building the cache, per TDD, not after.
  TEST TokenVaultTests.Protect_WithAccountDerivedEntropy_ProducesDifferentCiphertextPerAccount — once the DPAPI-no-entropy migration gap (SECTION 6) is addressed. STATE TODAY: N/A, feature not started.
AI/Ollama (beyond B2/B23/B24/B28/B29/B30 above):
  TEST OllamaClientTests.PullOllamaModel_ReportsRealProgress_NotRawPassthrough — once SpectreProgress streaming is added (SECTION 12). STATE TODAY: N/A.
  TEST HttpClientProviderTests.CheckNetworkStatus_UsesSharedClient_NotOwnInstance — regression-style test for finishing the partial HttpClientProvider adoption (SECTION 6). STATE TODAY: RED once written — AgyAccountCore.CheckNetworkStatus currently makes its own HttpClient.
Learning/Study:
  TEST StudyConsoleViewTests.CsharpQuiz_CompletedSession_CallsStudySessionRecord — parity fix for CsharpQuiz (SECTION 5/12). STATE TODAY: RED.
  TEST KanaQuizTests.ReviewLoop_EscapeKeyPressed_ExitsSessionEarly — parity fix (SECTION 5). STATE TODAY: RED.
  TEST GuidedLearnFlowTests.AnsweringCardWrongThenRight_UpdatesIntervalOnDisk — the single highest-value missing test per SECTION 16.5, real filesystem-backed integration test. STATE TODAY: this flow is confirmed WORKING (SECTION 4 flow 26) but UNTESTED — write this test now even though nothing is red, specifically because SECTION 6's correction-log shows this exact flow has needed re-verification multiple times; a green test here is what stops the next audit pass from having to re-derive "is this actually fixed" from scratch.
Terminal IDE:
  TEST IdeCommandRegistryTests.All_WiredIntoTerminalIdeDispatch — see B15's per-item note; only write once wire-vs-delete is decided for this specific one.
  TEST SymbolSearchTests.RoslynBased_CSharpFile_ExtractsSymbolsCorrectly — once regex-based extraction is replaced (SECTION 12), a genuinely new capability, not a regression guard — write this test against a fixture .cs file with nested classes/methods/comments containing symbol-like text, since that's exactly the false-positive class the regex approach gets wrong (SECTION 5's earlier finding on SymbolSearch, carried from the pre-restructure audit history).
Git:
  TEST GitClientTests.Worktree_AddListRemove_RoundTrips — once worktree support is actually built (SECTION 12), not before; this is a new-feature test, not a regression guard, since the feature doesn't exist.
Docker/AWS:
  TEST DockerClientTests.HealthDashboard_AutoRefreshes_WithoutManualReinvocation — once polling replaces the one-shot snapshot (SECTION 12). STATE TODAY: N/A.
  TEST AwsClientTests.LocalStackOnlyMode_SkipsRealAwsProbe — once the toggle is added (SECTION 12). STATE TODAY: N/A.
SSH/Network:
  TEST SshConsoleViewTests.StartMobileSshKeyReceiver_WrongToken_RejectsRequest
  TEST SshConsoleViewTests.StartMobileSshKeyReceiver_CorrectToken_AppendsToAuthorizedKeys
  BLOCKED first: extract the token-check into a pure function — the current implementation is a live HttpListener with no unit-testable seam (already noted SECTION 16.5). Do the extraction as its own red/green step before either test above can be written.
Skills / Secrets (SkillLoader, AgySecretVault):
  Per SECTION 12's own framing ("wire it in or delete it") — no test should be written until that decision is made for each. Once decided: SkillLoaderTests.Discover_WiredIntoSlashCommand_IsInvokedFromIdeCommandRegistry (if wired) or nothing (if deleted, compiler proves absence) — identical pattern to B15.
Cross-cutting:
  TEST AgyDoctorTests.SelfCheck_DetectsProfileHelpTypeAcceleratorCollision — once the `agy doctor` command (SECTION 12) is built, this is its own first acceptance test, and notably it would have caught B4 directly — build this test FIRST, then build just enough of `agy doctor` to pass it, then extend to the other checks (stale DLL, missing tool, config parse) each with their own added test, one at a time.
  TEST CommandRegistryTests.EveryGroupPath_HasMatchingMenuNodeLabel — the "cc --audit" mode's second check (SECTION 12). STATE TODAY: write against current data first to see if it's already RED or GREEN — this one wasn't verified either way this session, worth checking before assuming it needs building.
===============================================================================
SECTION 18. EXHAUSTIVE EDGE CASES PER FEATURE (beyond the one-test-per-bug shape of SECTION 17 — full boundary/error/unusual-input coverage per domain)
===============================================================================
SECTION 17 gives one or two tests per confirmed bug/task. That is necessary but not sufficient — it proves the specific known bug stays fixed, not that the surrounding feature is robust. This section goes feature-by-feature and lists the full edge-case surface, grounded in the actual formulas/logic read this session, not generic filler. Where a case is already named in SECTION 17, it's cross-referenced, not repeated in full.
18.1 QUOTA CALCULATION (AgyAccountCore.CalculateRollingQuotas / GetQuotaReleaseForecast / QuotaTracker.CalculateWindowUsage — SECTION 1)
  - Empty RequestHistory (brand-new account, zero requests ever) — must return 100% remaining, not divide-by-zero/NaN. Currently unverified either way.
  - Exactly at the limit (qCount5H==50) — Remaining5H must be exactly 0, not negative (Math.Max(0.0,...) should clamp; verify the clamp is actually reached at the exact boundary, not one-off).
  - One over the limit (qCount5H==51, e.g. a stale over-count from a bug elsewhere) — must still clamp to 0, not go negative and silently "add back" quota via a sign error.
  - A request timestamp exactly AT the 5-hour or 7-day boundary (dt == fiveHoursAgo exactly to the tick) — the current check is dt >= fiveHoursAgo (inclusive); confirm this is the intended boundary, not an off-by-one that either double-counts or drops the boundary request.
  - Malformed timestamp string in RequestHistory (corrupted file, hand-edited) — DateTime.TryParse fails, current code silently skips via .Where(dt.HasValue) — confirm this doesn't also silently skip a VALID timestamp due to a format mismatch (e.g. non-roundtrip-kind format from an older app version).
  - Duplicate timestamps (two requests recorded in the same tick, or a retry that double-logged) — currently counted twice; decide and test whether that's correct (probably yes, two real requests happened) or needs dedup.
  - DST transition spanning the 5-hour window (the repeated or skipped hour during fall-back/spring-forward) — UTC timestamps should make this a non-issue; confirm RequestHistory is stored/parsed as UTC consistently everywhere it's read, not just where it's written (Clock.GetUtcNow().UtcDateTime at write time — verify every read site agrees).
  - reqsLastHour==0 while reqs5H>0 (a burst of requests happened 2-4 hours ago, none in the last hour) — exhaustion5H must stay "Never" per the existing `reqsLastHour > 0 && reqs5H > 0` guard; test this exact combination explicitly, it's a real, reachable state.
  - RequestHistory grown very large (months of unpruned history if UpdateAccountMetadata's 7-day prune never ran for some account) — performance edge case: CalculateRollingQuotas re-parses the entire list on every call with no cache; worth a perf-budget test (e.g. under 50ms for 10,000 entries) once B1's centralization lands, not before.
  - Account name containing characters invalid in a directory name — GetAccountMetadata/GetAccountDirectory resolution; decide whether this should be sanitized/rejected at account-creation time (ties to B14's AddAccount edge cases below) rather than failing later at quota-check time.
  - Once B1 wires Claude/GPT quota to ai_activity_log.jsonl: a Success=false entry (a failed AI call) — decide explicitly whether failed calls count against quota (they may still consume server-side capacity even on failure) and write the test either way, don't leave it implicit.
18.2 AI PROVIDER ROUTING (InvokeClaude/InvokeCodex/GetEffectiveProviderMode/InvokeWithPipeline — SECTION 2)
  - providerModeOverride="cloud" explicit + Ollama not installed on the machine at all — must still attempt the cloud path cleanly, never touch Ollama.
  - providerModeOverride="local" explicit + Ollama binary missing entirely — EnsureOllamaServer/InitializeOllamaServer's failure path; confirm it surfaces a clear message rather than a raw exception.
  - AiProviderMode="auto" and the IsOllamaRunning() check itself throws (permission error enumerating processes, sandboxed environment) — confirm GetEffectiveProviderMode doesn't crash the whole invocation, and confirm which mode it defaults to on that failure (currently unverified).
  - Quota-exceeded fallback threshold boundary: GeminiFiveHour exactly 98.0 (should trigger the warning) vs 97.99 (should not) — off-by-a-fraction test, cheap and currently unverified at the exact boundary.
  - Quota-exceeded fallback with Console.IsInputRedirected==true (scripted/CI invocation) — shouldFallback must resolve to true with zero blocking wait, never hang on a Confirm() prompt that can't be answered.
  - .agy-context.md exists but is empty or whitespace-only — the `!string.IsNullOrEmpty(contextText)` check should skip appending; confirm no empty --append-system-prompt argument is ever added.
  - .agy-context.md exists but File.ReadAllText throws (locked by another process, permission denied) — caught by a bare catch{}; decide whether silent-ignore is correct or whether this should warn once, and test whichever is decided.
  - last_claude_account.txt references an account that has since been deleted — the "Warning: Account changed" prompt fires against a name that no longer resolves to a real account; confirm the confirm-to-continue path still writes the CURRENT active account correctly afterward, not the stale one.
  - Two InvokeClaude/InvokeCodex calls in rapid succession (double-invocation, e.g. a fast double-keypress) — race on last_claude_account.txt write; low-likelihood but worth a note even if not immediately tested.
  - Consistency check: does InvokeCodex have the same .agy-context.md handling as InvokeClaude? Not yet verified this session — read both side by side and write a test asserting parity, since drift here would be the same "two implementations of one concept diverge" pattern already found twice elsewhere (SECTION 1's quota forecasts, SECTION 6's dead satellites).
18.3 SM-2 SPACED REPETITION (SpacedRepetitionEngine.UpdateCard — confirmed-correct math, SECTION 5/6, but coverage is incomplete)
  - CRITICAL GAP: only quality=0 and quality=5 are tested today (SECTION 16.1's inventory). Quality=1,2,3,4 are completely untested — add UpdateCard_QualityOne/Two/Three/Four_ProducesExpectedEaseAndInterval for all four, computing the expected EF via the same formula (EF + (0.1 - (5-q)*(0.08+(5-q)*0.02))) so the test is a genuine independent check, not a copy of the implementation.
  - Specifically verify quality=3 ("Good," the SM-2 spec's traditional "no significant EF change" answer) — compute what this codebase's exact formula produces at q=3 and either confirm it matches the classic SM-2 expectation (near-zero EF change) or document explicitly that this variant's q=3 behavior differs, so nobody "fixes" it later based on a wrong assumption about what it's supposed to do.
  - First-ever review of a brand-new card (Repetitions=0, EaseFactor at whatever the seed/default value is, LastReviewed unset) — confirm UpdateCard's behavior on this exact initial state, not just on an already-reviewed-once card (all three existing tests seed a state with Repetitions=1 or 3, per SECTION 16.1 — none test Repetitions=0 from true scratch).
  - Long correct streak then a miss (Repetitions=20+, quality=0) — confirm interval resets to 1 (SM-2 spec behavior) and EaseFactor drops by the formula's amount, not catastrophically or not at all.
  - IntervalDays growth over many consecutive correct reviews (years of `IntervalDays * EF` compounding) — confirm no unreasonable jump and no integer overflow at extreme values (low real-world likelihood, cheap to test, worth a boundary check given (int)Math.Round has no explicit cap per SECTION 5).
  - Quality out of the 0-5 spec range (SECTION 16.3 already flags no clamping exists) — UpdateCard_QualityNegativeOne_And_QualityTen — decide whether these should throw, clamp, or are genuinely never reachable from any UI caller (worth confirming call-site guarantees rather than assuming); if reachable, this is a real bug to add to SECTION 5, not just a coverage gap.
  - LastReviewed timestamp in the future (clock skew, bad data import, or a card whose date got corrupted) — confirm IsDueToday()'s behavior doesn't produce a negative "days until due" that wraps into "due now" incorrectly, or vice versa never becomes due.
18.4 ACCOUNT MANAGEMENT (AddAccount/DeleteAccount/LogoutAccount/SetActiveAccount, AgyAccountCore)
  - Delete the currently-active account (regression-tested already, SECTION 1's original resurrection-bug fix) vs delete a NON-active account (different code path, not yet explicitly covered — confirm no active-marker rewrite happens when it shouldn't).
  - Delete an account, then immediately AddAccount with the exact same name — confirm no stale data (old RequestHistory, old tokens, old QuotaStatus) leaks into what should be a fresh account; this is a real "reuse" edge case a personal multi-account workflow would actually hit.
  - SetActiveAccount targeting a name that was never added (typo, stale reference from an old script/alias) — confirm a clean error/no-op rather than creating a phantom account directory implicitly.
  - Concurrent account switch from two terminal sessions open at once (a realistic scenario for this exact personal-dev-tooling use case) — race on active_account.txt; at minimum document the expected "last writer wins" behavior and confirm it doesn't corrupt the file (partial write) under concurrent access.
  - LogoutAccount on an account with no stored credential yet (added but never logged in) — must be a clean no-op, not throw looking for a keyring entry that doesn't exist.
  - AddAccount with a name colliding with the reserved "default" account, or containing characters invalid in a Windows directory name — decide validation behavior at creation time (ties to 18.1's quota-side note about invalid account names surfacing later instead).
  - First-ever run on a machine with no .gemini/AgySourceHome directory structure at all yet — confirm the whole account subsystem bootstraps cleanly rather than assuming the directory tree pre-exists.
18.5 CONFIG SAVE/LOAD (Config.cs — 2 existing regression tests for the Mode-collision bug, SECTION 16.1)
  - Save() when profile.config.json does not exist yet at all (very first run) — must create the file cleanly, not throw on a missing target.
  - Save() where a field's value itself contains the literal substring the line-match regex looks for (e.g. a string setting whose value happens to contain the text "Mode":) — could cause the line-scan to misfire on the wrong line; low-likelihood but a real class of the same bug shape.
  - Concurrent Save() calls (main TUI thread + a background widget refresh both saving around the same time) — confirm no interleaved/corrupted write; if there's no locking today, this is worth surfacing as a finding even before writing the test, since a plain text-based rewrite with no lock is a realistic corruption path in exactly this kind of TUI-with-live-widgets app.
  - Load() against a hand-edited profile.config.json with a trailing comma or other near-valid JSON mistake — confirm a clean error message at startup, not a raw parser exception dump that looks like a crash.
  - Load() against a zero-byte file (truncated by a crash mid-write, ties directly to the concurrent-save case above) — same question.
18.6 COMMANDROUTER / COMMANDREGISTRY DISPATCH
  - Alias case sensitivity — "Claude" vs "claude": confirm whether lookup is case-sensitive and whether that's the intended design (undocumented either way today).
  - Alias prefix collision — "claude" vs "claude-cloud" vs "claude-ollama": confirm exact-match dispatch only, never a prefix/fuzzy match at the CommandRouter level (fuzzy matching belongs to the `/` search UI, not the direct-invocation path).
  - Empty-string or whitespace-only alias input.
  - A disabled command (RequiresAiOllama/RequiresAgy gate closed) — confirm literally zero side effects occur before the gate blocks it, not just "the screen doesn't render" — e.g. confirm no file write, no process launch, sneaks through before the check (extends the already-known Clear()-before-gate-check ordering bug, SECTION 5, into "what ELSE might run before the gate").
18.7 UI NAVIGATION (ScrollableListView, FlatTreeRenderer, ThreePaneRenderer, SubPage*Navigator)
  - Zero items (empty category/search result) — partially covered by the existing (flagged-false-coverage) FlatTreeRendererTests case; needs a real version per SECTION 17's B10 note.
  - Exactly one item — the boundary between "never needs paging" and "the paging logic's edge."
  - maxVisibleRows <= 0 passed to ComputeViewport — SECTION 16.3/5 already found this silently falls back to 10 rather than surfacing the caller bug that produced a non-positive value; test the CURRENT behavior explicitly (documents it), then decide if it should change.
  - Selection at index 0, press Up — confirm wrap-to-bottom vs clamp-at-0, whichever is intended, and that it's consistent between FlatTreeRenderer and ThreePaneRenderer once B11 lands (today it can't be, since ThreePaneRenderer has no paging at all).
  - Selection at the last index, press Down — same, other direction.
  - Terminal resized to near-zero width/height mid-session — confirm no crash, even if the resulting display is degraded/unreadable.
  - Rapid repeated keypresses arriving faster than a render cycle completes — confirm selection index math stays internally consistent (no skipped clamp check) even under a burst of input, relevant given several of these methods aren't thread-safe by design (single input loop assumption) but the assumption itself has never been stress-tested.
18.8 GITCLIENT / GITDIFFVIEWER
  - Path with a space — already the B8 regression test.
  - Path containing non-ASCII/unicode characters (a filename with accented characters or CJK characters, common enough in real repos).
  - Diffing a file that was just deleted (exists in the diff's "before" state but not on disk anymore).
  - Diffing inside a repository with zero commits yet (a freshly-initialized repo).
  - Diffing a path that resolves to a submodule boundary.
  - A file that is binary/non-UTF8 (accidentally diffing an image or compiled binary) — confirm ColorizeHunk doesn't choke or produce garbage markup on non-text content.
  - git executable not on PATH at all — confirm a clean "git not found" message, not a raw Win32 exception.
18.9 DOCKERCLIENT
  - Docker daemon not running at all (distinct from "daemon running, zero containers") — confirm the health dashboard/cleanup commands distinguish these two states in their messaging, not conflate them.
  - Zero containers, zero images (a genuinely fresh Docker install).
  - Container/image name containing characters that need shell-escaping if ever passed through a string-concatenation path (ties to the already-found inconsistent-argument-style finding in an earlier deep-dive pass of this codebase).
  - A very large number of containers/images (100+) — confirm the dashboard's rendering doesn't become unusably slow or overflow the display without paging.
18.10 AWSCLIENT
  - No AWS credentials configured anywhere (no profile, no env vars, no IMDS role) — confirm the "not configured" message is accurate and distinct from a LocalStack-down message.
  - Both real AWS AND LocalStack fail (e.g. offline machine, LocalStack not started) — confirm the user sees which of the two was actually tried and why both failed, not just a generic empty result (ties to the already-known "doesn't indicate which backend answered" finding).
  - A configured region that doesn't match where LocalStack is actually listening.
18.11 TERMINALIDE / SYMBOLSEARCH / CODEVIEWER
  - Zero-byte file opened in the viewer.
  - A file with a byte-order-mark (BOM) at the start — confirm line-based parsing/symbol extraction isn't off by the BOM's bytes.
  - An extremely long single line (minified JS/CSS-style content with no newlines) — confirm the pager doesn't hang or render pathologically slowly.
  - A symlink or junction inside the browsed directory tree — given the already-known false-positive "Needs Repair" junction-detection bug elsewhere in the account domain (SECTION 5), confirm file browsing doesn't have an analogous confusion between real files and junction targets.
  - A very deeply nested directory structure (accidental infinite-loop-prone symlink cycle, or just unusually deep real nesting) — confirm no unbounded recursion.
18.12 SSH / MOBILE KEY ENROLLMENT
  - Token expires at the exact moment a request arrives (boundary race).
  - The same valid token submitted twice (replay within the window) — decide and test explicitly whether enrollment is single-use or multi-use per token; not currently documented either way.
  - A malformed SSH key format submitted (partially covered already by the existing regex-format validation finding) — confirm the rejection message is informative, not just a silent drop.
  - An unusually large POST body (not necessarily malicious, e.g. a paste that accidentally included extra content) — confirm there's a reasonable size limit rather than unbounded read.
18.13 OBSIDIANCLIENT / RESOURCEEXTRACTOR
  - Configured vault path no longer exists (deleted or moved since it was configured) — confirm a clean "vault not found" message, not a crash deep in a scan routine.
  - A note with malformed/unclosed YAML frontmatter (missing closing ---) — confirm the parser stops gracefully rather than treating the rest of the file as frontmatter.
  - Circular wikilinks (note A links to B links back to A) — confirm the graph-building logic terminates rather than looping.
  - A very large vault (thousands of notes) — direct performance test now that no caching exists (SECTION 5) — pick a concrete threshold (e.g. under 2 seconds for 5,000 notes) so "too slow" has a number attached, not just a vague complaint.
18.14 POWERSHELL PROFILE STARTUP
  - Profile accidentally dot-sourced twice in one session — confirm the $global:AgyProfileLoaded guard actually short-circuits the second load cleanly, including for any code added after B4's ProfileHelp fix.
  - Running in a non-elevated shell when a command that assumes elevation is invoked (optimize_profile_admin.ps1 territory, but also worth checking any in-session command that assumes write access to a protected path).
  - The AgyTui.dll load path when the host PowerShell process is NOT running on a compatible .NET runtime version — confirm the failure message names the actual mismatch rather than a generic "type not found."
  - PATH environment variable edge case: a required external tool (git/docker/aws/ollama) resolvable via a full path in Config but NOT on PATH — confirm FindOnPath's callers fall back correctly rather than assuming PATH always has everything.
===============================================================================
SECTION 19. TASK BREAKDOWN — SUB-STEPS FOR THE CRITICAL/HIGH PRIORITY TABLE ROWS (1-11)
===============================================================================
Each numbered task below breaks its priority-table row into ordered sub-steps. Do not skip a step or reorder within a task — each one is sequenced (extract-seam before test, test before fix, fix before cleanup) per SECTION 17's TDD discipline. Lower-priority tasks (rows 12-30) stay single-line in SECTION 14 until they're promoted to CRITICAL/HIGH by a future pass; break them down at that point, not preemptively.
19.1 B2 + B22 — Fix claude/codex auto-mode Ollama reroute
  1. Extract IsOllamaRunning() behind an injectable seam (interface, delegate field, or a static Func<bool> that tests can swap) — this is its own tiny commit, verify nothing else changes behavior.
  2. Write AiClientTests.InvokeClaude_NoProviderOverride_ResolvesToCloud_RegardlessOfOllamaRunning and the codex equivalent (SECTION 17 B2) against the new seam. Confirm both are RED.
  3. Change CommandRouter.cs's case "claude" to InvokeClaude([], "cloud") and case "codex" to InvokeCodex([], "cloud").
  4. Confirm both tests from step 2 go GREEN. Confirm no other existing test regresses (particularly nothing in ShowAiDashboard's path, which is intentionally NOT touched by this task — see the table's scope note).
  5. Update SECTION 2/5/14 status to DONE with the commit reference.
19.2 B1 — Quota centralization
  1. DECISION FIRST (blocks everything else): determine whether Claude/GPT have a real, trackable quota concept at all, or whether the "CLAUDE AND GPT MODELS" section of GetUsageLines was aspirational and should be deleted. This requires knowledge outside the codebase (the actual Claude/Codex CLI subscription model) — resolve this with the user before writing any code, per SECTION 1 fix plan step 1.
  2a. IF real quota exists: extend QuotaMetrics/AccountStats with a provider dimension; write a parser for ai_activity_log.jsonl filtered by Agent; write QuotaCentralizationTests.CalculateRollingQuotas_ClaudeProvider_ReturnsRealDataNotHardcoded100 (SECTION 17) against the new parser, confirm RED, then wire GetUsageLines to call it instead of the two hardcoded 100.0 literals.
  2b. IF fake/aspirational: delete the "CLAUDE AND GPT MODELS" section from GetUsageLines entirely; write a test asserting GetUsageLines's output no longer contains that section, confirm RED against current output, then remove it.
  3. Either way: unify the 5-hour and weekly window calculations to both route through QuotaTracker.CalculateWindowUsage (write QuotaCentralizationTests.CalculateRollingQuotas_5HourAndWeekly_UseSameCodePath first, confirm RED, then refactor the weekly branch).
  4. Delete AgyAccountCore.GetQuotaReleaseForecast, make QuotaTracker.ForecastQuotaRelease the sole implementation — write the unification test first (SECTION 17), confirm RED, reconcile the two methods' differing return shapes, then delete the duplicate.
  5. Re-run the full 18.1 edge-case list (boundary at limit, malformed timestamps, empty history, etc.) as new tests once the centralized implementation exists — this is the point where SECTION 18.1's list stops being speculative and becomes a real backlog against real code.
19.3 B4 — ProfileHelp collision
  1. Write the Pester test from SECTION 17 (ProfileHelp resolves to the AgyTui type accelerator) against the CURRENT profile — confirm it fails with the actual reproduction error, capturing the real failure mode.
  2. Rename the locally-defined class inside Load-HelpHelper's Invoke-Expression block to something that cannot collide (e.g. LocalProfileHelp), matching the ed61b11 pattern already used for GitHelper/DotNetHelper/DockerHelper/AwsHelper/SystemHelper -> Shell* renames.
  3. Confirm the test goes GREEN. Grep the whole profile for any other local class name that might collide with an AgyTui.* type accelerator (this collision class could exist elsewhere and hasn't been checked file-wide) — if found, add one test per instance before considering this task done.
19.4 B5 — CI paths
  1. Fix .github/workflows/ci.yml's dotnet-build job to target csapp/AgyTui/AgyTui.csproj.
  2. Fix the PowerShell test job to target psapp/Tests/run_tests.ps1.
  3. Add a dotnet test step against csapp/AgyTui.Tests/AgyTui.Tests.csproj, gating the job on its result.
  4. Push a deliberately-failing canary test, confirm the Actions run actually goes red (proves step 3 is real, not a silent no-op) — this is the acceptance test for this whole task, since CI config correctness isn't a unit test in itself.
  5. Remove the canary, confirm a clean run goes green.
  6. Mark branch protection requiring this job as a follow-up repo setting (not a file change — separate from this task, note it but don't block on it).
19.5 B3 — Antigravity Deck/Manager self-kill
  1. Write ProcessRunnerTests.RunInteractive_LongRunningProcess_DoesNotTimeOut (or the AntigravityDeckClient-specific version, SECTION 17) against a fixture long-running script. Confirm it currently fails when run through ProcessRunner.Run (the bounded method) to prove the bug reproduces on demand.
  2. Change AntigravityDeckClient.RunNpmCommand (and the equivalent in AntigravityManagerClient) from ProcessRunner.Run to ProcessRunner.RunInteractive.
  3. Confirm the test goes GREEN. Manually verify (per this project's own "verify" skill discipline) that deck-start/deck-online actually keep a real dev server alive past 30 seconds in a live run, not just in the test fixture.
19.6 B7 — SubPageTopicNavigator desync
  1. Extract the two topic lists (Render's long-form descriptions, HandleSelection's short codes) into one shared (Code, Description) source both methods read — this is the prerequisite seam.
  2. Write SubPageTopicNavigatorTests.Render_And_HandleSelection_FilterSameSourceList_ForIdenticalSearchBuffer (SECTION 17) against the new shared source. Confirm GREEN immediately if the extraction was done correctly (the test's purpose here is proving the extraction removed the desync possibility structurally, not catching a remaining bug).
  3. Manually verify in a live run: type a partial topic name in the picker, confirm the highlighted item and the item actually launched match.
19.7 B8 — GitDiffViewer unquoted path
  1. Write GitDiffViewerTests.ShowDiff_PathContainingSpace_DoesNotBreakGitInvocation (SECTION 17) using a temp fixture repo with a space in its path. Confirm RED against current code.
  2. Switch GitDiffViewer.ShowDiff's git invocation from string-interpolated args to the ArgumentList-based ProcessRunner overload (matching the pattern ConventionalCommitWizard already uses correctly elsewhere in the codebase, per an earlier finding).
  3. Confirm GREEN. Also capture and surface stderr (currently discarded) so a real git error shows instead of a generic "no diff" message — add a second test for this (GitDiffViewerTests.ShowDiff_GitNotARepo_ShowsRealErrorNotGenericMessage) since it's a related but distinct fix in the same method.
19.8 B6 — publish_release.ps1 broken paths
  1. Write the Pester test from SECTION 17 (every referenced project path exists on disk). Confirm RED.
  2. Update the script's csproj/test-project paths from csapp/AgyTuiApp/* to csapp/AgyTui/* and csapp/AgyTuiApp.Tests/* to csapp/AgyTui.Tests/*.
  3. Confirm GREEN. Give the script a Set-Alias entry in the profile (currently invisible from the shell, noted in an earlier finding) as a small bonus fix in the same change, since it's the same file area.
19.9 B14 — Menu reorder + Favorites group
  1. Write CommandRegistryTests.Categories_SortOrder_MatchesProposedSequence (SECTION 17) asserting the SECTION 9 sequence. Confirm RED against current default order.
  2. Update the 8 categories' SortOrder values in CommandRegistry.cs to match SECTION 9's proposed sequence.
  3. Confirm the sequence test GREEN.
  4. Write ConfigTests.FavoriteAliases_DefaultsToEmpty_AndIsToggleable. Confirm RED (field doesn't exist).
  5. Add FavoriteAliases: string[] to Config's Ui section, default empty; add a /favorite <alias> toggle command; wire a pinned "[Favorites]" MenuNode group that reads from it, reusing the existing leaf-node type (no new UI primitive needed).
  6. Confirm GREEN. Manually verify in a live run: toggle a favorite, confirm it appears pinned above the 8 categories on next launch.
===============================================================================
SECTION 14. TASK LIST (priority-ordered, each independently shippable, numbered for reference)
===============================================================================
B1  Fix quota centralization (SECTION 1) — decide fake-vs-real Claude/GPT quota first, then route both providers through one QuotaTracker call, delete the duplicate forecast implementation.        CRITICAL, user-reported this session
B2  Fix claude/ollama coupling (SECTION 2), Option A: pass "cloud" explicitly in CommandRouter's case "claude" AND case "codex" (both confirmed affected, same 1-line fix shape, same file).           CRITICAL, user-reported this session
B3  Fix Antigravity Deck/Manager 30s self-kill bug (Run -> RunInteractive).                                                                                                                          CRITICAL, blocks a whole feature
B4  Fix ProfileHelp type-accelerator name collision (confirmed crash).                                                                                                                                CRITICAL
B5  Fix CI paths, add a dotnet test step.                                                                                                                                                             CRITICAL, no regression safety net exists otherwise
B6  Fix publish_release.ps1's broken pre-rename paths.                                                                                                                                                HIGH
B7  Fix SubPageTopicNavigator's live selection-desync bug.                                                                                                                                            HIGH, silently picks the wrong topic
B8  Fix GitDiffViewer's unquoted-path bug.                                                                                                                                                            HIGH, breaks on common Windows paths
B9  Fix CommandPalette's Escape-treated-as-"All" bug.                                                                                                                                                 MEDIUM
B10 Fix emoji-width border misalignment.                                                                                                                                                              MEDIUM
B11 ThreePaneRenderer paging/viewport parity via MenuRendererBase.                                                                                                                                    MEDIUM
B12 Fix vim h/l key bug in SubPageNavigator.                                                                                                                                                          MEDIUM
B13 Untangle the circular UI<->Infrastructure account dependency (SECTION 6).                                                                                                                         MEDIUM, structural, do carefully
B14 Reorder the 8 category SortOrder values + add Favorites group (SECTION 9).                                                                                                                        HIGH, low effort, directly requested
B15 Wire or delete each confirmed-dead code path (SECTION 5/6 dead-code list).                                                                                                                        MEDIUM
B16 CommandInvocationLog middleware (SECTION 10).                                                                                                                                                     MEDIUM, unblocks better answers to B15-style questions going forward
B17 Remove/relocate the 86MB of orphaned personal assets.                                                                                                                                             LOW, easy
B18 Smooth-render adoption for AlgoVisualizer/InterviewBank.MockInterviewTimer.                                                                                                                       LOW
B19 Add exit-code propagation to Program.Main/CommandRouter.Execute.                                                                                                                                  LOW
B20 Move AssertSwitchCases-style checks from Main into a unit test.                                                                                                                                   LOW
B21 [DONE — this loop pass] Re-verify codex/openclaw/hermes for the SECTION 2-shaped auto-mode bug. Result: codex affected (folded into B2 scope), openclaw/hermes not affected. No further action item beyond B2.
B22 Write regression tests for B1 and B2 FIRST, before/alongside fixing them (SECTION 13 item 7) — this project's history shows unguarded fixes get silently re-broken.                              HIGH
B23 Extract InvokeCliAgent to de-duplicate InvokeClaude/InvokeCodex (SECTION 12 loop-pass addition, SECTION 13 item 8).                                                                               MEDIUM, do after B2 lands so there's only one behavior to preserve while refactoring
B24 Build `/ai-mode-check <alias>` diagnostic command (SECTION 12 loop-pass addition).                                                                                                                 LOW-MEDIUM, nice-to-have, pairs with B16
B25 Extract ScreenChrome.WriteLineSmooth + RenderFrame primitives, migrate AlgoVisualizer/InterviewBank.MockInterviewTimer onto them (SECTION 15.1).                                                  MEDIUM
B26 Fix emoji-width border bug via an explicit glyph-width lookup table in Icons.cs (SECTION 15.2, duplicate of B10, kept here for cross-reference).                                                  MEDIUM
B27 Unify Density/IsMobileContext into one signal, un-kill ScreenChrome's winWidth<60 dead check, make Density responsive to live terminal resize instead of relaunch-only (SECTION 15.4).             MEDIUM
B28 Fix InvokeHermes's arg-cleanup loop dropping non-default --model values (SECTION 2 loop-pass update 2, SECTION 5).                                                                                MEDIUM, real bug, corrupts Hermes CLI args
B29 Route InvokeHermesDesktop through InvokeWithPipeline for consistent activity logging (SECTION 2 loop-pass update 2).                                                                              LOW, small, mechanical
B30 Add mode indicator to each ShowAiDashboard agent menu item instead of changing its routing behavior (SECTION 2 loop-pass update 2, SECTION 12).                                                   LOW-MEDIUM, closes the dashboard's visibility gap without touching B2's fix
SUGGESTED ORDER: B2 (1-line fix x2 now — claude AND codex, user-reported, do immediately) -> B1 (needs a design decision first, see SECTION 1 fix plan step 1) -> B4/B5/B3 (crash, broken safety net, fully-broken feature) -> B7/B8 (live correctness bugs) -> B22 -> B23 -> B14 -> rest in any order.
===============================================================================
END OF FILE
===============================================================================
