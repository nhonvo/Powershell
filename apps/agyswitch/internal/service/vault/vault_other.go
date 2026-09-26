//go:build !windows

package vault

// ReadWindowsCredential returns empty string on non-Windows platforms.
func ReadWindowsCredential(target string) string {
	return ""
}

// WriteWindowsCredential is a no-op on non-Windows platforms.
func WriteWindowsCredential(target string, secret string) bool {
	return false
}

// DeleteWindowsCredential is a no-op on non-Windows platforms.
func DeleteWindowsCredential(target string) bool {
	return false
}
