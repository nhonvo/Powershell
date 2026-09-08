package detector

import (
	"bufio"
	"bytes"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"time"

	"agyproj/internal/model"
)

// Detector analyzes project directories for technology stacks, git status, and sessions.
type Detector struct {
	UserHome string
}

func NewDetector(userHome string) *Detector {
	if userHome == "" {
		if h, err := os.UserHomeDir(); err == nil {
			userHome = h
		}
	}
	return &Detector{UserHome: userHome}
}

// Analyze inspects a directory and populates ProjectInfo.
func (d *Detector) Analyze(path string) model.ProjectInfo {
	cleanPath := filepath.Clean(path)
	name := filepath.Base(cleanPath)
	info := model.ProjectInfo{
		ID:           strings.ToLower(name),
		Name:         name,
		Path:         cleanPath,
		LastModified: time.Now(),
	}

	fi, err := os.Stat(cleanPath)
	if err != nil || !fi.IsDir() {
		info.Stack = "Unknown"
		return info
	}
	info.LastModified = fi.ModTime()

	// 1. Detect Stack
	info.Stack = d.DetectStack(cleanPath)

	// 2. Detect Git
	gitBranch, isGit, isDirty, dirtyCount := d.DetectGit(cleanPath)
	info.GitBranch = gitBranch
	info.IsGit = isGit
	info.IsDirty = isDirty
	info.DirtyCount = dirtyCount

	// 3. Detect Antigravity Sessions & Cost
	sessCount, cost := d.DetectSessions(name)
	info.SessionCount = sessCount
	info.TotalCost = cost

	return info
}

// DetectStack inspects files in directory to identify framework/languages.
func (d *Detector) DetectStack(dir string) string {
	var tags []string
	hasCsharp := false
	hasNode := false
	hasGo := false
	hasDocker := false
	hasPython := false
	hasRust := false

	entries, err := os.ReadDir(dir)
	if err != nil {
		return "Unknown"
	}

	for _, e := range entries {
		name := strings.ToLower(e.Name())
		if strings.HasSuffix(name, ".sln") || strings.HasSuffix(name, ".slnx") || strings.HasSuffix(name, ".csproj") {
			hasCsharp = true
		}
		if name == "package.json" {
			hasNode = true
		}
		if name == "go.mod" {
			hasGo = true
		}
		if name == "cargo.toml" {
			hasRust = true
		}
		if name == "requirements.txt" || name == "pyproject.toml" {
			hasPython = true
		}
		if strings.HasPrefix(name, "docker-compose") || name == "dockerfile" {
			hasDocker = true
		}

		// Check one level deep for common mono-repo subdirs like ui, web, csharp-sln
		if e.IsDir() && !strings.HasPrefix(name, ".") && name != "node_modules" && name != "bin" && name != "obj" {
			subPath := filepath.Join(dir, e.Name())
			if subEntries, err := os.ReadDir(subPath); err == nil {
				for _, se := range subEntries {
					sname := strings.ToLower(se.Name())
					if strings.HasSuffix(sname, ".sln") || strings.HasSuffix(sname, ".csproj") {
						hasCsharp = true
					}
					if sname == "package.json" {
						hasNode = true
					}
					if sname == "go.mod" {
						hasGo = true
					}
				}
			}
		}
	}

	if hasCsharp {
		tags = append(tags, ".NET / C#")
	}
	if hasNode {
		// Inspect package.json for React/Next
		pkgPath := filepath.Join(dir, "package.json")
		if !fileExists(pkgPath) {
			pkgPath = filepath.Join(dir, "ui", "package.json")
		}
		if data, err := os.ReadFile(pkgPath); err == nil {
			content := string(data)
			if strings.Contains(content, "\"next\"") {
				tags = append(tags, "Next.js")
			} else if strings.Contains(content, "\"react\"") {
				tags = append(tags, "React")
			} else {
				tags = append(tags, "TypeScript")
			}
		} else {
			tags = append(tags, "TypeScript")
		}
	}
	if hasGo {
		tags = append(tags, "Go")
	}
	if hasRust {
		tags = append(tags, "Rust")
	}
	if hasPython {
		tags = append(tags, "Python")
	}
	if hasDocker {
		tags = append(tags, "Docker")
	}

	if len(tags) == 0 {
		return "Generic Codebase"
	}
	return strings.Join(tags, " · ")
}

// DetectGit inspects .git/ directory for branch and dirty status.
func (d *Detector) DetectGit(dir string) (branch string, isGit bool, isDirty bool, dirtyCount int) {
	gitDir := filepath.Join(dir, ".git")
	fi, err := os.Stat(gitDir)
	if err != nil {
		return "no git", false, false, 0
	}

	isGit = true
	branch = "main"

	// Read HEAD
	headFile := filepath.Join(gitDir, "HEAD")
	if fi.IsDir() {
		if data, err := os.ReadFile(headFile); err == nil {
			line := strings.TrimSpace(string(data))
			if strings.HasPrefix(line, "ref: refs/heads/") {
				branch = strings.TrimPrefix(line, "ref: refs/heads/")
			} else if len(line) >= 7 {
				branch = line[:7]
			}
		}
	} else {
		// Worktree git file format: gitdir: /path/to/.git/worktrees/<name>
		if data, err := os.ReadFile(gitDir); err == nil {
			line := strings.TrimSpace(string(data))
			if strings.HasPrefix(line, "gitdir:") {
				realGitDir := strings.TrimSpace(strings.TrimPrefix(line, "gitdir:"))
				if subData, err := os.ReadFile(filepath.Join(realGitDir, "HEAD")); err == nil {
					subLine := strings.TrimSpace(string(subData))
					if strings.HasPrefix(subLine, "ref: refs/heads/") {
						branch = strings.TrimPrefix(subLine, "ref: refs/heads/")
					}
				}
			}
		}
	}

	// Fast git status check
	cmd := exec.Command("git", "-C", dir, "status", "--porcelain")
	var out bytes.Buffer
	cmd.Stdout = &out
	if err := cmd.Run(); err == nil {
		lines := strings.Split(strings.TrimSpace(out.String()), "\n")
		count := 0
		for _, l := range lines {
			if strings.TrimSpace(l) != "" {
				count++
			}
		}
		if count > 0 {
			isDirty = true
			dirtyCount = count
		}
	}

	return branch, isGit, isDirty, dirtyCount
}

// DetectSessions queries Antigravity brain transcripts for related sessions.
func (d *Detector) DetectSessions(projectName string) (count int, cost float64) {
	brainDir := filepath.Join(d.UserHome, ".gemini", "antigravity-cli", "brain")
	entries, err := os.ReadDir(brainDir)
	if err != nil {
		return 0, 0
	}

	targetName := strings.ToLower(projectName)

	for _, e := range entries {
		if e.IsDir() && e.Name() != "scratch" {
			logPath := filepath.Join(brainDir, e.Name(), ".system_generated", "logs", "transcript.jsonl")
			if f, err := os.Open(logPath); err == nil {
				scanner := bufio.NewScanner(f)
				buf := make([]byte, 0, 32*1024)
				scanner.Buffer(buf, 256*1024)
				linesChecked := 0
				matched := false
				steps := 0
				for scanner.Scan() {
					steps++
					linesChecked++
					if !matched && linesChecked <= 25 {
						line := scanner.Text()
						if strings.Contains(strings.ToLower(line), "/projects/"+targetName) ||
							strings.Contains(strings.ToLower(line), `"`+targetName+`"`) {
							matched = true
						}
					}
				}
				f.Close()

				if matched {
					count++
					cost += float64(steps*400) / 1000000.0 * 1.25
				}
			}
		}
	}

	return count, cost
}

func fileExists(path string) bool {
	_, err := os.Stat(path)
	return err == nil
}
