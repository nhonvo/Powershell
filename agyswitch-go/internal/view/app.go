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
	"agyswitch/internal/service/store"
)

type App struct {
	Store          *store.Store
	SkillsManager  *skills.Manager
	RulesManager   *rules.Manager
	SessionManager *sessions.Manager
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
	defer func() { _ = term.Restore(fd, oldState) }()

	accs := a.Store.ListAccounts()
	activeAcc := a.Store.GetActiveAccount()
	for i, acc := range accs {
		if strings.EqualFold(acc.AccountName, activeAcc) {
			a.SelectedIndex = i
			break
		}
	}

	for {
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
					a.SelectedIndex++
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
		case 'k', 'K':
			if a.SelectedIndex > 0 {
				a.SelectedIndex--
			}
		case 'j', 'J':
			a.SelectedIndex++
		case '\r', '\n': // Enter key (Switch active context)
			if a.ActiveTab == 0 && a.SelectedIndex < len(accs) {
				target := accs[a.SelectedIndex].AccountName
				if err := a.Store.SetActiveAccount(target); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError switching account: %v\033[0m", err)
				} else {
					accs = a.Store.ListAccounts()
					a.StatusMsg = fmt.Sprintf("\033[32mSwitched active context to '%s'\033[0m", target)
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
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyswitch]\033[0m Launching 'agy' for account '\033[32m%s\033[0m'...\r\n", target)
				return a.Launcher(target, nil)
			}
		case 'a', 'A': // Auto-select best quota
			if a.ActiveTab == 0 {
				bestAcc := a.Store.SelectBestQuotaAccount()
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyswitch]\033[0m Auto-selected account '\033[32m%s\033[0m'...\r\n", bestAcc)
				return a.Launcher(bestAcc, nil)
			}
		case 'n', 'N': // New Account
			if a.ActiveTab == 0 {
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
			}
		case 'm', 'M': // Rename Account
			if a.ActiveTab == 0 && a.SelectedIndex < len(accs) {
				target := accs[a.SelectedIndex].AccountName
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
			}
		case 'd', 'D': // Delete Account
			if a.ActiveTab == 0 && a.SelectedIndex < len(accs) {
				target := accs[a.SelectedIndex].AccountName
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
			}
		case 'q', 'Q', 0x03:
			_ = term.Restore(fd, oldState)
			fmt.Print("\r\n")
			return nil
		}
	}
	return nil
}

func (a *App) Render(accs []model.AccountInfo) {
	fmt.Print("\033[H\033[2J") // Clear screen
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
	fmt.Printf(" Active Account Context: \033[1;32m%s\033[0m\r\n\r\n", activeAcc)

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
		fmt.Print(" \033[1m[Tab/1-4]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;32m[Enter]\033[0m Switch Acc · \033[1;36m[L]\033[0m Launch agy · \033[1;36m[N]\033[0m New · \033[1;33m[M]\033[0m Rename · \033[1;31m[D]\033[0m Del · \033[1;35m[A]\033[0m Auto · \033[1;31m[Q/Esc]\033[0m Exit\r\n")
	case 1:
		fmt.Print(" \033[1m[Tab/1-4]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;36m[S]\033[0m Sync Skills Across Vaults · \033[1;31m[Q/Esc]\033[0m Exit\r\n")
	case 2:
		fmt.Print(" \033[1m[Tab/1-4]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;36m[E]\033[0m Edit Rule · \033[1;31m[Q/Esc]\033[0m Exit\r\n")
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

	fmt.Print(" 🧩 \033[1;36mInstalled Antigravity Skill Modules:\033[0m\r\n\r\n")
	for i, s := range skillsList {
		cursor := "  "
		if i == a.SelectedIndex {
			cursor = "\033[1;36m> \033[0m"
		}
		scope := "\033[32m[Global]\033[0m"
		if !s.IsGlobal {
			scope = "\033[35m[Workspace]\033[0m"
		}
		fmt.Printf("%s%d. \033[1m%-20s\033[0m %s  %s\r\n", cursor, i+1, s.Name, scope, s.Description)
	}
}

func (a *App) renderRulesTab() {
	rulesList, err := a.RulesManager.DiscoverRules("")
	if err != nil || len(rulesList) == 0 {
		fmt.Print(" \033[33mNo custom rule files discovered in ~/.gemini/config/rules or .agents/rules\033[0m\r\n")
		return
	}

	fmt.Print(" 📜 \033[1;36mConfigured Antigravity Customization Rules:\033[0m\r\n\r\n")
	for i, r := range rulesList {
		cursor := "  "
		if i == a.SelectedIndex {
			cursor = "\033[1;36m> \033[0m"
		}
		scope := "\033[32m[Global]\033[0m"
		if !r.IsGlobal {
			scope = "\033[35m[Workspace]\033[0m"
		}
		fmt.Printf("%s%d. \033[1m%-24s\033[0m %s\r\n", cursor, i+1, r.Name, scope)
	}
}

func (a *App) renderSessionsTab() {
	sessionsList, err := a.SessionManager.DiscoverSessions()
	if err != nil || len(sessionsList) == 0 {
		fmt.Print(" \033[33mNo conversation sessions found in brain logs.\033[0m\r\n")
		return
	}

	fmt.Print(" 📊 \033[1;36mActive Conversation Session Trajectories & Token Usage:\033[0m\r\n\r\n")
	for i, s := range sessionsList {
		cursor := "  "
		if i == a.SelectedIndex {
			cursor = "\033[1;36m> \033[0m"
		}
		timeStr := s.LastActive.Format("2006-01-02 15:04:05")
		fmt.Printf("%s%d. ID: \033[1m%-38s\033[0m Steps: \033[1;33m%-4d\033[0m Last Active: %s\r\n",
			cursor, i+1, s.ConversationID, s.StepCount, timeStr)
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
