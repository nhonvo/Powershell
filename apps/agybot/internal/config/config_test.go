package config

import (
	"os"
	"testing"
	"time"
)

func TestConfig_LoadDefaults(t *testing.T) {
	_ = os.Setenv("ALLOWED_USER_IDS", "12345678,98765432")
	_ = os.Setenv("AUTH_PIN_HASH", "test_hash")
	_ = os.Setenv("DEFAULT_MODEL", "Claude Sonnet 4.6")

	defer func() {
		_ = os.Unsetenv("ALLOWED_USER_IDS")
		_ = os.Unsetenv("AUTH_PIN_HASH")
		_ = os.Unsetenv("DEFAULT_MODEL")
	}()

	cfg := LoadConfig()
	if !cfg.AllowedUserIDs[12345678] || !cfg.AllowedUserIDs[98765432] {
		t.Errorf("Expected user IDs in whitelist, got %v", cfg.AllowedUserIDs)
	}
	if cfg.DefaultModel != "Claude Sonnet 4.6" {
		t.Errorf("Expected Claude Sonnet 4.6, got %s", cfg.DefaultModel)
	}
	if cfg.AuthMaxAttempts != 5 {
		t.Errorf("Expected default 5 attempts, got %d", cfg.AuthMaxAttempts)
	}
	if cfg.AuthAutoLockTimeout != 30*time.Minute {
		t.Errorf("Expected 30m auto lock, got %v", cfg.AuthAutoLockTimeout)
	}
}
