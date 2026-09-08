package gitops

import (
	"os"
	"os/exec"
	"path/filepath"
	"testing"
)

func createTestRepo(t *testing.T) string {
	t.Helper()
	dir := t.TempDir()

	cmdInit := exec.Command("git", "init", "-b", "main")
	cmdInit.Dir = dir
	if err := cmdInit.Run(); err != nil {
		// Fallback for older git
		cmdInitLegacy := exec.Command("git", "init")
		cmdInitLegacy.Dir = dir
		_ = cmdInitLegacy.Run()
	}

	// Config user
	_ = exec.Command("git", "-C", dir, "config", "user.email", "test@agygit.local").Run()
	_ = exec.Command("git", "-C", dir, "config", "user.name", "AgyGit Tester").Run()

	// Initial commit
	readme := filepath.Join(dir, "README.md")
	_ = os.WriteFile(readme, []byte("# Test Repo\n"), 0644)
	_ = exec.Command("git", "-C", dir, "add", ".").Run()
	_ = exec.Command("git", "-C", dir, "commit", "-m", "Initial test commit").Run()

	return dir
}

func TestGitOps_IsGitRepo_And_GetStatus(t *testing.T) {
	repo := createTestRepo(t)

	if !IsGitRepo(repo) {
		t.Fatalf("expected repo to be recognized as git repo")
	}

	st, err := GetRepoStatus(repo)
	if err != nil {
		t.Fatalf("GetRepoStatus failed: %v", err)
	}

	if !st.IsClean {
		t.Errorf("expected clean repo, got not clean")
	}

	// Create dirty file
	dirty := filepath.Join(repo, "dirty.txt")
	_ = os.WriteFile(dirty, []byte("change"), 0644)

	stDirty, err := GetRepoStatus(repo)
	if err != nil {
		t.Fatalf("GetRepoStatus on dirty failed: %v", err)
	}
	if stDirty.IsClean {
		t.Errorf("expected dirty status, got clean")
	}
	if stDirty.UntrackedFiles != 1 {
		t.Errorf("expected 1 untracked file, got %d", stDirty.UntrackedFiles)
	}
}

func TestGitOps_Worktrees(t *testing.T) {
	repo := createTestRepo(t)

	wts, err := ListWorktrees(repo)
	if err != nil {
		t.Fatalf("ListWorktrees failed: %v", err)
	}
	if len(wts) != 1 {
		t.Fatalf("expected 1 initial main worktree, got %d", len(wts))
	}
	if !wts[0].IsMain {
		t.Errorf("expected first worktree to be main")
	}

	// Add worktree
	newWt, err := AddWorktree(repo, "feat/agent-test", "")
	if err != nil {
		t.Fatalf("AddWorktree failed: %v", err)
	}
	if newWt.Branch != "feat/agent-test" {
		t.Errorf("expected branch feat/agent-test, got %s", newWt.Branch)
	}

	wtsAfter, _ := ListWorktrees(repo)
	if len(wtsAfter) != 2 {
		t.Errorf("expected 2 worktrees after addition, got %d", len(wtsAfter))
	}

	// Remove worktree
	err = RemoveWorktree(repo, newWt.Path, true)
	if err != nil {
		t.Fatalf("RemoveWorktree failed: %v", err)
	}

	wtsFinal, _ := ListWorktrees(repo)
	if len(wtsFinal) != 1 {
		t.Errorf("expected 1 worktree after deletion, got %d", len(wtsFinal))
	}
}

func TestGitOps_Commit_And_Graph(t *testing.T) {
	repo := createTestRepo(t)

	// Create a new file
	file := filepath.Join(repo, "feature.txt")
	_ = os.WriteFile(file, []byte("feature content"), 0644)

	err := Commit(repo, "feat: add feature file", true)
	if err != nil {
		t.Fatalf("Commit failed: %v", err)
	}

	graph, err := GetLogGraph(repo, 5)
	if err != nil {
		t.Fatalf("GetLogGraph failed: %v", err)
	}
	if len(graph) == 0 {
		t.Errorf("expected non-empty graph")
	}

	commits, _ := GetLog(repo, 5)
	if len(commits) < 2 {
		t.Errorf("expected at least 2 commits, got %d", len(commits))
	}
}

func TestGitOps_Branch_Merge_Squash(t *testing.T) {
	repo := createTestRepo(t)

	// Create and switch branch
	err := CheckoutBranch(repo, "feat/login", true)
	if err != nil {
		t.Fatalf("CheckoutBranch failed: %v", err)
	}

	file := filepath.Join(repo, "login.go")
	_ = os.WriteFile(file, []byte("package login"), 0644)
	_ = Commit(repo, "feat: implement login", true)

	// Switch back to main
	_ = CheckoutBranch(repo, "main", false)

	// Squash merge feat/login into main
	err = Merge(repo, "feat/login", true)
	if err != nil {
		t.Fatalf("Squash Merge failed: %v", err)
	}

	// Staged file should exist
	st, _ := GetRepoStatus(repo)
	if st.StagedFiles == 0 {
		t.Errorf("expected staged files after squash merge")
	}
}

func TestGitOps_CherryPick(t *testing.T) {
	repo := createTestRepo(t)

	_ = CheckoutBranch(repo, "feat/cp", true)
	file := filepath.Join(repo, "cp.txt")
	_ = os.WriteFile(file, []byte("cp content"), 0644)
	_ = Commit(repo, "feat: commit to pick", true)

	commits, _ := GetLog(repo, 1)
	if len(commits) == 0 {
		t.Fatalf("failed to retrieve commit for cherry-pick")
	}
	hash := commits[0].Hash

	_ = CheckoutBranch(repo, "main", false)
	err := CherryPick(repo, hash)
	if err != nil {
		t.Fatalf("CherryPick failed: %v", err)
	}
}

