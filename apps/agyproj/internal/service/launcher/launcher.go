package launcher

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
)

type Launcher struct {
	Runner func(name string, args ...string) error
}

func NewLauncher() *Launcher {
	return &Launcher{
		Runner: func(name string, args ...string) error {
			cmd := exec.Command(name, args...)
			cmd.Stdin = os.Stdin
			cmd.Stdout = os.Stdout
			cmd.Stderr = os.Stderr
			return cmd.Run()
		},
	}
}

// Launch opens the project directory with the given IDE or tool.
func (l *Launcher) Launch(ide string, projectDir string) error {
	ide = strings.ToLower(strings.TrimSpace(ide))
	if ide == "" {
		ide = "code"
	}

	switch ide {
	case "code", "vscode":
		if _, err := exec.LookPath("code"); err != nil {
			if cand := findWindowsVSCode(); cand != "" {
				return l.Runner(cand, projectDir)
			}
		}
		return l.Runner("code", projectDir)
	case "cursor":
		if _, err := exec.LookPath("cursor"); err != nil {
			return l.Launch("code", projectDir)
		}
		return l.Runner("cursor", projectDir)
	case "nvim", "neovim":
		if _, err := exec.LookPath("nvim"); err == nil {
			return l.runInDir("nvim", projectDir, ".")
		}
		if _, err := exec.LookPath("vim"); err == nil {
			return l.runInDir("vim", projectDir, ".")
		}
		if _, err := exec.LookPath("nano"); err == nil {
			return l.runInDir("nano", projectDir, ".")
		}
		return l.Launch("code", projectDir)
	case "vim":
		if _, err := exec.LookPath("vim"); err == nil {
			return l.runInDir("vim", projectDir, ".")
		}
		return l.Launch("nvim", projectDir)
	case "agy", "antigravity":
		return l.runInDir("agy", projectDir)
	case "term", "terminal", "sh", "shell":
		shell := os.Getenv("SHELL")
		if shell == "" {
			shell = "/bin/bash"
		}
		return l.runInDir(shell, projectDir)
	case "files", "explorer":
		if _, err := exec.LookPath("explorer.exe"); err == nil {
			return l.Runner("explorer.exe", projectDir)
		}
		return l.Runner("xdg-open", projectDir)
	default:
		return l.Runner(ide, projectDir)
	}
}

func (l *Launcher) runInDir(name string, dir string, args ...string) error {
	cmd := exec.Command(name, args...)
	cmd.Dir = dir
	cmd.Stdin = os.Stdin
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	return cmd.Run()
}

func AvailableIDEs() []string {
	var ides []string
	candidates := []string{"code", "cursor", "nvim", "vim", "agy"}
	for _, c := range candidates {
		if _, err := exec.LookPath(c); err == nil {
			ides = append(ides, c)
		}
	}
	return ides
}

func FormatIdeName(ide string) string {
	switch strings.ToLower(ide) {
	case "code", "vscode":
		return "VS Code"
	case "cursor":
		return "Cursor AI"
	case "nvim", "neovim":
		return "Neovim"
	case "agy", "antigravity":
		return "Antigravity CLI"
	default:
		return ide
	}
}

// findWindowsVSCode attempts to dynamically discover the VS Code executable in WSL/Windows.
func findWindowsVSCode() string {
	// 1. Check LOCALAPPDATA environment variable
	if localApp := os.Getenv("LOCALAPPDATA"); localApp != "" {
		if p, err := windowsToWslPath(localApp); err == nil {
			cand := filepath.Join(p, "Programs", "Microsoft VS Code", "bin", "code")
			if isFile(cand) {
				return cand
			}
		}
	}

	// 2. Check USERPROFILE environment variable
	if userProf := os.Getenv("USERPROFILE"); userProf != "" {
		if p, err := windowsToWslPath(userProf); err == nil {
			cand := filepath.Join(p, "AppData", "Local", "Programs", "Microsoft VS Code", "bin", "code")
			if isFile(cand) {
				return cand
			}
		}
	}

	// 3. Dynamically check cmd.exe /c echo %LOCALAPPDATA%
	if out, err := exec.Command("cmd.exe", "/c", "echo %LOCALAPPDATA%").Output(); err == nil {
		raw := strings.TrimSpace(string(out))
		if raw != "" && !strings.Contains(raw, "%LOCALAPPDATA%") {
			if p, err := windowsToWslPath(raw); err == nil {
				cand := filepath.Join(p, "Programs", "Microsoft VS Code", "bin", "code")
				if isFile(cand) {
					return cand
				}
			}
		}
	}

	// 4. Dynamically check cmd.exe /c echo %USERPROFILE%
	if out, err := exec.Command("cmd.exe", "/c", "echo %USERPROFILE%").Output(); err == nil {
		raw := strings.TrimSpace(string(out))
		if raw != "" && !strings.Contains(raw, "%USERPROFILE%") {
			if p, err := windowsToWslPath(raw); err == nil {
				cand := filepath.Join(p, "AppData", "Local", "Programs", "Microsoft VS Code", "bin", "code")
				if isFile(cand) {
					return cand
				}
			}
		}
	}

	// 5. Fallback glob across /mnt/c/Users/*/AppData/Local/Programs/Microsoft VS Code/bin/code
	matches, _ := filepath.Glob("/mnt/c/Users/*/AppData/Local/Programs/Microsoft VS Code/bin/code")
	for _, m := range matches {
		if isFile(m) {
			return m
		}
	}

	return ""
}

func windowsToWslPath(winPath string) (string, error) {
	winPath = strings.TrimSpace(winPath)
	if winPath == "" {
		return "", fmt.Errorf("empty path")
	}
	// Try wslpath utility if present
	if out, err := exec.Command("wslpath", "-u", winPath).Output(); err == nil {
		res := strings.TrimSpace(string(out))
		if res != "" {
			return res, nil
		}
	}
	// Manual drive translation fallback (e.g. C:\path -> /mnt/c/path)
	if len(winPath) >= 2 && winPath[1] == ':' {
		drive := strings.ToLower(string(winPath[0]))
		rest := strings.ReplaceAll(winPath[2:], `\`, `/`)
		return filepath.Clean(fmt.Sprintf("/mnt/%s/%s", drive, strings.TrimPrefix(rest, "/"))), nil
	}
	return filepath.Clean(winPath), nil
}

func isFile(path string) bool {
	fi, err := os.Stat(path)
	return err == nil && !fi.IsDir()
}

// WindowsToWslPath converts a Windows path (e.g. C:\Users) to a WSL path (/mnt/c/Users).
func WindowsToWslPath(winPath string) (string, error) {
	return windowsToWslPath(winPath)
}

// FindWindowsVSCode dynamically discovers the VS Code binary on Windows/WSL.
func FindWindowsVSCode() string {
	return findWindowsVSCode()
}


