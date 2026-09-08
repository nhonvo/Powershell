package shelltool

import (
	"bufio"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"

	"agyterm/internal/model"
	"agyterm/internal/service/theme"
)

// ListExternalTools returns the status and configuration of external terminal apps
func ListExternalTools() []model.ExternalToolInfo {
	var tools []model.ExternalToolInfo

	// 1. Oh My Posh
	tools = append(tools, getOhMyPoshInfo())

	// 2. History & Autosuggestions (PSReadLine / ZSH)
	tools = append(tools, getHistoryAutosuggestInfo())

	// 3. fzf (Fuzzy Finder)
	tools = append(tools, getFzfInfo())

	// 4. Starship Prompt
	tools = append(tools, getStarshipInfo())

	// 5. eza (Modern ls)
	tools = append(tools, getEzaInfo())

	// 6. Zoxide (Smart cd)
	tools = append(tools, getZoxideInfo())

	// 7. Bat (Syntax Cat)
	tools = append(tools, getBatInfo())

	// 8. Git Shell Integration
	tools = append(tools, getGitInfo())

	// 9. Docker CLI & RAM
	tools = append(tools, getDockerInfo())

	return tools
}

func getOhMyPoshInfo() model.ExternalToolInfo {
	binPath := findBinary("oh-my-posh")
	installed := binPath != ""
	ver := ""
	if installed {
		ver = getCommandVersion(binPath, "--version")
	}

	curTheme := theme.GetSelectedTheme()
	if curTheme == "" {
		curTheme = "neko"
	}
	themesDir := theme.ResolveThemesDir()

	home, _ := os.UserHomeDir()
	configPath := filepath.Join(home, ".config", "selected_posh_theme.txt")

	summary := fmt.Sprintf("Active Theme: %s · 80+ themes available", curTheme)
	if !installed {
		summary = "Not found in PATH"
	}

	details := []string{
		fmt.Sprintf("Binary: %s", valueOrFallback(binPath, "Not installed")),
		fmt.Sprintf("Version: %s", valueOrFallback(ver, "Unknown")),
		fmt.Sprintf("Active Theme: %s", curTheme),
		fmt.Sprintf("Selected File: %s", configPath),
		fmt.Sprintf("Themes Dir: %s", valueOrFallback(themesDir, "None")),
		"Zsh Init: eval \"$(oh-my-posh init zsh --config <theme>.omp.json)\"",
		"PowerShell Init: oh-my-posh --init --shell pwsh --config <theme>.omp.json",
		"CLI Switcher: theme <theme-name> (instant shell switch)",
	}

	return model.ExternalToolInfo{
		ID:         "oh-my-posh",
		Name:       "Oh My Posh",
		Category:   "Prompt & Engine",
		Installed:  installed,
		BinaryPath: binPath,
		Version:    ver,
		ConfigPath: configPath,
		Summary:    summary,
		Details:    details,
		Tips:       "Use Tab 1 [Prompt Themes] in agyterm or run `theme <name>` to switch themes instantly.",
	}
}

func getHistoryAutosuggestInfo() model.ExternalToolInfo {
	home, _ := os.UserHomeDir()
	histFile := filepath.Join(home, ".zsh_history")
	histLines := 0
	histBytes := int64(0)

	if fi, err := os.Stat(histFile); err == nil {
		histBytes = fi.Size()
		if f, err := os.Open(histFile); err == nil {
			scanner := bufio.NewScanner(f)
			for scanner.Scan() {
				histLines++
			}
			_ = f.Close()
		}
	}

	pwshHist := `~\AppData\Roaming\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt`

	summary := fmt.Sprintf("Teal #70A99F · %d cmds · [Ctrl+Space] / [Up/Down]", histLines)

	details := []string{
		"Engine: ZSH Autosuggestions (Linux/WSL) · PSReadLine 2.x (Windows)",
		"Ghost Highlight: fg=#70A99F (Signature Teal)",
		"Strategy: [history, completion] with ListView prediction",
		fmt.Sprintf("Linux History File: %s (%d lines, %d KB)", histFile, histLines, histBytes/1024),
		fmt.Sprintf("Windows History File: %s", pwshHist),
		"Max Buffer: 50,000 commands (HISTSIZE=50000, SAVEHIST=50000)",
		"Accept Suggestion: [Ctrl+Space] · [Right Arrow] · [Ctrl+E]",
		"Prefix Search: [Up Arrow] (Backward) · [Down Arrow] (Forward)",
		"History Selector: [F7] (Out-GridView on Windows, fzf on Linux)",
		"Word Erase: [Ctrl+Backspace] / [Ctrl+W] · Word Jump: [Ctrl+Left/Right]",
	}

	return model.ExternalToolInfo{
		ID:         "history-autosuggest",
		Name:       "History & Autosuggest",
		Category:   "Shell & Prediction",
		Installed:  true,
		BinaryPath: "shell/windows + shell/linux profile",
		Version:    "PSReadLine 2.x / Zsh",
		ConfigPath: histFile,
		Summary:    summary,
		Details:    details,
		Tips:       "Type any command prefix and press [Up Arrow] to search history, or [Ctrl+Space] to accept suggestion.",
	}
}

func getFzfInfo() model.ExternalToolInfo {
	binPath := findBinary("fzf")
	installed := binPath != ""
	ver := ""
	if installed {
		ver = getCommandVersion(binPath, "--version")
	}

	summary := "Fuzzy history [Ctrl+R], files [Ctrl+T], cd [Alt+C]"
	if !installed {
		summary = "Not found (Install via `sudo apt install fzf`)"
	}

	details := []string{
		fmt.Sprintf("Binary: %s", valueOrFallback(binPath, "Not installed")),
		fmt.Sprintf("Version: %s", valueOrFallback(ver, "Unknown")),
		"Fuzzy History Search: [Ctrl+R] (interactive history browser)",
		"Fuzzy File Search:    [Ctrl+T] (paste file path into command line)",
		"Quick Directory cd:   [Alt+C]  (fzf directory hopper)",
		"Preview Mode:         Supports bat / cat file previewing",
	}

	return model.ExternalToolInfo{
		ID:         "fzf",
		Name:       "fzf (Fuzzy Finder)",
		Category:   "Navigation & Search",
		Installed:  installed,
		BinaryPath: binPath,
		Version:    ver,
		ConfigPath: "/etc/profile.d/fzf.zsh",
		Summary:    summary,
		Details:    details,
		Tips:       "Press [Ctrl+R] in shell to search all past commands interactively with fuzzy typing.",
	}
}

func getStarshipInfo() model.ExternalToolInfo {
	binPath := findBinary("starship")
	installed := binPath != ""
	ver := ""
	if installed {
		ver = getCommandVersion(binPath, "--version")
	}

	home, _ := os.UserHomeDir()
	configPath := filepath.Join(home, ".config", "starship.toml")

	summary := "Rust native prompt · Standby alternative"
	if !installed {
		summary = "Not installed"
	}

	details := []string{
		fmt.Sprintf("Binary: %s", valueOrFallback(binPath, "Not installed")),
		fmt.Sprintf("Version: %s", valueOrFallback(ver, "Unknown")),
		fmt.Sprintf("Config File: %s", configPath),
		"Type: Fast Rust prompt engine for Bash/Zsh/PowerShell",
		"Status: Standby (Oh My Posh is current default)",
		"Enable Command: eval \"$(starship init zsh)\"",
	}

	return model.ExternalToolInfo{
		ID:         "starship",
		Name:       "Starship Prompt",
		Category:   "Prompt & Engine",
		Installed:  installed,
		BinaryPath: binPath,
		Version:    ver,
		ConfigPath: configPath,
		Summary:    summary,
		Details:    details,
		Tips:       "Starship can be used as a featherlight fallback prompt if Oh My Posh is disabled.",
	}
}

func getEzaInfo() model.ExternalToolInfo {
	binPath := findBinary("eza")
	installed := binPath != ""
	ver := ""
	if installed {
		ver = getCommandVersion(binPath, "--version")
	}

	summary := "Modern ls with icons, git status, tree view"
	if !installed {
		summary = "Not installed (fallback to standard ls)"
	}

	details := []string{
		fmt.Sprintf("Binary: %s", valueOrFallback(binPath, "Not installed")),
		fmt.Sprintf("Version: %s", valueOrFallback(ver, "Unknown")),
		"Alias 'ls': eza --icons --group-directories-first",
		"Alias 'll': eza -l --icons --git --group-directories-first",
		"Alias 'la': eza -a --icons --group-directories-first",
		"Alias 'lt': eza --tree --level=2 --icons (tree visualizer)",
		"Nerd Font Icons: Supported across all filetypes",
	}

	return model.ExternalToolInfo{
		ID:         "eza",
		Name:       "eza (Modern ls)",
		Category:   "CLI Enhancements",
		Installed:  installed,
		BinaryPath: binPath,
		Version:    ver,
		ConfigPath: "shell/linux/posh-profile.zsh",
		Summary:    summary,
		Details:    details,
		Tips:       "Type `lt` in any project to see a beautiful 2-level directory tree with file icons.",
	}
}

func getZoxideInfo() model.ExternalToolInfo {
	binPath := findBinary("zoxide")
	installed := binPath != ""
	ver := ""
	if installed {
		ver = getCommandVersion(binPath, "--version")
	}

	summary := "Frecency-based smart cd hopper (`z <dir>`)"
	if !installed {
		summary = "Optional (`curl -sS https://raw.githubusercontent.com/ajeetdsouza/zoxide/main/install.sh | bash`)"
	}

	details := []string{
		fmt.Sprintf("Binary: %s", valueOrFallback(binPath, "Not installed")),
		fmt.Sprintf("Version: %s", valueOrFallback(ver, "Unknown")),
		"Purpose: Remembers directories you visit most often",
		"Usage: `z <query>` jumps to best matching workspace",
		"Complement: `proj` function in profile hops to ~/projects/",
	}

	return model.ExternalToolInfo{
		ID:         "zoxide",
		Name:       "Zoxide (Smart cd)",
		Category:   "Navigation & Search",
		Installed:  installed,
		BinaryPath: binPath,
		Version:    ver,
		ConfigPath: "~/.local/share/zoxide",
		Summary:    summary,
		Details:    details,
		Tips:       "Install zoxide to jump across nested project directories in 1 keystroke.",
	}
}

func getBatInfo() model.ExternalToolInfo {
	binPath := findBinary("bat")
	if binPath == "" {
		binPath = findBinary("batcat")
	}
	installed := binPath != ""
	ver := ""
	if installed {
		ver = getCommandVersion(binPath, "--version")
	}

	summary := "Cat clone with syntax highlighting & git diffs"
	if !installed {
		summary = "Optional (`sudo apt install bat`)"
	}

	details := []string{
		fmt.Sprintf("Binary: %s", valueOrFallback(binPath, "Not installed")),
		fmt.Sprintf("Version: %s", valueOrFallback(ver, "Unknown")),
		"Features: Syntax highlighting, line numbers, git change indicators",
		"Profile Viewer: `view <file>` / `cat-file <file>`",
	}

	return model.ExternalToolInfo{
		ID:         "bat",
		Name:       "Bat (Syntax Pager)",
		Category:   "CLI Enhancements",
		Installed:  installed,
		BinaryPath: binPath,
		Version:    ver,
		ConfigPath: "~/.config/bat/config",
		Summary:    summary,
		Details:    details,
		Tips:       "Use `view <file>` or `bat <file>` to read source files with color highlighting and line numbering.",
	}
}

func getGitInfo() model.ExternalToolInfo {
	binPath := findBinary("git")
	installed := binPath != ""
	ver := ""
	if installed {
		ver = getCommandVersion(binPath, "--version")
	}

	summary := "Active · Prompt branch detection & AGYGIT Cockpit"

	details := []string{
		fmt.Sprintf("Binary: %s", valueOrFallback(binPath, "Not installed")),
		fmt.Sprintf("Version: %s", valueOrFallback(ver, "Unknown")),
		"Shortcuts: gs (status), gd (diff), glo (graph), gb (branch)",
		"Commit Helpers: gcommit <msg> (auto-concatenates arguments)",
		"Workflow Suite: `agygit` - Interactive TUI Cockpit & Worktrees",
	}

	return model.ExternalToolInfo{
		ID:         "git",
		Name:       "Git Integration",
		Category:   "VCS & Workflow",
		Installed:  installed,
		BinaryPath: binPath,
		Version:    ver,
		ConfigPath: "~/.gitconfig",
		Summary:    summary,
		Details:    details,
		Tips:       "Run `agygit` or `agyg` for the interactive multi-agent Git cockpit.",
	}
}

func getDockerInfo() model.ExternalToolInfo {
	binPath := findBinary("docker")
	installed := binPath != ""
	ver := ""
	if installed {
		ver = getCommandVersion(binPath, "--version")
	}

	summary := "Active · Compose grouping & AGYDOCKER RAM Guard"

	details := []string{
		fmt.Sprintf("Binary: %s", valueOrFallback(binPath, "Not installed")),
		fmt.Sprintf("Version: %s", valueOrFallback(ver, "Unknown")),
		"Shortcuts: dk (ps), dcup (compose up), dcdown (compose down)",
		"Cleanup Helpers: fix-volume (prune volumes), fix-image (prune images)",
		"Workflow Suite: `agydocker` - Compose project grouping & WSL2 RAM Guard",
	}

	return model.ExternalToolInfo{
		ID:         "docker",
		Name:       "Docker & WSL2 RAM",
		Category:   "Containers & Infra",
		Installed:  installed,
		BinaryPath: binPath,
		Version:    ver,
		ConfigPath: "~/.docker/config.json",
		Summary:    summary,
		Details:    details,
		Tips:       "Run `agydocker` or `agyd` to manage container groups and monitor WSL2 memory consumption.",
	}
}

func findBinary(name string) string {
	if p, err := exec.LookPath(name); err == nil {
		return p
	}
	home, _ := os.UserHomeDir()
	localBin := filepath.Join(home, ".local", "bin", name)
	if fi, err := os.Stat(localBin); err == nil && !fi.IsDir() {
		return localBin
	}
	return ""
}

func getCommandVersion(bin string, arg string) string {
	out, err := exec.Command(bin, arg).Output()
	if err != nil {
		return ""
	}
	lines := strings.Split(strings.TrimSpace(string(out)), "\n")
	if len(lines) > 0 {
		return strings.TrimSpace(lines[0])
	}
	return ""
}

func valueOrFallback(v, fallback string) string {
	if strings.TrimSpace(v) == "" {
		return fallback
	}
	return v
}
