package engine

import (
	"fmt"
	"sync"

	"agyreview/internal/model"
)

type SwarmAgentRole string

const (
	RoleSecuritySentry     SwarmAgentRole = "Agent Alpha (Security Sentry)"
	RoleConcurrencyAuditor SwarmAgentRole = "Agent Beta (Concurrency Auditor)"
	RoleArchitectureJudge  SwarmAgentRole = "Agent Gamma (Architecture Arbiter)"
	RoleLeadArbiter        SwarmAgentRole = "Agent Delta (Lead Arbiter)"
)

type SwarmExecutionResult struct {
	AgentRole SwarmAgentRole
	Findings  []model.Finding
}

// CoordinateSwarm coordinates parallel multi-agent audits across the target directory.
func CoordinateSwarm(targetPath string) ([]model.Finding, model.BaremScore, error) {
	var wg sync.WaitGroup
	resultsChan := make(chan SwarmExecutionResult, 3)

	// Agent Alpha: Security Sentry
	wg.Add(1)
	go func() {
		defer wg.Done()
		findings, _ := AnalyzeDirectory(targetPath, 1)
		var secFindings []model.Finding
		for _, f := range findings {
			if f.Category == "Security" {
				secFindings = append(secFindings, f)
			}
		}
		resultsChan <- SwarmExecutionResult{
			AgentRole: RoleSecuritySentry,
			Findings:  secFindings,
		}
	}()

	// Agent Beta: Concurrency Auditor
	wg.Add(1)
	go func() {
		defer wg.Done()
		findings, _ := AnalyzeDirectory(targetPath, 2)
		var concFindings []model.Finding
		for _, f := range findings {
			if f.Category == "Concurrency" {
				concFindings = append(concFindings, f)
			}
		}
		resultsChan <- SwarmExecutionResult{
			AgentRole: RoleConcurrencyAuditor,
			Findings:  concFindings,
		}
	}()

	// Agent Gamma: Architecture Arbiter
	wg.Add(1)
	go func() {
		defer wg.Done()
		findings, _ := AnalyzeDirectory(targetPath, 3)
		var archFindings []model.Finding
		for _, f := range findings {
			if f.Category != "Security" && f.Category != "Concurrency" {
				archFindings = append(archFindings, f)
			}
		}
		resultsChan <- SwarmExecutionResult{
			AgentRole: RoleArchitectureJudge,
			Findings:  archFindings,
		}
	}()

	wg.Wait()
	close(resultsChan)

	// Agent Delta: Lead Arbiter collects and deduplicates
	var aggregated []model.Finding
	seen := make(map[string]bool)

	for res := range resultsChan {
		for _, f := range res.Findings {
			key := fmt.Sprintf("%s:%d:%s", f.FilePath, f.StartLine, f.ID)
			if !seen[key] {
				seen[key] = true
				aggregated = append(aggregated, f)
			}
		}
	}

	score := model.ComputeBaremScore(aggregated)
	return aggregated, score, nil
}
