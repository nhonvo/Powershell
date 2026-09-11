package bot

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"time"
)

type TelegramClient struct {
	token      string
	apiBaseURL string
	httpClient *http.Client
}

func NewTelegramClient(token string) *TelegramClient {
	return &TelegramClient{
		token:      token,
		apiBaseURL: fmt.Sprintf("https://api.telegram.org/bot%s", token),
		httpClient: &http.Client{Timeout: 35 * time.Second},
	}
}

func (c *TelegramClient) post(ctx context.Context, method string, payload interface{}, target interface{}) error {
	bodyBytes, err := json.Marshal(payload)
	if err != nil {
		return fmt.Errorf("marshal payload error: %w", err)
	}

	url := fmt.Sprintf("%s/%s", c.apiBaseURL, method)
	req, err := http.NewRequestWithContext(ctx, http.MethodPost, url, bytes.NewReader(bodyBytes))
	if err != nil {
		return fmt.Errorf("create request error: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")

	resp, err := c.httpClient.Do(req)
	if err != nil {
		return fmt.Errorf("request %s failed: %w", method, err)
	}
	defer resp.Body.Close()

	respBytes, err := io.ReadAll(resp.Body)
	if err != nil {
		return fmt.Errorf("read response body error: %w", err)
	}

	if target != nil {
		if err := json.Unmarshal(respBytes, target); err != nil {
			return fmt.Errorf("unmarshal error for %s: %w", method, err)
		}
	}
	return nil
}

func (c *TelegramClient) GetMe(ctx context.Context) (*User, error) {
	var resp APIResponse[User]
	if err := c.post(ctx, "getMe", map[string]interface{}{}, &resp); err != nil {
		return nil, err
	}
	if !resp.OK {
		return nil, fmt.Errorf("getMe error: %s (%d)", resp.Description, resp.ErrorCode)
	}
	return &resp.Result, nil
}

func (c *TelegramClient) GetUpdates(ctx context.Context, offset int64, timeoutSec int) ([]Update, error) {
	var resp APIResponse[[]Update]
	payload := map[string]interface{}{
		"offset":  offset,
		"timeout": timeoutSec,
	}
	if err := c.post(ctx, "getUpdates", payload, &resp); err != nil {
		return nil, err
	}
	if !resp.OK {
		return nil, fmt.Errorf("getUpdates error: %s (%d)", resp.Description, resp.ErrorCode)
	}
	return resp.Result, nil
}

func (c *TelegramClient) SendMessage(ctx context.Context, chatID int64, text string, markup *InlineKeyboardMarkup) (*Message, error) {
	var resp APIResponse[Message]
	payload := map[string]interface{}{
		"chat_id":    chatID,
		"text":       text,
		"parse_mode": "Markdown",
	}
	if markup != nil {
		payload["reply_markup"] = markup
	}

	if err := c.post(ctx, "sendMessage", payload, &resp); err != nil {
		// Fallback without markdown if formatting was invalid
		payload["parse_mode"] = ""
		_ = c.post(ctx, "sendMessage", payload, &resp)
	}
	if !resp.OK {
		return nil, fmt.Errorf("sendMessage error: %s", resp.Description)
	}
	return &resp.Result, nil
}

func (c *TelegramClient) EditMessageText(ctx context.Context, chatID int64, messageID int64, text string, markup *InlineKeyboardMarkup) error {
	var resp APIResponse[Message]
	payload := map[string]interface{}{
		"chat_id":    chatID,
		"message_id": messageID,
		"text":       text,
		"parse_mode": "Markdown",
	}
	if markup != nil {
		payload["reply_markup"] = markup
	}
	_ = c.post(ctx, "editMessageText", payload, &resp)
	return nil
}

func (c *TelegramClient) AnswerCallbackQuery(ctx context.Context, queryID string, text string, showAlert bool) error {
	var resp APIResponse[bool]
	payload := map[string]interface{}{
		"callback_query_id": queryID,
		"text":              text,
		"show_alert":        showAlert,
	}
	return c.post(ctx, "answerCallbackQuery", payload, &resp)
}

func (c *TelegramClient) SendChatAction(ctx context.Context, chatID int64, action string) {
	var resp APIResponse[bool]
	payload := map[string]interface{}{
		"chat_id": chatID,
		"action":  action,
	}
	_ = c.post(ctx, "sendChatAction", payload, &resp)
}
