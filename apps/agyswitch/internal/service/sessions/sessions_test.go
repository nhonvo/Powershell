package sessions_test

import (
	"os"
	"path/filepath"
	"testing"

	"agyswitch/internal/model"
	"agyswitch/internal/service/sessions"
)

func TestSessions_DiscoverAndParse(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "sessions_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	m := sessions.NewManager(tempDir)

	// Create dummy conversation transcript
	convoID := "test-convo-12345"
	logPath := filepath.Join(tempDir, ".gemini", "antigravity-cli", "brain", convoID, ".system_generated", "logs", "transcript.jsonl")
	_ = os.MkdirAll(filepath.Dir(logPath), 0755)

	logData := "{\"step_index\":1,\"type\":\"USER_INPUT\",\"status\":\"DONE\"}\n{\"step_index\":2,\"type\":\"PLANNER_RESPONSE\",\"status\":\"DONE\"}\n"
	_ = os.WriteFile(logPath, []byte(logData), 0644)

	sessList, err := m.DiscoverSessions()
	if err != nil {
		t.Fatalf("failed to discover sessions: %v", err)
	}

	if len(sessList) != 1 {
		t.Fatalf("expected 1 session, got %d", len(sessList))
	}

	if sessList[0].ConversationID != convoID {
		t.Errorf("expected conversationID '%s', got '%s'", convoID, sessList[0].ConversationID)
	}

	steps, err := sessions.ParseTranscriptSteps(logPath)
	if err != nil {
		t.Fatalf("failed to parse transcript steps: %v", err)
	}

	if len(steps) != 2 {
		t.Fatalf("expected 2 steps, got %d", len(steps))
	}
}

func TestCalculateSessionCost(t *testing.T) {
	tests := []struct {
		steps    int
		expected float64
	}{
		{steps: 0, expected: 0.0},
		{steps: -5, expected: 0.0},
		{steps: 2000, expected: float64(2000*400) / 1000000.0 * 1.25}, // 1.0
		{steps: 100, expected: float64(100*400) / 1000000.0 * 1.25},   // 0.05
	}

	for _, tt := range tests {
		got := sessions.CalculateSessionCost(tt.steps)
		if got != tt.expected {
			t.Errorf("CalculateSessionCost(%d) = %f; want %f", tt.steps, got, tt.expected)
		}
	}
}

func TestExtractProjectName(t *testing.T) {
	tests := []struct {
		wsDir    string
		expected string
	}{
		{"", "Default Workspace"},
		{"Default Workspace", "Default Workspace"},
		{"/home/truongnhon/projects/finance-dashboard", "finance-dashboard"},
		{"/home/truongnhon/projects/finance-dashboard/ui", "finance-dashboard"},
		{"/home/truongnhon/projects/csharp-sln", "csharp-sln"},
		{"/home/truongnhon/projects/powershell-profile", "powershell-profile"},
		{"/var/tmp/myapp", "myapp"},
	}

	for _, tt := range tests {
		got := sessions.ExtractProjectName(tt.wsDir)
		if got != tt.expected {
			t.Errorf("ExtractProjectName(%q) = %q; want %q", tt.wsDir, got, tt.expected)
		}
	}
}

func TestGroupSessionsByProject_And_Delete(t *testing.T) {
	tempDir, err := os.MkdirTemp("", "sessions_group_test_*")
	if err != nil {
		t.Fatalf("failed to create temp dir: %v", err)
	}
	defer os.RemoveAll(tempDir)

	m := sessions.NewManager(tempDir)

	// Create 2 conversations in brain
	c1 := "conv-finance-1"
	c2 := "conv-profile-1"

	p1 := filepath.Join(tempDir, ".gemini", "antigravity-cli", "brain", c1, ".system_generated", "logs", "transcript.jsonl")
	p2 := filepath.Join(tempDir, ".gemini", "antigravity-cli", "brain", c2, ".system_generated", "logs", "transcript.jsonl")

	_ = os.MkdirAll(filepath.Dir(p1), 0755)
	_ = os.MkdirAll(filepath.Dir(p2), 0755)

	_ = os.WriteFile(p1, []byte("{\"step_index\":1,\"tool_calls\":[{\"name\":\"run_command\",\"args\":{\"Cwd\":\"/home/user/projects/finance-dashboard\"}}]}\n"), 0644)
	_ = os.WriteFile(p2, []byte("{\"step_index\":1,\"tool_calls\":[{\"name\":\"run_command\",\"args\":{\"Cwd\":\"/home/user/projects/powershell-profile\"}}]}\n"), 0644)

	list, err := m.DiscoverSessions()
	if err != nil {
		t.Fatalf("failed to discover: %v", err)
	}
	if len(list) != 2 {
		t.Fatalf("expected 2 sessions, got %d", len(list))
	}

	groups := sessions.GroupSessionsByProject(list)
	if len(groups) != 2 {
		t.Fatalf("expected 2 groups, got %d", len(groups))
	}

	// Test DeleteSession
	if err := m.DeleteSession(c1); err != nil {
		t.Fatalf("failed to delete session %s: %v", c1, err)
	}

	afterDelete, _ := m.DiscoverSessions()
	if len(afterDelete) != 1 {
		t.Fatalf("expected 1 session after delete, got %d", len(afterDelete))
	}
	if afterDelete[0].ConversationID != c2 {
		t.Errorf("expected remaining session to be %s, got %s", c2, afterDelete[0].ConversationID)
	}
}

func TestCleanWorkspaceDir(t *testing.T) {
	tests := []struct {
		input    string
		expected string
	}{
		{"", "Default Workspace"},
		{"Default Workspace", "Default Workspace"},
		{"/home/truongnhon/projects/finance-dashboard", "/home/truongnhon/projects/finance-dashboard"},
		{"/home/truongnhon/projects/finance-dashboard/.agents/skills/project-scripts/SKILL.md", "/home/truongnhon/projects/finance-dashboard"},
		{"/home/truongnhon/projects/finance-dashboard/csharp-sln/Domain/Entities/Account.cs", "/home/truongnhon/projects/finance-dashboard"},
		{"/home/truongnhon/projects/finance-dashboard/ui/src/pages/auth/SignInPage.tsx", "/home/truongnhon/projects/finance-dashboard"},
		{"/home/truongnhon/projects/powershell-profile/apps/agyswitch", "/home/truongnhon/projects/powershell-profile"},
		{"/home/truongnhon/projects/finance-dashboard\\", "/home/truongnhon/projects/finance-dashboard"},
		{"\"/home/truongnhon/projects/finance-dashboard\"", "/home/truongnhon/projects/finance-dashboard"},
	}

	for _, tt := range tests {
		got := sessions.CleanWorkspaceDir(tt.input)
		if got != tt.expected {
			t.Errorf("CleanWorkspaceDir(%q) = %q; want %q", tt.input, got, tt.expected)
		}
	}
}

func TestFlattenProjectGroups(t *testing.T) {
	groups := []model.ProjectGroup{
		{
			ProjectName: "proj-1",
			Sessions: []model.SessionInfo{
				{ConversationID: "c1", ProjectName: "proj-1"},
				{ConversationID: "c2", ProjectName: "proj-1"},
			},
		},
		{
			ProjectName: "proj-2",
			Sessions: []model.SessionInfo{
				{ConversationID: "c3", ProjectName: "proj-2"},
			},
		},
	}

	flat := sessions.FlattenProjectGroups(groups)
	if len(flat) != 3 {
		t.Fatalf("expected 3 sessions, got %d", len(flat))
	}
	if flat[0].ConversationID != "c1" || flat[1].ConversationID != "c2" || flat[2].ConversationID != "c3" {
		t.Errorf("unexpected order: %+v", flat)
	}
}

func TestGroupSessionsByProjectSorted(t *testing.T) {
	sList := []model.SessionInfo{
		{ConversationID: "c1", ProjectName: "proj-1", StepCount: 10, EstimatedCost: 0.10},
		{ConversationID: "c2", ProjectName: "proj-2", StepCount: 100, EstimatedCost: 1.00},
	}

	// Sort by Cost (mode 1)
	byCost := sessions.GroupSessionsByProjectSorted(sList, 1)
	if len(byCost) != 2 || byCost[0].ProjectName != "proj-2" {
		t.Errorf("expected proj-2 first when sorted by cost, got %s", byCost[0].ProjectName)
	}

	// Sort by Steps (mode 2)
	bySteps := sessions.GroupSessionsByProjectSorted(sList, 2)
	if len(bySteps) != 2 || bySteps[0].ProjectName != "proj-2" {
		t.Errorf("expected proj-2 first when sorted by steps, got %s", bySteps[0].ProjectName)
	}
}

func TestDiscoverPrimarySessions_Live(t *testing.T) {
	home, err := os.UserHomeDir()
	if err != nil {
		t.Skip("skipping without home dir")
	}
	m := sessions.NewManager(home)
	list, err := m.DiscoverPrimarySessions()
	if err != nil {
		t.Fatalf("DiscoverPrimarySessions failed: %v", err)
	}
	if len(list) == 0 {
		t.Log("no sessions found in home dir (expected in isolated env)")
		return
	}
	t.Logf("Discovered %d primary sessions. First: %s (%s)", len(list), list[0].ConversationID, list[0].Title)
	for _, s := range list {
		if s.IsSubagent {
			t.Errorf("primary session %s marked as subagent", s.ConversationID)
		}
	}
}


