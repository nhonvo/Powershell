package main

import (
	"context"
	"fmt"
	"os"
	"os/signal"
	"strconv"
	"strings"
	"syscall"
	"time"

	"agybot/internal/account"
	"agybot/internal/auth"
	"agybot/internal/bot"
	"agybot/internal/config"
	"agybot/internal/runner"
	"agybot/internal/security"
	"agybot/internal/view"
	"agybot/internal/workspace"
)

func main() {
	cfg := config.LoadConfig()
	guard := security.NewSecurityGuard()
	authMgr := auth.NewAuthManager(
		cfg.AuthPinHash,
		cfg.AuthMaxAttempts,
		cfg.AuthLockoutDuration,
		cfg.AuthAutoLockTimeout,
		cfg.AllowedUserIDs,
	)
	wsMgr := workspace.NewWorkspaceManager(cfg.DefaultWorkspace)
	accMgr := account.NewAccountManager()
	r := runner.NewAntigravityRunner(guard)
	v := view.NewDashboardView(cfg, authMgr, wsMgr, accMgr)

	args := os.Args[1:]
	if len(args) == 0 {
		fmt.Print(v.RenderOverview())
		return
	}

	cmd := strings.ToLower(args[0])

	switch cmd {
	case "status", "info":
		fmt.Print(v.RenderOverview())

	case "set-pin":
		if len(args) < 2 {
			fmt.Println("Usage: agybot set-pin <6-digit-pin>")
			os.Exit(1)
		}
		pin := args[1]
		hashed := auth.HashPIN(pin)
		fmt.Printf("✅ Generated scrypt PIN hash:\n\nAUTH_PIN_HASH=\"%s\"\n\nAdd this to your .env or ~/.config/antigravity/bot.env\n", hashed)

	case "switch":
		if len(args) < 2 {
			fmt.Println("Usage: agybot switch <account_name>")
			os.Exit(1)
		}
		target := args[1]
		if err := accMgr.SwitchAccount(target); err != nil {
			fmt.Printf("❌ Switch failed: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Successfully switched active Antigravity account to: %s\n", target)

	case "projects", "proj":
		projs := wsMgr.ListProjects()
		fmt.Printf("📁 Registered Projects in agyproj (%d):\n\n", len(projs))
		for i, p := range projs {
			marker := "  "
			if p.IsActive {
				marker = "⭐ "
			}
			fmt.Printf("%s[%d] %-20s  Branch: %-12s  Path: %s\n", marker, i+1, p.Name, p.GitBranch, p.Path)
		}

	case "daemon", "start", "bot":
		if cfg.TelegramBotToken == "" {
			fmt.Println("❌ Error: TELEGRAM_BOT_TOKEN is not set in environment or .env file.")
			fmt.Println("👉 Please set TELEGRAM_BOT_TOKEN in .env or ~/.config/antigravity/bot.env")
			os.Exit(1)
		}

		client := bot.NewTelegramClient(cfg.TelegramBotToken)
		ctx, cancel := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
		defer cancel()

		u, err := client.GetMe(ctx)
		if err != nil {
			fmt.Printf("❌ Failed to connect to Telegram Bot API: %v\n", err)
			os.Exit(1)
		}

		fmt.Printf("🚀 Starting AgyBot Telegram Server as @%s (ID: %d)...\n", u.Username, u.ID)
		fmt.Printf("• Whitelist: %d authorized users\n", len(cfg.AllowedUserIDs))
		fmt.Printf("• Default Workspace: %s\n", cfg.DefaultWorkspace)
		fmt.Printf("• Active Account: %s\n", accMgr.GetActiveAccount())
		fmt.Println("Press Ctrl+C to terminate.")

		handler := bot.NewBotHandler(cfg, client, authMgr, wsMgr, accMgr, r, guard)
		if err := handler.StartPolling(ctx); err != nil && err != context.Canceled {
			fmt.Printf("Bot polling ended: %v\n", err)
		}

	case "test-prompt":
		if len(args) < 2 {
			fmt.Println("Usage: agybot test-prompt <prompt text>")
			os.Exit(1)
		}
		prompt := strings.Join(args[1:], " ")
		ws := wsMgr.GetUserWorkspace(0)
		fmt.Printf("🤖 Running Antigravity prompt in %s...\n\n", ws)

		ctx, cancel := context.WithTimeout(context.Background(), cfg.TaskTimeout)
		defer cancel()

		ch, err := r.ExecutePrompt(ctx, runner.RunnerOptions{
			AgyPath:      cfg.AgyPath,
			WorkspaceDir: ws,
			Prompt:       prompt,
			Model:        cfg.DefaultModel,
			Mode:         cfg.DefaultMode,
		})
		if err != nil {
			fmt.Printf("Error: %v\n", err)
			os.Exit(1)
		}

		for ev := range ch {
			switch ev.Type {
			case "tool_start":
				fmt.Printf("%s\n", ev.Content)
			case "content":
				fmt.Print(ev.Content)
			case "error":
				fmt.Printf("\n❌ %s\n", ev.Content)
			case "done":
				fmt.Println("\n✔ Done")
			}
		}

	case "config":
		handleConfigCommand(cfg, args[1:])

	case "sessions":
		maxCount := 10
		sessions := wsMgr.ListBrainSessions(maxCount)
		fmt.Printf("🧠 Recent Antigravity Sessions (%d found):\n\n", len(sessions))
		for i, s := range sessions {
			timeAgo := time.Since(s.CreatedAt).Round(time.Minute)
			fmt.Printf("[%d] ID: %s (%s ago · %d steps · est: $%.4f)\n    Summary: %s\n\n",
				i+1, s.ID, timeAgo, s.Steps, s.CostUSD, s.Summary)
		}

	case "newproj", "createproj":
		if len(args) < 2 {
			fmt.Println("Usage: agybot newproj <project_name> [go|node|python|dotnet]")
			os.Exit(1)
		}
		pName := args[1]
		stack := "generic"
		if len(args) > 2 {
			stack = args[2]
		}
		p, err := wsMgr.CreateProject(pName, stack)
		if err != nil {
			fmt.Printf("❌ Failed to create project: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("🎉 Project '%s' successfully created and registered in agyproj!\n", p.Name)
		fmt.Printf("• Path: %s\n• Stack: %s\n• Git: Initialized (main)\n", p.Path, p.Stack)

	case "research":
		if len(args) < 2 {
			fmt.Println("Usage: agybot research <topic or question>")
			os.Exit(1)
		}
		topic := strings.Join(args[1:], " ")
		ws := wsMgr.GetUserWorkspace(0)
		fmt.Printf("🔬 Performing deep Antigravity research on: '%s' in %s...\n\n", topic, ws)

		researchPrompt := fmt.Sprintf(
			"Perform deep research on: %s. Structure your output: "+
				"1. Executive Summary, 2. Architecture & Components, 3. Trade-offs & Risks, "+
				"4. Practical Steps, 5. Recommendation.",
			topic,
		)

		ctx, cancel := context.WithTimeout(context.Background(), cfg.TaskTimeout)
		defer cancel()

		ch, err := r.ExecutePrompt(ctx, runner.RunnerOptions{
			AgyPath:      cfg.AgyPath,
			WorkspaceDir: ws,
			Prompt:       researchPrompt,
			Model:        cfg.DefaultModel,
			Mode:         cfg.DefaultMode,
		})
		if err != nil {
			fmt.Printf("Error: %v\n", err)
			os.Exit(1)
		}

		for ev := range ch {
			switch ev.Type {
			case "tool_start":
				fmt.Printf("%s\n", ev.Content)
			case "content":
				fmt.Print(ev.Content)
			case "error":
				fmt.Printf("\n❌ %s\n", ev.Content)
			case "done":
				fmt.Println("\n✔ Research completed.")
			}
		}

	case "resume":
		if len(args) < 3 {
			fmt.Println("Usage: agybot resume <conversation_id> <prompt>")
			os.Exit(1)
		}
		convID := args[1]
		prompt := strings.Join(args[2:], " ")
		ws := wsMgr.GetUserWorkspace(0)
		fmt.Printf("🔄 Resuming conversation %s in %s...\n\n", convID, ws)

		ctx, cancel := context.WithTimeout(context.Background(), cfg.TaskTimeout)
		defer cancel()

		ch, err := r.ExecutePrompt(ctx, runner.RunnerOptions{
			AgyPath:        cfg.AgyPath,
			WorkspaceDir:   ws,
			Prompt:         prompt,
			ConversationID: convID,
			Model:          cfg.DefaultModel,
			Mode:           cfg.DefaultMode,
		})
		if err != nil {
			fmt.Printf("Error: %v\n", err)
			os.Exit(1)
		}

		for ev := range ch {
			switch ev.Type {
			case "tool_start":
				fmt.Printf("%s\n", ev.Content)
			case "content":
				fmt.Print(ev.Content)
			case "error":
				fmt.Printf("\n❌ %s\n", ev.Content)
			case "done":
				fmt.Println("\n✔ Done")
			}
		}

	default:
		fmt.Printf("Unknown command '%s'. Available: status, daemon, config, set-pin, switch, projects, newproj, research, sessions, resume, test-prompt\n", cmd)
	}
}

func handleConfigCommand(cfg *config.Config, args []string) {
	if len(args) == 0 {
		fmt.Println("⚙️ AGYBOT CONFIGURATION:")
		fmt.Printf("• Config File:        %s\n", config.GetConfigFilePath())
		fmt.Printf("• TELEGRAM_BOT_TOKEN: %s\n", maskToken(cfg.TelegramBotToken))
		fmt.Printf("• ALLOWED_USER_IDS:   %v\n", formatWhitelist(cfg.AllowedUserIDs))
		fmt.Printf("• AUTH_PIN_HASH:      %s\n", maskHash(cfg.AuthPinHash))
		fmt.Printf("• DEFAULT_WORKSPACE:  %s\n", cfg.DefaultWorkspace)
		fmt.Printf("• DEFAULT_MODEL:      %s\n", cfg.DefaultModel)
		fmt.Printf("• DEFAULT_EFFORT:     %s\n", cfg.DefaultEffort)
		fmt.Printf("• DEFAULT_MODE:       %s\n", cfg.DefaultMode)
		fmt.Printf("• TASK_TIMEOUT:       %v\n", cfg.TaskTimeout)
		fmt.Printf("• AUTO_LOCK_MINUTES:  %v\n", cfg.AuthAutoLockTimeout)
		fmt.Println("\nUsage:")
		fmt.Println("  agybot config set <KEY> <VALUE>      Update a setting in bot.env")
		fmt.Println("  agybot config whitelist add <USER_ID> Add Telegram ID to whitelist")
		fmt.Println("  agybot config whitelist rm <USER_ID>  Remove Telegram ID")
		return
	}

	sub := strings.ToLower(args[0])
	switch sub {
	case "set":
		if len(args) < 3 {
			fmt.Println("Usage: agybot config set <KEY> <VALUE>")
			os.Exit(1)
		}
		key := args[1]
		val := args[2]
		if err := config.SaveConfigKey(key, val); err != nil {
			fmt.Printf("❌ Failed to save config: %v\n", err)
			os.Exit(1)
		}
		fmt.Printf("✔ Saved %s=%q to %s\n", strings.ToUpper(key), val, config.GetConfigFilePath())

	case "whitelist":
		if len(args) < 2 {
			fmt.Printf("Allowed User IDs: %v\n", formatWhitelist(cfg.AllowedUserIDs))
			return
		}
		action := strings.ToLower(args[1])
		switch action {
		case "add":
			if len(args) < 3 {
				fmt.Println("Usage: agybot config whitelist add <USER_ID>")
				os.Exit(1)
			}
			id, err := strconv.ParseInt(args[2], 10, 64)
			if err != nil {
				fmt.Printf("Invalid User ID: %v\n", err)
				os.Exit(1)
			}
			if err := cfg.AddWhitelistUser(id); err != nil {
				fmt.Printf("Failed to update whitelist: %v\n", err)
				os.Exit(1)
			}
			fmt.Printf("✔ User ID %d added to whitelist!\n", id)
		case "rm", "remove", "del":
			if len(args) < 3 {
				fmt.Println("Usage: agybot config whitelist rm <USER_ID>")
				os.Exit(1)
			}
			id, err := strconv.ParseInt(args[2], 10, 64)
			if err != nil {
				fmt.Printf("Invalid User ID: %v\n", err)
				os.Exit(1)
			}
			if err := cfg.RemoveWhitelistUser(id); err != nil {
				fmt.Printf("Failed to update whitelist: %v\n", err)
				os.Exit(1)
			}
			fmt.Printf("✔ User ID %d removed from whitelist!\n", id)
		case "list", "ls":
			fmt.Printf("Allowed User IDs: %v\n", formatWhitelist(cfg.AllowedUserIDs))
		}
	}
}

func maskToken(tok string) string {
	if len(tok) <= 8 {
		return "******"
	}
	return tok[:6] + "..." + tok[len(tok)-4:]
}

func maskHash(h string) string {
	if len(h) <= 12 {
		return "******"
	}
	return h[:8] + "..."
}

func formatWhitelist(ids map[int64]bool) []int64 {
	var list []int64
	for id := range ids {
		list = append(list, id)
	}
	return list
}
