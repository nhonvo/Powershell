//go:build !windows

package vault

import (
	"strings"

	"github.com/zalando/go-keyring"
)

func parseTarget(target string) (string, string) {
	parts := strings.SplitN(target, ":", 2)
	if len(parts) == 2 {
		return parts[0], parts[1]
	}
	return target, target
}

// ReadWindowsCredential reads stored credentials from system Keyring / Secret Service on non-Windows platforms.
func ReadWindowsCredential(target string) string {
	svc, user := parseTarget(target)
	val, err := keyring.Get(svc, user)
	if err != nil {
		return ""
	}
	raw := strings.TrimSpace(val)
	if tok := ExtractCleanAccessToken(raw); tok != "" {
		if strings.HasPrefix(raw, "{") && strings.HasSuffix(raw, "}") {
			return raw
		}
		return tok
	}
	return raw
}

// WriteWindowsCredential stores credentials in system Keyring / Secret Service on non-Windows platforms.
func WriteWindowsCredential(target string, secret string) bool {
	if target == "" || secret == "" {
		return false
	}
	svc, user := parseTarget(target)
	err := keyring.Set(svc, user, secret)
	return err == nil
}

// DeleteWindowsCredential removes credentials from system Keyring / Secret Service on non-Windows platforms.
func DeleteWindowsCredential(target string) bool {
	if target == "" {
		return false
	}
	svc, user := parseTarget(target)
	err := keyring.Delete(svc, user)
	return err == nil
}
