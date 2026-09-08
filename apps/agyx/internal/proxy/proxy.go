package proxy

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
)

// ToolDefinition describes a managed tool in the suite
type ToolDefinition struct {
	Name        string   `json:"name"`
	BinaryName  string   `json:"binary_name"`
	Aliases     []string `json:"aliases"`
	Description string   `json:"description"`
}

// GetRegisteredTools returns all registered programs in the agy suite
func GetRegisteredTools() []ToolDefinition {
	return []ToolDefinition{
		{
			Name:        "switch",
			BinaryName:  "agyswitch",
			Aliases:     []string{"s", "acc", "vault", "quota"},
			Description: "Antigravity context, multi-account vault, model quotas & sessions",
		},
		{
			Name:        "proj",
			BinaryName:  "agyproj",
			Aliases:     []string{"p", "ws", "project", "ide"},
			Description: "Project workspaces registry, stack detection & IDE launchers",
		},
		{
			Name:        "git",
			BinaryName:  "agygit",
			Aliases:     []string{"g", "wt", "worktree"},
			Description: "Multi-Agent Git fleet status & isolated branch worktrees",
		},
		{
			Name:        "docker",
			BinaryName:  "agydocker",
			Aliases:     []string{"d", "ram", "ps"},
			Description: "Container fleet lifecycle & WSL2 RAM/Swap resource guard",
		},
		{
			Name:        "term",
			BinaryName:  "agyterm",
			Aliases:     []string{"t", "theme", "font"},
			Description: "Terminal font customizer (Windows Terminal) & prompt themes",
		},
	}
}

// ResolveTool matches a subcommand or alias to a registered tool
func ResolveTool(aliasOrName string) *ToolDefinition {
	aliasOrName = strings.ToLower(strings.TrimSpace(aliasOrName))
	tools := GetRegisteredTools()
	for _, t := range tools {
		if strings.EqualFold(t.Name, aliasOrName) {
			return &t
		}
		for _, a := range t.Aliases {
			if strings.EqualFold(a, aliasOrName) {
				return &t
			}
		}
	}
	return nil
}

// FindBinary searches for the tool binary
func FindBinary(binName string) (string, error) {
	// 1. Look in ~/.local/bin
	home, _ := os.UserHomeDir()
	localBin := filepath.Join(home, ".local", "bin", binName)
	if fi, err := os.Stat(localBin); err == nil && !fi.IsDir() {
		return localBin, nil
	}

	// 2. Look in PATH
	if path, err := exec.LookPath(binName); err == nil {
		return path, nil
	}

	// 3. Look relative to current project repo
	repoBin := filepath.Join(home, "projects", "powershell-profile", "apps", binName, binName)
	if fi, err := os.Stat(repoBin); err == nil && !fi.IsDir() {
		return repoBin, nil
	}

	return "", fmt.Errorf("binary '%s' not found. Run 'go build' or check ~/.local/bin", binName)
}

// Execute proxies input/output/error directly to the child process
func Execute(binName string, args []string) error {
	binPath, err := FindBinary(binName)
	if err != nil {
		return err
	}

	cmd := exec.Command(binPath, args...)
	cmd.Stdin = os.Stdin
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	cmd.Env = os.Environ()

	return cmd.Run()
}
