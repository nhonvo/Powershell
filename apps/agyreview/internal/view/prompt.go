package view

import (
	"bufio"
	"fmt"
	"os"
	"strings"

	"agyreview/internal/model"
)

// PromptSuggestedReview asks the user if they want to run a review on a detected change.
func PromptSuggestedReview(repoPath, oldBranch, newBranch string) bool {
	fmt.Printf("\n💡 [agyreview Sentinel] Changes detected in %s:\n", repoPath)
	fmt.Printf("   Branch switch / commit: %s ➔ %s\n", oldBranch, newBranch)
	fmt.Print("   Would you like to trigger an automated code review now? [Y/n]: ")

	scanner := bufio.NewScanner(os.Stdin)
	if scanner.Scan() {
		text := strings.ToLower(strings.TrimSpace(scanner.Text()))
		if text == "" || text == "y" || text == "yes" {
			return true
		}
	}
	return false
}

// DisplayScorecard prints a compact ANSI colored scorecard to the terminal.
func DisplayScorecard(target *model.TargetRepo, score model.BaremScore, findings []model.Finding, reportPath string) {
	fmt.Println("\n════════════════════════════════════════════════════════════════════════════════")
	fmt.Printf(" 🔍 AGYREVIEW Quality Scorecard: %s (%s)\n", target.Name, target.Branch)
	fmt.Println("════════════════════════════════════════════════════════════════════════════════")
	fmt.Printf(" Overall Grade: \033[1;32m%s\033[0m (%d / 100 Points)\n\n", score.LetterGrade, score.TotalPoints)

	fmt.Printf(" • Architecture & Modularity:    \033[1m%2d / 20\033[0m\n", score.ArchitecturePoints)
	fmt.Printf(" • Security & Secret Hygiene:    \033[1m%2d / 25\033[0m\n", score.SecurityPoints)
	fmt.Printf(" • Concurrency & Resource Safety:\033[1m%2d / 20\033[0m\n", score.ConcurrencyPoints)
	fmt.Printf(" • Robustness & Error Handling:  \033[1m%2d / 20\033[0m\n", score.RobustnessPoints)
	fmt.Printf(" • Documentation & Standards:    \033[1m%2d / 15\033[0m\n\n", score.DocumentationPoints)

	fmt.Printf(" Total Findings: \033[1;33m%d\033[0m\n", len(findings))
	for i, f := range findings {
		if i >= 5 {
			fmt.Printf(" ... and %d more findings\n", len(findings)-5)
			break
		}
		fmt.Printf("  %d. %s [%s] %s (%s:%d)\n", i+1, f.Severity.ColorCode(), f.Category, f.FailureExplanation, f.FilePath, f.StartLine)
	}

	fmt.Println("────────────────────────────────────────────────────────────────────────────────")
	fmt.Printf(" Pure Markdown Report: \033[1;36m%s\033[0m\n", reportPath)
	fmt.Println("════════════════════════════════════════════════════════════════════════════════")
}
