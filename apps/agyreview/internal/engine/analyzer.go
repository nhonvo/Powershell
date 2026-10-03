package engine

import (
	"bufio"
	"os"
	"path/filepath"
	"regexp"
	"strings"

	"agyreview/internal/model"
)

var (
	secretRegex   = regexp.MustCompile(`(?i)(api[_-]?key|secret|token|password|bearer|auth[_-]?token)\s*[:=]\s*["']([A-Za-z0-9_\-\.\~]{8,})["']`)
	execCmdRegex  = regexp.MustCompile(`exec\.Command\s*\(\s*["'](?:bash|sh)["']\s*,\s*["']-c["']\s*,\s*(fmt\.Sprintf|\+).*`)
	goroutineLeak = regexp.MustCompile(`go\s+func\s*\(\s*\)\s*\{`)
)

type FileAnalysis struct {
	FilePath string
	Findings []model.Finding
}

// AnalyzeDirectory scans source code files in target path for static hygiene, security patterns, and concurrency invariants.
func AnalyzeDirectory(rootPath string, loopIndex int) ([]model.Finding, error) {
	var findings []model.Finding
	count := 0

	err := filepath.Walk(rootPath, func(path string, info os.FileInfo, err error) error {
		if err != nil {
			return nil
		}
		if info.IsDir() {
			name := info.Name()
			if strings.HasPrefix(name, ".") || name == "node_modules" || name == "vendor" || name == "dist" || name == "bin" || name == "obj" {
				return filepath.SkipDir
			}
			return nil
		}

		relPath, _ := filepath.Rel(rootPath, path)
		relPath = filepath.ToSlash(relPath)

		// 1. Evaluate file naming, path standards, and temporary/backup file hygiene
		namingFindings := AnalyzeFileNamingAndHygiene(path, relPath, info, loopIndex)
		for _, f := range namingFindings {
			count++
			f.ID = f.ID + "-" + string(rune('0'+count))
			findings = append(findings, f)
		}

		// 2. Evaluate Markdown documentation hygiene & unused markdown checks
		ext := strings.ToLower(filepath.Ext(path))
		if ext == ".md" {
			mdFindings := AnalyzeMarkdownHygiene(path, relPath, info, loopIndex)
			for _, f := range mdFindings {
				count++
				f.ID = f.ID + "-" + string(rune('0'+count))
				findings = append(findings, f)
			}
			return nil
		}

		// 3. Only inspect user code & script files (skip minified/bundled vendor assets)
		lowerPath := strings.ToLower(path)
		if strings.HasSuffix(lowerPath, ".min.js") || strings.HasSuffix(lowerPath, ".bundle.js") || strings.Contains(lowerPath, "bootstrap") || strings.Contains(lowerPath, "jquery") {
			return nil
		}

		if ext != ".go" && ext != ".ps1" && ext != ".sh" && ext != ".js" && ext != ".ts" && ext != ".py" {
			return nil
		}

		fileFindings, _ := analyzeFile(path, relPath, loopIndex)
		for _, f := range fileFindings {
			count++
			f.ID = f.ID + "-" + string(rune('0'+count))
			findings = append(findings, f)
		}
		return nil
	})

	return findings, err
}

func analyzeFile(fullPath string, relPath string, loopIndex int) ([]model.Finding, error) {
	file, err := os.Open(fullPath)
	if err != nil {
		return nil, err
	}
	defer file.Close()

	var findings []model.Finding
	scanner := bufio.NewScanner(file)
	lineNum := 0

	for scanner.Scan() {
		lineNum++
		line := scanner.Text()

		// Loop 1 & 2: Hardcoded secrets detection (ignore test files, examples, and data attribute key constants)
		if secretRegex.MatchString(line) && !strings.Contains(strings.ToLower(relPath), "test") && !strings.Contains(line, "example") && !strings.Contains(line, "DATA_API_KEY") {
			findings = append(findings, model.Finding{
				ID:                 "SEC-KEY",
				Severity:           model.SevP0,
				Category:           "Security",
				FilePath:           relPath,
				StartLine:          lineNum,
				EndLine:            lineNum,
				OffendingCode:      strings.TrimSpace(line),
				FailureExplanation: "Potential hardcoded credential or sensitive secret detected directly in source code.",
				ConcreteFix:        "Extract sensitive secret into environment variables or secure vault store.",
				LoopDiscovered:     loopIndex,
			})
		}

		// Loop 1 & 2: Command injection sanitization check
		if execCmdRegex.MatchString(line) {
			findings = append(findings, model.Finding{
				ID:                 "SEC-INJ",
				Severity:           model.SevP0,
				Category:           "Security",
				FilePath:           relPath,
				StartLine:          lineNum,
				EndLine:            lineNum,
				OffendingCode:      strings.TrimSpace(line),
				FailureExplanation: "Unsanitized command string passed to subshell execution via exec.Command - high risk of arbitrary command injection.",
				ConcreteFix:        "Invoke command arguments as discrete array parameters without passing through bash -c string concatenation.",
				LoopDiscovered:     loopIndex,
			})
		}

		// Loop 2: Concurrency invariants (unbounded goroutines without context cancellation)
		if loopIndex >= 2 && goroutineLeak.MatchString(line) && !strings.Contains(line, "ctx") {
			findings = append(findings, model.Finding{
				ID:                 "CONC-LEAK",
				Severity:           model.SevP1,
				Category:           "Concurrency",
				FilePath:           relPath,
				StartLine:          lineNum,
				EndLine:            lineNum,
				OffendingCode:      strings.TrimSpace(line),
				FailureExplanation: "Anonymous background goroutine launched without context cancellation or lifecycle management channel.",
				ConcreteFix:        "Pass a context.Context with timeout/cancellation or a stop channel to guarantee goroutine termination.",
				LoopDiscovered:     loopIndex,
			})
		}
	}

	return findings, nil
}
