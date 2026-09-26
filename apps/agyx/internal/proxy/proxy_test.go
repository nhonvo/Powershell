package proxy

import (
	"testing"
)

func TestProxy_GetRegisteredTools(t *testing.T) {
	tools := GetRegisteredTools()
	if len(tools) != 10 {
		t.Fatalf("expected 10 tools, got %d", len(tools))
	}
}

func TestProxy_ResolveTool(t *testing.T) {
	cases := []struct {
		input    string
		expected string
	}{
		{"switch", "agyswitch"},
		{"s", "agyswitch"},
		{"acc", "agyswitch"},
		{"proj", "agyproj"},
		{"ws", "agyproj"},
		{"git", "agygit"},
		{"wt", "agygit"},
		{"docker", "agydocker"},
		{"ram", "agydocker"},
		{"term", "agyterm"},
		{"theme", "agyterm"},
		{"font", "agyterm"},
		{"mobile", "agymobile"},
		{"m", "agymobile"},
		{"remote", "agymobile"},
		{"phone", "agymobile"},
		{"pwa", "agymobile"},
		{"ollama", "agyollama"},
		{"ai", "agyollama"},
		{"llm", "agyollama"},
		{"localai", "agyollama"},
		{"aws", "aws"},
		{"cloud", "aws"},
		{"localstack", "aws"},
		{"bot", "agybot"},
		{"b", "agybot"},
		{"telegram", "agybot"},
		{"tg", "agybot"},
		{"port", "agyport"},
		{"ports", "agyport"},
		{"killport", "agyport"},
		{"kp", "agyport"},
	}

	for _, c := range cases {
		tool := ResolveTool(c.input)
		if tool == nil {
			t.Errorf("failed to resolve %s", c.input)
		} else if tool.BinaryName != c.expected {
			t.Errorf("for input %s, expected %s, got %s", c.input, c.expected, tool.BinaryName)
		}
	}
}

func TestProxy_FindBinary(t *testing.T) {
	// FindBinary should find agyswitch in PATH or candidate paths
	binPath, err := FindBinary("agyswitch")
	if err != nil {
		t.Fatalf("expected to find agyswitch, got error: %v", err)
	}
	if binPath == "" {
		t.Fatalf("expected non-empty path for agyswitch")
	}
}

