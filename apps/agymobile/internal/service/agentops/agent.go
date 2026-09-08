package agentops

import (
	"bufio"
	"encoding/json"
	"os"
	"os/exec"
	"path/filepath"
	"strconv"
	"strings"
)

type AgentSummary struct {
	ActiveCount int
	RecentStep  string
	ActiveAccount string
	QuotaUSD     float64
	TokenCount   int64
	ContainersUp int
	ContainersTotal int
}

// GetAgentSummary aggregates live agent, switch, and docker statuses
func GetAgentSummary() AgentSummary {
	summary := AgentSummary{
		ActiveAccount: "Default",
		QuotaUSD:     0.0,
		TokenCount:   0,
	}

	// 1. Check active account from ~/.gemini/antigravity-cli or agyswitch
	home, _ := os.UserHomeDir()
	envPath := filepath.Join(home, ".gemini", "antigravity-cli", "active_account.json")
	if data, err := os.ReadFile(envPath); err == nil {
		var acc struct {
			Name     string  `json:"name"`
			QuotaUSD float64 `json:"quota_usd"`
			Tokens   int64   `json:"tokens"`
		}
		if json.Unmarshal(data, &acc) == nil && acc.Name != "" {
			summary.ActiveAccount = acc.Name
			summary.QuotaUSD = acc.QuotaUSD
			summary.TokenCount = acc.Tokens
		}
	}

	// 2. Count active Docker containers
	if out, err := exec.Command("docker", "ps", "-q").Output(); err == nil {
		lines := strings.Split(strings.TrimSpace(string(out)), "\n")
		if len(lines) == 1 && lines[0] == "" {
			summary.ContainersUp = 0
		} else {
			summary.ContainersUp = len(lines)
		}
	}
	if out, err := exec.Command("docker", "ps", "-a", "-q").Output(); err == nil {
		lines := strings.Split(strings.TrimSpace(string(out)), "\n")
		if len(lines) == 1 && lines[0] == "" {
			summary.ContainersTotal = 0
		} else {
			summary.ContainersTotal = len(lines)
		}
	}

	// 3. Find latest transcript step in ~/.gemini/antigravity-cli/brain/
	brainDir := filepath.Join(home, ".gemini", "antigravity-cli", "brain")
	if entries, err := os.ReadDir(brainDir); err == nil {
		for _, e := range entries {
			if e.IsDir() {
				logPath := filepath.Join(brainDir, e.Name(), ".system_generated", "logs", "transcript.jsonl")
				if f, err := os.Open(logPath); err == nil {
					scanner := bufio.NewScanner(f)
					var lastLine string
					count := 0
					for scanner.Scan() {
						line := strings.TrimSpace(scanner.Text())
						if line != "" {
							lastLine = line
							count++
						}
					}
					_ = f.Close()

					if lastLine != "" {
						type stepInfo struct {
							StepIndex int    `json:"step_index"`
							Type      string `json:"type"`
							Thinking  string `json:"thinking"`
						}
						var s stepInfo
						if json.Unmarshal([]byte(lastLine), &s) == nil {
							summary.ActiveCount = 1
							summary.RecentStep = "Step #" + strconv.Itoa(s.StepIndex) + " (" + s.Type + ")"
							if len(s.Thinking) > 40 {
								summary.RecentStep += " · " + s.Thinking[:40] + "..."
							}
						}
					}
					break
				}
			}
		}
	}

	return summary
}
