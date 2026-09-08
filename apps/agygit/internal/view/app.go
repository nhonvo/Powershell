package view

import (
	"bufio"
	"fmt"
	"io"
	"os"
	"os/exec"
	"path/filepath"
	"strings"

	"golang.org/x/term"

	"agygit/internal/model"
	"agygit/internal/service/gitops"
)

type App struct {
	RootDir        string
	ActiveRepoPath string
	ActiveTab      int // 0: Status/Commit, 1: Graph/Log, 2: Branch/Rebase, 3: Worktrees, 4: Fleet
	SelectedIndex  int
	StatusMsg      string

	// View Modes & Navigation
	GraphViewMode int  // 0: Commit Table (Paginated), 1: ASCII Tree Graph
	tabSwitched   bool // Forces clean screen repaint on tab or mode switch

	// Cached state
	cachedFleet     []model.RepoStatus
	cachedStatus    *model.RepoStatus
	cachedSummary   string
	cachedFiles     []model.ChangedFile
	cachedCommits   []model.CommitInfo
	cachedGraph     string
	cachedBranches  []string
	activeBranch    string
	cachedWorktrees []model.WorktreeInfo
	needsReload     bool
}

func NewApp(rootDir string) *App {
	activeRepo := "."
	if !gitops.IsGitRepo(activeRepo) {
		activeRepo = ""
	} else {
		if abs, err := filepath.Abs(activeRepo); err == nil {
			activeRepo = abs
		}
	}

	return &App{
		RootDir:        rootDir,
		ActiveRepoPath: activeRepo,
		ActiveTab:      0,
		tabSwitched:    true,
		needsReload:    true,
	}
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
		if a.needsReload || a.cachedFleet == nil {
			a.cachedFleet, _ = gitops.ScanFleet(a.RootDir)
			if a.ActiveRepoPath == "" && len(a.cachedFleet) > 0 {
				a.ActiveRepoPath = a.cachedFleet[0].Path
			}
		}

		if a.needsReload || a.cachedStatus == nil {
			if a.ActiveRepoPath != "" {
				a.cachedStatus, _ = gitops.GetRepoStatus(a.ActiveRepoPath)
				a.cachedSummary, _ = gitops.GetStatusSummary(a.ActiveRepoPath)
				a.cachedFiles, _ = gitops.GetChangedFiles(a.ActiveRepoPath)
				a.cachedCommits, _ = gitops.GetLog(a.ActiveRepoPath, 40)
				a.cachedGraph, _ = gitops.GetLogGraph(a.ActiveRepoPath, 35)
				a.cachedBranches, a.activeBranch, _ = gitops.ListBranches(a.ActiveRepoPath)
				a.cachedWorktrees, _ = gitops.ListWorktrees(a.ActiveRepoPath)
			}
			a.needsReload = false
		}

		totalItems := 1
		switch a.ActiveTab {
		case 0:
			totalItems = len(a.cachedFiles)
			if totalItems == 0 {
				totalItems = 1
			}
		case 1:
			if a.GraphViewMode == 0 {
				totalItems = len(a.cachedCommits)
			} else {
				totalItems = 1
			}
		case 2:
			totalItems = len(a.cachedBranches)
		case 3:
			totalItems = len(a.cachedWorktrees)
		case 4:
			totalItems = len(a.cachedFleet)
		}

		if a.SelectedIndex >= totalItems && totalItems > 0 {
			a.SelectedIndex = totalItems - 1
		}
		if a.SelectedIndex < 0 {
			a.SelectedIndex = 0
		}

		a.Render()
		a.StatusMsg = ""

		var buf [32]byte
		n, err := os.Stdin.Read(buf[:])
		if err != nil || n == 0 {
			break
		}

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
					a.ActiveTab = (a.ActiveTab + 1) % 5
					a.SelectedIndex = 0
					a.tabSwitched = true
					continue
				case 'D': // Left Tab
					a.ActiveTab = (a.ActiveTab + 4) % 5
					a.SelectedIndex = 0
					a.tabSwitched = true
					continue
				}
			}
			continue
		}

		switch b {
		case '\t':
			a.ActiveTab = (a.ActiveTab + 1) % 5
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
		case '4':
			a.ActiveTab = 3
			a.SelectedIndex = 0
			a.tabSwitched = true
		case '5':
			a.ActiveTab = 4
			a.SelectedIndex = 0
			a.tabSwitched = true
		case 'k', 'K':
			if a.SelectedIndex > 0 {
				a.SelectedIndex--
			}
		case 'j', 'J':
			if a.SelectedIndex < totalItems-1 {
				a.SelectedIndex++
			}
		case 'g', 'G': // Toggle Graph View vs Commit Table in Tab 1
			if a.ActiveTab == 1 {
				a.GraphViewMode = (a.GraphViewMode + 1) % 2
				a.SelectedIndex = 0
				a.tabSwitched = true
				if a.GraphViewMode == 0 {
					a.StatusMsg = "\033[32mSwitched to Commit Table View\033[0m"
				} else {
					a.StatusMsg = "\033[32mSwitched to ASCII Tree Graph View\033[0m"
				}
			}
		case ' ': // Toggle Stage/Unstage selected file in Tab 0
			if a.ActiveTab == 0 && a.ActiveRepoPath != "" && len(a.cachedFiles) > 0 && a.SelectedIndex < len(a.cachedFiles) {
				f := a.cachedFiles[a.SelectedIndex]
				if f.IsStaged {
					if err := gitops.UnstageFile(a.ActiveRepoPath, f.Path); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mUnstage error: %v\033[0m", err)
					} else {
						a.needsReload = true
						a.StatusMsg = fmt.Sprintf("\033[33m✔ Unstaged '%s'\033[0m", f.Path)
					}
				} else {
					if err := gitops.StageFile(a.ActiveRepoPath, f.Path); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mStage error: %v\033[0m", err)
					} else {
						a.needsReload = true
						a.StatusMsg = fmt.Sprintf("\033[32m✔ Staged '%s'\033[0m", f.Path)
					}
				}
			}
		case 'a', 'A': // Stage All in Tab 0
			if a.ActiveTab == 0 && a.ActiveRepoPath != "" {
				if err := gitops.StageAll(a.ActiveRepoPath); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mStage error: %v\033[0m", err)
				} else {
					a.needsReload = true
					a.StatusMsg = "\033[32m✔ Staged all changes (git add -A)\033[0m"
				}
			}
		case 'u': // Unstage All in Tab 0
			if a.ActiveTab == 0 && a.ActiveRepoPath != "" {
				if err := gitops.UnstageAll(a.ActiveRepoPath); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mUnstage error: %v\033[0m", err)
				} else {
					a.needsReload = true
					a.StatusMsg = "\033[33m✔ Unstaged all changes (git reset)\033[0m"
				}
			}
		case 'U': // Undo Commit (git reset --soft HEAD~1) in Tab 0
			if a.ActiveTab == 0 && a.ActiveRepoPath != "" {
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[33m[agygit] Undo last commit (git reset --soft HEAD~1)? Changes will remain staged. (y/N): \033[0m")
				var confirm string
				fmt.Scanln(&confirm)
				if strings.EqualFold(strings.TrimSpace(confirm), "y") {
					if err := gitops.GitUndo(a.ActiveRepoPath); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mUndo error: %v\033[0m", err)
					} else {
						a.needsReload = true
						a.StatusMsg = "\033[32m✔ Undid last commit (git reset --soft HEAD~1)\033[0m"
					}
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
			}
		case 'c', 'C': // Input Commit in Tab 0 (staged only) or Cherry-pick in Tab 1
			if a.ActiveTab == 0 && a.ActiveRepoPath != "" {
				hasStaged := false
				for _, f := range a.cachedFiles {
					if f.IsStaged {
						hasStaged = true
						break
					}
				}
				if !hasStaged {
					a.StatusMsg = "\033[33mNo staged files to commit. Use [Space/s] to stage or [a] to stage all.\033[0m"
					continue
				}
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n📝 \033[1;36m[agygit] Commit Staged Changes in '%s'\033[0m\r\n", filepath.Base(a.ActiveRepoPath))
				fmt.Print("Enter commit message: ")
				scanner := bufio.NewScanner(os.Stdin)
				if scanner.Scan() {
					msg := strings.TrimSpace(scanner.Text())
					if msg != "" {
						if err := gitops.Commit(a.ActiveRepoPath, msg, false); err != nil {
							a.StatusMsg = fmt.Sprintf("\033[31mCommit error: %v\033[0m", err)
						} else {
							a.needsReload = true
							a.StatusMsg = fmt.Sprintf("\033[32m✔ Committed: %s\033[0m", truncateString(msg, 40))
						}
					}
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
			} else if a.ActiveTab == 1 && len(a.cachedCommits) > 0 && a.SelectedIndex < len(a.cachedCommits) {
				c := a.cachedCommits[a.SelectedIndex]
				if err := gitops.CherryPick(a.ActiveRepoPath, c.Hash); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mCherry-pick error: %v\033[0m", err)
				} else {
					a.needsReload = true
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Cherry-picked commit %s\033[0m", c.Hash)
				}
			}
		case 'p', 'P': // Push
			if a.ActiveRepoPath != "" {
				a.StatusMsg = "\033[36mPushing commits to remote...\033[0m"
				if err := gitops.Push(a.ActiveRepoPath); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mPush error: %v\033[0m", err)
				} else {
					a.needsReload = true
					a.StatusMsg = "\033[32m✔ Pushed commits successfully\033[0m"
				}
			}
		case 'l', 'L': // Pull / Pull Rebase
			if a.ActiveRepoPath != "" {
				if b == 'L' {
					a.StatusMsg = "\033[36mPulling with rebase...\033[0m"
					if err := gitops.PullRebase(a.ActiveRepoPath); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mPull --rebase error: %v\033[0m", err)
					} else {
						a.needsReload = true
						a.StatusMsg = "\033[32m✔ Pulled and rebased successfully\033[0m"
					}
				} else {
					a.StatusMsg = "\033[36mPulling fast-forward changes...\033[0m"
					if err := gitops.Pull(a.ActiveRepoPath); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mPull error: %v\033[0m", err)
					} else {
						a.needsReload = true
						a.StatusMsg = "\033[32m✔ Pulled successfully\033[0m"
					}
				}
			}
		case 'z', 'Z': // Stash Save / Pop
			if a.ActiveRepoPath != "" {
				if b == 'Z' {
					if err := gitops.StashPop(a.ActiveRepoPath); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mStash pop error: %v\033[0m", err)
					} else {
						a.needsReload = true
						a.StatusMsg = "\033[32m✔ Stash popped cleanly\033[0m"
					}
				} else {
					if err := gitops.StashSave(a.ActiveRepoPath, "agygit auto-stash"); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mStash save error: %v\033[0m", err)
					} else {
						a.needsReload = true
						a.StatusMsg = "\033[33m✔ Stashed working changes\033[0m"
					}
				}
			}
		case 'm', 'M': // Conflict resolution in Tab 0, or Merge branch in Tab 2
			if a.ActiveTab == 0 && a.ActiveRepoPath != "" && len(a.cachedFiles) > 0 && a.SelectedIndex < len(a.cachedFiles) {
				f := a.cachedFiles[a.SelectedIndex]
				if !f.IsConflict {
					a.StatusMsg = fmt.Sprintf("\033[33m'%s' is not in conflict state.\033[0m", f.Path)
					continue
				}
				fmt.Print("\033[H\033[2J")
				fmt.Printf("\r\n⚔️  \033[1;31mConflict Resolution: %s\033[0m\r\n", f.Path)
				fmt.Println("───────────────────────────────────────────────────────")
				fmt.Println(" [1] Accept Ours   (Keep current HEAD changes: git checkout --ours)")
				fmt.Println(" [2] Accept Theirs (Accept incoming branch changes: git checkout --theirs)")
				fmt.Println(" [Esc] Cancel")
				fmt.Println("───────────────────────────────────────────────────────")
				fmt.Print(" Select option: ")

				var optBuf [16]byte
				optN, _ := os.Stdin.Read(optBuf[:])
				if optN > 0 {
					switch optBuf[0] {
					case '1':
						if err := gitops.ResolveConflict(a.ActiveRepoPath, f.Path, "ours"); err != nil {
							a.StatusMsg = fmt.Sprintf("\033[31mResolve error: %v\033[0m", err)
						} else {
							a.needsReload = true
							a.StatusMsg = fmt.Sprintf("\033[32m✔ Resolved '%s' with --ours\033[0m", f.Path)
						}
					case '2':
						if err := gitops.ResolveConflict(a.ActiveRepoPath, f.Path, "theirs"); err != nil {
							a.StatusMsg = fmt.Sprintf("\033[31mResolve error: %v\033[0m", err)
						} else {
							a.needsReload = true
							a.StatusMsg = fmt.Sprintf("\033[32m✔ Resolved '%s' with --theirs\033[0m", f.Path)
						}
					default:
						a.StatusMsg = "\033[33mConflict resolution cancelled.\033[0m"
					}
				}
				a.tabSwitched = true
			} else if a.ActiveTab == 2 && len(a.cachedBranches) > 0 && a.SelectedIndex < len(a.cachedBranches) {
				br := a.cachedBranches[a.SelectedIndex]
				if br == a.activeBranch {
					a.StatusMsg = "\033[33mCannot merge branch into itself!\033[0m"
					continue
				}
				if err := gitops.Merge(a.ActiveRepoPath, br, false); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mMerge error: %v\033[0m", err)
				} else {
					a.needsReload = true
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Merged branch '%s'\033[0m", br)
				}
			}
		case 's', 'S': // Toggle Stage/Unstage in Tab 0, or Squash merge in Tab 2
			if a.ActiveTab == 0 && a.ActiveRepoPath != "" && len(a.cachedFiles) > 0 && a.SelectedIndex < len(a.cachedFiles) {
				f := a.cachedFiles[a.SelectedIndex]
				if f.IsStaged {
					if err := gitops.UnstageFile(a.ActiveRepoPath, f.Path); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mUnstage error: %v\033[0m", err)
					} else {
						a.needsReload = true
						a.StatusMsg = fmt.Sprintf("\033[33m✔ Unstaged '%s'\033[0m", f.Path)
					}
				} else {
					if err := gitops.StageFile(a.ActiveRepoPath, f.Path); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mStage error: %v\033[0m", err)
					} else {
						a.needsReload = true
						a.StatusMsg = fmt.Sprintf("\033[32m✔ Staged '%s'\033[0m", f.Path)
					}
				}
			} else if a.ActiveTab == 2 && len(a.cachedBranches) > 0 && a.SelectedIndex < len(a.cachedBranches) {
				br := a.cachedBranches[a.SelectedIndex]
				if br == a.activeBranch {
					a.StatusMsg = "\033[33mCannot squash merge branch into itself!\033[0m"
					continue
				}
				if err := gitops.Merge(a.ActiveRepoPath, br, true); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mSquash merge error: %v\033[0m", err)
				} else {
					a.needsReload = true
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Squash merged '%s' (changes staged)\033[0m", br)
				}
			}
		case 'r', 'x': // Reject selected file in Tab 0, Rebase in Tab 2 (r), Abort in Tab 2 (x)
			if a.ActiveTab == 0 && a.ActiveRepoPath != "" && len(a.cachedFiles) > 0 && a.SelectedIndex < len(a.cachedFiles) {
				f := a.cachedFiles[a.SelectedIndex]
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[31m[agygit] Discard changes in '%s'? (y/N): \033[0m", f.Path)
				var confirm string
				fmt.Scanln(&confirm)
				if strings.EqualFold(strings.TrimSpace(confirm), "y") {
					if err := gitops.RejectFile(a.ActiveRepoPath, f.Path, f.IsUntracked); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mReject error: %v\033[0m", err)
					} else {
						a.needsReload = true
						a.StatusMsg = fmt.Sprintf("\033[33m✔ Discarded changes in '%s'\033[0m", f.Path)
					}
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
			} else if a.ActiveTab == 2 {
				if b == 'r' && len(a.cachedBranches) > 0 && a.SelectedIndex < len(a.cachedBranches) {
					br := a.cachedBranches[a.SelectedIndex]
					if br == a.activeBranch {
						a.StatusMsg = "\033[33mCannot rebase branch onto itself!\033[0m"
						continue
					}
					if err := gitops.Rebase(a.ActiveRepoPath, br); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mRebase error: %v\033[0m", err)
					} else {
						a.needsReload = true
						a.StatusMsg = fmt.Sprintf("\033[32m✔ Rebased onto '%s'\033[0m", br)
					}
				} else if b == 'x' && a.ActiveRepoPath != "" {
					_ = gitops.AbortOperation(a.ActiveRepoPath)
					a.needsReload = true
					a.StatusMsg = "\033[33m✔ Aborted active merge/rebase operation\033[0m"
				}
			} else {
				a.needsReload = true
				a.StatusMsg = "\033[32mRefreshed status.\033[0m"
			}
		case 'R', 'X': // Reject all uncommitted changes in Tab 0
			if a.ActiveTab == 0 && a.ActiveRepoPath != "" {
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[1;31m[agygit] DISCARD ALL UNCOMMITTED CHANGES? (y/N): \033[0m")
				var confirm string
				fmt.Scanln(&confirm)
				if strings.EqualFold(strings.TrimSpace(confirm), "y") {
					if err := gitops.RejectAll(a.ActiveRepoPath); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mReject all error: %v\033[0m", err)
					} else {
						a.needsReload = true
						a.StatusMsg = "\033[33m✔ Discarded all uncommitted changes\033[0m"
					}
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
			} else if a.ActiveTab == 2 {
				if b == 'R' && len(a.cachedBranches) > 0 && a.SelectedIndex < len(a.cachedBranches) {
					br := a.cachedBranches[a.SelectedIndex]
					if br == a.activeBranch {
						a.StatusMsg = "\033[33mCannot rebase branch onto itself!\033[0m"
						continue
					}
					if err := gitops.Rebase(a.ActiveRepoPath, br); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mRebase error: %v\033[0m", err)
					} else {
						a.needsReload = true
						a.StatusMsg = fmt.Sprintf("\033[32m✔ Rebased onto '%s'\033[0m", br)
					}
				} else if b == 'X' && a.ActiveRepoPath != "" {
					_ = gitops.AbortOperation(a.ActiveRepoPath)
					a.needsReload = true
					a.StatusMsg = "\033[33m✔ Aborted active merge/rebase operation\033[0m"
				}
			} else {
				a.needsReload = true
				a.StatusMsg = "\033[32mRefreshed status.\033[0m"
			}
		case 'd', 'D': // Diff in Tab 0 or Tab 1, or Delete worktree in Tab 3
			if a.ActiveTab == 0 && a.ActiveRepoPath != "" && len(a.cachedFiles) > 0 && a.SelectedIndex < len(a.cachedFiles) {
				f := a.cachedFiles[a.SelectedIndex]
				diff, _ := gitops.GetFileDiff(a.ActiveRepoPath, f.Path, f.IsStaged)
				fmt.Print("\033[H\033[2J")
				fmt.Printf("\r\n📜 \033[1;36mDiff: %s\033[0m\r\n\r\n", f.Path)
				if strings.TrimSpace(diff) == "" {
					fmt.Println("  (no diff)")
				} else {
					fmt.Printf("%s\r\n", diff)
				}
				fmt.Print("\r\n \033[1mPress any key to return...\033[0m")
				var dummy [1]byte
				_, _ = os.Stdin.Read(dummy[:])
				a.tabSwitched = true
			} else if a.ActiveTab == 1 && len(a.cachedCommits) > 0 && a.SelectedIndex < len(a.cachedCommits) {
				c := a.cachedCommits[a.SelectedIndex]
				diff, _ := gitops.GetCommitDiff(a.ActiveRepoPath, c.Hash)
				fmt.Print("\033[H\033[2J")
				fmt.Printf("\r\n📜 \033[1;36mCommit Details: %s (%s)\033[0m\r\n\r\n", c.Hash, c.Message)
				fmt.Printf("%s\r\n", diff)
				fmt.Print("\r\n \033[1mPress any key to return...\033[0m")
				var dummy [1]byte
				_, _ = os.Stdin.Read(dummy[:])
				a.tabSwitched = true
			} else if a.ActiveTab == 3 && len(a.cachedWorktrees) > 0 && a.SelectedIndex < len(a.cachedWorktrees) {
				wt := a.cachedWorktrees[a.SelectedIndex]
				if wt.IsMain {
					a.StatusMsg = "\033[31mCannot delete the main worktree!\033[0m"
					continue
				}
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[31m[agygit] Delete worktree at '%s'? (y/N): \033[0m", wt.Path)
				var confirm string
				fmt.Scanln(&confirm)
				if strings.EqualFold(strings.TrimSpace(confirm), "y") {
					if err := gitops.RemoveWorktree(a.ActiveRepoPath, wt.Path, true); err != nil {
						a.StatusMsg = fmt.Sprintf("\033[31mError: %v\033[0m", err)
					} else {
						a.needsReload = true
						a.StatusMsg = fmt.Sprintf("\033[33mRemoved worktree '%s'\033[0m", wt.Branch)
					}
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
			}
		case 'n', 'N': // New AI Worktree in Tab 3
			if a.ActiveTab == 3 && a.ActiveRepoPath != "" {
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n🌿 \033[1;36m[agygit] Create Isolated Agent Worktree for '%s'\033[0m\r\n", filepath.Base(a.ActiveRepoPath))
				fmt.Print("Enter branch name (e.g. agent/refactor-auth): ")
				scanner := bufio.NewScanner(os.Stdin)
				if scanner.Scan() {
					branch := strings.TrimSpace(scanner.Text())
					if branch != "" {
						wt, err := gitops.AddWorktree(a.ActiveRepoPath, branch, "")
						if err != nil {
							a.StatusMsg = fmt.Sprintf("\033[31mError: %v\033[0m", err)
						} else {
							a.needsReload = true
							a.StatusMsg = fmt.Sprintf("\033[32m✔ Created worktree at %s\033[0m", wt.Path)
						}
					}
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
			}
		case 'b', 'B': // New / Switch Branch in Tab 2
			if a.ActiveTab == 2 && a.ActiveRepoPath != "" {
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n🌿 \033[1;36m[agygit] Checkout or Create Branch\033[0m\r\n")
				fmt.Print("Enter branch name: ")
				scanner := bufio.NewScanner(os.Stdin)
				if scanner.Scan() {
					br := strings.TrimSpace(scanner.Text())
					if br != "" {
						exists := false
						for _, bName := range a.cachedBranches {
							if bName == br {
								exists = true
								break
							}
						}
						create := !exists
						if err := gitops.CheckoutBranch(a.ActiveRepoPath, br, create); err != nil {
							a.StatusMsg = fmt.Sprintf("\033[31mCheckout error: %v\033[0m", err)
						} else {
							if create {
								a.StatusMsg = fmt.Sprintf("\033[32m✔ Created & checked out branch '%s'\033[0m", br)
							} else {
								a.StatusMsg = fmt.Sprintf("\033[32m✔ Checked out branch '%s'\033[0m", br)
							}
						}
						a.needsReload = true
					}
				}
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.tabSwitched = true
			}
		case '\r', '\n': // Select Action
			if a.ActiveTab == 4 && len(a.cachedFleet) > 0 && a.SelectedIndex < len(a.cachedFleet) {
				a.ActiveRepoPath = a.cachedFleet[a.SelectedIndex].Path
				a.ActiveTab = 0
				a.SelectedIndex = 0
				a.needsReload = true
				a.cachedStatus = nil
				a.tabSwitched = true
				a.StatusMsg = fmt.Sprintf("\033[32m✔ Switched active Git context to '%s'\033[0m", filepath.Base(a.ActiveRepoPath))
			} else if a.ActiveTab == 2 && len(a.cachedBranches) > 0 && a.SelectedIndex < len(a.cachedBranches) {
				br := a.cachedBranches[a.SelectedIndex]
				if err := gitops.CheckoutBranch(a.ActiveRepoPath, br, false); err != nil {
					a.StatusMsg = fmt.Sprintf("\033[31mCheckout error: %v\033[0m", err)
				} else {
					a.StatusMsg = fmt.Sprintf("\033[32m✔ Checked out branch '%s'\033[0m", br)
				}
				a.needsReload = true
			} else if a.ActiveTab == 3 && len(a.cachedWorktrees) > 0 && a.SelectedIndex < len(a.cachedWorktrees) {
				wt := a.cachedWorktrees[a.SelectedIndex]
				fmt.Print("\033[?25h\033[?1049l")
				_ = term.Restore(fd, oldState)
				fmt.Printf("\r\n\033[36m[agygit]\033[0m Launching Antigravity in worktree '\033[32m%s\033[0m'...\r\n", wt.Path)
				cmd := exec.Command("agy")
				cmd.Dir = wt.Path
				cmd.Stdin = os.Stdin
				cmd.Stdout = os.Stdout
				cmd.Stderr = os.Stderr
				_ = cmd.Run()
				oldState, _ = term.MakeRaw(fd)
				fmt.Print("\033[?1049h\033[?25l")
				a.needsReload = true
				a.tabSwitched = true
			} else if a.ActiveTab == 1 && len(a.cachedCommits) > 0 && a.SelectedIndex < len(a.cachedCommits) {
				c := a.cachedCommits[a.SelectedIndex]
				diff, _ := gitops.GetCommitDiff(a.ActiveRepoPath, c.Hash)
				fmt.Print("\033[H\033[2J")
				fmt.Printf("\r\n📜 \033[1;36mCommit Details: %s (%s)\033[0m\r\n\r\n", c.Hash, c.Message)
				fmt.Printf("%s\r\n", diff)
				fmt.Print("\r\n \033[1mPress any key to return...\033[0m")
				var dummy [1]byte
				_, _ = os.Stdin.Read(dummy[:])
				a.tabSwitched = true
			} else if a.ActiveTab == 0 && a.ActiveRepoPath != "" {
				if len(a.cachedFiles) > 0 && a.SelectedIndex < len(a.cachedFiles) {
					f := a.cachedFiles[a.SelectedIndex]
					diff, _ := gitops.GetFileDiff(a.ActiveRepoPath, f.Path, f.IsStaged)
					fmt.Print("\033[H\033[2J")
					fmt.Printf("\r\n📜 \033[1;36mDiff: %s\033[0m\r\n\r\n", f.Path)
					if strings.TrimSpace(diff) == "" {
						fmt.Println("  (no diff)")
					} else {
						fmt.Printf("%s\r\n", diff)
					}
					fmt.Print("\r\n \033[1mPress any key to return...\033[0m")
					var dummy [1]byte
					_, _ = os.Stdin.Read(dummy[:])
					a.tabSwitched = true
				} else {
					fmt.Print("\033[?25h\033[?1049l")
					_ = term.Restore(fd, oldState)
					fmt.Printf("\r\n\033[36m[agygit]\033[0m Launching Antigravity in '\033[32m%s\033[0m'...\r\n", a.ActiveRepoPath)
					cmd := exec.Command("agy")
					cmd.Dir = a.ActiveRepoPath
					cmd.Stdin = os.Stdin
					cmd.Stdout = os.Stdout
					cmd.Stderr = os.Stderr
					_ = cmd.Run()
					oldState, _ = term.MakeRaw(fd)
					fmt.Print("\033[?1049h\033[?25l")
					a.needsReload = true
					a.tabSwitched = true
				}
			}
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

	repoName := "No Repo Selected"
	if a.ActiveRepoPath != "" {
		repoName = filepath.Base(a.ActiveRepoPath)
	}

	if width < 80 {
		fmt.Fprintf(&b, "\r\n🐙 \033[1;36mAGYGIT\033[0m · Repo: \033[1;32m%s\033[0m\033[K\r\n", truncateString(repoName, 18))
		b.WriteString(hr(width))
		tabNames := []string{"1:Stat", "2:Graph", "3:Branch", "4:Wt", "5:Fleet"}
		for i, t := range tabNames {
			if i == a.ActiveTab {
				fmt.Fprintf(&b, "\033[1;37;44m [%s] \033[0m ", t)
			} else {
				fmt.Fprintf(&b, "\033[36m[%s]\033[0m ", t)
			}
		}
		b.WriteString("\033[K\r\n" + hr(width))
	} else {
		fmt.Fprintf(&b, "\r\n🐙 \033[1;36mAGYGIT - Advanced Git Cockpit & Multi-Agent Hub\033[0m (Active: \033[1;32m%s\033[0m)\033[K\r\n", repoName)
		b.WriteString(hr(width))
		tabs := []string{
			"[1] 📊 Status & Commit",
			"[2] 📜 Graph & Log",
			"[3] 🔀 Branch & Rebase",
			"[4] 🌿 AI Worktrees",
			"[5] 🌐 Fleet Overview",
		}
		for i, t := range tabs {
			if i == a.ActiveTab {
				fmt.Fprintf(&b, " \033[1;37;44m %s \033[0m ", t)
			} else {
				fmt.Fprintf(&b, " \033[36m%s\033[0m ", t)
			}
		}
		b.WriteString("\033[K\r\n" + hr(width))
	}

	switch a.ActiveTab {
	case 0:
		a.renderStatusTab(&b, width, height)
	case 1:
		a.renderGraphTab(&b, width, height)
	case 2:
		a.renderBranchTab(&b, width, height)
	case 3:
		a.renderWorktreesTab(&b, width, height)
	case 4:
		a.renderFleetTab(&b, width, height)
	}

	b.WriteString(hr(width))
	if a.StatusMsg != "" {
		fmt.Fprintf(&b, " %s\033[K\r\n", a.StatusMsg)
	} else {
		b.WriteString("\033[K\r\n")
	}

	if width < 80 {
		switch a.ActiveTab {
		case 0:
			b.WriteString(" \033[1m[Tab]\033[0mNav \033[1;32m[Space]\033[0mStage \033[1;36m[d]\033[0mDiff \033[1;31m[r]\033[0mReject \033[1;32m[c]\033[0mCommit \033[1;31m[Q]\033[0mExit\033[K\r\n")
		case 1:
			b.WriteString(" \033[1m[Tab]\033[0mNav \033[1;36m[c]\033[0mCherryPick \033[1;36m[d]\033[0mDiff \033[1;32m[g]\033[0mGraph \033[1;31m[Q]\033[0mExit\033[K\r\n")
		case 2:
			b.WriteString(" \033[1m[Tab]\033[0mNav \033[1;32m[m]\033[0mMerge \033[1;33m[s]\033[0mSquash \033[1;36m[r]\033[0mRebase \033[1;31m[Q]\033[0mExit\033[K\r\n")
		case 3:
			b.WriteString(" \033[1m[Tab]\033[0mNav \033[1;36m[n]\033[0mNew \033[1;32m[Enter]\033[0mAgy \033[1;31m[d]\033[0mDel \033[1;31m[Q]\033[0mExit\033[K\r\n")
		case 4:
			b.WriteString(" \033[1m[Tab]\033[0mNav \033[1;32m[Enter]\033[0mSwitch Repo \033[1;31m[Q]\033[0mExit\033[K\r\n")
		}
	} else {
		switch a.ActiveTab {
		case 0:
			b.WriteString(" \033[1m[Tab/1-5]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Select · \033[1;32m[Space/s]\033[0m Stage/Unstage · \033[1;36m[d/Enter]\033[0m Diff · \033[1;31m[r/x]\033[0m Reject · \033[1;31m[R/X]\033[0m Reject All · \033[1;32m[c]\033[0m Commit · \033[1;33m[a/u]\033[0m All · \033[1;31m[U]\033[0m Undo · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		case 1:
			b.WriteString(" \033[1m[Tab/1-5]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Select · \033[1;36m[c]\033[0m Cherry-Pick · \033[1;36m[d/Enter]\033[0m Diff · \033[1;32m[g]\033[0m Tree/Table · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		case 2:
			b.WriteString(" \033[1m[Tab/1-5]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Select Branch · \033[1;32m[m]\033[0m Merge · \033[1;33m[s]\033[0m Squash · \033[1;36m[r]\033[0m Rebase · \033[1;31m[x]\033[0m Abort · \033[1;36m[b]\033[0m New\033[K\r\n")
		case 3:
			b.WriteString(" \033[1m[Tab/1-5]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Select Tree · \033[1;36m[n]\033[0m New Agent Worktree · \033[1;32m[Enter]\033[0m Launch Agy · \033[1;31m[d]\033[0m Delete\033[K\r\n")
		case 4:
			b.WriteString(" \033[1m[Tab/1-5]\033[0m Switch Tab · \033[1m[↑/↓ j/k]\033[0m Nav Fleet · \033[1;32m[Enter]\033[0m Select Repo · \033[1;36m[r]\033[0m Rescan · \033[1;31m[Q/Esc]\033[0m Exit\033[K\r\n")
		}
	}

	b.WriteString("\033[J")
	os.Stdout.WriteString(b.String())
}

func fileStatusBadge(f model.ChangedFile) string {
	if f.IsConflict {
		return "\033[1;37;41m ⚠️ CONFLICT \033[0m"
	}
	if f.IsUntracked {
		return "\033[37m[?Untrack]\033[0m"
	}
	if f.IndexStatus == 'D' || f.WorkTreeStatus == 'D' {
		return "\033[31m[-Deleted]\033[0m"
	}
	if f.IndexStatus != ' ' && f.WorkTreeStatus != ' ' {
		return "\033[36m[±Partial]\033[0m"
	}
	if f.IsStaged {
		return "\033[32m[+Staged] \033[0m"
	}
	if f.WorkTreeStatus == 'M' {
		return "\033[33m[~Modified]\033[0m"
	}
	return "\033[33m[~Modified]\033[0m"
}

func (a *App) renderStatusTab(b *strings.Builder, width int, height int) {
	if a.ActiveRepoPath == "" {
		b.WriteString(" \033[33mNo repository selected. Switch to Tab 5 to pick a repo.\033[0m\033[K\r\n")
		return
	}

	r := a.cachedStatus
	if r == nil {
		b.WriteString(" \033[31mUnable to read Git status.\033[0m\033[K\r\n")
		return
	}

	fmt.Fprintf(b, " 📍 \033[1mRepository:\033[0m  \033[1;32m%s\033[0m (\033[35m%s\033[0m)\033[K\r\n", r.Name, truncateString(r.Path, width-20))
	fmt.Fprintf(b, " 🌿 \033[1mBranch:\033[0m      \033[1;33m%s\033[0m  ·  Sync: \033[32m↑%d ahead\033[0m · \033[31m↓%d behind\033[0m\033[K\r\n",
		r.CurrentBranch, r.Ahead, r.Behind)

	statusStr := "\033[1;32m✔ Working tree clean\033[0m"
	if !r.IsClean {
		statusStr = fmt.Sprintf("\033[1;33m● Changes detected:\033[0m \033[32m+%d staged\033[0m · \033[33m~%d modified\033[0m · \033[37m?%d untracked\033[0m",
			r.StagedFiles, r.DirtyFiles, r.UntrackedFiles)
	}
	fmt.Fprintf(b, " 📝 \033[1mStatus:\033[0m      %s\033[K\r\n", statusStr)
	fmt.Fprintf(b, " 🔖 \033[1mLast Commit:\033[0m \033[36m%s\033[0m\033[K\r\n\033[K\r\n", truncateString(r.LastCommit, width-18))

	files := a.cachedFiles
	if len(files) == 0 {
		b.WriteString(" \033[32m✔ No modified files to commit (working tree clean).\033[0m\033[K\r\n")
		return
	}

	b.WriteString(" \033[1;36mChanged Files (Interactive Selector):\033[0m\033[K\r\n")

	pageSize := height - 15
	if pageSize < 4 {
		pageSize = 4
	}
	if pageSize > 10 {
		pageSize = 10
	}

	page := a.SelectedIndex / pageSize
	startIdx := page * pageSize
	endIdx := startIdx + pageSize
	if endIdx > len(files) {
		endIdx = len(files)
	}
	totalPages := (len(files) + pageSize - 1) / pageSize
	if totalPages == 0 {
		totalPages = 1
	}

	for i := startIdx; i < endIdx; i++ {
		f := files[i]
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;32m▶ \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		badge := fileStatusBadge(f)
		maxPathLen := width - 26
		if maxPathLen < 15 {
			maxPathLen = 15
		}

		fmt.Fprintf(b, "%s%s%s %-*s%s\033[K\r\n",
			cursor, highlightStart, badge, maxPathLen, truncateString(f.Path, maxPathLen), highlightEnd)
	}

	fmt.Fprintf(b, "\033[K\r\n \033[37m[Page %d/%d · %d-%d of %d files · [Space/s] Stage/Unstage · [d/Enter] Diff · [r/x] Discard · [m] Conflict · [c] Commit]\033[0m\033[K\r\n",
		page+1, totalPages, startIdx+1, endIdx, len(files))
}

func (a *App) renderGraphTab(b *strings.Builder, width int, height int) {
	if a.ActiveRepoPath == "" {
		b.WriteString("  \033[33mNo repository selected.\033[0m\033[K\r\n")
		return
	}

	modeStr := "\033[1;32m[Table View]\033[0m (Press \033[1;36m[g]\033[0m for ASCII Tree)"
	if a.GraphViewMode == 1 {
		modeStr = "\033[1;35m[ASCII Tree Graph]\033[0m (Press \033[1;36m[g]\033[0m for Commit Table)"
	}
	fmt.Fprintf(b, " 📜 \033[1;36mGit History for '%s'\033[0m · View: %s\033[K\r\n\033[K\r\n", filepath.Base(a.ActiveRepoPath), modeStr)

	if a.GraphViewMode == 1 {
		// ASCII Tree View
		if a.cachedGraph == "" {
			b.WriteString("  \033[33mNo graph history available.\033[0m\033[K\r\n")
			return
		}
		lines := strings.Split(strings.TrimRight(a.cachedGraph, "\r\n"), "\n")
		maxLines := height - 10
		if maxLines < 5 {
			maxLines = 5
		}
		for idx, l := range lines {
			if idx >= maxLines {
				fmt.Fprintf(b, "  \033[37m...and %d more graph lines\033[0m\033[K\r\n", len(lines)-maxLines)
				break
			}
			fmt.Fprintf(b, "  %s\033[K\r\n", l)
		}
		return
	}

	// Mode 0: Commit Table with Pagination & Cherry-Pick/Diff Actions
	commits := a.cachedCommits
	if len(commits) == 0 {
		b.WriteString("  \033[33mNo commit history found.\033[0m\033[K\r\n")
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
	if endIdx > len(commits) {
		endIdx = len(commits)
	}
	totalPages := (len(commits) + pageSize - 1) / pageSize
	if totalPages == 0 {
		totalPages = 1
	}

	for i := startIdx; i < endIdx; i++ {
		c := commits[i]
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;32m▶ \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		maxSubject := width - 42
		if maxSubject < 15 {
			maxSubject = 15
		}

		fmt.Fprintf(b, "%s%s\033[33m%s\033[0m \033[1m%-*s\033[0m \033[36m(%s, %s)\033[0m%s\033[K\r\n",
			cursor, highlightStart, c.Hash, maxSubject, truncateString(c.Message, maxSubject), c.Author, c.RelativeTime, highlightEnd)
	}

	fmt.Fprintf(b, "\033[K\r\n \033[37m[Page %d/%d · Commits %d-%d of %d · [c] Cherry-Pick · [d/Enter] Diff · [g] Toggle Tree]\033[0m\033[K\r\n",
		page+1, totalPages, startIdx+1, endIdx, len(commits))
}

func (a *App) renderBranchTab(b *strings.Builder, width int, height int) {
	fmt.Fprintf(b, " 🔀 \033[1;36mBranches for '%s' (Active: \033[1;32m%s\033[1;36m):\033[0m\033[K\r\n\033[K\r\n",
		filepath.Base(a.ActiveRepoPath), a.activeBranch)

	branches := a.cachedBranches
	if len(branches) == 0 {
		b.WriteString("  \033[33mNo branches found.\033[0m\033[K\r\n")
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
	if endIdx > len(branches) {
		endIdx = len(branches)
	}
	totalPages := (len(branches) + pageSize - 1) / pageSize
	if totalPages == 0 {
		totalPages = 1
	}

	for i := startIdx; i < endIdx; i++ {
		br := branches[i]
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;32m▶ \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		activeTag := "  "
		if br == a.activeBranch {
			activeTag = "\033[1;32m● \033[0m"
		}

		maxBranchLen := width - 16
		if maxBranchLen < 15 {
			maxBranchLen = 15
		}

		fmt.Fprintf(b, "%s%s%s%2d. \033[1m%-*s\033[0m%s\033[K\r\n",
			cursor, highlightStart, activeTag, i+1, maxBranchLen, truncateString(br, maxBranchLen), highlightEnd)
	}

	fmt.Fprintf(b, "\033[K\r\n \033[37m[Page %d/%d · %d-%d of %d branches · [m] Merge · [s] Squash · [r] Rebase · [b] New Branch]\033[0m\033[K\r\n",
		page+1, totalPages, startIdx+1, endIdx, len(branches))
}

func (a *App) renderWorktreesTab(b *strings.Builder, width int, height int) {
	fmt.Fprintf(b, " 🌿 \033[1;36mMulti-Agent Isolated Worktrees (%s):\033[0m\033[K\r\n\033[K\r\n",
		filepath.Base(a.ActiveRepoPath))

	wts := a.cachedWorktrees
	if len(wts) == 0 {
		b.WriteString("  \033[33mNo worktrees configured. Press [n] to create an isolated agent worktree.\033[0m\033[K\r\n")
		return
	}

	pageSize := height - 12
	if pageSize < 4 {
		pageSize = 4
	}
	if pageSize > 8 {
		pageSize = 8
	}

	page := a.SelectedIndex / pageSize
	startIdx := page * pageSize
	endIdx := startIdx + pageSize
	if endIdx > len(wts) {
		endIdx = len(wts)
	}
	totalPages := (len(wts) + pageSize - 1) / pageSize
	if totalPages == 0 {
		totalPages = 1
	}

	for i := startIdx; i < endIdx; i++ {
		wt := wts[i]
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;32m▶ \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		typeTag := "\033[32m[Main]\033[0m"
		if !wt.IsMain {
			typeTag = "\033[36m[Agent Isolated]\033[0m"
		}

		branch := wt.Branch
		if branch == "" {
			branch = "detached"
		}

		fmt.Fprintf(b, "%s%s%d. %-18s \033[1m%-20s\033[0m \033[35m%-16s\033[0m \033[37m%s\033[0m%s\033[K\r\n",
			cursor, highlightStart, i+1, typeTag, truncateString(filepath.Base(wt.Path), 20), truncateString("("+branch+")", 16), truncateString(wt.Path, width-60), highlightEnd)
	}

	fmt.Fprintf(b, "\033[K\r\n \033[37m[Page %d/%d · %d-%d of %d trees · [Enter] Launch Agy · [n] New Tree · [d] Delete]\033[0m\033[K\r\n",
		page+1, totalPages, startIdx+1, endIdx, len(wts))
}

func (a *App) renderFleetTab(b *strings.Builder, width int, height int) {
	fleet := a.cachedFleet
	if len(fleet) == 0 {
		b.WriteString(" \033[33mNo Git repositories found in workspace or ~/projects.\033[0m\033[K\r\n")
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
	if endIdx > len(fleet) {
		endIdx = len(fleet)
	}
	totalPages := (len(fleet) + pageSize - 1) / pageSize
	if totalPages == 0 {
		totalPages = 1
	}

	fmt.Fprintf(b, " 🌐 \033[1;36mGit Fleet Repositories (%d Total):\033[0m\033[K\r\n\033[K\r\n", len(fleet))

	for i := startIdx; i < endIdx; i++ {
		r := fleet[i]
		cursor := "  "
		highlightStart := ""
		highlightEnd := ""
		if i == a.SelectedIndex {
			cursor = "\033[1;32m▶ \033[0m"
			highlightStart = "\033[1;37;44m"
			highlightEnd = "\033[0m"
		}

		activeMarker := "  "
		if r.Path == a.ActiveRepoPath {
			activeMarker = "\033[1;32m● \033[0m"
		}

		statusBadge := "\033[32m[Clean]\033[0m"
		if !r.IsClean {
			statusBadge = fmt.Sprintf("\033[33m[+%d ~%d ?%d]\033[0m", r.StagedFiles, r.DirtyFiles, r.UntrackedFiles)
		}

		maxNameLen := width - 42
		if maxNameLen < 15 {
			maxNameLen = 15
		}

		fmt.Fprintf(b, "%s%s%s%2d. \033[1m%-*s\033[0m (\033[35m%-14s\033[0m) %-14s%s\033[K\r\n",
			cursor, highlightStart, activeMarker, i+1, maxNameLen, truncateString(r.Name, maxNameLen), truncateString(r.CurrentBranch, 14), statusBadge, highlightEnd)
	}

	fmt.Fprintf(b, "\033[K\r\n \033[37m[Page %d/%d · %d-%d of %d repos · Press [Enter] to switch active control repo]\033[0m\033[K\r\n",
		page+1, totalPages, startIdx+1, endIdx, len(fleet))
}

func (a *App) runNonInteractive() error {
	a.PrintStatus(os.Stdout)
	return nil
}

func (a *App) PrintStatus(w io.Writer) {
	fmt.Fprintln(w, "\n🐙 \033[1;36mAGYGIT - Advanced Git Cockpit & Multi-Agent Hub\033[0m")
	fmt.Fprintln(w, "──────────────────────────────────────────────────────────────────────────────────")
	fleet, _ := gitops.ScanFleet(a.RootDir)
	for i, r := range fleet {
		cleanStr := "\033[32m[Clean]\033[0m"
		if !r.IsClean {
			cleanStr = fmt.Sprintf("\033[33m[Dirty: +%d ~%d ?%d]\033[0m", r.StagedFiles, r.DirtyFiles, r.UntrackedFiles)
		}
		fmt.Fprintf(w, " %2d. \033[1m%-24s\033[0m (\033[35m%-16s\033[0m) %s\n", i+1, r.Name, r.CurrentBranch, cleanStr)
	}
	fmt.Fprintln(w, "──────────────────────────────────────────────────────────────────────────────────")
}
