package theme

import (
	"os"
	"path/filepath"
	"testing"
)

func TestTheme_ListAndSetTheme(t *testing.T) {
	dir := t.TempDir()
	theme1 := filepath.Join(dir, "catppuccin.omp.json")
	_ = os.WriteFile(theme1, []byte(`{"blocks":[{"segments":[{"type":"path","background":"green"}]}]}`), 0644)

	themes, err := ListThemes(dir, "catppuccin")
	if err != nil {
		t.Fatalf("ListThemes failed: %v", err)
	}
	if len(themes) != 1 {
		t.Fatalf("expected 1 theme, got %d", len(themes))
	}
	if !themes[0].IsSelected {
		t.Errorf("expected catppuccin to be selected")
	}

	err = SetTheme("catppuccin", dir)
	if err != nil {
		t.Fatalf("SetTheme failed: %v", err)
	}
}
