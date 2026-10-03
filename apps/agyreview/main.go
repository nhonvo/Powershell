package main

import (
	"context"
	"flag"
	"fmt"
	"os"
	"os/signal"
	"syscall"

	"agyreview/internal/engine"
	"agyreview/internal/model"
	"agyreview/internal/remediation"
	"agyreview/internal/resolver"
	"agyreview/internal/roadmap"
	"agyreview/internal/view"
	"agyreview/internal/watcher"
)

const version = "1.0.0"

func main() {
	if len(os.Args) < 2 {
		runCockpit()
		return
	}

	cmd := os.Args[1]
	switch cmd {
	case "cockpit", "tui", "ui":
		runCockpit()
	case "run", "review":
		runReviewCmd(os.Args[2:])
	case "pr":
		runPRCmd(os.Args[2:])
	case "watch":
		runWatchCmd(os.Args[2:])
	case "fix", "remediate":
		runFixCmd(os.Args[2:])
	case "roadmap":
		runRoadmapCmd(os.Args[2:])
	case "version", "-v", "--version":
		fmt.Printf("agyreview v%s (Go 1.22.2 / Antigravity Developer Suite)\n", version)
	case "help", "-h", "--help":
		printHelp()
	default:
		// If argument looks like path or URL, treat as target to review
		if resolver.IsPRURL(cmd) {
			runPRCmd([]string{cmd})
		} else {
			runReviewCmd([]string{cmd})
		}
	}
}

func printHelp() {
	fmt.Printf(`⚡ AGYREVIEW v%s - Autonomous Multi-Repo Code Reviewer & Sentinel

USAGE:
  agyreview [command] [options]

COMMANDS:
  cockpit                 Launch interactive full-screen terminal TUI (default)
  run [target]            Run 3-loop review on local path or GitHub URL
  pr <pr-url>             Review GitHub pull request diff
  watch [start|add|rm|ls] Manage and run background git poller sentinel
  fix [target]            Execute remediation plan and apply verified patches
  roadmap [target]        Generate 3-Horizon Strategic Product Roadmap
  version                 Display application version and engine details

OPTIONS FOR 'run':
  --swarm                 Execute with parallel multi-agent swarm
  --scope [whole|security] Set audit scope (default: whole-codebase)

EXAMPLES:
  agyreview run .
  agyreview run github.com/owner/repo
  agyreview pr https://github.com/owner/repo/pull/42
  agyreview watch add /path/to/repo --mode auto
  agyreview fix --interactive
  agyreview roadmap .
`, version)
}

func runCockpit() {
	cfg, err := model.LoadConfig()
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error loading config: %v\n", err)
	}
	app := view.NewCockpitApp(cfg)
	if err := app.RunInteractive(); err != nil {
		fmt.Fprintf(os.Stderr, "Error running cockpit: %v\n", err)
	}
}

func runReviewCmd(args []string) {
	fs := flag.NewFlagSet("run", flag.ExitOnError)
	swarm := fs.Bool("swarm", false, "Execute with multi-agent swarm")
	scopeFlag := fs.String("scope", "whole-codebase", "Scope: whole-codebase or security-audit")
	_ = fs.Parse(args)

	targetInput := "."
	if fs.NArg() > 0 {
		targetInput = fs.Arg(0)
	}

	scope := model.ScopeWholeCodebase
	if *scopeFlag == "security-audit" || *scopeFlag == "security" {
		scope = model.ScopeSecurity
	}

	var target *model.TargetRepo
	var err error

	if resolver.IsPRURL(targetInput) {
		target, err = resolver.ResolvePR(targetInput)
	} else if resolver.IsGitHubURL(targetInput) {
		target, err = resolver.ResolveGitHub(targetInput, "", scope)
	} else {
		target, err = resolver.ResolveLocal(targetInput, scope)
	}

	if err != nil {
		fmt.Fprintf(os.Stderr, "❌ Failed resolving target: %v\n", err)
		os.Exit(1)
	}

	fmt.Printf("🔍 Launching Code Review on: %s (%s)\n", target.Name, target.Path)
	res, err := engine.RunReview(target, *swarm, func(loop int, name string, count int) {
		fmt.Printf("   [Loop %d] %s (%d findings detected so far)...\n", loop, name, count)
	})

	if err != nil {
		fmt.Fprintf(os.Stderr, "❌ Review failed: %v\n", err)
		os.Exit(1)
	}

	view.DisplayScorecard(res.Target, res.Score, res.Findings, res.ReportPath)
}

func runPRCmd(args []string) {
	if len(args) == 0 {
		fmt.Fprintln(os.Stderr, "Usage: agyreview pr <pr-url>")
		os.Exit(1)
	}

	prURL := args[0]
	target, err := resolver.ResolvePR(prURL)
	if err != nil {
		fmt.Fprintf(os.Stderr, "❌ Failed resolving PR: %v\n", err)
		os.Exit(1)
	}

	fmt.Printf("🐙 Launching PR Audit for: %s (PR #%d)\n", target.Name, target.PRNumber)
	res, err := engine.RunReview(target, false, func(loop int, name string, count int) {
		fmt.Printf("   [Loop %d] %s (%d findings)...\n", loop, name, count)
	})

	if err != nil {
		fmt.Fprintf(os.Stderr, "❌ PR review failed: %v\n", err)
		os.Exit(1)
	}

	view.DisplayScorecard(res.Target, res.Score, res.Findings, res.ReportPath)
}

func runWatchCmd(args []string) {
	cfg, _ := model.LoadConfig()
	if len(args) == 0 || args[0] == "ls" {
		fmt.Printf("👁️ Watched Repositories (%d total):\n", len(cfg.WatchedRepos))
		for i, w := range cfg.WatchedRepos {
			fmt.Printf("  %d. %s [Mode: %s, Last: %s]\n", i+1, w.Path, w.TriggerMode, w.LastCommit)
		}
		return
	}

	sub := args[0]
	switch sub {
	case "add":
		if len(args) < 2 {
			fmt.Fprintln(os.Stderr, "Usage: agyreview watch add <path> [--mode auto|suggest]")
			os.Exit(1)
		}
		path := args[1]
		mode := model.TriggerSuggest
		if len(args) >= 4 && args[2] == "--mode" && args[3] == "auto" {
			mode = model.TriggerAuto
		}
		if err := watcher.AddWatchRepo(cfg, path, mode); err != nil {
			fmt.Fprintf(os.Stderr, "❌ Error adding watch repo: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Added %s to watch list (mode: %s)\n", path, mode)

	case "rm", "remove":
		if len(args) < 2 {
			fmt.Fprintln(os.Stderr, "Usage: agyreview watch rm <path>")
			os.Exit(1)
		}
		path := args[1]
		if err := watcher.RemoveWatchRepo(cfg, path); err != nil {
			fmt.Fprintf(os.Stderr, "❌ Error removing watch repo: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Removed %s from watch list\n", path)

	case "start", "run":
		ctx, cancel := context.WithCancel(context.Background())
		defer cancel()

		sigChan := make(chan os.Signal, 1)
		signal.Notify(sigChan, os.Interrupt, syscall.SIGTERM)
		go func(ctx context.Context) {
			select {
			case <-sigChan:
				fmt.Println("\n⏹ Stopping agyreview watcher sentinel...")
				cancel()
			case <-ctx.Done():
			}
		}(ctx)

		fmt.Printf("👁️ [agyreview Sentinel] Started poller watching %d repos (poll interval: %ds)...\n",
			len(cfg.WatchedRepos), cfg.PollIntervalSec)
		poller := watcher.NewGitPoller(cfg, func(evt watcher.ChangeEvent) {
			_ = watcher.HandleChangeEvent(evt)
		})
		poller.StartPolling(ctx)

	default:
		fmt.Fprintf(os.Stderr, "Unknown watch command: %s\n", sub)
		os.Exit(1)
	}
}

func runFixCmd(args []string) {
	fs := flag.NewFlagSet("fix", flag.ExitOnError)
	autoP0P1 := fs.Bool("auto-p0-p1", false, "Automatically apply P0 and P1 fixes without prompt")
	interactive := fs.Bool("interactive", true, "Interactively confirm each patch")
	branch := fs.String("branch", "", "Create an isolated git branch for fixes")
	tests := fs.Bool("test", true, "Run tests after applying patches")
	_ = fs.Parse(args)

	targetInput := "."
	if fs.NArg() > 0 {
		targetInput = fs.Arg(0)
	}

	target, err := resolver.ResolveLocal(targetInput, model.ScopeWholeCodebase)
	if err != nil {
		fmt.Fprintf(os.Stderr, "❌ Failed resolving target: %v\n", err)
		os.Exit(1)
	}

	findings, _, err := engine.ExecuteThreeLoops(target.Path, nil)
	if err != nil {
		fmt.Fprintf(os.Stderr, "❌ Error evaluating findings: %v\n", err)
		os.Exit(1)
	}

	plan, planPath, err := remediation.GenerateRemediationPlan(target, findings)
	if err != nil {
		fmt.Fprintf(os.Stderr, "❌ Error generating plan: %v\n", err)
		os.Exit(1)
	}
	fmt.Printf("📋 Remediation plan generated at: %s\n", planPath)

	opts := remediation.PatcherOptions{
		AutoP0P1:    *autoP0P1,
		Interactive: *interactive,
		BranchName:  *branch,
		RunTests:    *tests,
	}

	applied, err := remediation.ApplyRemediation(target, plan, opts)
	if err != nil {
		fmt.Fprintf(os.Stderr, "❌ Remediation error: %v\n", err)
		os.Exit(1)
	}
	fmt.Printf("✔ Remediation finished: %d tasks applied.\n", applied)
}

func runRoadmapCmd(args []string) {
	targetInput := "."
	if len(args) > 0 {
		targetInput = args[0]
	}

	target, err := resolver.ResolveLocal(targetInput, model.ScopeWholeCodebase)
	if err != nil {
		fmt.Fprintf(os.Stderr, "❌ Failed resolving target: %v\n", err)
		os.Exit(1)
	}

	findings, score, err := engine.ExecuteThreeLoops(target.Path, nil)
	if err != nil {
		fmt.Fprintf(os.Stderr, "❌ Error analyzing codebase: %v\n", err)
		os.Exit(1)
	}

	roadmapPath, err := roadmap.GenerateProductRoadmap(target, findings, score)
	if err != nil {
		fmt.Fprintf(os.Stderr, "❌ Error generating product roadmap: %v\n", err)
		os.Exit(1)
	}

	fmt.Printf("🗺️ Strategic Product Roadmap generated at: %s\n", roadmapPath)
}
