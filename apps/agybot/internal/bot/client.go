package bot

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"mime/multipart"
	"net/http"
	"os"
	"path/filepath"
	"strconv"
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

	err := c.post(ctx, "sendMessage", payload, &resp)
	if err != nil || !resp.OK {
		// Fallback without markdown if formatting was invalid
		delete(payload, "parse_mode")
		resp = APIResponse[Message]{}
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
	err := c.post(ctx, "editMessageText", payload, &resp)
	if err != nil || !resp.OK {
		delete(payload, "parse_mode")
		_ = c.post(ctx, "editMessageText", payload, &resp)
	}
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

func (c *TelegramClient) SendDocument(ctx context.Context, chatID int64, filePath string, caption string) error {
	file, err := os.Open(filePath)
	if err != nil {
		return fmt.Errorf("open file error: %w", err)
	}
	defer file.Close()

	var b bytes.Buffer
	w := multipart.NewWriter(&b)

	_ = w.WriteField("chat_id", strconv.FormatInt(chatID, 10))
	if caption != "" {
		_ = w.WriteField("caption", caption)
	}

	fw, err := w.CreateFormFile("document", filepath.Base(filePath))
	if err != nil {
		return err
	}
	if _, err = io.Copy(fw, file); err != nil {
		return err
	}
	_ = w.Close()

	url := fmt.Sprintf("%s/sendDocument", c.apiBaseURL)
	req, err := http.NewRequestWithContext(ctx, http.MethodPost, url, &b)
	if err != nil {
		return err
	}
	req.Header.Set("Content-Type", w.FormDataContentType())

	resp, err := c.httpClient.Do(req)
	if err != nil {
		return err
	}
	defer resp.Body.Close()

	var apiResp APIResponse[Message]
	if err := json.NewDecoder(resp.Body).Decode(&apiResp); err != nil {
		return err
	}
	if !apiResp.OK {
		return fmt.Errorf("sendDocument error: %s", apiResp.Description)
	}
	return nil
}

type BotCommand struct {
	Command     string `json:"command"`
	Description string `json:"description"`
}

func (c *TelegramClient) SetMyCommands(ctx context.Context, commands []BotCommand) error {
	var resp APIResponse[bool]
	payload := map[string]interface{}{
		"commands": commands,
	}
	return c.post(ctx, "setMyCommands", payload, &resp)
}

func (c *TelegramClient) GetFile(ctx context.Context, fileID string) (*TGFile, error) {
	var resp APIResponse[TGFile]
	payload := map[string]interface{}{
		"file_id": fileID,
	}
	if err := c.post(ctx, "getFile", payload, &resp); err != nil {
		return nil, err
	}
	if !resp.OK {
		return nil, fmt.Errorf("getFile error: %s", resp.Description)
	}
	return &resp.Result, nil
}

func (c *TelegramClient) DownloadFile(ctx context.Context, filePath string, targetLocalPath string) error {
	url := fmt.Sprintf("https://api.telegram.org/file/bot%s/%s", c.token, filePath)
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, url, nil)
	if err != nil {
		return err
	}
	resp, err := c.httpClient.Do(req)
	if err != nil {
		return err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return fmt.Errorf("download file failed with status %d", resp.StatusCode)
	}

	out, err := os.Create(targetLocalPath)
	if err != nil {
		return err
	}
	defer out.Close()

	_, err = io.Copy(out, resp.Body)
	return err
}

