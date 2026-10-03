package markdown

import (
	"fmt"
	"os"
	"path/filepath"
	"regexp"
	"strings"
	"time"

	"agyreview/internal/model"
)

var ansiRegex = regexp.MustCompile(`\x1b\[[0-9;]*[a-zA-Z]`)

// StripANSI strips all ANSI escape sequences from strings.
func StripANSI(str string) string {
	return ansiRegex.ReplaceAllString(str, "")
}

// GenerateReviewReport generates a pure GitHub Flavored Markdown audit report.
func GenerateReviewReport(target *model.TargetRepo, findings []model.Finding, score model.BaremScore) (string, error) {
	var b strings.Builder
	timestamp := time.Now().Format("2006-01-02 15:04:05 MST")

	b.WriteString("# 🔍 Comprehensive Code Review & Architecture Audit Report\n\n")
	b.WriteString(fmt.Sprintf("> **Target Repository**: `%s`  \n", target.Name))
	b.WriteString(fmt.Sprintf("> **Target Path**: `%s`  \n", target.Path))
	b.WriteString(fmt.Sprintf("> **Branch / Commit**: `%s` (`%s`)  \n", target.Branch, target.HeadCommit))
	b.WriteString(fmt.Sprintf("> **Audit Date**: `%s`  \n", timestamp))
	b.WriteString("> **Auditor Engine**: `agyreview` (Headless 3-Loop + Code-Review Plugin)  \n\n")
	b.WriteString("---\n\n")

	// 1. Executive Quality Scorecard (100-Point Barème)
	b.WriteString("## 1. Executive Quality Scorecard & 100-Point Barème\n\n")
	b.WriteString(fmt.Sprintf("### Overall Grade: **%s** (%d / 100 Points)\n\n", score.LetterGrade, score.TotalPoints))

	b.WriteString("| Evaluation Category | Maximum Points | Awarded Score | Status |\n")
	b.WriteString("| :--- | :---: | :---: | :--- |\n")
	b.WriteString(fmt.Sprintf("| **Architecture & Modularity** | 20 | **%d** | %s |\n", score.ArchitecturePoints, getStatusIcon(score.ArchitecturePoints, 20)))
	b.WriteString(fmt.Sprintf("| **Security & Secret Hygiene** | 25 | **%d** | %s |\n", score.SecurityPoints, getStatusIcon(score.SecurityPoints, 25)))
	b.WriteString(fmt.Sprintf("| **Concurrency & Resource Safety** | 20 | **%d** | %s |\n", score.ConcurrencyPoints, getStatusIcon(score.ConcurrencyPoints, 20)))
	b.WriteString(fmt.Sprintf("| **Error Handling & Robustness** | 20 | **%d** | %s |\n", score.RobustnessPoints, getStatusIcon(score.RobustnessPoints, 20)))
	b.WriteString(fmt.Sprintf("| **Documentation & Code Standards** | 15 | **%d** | %s |\n", score.DocumentationPoints, getStatusIcon(score.DocumentationPoints, 15)))
	b.WriteString(fmt.Sprintf("| **TOTAL COMPOSITE SCORE** | **100** | **%d** | **Grade: %s** |\n\n", score.TotalPoints, score.LetterGrade))

	// Summary findings by severity
	p0Count, p1Count, p2Count, p3Count, p4Count := 0, 0, 0, 0, 0
	for _, f := range findings {
		switch f.Severity {
		case model.SevP0:
			p0Count++
		case model.SevP1:
			p1Count++
		case model.SevP2:
			p2Count++
		case model.SevP3:
			p3Count++
		case model.SevP4:
			p4Count++
		}
	}

	b.WriteString("### Finding Severity Breakdown\n\n")
	b.WriteString(fmt.Sprintf("- 🔴 **[P0 - CRITICAL]**: %d findings (Security, arbitary execution, deadlocks)\n", p0Count))
	b.WriteString(fmt.Sprintf("- 🟠 **[P1 - HIGH]**: %d findings (Resource leaks, unhandled panics)\n", p1Count))
	b.WriteString(fmt.Sprintf("- 🟡 **[P2 - MEDIUM]**: %d findings (Code smells, input validation)\n", p2Count))
	b.WriteString(fmt.Sprintf("- 🔵 **[P3 - LOW]**: %d findings (Stylistic inconsistencies, documentation)\n", p3Count))
	b.WriteString(fmt.Sprintf("- ⚪ **[P4 - INFO]**: %d recommendations (Optimizations, modernizations)\n\n", p4Count))
	b.WriteString("---\n\n")

	// 2. Evidence-Based Audit Findings
	b.WriteString("## 2. Evidence-Based Detailed Findings\n\n")
	if len(findings) == 0 {
		b.WriteString("🎉 **Zero defect findings detected! The codebase complies with all quality standards.**\n\n")
	} else {
		for i, f := range findings {
			link := fmt.Sprintf("[%s:%d](./%s#L%d)", f.FilePath, f.StartLine, f.FilePath, f.StartLine)
			b.WriteString(fmt.Sprintf("### Finding %d: [%s] %s in %s\n\n", i+1, f.Severity, f.Category, link))
			b.WriteString(fmt.Sprintf("* **Location**: `%s` (Lines %d–%d)\n", f.FilePath, f.StartLine, f.EndLine))
			b.WriteString(fmt.Sprintf("* **Discovered In**: Loop %d (Verified: %v)\n\n", f.LoopDiscovered, f.VerifiedNoRegression))
			b.WriteString("**Offending Code Snippet:**\n```go\n")
			b.WriteString(f.OffendingCode + "\n```\n\n")
			b.WriteString("**Failure Mechanism & Exploit Analysis:**\n")
			b.WriteString(f.FailureExplanation + "\n\n")
			b.WriteString("**Ready-to-Apply Concrete Remediation:**\n")
			b.WriteString(f.ConcreteFix + "\n\n")
			if f.PatchDiff != "" {
				b.WriteString("**Suggested Patch Diff:**\n```diff\n")
				b.WriteString(f.PatchDiff + "\n```\n\n")
			}
			b.WriteString("---\n\n")
		}
	}

	// 3. Recommended Next Steps
	b.WriteString("## 3. Recommended Strategic Next Steps\n\n")
	b.WriteString("1. **Remediation Plan**: Run `agyreview fix --interactive` to inspect and apply automated patches.\n")
	b.WriteString("2. **Product Roadmap**: Run `agyreview roadmap` to synthesize findings into a 3-horizon evolution plan.\n")
	b.WriteString("3. **CI/CD Integration**: Add `agyreview run --strict` as a mandatory blocking quality gate.\n\n")

	cleanOutput := StripANSI(b.String())

	// Save report in target repo: <target>/doc/audit/<timestamp>_review.md
	reportDir := filepath.Join(target.Path, "doc", "audit")
	_ = os.MkdirAll(reportDir, 0755)
	fileName := fmt.Sprintf("%s_audit_review.md", time.Now().Format("20060102_150405"))
	outPath := filepath.Join(reportDir, fileName)

	if err := os.WriteFile(outPath, []byte(cleanOutput), 0644); err != nil {
		return "", err
	}

	return outPath, nil
}

func getStatusIcon(current, max int) string {
	pct := float64(current) / float64(max)
	switch {
	case pct >= 0.9:
		return "🟢 EXCELLENT"
	case pct >= 0.75:
		return "🟡 GOOD"
	case pct >= 0.6:
		return "🟠 ACCEPTABLE"
	default:
		return "🔴 CRITICAL DEFICIT"
	}
}
