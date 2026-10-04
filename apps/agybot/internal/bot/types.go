package bot

type User struct {
	ID        int64  `json:"id"`
	IsBot     bool   `json:"is_bot"`
	FirstName string `json:"first_name"`
	Username  string `json:"username,omitempty"`
}

type Chat struct {
	ID    int64  `json:"id"`
	Type  string `json:"type"`
	Title string `json:"title,omitempty"`
}

type InlineKeyboardButton struct {
	Text         string `json:"text"`
	CallbackData string `json:"callback_data,omitempty"`
	URL          string `json:"url,omitempty"`
}

type InlineKeyboardMarkup struct {
	InlineKeyboard [][]InlineKeyboardButton `json:"inline_keyboard"`
}

type KeyboardButton struct {
	Text string `json:"text"`
}

type ReplyKeyboardMarkup struct {
	Keyboard       [][]KeyboardButton `json:"keyboard"`
	ResizeKeyboard bool               `json:"resize_keyboard,omitempty"`
	IsPersistent   bool               `json:"is_persistent,omitempty"`
}

type Document struct {
	FileID   string `json:"file_id"`
	FileName string `json:"file_name,omitempty"`
	MimeType string `json:"mime_type,omitempty"`
	FileSize int64  `json:"file_size,omitempty"`
}

type PhotoSize struct {
	FileID   string `json:"file_id"`
	Width    int    `json:"width"`
	Height   int    `json:"height"`
	FileSize int64  `json:"file_size,omitempty"`
}

type TGFile struct {
	FileID   string `json:"file_id"`
	FilePath string `json:"file_path,omitempty"`
}

type Message struct {
	MessageID int64       `json:"message_id"`
	From      *User       `json:"from,omitempty"`
	Chat      Chat        `json:"chat"`
	Text      string      `json:"text,omitempty"`
	Caption   string      `json:"caption,omitempty"`
	Document  *Document   `json:"document,omitempty"`
	Photo     []PhotoSize `json:"photo,omitempty"`
	Date      int64       `json:"date"`
	ReplyTo   *Message    `json:"reply_to_message,omitempty"`
}

type CallbackQuery struct {
	ID      string   `json:"id"`
	From    User     `json:"from"`
	Message *Message `json:"message,omitempty"`
	Data    string   `json:"data,omitempty"`
}

type Update struct {
	UpdateID      int64          `json:"update_id"`
	Message       *Message       `json:"message,omitempty"`
	CallbackQuery *CallbackQuery `json:"callback_query,omitempty"`
}

type APIResponse[T any] struct {
	OK          bool   `json:"ok"`
	Result      T      `json:"result,omitempty"`
	Description string `json:"description,omitempty"`
	ErrorCode   int    `json:"error_code,omitempty"`
}

type PromptMetrics struct {
	TotalPrompts int     `json:"total_prompts"`
	TotalSteps   int     `json:"total_steps"`
	EstTokens    int     `json:"est_tokens"`
	EstCostUSD   float64 `json:"est_cost_usd"`
}

type UserSessionState struct {
	ActiveConvID string        `json:"active_conv_id"`
	ActiveModel  string        `json:"active_model"`
	ActiveEffort string        `json:"active_effort"`
	ShowThinking bool          `json:"show_thinking"`
	Metrics      PromptMetrics `json:"metrics"`
}
