package main

import (
	"fmt"
	"os"
	"strings"

	"agyollama/internal/model"
	"agyollama/internal/service/ollamaops"
	"agyollama/internal/view"
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
	case "status":
		app := view.NewApp()
		app.PrintStatus(os.Stdout)

	case "ls", "models", "list":
		app := view.NewApp()
		app.PrintModels(os.Stdout)

	case "pull":
		if len(os.Args) < 3 {
			fmt.Println("Usage: agyollama pull <model-name>")
			os.Exit(1)
		}
		name := os.Args[2]
		fmt.Printf("📦 Pulling model '%s' from Ollama library...\n", name)
		var lastStatus string
		err := ollamaops.PullModel(name, func(p model.PullProgress) {
			if p.TotalBytes > 0 {
				completedGB := float64(p.CompletedBytes) / (1024 * 1024 * 1024)
				totalGB := float64(p.TotalBytes) / (1024 * 1024 * 1024)
				fmt.Printf("\r  %-20s [%.1f%%] (%.2f / %.2f GB)",
					p.Status, p.Percent, completedGB, totalGB)
			} else if p.Status != lastStatus {
				fmt.Printf("\r  %-50s\n", p.Status)
				lastStatus = p.Status
			}
		})
		fmt.Println()
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error pulling model '%s': %v\n", name, err)
			os.Exit(1)
		}
		fmt.Printf("✔ Successfully pulled model '%s'.\n", name)

	case "run":
		targetModel := ""
		if len(os.Args) >= 3 {
			targetModel = os.Args[2]
		} else {
			targetModel = ollamaops.GetDefaultModel()
		}
		if err := ollamaops.RunInteractive(targetModel); err != nil {
			fmt.Fprintf(os.Stderr, "Error running model '%s': %v\n", targetModel, err)
			os.Exit(1)
		}

	case "benchmark", "bench":
		app := view.NewApp()
		app.PrintBenchmark(os.Stdout)

	case "start":
		if ollamaops.IsRunning() {
			fmt.Println("✔ Ollama daemon is already online!")
			return
		}
		fmt.Println("🚀 Starting Ollama daemon in background...")
		if err := ollamaops.StartDaemon(); err != nil {
			fmt.Fprintf(os.Stderr, "Error starting daemon: %v\n", err)
			os.Exit(1)
		}
		fmt.Println("✔ Ollama daemon started successfully.")

	case "stop":
		if !ollamaops.IsRunning() {
			fmt.Println("✔ Ollama daemon is already offline.")
			return
		}
		fmt.Println("🛑 Stopping Ollama daemon...")
		if err := ollamaops.StopDaemon(); err != nil {
			fmt.Fprintf(os.Stderr, "Error stopping daemon: %v\n", err)
			os.Exit(1)
		}
		fmt.Println("✔ Ollama daemon stopped successfully.")

	case "delete", "rm":
		if len(os.Args) < 3 {
			fmt.Println("Usage: agyollama delete <model-name>")
			os.Exit(1)
		}
		name := os.Args[2]
		if err := ollamaops.DeleteModel(name); err != nil {
			fmt.Fprintf(os.Stderr, "Error deleting model '%s': %v\n", name, err)
			os.Exit(1)
		}
		fmt.Printf("✔ Model '%s' deleted successfully.\n", name)

	case "info", "show":
		if len(os.Args) < 3 {
			fmt.Println("Usage: agyollama info <model-name>")
			os.Exit(1)
		}
		name := os.Args[2]
		detail, err := ollamaops.ShowModel(name)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error fetching info for '%s': %v\n", name, err)
			os.Exit(1)
		}
		fmt.Printf("🤖 Model Details: %s\n", name)
		fmt.Println(strings.Repeat("─", 60))
		if detail.Template != "" {
			fmt.Println("\nTemplate:")
			fmt.Println(detail.Template)
		}
		if detail.System != "" {
			fmt.Println("\nSystem Prompt:")
			fmt.Println(detail.System)
		}
		if detail.Parameters != "" {
			fmt.Println("\nParameters:")
			fmt.Println(detail.Parameters)
		}
		if detail.Modelfile != "" {
			fmt.Println("\nModelfile:")
			fmt.Println(detail.Modelfile)
		}

	case "default":
		if len(os.Args) >= 3 {
			name := os.Args[2]
			if err := ollamaops.SetDefaultModel(name); err != nil {
				fmt.Fprintf(os.Stderr, "Error setting default model: %v\n", err)
				os.Exit(1)
			}
			fmt.Printf("✔ Set '%s' as active default model.\n", name)
		} else {
			fmt.Printf("Active default model: %s\n", ollamaops.GetDefaultModel())
		}

	case "logs":
		app := view.NewApp()
		app.PrintLogs(os.Stdout, 50)

	case "help", "-h", "--help":
		printHelp()

	default:
		fmt.Fprintf(os.Stderr, "Unknown command '%s'. Run 'agyollama help' for usage.\n", cmd)
		os.Exit(1)
	}
}

func printHelp() {
	fmt.Println(`🤖 AGYOLLAMA - Local AI & Ollama Engine Manager (Go Edition)

Usage:
  agyollama                        Launch interactive keyboard-only VT100 TUI
  agyollama status                 Check local Ollama daemon status & metrics
  agyollama ls                     List all pulled/installed models
  agyollama models                 Alias for 'ls'
  agyollama pull <name>            Pull model from Ollama library with streaming progress
  agyollama run [name]             Launch interactive chat session (defaults to active model)
  agyollama benchmark              Run latency & tokens/sec benchmark on models
  agyollama start                  Start local Ollama daemon ('ollama serve') in background
  agyollama delete <name>          Delete installed model
  agyollama info <name>            Display full model architecture & modelfile details
  agyollama default [name]         View or configure active default model
  agyollama logs                   View recent daemon server logs
  agyollama help                   Show this help message`)
}
