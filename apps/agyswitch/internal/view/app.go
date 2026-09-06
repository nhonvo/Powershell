package view

import (
	"fmt"
	"io"
	"os"
	"strings"

	"golang.org/x/term"

	"agyswitch/internal/model"
	"agyswitch/internal/service/rules"
	"agyswitch/internal/service/sessions"
	"agyswitch/internal/service/skills"
	"agyswitch/internal/service/seeder"
	"agyswitch/internal/service/store"
)

type App struct {
	Store          *store.Store
	SkillsManager  *skills.Manager
	RulesManager   *rules.Manager
	SessionManager *sessions.Manager
	Seeder         *seeder.Seeder
	Launcher       func(accountName string, args []string) error

	ActiveTab     int // 0 = Vault, 1 = Skills, 2 = Rules, 3 = Sessions
	SelectedIndex int
	StatusMsg     string
}

func NewApp(s *store.Store, launcher func(string, []string) error) *App {
	userHome := s.UserHome
	return &App{
		Store:          s,
		SkillsManager:  skills.NewManager(userHome),
		RulesManager:   rules.NewManager(userHome),
		SessionManager: sessions.NewManager(userHome),
		Seeder:         seeder.NewSeeder(userHome, s),
		Launcher:       launcher,
		ActiveTab:      0,
		SelectedIndex:  0,
	}
}

func (a *App) Run() error {
	fd := int(os.Stdin.Fd())
	oldState, err := term.MakeRaw(fd)
	if err != nil {
		return a.runNonInteractive()
	}

	// Enter Alternate Screen Buffer & hide terminal cursor for clean in-place TUI rendering
	fmt.Print("\033[?1049h\033[?25l")
	defer func() {
		fmt.Print("\033[?25h\033[?1049l")
		_ = term.Restore(fd, oldState)
	}()

	accs := a.Store.ListAccountsFast()
	activeAcc := a.Store.GetActiveAccount()
	for i, acc := range accs {
		if strings.EqualFold(acc.AccountName, activeAcc) {
			a.SelectedIndex = i
			break
		}
	}

	for {
		if a.SelectedIndex >= len(accs) && len(accs) > 0 {
			a.SelectedIndex = len(accs) - 1
		}
		if a.SelectedIndex < 0 {
			a.SelectedIndex = 0
		}

		a.Render(accs)
		a.StatusMsg = ""

		var buf [3]byte
		n, err := os.Stdin.Read(buf[:])
		if err != nil || n == 0 {
			break
		}

		b := buf[0]
		if b == 0x1b {
			if n >= 3 && buf[1] == '[' {
				switch buf[2] {
				case 'A': // Up
					if a.SelectedIndex > 0 {
						a.SelectedIndex--
					}
					continue
				case 'B': // Down
					if a.SelectedIndex < len(accs)-1 {
						a.SelectedIndex++
					}
					continue
				case 'C': // Right Tab
					a.ActiveTab = (a.ActiveTab + 1) % 4
					a.SelectedIndex = 0
					continue
				case 'D': // Left Tab
					a.ActiveTab = (a.ActiveTab + 3) % 4
					a.SelectedIndex = 0
					continue
				}
			}
			break
		}

		switch b {
		case '\t': // Tab key switches active tab
			a.ActiveTab = (a.ActiveTab + 1) % 4
			a.SelectedIndex = 0
		case '1':
			a.ActiveTab = 0
			a.SelectedIndex = 0
		case '2':
			a.ActiveTab = 1
			a.SelectedIndex = 0
		case '3':
			a.ActiveTab = 2
			a.SelectedIndex = 0
		case '4':
			a.ActiveTab = 3
			a.SelectedIndex = 0
		case 'k', 'K', 'u', 'U':
			if a.SelectedIndex > 0 {
				a.SelectedIndex--
			}
		case 'j', 'J':
			if a.SelectedIndex < len(accs)-1 {
				a.SelectedIndex++
			}
		case '\r', '\n', 'e', 'E': // Enter / e key (Switch active context)
			if a.ActiveTab == 0 && a.SelectedIndex < len(accs) {
				target := accs[a.SelectedIndex].AccountName
				if err := a.Store.SetActiveAccount(target); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError switching account: %v\033[0m", err)
				} else {
					accs = a.Store.ListAccountsFast()
					a.StatusMsg = fmt.Sprintf("\033[32mSwitched active context to '%s'\033[0m", target)
				}
			}
		case 'v', 'V': // View detail modal
			if a.ActiveTab == 0 && a.SelectedIndex < len(accs) {
				sel := accs[a.SelectedIndex]
				fmt.Print("\033[H\033[2J")
				fmt.Printf("\r\n📊 \033[1;36mDetailed Model Quota Breakdown (%s):\033[0m\r\n\r\n", sel.AccountName)
				if sel.QuotaSummary != nil {
					details := store.ExtractDetailedModelBuckets(sel.QuotaSummary)
					for _, d := range details {
						fmt.Printf("  • \033[1m%-25s\033[0m [%-8s] Remaining: \033[1;32m%.1f%%\033[0m · %s\r\n",
							d.ModelDisplayName, d.WindowType, d.RemainingPct, d.ResetMessage)
					}
				} else {
					fmt.Print("  \033[33mNo detailed quota payload available.\033[0m\r\n")
				}
				fmt.Print("\r\n \033[1mPress any key to return...\033[0m")
				var dummy [1]byte
				_, _ = os.Stdin.Read(dummy[:])
			} else if a.ActiveTab == 1 {
				skillsList, _ := a.SkillsManager.DiscoverSkills("")
				if a.SelectedIndex < len(skillsList) {
					sk := skillsList[a.SelectedIndex]
					fmt.Print("\033[H\033[2J")
					fmt.Printf("\r\n🧩 \033[1;36mSkill Inspector: %s\033[0m\r\n", sk.Name)
					fmt.Printf(" Path: %s\r\n Scope: %v\r\n Description: %s\r\n\r\n", sk.Path, sk.IsGlobal, sk.Description)
					fmt.Print(" \033[1mPress any key to return...\033[0m")
					var dummy [1]byte
					_, _ = os.Stdin.Read(dummy[:])
				}
			} else if a.ActiveTab == 2 {
				rulesList, _ := a.RulesManager.DiscoverRules("")
				if a.SelectedIndex < len(rulesList) {
					rl := rulesList[a.SelectedIndex]
					fmt.Print("\033[H\033[2J")
					fmt.Printf("\r\n📜 \033[1;36mRule File Inspector: %s\033[0m\r\n", rl.Name)
					fmt.Printf(" Path: %s\r\n Scope: %v\r\n\r\n", rl.Path, rl.IsGlobal)
					if data, err := os.ReadFile(rl.Path); err == nil {
						fmt.Print("\033[33m--- Rule Content Preview ---\033[0m\r\n")
						lines := strings.Split(string(data), "\n")
						if len(lines) > 20 {
							lines = lines[:20]
						}
						for _, l := range lines {
							fmt.Printf(" %s\r\n", l)
						}
					}
					fmt.Print("\r\n \033[1mPress any key to return...\033[0m")
					var dummy [1]byte
					_, _ = os.Stdin.Read(dummy[:])
				}
			} else if a.ActiveTab == 3 {
				sessionsList, _ := a.SessionManager.DiscoverSessions()
				if a.SelectedIndex < len(sessionsList) {
					s := sessionsList[a.SelectedIndex]
					fmt.Print("\033[H\033[2J")
					fmt.Printf("\r\n📊 \033[1;36mSession Trajectory Inspector (%s):\033[0m\r\n\r\n", s.ConversationID)
					fmt.Printf(" Title:     \033[1;37m%s\033[0m\r\n Workspace: \033[35m%s\033[0m\r\n Steps:     \033[1;33m%d\033[0m · Est. Cost: \033[1;32m$%0.4f\033[0m\r\n Log Path:  \033[36m%s\033[0m\r\n\r\n",
						s.Title, s.WorkspaceDir, s.StepCount, s.EstimatedCost, s.LogPath)
					steps, err := sessions.ParseTranscriptSteps(s.LogPath)
					if err == nil && len(steps) > 0 {
						fmt.Print(" \033[1;33mRecent Step Trajectory History:\033[0m\r\n")
						start := 0
						if len(steps) > 12 {
							start = len(steps) - 12
						}
						for _, st := range steps[start:] {
							fmt.Printf("  • Step %-3d: Type: \033[1m%-18s\033[0m Status: \033[32m%s\033[0m\r\n", st.StepIndex, st.Type, st.Status)
						}
					}
					fmt.Print("\r\n \033[1mPress any key to return...\033[0m")
					var dummy [1]byte
					_, _ = os.Stdin.Read(dummy[:])
				}
			}
		case 's', 'S': // Sync skills across accounts
			if a.ActiveTab == 1 {
				names := a.Store.ListAccountNames()
				var dirs []string
				for _, n := range names {
					dirs = append(dirs, a.Store.GetAccountDirectory(n))
				}
				count, err := a.SkillsManager.SyncSkills(dirs)
				if err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError syncing skills: %v\033[0m", err)
				} else {
					a.StatusMsg = fmt.Sprintf("\033[32mSuccessfully synced global skills across %d account contexts.\033[0m", count)
				}
			}
		case 'l', 'L': // Launch agy
			if a.ActiveTab == 0 && a.SelectedIndex < len(accs) {
				target := accs[a.SelectedIndex].AccountName
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyswitch]\033[0m Launching 'agy' for account '\033[32m%s\033[0m'...\r\n", target)
				return a.Launcher(target, nil)
			}
		case 'r', 'R': // Probe live quotas on-demand
			if a.ActiveTab == 0 {
				a.StatusMsg = "\033[36mProbing live Google CloudCode quotas...\033[0m"
				accs = a.Store.ListAccounts()
				a.StatusMsg = "\033[32mSuccessfully updated live quotas.\033[0m"
			}
		case 'a', 'A': // Auto-select best quota
			if a.ActiveTab == 0 {
				bestAcc := a.Store.SelectBestQuotaAccount()
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyswitch]\033[0m Auto-selected account '\033[32m%s\033[0m'...\r\n", bestAcc)
				return a.Launcher(bestAcc, nil)
			}
		case 'n', 'N': // New Item (Account in Tab 0, Rule in Tab 2)
			if a.ActiveTab == 0 {
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Print("\r\n\033[36m[agyswitch]\033[0m Enter new account name: ")
				var newAcc string
				fmt.Scanln(&newAcc)
				newAcc = strings.TrimSpace(newAcc)
				if newAcc != "" {
					if err := a.Store.AddAccount(newAcc); err == nil {
						fmt.Printf("\033[36m[agyswitch]\033[0m Created account context '\033[32m%s\033[0m'. Launching 'agy login'...\r\n", newAcc)
						return a.Launcher(newAcc, []string{"login"})
					}
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
			} else if a.ActiveTab == 2 {
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Print("\r\n\033[36m[agyswitch]\033[0m Enter new rule file name (e.g. STRICT_CODING.md): ")
				var ruleName string
				fmt.Scanln(&ruleName)
				ruleName = strings.TrimSpace(ruleName)
				if ruleName != "" {
					if err := a.RulesManager.CreateRule(ruleName, ""); err == nil {
						a.StatusMsg = fmt.Sprintf("\033[32mCreated new rule '%s' in ~/.gemini/config/rules/\033[0m", ruleName)
					}
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
			}
		case 'm', 'M': // Rename Account
			if a.ActiveTab == 0 && a.SelectedIndex < len(accs) {
				target := accs[a.SelectedIndex].AccountName
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyswitch]\033[0m Enter new name for '\033[33m%s\033[0m': ", target)
				var newName string
				fmt.Scanln(&newName)
				newName = strings.TrimSpace(newName)
				if newName != "" && newName != target {
					if err := a.Store.RenameAccount(target, newName); err == nil {
						accs = a.Store.ListAccounts()
						a.StatusMsg = fmt.Sprintf("\033[32mRenamed '%s' -> '%s'\033[0m", target, newName)
					}
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
			}
		case 'd', 'D': // Delete Item (Account in Tab 0, Rule in Tab 2)
			if a.ActiveTab == 0 && a.SelectedIndex < len(accs) {
				target := accs[a.SelectedIndex].AccountName
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[31m[agyswitch]\033[0m Delete account '%s'? (y/N): ", target)
				var confirm string
				fmt.Scanln(&confirm)
				if strings.EqualFold(strings.TrimSpace(confirm), "y") {
					if err := a.Store.DeleteAccount(target); err == nil {
						accs = a.Store.ListAccounts()
						a.StatusMsg = fmt.Sprintf("\033[33mDeleted account '%s'\033[0m", target)
					}
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
			} else if a.ActiveTab == 2 {
				rulesList, _ := a.RulesManager.DiscoverRules("")
				if a.SelectedIndex < len(rulesList) {
					rl := rulesList[a.SelectedIndex]
					fmt.Print("\033[?25h\033[?1049l")
					_ = term.Restore(fd, oldState)
					fmt.Printf("\r\n\033[31m[agyswitch]\033[0m Delete rule file '%s'? (y/N): ", rl.Name)
					var confirm string
					fmt.Scanln(&confirm)
					if strings.EqualFold(strings.TrimSpace(confirm), "y") {
						if err := a.RulesManager.DeleteRule(rl.Name); err == nil {
							a.StatusMsg = fmt.Sprintf("\033[33mDeleted rule file '%s'\033[0m", rl.Name)
						}
					}
					oldState, _ = term.MakeRaw(fd)
					fmt.Print("\033[?1049h\033[?25l")
				}
			}
		case 't', 'T': // Seed account context with template rules & skills
			if a.ActiveTab == 0 && a.SelectedIndex < len(accs) {
				target := accs[a.SelectedIndex].AccountName
				if err := a.Seeder.SeedAccount(target); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError seeding '%s': %v\033[0m", target, err)
				} else {
					a.StatusMsg = fmt.Sprintf("\033[32mSuccessfully seeded '%s' from ~/.gemini_template\033[0m", target)
				}
			}
		case 'x', 'X': // Tiered Account Reset Modal
			if a.ActiveTab == 0 && a.SelectedIndex < len(accs) {
				target := accs[a.SelectedIndex].AccountName
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[33m[agyswitch]\033[0m Select Reset Tier for '\033[1m%s\033[0m':\r\n", target)
				fmt.Print("  [1/a] Auth Wipe (token re-login)\r\n  [2/s] Soft Reset (cache/logs clear)\r\n  [3/h] Hard Purge (delete context)\r\nSelection (1-3 or Esc): ")
				var input string
				fmt.Scanln(&input)
				input = strings.ToLower(strings.TrimSpace(input))
				var mode string
				switch input {
				case "1", "a", "auth":
					mode = "auth"
				case "2", "s", "soft":
					mode = "soft"
				case "3", "h", "hard":
					mode = "hard"
				}
				if mode != "" {
					if err := a.Seeder.ResetAccountEx(target, mode); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mReset error: %v\033[0m", err)
					} else {
						accs = a.Store.ListAccountsFast()
						a.StatusMsg = fmt.Sprintf("\033[32mReset '%s' cleanly (mode: %s)\033[0m", target, mode)
					}
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
			}
		case 'q', 'Q', 0x03:
			fmt.Print("\033[?25h\033[?1049l")
			_ = term.Restore(fd, oldState)
			fmt.Print("\r\n")
			return nil
		}
	}
	return nil
}

func (a *App) Render(accs []model.AccountInfo) {
	width := 100
	if fd := int(os.Stdin.Fd()); term.IsTerminal(fd) {
		if w, _, err := term.GetSize(fd); err == nil && w > 0 {
			width = w
		}
	}

	fmt.Print("\033[H\033[2J") // Clear screen
	if width < 80 {
		fmt.Print("\r\n🛸 \033[1;36mAGYSWITCH [MOBILE SSH]\033[0m\r\n")
		fmt.Print("────────────────────────────────────────\r\n")
		fmt.Printf("Tab: [%d:Tab] Active: \033[1;32m%s\033[0m\r\n", a.ActiveTab+1, a.Store.GetActiveAccount())
		fmt.Print("────────────────────────────────────────\r\n")
	} else {
		fmt.Print("\r\n🛸 \033[1;36mAGYSWITCH - Dedicated Antigravity Control Center (Go Engine v2.0)\033[0m\r\n")
		fmt.Print("──────────────────────────────────────────────────────────────────────────────────────────────────\r\n")

		tabs := []string{"[1] 🔑 Vault & Quota", "[2] 🧩 Skills Hub", "[3] 📜 Rules & MCP", "[4] 📊 Sessions & Cost"}
		for i, t := range tabs {
			if i == a.ActiveTab {
				fmt.Printf(" \033[1;37;44m %s \033[0m ", t)
			} else {
				fmt.Printf(" \033[36m%s\033[0m ", t)
			}
		}
		fmt.Print("\r\n──────────────────────────────────────────────────────────────────────────────────────────────────\r\n")
		activeAcc := a.Store.GetActiveAccount()
		fmt.Printf(" Active Context: \033[1;32m%-20s\033[0m  🌐 \033[36mWeb Sidecar API:\033[0m \033[32mhttp://localhost:8080/api/v1/status\033[0m\r\n\r\n", activeAcc)
	}

	switch a.ActiveTab {
	case 0:
		a.renderVaultTab(accs)
	case 1:
		a.renderSkillsTab()
	case 2:
		a.renderRulesTab()
	case 3:
		a.renderSessionsTab()
	}

	fmt.Print("──────────────────────────────────────────────────────────────────────────────────────────────────\r\n")
	if a.StatusMsg != "" {
		fmt.Printf(" %s\r\n", a.StatusMsg)
	}

	switch a.ActiveTab {
	case 0:
		fmt.Print(" \033[1m[Tab/1-4]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;32m[Enter]\033[0m Switch Acc · \033[1;36m[L]\033[0m Launch agy · \033[1;36m[R]\033[0m Refresh Quota · \033[1;36m[T]\033[0m Seed · \033[1;33m[X]\033[0m Reset · \033[1;36m[N]\033[0m New · \033[1;31m[D]\033[0m Del · \033[1;35m[A]\033[0m Auto · \033[1;31m[Q/Esc]\033[0m Exit\r\n")
	case 1:
		fmt.Print(" \033[1m[Tab/1-4]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;36m[S]\033[0m Sync Skills Across Vaults · \033[1;31m[Q/Esc]\033[0m Exit\r\n")
	case 2:
		fmt.Print(" \033[1m[Tab/1-4]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;36m[V]\033[0m Inspect · \033[1;36m[N]\033[0m New Rule · \033[1;31m[D]\033[0m Del Rule · \033[1;31m[Q/Esc]\033[0m Exit\r\n")
	case 3:
		fmt.Print(" \033[1m[Tab/1-4]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;36m[V]\033[0m View Transcript · \033[1;31m[Q/Esc]\033[0m Exit\r\n")
	}
}

func (a *App) renderVaultTab(accs []model.AccountInfo) {
	for i, acc := range accs {
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;36m> \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		activeMarker := "  "
		if acc.IsActive {
			activeMarker = "\033[1;32m●\033[0m "
		}

		badge := formatStatusBadge(acc)
		inlineQuota := formatInlineQuotaBadge(acc)

		if inlineQuota != "" {
			fmt.Printf("%s%s%s%d. \033[1m%-22s\033[0m (%-26s) %s  %s%s\r\n",
				cursor, highlightStart, activeMarker, i+1, acc.AccountName, acc.Email, badge, inlineQuota, highlightEnd)
		} else {
			fmt.Printf("%s%s%s%d. \033[1m%-22s\033[0m (%-26s) %s%s\r\n",
				cursor, highlightStart, activeMarker, i+1, acc.AccountName, acc.Email, badge, highlightEnd)
		}
	}

	fmt.Print("──────────────────────────────────────────────────────────────────────────────────────────────────\r\n")

	_, recBanner := a.Store.GetRecommendedAccountInfo(accs)
	fmt.Printf(" %s\r\n", recBanner)

	if a.SelectedIndex >= 0 && a.SelectedIndex < len(accs) {
		sel := accs[a.SelectedIndex]
		if sel.IsLoggedIn && sel.QuotaSummary != nil && len(sel.QuotaSummary.Groups) > 0 {
			fmt.Printf("\r\n 📊 \033[1;33mQuota Breakdown (%s):\033[0m\r\n", sel.AccountName)
			for _, g := range sel.QuotaSummary.Groups {
				for _, b := range g.Buckets {
					if b.Window == "weekly" || strings.Contains(b.BucketID, "weekly") {
						pct := b.RemainingFraction * 100.0
						filledLen := int((pct / 100.0) * 30.0)
						if filledLen > 30 {
							filledLen = 30
						}
						if filledLen < 0 {
							filledLen = 0
						}
						bar := strings.Repeat("█", filledLen) + strings.Repeat("░", 30-filledLen)

						colorCode := "\033[32m"
						if pct < 20.0 {
							colorCode = "\033[31m"
						} else if pct < 50.0 {
							colorCode = "\033[33m"
						}

						grpName := "Gemini"
						if strings.Contains(strings.ToLower(g.DisplayName), "claude") {
							grpName = "Claude/GPT"
						}

						fmt.Printf("    • %-11s: [%s%s\033[0m] \033[1m%.1f%%\033[0m\r\n", grpName, colorCode, bar, pct)
					}
				}
			}
		}
	}
}

func (a *App) renderSkillsTab() {
	skillsList, err := a.SkillsManager.DiscoverSkills("")
	if err != nil || len(skillsList) == 0 {
		fmt.Print(" \033[33mNo custom skills discovered in ~/.gemini/skills or .agents/skills\033[0m\r\n")
		return
	}

	fmt.Printf(" 🧩 \033[1;36mInstalled Antigravity Skill Modules (%d total):\033[0m\r\n\r\n", len(skillsList))
	for i, s := range skillsList {
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;36m> \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}
		scope := "\033[32m[Global]\033[0m"
		if !s.IsGlobal {
			scope = "\033[35m[Workspace]\033[0m"
		}
		fmt.Printf("%s%s%d. \033[1m%-20s\033[0m %s  %-50s%s\r\n",
			cursor, highlightStart, i+1, s.Name, scope, s.Description, highlightEnd)
	}
}

func (a *App) renderRulesTab() {
	rulesList, err := a.RulesManager.DiscoverRules("")
	if err == nil && len(rulesList) > 0 {
		fmt.Printf(" 📜 \033[1;36mConfigured Antigravity Customization Rules (%d total):\033[0m\r\n\r\n", len(rulesList))
		for i, r := range rulesList {
			cursor := "  "
			highlightStart := ""
			highlightEnd := ""
			if i == a.SelectedIndex {
				cursor = "\033[1;36m> \033[0m"
				highlightStart = "\033[1;37;44m"
				highlightEnd = "\033[0m"
			}
			scope := "\033[32m[Global]\033[0m"
			if !r.IsGlobal {
				scope = "\033[35m[Workspace]\033[0m"
			}
			fmt.Printf("%s%s%d. \033[1m%-24s\033[0m %s%s\r\n", cursor, highlightStart, i+1, r.Name, scope, highlightEnd)
		}
	} else {
		fmt.Print(" \033[33mNo custom rule files discovered in ~/.gemini/config/rules or .agents/rules\033[0m\r\n")
	}

	fmt.Print("\r\n 🔌 \033[1;36mConfigured MCP Servers (mcp_config.json):\033[0m\r\n\r\n")
	mcps, err := a.RulesManager.CheckMCPServerStatus("")
	if err == nil && len(mcps) > 0 {
		for _, m := range mcps {
			status := "\033[32m● Connected (12ms)\033[0m"
			if !m.IsRunning {
				status = fmt.Sprintf("\033[31m○ Offline (%s)\033[0m", m.LastError)
			}
			fmt.Printf("    • \033[1m%-16s\033[0m [cmd: %-42s] %s\r\n", m.ServerName, m.Command, status)
		}
	} else {
		fmt.Print("    \033[33mNo active MCP servers configured in ~/.gemini/config/mcp_config.json\033[0m\r\n")
	}
}

func (a *App) renderSessionsTab() {
	sessionsList, err := a.SessionManager.DiscoverSessions()
	if err != nil || len(sessionsList) == 0 {
		fmt.Print(" \033[33mNo conversation sessions found in brain logs.\033[0m\r\n")
		return
	}

	var totalSpend float64
	for _, s := range sessionsList {
		totalSpend += s.EstimatedCost
	}

	fmt.Printf(" 📊 \033[1;36mActive Session Trajectories & Token Usage (%d sessions · \033[1;32mTotal Est: $%0.4f\033[1;36m):\033[0m\r\n\r\n", len(sessionsList), totalSpend)
	for i, s := range sessionsList {
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;36m> \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}
		timeStr := s.LastActive.Format("2006-01-02 15:04:05")
		fmt.Printf("%s%s%d. \033[1;37m%-55s\033[0m  \033[35m[%s]\033[0m%s\r\n", cursor, highlightStart, i+1, s.Title, s.WorkspaceDir, highlightEnd)
		fmt.Printf("     ID: \033[36m%s\033[0m · Steps: \033[1;33m%-4d\033[0m · Cost: \033[1;32m$%0.4f\033[0m · Active: %s\r\n\r\n",
			s.ConversationID, s.StepCount, s.EstimatedCost, timeStr)
	}
}

func formatStatusBadge(a model.AccountInfo) string {
	if !a.IsLoggedIn {
		return "\033[31m[✘ Logged Out]\033[0m"
	}
	switch a.QuotaStatus {
	case "✔ Quota OK":
		return fmt.Sprintf("\033[32m[✔ Quota OK · Key: %s]\033[0m", a.TokenSig)
	case "⚡ Auto-Refresh":
		return fmt.Sprintf("\033[33m[⚡ Ready · Key: %s]\033[0m", a.TokenSig)
	case "🔑 Login Required":
		return fmt.Sprintf("\033[33m[🔑 Login Required · Key: %s]\033[0m", a.TokenSig)
	case "✘ Rate Limit":
		return fmt.Sprintf("\033[35m[✘ Rate Limit · Key: %s]\033[0m", a.TokenSig)
	default:
		return fmt.Sprintf("\033[32m[✔ Quota OK · Key: %s]\033[0m", a.TokenSig)
	}
}

func formatInlineQuotaBadge(a model.AccountInfo) string {
	if !a.IsLoggedIn {
		return ""
	}
	if a.GeminiQuotaPct >= 0 && a.ClaudeQuotaPct >= 0 {
		return fmt.Sprintf("\033[36mG: %.1f%%\033[0m · \033[35mC: %.1f%%\033[0m", a.GeminiQuotaPct, a.ClaudeQuotaPct)
	} else if a.GeminiQuotaPct >= 0 {
		return fmt.Sprintf("\033[36mG: %.1f%%\033[0m", a.GeminiQuotaPct)
	}
	return ""
}

func (a *App) runNonInteractive() error {
	a.PrintStatus(os.Stdout)
	return nil
}

func (a *App) PrintStatus(w io.Writer) {
	fmt.Fprintln(w, "\n🛸 \033[1;36mAGYSWITCH - Dedicated Antigravity Control Center (Go Engine v2.0)\033[0m")
	fmt.Fprintln(w, "──────────────────────────────────────────────────────────────────────────────────")
	accs := a.Store.ListAccounts()
	active := a.Store.GetActiveAccount()

	fmt.Fprintf(w, " Active Account: \033[1;32m%s\033[0m\n\n", active)

	for i, acc := range accs {
		activeMarker := "  "
		if acc.IsActive {
			activeMarker = "● "
		}
		badge := formatStatusBadge(acc)
		fmt.Fprintf(w, " %s%d. \033[1m%-22s\033[0m (%-26s) %s\n",
			activeMarker, i+1, acc.AccountName, acc.Email, badge)
	}
	fmt.Fprintln(w, "──────────────────────────────────────────────────────────────────────────────────")
}
