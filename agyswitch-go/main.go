package main

import (
	"fmt"
	"os"
	"strings"

	"agyswitch/launcher"
	"agyswitch/store"
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
		active := s.GetActiveAccount()
		if err := l.LaunchAccount(active, nil); err != nil {
			fmt.Fprintf(os.Stderr, "Error launching agy: %v\n", err)
			os.Exit(1)
		}
		return
	}

	cmd := strings.ToLower(args[0])

	switch cmd {
	case "status", "list", "ls":
		printStatus(s)
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

func printStatus(s *store.Store) {
	fmt.Println("\n🛸 \033[1;36mAGYSWITCH - Dedicated Antigravity Multi-Account Vault (Go Engine)\033[0m")
	fmt.Println("──────────────────────────────────────────────────────────────────────────────────")
	accs := s.ListAccounts()
	active := s.GetActiveAccount()

	fmt.Printf(" Active Account: \033[1;32m%s\033[0m\n\n", active)

	for i, a := range accs {
		activeMarker := "  "
		if a.IsActive {
			activeMarker = "● "
		}

		statusBadge := "\033[31m✘ Logged Out\033[0m"
		if a.IsLoggedIn {
			statusBadge = fmt.Sprintf("\033[32m✔ Logged In · Key: %s\033[0m", a.TokenSig)
		}

		fmt.Printf(" %s%d. \033[1m%-22s\033[0m (%-30s) (%s)\n",
			activeMarker, i+1, a.AccountName, a.Email, statusBadge)
	}
	fmt.Printf("──────────────────────────────────────────────────────────────────────────────────\n\n")
}
