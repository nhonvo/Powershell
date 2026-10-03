package resolver

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"time"

	"agyreview/internal/model"
)

// ResolveLocal inspects a local filesystem path, validates existence, and probes git metadata.
func ResolveLocal(rawPath string, scope model.ReviewScope) (*model.TargetRepo, error) {
	absPath, err := filepath.Abs(rawPath)
	if err != nil {
		return nil, fmt.Errorf("failed to get absolute path for %s: %w", rawPath, err)
	}

	info, err := os.Stat(absPath)
	if err != nil {
		return nil, fmt.Errorf("target path %s does not exist: %w", absPath, err)
	}
	if !info.IsDir() {
		return nil, fmt.Errorf("target path %s is not a directory", absPath)
	}

	target := &model.TargetRepo{
		Name:         filepath.Base(absPath),
		Path:         absPath,
		Scope:        scope,
		DiscoveredAt: time.Now(),
	}

	// Probe git metadata if .git directory or worktree exists
	gitDir := filepath.Join(absPath, ".git")
	if _, err := os.Stat(gitDir); err == nil {
		if branch, err := runGit(absPath, "rev-parse", "--abbrev-ref", "HEAD"); err == nil {
			target.Branch = strings.TrimSpace(branch)
		}
		if commit, err := runGit(absPath, "rev-parse", "HEAD"); err == nil {
			target.HeadCommit = strings.TrimSpace(commit)
		}
		if msg, err := runGit(absPath, "log", "-1", "--pretty=%s"); err == nil {
			target.CommitMsg = strings.TrimSpace(msg)
		}
		if author, err := runGit(absPath, "log", "-1", "--pretty=%an <%ae>"); err == nil {
			target.Author = strings.TrimSpace(author)
		}
		if origin, err := runGit(absPath, "config", "--get", "remote.origin.url"); err == nil {
			target.OriginURL = strings.TrimSpace(origin)
		}
	} else {
		target.Branch = "none (non-git)"
		target.HeadCommit = "none"
	}

	return target, nil
}

func runGit(dir string, args ...string) (string, error) {
	cmd := exec.Command("git", args...)
	cmd.Dir = dir
	out, err := cmd.Output()
	if err != nil {
		return "", err
	}
	return string(out), nil
}
