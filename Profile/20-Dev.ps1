#region GIT HELPER
# ==============================================================================
#  Shortcuts and wrappers for common Git operations.
#
#  Embedded via Invoke-Expression on an escaped-from-parsing string, not defined
#  directly in this script's own AST: BranchCheckoutTui/Gcmt/GenerateAiCommitMessage
#  reference [AgyTui.*] types inside the class body, and PowerShell resolves class-body
#  type refs at parse time. If this class were a literal part of this file, an
#  unresolvable type (pre-PowerShell-7.6, before AgyTuiApp.dll's .NET 10 types can
#  load) would abort parsing of the ENTIRE file — verified empirically: nothing in the
#  whole script would run, not even code physically before the failing class.
#  Invoke-Expression gives this class its own independent parse pass, wrapped in
#  try/catch, so a failure here only disables the git aliases instead of all ~100.
# ==============================================================================
try {
    Invoke-Expression @'
class GitHelper {
    static [void] Status([string[]]$PassThruArgs) {
        Write-Host "[Git] Status" -ForegroundColor Yellow
        if ($PassThruArgs) { git status $PassThruArgs | Out-Default } else { git status | Out-Default }
    }
 
    static [void] Undo() {
        Write-Host "[Git] Undoing last commit (keeping changes)..." -ForegroundColor Yellow
        git reset --soft HEAD~1 | Out-Default
    }
 
    static [void] Unstage() {
        Write-Host "[Git] Unstaging changes..." -ForegroundColor Yellow
        git restore --staged . | Out-Default
    }
 
    static [void] StashSnapshot([string]$Message) {
        $msg = if ($Message) { $Message } else { "snapshot" }
        git stash push -m "$msg" | Out-Default
        git stash apply 0 | Out-Default
        Write-Host "[Git] Snapshot stashed: $msg" -ForegroundColor Green
    }
 
    static [void] Diff() {
        git diff | Out-Default
    }
 
    static [void] AddAll() {
        Write-Host "[Git] Staging all changes..." -ForegroundColor Green
        git add . | Out-Default
    }
 
    static [void] Commit([string[]]$Message) {
        $commitMessage = $Message -join ' '
        Write-Host "[Git] Committing with message: '$commitMessage'" -ForegroundColor Cyan
        git commit -m "$commitMessage" | Out-Default
    }
 
    static [void] Amend([string[]]$PassThruArgs) {
        Write-Host "[Git] Amending previous commit..." -ForegroundColor Cyan
        if ($PassThruArgs) { git commit --amend $PassThruArgs | Out-Default } else { git commit --amend | Out-Default }
    }
 
    static [void] Checkout([string]$branchName) {
        Write-Host "Check out branch: $branchName" -ForegroundColor Green
        git checkout $branchName | Out-Default
    }
 
    static [void] NewBranch([string]$branchName) {
        Write-Host "Check out and create a new branch: $branchName" -ForegroundColor Green
        git checkout -b $branchName | Out-Default
    }
 
    static [void] LogGraph() {
        git log --graph --oneline --decorate --all | Out-Default
    }
 
    static [void] LogPretty() {
        git log --pretty=format:"%C(yellow)%h%Creset -%C(red)%d%Creset %s %C(green)(%cr) %C(bold blue)<%an>%Creset" --abbrev-commit | Out-Default
    }
 
    static [void] Log() {
        git log | Out-Default
    }
 
    static [void] Pull([string[]]$PassThruArgs) {
        Write-Host "[Git] Pulling changes from remote..." -ForegroundColor Blue
        if ($PassThruArgs) { git pull $PassThruArgs | Out-Default } else { git pull | Out-Default }
    }
 
    static [void] Push([string[]]$PassThruArgs) {
        Write-Host "[Git] Pushing changes to remote..." -ForegroundColor Blue
        if ($PassThruArgs) { git push $PassThruArgs | Out-Default } else { git push | Out-Default }
    }
 
    static [void] PushForce([string[]]$PassThruArgs) {
        Write-Host "[Git] Force pushing changes..." -ForegroundColor Red
        if ($PassThruArgs) { git push --force $PassThruArgs | Out-Default } else { git push --force | Out-Default }
    }
 
    static [void] Fetch() {
        Write-Host "[Git] Fetching from all remotes..." -ForegroundColor Blue
        git fetch --all --prune | Out-Default
    }
 
    static [void] GetBranches([string[]]$PassThruArgs) {
        Write-Host "[Git] Branches:" -ForegroundColor Green
        if ($PassThruArgs) { git branch $PassThruArgs | Out-Default } else { git branch | Out-Default }
    }
 
    static [void] RemoveBranch([string]$branchName) {
        git branch -d $branchName | Out-Default
    }
 
    static [void] MergeSquash([string]$BranchName) {
        Write-Host "Squash merging branch: $BranchName" -ForegroundColor Yellow
        git merge --squash $BranchName | Out-Default
        Write-Host "Commit the squashed changes!" -ForegroundColor Cyan
    }
 
    static [void] ResetSoft() {
        git reset HEAD~ | Out-Default
    }
 
    static [void] ResetHard() {
        git reset --hard | Out-Default
    }
 
    static [void] BranchCheckoutTui() {
        if (-not (Test-Path ".git")) {
            Write-Error "Not a git repository."
            return
        }
        $branches = @()
        try {
            $raw = git branch --format="%(refname:short)" 2>$null
            if ($null -ne $raw) {
                foreach ($b in $raw) {
                    if (-not [string]::IsNullOrWhiteSpace($b)) {
                        $branches += $b.Trim()
                    }
                }
            }
        } catch {}
 
        if ($Global:AiMode) {
            foreach ($b in $branches) {
                Write-Host $b
            }
            return
        }
 
        if ($branches.Count -eq 0) {
            Write-Host "No branches found." -ForegroundColor Yellow
            return
        }
 
        $selected = [AgyTui.SpectreMenu]::Show("Select Git Branch to Checkout", $branches, 0)
        if ($selected -ge 0) {
            $bName = $branches[$selected]
            Write-Host "Checking out branch: $bName" -ForegroundColor Green
            git checkout $bName
        } else {
            Write-Host "Cancelled." -ForegroundColor DarkGray
        }
    }
 
    static [string] GenerateAiCommitMessage() {
        if (-not (Test-AgyAiGate)) { return "" }
        $diff = git diff --cached
        if (-not $diff) { return "" }
 
        $prompt = "Generate a concise, one-line conventional commit description (excluding type/scope prefix) based on the following git diff. Output ONLY the description:`n`n$diff"
 
        $body = @{
            model = [AgyTui.AgyAiCore]::OllamaDefaultModel
            prompt = $prompt
            stream = $false
        } | ConvertTo-Json
 
        try {
            $res = Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:11434/api/generate" -Body $body -ContentType "application/json" -TimeoutSec 5
            if ($res.response) {
                $desc = $res.response.Trim()
                $desc = $desc -replace '^(feat|fix|docs|style|refactor|test|chore)(\(.*?\))?:\s*', ''
                return $desc
            }
        } catch {
            Write-Warning "Failed to contact Ollama for AI commit suggestion: $_"
        }
        return ""
    }
 
    static [void] Gcmt([string]$DirectMessage) {
        $staged = git diff --cached --name-only
        if (-not $staged) {
            Write-Warning "No staged changes found. Run git add first."
            return
        }
 
        if ($Global:AiMode -and -not $DirectMessage) {
            Write-Host "Usage: gcmt <message>"
            Write-Host "Supported Commit Types: feat, fix, docs, style, refactor, test, chore"
            return
        }
 
        if ($DirectMessage) {
            Write-Host "Committing with message: '$DirectMessage'" -ForegroundColor Cyan
            git commit -m "$DirectMessage"
            return
        }
 
        $types = @(
            "feat     (New feature)",
            "fix      (Bug fix)",
            "docs     (Documentation changes)",
            "style    (Formatting, missing semi colons, etc)",
            "refactor (Code restructuring without behavior changes)",
            "test     (Adding missing tests)",
            "chore    (Maintenance/dependencies)"
        )
        $sel = [AgyTui.SpectreMenu]::Show("Select Commit Type", $types, 0)
        if ($sel -lt 0) { return }
 
        $type = switch ($sel) {
            0 { "feat" }
            1 { "fix" }
            2 { "docs" }
            3 { "style" }
            4 { "refactor" }
            5 { "test" }
            6 { "chore" }
        }
 
        $scope = Read-Host "Enter commit scope (optional, press Enter to skip)"
        $scope = $scope.Trim()
 
        $desc = Read-Host "Enter commit description (or type 'ai' to auto-generate)"
        if ($desc -eq "ai") {
            Write-Host "[Ollama] Querying Ollama for commit suggestion..." -ForegroundColor Yellow
            $desc = [GitHelper]::GenerateAiCommitMessage()
            if (-not $desc) {
                $desc = Read-Host "Ollama failed to generate message. Please enter description manually"
            } else {
                Write-Host "[Ollama] Generated: $desc" -ForegroundColor Green
            }
        }
 
        if ([string]::IsNullOrWhiteSpace($desc)) {
            Write-Warning "Commit description cannot be empty."
            return
        }
 
        $finalMsg = if ($scope) { "${type}($scope): $desc" } else { "${type}: $desc" }
        Write-Host ""
        Write-Host "Generated commit message: '$finalMsg'" -ForegroundColor Cyan
        $confirm = Read-Host "Confirm commit? (Y/N)"
        if ($confirm -match "^[Yy]") {
            git commit -m "$finalMsg"
        } else {
            Write-Host "Commit cancelled." -ForegroundColor DarkGray
        }
    }
}
'@
} catch {
}
#endregion
 
 

#region DOTNET HELPER
# ==============================================================================
#  Shortcuts and migrations tool wrappers for the .NET SDK.
# ==============================================================================
 
class DotNetHelper {
    static [void] Run([string[]]$PassThruArgs) {
        Write-Host "🚀 Running project..." -ForegroundColor Green
        if ($PassThruArgs) { dotnet run $PassThruArgs | Out-Default } else { dotnet run | Out-Default }
    }
 
    static [void] CleanBinObj() {
        Write-Host "💥 Destroying bin/ and obj/ folders..." -ForegroundColor Red
        $targets = Get-ChildItem -Path . -Recurse -Directory -Include bin,obj -Force -ErrorAction SilentlyContinue
        foreach ($t in $targets) {
            Remove-Item -Path $t.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
        Write-Host "✅ Clean complete." -ForegroundColor Green
    }
 
    static [void] NewSolution([string]$Name) {
        dotnet new sln -n $Name | Out-Default
    }
 
    static [void] AddAllProjectsToSolution() {
        $projects = Get-ChildItem -Recurse -Filter "*.csproj"
        foreach ($p in $projects) {
            dotnet sln add $p.FullName | Out-Default
        }
    }
 
    static [void] WatchTest([string[]]$PassThruArgs) {
        Write-Host "👀 Watching Tests..." -ForegroundColor Yellow
        if ($PassThruArgs) { dotnet watch test $PassThruArgs | Out-Default } else { dotnet watch test | Out-Default }
    }
 
    static [void] Watch([string[]]$PassThruArgs) {
        Write-Host "👀 Watching for changes..." -ForegroundColor Cyan
        if ($PassThruArgs) { dotnet watch $PassThruArgs | Out-Default } else { dotnet watch | Out-Default }
    }
 
    static [void] Build([string[]]$PassThruArgs) {
        Write-Host "🔨 Building project..." -ForegroundColor Blue
        if ($PassThruArgs) { dotnet build $PassThruArgs | Out-Default } else { dotnet build | Out-Default }
    }
 
    static [void] Format([string[]]$PassThruArgs) {
        Write-Host "💅 Formatting code..." -ForegroundColor Magenta
        if ($PassThruArgs) { dotnet format $PassThruArgs | Out-Default } else { dotnet format | Out-Default }
    }
 
    static [void] Test([string[]]$PassThruArgs) {
        Write-Host "🧪 Running tests..." -ForegroundColor Yellow
        if ($PassThruArgs) { dotnet test $PassThruArgs | Out-Default } else { dotnet test | Out-Default }
    }
 
    static [void] Clean([string[]]$PassThruArgs) {
        Write-Host "🧹 Cleaning project..." -ForegroundColor Yellow
        if ($PassThruArgs) { dotnet clean $PassThruArgs | Out-Default } else { dotnet clean | Out-Default }
    }
 
    static [void] Restore([string[]]$PassThruArgs) {
        Write-Host "📦 Restoring packages..." -ForegroundColor Magenta
        if ($PassThruArgs) { dotnet restore $PassThruArgs | Out-Default } else { dotnet restore | Out-Default }
    }
 
    static [void] UpdateDatabase([string]$Context) {
        Write-Host "📈 Updating database..." -ForegroundColor Green
        $params = @("ef", "database", "update")
        if ($Context) { $params += @("--context", $Context) }
        dotnet $params | Out-Default
    }
 
    static [void] AddMigration([string]$MigrationName, [string]$Context) {
        Write-Host "➕ Adding migration: $MigrationName" -ForegroundColor Cyan
        $params = @("ef", "migrations", "add", $MigrationName)
        if ($Context) { $params += @("--context", $Context) }
        dotnet $params | Out-Default
    }
 
    static [void] RemoveDatabase([string]$Context) {
        Write-Host "🔥 Dropping database..." -ForegroundColor Red
        if ((Read-Host "Are you sure? (y/N)") -eq "y") {
            $params = @("ef", "database", "drop", "--force")
            if ($Context) { $params += @("--context", $Context) }
            dotnet $params | Out-Default
            Write-Host "Database dropped."
        } else {
            Write-Host "Cancelled."
        }
    }
 
    static [void] RemoveMigration([string]$Context) {
        Write-Host "⏪ Removing last migration..." -ForegroundColor Yellow
        $params = @("ef", "migrations", "remove")
        if ($Context) { $params += @("--context", $Context) }
        dotnet $params | Out-Default
    }
 
    static [void] NewConsole([string]$Name) {
        dotnet new console -n $Name | Out-Default
    }
 
    static [void] NewWebApi([string]$Name) {
        dotnet new webapi -n $Name | Out-Default
    }
}
#endregion
 


 #region DOCKER HELPER
# ==============================================================================
# Shortcuts and prune utility wrappers for Docker and Docker Compose.
#
# Embedded via Invoke-Expression for the same reason as GitHelper above: Dkcl()
# references [AgyTui.*] types inside the class body.
# ==============================================================================
try {
 Invoke-Expression @'
class DockerHelper {
 static [void] GetContainers([bool]$All) {
 Write-Host "[Docker] Containers:" -ForegroundColor Blue
 if ($All) { docker container ls -a | Out-Default } else { docker container ls | Out-Default }
 }

 static [void] RemoveAllContainers() {
 Write-Host "[Prune] Removing ALL containers..." -ForegroundColor Red
 if ((Read-Host "This will remove ALL containers. Are you sure? (y/N)") -eq 'y') {
 $c = docker ps -aq
 if ($c) { docker rm $c | Out-Default }
 Write-Host "All containers removed."
 } else {
 Write-Host "Cancelled."
 }
 }

 static [void] StopAllContainers() {
 Write-Host "[Stop] Stopping ALL running containers..." -ForegroundColor Yellow
 $c = docker ps -q
 if ($c) { docker stop $c | Out-Default }
 Write-Host "All containers stopped."
 }

 static [void] ComposeUp([string[]]$PassThruArgs) {
 Write-Host "[Compose] Starting Docker Compose..." -ForegroundColor Green
 if ($PassThruArgs) { docker-compose up $PassThruArgs | Out-Default } else { docker-compose up | Out-Default }
 }

 static [void] ComposeUpBuild([string[]]$PassThruArgs) {
 Write-Host "[Compose] Building and starting Docker Compose... " -ForegroundColor Blue
 if ($PassThruArgs) { docker-compose up --build $PassThruArgs | Out-Default } else { docker-compose up --build | Out-Default }
 }

 static [void] ComposeDown([string[]]$PassThruArgs) {
 Write-Host "[Compose] Stopping Docker Compose..." -ForegroundColor Yellow
 if ($PassThruArgs) { docker-compose down $PassThruArgs | Out-Default } else { docker-compose down | Out-Default }
 }

 static [void] RemoveUnusedVolumes() {
 Write-Host "[Prune] Pruning Docker volumes..." -ForegroundColor Magenta
 docker volume prune | Out-Default
 }

 static [void] RemoveUnusedImages() {
 Write-Host "[Prune] Pruning Docker images..." -ForegroundColor Magenta
 docker image prune | Out-Default
 }

 static [void] Dkcl() {
 $containers = @()
 try {
 $open = "{" + "{"
 $close = "}" + "}"
 $fmt = "$open.Names$close:::$open.State$close:::$open.Image$close:::$open.Label 'com.docker.compose.project'$close"
 $raw = docker ps -a --format $fmt 2>$null
 if ($null -ne $raw) {
 foreach ($line in $raw) {
 if ([string]::IsNullOrWhiteSpace($line)) { continue }
 $parts = $line -split ':::'
 $proj = "(Standalone)"
 if ($parts[3]) { $proj = $parts[3] }
 $containers += [PSCustomObject]@{
 Name = $parts[0]
 State = $parts[1]
 Image = $parts[2]
 Project = $proj
 }
 }
 }
 } catch {}

 if ($Global:AiMode) {
 foreach ($c in $containers) {
 Write-Host "$($c.Name),$($c.State),$($c.Image)"
 }
 return
 }

 if ($containers.Count -eq 0) {
 Write-Host "No Docker containers found." -ForegroundColor Yellow
 return
 }

 while ($true) {
 $containers = [AgyTui.SpectreProgress]::SpinnerResult("[Docker] Querying active container configurations...", {
 $cList = @()
 try {
 $open = "{" + "{"
 $close = "}" + "}"
 $fmt = "$open.Names$close:::$open.State$close:::$open.Image$close:::$open.Label 'com.docker.compose.project'$close"
 $raw = docker ps -a --format $fmt 2>$null
 if ($null -ne $raw) {
 foreach ($line in $raw) {
 if ([string]::IsNullOrWhiteSpace($line)) { continue }
 $parts = $line -split ':::'
 $proj = "(Standalone)"
 if ($parts[3]) { $proj = $parts[3] }
 $cList += [PSCustomObject]@{
 Name = $parts[0]
 State = $parts[1]
 Image = $parts[2]
 Project = $proj
 }
 }
 }
 } catch {}
 return $cList
 })

 $grouped = $containers | Group-Object Project
 $menuItems = @()
 $itemMapping = @()
 foreach ($group in $grouped) {
 $menuItems += "[$($group.Name)]"
 $itemMapping += $null
 foreach ($c in $group.Group) {
 $statusIcon = "[-]"
 if ($c.State -eq "running") { $statusIcon = "[+]" }
 $menuItems += " $statusIcon $($c.Name) ($($c.State)) - $($c.Image)"
 $itemMapping += $c
 }
 }
 $menuItems += "[x] Exit Dashboard"
 $itemMapping += $null

 $selected = [AgyTui.SpectreMenu]::Show("Docker Containers Dashboard (dkcl)", $menuItems, 0)
 if ($selected -lt 0 -or $selected -eq ($menuItems.Count - 1)) {
 break
 }

 $c = $itemMapping[$selected]
 if ($null -eq $c) {
 continue
 }

 $subItems = @(
 "[Start] Start Container",
 "[Stop] Stop Container",
 "[Restart] Restart Container",
 "[Logs] View Logs (tail 50)",
 "[Back] Return"
 )
 $subSel = [AgyTui.SpectreMenu]::Show("Manage Container: $($c.Name)", $subItems, 0)
 if ($subSel -eq 0) {
 Write-Host "Starting $($c.Name)..." -ForegroundColor Green
 docker start $c.Name | Out-Null
 }
 elseif ($subSel -eq 1) {
 Write-Host "Stopping $($c.Name)..." -ForegroundColor Yellow
 docker stop $c.Name | Out-Null
 }
 elseif ($subSel -eq 2) {
 Write-Host "Restarting $($c.Name)..." -ForegroundColor Cyan
 docker restart $c.Name | Out-Null
 }
 elseif ($subSel -eq 3) {
 Write-Host "Fetching logs for $($c.Name)..." -ForegroundColor Blue
 $logs = docker logs --tail 50 $c.Name 2>&1
 [AgyTui.SpectrePager]::Show("Logs: $($c.Name)", $logs)
 }
 }
 }
}
'@
} catch {
}
#endregion

#region AWS HELPER
# ==============================================================================
# AWS LocalStack commands and S3/SQS utility wrappers.
# ==============================================================================

class AwsHelper {
 static [string]$LocalStackUrl = "http://localhost:4566"

 static [void] GetS3Buckets() {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) s3 ls | Out-Default
 }

 static [void] NewS3Bucket([string]$Name) {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) s3 mb s3://$Name | Out-Default
 }

 static [void] GetLambdaFunctions() {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) lambda list-functions | Out-Default
 }

 static [void] GetLocalSQSQueues() {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs list-queues | Out-Default
 }

 static [void] NewLocalSQSQueue([string]$QueueName) {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs create-queue --queue-name=$QueueName | Out-Default
 }

 static [void] ClearLocalSQSQueue([string]$QueueUrl) {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs purge-queue --queue-url $QueueUrl | Out-Default
 }

 static [void] SendLocalSQSMessage([string]$QueueUrl, [string]$MessageBody, [string]$GroupId) {
 $gid = if ($GroupId) { $GroupId } else { "default-group" }
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs send-message --queue-url $QueueUrl --message-body $MessageBody --message-group-id $gid | Out-Default
 }

 static [void] GetLocalSQSMessage([string]$QueueUrl) {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs receive-message --queue-url $QueueUrl | Out-Default
 }

 static [void] GetLocalSQSAttributes([string]$QueueUrl) {
 awslocal --endpoint-url=([AwsHelper]::LocalStackUrl) sqs get-queue-attributes --queue-url $QueueUrl --attribute-names All | Out-Default
 }
}
#endregion

