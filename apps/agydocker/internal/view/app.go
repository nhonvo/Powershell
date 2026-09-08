package view

import (
	"fmt"
	"io"
	"os"
	"os/exec"
	"sort"
	"strings"
	"sync"

	"golang.org/x/term"

	"agydocker/internal/model"
	"agydocker/internal/service/dockerops"
)

var spinnerFrames = []string{"⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"}

type App struct {
	ActiveTab     int // 0: Containers, 1: WSL2 RAM, 2: Volumes
	SelectedIndex int
	GroupViewMode int // 0: Grouped by Project, 1: Flat List
	StatusMsg     string
	tabSwitched   bool
	dockerErr     error

	cachedContainers []model.ContainerInfo
	cachedMem        *model.MemInfo
	cachedVolumes    []model.VolumeInfo
	needsReload      bool

	pendingActions map[string]string // map[containerID]action ("stopping", "starting", "restarting", "downing")
	pendingMu      sync.RWMutex
	spinnerIdx     int
	isReloading    bool
}

func NewApp() *App {
	return &App{
		ActiveTab:      0,
		GroupViewMode:  0, // Default to Grouped View
		tabSwitched:    true,
		needsReload:    true,
		pendingActions: make(map[string]string),
	}
}

func (a *App) getPendingAction(id string) string {
	a.pendingMu.RLock()
	defer a.pendingMu.RUnlock()
	return a.pendingActions[id]
}

func (a *App) setPendingAction(id string, act string) {
	a.pendingMu.Lock()
	defer a.pendingMu.Unlock()
	if act == "" {
		delete(a.pendingActions, id)
	} else {
		a.pendingActions[id] = act
	}
}

func (a *App) hasPendingActions() bool {
	a.pendingMu.RLock()
	defer a.pendingMu.RUnlock()
	return len(a.pendingActions) > 0
}

func (a *App) isReloadingActive() bool {
	a.pendingMu.RLock()
	defer a.pendingMu.RUnlock()
	return a.isReloading
}

func (a *App) reloadAsync() {
	a.pendingMu.Lock()
	if a.isReloading {
		a.pendingMu.Unlock()
		return
	}
	a.isReloading = true
	a.pendingMu.Unlock()

	go func() {
		containers, dErr := dockerops.ListContainers()
		mem, _ := dockerops.GetMemoryInfo()
		vols, _ := dockerops.ListVolumes()

		a.pendingMu.Lock()
		a.cachedContainers = containers
		a.dockerErr = dErr
		a.cachedMem = mem
		a.cachedVolumes = vols
		a.isReloading = false
		a.pendingMu.Unlock()
	}()
}

func (a *App) getOrderedContainers() []model.ContainerInfo {
	a.pendingMu.RLock()
	containers := make([]model.ContainerInfo, len(a.cachedContainers))
	copy(containers, a.cachedContainers)
	a.pendingMu.RUnlock()

	if a.GroupViewMode == 1 {
		return containers
	}

	// Group by ComposeProject
	groups := make(map[string][]model.ContainerInfo)
	var projNames []string
	for _, c := range containers {
		proj := c.ComposeProject
		if proj == "" {
			proj = "Standalone"
		}
		if _, exists := groups[proj]; !exists {
			projNames = append(projNames, proj)
		}
		groups[proj] = append(groups[proj], c)
	}

	sort.Strings(projNames)

	var ordered []model.ContainerInfo
	for _, p := range projNames {
		ordered = append(ordered, groups[p]...)
	}
	return ordered
}

func (a *App) RunInteractive() error {
	fd := int(os.Stdin.Fd())
	if !term.IsTerminal(fd) {
		return a.runNonInteractive()
	}

	oldState, err := term.MakeRaw(fd)
	if err != nil {
		return a.runNonInteractive()
	}

	defer func() {
		fmt.Print("\033[?25h\033[?1049l")
		_ = term.Restore(fd, oldState)
	}()

	fmt.Print("\033[?1049h\033[?25l\033[H\033[2J")

	for {
		a.pendingMu.RLock()
		needInit := a.cachedContainers == nil
		needRel := a.needsReload
		a.pendingMu.RUnlock()

		if needInit {
			a.cachedContainers, a.dockerErr = dockerops.ListContainers()
			a.cachedMem, _ = dockerops.GetMemoryInfo()
			a.cachedVolumes, _ = dockerops.ListVolumes()
		} else if needRel {
			a.pendingMu.Lock()
			a.needsReload = false
			a.pendingMu.Unlock()
			a.reloadAsync()
		}

		orderedContainers := a.getOrderedContainers()
		totalItems := len(orderedContainers)
		if a.ActiveTab == 1 {
			totalItems = 1
		} else if a.ActiveTab == 2 {
			a.pendingMu.RLock()
			totalItems = len(a.cachedVolumes)
			a.pendingMu.RUnlock()
		}

		if a.SelectedIndex >= totalItems && totalItems > 0 {
			a.SelectedIndex = totalItems - 1
		}
		if a.SelectedIndex < 0 {
			a.SelectedIndex = 0
		}

		a.Render()

		// Wait up to 150ms for keyboard input (non-blocking for background tasks)
		ready := waitKey(fd, 150)
		if !ready {
			if a.hasPendingActions() || a.isReloadingActive() {
				a.spinnerIdx = (a.spinnerIdx + 1) % len(spinnerFrames)
			}
			continue
		}

		var buf [32]byte
		n, err := os.Stdin.Read(buf[:])
		if err != nil || n == 0 {
			break
		}

		if a.hasPendingActions() || a.isReloadingActive() {
			a.spinnerIdx = (a.spinnerIdx + 1) % len(spinnerFrames)
		}

		a.StatusMsg = ""

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
					a.ActiveTab = (a.ActiveTab + 1) % 3
					a.SelectedIndex = 0
					a.tabSwitched = true
					continue
				case 'D': // Left Tab
					a.ActiveTab = (a.ActiveTab + 2) % 3
					a.SelectedIndex = 0
					a.tabSwitched = true
					continue
				}
			}
			continue
		}

		switch b {
		case '\t':
			a.ActiveTab = (a.ActiveTab + 1) % 3
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
		case 'k', 'K':
			if a.ActiveTab == 0 && len(orderedContainers) > 0 && a.SelectedIndex < len(orderedContainers) {
				c := orderedContainers[a.SelectedIndex]
				a.setPendingAction(c.ID, "killing")
				a.StatusMsg = fmt.Sprintf("\033[31mKilling %s in background...\033[0m", c.Names)
				go func(cid, cname string) {
					err := dockerops.KillContainer(cid)
					a.setPendingAction(cid, "")
					a.pendingMu.Lock()
					if err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mKill error (%s): %s\033[0m", cname, truncateString(err.Error(), 65))
					} else {
						a.StatusMsg = fmt.Sprintf("\033[32mKilled %s successfully.\033[0m", cname)
					}
					a.pendingMu.Unlock()
					a.reloadAsync()
				}(c.ID, c.Names)
			} else if a.SelectedIndex > 0 {
				a.SelectedIndex--
			}
		case 'd', 'D': // Remove container with confirmation
			if a.ActiveTab == 0 && len(orderedContainers) > 0 && a.SelectedIndex < len(orderedContainers) {
				c := orderedContainers[a.SelectedIndex]
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[31m[agydocker]\033[0m Force remove container '%s'? (y/N): ", c.Names)
				var confirm string
				fmt.Scanln(&confirm)
				if strings.EqualFold(strings.TrimSpace(confirm), "y") {
					err := dockerops.RemoveContainer(c.ID)
					if err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mRemove error (%s): %s\033[0m", c.Names, truncateString(err.Error(), 65))
					} else {
						a.StatusMsg = fmt.Sprintf("\033[32mRemoved container %s successfully.\033[0m", c.Names)
					}
					a.reloadAsync()
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
			}
		case 'x', 'X': // Down Stack: docker compose down (or down container if Standalone)
			if a.ActiveTab == 0 && len(orderedContainers) > 0 && a.SelectedIndex < len(orderedContainers) {
				sel := orderedContainers[a.SelectedIndex]
				proj := sel.ComposeProject
				if proj == "" {
					proj = "Standalone"
				}

				if proj == "Standalone" {
					a.setPendingAction(sel.ID, "downing")
					a.StatusMsg = fmt.Sprintf("\033[31mDowning standalone container %s in background...\033[0m", sel.Names)
					go func(cid, cname string) {
						err := dockerops.DownContainer(cid)
						a.setPendingAction(cid, "")
						a.pendingMu.Lock()
						if err != nil {
							a.StatusMsg = fmt.Sprintf("\033[31mDown error (%s): %s\033[0m", cname, truncateString(err.Error(), 65))
						} else {
							a.StatusMsg = fmt.Sprintf("\033[33mDowned container %s cleanly.\033[0m", cname)
						}
						a.pendingMu.Unlock()
						a.reloadAsync()
					}(sel.ID, sel.Names)
				} else {
					a.setPendingAction(proj, "downing")
					var inProj []model.ContainerInfo
					for _, c := range orderedContainers {
						if c.ComposeProject == proj {
							inProj = append(inProj, c)
							a.setPendingAction(c.ID, "downing")
						}
					}
					a.StatusMsg = fmt.Sprintf("\033[31mDowning compose stack '%s' in background...\033[0m", proj)
					go func(projectName string, targets []model.ContainerInfo) {
						err := dockerops.DownCompose(projectName)
						a.setPendingAction(projectName, "")
						for _, c := range targets {
							a.setPendingAction(c.ID, "")
						}
						a.pendingMu.Lock()
						if err != nil {
							a.StatusMsg = fmt.Sprintf("\033[31mDown stack error (%s): %s\033[0m", projectName, truncateString(err.Error(), 55))
						} else {
							a.StatusMsg = fmt.Sprintf("\033[33mDowned compose stack '%s' cleanly.\033[0m", projectName)
						}
						a.pendingMu.Unlock()
						a.reloadAsync()
					}(proj, inProj)
				}
			}
		case 'j', 'J':
			if a.SelectedIndex < totalItems-1 {
				a.SelectedIndex++
			}
		case 'g', 'G': // Toggle Grouped vs Flat view in Tab 0
			if a.ActiveTab == 0 {
				a.GroupViewMode = (a.GroupViewMode + 1) % 2
				a.SelectedIndex = 0
				a.tabSwitched = true
				if a.GroupViewMode == 0 {
					a.StatusMsg = "\033[32mSwitched to Grouped View (by Compose Project)\033[0m"
				} else {
					a.StatusMsg = "\033[32mSwitched to Flat Container View\033[0m"
				}
			}
		case 'r', 'R': // Restart container in background or refresh
			if a.ActiveTab == 0 && len(orderedContainers) > 0 && a.SelectedIndex < len(orderedContainers) {
				c := orderedContainers[a.SelectedIndex]
				a.setPendingAction(c.ID, "restarting")
				a.StatusMsg = fmt.Sprintf("\033[36mRestarting %s in background...\033[0m", c.Names)
				go func(cid, cname string) {
					err := dockerops.RestartContainer(cid)
					a.setPendingAction(cid, "")
					a.pendingMu.Lock()
					if err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mRestart error (%s): %s\033[0m", cname, truncateString(err.Error(), 65))
					} else {
						a.StatusMsg = fmt.Sprintf("\033[32mRestarted %s successfully.\033[0m", cname)
					}
					a.pendingMu.Unlock()
					a.reloadAsync()
				}(c.ID, c.Names)
			} else {
				a.reloadAsync()
				a.StatusMsg = "\033[32mRefreshed status in background.\033[0m"
			}
		case 's', 'S': // Start / Stop toggle in background
			if a.ActiveTab == 0 && len(orderedContainers) > 0 && a.SelectedIndex < len(orderedContainers) {
				c := orderedContainers[a.SelectedIndex]
				if a.getPendingAction(c.ID) != "" {
					a.StatusMsg = fmt.Sprintf("\033[33m%s is already %s...\033[0m", c.Names, a.getPendingAction(c.ID))
					continue
				}

				if c.IsRunning {
					a.setPendingAction(c.ID, "stopping")
					a.StatusMsg = fmt.Sprintf("\033[36mStopping %s in background...\033[0m", c.Names)
					go func(cid, cname string) {
						err := dockerops.StopContainer(cid)
						a.setPendingAction(cid, "")
						a.pendingMu.Lock()
						if err != nil {
							a.StatusMsg = fmt.Sprintf("\033[31mStop error (%s): %s\033[0m", cname, truncateString(err.Error(), 65))
						} else {
							a.StatusMsg = fmt.Sprintf("\033[33mStopped %s.\033[0m", cname)
						}
						a.pendingMu.Unlock()
						a.reloadAsync()
					}(c.ID, c.Names)
				} else {
					a.setPendingAction(c.ID, "starting")
					a.StatusMsg = fmt.Sprintf("\033[36mStarting %s in background...\033[0m", c.Names)
					go func(cid, cname string) {
						err := dockerops.StartContainer(cid)
						a.setPendingAction(cid, "")
						a.pendingMu.Lock()
						if err != nil {
							a.StatusMsg = fmt.Sprintf("\033[31mStart error (%s): %s\033[0m", cname, truncateString(err.Error(), 65))
						} else {
							a.StatusMsg = fmt.Sprintf("\033[32mStarted %s.\033[0m", cname)
						}
						a.pendingMu.Unlock()
						a.reloadAsync()
					}(c.ID, c.Names)
				}
			}
		case 'a', 'A': // Start / Stop entire Compose Project Stack in background
			if a.ActiveTab == 0 && len(orderedContainers) > 0 && a.SelectedIndex < len(orderedContainers) {
				sel := orderedContainers[a.SelectedIndex]
				proj := sel.ComposeProject
				if proj == "" {
					proj = "Standalone"
				}
				var inProj []model.ContainerInfo
				anyRunning := false
				for _, c := range orderedContainers {
					if (c.ComposeProject == proj) || (proj == "Standalone" && c.ComposeProject == "") {
						inProj = append(inProj, c)
						if c.IsRunning {
							anyRunning = true
						}
					}
				}

				if anyRunning {
					for _, c := range inProj {
						if c.IsRunning {
							a.setPendingAction(c.ID, "stopping")
						}
					}
					a.StatusMsg = fmt.Sprintf("\033[36mStopping %d containers in '%s' in background...\033[0m", len(inProj), proj)

					go func(project string, targets []model.ContainerInfo) {
						var stopped, failed int
						var firstErr error
						for _, c := range targets {
							if c.IsRunning {
								if err := dockerops.StopContainer(c.ID); err != nil {
									failed++
									if firstErr == nil {
										firstErr = err
									}
								} else {
									stopped++
								}
								a.setPendingAction(c.ID, "")
							}
						}
						a.pendingMu.Lock()
						if failed > 0 {
							a.StatusMsg = fmt.Sprintf("\033[31mStopped %d, failed %d in '%s': %s\033[0m", stopped, failed, project, truncateString(firstErr.Error(), 55))
						} else {
							a.StatusMsg = fmt.Sprintf("\033[33mStopped stack '%s' (%d containers)\033[0m", project, stopped)
						}
						a.pendingMu.Unlock()
						a.reloadAsync()
					}(proj, inProj)
				} else {
					for _, c := range inProj {
						if !c.IsRunning {
							a.setPendingAction(c.ID, "starting")
						}
					}
					a.StatusMsg = fmt.Sprintf("\033[36mStarting %d containers in '%s' in background...\033[0m", len(inProj), proj)

					go func(project string, targets []model.ContainerInfo) {
						var started, failed int
						var firstErr error
						for _, c := range targets {
							if !c.IsRunning {
								if err := dockerops.StartContainer(c.ID); err != nil {
									failed++
									if firstErr == nil {
										firstErr = err
									}
								} else {
									started++
								}
								a.setPendingAction(c.ID, "")
							}
						}
						a.pendingMu.Lock()
						if failed > 0 {
							a.StatusMsg = fmt.Sprintf("\033[31mStarted %d, failed %d in '%s': %s\033[0m", started, failed, project, truncateString(firstErr.Error(), 55))
						} else {
							a.StatusMsg = fmt.Sprintf("\033[32mStarted stack '%s' (%d containers)\033[0m", project, started)
						}
						a.pendingMu.Unlock()
						a.reloadAsync()
					}(proj, inProj)
				}
			}
		case 'l', 'L': // Logs modal
			if a.ActiveTab == 0 && len(orderedContainers) > 0 && a.SelectedIndex < len(orderedContainers) {
				c := orderedContainers[a.SelectedIndex]
				logs, _ := dockerops.GetContainerLogs(c.ID, 40)
				fmt.Print("\033[H\033[2J")
				fmt.Printf("\r\n📜 \033[1;36mRecent Logs for '%s':\033[0m\r\n\r\n", c.Names)
				if strings.TrimSpace(logs) == "" {
					fmt.Print("  \033[33m(No logs output recorded)\033[0m\r\n")
				} else {
					fmt.Printf("%s\r\n", logs)
				}
				fmt.Print("\r\n \033[1mPress any key to return...\033[0m")
				var dummy [1]byte
				_, _ = os.Stdin.Read(dummy[:])
				a.tabSwitched = true
			}
		case 'e', 'E': // Exec into container
			if a.ActiveTab == 0 && len(orderedContainers) > 0 && a.SelectedIndex < len(orderedContainers) {
				c := orderedContainers[a.SelectedIndex]
				if !c.IsRunning {
					a.StatusMsg = "\033[31mContainer is not running!\033[0m"
					continue
				}
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agydocker]\033[0m Connecting shell to '\033[32m%s\033[0m'...\r\n", c.Names)
				cmd := exec.Command("docker", "exec", "-it", c.ID, "sh")
				cmd.Stdin = os.Stdin
				cmd.Stdout = os.Stdout
				cmd.Stderr = os.Stderr
				_ = cmd.Run()
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
			}
		case 'p', 'P': // Prune cache / system or volumes
			fmt.Print("\033[?25h\033[?1049l")
			_ = term.Restore(fd, oldState)
			if a.ActiveTab == 2 {
				fmt.Print("\r\n\033[33m[agydocker]\033[0m Run 'docker volume prune -f' to remove unused local volumes? (y/N): ")
				var confirm string
				fmt.Scanln(&confirm)
				if strings.EqualFold(strings.TrimSpace(confirm), "y") {
					out, err := dockerops.PruneVolumes()
					if err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mVolume prune error: %v\033[0m", err)
					} else {
						a.StatusMsg = fmt.Sprintf("\033[32mVolumes pruned successfully: %s\033[0m", truncateString(out, 50))
					}
					a.reloadAsync()
				}
			} else {
				fmt.Print("\r\n\033[33m[agydocker]\033[0m Run 'docker system prune -f' to reclaim RAM/disk? (y/N): ")
				var confirm string
				fmt.Scanln(&confirm)
				if strings.EqualFold(strings.TrimSpace(confirm), "y") {
					out, err := dockerops.PruneSystem()
					if err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mPrune error: %v\033[0m", err)
					} else {
						a.StatusMsg = fmt.Sprintf("\033[32mPruned successfully: %s\033[0m", truncateString(out, 50))
					}
					a.reloadAsync()
				}
			}
			oldState, _ = term.MakeRaw(fd)
			fmt.Print("\033[?1049h\033[?25l")
			a.tabSwitched = true
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

	if width < 85 {
		b.WriteString("\r\n🐳 \033[1;36mAGYDOCKER\033[0m · Containers & RAM\033[K\r\n")
		b.WriteString(hr(width))
		tabNames := []string{"1:Containers", "2:WSL RAM", "3:Volumes"}
		for i, t := range tabNames {
			if i == a.ActiveTab {
				fmt.Fprintf(&b, "\033[1;37;44m [%s] \033[0m ", t)
			} else {
				fmt.Fprintf(&b, "\033[36m[%s]\033[0m ", t)
			}
		}
		b.WriteString("\033[K\r\n" + hr(width))
	} else {
		b.WriteString("\r\n🐳 \033[1;36mAGYDOCKER - Container & WSL2 RAM Manager (Go Engine)\033[0m\033[K\r\n")
		b.WriteString(hr(width))
		tabs := []string{"[1] 🐳 Containers & Compose", "[2] 🧠 WSL2 RAM & Resources", "[3] 💾 Volumes & Caches"}
		for i, t := range tabs {
			if i == a.ActiveTab {
				fmt.Fprintf(&b, " \033[1;37;44m %s \033[0m ", t)
			} else {
				fmt.Fprintf(&b, " \033[36m%s\033[0m ", t)
			}
		}
		b.WriteString("\033[K\r\n" + hr(width))
	}

	if a.dockerErr != nil {
		b.WriteString(" \033[1;37;41m ⚠️ Docker daemon is offline / cannot connect to docker.sock \033[0m\033[K\r\n\033[K\r\n")
	}

	switch a.ActiveTab {
	case 0:
		a.renderContainersTab(&b, width, height)
	case 1:
		a.renderMemTab(&b, width)
	case 2:
		a.renderVolumesTab(&b, width, height)
	}

	b.WriteString(hr(width))
	if a.StatusMsg != "" {
		fmt.Fprintf(&b, " %s\033[K\r\n", a.StatusMsg)
	} else {
		b.WriteString("\033[K\r\n")
	}

	if width < 85 {
		switch a.ActiveTab {
		case 0:
			b.WriteString(" \033[1m[Tab]\033[0mNav \033[1;32m[S]\033[0mStart \033[1;31m[K]\033[0mKill \033[1;31m[X]\033[0mDown \033[1;31m[D]\033[0mRm \033[1;33m[A]\033[0mStack \033[1;36m[g]\033[0mGrp \033[1;36m[L]\033[0mLog \033[1;31m[Q/Esc]\033[0mExit\033[K\r\n")
		case 1:
			b.WriteString(" \033[1m[Tab]\033[0mNav \033[1;33m[P]\033[0mPrune (Reclaim RAM) \033[1;36m[R]\033[0mRefresh \033[1;31m[Q/Esc]\033[0mExit\033[K\r\n")
		case 2:
			b.WriteString(" \033[1m[Tab]\033[0mNav \033[1;33m[P]\033[0mPrune Volumes \033[1;36m[R]\033[0mRefresh \033[1;31m[Q/Esc]\033[0mExit\033[K\r\n")
		}
	} else {
		switch a.ActiveTab {
		case 0:
			b.WriteString(" \033[1m[Tab/1-3]\033[0m Switch · \033[1m[↑/↓]\033[0m Nav · \033[1;32m[S]\033[0m Start/Stop · \033[1;31m[K]\033[0m Kill · \033[1;31m[X]\033[0m Down Stack · \033[1;31m[D]\033[0m Rm · \033[1;33m[A]\033[0m Start/Stop Stack · \033[1;35m[g]\033[0m Group/Flat · \033[1;36m[L]\033[0m Logs · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		case 1:
			b.WriteString(" \033[1m[Tab/1-3]\033[0m Switch · \033[1;33m[P]\033[0m Prune (Reclaim RAM & Docker Cache) · \033[1;36m[R]\033[0m Refresh · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		case 2:
			b.WriteString(" \033[1m[Tab/1-3]\033[0m Switch · \033[1m[↑/↓ j/k]\033[0m Nav · \033[1;33m[P]\033[0m Prune Volumes · \033[1;36m[R]\033[0m Refresh · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		}
	}

	b.WriteString("\033[J")
	os.Stdout.WriteString(b.String())
}

func (a *App) renderContainersTab(b *strings.Builder, width int, height int) {
	containers := a.getOrderedContainers()
	if len(containers) == 0 {
		b.WriteString(" \033[33mNo Docker containers found on this host.\033[0m\033[K\r\n")
		return
	}

	modeStr := "\033[1;32m[Grouped by Project]\033[0m"
	if a.GroupViewMode == 1 {
		modeStr = "\033[1;35m[Flat List]\033[0m"
	}
	fmt.Fprintf(b, " 🐳 \033[1;36mDocker Containers (%d total)\033[0m · View: %s\033[K\r\n\033[K\r\n", len(containers), modeStr)

	pageSize := height - 13
	if pageSize < 4 {
		pageSize = 4
	}
	if pageSize > 9 {
		pageSize = 9
	}

	page := a.SelectedIndex / pageSize
	startIdx := page * pageSize
	endIdx := startIdx + pageSize
	if endIdx > len(containers) {
		endIdx = len(containers)
	}
	totalPages := (len(containers) + pageSize - 1) / pageSize
	if totalPages == 0 {
		totalPages = 1
	}

	currentGroup := ""

	for i := startIdx; i < endIdx; i++ {
		c := containers[i]

		// In Grouped view, show group header if changed
		if a.GroupViewMode == 0 {
			proj := c.ComposeProject
			if proj == "" {
				proj = "Standalone"
			}
			if proj != currentGroup {
				currentGroup = proj
				// Count total and running in this project
				pTotal := 0
				pUp := 0
				for _, it := range containers {
					itProj := it.ComposeProject
					if itProj == "" {
						itProj = "Standalone"
					}
					if itProj == proj {
						pTotal++
						if it.IsRunning {
							pUp++
						}
					}
				}
				upBadge := fmt.Sprintf("\033[32m%d up\033[0m", pUp)
				if pUp == 0 {
					upBadge = "\033[31m0 up\033[0m"
				}
				actBadge := ""
				if act := a.getPendingAction(proj); act != "" {
					sp := spinnerFrames[a.spinnerIdx%len(spinnerFrames)]
					actBadge = fmt.Sprintf(" \033[1;31m[%s %s]\033[0m", sp, strings.ToUpper(act))
				}
				fmt.Fprintf(b, "📁 \033[1;36m%-24s\033[0m%s \033[37m(%d containers · %s)\033[0m\033[K\r\n", proj, actBadge, pTotal, upBadge)
			}
		}

		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;32m▶ \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		statusBadge := "\033[31m[Exited]\033[0m"
		if c.IsRunning {
			statusBadge = "\033[32m[Up]    \033[0m"
		}
		act := a.getPendingAction(c.ID)
		if act == "" && c.ComposeProject != "" {
			act = a.getPendingAction(c.ComposeProject)
		}
		if act != "" {
			sp := spinnerFrames[a.spinnerIdx%len(spinnerFrames)]
			switch act {
			case "stopping":
				statusBadge = fmt.Sprintf("\033[1;33m%s [Stop]\033[0m", sp)
			case "starting":
				statusBadge = fmt.Sprintf("\033[1;36m%s [Start]\033[0m", sp)
			case "restarting":
				statusBadge = fmt.Sprintf("\033[1;35m%s [Rest]\033[0m", sp)
			case "killing":
				statusBadge = fmt.Sprintf("\033[1;31m%s [Kill]\033[0m", sp)
			case "removing":
				statusBadge = fmt.Sprintf("\033[1;31m%s [Rm]\033[0m", sp)
			case "downing":
				statusBadge = fmt.Sprintf("\033[1;31m%s [Down]\033[0m", sp)
			}
		}

		indent := "  "
		if a.GroupViewMode == 0 {
			indent = "   "
		}

		if width < 85 {
			name := truncateString(c.Names, 18)
			status := truncateString(c.Status, 14)
			img := truncateString(c.Image, 16)
			fmt.Fprintf(b, "%s%s%s%2d. %s \033[1m%-18s\033[0m \033[35m%-14s\033[0m \033[37m%-16s\033[0m%s\033[K\r\n",
				cursor, indent, highlightStart, i+1, statusBadge, name, status, img, highlightEnd)
		} else {
			name := truncateString(c.Names, 22)
			img := truncateString(c.Image, 22)
			status := truncateString(c.Status, 16)
			portsLen := width - 82
			if portsLen < 8 {
				portsLen = 8
			}
			ports := truncateString(c.Ports, portsLen)
			fmt.Fprintf(b, "%s%s%s%2d. %s \033[1m%-22s\033[0m %-22s \033[35m%-16s\033[0m \033[37m%s\033[0m%s\033[K\r\n",
				cursor, indent, highlightStart, i+1, statusBadge, name, img, status, ports, highlightEnd)
		}
	}

	fmt.Fprintf(b, "\033[K\r\n \033[37m[Page %d/%d · %d-%d of %d containers · [g] Toggle View · [S] Start/Stop · [X] Down · [K] Kill · [D] Rm · [A] Stack Action]\033[0m\033[K\r\n",
		page+1, totalPages, startIdx+1, endIdx, len(containers))
}

func (a *App) renderMemTab(b *strings.Builder, width int) {
	mem := a.cachedMem
	if mem == nil {
		b.WriteString(" \033[31mUnable to read WSL2 memory statistics.\033[0m\033[K\r\n")
		return
	}

	fmt.Fprintf(b, " 🧠 \033[1;36mWSL2 RAM & Memory Guard (Host Linux Kernel):\033[0m\033[K\r\n\033[K\r\n")

	// RAM bar
	barMax := 30
	if width < 80 {
		barMax = 18
	}
	usedLen := int((mem.UsedPercent / 100.0) * float64(barMax))
	if usedLen > barMax {
		usedLen = barMax
	}
	if usedLen < 0 {
		usedLen = 0
	}
	barColor := "\033[32m"
	if mem.UsedPercent > 85.0 {
		barColor = "\033[31m"
	} else if mem.UsedPercent > 65.0 {
		barColor = "\033[33m"
	}
	ramBar := strings.Repeat("█", usedLen) + strings.Repeat("░", barMax-usedLen)

	totalGB := float64(mem.TotalKB) / (1024.0 * 1024.0)
	usedGB := float64(mem.UsedKB) / (1024.0 * 1024.0)
	availGB := float64(mem.AvailableKB) / (1024.0 * 1024.0)
	cachedGB := float64(mem.CachedKB) / (1024.0 * 1024.0)

	if width < 80 {
		fmt.Fprintf(b, "  • \033[1mWSL RAM:\033[0m  [%s%s\033[0m] \033[1m%.1f%%\033[0m (%.2f/%.2f GB)\033[K\r\n",
			barColor, ramBar, mem.UsedPercent, usedGB, totalGB)
	} else {
		fmt.Fprintf(b, "  • \033[1mWSL RAM:\033[0m  [%s%s\033[0m] \033[1m%.1f%%\033[0m (Used: \033[33m%.2f GB\033[0m / \033[32m%.2f GB\033[0m · Avail: %.2f GB)\033[K\r\n",
			barColor, ramBar, mem.UsedPercent, usedGB, totalGB, availGB)
	}

	// Swap bar
	if mem.SwapTotalKB > 0 {
		swapTotalGB := float64(mem.SwapTotalKB) / (1024.0 * 1024.0)
		swapUsedGB := float64(mem.SwapUsedKB) / (1024.0 * 1024.0)
		swapLen := int((mem.SwapUsedPercent / 100.0) * float64(barMax))
		if swapLen > barMax {
			swapLen = barMax
		}
		if swapLen < 0 {
			swapLen = 0
		}
		swapBar := strings.Repeat("█", swapLen) + strings.Repeat("░", barMax-swapLen)
		if width < 80 {
			fmt.Fprintf(b, "  • \033[1mWSL Swap:\033[0m [%s%s\033[0m] \033[1m%.1f%%\033[0m (%.2f/%.2f GB)\033[K\r\n",
				"\033[35m", swapBar, mem.SwapUsedPercent, swapUsedGB, swapTotalGB)
		} else {
			fmt.Fprintf(b, "  • \033[1mWSL Swap:\033[0m [%s%s\033[0m] \033[1m%.1f%%\033[0m (Used: %.2f GB / %.2f GB)\033[K\r\n",
				"\033[35m", swapBar, mem.SwapUsedPercent, swapUsedGB, swapTotalGB)
		}
	}

	fmt.Fprintf(b, "  • \033[1mBuffers & Cache:\033[0m %.2f GB\033[K\r\n\033[K\r\n", cachedGB)
	b.WriteString("  \033[37m💡 Tip: Press [P] to trigger 'docker system prune -f' to release cached RAM & reclaim vmmem.\033[0m\033[K\r\n")
}

func (a *App) renderVolumesTab(b *strings.Builder, width int, height int) {
	vols := a.cachedVolumes
	if len(vols) == 0 {
		b.WriteString(" \033[33mNo Docker volumes configured.\033[0m\033[K\r\n")
		return
	}

	pageSize := height - 12
	if pageSize < 4 {
		pageSize = 4
	}
	if pageSize > 10 {
		pageSize = 10
	}

	page := a.SelectedIndex / pageSize
	startIdx := page * pageSize
	endIdx := startIdx + pageSize
	if endIdx > len(vols) {
		endIdx = len(vols)
	}
	totalPages := (len(vols) + pageSize - 1) / pageSize
	if totalPages == 0 {
		totalPages = 1
	}

	fmt.Fprintf(b, " 💾 \033[1;36mDocker Volumes (%d total):\033[0m\033[K\r\n\033[K\r\n", len(vols))

	for i := startIdx; i < endIdx; i++ {
		v := vols[i]
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;32m▶ \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		maxName := width - 24
		if maxName < 15 {
			maxName = 15
		}

		fmt.Fprintf(b, "%s%s%2d. \033[1m%-*s\033[0m \033[35m[%s]\033[0m%s\033[K\r\n",
			cursor, highlightStart, i+1, maxName, truncateString(v.Name, maxName), v.Driver, highlightEnd)
	}

	fmt.Fprintf(b, "\033[K\r\n \033[37m[Page %d/%d · %d-%d of %d volumes · Press [P] to prune dangling volumes]\033[0m\033[K\r\n",
		page+1, totalPages, startIdx+1, endIdx, len(vols))
}

func (a *App) runNonInteractive() error {
	a.PrintStatus(os.Stdout)
	return nil
}

func (a *App) PrintStatus(w io.Writer) {
	fmt.Fprintln(w, "\n🐳 \033[1;36mAGYDOCKER - Container & WSL2 RAM Manager\033[0m")
	fmt.Fprintln(w, "──────────────────────────────────────────────────────────────────────────────────")
	mem, _ := dockerops.GetMemoryInfo()
	if mem != nil {
		totalGB := float64(mem.TotalKB) / (1024.0 * 1024.0)
		usedGB := float64(mem.UsedKB) / (1024.0 * 1024.0)
		fmt.Fprintf(w, " 🧠 WSL2 RAM: \033[1m%.2f / %.2f GB\033[0m (\033[33m%.1f%%\033[0m used)\n\n", usedGB, totalGB, mem.UsedPercent)
	}
	containers, err := dockerops.ListContainers()
	if err != nil {
		a.dockerErr = err
		fmt.Fprintf(w, " \033[1;37;41m ⚠️ Docker daemon is offline / cannot connect to docker.sock \033[0m\n\n")
	}
	fmt.Fprintf(w, " 🐳 Containers (%d total):\n", len(containers))
	for i, c := range containers {
		st := "\033[31m[Exited]\033[0m"
		if c.IsRunning {
			st = "\033[32m[Up]    \033[0m"
		}
		proj := c.ComposeProject
		if proj == "" {
			proj = "Standalone"
		}
		fmt.Fprintf(w, "  %2d. %s \033[1m%-22s\033[0m [%-18s] (%s)\n", i+1, st, c.Names, proj, c.Status)
	}
	fmt.Fprintln(w, "──────────────────────────────────────────────────────────────────────────────────")
}
