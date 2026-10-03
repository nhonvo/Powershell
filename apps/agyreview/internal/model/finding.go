package model

import "fmt"

type Severity string

const (
	SevP0 Severity = "P0" // CRITICAL: Security leaks, data loss, deadlock, broken build
	SevP1 Severity = "P1" // HIGH: Resource leaks, unhandled panics, business logic error
	SevP2 Severity = "P2" // MEDIUM: Code smells, missing input validation, no timeout
	SevP3 Severity = "P3" // LOW: Style issues, non-idiomatic naming, missing doc comments
	SevP4 Severity = "P4" // INFO: Modernization hints, micro-optimizations
)

func (s Severity) ColorCode() string {
	switch s {
	case SevP0:
		return "\033[1;31m[P0 - CRITICAL]\033[0m"
	case SevP1:
		return "\033[1;33m[P1 - HIGH]\033[0m"
	case SevP2:
		return "\033[1;34m[P2 - MEDIUM]\033[0m"
	case SevP3:
		return "\033[0;36m[P3 - LOW]\033[0m"
	case SevP4:
		return "\033[0;90m[P4 - INFO]\033[0m"
	default:
		return string(s)
	}
}

type Finding struct {
	ID                   string   `json:"id"`
	Severity             Severity `json:"severity"`
	Category             string   `json:"category"`
	FilePath             string   `json:"file_path"`
	StartLine            int      `json:"start_line"`
	EndLine              int      `json:"end_line"`
	OffendingCode        string   `json:"offending_code"`
	FailureExplanation   string   `json:"failure_explanation"`
	ConcreteFix          string   `json:"concrete_fix"`
	PatchDiff            string   `json:"patch_diff,omitempty"`
	LoopDiscovered       int      `json:"loop_discovered"`       // 1, 2, or 3
	VerifiedNoRegression bool     `json:"verified_no_regression"`
}

type BaremScore struct {
	ArchitecturePoints  int    `json:"architecture_points"`  // Max 20
	SecurityPoints      int    `json:"security_points"`      // Max 25
	ConcurrencyPoints   int    `json:"concurrency_points"`   // Max 20
	RobustnessPoints    int    `json:"robustness_points"`    // Max 20
	DocumentationPoints int    `json:"documentation_points"` // Max 15
	TotalPoints         int    `json:"total_points"`         // 0 - 100
	LetterGrade         string `json:"letter_grade"`         // A+, A, B, C, D, F
	Summary             string `json:"summary"`
}

func ComputeBaremScore(findings []Finding) BaremScore {
	arch := 20
	sec := 25
	conc := 20
	rob := 20
	doc := 15

	for _, f := range findings {
		switch f.Severity {
		case SevP0:
			// P0 heavily penalizes security and concurrency
			if f.Category == "Security" || f.Category == "Credential" {
				sec -= 15
			} else if f.Category == "Concurrency" || f.Category == "Deadlock" {
				conc -= 15
			} else {
				rob -= 12
				arch -= 8
			}
		case SevP1:
			if f.Category == "Security" {
				sec -= 8
			} else if f.Category == "Concurrency" || f.Category == "ResourceLeak" {
				conc -= 8
			} else {
				rob -= 6
			}
		case SevP2:
			arch -= 3
			rob -= 3
		case SevP3:
			doc -= 2
			arch -= 1
		case SevP4:
			// Info does not deduct points
		}
	}

	clamp := func(val, min, max int) int {
		if val < min {
			return min
		}
		if val > max {
			return max
		}
		return val
	}

	arch = clamp(arch, 0, 20)
	sec = clamp(sec, 0, 25)
	conc = clamp(conc, 0, 20)
	rob = clamp(rob, 0, 20)
	doc = clamp(doc, 0, 15)

	total := arch + sec + conc + rob + doc

	var grade string
	switch {
	case total >= 95:
		grade = "A+"
	case total >= 90:
		grade = "A"
	case total >= 80:
		grade = "B"
	case total >= 70:
		grade = "C"
	case total >= 60:
		grade = "D"
	default:
		grade = "F"
	}

	return BaremScore{
		ArchitecturePoints:  arch,
		SecurityPoints:      sec,
		ConcurrencyPoints:   conc,
		RobustnessPoints:    rob,
		DocumentationPoints: doc,
		TotalPoints:         total,
		LetterGrade:         grade,
		Summary:             fmt.Sprintf("Quality Score: %d/100 (Grade: %s)", total, grade),
	}
}
