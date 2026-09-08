package view

import (
	"fmt"
	"io"
	"os"
	"strings"
	"sync"

	"golang.org/x/term"

	"agyollama/internal/model"
	"agyollama/internal/service/ollamaops"
)

var spinnerFrames = []string{"⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"}

// App represents the interactive TUI application.
type App struct {
	client        *ollamaops.Client
	ActiveTab     int // 0: Status, 1: Models, 2: Benchmark, 3: Logs
	SelectedIndex int
	StatusMsg     string
	tabSwitched   bool
	spinnerIdx    int
	needsReload   bool

	// Cached state
	daemonStatus  *model.DaemonStatus
	models        []model.ModelInfo
	benchmarkList []model.BenchmarkResult
	logsContent   string

	// Active background action & progress
	activeAction  string // "starting", "pulling", "benchmarking"
	actionMsg     string
	pullProgress  *model.PullProgress
	pullModelName string
	actionMu      sync.RWMutex

	// Modals
	viewDetailModal *model.ModelDetail
	viewDetailName  string
	confirmDelete   string
}

// NewApp creates a new App instance.
func NewApp() *App {
	return NewAppWithClient(ollamaops.DefaultClient)
}

// NewAppWithClient creates an App instance with a specified Client.
func NewAppWithClient(client *ollamaops.Client) *App {
	return &App{
		client:        client,
		ActiveTab:     0,
		tabSwitched:   true,
		needsReload:   true,
		benchmarkList: make([]model.BenchmarkResult, 0),
	}
}

func (a *App) getActiveAction() string {
	a.actionMu.RLock()
	defer a.actionMu.RUnlock()
	return a.activeAction
}

func (a *App) setActiveAction(action, msg string) {
	a.actionMu.Lock()
	defer a.actionMu.Unlock()
	a.activeAction = action
	a.actionMsg = msg
}

func (a *App) hasActiveAction() bool {
	a.actionMu.RLock()
	defer a.actionMu.RUnlock()
	return a.activeAction != ""
}

// RunInteractive enters raw terminal mode and runs the interactive ANSI loop.
func (a *App) RunInteractive() error {
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

	// Switch to alternate screen and hide cursor
	fmt.Print("\033[?1049h\033[?25l\033[H\033[2J")

	for {
		if a.needsReload {
			a.refreshData()
			a.needsReload = false
		}

		totalItems := 1
		if a.ActiveTab == 1 {
			totalItems = len(a.models)
		} else if a.ActiveTab == 2 {
			totalItems = len(a.benchmarkList)
		}

		if a.SelectedIndex >= totalItems && totalItems > 0 {
			a.SelectedIndex = totalItems - 1
		}
		if a.SelectedIndex < 0 {
			a.SelectedIndex = 0
		}

		a.Render()

		// Poll keyboard input up to 80ms (non-blocking animated spinner loop)
		ready := waitKey(fd, 80)
		if !ready {
			if a.hasActiveAction() {
				a.spinnerIdx = (a.spinnerIdx + 1) % len(spinnerFrames)
			}
			continue
		}

		var buf [32]byte
		n, err := os.Stdin.Read(buf[:])
		if err != nil || n == 0 {
			break
		}

		a.StatusMsg = ""
		b := buf[0]

		// Handle Modal inputs first
		if a.viewDetailModal != nil {
			if b == 0x1b || b == 'q' || b == 'Q' || b == '\r' || b == '\n' || b == 'v' || b == 'V' {
				a.viewDetailModal = nil
				a.viewDetailName = ""
				a.tabSwitched = true
			}
			continue
		}

		if a.confirmDelete != "" {
			if b == 'y' || b == 'Y' {
				target := a.confirmDelete
				a.confirmDelete = ""
				a.tabSwitched = true
				a.StatusMsg = fmt.Sprintf("\033[33mDeleting model '%s'...\033[0m", target)
				go func(modelName string) {
					err := a.client.DeleteModel(modelName)
					a.actionMu.Lock()
					a.needsReload = true
					if err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mFailed to delete %s: %v\033[0m", modelName, err)
					} else {
						a.StatusMsg = fmt.Sprintf("\033[32mModel '%s' deleted successfully.\033[0m", modelName)
					}
					a.actionMu.Unlock()
				}(target)
			} else {
				a.confirmDelete = ""
				a.tabSwitched = true
				a.StatusMsg = "\033[33mDeletion canceled.\033[0m"
			}
			continue
		}

		// Escape sequences
		if b == 0x1b {
			if n == 1 {
				// Solitary Esc -> Exit cleanly
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
				case 'C': // Right tab
					a.ActiveTab = (a.ActiveTab + 1) % 4
					a.SelectedIndex = 0
					a.tabSwitched = true
					continue
				case 'D': // Left tab
					a.ActiveTab = (a.ActiveTab + 3) % 4
					a.SelectedIndex = 0
					a.tabSwitched = true
					continue
				}
			}
			continue
		}

		switch b {
		case 'q', 'Q':
			return nil
		case '\t':
			a.ActiveTab = (a.ActiveTab + 1) % 4
			a.SelectedIndex = 0
			a.tabSwitched = true
		case '1':
			a.ActiveTab = 0
			a.SelectedIndex = 0
			a.tabSwitched = true
		case '2':
			a.ActiveTab = 1
			a.SelectedIndex = 0
			a.tabSwitched = true
		case '3':
			a.ActiveTab = 2
			a.SelectedIndex = 0
			a.tabSwitched = true
		case '4':
			a.ActiveTab = 3
			a.SelectedIndex = 0
			a.tabSwitched = true

		case 'k', 'K':
			if a.SelectedIndex > 0 {
				a.SelectedIndex--
			}
		case 'j', 'J':
			if a.SelectedIndex < totalItems-1 {
				a.SelectedIndex++
			}

		case 's', 'S': // Start daemon (on Tab 0) or Refresh
			if a.ActiveTab == 0 && (a.daemonStatus == nil || !a.daemonStatus.IsOnline) {
				a.setActiveAction("starting", "Starting Ollama daemon in background...")
				go func() {
					err := a.client.StartDaemon()
					a.setActiveAction("", "")
					a.actionMu.Lock()
					a.needsReload = true
					if err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mDaemon start failed: %v\033[0m", err)
					} else {
						a.StatusMsg = "\033[32m✔ Ollama daemon started successfully!\033[0m"
					}
					a.actionMu.Unlock()
				}()
			} else {
				a.needsReload = true
				a.tabSwitched = true
				a.StatusMsg = "\033[32m✔ Refreshed.\033[0m"
			}

		case 'x', 'X': // Stop daemon (on Tab 0)
			if a.ActiveTab == 0 && a.daemonStatus != nil && a.daemonStatus.IsOnline {
				a.setActiveAction("stopping", "Stopping Ollama daemon...")
				go func() {
					err := a.client.StopDaemon()
					a.setActiveAction("", "")
					a.actionMu.Lock()
					a.needsReload = true
					if err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mDaemon stop failed: %v\033[0m", err)
					} else {
						a.StatusMsg = "\033[32m✔ Ollama daemon stopped successfully.\033[0m"
					}
					a.actionMu.Unlock()
				}()
			}

		case 'r', 'R': // Run interactive chat
			var targetModel string
			if a.ActiveTab == 1 && len(a.models) > 0 && a.SelectedIndex < len(a.models) {
				targetModel = a.models[a.SelectedIndex].Name
			} else {
				targetModel = a.client.GetDefaultModel()
			}

			// Restore terminal to cooked mode
			fmt.Print("\033[?25h\033[?1049l")
			_ = term.Restore(fd, oldState)
			fmt.Printf("\r\n\033[1;36m🤖 [agyollama]\033[0m Connecting to model '\033[1;32m%s\033[0m'...\r\n\r\n", targetModel)

			_ = a.client.RunInteractive(targetModel)

			// Restore raw terminal mode
			oldState, _ = term.MakeRaw(fd)
			fmt.Print("\033[?1049h\033[?25l")
			a.tabSwitched = true
			a.needsReload = true

		case '\r', '\n': // Enter: set default model
			if a.ActiveTab == 1 && len(a.models) > 0 && a.SelectedIndex < len(a.models) {
				target := a.models[a.SelectedIndex].Name
				if err := a.client.SetDefaultModel(target); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mError setting default model: %v\033[0m", err)
				} else {
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Set '%s' as active default model.\033[0m", target)
					a.needsReload = true
				}
			}

		case 'v', 'V': // View model details
			if a.ActiveTab == 1 && len(a.models) > 0 && a.SelectedIndex < len(a.models) {
				target := a.models[a.SelectedIndex].Name
				detail, err := a.client.ShowModel(target)
				if err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mFailed to load details for %s: %v\033[0m", target, err)
				} else {
					a.viewDetailModal = detail
					a.viewDetailName = target
					a.tabSwitched = true
				}
			}

		case 'd', 'D': // Delete model confirmation
			if a.ActiveTab == 1 && len(a.models) > 0 && a.SelectedIndex < len(a.models) {
				a.confirmDelete = a.models[a.SelectedIndex].Name
				a.tabSwitched = true
			}

		case 'p', 'P': // Pull new model
			fmt.Print("\033[?25h\033[?1049l")
			_ = term.Restore(fd, oldState)
			fmt.Print("\r\n\033[1;36m📦 [agyollama]\033[0m Enter model name to pull (e.g. qwen2.5-coder:7b, llama3.2:3b): ")

			var inputName string
			_, _ = fmt.Scanln(&inputName)
			inputName = strings.TrimSpace(inputName)

			oldState, _ = term.MakeRaw(fd)
			fmt.Print("\033[?1049h\033[?25l")
			a.tabSwitched = true

			if inputName == "" {
				a.StatusMsg = "\033[33mPull cancelled: empty model name.\033[0m"
				continue
			}

			a.pullModelName = inputName
			a.setActiveAction("pulling", fmt.Sprintf("Pulling %s...", inputName))
			go func(name string) {
				err := a.client.PullModel(name, func(p model.PullProgress) {
					a.actionMu.Lock()
					a.pullProgress = &p
					a.actionMu.Unlock()
				})
				a.setActiveAction("", "")
				a.actionMu.Lock()
				a.pullProgress = nil
				a.needsReload = true
				if err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mPull error (%s): %v\033[0m", name, err)
				} else {
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Successfully pulled model '%s'.\033[0m", name)
				}
				a.actionMu.Unlock()
			}(inputName)

		case 'b', 'B': // Benchmark models
			if a.hasActiveAction() {
				a.StatusMsg = "\033[33mAnother operation is already in progress.\033[0m"
				continue
			}
			a.ActiveTab = 2
			a.tabSwitched = true
			a.setActiveAction("benchmarking", "Running latency benchmark against installed models...")

			go func() {
				models, err := a.client.ListModels()
				if err != nil || len(models) == 0 {
					a.setActiveAction("", "")
					a.actionMu.Lock()
					a.StatusMsg = "\033[31mNo models found to benchmark or daemon offline.\033[0m"
					a.actionMu.Unlock()
					return
				}

				a.actionMu.Lock()
				a.benchmarkList = make([]model.BenchmarkResult, 0, len(models))
				for _, m := range models {
					a.benchmarkList = append(a.benchmarkList, model.BenchmarkResult{
						ModelName: m.Name,
						SizeGB:    m.SizeGB,
						Success:   false,
					})
				}
				a.actionMu.Unlock()

				for idx, m := range models {
					a.actionMu.Lock()
					a.actionMsg = fmt.Sprintf("Benchmarking model [%d/%d] %s...", idx+1, len(models), m.Name)
					a.actionMu.Unlock()

					res, benchErr := a.client.BenchmarkModel(m.Name)
					a.actionMu.Lock()
					if benchErr == nil && res != nil {
						a.benchmarkList[idx] = *res
					} else {
						a.benchmarkList[idx] = model.BenchmarkResult{
							ModelName: m.Name,
							SizeGB:    m.SizeGB,
							Success:   false,
							ErrorMsg:  "failed / timeout",
						}
					}
					a.actionMu.Unlock()
				}

				a.setActiveAction("", "")
				a.actionMu.Lock()
				a.StatusMsg = "\033[32m✔ Benchmark run complete.\033[0m"
				a.actionMu.Unlock()
			}()
		}
	}

	return nil
}

func (a *App) refreshData() {
	status, _ := a.client.GetDaemonStatus()
	a.daemonStatus = status

	models, _ := a.client.ListModels()
	a.models = models

	logs, _ := a.client.GetServerLogs(40)
	a.logsContent = logs
}

func getTermSize() (int, int) {
	width, height := 80, 24
	fd := int(os.Stdout.Fd())
	if term.IsTerminal(fd) {
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

func truncate(s string, maxLen int) string {
	if maxLen <= 0 {
		return ""
	}
	if len(s) <= maxLen {
		return s
	}
	if maxLen <= 3 {
		return s[:maxLen]
	}
	return s[:maxLen-3] + "..."
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

// Render writes the complete VT100 UI buffer to terminal.
func (a *App) Render() {
	width, height := getTermSize()
	var b strings.Builder
	b.Grow(4096)

	if a.tabSwitched {
		b.WriteString("\033[H\033[2J")
		a.tabSwitched = false
	} else {
		b.WriteString("\033[H")
	}

	// App Header
	b.WriteString("\r\n🤖 \033[1;36mAGYOLLAMA - Local AI & Ollama Engine (Go Edition)\033[0m\033[K\r\n")
	b.WriteString(hr(width))

	// Navigation Tabs
	tabs := []string{"[1] 🤖 Status", "[2] 📦 Models", "[3] ⚡ Benchmark", "[4] 📜 Logs"}
	for i, t := range tabs {
		if i == a.ActiveTab {
			fmt.Fprintf(&b, " \033[1;37;44m %s \033[0m ", t)
		} else {
			fmt.Fprintf(&b, " \033[36m%s\033[0m ", t)
		}
	}
	b.WriteString("\033[K\r\n" + hr(width))

	// Active operation banner / spinner
	if a.hasActiveAction() {
		spinner := spinnerFrames[a.spinnerIdx%len(spinnerFrames)]
		a.actionMu.RLock()
		msg := a.actionMsg
		prog := a.pullProgress
		a.actionMu.RUnlock()

		if prog != nil && prog.TotalBytes > 0 {
			completedGB := float64(prog.CompletedBytes) / (1024 * 1024 * 1024)
			totalGB := float64(prog.TotalBytes) / (1024 * 1024 * 1024)
			b.WriteString(fmt.Sprintf(" \033[1;33m%s %s\033[0m [%.1f%%] (%.2f / %.2f GB) - %s\033[K\r\n\033[K\r\n",
				spinner, msg, prog.Percent, completedGB, totalGB, prog.Status))
		} else {
			b.WriteString(fmt.Sprintf(" \033[1;33m%s %s\033[0m\033[K\r\n\033[K\r\n", spinner, msg))
		}
	}

	// Modals override tab contents
	if a.viewDetailModal != nil {
		a.renderDetailModal(&b, width)
	} else if a.confirmDelete != "" {
		a.renderDeleteConfirmModal(&b, width)
	} else {
		switch a.ActiveTab {
		case 0:
			a.renderStatusTab(&b, width)
		case 1:
			a.renderModelsTab(&b, width, height)
		case 2:
			a.renderBenchmarkTab(&b, width)
		case 3:
			a.renderLogsTab(&b, width, height)
		}
	}

	// Footer & Status message
	b.WriteString(hr(width))
	if a.StatusMsg != "" {
		fmt.Fprintf(&b, " %s\033[K\r\n", a.StatusMsg)
	} else {
		b.WriteString("\033[K\r\n")
	}

	// Action key legend
	switch a.ActiveTab {
	case 0:
		if a.daemonStatus != nil && a.daemonStatus.IsOnline {
			b.WriteString(" \033[1m[Tab/1-4]\033[0mNav \033[1;31m[X]\033[0mStop \033[1;32m[S]\033[0mRefresh \033[1;36m[R]\033[0mChat \033[1;33m[P]\033[0mPull \033[1;35m[B]\033[0mBenchmark \033[1;31m[Q/Esc]\033[0mExit\033[K\r\n")
		} else {
			b.WriteString(" \033[1m[Tab/1-4]\033[0mNav \033[1;32m[S]\033[0mStart/Refresh \033[1;36m[R]\033[0mChat \033[1;33m[P]\033[0mPull \033[1;35m[B]\033[0mBenchmark \033[1;31m[Q/Esc]\033[0mExit\033[K\r\n")
		}
	case 1:
		b.WriteString(" \033[1m[↑/↓]\033[0mSelect \033[1;32m[Enter]\033[0mDefault \033[1;36m[R]\033[0mRun \033[1;33m[V]\033[0mInfo \033[1;31m[D]\033[0mDelete \033[1;35m[P]\033[0mPull \033[1;32m[S]\033[0mRefresh \033[1;31m[Q/Esc]\033[0mExit\033[K\r\n")
	case 2:
		b.WriteString(" \033[1m[B]\033[0mRun Benchmark \033[1;36m[Tab/1-4]\033[0mNav \033[1;32m[S]\033[0mRefresh \033[1;31m[Q/Esc]\033[0mExit\033[K\r\n")
	case 3:
		b.WriteString(" \033[1m[Tab/1-4]\033[0mNav \033[1;32m[S/R]\033[0mRefresh Logs \033[1;31m[Q/Esc]\033[0mExit\033[K\r\n")
	}

	fmt.Print(b.String())
}

func (a *App) renderStatusTab(b *strings.Builder, width int) {
	st := a.daemonStatus
	if st == nil {
		b.WriteString(" \033[33mScanning Ollama daemon status...\033[0m\033[K\r\n")
		return
	}

	if st.IsOnline {
		fmt.Fprintf(b, "  \033[1;32m✔ Ollama Local Daemon: ONLINE (http://%s:%d)\033[0m\033[K\r\n", st.Host, st.Port)
	} else {
		fmt.Fprintf(b, "  \033[1;31m⚠️ Ollama Local Daemon: OFFLINE (target: http://%s:%d)\033[0m\033[K\r\n", st.Host, st.Port)
		b.WriteString("  \033[33mPress [S] to start daemon in the background or run 'ollama serve'.\033[0m\033[K\r\n")
	}

	b.WriteString("\033[K\r\n")
	fmt.Fprintf(b, "  • \033[1mOllama Version:\033[0m   %s\033[K\r\n", st.Version)
	fmt.Fprintf(b, "  • \033[1mActive Default:\033[0m   \033[1;36m%s\033[0m\033[K\r\n", st.ActiveModel)
	fmt.Fprintf(b, "  • \033[1mVRAM Usage:\033[0m       %s\033[K\r\n", st.VRAMAllocated)

	if st.TotalHostRAM > 0 {
		totalGB := float64(st.TotalHostRAM) / (1024 * 1024 * 1024)
		availGB := float64(st.AvailableHostRAM) / (1024 * 1024 * 1024)
		usedGB := totalGB - availGB
		usedPct := (usedGB / totalGB) * 100.0
		fmt.Fprintf(b, "  • \033[1mHost RAM:\033[0m         %.2f / %.2f GB (%.1f%% used, %.2f GB free)\033[K\r\n",
			usedGB, totalGB, usedPct, availGB)
	}

	b.WriteString("\033[K\r\n  \033[1;34m⚡ Quick Actions:\033[0m\033[K\r\n")
	if st.IsOnline {
		b.WriteString("    [\033[1;31mX\033[0m] Stop Ollama background daemon\033[K\r\n")
	} else {
		b.WriteString("    [\033[1;32mS\033[0m] Boot 'ollama serve' daemon in background\033[K\r\n")
	}
	b.WriteString("    [\033[1;36mR\033[0m] Launch instant terminal chat with active default model\033[K\r\n")
	b.WriteString("    [\033[1;33mP\033[0m] Pull a new model from Ollama library\033[K\r\n")
	b.WriteString("    [\033[1;35mB\033[0m] Run benchmark to measure prompt eval latency & tok/s\033[K\r\n")
}

func (a *App) renderModelsTab(b *strings.Builder, width, height int) {
	if len(a.models) == 0 {
		b.WriteString("  \033[33mNo local models detected.\033[0m Press [\033[1;32mP\033[0m] to pull a model (e.g. qwen2.5-coder:7b).\033[K\r\n")
		return
	}

	defaultModel := a.client.GetDefaultModel()

	// Table Header
	fmt.Fprintf(b, "  \033[1;37m%-2s %-24s %-10s %-12s %-10s %-10s\033[0m\033[K\r\n",
		"", "NAME", "SIZE", "FAMILY", "PARAMS", "QUANT")
	b.WriteString("  " + strings.Repeat("─", 74) + "\033[K\r\n")

	for i, m := range a.models {
		isSel := (i == a.SelectedIndex)
		isDef := (m.Name == defaultModel)

		cursor := "  "
		if isSel {
			cursor = "\033[1;32m❯ \033[0m"
		}

		defTag := ""
		if isDef {
			defTag = " \033[1;33m★ default\033[0m"
		}

		sizeStr := fmt.Sprintf("%.2f GB", m.SizeGB)
		family := m.Family
		if family == "" {
			family = "-"
		}
		params := m.ParameterSize
		if params == "" {
			params = "-"
		}
		quant := m.QuantizationLevel
		if quant == "" {
			quant = "-"
		}

		line := fmt.Sprintf("%-24s %-10s %-12s %-10s %-10s",
			truncate(m.Name, 24), sizeStr, truncate(family, 12), truncate(params, 10), truncate(quant, 10))

		if isSel {
			fmt.Fprintf(b, "  %s\033[1;37;44m %s \033[0m%s\033[K\r\n", cursor, line, defTag)
		} else {
			fmt.Fprintf(b, "  %s%-70s%s\033[K\r\n", cursor, line, defTag)
		}
	}
}

func (a *App) renderBenchmarkTab(b *strings.Builder, width int) {
	b.WriteString("  \033[1;36mLocal LLM Inference & Latency Scorecard\033[0m\033[K\r\n")
	b.WriteString("  \033[2mPrompt: \"" + ollamaops.DefaultTestPrompt + "\"\033[0m\033[K\r\n\033[K\r\n")

	if len(a.benchmarkList) == 0 {
		b.WriteString("  \033[33mNo benchmark run yet.\033[0m Press [\033[1;35mB\033[0m] to benchmark installed models.\033[K\r\n")
		return
	}

	fmt.Fprintf(b, "  \033[1;37m%-24s %-10s %-14s %-12s %-12s %-12s\033[0m\033[K\r\n",
		"MODEL", "SIZE (GB)", "PROMPT EVAL", "LATENCY", "SPEED", "STATUS")
	b.WriteString("  " + strings.Repeat("─", 88) + "\033[K\r\n")

	for _, bm := range a.benchmarkList {
		statusStr := "\033[1;32m✔ Done\033[0m"
		if !bm.Success {
			if bm.ErrorMsg != "" {
				statusStr = fmt.Sprintf("\033[1;31m❌ %s\033[0m", truncate(bm.ErrorMsg, 12))
			} else {
				statusStr = "\033[33m⏱️ Pending\033[0m"
			}
		}

		promptEvalStr := fmt.Sprintf("%.2fs", bm.PromptEvalSec)
		latencyStr := fmt.Sprintf("%.2fs", bm.LatencySec)
		speedStr := fmt.Sprintf("%.1f tok/s", bm.TokensPerSec)
		if !bm.Success && bm.TokensPerSec == 0 {
			promptEvalStr = "--"
			latencyStr = "--"
			speedStr = "--"
		}

		fmt.Fprintf(b, "  %-24s %-10.2f %-14s %-12s %-12s %s\033[K\r\n",
			truncate(bm.ModelName, 24), bm.SizeGB, promptEvalStr, latencyStr, speedStr, statusStr)
	}
}

func (a *App) renderLogsTab(b *strings.Builder, width, height int) {
	b.WriteString("  \033[1;36mOllama Server Logs (Tail)\033[0m\033[K\r\n")
	b.WriteString("  " + strings.Repeat("─", 60) + "\033[K\r\n")

	if strings.TrimSpace(a.logsContent) == "" {
		b.WriteString("  \033[33m(No logs available)\033[0m\033[K\r\n")
		return
	}

	lines := strings.Split(a.logsContent, "\n")
	maxLines := height - 10
	if maxLines < 5 {
		maxLines = 5
	}
	if len(lines) > maxLines {
		lines = lines[len(lines)-maxLines:]
	}

	for _, l := range lines {
		fmt.Fprintf(b, "  %s\033[K\r\n", truncate(l, width-4))
	}
}

func (a *App) renderDetailModal(b *strings.Builder, width int) {
	m := a.viewDetailModal
	name := a.viewDetailName

	b.WriteString(fmt.Sprintf("  \033[1;37;44m 📦 MODEL DETAILS: %s \033[0m [Press Esc / Q / Enter to close]\033[K\r\n\033[K\r\n", name))

	if m.Template != "" {
		b.WriteString("  \033[1;36mTemplate:\033[0m\033[K\r\n")
		tmplLines := strings.Split(m.Template, "\n")
		for i, l := range tmplLines {
			if i > 5 {
				b.WriteString("    ...\033[K\r\n")
				break
			}
			fmt.Fprintf(b, "    %s\033[K\r\n", truncate(l, width-6))
		}
	}

	if m.System != "" {
		b.WriteString("\033[K\r\n  \033[1;36mSystem Prompt:\033[0m\033[K\r\n")
		sysLines := strings.Split(m.System, "\n")
		for i, l := range sysLines {
			if i > 4 {
				b.WriteString("    ...\033[K\r\n")
				break
			}
			fmt.Fprintf(b, "    %s\033[K\r\n", truncate(l, width-6))
		}
	}

	if m.Parameters != "" {
		b.WriteString("\033[K\r\n  \033[1;36mParameters:\033[0m\033[K\r\n")
		paramLines := strings.Split(m.Parameters, "\n")
		for i, l := range paramLines {
			if i > 6 {
				b.WriteString("    ...\033[K\r\n")
				break
			}
			fmt.Fprintf(b, "    %s\033[K\r\n", truncate(l, width-6))
		}
	}
}

func (a *App) renderDeleteConfirmModal(b *strings.Builder, width int) {
	b.WriteString("\033[K\r\n")
	fmt.Fprintf(b, "  \033[1;37;41m ⚠️  DELETE MODEL CONFIRMATION \033[0m\033[K\r\n\033[K\r\n")
	fmt.Fprintf(b, "  Are you sure you want to permanently delete model '\033[1;31m%s\033[0m'?\033[K\r\n\033[K\r\n", a.confirmDelete)
	b.WriteString("  Press [\033[1;32mY\033[0m] to Confirm or [\033[1;31mN / Esc\033[0m] to Cancel.\033[K\r\n")
}

// PrintStatus outputs non-interactive status to w.
func (a *App) PrintStatus(w io.Writer) {
	a.refreshData()
	st := a.daemonStatus
	if st == nil {
		fmt.Fprintln(w, "Ollama status unavailable")
		return
	}

	if st.IsOnline {
		fmt.Fprintf(w, "✔ Ollama Local Daemon: ONLINE (http://%s:%d)\n", st.Host, st.Port)
		fmt.Fprintf(w, "  Version:       %s\n", st.Version)
		fmt.Fprintf(w, "  Default Model: %s\n", st.ActiveModel)
		fmt.Fprintf(w, "  VRAM:          %s\n", st.VRAMAllocated)
	} else {
		fmt.Fprintf(w, "⚠️ Ollama Local Daemon: OFFLINE (target: http://%s:%d)\n", st.Host, st.Port)
		fmt.Fprintf(w, "  Default Model: %s\n", st.ActiveModel)
	}

	if st.TotalHostRAM > 0 {
		totalGB := float64(st.TotalHostRAM) / (1024 * 1024 * 1024)
		availGB := float64(st.AvailableHostRAM) / (1024 * 1024 * 1024)
		fmt.Fprintf(w, "  Host RAM:      %.2f GB total, %.2f GB available\n", totalGB, availGB)
	}
}

// PrintModels outputs models table to w.
func (a *App) PrintModels(w io.Writer) {
	a.refreshData()
	if len(a.models) == 0 {
		fmt.Fprintln(w, "No local Ollama models installed.")
		return
	}

	defaultModel := a.client.GetDefaultModel()
	fmt.Fprintf(w, "%-26s %-12s %-12s %-10s %-10s %s\n", "NAME", "SIZE", "FAMILY", "PARAMS", "QUANT", "DEFAULT")
	fmt.Fprintln(w, strings.Repeat("─", 80))

	for _, m := range a.models {
		isDef := ""
		if m.Name == defaultModel {
			isDef = "★ (default)"
		}
		sizeStr := fmt.Sprintf("%.2f GB", m.SizeGB)
		fmt.Fprintf(w, "%-26s %-12s %-12s %-10s %-10s %s\n",
			m.Name, sizeStr, m.Family, m.ParameterSize, m.QuantizationLevel, isDef)
	}
}

// PrintBenchmark executes and prints benchmark to w.
func (a *App) PrintBenchmark(w io.Writer) {
	models, err := a.client.ListModels()
	if err != nil || len(models) == 0 {
		fmt.Fprintln(w, "No models available to benchmark.")
		return
	}

	fmt.Fprintf(w, "%-24s %-10s %-14s %-12s %-12s %s\n",
		"MODEL", "SIZE (GB)", "PROMPT EVAL", "LATENCY", "SPEED", "STATUS")
	fmt.Fprintln(w, strings.Repeat("─", 84))

	for _, m := range models {
		fmt.Fprintf(w, "Testing %s... ", m.Name)
		res, err := a.client.BenchmarkModel(m.Name)
		if err != nil || !res.Success {
			fmt.Fprintf(w, "FAILED (%v)\n", err)
			continue
		}
		fmt.Fprintf(w, "\r%-24s %-10.2f %-14.2fs %-12.2fs %-12.1f tok/s ✔ Done\n",
			m.Name, res.SizeGB, res.PromptEvalSec, res.LatencySec, res.TokensPerSec)
	}
}

// PrintLogs prints server logs to w.
func (a *App) PrintLogs(w io.Writer, lines int) {
	logs, err := a.client.GetServerLogs(lines)
	if err != nil {
		fmt.Fprintf(w, "Error retrieving logs: %v\n", err)
		return
	}
	fmt.Fprintln(w, logs)
}
