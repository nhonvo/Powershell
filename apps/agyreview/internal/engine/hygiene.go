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
	backupExts = map[string]bool{
		".bak": true, ".tmp": true, ".old": true, ".orig": true, ".swp": true, ".draft": true, ".copy": true,
	}
	tempPrefixes = []string{"temp", "tmp", "test2", "untitled", "scratch", "draft", "copy_of", "copy of"}
)

// AnalyzeFileNamingAndHygiene evaluates filenames, path conventions, and backup/temp file clutter.
func AnalyzeFileNamingAndHygiene(fullPath string, relPath string, info os.FileInfo, loopIndex int) []model.Finding {
	var findings []model.Finding
	name := info.Name()
	lowerName := strings.ToLower(name)
	ext := strings.ToLower(filepath.Ext(name))

	// 1. Unused backup/temporary extensions
	if backupExts[ext] || strings.HasSuffix(name, "~") {
		findings = append(findings, model.Finding{
			ID:                 "FILE-UNUSED-BACKUP",
			Severity:           model.SevP3,
			Category:           "Unused Files & File Standards",
			FilePath:           relPath,
			StartLine:          1,
			EndLine:            1,
			OffendingCode:      name,
			FailureExplanation: "Unused backup or temporary file left in source tree (" + ext + ").",
			ConcreteFix:        "Remove temporary backup file or add rule to .gitignore.",
			LoopDiscovered:     loopIndex,
		})
	}

	// 2. Spaces in filename
	if strings.Contains(name, " ") {
		findings = append(findings, model.Finding{
			ID:                 "FILE-NAME-SPACE",
			Severity:           model.SevP3,
			Category:           "Naming & File Standards",
			FilePath:           relPath,
			StartLine:          1,
			EndLine:            1,
			OffendingCode:      name,
			FailureExplanation: "Filename contains spaces which causes cross-platform shell escaping issues.",
			ConcreteFix:        "Rename file to use kebab-case or snake_case without spaces.",
			LoopDiscovered:     loopIndex,
		})
	}

	// 3. Draft or temporary file names
	for _, prefix := range tempPrefixes {
		if strings.HasPrefix(lowerName, prefix) && !strings.Contains(lowerName, "template") {
			findings = append(findings, model.Finding{
				ID:                 "FILE-NAME-TEMP",
				Severity:           model.SevP3,
				Category:           "Naming & File Standards",
				FilePath:           relPath,
				StartLine:          1,
				EndLine:            1,
				OffendingCode:      name,
				FailureExplanation: "Temporary or draft file name detected in source tree.",
				ConcreteFix:        "Rename file to reflect its domain purpose or clean up scratch file.",
				LoopDiscovered:     loopIndex,
			})
			break
		}
	}

	// 4. Go filename casing rules (Go filenames must be lowercase snake_case)
	if ext == ".go" && name != lowerName && !strings.HasSuffix(name, "_test.go") {
		findings = append(findings, model.Finding{
			ID:                 "FILE-NAME-GO-CASING",
			Severity:           model.SevP3,
			Category:           "Naming & File Standards",
			FilePath:           relPath,
			StartLine:          1,
			EndLine:            1,
			OffendingCode:      name,
			FailureExplanation: "Go source file name contains uppercase letters; idiomatic Go filenames use lowercase snake_case.",
			ConcreteFix:        "Rename Go source file to lowercase (e.g., " + strings.ToLower(name) + ").",
			LoopDiscovered:     loopIndex,
		})
	}

	return findings
}

// AnalyzeMarkdownHygiene checks markdown files for empty content, unresolved draft markers, or orphan scratch files.
func AnalyzeMarkdownHygiene(fullPath string, relPath string, info os.FileInfo, loopIndex int) []model.Finding {
	var findings []model.Finding

	// Ignore audit report directory doc/audit
	if strings.Contains(relPath, "doc/audit") || strings.Contains(relPath, "doc\\audit") {
		return nil
	}

	// 1. Empty Markdown File
	if info.Size() == 0 {
		findings = append(findings, model.Finding{
			ID:                 "MD-EMPTY-FILE",
			Severity:           model.SevP3,
			Category:           "Unused Files & Documentation",
			FilePath:           relPath,
			StartLine:          1,
			EndLine:            1,
			OffendingCode:      info.Name(),
			FailureExplanation: "Empty markdown documentation file without content.",
			ConcreteFix:        "Populate document content or delete unused empty markdown file.",
			LoopDiscovered:     loopIndex,
		})
		return findings
	}

	// 2. Orphan Scratch Markdown File
	lowerRel := strings.ToLower(relPath)
	if strings.Contains(lowerRel, "scratch/") || strings.Contains(lowerRel, "tmp/") || strings.HasPrefix(info.Name(), "temp_") || strings.HasPrefix(info.Name(), "scratch_") {
		findings = append(findings, model.Finding{
			ID:                 "MD-ORPHAN-SCRATCH",
			Severity:           model.SevP3,
			Category:           "Unused Files & Documentation",
			FilePath:           relPath,
			StartLine:          1,
			EndLine:            1,
			OffendingCode:      info.Name(),
			FailureExplanation: "Unused scratch or temporary markdown file in working repository.",
			ConcreteFix:        "Consolidate documentation into official doc/ structure or clean up scratch markdown file.",
			LoopDiscovered:     loopIndex,
		})
	}

	// 3. Inspect Markdown Content for Draft Placeholders
	file, err := os.Open(fullPath)
	if err != nil {
		return findings
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	lineNum := 0
	draftMarkerRegex := regexp.MustCompile(`(?i)(TODO:\s*write\s*doc|TBD|\[Placeholder\]|Draft\s*document|\[TODO\])`)

	for scanner.Scan() {
		lineNum++
		line := scanner.Text()
		if draftMarkerRegex.MatchString(line) {
			findings = append(findings, model.Finding{
				ID:                 "MD-UNRESOLVED-DRAFT",
				Severity:           model.SevP3,
				Category:           "Unused Files & Documentation",
				FilePath:           relPath,
				StartLine:          lineNum,
				EndLine:            lineNum,
				OffendingCode:      strings.TrimSpace(line),
				FailureExplanation: "Unresolved draft placeholder or incomplete documentation marker detected.",
				ConcreteFix:        "Complete the documentation text or remove draft marker.",
				LoopDiscovered:     loopIndex,
			})
		}
	}

	return findings
}
