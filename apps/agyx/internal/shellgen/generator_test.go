package shellgen

import (
	"strings"
	"testing"
)

func TestGenerateZsh(t *testing.T) {
	out := GenerateZsh()
	if len(out) == 0 {
		t.Fatal("expected non-empty zsh script")
	}

	requiredSnippets := []string{
		"export PATH=\"$HOME/.local/bin:$PATH\"",
		"alias agydocker=\"$HOME/.local/bin/agydocker\"",
		"alias dk=\"docker ps\"",
		"alias gsu=\"$HOME/.local/bin/agygit\"",
		"proj() {",
		"dclean() {",
	}

	for _, s := range requiredSnippets {
		if !strings.Contains(out, s) {
			t.Errorf("expected zsh script to contain '%s'", s)
		}
	}
}

func TestGeneratePowerShell(t *testing.T) {
	out := GeneratePowerShell()
	if len(out) == 0 {
		t.Fatal("expected non-empty powershell script")
	}

	requiredSnippets := []string{
		"$suiteAppNames = @(\"agyswitch\"",
		"Set-Alias -Name 'agyd' -Value 'agydocker' -Force",
		"function proj {",
		"function dclean {",
	}

	for _, s := range requiredSnippets {
		if !strings.Contains(out, s) {
			t.Errorf("expected powershell script to contain '%s'", s)
		}
	}
}
