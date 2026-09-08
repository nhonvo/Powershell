package shelltool

import (
	"testing"
)

func TestListExternalTools(t *testing.T) {
	tools := ListExternalTools()
	if len(tools) == 0 {
		t.Fatalf("expected at least 1 external tool info, got 0")
	}

	foundOhMyPosh := false
	foundHistory := false
	for _, tool := range tools {
		if tool.ID == "oh-my-posh" {
			foundOhMyPosh = true
		}
		if tool.ID == "history-autosuggest" {
			foundHistory = true
			if tool.Category != "Shell & Prediction" {
				t.Errorf("unexpected category for history tool: %s", tool.Category)
			}
		}
	}

	if !foundOhMyPosh {
		t.Errorf("expected oh-my-posh in external tools")
	}
	if !foundHistory {
		t.Errorf("expected history-autosuggest in external tools")
	}
}
