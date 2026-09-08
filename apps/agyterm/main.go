package main

import (
	"fmt"
	"os"
	"strconv"

	"agyterm/internal/service/theme"
	"agyterm/internal/service/winterm"
	"agyterm/internal/view"
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

	case "theme":
		themesDir := theme.ResolveThemesDir()
		if len(os.Args) == 2 {
			cur := theme.GetSelectedTheme()
			fmt.Printf("🎨 Current Theme: %s\n", cur)
			fmt.Printf("   Themes directory: %s\n", themesDir)
			return
		}
		target := os.Args[2]
		if err := theme.SetTheme(target, themesDir); err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Successfully set and persisted theme '%s'\n", target)

	case "font":
		settingsFile := winterm.FindSettingsFile()
		if settingsFile == "" {
			fmt.Fprintln(os.Stderr, "Error: Windows Terminal settings.json not found")
			os.Exit(1)
		}
		if len(os.Args) < 4 {
			fmt.Println("Usage: agyterm font <profile-name|*> <font-face> [font-size]")
			fmt.Println("Example: agyterm font Ubuntu \"Hack Nerd Font\" 13")
			return
		}
		prof := os.Args[2]
		face := os.Args[3]
		var size float64
		if len(os.Args) > 4 {
			size, _ = strconv.ParseFloat(os.Args[4], 64)
		}
		if err := winterm.UpdateProfileFont(settingsFile, prof, face, size); err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Updated font for profile '%s' -> '%s'\n", prof, face)

	case "opacity":
		settingsFile := winterm.FindSettingsFile()
		if settingsFile == "" {
			fmt.Fprintln(os.Stderr, "Error: Windows Terminal settings.json not found")
			os.Exit(1)
		}
		if len(os.Args) < 4 {
			fmt.Println("Usage: agyterm opacity <profile-name|*> <1-100>")
			return
		}
		prof := os.Args[2]
		op, err := strconv.Atoi(os.Args[3])
		if err != nil || op < 1 || op > 100 {
			fmt.Fprintln(os.Stderr, "Error: Opacity must be between 1 and 100")
			os.Exit(1)
		}
		if err := winterm.UpdateProfileOpacity(settingsFile, prof, op); err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Updated opacity for profile '%s' -> %d%%\n", prof, op)

	case "fonts":
		fonts, err := winterm.ListAvailableFonts()
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("🔤 Available Fonts (%d total):\n", len(fonts))
		for _, f := range fonts {
			tag := "[Nerd]"
			if !f.IsNerdFont {
				tag = "[Mono]"
			}
			fmt.Printf("  • %-6s %-26s (%s)\n", tag, f.Name, f.Source)
		}

	case "help", "-h", "--help":
		printHelp()

	default:
		// If argument matches an existing theme name, apply it directly!
		themesDir := theme.ResolveThemesDir()
		if themesDir != "" {
			themePath := themesDir + "/" + cmd + ".omp.json"
			if _, err := os.Stat(themePath); err == nil {
				if err := theme.SetTheme(cmd, themesDir); err == nil {
					fmt.Printf("✔ Switched theme to '%s'\n", cmd)
					return
				}
			}
		}
		fmt.Fprintf(os.Stderr, "Unknown command '%s'. Run 'agyterm help' for usage.\n", cmd)
		os.Exit(1)
	}
}

func printHelp() {
	fmt.Println(`🎨 AGYTERM - Terminal Font & Theme Manager (Go Engine)

Usage:
  agyterm                          Launch interactive keyboard-only TUI
  agyterm ls                       Display active prompt theme & terminal profiles
  agyterm theme <name>             Set Oh My Posh shell prompt theme
  agyterm font <prof|*> <face> [sz] Set Windows Terminal font face & size
  agyterm opacity <prof|*> <1-100> Set Windows Terminal background opacity
  agyterm fonts                    List available system & Nerd fonts
  agyterm help                     Show this help message`)
}
