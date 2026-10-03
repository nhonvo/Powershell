package engine

import (
	"bufio"
	"fmt"
	"io"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"sync"
	"syscall"
	"time"

	"github.com/creack/pty"
	"golang.org/x/term"

	"agyswarm/internal/model"
)

type SpawnConfig struct {
	Name         string
	AccountName  string
	WorkspaceDir string
	Command      string
	Args         []string
	Env          map[string]string
}

type Manager struct {
	mu       sync.RWMutex
	sessions map[string]*model.AgentSession
	ptys     map[string]*os.File
	cmds     map[string]*exec.Cmd
	UserHome string
}

func NewManager(userHome string) *Manager {
	if userHome == "" {
		if h, err := os.UserHomeDir(); err == nil {
			userHome = h
		}
	}
	return &Manager{
		sessions: make(map[string]*model.AgentSession),
		ptys:     make(map[string]*os.File),
		cmds:     make(map[string]*exec.Cmd),
		UserHome: userHome,
	}
}

// Spawn launches a child terminal agent process in its own PTY.
func (m *Manager) Spawn(cfg SpawnConfig) (*model.AgentSession, error) {
	m.mu.Lock()
	defer m.mu.Unlock()

	id := fmt.Sprintf("agent-%d", len(m.sessions)+1)
	if cfg.Name == "" {
		cfg.Name = id
	}

	ws := cfg.WorkspaceDir
	if ws == "" {
		ws = m.UserHome
	}

	cmd := exec.Command(cfg.Command, cfg.Args...)
	cmd.Dir = ws

	// Setup isolated environment per account, stripping parent environment variables
	var env []string
	for _, envStr := range os.Environ() {
		parts := strings.SplitN(envStr, "=", 2)
		if len(parts) == 2 {
			k := parts[0]
			if k != "GEMINI_HOME" && k != "GEMINI_CLI_HOME" && k != "AGY_ACTIVE_ACCOUNT" {
				env = append(env, envStr)
			}
		}
	}

	if cfg.AccountName != "" {
		acc := strings.TrimSpace(cfg.AccountName)
		accountDir := filepath.Join(m.UserHome, fmt.Sprintf(".gemini_%s", acc))
		if acc == "default" {
			accountDir = filepath.Join(m.UserHome, ".gemini")
		}
		if _, err := os.Stat(accountDir); err != nil {
			_ = os.MkdirAll(accountDir, 0755)
		}
		env = append(env, fmt.Sprintf("GEMINI_HOME=%s", accountDir))
		env = append(env, fmt.Sprintf("GEMINI_CLI_HOME=%s", accountDir))
		env = append(env, fmt.Sprintf("AGY_ACTIVE_ACCOUNT=%s", acc))
	}
	for k, v := range cfg.Env {
		env = append(env, fmt.Sprintf("%s=%s", k, v))
	}
	cmd.Env = env

	// Start inside dedicated Pseudo-Terminal
	ptmx, err := pty.Start(cmd)
	if err != nil {
		return nil, fmt.Errorf("failed to start pty: %w", err)
	}

	session := &model.AgentSession{
		ID:           id,
		Name:         cfg.Name,
		AccountName:  cfg.AccountName,
		WorkspaceDir: ws,
		Command:      cfg.Command,
		Args:         cfg.Args,
		Status:       model.StatusWorking,
		PID:          cmd.Process.Pid,
		CreatedAt:    time.Now(),
		Lines:        make([]string, 0, 100),
	}

	m.sessions[id] = session
	m.ptys[id] = ptmx
	m.cmds[id] = cmd

	// Background reader for child terminal output
	go m.readPTYOutput(id, ptmx, cmd)

	return session, nil
}

func (m *Manager) readPTYOutput(id string, ptmx *os.File, cmd *exec.Cmd) {
	scanner := bufio.NewScanner(ptmx)
	maxCap := 500

	for scanner.Scan() {
		line := scanner.Text()
		m.mu.RLock()
		sess, exists := m.sessions[id]
		m.mu.RUnlock()
		if !exists {
			break
		}

		sess.AppendLine(line, maxCap)
		recent := sess.GetRecentLines(5)
		newStatus, hint := DetectStatus(recent, true, 0)
		sess.Status = newStatus
		sess.PromptHint = hint
	}

	// Process exited
	exitCode := 0
	if err := cmd.Wait(); err != nil {
		if exiterr, ok := err.(*exec.ExitError); ok {
			exitCode = exiterr.ExitCode()
		} else {
			exitCode = 1
		}
	}

	m.mu.RLock()
	sess, exists := m.sessions[id]
	m.mu.RUnlock()
	if exists {
		sess.ExitCode = exitCode
		recent := sess.GetRecentLines(5)
		newStatus, hint := DetectStatus(recent, false, exitCode)
		sess.Status = newStatus
		sess.PromptHint = hint
	}
	_ = ptmx.Close()
}

func (m *Manager) getLocked(idOrName string) *model.AgentSession {
	if s, exists := m.sessions[idOrName]; exists {
		return s
	}
	for _, s := range m.sessions {
		if s.Name == idOrName {
			return s
		}
	}
	return nil
}

// List returns all active and recent agent sessions.
func (m *Manager) List() []*model.AgentSession {
	m.mu.RLock()
	defer m.mu.RUnlock()

	var list []*model.AgentSession
	for _, s := range m.sessions {
		list = append(list, s)
	}
	return list
}

// Get returns an agent session by ID or Name.
func (m *Manager) Get(idOrName string) *model.AgentSession {
	m.mu.RLock()
	defer m.mu.RUnlock()
	return m.getLocked(idOrName)
}

// SendInput writes data directly into the child agent's PTY stdin.
func (m *Manager) SendInput(idOrName string, data []byte) error {
	m.mu.RLock()
	defer m.mu.RUnlock()

	sess := m.getLocked(idOrName)
	if sess == nil {
		return fmt.Errorf("session not found: %s", idOrName)
	}

	ptmx, ok := m.ptys[sess.ID]
	if !ok {
		return fmt.Errorf("pty not found for %s", idOrName)
	}
	_, err := ptmx.Write(data)
	return err
}

// Kill terminates an agent process.
func (m *Manager) Kill(idOrName string) error {
	m.mu.Lock()
	defer m.mu.Unlock()

	sess := m.getLocked(idOrName)
	if sess == nil {
		return fmt.Errorf("session not found: %s", idOrName)
	}

	cmd, ok := m.cmds[sess.ID]
	if ok && cmd.Process != nil {
		_ = cmd.Process.Signal(syscall.SIGTERM)
		time.Sleep(20 * time.Millisecond)
		_ = cmd.Process.Kill()
	}
	if ptmx, ok := m.ptys[sess.ID]; ok {
		_ = ptmx.Close()
		delete(m.ptys, sess.ID)
	}
	sess.Status = model.StatusDone
	sess.ExitCode = 137
	return nil
}

// Attach directly connects raw host os.Stdin and os.Stdout to the agent PTY.
// Pressing Ctrl+] (0x1D) detaches back to the Cockpit without killing the agent.
func (m *Manager) Attach(idOrName string) error {
	sess := m.Get(idOrName)
	if sess == nil {
		return fmt.Errorf("session not found: %s", idOrName)
	}

	m.mu.RLock()
	ptmx, ok := m.ptys[sess.ID]
	m.mu.RUnlock()
	if !ok {
		return fmt.Errorf("pty not found for session %s", sess.ID)
	}

	// Switch host terminal into raw mode
	stdinFd := int(os.Stdin.Fd())
	oldState, err := term.MakeRaw(stdinFd)
	if err != nil {
		return fmt.Errorf("failed to set raw terminal: %w", err)
	}
	defer func() {
		_ = term.Restore(stdinFd, oldState)
	}()

	// Synchronize window size
	if w, h, err := term.GetSize(stdinFd); err == nil {
		_ = pty.Setsize(ptmx, &pty.Winsize{Rows: uint16(h), Cols: uint16(w)})
	}

	fmt.Print("\033[2J\033[H")
	fmt.Printf("\r\n\033[1;36m[agyswarm]\033[0m Attached to agent '\033[1;32m%s\033[0m' (PID: %d). \033[33mPress Ctrl+] to detach.\033[0m\r\n\r\n", sess.Name, sess.PID)

	// Stream PTY output to stdout
	done := make(chan struct{})
	go func() {
		_, _ = io.Copy(os.Stdout, ptmx)
		close(done)
	}()

	// Intercept stdin: check for Ctrl+] (0x1D) escape key
	buf := make([]byte, 128)
	for {
		select {
		case <-done:
			return nil
		default:
			n, err := os.Stdin.Read(buf)
			if err != nil || n == 0 {
				return nil
			}

			// Check for Ctrl+] (ASCII 29 / 0x1D)
			for i := 0; i < n; i++ {
				if buf[i] == 0x1D {
					fmt.Print("\r\n\033[33m[agyswarm] Detached from agent. Returning to Cockpit...\033[0m\r\n")
					return nil
				}
			}

			// Forward input to child PTY
			if _, err := ptmx.Write(buf[:n]); err != nil {
				return nil
			}
		}
	}
}
