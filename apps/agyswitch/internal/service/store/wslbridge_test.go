package store

import (
	"testing"
)

func TestPathConversion(t *testing.T) {
	linuxPath := "/mnt/c/Users/TruongNhon/.gemini_fptvttnhon2026"
	winPath := ToWindowsPath(linuxPath)
	expectedWin := "C:\\Users\\TruongNhon\\.gemini_fptvttnhon2026"
	if winPath != expectedWin {
		t.Errorf("ToWindowsPath(%q) = %q; want %q", linuxPath, winPath, expectedWin)
	}

	convertedLinux := ToLinuxPath(expectedWin)
	expectedLinux := "/mnt/c/Users/TruongNhon/.gemini_fptvttnhon2026"
	if convertedLinux != expectedLinux {
		t.Errorf("ToLinuxPath(%q) = %q; want %q", expectedWin, convertedLinux, expectedLinux)
	}
}
