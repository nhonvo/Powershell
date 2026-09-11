package security

import (
	"fmt"
	"path/filepath"
	"regexp"
	"strings"
)

type DangerRule struct {
	Pattern     *regexp.Regexp
	Category    string
	Description string
}

type SecurityGuard struct {
	rules []DangerRule
}

func NewSecurityGuard() *SecurityGuard {
	rawRules := []struct {
		pattern string
		cat     string
		desc    string
	}{
		// 1. Format & Partition Manipulation
		{`(?i)\bformat\s+([a-zA-Z]:|/fs:)`, "Format Drive", "Cấm lệnh format ổ đĩa trực tiếp"},
		{`(?i)\b(format-volume|initialize-disk|clear-disk|remove-partition)\b`, "Disk Manipulation", "Cấm lệnh thao tác phân vùng hoặc xóa đĩa"},
		{`(?i)\bdiskpart\b`, "Diskpart Utility", "Cấm tiện ích phân vùng DiskPart"},
		{`(?i)\bvssadmin\s+delete\s+shadows\b`, "Delete Shadows", "Cấm lệnh xóa bản sao lưu hệ thống Shadow Copies"},
		{`(?i)\b(bcdedit|bootrec)\b`, "Boot Configuration", "Cấm can thiệp cấu hình khởi động BCD"},

		// 2. Destructive Root Directory & OS Deletion
		{`(?i)\b(rmdir|rd)\s+.*\/s`, "Recursive Mass Deletion", "Cấm xóa cây thư mục gốc qua cmd rd /s"},
		{`(?i)\brm\s+-[a-zA-Z]*r[a-zA-Z]*f\s+([a-zA-Z]:[\\/]?$|[\\/]$|\*|\.\.)`, "Root Wipe (rm -rf /)", "Cấm lệnh xóa đệ quy toàn bộ ổ đĩa gốc"},
		{`(?i)\bremove-item\s+.*-(recurse|force).*[a-zA-Z]:[\\/]?$`, "PowerShell Wipe Drive", "Cấm lệnh PowerShell xóa sạch toàn bộ ổ đĩa"},
		{`(?i)\bdel\s+.*\/f\s+.*\/s\s+.*[a-zA-Z]:\\`, "Mass File Deletion", "Cấm lệnh xóa hàng loạt toàn bộ file trên ổ đĩa"},
		{`(?i)\b(del|rmdir|rd|remove-item)\s+.*[c-z]:\\windows`, "Windows Directory Wipe", "Cấm hành vi xóa thư mục hệ điều hành Windows"},
		{`(?i)\b(takeown|icacls)\s+.*[c-z]:\\windows`, "Windows Permissions Hijack", "Cấm lệnh chiếm quyền kiểm soát thư mục Windows"},

		// 3. Registry & System Settings
		{`(?i)\breg\s+delete\s+(hklm|hkcr|hkey_local_machine)`, "Registry Wipe HKLM", "Cấm xóa Registry nhánh hệ thống HKLM"},
		{`(?i)\bremove-item(property)?\s+.*(hklm:|hkcr:)`, "PowerShell Registry Deletion", "Cấm xóa Registry hệ thống bằng PowerShell"},

		// 4. Shutdown, Reboot & OS Crash
		{`(?i)\bshutdown\s+(\/s|\/r|-s|-r)`, "Shutdown/Restart", "Cấm lệnh tắt máy hoặc khởi động lại từ xa"},
		{`(?i)\b(stop-computer|restart-computer)\b`, "PowerShell Shutdown", "Cấm lệnh tắt máy bằng PowerShell"},

		// 5. Remote Malware Dropper & Arbitrary Execution
		{`(?i)(downloadstring|downloaddata|downloadfile).*\|\s*(iex|invoke-expression)`, "Remote Script Dropper", "Cấm tải và thực thi script độc hại từ xa"},
		{`(?i)(curl|wget|irm)\s+.*\|\s*(iex|sh|bash)`, "Pipeline Remote Exec", "Cấm đường ống tải và thực thi mã không kiểm soát"},
	}

	var compiled []DangerRule
	for _, r := range rawRules {
		if re, err := regexp.Compile(r.pattern); err == nil {
			compiled = append(compiled, DangerRule{
				Pattern:     re,
				Category:    r.cat,
				Description: r.desc,
			})
		}
	}

	return &SecurityGuard{rules: compiled}
}

// CheckCommand inspects a command string against safety rules.
// Returns (isBlocked, category, description)
func (g *SecurityGuard) CheckCommand(cmd string) (bool, string, string) {
	clean := strings.TrimSpace(cmd)
	if clean == "" {
		return false, "", ""
	}

	for _, rule := range g.rules {
		if rule.Pattern.MatchString(clean) {
			return true, rule.Category, rule.Description
		}
	}
	return false, "", ""
}

// ValidateWorkspaceContainment checks whether targetPath resides safely inside allowedRoot
func (g *SecurityGuard) ValidateWorkspaceContainment(targetPath string, allowedRoot string) bool {
	cleanTarget, err1 := filepath.Abs(targetPath)
	cleanRoot, err2 := filepath.Abs(allowedRoot)
	if err1 != nil || err2 != nil {
		return false
	}

	rel, err := filepath.Rel(cleanRoot, cleanTarget)
	if err != nil {
		return false
	}

	// If relative path begins with "..", it escaped root
	if rel == ".." || strings.HasPrefix(rel, ".."+string(filepath.Separator)) {
		return false
	}
	return true
}

// GetSafetyPromptPrefix generates systemic constraint injected into Antigravity AI sessions
func (g *SecurityGuard) GetSafetyPromptPrefix(workspaceDir string) string {
	return fmt.Sprintf(
		"[SYSTEM SAFETY CONSTRAINT: You must NEVER execute destructive OS operations "+
			"such as formatting disks, running diskpart, wiping system directories, modifying HKLM registry, "+
			"or shutting down the computer. Strictly confine all file operations to: %s]\n\n",
		workspaceDir,
	)
}
