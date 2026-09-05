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
	case "quota", "q":
		target := s.GetActiveAccount()
		if len(args) >= 2 {
			target = args[1]
		}
		if target == "all" {
			for _, a := range s.ListAccounts() {
				if a.IsLoggedIn {
					summary, err := s.GetAccountQuota(a.AccountName)
					if err == nil {
						fmt.Print(store.RenderQuotaSummary(a.Email, summary))
					}
				}
			}
		} else {
			summary, err := s.GetAccountQuota(target)
			if err != nil {
				fmt.Printf("\033[31mError fetching quota for '%s': %v\033[0m\n", target, err)
			} else {
				email := fmt.Sprintf("%s@gmail.com", target)
				fmt.Print(store.RenderQuotaSummary(email, summary))
			}
		}
	case "reset":
		target := s.GetActiveAccount()
		if len(args) >= 2 {
			target = args[1]
		}
		if err := s.ResetAccount(target); err != nil {
			fmt.Fprintf(os.Stderr, "Error resetting account '%s': %v\n", target, err)
			os.Exit(1)
		}
		fmt.Printf("\033[36m[agyswitch]\033[0m Credentials and session token for account '\033[33m%s\033[0m' reset cleanly.\n", target)
	case "launch-quota", "auto-launch":
		bestAcc := s.SelectBestQuotaAccount()
		fmt.Printf("\033[36m[agyswitch]\033[0m Quota selector auto-selected account '\033[32m%s\033[0m'\n", bestAcc)
		if err := l.LaunchAccount(bestAcc, args[1:]); err != nil {
			fmt.Fprintf(os.Stderr, "Error launching agy: %v\n", err)
			os.Exit(1)
		}
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
