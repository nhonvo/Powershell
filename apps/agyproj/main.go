package main

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"agyproj/internal/service/launcher"
	"agyproj/internal/service/registry"
	"agyproj/internal/view"
)

func main() {
	userHome, err := os.UserHomeDir()
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error getting user home directory: %v\n", err)
		os.Exit(1)
	}

	reg := registry.NewManager(userHome)
	lnch := launcher.NewLauncher()
	app := view.NewApp(reg, lnch)

	args := os.Args[1:]
	if len(args) == 0 {
		if err := app.Run(); err != nil {
			fmt.Fprintf(os.Stderr, "Error running TUI: %v\n", err)
			os.Exit(1)
		}
		return
	}

	cmd := strings.ToLower(args[0])

	switch cmd {
	case "list", "ls":
		list := reg.ListRegistered()
		if len(list) == 0 {
			fmt.Println("No registered workspaces found. Run 'agyproj scan' to discover projects.")
			return
		}
		fmt.Printf("\n📁 \033[1;36mRegistered Workspaces (%d total):\033[0m\n\n", len(list))
		for i, p := range list {
			active := "  "
			if p.IsActive {
				active = "\033[1;32m●\033[0m "
			}
			pin := "  "
			if p.IsPinned {
				pin = "\033[1;33m★\033[0m "
			}
			git := p.GitBranch
			if p.IsDirty {
				git = fmt.Sprintf("\033[33m%s (dirty: %d)\033[0m", p.GitBranch, p.DirtyCount)
			}
			fmt.Printf(" %s%s%2d. \033[1m%-24s\033[0m \033[35m[%-22s]\033[0m %-18s (%s)\n",
				active, pin, i+1, p.Name, p.Stack, git, p.Path)
		}
		fmt.Println()

	case "scan":
		root := filepath.Join(userHome, "projects")
		if len(args) >= 2 {
			root = args[1]
		}
		discovered, err := reg.ScanDirectory(root)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error scanning '%s': %v\n", root, err)
			os.Exit(1)
		}
		fmt.Printf("\n🔍 \033[1;36mDiscovered Projects in '%s' (%d total):\033[0m\n\n", root, len(discovered))
		for i, p := range discovered {
			status := "\033[36m[+ New]\033[0m"
			if p.IsPinned || p.IsActive {
				status = "\033[32m[✔ Registered]\033[0m"
			}
			fmt.Printf(" %2d. %-14s \033[1m%-26s\033[0m \033[35m[%-24s]\033[0m (%s)\n",
				i+1, status, p.Name, p.Stack, p.GitBranch)
		}
		fmt.Println()

	case "register", "add":
		if len(args) < 2 {
			cwd, _ := os.Getwd()
			args = append(args, cwd)
		}
		target := args[1]
		info, err := reg.Register(target, true)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error registering '%s': %v\n", target, err)
			os.Exit(1)
		}
		fmt.Printf("\033[32m✔ Successfully registered and pinned '%s' (%s)\033[0m\n", info.Name, info.Path)

	case "register-all", "add-all":
		root := filepath.Join(userHome, "projects")
		if len(args) >= 2 {
			root = args[1]
		}
		count, err := reg.RegisterAll(root)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error registering all in '%s': %v\n", root, err)
			os.Exit(1)
		}
		fmt.Printf("\033[32m✔ Successfully registered %d projects from '%s'\033[0m\n", count, root)

	case "unregister", "rm", "delete":
		if len(args) < 2 {
			fmt.Println("Usage: agyproj rm <project-name-or-id>")
			os.Exit(1)
		}
		target := args[1]
		if err := reg.Unregister(target); err != nil {
			fmt.Fprintf(os.Stderr, "Error unregistering '%s': %v\n", target, err)
			os.Exit(1)
		}
		fmt.Printf("\033[33m✔ Unregistered workspace '%s'\033[0m\n", target)

	case "pin":
		if len(args) < 2 {
			fmt.Println("Usage: agyproj pin <project-name-or-id>")
			os.Exit(1)
		}
		target := args[1]
		status, err := reg.TogglePin(target)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error toggling pin for '%s': %v\n", target, err)
			os.Exit(1)
		}
		if status {
			fmt.Printf("\033[32m★ Pinned '%s' to top of working workspaces\033[0m\n", target)
		} else {
			fmt.Printf("\033[33mUnpinned '%s'\033[0m\n", target)
		}

	case "cd":
		if len(args) < 2 {
			fmt.Println(filepath.Join(userHome, "projects"))
			return
		}
		target := strings.ToLower(args[1])
		list := reg.ListRegistered()
		for _, p := range list {
			if strings.EqualFold(p.ID, target) || strings.EqualFold(p.Name, target) || strings.Contains(strings.ToLower(p.Name), target) {
				fmt.Println(p.Path)
				return
			}
		}
		// Fallback to checking ~/projects/<target> directly
		cand := filepath.Join(userHome, "projects", args[1])
		if fi, err := os.Stat(cand); err == nil && fi.IsDir() {
			fmt.Println(cand)
			return
		}
		fmt.Fprintf(os.Stderr, "Workspace '%s' not found\n", args[1])
		os.Exit(1)

	case "open":
		if len(args) < 2 {
			fmt.Println("Usage: agyproj open <project-name> [code|cursor|nvim|agy]")
			os.Exit(1)
		}
		target := strings.ToLower(args[1])
		ide := "code"
		if len(args) >= 3 {
			ide = args[2]
		}
		targetPath := ""
		list := reg.ListRegistered()
		for _, p := range list {
			if strings.EqualFold(p.ID, target) || strings.EqualFold(p.Name, target) || strings.Contains(strings.ToLower(p.Name), target) {
				targetPath = p.Path
				break
			}
		}
		if targetPath == "" {
			cand := filepath.Join(userHome, "projects", args[1])
			if fi, err := os.Stat(cand); err == nil && fi.IsDir() {
				targetPath = cand
			}
		}
		if targetPath == "" {
			fmt.Fprintf(os.Stderr, "Project '%s' not found\n", args[1])
			os.Exit(1)
		}
		fmt.Printf("\033[36m[agyproj]\033[0m Launching %s in '%s'...\n", launcher.FormatIdeName(ide), targetPath)
		if err := lnch.Launch(ide, targetPath); err != nil {
			fmt.Fprintf(os.Stderr, "Error launching %s: %v\n", ide, err)
			os.Exit(1)
		}

	case "init":
		// Print shell helper function
		fmt.Println(`# For Bash / Zsh (add to ~/.bashrc or ~/.zshrc):
proj() {
  if [ -z "$1" ]; then
    agyproj
  else
    local dir
    dir="$(agyproj cd "$1" 2>/dev/null)"
    if [ -n "$dir" ]; then
      cd "$dir" || return
    else
      echo "Workspace not found: $1"
    fi
  fi
}

# For PowerShell (add to $PROFILE):
function proj {
    param([string]$target)
    if (-not $target) {
        agyproj
    } else {
        $dir = agyproj cd $target
        if ($dir -and (Test-Path $dir)) {
            Set-Location $dir
        }
    }
}`)

	default:
		fmt.Println("Unknown command. Available commands: ls, scan, register, unregister, pin, cd, open, init")
		os.Exit(1)
	}
}
