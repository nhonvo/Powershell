package proxy

import (
	"fmt"
	"net/http"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"time"
)

// ToolDefinition describes a managed tool in the suite
type ToolDefinition struct {
	Name        string   `json:"name"`
	BinaryName  string   `json:"binary_name"`
	Aliases     []string `json:"aliases"`
	Description string   `json:"description"`
}

// GetRegisteredTools returns all registered programs in the agy suite
func GetRegisteredTools() []ToolDefinition {
	return []ToolDefinition{
		{
			Name:        "switch",
			BinaryName:  "agyswitch",
			Aliases:     []string{"s", "acc", "vault", "quota"},
			Description: "Antigravity context, multi-account vault, model quotas & sessions",
		},
		{
			Name:        "proj",
			BinaryName:  "agyproj",
			Aliases:     []string{"p", "ws", "project", "ide"},
			Description: "Project workspaces registry, stack detection & IDE launchers",
		},
		{
			Name:        "git",
			BinaryName:  "agygit",
			Aliases:     []string{"g", "wt", "worktree"},
			Description: "Multi-Agent Git fleet status & isolated branch worktrees",
		},
		{
			Name:        "docker",
			BinaryName:  "agydocker",
			Aliases:     []string{"d", "ram", "ps"},
			Description: "Container fleet lifecycle & WSL2 RAM/Swap resource guard",
		},
		{
			Name:        "term",
			BinaryName:  "agyterm",
			Aliases:     []string{"t", "theme", "font"},
			Description: "Terminal font customizer (Windows Terminal) & prompt themes",
		},
		{
			Name:        "mobile",
			BinaryName:  "agymobile",
			Aliases:     []string{"m", "remote", "phone", "pwa"},
			Description: "Mobile cockpit & remote station for Tailscale & SSH",
		},
		{
			Name:        "ollama",
			BinaryName:  "agyollama",
			Aliases:     []string{"ai", "llm", "localai", "model"},
			Description: "Local Ollama daemon, model manager, benchmarking & AI agent cockpit",
		},
		{
			Name:        "aws",
			BinaryName:  "aws",
			Aliases:     []string{"cloud", "s3", "localstack"},
			Description: "AWS Cloud identity, S3 buckets, SQS queues & LocalStack diagnostics",
		},
	}
}

// ResolveTool matches a subcommand or alias to a registered tool
func ResolveTool(aliasOrName string) *ToolDefinition {
	aliasOrName = strings.ToLower(strings.TrimSpace(aliasOrName))
	tools := GetRegisteredTools()
	for _, t := range tools {
		if strings.EqualFold(t.Name, aliasOrName) {
			return &t
		}
		for _, a := range t.Aliases {
			if strings.EqualFold(a, aliasOrName) {
				return &t
			}
		}
	}
	return nil
}

// FindBinary searches for the tool binary across candidate paths and selects the NEWEST build version based on ModTime.
func FindBinary(binName string) (string, error) {
	home, _ := os.UserHomeDir()
	cwd, _ := os.Getwd()

	candidates := []string{
		// 1. Current working directory ./bin/
		filepath.Join(cwd, "bin", binName),
		// 2. Project workspace ./bin/
		filepath.Join(home, "projects", "powershell-profile", "bin", binName),
		// 3. ~/.local/bin
		filepath.Join(home, ".local", "bin", binName),
		// 4. In-tree app bin
		filepath.Join(home, "projects", "powershell-profile", "apps", binName, binName),
	}

	// 5. Exe directory of agyx
	if exe, err := os.Executable(); err == nil {
		candidates = append(candidates, filepath.Join(filepath.Dir(exe), binName))
	}

	// 6. Look in PATH (including .exe / .cmd in WSL)
	if path, err := exec.LookPath(binName); err == nil {
		candidates = append(candidates, path)
	}
	if path, err := exec.LookPath(binName + ".exe"); err == nil {
		candidates = append(candidates, path)
	}
	if path, err := exec.LookPath(binName + ".cmd"); err == nil {
		candidates = append(candidates, path)
	}

	var newestPath string
	var newestTime time.Time
	seen := make(map[string]bool)

	for _, c := range candidates {
		cleaned := filepath.Clean(c)
		if seen[cleaned] {
			continue
		}
		seen[cleaned] = true

		fi, err := os.Stat(cleaned)
		if err != nil || fi.IsDir() {
			continue
		}

		if newestPath == "" || fi.ModTime().After(newestTime) {
			newestPath = cleaned
			newestTime = fi.ModTime()
		}
	}

	if newestPath != "" {
		return newestPath, nil
	}

	return "", fmt.Errorf("binary '%s' not found. Run 'make build' or 'make install'", binName)
}

// Execute proxies input/output/error directly to the child process
func Execute(binName string, args []string) error {
	binPath, err := FindBinary(binName)
	if err != nil {
		if binName == "aws" {
			return runAwsDiagnostics()
		}
		return err
	}

	cmd := exec.Command(binPath, args...)
	cmd.Stdin = os.Stdin
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	cmd.Env = os.Environ()

	return cmd.Run()
}

func runAwsDiagnostics() error {
	fmt.Println("\r\n☁️  \033[1;36mAWS Cloud & LocalStack Diagnostics\033[0m")
	fmt.Println("──────────────────────────────────────────────────────────────────────────────────")
	fmt.Println("  • Native AWS CLI ('aws' / 'aws.exe') not found in Linux PATH.")
	fmt.Println("  • Available PowerShell commands: aws-whoami, aws-s3, aws-local, aws-sqs")
	fmt.Println("  • Testing LocalStack mock service endpoint (http://localhost:4566)...")
	client := http.Client{Timeout: 800 * time.Millisecond}
	resp, err := client.Get("http://localhost:4566/_localstack/health")
	if err == nil {
		defer resp.Body.Close()
		fmt.Printf("    \033[1;32m✔ LocalStack is ONLINE (Status: %s)\033[0m\r\n", resp.Status)
	} else {
		fmt.Println("    \033[1;33m⚠️ LocalStack service is OFFLINE (boot via 'agydocker' or docker run localstack/localstack)\033[0m")
	}
	fmt.Println("──────────────────────────────────────────────────────────────────────────────────")
	fmt.Print("Press [Enter] to return...")
	var dummy [1]byte
	_, _ = os.Stdin.Read(dummy[:])
	return nil
}
