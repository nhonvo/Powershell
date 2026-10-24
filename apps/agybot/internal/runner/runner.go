package runner

import (
	"bufio"
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
	"strings"
	"time"

	"agybot/internal/security"
)

type AgentEvent struct {
	Type           string                 `json:"type"` // "init", "status", "tool_start", "tool_done", "content", "done", "error"
	Content        string                 `json:"content"`
	ConversationID string                 `json:"conversation_id,omitempty"`
	ToolName       string                 `json:"tool_name,omitempty"`
	Raw            map[string]interface{} `json:"raw,omitempty"`
}

type RunnerOptions struct {
	AgyPath        string
	WorkspaceDir   string
	Prompt         string
	ConversationID string
	Model          string
	Effort         string
	Mode           string
	Timeout        time.Duration
}

type AntigravityRunner struct {
	guard *security.SecurityGuard
}

func NewAntigravityRunner(guard *security.SecurityGuard) *AntigravityRunner {
	if guard == nil {
		guard = security.NewSecurityGuard()
	}
	return &AntigravityRunner{guard: guard}
}

// AvailableModels returns standard Google Antigravity and local models
func (r *AntigravityRunner) AvailableModels() []string {
	return []string{
		"Gemini 3.8 Flash (Low)",
		"Gemini 3.8 Flash (Medium)",
		"Gemini 3.8 Flash (High)",
		"Claude Sonnet 4.6 (Thinking)",
		"Claude Opus 4.6 (Thinking)",
		"GPT-OSS 120B (Medium)",
		"Ollama Local",
	}
}

// NormalizeCLIModel maps human, legacy, or partial model names to canonical AGY CLI models.
// Effort is resolved directly into the canonical model identifier (e.g. Low/Medium/High).
func NormalizeCLIModel(model, effort string) string {
	m := strings.ToLower(strings.TrimSpace(model))
	eff := strings.ToLower(strings.TrimSpace(effort))

	canonical := []string{
		"Gemini 3.8 Flash (Low)",
		"Gemini 3.8 Flash (Medium)",
		"Gemini 3.8 Flash (High)",
		"Claude Sonnet 4.6 (Thinking)",
		"Claude Opus 4.6 (Thinking)",
		"GPT-OSS 120B (Medium)",
	}
	for _, c := range canonical {
		if strings.EqualFold(model, c) {
			return c
		}
	}

	if strings.Contains(m, "opus") {
		return "Claude Opus 4.6 (Thinking)"
	}
	if strings.Contains(m, "sonnet") || strings.Contains(m, "claude") {
		return "Claude Sonnet 4.6 (Thinking)"
	}
	if strings.Contains(m, "gpt") || strings.Contains(m, "oss") {
		return "GPT-OSS 120B (Medium)"
	}

	// Default family: Gemini Flash
	switch eff {
	case "high", "max", "xhigh":
		return "Gemini 3.8 Flash (High)"
	case "medium", "med":
		return "Gemini 3.8 Flash (Medium)"
	default:
		return "Gemini 3.8 Flash (Low)"
	}
}

// FormatToolNotification creates clean, emoji-rich status line for chat interfaces
func (r *AntigravityRunner) FormatToolNotification(toolName string, params map[string]interface{}) string {
	switch toolName {
	case "run_command":
		if cmd, ok := params["CommandLine"].(string); ok {
			if len(cmd) > 60 {
				cmd = cmd[:60] + "..."
			}
			return fmt.Sprintf("⚡ **Running command:** `%s`", cmd)
		}
		return "⚡ **Running terminal command...**"
	case "write_to_file":
		if tgt, ok := params["TargetFile"].(string); ok {
			return fmt.Sprintf("📝 **Creating file:** `%s`", filepath.Base(tgt))
		}
		return "📝 **Writing file...**"
	case "replace_file_content", "multi_replace_file_content":
		if tgt, ok := params["TargetFile"].(string); ok {
			return fmt.Sprintf("✏️ **Editing file:** `%s`", filepath.Base(tgt))
		}
		return "✏️ **Patching file...**"
	case "view_file", "list_dir", "find_by_name", "grep_search":
		return fmt.Sprintf("🔍 **Inspecting codebase:** `%s`", toolName)
	case "search_web", "read_url_content":
		return fmt.Sprintf("🌐 **Web research:** `%s`", toolName)
	case "generate_image":
		return "🎨 **Generating AI Image artifact...**"
	case "ask_question":
		return "❓ **Antigravity asking interactive question...**"
	default:
		return fmt.Sprintf("⚙️ **Executing tool:** `%s`", toolName)
	}
}

// ExecutePrompt runs agy CLI or Ollama Local with streaming output and pushes events to output channel
func (r *AntigravityRunner) ExecutePrompt(ctx context.Context, opts RunnerOptions) (<-chan AgentEvent, error) {
	outChan := make(chan AgentEvent, 100)

	// Check command safety firewall
	if blocked, cat, desc := r.guard.CheckCommand(opts.Prompt); blocked {
		go func() {
			outChan <- AgentEvent{
				Type:    "error",
				Content: fmt.Sprintf("⛔ Blocked by Safety Firewall [%s]: %s", cat, desc),
			}
			close(outChan)
		}()
		return outChan, nil
	}

	// Check if this is an Ollama Local request
	if strings.EqualFold(opts.Model, "Ollama Local") || strings.HasPrefix(strings.ToLower(opts.Model), "ollama") {
		go r.executeOllama(ctx, opts, outChan)
		return outChan, nil
	}

	agyExec := opts.AgyPath
	if agyExec == "" {
		agyExec = "agy"
	}

	// Prepare safe prompt
	effectivePrompt := opts.Prompt
	if opts.ConversationID == "" {
		effectivePrompt = r.guard.GetSafetyPromptPrefix(opts.WorkspaceDir) + opts.Prompt
	}

	effectiveModel := NormalizeCLIModel(opts.Model, opts.Effort)

	args := []string{
		"-p", effectivePrompt,
		"--output-format", "stream-json",
		"--dangerously-skip-permissions",
		"--model", effectiveModel,
	}

	if opts.ConversationID != "" {
		args = append(args, "--conversation", opts.ConversationID)
	}
	// Note: We do NOT append --effort because canonical AGY models embed their effort/thinking level
	// and passing --effort causes the CLI to error out with "error: --effort is not supported for model ..."

	cmd := exec.CommandContext(ctx, agyExec, args...)
	if opts.WorkspaceDir != "" {
		cmd.Dir = opts.WorkspaceDir
	}

	var errBuf bytes.Buffer
	cmd.Stderr = &errBuf

	stdout, err := cmd.StdoutPipe()
	if err != nil {
		close(outChan)
		return nil, fmt.Errorf("failed to open stdout pipe: %w", err)
	}

	if err := cmd.Start(); err != nil {
		close(outChan)
		return nil, fmt.Errorf("failed to start agy process: %w", err)
	}

	pid := cmd.Process.Pid
	go func() {
		<-ctx.Done()
		if ctx.Err() != nil && cmd.Process != nil {
			if runtime.GOOS == "windows" {
				_ = exec.Command("taskkill", "/F", "/T", "/PID", fmt.Sprintf("%d", pid)).Run()
			} else {
				_ = cmd.Process.Kill()
			}
		}
	}()

	go func() {
		defer close(outChan)
		scanner := bufio.NewScanner(stdout)
		// Support larger buffer for detailed JSON payloads
		buf := make([]byte, 1024*1024)
		scanner.Buffer(buf, 10*1024*1024)

		for scanner.Scan() {
			line := strings.TrimSpace(scanner.Text())
			if line == "" {
				continue
			}

			var payload map[string]interface{}
			if err := json.Unmarshal([]byte(line), &payload); err != nil {
				outChan <- AgentEvent{
					Type:    "content",
					Content: line + "\n",
				}
				continue
			}

			event, _ := payload["event"].(string)
			switch event {
			case "init":
				convID, _ := payload["conversation_id"].(string)
				outChan <- AgentEvent{
					Type:           "init",
					ConversationID: convID,
					Raw:            payload,
				}
			case "step_update":
				if step, ok := payload["step_update"].(map[string]interface{}); ok {
					stepType, _ := step["step_type"].(string)
					state, _ := step["state"].(string)
					toolName, _ := step["tool_name"].(string)

					if stepType == "tool" {
						params := make(map[string]interface{})
						if toolInfo, ok := step["tool_info"].(map[string]interface{}); ok {
							if p, ok := toolInfo["parameters"].(map[string]interface{}); ok {
								params = p
							}
						}

						if state == "ACTIVE" {
							statusMsg := r.FormatToolNotification(toolName, params)
							outChan <- AgentEvent{
								Type:     "tool_start",
								Content:  statusMsg,
								ToolName: toolName,
								Raw:      payload,
							}
						} else if state == "DONE" {
							outChan <- AgentEvent{
								Type:     "tool_done",
								ToolName: toolName,
								Raw:      payload,
							}
						}
					} else if stepType == "agent_response" {
						if textDelta, ok := step["text_delta"].(string); ok && textDelta != "" {
							outChan <- AgentEvent{
								Type:    "content",
								Content: textDelta,
								Raw:     payload,
							}
						}
						if thinkText, ok := step["thinking"].(string); ok && thinkText != "" {
							outChan <- AgentEvent{
								Type:    "thinking",
								Content: thinkText,
								Raw:     payload,
							}
						}
					}
				}
			case "result":
				if res, ok := payload["result"].(map[string]interface{}); ok {
					respText, _ := res["response"].(string)
					status, _ := res["status"].(string)
					if respText != "" {
						outChan <- AgentEvent{
							Type:    "result",
							Content: respText,
							Raw:     payload,
						}
					}
					if strings.EqualFold(status, "ERROR") {
						errMsg := "execution error from agy"
						if errObj, ok := res["error"].(map[string]interface{}); ok {
							if msg, ok := errObj["message"].(string); ok && msg != "" {
								errMsg = msg
							}
						}
						outChan <- AgentEvent{
							Type:    "error",
							Content: errMsg,
							Raw:     payload,
						}
					}
				}
				outChan <- AgentEvent{
					Type: "done",
					Raw:  payload,
				}
			case "content":
				txt, _ := payload["content"].(string)
				if txt != "" {
					outChan <- AgentEvent{
						Type:    "content",
						Content: txt,
						Raw:     payload,
					}
				}
			case "done":
				outChan <- AgentEvent{
					Type: "done",
					Raw:  payload,
				}
			}
		}

		err := cmd.Wait()
		if err != nil {
			errStr := strings.TrimSpace(errBuf.String())
			if errStr == "" {
				errStr = fmt.Sprintf("agy process failed: %v", err)
			}
			if !strings.Contains(errStr, "context canceled") {
				outChan <- AgentEvent{
					Type:    "error",
					Content: errStr,
				}
			}
		}
	}()

	return outChan, nil
}

// executeOllama handles local Ollama model generation via HTTP stream
func (r *AntigravityRunner) executeOllama(ctx context.Context, opts RunnerOptions, outChan chan<- AgentEvent) {
	defer close(outChan)

	targetModel := "llama3.2:3b"
	home, _ := os.UserHomeDir()
	if cfgData, err := os.ReadFile(filepath.Join(home, ".config", "antigravity", "ollama_default_model.txt")); err == nil {
		if tm := strings.TrimSpace(string(cfgData)); tm != "" {
			targetModel = tm
		}
	}

	reqBody, _ := json.Marshal(map[string]interface{}{
		"model":  targetModel,
		"prompt": opts.Prompt,
		"stream": true,
	})

	httpReq, err := http.NewRequestWithContext(ctx, "POST", "http://127.0.0.1:11434/api/generate", bytes.NewReader(reqBody))
	if err != nil {
		outChan <- AgentEvent{Type: "error", Content: fmt.Sprintf("Failed to build Ollama request: %v", err)}
		return
	}
	httpReq.Header.Set("Content-Type", "application/json")

	client := &http.Client{Timeout: 0}
	resp, err := client.Do(httpReq)
	if err != nil {
		outChan <- AgentEvent{
			Type:    "error",
			Content: fmt.Sprintf("Ollama daemon is offline at http://127.0.0.1:11434 (%v). Start it with 'ollama serve' or 'agyollama start'.", err),
		}
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		b, _ := io.ReadAll(resp.Body)
		outChan <- AgentEvent{Type: "error", Content: fmt.Sprintf("Ollama returned status %d: %s", resp.StatusCode, string(b))}
		return
	}

	scanner := bufio.NewScanner(resp.Body)
	for scanner.Scan() {
		line := scanner.Bytes()
		if len(line) == 0 {
			continue
		}
		var chunk struct {
			Response string `json:"response"`
			Done     bool   `json:"done"`
		}
		if err := json.Unmarshal(line, &chunk); err == nil {
			if chunk.Response != "" {
				outChan <- AgentEvent{Type: "content", Content: chunk.Response}
			}
			if chunk.Done {
				outChan <- AgentEvent{Type: "done"}
				return
			}
		}
	}

	if err := scanner.Err(); err != nil && ctx.Err() == nil {
		outChan <- AgentEvent{Type: "error", Content: fmt.Sprintf("Ollama stream error: %v", err)}
	}
}
