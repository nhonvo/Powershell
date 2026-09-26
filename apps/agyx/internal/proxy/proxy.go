package proxy

import (
	"context"
	"fmt"
	"net/http"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
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
		{
			Name:        "bot",
			BinaryName:  "agybot",
			Aliases:     []string{"b", "telegram", "tg", "agentbot"},
			Description: "Antigravity remote Telegram controller, multi-project & research daemon",
		},
		{
			Name:        "port",
			BinaryName:  "agyport",
			Aliases:     []string{"ports", "killport", "kp", "killports"},
			Description: "Active network port manager, process killer & smart RAM leverage optimizer",
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

// FindBinary searches for the tool binary across candidate paths and selects the best build version.
// On Linux/Unix, native binaries are always prioritized over WSL Windows interop (.exe / /mnt/) binaries.
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

	// 6. Look in PATH
	if path, err := exec.LookPath(binName); err == nil {
		candidates = append(candidates, path)
	}

	if runtime.GOOS == "windows" {
		candidates = append(candidates,
			filepath.Join(cwd, "bin", binName+".exe"),
			filepath.Join(home, "projects", "powershell-profile", "bin", binName+".exe"),
			filepath.Join(home, ".local", "bin", binName+".exe"),
			filepath.Join(home, "projects", "powershell-profile", "apps", binName, binName+".exe"),
		)
		if path, err := exec.LookPath(binName + ".exe"); err == nil {
			candidates = append(candidates, path)
		}
		if path, err := exec.LookPath(binName + ".cmd"); err == nil {
			candidates = append(candidates, path)
		}
	}

	var newestNativePath string
	var newestNativeTime time.Time
	var fallbackPath string
	var fallbackTime time.Time
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

		isWindowsInterop := runtime.GOOS != "windows" && (strings.HasPrefix(cleaned, "/mnt/") || strings.HasSuffix(strings.ToLower(cleaned), ".exe") || strings.HasSuffix(strings.ToLower(cleaned), ".cmd"))

		if isWindowsInterop {
			if fallbackPath == "" || fi.ModTime().After(fallbackTime) {
				fallbackPath = cleaned
				fallbackTime = fi.ModTime()
			}
		} else {
			if newestNativePath == "" || fi.ModTime().After(newestNativeTime) {
				newestNativePath = cleaned
				newestNativeTime = fi.ModTime()
			}
		}
	}

	if newestNativePath != "" {
		return newestNativePath, nil
	}
	if fallbackPath != "" {
		return fallbackPath, nil
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

// RunAWS handles AWS CLI execution, subcommands, and cheat sheet diagnostics
func RunAWS(args []string) error {
	binPath, _ := FindBinary("aws")

	// If no arguments provided, or sheet/cockpit requested, show the rich cheat sheet!
	if len(args) == 0 || args[0] == "sheet" || args[0] == "cheatsheet" || args[0] == "status" || args[0] == "diag" || args[0] == "diagnostics" || args[0] == "cockpit" || args[0] == "help" || args[0] == "-h" || args[0] == "--help" {
		pauseOnExit := len(args) > 0 && args[0] == "cockpit"
		return runAwsCheatSheet(binPath, pauseOnExit)
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
		return runAwsCheatSheet("", false)
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

func runAwsCheatSheet(binPath string, pauseOnExit bool) error {
	fmt.Println("\r\n☁️  \033[1;36mAWS Cloud & LocalStack Developer Cheat Sheet\033[0m")
	fmt.Println("──────────────────────────────────────────────────────────────────────────────────")

	// 1. Live Telemetry
	fmt.Print("  • AWS Identity:     ")
	if binPath != "" {
		ctx, cancel := context.WithTimeout(context.Background(), 1200*time.Millisecond)
		defer cancel()
		stsCmd := exec.CommandContext(ctx, binPath, "sts", "get-caller-identity", "--output", "text")
		if stsOut, err := stsCmd.CombinedOutput(); err == nil {
			parts := strings.Fields(strings.TrimSpace(string(stsOut)))
			if len(parts) >= 2 {
				fmt.Printf("\033[1;32mConfigured\033[0m (Account: \033[33m%s\033[0m, Arn: \033[90m%s\033[0m)\r\n", parts[0], parts[1])
			} else {
				fmt.Printf("\033[1;32mConfigured\033[0m (%s)\r\n", strings.TrimSpace(string(stsOut)))
			}
		} else {
			fmt.Println("\033[1;33mNo active credentials (or offline)\033[0m")
		}
	} else {
		fmt.Println("\033[90mAWS CLI not found in PATH\033[0m")
	}

	fmt.Print("  • LocalStack (:4566): ")
	client := http.Client{Timeout: 600 * time.Millisecond}
	resp, err := client.Get("http://localhost:4566/_localstack/health")
	if err == nil {
		defer resp.Body.Close()
		fmt.Printf("\033[1;32mONLINE (HTTP %s)\033[0m\r\n", resp.Status)
	} else {
		fmt.Println("\033[90mOFFLINE (Run 'docker run -d -p 4566:4566 localstack/localstack')\033[0m")
	}

	fmt.Println("──────────────────────────────────────────────────────────────────────────────────")
	fmt.Println("  \033[1;33m1. 🔐 AUTH, SSO & PROFILES:\033[0m")
	fmt.Println("     aws sts get-caller-identity                           \033[90m# Check active IAM caller identity\033[0m")
	fmt.Println("     aws configure list                                    \033[90m# View active profile, region & keys\033[0m")
	fmt.Println("     aws sso login --profile <profile>                     \033[90m# Single Sign-On browser login\033[0m")
	fmt.Println("     export AWS_PROFILE=<profile>                          \033[90m# Switch profile in current shell\033[0m")
	fmt.Println("")
	fmt.Println("  \033[1;33m2. 🧪 LOCALSTACK (PORT 4566 - ZERO CLOUD COST):\033[0m")
	fmt.Println("     docker run -d --name localstack -p 4566:4566 localstack/localstack")
	fmt.Println("     alias awslocal=\"aws --endpoint-url=http://localhost:4566\"")
	fmt.Println("     awslocal s3 mb s3://test-bucket                       \033[90m# Create mock S3 bucket\033[0m")
	fmt.Println("     awslocal sqs create-queue --queue-name test-queue     \033[90m# Create mock SQS queue\033[0m")
	fmt.Println("")
	fmt.Println("  \033[1;33m3. 🪣 S3 BUCKET & FILE OPERATIONS:\033[0m")
	fmt.Println("     aws s3 ls                                             \033[90m# List all S3 buckets\033[0m")
	fmt.Println("     aws s3 sync ./dist s3://my-bucket/ --delete           \033[90m# Sync build & delete missing\033[0m")
	fmt.Println("     aws s3 presign s3://bucket/file.zip --expires-in 3600 \033[90m# Generate 1-hr secure link\033[0m")
	fmt.Println("     aws s3 rb s3://my-bucket --force                      \033[90m# Force-delete bucket + files\033[0m")
	fmt.Println("")
	fmt.Println("  \033[1;33m4. 📨 SQS & SNS MESSAGING:\033[0m")
	fmt.Println("     aws sqs list-queues                                   \033[90m# List active SQS queues\033[0m")
	fmt.Println("     aws sqs send-message --queue-url <url> --message-body '{\"event\":\"test\"}'")
	fmt.Println("     aws sqs receive-message --queue-url <url> --max-number-of-messages 10")
	fmt.Println("     aws sqs purge-queue --queue-url <url>                 \033[90m# Purge all messages in queue\033[0m")
	fmt.Println("")
	fmt.Println("  \033[1;33m5. ⚡ DYNAMODB & LAMBDA LOGS:\033[0m")
	fmt.Println("     aws dynamodb list-tables                              \033[90m# List DynamoDB tables\033[0m")
	fmt.Println("     aws dynamodb scan --table-name Users --max-items 5    \033[90m# Quick sample table scan\033[0m")
	fmt.Println("     aws logs tail /aws/lambda/<name> --follow             \033[90m# Live tail logs (like tail -f)\033[0m")
	fmt.Println("──────────────────────────────────────────────────────────────────────────────────")
	fmt.Println("  \033[1mQuick AGYX Shortcuts:\033[0m")
	fmt.Println("    • agyx aws whoami    • agyx aws s3    • agyx aws sqs    • agyx aws local")
	fmt.Println("──────────────────────────────────────────────────────────────────────────────────")

	if pauseOnExit {
		fmt.Print("\r\n\033[1;32mPress [Enter] to return to AGYX Cockpit...\033[0m")
		var buf [1]byte
		_, _ = os.Stdin.Read(buf[:])
	}
	return nil
}
