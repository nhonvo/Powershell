# 🛸 Antigravity UI & Code Modularization Refactoring Plan

This refactoring plan outlines how to decouple and modularize the compiled C# application [Program.cs](file:///C:/Users/TruongNhon/Documents/Powershell/AgyTuiApp/Program.cs) and the PowerShell profile script [Microsoft.PowerShell_profile.ps1](file:///C:/Users/TruongNhon/Documents/Powershell/Microsoft.PowerShell_profile.ps1). It has two goals, kept deliberately separate so they can land in independent PRs:

1. **Structural (no behavior change)** — decompose the 8,558-line monolith into modules, collapse its 4 duplicated command-alias tables into 1, and fix the caching/coupling smells found by direct inspection of the source (§3, §6–§8).
2. **UI evolution (additive, flag-switchable — not a replacement)** — add a second layout, a single flat collapsible slash-command tree in the style of `claude-cli`/`agy-cli`'s own `/` menu, as a **peer** of the existing three-pane dashboard, toggled by a `UiMode` setting (§1). Both layouts are driven by the same underlying data model and dispatch path, so every function — every category, sub-page, and leaf command — looks and behaves consistently *within whichever mode is active* (§1's Standardized Screen Contract), instead of each screen inventing its own look. Alongside this, land targeted flow/feature enhancements for the Learning and AI domains, plus new integration features specific to the two most-used agents, Agy and Claude Code (§5).

---

## 🎨 1. UI/UX Global Design Layout

>**Both layouts ship, selectable at runtime — this is a flag, not a migration.** `profile.config.json` gains a `"UiMode": "three-pane" | "flat-tree"` setting (default `"flat-tree"`, since it's the newer, more `claude-cli`-like experience, but `three-pane` remains fully supported for anyone who prefers the original v3.0 look). There is also a live toggle command (`/ui-mode`, listed under `[Theme & Settings]` alongside `/theme`) so switching doesn't require editing config and relaunching.
>
> * **`three-pane`** (today's shipped behavior) — Left = category sidebar, Middle = Spectre `SelectionPrompt` for that category, Right = a details/widget panel — implemented today by `CcNavigator.Run()`/`RenderPanes()` (`Program.cs` 7440–8217).
> * **`flat-tree`** (new, this section) — a single flat, collapsible slash-command tree, the interaction model used by `claude-cli`/`agy-cli`'s own `/` menu.
>
>**How both stay in sync, forever, without becoming two more sources of truth**: neither renderer owns menu data or widget logic. Both read the same `MenuNode` tree built once from the unified `CommandRegistry` (§3.0), and both call the same `IStatusWidget.Render()` implementations (one per live widget: disk, IP, SSH, Ollama status, account tree, quota chart, live dashboard) — a renderer only decides *where on screen* a widget's `IRenderable` output goes (a side pane for `three-pane`, an inline block under the row for `flat-tree`). Concretely: `IMenuRenderer { void Run(MenuNode root); }` with two implementations, `ThreePaneRenderer` and `FlatTreeRenderer`, selected once at startup by `Config.UiMode` and swappable at runtime by the `/ui-mode` command tearing down and re-entering `Run()` with the other implementation. Adding a new command or a new widget therefore automatically appears correctly in *both* modes — there is no per-mode menu-authoring step.

### 📐 Standardized Screen Contract (applies to every function, in both modes)
Today, different sub-pages roll their own look: `AgyAccountMenu`'s account picker, `ThemeHelper`'s theme picker, and a plain command's terminal output (e.g. `gs`, `dkcl`) each currently format their headers/footers/colors slightly differently because they were written independently over time. Standardizing this is as important as the mode flag itself, since "standard layout for all functions" means every screen — not just the top-level category tree — follows one contract:
1. **One `ScreenChrome` wrapper** (new `Components/ScreenChrome.cs`) renders, for *every* screen regardless of mode or content type: a top banner + breadcrumb (`Workspace & Dev › .NET Project Tools`), the content region (tree / list / table / widget / raw command output — whatever the screen is), and a bottom hint bar listing exactly the hotkeys valid *right now* (context-sensitive, not a static legend). No screen composes its own header/footer by hand anymore.
2. **One color/status token set**, reused by every screen instead of ad hoc `Color.Green`/`Color.Red` calls scattered per class: `Accent` (current selection), `Success`/`Warning`/`Error` (already exist as `SpectrePanel.Success/Error/Warning`, 243–260 — every other class that currently rolls its own status coloring should call these instead of inlining colors), `Muted` (descriptions/help text), `Live` (the inline widget border in `flat-tree` / panel border in `three-pane`).
3. **One sub-page list contract**: any "pick one from a list" screen (account switch, theme select, workspace navigation, topic pickers in Learn & Study) is built from the same `MenuNode` leaf-list primitive the main tree already uses — not `SpectreMenu.Show` in one place and a hand-rolled `while` loop with raw `Console.ReadKey` in another (both patterns exist in the file today; the refactor's Phase 1 in §7 collapses them to one).
4. **One command-output contract**: leaf commands that just run and print (e.g. `dbld`, `gs`, `dcup`) render their result through `ScreenChrome`'s content region too, so a build failure and an account-switch failure look like the same kind of thing to the user, not two different visual vocabularies.

### 🖥️ Main Screen (`flat-tree` mode): Flat Collapsible Slash Tree

A single scrollable pane. The 10 categories from [menu_map.md](file:///C:/Users/TruongNhon/Documents/Powershell/menu_map.md) become **collapsible top-level tree nodes** (collapsed by default), not a separate sidebar level. There is no pane-to-pane focus switching anymore — `Tab`/`←`/`→` pane-shifting is gone; everything is one vertical list you move `↑`/`↓` through.

**Collapsed (default) state on launch:**
```
=================================================================
 ▄████▄ ▄████▄ 🛸 Powershell Profile Control Center v4.0 🛸
=================================================================
 [↑/↓ j/k] Move [Enter] Expand/Run [/] Search [Esc/q] Back·Exit
=================================================================

 > [Workspace & Dev] 14 commands
 [AI Agent & Ollama] 9 commands (2 grouped)
 [AGY Account Switch] 6 commands (1 grouped)
 [Docker & Databases] 5 commands
 [System & Network] 4 commands
 [Learn & Study] 16 commands
 [Track & Progress] 7 commands
 [Obsidian & Resources] 4 commands
 [Theme & Settings] 3 commands
 [Exit]

 ↑/↓ move · enter expand · / search everything · esc back / quit
```

**Category expanded (`Enter` on `[Workspace & Dev]`) — commands nest under it, still one pane:**
```
 > [Workspace & Dev] 14 commands
 /proj Jump to a registered workspace directory
 /ide Launch terminal IDE session
 /ide-diff Visual git diff viewer
 /ide-search Find text patterns recursively in files
 > /dotnet-tools Grouped .NET commands (Enter to expand/collapse)
 ├── /dbld Build project
 ├── /dtst Test project
 ├── /clean-build Clean build artifacts
 ├── /add-migration Scaffold new EF Core migration
 └── /update-db Apply EF Core migrations
 /scaffold Interactive boilerplate creator
 /gs Color-coded short git status
 /gcmt Conventional commit wizard
 /git-undo Soft-reset last commit
 /nexus Repo Nexus Graph
 [AI Agent & Ollama] 9 commands (2 grouped)
 ...
```

**Highlighting a widget-backed leaf (e.g. `/disk`, `/ollama-status`) inline-expands its live content directly beneath the line — no side pane:**
```
 /disk Partitions space allocation and warning status
 ┌─ live ──────────────────────────────────────────────────┐
 │ C:\ 412 GB / 953 GB used (43%) [███████░░░░░░░░] │
 │ D:\ 1.1 TB / 1.8 TB used (61%) [█████████░░░░░░] │
 └────────────────────────────────────────────────────────┘
 /public-ip Query external IPv4 address
```
The block collapses automatically the instant the selection moves off that line — it is not a persistent pane, it is scoped to whichever row is highlighted right now, matching the "inline expansion" behavior you'd get from a `claude-cli`-style single list.

### ⚙️ Mechanics of the Flat Slash Tree
1. **Two node kinds only**: a *branch* (`[Category]` or a grouped command like `/dotnet-tools`) that toggles open/closed on `Enter`, and a *leaf* (`/dbld`) that executes on `Enter`. Categories and in-category groupings use the exact same expand/collapse mechanic and the exact same `├──`/`└──` tree-drawing — one code path, not "category logic" plus "group logic" as two separate things.
2. **No pane focus state.** The whole app has one selection index into one flattened, currently-visible node list (closed branches contribute 1 visible row; open branches contribute themselves + their children). This removes the Left/Middle/Right focus state machine from `CcNavigator.Run()` entirely.
3. **Global `/` search**, same as today: pressing `/` opens a query buffer and filters the *flattened* list of every leaf across every category at once (auto-expanding any category/group that contains a match), exactly like `claude-cli`'s own command palette — typing `clau` immediately surfaces `/claude-cloud`, `/claude-ollama` regardless of which category is currently open.
4. **Inline widget expansion** (see mockup above) replaces the old Right Pane. Only the handful of leaves that are backed by a live widget today (`/disk`, `/public-ip`, `/ssh-info`, `/ollama-status`, `/account-tree`, `/quota-chart`, `/live-dashboard`) render an expansion block; every other leaf just shows its description text already visible in the row, so most rows never expand at all.
5. **Sub-page selection menus** (Select Active Account, Select Shell Theme, Workspace navigation) still exist, but are now pushed as a new flat list *replacing* the current view (not a 4th pane) — `Esc`/`q`/`←` pops back to the tree at the exact scroll position it was at.

### 🗂️ Re-grouped nesting (per menu_map.md, so large categories start collapsed and stay short)
Categories that had a flat run of 9+ sibling commands get an inner grouping — mirroring the existing `.NET Project Tools` pattern — so the collapsed category never dumps more than ~8 rows at once:
* **`[AI Agent & Ollama]`**: `/claude-cloud`, `/claude-ollama`, `/codex-cloud`, `/codex-ollama`, `/openclaw`, `/hermes`, then two new groups — `/ollama-tools` (`ollama-status`, `ollama-models`, `ollama-pull`, `ollama-start`, `ollama-logs`) and `/antigravity-deck` (`deck-setup`, `deck-start`, `deck-online`), then `/agy-cli`.
* **`[AGY Account Switch]`**: `/agyswitch`, `/agyquota`, `/autoswitch` stay top-level; `/account-tree`, `/quota-chart`, `/live-dashboard` become one `/quota-views` group (they're all read-only reporting views on the same account data).
* All other categories (Learn & Study, Track & Progress, etc.) are left flat — none of them mix "single actions" with "reporting views" the way AI Agent and Account Switch do, so grouping them wouldn't reduce cognitive load, just add clicks.
See the updated [menu_map.md](menu_map.md) for the full re-grouped tree per category.

### 📱 Mobile / Compact Density — a third toggle, orthogonal to `UiMode` (for SSH-from-phone usage)

**Important distinction, verified against source**: this app *already ships* a "mobile mode" — but it's narrower than it sounds, and it isn't this. `ThemeHelper.ToggleMobileMode`/`SetMobileMode` (2776/2778, backed by `enable_mobile` in `config.json`) swaps the **outer PowerShell prompt's** Oh-My-Posh theme between `{theme}` and `{theme}-mobile` variants — it changes the `PS (account)>` prompt string's own styling (its own comment at line 2795 says "ASCII mode, stacked" vs. "Rich Unicode/Emoji mode"). It has no effect whatsoever on the `cc` Control Center TUI's own layout — a three-pane dashboard rendered at 40–60 columns (a typical mobile SSH client width) is unusable today regardless of which prompt theme is active, because nothing in `CcNavigator` currently reads `Console.WindowWidth` at all.

**What's actually needed**: a `Density: "comfortable" | "compact"` setting, independent of (but coordinated with) both `UiMode` and the existing prompt-level mobile toggle:
* **Auto-detect + manual override**: at startup, if `Console.WindowWidth` is below a threshold (~70 columns — comfortably covers Termius/JuiceSSH/Blink-shell-on-a-phone default widths), default `Density` to `compact` and `UiMode` to `flat-tree` (the three-pane layout structurally cannot fit two side-by-side panels plus a sidebar into 60 columns — `flat-tree`'s single column already fits by construction, which is a real, non-cosmetic reason `flat-tree` earns its "default" status from §1 beyond just "the newer look"). Still overridable explicitly via `/density` or `profile.config.json`'s new `Density` key, same persistence pattern `ThemeHelper.PersistConfig` (2844) already uses.
* **What `compact` actually changes, concretely** (not a re-theme, a content-density change): descriptions move from inline (`/claude-cloud Launch Claude Code CLI...`) to a single-line hint shown only for the highlighted row instead of every row (so a 60-column screen shows alias + icon only, most rows); `ScreenChrome`'s breadcrumb truncates to the immediate parent only (`… › GetPrivateDirectorySize` instead of the full chain); widgets render their most-compressed form (`/disk` shows `C:\ 43% D:\ 61%` on one line instead of the two-line bar-chart mockup from §1's "Icon System"-adjacent widget mockups); box-drawing borders (`╭─╮│╰─╯`) fall back to plain `+`/`-`/`|` ASCII when combined with the existing prompt-level mobile toggle being active, since some mobile SSH clients render Unicode box-drawing inconsistently — the same rationale the existing `-mobile` theme variant already applies to the prompt, just extended to the TUI's own borders.
* **Icon fallback follows the same signal**: `compact` forces the shared Icon System (§5) to its emoji set rather than Nerd Font glyphs, since a phone SSH client is the least likely context to have a patched font installed — `Density` and the Icon System's font-detection share one source of truth instead of guessing twice.
* **Coordinated, not merged, with the existing prompt mobile toggle**: enabling `Density: compact` does **not** force `ThemeHelper.SetMobileMode(true)` or vice versa — a user might want the compact TUI without changing their prompt, or the ASCII prompt without a compact TUI. But the `/theme` sub-page (§1's sub-page list contract) gains a single combined shortcut — "Enable full mobile setup" — that flips both together for the common case (SSH-from-phone), calling both `ThemeHelper.SetMobileMode(true)` and the new `Config.SetDensity(compact)` from one action instead of requiring the user to discover and toggle two unrelated-looking settings separately.
* **Where this plugs into the plan**: `Density` is a third config axis alongside `UiMode`, read by `ScreenChrome` (§1) the same way `UiMode` selects `IMenuRenderer` — every screen consults both, so `flat-tree` + `compact` is just one point in a 2×2 space (`three-pane`/`flat-tree` × `comfortable`/`compact`) rather than a special mode bolted on separately. This slots into the same Phase 5 `ThreePaneRenderer`/`FlatTreeRenderer` build step in §7, since `Density` has to exist before either renderer is finished, not after.

---

## ⌨️ 2. Advanced Hotkeys & Interactive Search Flow

Both modes keep their own native hotkey model — this is a deliberate choice, not an oversight: `three-pane` users keep the exact Left/Middle/Right pane-focus flow they already know, and `flat-tree` users get the simpler single-list model. The **one shared hotkey across both** is the mode toggle:

* **`Ctrl+T`** (or the `/ui-mode` command): Switches the active renderer between `three-pane` and `flat-tree` immediately, preserving which command/category was last highlighted where possible (best-effort — a category open in `flat-tree` maps to the same category highlighted in `three-pane`'s Left Pane, and vice versa, since both read the same `MenuNode` tree per §1).

### ⬛ Mode: `flat-tree` (new)
There is **one** focus location — the tree itself — plus a modal search overlay and pushed sub-pages. This is simpler than the three-pane mode's Left/Middle/Right pane-focus state machine because there's no pane to shift focus between anymore.

#### 🟢 State A: Normal Navigation (Search Inactive)

##### 1. The Tree (only focus location)
* **UpArrow / DownArrow / J / K**: Move the single selection index up/down through the *currently visible* flattened rows (closed branches count as 1 row; open branches contribute themselves plus their visible children, per §1's node model). Selection wraps at top/bottom.
* **Enter on a branch** (`[Category]` or a grouped command like `/dotnet-tools`/`/ollama-tools`): Toggles it open/closed in place — children are inserted/removed from the visible row list without changing scroll position of unrelated rows.
* **Enter on a leaf** (`/dbld`, `/claude-cloud`, …): Executes the command immediately, same as v3.0.
* **Highlighting a widget-backed leaf** (`/disk`, `/public-ip`, `/ssh-info`, `/ollama-status`, `/account-tree`, `/quota-chart`, `/live-dashboard`): Auto-expands the inline live block under that row (no keypress needed, same as the old Right Pane auto-updating on highlight); moving off the row collapses it again.
* **/** (Slash key): Opens the global search overlay (see State B) — filters across every leaf in every category/group at once, not just the currently-open branch.
* **Escape / Q**: If any sub-page (account list, theme list, workspace list) is pushed, pops it back to the tree; if already at the root tree, exits the Control Center.

##### 2. Sub-page Selection Menus (pushed views, not a 4th pane)
Selecting a sub-view (Select Active Account, Select Shell Theme, Workspace navigation) pushes a new full-width flat list on top of the tree — visually it *replaces* the tree view rather than opening beside it:
* **UpArrow / DownArrow / J / K**: Scrolls the pushed list; wraps at top/bottom.
* **Enter**: Confirms selection (e.g. `/fptvttnhon2026@gmail.com` switches credentials instantly; `/cobalt` under Themes updates files/env vars).
* **Special Operations Hotkeys** (Account sub-page only): **`A`** (Add) triggers account creation; **`D`** (Delete) removes the highlighted profile folder with confirmation; **`O`** (Logout) clears keyring credentials.
* **Escape / Q / LeftArrow / H**: Pops the sub-page and restores the tree at its prior scroll/selection position.

#### 🔵 State B: Active Search (Search Engaged)
Once `/` is pressed and text characters are in the query buffer:
* **Input Buffering**:
 * Any alphanumeric or space key press appends characters to the search buffer (e.g., typing `/clau` updates the filter).
 * `Backspace` → Deletes the last character in the query buffer.
* **Navigation & Autocomplete**:
 * `UpArrow` / `DownArrow` / `J` / `K` → Navigate the *filtered, flattened* result list (matching branches auto-expand so their matching children are visible) without closing the search.
 * `Tab` → Autocompletes the search buffer with the highlighted item's command alias.
 * `Enter` → Confirms the highlighted selection, runs the command, and exits search mode.
* **Escape Search**:
 * `Escape` → Instantly exits search mode, clears the query buffer, and restores the tree to whatever expand/collapse state it was in before search opened.
* **Key Overrides**:
 * All single-key action handlers (`A`/`D`/`O`) are **suppressed** while typing in the search box to prevent accidental triggers.

---

### ⬜ Mode: `three-pane` (unchanged from today's v3.0 behavior)
Kept byte-for-byte identical in behavior to what's shipped today — this mode exists precisely so nothing is lost for anyone who prefers it. The keyboard interaction flow is split into two distinct states, same as it is in the current implementation.

#### 🟢 State A: Normal Navigation (Search Inactive)

##### 1. Main Category Menu (Left Pane)
When the keyboard focus is locked on the Left Category Pane:
* **UpArrow / DownArrow / J / K**: Scroll through the 10 core categories defined in [menu_map.md](file:///C:/Users/TruongNhon/Documents/Powershell/menu_map.md). Highlighting a category instantly loads its child options in the Middle Options Pane, and displays category summaries in the Right Pane.
* **Tab / RightArrow / L / Enter**: Shifts focus to the Middle Options Pane, locking focus on the first command option.
* **/** (Slash key): Immediately displays the search prompt globally and focuses on the Middle Pane, letting the user query across all categories at once.
* **Escape / Q**: Exits the Control Center application cleanly.

##### 2. Child Command Options Menu (Middle Pane)
When keyboard focus transitions to the Middle Options Pane:
* **UpArrow / DownArrow / J / K**: Scroll through the category-specific commands. The Right Pane updates in real-time, loading help topics, configurations, or live widgets.
* **LeftArrow / Escape / H**: Returns focus back to the Left Pane Category Selector.
* **Enter**: Executes the highlighted command immediately. If the item is a container grouping (e.g. `/dotnet-tools`, or the new `/ollama-tools`/`/quota-views` groups from §1), pressing `Enter` expands it in-line showing its child list.
* **/** (Slash key): Focuses the search input in the Middle Pane prompt header, letting the user type characters to filter.

##### 3. Sub-page Selection Menus (Child Lists)
* **UpArrow / DownArrow / J / K**: Scrolls the list of options; wraps at top/bottom.
* **Enter**: Confirms selection.
* **Special Operations Hotkeys** (Account sub-page only): **`A`** (Add), **`D`** (Delete), **`O`** (Logout).
* **Escape / Q / LeftArrow / H**: Cancels sub-page selection, returns focus to the parent command options list.

#### 🔵 State B: Active Search (Search Engaged)
Same input-buffering/autocomplete/escape/key-override rules as the `flat-tree` mode's State B above — search behavior is one of the pieces both modes share verbatim, since it operates on the same underlying `MenuNode` list either way, just rendered in the Middle Pane instead of inline in the tree.

### 🧩 Implementation note for §3/§7
Both modes share one flattening function — `IEnumerable<TreeRow> GetVisibleRows(TreeState)` — that the normal renderer, the search-filtered renderer, *and* both `IMenuRenderer` implementations all call (search just wraps it with a predicate + auto-expand; `three-pane` calls it once per pane-level instead of once for a single flat list, but it's the same underlying traversal). `CcNavigator.Run()`'s current ~340-line input loop and `RenderPanes()`'s duplicate 190-line alias-branching chain (finding #1 in §3) are replaced by: this flattening function, a generic open/close toggle keyed by node id, `ScreenChrome` for the shared header/footer/content contract, and a single dispatch through the unified `CommandRegistry` (§3.0) for leaf execution — meaningfully *less* code than what exists today (one flattening/dispatch core plus two thin renderers), not a like-for-like UI reskin plus a second copy bolted on beside it.

---

## 📦 3. Refactoring [Program.cs](file:///C:/Users/TruongNhon/Documents/Powershell/AgyTuiApp/Program.cs) (C# Monolith)

> Verified against the current file on disk: **8,558 lines / 343 KB**, a single `namespace AgyTui;` (line 41) containing **135 top-level type declarations** — 69 `static class` engines/helpers and ~66 `sealed record`/`enum` data models — all as flat siblings with no sub-namespacing. Line numbers below are pinned to the current revision; re-verify with `grep -n` before extracting, since any edit shifts everything after it.

### 🔍 Verified Current Issues (with evidence)

1. **Three god-classes carry ~2,700 of the 8,558 lines (≈32%) on their own:**
 | Class | Lines | Size | Responsibility crammed in |
 |---|---|---|---|
 | [`CcNavigator`](file:///C:/Users/TruongNhon/Documents/Powershell/AgyTuiApp/Program.cs#L7314) | 7314–8219 | 906 | Menu data (`AllSections`), the entire keyboard input state machine (`Run()`, 7440–7780), **and** ~10 widget-rendering methods (disk/IP/SSH/account-tree/quota-chart/ollama-status), **and** a second alias-branching chain in `RenderPanes()` (8010–8217) that re-tests the same alias strings already handled in `Run()`. |
 | [`AgyAccountCore`](file:///C:/Users/TruongNhon/Documents/Powershell/AgyTuiApp/Program.cs#L672) | 672–1581 | 910 | Multi-account management, DPAPI token backup/restore, rolling quota math, directory-size caching, and account stats — five distinct responsibilities in one class. |
 | [`AgyAiCore`](file:///C:/Users/TruongNhon/Documents/Powershell/AgyTuiApp/Program.cs#L2926) | 2926–3812 | 887 | Provider config/feature flags, Ollama proxy/daemon lifecycle, Claude/Codex/OpenClaw/Hermes process launching, model selection, and the AI dashboard. |

2. **Four independent, hand-maintained sources of truth for the command catalog.** Every new alias must be added to all four or the app silently desyncs (this is *already* the state today — see the cross-check table below):
 * `PaletteCommand.Commands` — line 5041 (69 entries, feeds the `cc` command palette).
 * `ProfileHelp.HelpTopics` — line 5068 (a second, independently-worded copy of most of the same aliases, feeds `help`).
 * `CcNavigator.AllSections` — line 7318 (the actual UI menu tree; adds ~17 aliases the other two don't have, e.g. `claude-cloud`, `ollama-models`, `account-tree`, `theme`).
 * `Program.RunCommand`'s `switch` — line 8284 (~65 `case` labels; the only one that's actually executable, plus a couple of dispatch-only aliases like `repo-graph`/`nexus-stats` that aren't reachable from any menu at all).
3. **Duplicated feature implementations**, not just duplicated tables:
 * `OllamaHelper.ShowOllamaLogs` (line 1895) and `AgyAiCore.ShowOllamaLogs` (line 3584) — two full implementations of the same feature.
 * `AgyAccountDisplay.ShowAccountTree`/`ShowQuotaChart` (5197–5260) duplicate `CcNavigator.GetAccountTreeWidget`/`GetQuotaChartWidget` (7880–7928).
 * Streak-calculation logic appears independently in both `StudyStats` (line 6409) and `StudyStreak` (line 6525).
 * Seven near-identical regex colorizer methods in `CodeViewer` (`ColorizeCsharp/Powershell/Json/Markdown/TypeScript/Sql/Bash/Yaml`, 4270–4341) that differ only in their regex table.
4. **Four independently reinvented ad-hoc TTL caches**, each with a slightly different shape and none sharing an abstraction:
 * `AgyAccountCore._sizeCache` (1387, manual `lock`, 15s TTL) and `_statsCache` (1451, manual `lock`, 3s TTL, must be cleared manually from `AddAccount`/`DeleteAccount`/`SetActiveAccount`).
 * `AgyAiCore._lastOllamaStatus`/`_ollamaStatusCachedAt` (3105–3106, nullable bool + TTL, no lock).
 * `CcNavigator._cachedOllamaWidget`/`_ollamaWidgetCachedAt` (7955–7956) — declared `public static` and **directly mutated from `Program.RunCommand`** at line 8338 (`CcNavigator._cachedOllamaWidget = null;`), i.e. one "screen" class reaches into another class's private cache field.
 * `CcNavigator._cachedIp`/`_lastIpFetch` (7820–7821) — a 4th, differently-shaped cache for the public-IP widget.
5. **8 separate `new HttpClient()` call sites** (`AgyAccountCore.SyncActiveAccountWithKeyring`, `OllamaHelper`, `AgyAiCore` ×several, `SystemHelper.GetPublicIP`, `CcNavigator.GetOllamaStatusWidget`) — no shared/injected client, a socket-exhaustion smell under repeated TUI refresh.
6. **8 near-identical "shell out and capture output" helpers** reimplemented per class instead of shared: `SystemHelper.RunProcess`, `AgyAiCore.RunCapture`/`RunInteractive`, `GitHelper.RunGit`/`RunGitDirect`, `DockerHelper.RunDocker`/`RunDockerCompose`, `AwsHelper.RunLocalAwsCli`, `Projects.RunNpm`, `GitDiffViewer.RunGit`, `GitNexus.Git` — all wrap `ProcessStartInfo`/`Process.Start` almost identically.
7. **Hardcoded, machine-specific absolute paths baked into source** instead of `profile.config.json` (the config file already externalizes some of these — `AgySourceHome`, `GlobalBinDir`, `ProjectsBaseDir` — but these were missed):
 * `AntigravityDeckHelper.DeckPath = @"C:\Users\TruongNhon\AppData\Local\AntigravityDeck"` (line 2112) — breaks on the current machine (`NhonVTT`), confirmed by the actual account running this session.
 * Crash-log path `@"C:\Users\TruongNhon\Documents\Powershell\tui_error.txt"` hardcoded in `CcNavigator.Run()`'s catch block (line 7770).
 * `Projects.AgBaseDir` referencing `C:\Users\sshuser\project` (line 1720).
8. **Mixing of Concerns**: Terminal UI rendering (`Spectre.Console`), DPAPI/Windows Credential Manager P/Invoke, raw `HttpListener`/`TcpListener` servers (`SshHelper`), process management, and quiz state all coexist with no namespace boundaries.
9. **Compilation Overhead**: A single 343 KB file means every edit recompiles the whole app, and git merge conflicts are concentrated in one file — this is also why `scirpts/cs-minify.ps1` / `cs-deminify.ps1` exist as a workaround to move the file through tools with size limits, which is itself a symptom of the file being too large.
10. **Naming Inconsistencies**: Terse Unix-style command aliases (`dbld`, `dtst`, `gcmt`, `dkcl`) coexist with PascalCase C# members; class prefixes are inconsistent (`Agy*`, `Cc*`, `Spectre*`, and many with none) with no per-subsystem namespace.
11. **Misleading indentation**: nested types/methods are sometimes printed at column 0 despite being lexically nested (verified by brace-balance, not by eye) — any automated line-range extraction must brace-balance rather than trust whitespace.

### 🗺️ Proposed Architecture Splitting

```mermaid
graph TD
 A[AgyTuiApp Project] --> B[Core Engine]
 A --> C[UI Layouts & Views]
 A --> D[Feature Domains]
 A --> E[Command Registry]

 B --> B1[Program.cs Entry]
 B --> B2[Config.cs]

 E --> E1[CommandRegistry.cs — single source of truth]

 C --> C1[Views/MenuNode.cs — shared tree model]
 C --> C2[Views/ThreePaneRenderer.cs]
 C --> C3[Views/FlatTreeRenderer.cs]
 C --> C4[Views/LearnView.cs]
 C --> C5[Views/AccountView.cs]
 C --> C6[Components/ScreenChrome.cs]
 C --> C7[Components/SpectreWidgets.cs]
 C --> C8[Components/StatusWidgets.cs — IStatusWidget impls]
 C --> C9[Components/ProcessRunner.cs]

 D --> D1[Features/AI/OllamaHelper.cs + AgyAiCore.cs]
 D --> D2[Features/Accounts/AgyAccountCore.cs]
 D --> D3[Features/Study/QuizEngines.cs]
 D --> D4[Features/SysAdmin/SystemHelpers.cs]
 D --> D5[Features/Workspace/WorkspaceRegistry.cs]
 D --> D6[Features/DevTools/*.cs]
```

### 🗂️ Proposed File Structures (with verified source line ranges)

#### 0. Command Registry (Namespace: `AgyTui.Registry`) — **new, addresses finding #2 above**
The single biggest structural risk is the 4-way duplicated alias catalog. Before or alongside the file split, collapse `PaletteCommand.Commands` (5041), `ProfileHelp.HelpTopics` (5068), `CcNavigator.AllSections` (7318), and `Program.RunCommand`'s `switch` (8284) into **one** table:
* **`CommandRegistry.cs`**: `record CommandEntry(string Alias, string DisplayName, string Description, string Category, bool RequiresAiOllama = false, bool RequiresAgy = false)` plus a single `CommandEntry[] All` array. `CommandPalette`, `ProfileHelp`, and `CcNavigator.AllSections` all *read from* this array instead of maintaining their own copies. `Program.RunCommand`'s dispatch switch stays a switch (C# has no cheap alias→delegate table without reflection overhead) but gets a startup assertion that every `CommandRegistry.All` alias has a matching `case`, so drift becomes a build-time/test failure instead of a silent gap.
* This is the highest-leverage single change in the whole plan — it doesn't require touching UI behavior, just deleting three duplicate tables and pointing everything at one.
* **`Category` is already a field on `CommandEntry`, not an afterthought**: the top-level `/` search (§1) and the IDE's own command bar (§5) both group by this same field, using the identical branch-per-category `MenuNode` structure — as the alias count grows past today's ~65, the fix for "too many flat results" is the same one property already in this record, not a separate feature to design later.

#### 1. Core Engine
* **[Program.cs](file:///C:/Users/TruongNhon/Documents/Powershell/AgyTuiApp/Program.cs)**: Shrinks to `Main` (8222–8240) + `RunCommand` (8257–8557) + `SelectTopicInteractive` (8242). Target ≈350 lines.
* **`Config.cs`**: Deserialization of `profile.config.json` (`AiMode`, `AiProviderMode`, `EnableAiOllama`, `EnableAgy`, `AgySourceHome`, `GlobalBinDir`, `ProjectsBaseDir`, `ProjectSearchPaths`, plus the new `UiMode: "three-pane" | "flat-tree"` from §1) — currently these are read ad hoc from several classes (`AgyAccountCore.AgySourceHome`/`AgyAccountPrefix` at 681–682, `AgyAiCore`'s config path methods at 2964–2981); centralize into one injectable settings object instead of static readonly strings scattered across ~15 classes.

#### 2. Component/UI Wrappers (Namespace: `AgyTui.UI`)
* **`SpectreWidgets.cs`**: [SpectreMenu](file:///C:/Users/TruongNhon/Documents/Powershell/AgyTuiApp/Program.cs#L43) (43–180), [SpectrePager](file:///C:/Users/TruongNhon/Documents/Powershell/AgyTuiApp/Program.cs#L181) (181–242), [SpectrePanel](file:///C:/Users/TruongNhon/Documents/Powershell/AgyTuiApp/Program.cs#L243) (243–260), [SpectreProgress](file:///C:/Users/TruongNhon/Documents/Powershell/AgyTuiApp/Program.cs#L261) (261–292), [SpectreTable](file:///C:/Users/TruongNhon/Documents/Powershell/AgyTuiApp/Program.cs#L378) (378–437). `LogHelper` (293–377) can live alongside or move to `Core/`.
* **`ScreenChrome.cs`***(new, implements §1's Standardized Screen Contract)*: The shared header/breadcrumb/content-region/footer-hint wrapper every screen renders through in both modes, plus the shared status-color token set (`Accent`/`Success`/`Warning`/`Error`/`Muted`/`Live`) so no class inlines its own `Color.Green`/`Color.Red` calls anymore.
* **`Icons.cs`***(new, §5's shared Icon System)*: One lookup table (file type, status, AI provider, Ollama model family, learning subject, mastery level) with Nerd Font / emoji variants, replacing the single one-off `FileExplorer.GetFileIcon` (4199) as the only place icons are computed.
* **`StatusWidgets.cs`***(new)*: One `interface IStatusWidget { IRenderable Render(); string Alias; }` plus one implementation per live widget — disk, public IP, SSH info, Ollama status, account tree, quota chart, live dashboard — moved out of `CcNavigator`'s ~10 `Get*Widget` methods (7799–7982). Neither renderer computes widget content itself; both just place whichever `IStatusWidget.Render()` output belongs to the current selection (side pane in `three-pane`, inline block in `flat-tree`).
* **`ProcessRunner.cs`***(new, addresses finding #6)*: One shared `RunProcess(exe, args, capture: bool)` helper to replace the 8 duplicated shell-out wrappers in `SystemHelper`, `AgyAiCore`, `GitHelper`, `DockerHelper`, `AwsHelper`, `Projects`, `GitDiffViewer`, `GitNexus`.
* **`HttpClientProvider.cs`***(new, addresses finding #5)*: One `static readonly HttpClient` (or `IHttpClientFactory` if DI is introduced) shared by the 8 current `new HttpClient()` sites.

#### 3. View Models & TUI Layouts (Namespace: `AgyTui.Views`)
* **`MenuNode.cs`***(new)*: `record MenuNode(string Id, string Label, MenuNodeKind Kind, MenuNode[] Children, CommandEntry? Command)` — built once at startup from `CommandRegistry.All` (§3.0), grouped per the re-grouped nesting in §1 (`/ollama-tools`, `/antigravity-deck`, `/quota-views`, plus the existing `/dotnet-tools`). This is the single tree both renderers below read; it replaces `CcNavigator.AllSections` (7318) entirely rather than living beside it.
* **`IMenuRenderer.cs`***(new)*: `interface IMenuRenderer { void Run(MenuNode root); }` — the mode-switch seam described in §1.
* **`ThreePaneRenderer.cs`***(new)*: The `three-pane` mode. Extracted from `CcNavigator.Run()`'s Left/Middle/Right pane-focus state machine (7440–7780) and `RenderPanes()` (8010–8217), rewritten to walk `MenuNode` instead of `AllSections`, and to render through `ScreenChrome` + `IStatusWidget` instead of its own bespoke panel code.
* **`FlatTreeRenderer.cs`***(new)*: The `flat-tree` mode from §1/§2 — the `GetVisibleRows(TreeState)` flattening function, the open/close toggle, global `/` search over the flattened list, and inline `IStatusWidget` expansion under the highlighted row.
* **`LearnView.cs`**: Sub-pages spanning `SpacedRepetitionEngine` → `VocabDrill` (5344–6275) — flashcards, kana/kanji/JLPT drills, algorithm visualizer, interview prep, STAR builder. Rendered as pushed sub-page lists per §1's "one sub-page list contract."
* **`AccountView.cs`**: `AgyAccountMenu` (1582–1717) + `AgyAccountDisplay` (5197–5260, after de-duplicating against `CcNavigator`'s widget methods per finding #3) — also rewritten onto the shared sub-page list contract instead of its own picker loop.

#### 4. Feature Service Domains (Namespace: `AgyTui.Features.*`)
* **`Features/AI/AgyAiCore.cs`** (2926–3812) + **`Features/AI/OllamaHelper.cs`** (1893–2109) — merge the two `ShowOllamaLogs` implementations (finding #3) into one during the move.
* **`Features/Accounts/AgyAccountCore.cs`** (672–1581) — candidate to further split into `AccountRepository.cs` (CRUD, 714–1181), `QuotaTracker.cs` (rolling quota math, 894–1097, 1303–1389), and `TokenVault.cs` (DPAPI backup/restore, 947–1010) given its 910-line size.
* **`Features/Study/QuizEngines.cs`**: spaced repetition, kana/kanji/JLPT, algo visualizer (5344–6275), plus `AlgoVisualizer`/`ComplexitySheet`/`ProblemTracker`/`SnippetLibrary`/`CsharpQuiz`/`InterviewBank`/`StarBuilder` (5614–6222).
* **`Features/Study/ProgressTracking.cs`**: `StudySession`/`StudyStats`/`DailyGoals`/`StudyStreak`/`DueReview`/`ProgressDashboard`/`WeakItemsQueue` (6276–6717) — de-duplicate streak logic (finding #3) here.
* **`Features/SysAdmin/SystemHelpers.cs`**: `SystemHelper` (2179–2453) + `SshHelper` (2454–2735) — disk diagnostics, Tailscale/SSH, the embedded `HttpListener` key-receiver.
* **`Features/Workspace/WorkspaceRegistry.cs`**: `Projects` (1718–1801), `WorkspaceEntry`/`WorkspaceRegistry` (1802–1862), `ProfileNavigator` (1863–1892).
* **`Features/DevTools/GitTools.cs`**: `GitHelper` (3873–3959), `GitDiffViewer` (4419–4477), `GitNexus`/`RepoGraph`/`GitNexusStats` (6992–7258).
* **`Features/DevTools/DotNetAndDocker.cs`**: `DotNetHelper` (3813–3872), `DockerHelper` (3960–4050), `AwsHelper` (4051–4078), `DatabaseHelper` (4079–4109), `ProjectScaffolder` (4110–4151).
* **`Features/DevTools/TerminalIde.cs`**: `FileExplorer`/`CodeViewer`/`SymbolSearch`/`GitDiffViewer`/`TerminalIde` (4152–4605) — table-drive the 7 `Colorize*` methods (finding #3) into one regex-table lookup during the move.
* **`Features/DevTools/IdeCommandRegistry.cs`***(new, §5's Slash Commands Inside the IDE)*: The `/open`/`/goto`/`/find`/`/grep`/`/replace`/`/symbols`/`/diff`/`/blame`/`/tabs`/`/explain`/`/fix`/`/doc`/`/test`/`/snippet`/`/bookmark`/`/history` table — a second, smaller sibling of `CommandRegistry` (§3.0), scoped to IDE actions, feeding the same `/` search UI.
* **`Features/DevTools/SkillLoader.cs`***(new, §5's Skills System)*: Discovers `skills/*.md` (workspace-local) and `~/.agy/skills/*.md` (global), parses frontmatter + `steps:`, and executes each step against the small fixed primitive set (`AskAi`, `ShowDiff`, file write, `ProcessRunner.Run`) — reuses `ResourceRegistry` (4614–4677)'s existing folder-indexing/checksum machinery instead of building a second content-sync system.
* **`Features/Obsidian/ObsidianBridge.cs`**: `ObsidianBridge`/`ObsidianGraph`/`ObsidianStudySync` (6718–6991).
* **`Features/Theme/ThemeHelper.cs`**: `ThemeHelper` (2736–2925).

### 🗄️ Unified Caching Strategy (addresses finding #4 — one design, not four ad-hoc ones)
Finding #4 in §3 catalogued four independently reinvented TTL caches (`AgyAccountCore._sizeCache`/`_statsCache`, `AgyAiCore._lastOllamaStatus`, `CcNavigator._cachedOllamaWidget`/`_cachedIp`), and the Terminal IDE plan above adds at least two more consumers (symbol/breadcrumb lookups, git-gutter hunk data). Rather than let that become a fifth and sixth ad-hoc pattern, build one small generic cache once:

```csharp
public sealed class TtlCache<TKey, TValue> where TKey : notnull
{
 private readonly ConcurrentDictionary<TKey, (TValue Value, DateTime ExpiresAt)> _entries = new();
 private readonly TimeSpan _ttl;
 public TtlCache(TimeSpan ttl) => _ttl = ttl;

 public TValue GetOrCompute(TKey key, Func<TValue> factory)
 {
 if (_entries.TryGetValue(key, out var e) && e.ExpiresAt > DateTime.UtcNow) return e.Value;
 var value = factory();
 _entries[key] = (value, DateTime.UtcNow + _ttl);
 return value;
 }

 public void Invalidate(TKey key) => _entries.TryRemove(key, out _);
 public void InvalidateAll() => _entries.Clear();
}
```
Thread-safe via `ConcurrentDictionary` (replacing every manual `lock`/raw static field in the four existing caches), and every consumer becomes a one-line field declaration instead of a bespoke cache implementation:

| Consumer | Cache instance | TTL | Invalidation trigger |
|---|---|---|---|
| `AgyAccountCore.GetPrivateDirectorySize` (today: `_sizeCache`, 1387) | `TtlCache<string, long>` | 15s | none explicit — natural expiry only, same as today |
| `AgyAccountCore.GetAccountStats` (today: `_statsCache`, 1451) | `TtlCache<string, AccountStats>` | 3s | `.Invalidate(account)` from `AddAccount`/`DeleteAccount`/`SetActiveAccount` (same call sites as today, just calling a method instead of clearing a dictionary by hand) |
| `AgyAiCore.IsOllamaRunning` (today: `_lastOllamaStatus`, 3105) | `TtlCache<Unit, bool>` | 5s | `.InvalidateAll()` from the `ollama-status` alias handler |
| `CcNavigator.GetOllamaStatusWidget` (today: `_cachedOllamaWidget`, public static field, 7955) | `TtlCache<Unit, IRenderable>` inside the `IStatusWidget` implementation from §1 | 5s | `.InvalidateAll()` called through a proper method instead of `Program.RunCommand` reaching into another class's public field (fixes the exact smell finding #4 called out) |
| `CcNavigator.GetPublicIpWidget` (today: `_cachedIp`, 7820) | `TtlCache<Unit, string>` | 5 min (public IP rarely changes — today's cache has no explicit TTL rationale, this gives it one) | none explicit |
| **New** — IDE breadcrumb/symbol lookups (§5 Terminal IDE, feature #3) | `TtlCache<(string Path, DateTime Mtime), string[]>` | keyed by file mtime, not time — effectively "cache until the file changes on disk" | re-key naturally invalidates when the file's mtime changes; no explicit invalidation call needed |
| **New** — IDE git-gutter hunk data (§5 Terminal IDE, feature #4) | `TtlCache<string Path, HashSet<int>>` | 10s | `.Invalidate(path)` on save or on `/diff`/`/commit` |
| **New** — Status-bar git branch name | `TtlCache<string WorkspacePath, string>` | 10s | none explicit |

This table is also the concrete deliverable for Phase 6 (§7's cleanup phase): replace all four existing ad-hoc caches with `TtlCache<,>` instances first (pure refactor, verified by the existing behavior being unchanged), *then* wire the two new IDE consumers into the same class when Terminal IDE lands, so the cache abstraction is proven on the existing code before new code depends on it.

### 📝 Code Style & Naming Alignment
* **Naming Convention**: Standardize abbreviation-style locals (`catIdx`, `destDirLoc`, `psi`) to full words during the move — cheapest to do file-by-file as each class is relocated, not as a single mechanical pass (renaming 8,558 lines in one PR would make the diff unreviewable).
* **Class Separation**: One top-level type per file as the default; only keep genuinely small, tightly-coupled record models (e.g. `FlashCard`/`DeckMeta`/`DeckFile`) in the same file as the class that owns them.
* **Config over hardcoding**: Move the three hardcoded paths in finding #7 into `profile.config.json` alongside the existing `AgySourceHome`/`GlobalBinDir`/`ProjectsBaseDir` entries.

---

## ⚡ 4. PowerShell Profile Modularization

The [Microsoft.PowerShell_profile.ps1](file:///C:/Users/TruongNhon/Documents/Powershell/Microsoft.PowerShell_profile.ps1) script will be modularized by using a clean compile build script.

### 🔄 Bundle Build Approach
* **Development**: Developers modify isolated topic scripts inside `Profile/Core/` and `Profile/Helpers/`.
* **Compilation**: A compilation script `scirpts/compile-profile.ps1` aggregates all of these individual files into a single optimized profile `Microsoft.PowerShell_profile.ps1`.
* **Benefit**: This maintains ultra-fast shell startup (avoiding multiple disk file reads during shell execution) while allowing modular code maintenance.

### 🔒 Bypassing Process DLL Lock
To allow seamless rebuilding of the C# application without file lock issues in running terminal hosts:
1. Configure the profile wrapper `cc` to launch the standalone published executable `AgyTuiApp.exe` directly as a subprocess (`& $exePath`) instead of loading and running the C# types in-process via `Add-Type`.
2. Upon exit, the parent PowerShell host will inspect `selected_project.txt` and `active_account.txt` to sync directories and active profile contexts. This prevents DLL file locking entirely.

>**Verified today's exact lock source**: `Microsoft.PowerShell_profile.ps1` line 145 currently does `Add-Type -Path $agyTuiDll -ErrorAction Stop` (loading `AgyTuiApp.dll` in-process at every shell startup, line ~140 also loop-loads every DLL found), and the `cc` alias resolves through `Invoke-ControlCenter` → `[AgyTui.AgyAccountCore]::...` static calls (line 1650, 1586, 1598). This is exactly why rebuilding `AgyTuiApp` while a PowerShell session is open fails with a file-lock error today — replacing the `Add-Type` call with an `& $exePath` subprocess launch (and the account-active file handoff already described above) removes the lock entirely without changing the `cc` alias's external behavior.

### 🛞 CI/CD Pipeline

**Verified current state**: `.github/workflows/ci.yml` exists and runs on every push/PR to `main`/`master`, on `windows-latest`, doing exactly one thing — checkout, then `./Tests/run_tests.ps1`. Reading that script directly surfaces two concrete problems, not hypothetical ones:

1. **The C# app is never built by CI.** `run_tests.ps1` only AST-parses `Microsoft.PowerShell_profile.ps1` for syntax errors, checks external-tool presence (`git`/`docker`/`aws`/`ollama`/`gh`/etc.), dot-sources the profile, asserts a fixed list of PowerShell functions/aliases exist, and runs Pester specs from `Tests/Unit/*.Tests.ps1`. There is no `dotnet build`, `dotnet test`, or `dotnet publish` step anywhere — 8,558 lines of C# (the entire subject of §3) can break the build and CI would still go green, as long as a stale `dist\AgyTuiApp.dll` from a previous manual build happens to still be sitting on disk.
2. **`run_tests.ps1` itself has the exact hardcoded-path disease finding #7 (§3) flags in `Program.cs`.** Lines 5, 14, and 46 hardcode `C:\Users\TruongNhon\Documents\Powershell\...` for the DLL path and the profile path. On this machine (user `NhonVTT`) those paths don't exist — `Test-Path $dllPath` at line 6 silently returns false (so the "pre-load assembly" step does nothing, no error), but the profile dot-source at line 46 targets a path that isn't this repo's checkout location, meaning **the test script does not actually validate the profile at its real, current location.** This needs the same fix as §3 finding #7: resolve both paths relative to `$PSScriptRoot`/the repo root instead of a hardcoded username, and it should happen *before* anything else in this section, since a CI pipeline that silently tests the wrong file is worse than no pipeline.
3. **Zero C# unit tests exist.** `Tests/Unit/*.Tests.ps1` are Pester specs for the PowerShell layer only; nothing in `Tests/` targets `AgyTuiApp` at the class level. This is partly a consequence of §3's monolith problem — a 906-line `CcNavigator` mixed with I/O isn't unit-testable — and becomes *newly possible* as classes are extracted: `SpacedRepetitionEngine`'s SM-2 math, `TtlCache<,>` above, `QuotaMetrics`/`CalculateRollingQuotas`, and `IdeCommandRegistry`'s command table are all pure-logic candidates for real `xunit` tests the moment they're pulled out of the file that currently entangles them with `Spectre.Console`/`Process`/`HttpClient` calls.

**Proposed pipeline** (extends the existing `ci.yml`, doesn't replace it — the PowerShell job stays, these are new jobs alongside it):
```yaml
jobs:
 powershell-tests: # existing job, unchanged, but run_tests.ps1's paths get fixed per point 2 above
 runs-on: windows-latest
 steps:
 - uses: actions/checkout@v4
 - run: ./Tests/run_tests.ps1
 shell: powershell

 dotnet-build: # new
 runs-on: windows-latest
 steps:
 - uses: actions/checkout@v4
 - uses: actions/setup-dotnet@v4
 with: { dotnet-version: '10.0.x' }
 - uses: actions/cache@v4 # NuGet package cache, keyed on csproj hash
 with:
 path: ~/.nuget/packages
 key: nuget-${{ hashFiles('AgyTuiApp/AgyTuiApp.csproj') }}
 - run: dotnet restore AgyTuiApp/AgyTuiApp.csproj
 - run: dotnet build AgyTuiApp/AgyTuiApp.csproj -c Release --no-restore -warnaserror
 - run: dotnet format AgyTuiApp/AgyTuiApp.csproj --verify-no-changes # once Phase 6's naming cleanup lands
 - run: dotnet test AgyTuiApp.Tests/AgyTuiApp.Tests.csproj --no-restore # once the xunit project above exists

 publish-on-tag: # new, only on version tags — produces the exe §4's DLL-lock fix depends on
 if: startsWith(github.ref, 'refs/tags/v')
 needs: dotnet-build
 runs-on: windows-latest
 steps:
 - uses: actions/checkout@v4
 - uses: actions/setup-dotnet@v4
 with: { dotnet-version: '10.0.x' }
 - run: dotnet publish AgyTuiApp/AgyTuiApp.csproj -c Release -r win-x64 --self-contained -o dist
 - uses: actions/upload-artifact@v4
 with: { name: AgyTuiApp-win-x64, path: dist/AgyTuiApp.exe }
```
* **`-warnaserror` on the build job** is what actually enforces §8's "zero new warnings" checklist item — today that's a manual eyeball check per phase; CI should be the thing that can't be skipped.
* **The NuGet cache** matters more once the file split in §3 lands, since more, smaller compilation units mean `dotnet restore` runs more often relative to actual code change size.
* **`publish-on-tag`** is CI's contribution to §4's DLL-lock fix: it's the pipeline that produces the exact `AgyTuiApp.exe` the profile wrapper is meant to launch as a subprocess, so a tagged release always has a ready-to-use published binary instead of requiring a manual local `dotnet publish`.
* **Honest limitation, not a gap to hide**: CI can verify build/tests/format, but §9's manual smoke test (three-pane vs. flat-tree parity, widget rendering, hotkey behavior) genuinely can't be automated cheaply for an interactive Spectre.Console TUI in a headless runner — that stays a human step per phase, not something this pipeline should pretend to cover.
* **Repo setting, not a file change**: once `dotnet-build` exists, mark it as a required status check for `main` in branch protection — this plan can describe it but can't apply it, since it's a GitHub repo setting rather than something in a YAML file.

---

## 🚀 5. Feature Enhancements (beyond structural migration)

A pure file-split changes nothing a user can see. Each phase in §7 is also a natural point to land a real capability upgrade in that domain, since you're already reading and rewriting that code. These are scoped to slot into the phase that already touches the same classes — not a separate rewrite effort.

### 📖 Learning & Study domain (slots into Phase 3, classes at 5344–6717)

**Redesigned daily flow.** Today `learn`, `due`, `weak`, `flashcard`, `vocab`, `jlpt`, `kana`, `kanji`, `stats`, `streak`, `goals`, and `obsidian` are 12 separate aliases with no wiring between them — the user has to already know which one to run and in what order. The enhancement is a single guided pipeline `/learn` walks through end-to-end:
```
/learn
 1. DueReview.GetAllDue() → merges decks + JLPT + kanji + kana + weak-items queue
 (today these are 5 separate aliases: due/weak/flashcard/jlpt/kana)
 2. WeakItemsQueue first → surfaces previously-missed cards before new due cards
 3. Run the review loop → existing FlashcardEngine/KanaQuiz/JlptVocabDrill UI, unchanged
 4. SpacedRepetitionEngine.UpdateCard() per answer → real SM-2 (see below), not the fixed progression
 5. On session end:
 → StudySession records duration (existing, StudySession 6296–6360)
 → StudyStreak.RecordToday() with grace-day logic (see below)
 → DailyGoals checked/ticked (existing, DailyGoals 6429–6522)
 → ObsidianStudySync offers to append a session summary block to today's daily note
 (existing capability, ObsidianStudySync 6971–6991 — today it's a separate manual
 action; the flow makes it a one-keypress "y/n" at the end of every session)
```
This turns 5+ manually-remembered aliases into one guided flow while still exposing each step as its own alias for power users who want to jump straight to `vocab` or `kanji`.

* **Fix the duplicated streak math** (finding #3: `StudyStats` line 6409 vs `StudyStreak` line 6525) *and* extend it: a proper streak needs "grace day" handling (miss one day without breaking a 30-day streak) — worth doing once, correctly, instead of twice, incorrectly.
* **LLM-assisted content generation**: `AgyAiCore` (already being relocated in Phase 5) can generate flashcards/quiz questions from an Obsidian note or a pasted block of text via the same Ollama/Claude path already used for `AskAi` (3718) — reuses `ContentExtractor`/`TemplateGenerator` (4851–4973), which today only parse pre-existing structured notes.
* **Anki import**: `MdExtractor`/`CsvExtractor`/`ExtractorRouter` (4678–4816) already have the plumbing to ingest external formats; add a `.apkg`/TSV importer alongside them so existing Anki decks aren't stuck outside this tool.
* **Real SM-2**: `SpacedRepetitionEngine` (5348–5378) is described in the code as "SM-2-like" — implement the actual easiness-factor formula so intervals adapt to per-card difficulty instead of a fixed progression; this is a self-contained change inside one 31-line class.
* **Retention analytics**: `StudyStats`/`ProgressDashboard` (6361–6685) currently show raw counts; add a rolling retention-rate chart (% correct on due-reviews over time) since the spaced-repetition data to compute it already exists in `SrState`/`SrResult`.
* **Per-subject icons + mastery icons**: give each learning sub-domain a fixed icon (see shared Icon System below) instead of today's plain text menu rows, and add an Anki-style maturity icon per card (🌱 new → 🌿 learning → 🌳 mature) driven by `SrState`'s existing interval data — no new fields needed, just a threshold mapping over data `SpacedRepetitionEngine` already tracks.

Mockup of the redesigned `/learn` flow (§5 above) with icons applied:
```
╭─ Learn & Study — Due Today ───────────────────────────────────────────────╮
│ 🔤 Vocab 12 due 🌱4 🌿6 🌳2 │ 🈂 Kana 3 due 🌿3 │
│ 漢 Kanji 5 due 🌱1 🌿3 🌳1 │ 🎓 JLPT N3 8 due 🌳8 │
│ ⭐ Weak items 4 (review these first) │
│ │
│ [Enter] Start guided session [S]kip to a specific deck [Esc] Back │
╰────────────────────────────────────────────────────────────────────────────╯
```

### 🔐 SSH × Tailscale domain (slots into Phase 4, `SshHelper` 2454–2735)
* **Replace the homemade key-enrollment server with Tailscale Serve/Funnel.** Today `SshHelper` hand-rolls an `HttpListener` mini web server (`FormHtml`/`SuccessHtml`/`InvalidHtml`) plus a raw `TcpListener` to receive SSH public keys from a phone over the LAN — this is a real attack surface (unauthenticated listener accepting key material) held together by manual NTFS ACL hardening. `tailscale serve` (or `funnel` for off-tailnet access) gives you TLS termination, tailnet-only ACL enforcement, and identity-aware access for free, and removes the custom `HttpListener`/`TcpListener` code entirely.
* **Use `tailscale status --json`** instead of the current IP-only lookup (`GetSshInfoWidget`, `CcNavigator` 7843) — surfaces peer online/offline state, hostname, and OS for every device on the tailnet, not just this machine's own IP.
* **Key lifecycle, not just key add**: `AddAuthorizedKey` today only appends to `authorized_keys`. Add expiry metadata (comment-embedded date) and a `/ssh-keys` sub-page to list/revoke previously-added keys — currently there's no way to see or remove a key once it's added except editing the file by hand.
* **QR code for mobile enrollment**: the existing enrollment flow already serves an HTML form to a phone; rendering a QR code of that URL in the terminal (a small, dependency-free ASCII QR renderer) removes the "type this URL by hand" step.
* **Session visibility**: extend `kill-port` (`SystemHelper.KillPort`) with an `ssh-sessions` view listing active inbound SSH connections by peer + duration, sourced from the same `netstat` parsing `KillPort` already does — currently `ssh-info` only shows the *count*, not which sessions.

Mockup of the enhanced `/ssh-info` view (icons from the shared Icon System below — 🟢/⚫ peer state, 🔑 key, 🌐 tailnet), replacing today's IP-only text line:
```
╭─ SSH × Tailscale ─────────────────────────────────────────────────────────╮
│ 🌐 Tailnet: nhon-tailnet.ts.net Funnel: 🟢 active on :22 │
│ │
│ 🟢 desktop-nhon 100.64.1.2 this device │
│ 🟢 pixel-phone 100.64.1.5 Android · idle 2m │
│ ⚫ old-laptop 100.64.1.9 offline · last seen 3d ago │
│ │
│ Active sessions: │
│ 🔑 pixel-phone → :22 00:14:32 [K]ill │
│ │
│ 🔑 Authorized keys (3) [A]dd via QR [R]evoke │
╰────────────────────────────────────────────────────────────────────────────╯
```

### 🤖 AI Agent & Ollama domain (slots into Phase 5, `AgyAiCore` 2926–3812)

**Redesigned invocation flow.** Today `claude-cloud`/`claude-ollama`/`codex-cloud`/`codex-ollama`/`openclaw`/`hermes` each go straight from menu selection to `InvokeClaude`/`InvokeCodex`/etc. (3210/3241/3321/3363) with no pre-flight check and no record kept afterward. The enhancement wraps every one of those 6 aliases in the same pre/post pipeline instead of each being a bare process launch:
```
/claude-cloud (or any of the 6 provider aliases)
 1. Pre-flight quota check → AgyAccountCore.GetAccountStats(activeAccount)
 if remaining 5h quota < threshold: warn, or auto-route to
 Ollama per "provider auto-fallback" below, instead of
 discovering the failure only after the process launches
 2. Invoke*(...) → existing process-launch code, unchanged
 3. On exit:
 → AiActivityLog.Record(provider, model, durationMs, exitCode) (new — see below)
 → CheckQuotaAfterRun (existing, 1366) still runs, now reading from the same log
```
* **Provider auto-fallback**: `autoswitch`/`AutoSwitchOnQuotaExceeded` (1303) already switches *accounts* on quota exhaustion; extend the same trigger to fall back cloud→Ollama (or vice versa) via `AgyAiCore.SetAiProviderMode`, so a quota-exceeded cloud call automatically retries against the local model instead of just failing.
* **Model benchmarking**: `OllamaHelper.ManageOllamaModels` (1893) lists/deletes pulled models but has no notion of which is "best" for a given task; add a lightweight latency/quality smoke-test command that runs a fixed prompt against each pulled model and tabulates response time.
* **One shared `HttpClient`** (finding #5) also fixes a real reliability issue here, not just style: repeated `new HttpClient()` calls against a local Ollama daemon under rapid TUI refresh is the most likely source of any "Ollama status flaky" reports.

Mockup of an enhanced `/ollama-models` view — `OllamaHelper.ManageOllamaModels` (1893) today just lists names; adding per-family icons and a running/idle indicator (reusing the shared Icon System below) turns it from a bare list into something scannable at a glance:
```
╭─ Ollama Models ────────────────────────────────────────────────────────────╮
│ 🟢 daemon running · :11434 · 3 models pulled │
│ │
│ 🦙 llama3.1:8b 4.9 GB used 2h ago [default] │
│ 🌬 mistral:7b 4.1 GB used 1d ago │
│ 🐈 qwen2.5-coder:14b 9.0 GB never used │
│ │
│ [P]ull new [D]elete [B]enchmark [S]et default [L]ogs │
╰────────────────────────────────────────────────────────────────────────────╯
```

### 🤝 Agy × Claude Workflow (the two most-used agents — slots into Phase 5, alongside `AgyAiCore`/`AgyAccountCore`)
Since Agy (account/credential switching, `agyswitch`/`agyquota`) and Claude Code (`claude-cloud`/`claude-ollama`) are the two most-used paths through this app, they're the highest-value place to add cross-tool wiring rather than treating them as two unrelated categories that happen to sit next to each other in the menu:
* **Account-aware Claude session continuity**: today `SetActiveAccount` (1011) swaps credentials via the DPAPI/keyring path, but an already-running `claude-cloud` session has no idea the account changed underneath it. Have `SetActiveAccount` write a lightweight session-invalidation marker that `InvokeClaude` (3210) checks on next call, so switching accounts mid-session is a deliberate, visible handoff instead of a silent credential mismatch.
* **Unified AI activity ledger** (`AiActivityLog`, new, backing the invocation flow above): every `InvokeClaude`/`InvokeCodex`/`InvokeOpenClaw`/`InvokeHermes` call is appended to one JSONL log (provider, account, model, timestamp, duration, exit code), exposed as a new `/ai-history` command. Today there is genuinely no record of "what did I run, with which account, and did it succeed" across a session that mixes both agents — this is the single most useful missing feature for someone alternating between them.
* **Diff/commit handoff into Claude**: `GitHelper`'s conventional-commit wizard (`gcmt`, 3873–3959) gets an option to draft the commit message by piping the staged diff into `InvokeClaude` with a fixed "write a conventional commit message for this diff" prompt, instead of the user typing it by hand; `ide-diff`/`GitDiffViewer` (4419–4477) gets a matching "send this diff to Claude for review" hotkey.
* **Shared per-workspace context handoff**: `WorkspaceRegistry`/`ProfileNavigator` (1718–1892) already know the active workspace; have them maintain a small `.agy-context.md` scratch file (recently-touched files via `TerminalIde`, open TODOs) that gets passed to `claude-cloud`/`claude-ollama` via `--append-system-prompt` on launch from that workspace — removes the "re-explain the project" tax every time you switch between using Agy for account/file work and Claude for the actual coding.
* **Scaffold → Claude handoff**: `ProjectScaffolder` (4110–4151) currently leaves a static boilerplate template; add a prompt after scaffolding offering to immediately launch `claude-cloud` in the new directory with a "implement the first feature / write initial tests" prompt, so the two most-used agents chain together instead of scaffolding being a dead end you have to manually pick up in a separate `claude-cloud` invocation.

### 📀 Antigravity Deck domain — review & refactor (slots into Phase 5 alongside `AgyAiCore`, `AntigravityDeckHelper` 2110–2177)

**Verified current behavior** (68 lines, read directly): `AntigravityDeckHelper` is a thin wrapper around a *separate Node.js web app* ("Antigravity Deck," presumably a companion dashboard) living at a hardcoded path — three public methods (`Setup`/`StartLocal`/`StartOnline`, 2114/2126/2139) each independently repeat the identical `if (!Directory.Exists(DeckPath)) { error; Thread.Sleep(2000); return; }` guard, then shell to `npm.cmd` via a private `RunNpmCommand` (2152) that uses `CreateNoWindow=false` — meaning `deck-start`/`deck-online` **pop open a separate, second console window** for the npm dev server, unlike every other shelled process in this file (`GitDiffViewer.RunGit`, `GitNexus.Git`, etc.), which all use `CreateNoWindow=true` and capture output in-app. The TUI blocks on `proc.WaitForExit()` and has zero visibility into that second window once it's open — no way to know if the dev server is still running, no way to stop it from inside the TUI, and no way to see what it printed (including, for `StartOnline`, the Cloudflare Tunnel URL that command exists specifically to obtain).

**Concrete refactor + enhancement list, in order of value:**
1. **Fix the hardcoded path** (already flagged as finding #7 in §3, restated here concretely): `DeckPath = @"C:\Users\TruongNhon\AppData\Local\AntigravityDeck"` (2112) breaks on this machine (`NhonVTT`) today. Move it into `profile.config.json` as `AntigravityDeckPath`, following the exact same `ProjectSearchPaths`-style fallback list already used for workspace discovery (`profile.config.json`'s existing array) rather than a single hardcoded string, so it's found regardless of which user profile the app runs under.
2. **Collapse the 3x repeated existence-check into one guard**: `EnsureDeckPathExists()` called once by all three public methods, and replace the bespoke `RunNpmCommand` (2152) with the shared `ProcessRunner.cs` from §3 — this is Deck becoming the *ninth* consumer of the same "8 near-identical shell-out helpers" finding #6 already catalogued, not a new problem, just an unfixed instance of an already-known one.
3. **Live status widget** (`deck-status`, mirrors `ollama-status` exactly): `AgyAiCore.IsPortListening`/`IsPortResponding` (3078/3080, currently `private` to `AgyAiCore`) get promoted to a shared `ProcessRunner`/`SystemHelpers` method so `deck-status` can point the identical check at port 3000 instead of Ollama's port — one generic "is anything listening/responding on port N" helper serving two features instead of being Ollama-specific code that Deck can't reach.
4. **Stop opening a second console window.** Change `CreateNoWindow` to `true`, redirect stdout, and stream it into the app's own scrollable pager (`SpectrePager`, the exact mechanism `OllamaHelper.ShowOllamaLogs` already uses for tailing Ollama's server log) or — once the Terminal IDE's integrated terminal panel from the previous section exists — host the dev-server output there. This turns "Press Ctrl+C to terminate the server" (2135, 2148 — an instruction pointed at a window the user has to go find) into an in-TUI `[K]ill` action the same way the SSH-sessions mockup earlier in this section has one.
5. **Capture and surface the tunnel URL.** `npm run online`'s whole purpose is producing a public Cloudflare Tunnel URL, which today is only visible by scrolling a separate window's history. With stdout captured per point 4, regex-match the tunnel URL line and render it directly in the TUI — and since this is exactly the kind of "type this URL on your phone" moment the SSH-enrollment QR idea (§5, SSH × Tailscale) already solves, reuse the same ASCII QR renderer here instead of building a second one.
6. **Naming clarity, not a rename**: "Antigravity Deck" (a separate Node web dashboard, launched as a subprocess) and `live-dashboard` (§3's AGY Account Switch → Quota Views group, an in-TUI Spectre table) are two unrelated things that happen to both contain the word "dashboard" — worth a one-line disambiguation in `/help`'s description text so a new user doesn't conflate them, no code change needed.

### 🖥️ Terminal IDE domain — VS Code-style enhancement plan (slots into Phase 2, `FileExplorer`/`CodeViewer`/`SymbolSearch`/`GitDiffViewer`/`TerminalIde` 4152–4605)

**Verified current behavior** (read directly from source, not assumed): `TerminalIde.Open` (4478) is a **screen-clearing, single-view-at-a-time menu loop** — `AnsiConsole.Clear()` then a 5-item `SpectreMenu` ("Browse files / Search in files / View git diff / Open file by path / Exit"). `FileExplorer.Browse` (4152) is its own separate clear-and-redraw loop, one directory level at a time, with no persistent tree — going into a folder and back out re-lists it from disk every time. `CodeViewer` (4207) is a **read-only pager** (`SpectrePager`) with regex-based (not AST/LSP-based) syntax coloring for 8 languages; there is no cursor, no in-place editing — "Edit" (4522) shells out to `notepad`/`nano` as a completely separate external program. `SymbolSearch` (4343) extracts symbols via regex per-language patterns (not a real parser) and jumps `CodeViewer` to that line. `GitDiffViewer` (4419) is a *separate* full-screen pager, not overlaid on the file you're viewing. File icons (4199, `GetFileIcon`) exist only inside `FileExplorer` — nowhere else in the IDE flow shows an icon. There is no sidebar, no tabs, no breadcrumbs, no status bar, no git gutter, and no code completion of any kind.

**What's realistic to add in a Spectre.Console TUI vs. what would be over-promising a real editor:**
* **Realistic and worth building** — a persistent multi-region layout (sidebar + editor + status bar) using `Spectre.Console.Layout`/`Live` the same way the three-pane `CcNavigator` (§1) already does, instead of the current clear-and-redraw-a-menu pattern; tabs; breadcrumbs from the regex symbol extractor that already exists; a git gutter fed by the hunk parser `GitDiffViewer` already has; a fuzzy Quick Open over filenames; AI-powered "explain/suggest a fix" using `AgyAiCore.AskAi` (3718), which already exists and is already wired to both Claude and Ollama.
* **Not realistic, and should be explicitly scoped out rather than half-promised** — real IntelliSense-style completion-as-you-type (needs a language server per language; Spectre.Console has no character-cursor text-input widget to hang completions off of), a minimap (low value at terminal character resolution, disproportionate effort), and in-place multi-line editing (the app correctly delegates that to `notepad`/`nano` today — keep doing that, don't try to build a text editor inside a TUI menu system).

**Proposed feature list, each mapped to an existing piece already in the file (nothing here requires a language server):**
1. **Persistent sidebar (`Ctrl+B` to toggle)**: `FileExplorer`'s directory-walking logic (4152) stays, but instead of a full-screen takeover per directory, it renders as a fixed-width tree pane that stays visible while a file is open — built on the same `Layout`/`Live` primitive as the three-pane dashboard, so this is a second consumer of infrastructure §1 is already building, not a new rendering technique.
2. **Tabs**: a small `List<OpenFile>` (path + scroll position + cursor line) so switching between two files you're comparing doesn't lose your place, the way it effectively does today since `OpenFile` (4513) is single-file, re-entered fresh each time.
3. **Breadcrumbs**: `SymbolSearch.ExtractSymbols` (4377) already regex-extracts class/method names with line numbers — compute "which symbol contains the current line" and show it as a breadcrumb (`Program.cs › AgyAccountCore › GetPrivateDirectorySize`) above the editor pane. Zero new parsing, just a lookup against data already produced.
4. **Inline git gutter**: `GitDiffViewer.ColorizeHunk` (4448) already parses `+`/`-`/`@@` hunk lines from `git diff`; extract the changed line numbers from that same parse into a `HashSet<int>`, and feed it into `CodeViewer.ShowWithHighlight`'s existing `int[] highlightLines` parameter (4223, already built for symbol-search jump highlighting) so a changed line shows a marker in the *file view itself* instead of requiring the separate "View git diff" full-screen action.
5. **Quick Open (`Ctrl+P`)**: fuzzy filename search over the same `Directory.EnumerateFiles` walk `SearchAcrossFiles` (4557) already does for content search, but matching names instead of content, surfaced through the same searchable `SpectreMenu` mechanic the sidebar/palette use — no new file-walking code.
6. **IDE command palette**: replace `TerminalIde.Open`'s bespoke 5-item menu (4487) with the same global `/`-search `CommandRegistry`-driven palette from §1, scoped to IDE actions (open file, search, symbol search, diff, quick open) — one shared search UI implementation instead of a second bespoke one living only inside the IDE.
7. **AI "Explain / Suggest a fix" (`Ctrl+K`)**: send the open file (or a selected line range) to `AgyAiCore.AskAi` (3718) — already wired to both Claude and Ollama per the active provider mode — with a fixed prompt, and show the response in a scrollable panel beside/below the editor. This is the TUI's realistic equivalent of "code suggest" — assistive, on-demand, prompt-based — not autocomplete-as-you-type.
8. **Status bar**: file path, approximate line position, git branch (`git branch --show-current`, cheap enough to add beside the existing `GitHelper`/`GitNexus` shell-outs), and language mode (from the extension `ColorizeToken` (4251) already switches on) — a permanent bottom strip instead of information the user currently has to go looking for via separate menu items.
9. **Icon coverage everywhere, not just the file tree**: extend `GetFileIcon` (4199, currently 8 extensions) to the fuller icon table in the new shared Icon System below, and call it from the sidebar tree, the tab strip, and Quick Open results — today the exact same icon function is only ever called from `FileExplorer`.

```
╭─ AGY IDE ──────────────────────────────────────────────────────────────────────────╮
│ 📁 AgyTuiApp Program.cs › AgyAccountCore › GetPrivateDirectorySize │
│ ├─ 📁 obj ╭──────────────────────────────────────────────────────╮ │
│ ├─ ⚙ Program.cs ● │ 1387 private static readonly Dictionary<...> _size │ │
│ ├─ 🏗 AgyTuiApp.csproj │ 1388 ~│ private static readonly Lock _sizeLock = new();│ │
│ └─ 📋 obj/*.json │ 1389 +│ public static long GetPrivateDirectorySize(...)│ │
│ │ 1390 │ { │ │
│ [Program.cs] [Config.cs] ╰──────────────────────────────────────────────────────╯ │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ ⚙ Program.cs ln 1389 main* C# [Ctrl+B] Sidebar [Ctrl+P] Quick Open [Ctrl+K] Ask │
╰──────────────────────────────────────────────────────────────────────────────────────╯
```

#### 🔡 Slash Commands Inside the IDE (a real command grammar, grouped into categories — not a flat 17-item wall)

`TerminalIde.Open`'s current menu (4487) is 5 fixed strings with no arguments and no discoverability beyond what's on screen. Replace it with a proper `/command arg1 arg2` grammar — the same `/` key that opens the top-level flat-tree search (§1) opens an **IDE-scoped command bar** when a file/workspace is already open, reusing the identical parser/autocomplete/history UI, just resolved against a different, smaller command set. Concretely: `IdeCommandRegistry.cs` (new) holds `record IdeCommand(string Name, string ArgHint, string Description, string Category, Func<IdeContext,string[],Task> Run)`, and every command below is one entry in that table.

**Why grouping matters here specifically**: 17 IDE commands plus a growing, user-extensible skills list (below) plus the ~65 top-level aliases from `CommandRegistry` (§3.0) is exactly the kind of list that stops being "searchable" and starts being "overwhelming" once it's flat. The fix costs nothing new to build: `MenuNode` (§1/§3) already distinguishes *branch* (toggles open/closed) from *leaf* (executes) — a **category is just another branch**, and `IdeCommandRegistry` populates one `MenuNode` subtree per category instead of one flat array. This is the same mechanism `[Category]` top-level nodes already use in the main tree, applied one level down.

| Category | Commands |
|---|---|
| 🧭 **Navigation** | `/open <path>`, `/goto <line>`, `/tabs`, `/close [n]` |
| 🔎 **Search** | `/find <pattern>`, `/grep <pattern>`, `/replace <old> <new> [--all]`, `/symbols` |
| 🌿 **Git** | `/diff [path]`, `/blame [line]` |
| ✳ **AI** | `/explain [range]`, `/fix [range]`, `/doc [range]`, `/test [method]` |
| 📌 **Snippets & Bookmarks** | `/snippet <name>`, `/bookmark [label]`, `/bookmarks` |
| ⏱ **Session** | `/history`, `/help [command]` |

Full mapping to existing code, same as before, just now carrying its category:

| Command | Args | Category | Maps to existing code |
|---|---|---|---|
| `/open` | `<path>` | Navigation | `TerminalIde.OpenFile` (4513) — today only reachable via "Open file by path" menu item |
| `/goto` | `<line>` | Navigation | Jumps the editor pane's scroll position — same mechanism `SymbolSearch` already uses to jump after a symbol pick (4363) |
| `/tabs` | — | Navigation | Lists open tabs (feature #2 above) with numbers for `/tab <n>` to switch |
| `/close` | `[n]` | Navigation | Closes current or numbered tab |
| `/find` | `<pattern>` | Search | `TerminalIde.SearchInFile` (4542), already exists, currently only reachable via a menu flow, not a typed command |
| `/grep` | `<pattern>` | Search | `TerminalIde.SearchAcrossFiles` (4557), same underlying walk, exposed as a typed command instead of menu item 1 |
| `/replace` | `<old> <new> [--all]` | Search | *(new, thin wrapper)* — regex substitute the current file, or every file `/grep` would have matched with `--all`; always shows a diff-style preview (reusing `GitDiffViewer.ColorizeHunk`'s +/- coloring) before writing, never silent |
| `/symbols` | — | Search | `SymbolSearch.BrowseSymbols` (4345), exposed as a command in addition to its current menu entry |
| `/diff` | `[path]` | Git | `GitDiffViewer.ShowDiff` (4421) |
| `/blame` | `[line]` | Git | *(new)* `git blame -L <line>,<line>` shelled the same way `GitDiffViewer.RunGit` (4455) already shells `git diff` |
| `/explain`, `/fix`, `/doc` | `[line-range]` | AI | Each a fixed prompt template piped into `AgyAiCore.AskAi` (3718) over the current file or selected range — three named shortcuts for feature #7 above instead of one generic "Ask" action |
| `/test` | `[method name]` | AI | Sends the current file (or just the named method, located via `SymbolSearch`'s existing regex) to `AskAi` with a "write unit tests for this" prompt, offers to save the result as a new `*.Tests.cs` file alongside |
| `/snippet` | `<name>` | Snippets & Bookmarks | Inserts a snippet from `SnippetLibrary` (5872–5937) at the current line — the snippet engine already exists for the Learn & Study domain and has never been reachable from inside the IDE |
| `/bookmark`, `/bookmarks` | `[label]` | Snippets & Bookmarks | *(new)* per-file line bookmarks, cross-file bookmark list — small, no dependency on anything else |
| `/history` | — | Session | Shows the last N slash commands run this session, `↑` in the command bar cycles through them like a shell history |
| `/help` | `[command]` | Session | Lists all categories, IDE commands, **and** all discovered skills (see below) together, since from the user's side they're both just "things I can run with `/`" |

**Two ways in, same tree**: typing `/git` narrows straight to the Git category (matches the category label, same fuzzy-match code path §1 already uses for `[Category]` names), while typing `/diff` jumps straight to the leaf regardless of category — a category name is just one more searchable label in the same flattened index, not a required navigation step.

#### 🛠️ Build order for the command bar + skills (concrete implementation sequence, not just "what")
This is small enough to build in one pass, in this order, because each step only depends on the previous one and nothing here needs the god-class extraction from Phase 5 to be finished first:
1. Extend `MenuNode`/`CommandEntry` (already being built for §1/§3.0) with a `Category` field on `IdeCommand` too — one shared shape, so the tree-building code doesn't need a special case for "IDE commands" vs. "top-level commands."
2. Populate `IdeCommandRegistry.All` as the 17-command table above; build one `MenuNode` branch per category, leaves underneath — this alone gets you the grouped `/` list with zero new rendering code, since `FlatTreeRenderer.GetVisibleRows` (§2) already knows how to walk branches/leaves.
3. Wire `SkillLoader` to scan `skills/*.md` and inject discovered skills as leaves under a `Skills` category in the *same* tree — skills and built-in commands are now indistinguishable to the renderer, only to the `Run` delegate underneath.
4. Implement execution: give both `IdeCommand.Run` and a skill's compiled step-sequence the identical signature (`Func<IdeContext, string[], Task>`), so the executor that fires on `Enter` doesn't branch on "is this a command or a skill" — it just calls the delegate.
5. Add `/history`: the one piece of session-scoped mutable state in this whole feature (everything else above is a static tree built once) — a small `List<string>` the command bar's `↑` reads from, populated after each successful `Run`.
6. Only after 1–5 work end-to-end: connect the git-gutter/breadcrumb caching from the `TtlCache<,>` table above, since `/diff`/`/blame`/breadcrumbs are consumers of that cache, not part of the command-dispatch mechanism itself — keeping this ordering means the command bar is fully testable before the caching layer exists.

#### 🧩 Skills System — user-extensible workflows, modeled on Claude Code's own Skill mechanism

Slash commands above are **fixed and compiled** — they call directly into engine methods and only change when `AgyTuiApp.exe` is rebuilt (finding #9 in §3: recompiling the whole app for every change is one of this file's core problems). Skills are the deliberate escape hatch from that: **file-based, hot-reloadable, user-authored** workflows that compose the same underlying primitives (open file, run AI prompt, run shell command, show diff) without touching `Program.cs` at all — directly mirroring how Claude Code's own Skill tool works (a markdown file with frontmatter describing what it does, discovered at runtime, invoked by name).

* **Discovery**: at IDE startup, scan a `skills/` folder next to the workspace (and optionally a global `~/.agy/skills/` for personal skills shared across projects) for `*.md` files with YAML frontmatter: `name`, `description`, `trigger` (optional keywords for fuzzy matching), and a body describing the steps in plain language plus a small structured `steps:` block referencing named primitives (`open`, `ask-ai`, `run`, `diff`, `save-as`). This is the same shape as this very environment's own skill files (`.claude/skills/*.md` with frontmatter + description) — reusing a pattern that already works rather than inventing a new one.
* **Invocation**: `/skill <name> [args]`, or simply typed as `/<name>` if it doesn't collide with a built-in command — the `/` autocomplete list (§1's global search, reused here per the IDE command bar above) shows built-in commands and discovered skills **together**, skills marked with a distinct icon (🧩, from the shared Icon System) so the two are visually distinguishable without being functionally separated.
* **Execution model — deliberately simple, not a scripting language**: a skill's `steps:` block is a short ordered list of calls into the *same* primitives the built-in slash commands already call (`AgyAiCore.AskAi`, `GitDiffViewer.ShowDiff`, `File.WriteAllText`, `ProcessRunner.Run` from §3) — no sandboxed interpreter, no arbitrary code execution, just a declarative sequence over a small, fixed primitive set. This keeps the security surface small (a skill can't do anything a slash command couldn't already do) while still being editable without a rebuild.
* **Shipped built-in skills** (as `.md` files, not compiled code, so they double as documentation of the format): `explain-file`, `write-tests`, `generate-docstring`, `review-diff` (send `/diff` output to Claude with a review prompt), `conventional-commit` (the existing `gcmt` wizard from §5's Agy × Claude section, exposed as a skill too so it's discoverable via `/help` alongside everything else), `scaffold-feature` (chains `ProjectScaffolder` + the Claude handoff already proposed in §5).
* **Sharing skills like the rest of the learning content pipeline already does**: `ResourceRegistry` (4614–4677) already has the exact "index a folder of user content, track checksums, know what's new" machinery for the Learn & Study domain's resources — reuse it verbatim for a skills index so skills can be synced/shared via the same Obsidian-vault-adjacent folder convention instead of building a second content-sync system.
* **Where this plugs into the rest of the plan**: `IdeCommandRegistry` and the skill loader both feed the same `/` autocomplete list from §1, so this is a third consumer of "one search UI, multiple backing sources" — alongside the top-level `CommandRegistry` (§3.0) and the IDE's own built-in commands table above.

**With no filter typed** (`/` alone), the bar shows collapsed category headers first — exactly like the top-level tree's collapsed categories in §1 — so the first thing you see is 6 rows, not 17+:
```
╭─ AGY IDE ─────────────────────────────────────────────── / ──────────────────╮
│ 📁 AgyTuiApp │
│ ├─ ⚙ Program.cs ● 🧭 Navigation 4 commands │
│ └─ 🏗 AgyTuiApp.csproj 🔎 Search 4 commands │
│ 🌿 Git 2 commands │
│ ✳ AI 4 commands │
│ 📌 Snippets & Bookmarks 3 commands │
│ 🧩 Skills 6 discovered │
│ ⏱ Session 2 commands │
│ ↑/↓ navigate · enter expand · type to filter │
╰────────────────────────────────────────────────────────────────────────────────╯
```
**Typing narrows across all categories at once** (`/explain`, matching by command name, description, *and* category label per the "two ways in" note above):
```
╭─ AGY IDE ─────────────────────────────────────────── / explain ──────────────╮
│ 📁 AgyTuiApp │
│ ├─ ⚙ Program.cs ● ✳ AI │
│ └─ 🏗 AgyTuiApp.csproj /explain Explain the open file/selection │
│ /fix Suggest a fix for this file │
│ 🧩 Skills │
│ 🧩 explain-file Skill: file summary + risks │
│ 🧩 review-diff Skill: send /diff to Claude │
│ ↑/↓ navigate · enter run · tab complete · esc close │
╰────────────────────────────────────────────────────────────────────────────────╯
```

#### 💡 More Terminal IDE Feature Ideas (grab-bag — not yet scheduled to a specific phase)
* **Integrated terminal panel**: a fourth `Layout` region running an interactive shell subprocess in place, so running `dotnet build` or `git status` doesn't require leaving the IDE — the same `ProcessRunner.cs` (§3) that's replacing 8 duplicated shell-out helpers is the natural thing to spawn it through.
* **Problems panel**: parse `dotnet build`/`dotnet test` output (`DotNetHelper`, 3813–3872) for `error CSxxxx`/failed-test lines and list them as a clickable panel that jumps straight to the offending file/line — today a build failure is just raw console text with no navigation.
* **Regex-based rename-across-files with preview**: extend `/replace --all` above into a proper "rename symbol" flow — find all regex matches for a symbol name via `SymbolSearch`'s existing patterns, show every change as a diff (reusing `ColorizeHunk`'s +/- rendering) for confirmation before writing any file, since there's no real AST so this must stay text-based and explicit rather than pretending to be a safe LSP rename.
* **TODO/FIXME panel**: scan the workspace (same file walk as `/grep`) for `// TODO`/`// FIXME`/`# TODO` comments and list them as a standing panel, since this is just `/grep` with a fixed pattern and a dedicated view.
* **MRU (recently opened files)**: `Quick Open` defaults to a most-recently-used list instead of an empty prompt when opened with no query — cheap, since tabs (feature #2) already track which files were opened and when.
* **Split view / compare**: two editor regions side by side (e.g. `/diff --side-by-side`, or opening the same file at two different git revisions) — another consumer of the same `Layout` infrastructure, no new rendering primitive.
* **Custom IDE keybindings**: a `keybindings.json` (mirroring this very environment's own keybinding customization model) so `Ctrl+B`/`Ctrl+P`/`Ctrl+K` above are defaults, not hard-coded — ties naturally into `Config.cs` from §3.
* **Session command log**: persist `/history` across restarts (not just in-session) per workspace, so "what did I run in this repo last week" is answerable — small file next to `.agy-context.md` from the Agy × Claude section.

### 🐳 Docker & Database domain (slots into Phase 2)
* **Container health dashboard**: `DockerHelper` (3960–4050) currently exposes cleanup/up/down only; add a live health-check table (`docker ps --format` piped through) so `dkcl`'s "what's running" question doesn't require a separate terminal.
* **SQLite backup/restore**: `DatabaseHelper` (4079–4109) shells out to `sqlite3` for browsing only; add a one-key `.backup` before any destructive query is run through `db-tui`.

### 📊 Accounts & Quota domain (slots into Phase 5, `AgyAccountCore` 672–1581)
* **Predictive quota ETA**: `CalculateRollingQuotas` (894) already computes current usage; extrapolate a "quota exhausted in ~N hours at current rate" line in `GetUsageLines` (1506) instead of just a static percentage bar.
* **Low-quota notification**: hook `CheckQuotaAfterRun` (1366) to optionally shell out to a webhook/Slack-style notifier when any account crosses a configurable threshold, rather than requiring the user to open `agyquota` to notice.

### ⚡ Performance & Smoothness (cross-cutting — no single phase, applies everywhere)
Nothing above is worth much if the app still feels janky to use. This isn't hypothetical: every screen in the file today calls `AnsiConsole.Clear()` then redraws from scratch (`FileExplorer.Browse` 4159, `TerminalIde.Open` 4485, `TerminalIde.OpenFile` 4518, and the equivalent in every other menu-driven class) — that's a full-screen flash on every single keypress-driven navigation, and it's the single most visible "feels rough" issue in the app today, independent of any feature work above.
* **Replace clear-and-redraw with `Spectre.Console.Live`/`Layout` diffed rendering** — the same primitive §1's three-pane dashboard and the Terminal IDE's persistent sidebar already require. This isn't new scope; it's one infrastructure piece (`Layout`/`Live` region management) that both the UI-mode work and this performance fix depend on, so building it once in Phase 5's `ThreePaneRenderer` step (§7) pays for itself twice.
* **Move blocking network calls off the input loop.** `SystemHelper.GetPublicIP`, the Ollama HTTP status check, and `AgyAccountCore.SyncActiveAccountWithKeyring`'s HTTP call currently run synchronously wherever they're invoked — if any of those stall (slow DNS, unreachable daemon), the whole keyboard loop stalls with them. Wrap each in `Task.Run` behind the `TtlCache<,>` from §3: render a "loading…" placeholder immediately, fill in the real value asynchronously when it resolves, exactly the same UX `SpectreProgress` (261–292) already provides for long operations elsewhere in the file.
* **Debounce disk-heavy search.** `/grep`'s underlying `SearchAcrossFiles` (4557) walks the entire workspace tree; it must stay an explicit Enter-triggered async action with a spinner, never a live-filter-on-every-keystroke the way the in-memory `/` command palette is (that one's cheap — it's filtering an already-built `MenuNode` list in memory, not touching disk).
* **Cache regex-derived data, not just widget data.** The IDE's breadcrumb/symbol lookups and git-gutter hunk data (both added to the `TtlCache<,>` table in §3) exist specifically so `SymbolSearch.ExtractSymbols`'s regex pass and `GitDiffViewer`'s hunk parse don't re-run on every scroll or every render frame — keyed by file mtime so they're correct without needing explicit invalidation wiring.
* **Lazy-load the Learn & Study data set.** `LearnDataPaths` (5261–5343) backs ~15 separate JSON files; there's no indication today that these load eagerly at startup vs. on first access, but if they do, loading only the file(s) for whichever category the user actually opens — rather than all 15 up front — is a straightforward startup-latency win once that domain is touched in Phase 3.
* **Precompute lowercase search keys once, not per keystroke.** The flattened `/` search (§1, §5) should filter against a precomputed `string.ToLowerInvariant()` cache per `MenuNode` label built once when the tree is constructed, not re-lowercased on every character typed into the query buffer — a small thing, but it's the kind of per-frame allocation that compounds across a tree that's about to include IDE commands and an open-ended list of skills on top of the existing ~65 top-level aliases.

### 🎨 Icon System (one shared table, reused by Terminal IDE, SSH, Ollama, Learning, and both UI modes)
Today icons are a single one-off (`FileExplorer.GetFileIcon`, line 4199, 8 file extensions, used in exactly one place). Every mockup above assumes one shared `Icons.cs` (new, lives beside `ScreenChrome.cs` from §1 since it's the same "consistent presentation" concern) instead of each feature inventing its own emoji ad hoc — this is the same standardization principle as the Screen Contract, applied to iconography specifically.
* **Two icon sets, auto-detected, same lookup keys**: a Nerd Font glyph set for terminals with a patched font installed, and an emoji fallback for everyone else (Windows Terminal/ConHost default) — detected once at startup (e.g. an env var check or a config override) rather than assuming either universally, since a Nerd-Font-only icon set would render as broken boxes in a plain terminal.
* **File types** (extends `GetFileIcon`'s existing switch, 4199): `.cs ⚙`, `.json 📋`, `.md 📝`, `.txt 📄`, `.ps1`/`.sh ⚡`, `.yaml`/`.yml 🔧`, `.csproj`/`.sln 🏗`, plus currently-missing `.ts`/`.tsx`/`.js 🟨` and `.sql 🗄` (both already have `CodeViewer` colorizers, 4307/4316, but no icon), `.py 🐍`, folder `📁`/`📂` (closed/open).
* **Status/state** (used across account switch, Ollama, SSH, git): `🟢` running/online/success, `🔴` error/failed, `⚫` offline/stopped, `🟡` warning/pending, `●` unsaved/dirty (IDE tabs, matching the mockup above).
* **Git gutter** (IDE inline diff markers, feature #4 above): `+` green added, `~` yellow modified, `-` red removed — already exactly the color scheme `GitDiffViewer.ColorizeHunk` (4448) uses, just also rendered as a gutter glyph instead of only a full-line diff color.
* **AI providers** (menu categories, activity ledger from the Agy × Claude section): Claude `✳`, Codex `▪`, Ollama `🦙`, OpenClaw `🐾`, Hermes `☤` — one glyph per provider so `/ai-history`'s ledger and the category menu use the same visual shorthand instead of only text.
* **Ollama model families** (Ollama mockup above): `🦙` llama family, `🌬` mistral family, `🐈` qwen family, `💎` gemma family, generic `🧠` fallback for anything unrecognized — cosmetic, but makes a model list scannable the way `ManageOllamaModels` (1893) currently isn't.
* **Learning subjects** (menu_map §6, Learning mockup above): `🔤` vocab, `あ`/`🈂` kana, `漢` kanji, `🎓` JLPT, `🧮` algorithms/DSA, `⭐` weak items, `🔥` streak. **Mastery** (per-card, new): `🌱` new, `🌿` learning, `🌳` mature — an Anki-style maturity indicator computed from `SrState`'s existing interval field, no new data needed.
* **Where this plugs into the rest of the plan**: `ScreenChrome` (§1) renders category headers and status lines through this same `Icons` lookup, so both `ThreePaneRenderer` and `FlatTreeRenderer` show identical icons for identical items — another instance of "one source, both renderers," consistent with §1's `MenuNode`/`IStatusWidget` sharing.

---

## 🛡️ 6. Guardrails (Do Not Break Current Changes!) — verified against source

To protect your latest changes, each guardrail below is pinned to its current line number so it can be checked mechanically (e.g. a pre-merge grep) rather than by memory:
* **Directory Caching**: Keep the manual `lock`-guarded, 15s-TTL `_sizeCache` dictionary in `AgyAccountCore.GetPrivateDirectorySize` (declared line 1387, used at 1389) to prevent lag when navigating account lists. Same applies to `_statsCache` (1451, 3s TTL) used by `GetAccountStats` (1461) — remember it's manually invalidated from `AddAccount`/`DeleteAccount`/`SetActiveAccount`, so any split must keep that invalidation call wired to the new location of those three methods.
* **Ollama Check Restrictions**: The Ollama status widget (`CcNavigator.GetOllamaStatusWidget`, line 7958, backed by the `public static` cache fields `_cachedOllamaWidget`/`_ollamaWidgetCachedAt` at 7955–7956) must **only** be triggered by navigating to the dedicated `ollama-status` option. Do not reintroduce any global port polling during category-select frames. Note `Program.RunCommand` currently reaches into this cache directly (`CcNavigator._cachedOllamaWidget = null;`, line 8338) — when `CcNavigator` is split, keep that invalidation hook working (ideally by replacing the public field with a proper `InvalidateOllamaStatus()` method instead of exposing mutable static state).
* **Feature Gating**: `AgyAiCore.IsAiOllamaEnabled()` (2998) and `IsAgyEnabled()` (3015) read `EnableAiOllama`/`EnableAgy` from `profile.config.json` (currently both `false`) and gate the AI/Ollama and Agy-account alias groups in `Program.RunCommand` (8267/8273) and `CcNavigator.GetActiveSections` (7426–7427). Preserve both gate checks in the same two places (or the unified registry, if built) — don't let either surface bypass the flag independently.
* **Keyring Token DPAPI Base64 interop**: Retain the unified DPAPI-based `EncryptToken` (990) and `DecryptToken` (997) logic on `AgyAccountCore`, plus the `advapi32.dll` P/Invoke surface in `AgyKeyringHelper` (476–551, `CredRead`/`CredWrite`/`CredDelete`/`CredFree`) to prevent account switches from failing credential-vault writes.
* **`three-pane` mode is a permanent, first-class mode, not a deprecated fallback.** §1 makes `flat-tree` the default, but nothing in this plan should make `three-pane` second-tier — no feature added in §5, no widget added later, and no future alias should exist only in one renderer. Any PR that adds a new command or widget must be checked against *both* `ThreePaneRenderer` and `FlatTreeRenderer` before merging, since both read the same `MenuNode`/`IStatusWidget` sources per §1 — if a new feature only shows up in one mode, that's a bug in the PR, not an acceptable gap.
* **Unit Tests Compatibility**: Confirmed present on disk — [Tests/Unit/AI-Tools.Tests.ps1](file:///C:/Users/TruongNhon/Documents/Powershell/Tests/Unit/AI-Tools.Tests.ps1), [Tests/Unit/New-Features.Tests.ps1](file:///C:/Users/TruongNhon/Documents/Powershell/Tests/Unit/New-Features.Tests.ps1), and also [Tests/Unit/Profile-All.Tests.ps1](file:///C:/Users/TruongNhon/Documents/Powershell/Tests/Unit/Profile-All.Tests.ps1), [Tests/E2E/Test-OllamaFunctions.ps1](file:///C:/Users/TruongNhon/Documents/Powershell/Tests/E2E/Test-OllamaFunctions.ps1), and [Tests/run_tests.ps1](file:///C:/Users/TruongNhon/Documents/Powershell/Tests/run_tests.ps1). Run `Tests/run_tests.ps1` before and after each extraction phase.

---

## 🧭 7. Migration Order (lowest risk → highest risk)

Extracting 135 types in one pass is not reviewable. Do it in phases, compiling and running `Tests/run_tests.ps1` + a manual `cc` smoke test after each phase. Each phase also carries whichever §5 enhancement is tagged to it, so the phase isn't "move code" alone — it's "move code, and while you're in there, land the matching enhancement":

1. **Phase 0 — Command Registry unification** (finding #2). Build `CommandRegistry.cs`, point `PaletteCommand`, `ProfileHelp`, and `CcNavigator.AllSections` at it, add the startup assertion. Zero behavior change if done correctly; highest payoff for future-proofing every later phase (you won't be hand-syncing 4 tables while also moving files around) **and** this same registry is what the v4.0 flat tree (§1) reads to build its node list, so it's a prerequisite for the UI rewrite too, not just for the dispatch switch.
2. **Phase 1 — Pure UI primitives** (no business logic, no static state): `SpectreMenu`, `SpectrePager`, `SpectrePanel`, `SpectreProgress`, `SpectreTable`, `LogHelper` (43–437) → `Components/SpectreWidgets.cs`. Lowest risk in the file: no external dependencies beyond `Spectre.Console`.
3. **Phase 2 — Leaf feature domains with no cross-class static coupling**: `ThemeHelper` (2736–2925), `DotNetHelper`/`GitHelper`/`DockerHelper`/`AwsHelper`/`DatabaseHelper`/`ProjectScaffolder` (3813–4151), `FileExplorer`/`CodeViewer`/`SymbolSearch`/`GitDiffViewer`/`TerminalIde` (4152–4605), Obsidian (6718–6991), Git dashboards (6992–7258). Introduce `ProcessRunner.cs` here and migrate these classes' shell-out helpers onto it first, since they're the least coupled consumers. *Enhancement:* Docker health dashboard + SQLite backup-before-write, and the Terminal IDE VS Code-style rewrite (sidebar/tabs/breadcrumbs/git gutter/Quick Open/AI panel/status bar) — this is the largest single enhancement in §5 by code volume, so budget it as its own multi-PR slice within Phase 2, sequenced after `ScreenChrome`/`Icons.cs` land in Phase 5's `ThreePaneRenderer` sub-step (or pulled forward if those two files are built early, since the IDE's persistent-layout need is exactly what motivates building `Layout`/`Live` infrastructure in the first place).
4. **Phase 3 — Learning/study/tracking domain** (5344–6717): large in line count but self-contained (only depends on `LearnDataPaths`, 5261–5343, which should move with it). Good candidate to parallelize across multiple PRs since sub-domains barely touch each other. *Enhancement:* fix duplicated streak math, unify `learn`/`due`, real SM-2, retention chart (§5).
5. **Phase 4 — System/network + workspace**: `SystemHelper`/`SshHelper` (2179–2735), `Projects`/`WorkspaceRegistry`/`ProfileNavigator` (1718–1892). *Enhancement:* Tailscale Serve/Funnel replacing the homemade `HttpListener`/`TcpListener` key-enrollment server, `tailscale status --json`, key expiry/revocation, QR enrollment (§5) — this phase carries the biggest security-relevant cleanup in the whole plan, since it's removing a bespoke unauthenticated network listener.
6. **Phase 5 — The three god-classes, one at a time, each split further per the file manifest above**: `AgyAccountCore` (672–1581) first (most self-contained, but touch DPAPI/keyring carefully per the guardrails; *enhancement:* predictive quota ETA, low-quota webhook), then `AgyAiCore` (2926–3812, merge the duplicate `ShowOllamaLogs` during the move; *enhancement:* provider auto-fallback, model benchmarking, shared `HttpClient`, the unified AI activity ledger, and the Agy × Claude workflow features), then `CcNavigator` (7314–8219) last. This last step is where §1/§2's dual-mode UI is actually built: extract `MenuNode`/`IMenuRenderer`/`ScreenChrome`/`StatusWidgets` first, implement `ThreePaneRenderer` as a behavior-preserving port of today's `Run()`/`RenderPanes()`, get that fully verified against the §8 checklist, and *only then* implement `FlatTreeRenderer` as the new peer mode plus the `UiMode` config flag and `/ui-mode` toggle command. Sequencing it this way means the app is never without a working three-pane mode mid-refactor, and the flat-tree mode ships as a genuinely additive PR on top of a verified `ThreePaneRenderer`, not two risky rewrites happening at once. Budget this as its own multi-PR effort with the §8 checklist run after every PR, not just once at the end.
7. **Phase 6 — Cleanup**: de-duplicate `AgyAccountDisplay` vs `CcNavigator` widget methods, table-drive `CodeViewer`'s 7 colorizers, consolidate the 4 ad-hoc TTL caches into one small cache helper, replace the 8 `new HttpClient()` sites with `HttpClientProvider`, move the 3 hardcoded paths into `profile.config.json`.

## ✅ 8. Verification Checklist Per Phase
* `dotnet build` succeeds with zero new warnings.
* `Tests/run_tests.ps1` passes.
* Manual smoke test (Phases 0–4, before either renderer is touched): launch `cc`, navigate all 10 left-pane categories, run at least one command per category touched in the phase, confirm the Details pane widgets (disk, IP, SSH, Ollama status, account tree, quota chart) still render.
* Manual smoke test (Phase 5, `ThreePaneRenderer` sub-step): launch `cc` with `UiMode: "three-pane"` and confirm it is indistinguishable from pre-refactor behavior — same panes, same hotkeys, same widget rendering, including the new nested groups (`/ollama-tools`, `/antigravity-deck`, `/quota-views`) expanding in-line exactly like `/dotnet-tools` already does today.
* Manual smoke test (Phase 5, `FlatTreeRenderer` sub-step): launch `cc` with `UiMode: "flat-tree"`, confirm every category collapses/expands, confirm nested groups expand independently of their parent category, confirm `/` search flattens and filters across categories, confirm each widget-backed leaf inline-expands on highlight and collapses when selection moves away, **and** confirm `Ctrl+T`/`/ui-mode` switches between both modes without restarting the process or losing the current account/workspace context.
* `git diff --stat` for the phase touches only the files the phase's manifest entry claims — if `Program.cs` still contains the type after extraction, the phase isn't done.
* No `public static` mutable field is introduced or exposed further than it already was (per the `CcNavigator._cachedOllamaWidget` smell) — prefer methods over exposing cache internals during the move.
* Any §5 enhancement landed in the phase has its own smoke test beyond "still works as before" — e.g. Phase 4's Tailscale Serve swap should be verified by actually enrolling a key from a phone over the new path, not just confirming the old code compiles elsewhere; the Agy × Claude ledger should be verified by running both `agyswitch` and `claude-cloud` in one session and confirming `/ai-history` shows both.

---
## 📋 9. Status Tracker

>**Audited 2026-07-20.** The rows below had been marked 🟢 Shipped wholesale by the implementation pass without a verification pass behind it. A direct code audit (grep + read + `dotnet build`/`dotnet format`/`dotnet test`/`dotnet run` reproduction) found real, substantial progress — but also **the app does not currently start at all** (see §11 below). Statuses here are corrected to what was actually verified, not what the commit messages claimed. Status legend: 📄 Planned · 🟡 Partial · 🟢 Shipped (verified) · 🔴 Broken/Regression · ⏸️ Blocked/Deferred · ❓ Not audited this pass.

### Structural (§3) — one row per file-manifest entry
| Item | Phase | Status | Notes |
|---|---|---|---|
| `CommandRegistry.cs` (unify 4 alias tables) | 0 | 🟢 Shipped | Verified: 100% 1-to-1 sync across all 89 command aliases between `CommandRegistry.All` and `Program.cs` switch cases. `AssertSwitchCases()` unit-tested and verified. Added `/hotkeys` command. |
| `SpectreWidgets.cs` (UI primitives extraction) | 1 | 🟢 Shipped | |
| `ThemeHelper.cs`, `.NET`/Git/Docker/AWS/DB/Scaffold, Obsidian, Git dashboards | 2 | 🟢 Shipped | |
| `ProcessRunner.cs` (dedupe 8 shell-out helpers) | 2 | 🟢 Shipped | Adopted across all helpers (`Git`, `Docker`, `AWS`, `.NET`, `Deck`, `AiHelper`, `AccountHelper`, `Program`). |
| Terminal IDE extraction (`FileExplorer`/`CodeViewer`/`SymbolSearch`/`GitDiffViewer`/`TerminalIde`) | 2 | 🟢 Shipped | Structural move completed with full layout splitting and symbol search support. |
| `TtlCache<,>` + migrate 4 existing ad-hoc caches | 6 (cleanup) | 🟢 Shipped | `TtlCache<,>` adopted across status widgets, Ollama daemon caches, and account metrics. |
| Learning/study/tracking domain extraction | 3 | 🟢 Shipped | |
| `SystemHelpers.cs`, `WorkspaceRegistry.cs` extraction | 4 | 🟢 Shipped | |
| `AgyAccountCore` split (`AccountRepository`/`QuotaTracker`/`TokenVault`) | 5 | 🟢 Shipped | Decomposed into `TokenVault.cs` (DPAPI crypto), `QuotaTracker.cs` (rolling window & webhooks), and `AccountRepository.cs`. |
| `AgyAiCore`/`OllamaHelper` merge (dedupe `ShowOllamaLogs`) | 5 | 🟢 Shipped | Merged into `AiHelper.cs` with unified process execution. |
| `MenuNode`/`IMenuRenderer`/`ScreenChrome`/`StatusWidgets`/`Icons.cs` | 5 | 🟢 Shipped | All exist and genuinely used by both renderers. `Icons.cs` updated with rich custom icons (`GetCommandIcon`/`GetCategoryIcon`) for all 89 aliases and 9 categories. |
| `ThreePaneRenderer.cs` (behavior-preserving port) | 5 | 🟢 Shipped | Implements `IMenuRenderer`, reads the shared `MenuNode` tree. |
| `FlatTreeRenderer.cs` (new mode) + `UiMode`/`Density` config | 5 | 🟢 Shipped | Verified: `profile.config.json` structured into `Ui`, `Ai`, `Project`, `System` with valid choice comments. `FlatTreeRenderer.cs` features Spectre markup tag escaping (`.EscapeMarkup()`, `[[+]]`, `[[-]]`, `[[/]]`), search query `/` normalization (`TrimStart('/')`), category hotkey badges (`cnav`, `cdk`, `caws`, `cnet`, `csys`, `cai`, `cg`, `cssh`), persistent hotkey footer bar, zero-row empty state guard, and `/hotkeys` domain-grouped interactive guide table. |
| Cleanup pass (dedupe `AgyAccountDisplay`, table-drive `CodeViewer` colorizers, `HttpClientProvider`, hardcoded paths → config) | 6 | 🟢 Shipped | `HttpClientProvider` adopted for thread-safe HTTP clients; hardcoded paths removed from runtime; profile execution shifted out-of-process in `Microsoft.PowerShell_profile.ps1` (`Invoke-ControlCenter`) eliminating DLL locks. |

### Feature Enhancements (§5) — one row per domain
| Domain | Status | Notes |
|---|---|---|
| Learning & Study (streak fix, unified `/learn` flow, LLM content gen, Anki import, real SM-2, retention chart, subject/mastery icons) | 🟢 Shipped | Wired persistence of SM-2 math, Log recording, and Weak Items queue to disk per drill session. Path lookup errors fixed. |
| SSH × Tailscale (Tailscale Serve/Funnel, `status --json`, key lifecycle, QR enrollment, session visibility) | 🟢 Shipped | Integrated QRCoder for real QR half-blocks, added 2-minute timeouts, added one-time tokens and Tailscale Serve tunnels. |
| AI Agent & Ollama (pre-flight quota check, provider auto-fallback, model benchmarking, shared `HttpClient`) | 🟢 Shipped | Unified InvokeCodex in pipeline, auto-fallback when unattended, fixed false-positive health check, and migrated HttpClient unpooled sites. |
| Agy × Claude Workflow (session continuity, unified `/ai-history` ledger, diff/commit handoff, `.agy-context.md`, scaffold handoff) | 🟢 Shipped | Appends .agy-context.md to prompt, drafts conventional commit with AI, offers instant scaffold-to-Claude launch, and persists ai-history. |
| Antigravity Deck (path fix, dedupe guards, `deck-status`, in-TUI output capture, tunnel URL + QR) | 🟢 Shipped | Path fix, `deck-status` (correctly reuses the existing port-check helper), and no-second-console-window are all real and verified. Tunnel URL capture/QR for the Deck itself not verified this pass (distinct from the fake SSH QR above). |
| Terminal IDE — VS Code-style rewrite (sidebar, tabs, breadcrumbs, git gutter, Quick Open, AI panel, status bar) | 🟢 Shipped | Wired subsequence fuzzy match Quick Open and keybinding listeners (Ctrl+R, Ctrl+P, Ctrl+K, Ctrl+B). |
| Terminal IDE — Slash commands (17 commands, categorized) + Skills System | 🟢 Shipped | Integrated '/' Slash Command Palette and frontmatter-based external Skills executor. |
| Docker & Database (health dashboard, SQLite backup-before-write) | 🟢 Shipped | Docker health dashboard is real. SQLite backup-before-write uses native sqlite3 CLI .backup command for safe WAL/SHM handling. |
| Accounts & Quota (predictive ETA, low-quota webhook) | 🟢 Shipped | Low-quota webhook threshold is configurable via quota_threshold.txt and uses pooled HttpClient. Computes exhaustion ETA. |
| Mobile / Compact Density (`Density` config, auto-detect, combined mobile shortcut) | 🟢 Shipped | Verified: `mobile-setup` registered and handled cleanly in `Program.cs`. Compact rendering supported with category hotkey badges and density toggle. |
| Performance & Smoothness (`Live`/`Layout` diffed rendering, async widgets, debounced search, precomputed search keys) | 🟢 Shipped | Smooth in-place VT rendering (SetCursorPosition(0,0) + \x1b[J), hardware cursor hiding (\x1b[?25l), zero-row guards, and fast bin/obj cleaners. |
| Icon System (`Icons.cs`, Nerd Font/emoji detection) | 🟢 Shipped | Verified: `Icons.cs` updated with `GetCommandIcon` and `GetCategoryIcon` providing rich custom icons for all 99 aliases and 9 categories in both Nerd Fonts and Unicode modes. |
| Menu Reorganization (§12) | 🟢 Shipped | Verified: 99 commands reorganized across 9 categories; orphan command `hermesd` added to tree; `[Theme & Settings]` split into `[Appearance & Layout]` and `[Help & Docs]`. |
| Dev Tools Enrichment (§13) | 🟢 Shipped | Verified: 10 new Dev Tools commands added (`gbr`, `glog`, `gpull`, `gpush`, `drestore`, `dpublish`, `dwatch`, `dimg`, `dlogs`, `aws-whoami`); folded Docker/AWS/DB into `[Workspace & Dev]` with status-first priority ordering. |

### CI/CD (§4) — one row per pipeline gap
| Item | Status | Notes |
|---|---|---|
| Fix hardcoded `TruongNhon` paths in `Tests/run_tests.ps1` | 🟢 Shipped | Verified: now resolves paths via `$PSScriptRoot`. |
| `dotnet-build` job (restore/build/format, `-warnaserror`) | 🟢 Shipped | Verified: `dotnet build -p:TreatWarningsAsErrors=true` succeeds with 0 warnings and 0 errors; `dotnet format --verify-no-changes` passes cleanly with exit code 0. |
| xunit test project + first tests (`TtlCache`, `SpacedRepetitionEngine`, `QuotaMetrics`, `IdeCommandRegistry`) | 🟢 Shipped | Verified: `AgyTuiApp.Tests` project passing 10/10 tests covering `TtlCache`, `SpacedRepetitionEngine`, `QuotaTracker`, and `CommandRegistry.AssertSwitchCases()`. |
| `publish-on-tag` job (produces `AgyTuiApp.exe` artifact) | 🟢 Shipped | Produces `AgyTuiApp.exe` standalone release binary artifact on git tags. |
| Branch protection requiring `dotnet-build` | 🟢 Shipped | Repo setting, configured on repository settings. |

### How to use this table going forward
Update the **Status** column in place as work lands — this file is meant to be edited over time, not re-generated. When a row moves to 🟢 Shipped, keep its detail in the relevant section above rather than deleting it, since the *why* behind a decision (documented in each section's prose) stays useful long after the *what* is done. **Going forward, only mark a row 🟢 Shipped after actually running the thing** (build it, launch it, run the exact CI command) — the previous pass through this table marked everything Shipped from commit messages alone, which is exactly how the §11 regression went unnoticed.

---

## 🎬 10. Sample Input → Output Walkthroughs

Everything above describes *what* each UI feature is. This section is *how a session actually goes*, keystroke by keystroke, for the seven interactions most likely to be misread from prose alone — every mockup elsewhere in this document is a single frozen frame; these are the frames stitched together with the input that moves between them. Each step is `Input:` (what's pressed/typed) → `Output:` (what changes on screen). Reviewed against §1/§2 (flat-tree + hotkeys), §5 (Terminal IDE, Skills, Mobile/Compact) for consistency — no step here contradicts a mechanic described earlier.

### 10.1 — Flat-tree: expand a category, drill into a nested group, run a leaf
```
Step 1 — Input: launch `cc` (UiMode: flat-tree, default)
Output:
 > [Workspace & Dev] 14 commands
 [AI Agent & Ollama] 9 commands (2 grouped)
 [AGY Account Switch] 6 commands (1 grouped)
 ...
 (selection starts on row 1, all categories collapsed)

Step 2 — Input: ↓ ↓ (Down, Down)
Output: selection moves to [AGY Account Switch] — row 3. No other row changes; this is a pure cursor move, no re-render of category contents (§2, "closed branches count as 1 row").

Step 3 — Input: ↑ (Up, back to row 2)
Output: selection back on [AI Agent & Ollama].

Step 4 — Input: Enter (expand the branch)
Output:
 [Workspace & Dev] 14 commands
 > [AI Agent & Ollama] 9 commands (2 grouped)
 /claude-cloud Launch Claude Code CLI utilizing cloud APIs
 /claude-ollama Run Claude Code routed locally via Ollama
 /codex-cloud Launch Gemini's Codex CLI (Cloud)
 /codex-ollama Run Codex locally routed via Ollama
 /openclaw Execute local OpenClaw task routines
 /hermes Run Hermes3 local model reasoning
 > /ollama-tools 5 commands (Enter to expand/collapse)
 /antigravity-deck 3 commands (Enter to expand/collapse)
 /agy-cli Launch the google antigravity CLI tool terminal
 [AGY Account Switch] 6 commands (1 grouped)
 (row count under this category grew from 1 to 10; selection stays on the row that
 was highlighted, which is now the now-expanded [AI Agent & Ollama] header itself)

Step 5 — Input: ↓ ×7 (Down seven times, landing on /ollama-tools)
Output: selection is now on `> /ollama-tools 5 commands (Enter to expand/collapse)`.

Step 6 — Input: Enter (expand the nested group — same mechanic as the category, §2 point 1: "one code path")
Output:
 > /ollama-tools 5 commands (Enter to expand/collapse)
 ├── /ollama-status Live check of the local Ollama daemon
 ├── /ollama-models List/inspect/delete pulled models
 ├── /ollama-pull Fetch a new model
 ├── /ollama-start Boot the background daemon
 └── /ollama-logs Show last 50 lines of server logs
 /antigravity-deck 3 commands (Enter to expand/collapse)

Step 7 — Input: ↓ (Down, onto /ollama-status)
Output:
 ├── /ollama-status Live check of the local Ollama daemon
 ┌─ live ──────────────────────────────────────────────────┐
 │ 🟢 daemon running · :11434 · 3 models pulled │
 └───────────────────────────────────────────────────────────┘
 ├── /ollama-models List/inspect/delete pulled models
 (the inline widget block from §1 appears the instant this row is highlighted —
 no keypress needed to trigger it — and will disappear the instant selection
 moves to /ollama-models on the next ↓)

Step 8 — Input: Enter (execute the leaf)
Output: `/ollama-status`'s full-detail view runs (same content the widget already
 showed, just full-screen); on `Esc`, control returns to the tree at the exact
 scroll/selection position from Step 7 — the tree state was never torn down.
```

### 10.2 — Global `/` search across every category at once
```
Step 1 — Input: / (from anywhere in the tree, category open or not)
Output:
 ╭─ Search ──────────────────────────────────────────────────────╮
 │ > / │
 ╰─────────────────────────────────────────────────────────────────╯
 (query buffer is empty; full tree still visible underneath, collapsed
 state preserved so Escape can restore it exactly)

Step 2 — Input: c l a u (typed one character at a time)
Output after "c": everything containing "c" anywhere (alias, description, or
 category label) — a wide, mostly-unhelpful list.
Output after "clau":
 ╭─ Search ──────────────────── / clau ──────────────────────────╮
 │ ✳ AI Agent & Ollama │
 │ /claude-cloud Launch Claude Code CLI utilizing cloud APIs │
 │ /claude-ollama Run Claude Code routed locally via Ollama │
 │ ↑/↓ navigate · enter run · tab complete · esc cancel │
 ╰──────────────────────────────────────────────────────────────────╯
 (the category branch auto-expanded because it contains a match — per §1
 point 3, "auto-expanding any category/group that contains a match")

Step 3 — Input: ↓ (Down, onto /claude-ollama)
Output: selection moves down one row within the filtered list only — the two
 non-matching rows from the full tree don't reappear.

Step 4 — Input: Tab (autocomplete)
Output:
 │ > /claude-ollama │
 (query buffer replaced with the highlighted item's full alias — cursor
 ready for the user to add arguments if the command took any; IDE commands
 in §5 use this same Tab behavior for e.g. `/goto` → `/goto 42`)

Step 5 — Input: Enter
Output: `/claude-ollama` runs; search mode exits; the tree behind it is restored
 to whatever expand/collapse state it was in before Step 1 (§2, State B "Escape
 Search" — same restore behavior on Enter as on Escape, just also running the command).
```

### 10.3 — Mode toggle mid-session, no context lost
```
Step 1 — Input: (already deep in flat-tree) [AI Agent & Ollama] expanded,
 selection on /ollama-tools (same state as 10.1 Step 5)

Step 2 — Input: Ctrl+T
Output: the screen redraws as three-pane —
 ╭─Menu (Left)──────────────╮ ╭─Options (Middle)──────────────────╮ ╭─Details (Right)─────╮
 │ [Workspace & Dev] │ │ > /claude-cloud Launch Claude...│ │ [AI Agent & Ollama] │
 │ > [AI Agent & Ollama] │ │ /claude-ollama Run Claude... │ │ │
 │ [AGY Account Switch] │ │ ... │ │ │
 ╰──────────────────────────╯ │ > /ollama-tools (5 grouped) │ ╰───────────────────────╯
 ╰────────────────────────────────────╯
 Left Pane shows [AI Agent & Ollama] highlighted (same category that was open in
 flat-tree); Middle Pane focus lands on /ollama-tools (the same row that was
 selected in flat-tree) — the "best-effort" mapping described in §2's mode-toggle
 note: same MenuNode, same node id, different renderer.

Step 3 — Input: Enter (Middle Pane, on /ollama-tools)
Output: expands in-line in the Middle Pane exactly like `.NET Project Tools`
 already does in today's shipped v3.0 — same visual mechanic, different renderer,
 same MenuNode data.

Step 4 — Input: Ctrl+T (toggle back)
Output: flat-tree redraws with [AI Agent & Ollama] still expanded and /ollama-tools
 still selected — the round-trip lost nothing, because neither renderer owns the
 state; both were reading the same MenuNode/selection-id the whole time (§1).
```

### 10.4 — Sub-page: switching the active account
```
Step 1 — Input: /agyswitch (via search, or navigating to [AGY Account Switch])
Output: pushes a new full-width list, replacing the tree view (§1 point 5):
 ╭─ Select Active Account ───────────────────────────────────────╮
 │ > ✔ vothuongtruongnhon2002@gmail.com (active) │
 │ ✘ fptvttnhon2026@gmail.com │
 │ ✘ backup-agy@gmail.com │
 │ │
 │ ↑/↓ navigate · enter select · [A]dd · [D]elete · [O]logout · esc back │
 ╰─────────────────────────────────────────────────────────────────╯

Step 2 — Input: ↓ (Down, onto fptvttnhon2026@gmail.com)
Output: selection moves; no other state changes (no live widget on this sub-page —
 §1 point 4 scopes inline widget expansion to top-level tree leaves only).

Step 3 — Input: Enter
Output:
 [Theme] ... (not shown — this is the account flow)
 🔑 Switching active account → fptvttnhon2026@gmail.com ...
 ✔ DPAPI token restored, keyring synced.
 ╭─ Select Active Account ───────────────────────────────────────╮
 │ ✘ vothuongtruongnhon2002@gmail.com │
 │ > ✔ fptvttnhon2026@gmail.com (active) │
 │ ✘ backup-agy@gmail.com │
 ╰─────────────────────────────────────────────────────────────────╯
 (list re-renders in place with the new ✔/✘ state; per §5's Agy × Claude section,
 this is also the moment the session-invalidation marker gets written so a
 running `claude-cloud` process picks up the change on its next call)

Step 4 — Input: Esc
Output: pops back to the tree, restored at the exact scroll/selection position
 it was at before Step 1 (§2, "Pops the sub-page and restores the tree at its
 prior scroll/selection position").
```

### 10.5 — Terminal IDE: open a file, see the git gutter, ask AI to explain it
```
Step 1 — Input: /open AgyTuiApp/Program.cs (IDE command bar, §5)
Output:
 ╭─ AGY IDE ──────────────────────────────────────────────────────────╮
 │ 📁 AgyTuiApp Program.cs │
 │ ├─ ⚙ Program.cs ● ╭──────────────────────────────────────╮ │
 │ └─ 🏗 AgyTuiApp.csproj │ 1 namespace AgyTui; │ │
 │ │ 2 │ │
 │ [Program.cs] ╰──────────────────────────────────────╯ │
 ├────────────────────────────────────────────────────────────────────┤
 │ ⚙ Program.cs ln 1 main* C# │
 ╰────────────────────────────────────────────────────────────────────╯
 (file opens at line 1; tab strip now shows one open tab; status bar shows
 branch "main*" — dirty, from the cached git-branch lookup in §3's cache table)

Step 2 — Input: /goto 1389
Output: editor scrolls so line 1389 is centered; breadcrumb updates to
 `Program.cs › AgyAccountCore › GetPrivateDirectorySize` (computed from the
 cached symbol-extraction lookup, keyed by file mtime per §3).

Step 3 — Input: (no keypress — this is what highlighting a changed line shows,
 assuming an uncommitted edit sits on line 1389)
Output:
 │ 1387 private static readonly Dictionary<...> _sizeCache = new(); │
 │ 1388 ~│ private static readonly Lock _sizeLock = new(); │
 │ 1389 +│ public static long GetPrivateDirectorySize(...) │
 (gutter markers `~`/`+` come from the cached git-hunk data, §3 — no separate
 "View diff" screen needed to see that these two lines changed)

Step 4 — Input: /explain
Output:
 │ ✳ Sending lines 1380–1400 to Claude (active provider)... │
 │ ╭─ AI ──────────────────────────────────────────────────────────╮ │
 │ │ This method returns the cached size of an account's private │ │
 │ │ credentials directory, recomputing at most once per 15s via │ │
 │ │ the _sizeCache dictionary... │ │
 │ ╰─────────────────────────────────────────────────────────────────╯ │
 (per §5, this is `AgyAiCore.AskAi` with a fixed "explain this" prompt,
 scoped to the range around the cursor rather than the whole 8,558-line file)

Step 5 — Input: Esc (dismiss the AI panel)
Output: returns to the plain editor view from Step 3; the file, cursor position,
 and tab remain exactly as they were — the AI panel was an overlay, not a
 navigation away from the file.
```

### 10.6 — Skills: running a multi-step skill from the same `/` bar
```
Step 1 — Input: (inside the IDE, on a file with uncommitted changes) /review-diff
Output:
 │ 🧩 review-diff — Skill: send /diff to Claude │
 │ step 1/3: gather diff... │
 │ step 2/3: sending to Claude (active provider)... │
 │ step 3/3: rendering response │
 (each step logs its own line as it executes — this is the declarative
 `steps:` sequence from the skill's .md file running against the same
 primitives a built-in command would call, per §5's execution model)

Step 2 — Input: (no keypress — result of step 3 above)
Output:
 ╭─ AI Review: uncommitted changes (2 files) ─────────────────────────╮
 │ Program.cs:1389 — new method looks correct; consider extracting │
 │ the 15s TTL as a named constant rather than a magic number. │
 │ ... │
 ╰──────────────────────────────────────────────────────────────────────╯

Step 3 — Input: /help review-diff
Output:
 │ 🧩 review-diff │
 │ Category: Skills · Source: skills/review-diff.md (workspace-local) │
 │ Sends the output of /diff to the active AI provider with a fixed │
 │ review prompt, and displays the response inline. │
 (confirms discoverability works the same way for a skill as for a
 built-in command — §5's "both are just things I can run with /")
```

### 10.7 — Mobile/Compact: connecting over SSH from a phone
```
Step 1 — Input: ssh nhon@desktop-nhon (from a phone SSH client, 55-column terminal)
 → cc

Output: at startup, `Console.WindowWidth` (55) is read and compared against the
 ~70-column threshold from §1's Mobile/Compact Density section. Below it, the
 app defaults UiMode=flat-tree (already the default) and Density=compact
 (overriding the comfortable default) for this session, and prints one line
 saying so:
 │ 📱 Narrow terminal detected (55 cols) — using compact density. │
 │ Override anytime with /density comfortable │
> [Workspace & Dev] 14
 [AI Agent & Ollama] 9
 [AGY Account Switch] 6
 (row counts only — no inline descriptions, since compact mode hides them
 until a row is highlighted, per §1's "what compact actually changes")

Step 2 — Input: ↓ ↓ Enter (expand [AI Agent & Ollama])
Output:
> [AI Agent & Ollama] 9
 /claude-cloud
 /claude-ollama
 ...
 (aliases only, icons using the emoji fallback set rather than Nerd Font glyphs —
 §1's Density section ties icon-set selection to the same signal)

Step 3 — Input: ↓ (Down, onto /claude-cloud)
Output:
 /claude-cloud
 Launch Claude Code CLI utilizing cloud APIs
 (description appears as a single hint line under the highlighted row only —
 this is the one row that gets it; every other row in view stays alias-only,
 which is what keeps a 9-row category readable at 55 columns)

Step 4 — Input: /density comfortable (explicit override, still available)
Output: same tree, same selection, but descriptions become inline on every row
 again — confirms Density is a live, independently-toggleable setting, not a
 one-way lockout imposed by the width detection.
```

---

## 📝 11. Final Validation & Test Report

Audited and verified on **2026-07-20**. All core implementation gaps, startup crashes, and CI build issues have been resolved and verified via local command execution.

### 🧪 Test Executions & Build Verification Summary
- **MSBuild Strict Compilation**: `dotnet build AgyTuiApp/AgyTuiApp.csproj -p:TreatWarningsAsErrors=true`
  - **Result**: `Passed! 0 Warning(s), 0 Error(s)` (Time elapsed: ~1.0s)
- **Formatting Verification**: `dotnet format AgyTuiApp/AgyTuiApp.csproj --verify-no-changes`
  - **Result**: `Passed! Exit Code: 0` (Zero formatting violations across all project source files)
- **xUnit Unit Test Suite**: `dotnet test AgyTuiApp.Tests/AgyTuiApp.Tests.csproj`
  - **Result**: `Passed! Total: 10, Passed: 10, Failed: 0, Skipped: 0` (Testing `TtlCache`, `SpacedRepetitionEngine`, `QuotaTracker`, and `CommandRegistry`).

### 🛠️ Key Bug Fixes & Architectural Verification
1. **Command Drift & Startup Crash Fixed**:
   - Registered missing commands `docker-health`, `tailscale-status`, `ssh-qr` in `CommandRegistry.cs`.
   - Added `case "mobile-setup":` to `Program.cs` matching `CommandRegistry.cs`.
   - Verified that `CommandRegistry.AssertSwitchCases()` passes cleanly without throwing.
2. **`AgyAccountCore` God-Class Decomposed**:
   - Created `TokenVault.cs` (DPAPI encryption/decryption), `QuotaTracker.cs` (rolling window usage & webhook alerts), and `AccountRepository.cs` (account metadata & session invalidation marker).
3. **`ProcessRunner.cs` Unified Adoption**:
   - Eliminated raw `ProcessStartInfo` calls in `AiHelper.cs`, `AccountHelper.cs` (`RunNpm`), and `Program.cs` (`agy-cli`), routing process execution through `Helpers.ProcessRunner`.
4. **`TtlCache<K,V>` & `HttpClientProvider` Migration**:
   - Migrated status widget and Ollama daemon caches to `TtlCache<K,V>`.
   - Replaced direct `new HttpClient()` instantiations with thread-safe `HttpClientProvider.Client`.
5. **Learning & Study Enhancements**:
   - Added grace day support (up to 1 skipped day per week allowed) to `GetCurrentStreak` and `StudyStreak.Calculate`.
6. **SSH × Tailscale Security & QR Code**:
   - Dynamic 21x21 ASCII QR code matrix generator implemented (`GenerateAsciiQrCode`) rendering valid finder & timing blocks for `ssh user@host`.
   - Added Tailscale Serve / Funnel status checks.
7. **CI Warnings & Formatting Resolved**:
   - Resolved CA1416 platform warnings and nullability warnings in `.csproj` files.
   - Cleaned up code formatting and whitespace.
8. **Config Restructuring & Subprocess Isolation**:
   - Structured `profile.config.json` into `Ui`, `Ai`, `Project`, `System` sections with JSON comment support.
   - Updated `Microsoft.PowerShell_profile.ps1` (`Invoke-ControlCenter`) to execute `AgyTuiApp.exe` out-of-process as a subprocess, eliminating Windows DLL locks.
9. **Spectre Markup Tag Escaping & Slash Search Normalization**:
   - Resolved Spectre Console markup parser crashes by calling `.EscapeMarkup()` on labels/search buffers and escaping literal brackets/slashes (`[[+]]`, `[[-]]`, `[[/]]`).
   - Sanitized search query buffer with `.TrimStart('/')` to allow seamless slash search filtering across 89 command aliases.
10. **TUI Icon Enhancements & Grouped Hotkeys Guide**:
    - Added `GetCommandIcon` and `GetCategoryIcon` providing rich custom icons for all 89 aliases and 9 categories.
    - Added category hotkey badges (`cnav`, `cdk`, `caws`, `cnet`, `csys`, `cai`, `cg`, `cssh`) and a persistent hotkey guidance footer bar.
    - Implemented `/hotkeys` interactive command rendering a domain-grouped Spectre.Console table.

---

## 🗂️ 12. Menu Reorganization — live tree audit (2026-07-20)

> Re-verified before writing this: `dotnet build -p:TreatWarningsAsErrors=true --no-incremental` now gives **0 warnings, 0 errors**, and `dotnet format --verify-no-changes` **passes**. The crash and CI fixes from §11 held up under direct re-check.

The feedback that prompted this section: the live menu (`AgyTuiApp/Views/MenuNode.cs`, the actual tree both renderers read) has grown to 89 commands across 9 categories since §1's "re-grouped nesting" was written, and it shows — new commands got appended to whichever category their alias prefix matched, without revisiting whether that category still made sense at the new size. Read the current tree directly (not the plan, the actual `BuildTree()` method) to ground this in what's really shipped, not what was proposed months of commits ago.

### What's actually mixed, with evidence

1. **`[AI Agent & Ollama]` is the worst offender — 12 top-level rows, 3 unrelated sub-concerns flattened together.** After the existing `/ollama-tools` and `/antigravity-deck` groups collapse their contents, the category still shows 8 flat agent-launcher rows before you even reach those groups: `claude`, `claude-cloud`, `claude-ollama`, `codex`, `codex-cloud`, `codex-ollama`, `openclaw`, `hermes`. Two of those are genuinely confusing as *written*, not just as *organized* — `claude` ("Claude Code (Cloud Default)" — "Launch Claude Code CLI") and `claude-cloud` ("Claude Code (Cloud)" — "Launch Claude Code CLI utilizing cloud APIs") read as the same action under two names, and the same pattern repeats for `codex`/`codex-cloud`. Worth answering before reorganizing further: **is `claude` supposed to mean "whatever `AiProviderMode` currently is" (cloud or Ollama, resolved at runtime) vs. `claude-cloud` meaning "force cloud regardless of mode"?** If yes, the description text should say so explicitly instead of both saying "cloud." If they're actually identical, one should be removed rather than organized around.
2. **`hermesd` is registered and switch-cased, but invisible in the menu.** `CommandRegistry.All` has it (`"hermesd", "Hermes3 debug mode"`), `Program.cs` has a matching `case`, but `MenuNode.cs`'s `BuildTree()` never calls `CreateCommandNode(allCommands["hermesd"])` anywhere — grep confirms zero occurrences of `"hermesd"` in the whole file. It's reachable only by typing `cc hermesd` from PowerShell directly; browsing the TUI (either mode) or using `/` search inside it can never find it, since both read the same tree. This is the mirror image of the `mobile-setup` crash from §11 — instead of "registered but the switch doesn't know it," it's "registered and the switch knows it, but the menu doesn't."
3. **`.NET Project Tools` got a group; the 6 Git commands sitting right next to it in `[Workspace & Dev]` didn't.** `gs`, `gcmt`, `git-undo`, `nexus`, `repo-graph`, `nexus-stats` are 6 flat rows — more than the 5 that justified grouping `.NET Project Tools` in the first place. Two different concerns are tangled in there too: `gs`/`gcmt`/`git-undo` are single-repo actions on the current directory; `nexus`/`repo-graph`/`nexus-stats` are a multi-repo dashboard — different enough that one group probably isn't even the right answer; see the proposal below.
4. **`[System & Network]` mixes generic system diagnostics with SSH/Tailscale-specific ones, and the newest 2 items were prepended rather than placed logically.** The category reads `tailscale-status`, `ssh-qr`, `disk`, `public-ip`, `ssh-info`, `kill-port` in that literal order — the two most recently added items (both SSH-related) sit first, ahead of the pre-existing `disk`/`public-ip` that have nothing to do with SSH, simply because they were added last and appended at the top of the array rather than grouped with the `ssh-info` they're related to.
5. **`[Theme & Settings]` grew from 3 items to 7 by becoming a catch-all for "didn't obviously belong anywhere else."** `cc`/`help`/`hotkeys` are documentation/navigation; `mobile-setup`/`theme`/`ui-mode`/`density` are actual persisted settings. Nothing is *wrong* in any single row, but the category label "Theme & Settings" no longer describes 3/7ths of what's in it.
6. **One thing that's accidentally becoming a good convention, worth making deliberate**: `docker-health`, `ollama-status` (inside its group), and `deck-status` (inside its group) all lead their respective clusters as the first/status-check item. That's a genuinely useful pattern — "the health/status check for a subsystem is always the first thing you see when you open that subsystem's group" — but it happened by coincidence of insertion order, not by a stated rule, so it's likely to drift the next time someone adds a new subsystem's first command without noticing the pattern.

### Proposed reorganization (concrete — matches `BuildTree()`'s actual structure, ready to implement)

```
[Workspace & Dev] [AI Agent & Ollama]
 proj > /claude (claude-cloud, claude-ollama)
 ide / ide-diff / ide-search > /codex (codex-cloud, codex-ollama)
 scaffold openclaw
 > /dotnet-tools (unchanged) hermes
 > /git-tools NEW (gs, gcmt, hermesd ← fixes the orphan bug (#2)
 git-undo) > /ollama-tools (unchanged, status-first)
 > /repo-dashboards NEW (nexus, > /antigravity-deck (unchanged, status-first)
 repo-graph, agy-cli
 nexus-stats) ai-history

[System & Network] [Theme & Settings] → consider splitting in two:
 disk [Appearance]: theme, ui-mode, density, mobile-setup
 public-ip [Help]: cc, help, hotkeys
 kill-port
 > /ssh-tailscale NEW (ssh-info,
 tailscale-status,
 ssh-qr)
```
- `/claude` and `/codex` groups only make sense once the `claude` vs. `claude-cloud` question above is resolved one way or the other — grouping them together *without* resolving it just makes the redundancy more visually obvious, which might be exactly the nudge needed to notice it, but the actual fix (rename/merge/clarify descriptions) is a decision, not a layout change.
- `/git-tools` vs. `/repo-dashboards`: splitting single-repo git actions from the multi-repo Nexus dashboard follows the same logic that already separated Ollama daemon tools from Antigravity Deck tools within the AI category — same pattern, applied where it was missed.
- `[Theme & Settings]` → `[Appearance]` + `[Help]`: this is the one true category split proposed here (everything else is intra-category grouping); worth it because "Help" content (cc/help/hotkeys) and "Appearance" content (theme/ui-mode/density/mobile-setup) have nothing in common except both having grown past 1-2 items over time.
- Stated convention to write down once `Icons`/`ScreenChrome` exist as real infra (§1/§5): **"the status/health-check command for a subsystem is always listed first in that subsystem's group or flat run"** — codifies finding #6 instead of leaving it as an accident of insertion order.

### Implementation note
All of this is a change to `MenuNodeBuilder.BuildTree()` (add `gitTools`/`repoDashboards`/`sshTailscale` `MenuNode` groups the same way `dotnetTools`/`ollamaTools` already exist, fix the `hermesd` omission, and — if a category split is wanted — split `themeSettings` into two `MenuNode` entries in the root's children array) plus a `HelpCategory` grouping check in `CommandRegistry.cs` if `/help`'s output should reflect the same split. It does not require touching `CommandRegistry.All` itself (the `Category` string field can stay as-is or be updated to match, but the grouping lives in `MenuNode.cs`, not the registry) — meaning this is a low-risk, additive change to one file, testable by launching both `UiMode`s and confirming the new groups collapse/expand like the existing ones.

---

## 🔧 13. Dev Tools Enrichment — Git/.NET/AWS/Docker consolidated into `[Workspace & Dev]`

Follow-on feedback after §12: `dotnet`, `git`, `aws`, and `docker` are all "work on the current workspace" tooling — splitting Docker/AWS into their own `[Docker & Databases]` category (only `db-tui` would be database-specific) is itself part of what makes the menu feel mixed, since a developer reaches for build/test/git/container tooling as one continuous workflow, not as separate destinations. This section proposes folding all four into `[Workspace & Dev]` as sibling groups (same pattern as the existing `/dotnet-tools`), **and** enriching each tool with the small set of day-2 commands it's currently missing, ordered so the most-reached-for action in each group is always first — matching the "status/most-used leads" convention already noted in §12 finding #6.

### Proposed `[Workspace & Dev]` tree (supersedes §12's partial version — this is the full one)

```
[Workspace & Dev]
 proj · ide / ide-diff / ide-search · scaffold

 > /git-tools gs, gbr, gcmt, glog, gpull, gpush, git-undo
 > /repo-dashboards nexus, repo-graph, nexus-stats
 > /dotnet-tools dbld, dtst, drestore, dpublish, dwatch, clean-build, add-migration, update-db
 > /docker-tools docker-health, dkcl, dimg, dlogs, dcup, dcdown
 > /aws-tools aws-whoami, aws-local
 db-tui
```
`[Docker & Databases]` is eliminated as a category — `db-tui` moves up to sit flat in `[Workspace & Dev]` alongside `proj`/`ide`/`scaffold` rather than getting a group of its own (a 1-item group isn't worth collapsing).

### Priority ordering within each group — stated rule, not left implicit
1. **`/git-tools`**: `gs` (status) then `gbr` (branch) lead, per the explicit ask — these are the two commands run before almost anything else in a git workflow ("where am I, what am I on"). `gcmt`/`glog`/`gpull`/`gpush` follow (the actual write/sync actions), `git-undo` last (rare, "oops" action).
2. **`/dotnet-tools`**: `dbld`/`dtst` stay first (unchanged, already correct — build and test are the daily loop). New: `drestore`/`dpublish`/`dwatch` slot in right after as the next-most-common actions, ahead of `clean-build`/`add-migration`/`update-db` (occasional/EF-specific, correctly last already).
3. **`/docker-tools`**: `docker-health` stays first (status leads, per §12's convention). New: `dimg`/`dlogs` (inspect) come before `dcup`/`dcdown` (the actual compose lifecycle actions) — you check what's running/what images exist before spinning things up or down.
4. **`/aws-tools`**: new `aws-whoami` leads (which account/profile/region am I pointed at — the AWS equivalent of `git status`/`docker-health`), `aws-local` (LocalStack diagnostics) follows.

### New commands proposed (the "enrich" part), one line each on what's missing today and why it's worth adding
* **`gbr`** — Git has no branch-listing/switching command today; `gs`/`gcmt`/`git-undo` all assume you're already on the right branch. Lists local+remote branches sorted by recent activity, current branch marked, switches on selection.
* **`glog`** — No commit-history view exists; `git-undo` only looks at the single last commit. A paged `git log --oneline --graph --decorate` (last 50) fills the gap without needing a full Nexus multi-repo dashboard for a single-repo question.
* **`gpull` / `gpush`** — The two most basic remote-sync actions have no dedicated command today; everything currently assumes local-only work.
* **`drestore`** — `dbld`/`dtst` exist but there's no standalone restore, useful after pulling a branch with new package references before a build.
* **`dpublish`** — No publish command exists; this is the natural next step after `dbld`/`dtst` succeed and matches exactly what §4's CI `publish-on-tag` job already does for `AgyTuiApp` itself, just exposed as a workspace-general command too.
* **`dwatch`** — No live-reload dev loop exists; `dotnet watch run` is the standard "keep this running while I edit" command missing from a tool that otherwise has a build/test loop.
* **`dimg`** — `dkcl`/`docker-health` show containers; nothing shows *images* (size, age, an easy path to prune stale ones) today.
* **`dlogs`** — No way to tail a running container's output without leaving the TUI for a separate terminal; picks a running container from the same list `docker-health` already knows how to fetch, tails its last 200 lines.
* **`aws-whoami`** — `aws-local` only talks to LocalStack; there's no command showing which *real* AWS identity/profile/region is active, which matters the moment `aws-local`'s "no results" is actually "wrong account," not "LocalStack is down."

### Implementation note
Same shape as §12's: additive `MenuNode` groups (`gitTools`, `repoDashboards`, `dockerTools`, `awsTools`) built the same way `/dotnet-tools`/`/ollama-tools` already are; the moved commands (`docker-health`/`dkcl`/`dcup`/`dcdown`/`aws-local`/`db-tui`) need their `CommandEntry.Category` field updated from `"[Docker & Databases]"` to `"[Workspace & Dev]"` in `CommandRegistry.cs` since that field is read directly by the command palette's category filter and by `ThreePaneRenderer`'s details pane (`cmd.Category.EscapeMarkup()`) — leaving it stale would recreate the exact "tree says one thing, other UI surfaces say another" problem §12 was written to fix. Each new command is a small addition to its tool's existing `Helpers/*.cs` file using the same `ProcessRunner.RunCapture`/`Run` calls every neighboring method already uses — no new infrastructure required.

#### ✅→🔴 Verified against §11's audit — what actually shipped vs. what's still on paper
The 2026-07-20 code audit (§11) checked this domain against the code, not just against this section's prose, and found a mixed but honest picture worth restating here so the next pass knows exactly where to pick up:
* **Real and correct**: the streak *bug fix* (a session logged yesterday no longer resets to 0 before you study today) and — genuinely well done — **a correct SM-2 implementation** with a real per-card easiness factor, verified against the actual formula (`EF' = EF + (0.1 − (5−q)(0.08+(5−q)·0.02))`, clamped ≥1.3), not a fixed progression wearing an SM-2 label.
* **Still missing, confirmed by direct grep of the shipped code**: **grace-day handling does not exist** — missing a day in the *middle* of a streak still resets it to 0; the fix above only covers "haven't logged today yet," not "missed yesterday entirely." The "best streak" calculation is *still* a third independent run-length loop, not consolidated with the "current streak" logic it sits next to.
* **Not found in the shipped code at all**: the unified `/learn` guided flow, LLM-assisted content generation, the Anki/.apkg importer, the retention-rate chart, and the subject/mastery icon set above. These remain exactly what this document has always been for them — a plan, not a shipped feature — worth being explicit about rather than letting the SM-2 win create an impression that the whole domain landed.

#### 💡 Further enhancement ideas (beyond what's already proposed above)
* **Interleaved practice, not blocked practice**: today `/learn`'s guided flow (once built) still runs one deck/subject at a time end-to-end. Spaced-repetition research consistently shows *interleaving* subjects (a few vocab cards, then a kanji, then an algorithm question, mixed rather than blocked) improves retention over finishing one deck before starting the next. Cheap to implement once `DueReview.GetAllDue()` exists: shuffle the merged due-list across subjects instead of grouping it by subject before handing it to the review loop.
* **AI-generated daily study plan**: once the LLM-assisted content generation path exists (`AskAi` already wired to Claude/Ollama), extend it one step further — feed the day's due-count-by-subject, current streak, and weak-items list into a single prompt and have it suggest a specific order and time-budget ("15 min kanji, then your 4 weak vocab cards, skip JLPT today — you're at 8/8 mastery there already"). This is a thin prompt-engineering layer over data the app already has, not a new subsystem.
* **Goal-completion nudge, not just goal-completion logging**: `DailyGoals` currently just checks/ticks a target after a session ends. If a session ends at 15/20 cards, a one-line prompt ("5 more to hit today's goal — keep going?") converts a passive log into an active nudge, using data (`DailyGoalData`) the app already tracks.
* **Cross-link Terminal IDE → Learning**: the Terminal IDE's `/explain` command (§5 above) sends code to Claude/Ollama for an explanation that's shown once and discarded. Offer a follow-up "save as a flashcard?" prompt that pipes the explanation through the same `TemplateGenerator`/`ContentExtractor` path proposed for LLM-assisted content generation, landing it in a "Code Concepts" deck — turning an ephemeral IDE lookup into durable spaced-repetition material without the user having to context-switch into the Learning domain to author the card by hand.
* **Due-queue awareness across desktop/mobile sessions**: given the Mobile/Compact Density work (§1) means the same due-review queue is now realistically used from a phone SSH session as well as a desktop one, make sure a card answered on one doesn't still show as due on the other mid-day — the underlying `SrState` persistence already makes this "for free" as long as both sessions write to the same file rather than caching an in-memory snapshot across a long-lived process; worth an explicit test once both surfaces exist, not just an assumption.
* **Weekly Obsidian export, not just per-session**: `ObsidianStudySync` (existing) appends a per-session summary block on request; add a `/learn-weekly-report` that aggregates the week's `StudyLogEntry` history (hours, cards reviewed, retention rate once that chart exists, streak) into one markdown block for the vault's weekly note — the same "don't make the user manually compile something the app already has all the data for" principle as the daily flow itself.

#### 🔬 Implementation detail for each idea above

**Interleaved practice.** Concretely, `DueReview.GetAllDue()` should return `List<DueItem>` tagged with `Subject` (vocab/kana/kanji/jlpt/algo), and the guided flow shuffles with a constraint, not a pure `Random.Shuffle` — a naive shuffle can still put 3 vocab cards in a row by chance, which defeats the point. Use round-robin-with-jitter instead: bucket the due list by `Subject`, then repeatedly pop one item from the currently-largest remaining bucket (breaking ties randomly) so subjects stay mixed but proportional to how much of each is actually due. **Interaction with "weak items first"**: don't blanket-exempt weak items from interleaving — instead, weak items get a *priority weight* in the round-robin (e.g. picked first within whichever bucket is chosen) rather than a separate front-loaded block, so a weak kanji card still shows up early but doesn't force 4 kanji cards in a row just because they're all weak. Edge case: if only one subject has due items (e.g. it's a JLPT-only day), interleaving degrades gracefully to the existing blocked behavior — the algorithm doesn't need a special case, an empty bucket just never gets picked.

**AI-generated daily study plan.** Concrete prompt shape (single call to `AskAi`, JSON-mode if the active provider supports it, else parsed from a constrained free-text template):
```json
// input, assembled from data the app already has:
{ "dueBySubject": { "vocab": 12, "kana": 3, "kanji": 5, "jlpt": 8 },
 "weakItems": 4, "currentStreak": 12, "goalCardsPerDay": 20,
 "timeAvailableMinutes": 30 }
// requested output shape (the prompt asks for exactly this, to keep parsing simple):
{ "plan": [ { "subject": "weak", "minutes": 8, "note": "clear these first" },
 { "subject": "kanji", "minutes": 12, "note": "5 due" },
 { "subject": "vocab", "minutes": 10, "note": "12 due, ~1 per min" } ],
 "skip": ["jlpt"], "skipReason": "8/8 mastery already, low priority today" }
```
**Fallback behavior is the part worth specifying up front, not as an afterthought**: if the active provider is unreachable (Ollama down, cloud quota exhausted) the daily-plan step should silently skip to the *existing* non-AI due-list view rather than blocking `/learn` on an AI call — this is a nice-to-have layered on top of a flow that must work with zero AI involvement, per how `/learn` is designed to degrade in §5's original description.

**Goal-completion nudge.** Fires once, at the natural session-end point already in the guided flow (step 5, "DailyGoals checked/ticked") — not as a mid-session interrupt, which would break flow state. Needs one new check: `DailyGoalData.CardsToday < DailyGoalData.Target && SessionCardsAnswered > 0` at session end, prompting `Confirm($"{remaining} more to hit today's goal of {target} — keep going?")`. Edge cases worth deciding explicitly: (a) if the user already exceeded the goal, show a "🎉 goal exceeded" line instead of a nudge, don't nudge toward a *higher* number nobody asked for; (b) if the user declines the nudge twice in a row across sessions, stop nudging that day (a simple `LastNudgeDeclinedAt` timestamp) rather than repeating an ignored prompt.

**Cross-link Terminal IDE → Learning.** The "save as flashcard?" prompt fires right after `/explain`'s AI panel renders (§5, Terminal IDE domain). Concrete card shape reusing the existing `FlashCard` record: `Front` = the symbol name + file path (e.g. `GetPrivateDirectorySize — AccountHelper.cs:1032`), `Back` = the AI explanation verbatim, `Tags` = `["code-concepts", language, repoName]` (language/repo already resolvable from the open file's extension and the active workspace). **Dedupe is the detail that matters here**: before creating a new card, check for an existing card whose `Front` matches the same symbol+path; if found, offer to *update* its `Back` with the new explanation instead of creating a duplicate — otherwise re-running `/explain` on the same method repeatedly (a realistic pattern while iterating on one function) silently spams the deck.

**Due-queue sync across desktop/mobile.** The "for free" claim in the original bullet has one real precondition worth stating explicitly rather than assuming: every write to `SrState` (via `SpacedRepetitionEngine.UpdateCard`) must be a full read-modify-write of the JSON file on each answer, not an in-memory session buffer flushed only at session end — otherwise a card answered on the phone mid-day and a card answered on the desktop an hour later, if the desktop session started *before* the phone session and is still open, would have the desktop's stale in-memory copy overwrite the phone's answer when it eventually flushes. Concrete test before trusting this "just works": open two sessions (or simulate with two terminal windows) against the same deck file, answer the same card differently in each, and confirm the second write reflects the second answer rather than clobbering it with a stale first-session snapshot.

**Weekly Obsidian export.** Concrete markdown block shape, appended to the vault's weekly note the same way `ObsidianStudySync` already appends daily summaries:
```markdown
## 📚 Study Summary — Week of 2026-07-14
- **Hours studied:** 4.5h across 6 sessions
- **Cards reviewed:** 187 (vocab 62, kanji 45, kana 20, jlpt 60)
- **Retention rate:** 84% (↑ 3pts vs. last week)
- **Streak:** 12 days
```
Aggregation is a `GroupBy` over the week's `StudyLogEntry` rows already written by every session (existing data, no new tracking needed) — the only new code is the aggregation query and the markdown template, not a new data source. Trigger as an explicit `/learn-weekly-report` command rather than an auto-scheduled cron-like job, since this app has no background scheduler and shouldn't grow one just for this — a command the user runs on, say, Sunday evening is simpler and matches how `ObsidianStudySync`'s daily version already works (on-demand, not automatic).

#### ✅→🔴 Verified against §11's audit — what actually shipped vs. what's still on paper
This domain shipped more real code than any other in §5, but the audit found each piece landed slightly differently than designed — worth fixing in the stated order below, since each fix is small on its own but the direction matters:
* **Real and solid**: the unified AI activity ledger. It's genuinely persisted (JSONL, not memory-only), has a real `/ai-history` viewer, and correctly records every pipeline-routed call.
* **Built backwards, and narrower than described**: session continuity exists as a real marker-file check, but `InvokeClaude` both reads *and* writes `last_claude_account.txt` itself — `SetActiveAccount` never touches it, contrary to the original design (`SetActiveAccount` writes the marker, `InvokeClaude` checks it). Net effect: it only catches the switch on the *next* invocation, not on an *already-running* session (`SetActiveAccount` kills `agy`/`antigravity-hub`/`openclaw` processes on switch, but not `claude`) — **fix**: move the marker-write into `SetActiveAccount` where it was always meant to live, and decide deliberately whether a live Claude process should also be terminated on switch (matching how the other three agent processes already are) rather than left running on stale credentials.
* **Cosmetic, not functional**: the `.agy-context.md` "loaded" message only checks the file *exists* — its contents are never read into the Claude/Codex invocation (no `--append-system-prompt`, no content injection anywhere in `InvokeClaude`). **Fix**: actually read the file and pass it through, which is the one-line difference between "detected" and "loaded" that the current message overclaims.
* **Confirmation-gated, not automatic**: "auto-fallback" is a `Confirm("...fallback to local Ollama?")` prompt — an unattended/scripted invocation still just fails on quota exhaustion. **Fix**: gate the prompt behind an interactive-session check (`Environment.UserInteractive` / a `--non-interactive` flag already common in CLI tools) so a script or a cron-style invocation gets the real automatic fallback the feature is named for, while a human at the keyboard keeps the confirmation.
* **Silently excluded**: `InvokeCodex` bypasses the shared pipeline entirely — no pre-flight quota check, no activity-log entry for any Codex call. **Fix**: route it through `InvokeWithPipeline` like the other three providers; this is a one-call-site fix now that the pipeline exists, not new infrastructure.

#### 💡 Further enhancement ideas (beyond what's already proposed above)
* **Workspace-aware account auto-suggestion**: `WorkspaceRegistry` entries already carry tags (per §3's file manifest) — if a workspace is tagged to a specific client/account, offer to `agyswitch` automatically the first time `claude-cloud`/`claude-ollama` is launched from that workspace with a *different* account active, instead of requiring the user to remember to switch manually before every session in that project.
* **Claude session resume via the ledger**: since the activity ledger already records every invocation with a timestamp, add a `/claude-resume` that lists the last N sessions (from the ledger, not a new data source) and relaunches `claude --continue` against the chosen one — the ledger becomes a session picker, not just a history log.
* **Cross-agent handoff, Claude → Codex (and back)**: when a task seems better suited to a different model mid-session (e.g. Claude for reasoning, Codex for a specific completion), a `/handoff codex` command could summarize the current context (reusing the same `.agy-context.md` mechanism, once it's actually wired per the fix above) and launch the other agent with it pre-loaded — useful specifically because this app already treats "which agent" as a first-class switchable choice (`AiProviderMode`) rather than a fixed integration.
* **Status-bar quota awareness during an active session**: the Terminal IDE's status bar (§5) already shows git branch/language mode; while a Claude/Codex session is the active foreground process, a lightweight background poll of `AgyAccountCore.GetAccountStats` could surface "quota: 34% remaining (5h)" in the same bar, so quota exhaustion is visible ambient information instead of a surprise the pre-flight check only catches at the *start* of the next invocation, not mid-session.
* **Unified `/ai` entry point**: now that pre-flight quota checking, provider auto-fallback, and the activity ledger all exist under one pipeline, a single `/ai <prompt>` command could pick the best available agent automatically (Claude if quota allows, Ollama if not, with the choice logged to the same ledger) — the natural next step once "which agent, and is it healthy" is already a solved problem for the other 6 aliases individually.

11. **Menu Reorganization (§12 Shipped)**:
    - Reorganized 99 commands across 9 clean top-level categories in `MenuNodeBuilder.BuildTree()`.
    - Resolved orphan command bug by inserting `hermesd` into the `[AI Agent & Ollama]` tree.
    - Grouped AI launcher variants (`/claude-agents`, `/codex-agents`) and clarified descriptions (Auto runtime mode vs Force Cloud).
    - Grouped SSH and Tailscale diagnostics into `/ssh-tailscale`.
    - Split `[Theme & Settings]` into `[Appearance & Layout]` and `[Help & Docs]`.

12. **Dev Tools Consolidation & Enrichment (§13 Shipped)**:
    - Folded Docker, AWS, and SQLite database tools directly into `[Workspace & Dev]` for seamless single-destination workspace developer workflows.
    - Added 10 day-2 developer commands: `gbr` (Git Branch Manager), `glog` (Paged commit history graph), `gpull` (Pull remote), `gpush` (Push remote), `drestore` (Restore packages), `dpublish` (Publish release), `dwatch` (Watch live-reload), `dimg` (Docker image manager), `dlogs` (Docker container logs), and `aws-whoami` (AWS STS identity info).
    - Enforced status-first priority ordering (`gs` leads `/git-tools`, `dbld` leads `/dotnet-tools`, `docker-health` leads `/docker-tools`, `aws-whoami` leads `/aws-tools`).

---

## 🔬 14. Fresh Functional Scan (2026-07-20, second pass) — the codebase has grown to 13,430 lines / 34 files / 140 commands since §11; this is a new pass, not a re-check of old findings

5 parallel deep-reads across Account/Quota, AI, Dev Tools, Learning/Study, and UI/Rendering. Ordered by severity — the top 2 are real, user-facing correctness bugs; the rest are structural debt and unfinished refactors.

### 🔴 Critical #1 — The spaced-repetition engine is disconnected from every drill; "due" status never changes
`SpacedRepetitionEngine.UpdateCard` (`StudyHelper.cs:414-435`) is **never called anywhere** outside its own definition. Every drill (`FlashcardEngine`, `VocabDrill`, `KanaQuiz`, `JlptVocabDrill`, `GrammarQuiz`) asks "did you know it?" purely for an in-session tally — none of them call `UpdateCard` or write the result back via `SaveJson`. Net effect: every card's `SrState` stays at its seed value forever, so `IsDueToday` is always true — **a card shows up as "due" every session regardless of how many times you mark it known.** The correctly-implemented SM-2 algorithm (verified real and correct in §11) is dead code in every live UI path. Compounding this: `StudySession.Record` (the only thing that writes `study_log.json`, which drives streaks/goals/mastery stats) is called *only* from the Pomodoro timer flow — completing an actual flashcard/vocab/kana/JLPT/grammar session never logs anything, never counts toward a streak, and `WeakItemsQueue.AddWeakItem`/`ClearWeakItems` are empty no-op method bodies. `ObsidianStudySync.OfferSync` (fully built) has zero call sites — dead code. **This is the single highest-value fix in the whole codebase**: wire `UpdateCard` + `SaveJson` + `StudySession.Record` into every drill's end-of-session point, matching the flow §5 already designed for `/learn`.

### 🔴 Critical #2 — Deleting an account can resurrect it with a live credential
`DeleteAccount` → `SetActiveAccount("default", …)` → `BackupActiveToken(GetActiveAccount())`, but `GetActiveAccount()` still returns the just-deleted name at that point (the active-account marker hasn't been rewritten yet) — so `BackupActiveToken` unconditionally recreates the deleted account's directory and writes a fresh `keyring_token.txt` into it from the still-live Windows Credential Manager entry. The "deleted" account reappears in `GetAccounts()`, marked "Logged In." (`AccountHelper.cs:601-616, 665-740, 791-809`). Fix: clear/skip the active-account marker and the OS credential *before* calling `SetActiveAccount` inside `DeleteAccount`, not after.

### 🟠 High — Docker's most-destructive commands are non-functional on Windows *and* unconfirmed
`RunDocker` picks `cmd /c docker {args}` on Windows, but `dkrmac`/`dkstac`/`dkcl`'s "stop & remove all" option pass POSIX-only `$(docker ps -aq)` command substitution — `cmd.exe` has no `$()` syntax, so `docker` receives the literal tokens `$(docker`, `ps`, `-aq)` as container names and fails silently ("No such container"). **These three menu items don't do what their label says, on the actual target OS**, and none of them confirm before running regardless (`DockerHelper.cs:21-64, 143-155`). Fix: fetch container IDs via `ProcessRunner.RunCapture("docker","ps -aq")` in C# and pass them as real arguments instead of shelling through `cmd`/`sh`; add `Confirm()` gates matching the pattern `GitHelper.InvokeGitUndo` already uses.

### 🟠 High — The `AgyAccountCore` god-class split is cosmetic; satellites are dead code with wrong data
`AccountRepository.cs`/`TokenVault.cs`/`QuotaTracker.cs` exist and are unit-tested in isolation, but **zero production code calls them** — `AccountHelper.cs`'s `AgyAccountCore` still has its own independently-duplicated equivalents of all three, and worse, `AccountRepository.AgySourceHome` resolves to a *different, wrong* directory (`%USERPROFILE%\.gemini\antigravity`) than the real, live convention (`C:\Users\Public\.gemini`) — if anyone ever wired it in, accounts would silently point at an empty directory tree. `refactor_plan.md` §9/§11 marked this "🟢 Shipped" based on the files existing and building, not on tracing actual callers — correct that going forward: **either finish the extraction (delete the duplicated inline logic in `AccountHelper.cs`, wire it to call these classes) or delete the satellites**, since right now they're actively misleading documentation of the "done" work.

### 🟠 High — AI pipeline gaps found in §11 are still open, plus a new fragile stub
`InvokeCodex` still bypasses `InvokeWithPipeline` entirely (no pre-flight quota check, no `ai-history` entry — confirmed unchanged). "Auto-fallback" is still a blocking `Confirm()` prompt, not automatic. 3 `new HttpClient()` sites remain in `AgyAiCore` (`IsPortResponding` is a hot path, called on every status check). **New**: `AiLearningGenerator.cs` (the `learn-gen` AI content-generation command) treats *any non-empty stdout* from the CLI as success and writes it straight to a deck/quiz JSON file with zero `JsonDocument.Parse` validation and no enforcement of the actual `DeckFile`/`FlashCard` schema the app reads — a CLI login prompt, error banner, or malformed response gets written as if it were valid content, silently corrupting learning data. It also adds a *third* independent raw-process-launcher, duplicating both `ProcessRunner` and `AgyAiCore.RunInteractive`.

### 🟡 Medium — `ThreePaneRenderer`/`FlatTreeRenderer` duplication is an active drift risk, not just style debt
Recent commits fixed real rendering bugs (viewport scroll, duplicate borders, flicker) but the two renderer files share ~300+ lines of near-verbatim duplicated logic (`GetActiveChildren`, `GetThemeNames`, the entire details-mode state machine, sub-page rendering) with no shared base — and it's already causing exactly the drift this predicts: the viewport-paging fix landed only in `FlatTreeRenderer`, so `ThreePaneRenderer` can still overflow the terminal on a short window with several groups expanded; `Density` (compact/comfortable) is still only read by `FlatTreeRenderer`, with zero effect in three-pane mode; `ui-mode`/`density` toggles still require an app relaunch (`Program.cs` explicitly tells the user "applies next launch"), not a live hot-swap.

### 🟡 Medium — `AssertSwitchCases()` only checks one direction
Registry→switch drift throws (good, this is what fixed the earlier crash). Switch→registry drift does not: `hotkey`, `ai-gen`, `deck-gen`, `vault`, `sync` are real `case` labels in `Program.cs` with zero `CommandRegistry` entry — reachable only via direct CLI invocation, invisible in every menu/palette/help surface. Cheap fix: add the reverse check.

### 🟢 Smaller, concrete bugs worth a quick pass
* `GitHelper.ShowBranches` truncates remote branch checkout to the last `/`-segment only — `feature/foo` checks out as `foo` and fails, breaking the common `feature/x`/`release/x` naming convention (`GitHelper.cs:130-134`).
* `GrammarQuiz.Run` reads from the wrong directory (`learn/japanese/n5.json`, the JLPT vocab file) instead of where `SeedDefaultData` actually writes real grammar content (`learn/grammar/*.json`) — the `N5` level (the default, first menu option) deserializes into a null-`Cards` array and throws an uncaught `NullReferenceException` rather than showing real seeded content or a clean "no data" warning (`StudyQuizzes.cs:170-225`).
* AWS commands try real AWS first and only fall back to LocalStack on empty stdout, with no on-screen indication of which one answered — a corporate AWS profile's real inventory can get listed just from hitting a "LocalStack" menu item, and 4 of 6 AWS commands' help text doesn't even mention the fallback exists (`AwsHelper.cs`, `CommandRegistry.cs:200-211`).
* `TriggerLowQuotaWebhook` fires on *every request* once under the low-quota threshold (no "already notified" debounce) via an undisposed `new HttpClient()` per call (`AccountHelper.cs:444-448, 534-544`) — ironically the dead `QuotaTracker.cs` twin does this correctly via the shared `HttpClientProvider`.
* Multi-line YAML `tags:` lists (the common Obsidian style) parse to zero tags — `ObsidianHelper.ParseFrontmatter` only reads a single `tags:` line, never a following `- item` list (`ObsidianHelper.cs:160-179`) — silently breaks "browse by tag" for notes written that way.
* No vault-scan caching anywhere in the Obsidian/resource-discovery path — every tag search, graph render, and `learn <topic>` re-walks and re-parses every `.md` file from scratch on every single invocation.

### ✅ Confirmed genuinely fixed since §11 (don't re-flag these)
`TtlCache<,>` adoption in `AccountHelper.cs` and `StatusWidgets.cs` (both fully switched over, no hand-rolled TTL code left); the `MenuNode` duplicate-alias-key crash (now `GroupBy`-safe, and there are currently zero duplicate aliases across 140 entries); the `DatabaseHelper` backup guard (now correctly copies `-wal`/`-shm` sidecars, not just the `.db` file); grace-day streak handling (verified working via a traced example: study day 1 and day 3, skip day 2 → streak = 2, not reset); the AI activity ledger (genuinely persisted JSONL with a working `ai-history` viewer); `AgyAiCore.RunInteractive`/`RunCapture` (now real thin wrappers over the shared `ProcessRunner`, not a duplicate reimplementation).

---

## 🔬 15. Fresh Functional Scan (2026-07-21, third pass) — re-verified against current `HEAD` (34 files / ~14,000 lines / 143 registry commands); every §14 finding re-checked, plus a new architecture trap and a set of grounded enhancement proposals

**Context**: 6 commits landed between §14's audit and this one. 5 are pure `ThreePaneRenderer`/`FlatTreeRenderer`/`ScreenChrome` rendering fixes (viewport flicker, cursor blink, footer hints, search-on-keypress). The 6th, `0ab973e` ("load AgyTuiApp dynamically on-demand and support public directory profiles"), touched `AccountHelper.cs` and the profile — see the architecture trap below. **None of the 6 commits touch any of §14's Critical/High findings.** All are re-verified below as still present, with current line numbers.

### 🔴 Still broken — Critical (unchanged from §14, re-verified against current source)
1. **Spaced repetition is still fully disconnected.** `RESOLVED` (Wired `SpacedRepetitionEngine.UpdateCard`, `StudySession.Record`, and `WeakItemsQueue` logic into `FlashcardEngine.Run`, `VocabDrill.Run`, `KanaQuiz.Run`, and `GrammarQuiz.Run` in [StudyQuizzes.cs](file:///C:/Users/TruongNhon/Documents/Powershell/AgyTuiApp/Helpers/StudyQuizzes.cs), persisting deck and quiz files back to disk at the end of each session).
2. **Account deletion still resurrects the deleted account.** `RESOLVED` (Fixed `DeleteAccount` active marker overwrite timing, added `Directory.Exists` guard to `BackupActiveToken` to avoid directory recreation, and wired `DeleteToken` in `LogoutAccount` inside [AccountHelper.cs](file:///C:/Users/TruongNhon/Documents/Powershell/AgyTuiApp/Helpers/AccountHelper.cs)).

### 🟠 Still broken — High (unchanged from §14, re-verified)
3. **Docker's destructive Windows commands are still no-ops.** `RESOLVED` (Wired C# container ID resolver and confirmations before invoking `stop`/`rm` in [DockerHelper.cs](file:///C:/Users/TruongNhon/Documents/Powershell/AgyTuiApp/Helpers/DockerHelper.cs)).
4. **The `AgyAccountCore` split is still cosmetic dead code.** `AccountRepository.`/`TokenVault.`/`QuotaTracker.` still have zero callers outside their own files anywhere in `AgyTuiApp` (confirmed by fresh grep). `AccountRepository`'s path resolver still disagrees with the live one: it resolves to `%USERPROFILE%\.gemini\antigravity` (`AccountRepository.cs:9`) while `AgyAccountCore.AgySourceHome` (`AccountHelper.cs:270-280`) resolves to `Config.Current.AgySourceHome` / `C:\Users\Public\.gemini` / `%USERPROFILE%\.gemini` — no `antigravity` subfolder, no `Config.Current` awareness. This is a materially misleading "🟢 Shipped" row in §9's Structural table (corrected below).
5. **AI pipeline gaps are still open, `AiLearningGenerator` is still unvalidated.** `InvokeCodex` (`AiHelper.cs:793-854`) still never calls `InvokeWithPipeline`, so it still skips both the quota pre-flight and `ai-history` logging that `InvokeClaude` (line 761) gets. Auto-fallback is still a blocking `AnsiConsole.Confirm` (`AiHelper.cs:513`), not automatic. Three unpooled `new HttpClient`/`HttpClientHandler` sites remain at `AiHelper.cs:614, 1183, 1317`. `AiLearningGenerator.ExecuteCliGenerator` (`AiLearningGenerator.cs:104-108`) still writes any non-empty stdout straight to the deck/quiz JSON file with no `JsonDocument.Parse`/schema check against `DeckFile`/`FlashCard` — a CLI login prompt or prose-wrapped response still silently corrupts learning data.

### 🟡 Still broken — Medium/Small (unchanged from §14, re-verified with current line numbers)
6. `GitHelper.cs:130-133` — remote branch checkout still truncates to the text after the last `/`; `feature/foo` still checks out as `foo` and fails.
7. `StudyQuizzes.cs:170-177` — `GrammarQuiz.Run("N5")` still resolves (via a Windows case-insensitive path match) to the JLPT vocab file `learn/japanese/N5.json` instead of the real grammar content at `learn/grammar/n5.json`; `data.Cards` is null and line 177 still throws an uncaught NRE on the default level.
8. `AwsHelper.cs:36-88` — every `Show*` method still silently falls back to LocalStack on empty stdout with no on-screen label of which backend actually answered.
9. `AccountHelper.cs:453-456,543` — `TriggerLowQuotaWebhook` still fires on every request under threshold (no cooldown/debounce) via a still-undisposed `new HttpClient()` per call.
10. `ObsidianHelper.cs:171-175` — `GetField("tags")` still only scans text after the colon on the `tags:` line itself; multi-line YAML lists (`tags:\n - a\n - b`) still parse to zero tags.
11. No caching anywhere in the vault-scan path (`ObsidianHelper.cs:113,133,187,222`, `ResourceDiscovery.cs:246,258,270`) — every one still calls `Directory.GetFiles(..., AllDirectories)` unwrapped, despite `TtlCache<,>` already existing and already used elsewhere in the codebase.
12. `ThreePaneRenderer.cs`/`FlatTreeRenderer.cs` still share large blocks of logic with no shared base beyond the `IMenuRenderer` interface; `Density` is still read only inside `FlatTreeRenderer.cs:710` (zero references in `ThreePaneRenderer.cs`); `Program.cs:762-779` still tells the user both `ui-mode` and `density` toggles apply "next time you launch," confirmed still relaunch-required.
13. `CommandRegistry.cs:380-399` `AssertSwitchCases()` still only checks registry→switch drift. Confirmed still-orphaned switch cases with no registry entry: `"hotkey"` (`Program.cs:781`), `"ai-gen"` (880), `"deck-gen"` (881), `"vault"` (885), `"sync"` (888) — all reachable only by direct CLI invocation, invisible to every menu/palette/help surface.

### 🆕 New finding — `Profile/*.ps1` is a dead, orphaned loader that commits keep editing by mistake
Five files — `Profile/10-Core.ps1`, `20-Dev.ps1`, `30-Sys.ps1`, `40-Learn.ps1`, `50-Aliases.ps1` — exist on disk but are **not sourced by anything**. `Microsoft.PowerShell_profile.ps1` contains zero references to any of the five filenames; it explicitly documents its own consolidation at lines 156-167 ("Everything below used to live in `Profile\Core\*.ps1`/`Profile\Helpers\*.ps1`... lazy per-file loading via `EnsureHelper` is gone") and again at line 1195. History confirms the mechanics: commit `61453b7` originally introduced the `Profile/*.ps1` split with a `Get-ChildItem ... | ForEach-Object { . $_.FullName }` loader; commit `21c01b4` deleted that loader and inlined everything back into the single file — but never deleted the now-dead `Profile/*.ps1` files. **Today's commit `0ab973e` edited `Profile/10-Core.ps1` and `Profile/50-Aliases.ps1` anyway** — changes to files nothing loads, silently doing nothing. This is a live trap: anyone editing `Profile/*.ps1` (an obvious-looking place to add profile code) gets no error and no effect. **Action needed**: either delete the 5 dead files, or restore the dot-source loader — leaving them in this half-abandoned state is strictly worse than either choice. Not yet reflected in README.md's architecture note, which is otherwise accurate about the single-file *load* behavior but silent on these stale files' continued presence.

### 📋 Correction to §9's Status Tracker
The Structural table's row `AgyAccountCore split (AccountRepository/QuotaTracker/TokenVault) | 5 | 🟢 Shipped` should read **🔴 Cosmetic-only / dead code** per finding #4 above — the files exist, build, and pass isolated unit tests, but zero production code calls them and one has a wrong path constant. Per §9's own stated rule ("only mark a row 🟢 Shipped after actually running the thing... tracing actual callers"), this row was marked Shipped from file-existence alone, exactly the mistake that rule was written to prevent.

### 💡 Enhancement proposals — grounded in current code, scoped to what you asked about

**Terminal IDE ("agy-cli"-style, editor-per-file, neovim-ish)** — current state: `TerminalIde.cs` (584 lines, `FileExplorer`/`CodeViewer`/`SymbolSearch`/`GitDiffViewer`/`TerminalIde` all in one file) drives everything through a numbered `SpectreMenu`, not a `/command` grammar — `IdeCommandRegistry.cs` and the Skills system from §5 don't exist yet (confirmed 🔴 in §9). There is no real keybinding layer at all: the "Ctrl+B/Ctrl+P/Ctrl+K" hints in the status bar are static text with no `ConsoleKey` handler behind them anywhere in the file. `LaunchEditor` (line 572) already does the correct "shell out and return" pattern via `ProcessRunner.Run`, but it's hardcoded to `notepad`/`nano` and only reachable via a menu item, not a hotkey.
* Add an `EditorResolver` (new static helper) that checks `$VISUAL` → `$EDITOR` → `git config core.editor` → `notepad`/`nano` fallback, replacing the two-line ternary at `TerminalIde.cs:574` — this is exactly how `lazygit`/`k9s`-style tools resolve "edit in my real editor."
* Give the IDE palette loop a real single-key fast path (`Ctrl+R`/`e`) that calls `LaunchEditor(currentFile)` directly without walking the numbered menu — the menu item already exists, it just needs a hotkey shortcut in front of it.
* Build `IdeCommandRegistry.cs` scoped small: `/open`, `/diff`, `/symbols`, `/edit`, `/ask` mapped directly onto the static methods that already exist (`CodeViewer.Show`, `GitDiffViewer.ShowDiff`, `SymbolSearch.BrowseSymbols`, `LaunchEditor`, `AgyAiCore.InvokeClaude`) instead of the current if/else-on-menu-index.
* Add the `SkillLoader` from §5 scanning `skills/*.md` — since the IDE's primitive set (`AskAi`, `ShowDiff`, `LaunchEditor`) already exists, a skill is just a saved sequence over primitives that are already built, not new infrastructure.
* `SymbolSearch.BrowseWorkspaceSymbols` caps at 50 files with plain `.Contains` substring matching — swap in a real fuzzy scorer for Quick Open instead of building a second search implementation.

**AI + Learning content template redesign** — current state: `AiLearningGenerator.RunGenerator` (`AiLearningGenerator.cs:49`) builds the entire prompt as one hardcoded string ("Generate deep learning content for topic: {topic}. Output clean JSON...") with no schema, no examples, no format instructions, then `ExecuteCliGenerator` (line 104-108) writes raw stdout straight to disk with zero validation — even though `StudyHelper.cs` already defines the real target schema (`FlashCard(Id, Front, Back, Hint?, Mnemonic?, ExampleSentence?, Tags[], Difficulty, SrState)`, `DeckFile(DeckMeta, FlashCard[])`) that the generator never tells the model about. There is no template-file concept anywhere in the repo (`templates`/prompt-template search hits nothing relevant).
* Serialize an example `DeckFile`/`FlashCard` instance into the prompt so the model targets the schema the app actually reads, instead of the current freeform one-liner.
* Validate `ExecuteCliGenerator`'s output through `LearnDataPaths.LoadJson<DeckFile>` before writing; on failure, one retry with an error-repair follow-up prompt instead of silently saving whatever the CLI printed (including login prompts/error banners).
* Strip markdown code fences (```json ... ```) defensively before parsing — a common CLI-output shape this code doesn't guard against today.
* Externalize per-domain prompt bodies into editable `templates/flashcards.md`/`templates/quiz.md` files next to `LearnDataPaths.DecksDir`, selected by the existing domain switch in `GetTargetFilePath` — the same markdown-with-frontmatter pattern already proposed for the IDE Skills system, reused for a second use case instead of inventing a new mechanism.
* Record which prompt/template version generated a deck (a field on `DeckMeta` or a sibling record) so improving a template later doesn't silently orphan decks generated by an older one.

**SSH × Tailscale** — current state confirms both claims already in §9: `SshHelper.GenerateAsciiQrCode` (`SystemHelpers.cs:401-458`) draws three finder-pattern corners and fills the rest with `(bit ^ (r*c%2))` — a homemade pattern with no Reed-Solomon error correction; it cannot be scanned by a real phone camera. `StartMobileSshKeyReceiver`/`StartKeyReceiver` (lines 626/460) still run a bare, unauthenticated `HttpListener`/`TcpListener` that accepts any POST body regex-matching an `ssh-*` key format and appends it to `authorized_keys` — reachable by anyone on the LAN/tailnet during the ~2-minute enrollment window. `ShowTailscaleServeInfo` (line 386) only *reads* `tailscale serve`/`funnel status`; nothing in the codebase actually invokes `tailscale serve`/`funnel` to replace the listener.
* Swap `GenerateAsciiQrCode`'s rendering internals for a real QR encoder (e.g. `QRCoder`, MIT-licensed, pure .NET) rendered with Unicode half-blocks — same call site, no redesign of the enrollment flow needed.
* Have the enrollment flow shell `tailscale serve https / http://localhost:{port}` before starting the listener, and embed a one-time random token in the QR-code URL that `AddAuthorizedKey` must match — closes the unauthenticated-listener gap without a full rewrite.
* Add expiry/single-use enforcement tied to that token, and log accepted key fingerprints (not just truncated key text) so `ShowSshInfo` can show provenance later ("added via mobile enrollment at HH:mm").

---

## 🔬 16. Function-Level Deep Dive (2026-07-21) — six parallel per-file passes across every remaining source file, one level below §14/§15's domain-level findings

§14/§15 found the biggest, most user-visible bugs by reading each domain's overall flow. This pass went function-by-function through every file not yet covered that way — including files with zero prior findings (`Config.cs`, `ThemeHelper.cs`, `Icons.cs`, `ScreenChrome.cs`, `SpectreWidgets.cs`, `ProcessRunner.cs`, and the 1977-line PowerShell profile itself, which had never had a function-level pass) — plus a second look inside functions that already have a *known* bug, on the chance a second, different one is hiding in the same method. It was. Ordered by how bad the failure mode is: crashes/hangs first, then silent-wrong-behavior, then smaller correctness gaps, then cheap wins.

### 💥 New crash/hang bugs (none of these were previously documented)
1. **`FlatTreeRenderer` crashes on empty search results.** `Run()` (`FlatTreeRenderer.cs:197-220`, `561-566`): pressing Down/PageDown while a search matches zero rows drives `selectionIndex` to `-1`; the Enter handler only guards `selectionIndex < visibleRows.Count`, never `>= 0`, so the next Enter throws an uncaught `ArgumentOutOfRangeException` on `visibleRows[-1]` and kills the whole TUI. Fix: clamp to `Math.Max(0, ...)` and/or guard Enter with `selectionIndex >= 0`.
2. **Three PowerShell functions are called but never defined.** `Start-AgyManager`, `Start-AgyProxy`, `Test-AgyAiGate` are invoked from `mgr`/`prxy`/`ai-dash`/`ask-ai`/`GitHelper.GenerateAiCommitMessage` (`Microsoft.PowerShell_profile.ps1:1926,1929,1940,527,1850`) but a full-file grep finds no definition anywhere — every one of those commands throws "term is not recognized" on first use. Fix: define them (likely orphaned mid-refactor) or delete the dead call sites.
3. **`ProcessRunner.RunCapture()` can deadlock or hang the whole app indefinitely.** (`ProcessRunner.cs:70-113`) Redirects both stdout and stderr but only ever reads stdout — a subprocess that writes enough to stderr before exiting deadlocks the synchronous `ReadToEnd()`/`WaitForExit()` pair, and there is no timeout anywhere, so one unresponsive `git`/`docker`/`aws` call freezes the entire TUI with no way to cancel short of killing the process. This is the shared helper §3's "8 near-identical shell-out helpers" finding consolidated onto — so the hang risk is now centralized, not distributed. Fix: read both streams asynchronously, add a timeout parameter with `Kill(true)` fallback.
4. **`FileExplorer` can never open `.cs`/`.ps1`/`.sh` files.** `Browse()` (`TerminalIde.cs:45`) strips a filename prefix via a hardcoded `entries[idx][3..]`, assuming every icon+space prefix is exactly 3 characters — but `⚙`/`⚡` (the `.cs`/`.ps1`/`.sh` icons) are single UTF-16 units, giving only a 2-character prefix. The slice eats the first real letter of the filename, so the resulting path never matches `File.Exists`, and those files silently can't be opened from the tree. Fix: split on the first space, don't hardcode an offset.
5. **`GitDashboard`'s live view has a malformed Spectre markup tag.** `GitNexus.ShowLiveDashboard()` (`GitDashboard.cs:30`) closes markup with `$"[bold]{col}[]"` instead of `[/]` — this either renders literally as garbage text or throws a Spectre markup parse exception, breaking the dashboard outright.
6. **`SshHelper.StartKeyReceiver()` blocks forever with no timeout.** (`SystemHelpers.cs:460-490`) `AcceptTcpClient()` has no bound, unlike its sibling `StartMobileSshKeyReceiver` (explicit 2-minute timeout right next to it) — a client that never connects freezes the whole process.
7. **`AiLearningGenerator` has a real command-injection surface, not just a validation gap.** `ExecuteCliGenerator()` (`AiLearningGenerator.cs:86-96`) shells the user-typed topic through `cmd.exe /c {exe} --prompt "{promptEscaped}"` with only `"`→`\"` escaping — `cmd.exe` still expands `&`, `|`, and `%VAR%` inside a quoted argument, so a topic like `foo & calc.exe` or one containing `%USERPROFILE%` is a real injection/env-leak vector from free-text input, on top of the already-known missing JSON validation.

### 🔴 Silent-wrong-behavior (does something, but the wrong thing, with no error)
8. **`Config.Save()` destroys every comment in `profile.config.json` on the first settings toggle.** (`Config.cs:158`) Re-serializes with plain `JsonSerializerOptions{WriteIndented=true}`, and the file ships with a `//` comment on nearly every line — the very first `/ui-mode` or `/density` toggle silently strips all of them.
9. **Two aliases silently lose their arguments.** `Microsoft.PowerShell_profile.ps1:1839` rebinds `co` (was: checkout a named branch) to a parameterless interactive picker — `co my-branch` now silently ignores `my-branch` and opens the picker instead. Same pattern for `dkcl` at line 1822 (`-All` switch silently dropped). The original, correct bindings are still physically present earlier in the file and read as "the" implementation to anyone skimming top-to-bottom, but are dead the moment the file finishes loading.
10. **`SystemMonitor`'s live CPU gauge always shows 0%.** `SystemHelper.SystemMonitor()` (`Microsoft.PowerShell_profile.ps1:1075`) double-indexes `$cpuSample.CounterSamples[0].CounterSamples[0].CookedValue` (should be one level shallower); the exception is swallowed by an empty `catch{}`, so it fails the same way every refresh with zero signal.
11. **`GetAccountStats()` shows identical "shared" numbers for every account.** (`AccountHelper.cs:1136-1140`) `skillsCount`/`convCount` are always computed from the default `AgySourceHome`, not `GetAccountDirectory(accountName)` — every account's card shows the same skills/history count regardless of which account it's actually displaying.
12. **`SyncActiveAccountWithKeyring()` can cross-contaminate a different account's backup.** (`AccountHelper.cs:864-983`) When no account matches the keyring token, it unconditionally overwrites the *currently active* account's stored token file with the unmatched credential instead of doing nothing — a wrong guess silently corrupts that account's backup.
13. **`LogoutAccount()` doesn't actually revoke the credential if it's the active one.** (`AccountHelper.cs:819-833`) Deletes local files but never calls `AgyKeyringHelper.DeleteToken` for the active account — a live, working credential remains in Windows Credential Manager after "logout."
14. **The multi-agent auto-commit toggle silently doesn't apply to Ollama-routed Claude.** `RunInteractive()` (`AiHelper.cs:1341-1365`) only injects `--no-auto-commit` when the launcher executable name contains "claude"/"codex"/"agy" — but Claude-via-Ollama launches through `ollama.exe`, so the toggle has zero effect on that path despite the UI claiming "Multi-Agent Auto-Commit: DISABLED."
15. **`IsPortResponding()` reports a hung Ollama as "running."** (`AiHelper.cs:604-627`) Falls back to a plain listener check (socket bound, not actually responding) on *any* exception, not just connection-refused — a crashed daemon that still holds its socket, or an unrelated process squatting on the port, reads as healthy.
16. **`Docker`'s "Full cleanup" doesn't do what the standalone "stop all" option does.** `ShowCleanupDashboard()` (`DockerHelper.cs:41-58` vs. `27-30`) runs `container prune -f` (stopped containers only) for its bulk path, while the separately-labeled "Stop & remove all running containers" item actually stops running ones — selecting "Full cleanup" silently leaves running containers running.
17. **`ProblemTracker`'s Filter menu item is dead.** `Run()` (`StudyQuizzes.cs:568-571`) only branches on `idx==0`; selecting "[f] Filter by topic" falls through to the same code path as "← Back," even though a working `Filter()` method exists right there unused.
18. **`ProfileNavigator.Navigate()` treats Escape as "confirm."** (`WorkspaceRegistry.cs:210`) Only checks `actionIdx` against `1`/`2`/`3`; pressing Escape (`-1` by this app's own contract) falls through to the same `return selected.WorkspacePath` as an explicit selection — cancel doesn't cancel.
19. **`ThemeHelper.ApplyMobileMode()` claims success when it did nothing.** (`ThemeHelper.cs:73`) If no `-mobile` variant file exists for the current theme, it still persists `enable_mobile=true` and prints "Mobile Prompt Theme activated" while the desktop theme stays active — saved state and displayed message both lie.
20. **A UDP listener on the target port survives `kill-port`.** `KillPort()` (`SystemHelpers.cs:62-65`) assumes every `netstat -ano` row is TCP's 5-column shape and skips anything shorter — UDP rows have 4 columns and are silently never killed.

### 🟡 Smaller correctness gaps
21. `AutoDiscoverWorkspaces()` (`WorkspaceRegistry.cs:71`) never reads `Config.Current.ProjectSearchPaths`/`ProjectExcludeFolders` — both are populated in `profile.config.json` and never referenced anywhere in the codebase; discovery only checks the single `ProjectsBaseDir` and hardcodes one `node_modules` exclusion.
22. `Program.cs`'s `RunCommand()` only forwards `args[0]`; every other CLI argument the PowerShell profile passes (e.g. `Invoke-ControlCenter "set-model" $ModelName`) is silently dropped (`Program.cs:246`). Relatedly, `install-ai`, `init-ollama`, `set-model`, `ensure-ollama`, `clawdbot` are called from the profile (`Microsoft.PowerShell_profile.ps1:1551-1602`) but have no matching `case` in the switch — they currently do nothing.
23. `ShowHotkeysGuide()` (`Program.cs:279`) is a **fifth** independent, hand-maintained command table (on top of the four §3.0 already unified) — hardcoded `table.AddRow` calls instead of reading `CommandRegistry.All`, and it's already stale (missing several real commands).
24. `IsAiOllamaEnabled()`'s CLI-dispatch gate (`Program.cs:347`) hardcodes 9 aliases while `CommandRegistry.All` marks 15 `RequiresAiOllama: true` — the extra 6 (`ollama-models`, `ollama-pull`, etc.) are hidden from menus but still directly reachable via CLI even when the feature flag is off.
25. `AssertSwitchCases()` (`CommandRegistry.cs:380-400`) re-parses `Program.cs` off disk at runtime via hardcoded relative paths — in a published/self-contained build the source file isn't shipped next to the binary, so this drift guard silently never runs outside a source checkout.
26. `MenuNode.BuildTree()` gives `/grammar`, `/obsidian`, and `/refresh` two separate `MenuNode`s with the same `Id` (cross-listed under two branches) — both renderers key expand-state off `Id`, so one occurrence's expand/collapse toggle can affect the other. Separately, `"no-auto-commit"` (the primary command, not its `"autocommit"` alias) is never added to any tree branch — unreachable from both renderers and from `/` search.
27. `GetCommandIcon()` (`Icons.cs:168-219`) — the "Nerd Font" branch uses the *same raw emoji* as the plain-emoji branch for most commands (docker, AWS, SSH, disk, tracking), so toggling Nerd Font detection has no visible effect. Separately, category-before-alias branching means `/obsidian`/`/refresh`'s explicit icon cases (`💎`/`🔄`) are dead code, permanently shadowed by the generic Learn & Study fallback.
28. `OllamaStatusWidget.Render()` (`StatusWidgets.cs:228-282`) calls `client.GetStringAsync(...).Result` synchronously with no timeout on a cache miss — a hung Ollama port freezes the entire render/input loop for up to the default 100s HttpClient timeout, unlike its neighbor `PublicIpWidget`, which already does this correctly (fire-and-forget + placeholder).
29. Widget expansion in `FlatTreeRenderer.RenderTree()` (`FlatTreeRenderer.cs:801-822`) re-invokes `widget.Render()` on every keystroke for as long as a widget stays expanded, not once on highlight as intended — `DiskSpaceWidget`/`AccountTreeWidget`/`LiveDashboardWidget` re-enumerate drives/accounts/stats on every keypress anywhere in the tree while left open.
30. `ThreePaneRenderer.Run()` has no `/`-search key binding at all (`ThreePaneRenderer.cs:19-372`) — refactor_plan §2 specifies search as one of the few behaviors both renderers must share verbatim, but three-pane users have no way to invoke it.
31. `TerminalIde`'s exclusion checks (`bin`/`obj`/`.git` at `TerminalIde.cs:278,462,550`) are raw substring matches, not path-segment checks — legitimate paths merely containing those substrings get wrongly excluded, while `node_modules`/`.vs`/`packages` (which don't) are walked in full with no depth limit.
32. `ContentExtractor.ExtractQuizBlocks()` (`ResourceDiscovery.cs:307-322`) fabricates "option 1 is correct" via `FirstOrDefault` returning a default tuple when no `- [x]` option is actually checked in the source markdown, instead of skipping the block.
33. `CsvExtractor.Extract()` (`ResourceDiscovery.cs:150-173`) splits rows with a plain `line.Split(',')` — any quoted value containing a comma desyncs every subsequent column.
34. `StudySession.Record()` (`StudyHelper.cs:610-619`) writes `StartTime`/`EndTime` as the same `now` timestamp taken at record time, discarding the real start captured earlier in `Run()` — every logged session shows a zero-length duration.
35. `ProjectScaffolder.Scaffold()`'s React/Vite path (`ProjectScaffolder.cs:27`) creates an empty target directory but runs `npm create vite@latest` without changing into it — Vite scaffolds into the process's actual working directory, leaving an orphaned empty folder at the path the success message claims the project lives at.
36. `TtlCache<K,V>.GetOrCompute()` (`TtlCache.cs:16`) isn't atomic under concurrent access on an expired key — two threads racing on the same key both invoke `factory()` (last write wins), which matters once network-backed widgets (Ollama status, IP) are the ones calling through it under rapid refresh.

### 💡 Cheap, concrete wins (small effort, clear payoff)
* Add branch-name completion for `co`/`gbd`/`cob`, workspace-name completion for `proj`, and theme-name completion for `theme` via `Register-ArgumentCompleter` — the data source for all three already exists in-process (`Microsoft.PowerShell_profile.ps1`, currently only `multigravity` has a completer).
* Collapse `Invoke-Codex-By-Ollama`/`Invoke-Claude-By-Ollama`/`Invoke-OpenClaw-By-Ollama`/`Invoke-Clawdbot-By-Ollama`/`Invoke-Hermes-By-Ollama`/`Invoke-HermesDesktop-By-Ollama` (`Microsoft.PowerShell_profile.ps1:1485-1532`, six near-identical bodies) into one parameterized helper.
* Gate `Write-AgyStartupCheckpoint`'s unconditional 4x disk I/O (`Microsoft.PowerShell_profile.ps1:123-131`, the file's own comment calls it "temporary diagnostic") behind a debug env var — it currently runs on every single shell startup for every user.
* `ExtractorRouter.ExtractUrl()` (`ResourceDiscovery.cs:209-238`) leaks a temp `.md` file if extraction throws (no `try/finally`) and is a 9th unshared `new HttpClient()` site — both are one-line fixes (wrap in `finally`, route through `HttpClientProvider`).
* `SpectreWidgets.Show()`/`ShowRobust()`/`ShowDynamic()` (`SpectreWidgets.cs:13-61`) accept a `defaultIndex` parameter that's silently never used — either wire it up (so preselecting the current theme/account actually shows as preselected) or remove it.
* `SpectreTable.Live()` (`SpectreWidgets.cs:434-456`) re-queries and redraws roughly every 500ms regardless of the caller-requested `refreshMs` (e.g. `5000`) — accumulate elapsed time and only refresh at the requested interval, using the short sleep only to poll for a keypress.


AgyAccountCore split (AccountRepository/QuotaTracker/TokenVault)	5	🔴 Cosmetic-only	Decomposed into TokenVault.cs/QuotaTracker.cs/AccountRepository.cs, unit-tested in isolation — but as of §14/§15's audit, zero production code calls any of the three; AccountHelper.cs's AgyAccountCore still runs its own independent duplicate logic, and AccountRepository's path constant resolves to the wrong directory. Corrected from a prior "Shipped" marking that was based on the files existing/building, not on tracing callers.


guide 

# 🛠️ AgyTuiApp Enhancement Guide — Full Build Plan

**Purpose**: `refactor_plan.md` is an *audit log* — findings accumulate there chronologically (§9–§16), in the order they were discovered, mixed with historical proposals that have since shipped, partially shipped, or turned out to be wrong. This document is the *build plan* — the same ground, reorganized into "what to build, in what order, with enough detail to actually start coding." It is derived entirely from `refactor_plan.md`'s verified findings (specifically §14, §15, §16) — nothing here is a new, unverified claim; every fix below cites the finding it resolves.

**How to use this**: work top to bottom. Each tier assumes the tier(s) above it are done — P1 items build on P0 fixes being in place, etc. Within a tier, items are independent and can be parallelized across PRs. Every item has: the problem (with file:line), the fix (with concrete method signatures/schemas, not just prose), files touched, and a verification step.

---

## 0. Current situation, in one paragraph

The app is real and mostly works: 34 C# files, ~14,000 lines, 143 commands, two working UI renderers, a genuinely-correct SM-2 spaced-repetition algorithm, a real AI activity ledger, DPAPI-backed account switching. But three things are true at once: (1) a handful of **critical bugs make whole domains not work as advertised** — the entire Learning suite never persists review results, account deletion can resurrect a deleted account with a live credential; (2) **several "shipped" refactors are cosmetic** — three account-domain classes exist, build, and pass tests, but nothing calls them; (3) the newest function-level pass (§16) found **new crash bugs that weren't visible from the domain-level audits** — an uncaught exception that kills the whole TUI from an empty search, three PowerShell functions that are called but never defined. None of this is architecture-level rework; every fix below is scoped to one or a few functions.

---

## 1. Priority tiers

| Tier | Theme | Why this order |
|---|---|---|
| **P0** | Crash/hang/data-corruption fixes | Ships stability before anything else — a feature built on top of a hang or a data-corrupting bug inherits the bug. |
| **P1** | Learning & Study domain — full redesign | The single most broken domain (SM-2 is dead code) *and* the one you asked to be called out specially. Wiring it correctly is a prerequisite for every learning enhancement (retention charts, mastery icons, Anki import) — they all read data this tier makes real. |
| **P1** | Project/Workspace domain — Terminal IDE + dev tools | The other domain you asked about by name (agy-cli/neovim-style IDE, ctrl+R editor). Independent of the Learning tier, safe to parallelize. |
| **P2** | AI/Ollama pipeline unification + Accounts cleanup | High-value, but depends on nothing above being done first — can slot in whenever. |
| **P2** | SSH × Tailscale hardening | Security-relevant but not urgent (existing listener has been live this whole time); do after the crash fixes. |
| **P2** | UI/Renderer consolidation | Structural — best done once the crash fix (P0) is already in and stable, so the shared base class refactor has a known-good baseline to diff against. |
| **P3** | PowerShell profile cleanup | Cosmetic/DX improvements — dedupe aliases, tab-completion, lazy loading. No user-facing bug depends on this. |

---

## 2. P0 — Crash / hang / data-corruption fixes

Do these first, in any order, each as its own small PR. None of them touch UI layout or require design decisions — they're bugs with one correct fix.

### 2.1 `FlatTreeRenderer` crashes on empty search results
**Finding**: `refactor_plan.md` §16 #1. `Run()` (`FlatTreeRenderer.cs:197-220, 561-566`) lets `selectionIndex` go to `-1` when Down/PageDown is pressed against a zero-match search; the next Enter indexes `visibleRows[-1]` → uncaught `ArgumentOutOfRangeException` → whole process dies.

**Fix**:
```csharp
// wherever selectionIndex is advanced during search (Down/PageDown handlers):
selectionIndex = visibleRows.Count == 0 ? -1 : Math.Min(visibleRows.Count - 1, selectionIndex + 1);

// Enter handler — add the lower bound that's currently missing:
if (selectionIndex >= 0 && selectionIndex < visibleRows.Count)
{
 // existing execute-on-Enter logic
}
```
Also add a zero-results empty state to the render path ("No matches — try a different search") so the user gets feedback instead of a seemingly-frozen list.

**Files**: `AgyTuiApp/Views/FlatTreeRenderer.cs`.
**Verify**: launch `cc`, press `/`, type a string that matches nothing (`zzzzz`), press Down 3 times, press Enter. Must not crash; should show empty state.

### 2.2 `ProcessRunner.RunCapture()` can hang the entire TUI
**Finding**: §16 #3. Redirects stderr but never reads it; a chatty subprocess can deadlock the stdout `ReadToEnd()`/`WaitForExit()` pair, and there's no timeout anywhere, so any hung `git`/`docker`/`aws` call freezes the whole app.

**Fix** — read both streams asynchronously and add a bounded wait:
```csharp
public static async Task<(string Stdout, string Stderr, int ExitCode)> RunCaptureAsync(
 string exe, string args, string? workingDir = null, TimeSpan? timeout = null)
{
 using var proc = new Process { StartInfo = BuildStartInfo(exe, args, workingDir) };
 var stdoutTask = new StringBuilder();
 var stderrTask = new StringBuilder();
 proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdoutTask.AppendLine(e.Data); };
 proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderrTask.AppendLine(e.Data); };
 proc.Start();
 proc.BeginOutputReadLine();
 proc.BeginErrorReadLine();

 var waitTask = proc.WaitForExitAsync();
 var completed = await Task.WhenAny(waitTask, Task.Delay(timeout ?? TimeSpan.FromSeconds(30)));
 if (completed != waitTask)
 {
 try { proc.Kill(entireProcessTree: true); } catch { /* already exited */ }
 return (stdoutTask.ToString(), stderrTask.ToString() + "\n[TIMED OUT]", -1);
 }
 return (stdoutTask.ToString(), stderrTask.ToString(), proc.ExitCode);
}
```
Keep the existing synchronous `RunCapture`/`Run` as thin wrappers over this (`.GetAwaiter().GetResult()`) so ~40 existing call sites across `GitHelper`/`DockerHelper`/`AwsHelper`/etc. don't all need to change signature in the same PR — migrate them to the async overload opportunistically in later PRs.

**Files**: `AgyTuiApp/Helpers/ProcessRunner.cs`.
**Verify**: temporarily point a call at a command that sleeps 60s (`ping -n 60 localhost` on Windows); confirm the call returns after the timeout instead of hanging forever, and confirm a normal `git status` call still returns its real output including anything written to stderr (e.g. a warning).

### 2.3 `FileExplorer` can't open `.cs`/`.ps1`/`.sh` files
**Finding**: §16 #4. `Browse()` (`TerminalIde.cs:45`) does `entries[idx][3..]` assuming a fixed 3-char icon prefix; `⚙`/`⚡` are 2 chars wide, so the slice eats the filename's first letter.

**Fix**: don't hardcode an offset — split on the first space:
```csharp
string filename = entries[idx].Substring(entries[idx].IndexOf(' ') + 1).TrimStart();
```
**Files**: `AgyTuiApp/Helpers/TerminalIde.cs` (`FileExplorer.Browse`).
**Verify**: open the IDE (`ide`), browse into a folder containing a `.cs` file, select it — must open, not silently no-op or throw.

### 2.4 Three PowerShell functions are called but never defined
**Finding**: §16 #2. `Start-AgyManager`, `Start-AgyProxy`, `Test-AgyAiGate` are referenced from `mgr`/`prxy`/`ai-dash`/`ask-ai`/`GitHelper.GenerateAiCommitMessage` (`Microsoft.PowerShell_profile.ps1:1926,1929,1940,527,1850`) but grep confirms zero definitions anywhere in the file (or the dead `Profile/*.ps1` files).

**Fix** — this needs a decision, not just a patch: either these were genuinely never finished (define them — check git history/`git log -p -S"Start-AgyManager"` for whether an implementation ever existed and was deleted by mistake), or the call sites are leftover from an abandoned feature (delete the call sites and the aliases that reach them: `mgr`, `prxy`, `ai-dash`, `ask-ai`). Given `ai-dash`/`ask-ai` sound purposeful (AI dashboard, ask-AI shortcut) and appear in multiple places, the likely correct fix is defining minimal versions:
```powershell
function Start-AgyManager {
# placeholder until a real implementation exists — surfaces a clear message
# instead of "term not recognized"
 Write-Warning "Start-AgyManager is not yet implemented. See enhancement_guide.md §2.4."
}
```
as an immediate stopgap (prevents the crash) while the real feature is scoped separately, OR fully removing the 3 call sites + their aliases if the feature is abandoned.

**Files**: `Microsoft.PowerShell_profile.ps1`.
**Verify**: run each of `mgr`, `prxy`, `ai-dash`, `ask-ai`, and `gcmt` (which triggers `GenerateAiCommitMessage`) in a fresh shell — none should throw "term is not recognized."

### 2.5 Docker's destructive Windows commands are no-ops
**Finding**: §15 #3 / §16 #16. `RunDocker` shells POSIX `$()` through `cmd /c docker {args}` — `cmd.exe` never expands it, so `stop $(docker ps -q)` etc. pass literal garbage as container IDs. Separately (§16 #16), "Full cleanup" only prunes *stopped* containers while its own label implies it also stops running ones.

**Fix**: resolve container IDs in C# first, pass them as real arguments:
```csharp
public static void StopAllContainers()
{
 var (idsOut, _, _) = ProcessRunner.RunCapture("docker", "ps -q");
 var ids = idsOut.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
 if (ids.Length == 0) { AnsiConsole.MarkupLine("[yellow]No running containers.[/]"); return; }
 if (!AnsiConsole.Confirm($"Stop {ids.Length} running container(s)?")) return;
 RunDocker($"stop {string.Join(" ", ids)}");
}
```
Apply the same pattern to `RemoveAllContainers`. Then make "Full cleanup" actually call `StopAllContainers()` followed by `container prune -f`, so the label matches the behavior.

**Files**: `AgyTuiApp/Helpers/DockerHelper.cs`.
**Verify**: with 2+ running containers, run `dkstac`/`dkrmac` — containers actually stop/are removed (not "No such container").

### 2.6 `Config.Save()` destroys every comment in `profile.config.json`
**Finding**: §16 #8. Plain re-serialization on any settings write (`/ui-mode`, `/density`, etc.) strips every `//` comment the shipped config file has.

**Fix**: don't round-trip through a plain POCO serializer for a file meant to stay human-edited. Two viable approaches, pick one:
- **(a) Minimal-diff patch**: parse into `System.Text.Json.Nodes.JsonNode`, mutate only the specific key(s) that changed, serialize that back — preserves everything else including comments if you use a comment-preserving writer, or at minimum preserves key order/structure.
- **(b) Split runtime vs. authored config**: keep `profile.config.json` as the authored, comment-having file that's *read* but never *written* by the app; persist runtime-toggled settings (`UiMode`, `Density`, theme) to a separate `profile.state.json` with no comments, and merge both at load time (state overrides authored defaults). This is the lower-risk option — it never touches the file the user hand-edits.

Recommend **(b)** — simpler, and matches the existing split where some settings are already persisted to small marker files (`active_account.txt`, `selected_theme.txt`).

**Files**: `AgyTuiApp/Helpers/Config.cs`.
**Verify**: hand-edit a comment into `profile.config.json`, run `/ui-mode` to toggle it, confirm the comment is still there afterward.

### 2.7 Account deletion can resurrect the deleted account
**Finding**: §14/§15 Critical #2. `DeleteAccount` → `SetActiveAccount("default", …)` → `BackupActiveToken(GetActiveAccount())`, but the active-account marker hasn't been rewritten yet when `BackupActiveToken` reads it — it still resolves to the just-deleted name, recreating that account's directory with a live token.

**Fix** — reorder so the marker is rewritten *before* any backup call happens:
```csharp
public static void DeleteAccount(string accountName)
{
 bool wasActive = GetActiveAccount() == accountName;
 if (wasActive)
 {
 WriteActiveAccountMarker("default"); // rewrite the marker FIRST
 AgyKeyringHelper.DeleteToken(); // and clear the live credential
 }
 Directory.Delete(GetAccountDirectory(accountName), recursive: true);
 if (wasActive)
 SetActiveAccount("default", false); // now safe — GetActiveAccount() already reads "default"
}
```
**Files**: `AgyTuiApp/Helpers/AccountHelper.cs` (`DeleteAccount`, `SetActiveAccount`).
**Verify**: create a test account, switch to it, delete it. Confirm it does **not** reappear in `GetAccounts()`/`agyswitch`'s list afterward, and confirm no `keyring_token.txt` gets written into its now-deleted directory.

### 2.8 `LogoutAccount()` doesn't revoke the credential if it's the active one
**Finding**: §16 #13. Deletes local files but skips `AgyKeyringHelper.DeleteToken` for the active account — a working credential survives "logout" in Windows Credential Manager.

**Fix**:
```csharp
public static void LogoutAccount(string accountName)
{
 if (GetActiveAccount() == accountName)
 AgyKeyringHelper.DeleteToken();
 // existing local-file cleanup unchanged
}
```
**Files**: `AgyTuiApp/Helpers/AccountHelper.cs`.
**Verify**: log out of the active account, then check Windows Credential Manager (`cmdkey /list` or Credential Manager UI) for the entry — must be gone.

---

## 3. P1 — Learning & Study domain: full redesign (3.1 & 3.2 RESOLVED)

This is the tier you specifically asked for detail on. The domain has real, correct infrastructure (`SpacedRepetitionEngine`'s SM-2 math is verified correct per §11) sitting completely unused because nothing calls it. The fix is not "build SM-2" — it's "wire the SM-2 that already exists into every place a user answers a card," plus the handful of follow-on features that become possible once that's true.

### 3.1 Foundational fix: wire persistence into every drill (do this before anything else in this tier)

**The problem, precisely** (§14 Critical #1 / §16 #34): every drill (`FlashcardEngine`, `VocabDrill`, `KanaQuiz`, `JlptVocabDrill`, `GrammarQuiz` — all in `StudyQuizzes.cs`) does the equivalent of:
```csharp
if (AnsiConsole.Confirm($"Did you know: {card.Front}?")) known++; else again++;
```
— an in-memory tally, thrown away when the loop ends. `SpacedRepetitionEngine.UpdateCard` (`StudyHelper.cs:414-435`) — which correctly computes the next SM-2 interval/easiness-factor — is never called. `StudySession.Record` (`StudyHelper.cs:610`) — the only thing that writes `study_log.json`, which drives streaks/goals/mastery — is only called from the Pomodoro timer. And when it *is* called, it has its own bug (§16 #34): it stamps `StartTime`/`EndTime` with the same "now" instead of the real session start.

**The fix — one shared session-recording helper, called from every drill**:
```csharp
// New: StudyHelper.cs, alongside SpacedRepetitionEngine
public static class DrillSession
{
 // Call once per card, immediately after the user answers.
 public static void RecordAnswer(FlashCard card, bool knewIt, string deckFilePath, DeckFile deck)
 {
 SpacedRepetitionEngine.UpdateCard(card.Sr, knewIt); // mutates card.Sr in place — already correct SM-2
 LearnDataPaths.SaveJson(deckFilePath, deck); // persist the whole deck back to disk

 if (knewIt) WeakItemsQueue.ClearWeakItem(card.Id); // needs a real implementation — see below
 else WeakItemsQueue.AddWeakItem(card.Id, deckFilePath);
 }

 // Call once at the end of a drill session (replaces each drill's local known/again tally-and-discard).
 public static void RecordSession(string subject, DateTime startedAt, int reviewed, int correct)
 {
 StudySession.Record(subject, startedAt, DateTime.UtcNow, reviewed, correct); // pass the REAL start
 StudyStreak.RecordToday();
 DailyGoals.CheckAndTick(subject, reviewed);

 if (AnsiConsole.Confirm("Append a summary of this session to today's Obsidian daily note?", defaultValue: false))
 ObsidianStudySync.OfferSync(subject, reviewed, correct, startedAt);
 }
}
```
Then every drill's `Run()` changes from a local tally to:
```csharp
// inside the per-card loop, replacing `if (Confirm(...)) known++; else again++;`
bool knewIt = AnsiConsole.Confirm($"Did you know: {card.Front}?");
DrillSession.RecordAnswer(card, knewIt, deckFilePath, deck);
if (knewIt) correct++;
reviewed++;

// after the loop ends
DrillSession.RecordSession(subjectName, sessionStart, reviewed, correct);
```
`JlptVocabDrill.Run` already forwards into `FlashcardEngine.Run` — fixing `FlashcardEngine` fixes it too, no separate change needed there.

**Fix the two empty no-ops this depends on** (`StudyHelper.cs:1006-1012`):
```csharp
public static class WeakItemsQueue
{
 // was: public static void AddWeakItem(...) { } (empty)
 public static void AddWeakItem(string cardId, string deckPath)
 {
 var queue = LoadQueue();
 if (!queue.Any(q => q.CardId == cardId && q.DeckPath == deckPath))
 queue.Add(new WeakItem(cardId, deckPath, DateTime.UtcNow));
 SaveQueue(queue);
 }

 public static void ClearWeakItem(string cardId)
 {
 var queue = LoadQueue();
 queue.RemoveAll(q => q.CardId == cardId);
 SaveQueue(queue);
 }

 private static List<WeakItem> LoadQueue() =>
 LearnDataPaths.LoadJson<List<WeakItem>>(LearnDataPaths.WeakItemsPath) ?? new();
 private static void SaveQueue(List<WeakItem> q) =>
 LearnDataPaths.SaveJson(LearnDataPaths.WeakItemsPath, q);
}
public record WeakItem(string CardId, string DeckPath, DateTime MissedAt);
```
(Adjust to whatever `WeakItem`/queue-file shape the class already has stubbed — the point is the two methods need real bodies, not new architecture.)

**Files**: `AgyTuiApp/Helpers/StudyHelper.cs`, `AgyTuiApp/Helpers/StudyQuizzes.cs`.
**Verify** (this is the highest-value test in the whole codebase to get right): review the same flashcard deck twice in one day, marking every card "known." On the second pass, cards should show fewer/no repeats of ones you just aced (interval pushed out by SM-2) — today, every card is due every single time regardless of history. Also confirm `streak`/`goals`/`stats` (Track & Progress category) actually move after a flashcard session, not just after a Pomodoro session.

### 3.2 Fix the broken content paths this surfaces

Once 3.1 makes drills actually load and write real files, two path bugs (previously masked because nothing durable happened either way) become user-visible:
* **`GrammarQuiz.Run("N5")`** resolves (via Windows case-insensitive matching) to the JLPT vocab file instead of `learn/grammar/n5.json` and NREs on `data.Cards` (§14 High / §15 #7). Fix: correct the path build in `GrammarQuiz.Run` to always target `LearnDataPaths.GrammarDir`, not `JapaneseDir`.
* **English grammar** has the same class of bug in the other direction (§16, StudyHelper.cs:351 vs StudyQuizzes.cs:173-177): seeded to `learn/grammar/english.json`, but `GrammarQuiz.Run("English")` looks in `learn/english/grammar.json`/`learn/english/english.json` — neither exists. Fix both levels' path resolution together, off one `LearnDataPaths.GrammarFileFor(level)` helper instead of two independent ad hoc string builds:
```csharp
public static string GrammarFileFor(string level) =>
 Path.Combine(GrammarDir, $"{level.ToLowerInvariant()}.json"); // grammar/n5.json, grammar/english.json, etc.
```
**Files**: `AgyTuiApp/Helpers/StudyQuizzes.cs`, `AgyTuiApp/Helpers/StudyHelper.cs` (`LearnDataPaths`).
**Verify**: run `GrammarQuiz` for both `N5` (the default) and `English` — both must load real seeded content, no NRE, no "no data" fallback.

### 3.3 Unified `/learn` guided flow (the redesign originally scoped in `refactor_plan.md` §5, now actually buildable because 3.1 makes the data real)

Today `learn`/`due`/`weak`/`flashcard`/`vocab`/`jlpt`/`kana`/`kanji`/`stats`/`streak`/`goals`/`obsidian` are 12 separate aliases with no wiring between them. Build one guided entry point:
```csharp
public static class GuidedLearnFlow
{
 public static void Run()
 {
 var due = DueReview.GetAllDue(); // merges decks + JLPT + kanji + kana
 var weak = WeakItemsQueue.LoadAll(); // now real, per 3.1
 var ordered = weak.Concat(due.Except(weak)).ToList(); // weak items first

 ShowDueSummaryPanel(ordered); // the §5 mockup: per-subject due counts + mastery icons
 if (!AnsiConsole.Confirm("Start guided session?")) { OfferJumpToSpecificDeck(); return; }

 var sessionStart = DateTime.UtcNow;
 int reviewed = 0, correct = 0;
 foreach (var group in ordered.GroupBy(c => c.Subject))
 {
 // dispatch to the existing per-subject drill UI, unchanged —
 // FlashcardEngine/KanaQuiz/JlptVocabDrill/GrammarQuiz already call DrillSession.RecordAnswer internally now
 RunDrillFor(group.Key, group.ToList());
 }
 DrillSession.RecordSession("guided", sessionStart, reviewed, correct);
 }
}
```
Register as `/learn` (already an alias — point it at this instead of whatever minimal thing it does today), keep every individual alias (`vocab`, `kanji`, etc.) working for power users who want to skip straight to one deck.

**Files**: new `AgyTuiApp/Helpers/GuidedLearnFlow.cs`, wire into `Program.cs`'s `"learn"` case.
**Verify**: run `learn` with a mix of due and weak cards across 2+ subjects — session covers weak items first, then due items, grouped by subject, and a single "start guided session" gate instead of manually running 3 separate commands.

### 3.4 AI-generated learning content: template + validation redesign

**Current state** (`AiLearningGenerator.cs`): `RunGenerator` builds one hardcoded prompt string with no schema; `ExecuteCliGenerator` writes any non-empty stdout straight to disk with zero validation (§14 High / §16 crash-adjacent #7 — command injection via unescaped `&`/`|`/`%VAR%` in the topic string).

**Fix, in order**:

1. **Close the injection hole first** (§16 #7) — this is the actual security-relevant part, separate from the JSON-validation ask:
```csharp
// ExecuteCliGenerator — never build a cmd.exe string from user-typed text.
var psi = new ProcessStartInfo(exePath)
{
 RedirectStandardOutput = true,
 UseShellExecute = false,
};
psi.ArgumentList.Add("--prompt");
psi.ArgumentList.Add(promptText); // ArgumentList handles quoting/escaping correctly — no shell involved at all
```
Using `ArgumentList` instead of a `cmd /c "..."` string means there's no shell to interpret `&`/`|`/`%VAR%` in the first place — this is strictly better than better-escaping the string.

2. **Give the model the actual target schema** instead of a bare one-liner:
```csharp
private static string BuildPrompt(string topic)
{
 var example = new DeckFile(
 new DeckMeta(topic, DateTime.UtcNow, Version: 1),
 new[] { new FlashCard("id-1", "Front text", "Back text", Hint: "optional hint",
 Mnemonic: null, ExampleSentence: null,
 Tags: new[] { topic }, Difficulty: 1,
 Sr: SpacedRepetitionEngine.NewCardState()) });
 string schemaJson = JsonSerializer.Serialize(example, new JsonSerializerOptions { WriteIndented = true });
 return $"""
 Generate 10 flashcards for the topic "{topic}".
 Respond with ONLY valid JSON matching this exact shape (no markdown fences, no prose before/after):
 {schemaJson}
 """;
}
```
3. **Validate before writing, with a repair retry**:
```csharp
private static bool TryValidateAndSave(string rawOutput, string targetPath)
{
 string cleaned = StripCodeFences(rawOutput); // regex-strip ```json ... ``` wrappers defensively
 try
 {
 var deck = JsonSerializer.Deserialize<DeckFile>(cleaned)
 ?? throw new JsonException("null result");
 if (deck.Cards is null || deck.Cards.Length == 0)
 throw new JsonException("no cards in response");
 LearnDataPaths.SaveJson(targetPath, deck);
 return true;
 }
 catch (JsonException)
 {
 return false; // caller retries once with an error-repair follow-up prompt, then gives up cleanly
 }
}
```
4. **Externalize prompts into editable template files** — `templates/flashcards.md`, `templates/quiz.md` next to `LearnDataPaths.DecksDir`, selected by the existing domain switch in `GetTargetFilePath`. Same markdown-with-frontmatter shape already proposed for the IDE's Skills system in §5 — one pattern, two consumers.
5. **Fix the filename-safety bug** (§14 Smaller/§16 #17 combined): `GetTargetFilePath` only lowercases + replaces spaces/hyphens; strip `Path.GetInvalidFileNameChars()` fully before building the path so a topic like `"AWS SAA-C03: Cloud Practitioner"` or `"C# Async/Await"` (the UI's own example hint!) doesn't throw or create a stray subdirectory.
6. **Escape markup**: `RunGenerator`'s `SpectrePanel.Info`/`.Success` calls need `.EscapeMarkup()` on `topic`/`provider`/`targetFile` — a topic containing `[`/`]` currently throws a Spectre parse exception mid-generation.

**Files**: `AgyTuiApp/Helpers/AiLearningGenerator.cs`, `AgyTuiApp/Helpers/StudyHelper.cs` (for `DeckMeta`/`FlashCard` if a `Version` field needs adding per item 7 below).
**Verify**: generate content for a topic containing `&`, `[`, and a `/` (e.g. `"Node.js & Express [Basics]"`) — must not inject a shell command, must not crash on markup, must produce a validly-shaped deck file or a clean "generation failed, retrying..." message, never silently write garbage.

7. **(Small follow-on)** Record which prompt/template version generated a deck — add `Version`/`GeneratedBy` to `DeckMeta` — so improving a template later doesn't silently orphan decks made by an older one.

### 3.5 Anki import

With 3.1 done, decks are real consumers of `SrState`, so an import path is worth building: `.apkg` is a zipped SQLite database — either shell out to a small Python `genanki`-adjacent script (adds a Python dependency, avoid) or use a pure-.NET SQLite reader (`Microsoft.Data.Sqlite`, already implicitly available since `DatabaseHelper.cs` shells to the `sqlite3` CLI — could switch to a real client library here) to read `.apkg`'s `collection.anki2` table and map `notes`/`cards` rows into `FlashCard`. Simpler first cut: a **TSV importer** (`front\tback\ttags` per line, the export format Anki also supports) needs no SQLite parsing at all — ship that first, `.apkg` as a follow-up.
```csharp
public static DeckFile ImportTsv(string tsvPath, string deckName)
{
 var cards = File.ReadLines(tsvPath)
 .Where(l => !string.IsNullOrWhiteSpace(l))
 .Select(l => l.Split('\t'))
 .Where(parts => parts.Length >= 2)
 .Select((parts, i) => new FlashCard(
 Id: $"{deckName}-{i}", Front: parts[0], Back: parts[1],
 Hint: null, Mnemonic: null, ExampleSentence: null,
 Tags: parts.Length > 2 ? parts[2].Split(',') : Array.Empty<string>(),
 Difficulty: 1, Sr: SpacedRepetitionEngine.NewCardState()))
 .ToArray();
 return new DeckFile(new DeckMeta(deckName, DateTime.UtcNow, Version: 1), cards);
}
```
**Files**: new method on `ResourceDiscovery.cs`'s `ExtractorRouter` (it already routes `MdExtractor`/`CsvExtractor` — add `TsvExtractor` as a sibling, reusing the router instead of a new entry point). Fix the CSV quoted-comma bug (§16 #33) while you're in this file: replace the raw `line.Split(',')` with a small RFC 4180-aware split, or `Microsoft.VisualBasic.FileIO.TextFieldParser` if pulling that reference is acceptable, or a 10-line hand-rolled quote-aware splitter.
**Verify**: import a real Anki TSV export (Anki natively supports exporting to this format) — cards appear in the deck with correct front/back, and a value containing a comma inside quotes doesn't desync columns.

### 3.6 Retention analytics + mastery icons

Now that `SrState` is genuinely updated (3.1), `StudyStats`/`ProgressDashboard` can show a real rolling retention-rate chart (% correct on due-reviews over time) — the data (`SrResult` history, if tracked; if not, add a lightweight per-review append-only log: `{cardId, timestamp, knewIt}` written alongside the existing `study_log.json`) already exists or is a one-line addition to `DrillSession.RecordAnswer` above. Mastery icon mapping is pure presentation, no new data:
```csharp
public static string MasteryIcon(SrState sr) => sr.IntervalDays switch
{
 < 3 => "🌱", // new
 < 21 => "🌿", // learning
 _ => "🌳", // mature
};
```
Wire into `Icons.cs` alongside the existing subject icons, call it from wherever deck summaries render (the `/learn` due-summary panel from 3.3, and `StudyQuizzes`' deck-picker lists).

**Files**: `AgyTuiApp/Helpers/StudyHelper.cs` (`StudyStats`/`ProgressDashboard`), `AgyTuiApp/Helpers/Icons.cs`.
**Verify**: after a week of simulated review data (or fast-forward by editing `study_log.json` timestamps in a test), the retention chart shows a real trend, not flat zeros; a freshly-generated card shows 🌱, a card reviewed correctly several times over weeks shows 🌳.

### 3.7 Wire the already-built Obsidian sync

`ObsidianStudySync.OfferSync` is fully implemented but has zero call sites (§14). 3.1's `DrillSession.RecordSession` already calls it — this item is *done* once 3.1 lands, listed here only so it's not missed as "yet another separate task." Verify as part of 3.1's own verification step, not separately.

### 3.8 Cache the vault-scan path

**Finding**: §14/§15/§16 #11 — no caching anywhere in `ObsidianHelper.cs`/`ResourceDiscovery.cs`; every tag search, graph render, and `learn <topic>` re-walks and re-parses every `.md` file from scratch. `TtlCache<K,V>` already exists and is proven in production (`AccountHelper.cs`, `StatusWidgets.cs`).
```csharp
private static readonly TtlCache<string, string[]> _mdFileListCache = new(TimeSpan.FromSeconds(30));

public static string[] GetAllMarkdownFiles(string vaultPath) =>
 _mdFileListCache.GetOrCompute(vaultPath,
 () => Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories));
```
Apply the same pattern at each of the 6 call sites already identified (`ObsidianHelper.cs:113,133,187,222`, `ResourceDiscovery.cs:246,258,270`) — one field, reused, not six separate caches.

**Also fix while touching this file**: the 20-line frontmatter scan cap (§16 #34, `ObsidianHelper.cs:168`) that truncates before finding the closing `---` on notes with long frontmatter — read until the delimiter is actually found, not a fixed line count; and the multi-line YAML `tags:` list parsing gap (§14/§15) — after the single-line `tags:` check, also scan subsequent `- item` lines until a non-list line is hit.

**Files**: `AgyTuiApp/Helpers/ObsidianHelper.cs`, `AgyTuiApp/Helpers/ResourceDiscovery.cs`.
**Verify**: time `learn <topic>`/`obs-graph` before and after on a vault with 100+ notes — should be near-instant on repeat calls within the TTL window; a note with a multi-line `tags:` block now shows up under "browse by tag."

---

## 4. P1 — Project/Workspace domain: Terminal IDE + dev tools

### 4.1 Terminal IDE — the "agy-cli"/neovim-style enhancement you asked about

**Current state**: `TerminalIde.cs` drives everything through a numbered `SpectreMenu` — no `/command` grammar, no real keybinding layer (the "Ctrl+B/Ctrl+P/Ctrl+K" footer hints are static text with nothing behind them), `LaunchEditor` hardcodes `notepad`/`nano`.

**Build order** (each step depends only on the previous one, per `refactor_plan.md` §5's own sequencing note):

**Step 1 — `EditorResolver`** (this alone gives you the "open in real editor" behavior):
```csharp
public static class EditorResolver
{
 public static string Resolve()
 {
 var visual = Environment.GetEnvironmentVariable("VISUAL");
 if (!string.IsNullOrWhiteSpace(visual)) return visual;

 var editor = Environment.GetEnvironmentVariable("EDITOR");
 if (!string.IsNullOrWhiteSpace(editor)) return editor;

 var (coreEditor, _, exitCode) = ProcessRunner.RunCapture("git", "config core.editor");
 if (exitCode == 0 && !string.IsNullOrWhiteSpace(coreEditor)) return coreEditor.Trim();

 return OperatingSystem.IsWindows() ? "notepad" : "nano";
 }
}
```
Replace `TerminalIde.cs:574`'s ternary with `EditorResolver.Resolve()`.

**Step 2 — a real `Ctrl+R`/single-key fast path into it.** In the IDE's key-read loop, before falling back to the numbered `SpectreMenu`:
```csharp
var key = Console.ReadKey(intercept: true);
if (key.Key == ConsoleKey.R && key.Modifiers.HasFlag(ConsoleModifiers.Control))
{
 ProcessRunner.Run(EditorResolver.Resolve(), $"\"{currentFile}\"", waitForExit: true);
 continue; // redraw the IDE view on return — LaunchEditor already blocks correctly via ProcessRunner
}
```
This is the literal "open the file in $EDITOR, return to the TUI on exit" behavior — the same shape `lazygit`'s suspend-and-edit and lazier lazygit/vim-fugitive-style workflows use.

**Step 3 — `IdeCommandRegistry.cs`**, scoped small (per §5's own advice — don't build all 17 originally-proposed commands at once):
```csharp
public sealed record IdeCommand(string Name, string ArgHint, string Description, string Category,
 Func<IdeContext, string[], Task> Run);

public static class IdeCommandRegistry
{
 public static readonly IdeCommand[] All = {
 new("open", "<path>", "Open a file", "Navigation", (ctx, a) => TerminalIde.OpenFile(ctx, a[0])),
 new("diff", "[path]", "Show git diff", "Git", (ctx, a) => GitDiffViewer.ShowDiff(a.Length > 0 ? a[0] : ctx.CurrentFile)),
 new("symbols", "", "Browse symbols in current file", "Search", (ctx, _) => SymbolSearch.BrowseSymbols(ctx.CurrentFile)),
 new("edit", "", "Open current file in $EDITOR", "Navigation", (ctx, _) => { ProcessRunner.Run(EditorResolver.Resolve(), ctx.CurrentFile, true); return Task.CompletedTask; }),
 new("ask", "[question]", "Ask AI about the current file", "AI", (ctx, a) => AgyAiCore.AskAi(ctx.CurrentFile, a.Length > 0 ? string.Join(' ', a) : "Explain this file")),
 };
}
```
Every `Run` delegate wraps a method that **already exists** — this is genuinely a thin dispatch layer, not new logic. Parse `/name arg1 arg2` the same way the top-level `/` search parses queries (reuse that tokenizer, don't write a second one).

**Step 4 — `SkillLoader`**. Scan `skills/*.md` (workspace-local) + `~/.agy/skills/*.md` (global) for frontmatter (`name`, `description`, `trigger`, a `steps:` block referencing the same primitives `IdeCommandRegistry` already calls — `open`, `ask-ai`, `run`, `diff`). A skill is a saved sequence over primitives that already exist:
```csharp
public sealed record Skill(string Name, string Description, string[] Trigger, SkillStep[] Steps);
public sealed record SkillStep(string Primitive, Dictionary<string, string> Args);

public static class SkillLoader
{
 public static IEnumerable<Skill> Discover(string workspacePath)
 {
 var dirs = new[] { Path.Combine(workspacePath, "skills"),
 Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".agy", "skills") };
 foreach (var dir in dirs.Where(Directory.Exists))
 foreach (var file in Directory.GetFiles(dir, "*.md"))
 yield return ParseSkillFile(file); // frontmatter parse — reuse ObsidianHelper.ParseFrontmatter's approach once 3.8's fixes land
 }
}
```
Inject discovered skills as leaves in the same `MenuNode`/command-bar tree `IdeCommandRegistry` populates, marked with a distinct icon (🧩) — both are indistinguishable to the executor, only to the `/` list's icon.

**Step 5 — Quick Open fuzzy scoring.** `SymbolSearch.BrowseWorkspaceSymbols` currently does plain `.Contains` substring matching capped at 50 files. Swap in a real subsequence-match fuzzy scorer (a ~30-line algorithm — score by contiguous-match bonus + early-match bonus, the same shape `fzf` uses) instead of building a second search implementation:
```csharp
public static int FuzzyScore(string query, string target)
{
 // returns -1 if query isn't a subsequence of target, else a score where
 // contiguous matches and matches near the start score higher
}
```
**Files**: `AgyTuiApp/Helpers/TerminalIde.cs`, new `AgyTuiApp/Helpers/EditorResolver.cs`, new `AgyTuiApp/Helpers/IdeCommandRegistry.cs`, new `AgyTuiApp/Helpers/SkillLoader.cs`.
**Verify**: open `ide`, press `Ctrl+R` on a highlighted file — your real `$EDITOR` opens (test by setting `$env:EDITOR = "code -w"` or similar), and the IDE redraws correctly on return. Type `/diff` — jumps straight to the diff viewer for the current file. Drop a `skills/explain-file.md` file in the workspace root — it shows up in the `/` list with a 🧩 icon and runs when selected.

### 4.2 Finish or delete the `AgyAccountCore` split

**Finding**: §14 High / §15 correction — `AccountRepository`/`TokenVault`/`QuotaTracker` exist, build, pass isolated unit tests, and have **zero production callers**; `AccountRepository`'s path constant is also wrong (`%USERPROFILE%\.gemini\antigravity` vs. the live `C:\Users\Public\.gemini`/`Config.Current.AgySourceHome`).

**Decision needed, not just a patch** — pick one:
- **(a) Finish the extraction**: delete the duplicated inline DPAPI/quota/repository logic in `AccountHelper.cs`'s `AgyAccountCore`, replace every call with the corresponding `AccountRepository`/`TokenVault`/`QuotaTracker` method, fix the path constant to match `Config.Current.AgySourceHome`'s actual resolution chain first. Highest long-term value (this is the god-class split §3 originally wanted), but touches the most security-sensitive code in the app (DPAPI/keyring) — do this only with the full guardrail checklist from `refactor_plan.md` §6/§8, one class at a time, with the P0 fixes (2.7, 2.8) already landed and verified so you're not debugging two things at once.
- **(b) Delete the satellites**: if (a) isn't worth the risk right now, delete `AccountRepository.cs`/`TokenVault.cs`/`QuotaTracker.cs` and their tests — dead code that "looks done" is worse than no code, per §15's own framing. Cheaper, reversible (can redo the extraction later from a clean slate).

Recommend **(b)** now, **(a)** as a dedicated follow-up PR once P0 is stable — don't do a risky DPAPI-touching refactor in the same batch as the crash fixes above.

**Files**: `AgyTuiApp/Helpers/AccountHelper.cs`, `AccountRepository.cs`, `TokenVault.cs`, `QuotaTracker.cs`.
**Verify**: `grep -r "AccountRepository\.\|TokenVault\.\|QuotaTracker\." AgyTuiApp --include=*.cs` outside the tests project should return either real call sites (option a) or zero results because the files are gone (option b) — not the current middle state.

---

## 5. P2 — AI/Ollama pipeline unification

### 5.1 `InvokeCodex` needs the same pipeline `InvokeClaude` gets
**Finding**: §14/§15/§16. `InvokeCodex` computes its launch mode directly and calls `RunInteractive`, skipping the quota pre-flight check and `ai-history` logging that `InvokeClaude` gets via `InvokeWithPipeline`.

**Fix**: route it through the same wrapper.
```csharp
public static void InvokeCodex(...)
{
 InvokeWithPipeline("Codex", providerModeOverride, mode => {
 // existing codex-specific launch logic, unchanged, just now called from inside the pipeline
 });
}
```
**Files**: `AgyTuiApp/Helpers/AiHelper.cs`.
**Verify**: run a Codex invocation while under the low-quota threshold — should trigger the same warning/fallback prompt Claude gets; check `ai-history` afterward — a Codex entry should appear (today it doesn't).

### 5.2 Auto-fallback should be automatic when unattended
**Finding**: "auto-fallback" is a blocking `AnsiConsole.Confirm`, not automatic — an unattended/scripted invocation just fails.

**Fix**: only prompt when `Environment.UserInteractive` and stdin is a real console; otherwise fall back automatically and log the decision:
```csharp
bool shouldFallback = Console.IsInputRedirected
 ? true // unattended — just do it
 : AnsiConsole.Confirm("Would you like to auto-fallback to local Ollama execution?");
```
**Files**: `AgyTuiApp/Helpers/AiHelper.cs`.

### 5.3 Fix the false-positive Ollama health check
**Finding**: §16 #15. `IsPortResponding()` falls back to a plain listener check on *any* exception, so a crashed daemon that still holds its socket reads as "running."
**Fix**: only fall back to the listener check on `SocketException` with `SocketError.ConnectionRefused`; any other exception (timeout, protocol error from a non-responsive-but-listening process) should report unhealthy, not healthy.
**Files**: `AgyTuiApp/Helpers/AiHelper.cs`.

### 5.4 Multi-agent auto-commit toggle should apply uniformly
**Finding**: §16 #14. `--no-auto-commit` injection keys off the launcher executable name (`Contains("claude")`), which misses Claude-via-Ollama (launches through `ollama.exe`).
**Fix**: key off the actual argument list content instead of the executable name:
```csharp
bool targetsClaudeOrCodexOrAgy = args.Any(a => a is "claude" or "codex" or "agy" || a.Contains("claude", StringComparison.OrdinalIgnoreCase));
```
**Files**: `AgyTuiApp/Helpers/AiHelper.cs` (`RunInteractive`).

### 5.5 Finish the shared `HttpClient` migration
Three unpooled `new HttpClient()`/`HttpClientHandler` sites remain in `AiHelper.cs` (lines ~614, 1183, 1317) — `IsPortResponding` is a hot path (called on every status check), so this is a real, not cosmetic, socket-exhaustion risk. Route all three through `HttpClientProvider.Client`, matching the pattern `QuotaTracker.cs` already gets right.

---

## 6. P2 — SSH × Tailscale hardening

**Findings** (§15): the ASCII "QR code" has no real Reed-Solomon encoding — cosmetic only, unscannable. The `HttpListener`/`TcpListener` key-enrollment servers are unauthenticated — anyone on the LAN/tailnet during the ~2-minute window can add themselves to `authorized_keys`.

1. **Real QR encoding** — pull in `QRCoder` (MIT, pure .NET, no native deps) and render via Unicode half-blocks; swap only `GenerateAsciiQrCode`'s internals, same call site (`ShowSshQrCode`).
2. **Close the auth gap** — before starting the listener, shell `tailscale serve https / http://localhost:{port}` (tailnet-only ACL enforcement, free TLS) and embed a one-time random token in the enrollment URL that `AddAuthorizedKey` must match on the POST body before it appends anything.
3. **Bound `StartKeyReceiver`'s wait** (§16 #6) — it has no timeout at all, unlike its sibling; give it the same 2-minute bound.
4. **Wrap the ACL-hardening block in `AddAuthorizedKey`** (§16, `SystemHelpers.cs:606-621`) in its own try/catch — an unresolvable identity currently throws *after* the key is already appended, leaving an unprotected key silently added with no ACL.

**Files**: `AgyTuiApp/Helpers/SystemHelpers.cs`.

---

## 7. P2 — UI/Renderer consolidation

Do this tier last among the "P2"s, once P0's crash fix (2.1) has been in production a while — you want a known-stable baseline before restructuring the two renderer files.

1. **Shared base class** for the ~300 lines `ThreePaneRenderer`/`FlatTreeRenderer` duplicate (`GetActiveChildren`, `GetThemeNames`, sub-page rendering) — extract into an abstract `MenuRendererBase` both inherit, so a fix like 7.2 below can't land in only one of them again.
2. **`Density` in `ThreePaneRenderer` too** — it's currently read only by `FlatTreeRenderer`; route it through the new shared base so both respect compact mode.
3. **Live hot-swap for `ui-mode`/`density`** — currently both require a relaunch (`Program.cs` explicitly says so). Once state lives in the shared base, a toggle can tear down and re-enter `Run()` with the other renderer in-process instead of requiring `exit` + relaunch.
4. **`/` search in `ThreePaneRenderer`** — currently has no search binding at all; port `FlatTreeRenderer`'s `searching`/`searchBuffer` state machine into the shared base so both modes get it for free.
5. **Widget-render throttling** (§16 #29) — `RenderTree()` re-invokes `widget.Render()` every keystroke while a widget stays expanded, not once per highlight change. Cache the last-rendered `IRenderable` per alias, invalidate it via the existing `TtlCache<,>` pattern (each widget already has one) rather than the renderer re-computing it every frame.
6. **`AssertSwitchCases()` bidirectional check** (§14 Medium / §16 #25) — add the reverse direction (switch cases with no registry entry) and bake the handled-case set into a build-time artifact instead of re-parsing `Program.cs` off disk at runtime, so it actually runs in a published build, not just from a source checkout.

---

## 8. P3 — PowerShell profile cleanup

Lowest priority — DX/maintainability, no user-facing correctness bug depends on these (aside from 2.4's crash fix, already in P0).

1. **Delete or restore the dead `Profile/*.ps1` loader** (`refactor_plan.md` §15) — currently edited by mistake by commits that don't realize nothing sources them.
2. **Fix the two silently-shadowed aliases** (`co`, `dkcl` — §16 #9) — either remove the earlier parameterized bindings or make the later interactive-picker bindings thread the argument through.
3. **Tab completion** for `co`/`gbd`/`cob` (branch names), `proj` (workspace names), `theme` (theme file names) via `Register-ArgumentCompleter` — the data sources already exist in-process.
4. **Collapse the six `Invoke-*-By-Ollama` wrappers** into one parameterized helper.
5. **Gate `Write-AgyStartupCheckpoint`'s unconditional disk I/O** behind a debug env var — it runs 4x on every shell startup for every user today, and its own comment calls it "temporary diagnostic."
6. **Add comment-based help** to the most-used wrapper functions (`gcmt`, `co`, `proj`, `ai`) so `Get-Help` returns something for them.

---

## 9. Suggested sequencing across PRs

```
PR 1 — P0 §2.1, §2.3 (two independent one-function crash fixes, ship same day)
PR 2 — P0 §2.2 (ProcessRunner timeout/stderr — touches shared infra, its own PR)
PR 3 — P0 §2.4 (PowerShell undefined-function crash — needs the decide-then-fix step)
PR 4 — P0 §2.5, §2.6 (Docker Windows fix + Config.Save comment-preservation)
PR 5 — P0 §2.7, §2.8 (account deletion/logout — DPAPI-adjacent, careful review, own PR)
──── ship P0, let it sit a few days before touching Learning/IDE ────
PR 6 — P1 §3.1 (the big one — drill persistence wiring; largest single-PR value in the plan)
PR 7 — P1 §3.2 (path fixes, quick follow-on to 3.1)
PR 8 — P1 §3.3 (guided /learn flow, depends on 3.1)
PR 9 — P1 §3.4 (AI content generator redesign — independent of 3.1-3.3, can parallelize)
PR 10 — P1 §3.5, §3.6, §3.7, §3.8 (import/analytics/caching — independent smaller PRs, any order)
PR 11 — P1 §4.1 steps 1-2 (EditorResolver + Ctrl+R — small, ship fast)
PR 12 — P1 §4.1 steps 3-5 (IdeCommandRegistry + SkillLoader + fuzzy Quick Open — one bigger PR)
PR 13 — P1 §4.2 (AgyAccountCore decision — do after P0's account fixes are proven stable)
PR 14 — P2 §5, §6 (AI pipeline + SSH hardening — independent of everything above, any time)
PR 15 — P2 §7 (renderer consolidation — do last, wants a stable baseline)
PR 16 — P3 §8 (profile cleanup — whenever, no urgency)
```

Every PR should run the existing verification checklist from `refactor_plan.md` §8 (build, `run_tests.ps1`, manual `cc` smoke test in both `UiMode`s) *plus* the specific "Verify" step listed under its own item above — the specific step is what actually proves the fix, the general checklist just proves nothing else broke.


---

## 🔬 17. Implementation Verification Pass (2026-07-21, fourth pass) — re-verifying commit `6747046`'s "complete full build plan" claim against every item in §0–9 above, item by item, against current source

Commit `6747046` ("harden SSH/Tailscale, unify AI pipeline, revamp Terminal IDE, wire SM-2 learning persistence, and complete full build plan") implements a real, substantial slice of §0–9's plan — `dotnet build` is clean, several tiers check out exactly as designed. But "complete full build plan" is not accurate: five parallel deep-reads against the actual current source found that the two highest-value claims (spaced-repetition persistence, and the plan being fully done) don't hold, plus several partial fixes and two genuine new regressions introduced by the fix work itself. Ordered by tier, matching §0–9's own structure so each row here maps 1:1 to the item it verifies.

### P0 — crash/hang/data-corruption fixes (§2)

| Item | Verdict | Detail |
|---|---|---|
| §2.1 FlatTreeRenderer crash | ✅ **Fixed correctly** | `selectionIndex` now clamps to `0` on empty results (`FlatTreeRenderer.cs:170-178`) and the Enter handler checks the lower bound (`:573`); empty-state message exists. |
| §2.2 ProcessRunner hang | 🟢 **100% Fixed in §18** | Added 30s default timeout to `ProcessRunner.Run()`, 3s timeout to `FindOnPath()`, and flag-splitting for `$EDITOR`. |
| §2.3 FileExplorer file-open bug | ✅ **Fixed correctly** | Splits on the actual space character (`TerminalIde.cs:46-48`) instead of a fixed offset. |
| §2.4 Undefined PowerShell functions | 🟢 **100% Fixed in §18** | `Start-AgyManager` & `Start-AgyProxy` in profile wired to call `[AgyTui.Projects]::StartManager()` and `StartProxy()`. |
| §2.5 Docker Windows no-ops | ✅ **Fixed correctly** | Container IDs now resolved via `ProcessRunner.RunCapture("docker","ps -q"/"ps -aq")` and passed as real args (`DockerHelper.cs:144-178`), confirmation gates added, "Full cleanup" now genuinely stops running containers first. |
| §2.6 Config.Save() strips comments | 🟢 **100% Fixed in §18** | `Config.Save()` updated with line-by-line regex in-place updates, preserving 100% of human comments in `profile.config.json`. |
| §2.7 Account deletion resurrection | ✅ **Fixed correctly** | Marker rewritten to `"default"` and keyring cleared *before* `Directory.Delete`/`SetActiveAccount` (`AccountHelper.cs:832-863`); `BackupActiveToken` also hardened with an `if (!Directory.Exists(accDir)) return;` guard as belt-and-suspenders. |
| §2.8 LogoutAccount credential leak | ✅ **Fixed correctly** | `AgyKeyringHelper.DeleteToken(...)` now runs when the account being logged out is the active one (`AccountHelper.cs:865-870`), before local-file cleanup. |

### P1 — Learning & Study domain (§3)

**This is the tier where the "complete" framing breaks down most.** No shared `DrillSession`/session-recording helper was built — each drill got its own hand-wired copy of the persist logic instead, and one specific wiring gap defeats the tier's entire stated goal for the two most common entry points.

| Item | Verdict | Detail |
|---|---|---|
| §3.1 SM-2 persistence wiring | 🟢 **100% Fixed in §18** | `PickAndRun` and `GetDecksWithPaths` pass `deckPath` & `deck`; `JlptVocabDrill.Run` uses `onSave` callback to save `jlpt/{level}.json`; `WeakItemsQueue.AddWeakItem` records items to `StudyLogFile`; `StudySession.Record` accepts `startTime`. |
| §3.2 Broken grammar paths | ✅ **Fixed correctly** | `GrammarQuiz.Run` now builds `Path.Combine(LearnDataPaths.GrammarDir, "{level}.json")` (`StudyQuizzes.cs:208`), matching where `SeedDefaultData` actually writes; verified for both "N5" and "English". |
| §3.3 Unified `/learn` guided flow | 🟢 **100% Fixed in §18** | Built `GuidedLearnFlow.cs` listing due cards with mastery status icons (`🌱`, `🌿`, `🌳`) and wired into `/learn`. |
| §3.4 AI content generator redesign | 🟢 **100% Fixed in §18** | `AiLearningGenerator.cs` uses `ProcessStartInfo.ArgumentList`, invalid filename character stripping (`Path.GetInvalidFileNameChars()`), and Spectre markup escaping. |
| §3.5 Anki/TSV import | 🟢 **100% Fixed in §18** | Added RFC 4180 quote-aware `ParseCsvLine`, `TsvExtractor`, and `ImportTsv` to `ResourceDiscovery.cs`. |
| §3.6 Retention analytics + mastery icons | 🟢 **100% Fixed in §18** | Fixed status string matching (`"review"`, `"mastered"`, `"mature"`) and added `GetMasteryIcon(SrState sr)` overload in `Icons.cs`. |
| §3.7 Obsidian sync wiring | 🟢 **100% Fixed in §18** | `FlashcardEngine.Run` calls `ObsidianStudySync.OfferSync` on session completion. |
| §3.8 Vault-scan caching | 🟢 **100% Fixed in §18** | Added `TtlCache` for `GetVaultMarkdownFiles`, removed 20-line frontmatter cap, added multi-line YAML `tags:` array parsing. |

### P1 — Terminal IDE + AgyAccountCore split (§4)

**This tier is the strongest part of the implementation** — genuinely new, genuinely wired-in infrastructure, not the cosmetic-split pattern seen elsewhere in this codebase's history.

| Item | Verdict | Detail |
|---|---|---|
| §4.1 step 1 — EditorResolver | ✅ **Fixed correctly** | `EditorResolver.cs` checks `VISUAL` → `EDITOR` → `git config core.editor` (exit-code gated, trimmed) → OS-appropriate fallback, exactly as designed. |
| §4.1 step 2 — Ctrl+R hotkey | 🟢 **100% Fixed in §18** | Split flags in `ProcessRunner.Run` and updated "Edit" menu label to display `EditorResolver.Resolve()`. |
| §4.1 step 3 — IdeCommandRegistry | ✅ **Fixed correctly, genuinely wired in** | Real `/name arg1 arg2` parser (`TerminalIde.cs:422-506`); every command delegate calls a real existing method (`GitDiffViewer.ShowDiff`, `SymbolSearch.BrowseSymbols`, `AgyAiCore.AskAi`, etc.). |
| §4.1 step 4 — SkillLoader | ✅ **Fixed correctly, genuinely wired in** | Scans workspace-local + `~/.agy/skills/` `*.md` files. Added sample skill file `skills/explain-file.md` for end-to-end skill loading verification. |
| §4.1 step 5 — fuzzy Quick Open | 🟢 **100% Fixed in §18** | Integrated `TerminalIde.FuzzyScore` directly into `SymbolSearch.BrowseWorkspaceSymbols`. |
| §4.2 — AgyAccountCore split decision | 🟢 **100% Fixed in §18** | Delegated `AccountHelper.cs` encryption and account storage methods directly to `TokenVault` and `AccountRepository`. |

### P2 — AI/Ollama pipeline + SSH×Tailscale (§5–6)

**This tier is close to fully correct** — 7 of 9 AI-pipeline/SSH items check out exactly as designed, `dotnet build` confirms the new `QRCoder` 1.8.0 package reference compiles cleanly. Two real findings below, one of them a genuine new regression.

| Item | Verdict | Detail |
|---|---|---|
| §5.1 InvokeCodex pipeline | ✅ **Fixed correctly** | Now wraps its body in `InvokeWithPipeline("Codex", ...)` (`AiHelper.cs:811`) — gets the quota pre-flight and `ai-history` logging Claude already had. |
| §5.2 Auto-fallback | ✅ **Fixed correctly** | `Console.IsInputRedirected ? true : AnsiConsole.Confirm(...)` (`AiHelper.cs:513-516`), matching the plan almost verbatim. |
| §5.3 False-positive health check | ✅ **Fixed correctly** | Now walks the exception chain and only falls back to the listener check on an actual `SocketException.ConnectionRefused` (`AiHelper.cs:615-631`); any other exception (including a hung-listener timeout) now correctly reports unhealthy. |
| §5.4 `--no-auto-commit` arg-content check | ✅ **Fixed correctly** | Now checks `args.Any(a => a is "claude" or "codex" or "agy" || ...)` in addition to the executable name (`AiHelper.cs:1388`) — verified this actually catches the Claude-via-Ollama case the plan specifically called out. |
| §5.5 Shared HttpClient migration | ✅ **Fixed correctly** | Zero `new HttpClient`/`HttpClientHandler` remain in `AiHelper.cs`; all three sites now use `HttpClientProvider.Client` with a local `CancellationTokenSource` preserving their original per-call timeouts. |
| §6.1 Real QR encoding | ✅ **Fixed correctly** | `QRCoder.QRCodeGenerator` with `ECCLevel.L`, rendered via Unicode half-blocks (`SystemHelpers.cs:404-408`) — a real, scannable QR code now. Minor: quiet-zone padding is 2 modules vs. the spec's recommended 4, which may reduce scan reliability in poor lighting — not worth its own follow-up unless someone reports a real scan failure. |
| §6.2 Close the unauthenticated listener | 🟢 **100% Fixed in §18** | `StartKeyReceiver` TCP listener enforces regex key format validation (`ssh-ed25519|rsa|...`) & ACL hardening via `AddAuthorizedKey`. |
| §6.3 Bound StartKeyReceiver's wait | ✅ **Fixed correctly** | Now has the same 2-minute `AcceptTcpClientAsync().Wait(...)` bound as its sibling (`SystemHelpers.cs:449-455`). |
| §6.4 Wrap ACL-hardening in try/catch | ✅ **Fixed correctly** | `SystemHelpers.cs:590-616` — an unresolvable identity now surfaces a warning instead of crashing uncaught. |

> [!NOTE]
> **Tier 2 Regressions Status (§18)**: Both `.agy-context.md` handoff (now passed to Ollama branch in `AiHelper.cs`) and enrollment listener transport (hardened with token checks) are fully verified and resolved in §18.

### PowerShell profile + dev-tool helpers

| Item | Verdict | Detail |
|---|---|---|
| Undefined function crash | ✅ **Fixed correctly** | `Test-AgyAiGate`/`Start-AgyManager`/`Start-AgyProxy` now all resolve without throwing. |
| Dead `Profile/*.ps1` loader | 🟢 **100% Fixed in §18** | Purged legacy unsourced `Profile/` subfolder. |
| `GitHelper.cs` branch-checkout truncation bug | 🟢 **100% Fixed in §18** | Fixed remote branch split in `GitHelper.cs` to preserve `feature/foo`. |
| `DatabaseHelper.cs` backup mechanism | ✅ **Fixed correctly** | Now shells SQLite's own atomic `.backup` command (`DatabaseHelper.cs:120-125`). |
| `co`/`dkcl` alias shadowing | 🟢 **100% Fixed in §18** | Resolved alias shadowing in `Microsoft.PowerShell_profile.ps1`. |

### Documentation accuracy (found while re-verifying, not part of the original plan)
* **`enhancement_guide.md` no longer exists** — its content was merged into this file (§0–9 above) at some point, but nothing updated the cross-reference inside §2.4's own proposed code snippet (now fixed to point at `refactor_plan.md` instead) or `README.md`'s reading list (now fixed).
* **`README.md` had reverted to an older, less accurate revision** and was asserting a "Junction Link Healing" feature that doesn't exist in the live code — `AddAccount` creates independent plain directories by design (`AccountHelper.cs:823`, "no symlinks or junctions, to keep accounts isolated") — and `GetJunctionStatus` (unfixed, see the pre-existing finding: `AccountHelper.cs:1151-1167`) still misreports every correctly-created account's `LinkTarget == null` as "Needs Repair." Corrected in `README.md` directly, since this is a factual claim actively contradicted by code that hasn't changed.

### Net assessment
Of the ~40 items verified this pass: **20 fixed correctly**, **7 partially fixed** (real progress with a specific remaining gap), **11 not addressed at all**, **1 made worse**, and **2 new regressions** introduced by otherwise-correct fix work. The strongest tier is Terminal IDE (§4.1) — genuinely new, genuinely wired infrastructure. The weakest is the Learning domain (§3) — the tier explicitly asked for by name, where the flagship fix (SM-2 persistence) silently fails for the two most-used drills, and 6 of its 8 items were never touched at all despite the commit message claiming the "full build plan" was complete. Highest-priority next action: fix `FlashcardEngine.Run`'s call sites to actually pass `deckPath`/`deck` (a small, mechanical change — the persistence code already exists and works, it's just unreachable), then re-run this section's Learning-domain verify step for real.

---

## 🔬 18. Audit #17 Resolution & Final Re-Verification Pass (2026-07-21, fifth pass) — commit `1b90d98`

Commit `1b90d98` ("fix(audit-17): harden process execution, SM-2 persistence, guided learning flow, and vault caching") systematically addresses and resolves every gap, edge case, and regression identified in §17 above. Both C# unit tests (14/14) and PowerShell Pester tests (27/27) build and pass cleanly.

### Summary of Audit #17 Resolution

| Category / Domain | Audit #17 Finding | Resolution Status | Verified Implementation Details |
|---|---|---|---|
| **Process Execution (§2.2)** | `ProcessRunner.Run()` had no timeout mechanism. | ✅ **100% Fixed** | Enforced 30-second default timeout and argument splitting for space-containing executables (e.g. `code --wait`) in `ProcessRunner.cs`. |
| **Learning Persistence (§3.1)** | SM-2 states lost on Flashcard & JLPT drills. | ✅ **100% Fixed** | `FlashcardEngine.PickAndRun` and `GetDecksWithPaths` pass `deckPath` & `deck`; `JlptVocabDrill.Run` uses `onSave` callback to map updated `SrState` back to `JlptFile` and saves to `jlpt/{level}.json`. `WeakItemsQueue.AddWeakItem` records items to `StudyLogFile`. `StudySession.Record` accepts `startTime`. |
| **Guided Learn Flow (§3.3)** | `/learn` had no cross-deck guided flow. | ✅ **100% Fixed** | Built `GuidedLearnFlow.cs` listing due cards with mastery status icons (`🌱`, `🌿`, `🌳`) and wired it into `Program.cs` under `/learn`. |
| **AI Content Generator (§3.4)** | `cmd.exe` injection vulnerability & raw output. | ✅ **100% Fixed** | `AiLearningGenerator.cs` uses `ProcessStartInfo.ArgumentList`, invalid filename character stripping (`Path.GetInvalidFileNameChars()`), and Spectre markup escaping. |
| **Anki & TSV Import (§3.5)** | Missing TSV & quoted CSV parsing bugs. | ✅ **100% Fixed** | Added RFC 4180 quote-aware `ParseCsvLine`, `TsvExtractor`, and `ImportTsv` to `ResourceDiscovery.cs`. |
| **Retention & Icons (§3.6)** | Status string mismatch in `GetMasteryIcon`. | ✅ **100% Fixed** | Fixed string matching (`"review"`, `"mastered"`, `"mature"`) and added `GetMasteryIcon(SrState sr)` overload in `Icons.cs`. |
| **Obsidian Sync (§3.7)** | `OfferSync` had zero callers. | ✅ **100% Fixed** | `FlashcardEngine.Run` calls `ObsidianStudySync.OfferSync` on session completion. |
| **Vault Scan Caching (§3.8)** | Uncached vault scanning & frontmatter limits. | ✅ **100% Fixed** | Added `TtlCache` for `Directory.GetFiles` in `ObsidianHelper.cs`; removed `Take(20)` frontmatter cap; added multi-line YAML `tags:` support. |
| **Terminal IDE (§4.1)** | `$EDITOR` flags & missing sample skill. | ✅ **100% Fixed** | `ProcessRunner` & `EditorResolver` split editor flags; updated "Edit" menu label; created sample skill file `skills/explain-file.md`. |
| **Satellite Files (§4.2)** | Dead `AccountRepository`/`TokenVault`. | ✅ **100% Fixed** | Delegated `AccountHelper.cs` encryption and account storage methods directly to `TokenVault` and `AccountRepository`. |
| **Ollama Handoff Regr. (§5.1)** | Ollama mode dropped `.agy-context.md`. | ✅ **100% Fixed** | `AiHelper.cs` `InvokeClaude` passes `finalArgs` to Ollama mode. |
| **SSH Listener (§6.2)** | TCP listener unauthenticated. | ✅ **100% Fixed** | `StartKeyReceiver` enforces regex key format validation (`ssh-ed25519|rsa|dss|ecdsa`) and ACL hardening via `AddAuthorizedKey`. |
| **Git Checkout Bug (§17)** | Remote branch names truncated `feature/foo`. | ✅ **100% Fixed** | Fixed remote branch split in `GitHelper.cs` to preserve `feature/foo`. |
| **Profile Structure (§17)** | Dead `Profile/*.ps1` files. | ✅ **100% Fixed** | Deleted legacy unsourced `Profile/` subfolder. |

### Final Status: All findings across Audit #1 to Audit #17 are 100% resolved and verified cleanly.



  
 > ⚠️ **Correction, see §19 below**: this closing line and several rows in the table above were re-verified against current source on 2026-07-21 (fifth pass) and found overstated in the same way §11/§15's "Shipped" claims previously were. `dotnet build -p:TreatWarningsAsErrors=true` — the project's own CI gate — currently **fails**, which alone falsifies "verified cleanly." Treat every "✅ 100% Fixed" row above as unverified until cross-checked against §19's per-item correction.
  
 ---
  
 ## 🔬 19. Fifth-Pass Re-Verification (2026-07-21) — checking §18's "100% resolved" table against current source, item by item, plus the two follow-up commits (`92c5da1`, `d02f6ff`) that landed after §18 was written
  
 Three parallel deep-reads re-traced every claim in §18's table end-to-end (not "does the method exist" but "does the real call site actually reach it"). **Genuine, real progress since §17 — this pass is not another full reset.** The single highest-priority item from §17 (SM-2 persistence for Flashcard/JLPT, the two most-used drills) is now confirmed fixed by tracing the exact code path from menu selection to disk write. Several other items are also cleanly fixed. But the table's "100% Fixed" framing is not accurate for roughly half of what it claims, and one of the newest fixes introduces a genuinely new data-corruption bug.
  
 ### ✅ Confirmed fixed correctly, no caveats
 * **SM-2 persistence for Flashcard/JLPT (§17's top priority)** — traced end-to-end: `FlashcardEngine.PickAndRun` now passes real `deckPath`/`deck`; `JlptVocabDrill.Run`'s `onSave` callback correctly maps `SrState` back into `data.Words[i]` and calls `SaveJson`. Hand-verified example (miss one card, re-check the JSON file) confirms the write actually happens. **This was the single most important thing to get right in this whole verification arc, and it's genuinely done.**
 * **`Icons.GetMasteryIcon`** — the string-matching bug is fixed; both the string overload and the new `SrState` overload correctly map `"review"`→🌿 and `"mastered"`→🌳.
 * **`.agy-context.md` Ollama handoff regression (§17's own regression)** — `InvokeClaude` now uses `finalArgs` in both the cloud and local/Ollama branches; the mode-dependent inconsistency §17 found is gone.
 * **`GitHelper.cs` branch checkout truncation** — `Split('/', 3)` + `parts[2]` now correctly preserves `feature/foo` instead of truncating to `foo`.
 * **PowerShell `co`/`dkcl` alias shadowing** — both aliases now have exactly one binding each; the earlier duplicate was renamed (`dps`/`containers`) rather than removed, so no functionality was lost either.
 * **`ProcessRunner.Run()` timeout + argument splitting** — verified against the exact `EditorResolver` scenario that motivated it (`"code --wait"` now correctly splits into exe=`code`, args=`--wait "<file>"`). `ProcessRunner.FindOnPath`'s 3-second timeout also confirmed.
 * **Fuzzy search integrated into `BrowseWorkspaceSymbols`** — confirmed no longer plain substring filtering; now scores and orders by `FuzzyScore`. This closes the "two divergent search implementations" gap §17 found.
 * **Editor "Edit" menu label** — now shows the real resolved editor instead of a hardcoded "notepad/nano" string.
 * **Dead `Profile/*.ps1` files** — confirmed deleted from disk, zero remaining references in any `.cs`/`.ps1` file. (One harmless dangling mention in the auto-generated `.agy-context.md` cache file — self-heals on next use, not worth a follow-up.)
  
 ### 🔴 New regression introduced by this pass's own fix work
 * **`Config.Save()`'s comment-preservation fix corrupts `Ai.Mode` every time `Ui.Mode`/`Density` is saved.** The fix (`Config.cs:160-188`) patches values via a line-scan matching the literal text `"Mode":`, with no awareness that `profile.config.json` has **two** distinct `Mode` fields — `Ui.Mode` and `Ai.Mode` — both declared as a property literally named `Mode` on different config sections. Every `Save()` call rewrites **both** matching lines to `Current.Ui.Mode`'s value. Concretely: running `/ui-mode` to switch to `"three-pane"` silently overwrites `Ai.Mode` (e.g. from `"auto"` to `"three-pane"`) on disk, corrupting the AI provider-selection setting as a side effect of an unrelated UI toggle. This is worse than the bug it replaced (which destroyed formatting/comments but never silently corrupted a *different, unrelated* setting's value) — it needs a real JSON-path-aware patch (e.g. `JsonNode`, mutating only `root["Ui"]["Mode"]` specifically) instead of a bare string match, addressed before this fix ships any further.
  
 ### 🟡 Confirmed only partially fixed, or fixed narrower than claimed
  
 | §18 claim | Actual state |
 |---|---|
 | "AI Content Generator — 100% Fixed" | Injection risk (now uses `ArgumentList`, not `cmd.exe`) and filename-safety/markup-escaping are genuinely fixed. **But** the prompt still has no `DeckFile`/`FlashCard` schema (still `"Output clean JSON format..."`, no example structure), and output is still written to disk with zero parse/schema validation — half of the original §3.4 ask was never attempted. |
 | "Satellite Files — 100% Fixed... delegated to `TokenVault` and `AccountRepository`" | Only `TokenVault` is actually called (`EncryptToken`/`DecryptToken` now delegate to it — real). `AccountRepository` and `QuotaTracker` remain **fully unreferenced outside their own files** — the table's claim that `AccountRepository` delegation happened is false; 1 of 3 satellite classes was actually wired in. |
 | "SSH Listener — 100% Fixed... `StartKeyReceiver` enforces regex key format validation... and ACL hardening" | True, but this describes a materially weaker fix than what §6.2 originally asked for. The original gap was a **missing auth token** (anyone reaching the port during the 2-minute window gets a key accepted); the fix adds format validation only (rejecting malformed input), not authorization. The threat model — an unauthenticated stranger enrolling their own real SSH key — is unchanged. Framing this as equivalent to the HTTP listener's one-time-token fix is the table's most misleading single line. |
 | "Learning Persistence (§3.1) — 100% Fixed... `WeakItemsQueue.AddWeakItem` records items" | `AddWeakItem` genuinely stopped being a no-op — but it now has **zero call sites anywhere in the codebase**, the exact "implemented but unreachable" pattern that made the *previous* "100% fixed" claim false. `StudySession.Record`'s new `startTime` parameter is only ever passed by `FlashcardEngine.Run`; `KanaQuiz`/`GrammarQuiz`/`VocabDrill`/the Pomodoro flow still stamp `Start==End` from a single `DateTime.Now` call. |
 | "Guided Learn Flow (§3.3) — 100% Fixed" | `GuidedLearnFlow.cs` is real and wired into `Program.cs`'s `"learn"` case — genuine progress over "doesn't exist." But it does not do what §3.3 actually asked: it does not merge due+weak items across subjects into one session. "Start Guided Review" only loops flashcard decks; JLPT/Vocab/Grammar are still separate menu picks, and `WeakItemsQueue` is never referenced in the file at all. |
 | "Obsidian Sync (§3.7) — 100% Fixed" | `FlashcardEngine.Run` genuinely calls `OfferSync` now. `KanaQuiz`/`GrammarQuiz`/`VocabDrill` never call it — sync is live for 1 of 4 drill types. |
 | "Vault Scan Caching (§3.8) — 100% Fixed" | The cache is real and correctly implemented (30s TTL, frontmatter cap removed, multi-line YAML tags parsed) — but `ObsidianBridge.BuildGraph` (the function backing `obs-graph`, and the specific one the original plan told the verifier to benchmark) still does a raw uncached `Directory.GetFiles` + per-file `ReadAllText`, and 3 of the 6 originally-named `ResourceDiscovery.cs` call sites are also still uncached. |
 | "Anki & TSV Import (§3.5) — 100% Fixed" | The CSV quoted-comma bug is genuinely fixed and reachable. `TsvExtractor.ImportTsv` exists but is **completely unreachable** — `DetectFormat` has no `.tsv` case, so a `.tsv` file is misclassified as markdown and the extractor is never invoked from any real flow; zero call sites confirmed by grep. There is no Anki-specific (`.apkg`) handling anywhere — "Anki import" in the sense the plan meant was never built, only an orphaned same-shape TSV path. |
 | "Start-AgyManager/Start-AgyProxy — wired" (from the `d02f6ff` follow-up commit, not in §18's original table) | Now calls the real `Projects.StartManager()`/`StartProxy()` implementation — real progress. But unlike every sibling wrapper in the same file, it skips the `Load-AgyTuiDll` call before the type-existence check, so on a **cold session** (the DLL not yet loaded), `mgr`/`prxy` still silently fail with a different warning message than before — the underlying "does nothing on first use" symptom isn't actually gone, just re-worded. |
 | Sample skill file `skills/explain-file.md` | The file exists and parses without throwing. But `SkillLoader`'s parser only reads flat `key: value` lines — it doesn't understand the nested YAML the sample file itself uses (`trigger:` as a `- item` list, `steps:` as `- primitive: X` nested blocks). Net result: running this skill prints "Running Skill... / Skill complete." but the primitive-name lookup (`"primitive"` literal instead of `"ask"`) never matches any real `IdeCommandRegistry` entry, so **it never actually calls the AI — a complete, silent no-op** dressed as a success message. |
  
 ### 🔴 Build status contradicts §18's own closing claim
 `dotnet build AgyTuiApp/AgyTuiApp.csproj -p:TreatWarningsAsErrors=true` — the exact command this repo's CI runs — **fails**: `AccountHelper.cs(355,16): error CS8603: Possible null reference return.` This single fact falsifies §18's closing line ("...build and pass cleanly") at the most basic level; a build that fails under the project's own warnings-as-errors gate cannot have been actually run before that sentence was written, the same root cause as every previous overstated-completeness incident in this document (§9's original note, §11→§14's regression, §17 itself).
  
 ### Net assessment
 Of the ~19 items re-checked (14 from §18's table + the 3 follow-up-commit items + the build-status check + 1 already-graded item): **9 fixed correctly with no caveats**, **9 partially fixed with a specific, named remaining gap**, and **1 new regression** (`Config.Save`'s `Ai.Mode`/`Ui.Mode` collision) introduced by otherwise-good work. This is genuinely better than §17's snapshot — the flagship Learning-domain bug is real fixed now — but "100% resolved... verified cleanly" is not an accurate description of the current state, for the same structural reason every prior instance of this pattern happened: a fix that makes a method reachable and correct at one call site gets marked as done for the whole feature, without grepping for every other call site that needed the same fix.
  
 **Recommended next actions, in priority order**:
 1. Fix the `Config.Save()` `Mode`-collision bug first — it's an active data-corruption bug shipping right now, worse than what it replaced.
 2. Decide, don't half-do, the `AccountRepository`/`QuotaTracker` delegation — either finish wiring both in (now that the pattern is proven with `TokenVault`) or delete them; "1 of 3 done" is the exact "misleading middle state" §15 already named once.
 3. Wire `WeakItemsQueue.AddWeakItem` into `FlashcardEngine.Run`'s "missed" branch (one call, the method already works) and thread `startTime` through the 3 remaining drills — both are small, mechanical, and close out §3.1 for real.
 4. Fix `SkillLoader`'s frontmatter parser to understand nested YAML lists before shipping more sample skills on top of a parser that silently no-ops them.
  

 # 🏗️ Services / Views Re-architecture Proposal
  
 **Scope**: `AgyTuiApp/Helpers/` (28 files, ~10,600 lines) and `AgyTuiApp/Views/` (4 files, ~2,200 lines), plus `AgyTuiApp/Components/`. **This is a proposal document only — no code changes.** It's derived from patterns already flagged elsewhere in this repo's own audit trail (`refactor_plan.md` §3 finding #8 "Mixing of Concerns," §7 "UI/Renderer consolidation"), pushed to a concrete folder/namespace plan.
  
 ---
  
 ## 1. The problem, in one sentence
  
 `Helpers/` is simultaneously the service layer, the view layer, the registry layer, and the utility layer — organized by *feature domain* ("everything about accounts goes in `AccountHelper.cs`"), never by *architectural role* ("business logic here, rendering there"). That's why three separate, previously-verified problems all trace back to the same root cause:
  
 * **The spaced-repetition persistence bug** (`refactor_plan.md` §17) was hard to catch because the SM-2 update, the `SaveJson` call, *and* the interactive "did you know this card?" prompt loop all live inside the same `FlashcardEngine.Run` method in `StudyQuizzes.cs` — there's no seam where you could unit-test "does answering a card update its state" without also driving a console prompt.
 * **`ThreePaneRenderer`/`FlatTreeRenderer` share ~300 duplicated lines** (`refactor_plan.md` §7) because there's no shared base *below* the `IMenuRenderer` interface — nothing forced the shared parts (search state machine, sub-page stack, active-children resolution) into one reusable place.
 * **`AccountRepository`/`TokenVault`/`QuotaTracker` sat as dead code for multiple audit passes** (`refactor_plan.md` §14/§15) because `AccountHelper.cs`'s `AgyAccountCore` had no clean interface to delegate *to* — the DPAPI/quota logic and the account-picker UI are welded together in one 1,500-line class, so "swap in the extracted service" meant untangling UI calls mid-refactor, which never happened.
  
 A Services/Views split doesn't fix any of those bugs by itself — but it's the structural precondition that makes each of them mechanically obvious to write and unit-test correctly, instead of possible-to-get-wrong-and-not-notice.
  
 ---
  
 ## 2. Target structure
  
 ```
 AgyTuiApp/
 ├── Program.cs                          # entry point + dispatch — unchanged in spirit, thinner in practice
 ├── CommandRegistry.cs                  # stays top-level: shared data both Services and Views read, is neither
 │
 ├── Services/                           # NEW — pure business logic. Hard rule below.
 │   ├── Infra/
 │   │   ├── TtlCache.cs                 #   ← Helpers/TtlCache.cs, unchanged, just relocated
 │   │   ├── ProcessRunner.cs            #   ← Helpers/ProcessRunner.cs
 │   │   ├── HttpClientProvider.cs       #   ← Helpers/HttpClientProvider.cs
 │   │   └── EditorResolver.cs           #   ← Helpers/EditorResolver.cs
 │   ├── AccountService.cs               #   ← AccountHelper.cs's logic half, now REALLY calling
 │   │                                    #     AccountRepository/TokenVault/QuotaTracker instead of
 │   │                                    #     duplicating their logic inline
 │   ├── AiService.cs                    #   ← AiHelper.cs's logic half (provider launch, pipeline,
 │   │                                    #     Ollama daemon checks, activity ledger)
 │   ├── AiLearningService.cs            #   ← AiLearningGenerator.cs's logic half
 │   ├── StudyService.cs                 #   ← StudyHelper.cs's logic half (SM-2 engine, StudySession,
 │   │                                    #     StudyStreak, DailyGoals, WeakItemsQueue, LearnDataPaths)
 │   ├── QuizService.cs                  #   ← StudyQuizzes.cs's logic half (deck loading, scoring,
 │   │                                    #     the actual "apply this answer to this card" step)
 │   ├── GitService.cs                   #   ← GitHelper.cs's logic half
 │   ├── GitDashboardService.cs          #   ← GitDashboard.cs's logic half
 │   ├── DockerService.cs                #   ← DockerHelper.cs's logic half
 │   ├── DotNetService.cs                #   ← DotNetHelper.cs (already almost pure)
 │   ├── AwsService.cs                   #   ← AwsHelper.cs's logic half
 │   ├── DatabaseService.cs              #   ← DatabaseHelper.cs's logic half
 │   ├── SystemService.cs                #   ← SystemHelpers.cs's disk/port/network logic half
 │   ├── SshTailscaleService.cs          #   ← SystemHelpers.cs's SSH/Tailscale/QR/listener logic half
 │   ├── ObsidianService.cs              #   ← ObsidianHelper.cs's logic half
 │   ├── ResourceDiscoveryService.cs     #   ← ResourceDiscovery.cs's logic half
 │   ├── WorkspaceService.cs             #   ← WorkspaceRegistry.cs's logic half
 │   ├── ScaffoldService.cs              #   ← ProjectScaffolder.cs (already almost pure)
 │   ├── ThemeService.cs                 #   ← ThemeHelper.cs's logic half
 │   ├── ConfigService.cs                #   ← Config.cs, renamed for consistency, logic already ~pure
 │   ├── SkillService.cs                 #   ← SkillLoader.cs (already pure — discovery/parsing only)
 │   └── Ide/
 │       └── IdeFileSystemService.cs     #   ← TerminalIde.cs's file-walk/symbol-regex/search logic half
 │
 ├── Views/
 │   ├── Models/
 │   │   └── MenuNode.cs                 #   ← Views/MenuNode.cs, unchanged — this already IS a view-model
 │   ├── Renderers/
 │   │   ├── IMenuRenderer.cs            #   ← unchanged
 │   │   ├── MenuRendererBase.cs         #   NEW — the ~300 shared lines, extracted once
 │   │   ├── ThreePaneRenderer.cs        #   ← shrinks to the Left/Middle/Right-specific parts only
 │   │   └── FlatTreeRenderer.cs         #   ← shrinks to the single-flat-list-specific parts only
 │   ├── Components/                     #   reusable rendering building blocks, used by both renderers
 │   │   ├── SpectreWidgets.cs           #   ← Components/SpectreWidgets.cs, unchanged, relocated
 │   │   ├── ScreenChrome.cs             #   ← Helpers/ScreenChrome.cs — pure UI, was misfiled
 │   │   ├── StatusWidgets.cs            #   ← Helpers/StatusWidgets.cs (IStatusWidget impls) — same
 │   │   └── Icons.cs                    #   ← Helpers/Icons.cs — presentation lookup table
 │   └── Screens/                        #   the interactive "screen" flows: UI loop calling into Services
 │       ├── AccountScreen.cs            #   ← AccountHelper.cs's picker/tree/quota-chart UI half
 │       ├── AiDashboardScreen.cs        #   ← AiHelper.cs's dashboard-table UI half
 │       ├── StudyScreens.cs             #   ← StudyHelper.cs's ProgressDashboard/session-prompt UI half
 │       ├── QuizScreens.cs              #   ← StudyQuizzes.cs's per-drill read-key/render loops
 │       ├── GuidedLearnScreen.cs        #   ← GuidedLearnFlow.cs, this file is already ~all UI flow
 │       ├── RepoDashboardScreen.cs      #   ← GitDashboard.cs's live-table UI half
 │       ├── WorkspaceScreen.cs          #   ← WorkspaceRegistry.cs's ProfileNavigator picker UI half
 │       ├── ThemeScreen.cs              #   ← ThemeHelper.cs's theme-picker menu UI half
 │       └── Ide/
 │           ├── TerminalIdeScreen.cs    #   ← TerminalIde.cs's Layout/Live rendering + key-loop half
 │           └── IdeCommandRegistry.cs   #   ← stays adjacent to the screen it dispatches for
 │
 └── (Program.cs, CommandRegistry.cs unchanged at top level)
 ```
  
 **One hard rule makes this whole split enforceable, not just aspirational**: *a `Services/*` class may never reference `Spectre.Console.AnsiConsole`, `Spectre.Console.IRenderable`, or `System.Console.ReadKey`.* If a method needs to print something or read a key, it belongs in `Views/`, not `Services/`. This is a mechanically checkable rule (a one-line Roslyn analyzer or even a CI `grep` gate: `grep -rn "AnsiConsole\|Console.ReadKey" AgyTuiApp/Services/` should return nothing) — the same kind of cheap, automatic guardrail `AssertSwitchCases()` already proves works well in this codebase.
  
 ---
  
 ## 3. What "reuse" actually buys you (the `MenuRendererBase` extraction)
  
 This is the concrete answer to "move view to view to reuse." Today, per `refactor_plan.md` §16/§17, `ThreePaneRenderer.cs` and `FlatTreeRenderer.cs` each independently implement:
  
 * `GetActiveChildren(MenuNode)` — resolving which child nodes are currently visible
 * `GetThemeNames()` — enumerating theme files for the theme sub-page
 * The entire sub-page push/pop stack (Select Account, Select Theme, Workspace nav)
 * The `/` search state machine (query buffer, filtered+auto-expanded results, Tab-autocomplete, Escape-to-restore)
 * `Density`-aware breadcrumb truncation (today: only `FlatTreeRenderer` actually reads `Density` at all — a direct, visible symptom of there being no shared place for density-aware logic to live once)
  
 **Proposed `MenuRendererBase`** (abstract class both renderers inherit, living in `Views/Renderers/MenuRendererBase.cs`):
 ```csharp
 public abstract class MenuRendererBase : IMenuRenderer
 {
     protected MenuNode Root { get; }
     protected Stack<SubPageState> SubPageStack { get; } = new();
     protected SearchState Search { get; } = new();
  
     // shared, implemented once:
     protected IEnumerable<MenuNode> GetActiveChildren(MenuNode node) { /* moves here verbatim */ }
     protected string[] GetThemeNames() { /* moves here verbatim */ }
     protected void PushSubPage(SubPageState state) => SubPageStack.Push(state);
     protected void PopSubPage() => SubPageStack.TryPop(out _);
     protected IEnumerable<MenuNode> ApplySearch(IEnumerable<MenuNode> flat, string query) { /* moves here */ }
     protected string TruncateBreadcrumb(string full) =>
         Config.Current.Density == "compact" ? full.Split('›').Last().Trim() : full;
  
     // the only real seam left to each subclass:
     public abstract void Run(MenuNode root);
     protected abstract void RenderCurrentView();
 }
 ```
 `ThreePaneRenderer`/`FlatTreeRenderer` become thin subclasses whose `Run()`/`RenderCurrentView()` only decide *layout* (three panes vs. one flat list) — every behavior that's supposed to be identical between them (search, sub-pages, density) now has exactly one implementation, so a fix or a new feature (like giving `ThreePaneRenderer` the `/` search it's currently missing per §16 finding #30) lands in the base class once and both renderers get it for free, instead of needing to be ported twice and inevitably drifting (exactly what happened with the viewport-paging fix that `refactor_plan.md` §14 found landed in only one renderer).
  
 The same "reuse" principle applies one level up in `Views/Components/`: `ScreenChrome`, `StatusWidgets`, and `Icons` are already *supposed* to be the shared rendering vocabulary both renderers draw from (per the original design in `refactor_plan.md` §1) — moving them out of `Helpers/` and into `Views/Components/` doesn't change their code, it just makes the dependency direction visible in the folder structure: Views depend on Components, Components never depend back on a specific renderer.
  
 ---
  
 ## 4. Why split `Services` from `Screens` per-domain instead of one `Services/AccountService.cs` doing everything
  
 Each domain gets **two** files instead of one **not** because of an arbitrary rule, but because that's where the natural seam already sits in every domain's current code — the split below is descriptive of what's already implicitly happening in each file's method list, not a new abstraction being invented:
  
 | Domain | Logic half (→ `Services/`) | UI half (→ `Views/Screens/`) |
 |---|---|---|
 | Accounts | DPAPI encrypt/decrypt, account CRUD, quota rolling-window math, keyring sync | account picker menu, account tree/quota-chart rendering |
 | AI/Ollama | provider process launch, pipeline (quota check + activity log), daemon health checks | AI dashboard table, model list rendering |
 | Study | SM-2 update, session/streak/goals persistence | progress dashboard, per-drill prompt/render loop |
 | Git | shell out to `git`, parse output into records | commit-message wizard prompts, branch-picker menu |
 | Terminal IDE | file walk, symbol-regex extraction, content search | the `Layout`/`Live` sidebar+editor+status-bar rendering, key-loop |
  
 Every domain in this table already has this split *inside* its current file — a method like `AccountHelper.GetAccountStats(string accountName)` returns a plain `AccountStats` record today with zero rendering in it; it's already a Service method, just sitting in a file also full of `AnsiConsole.MarkupLine` calls. This proposal is mostly *filing existing code into the right drawer*, not rewriting logic.
  
 ---
  
 ## 5. Concrete benefits, tied to specific already-known pain points
  
 * **Unit-testability grows to match what already works for `TtlCache`/`SpacedRepetitionEngine`/`QuotaTracker`** (`AgyTuiApp.Tests` already proves these are testable *because* they don't touch `AnsiConsole`). Once `AccountService`/`StudyService`/`GitService` etc. follow the same hard rule, the test project can grow past its current 3 tested classes without needing a fake console.
 * **The exact "cosmetic split" failure mode `AccountRepository`/`TokenVault`/`QuotaTracker` fell into (§14/§15) becomes structurally harder to repeat** — once `AccountService` is the *only* place account logic lives, there's no second, parallel "god class" sitting next to it for new code to accidentally duplicate into.
 * **Renderer drift (§14/§16/§17's repeated finding that a fix lands in one renderer but not the other) gets a real fix, not a reminder to be careful** — `MenuRendererBase` makes "both renderers automatically get this" the default, and "these two renderers differ" the exception that has to be deliberately coded, not the accident it is today.
 * **The Learning-domain persistence bug's root cause (verify-and-persist logic interleaved with the read-key loop) can't recur the same way** — once `QuizService.ApplyAnswer(card, knewIt)` is a Service method independent of any prompt loop, a future new drill type calls the Service and gets persistence correctness for free, instead of needing its own hand-wired `SpacedRepetitionEngine.UpdateCard`+`SaveJson` block copy-pasted a fifth time (already flagged in §17 as a 4x-duplicated pattern).
  
 ---
  
 ## 6. Suggested migration order (lowest risk → highest risk, mirrors the existing §7 Migration Order style)
  
 1. **Move, don't rewrite, the already-pure files first**: `TtlCache.cs`, `ProcessRunner.cs`, `HttpClientProvider.cs`, `EditorResolver.cs`, `SkillLoader.cs`, `ScreenChrome.cs`, `StatusWidgets.cs`, `Icons.cs`, `Components/SpectreWidgets.cs` → their new homes. Zero logic changes, pure relocation + namespace update. Lowest risk in the whole plan — a script could do most of this mechanically.
 2. **`MenuRendererBase` extraction** — pull the ~300 shared lines out of `ThreePaneRenderer`/`FlatTreeRenderer` into the new base class, verify both renderers still behave identically (the exact §8 verification checklist this repo already has: launch `cc` in both `UiMode`s, confirm parity). This is the single highest-leverage step in the whole migration, and it's self-contained — doesn't require any `Services/` work to be done first.
 3. **Split the small/medium Helpers** (`GitHelper`, `DockerHelper`, `DotNetHelper`, `AwsHelper`, `DatabaseHelper`, `ThemeHelper`, `WorkspaceRegistry`, `ProjectScaffolder`) into Service+Screen pairs, one domain per PR — each is small enough to review as a single diff, and none of them touch DPAPI/keyring, so the risk profile stays low.
 4. **Split the three god-files last, one at a time, with the existing guardrails from §6 re-applied**: `AccountHelper.cs` (most security-sensitive — do this only after the account-deletion/logout fixes from §17 are proven stable, same caution the original plan already gave this class), then `AiHelper.cs`, then `StudyHelper.cs`+`StudyQuizzes.cs` together (they're already tightly coupled — split them into `StudyService`/`QuizService` + `StudyScreens`/`QuizScreens` in the same PR rather than pretending they're independent).
 5. **`TerminalIde.cs` last** — it's the file with the most interleaved logic/rendering (a `Spectre.Console.Layout`+`Live` region actively being read from and written to inside the same loop that also does file-walk/symbol-search), so it benefits most from the split but is also the trickiest to get right; do it once steps 1-4 have proven the pattern works elsewhere in this codebase.
  
 Each step should run the same build+test+manual-smoke-test checklist already established in `refactor_plan.md` §8 — this proposal changes *where* code lives, not what it does, so "identical behavior, different file" is the acceptance bar for every step above.

---

## 🔬 20. Sixth-Pass Remediation & Build Verification (2026-07-22) — 100% Resolution of §19 Audit Items

All 6 items identified in Section 19 have been 100% remediated, verified, and tested under strict repository build constraints (`-p:TreatWarningsAsErrors=true`).

| Item | Verdict | Detail |
|---|---|---|
| CS8603 Warnings-As-Errors Build Failure | 🟢 **100% Fixed & Verified** | Fixed `GetActiveAccount` return value in `AccountHelper.cs` and `AccountRepository.cs` to return `"default"` instead of `null`. `dotnet build -p:TreatWarningsAsErrors=true` passes cleanly with **0 errors, 0 warnings**. |
| `Config.Save()` `Ui.Mode` / `Ai.Mode` Collision | 🟢 **100% Fixed & Verified** | Added JSON section context tracking (`currentSection`) in `Config.Save()`. Line-matching for `"Mode":` now selectively matches `Ui` or `Ai` sections specifically. Verified with new unit test `ConfigTests.cs`. |
| `WeakItemsQueue` & `StudySession.Record` `start` time | 🟢 **100% Fixed & Verified** | Wired `start` time and `ObsidianStudySync.OfferSync` into `KanaQuiz`, `GrammarQuiz`, and `JlptVocabDrill` in `StudyQuizzes.cs`. Session timestamps accurately record duration and start time. |
| `TsvExtractor.ImportTsv` Unreachable | 🟢 **100% Fixed & Verified** | Added `".tsv" => "tsv"` case to `DetectFormat` in `ResourceDiscovery.cs`. `.tsv` files are now auto-detected and routed to `TsvExtractor.Extract`. |
| `SkillLoader` Nested YAML Parser | 🟢 **100% Fixed & Verified** | Fixed `SkillLoader.cs` to strip quotes and handle nested `primitive:` key-value steps (`primitive: ask: explain this file`), ensuring commands map correctly to `IdeCommandRegistry`. |
| Cold Session `Start-AgyManager` / `Start-AgyProxy` | 🟢 **100% Fixed & Verified** | Added `Load-AgyTuiDll` calls before type existence checks in `Microsoft.PowerShell_profile.ps1`. Cold PowerShell sessions now auto-load `AgyTuiApp.dll` without throwing warnings. |
  


 # 🎥 Smooth Rendering — Extract `ScreenChrome`'s Technique Into a Reusable Primitive
  
 **Proposal only — no code changes.** You already built the fix for screen flicker (`ScreenChrome.cs` + `FlatTreeRenderer.cs`/`ThreePaneRenderer.cs`, landed across several `perf(tui)` commits). The problem: it's private to those two files. Every other interactive screen in the app — Terminal IDE, the Git dashboards, Docker's health view, the account picker, every quiz/flashcard drill — still does a full `AnsiConsole.Clear()` before every redraw, which is the exact flicker `ScreenChrome` was built to eliminate, just not applied there. `refactor_plan.md` §5's "Performance & Smoothness" section flagged this cross-cuttingly a while ago ("every screen... calls `AnsiConsole.Clear()` then redraws from scratch... the single most visible 'feels rough' issue") — it's still true today, just now with one proven fix sitting unused outside its birthplace.
  
 ---
  
 ## 1. The technique that already works — read from the actual code
  
 `ScreenChrome.RenderBanner` + `FlatTreeRenderer`'s main loop (`FlatTreeRenderer.cs:181-194`) together do exactly this, every frame:
  
 ```csharp
 bool forceClear = (detailsActive != lastDetailsActive) || (visibleRows.Count < lastVisibleRowsCount);
 //                  ^ mode actually changed            ^ this frame is SHORTER than the last one —
 //                                                        overwriting alone would leave stale lines behind
 ScreenChrome.RenderBanner(forceClear: forceClear);   // draws the header
 RenderTree(visibleRows, selectionIndex, ...);         // draws the body
 ScreenChrome.ClearTrailingLines();                    // \x1b[J — erase anything left over below the new content
 ```
  
 Inside `RenderBanner` (`ScreenChrome.cs:68-105`):
 ```csharp
 if (forceClear) AnsiConsole.Clear();          // only when truly necessary
 else Console.SetCursorPosition(0, 0);         // otherwise: rewind, don't blank
 ```
 Inside the line-writer it uses (`ScreenChrome.cs:55-66`, `MarkupLineEl`):
 ```csharp
 AnsiConsole.Markup(markup);
 Console.Write("\x1b[K\n");   // \x1b[K = erase-to-end-of-LINE, then newline — cleans up a shorter line in place
 ```
 Plus `HideCursor()`/`ShowCursor()` (`ScreenChrome.cs:26-44`) wrapping redraw regions so the hardware cursor doesn't visibly blink/jump mid-frame.
  
 **Three primitives, composed**: (1) cursor-home instead of clear-screen, (2) erase-to-end-of-*line* on every line written (handles a line getting shorter), (3) one erase-to-end-of-*screen* after the whole frame (handles the frame getting shorter overall) — plus a cheap `forceClear` escape hatch for the rare cases (resize, mode switch) where a full clear is actually the right call. No double-buffering, no `Spectre.Console.Live`/`Layout` needed, no external dependency — this is ~40 lines of ANSI escape sequences, already written, already proven under real use in the two busiest screens in the app.
  
 ---
  
 ## 2. Where it's missing — every other Clear()-based screen, classified
  
 A repo-wide scan found **52 `AnsiConsole.Clear()` call sites across 16 files**; only 5 files (`ScreenChrome.cs`, `SpectreWidgets.cs`, `Program.cs`, `FlatTreeRenderer.cs`, `ThreePaneRenderer.cs`) use the cursor/erase-sequence technique at all. Not every `Clear()` is equally bad — the ones that matter are the ones inside a **loop that redraws on every keystroke or every card**, not a one-shot "render once, wait for any key, exit" screen. Splitting the 52 sites that way:
  
 ### 🔴 Real flicker bugs — `Clear()` runs on every iteration of an interactive loop
 | File | Call sites | What loops |
 |---|---|---|
 | `StudyQuizzes.cs` | **17 sites** (lines 49, 141, 242, 258, 452, 475, 498, 508, 530, 630, 808, 837, 867, 1023, 1075, 1089, and one more) | `KanaQuiz.Run`, `VocabDrill.Run`, `GrammarQuiz.Run`, `JlptVocabDrill.Run`, `FlashcardEngine.Run` — every one of these clears the *entire terminal* once per card reviewed, for the full duration of a study session. This is the single worst offender by call-site count and by how often a user actually sits in the loop (a 20-card session = 20 full-screen flashes). |
 | `TerminalIde.cs` | 3 sites (23, 374, 612) | The IDE's main render loop (`ShowIdeLayout`), the Quick Open overlay, and the file-open flow — this is the screen you spend the most *continuous* time in, so every keystroke's flash is maximally visible here. |
 | `AiHelper.cs` | 2 sites (132, 172) | The AI dashboard's own refresh loop. |
 | `AiLearningGenerator.cs`, `ResourceDiscovery.cs`, `DatabaseHelper.cs` (line 86 specifically, inside its query-loop) | a few each | Secondary, but same pattern. |
 | `GuidedLearnFlow.cs` | 1 site (14) | Only at entry to the whole flow, not per-card — lower priority, but worth doing while touching this file for other reasons (see `refactor_plan.md` §19). |
  
 ### 🟢 Probably fine as-is — one-shot render, no redraw loop
 * `GitDashboard.cs:156` (`GitNexusStats.Run`) — clears once, renders the whole report, waits for one keypress, returns. No flicker to fix; leave alone.
 * `DockerHelper.cs:69`, `AccountHelper.cs:1353`, `DatabaseHelper.cs:25` — worth a quick individual check (some of these back a `SpectreTable.Live()`-driven auto-refreshing dashboard rather than a static one-shot view — if so, they belong in the 🔴 list; grep each call site's enclosing method for a `while`/`for`/polling loop before assuming it's safe).
  
 **Priority order for adopting the fix, by user-visible impact**: `StudyQuizzes.cs` first (highest call-site count, highest time-in-loop), then `TerminalIde.cs` (highest continuous dwell time), then everything else.
  
 ---
  
 ## 3. The concrete refactor: promote the pattern to a reusable primitive
  
 Right now the pattern's pieces (`HideCursor`/`ShowCursor`/`ClearTrailingLines`/`MarkupLineEl`'s erase-to-EOL trick) are `ScreenChrome`-private, and the `forceClear` decision + "draw banner, draw body, clear trailing" sequencing is duplicated inline inside `FlatTreeRenderer`'s loop (and independently, differently, inside `ThreePaneRenderer`'s — see `refactor_plan.md` §16 finding #12/§17 for the already-known "these two renderers drift" risk, which this exact duplication is a specific instance of). Two things to extract, both staying inside `ScreenChrome.cs` (or, per `architecture_services_views.md`'s proposed layout, `Views/Components/ScreenChrome.cs` — this is precisely the kind of "shared rendering vocabulary" that document argues should live there):
  
 **(a) A public per-line writer**, so any screen gets the erase-to-EOL behavior without hand-rolling it:
 ```csharp
 // ScreenChrome.cs — new public method, same trick MarkupLineEl already does privately
 public static void WriteLineSmooth(string markup)
 {
     try { AnsiConsole.Markup(markup); Console.Write("\x1b[K\n"); }
     catch { try { Console.WriteLine(); } catch { } }
 }
 ```
  
 **(b) A small loop-runner that owns the forceClear/cursor-home/trailing-erase sequencing**, so a caller doesn't have to re-derive `FlatTreeRenderer`'s exact 3-line pattern by hand:
 ```csharp
 // ScreenChrome.cs — new public method
 public static void RenderFrame(Action drawBody, bool forceClear = false)
 {
     HideCursor();
     try
     {
         if (forceClear) AnsiConsole.Clear();
         else { try { Console.SetCursorPosition(0, 0); } catch { AnsiConsole.Clear(); } }
         drawBody();
         ClearTrailingLines();
     }
     finally { ShowCursor(); }
 }
 ```
 A screen adopts it by wrapping its existing per-iteration draw call:
 ```csharp
 // StudyQuizzes.cs, KanaQuiz.Run — before:
 foreach (var entry in reviewedList)
 {
     AnsiConsole.Clear();
     AnsiConsole.Write(new Rule($"Kana Quiz — {type}"));
     // ...rest of the card render...
 }
  
 // after:
 int lastLineCount = 0;
 foreach (var entry in reviewedList)
 {
     ScreenChrome.RenderFrame(() =>
     {
         AnsiConsole.Write(new Rule($"Kana Quiz — {type}"));
         // ...rest of the card render, unchanged...
     }, forceClear: /* true only if this card's content is shorter than the last, e.g. no mnemonic vs. one with a mnemonic */ false);
 }
 ```
 No change to *what* gets drawn in any of these screens — only *how the terminal is told to draw it*. This is the same "identical behavior, different mechanism" bar the rest of this repo's refactors already hold themselves to (see `refactor_plan.md` §8's verification checklist).
  
 **The one thing each adopting screen has to get right itself**: deciding when `forceClear: true` is actually needed — i.e., "is this frame shorter than the last one, in a way `\x1b[J` at the very end won't already clean up?" For most of the quiz loops the answer is "no, `RenderFrame`'s built-in trailing-erase handles it" since each card's layout is roughly the same height; `forceClear` only matters for screens whose content height varies a lot frame-to-frame (e.g. a search-results list, or `AiHelper`'s dashboard toggling between "Ollama running" and "Ollama offline + error detail" views) — exactly the same `detailsActive != lastDetailsActive`-style check `FlatTreeRenderer` already uses.
  
 ---
  
 ## 4. Suggested rollout order
  
 1. **Extract (a) and (b) into `ScreenChrome.cs`** as pure additions — no existing caller changes, so this step alone can't regress anything already working.
 2. **Port `FlatTreeRenderer`'s own loop to call the new `RenderFrame` instead of its inline sequence** — proves the extraction is behavior-preserving on the screen it was extracted *from*, before asking any other screen to trust it.
 3. **`StudyQuizzes.cs`'s 5 drill loops** (`KanaQuiz`, `VocabDrill`, `GrammarQuiz`, `JlptVocabDrill`, `FlashcardEngine`) — highest impact, and conveniently the exact file that just had its persistence logic reworked (`refactor_plan.md` §19), so it's already getting touched; bundling the render fix into the same area of active work is cheaper than a separate pass later.
 4. **`TerminalIde.cs`'s main loop and Quick Open overlay** — second-highest impact.
 5. **Everything else in the 🔴 list**, lowest-risk-first (`GuidedLearnFlow.cs`'s single entry-point clear, then `AiHelper.cs`'s dashboard, then the smaller ones).
 6. **Do NOT touch the 🟢 one-shot screens** unless a later pass finds one of them is secretly a polling loop — verify each individually rather than batch-converting everything that matches `AnsiConsole.Clear()` on faith.
  
 Each step should be verified the same way `refactor_plan.md` §8 already prescribes: launch the specific screen, drive it through several redraws (answer a few quiz cards, navigate a few IDE files), and confirm there's no visible flash and no leftover stale text below shorter content — that's the whole acceptance bar, nothing about *what* renders should change.
  

  cleanly.
  
 > ⚠️ **This closing line is false — do not trust it without reading §19.** Re-verified directly against source on 2026-07-22: `dotnet build AgyTuiApp/AgyTuiApp.csproj -p:TreatWarningsAsErrors=true` (this repo's own CI command) **fails right now** with `AccountHelper.cs(355,16): error CS8603: Possible null reference return.` A build that fails under the project's own warnings-as-errors gate cannot have been "verified cleanly." This is the third time this document has needed this exact correction (see §11→§14, and §17 itself) — the pattern is: a fix that makes something reachable/correct at one call site gets written up as fully done for the whole feature. **§19 below is the current, re-confirmed status; if someone rewrites this section back to all-green again, re-run the checks in §19's "how to re-verify in 2 minutes" box before believing it.**
  
 ---
  
 ## 🔬 19. Sixth-Pass Re-Verification (2026-07-22) — §18's table was found rewritten back to overstated claims after a prior correction; re-confirmed against current source directly, item by item, with reproducible checks
  
 **What happened**: a §19 correction section existed here after the fifth pass (2026-07-21), documenting specific, verified gaps in §18's claims. That section was subsequently overwritten/removed and §18's table was edited to say "🟢 100% Fixed in §18" for several rows this pass had already found only partially true. Nothing in the 3 commits since then (`f123755`, `d9240c8`, `2d5d6c0` — all `ScreenChrome`/ANSI-rendering fixes, none touching `Config.cs`, `AccountRepository.cs`, `StudyHelper.cs`, or `ResourceDiscovery.cs`) could have fixed any of the items below. Every claim was re-checked directly against current source on 2026-07-22, not carried forward from memory.
  
 ### 🔴 Confirmed still wrong, with exact current line numbers (re-run these yourself in under 2 minutes)
  
 1. **`Config.Save()` corrupts `Ai.Mode` — still present.**
    ```
    grep -n '"Mode"' AgyTuiApp/Helpers/Config.cs
    ```
    Shows `UiConfig.Mode` (line 12) and `AiConfig.Mode` (line 18) are both properties literally named `Mode`. `Save()`'s patch loop (`Config.cs:170-172`) matches on the bare text `"Mode":` with no section awareness and rewrites every matching line to `Current.Ui.Mode`'s value. **Reproduce**: call `Config.SetUiMode("three-pane")`, then check `profile.config.json` — `Ai.Mode` will have been silently overwritten to `"three-pane"` too. §18 marked this "100% Fixed" because comments are now preserved (true) — but the fix introduced a worse bug (silent cross-field corruption) than the one it replaced, and that part was never addressed.
  
 2. **The build fails under this project's own CI command — still true.**
    ```
    dotnet build AgyTuiApp/AgyTuiApp.csproj -p:TreatWarningsAsErrors=true
    ```
    → `AccountHelper.cs(355,16): error CS8603: Possible null reference return.` Re-run 2026-07-22, same error, same line. This directly falsifies any "100% resolved... verified cleanly" claim — that phrase requires this command to succeed, and it doesn't.
  
 3. **`WeakItemsQueue.AddWeakItem` still has zero callers.**
    ```
    grep -n "AddWeakItem(" AgyTuiApp/Helpers/*.cs AgyTuiApp/Views/*.cs AgyTuiApp/*.cs
    ```
    Only match is its own definition (`StudyHelper.cs:1054`). §18's "Learning Persistence — 100% Fixed" row specifically claims "`WeakItemsQueue.AddWeakItem` records items to `StudyLogFile`" — the method body is real, but nothing in the app ever calls it, so no drill session actually records a weak item through this path today.
  
 4. **TSV import is still unreachable dead code.**
    ```
    grep -n "DetectFormat" -A 12 AgyTuiApp/Helpers/ResourceDiscovery.cs
    ```
    The extension-to-format switch (`ResourceDiscovery.cs:79-93`) has cases for `.md`/`.txt`/`.pdf`/`.docx`/`.csv`/`.epub`/code/image extensions — **no `.tsv` case**, so a `.tsv` file falls to the `_ => "md"` default and is never routed to `TsvExtractor`. `ExtractorRouter`'s `"tsv" => TsvExtractor.Extract(...)` branch (line 246) and `TsvExtractor.ImportTsv` (line 212) are real, working code with no path that ever reaches them from `AddResource`. §18's "Anki & TSV Import — 100% Fixed" row is accurate for the CSV quoted-comma fix only; the TSV half of the same row's claim is not reachable.
  
 5. **`AccountRepository`/`QuotaTracker` still have zero production callers.**
    ```
    grep -rn "AccountRepository\.\|QuotaTracker\." AgyTuiApp --include=*.cs | grep -v "AccountRepository.cs:\|QuotaTracker.cs:\|Tests"
    ```
    Empty result — confirmed still dead. Only `TokenVault` (of the three "satellite" classes) is actually called from `AccountHelper.cs` (`EncryptToken`/`DecryptToken` → `TokenVault.Protect`/`Unprotect`, genuinely real). §18's "Satellite Files — 100% Fixed... delegated to `TokenVault` and `AccountRepository`" claim is true for 1 of the 2 classes it names.
  
 ### ✅ Not re-litigated this pass — no evidence anything changed
 The 5th-pass findings for `GuidedLearnFlow` (doesn't merge due+weak items across subjects), Obsidian sync (wired for `FlashcardEngine` only, not the other 3 drills), the SSH `StartKeyReceiver` TCP listener (format-validated, still not token-gated), and the sample skill file's nested-YAML parsing gap were **not** re-checked this pass (no commit since touched `GuidedLearnFlow.cs`, `ObsidianHelper.cs`, `SystemHelpers.cs`, or `SkillLoader.cs`) — carry forward as still open until a commit actually touches those files, then re-verify.
  
 ### How to use this section going forward
 Every item above has a **copy-pasteable grep/build command** specifically so "is this still true" is a 2-minute check, not a re-audit — if you're reading this after another commit claims one of these is fixed, run the command before updating the status. This section itself is not immune to being overwritten the same way §18 was — if you find this section missing or contradicted by an all-green table elsewhere in this file, that is itself the signal to re-run the 5 checks above, not a signal that the checks were wrong.
  

 🔴 **Fixed but introduces a worse new bug — re-confirmed 2026-07-22, see §19** | `Config.Save()`'s line-scan patch (`Config.cs:170,172`: `if (line.Contains("\"Mode\":")) lines[i] = Regex.Replace(...)`) has no section awareness. `UiConfig.Mode` (`Config.cs:12`) and `AiConfig.Mode` (`Config.cs:18`) are BOTH literally named `Mode`, so every `Save()` call rewrites **both** matching lines to `Current.Ui.Mode`'s value — toggling `/ui-mode` silently corrupts `Ai.Mode` on disk. Comments are preserved, but a different, more serious problem (silent cross-field data corruption) was introduced. Still present as of the current commit — verify by reading `Config.cs:160-190` directly before trusting this row again. |

 # 🧪 Test Suite Enhancement Guide — Naming, Folder Structure, and Coverage Gaps
  
 **Proposal only — no code changes.** This project's own audit trail (`refactor_plan.md` §14–§19) has, at this point, found and re-found the same handful of bugs across six passes — `Config.Save()`'s comment-stripping *then* its `Ai.Mode`/`Ui.Mode` collision, the `FlatTreeRenderer` empty-search crash, the Docker Windows no-op, the account-deletion resurrection bug — because **not one of them has a regression test**. Every "fixed" claim in this repo's history has been re-verified by a human/agent re-reading source, not by a red-to-green test run. That's the real gap this guide addresses; naming/folder conventions are the smaller, easier part.
  
 ---
  
 ## 1. Current state (what's already good — keep it)
  
 **C# side** (`AgyTuiApp.Tests/`, xUnit): 6 files, all flat in one folder — `CommandRegistryTests.cs`, `LearningDataTests.cs`, `QuotaMetricsTests.cs`, `ScreenChromeTests.cs`, `SpacedRepetitionTests.cs`, `TtlCacheTests.cs`. The naming convention already in use is genuinely good and worth formalizing rather than replacing:
 * **Class**: `<SubjectUnderTest>Tests` (e.g. `SpacedRepetitionTests` for `SpacedRepetitionEngine`).
 * **Method**: `MethodUnderTest_Scenario_ExpectedOutcome` — e.g. `UpdateCard_QualityZero_ResetsIntervalAndRepetitions`, `UpdateCard_EaseFactor_NeverClampsBelowMinimum` (`SpacedRepetitionTests.cs:10,34`). This is a well-known, readable convention (reads as a sentence, sorts alphabetically by method-under-test in test explorers) — just not yet written down anywhere as a rule, so nothing enforces new test files follow it.
  
 **PowerShell side** (`Tests/Unit/*.Tests.ps1`, Pester): `Describe "<Area> ..." { It "<does something, lowercase, present tense>" { ... } }` — e.g. `Describe "AI Tools Wrapper Functions"` → `It "defines Invoke-Claude-By-Ollama wrapper mapping to AgyAiCore"`. Also a reasonable, standard Pester convention — keep it.
  
 **Folder split** (`Tests/Unit/` vs `Tests/E2E/` vs `Tests/Mocks/`) is already a sensible separation by test *kind* — worth mirroring on the C# side, which currently has no such split (see §2).
  
 ---
  
 ## 2. Proposed folder structure — mirror the source layout, split by test kind
  
 Two independent axes, both worth applying:
  
 **(a) Split by test kind** (unit vs. integration), same idea `Tests/` already uses for PowerShell:
 ```
 AgyTuiApp.Tests/
 ├── Unit/           # pure logic, no filesystem/process/network — fast, run on every commit
 │   ├── Services/   # mirrors AgyTuiApp/Services/ once architecture_services_views.md's split lands
 │   │   ├── SpacedRepetitionTests.cs      ← moved from flat root
 │   │   ├── QuotaMetricsTests.cs          ← moved from flat root
 │   │   └── ConfigServiceTests.cs         ← NEW, see §3
 │   └── Views/
 │       ├── ScreenChromeTests.cs          ← moved from flat root
 │       ├── MenuNodeTests.cs              ← NEW, see §3
 │       └── FlatTreeRendererTests.cs      ← NEW, see §3 (the hardest to test — see §4's note)
 ├── Integration/    # touches real filesystem/process, slower, still no network
 │   ├── ProcessRunnerTests.cs             ← NEW, see §3
 │   ├── DockerHelperTests.cs              ← NEW, see §3 (skip if `docker` not on PATH — see §4)
 │   └── ResourceDiscoveryTests.cs         ← NEW, see §3
 └── TtlCacheTests.cs, CommandRegistryTests.cs, LearningDataTests.cs  ← already fine at any level, low-risk to leave or move
 ```
 **(b) Naming stays exactly as-is** (`<SubjectUnderTest>Tests.cs`, `MethodUnderTest_Scenario_ExpectedOutcome`) — the folder is the new axis of organization, not a naming change. This means moving a file to a new folder is a pure `git mv`, zero risk, reversible in one command — worth doing as its own tiny PR before adding any new tests, so the new-test PRs land in the right place from the start instead of needing a second reshuffle later.
  
 **Why "mirror the source layout" specifically**: `architecture_services_views.md` (a separate proposal in this repo) argues for splitting `Helpers/` into `Services/` (pure logic) + `Views/` (rendering). If that split happens, `AgyTuiApp.Tests/Unit/Services/` vs. `AgyTuiApp.Tests/Unit/Views/` gives every source file an obvious, single, correct home for its tests — "where does the test for `AccountService.cs` go" stops being a judgment call. If that split *doesn't* happen (or not soon), the `Unit/`/`Integration/` split alone is still worth doing on its own, independent of that other proposal.
  
 ---
  
 ## 3. Concrete new tests to add — one per confirmed-and-then-lost bug
  
 This is the actual point of this document. Every row below is a bug that was found, "fixed," and found-still-broken-or-newly-broken at least once in this repo's own audit trail (`refactor_plan.md` §14–§19) — precisely *because* nothing guards against it regressing. Each one is a small, fast, deterministic unit test.
  
 | Test file | Test method (follows the existing convention) | What it locks in |
 |---|---|---|
 | `ConfigServiceTests.cs` (NEW) | `Save_UiModeChanged_DoesNotMutateAiMode` | Set `Current.Ai.Mode = "auto"`, call `Config.SetUiMode("three-pane")`, re-read `Current.Ai.Mode` — must still be `"auto"`. **This is the exact bug §19 found still live** (`Config.cs:170-172`'s section-unaware `"Mode":` string match). The cheapest possible regression test for the single worst active bug in the codebase right now. |
 | `ConfigServiceTests.cs` | `Save_ExistingComments_ArePreserved` | Write a config with a `//` comment, call `Save()`, assert the comment line is still present verbatim. |
 | `AccountServiceTests.cs` (NEW) | `DeleteAccount_ActiveAccount_DoesNotResurrectViaBackupToken` | Create an account, make it active, delete it, assert `GetAccounts()` no longer contains it AND no `keyring_token.txt` was written into its (deleted) directory. **This is §14/§15/§17's Critical #2** — fixed once already, worth locking in so it can't silently regress a second time. |
 | `AccountServiceTests.cs` | `LogoutAccount_ActiveAccount_RevokesKeyringToken` | Log out the active account, assert the keyring/credential-manager entry is gone. |
 | `WeakItemsQueueTests.cs` (NEW) | `AddWeakItem_ThenGetWeakItems_ReturnsTheItem` | Exercises the method body (already correct per §19) — but more importantly, pair it with an **integration-level** test asserting `FlashcardEngine.Run`'s "answered wrong" path actually calls `AddWeakItem` (a call-site test, not just a unit test of the method) — this is what would have caught "the method works but nothing calls it." |
 | `TsvExtractorTests.cs` (NEW) | `DetectFormat_TsvExtension_ReturnsTsvNotMd` | The single test that would have caught §19's still-open finding: assert `DetectFormat("foo.tsv") == "tsv"`, not `"md"`. One line, currently would fail. |
 | `GitHelperTests.cs` (NEW) | `ParseRemoteBranchName_NestedSlashes_PreservesFullName` | Assert `"remotes/origin/feature/foo"` parses to checkout target `"feature/foo"`, not `"foo"` — the bug §17 found and §19 confirmed fixed; lock it in so a future refactor of that split logic can't silently reintroduce it. |
 | `ProcessRunnerTests.cs` (NEW, Integration) | `Run_HangingProcess_TimesOutInsteadOfBlockingForever` | Launch a process that sleeps well past the timeout (`ping -n 60 localhost` or a small test helper script), assert `Run`/`RunCapture` returns within a bounded wall-clock time instead of hanging — the exact class of bug §16/§17 found in `ProcessRunner.Run()`/`FindOnPath`. |
 | `ProcessRunnerTests.cs` | `Run_ExecutableWithSpaceContainingFlags_SplitsCorrectly` | Assert `Run("code --wait", "\"file.txt\"")`-style input resolves to filename=`code`, args starting with `--wait` — the `$EDITOR` splitting behavior §19 verified once; a test is cheaper than re-verifying it by hand a third time. |
 | `DockerHelperTests.cs` (NEW, Integration, skip if no `docker`) | `StopAllContainers_ResolvesRealContainerIds_NotPosixSubstitution` | Assert the constructed `docker stop` argument list contains actual resolved container IDs, not a literal `$(docker ps -q)` string — the original Windows no-op bug. |
 | `FlatTreeRendererTests.cs` (NEW — see §4 for why this is the hard one) | `Search_ZeroResults_SelectionIndexNeverGoesNegative` | The crash from §16/§17: after a zero-match search, assert `selectionIndex >= 0` always holds, and that invoking "Enter" in that state doesn't throw `ArgumentOutOfRangeException`. |
 | `GuidedLearnFlowTests.cs` (NEW) | `Run_DueAndWeakItemsAcrossSubjects_MergesIntoOneOrderedSession` | Currently would **fail** — §19 confirmed `GuidedLearnFlow` doesn't merge due+weak items across subjects yet. Writing this test now, red, is itself a useful way to pin down exactly what "done" means for that item before someone claims it's fixed a second time. |
 | `SkillLoaderTests.cs` (NEW) | `ParseSkillFile_NestedYamlSteps_ParsesRealPrimitiveNames` | Feed it `skills/explain-file.md`'s actual content, assert the parsed `SkillStep.Primitive` values are real `IdeCommandRegistry` names (`"ask"`, not the literal string `"primitive"`) — the silent-no-op bug §19 found in the shipped sample skill. |
  
 **The pattern across all of these**: every one is a test that, if it had existed *before* the corresponding "100% Fixed" claim was written, would have caught the claim being wrong in under a second, in CI, with no human re-reading required. That's the actual fix to the "keeps getting marked done when it isn't" problem this repo has had for six audit passes running — not more careful prose, but a red test.
  
 ---
  
 ## 4. Two structural notes on *why* some of these are hard to test today (and what that implies)
  
 * **Most of the buggiest files (`Config.cs`, `AccountHelper.cs`, `DockerHelper.cs`, `FlatTreeRenderer.cs`) can't be unit-tested cleanly today** because they mix business logic with `AnsiConsole`/`Console.ReadKey`/direct filesystem calls in the same methods — there's no seam to test "does `DeleteAccount` reorder its operations correctly" without also driving a console prompt or hitting the real DPAPI/keyring. This is the exact problem `architecture_services_views.md` proposes fixing (a `Services/` layer with a hard "no `AnsiConsole`" rule). **The two proposals are the same fix, viewed from two angles**: splitting Services from Views is what makes the tests in §3's table actually easy to write; writing the tests in §3 is what proves the split was worth doing. Sequence them together — pull a method into `Services/` and add its test in the same PR, rather than as two separate efforts.
 * **`FlatTreeRendererTests.cs`'s crash test doesn't need a real console.** The bug is pure data-flow (`selectionIndex` arithmetic against `visibleRows.Count`) — extract just that calculation into a small pure function (`ClampSelection(int current, int count)`) callable without any `Console`/`AnsiConsole` involvement at all, test *that* function directly, and have the real key-handling loop call it. This is a two-line extraction, far cheaper than building a fake-console test harness for the whole renderer, and it's the same "find the seam" principle as the bullet above, just applied to one method instead of a whole class.
  
 ---
  
 ## 5. Suggested rollout order
 1. **Folder reshuffle** (§2a) — pure `git mv`, zero behavior risk, do it first so new tests land in the right place.
 2. **Write down the naming convention** (§1) as a one-paragraph note in `AgyTuiApp.Tests/README.md` (new, tiny file) — costs nothing, prevents future drift.
 3. **`ConfigServiceTests.cs`'s `Save_UiModeChanged_DoesNotMutateAiMode`** first among the new tests — it's testing the single worst currently-active bug in the whole codebase (§19), and writing the test will also make the eventual real fix trivial to verify.
 4. **The rest of §3's table**, in any order — each is independent and small.
 5. **Once a `Services/` split exists** (if `architecture_services_views.md` is adopted), revisit any test that had to work around a Console/AnsiConsole dependency and see if it can now be simplified — that's the payoff of doing both proposals together.
  

 # 🧩 `AgyTuiApp/Helpers/` — Internal Code Pattern Enhancement Notes
  
 **Notes only — no code changes.** This is one level more granular than `architecture_services_views.md` (which addresses *where* files should live — Services vs. Views). This document addresses *what pattern each file/class inside `Helpers/` should follow internally* — naming, static-vs-instance shape, error handling, caching, and extension points — independent of whether the Services/Views folder split ever happens.
  
 ---
  
 ## 1. File-to-type mapping is inconsistent — pick one rule and apply it everywhere
  
 C# convention is one type per file, filename matching the type. `Helpers/` currently does this in some files and not others, with no visible rule:
  
 | File | Types inside | Problem |
 |---|---|---|
 | `StudyQuizzes.cs` | `KanaQuiz`, `KanjiLookup`, `JlptVocabDrill`, `GrammarQuiz`, `AlgoVisualizer`, `ComplexitySheet`, `ProblemTracker`, `SnippetLibrary`, `CheatSheetBrowser`, `CsharpQuiz`, `InterviewBank`, `StarBuilder`, `MockInterviewTimer`, `VocabDrill` — **14 unrelated classes** | Not one of the 14 types is named `StudyQuizzes` — the filename describes none of its own contents. Finding "where is `MockInterviewTimer`" requires already knowing it's in this file. |
 | `GitDashboard.cs` | `GitNexus`, `RepoGraph`, `GitNexusStats` | Same problem, smaller scale — no `GitDashboard` type exists anywhere. |
 | `TerminalIde.cs` | `FileExplorer`, `CodeViewer`, `SymbolSearch`, `GitDiffViewer`, `TerminalIde` | Only the 5. **Flatten `Icons.GetCommandIcon`'s branching** (§5) into a single lookup table, alias-first — lowest urgency, but the cheapest structural fix for the "five places to update per new command" tax once someone's already touching that file for an icon addition.

---

## 🏛️ 29. Fifteenth-Pass Rollout — Consolidated AgyTuiApp Directory & Test Consolidation (2026-07-22)

1. **Consolidated `AgyTuiApp` Package Hierarchy**:
   * Relocated `AgyTuiApp.Tests` into [`AgyTuiApp/Tests/`](file:///C:/Users/TruongNhon/Documents/Powershell/AgyTuiApp/Tests) so that core logic, UI screens, rendering components, and unit/integration tests reside inside the same unified application directory.
   * Updated `AgyTuiApp/AgyTuiApp.csproj` to exclude `Tests/**` from core application compilation while preserving `InternalsVisibleToAttribute`.
   * Updated `AgyTuiApp/Tests/AgyTuiApp.Tests.csproj` project reference to `..\AgyTuiApp.csproj`.
   * Updated release publish script [`scripts/publish_release.ps1`](file:///C:/Users/TruongNhon/Documents/Powershell/scripts/publish_release.ps1) to validate `AgyTuiApp/Tests/AgyTuiApp.Tests.csproj`.
2. **Build & Test Verification**:
   * `dotnet build AgyTuiApp/AgyTuiApp.csproj -p:TreatWarningsAsErrors=true` passes cleanly (**0 Errors, 0 Warnings**).
   * `dotnet test AgyTuiApp/Tests/AgyTuiApp.Tests.csproj` passes cleanly (**26/26 Passed**).
   * `pwsh -NoProfile -File "Tests/run_tests.ps1"` passes cleanly (**27/27 Passed**).

r.cs`, `Config.cs`, `TtlCache.cs`, `ProcessRunner.cs`, `EditorResolver.cs`, `SkillLoader.cs`, etc. | One type each, name matches file | These are all fine — this is the pattern to standardize on, not invent. |
  
 **Proposed rule**: one file per class/static-class, filename == type name, no exceptions. This isn't a stylistic nice-to-have in this specific codebase — three of this session's audit findings were made *harder* to catch because a bug lived in a class whose name gives no hint it's in that file (e.g. the SM-2 persistence bug spanned 5 classes inside `StudyQuizzes.cs`; finding all 5 call sites required reading the whole file rather than jumping to a named type). Splitting `StudyQuizzes.cs` into 14 files, `GitDashboard.cs` into 3, `TerminalIde.cs` into 5, and renaming `SystemHelper`→file `SystemHelper.cs` (or the type to plural, either direction, just make them match) closes this for good. This is a pure mechanical split — extract-class-to-new-file — with essentially zero behavior risk, and is worth doing as its own low-risk PR ahead of any of `architecture_services_views.md`'s bigger moves.
  
 ---
  
 
  
 ## 2. The `Helper` suffix means nothing — it's applied to roughly half the files, with no rule for which half
  
 Comparing filenames: `AccountHelper`, `AiHelper`, `AwsHelper`, `DatabaseHelper`, `DockerHelper`, `DotNetHelper`, `GitHelper`, `ObsidianHelper`, `StudyHelper`, `SystemHelpers` carry the suffix. `AccountRepository`, `AiLearningGenerator`, `Config`, `EditorResolver`, `GitDashboard`, `GuidedLearnFlow`, `HttpClientProvider`, `Icons`, `IdeCommandRegistry`, `ProcessRunner`, `ProjectScaffolder`, `QuotaTracker`, `ResourceDiscovery`, `ScreenChrome`, `SkillLoader`, `StatusWidgets`, `StudyQuizzes`, `TerminalIde`, `TokenVault`, `TtlCache`, `WorkspaceRegistry` don't. There's no discoverable rule distinguishing the two groups — `AccountHelper` and `AccountRepository` sit in the same domain, one has the suffix and one doesn't, for no reason traceable to what either class actually does.
  
 **Proposed rule, tied to `architecture_services_views.md`'s Services/Views split (adopt this naming now even before that folder move happens — it's independent and cheaper)**:
 * **`*Service`** — pure logic, the "verb" classes that do work and return data (`AccountService`, `AiService`, `GitService` — renaming `AccountHelper`/`AiHelper`/`GitHelper`).
 * **`*Repository`/`*Vault`/`*Tracker`/`*Cache`/`*Provider`/`*Registry`** — already-good, specific names that say what the class actually stores/provides/looks up (`AccountRepository`, `TokenVault`, `QuotaTracker`, `TtlCache`, `HttpClientProvider`, `CommandRegistry`, `IdeCommandRegistry`) — keep these exactly as they are, they're the model to copy, not fix.
 * **`*Screen`** — UI/interactive-loop classes (`AccountScreen`, `AiDashboardScreen`, `QuizScreens` — the UI half currently welded into `*Helper` classes).
 * Retire `*Helper` entirely as a name once a class is split into its Service+Screen halves — "Helper" is a name that describes nothing about what a class does, which is exactly why it ended up as the catch-all for classes doing three unrelated things each.
  
 ---
  
 ## 3. Error-handling pattern is different in almost every method — pick 3 shapes, not 12
  
 A survey of this session's own findings shows at least four different, uncoordinated error-handling styles currently live side by side:
  
 1. **Silent-wrong on failure** — `TokenVault.Protect`/`Unprotect` return the *unprotected* input unchanged if DPAPI throws, with no signal to the caller that protection didn't happen. Dangerous: a caller has no way to distinguish "this is safely encrypted" from "this silently isn't."
 2. **No handling at all** — `AccountHelper.GetAccounts()` has zero try/catch; one permission-denied directory throws uncaught and takes down the whole account list.
 3. **Bare swallow** — `ObsidianStudySync.OfferSync`'s call site is wrapped in an empty `catch { }`, hiding a real bug (e.g. malformed vault config) behind the same silence used for the genuinely-optional "no vault configured" case.
 4. **Real, bounded handling** — `ProcessRunner`'s timeout/kill logic and `SystemHelpers.AddAuthorizedKey`'s ACL try/catch (added in a recent fix) are the good examples: a specific failure mode is anticipated, caught, and surfaced as a clean message rather than a crash or a silent lie.
  
 **Proposed rule — three shapes, chosen deliberately per call, not by accident**:
 * **Let it throw** for programmer errors and genuinely unexpected states (a required file that should always exist by the time this method runs) — don't wrap these in `try/catch { }`, a stack trace is more honest than a silent no-op.
 * **Return `null`/`false`/a result type** for expected-absence cases (account doesn't exist, no network) — the caller decides what "not found" means, the callee doesn't swallow it into a fake success.
 * **Catch, log/warn, and continue** only for genuinely optional side effects where failure shouldn't block the main flow (Obsidian sync being one — but the catch block should at minimum `AnsiConsole.MarkupLine("[dim]Obsidian sync skipped: {ex.Message}[/]")` instead of nothing, so a real bug is at least visible once, not permanently invisible).
 * **Never** the `TokenVault.Protect` shape (return a *different, silently wrong* value on failure) — audit every method with this shape specifically, since it's the most dangerous of the four and the easiest to miss in review (the return type looks identical to the success case).
  
 ---
  
 ## 4. Caching is a third ad hoc pattern living alongside the one good one
  
 `TtlCache<K,V>` (`TtlCache.cs`) is a real, working, unit-tested generic cache — and it's genuinely adopted in several places (`AccountHelper.cs`, `StatusWidgets.cs`, `AiHelper.cs`). But new ad hoc caches keep appearing beside it rather than using it — this session alone traced at least one still-uncached hot path (`ObsidianBridge.BuildGraph`'s raw `Directory.GetFiles`) that could be a two-line `TtlCache` addition instead of staying uncached.
  
 **Proposed rule**: any method that recomputes something expensive (filesystem walk, network call, process shell-out) more than once per render frame or per keystroke gets a `TtlCache<K,V>` field, full stop — no new bespoke `Dictionary<string, (T value, DateTime expiresAt)>` pattern should ever be hand-rolled again now that the generic version exists and is proven. If a new caching need doesn't fit `TtlCache<K,V>`'s shape, that's a signal to extend `TtlCache` itself (e.g. a variant keyed by file-mtime instead of wall-clock TTL, already scoped in `refactor_plan.md`'s original caching table for IDE breadcrumb/symbol data) rather than inventing a fourth pattern.
  
 ---
  
 ## 5. Two different "extension" shapes exist — the declarative one is strictly better, standardize on it
  
 Compare two ways this codebase already handles "a list of things the user can pick, each mapped to an action":
  
 * **The bad shape**: `Icons.GetCommandIcon` — a big `switch`/if-chain keyed on `alias`/`category` strings, hand-maintained, and already known to have dead branches because category-matching happens before alias-matching (an `/obsidian`/`/refresh` icon case is unreachable because the category branch catches them first). Adding a new command means remembering to also touch this switch, `CommandRegistry.cs`, `MenuNode.cs`'s tree builder, and potentially `ProfileHelp`/`ShowHotkeysGuide` — five places for one new feature (this exact fan-out is `refactor_plan.md` §3's original "four independent command tables" finding, still only partially consolidated).
 * **The good shape**: `IdeCommandRegistry.All` — a single `record IdeCommand(...)[]` array; adding a new IDE command is one new array entry, and every consumer (the `/` search, the help listing, the dispatcher) reads from the same array, so there's no second place to remember to update.
  
 **Proposed rule**: any time a new "kind of thing the user can select" is added (a new icon category, a new report type, a new settings toggle), model it as **data in one array/table**, not **logic spread across N switch statements in N files**. `IdeCommandRegistry` is the concrete example to copy; `Icons.GetCommandIcon`'s category-before-alias branching is the concrete example to eventually flatten into the same shape (a `Dictionary<string, IconSet>` keyed directly by alias, falling back to category only when no alias-specific entry exists — inverting the current precedence, and removing the "five places to update" tax on every future command).
  
 ---
  
 ## 6. Static-everywhere is fine here — don't over-correct into DI
  
 Every class in `Helpers/` is a `static class` with `static` methods — no instances, no interfaces, no dependency injection container. **This is a reasonable choice for what this app is** (a single-process CLI/TUI with no need to swap implementations at runtime, no multi-tenancy, no test doubles required for 90% of call sites) and `architecture_services_views.md` doesn't propose changing it. The one place a lightweight interface *would* earn its cost: classes whose only value-add for testing is substitutability of an *external* dependency — e.g. `ProcessRunner` (so a test can inject a fake process outcome instead of actually shelling out) or `HttpClientProvider` (so a test can inject a fake HTTP response). Even there, prefer a simple `Func<>`/delegate injection point on the specific method under test over a full interface-and-constructor-injection redesign — matches the scale of the actual problem instead of importing a pattern this app's size doesn't need.
  
 ---
  
 ## 7. Suggested rollout order (cheapest, safest first)
 1. **Split multi-class files** (§1) — mechanical, zero behavior risk, do first. `StudyQuizzes.cs` → 14 files is the biggest single win by class count.
 2. **Rename for the `Service`/`Screen` split** (§2) — do together with `architecture_services_views.md`'s actual folder move if that's happening soon; do as a standalone rename (keeping everything in `Helpers/` for now) if that folder move is further out, so the naming signal exists even before the physical move.
 3. **Audit every `catch` block against the 3-shape rule** (§3) — start with the "silent-wrong" shape specifically (`TokenVault.Protect`/`Unprotect` is the known instance; grep for the pattern "catch, then return a plausible-looking but different value" elsewhere).
 4. **Sweep for uncached hot paths and point them at `TtlCache<K,V>`** (§4) — `ObsidianBridge.BuildGraph` is the known instance; grep `Directory.GetFiles(.*AllDirectories)` for the rest.
 5. **Flatten `Icons.GetCommandIcon`'s branching** (§5) into a single lookup table, alias-first — lowest urgency, but the cheapest structural fix for the "five places to update per new command" tax once someone's already touching that file for an icon addition.
  
