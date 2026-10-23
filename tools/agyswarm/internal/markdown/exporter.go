package markdown

import (
	"fmt"
	"os"
	"path/filepath"
	"regexp"
	"strings"
	"time"

	"agyswarm/internal/model"
)

var ansiRegex = regexp.MustCompile(`\x1b\[[0-9;]*[a-zA-Z]`)

// StripANSI removes ANSI terminal color and control codes from a string.
func StripANSI(str string) string {
	return ansiRegex.ReplaceAllString(str, "")
}

type Exporter struct {
	OutputDir string
}

func NewExporter(outputDir string) *Exporter {
	if outputDir == "" {
		outputDir = filepath.Join(".", "doc", "swarm")
	}
	return &Exporter{OutputDir: outputDir}
}

// ExportAgentSession writes a dedicated Markdown report for a single agent session.
func (e *Exporter) ExportAgentSession(sess *model.AgentSession) (string, error) {
	if err := os.MkdirAll(e.OutputDir, 0755); err != nil {
		return "", fmt.Errorf("failed to create output dir: %w", err)
	}

	timestamp := sess.CreatedAt.Format("20060102_150405")
	filename := fmt.Sprintf("agent_%s_%s.md", sess.ID, timestamp)
	targetPath := filepath.Join(e.OutputDir, filename)

	var sb strings.Builder
	sb.WriteString(fmt.Sprintf("# Agent Session Dossier: %s\n\n", sess.Name))
	sb.WriteString("> **Deliverable Type**: Swarm Agent Execution & Audit Transcript\n")
	sb.WriteString(fmt.Sprintf("> **Generated**: %s\n\n", time.Now().Format("2006-01-02 15:04:05 MST")))

	sb.WriteString("## 1. Metadata & Execution Context\n\n")
	sb.WriteString("| Field | Value |\n")
	sb.WriteString("| :--- | :--- |\n")
	sb.WriteString(fmt.Sprintf("| **Agent ID** | `%s` |\n", sess.ID))
	sb.WriteString(fmt.Sprintf("| **Agent Name** | `%s` |\n", sess.Name))
	sb.WriteString(fmt.Sprintf("| **Account Context** | `%s` |\n", sess.AccountName))
	sb.WriteString(fmt.Sprintf("| **Workspace Dir** | `%s` |\n", sess.WorkspaceDir))
	sb.WriteString(fmt.Sprintf("| **Command** | `%s %s` |\n", sess.Command, strings.Join(sess.Args, " ")))
	sb.WriteString(fmt.Sprintf("| **Status** | `%s` |\n", sess.Status))
	sb.WriteString(fmt.Sprintf("| **PID** | `%d` |\n", sess.PID))
	sb.WriteString(fmt.Sprintf("| **Exit Code** | `%d` |\n", sess.ExitCode))
	if sess.PromptHint != "" {
		sb.WriteString(fmt.Sprintf("| **Last Prompt Hint** | `%s` |\n", sess.PromptHint))
	}
	sb.WriteString("\n---\n\n")

	sb.WriteString("## 2. Terminal Transcript Log\n\n")
	sb.WriteString("```text\n")
	lines := sess.GetRecentLines(len(sess.Lines))
	if len(lines) == 0 {
		sb.WriteString("[No console output recorded]\n")
	} else {
		for _, l := range lines {
			cleanLine := StripANSI(l)
			sb.WriteString(cleanLine)
			sb.WriteString("\n")
		}
	}
	sb.WriteString("```\n\n")

	sb.WriteString("## 3. Reference Links\n\n")
	relPath := fmt.Sprintf("./%s/%s", filepath.Clean(e.OutputDir), filename)
	sb.WriteString(fmt.Sprintf("* **VS Code Relative Link:** [%s](%s)\n", filename, relPath))
	sb.WriteString(fmt.Sprintf("* **Local File Path:** `%s`\n", targetPath))

	if err := os.WriteFile(targetPath, []byte(sb.String()), 0644); err != nil {
		return "", fmt.Errorf("failed to write markdown dossier: %w", err)
	}

	return targetPath, nil
}

// ExportSwarmTask writes a comprehensive swarm summary report containing all involved agents and artifacts.
func (e *Exporter) ExportSwarmTask(task *model.SwarmTask, sessions []*model.AgentSession, artifacts []model.BlackboardArtifact) (string, error) {
	if err := os.MkdirAll(e.OutputDir, 0755); err != nil {
		return "", fmt.Errorf("failed to create output dir: %w", err)
	}

	timestamp := task.CreatedAt.Format("20060102_150405")
	slug := strings.ToLower(strings.ReplaceAll(task.ID, " ", "_"))
	filename := fmt.Sprintf("swarm_task_%s_%s.md", slug, timestamp)
	targetPath := filepath.Join(e.OutputDir, filename)

	var sb strings.Builder
	sb.WriteString(fmt.Sprintf("# Swarm Task Dossier: %s\n\n", task.Goal))
	sb.WriteString("> **Deliverable Type**: Multi-Agent Swarm Collaborative Report\n")
	sb.WriteString(fmt.Sprintf("> **Generated**: %s\n\n", time.Now().Format("2006-01-02 15:04:05 MST")))

	sb.WriteString("## 1. Task Overview\n\n")
	sb.WriteString(fmt.Sprintf("- **Task ID**: `%s`\n", task.ID))
	sb.WriteString(fmt.Sprintf("- **Target Project**: `%s`\n", task.TargetProject))
	sb.WriteString(fmt.Sprintf("- **Status**: `%s`\n", task.Status))
	sb.WriteString(fmt.Sprintf("- **Created**: `%s`\n", task.CreatedAt.Format(time.RFC3339)))
	if !task.CompletedAt.IsZero() {
		sb.WriteString(fmt.Sprintf("- **Completed**: `%s`\n", task.CompletedAt.Format(time.RFC3339)))
	}
	sb.WriteString("\n")

	sb.WriteString("## 2. Participated Agents\n\n")
	sb.WriteString("| ID | Name | Account | Status | Exit Code |\n")
	sb.WriteString("| :--- | :--- | :--- | :--- | :--- |\n")
	for _, s := range sessions {
		sb.WriteString(fmt.Sprintf("| `%s` | `%s` | `%s` | `%s` | `%d` |\n",
			s.ID, s.Name, s.AccountName, s.Status, s.ExitCode))
	}
	sb.WriteString("\n")

	sb.WriteString("## 3. Shared Blackboard Artifacts\n\n")
	if len(artifacts) == 0 {
		sb.WriteString("_No shared blackboard artifacts created during this swarm run._\n\n")
	} else {
		for _, art := range artifacts {
			sb.WriteString(fmt.Sprintf("### Artifact: `%s` (by Agent `%s`)\n\n", art.Topic, art.AgentID))
			sb.WriteString(fmt.Sprintf("> Updated at: %s\n\n", art.Timestamp.Format(time.RFC3339)))
			sb.WriteString("```markdown\n")
			sb.WriteString(art.Markdown)
			sb.WriteString("\n```\n\n")
		}
	}

	sb.WriteString("## 4. Agent Console Outputs\n\n")
	for _, s := range sessions {
		sb.WriteString(fmt.Sprintf("### Output: %s (`%s`)\n\n", s.Name, s.ID))
		sb.WriteString("```text\n")
		lines := s.GetRecentLines(len(s.Lines))
		for _, l := range lines {
			sb.WriteString(StripANSI(l))
			sb.WriteString("\n")
		}
		sb.WriteString("```\n\n")
	}

	sb.WriteString("## 5. Reference Links\n\n")
	relPath := fmt.Sprintf("./%s/%s", filepath.Clean(e.OutputDir), filename)
	sb.WriteString(fmt.Sprintf("* **VS Code Relative Link:** [%s](%s)\n", filename, relPath))
	sb.WriteString(fmt.Sprintf("* **Local File Path:** `%s`\n", targetPath))

	if err := os.WriteFile(targetPath, []byte(sb.String()), 0644); err != nil {
		return "", fmt.Errorf("failed to write swarm task markdown: %w", err)
	}

	return targetPath, nil
}
