package view

import (
	"fmt"
	"io"
	"os"
	"sort"
	"strings"

	"golang.org/x/term"

	"agyswitch/internal/model"
	"agyswitch/internal/service/rules"
	"agyswitch/internal/service/seeder"
	"agyswitch/internal/service/sessions"
	"agyswitch/internal/service/skills"
	"agyswitch/internal/service/store"
)

type App struct {
	Store          *store.Store
	SkillsManager  *skills.Manager
	RulesManager   *rules.Manager
	SessionManager *sessions.Manager
	Seeder         *seeder.Seeder
	Launcher       func(accountName string, dir string, args []string) error

	ActiveTab            int // 0 = Vault, 1 = Skills, 2 = Rules, 3 = Sessions
	SelectedIndex        int
	StatusMsg            string
	SessionViewMode      int    // 0 = Grouped by Project, 1 = Flat Chronological
	SessionFilterProject string // "" = All Projects, or specific project name
	SessionSortMode      int    // 0 = Newest Time, 1 = Highest Cost, 2 = Most Steps
	SessionScope         int    // 0 = CLI Only (/resume matching) [Default], 1 = All (incl. Subagents)
	tabSwitched          bool

	// In-memory telemetry cache for 60fps keyboard responsiveness
	cachedAccs     []model.AccountInfo
	cachedSkills   []model.SkillInfo
	cachedRules    []model.RuleInfo
	cachedSessions []model.SessionInfo
	needsReload    bool
}

func NewApp(s *store.Store, launcher func(string, string, []string) error) *App {
	userHome := s.UserHome
	return &App{
		Store:                s,
		SkillsManager:        skills.NewManager(userHome),
		RulesManager:         rules.NewManager(userHome),
		SessionManager:       sessions.NewManager(userHome),
		Seeder:               seeder.NewSeeder(userHome, s),
		Launcher:             launcher,
		ActiveTab:            0,
		SelectedIndex:        0,
		SessionViewMode:      0,
		SessionFilterProject: "",
		SessionSortMode:      0,
		SessionScope:         0,
		tabSwitched:          true,
		needsReload:          true,
	}
}

func getWorkspaceDir() string {
	cwd, err := os.Getwd()
	if err != nil {
		return ""
	}
	return cwd
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
	var sessionsList []model.SessionInfo
	var skillsList []model.SkillInfo
	var rulesList []model.RuleInfo
	activeAcc := a.Store.GetActiveAccount()
	for i, acc := range accs {
		if strings.EqualFold(acc.AccountName, activeAcc) {
			a.SelectedIndex = i
			break
		}
	}

	for {
		if a.needsReload || a.cachedAccs == nil {
			a.cachedAccs = a.Store.ListAccountsFast()
		}
		if a.ActiveTab == 1 && (a.needsReload || a.cachedSkills == nil) {
			a.cachedSkills, _ = a.SkillsManager.DiscoverSkills(getWorkspaceDir())
		}
		if a.ActiveTab == 2 && (a.needsReload || a.cachedRules == nil) {
			a.cachedRules, _ = a.RulesManager.DiscoverRules(getWorkspaceDir())
		}
		if a.ActiveTab == 3 && (a.needsReload || a.cachedSessions == nil) {
			a.cachedSessions = a.getPreparedSessions()
		}
		a.needsReload = false

		accs = a.cachedAccs
		skillsList = a.cachedSkills
		rulesList = a.cachedRules
		sessionsList = a.cachedSessions
		totalItems := len(accs)

		switch a.ActiveTab {
		case 0:
			totalItems = len(accs)
		case 1:
			totalItems = len(skillsList)
		case 2:
			totalItems = len(rulesList)
		case 3:
			totalItems = len(sessionsList)
		}

		if a.SelectedIndex >= totalItems && totalItems > 0 {
			a.SelectedIndex = totalItems - 1
		}
		if a.SelectedIndex < 0 {
			a.SelectedIndex = 0
		}

		a.Render(accs, sessionsList)
		a.StatusMsg = ""

		var buf [32]byte
		n, err := os.Stdin.Read(buf[:])
		if err != nil || n == 0 {
			break
		}

		b := buf[0]
		if b == 0x1b {
			if n == 1 {
				// Solitary Esc key pressed -> Exit cleanly!
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Print("\r\n")
				return nil
			}
			if n >= 3 && buf[1] == '[' {
				switch buf[2] {
				case 'A': // Up
					if a.SelectedIndex > 0 {
						a.SelectedIndex--
					}
					continue
				case 'B': // Down
					if a.SelectedIndex < totalItems-1 {
						a.SelectedIndex++
					}
					continue
				case 'C': // Right Tab
					a.ActiveTab = (a.ActiveTab + 1) % 4
					a.SelectedIndex = 0
					a.tabSwitched = true
					a.needsReload = true
					continue
				case 'D': // Left Tab
					a.ActiveTab = (a.ActiveTab + 3) % 4
					a.SelectedIndex = 0
					a.tabSwitched = true
					a.needsReload = true
					continue
				case '5': // PageUp
					pageSize := 8
					a.SelectedIndex -= pageSize
					if a.SelectedIndex < 0 {
						a.SelectedIndex = 0
					}
					continue
				case '6': // PageDown
					pageSize := 8
					a.SelectedIndex += pageSize
					if a.SelectedIndex >= totalItems && totalItems > 0 {
						a.SelectedIndex = totalItems - 1
					}
					continue
				}
			}
			// Ignore any trackpad/mouse scroll escape codes cleanly
			continue
		}

		switch b {
		case '\t': // Tab key switches active tab
			a.ActiveTab = (a.ActiveTab + 1) % 4
			a.SelectedIndex = 0
			a.tabSwitched = true
			a.needsReload = true
		case '1':
			a.ActiveTab = 0
			a.SelectedIndex = 0
			a.tabSwitched = true
			a.needsReload = true
		case '2':
			a.ActiveTab = 1
			a.SelectedIndex = 0
			a.tabSwitched = true
			a.needsReload = true
		case '3':
			a.ActiveTab = 2
			a.SelectedIndex = 0
			a.tabSwitched = true
			a.needsReload = true
		case '4':
			a.ActiveTab = 3
			a.SelectedIndex = 0
			a.tabSwitched = true
			a.needsReload = true
		case 'k', 'K', 'u', 'U':
			if a.SelectedIndex > 0 {
				a.SelectedIndex--
			}
		case 'j', 'J':
			if a.SelectedIndex < totalItems-1 {
				a.SelectedIndex++
			}
		case '\r', '\n', 'c', 'C', 'e', 'E': // Enter / c / e key (Switch active account in Tab 0, Continue session in Tab 3)
			if a.ActiveTab == 0 && a.SelectedIndex < len(accs) {
				target := accs[a.SelectedIndex].AccountName
				if err := a.Store.SetActiveAccount(target); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError switching account: %v\033[0m", err)
				} else {
					accs = a.Store.ListAccountsFast()
					a.StatusMsg = fmt.Sprintf("\033[32mSwitched active context to '%s'\033[0m", target)
				}
			} else if a.ActiveTab == 3 && len(sessionsList) > 0 && a.SelectedIndex < len(sessionsList) {
				sel := sessionsList[a.SelectedIndex]
				curActive := a.Store.GetActiveAccount()
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyswitch]\033[0m Continuing session '\033[32m%s\033[0m' under account '\033[33m%s\033[0m'...\r\n", sel.ConversationID, curActive)
				if sel.WorkspaceDir != "" && sel.WorkspaceDir != "Default Workspace" {
					fmt.Printf(" \033[1mWorkspace:\033[0m \033[35m%s\033[0m\r\n", sel.WorkspaceDir)
				}
				fmt.Printf(" \033[1mTask:\033[0m      %s\r\n\r\n", sel.Title)
				return a.Launcher(curActive, sel.WorkspaceDir, []string{"--conversation", sel.ConversationID})
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
				skillsList, _ := a.SkillsManager.DiscoverSkills(getWorkspaceDir())
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
				rulesList, _ := a.RulesManager.DiscoverRules(getWorkspaceDir())
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
		case 'g', 'G': // Toggle Grouped by Project vs Flat view
			if a.ActiveTab == 3 {
				a.SessionViewMode = (a.SessionViewMode + 1) % 2
				a.SelectedIndex = 0
				a.needsReload = true
				if a.SessionViewMode == 0 {
					a.StatusMsg = "\033[32mSwitched to Project Grouped View\033[0m"
				} else {
					a.StatusMsg = "\033[32mSwitched to Chronological Flat View\033[0m"
				}
			}
		case 'f', 'F': // Cycle Project Filter in Sessions Tab
			if a.ActiveTab == 3 {
				raw, _ := a.SessionManager.DiscoverSessions()
				projectMap := make(map[string]bool)
				var projects []string
				for _, s := range raw {
					p := s.ProjectName
					if p == "" {
						p = "Default Workspace"
					}
					if !projectMap[p] {
						projectMap[p] = true
						projects = append(projects, p)
					}
				}
				options := append([]string{""}, projects...)
				currentIdx := 0
				for i, opt := range options {
					if strings.EqualFold(opt, a.SessionFilterProject) {
						currentIdx = i
						break
					}
				}
				nextIdx := (currentIdx + 1) % len(options)
				a.SessionFilterProject = options[nextIdx]
				a.SelectedIndex = 0
				a.needsReload = true
				if a.SessionFilterProject == "" {
					a.StatusMsg = "\033[32mFilter: All Projects\033[0m"
				} else {
					a.StatusMsg = fmt.Sprintf("\033[32mFilter: %s\033[0m", a.SessionFilterProject)
				}
			}
		case 'o', 'O': // Toggle Sort Order in Sessions Tab
			if a.ActiveTab == 3 {
				a.SessionSortMode = (a.SessionSortMode + 1) % 3
				a.SelectedIndex = 0
				a.needsReload = true
				switch a.SessionSortMode {
				case 1:
					a.StatusMsg = "\033[32mSort: Highest Cost ($$$)\033[0m"
				case 2:
					a.StatusMsg = "\033[32mSort: Most Steps\033[0m"
				default:
					a.StatusMsg = "\033[32mSort: Newest Time\033[0m"
				}
			}
		case 'p', 'P': // Previous Page in Sessions tab
			if a.ActiveTab == 3 {
				pageSize := 8
				a.SelectedIndex -= pageSize
				if a.SelectedIndex < 0 {
					a.SelectedIndex = 0
				}
			}
		case 'l', 'L': // Launch agy
			if a.ActiveTab == 0 && a.SelectedIndex < len(accs) {
				target := accs[a.SelectedIndex].AccountName
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyswitch]\033[0m Launching 'agy' for account '\033[32m%s\033[0m'...\r\n", target)
				return a.Launcher(target, "", nil)
			} else if a.ActiveTab == 3 && len(sessionsList) > 0 && a.SelectedIndex < len(sessionsList) {
				sel := sessionsList[a.SelectedIndex]
				curActive := a.Store.GetActiveAccount()
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyswitch]\033[0m Continuing session '\033[32m%s\033[0m' under account '\033[33m%s\033[0m'...\r\n", sel.ConversationID, curActive)
				if sel.WorkspaceDir != "" && sel.WorkspaceDir != "Default Workspace" {
					fmt.Printf(" \033[1mWorkspace:\033[0m \033[35m%s\033[0m\r\n", sel.WorkspaceDir)
				}
				fmt.Printf(" \033[1mTask:\033[0m      %s\r\n\r\n", sel.Title)
				return a.Launcher(curActive, sel.WorkspaceDir, []string{"--conversation", sel.ConversationID})
			}
		case 'r', 'R': // Probe live quotas on-demand
			if a.ActiveTab == 0 {
				a.StatusMsg = "\033[36mProbing live Google CloudCode quotas...\033[0m"
				a.cachedAccs = a.Store.ListAccounts()
				a.needsReload = true
				a.StatusMsg = "\033[32mSuccessfully updated live quotas.\033[0m"
			} else if a.ActiveTab == 3 {
				a.needsReload = true
				a.StatusMsg = "\033[32mRefreshed sessions telemetry.\033[0m"
			}
		case 'a', 'A': // In Tab 0: Auto-select best quota. In Tab 3: Toggle CLI only vs All incl. Subagents
			if a.ActiveTab == 0 {
				bestAcc := a.Store.SelectBestQuotaAccount()
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agyswitch]\033[0m Auto-selected account '\033[32m%s\033[0m'...\r\n", bestAcc)
				return a.Launcher(bestAcc, "", nil)
			} else if a.ActiveTab == 3 {
				if a.SessionScope == 0 {
					a.SessionScope = 1
					a.StatusMsg = "\033[32mScope: Showing ALL sessions (including subagent tasks)\033[0m"
				} else {
					a.SessionScope = 0
					a.StatusMsg = "\033[32mScope: Showing PRIMARY CLI sessions (matching /resume)\033[0m"
				}
				a.SelectedIndex = 0
				a.cachedSessions = nil
				a.needsReload = true
			}
		case 'n', 'N': // In Tab 3: Next Page. In Tab 0: New Account. In Tab 2: New Rule.
			if a.ActiveTab == 3 {
				pageSize := 8
				a.SelectedIndex += pageSize
				if a.SelectedIndex >= totalItems && totalItems > 0 {
					a.SelectedIndex = totalItems - 1
				}
			} else if a.ActiveTab == 0 {
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Print("\r\n\033[36m[agyswitch]\033[0m Enter new account name: ")
				var newAcc string
				fmt.Scanln(&newAcc)
				newAcc = strings.TrimSpace(newAcc)
				if newAcc != "" {
					if err := a.Store.AddAccount(newAcc); err == nil {
						fmt.Printf("\033[36m[agyswitch]\033[0m Created account context '\033[32m%s\033[0m'. Launching 'agy login'...\r\n", newAcc)
						return a.Launcher(newAcc, "", []string{"login"})
					}
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.needsReload = true
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
				a.needsReload = true
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
		case 'd', 'D': // Delete Item (Account in Tab 0, Rule in Tab 2, Session in Tab 3)
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
			} else if a.ActiveTab == 3 && len(sessionsList) > 0 && a.SelectedIndex < len(sessionsList) {
				sel := sessionsList[a.SelectedIndex]
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[31m[agyswitch]\033[0m Delete conversation session '%s' (%s)? (y/N): ", sel.ConversationID, sel.Title)
				var confirm string
				fmt.Scanln(&confirm)
				if strings.EqualFold(strings.TrimSpace(confirm), "y") {
					if err := a.SessionManager.DeleteSession(sel.ConversationID); err == nil {
						a.StatusMsg = fmt.Sprintf("\033[33mDeleted session '%s'\033[0m", sel.ConversationID)
					} else {
						a.StatusMsg = fmt.Sprintf("\033[31mError deleting session: %v\033[0m", err)
					}
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
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

func getTermSize() (int, int) {
	width := 80
	height := 24
	if fd := int(os.Stdin.Fd()); term.IsTerminal(fd) {
		if w, h, err := term.GetSize(fd); err == nil {
			if w > 0 {
				width = w
			}
			if h > 0 {
				height = h
			}
		}
	}
	return width, height
}

func truncateString(s string, maxLen int) string {
	if maxLen <= 0 {
		return ""
	}
	if maxLen <= 3 {
		if len(s) > maxLen {
			return s[:maxLen]
		}
		return s
	}
	if len(s) > maxLen {
		return s[:maxLen-3] + "..."
	}
	return s
}

func hr(width int) string {
	w := width - 1
	if w < 20 {
		w = 20
	}
	if w > 100 {
		w = 100
	}
	return strings.Repeat("─", w) + "\033[K\r\n"
}

func (a *App) Render(accs []model.AccountInfo, sessionsList []model.SessionInfo) {
	width, height := getTermSize()
	var b strings.Builder
	b.Grow(4096)

	// Clear screen on tab switch, otherwise home cursor in-place
	if a.tabSwitched {
		b.WriteString("\033[H\033[2J")
		a.tabSwitched = false
	} else {
		b.WriteString("\033[H")
	}

	if width < 80 {
		fmt.Fprintf(&b, "\r\n🛸 \033[1;36mAGYSWITCH\033[0m · Active: \033[1;32m%s\033[0m\033[K\r\n", truncateString(a.Store.GetActiveAccount(), 18))
		b.WriteString(hr(width))
		tabNames := []string{"1:Vault", "2:Skills", "3:Rules", "4:Sess"}
		for i, t := range tabNames {
			if i == a.ActiveTab {
				fmt.Fprintf(&b, "\033[1;37;44m [%s] \033[0m ", t)
			} else {
				fmt.Fprintf(&b, "\033[36m[%s]\033[0m ", t)
			}
		}
		b.WriteString("\033[K\r\n" + hr(width))
	} else {
		b.WriteString("\r\n🛸 \033[1;36mAGYSWITCH - Dedicated Antigravity Control Center (Go Engine v2.0)\033[0m\033[K\r\n")
		b.WriteString(hr(width))

		tabs := []string{"[1] 🔑 Vault & Quota", "[2] 🧩 Skills Hub", "[3] 📜 Rules & MCP", "[4] 📊 Sessions & Cost"}
		for i, t := range tabs {
			if i == a.ActiveTab {
				fmt.Fprintf(&b, " \033[1;37;44m %s \033[0m ", t)
			} else {
				fmt.Fprintf(&b, " \033[36m%s\033[0m ", t)
			}
		}
		b.WriteString("\033[K\r\n" + hr(width))
		activeAcc := a.Store.GetActiveAccount()
		fmt.Fprintf(&b, " Active Context: \033[1;32m%-20s\033[0m  🌐 \033[36mWeb Sidecar API:\033[0m \033[32mhttp://localhost:8080/api/v1/status\033[0m\033[K\r\n\033[K\r\n", activeAcc)
	}

	switch a.ActiveTab {
	case 0:
		a.renderVaultTab(&b, accs, width)
	case 1:
		a.renderSkillsTab(&b, width)
	case 2:
		a.renderRulesTab(&b, width)
	case 3:
		a.renderSessionsTab(&b, sessionsList, width, height)
	}

	b.WriteString(hr(width))
	if a.StatusMsg != "" {
		fmt.Fprintf(&b, " %s\033[K\r\n", a.StatusMsg)
	} else {
		b.WriteString("\033[K\r\n")
	}

	if width < 80 {
		switch a.ActiveTab {
		case 0:
			b.WriteString(" \033[1m[Tab]\033[0mTab \033[1m[↑/↓]\033[0mNav \033[1;32m[Enter]\033[0mUse \033[1;36m[L]\033[0mAgy \033[1;36m[R]\033[0mQuota \033[1;31m[Q]\033[0mExit\033[K\r\n")
		case 1:
			b.WriteString(" \033[1m[Tab]\033[0mTab \033[1m[↑/↓]\033[0mNav \033[1;36m[V]\033[0mView \033[1;36m[S]\033[0mSync \033[1;31m[Q]\033[0mExit\033[K\r\n")
		case 2:
			b.WriteString(" \033[1m[Tab]\033[0mTab \033[1m[↑/↓]\033[0mNav \033[1;36m[V]\033[0mView \033[1;36m[N]\033[0mNew \033[1;31m[D]\033[0mDel \033[1;31m[Q]\033[0mExit\033[K\r\n")
		case 3:
			b.WriteString(" \033[1m[Tab]\033[0mTab \033[1m[↑/↓]\033[0mNav \033[1;32m[Enter]\033[0mResume \033[1;36m[A]\033[0mScope \033[1;36m[n/p]\033[0mPg \033[1;36m[G]\033[0mGrp \033[1;31m[Q]\033[0mExit\033[K\r\n")
		}
	} else {
		switch a.ActiveTab {
		case 0:
			b.WriteString(" \033[1m[Tab/1-4]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;32m[Enter]\033[0m Switch Acc · \033[1;36m[L]\033[0m Launch agy · \033[1;36m[R]\033[0m Refresh Quota · \033[1;36m[T]\033[0m Seed · \033[1;33m[X]\033[0m Reset · \033[1;36m[N]\033[0m New · \033[1;31m[D]\033[0m Del · \033[1;35m[A]\033[0m Auto · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		case 1:
			b.WriteString(" \033[1m[Tab/1-4]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;36m[V]\033[0m View Detail · \033[1;36m[S]\033[0m Sync Skills Across Vaults · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		case 2:
			b.WriteString(" \033[1m[Tab/1-4]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;36m[V]\033[0m Inspect · \033[1;36m[N]\033[0m New Rule · \033[1;31m[D]\033[0m Del Rule · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		case 3:
			b.WriteString(" \033[1m[Tab/1-4]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;32m[Enter/C]\033[0m Continue Session · \033[1;36m[A]\033[0m CLI/All · \033[1;36m[n/p]\033[0m Page · \033[1;36m[G]\033[0m Group/Flat · \033[1;36m[F]\033[0m Filter Proj · \033[1;36m[O]\033[0m Sort · \033[1;36m[V]\033[0m Transcript · \033[1;31m[D]\033[0m Del · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		}
	}

	b.WriteString("\033[J") // Clear remainder of screen below
	os.Stdout.WriteString(b.String())
}

func (a *App) renderVaultTab(b *strings.Builder, accs []model.AccountInfo, width int) {
	for i, acc := range accs {
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;32m▶ \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		activeMarker := "  "
		if acc.IsActive {
			activeMarker = "\033[1;32m●\033[0m "
		}

			if width < 80 {
			statusShort := "\033[32m[OK]\033[0m"
			if !acc.IsLoggedIn {
				statusShort = "\033[31m[Out]\033[0m"
			}
			quotaShort := ""
			if acc.GeminiQuotaPct >= 0 {
				quotaShort = fmt.Sprintf(" G:%.0f%%", acc.GeminiQuotaPct)
			}
			if acc.ClaudeQuotaPct >= 0 {
				quotaShort += fmt.Sprintf(" C:%.0f%%", acc.ClaudeQuotaPct)
			}
			name := truncateString(acc.AccountName, 18)
			fmt.Fprintf(b, "%s%s%s%d. \033[1m%-18s\033[0m %s%s%s\033[K\r\n",
				cursor, highlightStart, activeMarker, i+1, name, statusShort, quotaShort, highlightEnd)
		} else {
			badge := formatStatusBadge(acc)
			inlineQuota := formatInlineQuotaBadge(acc)
			if inlineQuota != "" {
				fmt.Fprintf(b, "%s%s%s%d. \033[1m%-22s\033[0m (%-26s) %s  %s%s\033[K\r\n",
					cursor, highlightStart, activeMarker, i+1, acc.AccountName, acc.Email, badge, inlineQuota, highlightEnd)
			} else {
				fmt.Fprintf(b, "%s%s%s%d. \033[1m%-22s\033[0m (%-26s) %s%s\033[K\r\n",
					cursor, highlightStart, activeMarker, i+1, acc.AccountName, acc.Email, badge, highlightEnd)
			}
		}
	}

	b.WriteString(hr(width))

	_, recBanner := a.Store.GetRecommendedAccountInfo(accs)
	if width < 80 {
		recBanner = truncateString(recBanner, width-2)
	}
	fmt.Fprintf(b, " %s\033[K\r\n", recBanner)

	if a.SelectedIndex >= 0 && a.SelectedIndex < len(accs) {
		sel := accs[a.SelectedIndex]
		if sel.IsLoggedIn && sel.QuotaSummary != nil && len(sel.QuotaSummary.Groups) > 0 {
			fmt.Fprintf(b, "\033[K\r\n 📊 \033[1;33mQuota Breakdown (%s):\033[0m\033[K\r\n", sel.AccountName)
			barMax := 30
			if width < 80 {
				barMax = 14
			}
			for _, g := range sel.QuotaSummary.Groups {
				for _, bk := range g.Buckets {
					if bk.Window == "weekly" || strings.Contains(bk.BucketID, "weekly") {
						pct := bk.RemainingFraction * 100.0
						filledLen := int((pct / 100.0) * float64(barMax))
						if filledLen > barMax {
							filledLen = barMax
						}
						if filledLen < 0 {
							filledLen = 0
						}
						bar := strings.Repeat("█", filledLen) + strings.Repeat("░", barMax-filledLen)

						colorCode := "\033[32m"
						if pct < 20.0 {
							colorCode = "\033[31m"
						} else if pct < 50.0 {
							colorCode = "\033[33m"
						}

						grpName := "Gemini"
						if strings.Contains(strings.ToLower(g.DisplayName), "claude") {
							grpName = "Claude"
						}

						fmt.Fprintf(b, "    • %-7s: [%s%s\033[0m] \033[1m%.1f%%\033[0m\033[K\r\n", grpName, colorCode, bar, pct)
					}
				}
			}
		}
	}
}

func (a *App) renderSkillsTab(b *strings.Builder, width int) {
	skillsList := a.cachedSkills
	if skillsList == nil {
		skillsList, _ = a.SkillsManager.DiscoverSkills(getWorkspaceDir())
	}
	if len(skillsList) == 0 {
		b.WriteString(" \033[33mNo custom skills discovered in ~/.gemini/skills or .agents/skills\033[0m\033[K\r\n")
		return
	}

	fmt.Fprintf(b, " 🧩 \033[1;36mSkills (%d total):\033[0m\033[K\r\n\033[K\r\n", len(skillsList))
	for i, s := range skillsList {
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;32m▶ \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}
		scope := "\033[32m[G]\033[0m"
		if !s.IsGlobal {
			scope = "\033[35m[W]\033[0m"
		}
		if width < 80 {
			name := truncateString(s.Name, width-16)
			fmt.Fprintf(b, "%s%s%d. %s \033[1m%s\033[0m%s\033[K\r\n", cursor, highlightStart, i+1, scope, name, highlightEnd)
			if i == a.SelectedIndex {
				desc := truncateString(s.Description, width-6)
				fmt.Fprintf(b, "      \033[37m%s\033[0m\033[K\r\n", desc)
			}
		} else {
			desc := truncateString(s.Description, width-36)
			fmt.Fprintf(b, "%s%s%d. \033[1m%-20s\033[0m %s  %s%s\033[K\r\n",
				cursor, highlightStart, i+1, s.Name, scope, desc, highlightEnd)
		}
	}
}

func (a *App) renderRulesTab(b *strings.Builder, width int) {
	rulesList := a.cachedRules
	if rulesList == nil {
		rulesList, _ = a.RulesManager.DiscoverRules(getWorkspaceDir())
	}
	if len(rulesList) > 0 {
		fmt.Fprintf(b, " 📜 \033[1;36mRules (%d total):\033[0m\033[K\r\n", len(rulesList))
		for i, r := range rulesList {
			cursor := "  "
			highlightStart := ""
			highlightEnd := ""
			if i == a.SelectedIndex {
				cursor = "\033[1;32m▶ \033[0m"
				highlightStart = "\033[1;37;44m"
				highlightEnd = "\033[0m"
			}
			scope := "\033[32m[G]\033[0m"
			if !r.IsGlobal {
				scope = "\033[35m[W]\033[0m"
			}
			name := truncateString(r.Name, width-16)
			fmt.Fprintf(b, "%s%s%d. %s \033[1m%s\033[0m%s\033[K\r\n", cursor, highlightStart, i+1, scope, name, highlightEnd)
		}
	} else {
		b.WriteString(" \033[33mNo custom rule files discovered.\033[0m\033[K\r\n")
	}

	b.WriteString("\033[K\r\n 🔌 \033[1;36mMCP Servers:\033[0m\033[K\r\n")
	mcps, err := a.RulesManager.CheckMCPServerStatus(getWorkspaceDir())
	if err == nil && len(mcps) > 0 {
		for _, m := range mcps {
			status := "\033[32m● Connected (12ms)\033[0m"
			if !m.IsRunning {
				status = fmt.Sprintf("\033[31m○ Off (%s)\033[0m", truncateString(m.LastError, 12))
			}
			name := truncateString(m.ServerName, 18)
			if width < 80 {
				fmt.Fprintf(b, "    • \033[1m%-16s\033[0m %s\033[K\r\n", name, status)
			} else {
				cmd := truncateString(m.Command, 36)
				fmt.Fprintf(b, "    • \033[1m%-16s\033[0m [cmd: %-36s] %s\033[K\r\n", name, cmd, status)
			}
		}
	} else {
		b.WriteString("    \033[33mNo active MCP servers configured.\033[0m\033[K\r\n")
	}
}

func (a *App) getPreparedSessions() []model.SessionInfo {
	var raw []model.SessionInfo
	var err error
	if a.SessionScope == 0 {
		raw, err = a.SessionManager.DiscoverPrimarySessions()
	} else {
		raw, err = a.SessionManager.DiscoverAllSessions()
	}
	if err != nil || len(raw) == 0 {
		raw, _ = a.SessionManager.DiscoverSessions()
	}
	if len(raw) == 0 {
		return nil
	}

	// 1. Filter by project if set
	var filtered []model.SessionInfo
	if a.SessionFilterProject != "" {
		for _, s := range raw {
			if strings.EqualFold(s.ProjectName, a.SessionFilterProject) {
				filtered = append(filtered, s)
			}
		}
	} else {
		filtered = raw
	}

	// 2. Sort the sessions according to SessionSortMode
	switch a.SessionSortMode {
	case 1: // Highest Cost
		sort.Slice(filtered, func(i, j int) bool {
			return filtered[i].EstimatedCost > filtered[j].EstimatedCost
		})
	case 2: // Most Steps
		sort.Slice(filtered, func(i, j int) bool {
			return filtered[i].StepCount > filtered[j].StepCount
		})
	default: // 0: Newest Time
		sort.Slice(filtered, func(i, j int) bool {
			return filtered[i].LastActive.After(filtered[j].LastActive)
		})
	}

	// 3. View mode: Grouped vs Flat
	if a.SessionViewMode == 0 {
		// Grouped mode: group by project, sort groups, and flatten so each project's sessions are contiguous
		groups := sessions.GroupSessionsByProjectSorted(filtered, a.SessionSortMode)
		return sessions.FlattenProjectGroups(groups)
	}

	return filtered
}

func (a *App) renderSessionsTab(b *strings.Builder, sessionsList []model.SessionInfo, width int, height int) {
	if len(sessionsList) == 0 {
		b.WriteString(" \033[33mNo conversation sessions match the current criteria.\033[0m\r\n")
		return
	}

	var totalSpend float64
	var totalSteps int
	for _, s := range sessionsList {
		totalSpend += s.EstimatedCost
		totalSteps += s.StepCount
	}

	var sortLabel string
	switch a.SessionSortMode {
	case 1:
		sortLabel = "Cost"
	case 2:
		sortLabel = "Steps"
	default:
		sortLabel = "Newest"
	}

	modeStr := "Grouped"
	if a.SessionViewMode == 1 {
		modeStr = "Flat"
	}

	scopeStr := "CLI /resume"
	if a.SessionScope == 1 {
		scopeStr = "All incl. Subagents"
	}

	if width < 80 {
		fmt.Fprintf(b, " 📊 \033[1;36mSessions\033[0m (%d sess · \033[1;32m$%0.2f\033[0m)\033[K\r\n", len(sessionsList), totalSpend)
		filterStr := "All"
		if a.SessionFilterProject != "" {
			filterStr = a.SessionFilterProject
		}
		fmt.Fprintf(b, "   \033[33m[%s · %s · %s · %s]\033[0m\033[K\r\n", modeStr, sortLabel, scopeStr, truncateString(filterStr, 14))
	} else {
		filterBadge := ""
		if a.SessionFilterProject != "" {
			filterBadge = fmt.Sprintf(" · \033[33mFilter: %s\033[0m", a.SessionFilterProject)
		}
		fmt.Fprintf(b, " 📊 \033[1;36mAntigravity Sessions (%d total · %d steps · \033[1;32mEst: $%0.4f\033[1;36m) · [%s · %s · \033[1;33m%s\033[1;36m]%s:\033[0m\033[K\r\n\033[K\r\n",
			len(sessionsList), totalSteps, totalSpend, modeStr, sortLabel, scopeStr, filterBadge)
	}

	// Dynamic page size based on available terminal height
	pageSize := 7
	if height < 20 {
		pageSize = 4
	} else if height < 26 {
		pageSize = 5
	} else if height >= 32 {
		pageSize = 9
	}

	// Fixed page pagination: keeps view stationary when moving cursor within the page
	page := a.SelectedIndex / pageSize
	startIdx := page * pageSize
	endIdx := startIdx + pageSize
	if endIdx > len(sessionsList) {
		endIdx = len(sessionsList)
	}
	totalPages := (len(sessionsList) + pageSize - 1) / pageSize
	if totalPages == 0 {
		totalPages = 1
	}

	if a.SessionViewMode == 0 {
		// Grouped by project view (sessionsList is already ordered project-by-project)
		groups := sessions.GroupSessionsByProjectSorted(sessionsList, a.SessionSortMode)
		currentProj := ""

		for i := startIdx; i < endIdx; i++ {
			s := sessionsList[i]
			proj := s.ProjectName
			if proj == "" {
				proj = "Default Workspace"
			}

			if i == startIdx || proj != currentProj {
				currentProj = proj
				var count int
				var cost float64
				for _, g := range groups {
					if g.ProjectName == proj {
						count = len(g.Sessions)
						cost = g.TotalCost
						break
					}
				}
				if width < 80 {
					pName := truncateString(proj, width-18)
					fmt.Fprintf(b, " \033[1;35m📁 %s\033[0m \033[36m(%d · $%0.2f)\033[0m\033[K\r\n", pName, count, cost)
				} else {
					fmt.Fprintf(b, " \033[1;35m📁 %-32s\033[0m \033[36m(%d sessions · $%0.4f)\033[0m\033[K\r\n", proj, count, cost)
				}
			}

			cursor := "    "
			highlightStart := ""
			highlightEnd := ""
			if i == a.SelectedIndex {
				cursor = "  \033[1;32m▶ \033[0m"
				highlightStart = "\033[1;37;44m"
				highlightEnd = "\033[0m"
			}
			timeStr := s.LastActive.Format("01-02 15:04")

			if width < 80 {
				title := truncateString(s.Title, width-12)
				fmt.Fprintf(b, "%s%s%2d. \033[1;37m%s\033[0m%s\033[K\r\n", cursor, highlightStart, i+1, title, highlightEnd)
				fmt.Fprintf(b, "        \033[33m%d st\033[0m · \033[32m$%0.4f\033[0m · \033[36m%s\033[0m\033[K\r\n", s.StepCount, s.EstimatedCost, timeStr)
				if i == a.SelectedIndex {
					fmt.Fprintf(b, "        \033[36mID: %s\033[0m\033[K\r\n", truncateString(s.ConversationID, width-14))
				}
			} else {
				availTitle := width - 42
				if availTitle < 20 {
					availTitle = 20
				}
				title := truncateString(s.Title, availTitle)
				fmt.Fprintf(b, "%s%s%2d. \033[1;37m%-48s\033[0m  \033[33m%3d st\033[0m · \033[32m$%0.4f\033[0m · \033[36m%s\033[0m%s\033[K\r\n",
					cursor, highlightStart, i+1, title, s.StepCount, s.EstimatedCost, timeStr, highlightEnd)
				if i == a.SelectedIndex {
					wsShort := truncateString(s.WorkspaceDir, width-52)
					fmt.Fprintf(b, "        \033[36mID: %s\033[0m · \033[35mWorkspace: %s\033[0m\033[K\r\n", s.ConversationID, wsShort)
				}
			}
		}
	} else {
		// Flat view
		for i := startIdx; i < endIdx; i++ {
			s := sessionsList[i]
			cursor := "  "
			highlightStart := ""
			highlightEnd := ""
			if i == a.SelectedIndex {
				cursor = "\033[1;32m▶ \033[0m"
				highlightStart = "\033[1;37;44m"
				highlightEnd = "\033[0m"
			}
			timeStr := s.LastActive.Format("01-02 15:04")
			if width < 80 {
				title := truncateString(s.Title, width-10)
				fmt.Fprintf(b, "%s%s%2d. \033[1;37m%s\033[0m%s\033[K\r\n", cursor, highlightStart, i+1, title, highlightEnd)
				fmt.Fprintf(b, "     \033[33m%d st\033[0m · \033[32m$%0.4f\033[0m · \033[35m[%s]\033[0m\033[K\r\n",
					s.StepCount, s.EstimatedCost, truncateString(s.ProjectName, 16))
			} else {
				availTitle := width - 42
				if availTitle < 20 {
					availTitle = 20
				}
				title := truncateString(s.Title, availTitle)
				fmt.Fprintf(b, "%s%s%2d. \033[1;37m%-48s\033[0m  \033[35m[%s]\033[0m%s\033[K\r\n",
					cursor, highlightStart, i+1, title, truncateString(s.ProjectName, 18), highlightEnd)
				fmt.Fprintf(b, "     ID: \033[36m%s\033[0m · Steps: \033[1;33m%-4d\033[0m · Cost: \033[1;32m$%0.4f\033[0m · Active: %s\033[K\r\n",
					s.ConversationID, s.StepCount, s.EstimatedCost, timeStr)
			}
		}
	}

	fmt.Fprintf(b, "\033[K\r\n \033[37m[Page %d/%d · %d-%d of %d sess · #%d · Enter to Resume]\033[0m\033[K\r\n",
		page+1, totalPages, startIdx+1, endIdx, len(sessionsList), a.SelectedIndex+1)
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
