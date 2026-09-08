package gitops

import (
	"bufio"
	"encoding/json"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"strconv"
	"strings"
	"sync"

	"agygit/internal/model"
)

// IsGitRepo checks whether path is a valid Git working directory or worktree
func IsGitRepo(path string) bool {
	gitPath := filepath.Join(path, ".git")
	fi, err := os.Stat(gitPath)
	if err != nil {
		return false
	}
	return fi.IsDir() || !fi.IsDir() // file for worktrees / submodules
}

// ScanFleet discovers git repositories under rootDir and from projects registry
func ScanFleet(rootDir string) ([]model.RepoStatus, error) {
	seen := make(map[string]bool)
	var paths []string

	// 1. Check registered projects if present in ~/.config/antigravity/projects.json
	homeDir, _ := os.UserHomeDir()
	regFile := filepath.Join(homeDir, ".config", "antigravity", "projects.json")
	if data, err := os.ReadFile(regFile); err == nil {
		var reg struct {
			Projects []struct {
				Path string `json:"path"`
			} `json:"projects"`
		}
		if err := json.Unmarshal(data, &reg); err == nil {
			for _, p := range reg.Projects {
				if IsGitRepo(p.Path) && !seen[p.Path] {
					seen[p.Path] = true
					paths = append(paths, p.Path)
				}
			}
		}
	}

	// 2. Scan direct children of rootDir
	if entries, err := os.ReadDir(rootDir); err == nil {
		for _, entry := range entries {
			if !entry.IsDir() {
				continue
			}
			fullPath := filepath.Join(rootDir, entry.Name())
			if IsGitRepo(fullPath) && !seen[fullPath] {
				seen[fullPath] = true
				paths = append(paths, fullPath)
			}
		}
	}

	// 3. Parallel inspection with goroutines (max 8 concurrent workers)
	var wg sync.WaitGroup
	var mu sync.Mutex
	var results []model.RepoStatus
	sem := make(chan struct{}, 8)

	for _, p := range paths {
		wg.Add(1)
		sem <- struct{}{}
		go func(repoPath string) {
			defer wg.Done()
			defer func() { <-sem }()
			st, err := GetRepoStatus(repoPath)
			if err == nil && st != nil {
				mu.Lock()
				results = append(results, *st)
				mu.Unlock()
			}
		}(p)
	}
	wg.Wait()

	return results, nil
}

// GetRepoStatus gathers Git status metrics for a single repository
func GetRepoStatus(repoPath string) (*model.RepoStatus, error) {
	if !IsGitRepo(repoPath) {
		return nil, fmt.Errorf("not a git repository: %s", repoPath)
	}

	st := &model.RepoStatus{
		Name:    filepath.Base(repoPath),
		Path:    repoPath,
		IsClean: true,
	}

	// Branch
	cmdBranch := exec.Command("git", "symbolic-ref", "--short", "HEAD")
	cmdBranch.Dir = repoPath
	if out, err := cmdBranch.Output(); err == nil {
		st.CurrentBranch = strings.TrimSpace(string(out))
	} else {
		// Detached HEAD
		cmdHash := exec.Command("git", "rev-parse", "--short", "HEAD")
		cmdHash.Dir = repoPath
		if outH, errH := cmdHash.Output(); errH == nil {
			st.CurrentBranch = fmt.Sprintf("detached@%s", strings.TrimSpace(string(outH)))
		} else {
			st.CurrentBranch = "unknown"
		}
	}

	// Status porcelain
	cmdStatus := exec.Command("git", "status", "--porcelain=v1")
	cmdStatus.Dir = repoPath
	if out, err := cmdStatus.Output(); err == nil {
		scanner := bufio.NewScanner(strings.NewReader(string(out)))
		for scanner.Scan() {
			line := scanner.Text()
			if len(line) < 2 {
				continue
			}
			x := line[0]
			y := line[1]

			if x == '?' && y == '?' {
				st.UntrackedFiles++
				st.IsClean = false
			} else {
				if x != ' ' {
					st.StagedFiles++
					st.IsClean = false
				}
				if y != ' ' {
					st.DirtyFiles++
					st.IsClean = false
				}
			}
		}
	}

	// Ahead / Behind upstream
	cmdAB := exec.Command("git", "rev-list", "--left-right", "--count", "HEAD...@{u}")
	cmdAB.Dir = repoPath
	if out, err := cmdAB.Output(); err == nil {
		parts := strings.Fields(string(out))
		if len(parts) >= 2 {
			ahead, _ := strconv.Atoi(parts[0])
			behind, _ := strconv.Atoi(parts[1])
			st.Ahead = ahead
			st.Behind = behind
		}
	}

	// Stashes
	cmdStash := exec.Command("git", "stash", "list")
	cmdStash.Dir = repoPath
	if out, err := cmdStash.Output(); err == nil {
		lines := strings.Split(strings.TrimSpace(string(out)), "\n")
		if len(lines) == 1 && lines[0] == "" {
			st.StashCount = 0
		} else {
			st.StashCount = len(lines)
		}
	}

	// Worktrees count
	cmdWt := exec.Command("git", "worktree", "list", "--porcelain")
	cmdWt.Dir = repoPath
	if out, err := cmdWt.Output(); err == nil {
		scanner := bufio.NewScanner(strings.NewReader(string(out)))
		wtCount := 0
		for scanner.Scan() {
			if strings.HasPrefix(scanner.Text(), "worktree ") {
				wtCount++
			}
		}
		st.WorktreeCount = wtCount
	}

	// Last commit
	cmdLog := exec.Command("git", "log", "-1", "--format=%h|%cr|%s")
	cmdLog.Dir = repoPath
	if out, err := cmdLog.Output(); err == nil {
		trimmed := strings.TrimSpace(string(out))
		if trimmed != "" {
			parts := strings.SplitN(trimmed, "|", 3)
			if len(parts) >= 3 {
				st.LastCommit = fmt.Sprintf("[%s] %s (%s)", parts[0], parts[2], parts[1])
			} else {
				st.LastCommit = trimmed
			}
		}
	}

	return st, nil
}

// ListWorktrees returns all worktrees for a repository
func ListWorktrees(repoPath string) ([]model.WorktreeInfo, error) {
	cmd := exec.Command("git", "worktree", "list", "--porcelain")
	cmd.Dir = repoPath
	out, err := cmd.Output()
	if err != nil {
		return nil, fmt.Errorf("error listing worktrees: %w", err)
	}

	var list []model.WorktreeInfo
	scanner := bufio.NewScanner(strings.NewReader(string(out)))
	var current model.WorktreeInfo
	isFirst := true

	repoName := filepath.Base(repoPath)

	for scanner.Scan() {
		line := scanner.Text()
		if strings.TrimSpace(line) == "" {
			if current.Path != "" {
				current.RepoName = repoName
				current.RepoPath = repoPath
				list = append(list, current)
				current = model.WorktreeInfo{}
			}
			continue
		}

		if strings.HasPrefix(line, "worktree ") {
			current.Path = strings.TrimPrefix(line, "worktree ")
			if isFirst {
				current.IsMain = true
				isFirst = false
			}
		} else if strings.HasPrefix(line, "HEAD ") {
			current.CommitHash = strings.TrimPrefix(line, "HEAD ")
		} else if strings.HasPrefix(line, "branch ") {
			ref := strings.TrimPrefix(line, "branch ")
			current.Branch = strings.TrimPrefix(ref, "refs/heads/")
		} else if strings.HasPrefix(line, "bare") {
			// bare repo marker
		} else if strings.HasPrefix(line, "locked") {
			current.IsLocked = true
		}
	}

	if current.Path != "" {
		current.RepoName = repoName
		current.RepoPath = repoPath
		list = append(list, current)
	}

	return list, nil
}

// AddWorktree creates a new isolated worktree for AI or feature branching
func AddWorktree(repoPath string, branch string, customPath string) (*model.WorktreeInfo, error) {
	branch = strings.TrimSpace(branch)
	if branch == "" {
		return nil, fmt.Errorf("branch name cannot be empty")
	}

	targetPath := customPath
	if targetPath == "" {
		sanitized := strings.ReplaceAll(branch, "/", "-")
		sanitized = strings.ReplaceAll(sanitized, " ", "-")
		targetPath = filepath.Join(repoPath, ".worktrees", sanitized)
	}

	_ = os.MkdirAll(filepath.Dir(targetPath), 0755)

	// Check if branch already exists
	cmdCheck := exec.Command("git", "rev-parse", "--verify", "refs/heads/"+branch)
	cmdCheck.Dir = repoPath
	branchExists := cmdCheck.Run() == nil

	var cmd *exec.Cmd
	if branchExists {
		cmd = exec.Command("git", "worktree", "add", targetPath, branch)
	} else {
		cmd = exec.Command("git", "worktree", "add", "-b", branch, targetPath)
	}
	cmd.Dir = repoPath

	out, err := cmd.CombinedOutput()
	if err != nil {
		return nil, fmt.Errorf("git worktree add failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}

	return &model.WorktreeInfo{
		RepoName: filepath.Base(repoPath),
		RepoPath: repoPath,
		Path:     targetPath,
		Branch:   branch,
		IsMain:   false,
	}, nil
}

// RemoveWorktree deletes a worktree and cleans directory
func RemoveWorktree(repoPath string, worktreePath string, force bool) error {
	args := []string{"worktree", "remove"}
	if force {
		args = append(args, "--force")
	}
	args = append(args, worktreePath)

	cmd := exec.Command("git", args...)
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("git worktree remove failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// Fetch runs git fetch on the repository
func Fetch(repoPath string) error {
	cmd := exec.Command("git", "fetch", "--all", "--prune")
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("fetch failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// Pull runs git pull --ff-only
func Pull(repoPath string) error {
	cmd := exec.Command("git", "pull", "--ff-only")
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("pull failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// Push runs git push
func Push(repoPath string) error {
	cmd := exec.Command("git", "push")
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("push failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// GetLog returns recent commits
func GetLog(repoPath string, maxCount int) ([]model.CommitInfo, error) {
	if maxCount <= 0 {
		maxCount = 5
	}
	cmd := exec.Command("git", "log", fmt.Sprintf("-n%d", maxCount), "--format=%h|%an|%cr|%s")
	cmd.Dir = repoPath
	out, err := cmd.Output()
	if err != nil {
		return nil, err
	}

	var commits []model.CommitInfo
	scanner := bufio.NewScanner(strings.NewReader(string(out)))
	for scanner.Scan() {
		line := scanner.Text()
		parts := strings.SplitN(line, "|", 4)
		if len(parts) == 4 {
			commits = append(commits, model.CommitInfo{
				Hash:         parts[0],
				Author:       parts[1],
				RelativeTime: parts[2],
				Message:      parts[3],
			})
		}
	}
	return commits, nil
}

// GetStatusSummary returns porcelain summary
func GetStatusSummary(repoPath string) (string, error) {
	cmd := exec.Command("git", "status", "-s")
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	return string(out), err
}

// GetLogGraph returns a colorful visual ASCII git graph
func GetLogGraph(repoPath string, maxCount int) (string, error) {
	if maxCount <= 0 {
		maxCount = 25
	}
	cmd := exec.Command("git", "log", "--graph", "--oneline", "--decorate", "--color=always", fmt.Sprintf("-n%d", maxCount))
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	return string(out), err
}

// Commit stages all (if addAll) and commits with message
func Commit(repoPath string, message string, addAll bool) error {
	message = strings.TrimSpace(message)
	if message == "" {
		return fmt.Errorf("commit message cannot be empty")
	}

	if addAll {
		cmdAdd := exec.Command("git", "add", "-A")
		cmdAdd.Dir = repoPath
		if out, err := cmdAdd.CombinedOutput(); err != nil {
			return fmt.Errorf("git add failed: %s (%w)", strings.TrimSpace(string(out)), err)
		}
	}

	cmdCommit := exec.Command("git", "commit", "-m", message)
	cmdCommit.Dir = repoPath
	out, err := cmdCommit.CombinedOutput()
	if err != nil {
		return fmt.Errorf("git commit failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// StageAll runs git add -A
func StageAll(repoPath string) error {
	cmd := exec.Command("git", "add", "-A")
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("stage all failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// UnstageAll runs git reset
func UnstageAll(repoPath string) error {
	cmd := exec.Command("git", "reset")
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("unstage failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// CherryPick cherry-picks a commit hash
func CherryPick(repoPath string, commitHash string) error {
	commitHash = strings.TrimSpace(commitHash)
	if commitHash == "" {
		return fmt.Errorf("commit hash cannot be empty")
	}
	cmd := exec.Command("git", "cherry-pick", commitHash)
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("cherry-pick failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// Merge merges a branch, optionally squashed
func Merge(repoPath string, branchName string, squash bool) error {
	branchName = strings.TrimSpace(branchName)
	if branchName == "" {
		return fmt.Errorf("branch name cannot be empty")
	}

	args := []string{"merge"}
	if squash {
		args = append(args, "--squash")
	}
	args = append(args, branchName)

	cmd := exec.Command("git", args...)
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("merge failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// Rebase rebases current branch onto upstream
func Rebase(repoPath string, upstreamBranch string) error {
	upstreamBranch = strings.TrimSpace(upstreamBranch)
	if upstreamBranch == "" {
		return fmt.Errorf("upstream branch cannot be empty")
	}
	cmd := exec.Command("git", "rebase", upstreamBranch)
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("rebase failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// AbortOperation aborts any ongoing merge, rebase, or cherry-pick
func AbortOperation(repoPath string) error {
	_ = exec.Command("git", "-C", repoPath, "merge", "--abort").Run()
	_ = exec.Command("git", "-C", repoPath, "rebase", "--abort").Run()
	_ = exec.Command("git", "-C", repoPath, "cherry-pick", "--abort").Run()
	return nil
}

// PullRebase runs git pull --rebase
func PullRebase(repoPath string) error {
	cmd := exec.Command("git", "pull", "--rebase")
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("pull --rebase failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// ListBranches returns all local branches and active branch
func ListBranches(repoPath string) ([]string, string, error) {
	cmd := exec.Command("git", "branch", "-a")
	cmd.Dir = repoPath
	out, err := cmd.Output()
	if err != nil {
		return nil, "", err
	}

	var branches []string
	var active string
	scanner := bufio.NewScanner(strings.NewReader(string(out)))
	for scanner.Scan() {
		line := scanner.Text()
		trimmed := strings.TrimSpace(line)
		if strings.HasPrefix(line, "*") {
			active = strings.TrimPrefix(trimmed, "* ")
			branches = append([]string{active}, branches...)
		} else if trimmed != "" && !strings.Contains(trimmed, "->") {
			branches = append(branches, trimmed)
		}
	}
	return branches, active, nil
}

// CheckoutBranch switches to a branch or creates a new one
func CheckoutBranch(repoPath string, branchName string, create bool) error {
	branchName = strings.TrimSpace(branchName)
	if branchName == "" {
		return fmt.Errorf("branch name cannot be empty")
	}
	args := []string{"checkout"}
	if create {
		args = append(args, "-b")
	}
	args = append(args, branchName)

	cmd := exec.Command("git", args...)
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("checkout failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// StashSave saves current working tree changes
func StashSave(repoPath string, message string) error {
	args := []string{"stash", "push"}
	if strings.TrimSpace(message) != "" {
		args = append(args, "-m", message)
	}
	cmd := exec.Command("git", args...)
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("stash failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// StashPop restores most recent stash
func StashPop(repoPath string) error {
	cmd := exec.Command("git", "stash", "pop")
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("stash pop failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// GetCommitDiff shows details/diff of a commit
func GetCommitDiff(repoPath string, commitHash string) (string, error) {
	cmd := exec.Command("git", "show", "--stat", "--color=always", commitHash)
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	return string(out), err
}

// GitUndo undoes the most recent commit keeping all changes staged (git reset --soft HEAD~1)
func GitUndo(repoPath string) error {
	cmd := exec.Command("git", "reset", "--soft", "HEAD~1")
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("git undo failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// GetChangedFiles runs git status --porcelain=v1 -uall and parses into []model.ChangedFile
func GetChangedFiles(repoPath string) ([]model.ChangedFile, error) {
	cmd := exec.Command("git", "status", "--porcelain=v1", "-uall")
	cmd.Dir = repoPath
	out, err := cmd.Output()
	if err != nil {
		return nil, fmt.Errorf("git status failed: %w", err)
	}

	var files []model.ChangedFile
	scanner := bufio.NewScanner(strings.NewReader(string(out)))
	for scanner.Scan() {
		line := scanner.Text()
		if len(line) < 3 {
			continue
		}
		x := line[0]
		y := line[1]
		rest := line[3:]

		cf := model.ChangedFile{
			IndexStatus:    x,
			WorkTreeStatus: y,
		}

		if x == '?' && y == '?' {
			cf.IsUntracked = true
			cf.Path = rest
			files = append(files, cf)
			continue
		}

		xy := line[:2]
		if xy == "UU" || xy == "AA" || xy == "UD" || xy == "DU" || xy == "DD" || xy == "AU" || xy == "UA" {
			cf.IsConflict = true
		} else if x != ' ' && x != '?' {
			cf.IsStaged = true
		}

		if strings.Contains(rest, " -> ") {
			parts := strings.Split(rest, " -> ")
			cf.OriginalPath = strings.Trim(parts[0], "\"")
			cf.Path = strings.Trim(parts[1], "\"")
		} else {
			cf.Path = strings.Trim(rest, "\"")
		}

		files = append(files, cf)
	}
	return files, nil
}

// StageFile runs git add -- <file>
func StageFile(repoPath, file string) error {
	cmd := exec.Command("git", "add", "--", file)
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("git add failed: %s (%w)", strings.TrimSpace(string(out)), err)
	}
	return nil
}

// UnstageFile runs git restore --staged -- <file> falling back to git reset HEAD -- <file>
func UnstageFile(repoPath, file string) error {
	cmd := exec.Command("git", "restore", "--staged", "--", file)
	cmd.Dir = repoPath
	if out, err := cmd.CombinedOutput(); err != nil {
		cmdFallback := exec.Command("git", "reset", "HEAD", "--", file)
		cmdFallback.Dir = repoPath
		if outFallback, errFallback := cmdFallback.CombinedOutput(); errFallback != nil {
			cmdRm := exec.Command("git", "rm", "--cached", "--", file)
			cmdRm.Dir = repoPath
			if outRm, errRm := cmdRm.CombinedOutput(); errRm != nil {
				return fmt.Errorf("git unstage failed: %s (%w)", strings.TrimSpace(string(outFallback)+" "+string(outRm)), errFallback)
			}
		}
		_ = out
	}
	return nil
}

// RejectFile rejects changes: if untracked runs git clean -fd -- <file>, else git restore -- <file>
func RejectFile(repoPath, file string, isUntracked bool) error {
	if isUntracked {
		cmd := exec.Command("git", "clean", "-fd", "--", file)
		cmd.Dir = repoPath
		if out, err := cmd.CombinedOutput(); err != nil {
			fullPath := filepath.Join(repoPath, file)
			if errRm := os.RemoveAll(fullPath); errRm != nil {
				return fmt.Errorf("git clean failed: %s (%w)", strings.TrimSpace(string(out)), err)
			}
		}
		return nil
	}

	cmd := exec.Command("git", "restore", "--", file)
	cmd.Dir = repoPath
	if out, err := cmd.CombinedOutput(); err != nil {
		cmdFallback := exec.Command("git", "checkout", "--", file)
		cmdFallback.Dir = repoPath
		if outFallback, errFallback := cmdFallback.CombinedOutput(); errFallback != nil {
			return fmt.Errorf("git restore failed: %s (%w)", strings.TrimSpace(string(outFallback)), errFallback)
		}
		_ = out
	}
	return nil
}

// RejectAll runs git restore . and git clean -fd
func RejectAll(repoPath string) error {
	cmdRestore := exec.Command("git", "restore", ".")
	cmdRestore.Dir = repoPath
	if out, err := cmdRestore.CombinedOutput(); err != nil {
		cmdCheckout := exec.Command("git", "checkout", "--", ".")
		cmdCheckout.Dir = repoPath
		if outC, errC := cmdCheckout.CombinedOutput(); errC != nil {
			return fmt.Errorf("git restore . failed: %s (%w)", strings.TrimSpace(string(outC)), errC)
		}
		_ = out
	}

	cmdClean := exec.Command("git", "clean", "-fd")
	cmdClean.Dir = repoPath
	outClean, errClean := cmdClean.CombinedOutput()
	if errClean != nil {
		return fmt.Errorf("git clean -fd failed: %s (%w)", strings.TrimSpace(string(outClean)), errClean)
	}
	return nil
}

// GetFileDiff returns diff for a file; if staged runs git diff --staged -- <file>, else git diff -- <file>
// If untracked, reads file content or returns untracked preview
func GetFileDiff(repoPath, file string, staged bool) (string, error) {
	var cmd *exec.Cmd
	if staged {
		cmd = exec.Command("git", "diff", "--staged", "--", file)
	} else {
		cmd = exec.Command("git", "diff", "--", file)
	}
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return string(out), err
	}

	res := string(out)
	if strings.TrimSpace(res) != "" {
		return res, nil
	}

	fullPath := filepath.Join(repoPath, file)
	cmdCheck := exec.Command("git", "ls-files", "--error-unmatch", "--", file)
	cmdCheck.Dir = repoPath
	if errCheck := cmdCheck.Run(); errCheck != nil {
		data, readErr := os.ReadFile(fullPath)
		if readErr == nil {
			var sb strings.Builder
			sb.WriteString(fmt.Sprintf("--- /dev/null\n+++ b/%s\n", file))
			lines := strings.Split(string(data), "\n")
			for _, l := range lines {
				sb.WriteString("+" + l + "\n")
			}
			return sb.String(), nil
		}
	}

	return res, nil
}

// ResolveConflict resolves a conflict using "ours" or "theirs" strategy and marks as resolved with git add
func ResolveConflict(repoPath, file string, strategy string) error {
	strategy = strings.TrimSpace(strings.ToLower(strategy))
	if strategy != "ours" && strategy != "theirs" {
		return fmt.Errorf("invalid conflict resolution strategy '%s', must be 'ours' or 'theirs'", strategy)
	}

	cmd := exec.Command("git", "checkout", "--"+strategy, "--", file)
	cmd.Dir = repoPath
	out, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("git checkout --%s failed: %s (%w)", strategy, strings.TrimSpace(string(out)), err)
	}

	cmdAdd := exec.Command("git", "add", "--", file)
	cmdAdd.Dir = repoPath
	outAdd, errAdd := cmdAdd.CombinedOutput()
	if errAdd != nil {
		return fmt.Errorf("git add failed: %s (%w)", strings.TrimSpace(string(outAdd)), errAdd)
	}

	return nil
}


