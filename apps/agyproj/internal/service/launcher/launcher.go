package launcher

import (
	"os"
	"os/exec"
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
			cand := "/mnt/c/Users/TruongNhon/AppData/Local/Programs/Microsoft VS Code/bin/code"
			if fi, errStat := os.Stat(cand); errStat == nil && !fi.IsDir() {
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
