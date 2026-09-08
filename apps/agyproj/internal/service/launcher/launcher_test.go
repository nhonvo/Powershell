package launcher_test

import (
	"testing"

	"agyproj/internal/service/launcher"
)

func TestLauncher_Launch(t *testing.T) {
	var executedName string
	var executedArgs []string

	l := &launcher.Launcher{
		Runner: func(name string, args ...string) error {
			executedName = name
			executedArgs = args
			return nil
		},
	}

	err := l.Launch("code", "/home/user/projects/finance-dashboard")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}

	if executedName != "code" {
		t.Errorf("expected 'code', got '%s'", executedName)
	}

	if len(executedArgs) != 1 || executedArgs[0] != "/home/user/projects/finance-dashboard" {
		t.Errorf("unexpected args: %+v", executedArgs)
	}
}

func TestLauncher_WindowsPathConversion(t *testing.T) {
	// Test basic Windows path translation
	wslPath, err := launcher.WindowsToWslPath(`C:\Users\TestUser\AppData\Local`)
	if err != nil {
		t.Fatalf("unexpected error converting path: %v", err)
	}
	if wslPath != "/mnt/c/Users/TestUser/AppData/Local" {
		t.Errorf("expected '/mnt/c/Users/TestUser/AppData/Local', got '%s'", wslPath)
	}

	// Test FindWindowsVSCode does not crash
	_ = launcher.FindWindowsVSCode()
}

