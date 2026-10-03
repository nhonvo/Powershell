package engine

import (
	"fmt"
	"time"

	"agyreview/internal/markdown"
	"agyreview/internal/model"
)

type ReviewResult struct {
	Target     *model.TargetRepo
	Findings   []model.Finding
	Score      model.BaremScore
	ReportPath string
	Duration   time.Duration
}

// RunReview executes the review engine pipeline against the target repository.
func RunReview(target *model.TargetRepo, useSwarm bool, cb LoopProgressCallback) (*ReviewResult, error) {
	start := time.Now()

	var findings []model.Finding
	var score model.BaremScore
	var err error

	if useSwarm {
		findings, score, err = CoordinateSwarm(target.Path)
	} else {
		findings, score, err = ExecuteThreeLoops(target.Path, cb)
	}

	if err != nil {
		return nil, fmt.Errorf("failed executing review pipeline: %w", err)
	}

	reportPath, err := markdown.GenerateReviewReport(target, findings, score)
	if err != nil {
		return nil, fmt.Errorf("failed generating markdown report: %w", err)
	}

	return &ReviewResult{
		Target:     target,
		Findings:   findings,
		Score:      score,
		ReportPath: reportPath,
		Duration:   time.Since(start),
	}, nil
}
