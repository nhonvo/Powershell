package main

import (
	"fmt"
	"os"
	"os/exec"

	"agydocker/internal/model"
	"agydocker/internal/service/devtools"
	"agydocker/internal/service/dockerops"
	"agydocker/internal/view"
)

func main() {
	if len(os.Args) == 1 {
		app := view.NewApp()
		if err := app.RunInteractive(); err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		return
	}

	cmd := os.Args[1]
	switch cmd {
	case "ls", "list":
		app := view.NewApp()
		app.PrintStatus(os.Stdout)

	case "ram", "mem":
		mem, err := dockerops.GetMemoryInfo()
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		totalGB := float64(mem.TotalKB) / (1024.0 * 1024.0)
		usedGB := float64(mem.UsedKB) / (1024.0 * 1024.0)
		availGB := float64(mem.AvailableKB) / (1024.0 * 1024.0)
		fmt.Printf("🧠 WSL2 RAM: %.2f / %.2f GB (%.1f%% used, %.2f GB available)\n",
			usedGB, totalGB, mem.UsedPercent, availGB)
		if mem.SwapTotalKB > 0 {
			swapTotalGB := float64(mem.SwapTotalKB) / (1024.0 * 1024.0)
			swapUsedGB := float64(mem.SwapUsedKB) / (1024.0 * 1024.0)
			fmt.Printf("   Swap:     %.2f / %.2f GB (%.1f%% used)\n",
				swapUsedGB, swapTotalGB, mem.SwapUsedPercent)
		}

	case "prune":
		fmt.Println("🧹 Pruning unused Docker containers, networks, and images...")
		out, err := dockerops.PruneSystem()
		if err != nil {
			fmt.Fprintf(os.Stderr, "Prune error: %v\n", err)
			os.Exit(1)
		}
		fmt.Println(out)

	case "start":
		if len(os.Args) < 3 {
			fmt.Println("Usage: agydocker start <container-id>")
			os.Exit(1)
		}
		if err := dockerops.StartContainer(os.Args[2]); err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Started %s\n", os.Args[2])

	case "stop":
		if len(os.Args) < 3 {
			fmt.Println("Usage: agydocker stop <container-id>")
			os.Exit(1)
		}
		if err := dockerops.StopContainer(os.Args[2]); err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Stopped %s\n", os.Args[2])

	case "restart":
		if len(os.Args) < 3 {
			fmt.Println("Usage: agydocker restart <container-id>")
			os.Exit(1)
		}
		if err := dockerops.RestartContainer(os.Args[2]); err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Restarted %s\n", os.Args[2])

	case "kill":
		if len(os.Args) < 3 {
			fmt.Println("Usage: agydocker kill <container-id>")
			os.Exit(1)
		}
		if err := dockerops.KillContainer(os.Args[2]); err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Killed %s\n", os.Args[2])

	case "rm", "delete":
		if len(os.Args) < 3 {
			fmt.Println("Usage: agydocker rm <container-id>")
			os.Exit(1)
		}
		if err := dockerops.RemoveContainer(os.Args[2]); err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Removed %s\n", os.Args[2])

	case "logs":
		if len(os.Args) < 3 {
			fmt.Println("Usage: agydocker logs <container-id>")
			os.Exit(1)
		}
		logs, err := dockerops.GetContainerLogs(os.Args[2], 50)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Println(logs)

	case "down":
		if len(os.Args) < 3 {
			fmt.Println("Usage: agydocker down <project-or-id>")
			os.Exit(1)
		}
		target := os.Args[2]
		// Try compose down first
		err := dockerops.DownCompose(target)
		if err == nil {
			fmt.Printf("✔ Downed compose project '%s'\n", target)
		} else {
			// Fallback to container down
			if err2 := dockerops.DownContainer(target); err2 == nil {
				fmt.Printf("✔ Downed container '%s'\n", target)
			} else {
				fmt.Fprintf(os.Stderr, "Error downing '%s': compose down failed (%v), container down failed (%v)\n", target, err, err2)
				os.Exit(1)
			}
		}

	case "tools", "devtools":
		runDevToolsCmd(os.Args[2:])

	case "help", "-h", "--help":
		printHelp()

	default:
		fmt.Fprintf(os.Stderr, "Unknown command '%s'. Run 'agydocker help' for usage.\n", cmd)
		os.Exit(1)
	}
}

func runDevToolsCmd(args []string) {
	if len(args) == 0 || args[0] == "status" {
		status, err := devtools.GetStackStatus()
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("\n🛠️  AGYDOCKER Centralized Developer GUI Tools Stack\n")
		fmt.Printf("─────────────────────────────────────────────────────────────────────────────\n")
		fmt.Printf(" Stack Root:     %s\n", status.RootDir)
		if status.PgAdminRunning {
			fmt.Printf(" pgAdmin 4:      \033[1;32m🟢 Running\033[0m (%s)\n", status.PgAdminURL)
		} else {
			fmt.Printf(" pgAdmin 4:      \033[90m⚪ Stopped\033[0m\n")
		}
		if status.MongoRunning {
			fmt.Printf(" Mongo Express:  \033[1;32m🟢 Running\033[0m (%s)\n", status.MongoURL)
		} else {
			fmt.Printf(" Mongo Express:  \033[90m⚪ Stopped\033[0m\n")
		}

		fmt.Printf("\n 📁 Registered Database Connections (%d total):\n", len(status.RegisteredServers))
		for i, s := range status.RegisteredServers {
			grp := s.Group
			if grp == "" {
				grp = "General"
			}
			fmt.Printf("  %2d. [%-20s] %-28s (host: %s:%d, db: %s, user: %s)\n",
				i+1, grp, s.Name, s.Host, s.Port, s.MaintenanceDB, s.Username)
		}
		fmt.Printf("─────────────────────────────────────────────────────────────────────────────\n\n")
		return
	}

	sub := args[0]
	switch sub {
	case "up", "start":
		fmt.Println("🚀 Starting centralized dev-tools stack...")
		out, err := devtools.StartStack()
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Println(out)
		fmt.Println("✔ pgAdmin 4:     http://localhost:5050 (admin@devtools.com / admin)")
		fmt.Println("✔ Mongo Express: http://localhost:8082")

	case "down", "stop":
		fmt.Println("🛑 Stopping centralized dev-tools stack...")
		out, err := devtools.StopStack()
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Println(out)
		fmt.Println("✔ Centralized dev-tools stack stopped.")

	case "restart":
		fmt.Println("🔄 Restarting centralized dev-tools stack...")
		out, err := devtools.RestartStack()
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Println(out)
		fmt.Println("✔ Centralized dev-tools restarted.")

	case "attach", "add":
		target := "."
		if len(args) > 1 {
			target = args[1]
		}
		fmt.Printf("🔍 Auto-detecting database in '%s'...\n", target)
		dbTarget, err := devtools.DetectProjectDB(target)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Detection error: %v\n", err)
			os.Exit(1)
		}

		devDir := devtools.GetDevToolsDir()
		if devDir == "" {
			fmt.Fprintln(os.Stderr, "Error: dev-tools stack directory not found")
			os.Exit(1)
		}

		// Connect network
		if err := devtools.ConnectNetwork(dbTarget.NetworkName); err != nil {
			fmt.Printf("⚠️ Network warning: %v\n", err)
		}
		_ = devtools.EnsureNetworkInCompose(devDir, dbTarget.NetworkName)

		entry := model.DevToolsServerEntry{
			Name:          fmt.Sprintf("%s (%s)", dbTarget.ProjectName, dbTarget.DatabaseName),
			Group:         dbTarget.ProjectName,
			Host:          dbTarget.Host,
			Port:          dbTarget.Port,
			MaintenanceDB: dbTarget.DatabaseName,
			Username:      dbTarget.Username,
			SSLMode:       "prefer",
			PassFile:      "/pgpassfile",
		}

		if err := devtools.RegisterServer(devDir, entry, dbTarget.Password); err != nil {
			fmt.Fprintf(os.Stderr, "Error registering server: %v\n", err)
			os.Exit(1)
		}
		_ = devtools.ReloadPgAdminServers()

		fmt.Printf("✔ Successfully registered '%s' into pgAdmin!\n", entry.Name)
		fmt.Printf("  • Host: %s:%d (Network: %s)\n", entry.Host, entry.Port, dbTarget.NetworkName)
		fmt.Printf("  • Database: %s, User: %s\n", entry.MaintenanceDB, entry.Username)
		fmt.Printf("  • Open in pgAdmin: http://localhost:5050\n")

	case "detach", "rm", "remove":
		if len(args) < 2 {
			fmt.Println("Usage: agydocker tools detach <server-name-or-id>")
			os.Exit(1)
		}
		target := args[1]
		devDir := devtools.GetDevToolsDir()
		removed, err := devtools.RemoveServer(devDir, target)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		if !removed {
			fmt.Printf("⚠️ Server '%s' not found in pgAdmin registry\n", target)
		} else {
			_ = devtools.ReloadPgAdminServers()
			fmt.Printf("✔ Removed '%s' from pgAdmin registry\n", target)
		}

	case "sync":
		devDir := devtools.GetDevToolsDir()
		fmt.Println("🔄 Scanning all running database containers across WSL2...")
		count, err := devtools.SyncAllRunningDatabases(devDir)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Sync error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Synced %d active database containers into pgAdmin!\n", count)

	case "open":
		target := "pgadmin"
		if len(args) > 1 {
			target = args[1]
		}
		url := "http://localhost:5050"
		if target == "mongo" || target == "mongo_express" {
			url = "http://localhost:8082"
		}
		openBrowser(url)
		fmt.Printf("✔ Opened %s in default browser: %s\n", target, url)

	case "config":
		if len(args) > 1 {
			path := args[1]
			if err := devtools.SetDevToolsDir(path); err != nil {
				fmt.Fprintf(os.Stderr, "Error saving config: %v\n", err)
				os.Exit(1)
			}
			fmt.Printf("✔ Saved dev-tools directory: %s\n", path)
		} else {
			fmt.Printf("Dev-tools stack path: %s\n", devtools.GetDevToolsDir())
		}

	default:
		fmt.Printf("Unknown tools command: %s. Run 'agydocker help' for usage.\n", sub)
		os.Exit(1)
	}
}

func openBrowser(url string) {
	if _, err := exec.LookPath("wslview"); err == nil {
		_ = exec.Command("wslview", url).Start()
		return
	}
	if _, err := exec.LookPath("cmd.exe"); err == nil {
		_ = exec.Command("cmd.exe", "/c", "start", url).Start()
		return
	}
	_ = exec.Command("xdg-open", url).Start()
}

func printHelp() {
	fmt.Println(`🐳 AGYDOCKER - Container & WSL2 RAM Manager (Go Engine)

Usage:
  agydocker                        Launch interactive keyboard-only TUI (Tabs 1-4)
  agydocker ls                     List all Docker containers with status
  agydocker ram                    Display WSL2 RAM and Swap metrics
  agydocker prune                  Run 'docker system prune -f' to reclaim RAM/disk
  agydocker start <id>             Start a container
  agydocker stop <id>              Stop a container
  agydocker restart <id>           Restart a container
  agydocker kill <id>              Send SIGKILL to a container
  agydocker rm <id>                Force remove a container (docker rm -f)
  agydocker down <project-or-id>   Down compose project stack or stop+rm container
  agydocker logs <id>              View recent logs for a container

Developer GUI Tools (pgAdmin 4 & Mongo Express):
  agydocker tools                  Show centralized dev-tools status & DB servers
  agydocker tools up               Start shared dev-tools stack (:5050 & :8082)
  agydocker tools down             Stop shared dev-tools stack to reclaim RAM
  agydocker tools attach [path]    Auto-detect project DB, bridge network & register
  agydocker tools detach <name>    Remove database server from pgAdmin registry
  agydocker tools sync             Auto-scan all running DBs and link into pgAdmin
  agydocker tools open [pgadmin]   Open pgAdmin or Mongo Express in browser
  agydocker tools config [dir]     Show or configure central dev-tools directory
  agydocker help                   Show this help message`)
}
