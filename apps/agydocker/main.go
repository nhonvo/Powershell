package main

import (
	"fmt"
	"os"

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

	case "help", "-h", "--help":
		printHelp()

	default:
		fmt.Fprintf(os.Stderr, "Unknown command '%s'. Run 'agydocker help' for usage.\n", cmd)
		os.Exit(1)
	}
}

func printHelp() {
	fmt.Println(`🐳 AGYDOCKER - Container & WSL2 RAM Manager (Go Engine)

Usage:
  agydocker                        Launch interactive keyboard-only TUI
  agydocker ls                     List all Docker containers with status
  agydocker ram                    Display WSL2 RAM and Swap metrics
  agydocker prune                  Run 'docker system prune -f' to reclaim RAM/disk
  agydocker start <id>             Start a container
  agydocker stop <id>              Stop a container
  agydocker restart <id>           Restart a container
  agydocker logs <id>              View recent logs for a container
  agydocker help                   Show this help message`)
}
