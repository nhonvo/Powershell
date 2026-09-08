package main

import (
	"fmt"
	"os"
	"strconv"

	"agymobile/internal/service/hostops"
	"agymobile/internal/view"
	"agymobile/internal/web"
)

func main() {
	args := os.Args[1:]

	if len(args) > 0 {
		switch args[0] {
		case "status", "ls":
			app := view.NewApp()
			app.PrintStatus(os.Stdout)
			return
		case "serve", "web":
			port := 7890
			if len(args) > 1 {
				if p, err := strconv.Atoi(args[1]); err == nil && p > 0 {
					port = p
				}
			}
			if err := web.StartServer(port); err != nil {
				fmt.Fprintf(os.Stderr, "Error starting web server: %v\n", err)
				os.Exit(1)
			}
			return
		case "flush", "drop-cache":
			fmt.Println("Reclaiming WSL2 RAM buffers & cache...")
			if err := hostops.DropCaches(); err != nil {
				fmt.Fprintf(os.Stderr, "Error flushing RAM: %v\n", err)
				os.Exit(1)
			}
			fmt.Println("✔ Reclaimed Linux/WSL2 buffer memory successfully.")
			return
		case "help", "-h", "--help":
			printHelp()
			return
		}
	}

	// Default: interactive mobile TUI
	app := view.NewApp()
	if err := app.RunInteractive(); err != nil {
		fmt.Fprintf(os.Stderr, "Error: %v\n", err)
		os.Exit(1)
	}
}

func printHelp() {
	fmt.Println(`📱 AGYMOBILE - Mobile Cockpit & Remote Station (Go Engine)

Usage:
  agymobile               Launch interactive 38-column mobile TUI
  agymobile status        Print mobile-optimized summary metrics
  agymobile serve [port]  Start embedded Mobile Web PWA (default port 7890)
  agymobile flush         Flush WSL2/Linux buffer RAM cache
  agymobile help          Display this help message

Designed for Tailscale & SSH mobile development.`)
}
