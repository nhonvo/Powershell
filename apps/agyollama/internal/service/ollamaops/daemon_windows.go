//go:build windows

package ollamaops

import (
	"os/exec"
)

func setDaemonSysProcAttr(cmd *exec.Cmd) {
	// On Windows, syscall.SysProcAttr has different flags
}
