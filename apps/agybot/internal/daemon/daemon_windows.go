//go:build windows

package daemon

import (
	"os/exec"
)

func setDaemonSysProcAttr(cmd *exec.Cmd) {
	// Setsid is not supported on Windows
}
