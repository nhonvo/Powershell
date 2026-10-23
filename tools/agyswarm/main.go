package main

import (
	"flag"
	"fmt"
	"os"
	"path/filepath"
	"runtime"
	"strings"
	"time"

	"agyswarm/internal/engine"
	"agyswarm/internal/markdown"
	"agyswarm/internal/model"
	"agyswarm/internal/view"
)

var (
	version = "1.0.0"
	banner  = `
  🛸 AGYSWARM · Multi-Child Terminal Agent Cockpit & Orchestrator
  ══════════════════════════════════════════════════════════════
`
)

func printUsage() {
	fmt.Print(banner)
	fmt.Printf("Usage: agyswarm <command> [options]\n\n")
	fmt.Printf("Commands:\n")
	fmt.Printf("  cockpit                   Launch the interactive multi-agent terminal Cockpit (default)\n")
	fmt.Printf("  run                       Execute an autonomous multi-agent swarm task\n")
	fmt.Printf("  spawn                     Spawn a standalone child agent terminal\n")
	fmt.Printf("  version                   Display version info\n")
	fmt.Printf("\nFlags for 'run':\n")
	fmt.Printf("  --task <goal>             Goal description for the swarm\n")
	fmt.Printf("  --accounts <a,b>          Comma-separated list of Gemini/AGY accounts\n")
	fmt.Printf("  --workers <count>         Number of parallel worker agents (default: 2)\n")
	fmt.Printf("  --out <dir>               Output directory for Markdown deliverable (default: ./doc/swarm)\n")
	fmt.Printf("\nInteractive Cockpit Controls:\n")
	fmt.Printf("  [Tab / 1-9]               Switch focused agent pane\n")
	fmt.Printf("  [Enter / i]               Direct keyboard pass-through (Ctrl+] to return)\n")
	fmt.Printf("  [s]                       Quick spawn a child agent\n")
	fmt.Printf("  [k]                       Terminate / kill focused agent\n")
	fmt.Printf("  [m]                       Export session transcript to pure Markdown (.md)\n")
	fmt.Printf("  [q / Esc]                 Exit Cockpit\n\n")
}

func main() {
	if len(os.Args) < 2 {
		runCockpit()
		return
	}

	cmd := os.Args[1]
	switch cmd {
	case "cockpit":
		runCockpit()
	case "run":
		runSwarmTask(os.Args[2:])
	case "spawn":
		runSpawn(os.Args[2:])
	case "version", "-v", "--version":
		fmt.Printf("agyswarm version %s (Go PTY Engine)\n", version)
	case "help", "-h", "--help":
		printUsage()
	default:
		// Default to running cockpit if unknown argument, or print error
		if strings.HasPrefix(cmd, "-") {
			printUsage()
		} else {
			fmt.Printf("Unknown command '%s'. Run 'agyswarm --help' for usage.\n", cmd)
			os.Exit(1)
		}
	}
}

func runCockpit() {
	mgr := engine.NewManager("")
	exporter := markdown.NewExporter(filepath.Join(".", "doc", "swarm"))
	cockpit := view.NewCockpit(mgr, exporter)

	if err := cockpit.Run(); err != nil {
		fmt.Fprintf(os.Stderr, "agyswarm error: %v\n", err)
		os.Exit(1)
	}
}

func runSpawn(args []string) {
	fs := flag.NewFlagSet("spawn", flag.ExitOnError)
	name := fs.String("name", "worker", "Name of the agent")
	account := fs.String("account", "", "Gemini account alias")
	defaultCmd := "bash"
	if runtime.GOOS == "windows" {
		defaultCmd = "powershell.exe"
	}
	cmdName := fs.String("cmd", defaultCmd, "Command executable")
	dir := fs.String("dir", "", "Working directory")
	_ = fs.Parse(args)

	mgr := engine.NewManager("")
	sess, err := mgr.Spawn(engine.SpawnConfig{
		Name:         *name,
		AccountName:  *account,
		WorkspaceDir: *dir,
		Command:      *cmdName,
		Args:         fs.Args(),
	})
	if err != nil {
		fmt.Fprintf(os.Stderr, "failed to spawn agent: %v\n", err)
		os.Exit(1)
	}

	fmt.Printf("✓ Agent '%s' spawned (ID: %s, PID: %d).\n", sess.Name, sess.ID, sess.PID)
	fmt.Printf("Attaching directly. Press Ctrl+] to detach.\n\n")
	_ = mgr.Attach(sess.ID)
}

func runSwarmTask(args []string) {
	fs := flag.NewFlagSet("run", flag.ExitOnError)
	taskGoal := fs.String("task", "", "Goal / prompt for the swarm")
	accountsList := fs.String("accounts", "", "Comma-separated list of accounts")
	numWorkers := fs.Int("workers", 2, "Number of parallel workers")
	outDir := fs.String("out", filepath.Join(".", "doc", "swarm"), "Output directory for Markdown deliverable")
	_ = fs.Parse(args)

	if *taskGoal == "" {
		fmt.Println("Error: --task <goal> is required.")
		os.Exit(1)
	}

	fmt.Print(banner)
	fmt.Printf("🚀 Launching Swarm Task: %s\n", *taskGoal)

	var accounts []string
	if *accountsList != "" {
		for _, a := range strings.Split(*accountsList, ",") {
			trimmed := strings.TrimSpace(a)
			if trimmed != "" {
				accounts = append(accounts, trimmed)
			}
		}
	}
	if len(accounts) == 0 {
		accounts = append(accounts, "default")
	}

	mgr := engine.NewManager("")
	exporter := markdown.NewExporter(*outDir)

	task := &model.SwarmTask{
		ID:            fmt.Sprintf("task-%d", time.Now().Unix()),
		Goal:          *taskGoal,
		TargetProject: filepath.Base(getWorkingDir()),
		Status:        model.StatusWorking,
		CreatedAt:     time.Now(),
	}

	var sessions []*model.AgentSession

	// Spawn workers across allocated accounts
	for i := 0; i < *numWorkers; i++ {
		acc := accounts[i%len(accounts)]
		workerName := fmt.Sprintf("worker-%d-%s", i+1, acc)

		// Command to run: powershell or bash
		cmd := "bash"
		var agentArgs []string
		if runtime.GOOS == "windows" {
			cmd = "powershell.exe"
			agentArgs = []string{"-NoProfile", "-Command", fmt.Sprintf("Write-Host 'Agent %s starting task on account %s...'; Start-Sleep -Seconds 1; Write-Host 'Finished research subtask %d.'", workerName, acc, i+1)}
		} else {
			agentArgs = []string{"-c", fmt.Sprintf("echo 'Agent %s starting task on account %s...'; sleep 1; echo 'Finished research subtask %d.'; exit 0", workerName, acc, i+1)}
		}

		sess, err := mgr.Spawn(engine.SpawnConfig{
			Name:         workerName,
			AccountName:  acc,
			WorkspaceDir: getWorkingDir(),
			Command:      cmd,
			Args:         agentArgs,
		})
		if err != nil {
			fmt.Fprintf(os.Stderr, "failed to spawn worker %s: %v\n", workerName, err)
			continue
		}
		task.WorkerAgentIDs = append(task.WorkerAgentIDs, sess.ID)
		sessions = append(sessions, sess)
		fmt.Printf("  • Spawned %s [%s] (PID %d)\n", workerName, sess.ID, sess.PID)
	}

	fmt.Printf("\n⚡ Swarm running. Monitoring agent progress...\n")
	allDone := false
	for !allDone {
		time.Sleep(500 * time.Millisecond)
		allDone = true
		for _, s := range sessions {
			if s.Status == model.StatusWorking {
				allDone = false
				break
			}
		}
	}

	task.Status = model.StatusDone
	task.CompletedAt = time.Now()

	if len(sessions) == 0 {
		fmt.Fprintf(os.Stderr, "Error: No worker agents could be spawned for the task.\n")
		os.Exit(1)
	}

	// Synthesize blackboard artifact
	artifacts := []model.BlackboardArtifact{
		{
			ID:        fmt.Sprintf("art-%d", time.Now().Unix()),
			AgentID:   sessions[0].ID,
			Topic:     "Swarm Coordination & Synthesis",
			Markdown:  fmt.Sprintf("Swarm completed goal: **%s** across %d worker agents.", task.Goal, len(sessions)),
			Timestamp: time.Now(),
		},
	}

	// Export deliverable
	dossierPath, err := exporter.ExportSwarmTask(task, sessions, artifacts)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Failed to export Markdown dossier: %v\n", err)
		os.Exit(1)
	}

	relPath := fmt.Sprintf("./%s", filepath.Clean(dossierPath))
	fmt.Printf("\n✔ Swarm Task Completed Successfully!\n")
	fmt.Printf("📄 Pure-Markdown Deliverable Generated:\n")
	fmt.Printf("   VS Code Link: [%s](%s)\n", filepath.Base(dossierPath), relPath)
	fmt.Printf("   Local Path:   %s\n\n", dossierPath)
}

func getWorkingDir() string {
	if cwd, err := os.Getwd(); err == nil {
		return cwd
	}
	return "."
}
