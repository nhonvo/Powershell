# AGYDOCKER & AGYTERM Fix and Feature Implementation Report

**Author**: Infra & Styling Tools Specialist (agydocker & agyterm)  
**Date**: September 8, 2026  
**Status**: All Tests Passing cleanly (100% Success)

---

## 1. Executive Summary

All requested fixes, error handling enhancements, and feature implementations for **agydocker** and **agyterm** have been implemented and verified. In adherence to critical project instructions, **agyswitch** account management, tokens, vault credentials, and configuration files remained completely untouched.

---

## 2. Key Changes Implemented

### 2.1 agydocker

1. **Volume Prune Implementation**:
   - Added `PruneVolumes() (string, error)` in [dockerops.go](./apps/agydocker/internal/service/dockerops/dockerops.go), executing `docker volume prune -f`.
   - In [app.go](./apps/agydocker/internal/view/app.go): Fixed Tab 2 (Volumes) key `P` handler so that pressing `P` triggers `dockerops.PruneVolumes()` instead of `dockerops.PruneSystem()`.
   - Added unit test `TestDockerOps_PruneVolumes` in [dockerops_test.go](./apps/agydocker/internal/service/dockerops/dockerops_test.go).

2. **Docker Daemon Offline Detection & Warning Banner**:
   - In [app.go](./apps/agydocker/internal/view/app.go): Added `dockerErr error` field to the `App` struct.
   - Captured the error from `dockerops.ListContainers()` into `a.dockerErr` instead of discarding with `_`.
   - When Docker daemon is unreachable or offline, rendered a prominent warning banner in the UI:
     `⚠️ Docker daemon is offline / cannot connect to docker.sock`.
   - Updated non-interactive `PrintStatus` to also report this warning when offline.
   - Added `TestApp_DockerDaemonWarning` in [view_test.go](./apps/agydocker/internal/view/view_test.go).

3. **Container Kill & Remove Operations**:
   - Added `KillContainer(id string) error` executing `docker kill <id>` in [dockerops.go](./apps/agydocker/internal/service/dockerops/dockerops.go).
   - Added `RemoveContainer(id string) error` executing `docker rm -f <id>` in [dockerops.go](./apps/agydocker/internal/service/dockerops/dockerops.go).
   - In [app.go](./apps/agydocker/internal/view/app.go):
     - Bound `k` / `K` in Tab 0 to kill the selected container in background with status update and spinner badge `[Kill]`.
     - Bound `d` / `D` in Tab 0 to force-remove the selected container after interactive confirmation:
       `Force remove container '<name>'? (y/N): `.
     - Updated UI footers and hotkey guides across compact and full-width views to display `[K] Kill` and `[D] Rm`.
   - Added unit tests `TestDockerOps_KillContainer` and `TestDockerOps_RemoveContainer` in [dockerops_test.go](./apps/agydocker/internal/service/dockerops/dockerops_test.go), plus pending action tests in [view_test.go](./apps/agydocker/internal/view/view_test.go).

4. **CLI Subcommands in agydocker**:
   - In [main.go](./apps/agydocker/main.go): Added CLI commands:
     - `agydocker kill <id>`: Sends SIGKILL to a container.
     - `agydocker rm <id>` / `agydocker delete <id>`: Force removes container (`docker rm -f`).
   - Updated `printHelp()` with descriptions for `kill` and `rm`.

---

### 2.2 agyterm

1. **Linux Terminal Emulator Service (`linuxterm`)**:
   - Created [linuxterm.go](./apps/agyterm/internal/service/linuxterm/linuxterm.go) in `apps/agyterm/internal/service/linuxterm`:
     - Supported emulators: Alacritty (`~/.config/alacritty/alacritty.toml`), Kitty (`~/.config/kitty/kitty.conf`), and WezTerm (`~/.config/wezterm/wezterm.lua`).
     - Implemented `DetectTerminals() []EmulatorInfo` which checks binary paths and config presence across standard user directories.
     - Implemented `ReadTerminalConfig` for parsing font face, font size, and background opacity.
     - Implemented `SetTerminalFont` and `SetTerminalOpacity` for configuring font face, font size, and background opacity across TOML, Kitty conf, and Lua configurations (creating files and directory structures when not pre-existing).
     - Added `CanonicalFontName` normalizer mapping shorthand names (e.g. `hack`, `fira-code`, `jetbrains-mono`) to official Nerd Font release names.
   - Created comprehensive unit tests in [linuxterm_test.go](./apps/agyterm/internal/service/linuxterm/linuxterm_test.go):
     - `TestDefaultConfigPath`
     - `TestDetectTerminals`
     - `TestAlacrittyConfig` (create & update font/opacity)
     - `TestKittyConfig` (create & update font/opacity)
     - `TestWezTermConfig` (create & update font/opacity)
     - `TestCanonicalFontName`
     - `TestInstallNerdFont_Empty`

2. **Automated Font Installer (`font install <name>`)**:
   - Implemented `InstallNerdFont(fontName string)` in [linuxterm.go](./apps/agyterm/internal/service/linuxterm/linuxterm.go):
     - Downloads official release archives from GitHub (`ryanoasis/nerd-fonts`).
     - Extracts fonts into `~/.local/share/fonts/`.
     - Refreshes font cache by running `fc-cache -f`.
     - If network/download fails or system is offline, produces clear step-by-step manual instructions.
   - In [main.go](./apps/agyterm/main.go):
     - Added `font install <name>` command handling.
     - Updated `printHelp()` with usage examples and popular font recommendations (`Hack`, `FiraCode`, `JetBrainsMono`, `CascadiaCode`, `Meslo`, `UbuntuMono`, `SourceCodePro`).

---

## 3. Verification & Test Results

### 3.1 agydocker Tests (`go test -count=1 -v ./...`)
```
=== RUN   TestDockerOps_GetMemoryInfo
--- PASS: TestDockerOps_GetMemoryInfo (0.00s)
=== RUN   TestDockerOps_ListContainers
--- PASS: TestDockerOps_ListContainers (0.01s)
=== RUN   TestDockerOps_PruneVolumes
--- PASS: TestDockerOps_PruneVolumes (0.01s)
=== RUN   TestDockerOps_KillContainer
--- PASS: TestDockerOps_KillContainer (0.01s)
=== RUN   TestDockerOps_RemoveContainer
--- PASS: TestDockerOps_RemoveContainer (0.01s)
PASS
ok  	agydocker/internal/service/dockerops	0.054s

=== RUN   TestApp_PrintStatus
--- PASS: TestApp_PrintStatus (0.01s)
=== RUN   TestApp_Render
--- PASS: TestApp_Render (0.00s)
=== RUN   TestApp_PendingActions
--- PASS: TestApp_PendingActions (0.00s)
=== RUN   TestApp_DockerDaemonWarning
--- PASS: TestApp_DockerDaemonWarning (0.01s)
PASS
ok  	agydocker/internal/view	0.025s
```

### 3.2 agyterm Tests (`go test -count=1 -v ./...`)
```
=== RUN   TestDefaultConfigPath
--- PASS: TestDefaultConfigPath (0.00s)
=== RUN   TestDetectTerminals
--- PASS: TestDetectTerminals (0.37s)
=== RUN   TestAlacrittyConfig
--- PASS: TestAlacrittyConfig (0.00s)
=== RUN   TestKittyConfig
--- PASS: TestKittyConfig (0.00s)
=== RUN   TestWezTermConfig
--- PASS: TestWezTermConfig (0.00s)
=== RUN   TestCanonicalFontName
--- PASS: TestCanonicalFontName (0.00s)
=== RUN   TestInstallNerdFont_Empty
--- PASS: TestInstallNerdFont_Empty (0.00s)
PASS
ok  	agyterm/internal/service/linuxterm	0.380s

=== RUN   TestListExternalTools
--- PASS: TestListExternalTools (0.47s)
PASS
ok  	agyterm/internal/service/shelltool	0.476s
=== RUN   TestTheme_ListAndSetTheme
--- PASS: TestTheme_ListAndSetTheme (0.00s)
PASS
ok  	agyterm/internal/service/theme	0.004s
=== RUN   TestWinTerm_ReadAndModifyProfiles
--- PASS: TestWinTerm_ReadAndModifyProfiles (0.00s)
=== RUN   TestWinTerm_ListAvailableFonts
--- PASS: TestWinTerm_ListAvailableFonts (0.03s)
PASS
ok  	agyterm/internal/service/winterm	0.033s
=== RUN   TestApp_PrintStatus
--- PASS: TestApp_PrintStatus (0.37s)
=== RUN   TestApp_Render
--- PASS: TestApp_Render (0.01s)
=== RUN   TestApp_FilteredThemes
--- PASS: TestApp_FilteredThemes (0.02s)
PASS
ok  	agyterm/internal/view	0.400s
```

---

## 4. Modified & Created Files Reference

- **Report**: [AGYDOCKER_AGYTERM_FIX_REPORT.md](./AGYDOCKER_AGYTERM_FIX_REPORT.md)
- **Windows UNC Path**: `\\wsl.localhost\Ubuntu\home\truongnhon\projects\powershell-profile\AGYDOCKER_AGYTERM_FIX_REPORT.md`
- **Source Files Modified / Created**:
  - [dockerops.go](./apps/agydocker/internal/service/dockerops/dockerops.go)
  - [dockerops_test.go](./apps/agydocker/internal/service/dockerops/dockerops_test.go)
  - [app.go (agydocker)](./apps/agydocker/internal/view/app.go)
  - [view_test.go (agydocker)](./apps/agydocker/internal/view/view_test.go)
  - [main.go (agydocker)](./apps/agydocker/main.go)
  - [linuxterm.go](./apps/agyterm/internal/service/linuxterm/linuxterm.go)
  - [linuxterm_test.go](./apps/agyterm/internal/service/linuxterm/linuxterm_test.go)
  - [main.go (agyterm)](./apps/agyterm/main.go)
