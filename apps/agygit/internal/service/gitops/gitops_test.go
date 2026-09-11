package gitops

import (
	"os"
	"os/exec"
	"path/filepath"
	"strings"
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

func TestGitOps_GitUndo(t *testing.T) {
	repo := createTestRepo(t)

	// Add a new commit
	file := filepath.Join(repo, "undo_test.txt")
	_ = os.WriteFile(file, []byte("will be undone"), 0644)
	err := Commit(repo, "feat: commit to undo", true)
	if err != nil {
		t.Fatalf("Commit failed: %v", err)
	}

	commitsBefore, _ := GetLog(repo, 5)
	if len(commitsBefore) < 2 {
		t.Fatalf("expected at least 2 commits before undo, got %d", len(commitsBefore))
	}

	// Undo commit
	if err := GitUndo(repo); err != nil {
		t.Fatalf("GitUndo failed: %v", err)
	}

	commitsAfter, _ := GetLog(repo, 5)
	if len(commitsAfter) != len(commitsBefore)-1 {
		t.Errorf("expected %d commits after undo, got %d", len(commitsBefore)-1, len(commitsAfter))
	}

	// Staged file should still exist with changes preserved
	st, err := GetRepoStatus(repo)
	if err != nil {
		t.Fatalf("GetRepoStatus failed: %v", err)
	}
	if st.StagedFiles == 0 {
		t.Errorf("expected staged files to be preserved after soft reset undo")
	}
}

func TestGitOps_ChangedFiles_Staging_Unstaging(t *testing.T) {
	repo := createTestRepo(t)

	// Create untracked file
	file := filepath.Join(repo, "test_file.txt")
	_ = os.WriteFile(file, []byte("some content\n"), 0644)

	files, err := GetChangedFiles(repo)
	if err != nil {
		t.Fatalf("GetChangedFiles failed: %v", err)
	}
	if len(files) != 1 {
		t.Fatalf("expected 1 changed file, got %d", len(files))
	}
	if !files[0].IsUntracked || files[0].IsStaged || files[0].Path != "test_file.txt" {
		t.Fatalf("unexpected changed file: %+v", files[0])
	}

	// Stage file
	if err := StageFile(repo, "test_file.txt"); err != nil {
		t.Fatalf("StageFile failed: %v", err)
	}

	filesAfterStage, err := GetChangedFiles(repo)
	if err != nil {
		t.Fatalf("GetChangedFiles failed: %v", err)
	}
	if len(filesAfterStage) != 1 {
		t.Fatalf("expected 1 file after staging, got %d", len(filesAfterStage))
	}
	if !filesAfterStage[0].IsStaged || filesAfterStage[0].IsUntracked {
		t.Fatalf("expected file to be staged: %+v", filesAfterStage[0])
	}

	// Unstage file
	if err := UnstageFile(repo, "test_file.txt"); err != nil {
		t.Fatalf("UnstageFile failed: %v", err)
	}

	filesAfterUnstage, err := GetChangedFiles(repo)
	if err != nil {
		t.Fatalf("GetChangedFiles failed: %v", err)
	}
	if len(filesAfterUnstage) != 1 {
		t.Fatalf("expected 1 file after unstaging, got %d", len(filesAfterUnstage))
	}
	if filesAfterUnstage[0].IsStaged {
		t.Fatalf("expected file to be unstaged: %+v", filesAfterUnstage[0])
	}
}

func TestGitOps_RejectFile_And_RejectAll(t *testing.T) {
	repo := createTestRepo(t)

	// Modify tracked file
	readme := filepath.Join(repo, "README.md")
	_ = os.WriteFile(readme, []byte("modified readme content\n"), 0644)

	// Create untracked file
	junk := filepath.Join(repo, "junk.txt")
	_ = os.WriteFile(junk, []byte("junk content\n"), 0644)

	// Reject untracked file
	if err := RejectFile(repo, "junk.txt", true); err != nil {
		t.Fatalf("RejectFile untracked failed: %v", err)
	}
	if _, err := os.Stat(junk); !os.IsNotExist(err) {
		t.Fatalf("expected junk.txt to be removed")
	}

	// Reject modified tracked file
	if err := RejectFile(repo, "README.md", false); err != nil {
		t.Fatalf("RejectFile tracked failed: %v", err)
	}
	content, _ := os.ReadFile(readme)
	if string(content) != "# Test Repo\n" {
		t.Fatalf("expected README.md to be restored, got: %s", string(content))
	}

	// Test RejectAll
	_ = os.WriteFile(readme, []byte("another modification\n"), 0644)
	newJunk := filepath.Join(repo, "junk2.txt")
	_ = os.WriteFile(newJunk, []byte("junk2 content\n"), 0644)

	if err := RejectAll(repo); err != nil {
		t.Fatalf("RejectAll failed: %v", err)
	}
	if _, err := os.Stat(newJunk); !os.IsNotExist(err) {
		t.Fatalf("expected junk2.txt to be removed by RejectAll")
	}
	contentAfter, _ := os.ReadFile(readme)
	if string(contentAfter) != "# Test Repo\n" {
		t.Fatalf("expected README.md to be restored by RejectAll, got: %s", string(contentAfter))
	}
}

func TestGitOps_GetFileDiff(t *testing.T) {
	repo := createTestRepo(t)

	// Modify README.md
	readme := filepath.Join(repo, "README.md")
	_ = os.WriteFile(readme, []byte("# Test Repo\nNew Line\n"), 0644)

	diffUnstaged, err := GetFileDiff(repo, "README.md", false)
	if err != nil {
		t.Fatalf("GetFileDiff unstaged failed: %v", err)
	}
	if !strings.Contains(diffUnstaged, "+New Line") {
		t.Fatalf("expected diff to contain '+New Line', got: %s", diffUnstaged)
	}

	// Stage README.md
	_ = StageFile(repo, "README.md")
	diffStaged, err := GetFileDiff(repo, "README.md", true)
	if err != nil {
		t.Fatalf("GetFileDiff staged failed: %v", err)
	}
	if !strings.Contains(diffStaged, "+New Line") {
		t.Fatalf("expected staged diff to contain '+New Line', got: %s", diffStaged)
	}

	// Untracked file preview
	untracked := filepath.Join(repo, "preview.txt")
	_ = os.WriteFile(untracked, []byte("Preview Line 1\nPreview Line 2\n"), 0644)
	diffUntracked, err := GetFileDiff(repo, "preview.txt", false)
	if err != nil {
		t.Fatalf("GetFileDiff untracked failed: %v", err)
	}
	if !strings.Contains(diffUntracked, "+Preview Line 1") {
		t.Fatalf("expected untracked diff to contain '+Preview Line 1', got: %s", diffUntracked)
	}
}

func TestGitOps_ConflictResolution(t *testing.T) {
	repo := createTestRepo(t)

	// Create branch conflict-branch
	_ = CheckoutBranch(repo, "conflict-branch", true)
	cFile := filepath.Join(repo, "conflict.txt")
	_ = os.WriteFile(cFile, []byte("theirs content\n"), 0644)
	_ = Commit(repo, "commit on branch", true)

	// Switch back to main
	_ = CheckoutBranch(repo, "main", false)
	_ = os.WriteFile(cFile, []byte("ours content\n"), 0644)
	_ = Commit(repo, "commit on main", true)

	// Merge branch to cause conflict
	_ = exec.Command("git", "-C", repo, "merge", "conflict-branch").Run()

	files, err := GetChangedFiles(repo)
	if err != nil {
		t.Fatalf("GetChangedFiles during conflict failed: %v", err)
	}
	foundConflict := false
	for _, f := range files {
		if f.Path == "conflict.txt" && f.IsConflict {
			foundConflict = true
			break
		}
	}
	if !foundConflict {
		t.Fatalf("expected conflict.txt to be flagged as conflict, got: %+v", files)
	}

	// Resolve with "ours"
	if err := ResolveConflict(repo, "conflict.txt", "ours"); err != nil {
		t.Fatalf("ResolveConflict failed: %v", err)
	}

	content, _ := os.ReadFile(cFile)
	if string(content) != "ours content\n" {
		t.Fatalf("expected 'ours content\\n', got: %s", string(content))
	}

	filesAfter, err := GetChangedFiles(repo)
	if err != nil {
		t.Fatalf("GetChangedFiles after resolve failed: %v", err)
	}
	for _, f := range filesAfter {
		if f.Path == "conflict.txt" {
			if f.IsConflict {
				t.Fatalf("expected conflict to be resolved, but IsConflict is true")
			}
			if !f.IsStaged {
				t.Fatalf("expected conflict.txt to be staged after resolution")
			}
		}
	}
}

func TestGitOps_CommitAmend_And_GetLastCommitMessage(t *testing.T) {
	repo := createTestRepo(t)

	// Check initial commit message
	msg, err := GetLastCommitMessage(repo)
	if err != nil {
		t.Fatalf("GetLastCommitMessage failed: %v", err)
	}
	if msg != "Initial test commit" {
		t.Errorf("expected 'Initial test commit', got '%s'", msg)
	}

	// Create and stage a new file
	f := filepath.Join(repo, "amend_test.txt")
	_ = os.WriteFile(f, []byte("amended file content"), 0644)
	if err := StageFile(repo, "amend_test.txt"); err != nil {
		t.Fatalf("StageFile failed: %v", err)
	}

	// Amend commit with a new message
	newMsg := "Initial test commit amended"
	if err := CommitAmend(repo, newMsg, false); err != nil {
		t.Fatalf("CommitAmend with new message failed: %v", err)
	}

	msg2, err := GetLastCommitMessage(repo)
	if err != nil {
		t.Fatalf("GetLastCommitMessage failed: %v", err)
	}
	if msg2 != newMsg {
		t.Errorf("expected '%s', got '%s'", newMsg, msg2)
	}

	// Amend commit with --no-edit
	f2 := filepath.Join(repo, "amend_test2.txt")
	_ = os.WriteFile(f2, []byte("amended file content 2"), 0644)
	if err := StageFile(repo, "amend_test2.txt"); err != nil {
		t.Fatalf("StageFile failed: %v", err)
	}

	if err := CommitAmend(repo, "", true); err != nil {
		t.Fatalf("CommitAmend --no-edit failed: %v", err)
	}

	msg3, err := GetLastCommitMessage(repo)
	if err != nil {
		t.Fatalf("GetLastCommitMessage failed: %v", err)
	}
	if msg3 != newMsg {
		t.Errorf("expected '%s' to be preserved with --no-edit, got '%s'", newMsg, msg3)
	}
}



