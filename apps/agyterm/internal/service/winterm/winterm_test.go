package winterm

import (
	"os"
	"path/filepath"
	"testing"
)

func TestWinTerm_ReadAndModifyProfiles(t *testing.T) {
	dir := t.TempDir()
	settingsPath := filepath.Join(dir, "settings.json")

	dummyJSON := `{
		"profiles": {
			"list": [
				{
					"name": "Ubuntu",
					"guid": "{1234}",
					"font": {
						"face": "Consolas",
						"size": 12.0
					},
					"opacity": 90
				}
			]
		}
	}`
	if err := os.WriteFile(settingsPath, []byte(dummyJSON), 0644); err != nil {
		t.Fatalf("failed to write dummy settings: %v", err)
	}

	profiles, err := ReadProfiles(settingsPath)
	if err != nil {
		t.Fatalf("ReadProfiles failed: %v", err)
	}
	if len(profiles) != 1 {
		t.Fatalf("expected 1 profile, got %d", len(profiles))
	}
	if profiles[0].FontFace != "Consolas" {
		t.Errorf("expected Consolas, got %s", profiles[0].FontFace)
	}

	// Update font
	err = UpdateProfileFont(settingsPath, "Ubuntu", "Hack Nerd Font", 13.0)
	if err != nil {
		t.Fatalf("UpdateProfileFont failed: %v", err)
	}

	updated, _ := ReadProfiles(settingsPath)
	if updated[0].FontFace != "Hack Nerd Font" {
		t.Errorf("expected Hack Nerd Font, got %s", updated[0].FontFace)
	}
	if updated[0].FontSize != 13.0 {
		t.Errorf("expected 13.0, got %f", updated[0].FontSize)
	}

	// Update opacity
	err = UpdateProfileOpacity(settingsPath, "Ubuntu", 85)
	if err != nil {
		t.Fatalf("UpdateProfileOpacity failed: %v", err)
	}

	updatedOp, _ := ReadProfiles(settingsPath)
	if updatedOp[0].Opacity != 85 {
		t.Errorf("expected opacity 85, got %d", updatedOp[0].Opacity)
	}
}

func TestWinTerm_ListAvailableFonts(t *testing.T) {
	fonts, err := ListAvailableFonts()
	if err != nil {
		t.Fatalf("ListAvailableFonts failed: %v", err)
	}
	if len(fonts) == 0 {
		t.Errorf("expected fonts list not to be empty")
	}
}
