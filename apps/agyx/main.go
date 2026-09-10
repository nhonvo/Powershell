package main

import (
	"fmt"
	"os"
	"os/exec"
	"strings"

	"agyx/internal/proxy"
	"agyx/internal/shellgen"
	"agyx/internal/view"
)

func main() {
	if len(os.Args) == 1 {
		app := view.NewCockpitApp()
		if err := app.RunInteractive(); err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		return
	}

	cmd := os.Args[1]

	// 1. Builtin suite commands
	switch cmd {
	case "ls", "list":
		app := view.NewCockpitApp()
		app.PrintStatus(os.Stdout)
		return

	case "status":
		printSuiteStatus()
		return

	case "init":
		printShellInit()
		return

	case "tools", "util", "utils":
		app := view.NewCockpitApp()
		app.ActiveTab = 5
		if err := app.RunInteractive(); err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		return

	case "help", "-h", "--help":
		printHelp()
		return
	}

	// 2. Subcommand routing to registered suite tools
	tool := proxy.ResolveTool(cmd)
	if tool != nil {
		args := os.Args[2:]
		if err := proxy.Execute(tool.BinaryName, args); err != nil {
			if exitErr, ok := err.(*exec.ExitError); ok {
				os.Exit(exitErr.ExitCode())
			}
			os.Exit(1)
		}
		return
	}

	fmt.Fprintf(os.Stderr, "Unknown suite command '%s'. Run 'agyx help' for usage.\n", cmd)
	os.Exit(1)
}

func printHelp() {
	fmt.Println(`⚡ AGYX - Unified Developer Suite Proxy & Orchestrator (Go Engine)

Usage:
  agyx                             Launch interactive Master Cockpit TUI
  agyx status                      Print live health & status across all suite modules
  agyx init                        Generate shell functions for Zsh, Bash, and PowerShell
  agyx ls                          List all registered tools and their aliases

Registered Module Proxies:
  agyx switch [args...]            Proxy to 'agyswitch' (Context, Quota, Vault, Skills)
       Aliases: s, acc, vault, quota
  agyx proj [args...]              Proxy to 'agyproj' (Workspaces, Stack Detector, IDE)
       Aliases: p, ws, project, ide
  agyx git [args...]               Proxy to 'agygit' (Multi-Agent Git Fleet & Worktrees)
       Aliases: g, wt, worktree
  agyx docker [args...]            Proxy to 'agydocker' (Container Fleet & WSL2 RAM Guard)
       Aliases: d, ram, ps
  agyx term [args...]              Proxy to 'agyterm' (Terminal Fonts & Shell Themes)
       Aliases: t, theme, font
  agyx mobile [args...]            Proxy to 'agymobile' (Mobile Cockpit & Remote Station)
       Aliases: m, remote, phone, pwa
  agyx ollama [args...]            Proxy to 'agyollama' (Local Ollama & Open LLM AI Cockpit)
       Aliases: ai, llm, localai, model
  agyx aws [args...]               Proxy to 'aws' (AWS Cloud Identity, S3 & LocalStack)
       Aliases: cloud, s3, localstack

Examples:
  agyx switch                      Launch interactive Antigravity switcher
  agyx proj ls                     List registered workspaces
  agyx git worktree add agent/test Create isolated AI agent worktree
  agyx docker ram                  Display real-time WSL2 RAM & Swap headroom
  agyx term theme neko             Switch shell prompt theme to 'neko'
  agyx term font Ubuntu "Hack Nerd Font" 13 Set Windows Terminal font
  agyx mobile                      Launch mobile cockpit station
  agyx mobile serve                Start mobile web dashboard on port 7890
  agyx ollama status               Check local Ollama daemon status
  agyx ollama run qwen2.5-coder:7b Launch interactive local AI pair programming`)
}

func printSuiteStatus() {
	fmt.Println("\n⚡ \033[1;36mAGYX SUITE LIVE STATUS\033[0m")
	fmt.Println("──────────────────────────────────────────────────────────────────────────────────")
	tools := proxy.GetRegisteredTools()
	for _, t := range tools {
		binPath, err := proxy.FindBinary(t.BinaryName)
		statusStr := "\033[32m[Installed]\033[0m"
		if err != nil {
			statusStr = "\033[31m[Not Found]\033[0m"
		}
		fmt.Printf("  • \033[1m%-10s\033[0m %-12s %s  \033[37m(%s)\033[0m\n",
			t.Name, statusStr, t.Description, binPath)
	}
	fmt.Println("──────────────────────────────────────────────────────────────────────────────────")
}

func printShellInit() {
	target := ""
	if len(os.Args) > 2 {
		target = strings.ToLower(strings.TrimSpace(os.Args[2]))
	}

	switch target {
	case "zsh", "bash", "sh":
		fmt.Print(shellgen.GenerateZsh())
	case "powershell", "pwsh", "ps1":
		fmt.Print(shellgen.GeneratePowerShell())
	default:
		fmt.Print(`# === AGYX DEVELOPER SUITE — SHELL INTEGRATION ===
#
# To load all aliases, suite binaries, and workspace helpers dynamically:
#
# 1. For Linux (Zsh):
#    eval "$(agyx init zsh)"
#
# 2. For Linux (Bash):
#    eval "$(agyx init bash)"
#
# 3. For Windows (PowerShell):
#    Invoke-Expression (& (Join-Path $HOME ".local\bin\agyx.exe") init powershell)
`)
	}
}
