package main

import (
	"fmt"
	"os"
	"strings"

	"agyswitch/launcher"
	"agyswitch/store"
	"agyswitch/tui"
	"agyswitch/vault"
)

func main() {
	userHome, err := os.UserHomeDir()
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error getting user home dir: %v\n", err)
		os.Exit(1)
	}

	v := vault.NewVault(userHome)
	s := store.NewStore(userHome, v)
	l := launcher.NewLauncher(s, v)

	args := os.Args[1:]

	if len(args) == 0 {
		if err := tui.Run(s, v, l); err != nil {
			fmt.Fprintf(os.Stderr, "Error running TUI: %v\n", err)
			os.Exit(1)
		}
		return
	}

	cmd := strings.ToLower(args[0])

	switch cmd {
	case "status", "list", "ls":
		tui.PrintStatus(s)
	case "switch", "sw":
		if len(args) < 2 {
			fmt.Println("Usage: agyswitch switch <accountName>")
			os.Exit(1)
		}
		target := args[1]
		if err := s.SetActiveAccount(target); err != nil {
			fmt.Fprintf(os.Stderr, "Error switching account: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("\033[36m[agyswitch]\033[0m Successfully switched active account context to '\033[32m%s\033[0m'.\n", target)
	default:
		// Check if first argument is a known account name or folder name
		accs := s.ListAccounts()
		isAcc := false
		targetAcc := cmd

		for _, a := range accs {
			if strings.EqualFold(a.AccountName, cmd) {
				isAcc = true
				break
			}
		}

		if isAcc {
			if err := l.LaunchAccount(targetAcc, args[1:]); err != nil {
				fmt.Fprintf(os.Stderr, "Error launching agy: %v\n", err)
				os.Exit(1)
			}
		} else {
			// Launch current active account with passed args
			active := s.GetActiveAccount()
			if err := l.LaunchAccount(active, args); err != nil {
				fmt.Fprintf(os.Stderr, "Error launching agy: %v\n", err)
				os.Exit(1)
			}
		}
	}
}
