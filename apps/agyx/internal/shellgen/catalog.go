package shellgen

// CommandType defines how a command is structured across shells
type CommandType int

const (
	// SuiteApp: executes an AGYX Go native binary (agydocker, agygit, agyproj, etc.)
	SuiteApp CommandType = iota
	// ExternalCli: executes an external CLI tool directly (dotnet, git, docker, aws)
	ExternalCli
	// AliasMapping: simple alias mapping to another command or function
	AliasMapping
	// ShellFunction: complex function with custom shell implementation
	ShellFunction
)

// AliasSpec represents a unified specification for an alias or command
type AliasSpec struct {
	Name        string      `json:"name"`
	Category    string      `json:"category"`
	Type        CommandType `json:"type"`
	Target      string      `json:"target"`
	Args        []string    `json:"args,omitempty"`
	Description string      `json:"description,omitempty"`
}

// GetSuiteApps returns all native AGYX Go suite binaries
func GetSuiteApps() []string {
	return []string{
		"agyswitch",
		"agyproj",
		"agygit",
		"agydocker",
		"agyterm",
		"agyx",
		"agymobile",
		"agyollama",
	}
}

// GetCatalog returns all unified aliases across both platforms
func GetCatalog() []AliasSpec {
	return []AliasSpec{
		// =========================================================================
		// 1. AGYX DEVELOPER SUITE BINARIES & SHORTCUTS
		// =========================================================================
		{Name: "agys", Category: "Suite", Type: SuiteApp, Target: "agyswitch", Description: "Quick launch Antigravity multi-account vault"},
		{Name: "agysw", Category: "Suite", Type: SuiteApp, Target: "agyswitch", Description: "Alias for agyswitch"},
		{Name: "agy-quota", Category: "Suite", Type: SuiteApp, Target: "agyswitch", Args: []string{"launch-quota"}, Description: "Launch quota monitor"},
		{Name: "agyp", Category: "Suite", Type: SuiteApp, Target: "agyproj", Description: "Quick launch Workspace Navigator"},
		{Name: "agyg", Category: "Suite", Type: SuiteApp, Target: "agygit", Description: "Quick launch Git Fleet manager"},
		{Name: "agyd", Category: "Suite", Type: SuiteApp, Target: "agydocker", Description: "Quick launch Container Fleet manager"},
		{Name: "agyt", Category: "Suite", Type: SuiteApp, Target: "agyterm", Description: "Quick launch Terminal Theme & Font manager"},
		{Name: "agym", Category: "Suite", Type: SuiteApp, Target: "agymobile", Description: "Quick launch Mobile & Tailscale Station"},
		{Name: "agyo", Category: "Suite", Type: SuiteApp, Target: "agyollama", Description: "Quick launch Local Ollama AI Cockpit"},

		// =========================================================================
		// 2. DOCKER & CONTAINER SHORTCUTS
		// =========================================================================
		{Name: "dk", Category: "Docker", Type: ExternalCli, Target: "docker", Args: []string{"ps"}, Description: "List running containers"},
		{Name: "dps", Category: "Docker", Type: ExternalCli, Target: "docker", Args: []string{"ps"}, Description: "List running containers"},
		{Name: "containers", Category: "Docker", Type: ExternalCli, Target: "docker", Args: []string{"ps"}, Description: "List running containers"},
		{Name: "dimg", Category: "Docker", Type: ExternalCli, Target: "docker", Args: []string{"images"}, Description: "List local docker images"},
		{Name: "dlogs", Category: "Docker", Type: ExternalCli, Target: "docker", Args: []string{"logs"}, Description: "View container logs"},
		{Name: "dku", Category: "Docker", Type: SuiteApp, Target: "agydocker", Description: "Interactive Docker container TUI"},
		{Name: "dki", Category: "Docker", Type: SuiteApp, Target: "agydocker", Description: "Interactive Docker container TUI"},
		{Name: "dkcl", Category: "Docker", Type: SuiteApp, Target: "agydocker", Description: "Interactive Docker dashboard"},
		{Name: "dimgu", Category: "Docker", Type: SuiteApp, Target: "agydocker", Description: "Interactive Docker image manager"},
		{Name: "docker-health", Category: "Docker", Type: SuiteApp, Target: "agydocker", Args: []string{"ram"}, Description: "Display WSL2 RAM & Swap usage"},
		{Name: "dkprune", Category: "Docker", Type: SuiteApp, Target: "agydocker", Args: []string{"prune"}, Description: "Prune unused docker resources"},
		{Name: "fix-volume", Category: "Docker", Type: SuiteApp, Target: "agydocker", Args: []string{"prune"}, Description: "Reclaim volume space"},
		{Name: "fix-image", Category: "Docker", Type: SuiteApp, Target: "agydocker", Args: []string{"prune"}, Description: "Reclaim unused image space"},
		{Name: "dkcpu", Category: "Docker", Type: ExternalCli, Target: "docker", Args: []string{"compose", "up"}, Description: "Docker compose up"},
		{Name: "dcup", Category: "Docker", Type: ExternalCli, Target: "docker", Args: []string{"compose", "up"}, Description: "Docker compose up"},
		{Name: "dkcpub", Category: "Docker", Type: ExternalCli, Target: "docker", Args: []string{"compose", "up", "--build"}, Description: "Docker compose up with build"},
		{Name: "dcupb", Category: "Docker", Type: ExternalCli, Target: "docker", Args: []string{"compose", "up", "--build"}, Description: "Docker compose up with build"},
		{Name: "dkcpd", Category: "Docker", Type: ExternalCli, Target: "docker", Args: []string{"compose", "down"}, Description: "Docker compose down"},
		{Name: "dcdown", Category: "Docker", Type: SuiteApp, Target: "agydocker", Args: []string{"down"}, Description: "Compose down or container down"},
		{Name: "dlogsu", Category: "Docker", Type: ShellFunction, Target: "dlogsu", Description: "Tail container logs with agydocker"},
		{Name: "dkstac", Category: "Docker", Type: ShellFunction, Target: "dkstac", Description: "Stop all running containers"},
		{Name: "dkrmac", Category: "Docker", Type: ShellFunction, Target: "dkrmac", Description: "Remove all stopped containers"},

		// =========================================================================
		// 3. GIT & VCS SHORTCUTS
		// =========================================================================
		{Name: "gs", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"status"}, Description: "Git status"},
		{Name: "gsi", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"status"}, Description: "Git status"},
		{Name: "gsu", Category: "Git", Type: SuiteApp, Target: "agygit", Description: "Interactive Git Fleet dashboard"},
		{Name: "gd", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"diff"}, Description: "Git diff"},
		{Name: "glo", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"log", "--graph", "--oneline", "--decorate"}, Description: "Compact git graph"},
		{Name: "glg", Category: "Git", Type: SuiteApp, Target: "agygit", Args: []string{"graph"}, Description: "Interactive Git log graph"},
		{Name: "glog", Category: "Git", Type: SuiteApp, Target: "agygit", Args: []string{"log"}, Description: "Interactive Git log"},
		{Name: "gb", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"branch"}, Description: "List git branches"},
		{Name: "gbr", Category: "Git", Type: SuiteApp, Target: "agygit", Description: "Interactive Git branch switcher"},
		{Name: "gbu", Category: "Git", Type: SuiteApp, Target: "agygit", Description: "Interactive Git branch switcher"},
		{Name: "co", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"checkout"}, Description: "Git checkout"},
		{Name: "cob", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"checkout", "-b"}, Description: "Create & switch to new git branch"},
		{Name: "gbd", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"branch", "-d"}, Description: "Delete git branch"},
		{Name: "ga", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"add", "."}, Description: "Stage all changes"},
		{Name: "gunstage", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"restore", "--staged", "."}, Description: "Unstage all changes"},
		{Name: "gca", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"commit", "--amend"}, Description: "Amend previous commit"},
		{Name: "gundo", Category: "Git", Type: SuiteApp, Target: "agygit", Args: []string{"undo"}, Description: "Undo last commit safely"},
		{Name: "git-undo", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"reset", "--soft", "HEAD~1"}, Description: "Soft reset previous commit"},
		{Name: "gr", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"reset", "--soft", "HEAD~1"}, Description: "Soft reset previous commit"},
		{Name: "grh", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"reset", "--hard"}, Description: "Hard reset working directory"},
		{Name: "gfetch", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"fetch"}, Description: "Fetch latest remote refs"},
		{Name: "gpu", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"push"}, Description: "Push to remote"},
		{Name: "gpush", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"push"}, Description: "Push to remote"},
		{Name: "gpull", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"pull"}, Description: "Pull latest changes"},
		{Name: "guf", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"push", "--force-with-lease"}, Description: "Safe force push"},
		{Name: "gclone", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"clone"}, Description: "Clone repository"},
		{Name: "gremote", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"remote", "-v"}, Description: "Show remote URLs"},
		{Name: "grt", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"remote", "-v"}, Description: "Show remote URLs"},
		{Name: "gco-remote", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"checkout", "-t"}, Description: "Track & checkout remote branch"},
		{Name: "cor", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"checkout", "-t"}, Description: "Track & checkout remote branch"},
		{Name: "gmerge", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"merge"}, Description: "Merge branch"},
		{Name: "gm", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"merge"}, Description: "Merge branch"},
		{Name: "gstash", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"stash"}, Description: "Stash working directory"},
		{Name: "gst", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"stash"}, Description: "Stash working directory"},
		{Name: "grebase", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"rebase"}, Description: "Rebase current branch"},
		{Name: "grb", Category: "Git", Type: ExternalCli, Target: "git", Args: []string{"rebase"}, Description: "Rebase current branch"},
		{Name: "gcmt", Category: "Git", Type: ShellFunction, Target: "gcmt", Description: "Fast commit or interactive commit wizard"},
		{Name: "gmergeu", Category: "Git", Type: ShellFunction, Target: "gmergeu", Description: "Interactive branch merger"},

		// =========================================================================
		// 4. DOTNET SDK & EF CORE SHORTCUTS
		// =========================================================================
		{Name: "dr", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"run"}, Description: "dotnet run"},
		{Name: "dru", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"run"}, Description: "dotnet run"},
		{Name: "dw", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"watch"}, Description: "dotnet watch"},
		{Name: "dwatch", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"watch"}, Description: "dotnet watch"},
		{Name: "db", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"build"}, Description: "dotnet build"},
		{Name: "dbld", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"build"}, Description: "dotnet build"},
		{Name: "dbldu", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"build"}, Description: "dotnet build"},
		{Name: "dbu", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"build"}, Description: "dotnet build"},
		{Name: "rebuild", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"build"}, Description: "dotnet build"},
		{Name: "df", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"format"}, Description: "dotnet format code"},
		{Name: "dfmt", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"format"}, Description: "dotnet format code"},
		{Name: "dt", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"test"}, Description: "dotnet test"},
		{Name: "dtst", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"test"}, Description: "dotnet test"},
		{Name: "dtstu", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"test"}, Description: "dotnet test"},
		{Name: "dtu", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"test"}, Description: "dotnet test"},
		{Name: "dwt", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"watch", "test"}, Description: "dotnet watch test"},
		{Name: "dcl", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"clean"}, Description: "dotnet clean"},
		{Name: "dres", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"restore"}, Description: "dotnet restore"},
		{Name: "drestore", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"restore"}, Description: "dotnet restore"},
		{Name: "dclean", Category: "DotNet", Type: ShellFunction, Target: "dclean", Description: "Purge all bin and obj folders"},
		{Name: "clean-build", Category: "DotNet", Type: AliasMapping, Target: "dclean", Description: "Alias for dclean"},
		{Name: "du", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"ef", "database", "update"}, Description: "EF Core database update"},
		{Name: "update-db", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"ef", "database", "update"}, Description: "EF Core database update"},
		{Name: "da", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"ef", "migrations", "add"}, Description: "EF Core add migration"},
		{Name: "add-migration", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"ef", "migrations", "add"}, Description: "EF Core add migration"},
		{Name: "dd", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"ef", "database", "drop"}, Description: "EF Core database drop"},
		{Name: "dremove", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"ef", "migrations", "remove"}, Description: "EF Core remove migration"},
		{Name: "sln", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"new", "sln"}, Description: "Create new solution"},
		{Name: "sln-add", Category: "DotNet", Type: ShellFunction, Target: "sln-add", Description: "Add all recursive csproj to sln"},
		{Name: "console", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"new", "console"}, Description: "Create console project"},
		{Name: "webapi", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"new", "webapi"}, Description: "Create webapi project"},
		{Name: "dpack", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"pack"}, Description: "dotnet pack"},
		{Name: "dpubpkg", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"pack", "-c", "Release"}, Description: "dotnet pack release"},
		{Name: "dotnet-info", Category: "DotNet", Type: ExternalCli, Target: "dotnet", Args: []string{"--info"}, Description: "Display .NET SDK environment"},

		// =========================================================================
		// 5. AWS & LOCALSTACK SHORTCUTS
		// =========================================================================
		{Name: "aws-whoami", Category: "AWS", Type: ExternalCli, Target: "aws", Args: []string{"sts", "get-caller-identity"}, Description: "AWS identity"},
		{Name: "aws-whoamiu", Category: "AWS", Type: ExternalCli, Target: "aws", Args: []string{"sts", "get-caller-identity"}, Description: "AWS identity"},
		{Name: "aws-s3", Category: "AWS", Type: ExternalCli, Target: "aws", Args: []string{"s3", "ls"}, Description: "List S3 buckets"},
		{Name: "aws-s3u", Category: "AWS", Type: ExternalCli, Target: "aws", Args: []string{"s3", "ls"}, Description: "List S3 buckets"},
		{Name: "s3mb", Category: "AWS", Type: ExternalCli, Target: "aws", Args: []string{"s3", "mb"}, Description: "Create S3 bucket"},
		{Name: "aws-local", Category: "AWS", Type: ExternalCli, Target: "aws", Args: []string{"lambda", "list-functions"}, Description: "List LocalStack lambda functions"},
		{Name: "aws-sqs", Category: "AWS", Type: ExternalCli, Target: "aws", Args: []string{"sqs", "list-queues"}, Description: "List SQS queues"},
		{Name: "sqsmb", Category: "AWS", Type: ExternalCli, Target: "aws", Args: []string{"sqs", "create-queue"}, Description: "Create SQS queue"},
		{Name: "sqspurge", Category: "AWS", Type: ExternalCli, Target: "aws", Args: []string{"sqs", "purge-queue"}, Description: "Purge SQS queue"},
		{Name: "sqssend", Category: "AWS", Type: ExternalCli, Target: "aws", Args: []string{"sqs", "send-message"}, Description: "Send message to SQS"},
		{Name: "sqsrecv", Category: "AWS", Type: ExternalCli, Target: "aws", Args: []string{"sqs", "receive-message"}, Description: "Receive SQS message"},
		{Name: "sqsattr", Category: "AWS", Type: ExternalCli, Target: "aws", Args: []string{"sqs", "get-queue-attributes"}, Description: "Get SQS queue attributes"},

		// =========================================================================
		// 6. COCKPIT, AI & VAULT
		// =========================================================================
		{Name: "ai", Category: "Cockpit", Type: SuiteApp, Target: "agyx", Args: []string{"ai"}, Description: "Launch AI Cockpit"},
		{Name: "cai", Category: "Cockpit", Type: SuiteApp, Target: "agyx", Args: []string{"ai"}, Description: "Launch AI Cockpit"},
		{Name: "cc", Category: "Cockpit", Type: SuiteApp, Target: "agyx", Description: "Launch Master Cockpit"},
		{Name: "ccd", Category: "Cockpit", Type: SuiteApp, Target: "agyx", Description: "Launch Master Cockpit"},
		{Name: "reset-agy", Category: "Cockpit", Type: SuiteApp, Target: "agyswitch", Args: []string{"status"}, Description: "Reset active Antigravity account"},
		{Name: "purge-accounts", Category: "Cockpit", Type: SuiteApp, Target: "agyswitch", Args: []string{"status"}, Description: "Manage accounts in vault"},

		// =========================================================================
		// 7. NAVIGATION & WORKSPACE HOPPER
		// =========================================================================
		{Name: "proj", Category: "Nav", Type: ShellFunction, Target: "proj", Description: "Interactive workspace hopper and registry"},
		{Name: "p", Category: "Nav", Type: AliasMapping, Target: "proj", Description: "Alias for proj"},
		{Name: "cnav", Category: "Nav", Type: AliasMapping, Target: "proj", Description: "Alias for proj"},
		{Name: "ide", Category: "Nav", Type: ShellFunction, Target: "ide", Description: "Open workspace in IDE (VS Code / Cursor / Neovim)"},
		{Name: "theme", Category: "Nav", Type: ShellFunction, Target: "theme", Description: "Switch Oh My Posh shell theme interactively"},
		{Name: "term", Category: "Nav", Type: ShellFunction, Target: "open-term", Description: "Launch new terminal session in current directory"},
		{Name: "wt", Category: "Nav", Type: ShellFunction, Target: "open-term", Description: "Launch Windows Terminal in current directory"},
		{Name: "f", Category: "Nav", Type: ShellFunction, Target: "open-folder", Description: "Open current folder in file explorer"},
		{Name: "ip", Category: "Nav", Type: ShellFunction, Target: "ip-info", Description: "Show IP configuration"},
		{Name: "ip-info", Category: "Nav", Type: ShellFunction, Target: "ip-info", Description: "Show IP configuration"},

		// =========================================================================
		// 8. FILE & UTILITY HELPERS
		// =========================================================================
		{Name: "view", Category: "Util", Type: ShellFunction, Target: "view-file", Description: "Formatted file viewer with line numbers"},
		{Name: "cat-file", Category: "Util", Type: AliasMapping, Target: "view", Description: "Alias for view"},
		{Name: "open", Category: "Util", Type: ShellFunction, Target: "open-file", Description: "Smart opener for files and URLs"},
		{Name: "head-file", Category: "Util", Type: ShellFunction, Target: "head-file", Description: "Formatted head viewer"},
		{Name: "tail-file", Category: "Util", Type: ShellFunction, Target: "tail-file", Description: "Formatted tail viewer"},
		{Name: "ff", Category: "Util", Type: ShellFunction, Target: "find-file", Description: "Fast recursive file finder"},
		{Name: "gf", Category: "Util", Type: ShellFunction, Target: "grep-file", Description: "Fast recursive text searcher"},
		{Name: "clh", Category: "Util", Type: ShellFunction, Target: "clh", Description: "Clear all shell command history"},
	}
}
