package main

import (
	"fmt"
	"os"
	"strconv"
	"strings"

	"agyswitch/internal/service/rules"
	"agyswitch/internal/service/seeder"
	"agyswitch/internal/service/server"
	"agyswitch/internal/service/sessions"
	"agyswitch/internal/service/skills"
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
	launchAdapter := func(accountName string, dir string, args []string) error {
		oldLauncher := launcher.NewLauncher(s, v)
		return oldLauncher.LaunchAccountInDir(accountName, dir, args)
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
	case "switch", "use":
		if len(args) < 2 {
			fmt.Println("Usage: agyswitch switch <accountName>")
			os.Exit(1)
		}
		target := s.ResolveAccount(args[1])
		if err := s.SetActiveAccount(target); err != nil {
			fmt.Fprintf(os.Stderr, "Error switching account: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("\033[36m[agyswitch]\033[0m Successfully switched active context to '\033[32m%s\033[0m'.\n", target)
	case "status", "list", "ls":
		app.PrintStatus(os.Stdout)
	case "sessions", "session", "sess":
		sm := sessions.NewManager(userHome)
		list, err := sm.DiscoverSessions()
		if err != nil || len(list) == 0 {
			fmt.Println("No conversation sessions found in brain logs.")
			return
		}
		if len(args) >= 2 && (args[1] == "resume" || args[1] == "continue") {
			targetID := ""
			if len(args) >= 3 {
				targetID = args[2]
			} else {
				targetID = list[0].ConversationID
			}
			wsDir := ""
			for _, item := range list {
				if strings.HasPrefix(item.ConversationID, targetID) {
					targetID = item.ConversationID
					wsDir = item.WorkspaceDir
					break
				}
			}
			activeAcc := s.GetActiveAccount()
			fmt.Printf("\033[36m[agyswitch]\033[0m Resuming session '\033[32m%s\033[0m' under account '\033[33m%s\033[0m'...\n", targetID, activeAcc)
			if err := launchAdapter(activeAcc, wsDir, []string{"--conversation", targetID}); err != nil {
				fmt.Fprintf(os.Stderr, "Error resuming session: %v\n", err)
				os.Exit(1)
			}
			return
		}

		groups := sessions.GroupSessionsByProject(list)
		fmt.Printf("\n📊 \033[1;36mAntigravity Sessions (%d total across %d projects):\033[0m\n\n", len(list), len(groups))
		for _, g := range groups {
			fmt.Printf(" \033[1;35m📁 %-30s\033[0m \033[36m(%d sessions · $%0.4f)\033[0m\n", g.ProjectName, len(g.Sessions), g.TotalCost)
			for i, sess := range g.Sessions {
				timeStr := sess.LastActive.Format("2006-01-02 15:04")
				prefix := "├──"
				if i == len(g.Sessions)-1 {
					prefix = "└──"
				}
				fmt.Printf("   %s \033[1m%-48s\033[0m [%s] · %d steps · $%0.4f · %s\n",
					prefix, sess.Title, sess.ConversationID[:8], sess.StepCount, sess.EstimatedCost, timeStr)
			}
			fmt.Println()
		}
	case "skills", "skill":
		skm := skills.NewManager(userHome)
		cwd, _ := os.Getwd()
		list, err := skm.DiscoverSkills(cwd)
		if err != nil || len(list) == 0 {
			fmt.Println("No custom skills discovered in ~/.gemini/skills or .agents/skills")
			return
		}
		fmt.Printf("\n🧩 \033[1;36mAntigravity Skills (%d total):\033[0m\n\n", len(list))
		for i, sk := range list {
			scope := "\033[32m[Global]\033[0m"
			if !sk.IsGlobal {
				scope = "\033[35m[Workspace]\033[0m"
			}
			fmt.Printf(" %2d. %s \033[1m%-24s\033[0m %s\n", i+1, scope, sk.Name, sk.Description)
		}
		fmt.Println()
	case "rules", "rule":
		rm := rules.NewManager(userHome)
		cwd, _ := os.Getwd()
		list, _ := rm.DiscoverRules(cwd)
		fmt.Printf("\n📜 \033[1;36mAntigravity Rules (%d total):\033[0m\n\n", len(list))
		for i, r := range list {
			scope := "\033[32m[Global]\033[0m"
			if !r.IsGlobal {
				scope = "\033[35m[Workspace]\033[0m"
			}
			fmt.Printf(" %2d. %s \033[1m%s\033[0m (%s)\n", i+1, scope, r.Name, r.Path)
		}
		mcps, _ := rm.CheckMCPServerStatus(cwd)
		fmt.Printf("\n🔌 \033[1;36mMCP Servers (%d total):\033[0m\n\n", len(mcps))
		for _, m := range mcps {
			status := "\033[32m● Connected\033[0m"
			if !m.IsRunning {
				status = fmt.Sprintf("\033[31m○ Off (%s)\033[0m", m.LastError)
			}
			fmt.Printf("  • \033[1m%-20s\033[0m %s\n", m.ServerName, status)
		}
		fmt.Println()
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
			target = s.ResolveAccount(args[1])
			_ = s.SetActiveAccount(target)
		}
		fmt.Printf("\033[36m[agyswitch]\033[0m Starting authentication for account '\033[32m%s\033[0m'...\n", target)
		if err := launchAdapter(target, "", []string{"login"}); err != nil {
			fmt.Fprintf(os.Stderr, "Error launching login: %v\n", err)
			os.Exit(1)
		}
	case "logout":
		target := s.GetActiveAccount()
		if len(args) >= 2 {
			target = s.ResolveAccount(args[1])
		}
		if err := s.LogoutAccount(target); err != nil {
			fmt.Fprintf(os.Stderr, "Error logging out account '%s': %v\n", target, err)
			os.Exit(1)
		}
		fmt.Printf("\033[36m[agyswitch]\033[0m Successfully logged out account '\033[33m%s\033[0m'. Context wiped clean.\n", target)
		fmt.Printf("\033[36m[agyswitch]\033[0m Run '\033[33magyswitch login %s\033[0m' to authenticate again.\n", target)
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
		target := s.ResolveAccount(args[1])
		if err := s.DeleteAccount(target); err != nil {
			fmt.Fprintf(os.Stderr, "Error deleting account '%s': %v\n", target, err)
			os.Exit(1)
		}
		fmt.Printf("\033[36m[agyswitch]\033[0m Successfully deleted account '\033[31m%s\033[0m'. Folder and database records removed.\n", target)
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
			target = s.ResolveAccount(args[1])
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
			target = s.ResolveAccount(args[1])
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
		if err := launchAdapter(bestAcc, "", args[1:]); err != nil {
			fmt.Fprintf(os.Stderr, "Error launching agy: %v\n", err)
			os.Exit(1)
		}
	default:
		resolved := s.ResolveAccount(cmd)
		knownAccounts := s.ListAccountNames()
		isRegisteredAccount := false
		for _, a := range knownAccounts {
			if strings.EqualFold(a, resolved) {
				isRegisteredAccount = true
				break
			}
		}

		if isRegisteredAccount && !strings.HasPrefix(cmd, "-") {
			target := resolved
			if err := s.SetActiveAccount(target); err != nil {
				fmt.Fprintf(os.Stderr, "Error setting active account context to '%s': %v\n", target, err)
				os.Exit(1)
			}
			fmt.Printf("\033[36m[agyswitch]\033[0m Switched active context to '\033[32m%s\033[0m'. Launching agy...\n", target)
			if err := launchAdapter(target, "", args[1:]); err != nil {
				fmt.Fprintf(os.Stderr, "Error launching agy: %v\n", err)
				os.Exit(1)
			}
		} else {
			target := s.GetActiveAccount()
			if err := launchAdapter(target, "", args); err != nil {
				fmt.Fprintf(os.Stderr, "Error launching agy: %v\n", err)
				os.Exit(1)
			}
		}
	}
}
