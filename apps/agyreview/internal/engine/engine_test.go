package engine

import (
	"os"
	"testing"

	"agyreview/internal/model"
)

func TestEngine_ThreeLoops(t *testing.T) {
	cwd, err := os.Getwd()
	if err != nil {
		t.Fatalf("failed to get cwd: %v", err)
	}

	progressCalls := 0
	findings, score, err := ExecuteThreeLoops(cwd, func(loop int, name string, count int) {
		progressCalls++
	})

	if err != nil {
		t.Fatalf("unexpected error running three loops: %v", err)
	}
	if progressCalls != 3 {
		t.Errorf("expected 3 loop progress callbacks, got %d", progressCalls)
	}
	if score.TotalPoints < 0 || score.TotalPoints > 100 {
		t.Errorf("score out of range [0, 100]: %d", score.TotalPoints)
	}
	_ = findings
}

func TestEngine_BaremScore(t *testing.T) {
	findings := []model.Finding{
		{
			ID:       "SEC-1",
			Severity: model.SevP0,
			Category: "Security",
		},
		{
			ID:       "CONC-1",
			Severity: model.SevP1,
			Category: "Concurrency",
		},
	}

	score := model.ComputeBaremScore(findings)
	if score.TotalPoints >= 100 {
		t.Errorf("expected score penalty, got %d", score.TotalPoints)
	}
	if score.SecurityPoints >= 25 {
		t.Errorf("expected security deduction, got %d", score.SecurityPoints)
	}
}
