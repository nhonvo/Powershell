package proxy

import (
	"context"
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
	if binName == "aws" {
		return RunAWS(args)
	}

	binPath, err := FindBinary(binName)
	if err != nil {
		return err
	}

	cmd := exec.Command(binPath, args...)
	cmd.Stdin = os.Stdin
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	cmd.Env = os.Environ()

	return cmd.Run()
}

// RunAWS handles AWS CLI execution, subcommands, and diagnostic fallbacks safely
func RunAWS(args []string) error {
	binPath, _ := FindBinary("aws")

	// If no arguments provided, run the diagnostics and quick command console!
	if len(args) == 0 || args[0] == "status" || args[0] == "diagnostics" || args[0] == "diag" {
		return runAwsDiagnostics(binPath)
	}

	// Translate friendly shortcuts
	var finalArgs []string
	switch strings.ToLower(args[0]) {
	case "whoami", "identity", "id":
		finalArgs = append([]string{"sts", "get-caller-identity"}, args[1:]...)
	case "s3":
		if len(args) == 1 {
			finalArgs = []string{"s3", "ls"}
		} else {
			finalArgs = args
		}
	case "sqs":
		if len(args) == 1 {
			finalArgs = []string{"sqs", "list-queues"}
		} else {
			finalArgs = args
		}
	case "local", "localstack", "health":
		return checkLocalStack()
	default:
		finalArgs = args
	}

	if binPath == "" {
		fmt.Println("\r\n⚠️  \033[1;33mAWS CLI binary not found in PATH.\033[0m")
		return runAwsDiagnostics("")
	}

	cmd := exec.Command(binPath, finalArgs...)
	cmd.Stdin = os.Stdin
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	cmd.Env = os.Environ()
	return cmd.Run()
}

func checkLocalStack() error {
	client := http.Client{Timeout: 1500 * time.Millisecond}
	resp, err := client.Get("http://localhost:4566/_localstack/health")
	if err != nil {
		fmt.Println("\r\n⚠️  \033[1;33mLocalStack service is not reachable at http://localhost:4566\033[0m")
		fmt.Println("   Start it via: 'docker run -d -p 4566:4566 localstack/localstack'")
		return nil
	}
	defer resp.Body.Close()
	fmt.Printf("\r\n✔ \033[1;32mLocalStack is ONLINE (HTTP %s)\033[0m\r\n", resp.Status)
	return nil
}

func runAwsDiagnostics(binPath string) error {
	fmt.Println("\r\n☁️  \033[1;36mAWS Cloud & LocalStack Diagnostics\033[0m")
	fmt.Println("──────────────────────────────────────────────────────────────────────────────────")
	if binPath != "" {
		fmt.Printf("  • AWS CLI Binary:   \033[1;32m%s\033[0m\r\n", binPath)
		ctx1, cancel1 := context.WithTimeout(context.Background(), 1500*time.Millisecond)
		defer cancel1()
		if verOut, err := exec.CommandContext(ctx1, binPath, "--version").CombinedOutput(); err == nil {
			fmt.Printf("  • Version:          %s", string(verOut))
		}
		fmt.Print("  • AWS IAM Identity: ")
		ctx2, cancel2 := context.WithTimeout(context.Background(), 1500*time.Millisecond)
		defer cancel2()
		stsCmd := exec.CommandContext(ctx2, binPath, "sts", "get-caller-identity")
		if stsOut, err := stsCmd.CombinedOutput(); err == nil {
			fmt.Printf("\033[1;32mConfigured\033[0m\r\n    %s\r\n", strings.TrimSpace(string(stsOut)))
		} else {
			fmt.Println("\033[1;33mNo active credentials (or offline)\033[0m")
		}
	} else {
		fmt.Println("  • AWS CLI:          \033[1;33mNot found in Linux/Windows PATH\033[0m")
	}

	fmt.Print("  • LocalStack Mock:  ")
	client := http.Client{Timeout: 800 * time.Millisecond}
	resp, err := client.Get("http://localhost:4566/_localstack/health")
	if err == nil {
		defer resp.Body.Close()
		fmt.Printf("\033[1;32mONLINE (Status: %s)\033[0m\r\n", resp.Status)
	} else {
		fmt.Println("\033[1;33mOFFLINE (http://localhost:4566)\033[0m")
	}

	fmt.Println("──────────────────────────────────────────────────────────────────────────────────")
	fmt.Println("  Quick Commands:")
	fmt.Println("    • agyx aws whoami       - Query caller identity (aws sts get-caller-identity)")
	fmt.Println("    • agyx aws s3           - List S3 buckets (aws s3 ls)")
	fmt.Println("    • agyx aws sqs          - List SQS queues (aws sqs list-queues)")
	fmt.Println("    • agyx aws local        - Test LocalStack health")
	fmt.Println("──────────────────────────────────────────────────────────────────────────────────")
	return nil
}
