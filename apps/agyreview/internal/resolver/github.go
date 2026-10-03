package resolver

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"regexp"
	"strings"
	"time"

	"agyreview/internal/model"
)

var ghRepoRegex = regexp.MustCompile(`^(?:https?://)?github\.com/([a-zA-Z0-9_\-\.]+)/([a-zA-Z0-9_\-\.]+?)(?:\.git)?(?:/)?$`)

// IsGitHubURL returns true if the input looks like a GitHub repo URL or slug (e.g. owner/repo or github.com/owner/repo).
func IsGitHubURL(input string) bool {
	trimmed := strings.TrimSpace(input)
	if ghRepoRegex.MatchString(trimmed) {
		return true
	}
	if strings.Contains(trimmed, "github.com/") && !strings.Contains(trimmed, "/pull/") {
		return true
	}
	return false
}

// ResolveGitHub clones or updates a GitHub repo into an isolated cache directory.
func ResolveGitHub(rawURL string, branch string, scope model.ReviewScope) (*model.TargetRepo, error) {
	cleanURL := strings.TrimSpace(rawURL)
	if !strings.HasPrefix(cleanURL, "http://") && !strings.HasPrefix(cleanURL, "https://") && !strings.HasPrefix(cleanURL, "git@") {
		cleanURL = "https://" + cleanURL
	}
	if !strings.HasSuffix(cleanURL, ".git") && !strings.Contains(cleanURL, "@") {
		cleanURL = strings.TrimSuffix(cleanURL, "/") + ".git"
	}

	matches := ghRepoRegex.FindStringSubmatch(strings.TrimSuffix(cleanURL, ".git"))
	var owner, repoName string
	if len(matches) >= 3 {
		owner = matches[1]
		repoName = matches[2]
	} else {
		parts := strings.Split(cleanURL, "/")
		if len(parts) >= 2 {
			repoName = strings.TrimSuffix(parts[len(parts)-1], ".git")
			owner = parts[len(parts)-2]
		} else {
			owner = "repo"
			repoName = "cache"
		}
	}

	home, err := os.UserHomeDir()
	if err != nil {
		return nil, err
	}
	cacheDir := filepath.Join(home, ".cache", "antigravity", "repos", owner, repoName)
	if err := os.MkdirAll(filepath.Dir(cacheDir), 0755); err != nil {
		return nil, err
	}

	if _, err := os.Stat(filepath.Join(cacheDir, ".git")); os.IsNotExist(err) {
		// Shallow clone
		args := []string{"clone", "--depth", "1"}
		if branch != "" {
			args = append(args, "--branch", branch)
		}
		args = append(args, cleanURL, cacheDir)
		cmd := exec.Command("git", args...)
		if out, err := cmd.CombinedOutput(); err != nil {
			return nil, fmt.Errorf("git clone failed for %s: %s (%w)", cleanURL, string(out), err)
		}
	} else {
		// Fetch updates
		cmd := exec.Command("git", "fetch", "--depth", "1")
		cmd.Dir = cacheDir
		_ = cmd.Run()
		if branch != "" {
			_ = exec.Command("git", "-C", cacheDir, "checkout", branch).Run()
		}
	}

	target, err := ResolveLocal(cacheDir, scope)
	if err != nil {
		return nil, err
	}
	target.OriginURL = cleanURL
	target.IsRemote = true
	target.DiscoveredAt = time.Now()
	return target, nil
}
