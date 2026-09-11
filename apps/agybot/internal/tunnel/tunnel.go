package tunnel

import (
	"bufio"
	"context"
	"fmt"
	"os/exec"
	"regexp"
	"sync"
	"time"
)

type CloudflareTunnel struct {
	mu        sync.RWMutex
	cmd       *exec.Cmd
	PublicURL string
	Port      int
	IsActive  bool
	cancel    context.CancelFunc
}

func NewCloudflareTunnel() *CloudflareTunnel {
	return &CloudflareTunnel{}
}

// StartQuickTunnel spawns cloudflared tunnel --url http://localhost:<port> and captures assigned HTTPS URL
func (t *CloudflareTunnel) StartQuickTunnel(ctx context.Context, port int) (string, error) {
	t.mu.Lock()
	defer t.mu.Unlock()

	if t.IsActive && t.PublicURL != "" {
		return t.PublicURL, nil
	}

	bin, err := exec.LookPath("cloudflared")
	if err != nil {
		bin, err = exec.LookPath("cloudflared.exe")
		if err != nil {
			return "", fmt.Errorf("cloudflared binary not found in PATH")
		}
	}

	tCtx, cancel := context.WithCancel(ctx)
	t.cancel = cancel
	t.Port = port

	cmd := exec.CommandContext(tCtx, bin, "tunnel", "--url", fmt.Sprintf("http://localhost:%d", port))
	stderr, err := cmd.StderrPipe()
	if err != nil {
		cancel()
		return "", fmt.Errorf("failed to open cloudflared stderr pipe: %w", err)
	}

	if err := cmd.Start(); err != nil {
		cancel()
		return "", fmt.Errorf("failed to start cloudflared: %w", err)
	}
	t.cmd = cmd

	urlChan := make(chan string, 1)
	errChan := make(chan error, 1)

	go func() {
		scanner := bufio.NewScanner(stderr)
		urlRegex := regexp.MustCompile(`https://[a-zA-Z0-9-]+\.trycloudflare\.com`)
		for scanner.Scan() {
			line := scanner.Text()
			if match := urlRegex.FindString(line); match != "" {
				urlChan <- match
				break
			}
		}
	}()

	select {
	case u := <-urlChan:
		t.PublicURL = u
		t.IsActive = true
		return u, nil
	case err := <-errChan:
		t.Stop()
		return "", err
	case <-time.After(15 * time.Second):
		t.Stop()
		return "", fmt.Errorf("timeout waiting for Cloudflare Tunnel URL")
	}
}

// Stop terminates active tunnel process
func (t *CloudflareTunnel) Stop() {
	t.mu.Lock()
	defer t.mu.Unlock()

	if t.cancel != nil {
		t.cancel()
		t.cancel = nil
	}
	if t.cmd != nil && t.cmd.Process != nil {
		_ = t.cmd.Process.Kill()
		t.cmd = nil
	}
	t.IsActive = false
	t.PublicURL = ""
}
