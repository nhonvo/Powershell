package main

import (
	"context"
	"fmt"
	"os"
	"os/signal"
	"strings"
	"syscall"

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

	default:
		fmt.Printf("Unknown command '%s'. Available: status, daemon, set-pin, switch, projects, test-prompt\n", cmd)
	}
}
