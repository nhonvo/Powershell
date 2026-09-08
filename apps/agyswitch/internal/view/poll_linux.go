//go:build !windows

package view

import (
	"golang.org/x/sys/unix"
)

// waitKey polls fd for input up to timeoutMs milliseconds.
// Returns true if data is ready to read, false on timeout.
func waitKey(fd int, timeoutMs int) bool {
	pfd := []unix.PollFd{{Fd: int32(fd), Events: unix.POLLIN}}
	n, err := unix.Poll(pfd, timeoutMs)
	return err == nil && n > 0
}
