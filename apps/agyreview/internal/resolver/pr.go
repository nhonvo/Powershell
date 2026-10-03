package resolver

import (
	"fmt"
	"os/exec"
	"path/filepath"
	"regexp"
	"strconv"
	"strings"
	"time"

	"agyreview/internal/model"
)

var prURLRegex = regexp.MustCompile(`github\.com/([a-zA-Z0-9_\-\.]+)/([a-zA-Z0-9_\-\.]+)/pull/([0-9]+)`)

// IsPRURL checks if a given input string is a GitHub Pull Request URL.
func IsPRURL(input string) bool {
	return prURLRegex.MatchString(strings.TrimSpace(input))
}

// ParsePRURL extracts owner, repo, and PR number from a PR URL.
func ParsePRURL(rawURL string) (*model.PRInfo, error) {
	matches := prURLRegex.FindStringSubmatch(strings.TrimSpace(rawURL))
	if len(matches) < 4 {
		return nil, fmt.Errorf("invalid GitHub PR URL format: %s", rawURL)
	}

	num, err := strconv.Atoi(matches[3])
	if err != nil {
		return nil, fmt.Errorf("invalid PR number: %v", err)
	}

	info := &model.PRInfo{
		RepoOwner: matches[1],
		RepoName:  matches[2],
		Number:    num,
		DiffURL:   fmt.Sprintf("https://github.com/%s/%s/pull/%d.diff", matches[1], matches[2], num),
		HTMLURL:   fmt.Sprintf("https://github.com/%s/%s/pull/%d", matches[1], matches[2], num),
	}
	return info, nil
}

// ResolvePR clones the repo and fetches the PR head ref into a local workspace ready for review.
func ResolvePR(prURL string) (*model.TargetRepo, error) {
	prInfo, err := ParsePRURL(prURL)
	if err != nil {
		return nil, err
	}

	cloneURL := fmt.Sprintf("https://github.com/%s/%s.git", prInfo.RepoOwner, prInfo.RepoName)
	target, err := ResolveGitHub(cloneURL, "", model.ScopePRDiff)
	if err != nil {
		return nil, fmt.Errorf("failed to clone base repo for PR: %w", err)
	}

	target.PRNumber = prInfo.Number
	target.Scope = model.ScopePRDiff
	target.DiscoveredAt = time.Now()

	// Fetch PR head ref: git fetch origin pull/<id>/head:pr-<id>
	prBranch := fmt.Sprintf("pr-%d", prInfo.Number)
	refSpec := fmt.Sprintf("pull/%d/head:%s", prInfo.Number, prBranch)
	fetchCmd := exec.Command("git", "fetch", "origin", refSpec)
	fetchCmd.Dir = target.Path
	if out, err := fetchCmd.CombinedOutput(); err != nil {
		// If pull ref fails, fallback to reviewing current branch with PR context
		_ = out
	} else {
		// Checkout PR branch
		_ = exec.Command("git", "-C", target.Path, "checkout", prBranch).Run()
		target.Branch = prBranch
		if commit, err := runGit(target.Path, "rev-parse", "HEAD"); err == nil {
			target.HeadCommit = strings.TrimSpace(commit)
		}
	}

	// Read changed files in PR: git diff --name-only origin/main...HEAD or HEAD~1
	diffCmd := exec.Command("git", "diff", "--name-only", "HEAD~1..HEAD")
	diffCmd.Dir = target.Path
	if out, err := diffCmd.Output(); err == nil {
		lines := strings.Split(strings.TrimSpace(string(out)), "\n")
		for _, l := range lines {
			if trimmed := strings.TrimSpace(l); trimmed != "" {
				prInfo.ChangedFiles = append(prInfo.ChangedFiles, filepath.ToSlash(trimmed))
			}
		}
	}

	return target, nil
}
