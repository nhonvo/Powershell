package engine

import (
	"strings"

	"agyswarm/internal/model"
)

var inputPatterns = []string{
	"[y/n]",
	"[y/N]",
	"[Y/n]",
	"(y/n)",
	"select an option",
	"choose an option",
	"confirm execution",
	"press enter to continue",
	"press any key",
	"approve tool call",
	"approve?",
	"password:",
	"enter value:",
	"? ",
	"> ",
}

var errorPatterns = []string{
	"429 rate limit",
	"resource_exhausted",
	"quota exceeded",
	"fatal: ",
	"panic: ",
	"segmentation fault",
	"permission denied",
	"command not found",
}

// DetectStatus analyzes the latest terminal output line and returns the inferred AgentStatus.
func DetectStatus(lastLines []string, isProcessAlive bool, exitCode int) (model.AgentStatus, string) {
	if !isProcessAlive {
		if exitCode == 0 {
			return model.StatusDone, "Process completed successfully"
		}
		return model.StatusError, "Process exited with failure"
	}

	if len(lastLines) == 0 {
		return model.StatusWorking, "Starting agent..."
	}

	// Examine last 3 lines for heuristics
	sampleSize := 3
	if len(lastLines) < sampleSize {
		sampleSize = len(lastLines)
	}
	recent := lastLines[len(lastLines)-sampleSize:]

	combined := strings.ToLower(strings.Join(recent, " "))

	// 1. Check for rate limit or fatal errors
	for _, ep := range errorPatterns {
		if strings.Contains(combined, ep) {
			return model.StatusError, "Detected quota/rate limit error"
		}
	}

	// 2. Check for human approval prompt
	lastLine := strings.ToLower(strings.TrimSpace(lastLines[len(lastLines)-1]))
	for _, ip := range inputPatterns {
		if strings.Contains(lastLine, ip) || strings.HasSuffix(lastLine, ip) {
			return model.StatusNeedInput, "Waiting for confirmation: " + truncate(lastLine, 40)
		}
	}

	return model.StatusWorking, "Agent actively executing"
}

func truncate(s string, max int) string {
	if len(s) > max {
		return s[:max-3] + "..."
	}
	return s
}
