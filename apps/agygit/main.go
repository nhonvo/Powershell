package main

import (
	"bufio"
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"agygit/internal/service/gitops"
	"agygit/internal/view"
)

func main() {
	rootDir := os.Getenv("AGYGIT_ROOT")
	if rootDir == "" {
		home, _ := os.UserHomeDir()
		rootDir = filepath.Join(home, "projects")
	}

	if len(os.Args) == 1 {
		app := view.NewApp(rootDir)
		if err := app.RunInteractive(); err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		return
	}

	cmd := os.Args[1]
	switch cmd {
	case "ls", "list", "fleet":
		app := view.NewApp(rootDir)
		app.PrintStatus(os.Stdout)

	case "graph":
		graph, err := gitops.GetLogGraph(".", 30)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Println(graph)

	case "log":
		commits, err := gitops.GetLog(".", 10)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("📜 Recent Commits (%d):\n", len(commits))
		for _, c := range commits {
			fmt.Printf("  • \033[33m%s\033[0m \033[1m%-36s\033[0m (%s, %s)\n",
				c.Hash, c.Message, c.Author, c.RelativeTime)
		}

	case "commit":
		var msg string
		if len(os.Args) > 2 {
			msg = strings.Join(os.Args[2:], " ")
		} else {
			fmt.Print("Enter commit message: ")
			scanner := bufio.NewScanner(os.Stdin)
			if scanner.Scan() {
				msg = scanner.Text()
			}
		}
		if strings.TrimSpace(msg) == "" {
			fmt.Fprintln(os.Stderr, "Error: commit message cannot be empty")
			os.Exit(1)
		}
		if err := gitops.Commit(".", msg, true); err != nil {
			fmt.Fprintf(os.Stderr, "Commit error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Committed: %s\n", msg)

	case "merge":
		if len(os.Args) < 3 {
			fmt.Println("Usage: agygit merge <branch-name> [--squash]")
			os.Exit(1)
		}
		branch := os.Args[2]
		squash := false
		if len(os.Args) > 3 && os.Args[3] == "--squash" {
			squash = true
		}
		if err := gitops.Merge(".", branch, squash); err != nil {
			fmt.Fprintf(os.Stderr, "Merge error: %v\n", err)
			os.Exit(1)
		}
		if squash {
			fmt.Printf("✔ Squash merged branch '%s' (staged, ready to commit)\n", branch)
		} else {
			fmt.Printf("✔ Merged branch '%s'\n", branch)
		}

	case "squash-merge":
		if len(os.Args) < 3 {
			fmt.Println("Usage: agygit squash-merge <branch-name>")
			os.Exit(1)
		}
		branch := os.Args[2]
		if err := gitops.Merge(".", branch, true); err != nil {
			fmt.Fprintf(os.Stderr, "Squash merge error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Squash merged branch '%s' (staged, ready to commit)\n", branch)

	case "rebase":
		if len(os.Args) < 3 {
			fmt.Println("Usage: agygit rebase <upstream-branch>")
			os.Exit(1)
		}
		branch := os.Args[2]
		if err := gitops.Rebase(".", branch); err != nil {
			fmt.Fprintf(os.Stderr, "Rebase error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Rebased current branch onto '%s'\n", branch)

	case "cherry-pick":
		if len(os.Args) < 3 {
			fmt.Println("Usage: agygit cherry-pick <commit-hash>")
			os.Exit(1)
		}
		hash := os.Args[2]
		if err := gitops.CherryPick(".", hash); err != nil {
			fmt.Fprintf(os.Stderr, "Cherry-pick error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Cherry-picked commit %s\n", hash)

	case "stash":
		sub := "save"
		if len(os.Args) > 2 {
			sub = os.Args[2]
		}
		if sub == "pop" {
			if err := gitops.StashPop("."); err != nil {
				fmt.Fprintf(os.Stderr, "Stash pop error: %v\n", err)
				os.Exit(1)
			}
			fmt.Println("✔ Popped latest stash")
		} else {
			msg := "agygit stash"
			if len(os.Args) > 3 {
				msg = strings.Join(os.Args[3:], " ")
			}
			if err := gitops.StashSave(".", msg); err != nil {
				fmt.Fprintf(os.Stderr, "Stash error: %v\n", err)
				os.Exit(1)
			}
			fmt.Printf("✔ Stashed changes: %s\n", msg)
		}

	case "branch":
		branches, active, err := gitops.ListBranches(".")
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("🌿 Branches (Active: %s):\n", active)
		for _, b := range branches {
			marker := "  "
			if b == active {
				marker = "* "
			}
			fmt.Printf("  %s%s\n", marker, b)
		}

	case "worktree", "wt":
		if len(os.Args) < 3 {
			printWorktreeHelp()
			return
		}
		sub := os.Args[2]
		switch sub {
		case "ls", "list":
			target := "."
			if len(os.Args) > 3 {
				target = os.Args[3]
			}
			wts, err := gitops.ListWorktrees(target)
			if err != nil {
				fmt.Fprintf(os.Stderr, "Error: %v\n", err)
				os.Exit(1)
			}
			fmt.Printf("🌿 Worktrees for %s (%d total):\n", target, len(wts))
			for i, wt := range wts {
				tag := "[Main]"
				if !wt.IsMain {
					tag = "[Agent]"
				}
				fmt.Printf("  %d. %-8s %-20s -> %s\n", i+1, tag, "("+wt.Branch+")", wt.Path)
			}

		case "add":
			if len(os.Args) < 4 {
				fmt.Println("Usage: agygit worktree add <branch> [custom-path]")
				os.Exit(1)
			}
			branch := os.Args[3]
			customPath := ""
			if len(os.Args) > 4 {
				customPath = os.Args[4]
			}
			wt, err := gitops.AddWorktree(".", branch, customPath)
			if err != nil {
				fmt.Fprintf(os.Stderr, "Error adding worktree: %v\n", err)
				os.Exit(1)
			}
			fmt.Printf("✔ Successfully created agent worktree: %s\n", wt.Path)

		case "rm", "remove":
			if len(os.Args) < 4 {
				fmt.Println("Usage: agygit worktree rm <path>")
				os.Exit(1)
			}
			wtPath := os.Args[3]
			if err := gitops.RemoveWorktree(".", wtPath, true); err != nil {
				fmt.Fprintf(os.Stderr, "Error removing worktree: %v\n", err)
				os.Exit(1)
			}
			fmt.Printf("✔ Removed worktree: %s\n", wtPath)

		default:
			printWorktreeHelp()
		}

	case "fetch":
		if err := gitops.Fetch("."); err != nil {
			fmt.Fprintf(os.Stderr, "Fetch error: %v\n", err)
			os.Exit(1)
		}
		fmt.Println("✔ Fetched changes from remote")

	case "pull":
		if err := gitops.Pull("."); err != nil {
			fmt.Fprintf(os.Stderr, "Pull error: %v\n", err)
			os.Exit(1)
		}
		fmt.Println("✔ Pulled changes successfully")

	case "push":
		if err := gitops.Push("."); err != nil {
			fmt.Fprintf(os.Stderr, "Push error: %v\n", err)
			os.Exit(1)
		}
		fmt.Println("✔ Pushed commits to remote")

	case "status":
		st, err := gitops.GetRepoStatus(".")
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("📦 Repository: %s\n", st.Name)
		fmt.Printf("   Branch:   %s\n", st.CurrentBranch)
		fmt.Printf("   Clean:    %v\n", st.IsClean)
		fmt.Printf("   Changes:  +%d staged, ~%d dirty, ?%d untracked\n", st.StagedFiles, st.DirtyFiles, st.UntrackedFiles)
		fmt.Printf("   Sync:     ↑%d ahead, ↓%d behind\n", st.Ahead, st.Behind)

	case "help", "-h", "--help":
		printHelp()

	default:
		fmt.Fprintf(os.Stderr, "Unknown command '%s'. Run 'agygit help' for usage.\n", cmd)
		os.Exit(1)
	}
}

func printHelp() {
	fmt.Println(`🐙 AGYGIT - Advanced Git Cockpit & Multi-Agent Hub (Go Engine)

Usage:
  agygit                           Launch interactive Git Cockpit TUI
  agygit graph                     Display visual ASCII git log graph with colors
  agygit log                       Show recent commits
  agygit commit [msg]              Commit changes (stages all if untracked)
  agygit push                      Push commits to remote
  agygit pull                      Pull fast-forward changes
  agygit merge <branch> [--squash] Merge branch into current branch
  agygit squash-merge <branch>     Squash merge branch into current branch
  agygit rebase <branch>           Rebase current branch onto target branch
  agygit cherry-pick <hash>        Cherry-pick a commit by hash
  agygit stash [save|pop]          Stash changes or pop stash
  agygit branch                    List branches
  agygit worktree <ls|add|rm>      Manage isolated multi-agent worktrees
  agygit ls                        List all repositories in Git fleet
  agygit help                      Show this help message`)
}

func printWorktreeHelp() {
	fmt.Println(`Usage: agygit worktree <ls|add|rm> [args]
  ls [repo]             List worktrees
  add <branch> [path]   Create new isolated agent worktree
  rm <path>             Remove worktree`)
}
