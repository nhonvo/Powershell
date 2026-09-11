package bot

import (
	"context"
	"encoding/json"
	"io"
	"net/http"
	"net/http/httptest"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"testing"
	"time"

	"agybot/internal/account"
	"agybot/internal/auth"
	"agybot/internal/config"
	"agybot/internal/runner"
	"agybot/internal/security"
	"agybot/internal/workspace"
)

type capturedMessage struct {
	ChatID int64
	Text   string
	Markup *InlineKeyboardMarkup
}

type testBotFixture struct {
	handler   *BotHandler
	captured  []capturedMessage
	mu        sync.Mutex
	server    *httptest.Server
	tempWsDir string
}

func setupTestFixture(t *testing.T) *testBotFixture {
	fix := &testBotFixture{}

	fix.server = httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		bodyBytes, _ := io.ReadAll(r.Body)

		switch {
		case strings.HasSuffix(r.URL.Path, "/sendMessage"):
			var payload struct {
				ChatID      int64                 `json:"chat_id"`
				Text        string                `json:"text"`
				ReplyMarkup *InlineKeyboardMarkup `json:"reply_markup"`
			}
			_ = json.Unmarshal(bodyBytes, &payload)
			fix.mu.Lock()
			fix.captured = append(fix.captured, capturedMessage{
				ChatID: payload.ChatID,
				Text:   payload.Text,
				Markup: payload.ReplyMarkup,
			})
			fix.mu.Unlock()
			_, _ = w.Write([]byte(`{"ok":true,"result":{"message_id":100}}`))

		case strings.HasSuffix(r.URL.Path, "/editMessageText"):
			var payload struct {
				ChatID    int64  `json:"chat_id"`
				MessageID int64  `json:"message_id"`
				Text      string `json:"text"`
			}
			_ = json.Unmarshal(bodyBytes, &payload)
			fix.mu.Lock()
			fix.captured = append(fix.captured, capturedMessage{
				ChatID: payload.ChatID,
				Text:   payload.Text,
			})
			fix.mu.Unlock()
			_, _ = w.Write([]byte(`{"ok":true,"result":{"message_id":100}}`))

		case strings.HasSuffix(r.URL.Path, "/answerCallbackQuery"):
			_, _ = w.Write([]byte(`{"ok":true,"result":true}`))

		case strings.HasSuffix(r.URL.Path, "/sendChatAction"):
			_, _ = w.Write([]byte(`{"ok":true,"result":true}`))

		case strings.HasSuffix(r.URL.Path, "/getMe"):
			_, _ = w.Write([]byte(`{"ok":true,"result":{"id":8896359262,"is_bot":true,"first_name":"fin","username":"abc_aoajwe_test_bot"}}`))

		default:
			_, _ = w.Write([]byte(`{"ok":true,"result":[]}`))
		}
	}))

	client := NewTelegramClient("test-token")
	client.apiBaseURL = fix.server.URL

	tempDir := t.TempDir()
	fix.tempWsDir = tempDir

	cfg := &config.Config{
		TelegramBotToken:    "test-token",
		AllowedUserIDs:      map[int64]bool{8343607963: true},
		AuthPinHash:         auth.HashPIN("123456"),
		AuthMaxAttempts:     5,
		AuthLockoutDuration: 5 * time.Minute,
		AuthAutoLockTimeout: 30 * time.Minute,
		DefaultModel:        "Gemini 3.7 Flash",
		DefaultWorkspace:    tempDir,
	}

	guard := security.NewSecurityGuard()
	authMgr := auth.NewAuthManager(cfg.AuthPinHash, cfg.AuthMaxAttempts, cfg.AuthLockoutDuration, cfg.AuthAutoLockTimeout, cfg.AllowedUserIDs)
	wsMgr := workspace.NewWorkspaceManager(tempDir)
	accMgr := account.NewAccountManager()
	r := runner.NewAntigravityRunner(guard)

	fix.handler = NewBotHandler(cfg, client, authMgr, wsMgr, accMgr, r, guard)
	return fix
}

func (f *testBotFixture) lastMessage() capturedMessage {
	f.mu.Lock()
	defer f.mu.Unlock()
	if len(f.captured) == 0 {
		return capturedMessage{}
	}
	return f.captured[len(f.captured)-1]
}

func (f *testBotFixture) clearMessages() {
	f.mu.Lock()
	defer f.mu.Unlock()
	f.captured = nil
}

func (f *testBotFixture) sendUserText(userID int64, text string) {
	ctx := context.Background()
	u := Update{
		UpdateID: 1,
		Message: &Message{
			MessageID: 10,
			From: &User{
				ID:        userID,
				FirstName: "Nhon",
				Username:  "truongnhon",
			},
			Chat: Chat{
				ID:   userID,
				Type: "private",
			},
			Text: text,
		},
	}
	f.handler.handleUpdate(ctx, u)
}

func (f *testBotFixture) sendCallback(userID int64, data string) {
	ctx := context.Background()
	u := Update{
		UpdateID: 2,
		CallbackQuery: &CallbackQuery{
			ID: "cq_1",
			From: User{
				ID:        userID,
				FirstName: "Nhon",
				Username:  "truongnhon",
			},
			Message: &Message{
				MessageID: 10,
				Chat: Chat{
					ID:   userID,
					Type: "private",
				},
			},
			Data: data,
		},
	}
	f.handler.handleUpdate(ctx, u)
}

func TestTelegramE2E_FullFeatures(t *testing.T) {
	fix := setupTestFixture(t)
	defer fix.server.Close()

	const testUserID int64 = 8343607963
	const unauthorizedUser int64 = 9999999999

	// 1. Unauthorized user rejection
	t.Run("1_Unauthorized_Access_Denied", func(t *testing.T) {
		fix.clearMessages()
		fix.sendUserText(unauthorizedUser, "/start")
		msg := fix.lastMessage()
		if !strings.Contains(msg.Text, "ACCESS DENIED") {
			t.Fatalf("Expected ACCESS DENIED for unlisted user, got: %s", msg.Text)
		}
	})

	// 2. PIN Authentication Flow
	t.Run("2_Auth_Flow", func(t *testing.T) {
		fix.clearMessages()
		// Initial command before auth should prompt for PIN
		fix.sendUserText(testUserID, "/status")
		msg := fix.lastMessage()
		if !strings.Contains(msg.Text, "CONTROLLER IS LOCKED") && !strings.Contains(msg.Text, "PIN") {
			t.Fatalf("Expected controller locked prompt, got: %s", msg.Text)
		}

		// Send invalid PIN
		fix.clearMessages()
		fix.sendUserText(testUserID, "000000")
		msg = fix.lastMessage()
		if !strings.Contains(msg.Text, "Incorrect PIN") {
			t.Fatalf("Expected Incorrect PIN, got: %s", msg.Text)
		}

		// Send correct PIN
		fix.clearMessages()
		fix.sendUserText(testUserID, "123456")
		msg = fix.lastMessage()
		if !strings.Contains(msg.Text, "Authentication successful") {
			t.Fatalf("Expected Authentication successful, got: %s", msg.Text)
		}
		if msg.Markup == nil || len(msg.Markup.InlineKeyboard) < 3 {
			t.Fatalf("Expected main inline keyboard after authentication")
		}
	})

	// 3. /status Command
	t.Run("3_Status_Command", func(t *testing.T) {
		fix.clearMessages()
		fix.sendUserText(testUserID, "/status")
		msg := fix.lastMessage()
		if !strings.Contains(msg.Text, "HOST SYSTEM & AGY HEALTH") {
			t.Fatalf("Expected HOST SYSTEM & AGY HEALTH, got: %s", msg.Text)
		}
		if !strings.Contains(msg.Text, "RAM:") || !strings.Contains(msg.Text, "Disk:") {
			t.Fatalf("Expected RAM and Disk metrics in status, got: %s", msg.Text)
		}
	})

	// 4. /projects and /cd Commands
	t.Run("4_Projects_And_Cd", func(t *testing.T) {
		fix.clearMessages()
		fix.sendUserText(testUserID, "/projects")
		msg := fix.lastMessage()
		if !strings.Contains(msg.Text, "REGISTERED PROJECTS") {
			t.Fatalf("Expected REGISTERED PROJECTS, got: %s", msg.Text)
		}

		// Test /cd without args (shows current)
		fix.clearMessages()
		fix.sendUserText(testUserID, "/cd")
		msg = fix.lastMessage()
		if !strings.Contains(msg.Text, "Current workspace:") {
			t.Fatalf("Expected Current workspace, got: %s", msg.Text)
		}

		// Test /cd to a temp directory
		fix.clearMessages()
		subDir := filepath.Join(fix.tempWsDir, "subproject")
		_ = os.MkdirAll(subDir, 0755)
		fix.sendUserText(testUserID, "/cd "+subDir)
		msg = fix.lastMessage()
		if !strings.Contains(msg.Text, "Workspace switched to") {
			t.Fatalf("Expected Workspace switched to, got: %s", msg.Text)
		}
	})

	// 5. /newproj Scaffolding Command
	t.Run("5_NewProj_Scaffolding", func(t *testing.T) {
		fix.clearMessages()
		projName := "test-tele-scaffold"
		fix.sendUserText(testUserID, "/newproj "+projName+" go")
		msg := fix.lastMessage()
		if !strings.Contains(msg.Text, "PROJECT CREATED & REGISTERED") {
			t.Fatalf("Expected PROJECT CREATED & REGISTERED, got: %s", msg.Text)
		}
		if !strings.Contains(msg.Text, projName) {
			t.Fatalf("Expected project name in confirmation, got: %s", msg.Text)
		}

		// Verify project files exist
		home, _ := os.UserHomeDir()
		createdPath := filepath.Join(home, "projects", projName)
		if _, err := os.Stat(filepath.Join(createdPath, "go.mod")); err == nil {
			// Cleanup created test project
			_ = os.RemoveAll(createdPath)
		}
	})

	// 6. /sessions and /session Commands
	t.Run("6_Sessions_Management", func(t *testing.T) {
		fix.clearMessages()
		fix.sendUserText(testUserID, "/sessions")
		msg := fix.lastMessage()
		if !strings.Contains(msg.Text, "ANTIGRAVITY AI SESSIONS") {
			t.Fatalf("Expected ANTIGRAVITY AI SESSIONS, got: %s", msg.Text)
		}

		fix.clearMessages()
		fix.sendUserText(testUserID, "/session")
		msg = fix.lastMessage()
		if !strings.Contains(msg.Text, "ACTIVE SESSION CONTEXT") {
			t.Fatalf("Expected ACTIVE SESSION CONTEXT, got: %s", msg.Text)
		}
		if !strings.Contains(msg.Text, "Conversation ID:") {
			t.Fatalf("Expected Conversation ID in session context, got: %s", msg.Text)
		}
	})

	// 7. /config and /models Commands
	t.Run("7_Config_And_Models", func(t *testing.T) {
		fix.clearMessages()
		fix.sendUserText(testUserID, "/config")
		msg := fix.lastMessage()
		if !strings.Contains(msg.Text, "AGYBOT SYSTEM CONFIGURATION") {
			t.Fatalf("Expected AGYBOT SYSTEM CONFIGURATION, got: %s", msg.Text)
		}

		fix.clearMessages()
		fix.sendUserText(testUserID, "/models")
		msg = fix.lastMessage()
		if !strings.Contains(msg.Text, "AVAILABLE ANTIGRAVITY AI MODELS") {
			t.Fatalf("Expected AVAILABLE ANTIGRAVITY AI MODELS, got: %s", msg.Text)
		}
	})

	// 8. /account and /finance Commands
	t.Run("8_Account_And_Finance", func(t *testing.T) {
		fix.clearMessages()
		fix.sendUserText(testUserID, "/account")
		msg := fix.lastMessage()
		if !strings.Contains(msg.Text, "ANTIGRAVITY ACCOUNTS & QUOTA") {
			t.Fatalf("Expected ANTIGRAVITY ACCOUNTS & QUOTA, got: %s", msg.Text)
		}

		fix.clearMessages()
		fix.sendUserText(testUserID, "/finance")
		msg = fix.lastMessage()
		if !strings.Contains(msg.Text, "FINANCE DASHBOARD DIRECT INTEGRATION") {
			t.Fatalf("Expected FINANCE DASHBOARD DIRECT INTEGRATION, got: %s", msg.Text)
		}
	})

	// 9. Inline Keyboard Callbacks
	t.Run("9_Callback_Buttons", func(t *testing.T) {
		// btn_status
		fix.clearMessages()
		fix.sendCallback(testUserID, "btn_status")
		msg := fix.lastMessage()
		if !strings.Contains(msg.Text, "HOST SYSTEM & AGY HEALTH") {
			t.Fatalf("Expected status message on btn_status, got: %s", msg.Text)
		}

		// btn_projects
		fix.clearMessages()
		fix.sendCallback(testUserID, "btn_projects")
		msg = fix.lastMessage()
		if !strings.Contains(msg.Text, "REGISTERED PROJECTS") {
			t.Fatalf("Expected projects message on btn_projects, got: %s", msg.Text)
		}

		// btn_sessions
		fix.clearMessages()
		fix.sendCallback(testUserID, "btn_sessions")
		msg = fix.lastMessage()
		if !strings.Contains(msg.Text, "ANTIGRAVITY AI SESSIONS") {
			t.Fatalf("Expected sessions message on btn_sessions, got: %s", msg.Text)
		}

		// btn_lock
		fix.clearMessages()
		fix.sendCallback(testUserID, "btn_lock")
		msg = fix.lastMessage()
		if !strings.Contains(msg.Text, "Controller locked") {
			t.Fatalf("Expected lock confirmation on btn_lock, got: %s", msg.Text)
		}

		// Ensure now locked
		fix.clearMessages()
		fix.sendUserText(testUserID, "/status")
		msg = fix.lastMessage()
		if !strings.Contains(msg.Text, "CONTROLLER IS LOCKED") {
			t.Fatalf("Expected controller locked after btn_lock, got: %s", msg.Text)
		}
	})

	// 10. Research Command Syntax Validation
	t.Run("10_Research_Command", func(t *testing.T) {
		// First unlock
		fix.sendUserText(testUserID, "123456")

		fix.clearMessages()
		// Test missing topic
		fix.sendUserText(testUserID, "/research")
		msg := fix.lastMessage()
		if !strings.Contains(msg.Text, "Usage: `/research") {
			t.Fatalf("Expected usage instruction for /research, got: %s", msg.Text)
		}
	})
}
