package ollamaops

import (
	"bufio"
	"bytes"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"math"
	"net"
	"net/http"
	"net/url"
	"os"
	"os/exec"
	"path/filepath"
	"strconv"
	"strings"
	"time"

	"agyollama/internal/model"
)

const (
	DefaultHost         = "127.0.0.1"
	DefaultPort         = 11434
	DefaultModelName    = "qwen2.5-coder:7b"
	DefaultTestPrompt   = "Explain gravity in 5 words."
)

// Client interacts with the Ollama HTTP API and local process.
type Client struct {
	BaseURL        string
	Host           string
	Port           int
	HTTPClient     *http.Client
	ConfigFilePath string
	LogFilePath    string
	OllamaCmd      string
}

// ClientOption configures a Client.
type ClientOption func(*Client)

// WithBaseURL overrides the target Ollama HTTP base URL.
func WithBaseURL(rawURL string) ClientOption {
	return func(c *Client) {
		c.setBaseURL(rawURL)
	}
}

// WithConfigFile overrides the default model configuration file path.
func WithConfigFile(path string) ClientOption {
	return func(c *Client) {
		c.ConfigFilePath = path
	}
}

// WithLogFile overrides the Ollama log file path.
func WithLogFile(path string) ClientOption {
	return func(c *Client) {
		c.LogFilePath = path
	}
}

// WithHTTPClient overrides the HTTP client.
func WithHTTPClient(client *http.Client) ClientOption {
	return func(c *Client) {
		c.HTTPClient = client
	}
}

// resolveOllamaCmd detects the Ollama binary across Linux, WSL2, and Windows paths.
func resolveOllamaCmd() string {
	if env := strings.TrimSpace(os.Getenv("OLLAMA_BIN")); env != "" {
		return env
	}
	if env := strings.TrimSpace(os.Getenv("OLLAMA_CMD")); env != "" {
		return env
	}
	if p, err := exec.LookPath("ollama"); err == nil {
		return p
	}
	if p, err := exec.LookPath("ollama.exe"); err == nil {
		return p
	}

	var candidates []string
	home, _ := os.UserHomeDir()
	if home != "" {
		candidates = append(candidates,
			filepath.Join(home, ".local", "bin", "ollama"),
			filepath.Join(home, "bin", "ollama"),
		)
	}

	candidates = append(candidates,
		"/usr/local/bin/ollama",
		"/usr/bin/ollama",
		"/bin/ollama",
	)

	// WSL2 paths to Windows host
	wslMatches, _ := filepath.Glob("/mnt/c/Users/*/AppData/Local/Programs/Ollama/ollama.exe")
	candidates = append(candidates, wslMatches...)
	candidates = append(candidates,
		"/mnt/c/Program Files/Ollama/ollama.exe",
		"/mnt/c/Users/TruongNhon/AppData/Local/Programs/Ollama/ollama.exe",
	)

	for _, cand := range candidates {
		if fi, err := os.Stat(cand); err == nil && !fi.IsDir() {
			return cand
		}
	}

	return "ollama"
}

// NewClient initializes an Ollama client inspecting $OLLAMA_HOST and defaults.
func NewClient(opts ...ClientOption) *Client {
	c := &Client{
		HTTPClient: &http.Client{
			Timeout: 60 * time.Second,
		},
		OllamaCmd: resolveOllamaCmd(),
	}

	rawHost := os.Getenv("OLLAMA_HOST")
	if strings.TrimSpace(rawHost) == "" {
		rawHost = fmt.Sprintf("http://%s:%d", DefaultHost, DefaultPort)
	}
	c.setBaseURL(rawHost)

	// Default config file location: ~/.config/antigravity/ollama_default_model.txt
	home, err := os.UserHomeDir()
	if err == nil {
		c.ConfigFilePath = filepath.Join(home, ".config", "antigravity", "ollama_default_model.txt")
	}

	for _, opt := range opts {
		opt(c)
	}

	return c
}

// NewClientWithURL is a constructor helper for testing.
func NewClientWithURL(baseURL string) *Client {
	return NewClient(WithBaseURL(baseURL))
}

func (c *Client) setBaseURL(raw string) {
	raw = strings.TrimSpace(raw)
	if !strings.HasPrefix(raw, "http://") && !strings.HasPrefix(raw, "https://") {
		// e.g. "127.0.0.1:11434" or ":11434"
		if strings.HasPrefix(raw, ":") {
			raw = "127.0.0.1" + raw
		}
		raw = "http://" + raw
	}
	c.BaseURL = strings.TrimRight(raw, "/")

	parsed, err := url.Parse(c.BaseURL)
	if err == nil {
		h, p, errSplit := net.SplitHostPort(parsed.Host)
		if errSplit == nil {
			c.Host = h
			portInt, _ := strconv.Atoi(p)
			c.Port = portInt
		} else {
			c.Host = parsed.Hostname()
			if parsed.Scheme == "https" {
				c.Port = 443
			} else {
				c.Port = DefaultPort
			}
		}
	} else {
		c.Host = DefaultHost
		c.Port = DefaultPort
	}
}

// IsRunning performs a fast health check on port 11434 or GET /api/version (500ms timeout).
func (c *Client) IsRunning() bool {
	checkClient := &http.Client{
		Timeout: 500 * time.Millisecond,
	}
	resp, err := checkClient.Get(c.BaseURL + "/api/version")
	if err == nil {
		_ = resp.Body.Close()
		if resp.StatusCode == http.StatusOK {
			return true
		}
	}

	// Fallback to TCP dial check
	targetAddr := net.JoinHostPort(c.Host, strconv.Itoa(c.Port))
	conn, err := net.DialTimeout("tcp", targetAddr, 500*time.Millisecond)
	if err == nil {
		_ = conn.Close()
		return true
	}

	return false
}

// StartDaemon starts `ollama serve` in the background if offline.
func (c *Client) StartDaemon() error {
	if c.IsRunning() {
		return nil
	}

	cmd := exec.Command(c.OllamaCmd, "serve")
	cmd.Stdin = nil
	// Decouple process group on Unix so child survives parent
	setDaemonSysProcAttr(cmd)

	if err := cmd.Start(); err != nil {
		return fmt.Errorf("failed to start %s serve: %w", c.OllamaCmd, err)
	}

	// Poll IsRunning up to 12 iterations (3.6s total)
	for i := 0; i < 12; i++ {
		time.Sleep(300 * time.Millisecond)
		if c.IsRunning() {
			return nil
		}
	}

	return errors.New("daemon process started but health check timed out")
}

// StopDaemon terminates any running Ollama daemon processes (Linux and Windows host via WSL).
func (c *Client) StopDaemon() error {
	if !c.IsRunning() {
		return nil
	}

	// 1. Terminate local Linux ollama serve processes
	_ = exec.Command("pkill", "-f", "ollama serve").Run()
	_ = exec.Command("pkill", "-f", "ollama").Run()

	// 2. If running under WSL, also try terminating Windows host ollama.exe
	if _, err := os.Stat("/mnt/c/Windows/System32/taskkill.exe"); err == nil {
		_ = exec.Command("/mnt/c/Windows/System32/taskkill.exe", "/F", "/IM", "ollama.exe").Run()
	}

	// 3. Poll up to 10 iterations (2.0s total) to verify offline
	for i := 0; i < 10; i++ {
		time.Sleep(200 * time.Millisecond)
		if !c.IsRunning() {
			return nil
		}
	}

	return errors.New("daemon process could not be terminated")
}

// ListModels queries GET /api/tags and parses the installed models list.
func (c *Client) ListModels() ([]model.ModelInfo, error) {
	resp, err := c.HTTPClient.Get(c.BaseURL + "/api/tags")
	if err != nil {
		return nil, fmt.Errorf("GET /api/tags failed: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return nil, fmt.Errorf("GET /api/tags returned status %d: %s", resp.StatusCode, strings.TrimSpace(string(body)))
	}

	var tagsResp struct {
		Models []struct {
			Name       string    `json:"name"`
			Model      string    `json:"model"`
			ModifiedAt time.Time `json:"modified_at"`
			Size       int64     `json:"size"`
			Digest     string    `json:"digest"`
			Details    struct {
				Format            string `json:"format"`
				Family            string `json:"family"`
				ParameterSize     string `json:"parameter_size"`
				QuantizationLevel string `json:"quantization_level"`
			} `json:"details"`
		} `json:"models"`
	}

	if err := json.NewDecoder(resp.Body).Decode(&tagsResp); err != nil {
		return nil, fmt.Errorf("failed to parse /api/tags response: %w", err)
	}

	var result []model.ModelInfo
	for _, m := range tagsResp.Models {
		sizeGB := math.Round((float64(m.Size)/(1024*1024*1024))*100) / 100
		result = append(result, model.ModelInfo{
			Name:              m.Name,
			SizeBytes:         m.Size,
			SizeGB:            sizeGB,
			ModifiedAt:        m.ModifiedAt,
			Family:            m.Details.Family,
			ParameterSize:     m.Details.ParameterSize,
			QuantizationLevel: m.Details.QuantizationLevel,
		})
	}

	return result, nil
}

// ShowModel queries POST /api/show for model details.
func (c *Client) ShowModel(name string) (*model.ModelDetail, error) {
	payload, _ := json.Marshal(map[string]string{"name": name})
	resp, err := c.HTTPClient.Post(c.BaseURL+"/api/show", "application/json", bytes.NewReader(payload))
	if err != nil {
		return nil, fmt.Errorf("POST /api/show failed: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return nil, fmt.Errorf("POST /api/show returned status %d: %s", resp.StatusCode, strings.TrimSpace(string(body)))
	}

	var detailResp struct {
		Modelfile  string                 `json:"modelfile"`
		Parameters string                 `json:"parameters"`
		Template   string                 `json:"template"`
		System     string                 `json:"system"`
		Details    map[string]interface{} `json:"details"`
	}

	if err := json.NewDecoder(resp.Body).Decode(&detailResp); err != nil {
		return nil, fmt.Errorf("failed to parse /api/show response: %w", err)
	}

	return &model.ModelDetail{
		Modelfile:  detailResp.Modelfile,
		Parameters: detailResp.Parameters,
		Template:   detailResp.Template,
		System:     detailResp.System,
		Details:    detailResp.Details,
	}, nil
}

// DeleteModel sends DELETE /api/delete to delete a model.
func (c *Client) DeleteModel(name string) error {
	payload, _ := json.Marshal(map[string]string{"name": name})
	req, err := http.NewRequest(http.MethodDelete, c.BaseURL+"/api/delete", bytes.NewReader(payload))
	if err != nil {
		return err
	}
	req.Header.Set("Content-Type", "application/json")

	resp, err := c.HTTPClient.Do(req)
	if err != nil {
		// Fallback to CLI command if HTTP API is unreachable
		cmd := exec.Command(c.OllamaCmd, "rm", name)
		if cliErr := cmd.Run(); cliErr == nil {
			return nil
		}
		return fmt.Errorf("DELETE /api/delete failed: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusNoContent {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("delete model failed (HTTP %d): %s", resp.StatusCode, strings.TrimSpace(string(body)))
	}

	return nil
}

// PullModel sends POST /api/pull and streams JSON progress lines.
func (c *Client) PullModel(name string, progressCb func(model.PullProgress)) error {
	payload, _ := json.Marshal(map[string]interface{}{
		"name":   name,
		"stream": true,
	})

	resp, err := c.HTTPClient.Post(c.BaseURL+"/api/pull", "application/json", bytes.NewReader(payload))
	if err != nil {
		return fmt.Errorf("POST /api/pull failed: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("pull model failed (HTTP %d): %s", resp.StatusCode, strings.TrimSpace(string(body)))
	}

	scanner := bufio.NewScanner(resp.Body)
	for scanner.Scan() {
		line := scanner.Bytes()
		if len(bytes.TrimSpace(line)) == 0 {
			continue
		}

		var chunk struct {
			Status    string `json:"status"`
			Digest    string `json:"digest"`
			Total     int64  `json:"total"`
			Completed int64  `json:"completed"`
			Error     string `json:"error"`
		}

		if err := json.Unmarshal(line, &chunk); err != nil {
			continue
		}

		if chunk.Error != "" {
			return errors.New(chunk.Error)
		}

		var percent float64
		if chunk.Total > 0 {
			percent = math.Round((float64(chunk.Completed)/float64(chunk.Total)*100)*10) / 10
		}

		if progressCb != nil {
			progressCb(model.PullProgress{
				Status:         chunk.Status,
				Digest:         chunk.Digest,
				TotalBytes:     chunk.Total,
				CompletedBytes: chunk.Completed,
				Percent:        percent,
			})
		}
	}

	return scanner.Err()
}

// BenchmarkModel sends test prompt ("Explain gravity in 5 words.") to POST /api/generate
// and measures prompt evaluation latency and tokens/sec.
func (c *Client) BenchmarkModel(name string) (*model.BenchmarkResult, error) {
	res := &model.BenchmarkResult{
		ModelName: name,
	}

	// Try to get model size
	models, err := c.ListModels()
	if err == nil {
		for _, m := range models {
			if m.Name == name {
				res.SizeGB = m.SizeGB
				break
			}
		}
	}

	reqBody, _ := json.Marshal(map[string]interface{}{
		"model":  name,
		"prompt": DefaultTestPrompt,
		"stream": false,
	})

	startTime := time.Now()
	resp, err := c.HTTPClient.Post(c.BaseURL+"/api/generate", "application/json", bytes.NewReader(reqBody))
	elapsedWall := time.Since(startTime).Seconds()

	if err != nil {
		res.Success = false
		res.ErrorMsg = err.Error()
		return res, fmt.Errorf("POST /api/generate failed: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		res.Success = false
		res.ErrorMsg = fmt.Sprintf("HTTP %d: %s", resp.StatusCode, strings.TrimSpace(string(body)))
		return res, fmt.Errorf("benchmark failed: %s", res.ErrorMsg)
	}

	var genResp struct {
		Model              string `json:"model"`
		Response           string `json:"response"`
		TotalDuration      int64  `json:"total_duration"`      // nanoseconds
		PromptEvalDuration int64  `json:"prompt_eval_duration"` // nanoseconds
		EvalCount          int    `json:"eval_count"`
		EvalDuration       int64  `json:"eval_duration"`       // nanoseconds
		Error              string `json:"error,omitempty"`
	}

	if err := json.NewDecoder(resp.Body).Decode(&genResp); err != nil {
		res.Success = false
		res.ErrorMsg = err.Error()
		return res, fmt.Errorf("failed to parse /api/generate response: %w", err)
	}

	if genResp.Error != "" {
		res.Success = false
		res.ErrorMsg = genResp.Error
		return res, errors.New(genResp.Error)
	}

	// Latency
	if genResp.TotalDuration > 0 {
		res.LatencySec = math.Round((float64(genResp.TotalDuration)/1e9)*100) / 100
	} else {
		res.LatencySec = math.Round(elapsedWall*100) / 100
	}

	// Prompt Eval
	if genResp.PromptEvalDuration > 0 {
		res.PromptEvalSec = math.Round((float64(genResp.PromptEvalDuration)/1e9)*100) / 100
	}

	// Tokens per second
	if genResp.EvalDuration > 0 && genResp.EvalCount > 0 {
		sec := float64(genResp.EvalDuration) / 1e9
		res.TokensPerSec = math.Round((float64(genResp.EvalCount)/sec)*10) / 10
	}

	res.Success = true
	return res, nil
}

// GetDefaultModel returns the active default model from ~/.config/antigravity/ollama_default_model.txt.
func (c *Client) GetDefaultModel() string {
	if c.ConfigFilePath != "" {
		data, err := os.ReadFile(c.ConfigFilePath)
		if err == nil {
			s := strings.TrimSpace(string(data))
			if s != "" {
				return s
			}
		}
	}
	return DefaultModelName
}

// SetDefaultModel saves the default model to ~/.config/antigravity/ollama_default_model.txt.
func (c *Client) SetDefaultModel(name string) error {
	name = strings.TrimSpace(name)
	if name == "" {
		return errors.New("model name cannot be empty")
	}

	if c.ConfigFilePath == "" {
		home, err := os.UserHomeDir()
		if err != nil {
			return fmt.Errorf("unable to resolve user home: %w", err)
		}
		c.ConfigFilePath = filepath.Join(home, ".config", "antigravity", "ollama_default_model.txt")
	}

	dir := filepath.Dir(c.ConfigFilePath)
	if err := os.MkdirAll(dir, 0755); err != nil {
		return fmt.Errorf("failed to create directory %s: %w", dir, err)
	}

	if err := os.WriteFile(c.ConfigFilePath, []byte(name+"\n"), 0644); err != nil {
		return fmt.Errorf("failed to write config file %s: %w", c.ConfigFilePath, err)
	}

	return nil
}

// GetServerLogs attempts to find and return the last N lines of Ollama server logs.
func (c *Client) GetServerLogs(lines int) (string, error) {
	if lines <= 0 {
		lines = 50
	}

	candidatePaths := []string{}
	if c.LogFilePath != "" {
		candidatePaths = append(candidatePaths, c.LogFilePath)
	}
	if envLog := os.Getenv("OLLAMA_LOG_PATH"); envLog != "" {
		candidatePaths = append(candidatePaths, envLog)
	}

	home, _ := os.UserHomeDir()
	if home != "" {
		candidatePaths = append(candidatePaths,
			filepath.Join(home, ".ollama", "logs", "server.log"),
			filepath.Join(home, ".ollama", "server.log"),
			filepath.Join(home, ".local", "share", "Ollama", "server.log"),
		)
	}
	candidatePaths = append(candidatePaths,
		"/var/log/ollama/server.log",
		"/var/log/ollama.log",
		"/tmp/ollama.log",
	)

	var foundPath string
	for _, p := range candidatePaths {
		if _, err := os.Stat(p); err == nil {
			foundPath = p
			break
		}
	}

	if foundPath == "" {
		return fmt.Sprintf("⚠️ Ollama log file not found in standard paths:\n  %s\n\nIf running Ollama via systemd service, run:\n  journalctl -u ollama -n %d --no-pager",
			strings.Join(candidatePaths, "\n  "), lines), nil
	}

	content, err := os.ReadFile(foundPath)
	if err != nil {
		return "", fmt.Errorf("failed to read log file %s: %w", foundPath, err)
	}

	allLines := strings.Split(string(content), "\n")
	if len(allLines) > 0 && allLines[len(allLines)-1] == "" {
		allLines = allLines[:len(allLines)-1]
	}

	if len(allLines) > lines {
		allLines = allLines[len(allLines)-lines:]
	}

	return fmt.Sprintf("Log Path: %s\n\n%s", foundPath, strings.Join(allLines, "\n")), nil
}

// RunInteractive launches `ollama run <model>` attached directly to stdin/stdout/stderr.
func (c *Client) RunInteractive(modelName string) error {
	if strings.TrimSpace(modelName) == "" {
		modelName = c.GetDefaultModel()
	}

	cmd := exec.Command(c.OllamaCmd, "run", modelName)
	cmd.Stdin = os.Stdin
	cmd.Stdout = os.Stdout
	cmd.Stderr = os.Stderr
	return cmd.Run()
}

// GetDaemonStatus collects status metrics for status reporting.
func (c *Client) GetDaemonStatus() (*model.DaemonStatus, error) {
	status := &model.DaemonStatus{
		Host:        c.Host,
		Port:        c.Port,
		ActiveModel: c.GetDefaultModel(),
	}

	totalRAM, availRAM := readHostRAM()
	status.TotalHostRAM = totalRAM
	status.AvailableHostRAM = availRAM

	if !c.IsRunning() {
		status.IsOnline = false
		status.Version = "offline"
		status.VRAMAllocated = "0.0 GB (Offline)"
		return status, nil
	}

	status.IsOnline = true

	// Query /api/version
	verResp, err := c.HTTPClient.Get(c.BaseURL + "/api/version")
	if err == nil {
		defer verResp.Body.Close()
		var v struct {
			Version string `json:"version"`
		}
		if json.NewDecoder(verResp.Body).Decode(&v) == nil && v.Version != "" {
			status.Version = v.Version
		}
	}
	if status.Version == "" {
		status.Version = "active"
	}

	// Query /api/ps for running models & VRAM
	status.VRAMAllocated = "0.0 GB (Idle)"
	psResp, err := c.HTTPClient.Get(c.BaseURL + "/api/ps")
	if err == nil {
		defer psResp.Body.Close()
		var psData struct {
			Models []struct {
				Name     string `json:"name"`
				Model    string `json:"model"`
				SizeVRAM int64  `json:"size_vram"`
			} `json:"models"`
		}
		if json.NewDecoder(psResp.Body).Decode(&psData) == nil && len(psData.Models) > 0 {
			m0 := psData.Models[0]
			if m0.Name != "" {
				status.ActiveModel = m0.Name
			}
			if m0.SizeVRAM > 0 {
				vramGB := float64(m0.SizeVRAM) / (1024 * 1024 * 1024)
				status.VRAMAllocated = fmt.Sprintf("%.1f GB allocated", vramGB)
			}
		}
	}

	return status, nil
}

func readHostRAM() (total uint64, available uint64) {
	data, err := os.ReadFile("/proc/meminfo")
	if err != nil {
		return 0, 0
	}
	lines := strings.Split(string(data), "\n")
	for _, line := range lines {
		fields := strings.Fields(line)
		if len(fields) < 2 {
			continue
		}
		switch fields[0] {
		case "MemTotal:":
			kb, _ := strconv.ParseUint(fields[1], 10, 64)
			total = kb * 1024
		case "MemAvailable:":
			kb, _ := strconv.ParseUint(fields[1], 10, 64)
			available = kb * 1024
		}
	}
	return total, available
}

// DefaultClient is the package-level default client instance.
var DefaultClient = NewClient()

// Package-level convenience wrappers delegation:
func IsRunning() bool                                    { return DefaultClient.IsRunning() }
func StartDaemon() error                                 { return DefaultClient.StartDaemon() }
func StopDaemon() error                                  { return DefaultClient.StopDaemon() }
func ListModels() ([]model.ModelInfo, error)             { return DefaultClient.ListModels() }
func ShowModel(name string) (*model.ModelDetail, error)  { return DefaultClient.ShowModel(name) }
func DeleteModel(name string) error                      { return DefaultClient.DeleteModel(name) }
func PullModel(name string, cb func(model.PullProgress)) error {
	return DefaultClient.PullModel(name, cb)
}
func BenchmarkModel(name string) (*model.BenchmarkResult, error) {
	return DefaultClient.BenchmarkModel(name)
}
func GetDefaultModel() string                   { return DefaultClient.GetDefaultModel() }
func SetDefaultModel(name string) error         { return DefaultClient.SetDefaultModel(name) }
func GetServerLogs(lines int) (string, error)   { return DefaultClient.GetServerLogs(lines) }
func RunInteractive(model string) error         { return DefaultClient.RunInteractive(model) }
func GetDaemonStatus() (*model.DaemonStatus, error) {
	return DefaultClient.GetDaemonStatus()
}
