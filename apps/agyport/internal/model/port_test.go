package model

import (
	"testing"
)

func TestFormatBytes(t *testing.T) {
	tests := []struct {
		bytes    uint64
		expected string
	}{
		{0, "0 B"},
		{500, "500 B"},
		{1024, "1.0 KB"},
		{1536, "1.5 KB"},
		{1048576, "1.0 MB"},
		{1073741824, "1.0 GB"},
		{2684354560, "2.5 GB"},
	}

	for _, tt := range tests {
		actual := FormatBytes(tt.bytes)
		if actual != tt.expected {
			t.Errorf("FormatBytes(%d) = %q; want %q", tt.bytes, actual, tt.expected)
		}
	}
}
