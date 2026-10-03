package view

import (
	"fmt"
	"io"
	"os"
	"path/filepath"
	"strings"

	"golang.org/x/term"

	"agyreview/internal/engine"
	"agyreview/internal/model"
	"agyreview/internal/resolver"
)

type CockpitApp struct {
	ActiveTab         int // 0: Repos & Targets, 1: Findings Explorer, 2: Rules & Standards, 3: Watcher Sentinel
	SelectedIndex     int
	Config            *model.AppConfig
	DiscoveredTargets []model.TargetRepo
	ActiveTarget      *model.TargetRepo
	LastFindings      []model.Finding
	LastScore         model.BaremScore
	LastReportPath    string
	StatusMsg         string
	Running           bool
}

func NewCockpitApp(cfg *model.AppConfig) *CockpitApp {
	targets := resolver.DiscoverAllProjects()
	return &CockpitApp{
		ActiveTab:         0,
		Config:            cfg,
		DiscoveredTargets: targets,
	}
}

func (a *CockpitApp) RunInteractive() error {
	fd := int(os.Stdin.Fd())
	if !term.IsTerminal(fd) {
		a.PrintStatus(os.Stdout)
		return nil
	}

	oldState, err := term.MakeRaw(fd)
	if err != nil {
		a.PrintStatus(os.Stdout)
		return nil
	}

	defer func() {
		fmt.Print("\033[?25h\033[?1049l")
		_ = term.Restore(fd, oldState)
	}()

	fmt.Print("\033[?1049h\033[?25l")
	a.Running = true

	for a.Running {
		if len(a.DiscoveredTargets) == 0 {
			a.DiscoveredTargets = resolver.DiscoverAllProjects()
		}

		a.Render()
		a.StatusMsg = ""

		var buf [16]byte
		n, err := os.Stdin.Read(buf[:])
		if err != nil || n == 0 {
			break
		}

		b := buf[0]

		if b == 0x1b { // Escape or arrow key
			if n == 1 {
				// Solitary Esc
				return nil
			}
			if n >= 3 && buf[1] == '[' {
				switch buf[2] {
				case 'A': // Up
					if a.SelectedIndex > 0 {
						a.SelectedIndex--
					}
				case 'B': // Down
					maxIdx := a.getMaxSelectableIndex()
					if a.SelectedIndex < maxIdx {
						a.SelectedIndex++
					}
				case 'C': // Right
					a.ActiveTab = (a.ActiveTab + 1) % 4
					a.SelectedIndex = 0
				case 'D': // Left
					a.ActiveTab = (a.ActiveTab + 3) % 4
					a.SelectedIndex = 0
				}
			}
			continue
		}

		switch b {
		case '\t':
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
		case 'j', 'J':
			maxIdx := a.getMaxSelectableIndex()
			if a.SelectedIndex < maxIdx {
				a.SelectedIndex++
			}
		case 'k', 'K':
			if a.SelectedIndex > 0 {
				a.SelectedIndex--
			}
		case '\r', '\n', 'r', 'R':
			if a.ActiveTab == 0 && len(a.DiscoveredTargets) > 0 {
				if a.SelectedIndex >= len(a.DiscoveredTargets) {
					a.SelectedIndex = len(a.DiscoveredTargets) - 1
				}
				target := a.DiscoveredTargets[a.SelectedIndex]
				a.ActiveTarget = &target

				a.StatusMsg = fmt.Sprintf("\033[36m⏳ Executing 3-Loop Code Review for '%s'...\033[0m", target.Name)
				a.Render()

				result, err := engine.RunReview(&target, false, func(loop int, msg string) {
					a.StatusMsg = fmt.Sprintf("\033[36mLoop %d: %s...\033[0m", loop, msg)
					a.Render()
				})

				if err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31m❌ Review failed: %v\033[0m", err)
				} else {
					a.LastFindings = result.Findings
					a.LastScore = result.Score
					a.LastReportPath = result.ReportPath
					a.ActiveTab = 1 // Switch automatically to Findings Explorer
					a.SelectedIndex = 0
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Review completed! Score: %s (%d pts) · Report: ./%s\033[0m",
						result.Score.LetterGrade, result.Score.TotalPoints, filepath.Clean(result.ReportPath))
				}
			} else {
				a.StatusMsg = "\033[32m✔ Refreshed review telemetry\033[0m"
			}
		case 'q', 'Q', 0x03:
			return nil
		}
	}

	return nil
}

func (a *CockpitApp) getMaxSelectableIndex() int {
	switch a.ActiveTab {
	case 0:
		if len(a.DiscoveredTargets) > 0 {
			return len(a.DiscoveredTargets) - 1
		}
	case 1:
		if len(a.LastFindings) > 0 {
			return len(a.LastFindings) - 1
		}
	}
	return 0
}

func (a *CockpitApp) Render() {
	var b strings.Builder
	b.WriteString("\033[H\033[2J") // Clear screen

	// Header
	b.WriteString("⚡ \033[1;36mAGYREVIEW - Autonomous Multi-Repo Code Reviewer & Sentinel\033[0m\r\n")
	b.WriteString("────────────────────────────────────────────────────────────────────────────────\r\n")

	// Tabs
	tabs := []string{
		"[1] 📁 Repos & Targets",
		"[2] 🔍 Findings Explorer",
		"[3] 📜 Rules & Standards",
		"[4] 👁️ Watcher Sentinel",
	}
	for i, t := range tabs {
		if i == a.ActiveTab {
			b.WriteString(fmt.Sprintf("\033[1;30;46m %s \033[0m ", t))
		} else {
			b.WriteString(fmt.Sprintf("\033[90m %s \033[0m ", t))
		}
	}
	b.WriteString("\r\n────────────────────────────────────────────────────────────────────────────────\r\n\r\n")

	switch a.ActiveTab {
	case 0:
		a.renderReposTab(&b)
	case 1:
		a.renderFindingsTab(&b)
	case 2:
		a.renderRulesTab(&b)
	case 3:
		a.renderWatcherTab(&b)
	}

	b.WriteString("\r\n────────────────────────────────────────────────────────────────────────────────\r\n")
	if a.StatusMsg != "" {
		b.WriteString(fmt.Sprintf(" %s\r\n", a.StatusMsg))
	} else {
		b.WriteString(" \033[90m[Tab/1-4] Switch Tabs  ·  [↑/↓] Select Project  ·  [Enter/R] Start Review  ·  [Q/Esc] Exit\033[0m\r\n")
	}

	fmt.Print(b.String())
}

func (a *CockpitApp) renderReposTab(b *strings.Builder) {
	b.WriteString(" 📁 \033[1mSelect Project Workspace to Start Instant Code Review:\033[0m\r\n\r\n")
	if len(a.DiscoveredTargets) == 0 {
		b.WriteString("   \033[90mNo projects discovered. Register workspaces via agyproj or agybot.\033[0m\r\n\r\n")
		return
	}

	for i, t := range a.DiscoveredTargets {
		prefix := "   "
		badge := "\033[36m"
		if i == a.SelectedIndex {
			prefix = " \033[1;32m▶ \033[0m"
			badge = "\033[1;37;42m"
		}

		b.WriteString(fmt.Sprintf("%s%s [%d] \033[0m \033[1m%-24s\033[0m  Branch: \033[35m%-12s\033[0m  Commit: %s\r\n",
			prefix, badge, i+1, t.Name, t.Branch, t.HeadCommit))
		b.WriteString(fmt.Sprintf("       \033[90mPath: %s\033[0m\r\n", t.Path))
	}
	b.WriteString("\r\n   \033[33mPress [Enter] or [R] to launch 3-Loop Code Review for selected project.\033[0m\r\n")
}

func (a *CockpitApp) renderFindingsTab(b *strings.Builder) {
	b.WriteString(" 🔍 \033[1mActive Review Findings & 100-Point Scorecard:\033[0m\r\n\r\n")
	if a.ActiveTarget != nil {
		b.WriteString(fmt.Sprintf("   \033[1mTarget Workspace:\033[0m \033[36m%s\033[0m (%s)\r\n", a.ActiveTarget.Name, a.ActiveTarget.Path))
	}

	if len(a.LastFindings) == 0 {
		b.WriteString("   \033[90mNo active audit findings. Select a project in Tab 1 and press [Enter/R] to run review.\033[0m\r\n\r\n")
	} else {
		b.WriteString(fmt.Sprintf("   Overall Grade: \033[1;32m%s\033[0m (%d/100 points)  ·  Report: \033[36m%s\033[0m\r\n\r\n",
			a.LastScore.LetterGrade, a.LastScore.TotalPoints, a.LastReportPath))
		for i, f := range a.LastFindings {
			prefix := "   "
			if i == a.SelectedIndex {
				prefix = " \033[1;32m▶\033[0m "
			}
			b.WriteString(fmt.Sprintf("%s%d. %s \033[1m%-18s\033[0m %s (%s:%d)\r\n",
				prefix, i+1, f.Severity.ColorCode(), f.Category, f.FailureExplanation, f.FilePath, f.StartLine))
		}
	}
}

func (a *CockpitApp) renderRulesTab(b *strings.Builder) {
	b.WriteString(" 📜 \033[1mActive Code Review Rules & Quality Standards (AGENTS.md):\033[0m\r\n\r\n")
	b.WriteString("   \033[1;36mSeverity Rating Taxonomy:\033[0m\r\n")
	b.WriteString("     • \033[1;31m[P0 - CRITICAL]\033[0m Security vulnerabilities, arbitrary execution, credential leaks, data loss.\r\n")
	b.WriteString("     • \033[1;33m[P1 - HIGH]\033[0m     Resource leaks, unhandled error panics, broken business logic.\r\n")
	b.WriteString("     • \033[1;35m[P2 - MEDIUM]\033[0m   Code smells, missing validation, missing IPC timeouts.\r\n")
	b.WriteString("     • \033[1;34m[P3 - LOW]\033[0m      Stylistic inconsistencies, non-idiomatic naming conventions.\r\n")
	b.WriteString("     • \033[1;32m[P4 - INFO]\033[0m     Modernization opportunities, micro-optimizations, documentation.\r\n\r\n")
	b.WriteString("   \033[1;36mEvidence-Based Reporting Standard:\033[0m\r\n")
	b.WriteString("     1. File path & line range formatted as workspace links.\r\n")
	b.WriteString("     2. Exact quoted code snippet of offending logic.\r\n")
	b.WriteString("     3. Detailed failure mechanism explanation.\r\n")
	b.WriteString("     4. Ready-to-apply diff patch / concrete code fix.\r\n\r\n")

	// Read active AGENTS.md rule file if present
	home, _ := os.UserHomeDir()
	rulePaths := []string{
		filepath.Join(".", ".agents", "plugins", "code-review", "rules", "AGENTS.md"),
		filepath.Join(home, ".gemini", "config", "rules", "AGENTS.md"),
	}

	for _, p := range rulePaths {
		if data, err := os.ReadFile(p); err == nil {
			b.WriteString(fmt.Sprintf("   \033[1;32m✔ Loaded Markdown Rule Config (%s):\033[0m\r\n", p))
			lines := strings.Split(string(data), "\n")
			if len(lines) > 12 {
				lines = lines[:12]
			}
			for _, l := range lines {
				b.WriteString(fmt.Sprintf("     \033[90m%s\033[0m\r\n", l))
			}
			break
		}
	}
}

func (a *CockpitApp) renderWatcherTab(b *strings.Builder) {
	b.WriteString(" 👁️ \033[1mGit Watcher Sentinel & Background Poller:\033[0m\r\n\r\n")
	b.WriteString(fmt.Sprintf("   • Polling Interval:       \033[1m%d seconds\033[0m\r\n", a.Config.PollIntervalSec))
	b.WriteString(fmt.Sprintf("   • Default Trigger Mode:   \033[1m%s\033[0m\r\n", a.Config.DefaultTrigger))
	b.WriteString(fmt.Sprintf("   • 3-Loop Headless Engine: \033[1m%v\033[0m\r\n", a.Config.ThreeLoops))
	b.WriteString(fmt.Sprintf("   • Swarm Concurrency:      \033[1m%d agents\033[0m\r\n", a.Config.SwarmConcurrency))
	b.WriteString(fmt.Sprintf("   • Deliverable Output Dir: \033[1m%s\033[0m\r\n\r\n", a.Config.ReportDir))
	b.WriteString("   Commands: agyreview watch [start|add|rm|ls]\r\n")
}

func (a *CockpitApp) PrintStatus(w io.Writer) {
	fmt.Fprintln(w, "\n⚡ AGYREVIEW - Autonomous Code Reviewer & Sentinel")
	fmt.Fprintln(w, "─────────────────────────────────────────────────────────────")
	fmt.Fprintf(w, " Discovered Projects: %d\n", len(a.DiscoveredTargets))
	fmt.Fprintf(w, " Poll Interval:       %d seconds\n", a.Config.PollIntervalSec)
	fmt.Fprintf(w, " Default Mode:        %s\n", a.Config.DefaultTrigger)
	fmt.Fprintln(w, "─────────────────────────────────────────────────────────────")
}
