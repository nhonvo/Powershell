package runner

import (
	"bufio"
	"context"
	"encoding/json"
	"fmt"
	"os/exec"
	"path/filepath"
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

// AvailableModels returns standard Google Antigravity models
func (r *AntigravityRunner) AvailableModels() []string {
	return []string{
		"Gemini 3.7 Flash",
		"Gemini 3.1 Pro",
		"Claude Sonnet 4.6",
		"Claude Opus 4.6",
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

// ExecutePrompt runs agy CLI with streaming JSON and pushes events to output channel
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

	agyExec := opts.AgyPath
	if agyExec == "" {
		agyExec = "agy"
	}

	// Prepare safe prompt
	effectivePrompt := opts.Prompt
	if opts.ConversationID == "" {
		effectivePrompt = r.guard.GetSafetyPromptPrefix(opts.WorkspaceDir) + opts.Prompt
	}

	args := []string{
		"-p", effectivePrompt,
		"--output-format", "stream-json",
		"--dangerously-skip-permissions",
	}

	if opts.ConversationID != "" {
		args = append(args, "--conversation", opts.ConversationID)
	}
	if opts.Model != "" {
		args = append(args, "--model", opts.Model)
	}
	if opts.Effort != "" {
		args = append(args, "--effort", opts.Effort)
	}
	if opts.Mode != "" {
		args = append(args, "--mode", opts.Mode)
	}

	cmd := exec.CommandContext(ctx, agyExec, args...)
	if opts.WorkspaceDir != "" {
		cmd.Dir = opts.WorkspaceDir
	}

	stdout, err := cmd.StdoutPipe()
	if err != nil {
		close(outChan)
		return nil, fmt.Errorf("failed to open stdout pipe: %w", err)
	}

	if err := cmd.Start(); err != nil {
		close(outChan)
		return nil, fmt.Errorf("failed to start agy process: %w", err)
	}

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
					}
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

		_ = cmd.Wait()
	}()

	return outChan, nil
}
