package tunnel

import (
	"testing"
)

func TestCloudflareTunnel_Lifecycle(t *testing.T) {
	tun := NewCloudflareTunnel()
	if tun.IsActive {
		t.Errorf("Expected new tunnel to be inactive")
	}
	tun.Stop()
	if tun.IsActive {
		t.Errorf("Expected tunnel to remain inactive after Stop()")
	}
}
