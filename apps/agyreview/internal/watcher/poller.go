package watcher

import (
	"context"
	"fmt"
	"os/exec"
	"strings"
	"sync"
	"time"

	"agyreview/internal/model"
)

type ChangeEvent struct {
	RepoPath    string
	OldCommit   string
	NewCommit   string
	OldBranch   string
	NewBranch   string
	TriggerMode model.TriggerMode
}

type ChangeHandler func(event ChangeEvent)

type GitPoller struct {
	config  *model.AppConfig
	mu      sync.RWMutex
	handler ChangeHandler
}

func NewGitPoller(cfg *model.AppConfig, handler ChangeHandler) *GitPoller {
	return &GitPoller{
		config:  cfg,
		handler: handler,
	}
}

// CheckRepo checks a single repository for new commits or branches.
func (p *GitPoller) CheckRepo(item *model.WatchItem) (*ChangeEvent, error) {
	// Query current branch
	branchCmd := exec.Command("git", "rev-parse", "--abbrev-ref", "HEAD")
	branchCmd.Dir = item.Path
	branchOut, err := branchCmd.Output()
	if err != nil {
		return nil, fmt.Errorf("git error on %s: %w", item.Path, err)
	}
	currentBranch := strings.TrimSpace(string(branchOut))

	// Query head commit
	commitCmd := exec.Command("git", "rev-parse", "HEAD")
	commitCmd.Dir = item.Path
	commitOut, err := commitCmd.Output()
	if err != nil {
		return nil, fmt.Errorf("git error on %s: %w", item.Path, err)
	}
	currentCommit := strings.TrimSpace(string(commitOut))

	hasChange := false
	var evt *ChangeEvent

	if item.LastCommit != "" && (item.LastCommit != currentCommit || item.LastBranch != currentBranch) {
		hasChange = true
		evt = &ChangeEvent{
			RepoPath:    item.Path,
			OldCommit:   item.LastCommit,
			NewCommit:   currentCommit,
			OldBranch:   item.LastBranch,
			NewBranch:   currentBranch,
			TriggerMode: item.TriggerMode,
		}
	}

	item.LastCommit = currentCommit
	item.LastBranch = currentBranch
	item.LastChecked = time.Now().Format(time.RFC3339)

	if hasChange {
		return evt, nil
	}
	return nil, nil
}

// StartPolling starts the background ticker loop.
func (p *GitPoller) StartPolling(ctx context.Context) {
	interval := time.Duration(p.config.PollIntervalSec) * time.Second
	ticker := time.NewTicker(interval)
	defer ticker.Stop()

	for {
		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
			p.mu.Lock()
			for i := range p.config.WatchedRepos {
				item := &p.config.WatchedRepos[i]
				evt, err := p.CheckRepo(item)
				if err == nil && evt != nil && p.handler != nil {
					p.handler(*evt)
				}
			}
			_ = model.SaveConfig(p.config)
			p.mu.Unlock()
		}
	}
}
