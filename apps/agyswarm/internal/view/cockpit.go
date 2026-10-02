package view

import (
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"

	"golang.org/x/term"

	"agyswarm/internal/engine"
	"agyswarm/internal/markdown"
	"agyswarm/internal/model"
)

type Cockpit struct {
	Manager      *engine.Manager
	Exporter     *markdown.Exporter
	SelectedIndex int
	StatusMsg    string
	statusTime   time.Time
	mu           sync.Mutex
	spinnerIdx   int
}

var spinnerFrames = []string{"⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"}

func NewCockpit(mgr *engine.Manager, exporter *markdown.Exporter) *Cockpit {
	if exporter == nil {
		exporter = markdown.NewExporter(filepath.Join(".", "doc", "swarm"))
	}
	return &Cockpit{
		Manager:       mgr,
		Exporter:      exporter,
		SelectedIndex: 0,
		StatusMsg:     "Welcome to Agyswarm Cockpit. Press [s] to spawn an agent, [Tab] to navigate.",
		statusTime:    time.Now(),
	}
}

func (c *Cockpit) SetStatus(msg string) {
	c.mu.Lock()
	defer c.mu.Unlock()
	c.StatusMsg = msg
	c.statusTime = time.Now()
}

func (c *Cockpit) Run() error {
	fd := int(os.Stdin.Fd())
	if !term.IsTerminal(fd) {
		return fmt.Errorf("stdin is not a terminal")
	}

	oldState, err := term.MakeRaw(fd)
	if err != nil {
		return fmt.Errorf("failed to set raw terminal: %w", err)
	}

	// Switch to alternate screen & hide cursor
	fmt.Print("\033[?1049h\033[?25l")
	defer func() {
		fmt.Print("\033[?25h\033[?1049l")
		_ = term.Restore(fd, oldState)
	}()

	keyChan := make(chan []byte, 32)
	doneChan := make(chan struct{})

	// Background keyboard reader
	go func() {
		buf := make([]byte, 16)
		for {
			n, err := os.Stdin.Read(buf)
			if err != nil || n == 0 {
				select {
				case <-doneChan:
					return
				default:
					time.Sleep(10 * time.Millisecond)
					continue
				}
			}
			data := make([]byte, n)
			copy(data, buf[:n])
			keyChan <- data
		}
	}()
	defer close(doneChan)

	ticker := time.NewTicker(150 * time.Millisecond)
	defer ticker.Stop()

	for {
		c.mu.Lock()
		c.spinnerIdx = (c.spinnerIdx + 1) % len(spinnerFrames)
		c.mu.Unlock()

		c.render()

		select {
		case <-ticker.C:
			// Redraw loop
		case keys := <-keyChan:
			if len(keys) == 0 {
				continue
			}

			// Ctrl+C (0x03) or 'q' / 'Q' or Esc (0x1b followed by nothing)
			if keys[0] == 0x03 || keys[0] == 'q' || keys[0] == 'Q' {
				return nil
			}

			// Tab (0x09) or Arrow Down / Right
			agents := c.Manager.List()
			numAgents := len(agents)

			if keys[0] == 0x09 || (len(keys) >= 3 && keys[0] == 0x1b && keys[1] == '[' && (keys[2] == 'B' || keys[2] == 'C')) {
				if numAgents > 0 {
					c.SelectedIndex = (c.SelectedIndex + 1) % numAgents
				}
				continue
			}

			// Arrow Up / Left
			if len(keys) >= 3 && keys[0] == 0x1b && keys[1] == '[' && (keys[2] == 'A' || keys[2] == 'D') {
				if numAgents > 0 {
					c.SelectedIndex = (c.SelectedIndex - 1 + numAgents) % numAgents
				}
				continue
			}

			// Number 1-9
			if keys[0] >= '1' && keys[0] <= '9' {
				idx := int(keys[0] - '1')
				if idx < numAgents {
					c.SelectedIndex = idx
				}
				continue
			}

			// 's' or 'S' - Quick Spawn
			if keys[0] == 's' || keys[0] == 'S' {
				cfg := engine.SpawnConfig{
					Name:    fmt.Sprintf("worker-%d", numAgents+1),
					Command: "bash",
					Args:    []string{"-i"},
				}
				sess, err := c.Manager.Spawn(cfg)
				if err != nil {
					c.SetStatus(fmt.Sprintf("Failed to spawn worker: %v", err))
				} else {
					c.SetStatus(fmt.Sprintf("Spawned agent '%s' (PID: %d)", sess.Name, sess.PID))
					c.SelectedIndex = len(c.Manager.List()) - 1
				}
				continue
			}

			// 'k' or 'K' - Kill Agent
			if keys[0] == 'k' || keys[0] == 'K' {
				if numAgents > 0 && c.SelectedIndex < numAgents {
					target := agents[c.SelectedIndex]
					_ = c.Manager.Kill(target.ID)
					c.SetStatus(fmt.Sprintf("Terminated agent '%s' (%s)", target.Name, target.ID))
				}
				continue
			}

			// 'm' or 'M' - Export Markdown Dossier
			if keys[0] == 'm' || keys[0] == 'M' {
				if numAgents > 0 && c.SelectedIndex < numAgents {
					target := agents[c.SelectedIndex]
					path, err := c.Exporter.ExportAgentSession(target)
					if err != nil {
						c.SetStatus(fmt.Sprintf("Export failed: %v", err))
					} else {
						c.SetStatus(fmt.Sprintf("Exported Markdown: ./%s", filepath.Clean(path)))
					}
				} else {
					c.SetStatus("No active agent to export.")
				}
				continue
			}

			// Enter (0x0D or 0x0A) or 'i' / 'I' - Attach interactive console
			if keys[0] == 0x0D || keys[0] == 0x0A || keys[0] == 'i' || keys[0] == 'I' {
				if numAgents > 0 && c.SelectedIndex < numAgents {
					target := agents[c.SelectedIndex]

					// Exit alternate screen temporarily for full direct PTY attach
					fmt.Print("\033[?25h\033[?1049l")
					_ = term.Restore(fd, oldState)

					_ = c.Manager.Attach(target.ID)

					// Restore alternate screen and raw mode for Cockpit
					oldState, _ = term.MakeRaw(fd)
					fmt.Print("\033[?1049h\033[?25l")
					c.SetStatus(fmt.Sprintf("Detached from agent '%s'.", target.Name))
				}
				continue
			}
		}
	}
}

func (c *Cockpit) render() {
	width, height, err := term.GetSize(int(os.Stdout.Fd()))
	if err != nil || width < 40 || height < 10 {
		width = 100
		height = 30
	}

	var sb strings.Builder
	// Move cursor to top-left
	sb.WriteString("\033[H")

	// Header
	header := "🛸 AGYSWARM COCKPIT · Multi-Agent Child Terminal Cockpit"
	sb.WriteString("\033[1;37;44m") // White on Blue
	sb.WriteString(centerText(header, width))
	sb.WriteString("\033[0m\r\n")

	agents := c.Manager.List()
	numAgents := len(agents)

	// Quick Stats & Status Banner
	c.mu.Lock()
	status := c.StatusMsg
	spinner := spinnerFrames[c.spinnerIdx]
	c.mu.Unlock()

	statusLine := fmt.Sprintf(" Active Agents: %d │ %s │ %s", numAgents, spinner, status)
	if len(statusLine) > width {
		statusLine = statusLine[:width]
	} else {
		statusLine = statusLine + strings.Repeat(" ", width-len(statusLine))
	}
	sb.WriteString("\033[1;30;47m" + statusLine + "\033[0m\r\n")

	// Agent Panes
	if numAgents == 0 {
		emptyLines := height - 7
		if emptyLines < 4 {
			emptyLines = 4
		}
		sb.WriteString("\r\n\033[33m  ┌────────────────────────────────────────────────────────────────────────────┐\033[0m\r\n")
		sb.WriteString("\033[33m  │                          NO ACTIVE SWARM AGENTS                            │\033[0m\r\n")
		sb.WriteString("\033[33m  │                                                                            │\033[0m\r\n")
		sb.WriteString("\033[33m  │  • Press \033[1;32m[s]\033[0;33m to quick-spawn a child bash/agy agent terminal             │\033[0m\r\n")
		sb.WriteString("\033[33m  │  • Or run via CLI: \033[1;36magyswarm spawn --name researcher --acc nhon\033[0;33m            │\033[0m\r\n")
		sb.WriteString("\033[33m  │  • Press \033[1;31m[q]\033[0;33m or \033[1;31m[Ctrl+C]\033[0;33m to exit                                           │\033[0m\r\n")
		sb.WriteString("\033[33m  └────────────────────────────────────────────────────────────────────────────┘\033[0m\r\n")
		for i := 0; i < emptyLines-7; i++ {
			sb.WriteString("\r\n")
		}
	} else {
		// Render Agent List / Panes
		// We calculate pane heights based on terminal height
		paneHeight := (height - 6) / numAgents
		if paneHeight < 5 {
			paneHeight = 5
		}

		for idx, ag := range agents {
			isSelected := (idx == c.SelectedIndex)

			borderColor := "\033[90m" // Gray
			if isSelected {
				borderColor = "\033[1;36m" // Cyan
			}

			// Status badge
			badge := ""
			switch ag.Status {
			case model.StatusWorking:
				badge = fmt.Sprintf("\033[1;33m● WORKING %s\033[0m", spinner)
			case model.StatusNeedInput:
				badge = "\033[1;35;5m⚡ NEED INPUT\033[0m"
			case model.StatusDone:
				badge = fmt.Sprintf("\033[1;32m✔ DONE (exit %d)\033[0m", ag.ExitCode)
			case model.StatusError:
				badge = fmt.Sprintf("\033[1;31m✖ ERROR (exit %d)\033[0m", ag.ExitCode)
			default:
				badge = fmt.Sprintf("\033[90m%s\033[0m", ag.Status)
			}

			accTag := ""
			if ag.AccountName != "" {
				accTag = fmt.Sprintf(" [Acc: %s]", ag.AccountName)
			}

			selMarker := " "
			if isSelected {
				selMarker = "▶"
			}

			title := fmt.Sprintf("%s [%d] %s (PID: %d)%s ── Status: %s", selMarker, idx+1, ag.Name, ag.PID, accTag, badge)
			sb.WriteString(borderColor + "┌── " + title + " " + strings.Repeat("─", max(0, width-stripLen(title)-8)) + "┐\033[0m\r\n")

			// Body lines
			innerLines := paneHeight - 2
			recentLines := ag.GetRecentLines(innerLines)

			for i := 0; i < innerLines; i++ {
				var lineContent string
				lineIdx := len(recentLines) - innerLines + i
				if lineIdx >= 0 && lineIdx < len(recentLines) {
					lineContent = markdown.StripANSI(recentLines[lineIdx])
				}
				if ag.Status == model.StatusNeedInput && i == innerLines-1 && ag.PromptHint != "" {
					lineContent = fmt.Sprintf("⚡ %s", ag.PromptHint)
				}

				if len(lineContent) > width-4 {
					lineContent = lineContent[:width-7] + "..."
				} else {
					lineContent = lineContent + strings.Repeat(" ", max(0, width-4-len(lineContent)))
				}

				if ag.Status == model.StatusNeedInput && i == innerLines-1 {
					sb.WriteString(borderColor + "│ " + "\033[1;33m" + lineContent + "\033[0m" + borderColor + " │\033[0m\r\n")
				} else {
					sb.WriteString(borderColor + "│ " + "\033[37m" + lineContent + "\033[0m" + borderColor + " │\033[0m\r\n")
				}
			}

			footerHint := "[Enter / i] Direct Keyboard (Ctrl+] to return) │ [k] Kill │ [m] Export .md"
			sb.WriteString(borderColor + "└── " + footerHint + " " + strings.Repeat("─", max(0, width-len(footerHint)-8)) + "┘\033[0m\r\n")
		}
	}

	// Bottom command bar
	sb.WriteString("\033[1;37;44m")
	footer := " [Tab/1-9] Switch Agent │ [Enter/i] Direct Control │ [s] Spawn │ [k] Kill │ [m] Export MD │ [q] Quit"
	if len(footer) > width {
		footer = footer[:width]
	} else {
		footer = footer + strings.Repeat(" ", width-len(footer))
	}
	sb.WriteString(footer + "\033[0m")

	os.Stdout.WriteString(sb.String())
}

func centerText(s string, width int) string {
	if len(s) >= width {
		return s[:width]
	}
	pad := (width - len(s)) / 2
	rightPad := width - len(s) - pad
	return strings.Repeat(" ", pad) + s + strings.Repeat(" ", rightPad)
}

func stripLen(s string) int {
	return len(markdown.StripANSI(s))
}

func max(a, b int) int {
	if a > b {
		return a
	}
	return b
}
