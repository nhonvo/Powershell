//go:build windows

package view

import "time"

// waitKey on Windows sleeps briefly and returns true to allow checking input.
func waitKey(fd int, timeoutMs int) bool {
	time.Sleep(time.Duration(timeoutMs) * time.Millisecond)
	return true
}
