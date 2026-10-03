package view

import (
	"fmt"
	"io"
	"os"
	"strings"

	"golang.org/x/term"

	"agyreview/internal/model"
)

type CockpitApp struct {
	ActiveTab    int // 0: Repos & Targets, 1: Findings Explorer, 2: Watcher Daemon
	SelectedIndex int
	Config       *model.AppConfig
	LastFindings []model.Finding
	LastScore    model.BaremScore
	StatusMsg    string
	Running      bool
}

func NewCockpitApp(cfg *model.AppConfig) *CockpitApp {
	return &CockpitApp{
		ActiveTab: 0,
		Config:    cfg,
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
					a.SelectedIndex++
				case 'C': // Right
					a.ActiveTab = (a.ActiveTab + 1) % 3
					a.SelectedIndex = 0
				case 'D': // Left
					a.ActiveTab = (a.ActiveTab + 2) % 3
					a.SelectedIndex = 0
				}
			}
			continue
		}

		switch b {
		case '\t':
			a.ActiveTab = (a.ActiveTab + 1) % 3
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
		case 'j', 'J':
			a.SelectedIndex++
		case 'k', 'K':
			if a.SelectedIndex > 0 {
				a.SelectedIndex--
			}
		case 'r', 'R':
			a.StatusMsg = "\033[32m✔ Triggered audit refresh\033[0m"
		case 'q', 'Q', 0x03:
			return nil
		}
	}

	return nil
}

func (a *CockpitApp) Render() {
	var b strings.Builder
	b.WriteString("\033[H\033[2J") // Clear screen

	// Header
	b.WriteString("⚡ \033[1;36mAGYREVIEW - Autonomous Multi-Repo Code Reviewer & Sentinel\033[0m\r\n")
	b.WriteString("────────────────────────────────────────────────────────────────────────────────\r\n")

	// Tabs
	tabs := []string{"[1] 📁 Targets & Repos", "[2] 🔍 Findings Explorer", "[3] 👁️ Watcher Sentinel"}
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
		a.renderWatcherTab(&b)
	}

	b.WriteString("\r\n────────────────────────────────────────────────────────────────────────────────\r\n")
	if a.StatusMsg != "" {
		b.WriteString(fmt.Sprintf(" %s\r\n", a.StatusMsg))
	} else {
		b.WriteString(" \033[90m[Tab/1-3] Switch Tabs  ·  [↑/↓] Navigate  ·  [R] Run Review  ·  [Q/Esc] Exit\033[0m\r\n")
	}

	fmt.Print(b.String())
}

func (a *CockpitApp) renderReposTab(b *strings.Builder) {
	b.WriteString(" 📁 \033[1mConfigured & Watched Repositories:\033[0m\r\n\r\n")
	if len(a.Config.WatchedRepos) == 0 {
		b.WriteString("   \033[90mNo repositories configured in watch list.\033[0m\r\n")
		b.WriteString("   Add repositories via: \033[1magyreview watch add <path>\033[0m\r\n\r\n")
	} else {
		for i, r := range a.Config.WatchedRepos {
			prefix := "   "
			if i == a.SelectedIndex {
				prefix = " \033[1;32m▶\033[0m "
			}
			b.WriteString(fmt.Sprintf("%s\033[1m%-25s\033[0m mode: \033[36m%-7s\033[0m branch: %-15s commit: %s\r\n",
				prefix, r.Path, r.TriggerMode, r.LastBranch, r.LastCommit))
		}
	}
}

func (a *CockpitApp) renderFindingsTab(b *strings.Builder) {
	b.WriteString(" 🔍 \033[1mActive Review Findings & 100-Point Scorecard:\033[0m\r\n\r\n")
	if len(a.LastFindings) == 0 {
		b.WriteString("   \033[90mNo active audit loaded. Press [R] or run 'agyreview run <path>' to execute an audit.\033[0m\r\n\r\n")
	} else {
		b.WriteString(fmt.Sprintf("   Overall Grade: \033[1;32m%s\033[0m (%d/100 points)\r\n\r\n", a.LastScore.LetterGrade, a.LastScore.TotalPoints))
		for i, f := range a.LastFindings {
			b.WriteString(fmt.Sprintf("   %d. %s \033[1m%s\033[0m: %s (%s:%d)\r\n",
				i+1, f.Severity.ColorCode(), f.Category, f.FailureExplanation, f.FilePath, f.StartLine))
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
	fmt.Fprintf(w, " Watched Repos: %d\n", len(a.Config.WatchedRepos))
	fmt.Fprintf(w, " Poll Interval: %d seconds\n", a.Config.PollIntervalSec)
	fmt.Fprintf(w, " Default Mode:  %s\n", a.Config.DefaultTrigger)
	fmt.Fprintln(w, "─────────────────────────────────────────────────────────────")
}
