//go:build !windows

package ollamaops

import (
	"os/exec"
	"syscall"
)

func setDaemonSysProcAttr(cmd *exec.Cmd) {
	cmd.SysProcAttr = &syscall.SysProcAttr{Setpgid: true}
}
