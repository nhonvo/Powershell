package watcher

import (
	"os"
	"testing"

	"agyreview/internal/model"
)

func TestWatcher_AddRemove(t *testing.T) {
	cwd, err := os.Getwd()
	if err != nil {
		t.Fatalf("failed to get cwd: %v", err)
	}

	cfg := model.DefaultConfig()
	err = AddWatchRepo(cfg, cwd, model.TriggerSuggest)
	if err != nil {
		t.Fatalf("failed to add repo to watch list: %v", err)
	}

	if len(cfg.WatchedRepos) != 1 {
		t.Errorf("expected 1 watched repo, got %d", len(cfg.WatchedRepos))
	}

	err = RemoveWatchRepo(cfg, cfg.WatchedRepos[0].Path)
	if err != nil {
		t.Fatalf("failed to remove repo from watch list: %v", err)
	}

	if len(cfg.WatchedRepos) != 0 {
		t.Errorf("expected 0 watched repos, got %d", len(cfg.WatchedRepos))
	}
}
