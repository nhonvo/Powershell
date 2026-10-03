package watcher

import (
	"fmt"
	"os"

	"agyreview/internal/engine"
	"agyreview/internal/model"
	"agyreview/internal/resolver"
)

// HandleChangeEvent processes a detected change event based on TriggerMode ("auto" vs "suggest").
func HandleChangeEvent(evt ChangeEvent) error {
	fmt.Printf("\n🔔 [agyreview Sentinel] Change detected in %s:\n", evt.RepoPath)
	fmt.Printf("   Branch: %s (was %s)\n", evt.NewBranch, evt.OldBranch)
	fmt.Printf("   Commit: %s (was %s)\n", evt.NewCommit[:8], evt.OldCommit[:8])

	if evt.TriggerMode == model.TriggerAuto {
		fmt.Printf("   ⚡ TriggerMode is 'auto' ➔ Launching 3-Loop autonomous audit...\n")
		target, err := resolver.ResolveLocal(evt.RepoPath, model.ScopeWholeCodebase)
		if err != nil {
			return err
		}
		res, err := engine.RunReview(target, false, func(loop int, name string, count int) {
			fmt.Printf("      [Loop %d] %s (%d findings)\n", loop, name, count)
		})
		if err != nil {
			return err
		}
		fmt.Printf("   ✔ Audit Complete! Score: %d/100 (%s). Report saved to: %s\n", res.Score.TotalPoints, res.Score.LetterGrade, res.ReportPath)
		return nil
	}

	// TriggerMode is "suggest"
	fmt.Printf("   💡 [Suggested Action] Would you like to review this changeset now?\n")
	fmt.Printf("      Run: agyreview run %s\n\n", evt.RepoPath)
	return nil
}

// AddWatchRepo adds a repository path to the watch list in config.
func AddWatchRepo(cfg *model.AppConfig, path string, mode model.TriggerMode) error {
	absPath, err := resolver.ResolveLocal(path, model.ScopeWholeCodebase)
	if err != nil {
		return err
	}

	for _, w := range cfg.WatchedRepos {
		if w.Path == absPath.Path {
			return fmt.Errorf("repository %s is already being watched", absPath.Path)
		}
	}

	item := model.WatchItem{
		Path:        absPath.Path,
		TriggerMode: mode,
		LastCommit:  absPath.HeadCommit,
		LastBranch:  absPath.Branch,
	}
	cfg.WatchedRepos = append(cfg.WatchedRepos, item)
	return model.SaveConfig(cfg)
}

// RemoveWatchRepo removes a repository path from the watch list.
func RemoveWatchRepo(cfg *model.AppConfig, path string) error {
	var remaining []model.WatchItem
	found := false
	for _, w := range cfg.WatchedRepos {
		if w.Path == path || os.ExpandEnv(w.Path) == path {
			found = true
		} else {
			remaining = append(remaining, w)
		}
	}
	if !found {
		return fmt.Errorf("repository %s not found in watch list", path)
	}
	cfg.WatchedRepos = remaining
	return model.SaveConfig(cfg)
}
