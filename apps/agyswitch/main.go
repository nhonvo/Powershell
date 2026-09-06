package main

import (
	"fmt"
	"os"
	"strconv"
	"strings"

	"agyswitch/internal/service/seeder"
	"agyswitch/internal/service/server"
	"agyswitch/internal/service/store"
	"agyswitch/internal/service/vault"
	"agyswitch/internal/view"
	"agyswitch/launcher"
)

func main() {
	userHome, err := os.UserHomeDir()
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error getting user home dir: %v\n", err)
		os.Exit(1)
	}

	v := vault.NewVault(userHome)
	s := store.NewStore(userHome, v)

	// Custom launcher closure adapter
	launchAdapter := func(accountName string, args []string) error {
		oldLauncher := launcher.NewLauncher(s, v)
		return oldLauncher.LaunchAccount(accountName, args)
	}

	app := view.NewApp(s, launchAdapter)
	args := os.Args[1:]

	if len(args) == 0 {
		if err := app.Run(); err != nil {
			fmt.Fprintf(os.Stderr, "Error running TUI: %v\n", err)
			os.Exit(1)
		}
		return
	}

	cmd := strings.ToLower(args[0])

	switch cmd {
	case "status", "list", "ls":
		app.PrintStatus(os.Stdout)
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
	case "add", "create", "new":
		if len(args) < 2 {
			fmt.Println("Usage: agyswitch add <accountName>")
			os.Exit(1)
		}
		target := args[1]
		if err := s.AddAccount(target); err != nil {
			fmt.Fprintf(os.Stderr, "Error creating account '%s': %v\n", target, err)
			os.Exit(1)
		}
		fmt.Printf("\033[36m[agyswitch]\033[0m Created account context '\033[32m%s\033[0m' and set active.\n", target)
		fmt.Printf("\033[36m[agyswitch]\033[0m Run '\033[33magyswitch login\033[0m' or launch \033[33magysw\033[0m to authenticate with Google.\n")
	case "login":
		target := s.GetActiveAccount()
		if len(args) >= 2 {
			target = args[1]
			_ = s.SetActiveAccount(target)
		}
		fmt.Printf("\033[36m[agyswitch]\033[0m Launching 'agy login' for account '\033[32m%s\033[0m'...\n", target)
		if err := launchAdapter(target, []string{"login"}); err != nil {
			fmt.Fprintf(os.Stderr, "Error launching agy login: %v\n", err)
			os.Exit(1)
		}
	case "rename", "mv":
		if len(args) < 3 {
			fmt.Println("Usage: agyswitch rename <oldName> <newName>")
			os.Exit(1)
		}
		if err := s.RenameAccount(args[1], args[2]); err != nil {
			fmt.Fprintf(os.Stderr, "Error renaming account: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("\033[36m[agyswitch]\033[0m Successfully renamed account '\033[33m%s\033[0m' -> '\033[32m%s\033[0m'.\n", args[1], args[2])
	case "delete", "rm":
		if len(args) < 2 {
			fmt.Println("Usage: agyswitch delete <accountName>")
			os.Exit(1)
		}
		target := args[1]
		if err := s.DeleteAccount(target); err != nil {
			fmt.Fprintf(os.Stderr, "Error deleting account '%s': %v\n", target, err)
			os.Exit(1)
		}
		fmt.Printf("\033[36m[agyswitch]\033[0m Successfully deleted account '\033[31m%s\033[0m'.\n", target)
	case "init":
		sd := seeder.NewSeeder(userHome, s)
		if err := sd.EnsureSeedTemplate(); err != nil {
			fmt.Fprintf(os.Stderr, "Error initializing seed template: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("\033[36m[agyswitch]\033[0m Successfully initialized master seed template at ~/.gemini_template\n")
	case "seed":
		target := s.GetActiveAccount()
		if len(args) >= 2 {
			target = args[1]
		}
		sd := seeder.NewSeeder(userHome, s)
		if err := sd.SeedAccount(target); err != nil {
			fmt.Fprintf(os.Stderr, "Error seeding account '%s': %v\n", target, err)
			os.Exit(1)
		}
		fmt.Printf("\033[36m[agyswitch]\033[0m Successfully seeded account context '\033[32m%s\033[0m' from ~/.gemini_template\n", target)
	case "serve", "--serve":
		port := 8080
		if len(args) >= 2 {
			if p, err := strconv.Atoi(args[1]); err == nil && p > 0 {
				port = p
			}
		}
		srv := server.NewServer(s, port)
		if err := srv.Start(); err != nil {
			fmt.Fprintf(os.Stderr, "Error running web sidecar: %v\n", err)
			os.Exit(1)
		}
	case "reset":
		target := s.GetActiveAccount()
		mode := "normal"
		if len(args) >= 2 {
			target = args[1]
		}
		if len(args) >= 3 {
			mode = strings.TrimPrefix(args[2], "--")
		}
		sd := seeder.NewSeeder(userHome, s)
		if err := sd.ResetAccountEx(target, mode); err != nil {
			fmt.Fprintf(os.Stderr, "Error resetting account '%s': %v\n", target, err)
			os.Exit(1)
		}
		fmt.Printf("\033[36m[agyswitch]\033[0m Account '\033[33m%s\033[0m' reset cleanly (mode: %s).\n", target, mode)
	case "launch-quota", "auto-launch":
		bestAcc := s.SelectBestQuotaAccount()
		fmt.Printf("\033[36m[agyswitch]\033[0m Quota selector auto-selected account '\033[32m%s\033[0m'\n", bestAcc)
		if err := launchAdapter(bestAcc, args[1:]); err != nil {
			fmt.Fprintf(os.Stderr, "Error launching agy: %v\n", err)
			os.Exit(1)
		}
	default:
		knownAccounts := s.ListAccountNames()
		isRegisteredAccount := false
		for _, a := range knownAccounts {
			if strings.EqualFold(a, cmd) {
				isRegisteredAccount = true
				break
			}
		}

		if isRegisteredAccount && !strings.HasPrefix(cmd, "-") {
			target := cmd
			if err := s.SetActiveAccount(target); err != nil {
				fmt.Fprintf(os.Stderr, "Error setting active account context to '%s': %v\n", target, err)
				os.Exit(1)
			}
			fmt.Printf("\033[36m[agyswitch]\033[0m Switched active context to '\033[32m%s\033[0m'. Launching agy...\n", target)
			if err := launchAdapter(target, args[1:]); err != nil {
				fmt.Fprintf(os.Stderr, "Error launching agy: %v\n", err)
				os.Exit(1)
			}
		} else {
			target := s.GetActiveAccount()
			if err := launchAdapter(target, args); err != nil {
				fmt.Fprintf(os.Stderr, "Error launching agy: %v\n", err)
				os.Exit(1)
			}
		}
	}
}
