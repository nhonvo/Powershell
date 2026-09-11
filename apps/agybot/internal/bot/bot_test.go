package bot

import (
	"context"
	"net/http"
	"net/http/httptest"
	"testing"

	"agybot/internal/account"
	"agybot/internal/auth"
	"agybot/internal/config"
	"agybot/internal/runner"
	"agybot/internal/security"
	"agybot/internal/workspace"
)

func TestBot_MockServerAndHandler(t *testing.T) {
	ts := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		switch r.URL.Path {
		case "/bot12345/getMe":
			_, _ = w.Write([]byte(`{"ok":true,"result":{"id":999,"is_bot":true,"first_name":"AgyBot"}}`))
		case "/bot12345/sendMessage":
			_, _ = w.Write([]byte(`{"ok":true,"result":{"message_id":1,"chat":{"id":123,"type":"private"}}}`))
		default:
			_, _ = w.Write([]byte(`{"ok":true,"result":[]}`))
		}
	}))
	defer ts.Close()

	client := NewTelegramClient("12345")
	client.apiBaseURL = ts.URL + "/bot12345"

	u, err := client.GetMe(context.Background())
	if err != nil || u.FirstName != "AgyBot" {
		t.Fatalf("Expected getMe AgyBot, got %v (err: %v)", u, err)
	}

	cfg := &config.Config{
		AllowedUserIDs: map[int64]bool{123: true},
	}
	guard := security.NewSecurityGuard()
	authMgr := auth.NewAuthManager(auth.HashPIN("123456"), 3, 0, 0, cfg.AllowedUserIDs)
	wsMgr := workspace.NewWorkspaceManager("/tmp")
	accMgr := account.NewAccountManager()
	r := runner.NewAntigravityRunner(guard)

	handler := NewBotHandler(cfg, client, authMgr, wsMgr, accMgr, r, guard)
	if handler == nil {
		t.Fatalf("Expected non-nil BotHandler")
	}

	// Verify buildMainKeyboard
	kb := handler.buildMainKeyboard()
	if len(kb.InlineKeyboard) < 3 {
		t.Errorf("Expected at least 3 rows in keyboard, got %d", len(kb.InlineKeyboard))
	}
}
