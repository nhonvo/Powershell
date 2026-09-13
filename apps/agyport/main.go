package main

import (
	"fmt"
	"os"
	"strconv"
	"strings"

	"agyport/internal/model"
	"agyport/internal/service/portops"
	"agyport/internal/service/ramops"
	"agyport/internal/view"
	"agyport/internal/web"
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

	cmd := strings.ToLower(os.Args[1])
	args := os.Args[2:]

	switch cmd {
	case "ls", "list", "ps":
		handleList(args)

	case "check", "find", "get":
		handleCheck(args)

	case "kill":
		handleKill(args)

	case "kill-all", "killall":
		handleKillAll(args)

	case "kill-pid", "killpid":
		handleKillPID(args)

	case "ram", "mem", "memory":
		handleRAM(args)

	case "top", "top-mem":
		handleTop(args)

	case "suggest", "suggestions":
		handleSuggestions(args)

	case "reclaim", "clean", "clean-dev":
		handleReclaim(args)

	case "ui", "web", "dashboard":
		handleWebUI(args)

	case "help", "-h", "--help":
		printHelp()

	default:
		// If user typed a number directly (e.g. `agyport 3000` or `agyport 8080`), treat as check
		if port, err := strconv.Atoi(cmd); err == nil && port > 0 {
			p, err := portops.FindPort(port)
			if err != nil {
				fmt.Printf("✔ Port %d is FREE (no active listener)\n", port)
				return
			}
			printPortDetail(p)
			return
		}

		fmt.Fprintf(os.Stderr, "Unknown command '%s'. Run 'agyport help' for usage.\n", cmd)
		os.Exit(1)
	}
}

func handleList(args []string) {
	asJSON := hasFlag(args, "--json")
	ports, err := portops.ListPorts()
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error listing ports: %v\n", err)
		os.Exit(1)
	}

	if asJSON {
		_ = view.PrintJSON(os.Stdout, ports)
		return
	}

	app := view.NewApp()
	app.PrintStatus(os.Stdout)
}

func handleCheck(args []string) {
	if len(args) == 0 {
		fmt.Println("Usage: agyport check <port>")
		os.Exit(1)
	}

	port, err := strconv.Atoi(args[0])
	if err != nil {
		fmt.Fprintf(os.Stderr, "Invalid port number '%s'\n", args[0])
		os.Exit(1)
	}

	p, err := portops.FindPort(port)
	if err != nil {
		fmt.Printf("✔ Port %d is FREE (not currently in use)\n", port)
		return
	}

	if hasFlag(args, "--json") {
		_ = view.PrintJSON(os.Stdout, p)
		return
	}

	printPortDetail(p)
}

func handleKill(args []string) {
	if len(args) == 0 {
		fmt.Println("Usage: agyport kill <port> [--force, -f]")
		os.Exit(1)
	}

	force := hasFlag(args, "--force") || hasFlag(args, "-f")

	// Parse comma-separated ports or single port
	portStr := args[0]
	parts := strings.Split(portStr, ",")
	for _, pStr := range parts {
		port, err := strconv.Atoi(strings.TrimSpace(pStr))
		if err != nil {
			fmt.Fprintf(os.Stderr, "Invalid port '%s'\n", pStr)
			continue
		}

		res, err := portops.KillPort(port, force)
		if err != nil {
			fmt.Fprintf(os.Stderr, "✖ Failed to terminate port %d: %v\n", port, err)
		} else if res.Success {
			fmt.Printf("✔ Terminated %s on port %d (PID %d)\n", res.ProcessName, res.Port, res.PID)
		}
	}
}

func handleKillAll(args []string) {
	force := hasFlag(args, "--force") || hasFlag(args, "-f")
	results, err := portops.KillAllDevPorts(force)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error killing dev ports: %v\n", err)
		os.Exit(1)
	}

	killed := 0
	for _, r := range results {
		if r.Success {
			killed++
			fmt.Printf("✔ Terminated %s on port %d (PID %d)\n", r.ProcessName, r.Port, r.PID)
		} else {
			fmt.Printf("✖ Failed to kill port %d: %s\n", r.Port, r.Error)
		}
	}
	fmt.Printf("\nDone. Terminated %d developer port processes.\n", killed)
}

func handleKillPID(args []string) {
	if len(args) == 0 {
		fmt.Println("Usage: agyport kill-pid <pid> [--force, -f]")
		os.Exit(1)
	}
	pid, err := strconv.Atoi(args[0])
	if err != nil {
		fmt.Fprintf(os.Stderr, "Invalid PID '%s'\n", args[0])
		os.Exit(1)
	}

	force := hasFlag(args, "--force") || hasFlag(args, "-f")
	if err := portops.KillPID(pid, force); err != nil {
		fmt.Fprintf(os.Stderr, "✖ Failed to kill PID %d: %v\n", pid, err)
		os.Exit(1)
	}
	fmt.Printf("✔ Successfully terminated process PID %d\n", pid)
}

func handleRAM(args []string) {
	mem, err := ramops.GetMemorySummary()
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error retrieving memory summary: %v\n", err)
		os.Exit(1)
	}

	if hasFlag(args, "--json") {
		_ = view.PrintJSON(os.Stdout, mem)
		return
	}

	fmt.Printf("🧠 Host & WSL2 Memory Status:\n")
	fmt.Printf("   Total:     %s\n", mem.TotalFormatted)
	fmt.Printf("   Used:      %s (%.1f%%)\n", mem.UsedFormatted, mem.UsedPercent)
	fmt.Printf("   Available: %s\n", mem.AvailableFormatted)
	fmt.Printf("   Cached:    %s\n", mem.CachedFormatted)
	if mem.SwapTotalBytes > 0 {
		fmt.Printf("   Swap:      %s / %s (%.1f%%)\n",
			mem.SwapUsedFormatted, model.FormatBytes(mem.SwapTotalBytes), mem.SwapUsedPercent)
	}
}

func handleTop(args []string) {
	limit := 10
	if len(args) > 0 {
		if l, err := strconv.Atoi(args[0]); err == nil && l > 0 {
			limit = l
		}
	}

	procs, err := ramops.GetTopMemoryProcesses(limit)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error retrieving top processes: %v\n", err)
		os.Exit(1)
	}

	if hasFlag(args, "--json") {
		_ = view.PrintJSON(os.Stdout, procs)
		return
	}

	fmt.Printf("🔥 Top %d Memory-Consuming Processes:\n", len(procs))
	fmt.Printf("%-7s %-18s %-12s %-10s %-12s %-20s\n",
		"PID", "PROCESS", "RAM (RSS)", "DEV SERVER", "PORTS", "USER")
	fmt.Println(strings.Repeat("─", 80))
	for _, p := range procs {
		devStr := "No"
		if p.IsDevServer {
			devStr = fmt.Sprintf("Yes (%s)", p.Framework)
		}
		var portsStr []string
		for _, port := range p.Ports {
			portsStr = append(portsStr, strconv.Itoa(port))
		}
		fmt.Printf("%-7d %-18s %-12s %-10s %-12s %-20s\n",
			p.PID, p.Name, p.RSSFormatted, devStr, strings.Join(portsStr, ","), p.User)
	}
}

func handleSuggestions(args []string) {
	ports, _ := portops.ListPorts()
	mem, _ := ramops.GetMemorySummary()
	procs, _ := ramops.GetTopMemoryProcesses(25)
	suggestions := ramops.GenerateRAMSuggestions(ports, procs, mem)

	if hasFlag(args, "--json") {
		_ = view.PrintJSON(os.Stdout, suggestions)
		return
	}

	fmt.Println("💡 Smart RAM Leverage Suggestions:")
	if len(suggestions) == 0 {
		fmt.Println("  ✔ System RAM usage is optimal. No idle dev servers or memory hogs detected.")
		return
	}

	for i, s := range suggestions {
		fmt.Printf("\n[%d] %s (+%s reclaimable)\n", i+1, s.Title, s.ReclaimableFormatted)
		fmt.Printf("    %s\n", s.Description)
		fmt.Printf("    Action: %s\n", s.SuggestedCommand)
	}
}

func handleReclaim(args []string) {
	dryRun := hasFlag(args, "--dry-run")
	res, err := ramops.ReclaimDevRAM(dryRun)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Reclaim error: %v\n", err)
		os.Exit(1)
	}

	if dryRun {
		fmt.Printf("🔍 Reclaim Dry Run: Found %d dev processes holding %s RAM:\n",
			len(res.KilledProcesses), res.FreedFormatted)
		for _, proc := range res.KilledProcesses {
			fmt.Printf("   • %s\n", proc)
		}
		fmt.Println("\nRun 'agyport reclaim' to terminate these processes.")
		return
	}

	fmt.Printf("✔ Reclaimed %s RAM across %d developer processes:\n",
		res.FreedFormatted, len(res.KilledProcesses))
	for _, proc := range res.KilledProcesses {
		fmt.Printf("   • %s\n", proc)
	}
}

func printPortDetail(p *model.PortInfo) {
	fmt.Printf("🌐 Port %d Details:\n", p.Port)
	fmt.Printf("   Protocol:    %s (%s)\n", strings.ToUpper(p.Protocol), p.State)
	fmt.Printf("   Bind:        %s\n", p.BindAddress)
	fmt.Printf("   PID:         %d\n", p.PID)
	fmt.Printf("   Process:     %s\n", p.ProcessName)
	fmt.Printf("   Framework:   %s (%s)\n", p.Framework, p.DevCategory)
	if p.MemoryBytes > 0 {
		fmt.Printf("   RAM Usage:   %s (%.2f%% of system RAM)\n", p.MemoryFormatted, p.MemoryPercent)
	}
	fmt.Printf("   User:        %s\n", p.User)
	if p.CommandLine != "" {
		fmt.Printf("   Command:     %s\n", p.CommandLine)
	}
}

func handleWebUI(args []string) {
	port := 5999
	for i, a := range args {
		if (a == "-p" || a == "--port") && i+1 < len(args) {
			if p, err := strconv.Atoi(args[i+1]); err == nil && p > 0 {
				port = p
			}
		}
	}
	noOpen := hasFlag(args, "--no-open")
	if err := web.StartServer(port, !noOpen); err != nil {
		fmt.Fprintf(os.Stderr, "Web server error: %v\n", err)
		os.Exit(1)
	}
}

func hasFlag(args []string, flag string) bool {
	for _, a := range args {
		if strings.EqualFold(a, flag) {
			return true
		}
	}
	return false
}

func printHelp() {
	fmt.Println(`⚡ AGYPORT - Antigravity Port & Memory Manager (Go Engine)

Usage:
  agyport                          Launch interactive full-screen Cockpit TUI
  agyport ui [--port 5999]         Launch modern browser Web UI Dashboard & auto-open
  agyport ls                       List all listening network ports with PID & RAM
  agyport check <port>             Check if a port is in use and inspect its process
  agyport kill <port> [--force]    Terminate process listening on port (supports 3000,8080)
  agyport kill-all [--force]       Terminate all running dev server ports
  agyport kill-pid <pid> [--force] Terminate process by PID directly
  agyport ram                      Show host & WSL2 RAM and Swap summary
  agyport top [limit]              Show highest RAM-consuming processes
  agyport suggestions              Show smart RAM leverage & optimization suggestions
  agyport reclaim [--dry-run]      Kill dangling dev servers to instantly free RAM
  agyport <port>                   Shortcut to check a port (e.g. 'agyport 3000')

TUI Hotkeys:
  [↑/↓] Navigate  [k] Kill Port  [K/f] Force Kill  [a] Kill All Dev Ports
  [c] Reclaim RAM  [/] Search/Filter  [i/Enter] Process Details  [Tab] Switch Tab  [q] Quit`)
}
