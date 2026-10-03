package resolver

import (
	"os"
	"testing"

	"agyreview/internal/model"
)

func TestURLDetections(t *testing.T) {
	if !IsGitHubURL("github.com/owner/repo") {
		t.Errorf("expected github.com/owner/repo to be recognized as GitHub URL")
	}
	if !IsGitHubURL("https://github.com/owner/repo.git") {
		t.Errorf("expected https URL to be recognized")
	}
	if IsGitHubURL("https://github.com/owner/repo/pull/42") {
		t.Errorf("PR URL should not be classified as bare repo URL")
	}

	if !IsPRURL("https://github.com/owner/repo/pull/123") {
		t.Errorf("expected PR URL to be recognized")
	}
	if IsPRURL("https://github.com/owner/repo") {
		t.Errorf("repo URL should not be recognized as PR URL")
	}

	prInfo, err := ParsePRURL("https://github.com/myorg/awesome-tool/pull/99")
	if err != nil {
		t.Fatalf("unexpected error parsing PR URL: %v", err)
	}
	if prInfo.RepoOwner != "myorg" || prInfo.RepoName != "awesome-tool" || prInfo.Number != 99 {
		t.Errorf("unexpected parsed PR info: %+v", prInfo)
	}
}

func TestResolveLocal(t *testing.T) {
	cwd, err := os.Getwd()
	if err != nil {
		t.Fatalf("failed to get cwd: %v", err)
	}
	target, err := ResolveLocal(cwd, model.ScopeWholeCodebase)
	if err != nil {
		t.Fatalf("failed to resolve current directory: %v", err)
	}
	if target.Path != cwd {
		t.Errorf("expected path %s, got %s", cwd, target.Path)
	}
	if target.Scope != model.ScopeWholeCodebase {
		t.Errorf("expected ScopeWholeCodebase, got %s", target.Scope)
	}
}
